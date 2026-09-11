using System;
using System.Collections;
using System.Collections.Generic;

// CS8714: K1..K3 are deliberately unconstrained, matching TwoKeyDictionary<K1, K2, V>
// (a null component is an ordinary component, hashed to 0 and equal only to itself).
#pragma warning disable CS8714

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Represents a dictionary whose key is a composite of exactly three differently typed
    /// components, with a typed indexer and per-axis projections. This is a thin ergonomic wrapper
    /// over <see cref="MultiKeyDictionary{TKey,TValue}"/>, the same shape
    /// <see cref="TwoKeyDictionary{K1,K2,V}"/> takes: it hides the trie's homogeneous
    /// <c>object[]</c> key behind <c>(k1, k2, k3)</c> tuples, exposes the first-axis prefix
    /// queries (<see cref="GetByFirstKey(K1)"/> and friends) the trie natively answers, and
    /// otherwise stays out of the way.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The wrapper follows the same axis-tag scheme as <see cref="TwoKeyDictionary{K1,K2,V}"/>:
    /// each component is stored in the trie wrapped in a tag that records the position it came
    /// from, so the trie's comparer can dispatch to the right injected comparer even when the
    /// three axes share a runtime type (e.g. the common <c>&lt;string, string, string, T&gt;</c>
    /// case).
    /// </para>
    /// <para>
    /// The trie is indexed by <c>(K1, K2, K3)</c> in that order, so only the first axis is a
    /// prefix: the second and third axis slices are answered by scanning every entry, which is
    /// O(n). (<see cref="TwoKeyDictionary{K1,K2,V}"/> carries a reverse index for its second
    /// axis; this wrapper deliberately does not replicate that machinery - when the second and
    /// third axes need indexed lookups, use <see cref="MultiKeyDictionary{TKey,TValue}"/> with an
    /// explicit key order that puts the queried axis first.)
    /// </para>
    /// <para>
    /// This class is not thread-safe. Wrap it with external synchronization for concurrent use.
    /// </para>
    /// </remarks>
    public class ThreeKeyDictionary<K1, K2, K3, V> :
        IEnumerable<(K1 Key1, K2 Key2, K3 Key3, V Value)>
    {
        private readonly MultiKeyDictionary<object, V> _trie;

        /// <summary>
        /// Wrapper that marks a component as belonging to a given axis. The underlying trie is
        /// homogeneous (<c>MultiKeyDictionary&lt;object, V&gt;</c>) while the three axes are typed
        /// independently, so a bare boxed component is ambiguous whenever the axis types overlap.
        /// Tagging each component with the position it came from - rather than guessing from its
        /// runtime type - is what lets the trie's comparer dispatch to the right injected
        /// comparer deterministically. Same scheme as <see cref="TwoKeyDictionary{K1,K2,V}"/>.
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

            public Axis(K3 key3)
            {
                Position = 2;
                Value = key3!;
            }

            /// <summary><c>0</c> for the first axis, <c>1</c> for the second, <c>2</c> for the third.</summary>
            public int Position { get; }

            /// <summary>The boxed component (or <c>null</c> when the component was null).</summary>
            public object Value { get; }
        }

        /// <summary>
        /// Initializes an empty <see cref="ThreeKeyDictionary{K1,K2,K3,V}"/> using the default
        /// comparers for all three key components.
        /// </summary>
        public ThreeKeyDictionary()
            : this((IEqualityComparer<K1>?)null, (IEqualityComparer<K2>?)null, (IEqualityComparer<K3>?)null)
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="ThreeKeyDictionary{K1,K2,K3,V}"/> with the specified
        /// comparers for the three key components.
        /// </summary>
        public ThreeKeyDictionary(
            IEqualityComparer<K1>? comparer1, IEqualityComparer<K2>? comparer2, IEqualityComparer<K3>? comparer3)
        {
            Comparer1 = comparer1 ?? EqualityComparer<K1>.Default;
            Comparer2 = comparer2 ?? EqualityComparer<K2>.Default;
            Comparer3 = comparer3 ?? EqualityComparer<K3>.Default;
            _trie = new MultiKeyDictionary<object, V>(new AxisComparer(Comparer1, Comparer2, Comparer3));
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
        /// Gets the comparer used for the third key component.
        /// </summary>
        public IEqualityComparer<K3> Comparer3 { get; }

        /// <summary>
        /// Gets the number of stored entries.
        /// </summary>
        public int Count => _trie.Count;

        /// <summary>
        /// Gets a value indicating whether the dictionary is empty.
        /// </summary>
        public bool IsEmpty => _trie.IsEmpty;

        /// <summary>
        /// Gets the distinct <typeparamref name="K1"/> values currently in use (the first-axis
        /// projection of the key space).
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
        /// Gets the distinct <typeparamref name="K2"/> values currently in use (the second-axis
        /// projection of the key space, de-duplicated across all first components).
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
        /// Gets the distinct <typeparamref name="K3"/> values currently in use (the third-axis
        /// projection of the key space, de-duplicated across all first and second components).
        /// </summary>
        public IEnumerable<K3> Keys3
        {
            get
            {
                var seen = new HashSet<K3>(Comparer3);
                foreach (var entry in _trie)
                {
                    var key3 = Axis3(entry.Key);
                    if (seen.Add(key3))
                    {
                        yield return key3;
                    }
                }
            }
        }

        /// <summary>
        /// Gets every stored (k1, k2, k3, value) entry.
        /// </summary>
        public IEnumerable<(K1 Key1, K2 Key2, K3 Key3, V Value)> Entries
        {
            get
            {
                foreach (var entry in _trie)
                {
                    yield return (Axis1(entry.Key), Axis2(entry.Key), Axis3(entry.Key), entry.Value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the value stored under the (k1, k2, k3) triple. Reading a missing key
        /// raises <see cref="KeyNotFoundException"/>; writing overwrites any previous value.
        /// </summary>
        /// <example>
        /// <code>
        /// map[2026, 9, 11] = 42;
        /// </code>
        /// </example>
        public V this[K1 key1, K2 key2, K3 key3]
        {
            get
            {
                if (!TryGetValue(key1, key2, key3, out var value))
                {
                    throw new KeyNotFoundException("The given key was not present in the dictionary.");
                }

                return value;
            }

            set => Add(key1, key2, key3, value);
        }

        // ------------------------------------------------------------------
        // Add
        // ------------------------------------------------------------------

        /// <summary>
        /// Adds a value under the (k1, k2, k3) triple, overwriting any previous value.
        /// </summary>
        public void Add(K1 key1, K2 key2, K3 key3, V value)
        {
            _trie.Add(ToKey(key1, key2, key3), value);
        }

        /// <summary>
        /// Adds a value under the (k1, k2, k3) triple. When <paramref name="overwrite"/> is
        /// <c>false</c>, an existing entry raises <see cref="ArgumentException"/> instead of being
        /// replaced; the return value reports whether a previous value was replaced.
        /// </summary>
        /// <exception cref="ArgumentException">The triple is already present and <paramref name="overwrite"/> is <c>false</c>.</exception>
        public bool Add(K1 key1, K2 key2, K3 key3, V value, bool overwrite)
        {
            return _trie.Add(ToKey(key1, key2, key3), value, overwrite);
        }

        /// <summary>
        /// Adds the (k1, k2, k3, value) entry when the key is absent. Returns <c>true</c> when the
        /// entry was added, <c>false</c> when the triple was already present (left untouched).
        /// </summary>
        public bool TryAdd(K1 key1, K2 key2, K3 key3, V value)
        {
            return _trie.TryAdd(ToKey(key1, key2, key3), value);
        }

        // ------------------------------------------------------------------
        // Lookup
        // ------------------------------------------------------------------

        /// <summary>
        /// Gets the value stored under the (k1, k2, k3) triple.
        /// </summary>
        public bool TryGetValue(K1 key1, K2 key2, K3 key3, out V value)
        {
            return _trie.TryGetValue(ToKey(key1, key2, key3), out value);
        }

        /// <summary>
        /// Determines whether an entry exists for the (k1, k2, k3) triple.
        /// </summary>
        public bool ContainsKey(K1 key1, K2 key2, K3 key3)
        {
            return _trie.ContainsKey(ToKey(key1, key2, key3));
        }

        /// <summary>
        /// Determines whether an entry exists for the (k1, k2, k3) triple and holds the specified
        /// value (compared with <see cref="EqualityComparer{T}.Default"/>).
        /// </summary>
        public bool Contains(K1 key1, K2 key2, K3 key3, V value)
        {
            return TryGetValue(key1, key2, key3, out var existing)
                && EqualityComparer<V>.Default.Equals(existing, value);
        }

        // ------------------------------------------------------------------
        // Per-axis projection
        // ------------------------------------------------------------------

        /// <summary>
        /// Determines whether the first axis has the specified value, i.e. whether any entry
        /// exists whose <typeparamref name="K1"/> is <paramref name="key1"/>. This is a prefix
        /// walk, the trie natively answers it.
        /// </summary>
        public bool ContainsFirstKey(K1 key1)
        {
            return _trie.ContainsPrefix(ToFirstAxisKey(key1));
        }

        /// <summary>
        /// Determines whether the second axis has the specified value under any first key.
        /// </summary>
        /// <remarks>
        /// The second axis is not a prefix, so this scans every entry: O(n). See the type
        /// remarks for the wrapper's deliberate scope.
        /// </remarks>
        public bool ContainsSecondKey(K2 key2)
        {
            foreach (var key2Seen in Keys2)
            {
                if (Comparer2.Equals(key2Seen, key2))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether the third axis has the specified value under any (k1, k2) pair.
        /// </summary>
        /// <remarks>
        /// The third axis is not a prefix, so this scans every entry: O(n).
        /// </remarks>
        public bool ContainsThirdKey(K3 key3)
        {
            foreach (var key3Seen in Keys3)
            {
                if (Comparer3.Equals(key3Seen, key3))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Enumerates every entry whose first key is <paramref name="key1"/>, i.e. the first-axis
        /// slice. This is the primary reason the type exists: a plain
        /// <c>Dictionary&lt;(K1,K2,K3), V&gt;</c> can not answer it without a full scan.
        /// </summary>
        public IEnumerable<(K2 Key2, K3 Key3, V Value)> GetByFirstKey(K1 key1)
        {
            foreach (var entry in _trie.GetByPrefix(ToFirstAxisKey(key1)))
            {
                yield return (Axis2(entry.Key), Axis3(entry.Key), entry.Value);
            }
        }

        /// <summary>
        /// Enumerates every entry whose second key is <paramref name="key2"/>, across all first
        /// keys.
        /// </summary>
        /// <remarks>
        /// The second axis is not a prefix, so this scans every entry: O(n).
        /// </remarks>
        public IEnumerable<(K1 Key1, K3 Key3, V Value)> GetBySecondKey(K2 key2)
        {
            foreach (var entry in _trie)
            {
                if (Comparer2.Equals(Axis2(entry.Key), key2))
                {
                    yield return (Axis1(entry.Key), Axis3(entry.Key), entry.Value);
                }
            }
        }

        /// <summary>
        /// Enumerates every entry whose third key is <paramref name="key3"/>, across all (k1, k2)
        /// pairs.
        /// </summary>
        /// <remarks>
        /// The third axis is not a prefix, so this scans every entry: O(n).
        /// </remarks>
        public IEnumerable<(K1 Key1, K2 Key2, V Value)> GetByThirdKey(K3 key3)
        {
            foreach (var entry in _trie)
            {
                if (Comparer3.Equals(Axis3(entry.Key), key3))
                {
                    yield return (Axis1(entry.Key), Axis2(entry.Key), entry.Value);
                }
            }
        }

        /// <summary>
        /// Gets the number of entries whose first key is <paramref name="key1"/>. A prefix walk.
        /// </summary>
        public int CountOfFirstKey(K1 key1)
        {
            return _trie.CountOfPrefix(ToFirstAxisKey(key1));
        }

        /// <summary>
        /// Gets the number of entries whose second key is <paramref name="key2"/>. O(n), the
        /// second axis is not a prefix.
        /// </summary>
        public int CountOfSecondKey(K2 key2)
        {
            var count = 0;
            foreach (var entry in _trie)
            {
                if (Comparer2.Equals(Axis2(entry.Key), key2))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Gets the number of entries whose third key is <paramref name="key3"/>. O(n), the third
        /// axis is not a prefix.
        /// </summary>
        public int CountOfThirdKey(K3 key3)
        {
            var count = 0;
            foreach (var entry in _trie)
            {
                if (Comparer3.Equals(Axis3(entry.Key), key3))
                {
                    count++;
                }
            }

            return count;
        }

        // ------------------------------------------------------------------
        // Remove
        // ------------------------------------------------------------------

        /// <summary>
        /// Removes the entry stored under the (k1, k2, k3) triple. Returns <c>true</c> when an
        /// entry was removed.
        /// </summary>
        public bool Remove(K1 key1, K2 key2, K3 key3)
        {
            return _trie.Remove(ToKey(key1, key2, key3));
        }

        /// <summary>
        /// Removes the entry stored under the (k1, k2, k3) triple and returns the removed value.
        /// </summary>
        public bool Remove(K1 key1, K2 key2, K3 key3, out V value)
        {
            return _trie.Remove(ToKey(key1, key2, key3), out value);
        }

        /// <summary>
        /// Removes every entry whose first key is <paramref name="key1"/> (cascade delete of the
        /// first-axis slice). Returns the number of removed entries. A prefix walk.
        /// </summary>
        public int RemoveByFirstKey(K1 key1)
        {
            return _trie.RemovePrefix(ToFirstAxisKey(key1));
        }

        /// <summary>
        /// Removes every entry whose second key is <paramref name="key2"/>, across all first keys.
        /// Returns the number of removed entries. O(n): the second axis is not a prefix, so the
        /// slice is materialized and removed one triple at a time.
        /// </summary>
        public int RemoveBySecondKey(K2 key2)
        {
            var triples = new List<(K1 Key1, K2 Key2, K3 Key3)>();
            foreach (var entry in _trie)
            {
                if (Comparer2.Equals(Axis2(entry.Key), key2))
                {
                    triples.Add((Axis1(entry.Key), Axis2(entry.Key), Axis3(entry.Key)));
                }
            }

            var removed = 0;
            foreach (var triple in triples)
            {
                if (_trie.Remove(ToKey(triple.Key1, triple.Key2, triple.Key3)))
                {
                    removed++;
                }
            }

            return removed;
        }

        /// <summary>
        /// Removes every entry whose third key is <paramref name="key3"/>, across all (k1, k2)
        /// pairs. Returns the number of removed entries. O(n): the third axis is not a prefix.
        /// </summary>
        public int RemoveByThirdKey(K3 key3)
        {
            var triples = new List<(K1 Key1, K2 Key2, K3 Key3)>();
            foreach (var entry in _trie)
            {
                if (Comparer3.Equals(Axis3(entry.Key), key3))
                {
                    triples.Add((Axis1(entry.Key), Axis2(entry.Key), Axis3(entry.Key)));
                }
            }

            var removed = 0;
            foreach (var triple in triples)
            {
                if (_trie.Remove(ToKey(triple.Key1, triple.Key2, triple.Key3)))
                {
                    removed++;
                }
            }

            return removed;
        }

        /// <summary>
        /// Removes every entry.
        /// </summary>
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
        public ThreeKeyDictionary<K1, K2, K3, V> Clone()
        {
            var clone = new ThreeKeyDictionary<K1, K2, K3, V>(Comparer1, Comparer2, Comparer3);
            foreach (var entry in Entries)
            {
                clone.Add(entry.Key1, entry.Key2, entry.Key3, entry.Value);
            }

            return clone;
        }

        /// <summary>
        /// Returns a live read-only view of the dictionary: enumeration reflects subsequent
        /// changes to the owning dictionary. Mutating members are not exposed.
        /// </summary>
        public IReadOnlyCollection<(K1 Key1, K2 Key2, K3 Key3, V Value)> AsReadOnly()
        {
            return new ReadOnlyView(this);
        }

        /// <summary>
        /// Exports the dictionary as a snapshot keyed by the (k1, k2, k3) tuple. The returned
        /// dictionary is independent of the source.
        /// </summary>
        public IReadOnlyDictionary<(K1 Key1, K2 Key2, K3 Key3), V> ToDictionary()
        {
            var dictionary = new Dictionary<(K1, K2, K3), V>();
            foreach (var entry in Entries)
            {
                dictionary.Add((entry.Key1, entry.Key2, entry.Key3), entry.Value);
            }

            return dictionary;
        }

        /// <summary>
        /// Returns the trie that backs this dictionary, rebuilt with plain boxed key components.
        /// </summary>
        /// <remarks>
        /// The internal trie keys its components with position tags (see <see cref="Axis"/>),
        /// which are an implementation detail; this method projects them away so callers can use
        /// the full <see cref="MultiKeyDictionary{TKey,TValue}"/> surface - including
        /// arbitrary-depth prefix walks past the three key components - without seeing the tags.
        /// It is a <b>copy</b>, not a view: mutations do not propagate in either direction. Note
        /// that plain components lose the axis distinction, so this is only useful when the three
        /// axis types do not overlap.
        /// </remarks>
        public MultiKeyDictionary<object, V> AsTrie()
        {
            var trie = new MultiKeyDictionary<object, V>();
            foreach (var entry in _trie)
            {
                trie.Add(
                    new object[] { ((Axis)entry.Key[0]).Value, ((Axis)entry.Key[1]).Value, ((Axis)entry.Key[2]).Value },
                    entry.Value);
            }

            return trie;
        }

        // ------------------------------------------------------------------
        // Enumeration
        // ------------------------------------------------------------------

        /// <summary>
        /// Enumerates every (k1, k2, k3, value) entry.
        /// </summary>
        public IEnumerator<(K1 Key1, K2 Key2, K3 Key3, V Value)> GetEnumerator()
        {
            return Entries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns the contents as <c>(k1,k2,k3):value</c> pairs, comma separated.
        /// </summary>
        public override string ToString()
        {
            var parts = new List<string>();
            foreach (var entry in Entries)
            {
                parts.Add($"({entry.Key1},{entry.Key2},{entry.Key3}):{entry.Value}");
            }

            return string.Join(",", parts);
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        /// <summary>Shared empty key, used for whole-dictionary prefix walks (net451 has no <c>Array.Empty</c>).</summary>
        private static readonly object[] EmptyKey = new object[0];

        private static object[] ToKey(K1 key1, K2 key2, K3 key3)
        {
            return new object[] { new Axis(key1), new Axis(key2), new Axis(key3) };
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

        private static K3 Axis3(object[] key)
        {
            return (K3)((Axis)key[2]).Value!;
        }

        /// <summary>
        /// Comparer handed to the underlying trie. Components arrive as <see cref="Axis"/> tags,
        /// so the injected comparer for each position is selected from the tag rather than from
        /// the runtime type - the latter is ambiguous whenever the three axes share a type. Two
        /// components compare equal only when they sit at the same position and the corresponding
        /// comparer agrees.
        /// </summary>
        private sealed class AxisComparer : IEqualityComparer<object>
        {
            private readonly IEqualityComparer<K1> _comparer1;
            private readonly IEqualityComparer<K2> _comparer2;
            private readonly IEqualityComparer<K3> _comparer3;

            public AxisComparer(
                IEqualityComparer<K1> comparer1, IEqualityComparer<K2> comparer2, IEqualityComparer<K3> comparer3)
            {
                _comparer1 = comparer1;
                _comparer2 = comparer2;
                _comparer3 = comparer3;
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

                if (a.Position == 1)
                {
                    return _comparer2.Equals((K2)a.Value, (K2)b.Value);
                }

                return _comparer3.Equals((K3)a.Value, (K3)b.Value);
            }

            public int GetHashCode(object obj)
            {
                if (!(obj is Axis axis) || axis.Value == null)
                {
                    return 0;
                }

                // Mix in the position so the three axes can never collide on hash alone; the
                // trie keys positions separately anyway, but a stable per-position hash makes
                // the invariant explicit.
                var componentHash = axis.Position == 0
                    ? _comparer1.GetHashCode((K1)axis.Value)
                    : axis.Position == 1
                        ? _comparer2.GetHashCode((K2)axis.Value)
                        : _comparer3.GetHashCode((K3)axis.Value);
                return (componentHash * 397) ^ axis.Position;
            }
        }

        private sealed class ReadOnlyView : IReadOnlyCollection<(K1 Key1, K2 Key2, K3 Key3, V Value)>
        {
            private readonly ThreeKeyDictionary<K1, K2, K3, V> _owner;

            public ReadOnlyView(ThreeKeyDictionary<K1, K2, K3, V> owner)
            {
                _owner = owner;
            }

            public int Count => _owner.Count;

            public IEnumerator<(K1 Key1, K2 Key2, K3 Key3, V Value)> GetEnumerator()
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
