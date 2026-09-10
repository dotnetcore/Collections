using System;
using System.Collections;
using System.Collections.Generic;

// CS8714: K1 / K2 are deliberately unconstrained (null key *values* are supported through
// the underlying trie's per-node null branch, but the type parameters themselves must stay
// nullable-friendly, e.g. K1 = string?); the "notnull" key constraint of the annotated
// Dictionary<TKey, TValue> (net5.0+ reference assemblies) is a false positive here.
#pragma warning disable CS8714

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Represents a two-dimensional dictionary: a composite-key dictionary whose keys are
    /// exactly two components, <typeparamref name="K1"/> and <typeparamref name="K2"/>,
    /// mapping to a single <typeparamref name="V"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a thin ergonomic wrapper over <see cref="MultiKeyDictionary{TKey,TValue}"/>
    /// (arity 2): it adds the two-dimensional indexer <c>this[k1, k2]</c> and the per-axis
    /// projections <see cref="GetByFirstKey"/> / <see cref="GetBySecondKey"/>, and otherwise
    /// delegates wholesale. Because the two components may have <em>different</em> types —
    /// which the homogeneous trie can not express — the components are boxed into
    /// <see cref="object"/> before being handed to the trie. The wrapper therefore trades a
    /// small per-operation boxing cost for the strongly-typed two-key surface; when the two
    /// components share a type and the extra allocation matters, use
    /// <see cref="MultiKeyDictionary{TKey,TValue}"/> directly.
    /// </para>
    /// <para>
    /// Key equality is determined per component by the
    /// <see cref="IEqualityComparer{T}"/> values supplied at construction (each defaulting to
    /// <see cref="EqualityComparer{T}.Default"/>). <c>null</c> components are supported.
    /// </para>
    /// <para>
    /// This class is not thread-safe. Wrap it with external synchronization for concurrent use.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var map = new TwoKeyDictionary&lt;int, string, decimal&gt;();
    /// map[1, "USD"] = 1.00m;
    /// map[1, "EUR"] = 0.92m;
    /// map[2, "USD"] = 1.05m;
    ///
    /// decimal rate = map[1, "USD"];                  // 1.00
    /// map.GetByFirstKey(1).Count;                     // 2
    /// map.GetBySecondKey("USD").Count;                // 2
    /// </code>
    /// </example>
    public class TwoKeyDictionary<K1, K2, V> :
        IEnumerable<(K1 Key1, K2 Key2, V Value)>
    {
        private readonly MultiKeyDictionary<object, V> _trie;

        /// <summary>
        /// Wrapper that marks a component as belonging to the first axis. The underlying trie
        /// is homogeneous (<c>MultiKeyDictionary&lt;object, V&gt;</c>) while the two axes are
        /// typed independently, so a bare boxed component is ambiguous whenever
        /// <typeparamref name="K1"/> and <typeparamref name="K2"/> overlap (e.g. the common
        /// <c>&lt;string, string, T&gt;</c> case). Tagging each component with the position it
        /// came from — rather than guessing from its runtime type — is what lets the trie's
        /// comparer dispatch to the right injected comparer deterministically.
        /// </summary>
        private readonly struct Axis
        {
            public Axis(K1 key1)
            {
                Position = 0;
                Value = key1!;
            }

            public Axis(K2 key2)
            {
                Position = 1;
                Value = key2!;
            }

            /// <summary><c>0</c> for the first axis, <c>1</c> for the second.</summary>
            public int Position { get; }

            /// <summary>The boxed component (or <c>null</c> when the component was null).</summary>
            public object Value { get; }
        }

        /// <summary>
        /// Initializes an empty <see cref="TwoKeyDictionary{K1,K2,V}"/> using the default
        /// comparers for both key components.
        /// </summary>
        public TwoKeyDictionary()
            : this((IEqualityComparer<K1>?)null, (IEqualityComparer<K2>?)null)
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="TwoKeyDictionary{K1,K2,V}"/> with the specified
        /// comparers for the two key components.
        /// </summary>
        public TwoKeyDictionary(IEqualityComparer<K1>? comparer1, IEqualityComparer<K2>? comparer2)
        {
            Comparer1 = comparer1 ?? EqualityComparer<K1>.Default;
            Comparer2 = comparer2 ?? EqualityComparer<K2>.Default;
            _trie = new MultiKeyDictionary<object, V>(new AxisComparer(Comparer1, Comparer2));
        }

        /// <summary>
        /// Gets the comparer used for the first key component.
        /// </summary>
        public IEqualityComparer<K1> Comparer1 { get; }

        /// <summary>
        /// Gets the comparer used for the second key component.
        /// </summary>
        public IEqualityComparer<K2> Comparer2 { get; }

        /// <summary>
        /// Gets the number of stored entries.
        /// </summary>
        public int Count => _trie.Count;

        /// <summary>
        /// Gets a value indicating whether the dictionary is empty.
        /// </summary>
        public bool IsEmpty => _trie.IsEmpty;

        /// <summary>
        /// Gets the distinct <typeparamref name="K1"/> values currently in use
        /// (the first-axis projection of the key space).
        /// </summary>
        public IEnumerable<K1> Keys1
        {
            get
            {
                foreach (var branch in _trie.GetBranches(EmptyKey))
                {
                    yield return (K1)((Axis)branch).Value!;
                }
            }
        }

        /// <summary>
        /// Gets the distinct <typeparamref name="K2"/> values currently in use
        /// (the second-axis projection of the key space, de-duplicated across all
        /// <typeparamref name="K1"/> values).
        /// </summary>
        public IEnumerable<K2> Keys2
        {
            get
            {
                var seen = new HashSet<K2>(Comparer2);
                foreach (var entry in _trie)
                {
                    var key2 = Axis2(entry.Key);
                    if (seen.Add(key2))
                    {
                        yield return key2;
                    }
                }
            }
        }

        /// <summary>
        /// Gets every stored (k1, k2, value) entry.
        /// </summary>
        public IEnumerable<(K1 Key1, K2 Key2, V Value)> Entries
        {
            get
            {
                foreach (var entry in _trie)
                {
                    yield return (Axis1(entry.Key), Axis2(entry.Key), entry.Value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the value stored under the (k1, k2) pair. Reading a missing pair raises
        /// <see cref="KeyNotFoundException"/>; writing overwrites any previous value.
        /// </summary>
        /// <example>
        /// <code>
        /// map[1, "USD"] = 1.00m;
        /// decimal v = map[1, "USD"];
        /// </code>
        /// </example>
        public V this[K1 key1, K2 key2]
        {
            get
            {
                if (!TryGetValue(key1, key2, out var value))
                {
                    throw new KeyNotFoundException("The given key was not present in the dictionary.");
                }

                return value;
            }

            set => Add(key1, key2, value);
        }

        // ------------------------------------------------------------------
        // Add
        // ------------------------------------------------------------------

        /// <summary>
        /// Adds a value under the (k1, k2) pair, overwriting any previous value.
        /// </summary>
        /// <example>
        /// <code>
        /// map.Add(1, "USD", 1.00m);
        /// </code>
        /// </example>
        public void Add(K1 key1, K2 key2, V value)
        {
            _trie.Add(ToKey(key1, key2), value);
        }

        /// <summary>
        /// Adds a value under the (k1, k2) pair. When <paramref name="overwrite"/> is
        /// <c>false</c>, an existing entry raises <see cref="ArgumentException"/> instead of
        /// being replaced; the return value reports whether a previous value was replaced.
        /// </summary>
        /// <exception cref="ArgumentException">The pair is already present and <paramref name="overwrite"/> is <c>false</c>.</exception>
        /// <example>
        /// <code>
        /// map.Add(1, "USD", 1.00m, overwrite: false);
        /// </code>
        /// </example>
        public bool Add(K1 key1, K2 key2, V value, bool overwrite)
        {
            return _trie.Add(ToKey(key1, key2), value, overwrite);
        }

        /// <summary>
        /// Adds the (k1, k2, value) entry when the pair is absent. Returns <c>true</c> when the
        /// entry was added, <c>false</c> when the pair was already present (left untouched).
        /// </summary>
        /// <example>
        /// <code>
        /// bool added = map.TryAdd(1, "USD", 1.00m);
        /// </code>
        /// </example>
        public bool TryAdd(K1 key1, K2 key2, V value)
        {
            return _trie.TryAdd(ToKey(key1, key2), value);
        }

        // ------------------------------------------------------------------
        // Lookup
        // ------------------------------------------------------------------

        /// <summary>
        /// Gets the value stored under the (k1, k2) pair.
        /// </summary>
        /// <example>
        /// <code>
        /// if (map.TryGetValue(1, "USD", out var v)) { }
        /// </code>
        /// </example>
        public bool TryGetValue(K1 key1, K2 key2, out V value)
        {
            return _trie.TryGetValue(ToKey(key1, key2), out value);
        }

        /// <summary>
        /// Determines whether an entry exists for the (k1, k2) pair.
        /// </summary>
        /// <example>
        /// <code>
        /// bool has = map.ContainsKey(1, "USD");
        /// </code>
        /// </example>
        public bool ContainsKey(K1 key1, K2 key2)
        {
            return _trie.ContainsKey(ToKey(key1, key2));
        }

        /// <summary>
        /// Determines whether an entry exists for the (k1, k2) pair and holds the specified
        /// value (compared with <see cref="EqualityComparer{T}.Default"/>).
        /// </summary>
        /// <example>
        /// <code>
        /// bool has = map.Contains(1, "USD", 1.00m);
        /// </code>
        /// </example>
        public bool Contains(K1 key1, K2 key2, V value)
        {
            return TryGetValue(key1, key2, out var existing)
                && EqualityComparer<V>.Default.Equals(existing, value);
        }

        // ------------------------------------------------------------------
        // Per-axis projection
        // ------------------------------------------------------------------

        /// <summary>
        /// Determines whether the first axis has the specified value, i.e. whether any entry
        /// exists whose <typeparamref name="K1"/> is <paramref name="key1"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// bool has = map.ContainsFirstKey(1);
        /// </code>
        /// </example>
        public bool ContainsFirstKey(K1 key1)
        {
            return _trie.ContainsPrefix(ToFirstAxisKey(key1));
        }

        /// <summary>
        /// Determines whether the second axis has the specified value under any first key.
        /// </summary>
        /// <example>
        /// <code>
        /// bool has = map.ContainsSecondKey("USD");
        /// </code>
        /// </example>
        public bool ContainsSecondKey(K2 key2)
        {
            foreach (var entry in _trie)
            {
                if (Comparer2.Equals(Axis2(entry.Key), key2))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Enumerates every entry whose first key is <paramref name="key1"/>, i.e. the
        /// first-axis slice of the dictionary. This is the primary reason the type exists:
        /// a plain <c>Dictionary&lt;(K1,K2), V&gt;</c> can not answer it without a full scan.
        /// </summary>
        /// <example>
        /// <code>
        /// foreach (var (k2, value) in map.GetByFirstKey(1)) { }
        /// </code>
        /// </example>
        public IEnumerable<(K2 Key2, V Value)> GetByFirstKey(K1 key1)
        {
            foreach (var entry in _trie.GetByPrefix(ToFirstAxisKey(key1)))
            {
                yield return (Axis2(entry.Key), entry.Value);
            }
        }

        /// <summary>
        /// Enumerates every entry whose second key is <paramref name="key2"/>, i.e. the
        /// second-axis slice of the dictionary.
        /// </summary>
        /// <remarks>
        /// The trie is indexed by <c>(K1, K2)</c> in that order, so the second axis is not a
        /// prefix and can not be resolved by a subtree walk: this method scans every entry and
        /// compares the second component. It is O(n) — see R-02 in the planning notes.
        /// </remarks>
        /// <example>
        /// <code>
        /// foreach (var (k1, value) in map.GetBySecondKey("USD")) { }
        /// </code>
        /// </example>
        public IEnumerable<(K1 Key1, V Value)> GetBySecondKey(K2 key2)
        {
            foreach (var entry in _trie)
            {
                if (Comparer2.Equals(Axis2(entry.Key), key2))
                {
                    yield return (Axis1(entry.Key), entry.Value);
                }
            }
        }

        /// <summary>
        /// Gets the number of entries whose first key is <paramref name="key1"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// int n = map.CountOfFirstKey(1);
        /// </code>
        /// </example>
        public int CountOfFirstKey(K1 key1)
        {
            return _trie.CountOfPrefix(ToFirstAxisKey(key1));
        }

        /// <summary>
        /// Gets the number of entries whose second key is <paramref name="key2"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// int n = map.CountOfSecondKey("USD");
        /// </code>
        /// </example>
        public int CountOfSecondKey(K2 key2)
        {
            var total = 0;
            foreach (var entry in _trie)
            {
                if (Comparer2.Equals(Axis2(entry.Key), key2))
                {
                    total++;
                }
            }

            return total;
        }

        // ------------------------------------------------------------------
        // Remove
        // ------------------------------------------------------------------

        /// <summary>
        /// Removes the entry stored under the (k1, k2) pair. Returns <c>true</c> when an entry
        /// was removed.
        /// </summary>
        /// <example>
        /// <code>
        /// bool removed = map.Remove(1, "USD");
        /// </code>
        /// </example>
        public bool Remove(K1 key1, K2 key2)
        {
            return _trie.Remove(ToKey(key1, key2));
        }

        /// <summary>
        /// Removes the entry stored under the (k1, k2) pair and returns the removed value.
        /// </summary>
        /// <example>
        /// <code>
        /// if (map.Remove(1, "USD", out var value)) { }
        /// </code>
        /// </example>
        public bool Remove(K1 key1, K2 key2, out V value)
        {
            return _trie.Remove(ToKey(key1, key2), out value);
        }

        /// <summary>
        /// Removes every entry whose first key is <paramref name="key1"/> (cascade delete of
        /// the first-axis slice). Returns the number of removed entries.
        /// </summary>
        /// <example>
        /// <code>
        /// int removed = map.RemoveByFirstKey(1);
        /// </code>
        /// </example>
        public int RemoveByFirstKey(K1 key1)
        {
            return _trie.RemovePrefix(ToFirstAxisKey(key1));
        }

        /// <summary>
        /// Removes every entry whose second key is <paramref name="key2"/>, across all first
        /// keys. Returns the number of removed entries.
        /// </summary>
        /// <example>
        /// <code>
        /// int removed = map.RemoveBySecondKey("USD");
        /// </code>
        /// </example>
        public int RemoveBySecondKey(K2 key2)
        {
            var removed = 0;
            foreach (var entry in new List<(object[] Key, V Value)>(_trie))
            {
                if (Comparer2.Equals(Axis2(entry.Key), key2))
                {
                    if (_trie.Remove(entry.Key))
                    {
                        removed++;
                    }
                }
            }

            return removed;
        }

        /// <summary>
        /// Removes every entry.
        /// </summary>
        /// <example>
        /// <code>
        /// map.Clear();
        /// </code>
        /// </example>
        public void Clear()
        {
            _trie.Clear();
        }

        // ------------------------------------------------------------------
        // Copying
        // ------------------------------------------------------------------

        /// <summary>
        /// Creates a copy: key components and values are shared, the structure is independent.
        /// </summary>
        /// <example>
        /// <code>
        /// TwoKeyDictionary&lt;int, string, decimal&gt; copy = map.Clone();
        /// </code>
        /// </example>
        public TwoKeyDictionary<K1, K2, V> Clone()
        {
            var clone = new TwoKeyDictionary<K1, K2, V>(Comparer1, Comparer2);
            foreach (var entry in Entries)
            {
                clone.Add(entry.Key1, entry.Key2, entry.Value);
            }

            return clone;
        }

        /// <summary>
        /// Returns a live read-only view of the dictionary: enumeration reflects subsequent
        /// changes to the owning dictionary. Mutating members are not exposed.
        /// </summary>
        /// <example>
        /// <code>
        /// IReadOnlyCollection&lt;(int, string, decimal)&gt; view = map.AsReadOnly();
        /// </code>
        /// </example>
        public IReadOnlyCollection<(K1 Key1, K2 Key2, V Value)> AsReadOnly()
        {
            return new ReadOnlyView(this);
        }

        /// <summary>
        /// Exports the dictionary as a snapshot keyed by the (k1, k2) tuple. The returned
        /// dictionary is independent of the source.
        /// </summary>
        /// <example>
        /// <code>
        /// IReadOnlyDictionary&lt;(int, string), decimal&gt; snapshot = map.ToDictionary();
        /// </code>
        /// </example>
        public IReadOnlyDictionary<(K1 Key1, K2 Key2), V> ToDictionary()
        {
            var dictionary = new Dictionary<(K1, K2), V>();
            foreach (var entry in Entries)
            {
                dictionary.Add((entry.Key1, entry.Key2), entry.Value);
            }

            return dictionary;
        }

        /// <summary>
        /// Returns the trie that backs this dictionary, rebuilt with plain boxed key
        /// components.
        /// </summary>
        /// <remarks>
        /// The internal trie keys its components with position tags (see <see cref="Axis"/>),
        /// which are an implementation detail; this method projects them away so callers can
        /// use the full <see cref="MultiKeyDictionary{TKey,TValue}"/> surface — including
        /// arbitrary-depth prefix walks past the two key components — without seeing the tags.
        /// It is a <b>copy</b>, not a view: mutations do not propagate in either direction.
        /// Note that plain components lose the axis distinction, so this is only useful when
        /// <typeparamref name="K1"/> and <typeparamref name="K2"/> do not overlap.
        /// </remarks>
        /// <example>
        /// <code>
        /// MultiKeyDictionary&lt;object, decimal&gt; trie = map.AsTrie();
        /// </code>
        /// </example>
        public MultiKeyDictionary<object, V> AsTrie()
        {
            var trie = new MultiKeyDictionary<object, V>();
            foreach (var entry in _trie)
            {
                trie.Add(
                    new object[] { ((Axis)entry.Key[0]).Value, ((Axis)entry.Key[1]).Value },
                    entry.Value);
            }

            return trie;
        }

        // ------------------------------------------------------------------
        // Enumeration
        // ------------------------------------------------------------------

        /// <summary>
        /// Enumerates every (k1, k2, value) entry.
        /// </summary>
        /// <example>
        /// <code>
        /// foreach (var (k1, k2, value) in map) { }
        /// </code>
        /// </example>
        public IEnumerator<(K1 Key1, K2 Key2, V Value)> GetEnumerator()
        {
            return Entries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns the contents as <c>(k1,k2):value</c> pairs, comma separated,
        /// e.g. <c>(1,USD):1.00,(2,EUR):0.92</c>.
        /// </summary>
        /// <example>
        /// <code>
        /// string text = map.ToString();
        /// </code>
        /// </example>
        public override string ToString()
        {
            var parts = new List<string>();
            foreach (var entry in Entries)
            {
                parts.Add($"({entry.Key1},{entry.Key2}):{entry.Value}");
            }

            return string.Join(",", parts);
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        /// <summary>Shared empty key, used for whole-dictionary prefix walks (net451 has no <c>Array.Empty</c>).</summary>
        private static readonly object[] EmptyKey = new object[0];

        private static object[] ToKey(K1 key1, K2 key2)
        {
            return new object[] { new Axis(key1), new Axis(key2) };
        }

        /// <summary>Single-component key for a first-axis prefix walk.</summary>
        private static object[] ToFirstAxisKey(K1 key1)
        {
            return new object[] { new Axis(key1) };
        }

        private static K1 Axis1(object[] key)
        {
            return (K1)((Axis)key[0]).Value!;
        }

        private static K2 Axis2(object[] key)
        {
            return (K2)((Axis)key[1]).Value!;
        }

        /// <summary>
        /// Comparer handed to the underlying trie. Components arrive as <see cref="Axis"/>
        /// tags, so the injected comparer for each position is selected from the tag rather
        /// than from the runtime type — the latter is ambiguous whenever the two axes share a
        /// type. Two components compare equal only when they sit at the same position and the
        /// corresponding comparer agrees.
        /// </summary>
        private sealed class AxisComparer : IEqualityComparer<object>
        {
            private readonly IEqualityComparer<K1> _comparer1;
            private readonly IEqualityComparer<K2> _comparer2;

            public AxisComparer(IEqualityComparer<K1> comparer1, IEqualityComparer<K2> comparer2)
            {
                _comparer1 = comparer1;
                _comparer2 = comparer2;
            }

            public new bool Equals(object? x, object? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                // The trie only ever hands back tags it stored, but be defensive rather than
                // letting a foreign object throw.
                if (!(x is Axis a) || !(y is Axis b) || a.Position != b.Position)
                {
                    return false;
                }

                if (a.Value == null || b.Value == null)
                {
                    return a.Value == null && b.Value == null;
                }

                if (a.Position == 0)
                {
                    return _comparer1.Equals((K1)a.Value, (K1)b.Value);
                }

                return _comparer2.Equals((K2)a.Value, (K2)b.Value);
            }

            public int GetHashCode(object obj)
            {
                if (!(obj is Axis axis) || axis.Value == null)
                {
                    return 0;
                }

                // Mix in the position so the two axes can never collide on hash alone; the
                // trie keys positions separately anyway, but a stable per-position hash makes
                // the invariant explicit.
                var componentHash = axis.Position == 0
                    ? _comparer1.GetHashCode((K1)axis.Value)
                    : _comparer2.GetHashCode((K2)axis.Value);
                return (componentHash * 397) ^ axis.Position;
            }
        }

        private sealed class ReadOnlyView : IReadOnlyCollection<(K1 Key1, K2 Key2, V Value)>
        {
            private readonly TwoKeyDictionary<K1, K2, V> _owner;

            public ReadOnlyView(TwoKeyDictionary<K1, K2, V> owner)
            {
                _owner = owner;
            }

            public int Count => _owner.Count;

            public IEnumerator<(K1 Key1, K2 Key2, V Value)> GetEnumerator()
            {
                return _owner.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
