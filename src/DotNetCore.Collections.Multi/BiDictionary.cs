using System;
using System.Collections;
using System.Collections.Generic;

// CS8714: TLeft / TRight are deliberately unconstrained (null keys are supported on both
// sides through dedicated buckets, e.g. TLeft = string?); the "notnull" key constraint of
// the annotated Dictionary<TKey, TValue> (net5.0+ reference assemblies) is a false positive
// here, exactly as it is for the trie-backed types.
#pragma warning disable CS8714

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Represents a strict one-to-one (bijective) dictionary: every
    /// <typeparamref name="TLeft"/> maps to exactly one <typeparamref name="TRight"/> and no
    /// <typeparamref name="TRight"/> is shared by two <typeparamref name="TLeft"/> values.
    /// Both directions are O(1) lookups.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The map keeps two indexes — <c>left &#8594; right</c> and <c>right &#8594; left</c> —
    /// maintained together on every write path, so <c>left &#8594; right</c> and
    /// <c>right &#8594; left</c> lookups, additions and removals all cost one dictionary
    /// operation. The type implements <see cref="IReadOnlyDictionary{TLeft,TRight}"/> for the
    /// forward direction; the reverse direction is exposed through <see cref="TryGetLeft"/>,
    /// <see cref="GetLeft(TRight)"/>, <see cref="ContainsRight"/> and the live read-only view
    /// returned by <see cref="AsReverse"/>.
    /// </para>
    /// <para>
    /// Conflict handling is <b>strict</b>: <see cref="Add(TLeft,TRight)"/> raises
    /// <see cref="ArgumentException"/> when the left value is already present or when the
    /// right value is already bound to a different left value, and there is deliberately no
    /// silently-overwriting setter. Overwriting a right value would silently unbind the left
    /// value it used to belong to — an entry the caller never mentioned — so breaking an
    /// existing binding is always an explicit <see cref="Remove(TLeft)"/> (or
    /// <see cref="RemoveRight(TRight)"/>) first. Callers that prefer to test rather than
    /// catch use <see cref="TryAdd"/>, which reports the same conditions without throwing.
    /// </para>
    /// <para>
    /// <c>null</c> is accepted on both sides. Because
    /// <see cref="Dictionary{TKey,TValue}"/> rejects a <c>null</c> key, each side gets a
    /// dedicated <c>null</c> bucket (the same strategy the trie-backed types use); the
    /// binding <c>(null, null)</c> is therefore expressible and occupies one entry. Note that
    /// <see cref="ToDictionary"/> can not carry a <c>null</c> left value into its result for
    /// the same reason and omits such an entry.
    /// </para>
    /// <para>
    /// Within the package taxonomy this type is orthogonal to the "multi" family: nothing
    /// repeats. It is the one-to-one counterpart of
    /// <see cref="MultiDictionary{TKey,TValue}"/> (one key &#8594; many values) and of
    /// <see cref="MultiKeyDictionary{TKey,TValue}"/> (many key components &#8594; one value).
    /// </para>
    /// <para>
    /// This class is not thread-safe. Wrap it with external synchronization for concurrent use.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var users = new BiDictionary&lt;int, string&gt;();
    /// users.Add(1, "alice");
    /// users.Add(2, "bob");
    ///
    /// string name = users[1];          // "alice"
    /// int id = users.GetLeft("bob");   // 2
    ///
    /// users.TryAdd(3, "bob");          // false - "bob" is already bound to 2
    /// users.Add(3, "bob");             // throws ArgumentException
    /// users.Remove(2);                 // frees "bob" explicitly
    /// users.TryAdd(3, "bob");          // true
    /// </code>
    /// </example>
    public class BiDictionary<TLeft, TRight> : IReadOnlyDictionary<TLeft, TRight>
    {
        private readonly Dictionary<TLeft, TRight> _forward;
        private readonly Dictionary<TRight, TLeft> _reverse;

        /// <summary>
        /// The <c>null</c> left bucket, held outside <see cref="_forward"/> because
        /// <see cref="Dictionary{TKey,TValue}"/> rejects a <c>null</c> key.
        /// <see cref="_hasNullLeft"/> says whether the bucket is occupied;
        /// <see cref="_nullLeftRight"/> holds the bound right value, which may itself be
        /// <c>null</c> (the <c>(null, null)</c> binding).
        /// </summary>
        private bool _hasNullLeft;
        private TRight _nullLeftRight = default!;

        /// <summary>
        /// The <c>null</c> right bucket, the mirror of <see cref="_hasNullLeft"/> /
        /// <see cref="_nullLeftRight"/> on the reverse index.
        /// </summary>
        private bool _hasNullRight;
        private TLeft _nullRightLeft = default!;

        /// <summary>
        /// Initializes an empty <see cref="BiDictionary{TLeft,TRight}"/> using the default
        /// comparers for both sides.
        /// </summary>
        public BiDictionary()
            : this((IEqualityComparer<TLeft>?)null, (IEqualityComparer<TRight>?)null)
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="BiDictionary{TLeft,TRight}"/> with the specified
        /// comparers for the two sides.
        /// </summary>
        public BiDictionary(IEqualityComparer<TLeft>? leftComparer, IEqualityComparer<TRight>? rightComparer)
        {
            LeftComparer = leftComparer ?? EqualityComparer<TLeft>.Default;
            RightComparer = rightComparer ?? EqualityComparer<TRight>.Default;
            _forward = new Dictionary<TLeft, TRight>(LeftComparer);
            _reverse = new Dictionary<TRight, TLeft>(RightComparer);
        }

        /// <summary>
        /// Gets the comparer used for the left side.
        /// </summary>
        public IEqualityComparer<TLeft> LeftComparer { get; }

        /// <summary>
        /// Gets the comparer used for the right side.
        /// </summary>
        public IEqualityComparer<TRight> RightComparer { get; }

        /// <summary>
        /// Gets the number of stored bindings.
        /// </summary>
        /// <remarks>
        /// Every binding has exactly one left value, so the count is the non-null forward
        /// index plus at most one <c>null</c>-left bucket — no separate counter to keep in step.
        /// </remarks>
        public int Count => _forward.Count + (_hasNullLeft ? 1 : 0);

        /// <summary>
        /// Gets a value indicating whether the dictionary is empty.
        /// </summary>
        public bool IsEmpty => Count == 0;

        // ------------------------------------------------------------------
        // Forward surface (IReadOnlyDictionary<TLeft, TRight>)
        // ------------------------------------------------------------------

        /// <summary>
        /// Gets the right value bound to <paramref name="left"/>. Reading a missing left value
        /// raises <see cref="KeyNotFoundException"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// string name = users[1];
        /// </code>
        /// </example>
        public TRight this[TLeft left]
        {
            get
            {
                if (!TryGetValue(left, out var right))
                {
                    throw new KeyNotFoundException("The given left value was not present in the dictionary.");
                }

                return right;
            }
        }

        /// <summary>
        /// Gets the left values currently in use, including a <c>null</c> entry when the
        /// <c>null</c> bucket is occupied.
        /// </summary>
        public IEnumerable<TLeft> Keys
        {
            get
            {
                foreach (var pair in this)
                {
                    yield return pair.Key;
                }
            }
        }

        /// <summary>
        /// Gets the right values currently in use, including a <c>null</c> entry when the
        /// <c>null</c>-right bucket is occupied.
        /// </summary>
        public IEnumerable<TRight> Values
        {
            get
            {
                foreach (var pair in this)
                {
                    yield return pair.Value;
                }
            }
        }

        /// <summary>
        /// Determines whether a binding exists for <paramref name="left"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// bool has = users.ContainsKey(1);
        /// </code>
        /// </example>
        public bool ContainsKey(TLeft left)
        {
            return left == null ? _hasNullLeft : _forward.ContainsKey(left);
        }

        /// <summary>
        /// Determines whether a binding exists for <paramref name="right"/>, i.e. whether
        /// <paramref name="right"/> is currently bound to any left value.
        /// </summary>
        /// <example>
        /// <code>
        /// bool has = users.ContainsRight("alice");
        /// </code>
        /// </example>
        public bool ContainsRight(TRight right)
        {
            return right == null ? _hasNullRight : _reverse.ContainsKey(right);
        }

        /// <summary>
        /// Gets the right value bound to <paramref name="left"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// if (users.TryGetValue(1, out var name)) { }
        /// </code>
        /// </example>
        public bool TryGetValue(TLeft left, out TRight right)
        {
            if (left == null)
            {
                right = _nullLeftRight;
                return _hasNullLeft;
            }

            // Branching instead of forwarding the call outright: TryGetValue's out parameter is
            // annotated [MaybeNullWhen(false)], and a blind forward makes the compiler flag the
            // (unannotated) out parameter of this method as possibly null-assigned (CS8601).
            if (_forward.TryGetValue(left, out var found))
            {
                right = found;
                return true;
            }

            right = default!;
            return false;
        }

        // ------------------------------------------------------------------
        // Reverse surface
        // ------------------------------------------------------------------

        /// <summary>
        /// Gets the left value bound to <paramref name="right"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// if (users.TryGetLeft("bob", out var id)) { }
        /// </code>
        /// </example>
        public bool TryGetLeft(TRight right, out TLeft left)
        {
            if (right == null)
            {
                left = _nullRightLeft;
                return _hasNullRight;
            }

            // Same branch-not-forward shape as TryGetValue (avoids CS8601 on the annotated
            // out parameter of Dictionary.TryGetValue).
            if (_reverse.TryGetValue(right, out var found))
            {
                left = found;
                return true;
            }

            left = default!;
            return false;
        }

        /// <summary>
        /// Gets the left value bound to <paramref name="right"/>. Reading an unbound right
        /// value raises <see cref="KeyNotFoundException"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// int id = users.GetLeft("bob");
        /// </code>
        /// </example>
        public TLeft GetLeft(TRight right)
        {
            if (!TryGetLeft(right, out var left))
            {
                throw new KeyNotFoundException("The given right value was not present in the dictionary.");
            }

            return left;
        }

        /// <summary>
        /// Returns a live read-only view of the reverse (right &#8594; left) direction:
        /// enumeration and lookups reflect subsequent changes to the owning dictionary, and
        /// mutating members are not exposed.
        /// </summary>
        /// <remarks>
        /// The view is the read side of the same two indexes the dictionary already maintains —
        /// it allocates nothing per operation beyond the wrapper itself. Enumeration order is
        /// the forward index's insertion order projected to <c>(right, left)</c> pairs.
        /// </remarks>
        /// <example>
        /// <code>
        /// IReadOnlyDictionary&lt;string, int&gt; byName = users.AsReverse();
        /// int id = byName["alice"];
        /// </code>
        /// </example>
        public IReadOnlyDictionary<TRight, TLeft> AsReverse()
        {
            return new ReverseView(this);
        }

        // ------------------------------------------------------------------
        // Add
        // ------------------------------------------------------------------

        /// <summary>
        /// Adds a binding from <paramref name="left"/> to <paramref name="right"/>.
        /// </summary>
        /// <remarks>
        /// The left-value check runs first, so when both sides conflict the exception reports
        /// the duplicate left value; a right-value exception therefore always means the right
        /// value is bound to a <em>different</em> left value.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// <paramref name="left"/> already has a binding, or <paramref name="right"/> is
        /// already bound to a different left value.
        /// </exception>
        /// <example>
        /// <code>
        /// users.Add(1, "alice");
        /// </code>
        /// </example>
        public void Add(TLeft left, TRight right)
        {
            if (ContainsKey(left))
            {
                throw new ArgumentException("An item with the same left value has already been added.");
            }

            if (ContainsRight(right))
            {
                throw new ArgumentException(
                    "The given right value is already bound to a different left value. " +
                    "Remove the existing binding first; BiDictionary never overwrites silently.");
            }

            Write(left, right);
        }

        /// <summary>
        /// Adds the binding when neither side is already in use. Returns <c>true</c> when the
        /// binding was added, <c>false</c> when <paramref name="left"/> already has a binding
        /// or <paramref name="right"/> is already bound to a different left value (both left
        /// untouched, nothing thrown).
        /// </summary>
        /// <example>
        /// <code>
        /// bool added = users.TryAdd(3, "bob");
        /// </code>
        /// </example>
        public bool TryAdd(TLeft left, TRight right)
        {
            if (ContainsKey(left) || ContainsRight(right))
            {
                return false;
            }

            Write(left, right);
            return true;
        }

        /// <summary>
        /// Writes the binding to both indexes (and the <c>null</c> buckets). Only called after
        /// both sides have been checked free, so no conflict can reach this point.
        /// </summary>
        private void Write(TLeft left, TRight right)
        {
            if (left == null)
            {
                _hasNullLeft = true;
                _nullLeftRight = right;
            }
            else
            {
                _forward.Add(left, right);
            }

            if (right == null)
            {
                _hasNullRight = true;
                _nullRightLeft = left;
            }
            else
            {
                _reverse.Add(right, left);
            }
        }

        // ------------------------------------------------------------------
        // Remove
        // ------------------------------------------------------------------

        /// <summary>
        /// Removes the binding of <paramref name="left"/> (and frees its right value for a
        /// new binding). Returns <c>true</c> when a binding was removed.
        /// </summary>
        /// <example>
        /// <code>
        /// bool removed = users.Remove(1);
        /// </code>
        /// </example>
        public bool Remove(TLeft left)
        {
            TRight right;
            if (left == null)
            {
                if (!_hasNullLeft)
                {
                    return false;
                }

                right = _nullLeftRight;
                _hasNullLeft = false;
                _nullLeftRight = default!;
            }
            else
            {
                if (!_forward.TryGetValue(left, out right))
                {
                    return false;
                }

                _forward.Remove(left);
            }

            if (right == null)
            {
                _hasNullRight = false;
                _nullRightLeft = default!;
            }
            else
            {
                // The two indexes are kept in step on every write path, so the reverse entry
                // must be present; Remove's return value is deliberately not asserted.
                _reverse.Remove(right);
            }

            return true;
        }

        /// <summary>
        /// Removes the binding of <paramref name="right"/> (and frees its left value for a new
        /// binding). Returns <c>true</c> when a binding was removed.
        /// </summary>
        /// <example>
        /// <code>
        /// bool removed = users.RemoveRight("bob");
        /// </code>
        /// </example>
        public bool RemoveRight(TRight right)
        {
            TLeft left;
            if (right == null)
            {
                if (!_hasNullRight)
                {
                    return false;
                }

                left = _nullRightLeft;
                _hasNullRight = false;
                _nullRightLeft = default!;
            }
            else
            {
                if (!_reverse.TryGetValue(right, out left))
                {
                    return false;
                }

                _reverse.Remove(right);
            }

            if (left == null)
            {
                _hasNullLeft = false;
                _nullLeftRight = default!;
            }
            else
            {
                _forward.Remove(left);
            }

            return true;
        }

        /// <summary>
        /// Removes every binding.
        /// </summary>
        /// <example>
        /// <code>
        /// users.Clear();
        /// </code>
        /// </example>
        public void Clear()
        {
            _forward.Clear();
            _reverse.Clear();
            _hasNullLeft = false;
            _nullLeftRight = default!;
            _hasNullRight = false;
            _nullRightLeft = default!;
        }

        // ------------------------------------------------------------------
        // Copying and export
        // ------------------------------------------------------------------

        /// <summary>
        /// Creates a copy: keys and values are shared, the structure is independent.
        /// </summary>
        /// <example>
        /// <code>
        /// BiDictionary&lt;int, string&gt; copy = users.Clone();
        /// </code>
        /// </example>
        public BiDictionary<TLeft, TRight> Clone()
        {
            var clone = new BiDictionary<TLeft, TRight>(LeftComparer, RightComparer);
            foreach (var pair in this)
            {
                clone.Add(pair.Key, pair.Value);
            }

            return clone;
        }

        /// <summary>
        /// Exports the dictionary as a snapshot keyed by the left value. The returned
        /// dictionary is independent of the source.
        /// </summary>
        /// <remarks>
        /// A binding whose left value is <c>null</c> can not be represented as a
        /// <see cref="Dictionary{TKey,TValue}"/> key and is <b>omitted</b> from the result;
        /// every non-null left value is carried verbatim. The reverse direction is not
        /// exported — rebuild it with <see cref="AsReverse"/> or by reading the result backwards.
        /// </remarks>
        /// <example>
        /// <code>
        /// IReadOnlyDictionary&lt;int, string&gt; snapshot = users.ToDictionary();
        /// </code>
        /// </example>
        public IReadOnlyDictionary<TLeft, TRight> ToDictionary()
        {
            var dictionary = new Dictionary<TLeft, TRight>(LeftComparer);
            foreach (var pair in _forward)
            {
                dictionary.Add(pair.Key, pair.Value);
            }

            return dictionary;
        }

        // ------------------------------------------------------------------
        // Enumeration
        // ------------------------------------------------------------------

        /// <summary>
        /// Enumerates every (left, right) binding, including a <c>(null, right)</c> entry when
        /// the <c>null</c>-left bucket is occupied.
        /// </summary>
        /// <example>
        /// <code>
        /// foreach (var pair in users) { }
        /// </code>
        /// </example>
        public IEnumerator<KeyValuePair<TLeft, TRight>> GetEnumerator()
        {
            foreach (var pair in _forward)
            {
                yield return pair;
            }

            if (_hasNullLeft)
            {
                yield return new KeyValuePair<TLeft, TRight>(default!, _nullLeftRight);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns the contents as <c>left:right</c> pairs, comma separated,
        /// e.g. <c>1:alice,2:bob</c>.
        /// </summary>
        /// <example>
        /// <code>
        /// string text = users.ToString();
        /// </code>
        /// </example>
        public override string ToString()
        {
            var parts = new List<string>();
            foreach (var pair in this)
            {
                parts.Add($"{pair.Key}:{pair.Value}");
            }

            return string.Join(",", parts);
        }

        // ------------------------------------------------------------------
        // Views
        // ------------------------------------------------------------------

        /// <summary>
        /// Live read-only view of the reverse (right &#8594; left) direction. Everything is
        /// answered from the owning dictionary's existing state, so the view can never drift
        /// from the source: adds and removals on the owner are visible immediately.
        /// </summary>
        private sealed class ReverseView : IReadOnlyDictionary<TRight, TLeft>
        {
            private readonly BiDictionary<TLeft, TRight> _owner;

            public ReverseView(BiDictionary<TLeft, TRight> owner)
            {
                _owner = owner;
            }

            public int Count => _owner.Count;

            public IEnumerable<TRight> Keys => _owner.Values;

            public IEnumerable<TLeft> Values => _owner.Keys;

            public TLeft this[TRight right]
            {
                get
                {
                    if (!_owner.TryGetLeft(right, out var left))
                    {
                        throw new KeyNotFoundException("The given right value was not present in the dictionary.");
                    }

                    return left;
                }
            }

            public bool ContainsKey(TRight right)
            {
                return _owner.ContainsRight(right);
            }

            public bool TryGetValue(TRight right, out TLeft left)
            {
                return _owner.TryGetLeft(right, out left);
            }

            public IEnumerator<KeyValuePair<TRight, TLeft>> GetEnumerator()
            {
                foreach (var pair in _owner)
                {
                    yield return new KeyValuePair<TRight, TLeft>(pair.Value, pair.Key);
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
