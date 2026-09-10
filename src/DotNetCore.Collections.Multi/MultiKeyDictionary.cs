using System;
using System.Collections;
using System.Collections.Generic;

// CS8714: TKey is deliberately unconstrained (null key *components* are supported
// through a per-node sentinel bucket, but the type parameter itself must stay
// nullable-friendly, e.g. TKey = string?); the "notnull" key constraint of the
// annotated Dictionary<TKey, TValue> (net5.0+ reference assemblies) is a false
// positive here.
#pragma warning disable CS8714

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Represents a composite-key dictionary backed by a trie (prefix tree): a dictionary whose
    /// keys are <em>sequences</em> of components of a single type (<c>TKey[]</c>) mapping to one
    /// value each. Keys of any arity are supported by the same type, and entries sharing a
    /// common prefix share storage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlike <see cref="Dictionary{TKey,TValue}"/> with a tuple key — which can only ever be
    /// addressed by the <em>complete</em> key — this type supports <b>prefix projection</b>:
    /// <see cref="GetByPrefix(TKey[], bool)"/> enumerates everything stored under a partial key, and
    /// <see cref="RemovePrefix(TKey[])"/> deletes a whole subtree at once. For the two-component
    /// case <c>GetByPrefix(new[] { k1 })</c> is exactly "everything for k1", which is the core
    /// reason a dedicated type exists at all.
    /// </para>
    /// <para>
    /// Key equality is determined per component by the <see cref="IEqualityComparer{TKey}"/>
    /// supplied at construction (default: <see cref="EqualityComparer{TKey}.Default"/>).
    /// Components are never keyed by their hash code alone, so hash collisions between
    /// distinct components can not corrupt the trie. <c>null</c> components are supported and
    /// tracked in a dedicated per-node bucket, mirroring the <see cref="MultiList{T}"/>
    /// strategy (<see cref="Dictionary{TKey,TValue}"/> rejects null keys).
    /// </para>
    /// <para>
    /// This class is not thread-safe. Wrap it with external synchronization for concurrent use.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var map = new MultiKeyDictionary&lt;string, int&gt;();
    /// map.Add(new[] { "eu", "de", "berlin" }, 1);
    /// map.Add(new[] { "eu", "de", "munich" }, 2);
    /// map.Add(new[] { "eu", "fr", "paris" }, 3);
    ///
    /// map.Count;                          // 3  (whole entries)
    /// map[new[] { "eu", "de", "berlin" }]; // 1
    /// map.CountOfPrefix(new[] { "eu", "de" }); // 2
    /// </code>
    /// </example>
    public class MultiKeyDictionary<TKey, TValue> :
        IEnumerable<(TKey[] Key, TValue Value)>
    {
        private static readonly TKey[] EmptyKey = new TKey[0];

        private readonly IEqualityComparer<TKey> _comparer;
        private readonly Node _root;

        /// <summary>
        /// Initializes an empty <see cref="MultiKeyDictionary{TKey,TValue}"/> using the default
        /// component comparer.
        /// </summary>
        public MultiKeyDictionary() : this((IEqualityComparer<TKey>?)null)
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="MultiKeyDictionary{TKey,TValue}"/> with the specified
        /// component comparer.
        /// </summary>
        public MultiKeyDictionary(IEqualityComparer<TKey>? comparer)
        {
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
            _root = new Node(_comparer);
        }

        /// <summary>
        /// Gets the comparer used to determine equality of individual key components.
        /// </summary>
        public IEqualityComparer<TKey> Comparer => _comparer;

        /// <summary>
        /// Gets the number of stored (whole key, value) entries.
        /// </summary>
        public int Count => _root.SubtreeValueCount;

        /// <summary>
        /// Gets a value indicating whether the map is empty.
        /// </summary>
        public bool IsEmpty => Count == 0;

        /// <summary>
        /// Gets the number of distinct nodes in the trie (every prefix actually materialized,
        /// including the root). Useful for observing prefix sharing.
        /// </summary>
        public int NodeCount => _root.SubtreeNodeCount;

        /// <summary>
        /// Gets every stored key, materialized as a fresh array.
        /// </summary>
        public IEnumerable<TKey[]> Keys
        {
            get
            {
                foreach (var entry in this)
                {
                    yield return entry.Key;
                }
            }
        }

        /// <summary>
        /// Gets every stored value, in depth-first traversal order.
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get
            {
                foreach (var entry in this)
                {
                    yield return entry.Value;
                }
            }
        }

        // ------------------------------------------------------------------
        // Add
        // ------------------------------------------------------------------

        /// <summary>
        /// Adds a value under the specified key sequence. An existing value under the same key
        /// is overwritten.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// map.Add(new[] { "eu", "de" }, 1);
        /// </code>
        /// </example>
        public void Add(TKey[] key, TValue value)
        {
            Add(key, value, true);
        }

        /// <summary>
        /// Adds a value under the specified key sequence. When
        /// <paramref name="overwrite"/> is <c>false</c>, an existing entry for the same key
        /// raises <see cref="ArgumentException"/> instead of being replaced. When
        /// <paramref name="overwrite"/> is <c>true</c>, the return value reports
        /// whether a previous value was replaced (meaningless when this overload throws).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The key is already present and <paramref name="overwrite"/> is <c>false</c>.</exception>
        /// <example>
        /// <code>
        /// bool replaced = map.Add(new[] { "eu", "de" }, 1, overwrite: false);
        /// </code>
        /// </example>
        public bool Add(TKey[] key, TValue value, bool overwrite)
        {
            var node = Resolve(key, nameof(key), create: true)!;
            if (node.HasValue && !overwrite)
            {
                throw new ArgumentException(
                    "An element with the same key already exists.", nameof(key));
            }

            var hadValue = node.HasValue;
            node.Value = value;
            node.HasValue = true;
            return hadValue;
        }

        /// <summary>
        /// Adds the (key, value) entry when the key is absent. Returns <c>true</c> when the
        /// entry was added, <c>false</c> when the key was already present (and left untouched).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bool added = map.TryAdd(new[] { "eu", "de" }, 1);
        /// </code>
        /// </example>
        public bool TryAdd(TKey[] key, TValue value)
        {
            var node = Resolve(key, nameof(key), create: true)!;
            if (node.HasValue)
            {
                return false;
            }

            node.Value = value;
            node.HasValue = true;
            return true;
        }

        /// <summary>
        /// Adds (key, value) through the indexer with sequence syntax,
        /// e.g. <c>map[new[] { "eu", "de" }] = 1</c>. Getting a missing key raises
        /// <see cref="KeyNotFoundException"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <exception cref="KeyNotFoundException">The key is missing on a read.</exception>
        /// <example>
        /// <code>
        /// map[new[] { "eu", "de" }] = 1;
        /// int v = map[new[] { "eu", "de" }];
        /// </code>
        /// </example>
        public TValue this[TKey[] key]
        {
            get
            {
                if (!TryGetValue(key, out var value))
                {
                    throw new KeyNotFoundException("The given key was not present in the dictionary.");
                }

                return value;
            }

            set => Add(key, value);
        }

        // ------------------------------------------------------------------
        // Lookup
        // ------------------------------------------------------------------

        /// <summary>
        /// Gets the value stored under the exact key sequence.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// if (map.TryGetValue(new[] { "eu", "de" }, out var v)) { }
        /// </code>
        /// </example>
        public bool TryGetValue(TKey[] key, out TValue value)
        {
            var node = Resolve(key, nameof(key), create: false);
            if (node != null && node.HasValue)
            {
                value = node.Value!;
                return true;
            }

            value = default!;
            return false;
        }

        /// <summary>
        /// Determines whether an entry exists for the exact key sequence.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bool has = map.ContainsKey(new[] { "eu", "de" });
        /// </code>
        /// </example>
        public bool ContainsKey(TKey[] key)
        {
            var node = Resolve(key, nameof(key), create: false);
            return node != null && node.HasValue;
        }

        /// <summary>
        /// Determines whether an entry exists for the exact key sequence and holds the specified
        /// value (compared with <see cref="EqualityComparer{TValue}.Default"/>).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bool has = map.Contains(new[] { "eu", "de" }, 1);
        /// </code>
        /// </example>
        public bool Contains(TKey[] key, TValue value)
        {
            return TryGetValue(key, out var existing)
                && EqualityComparer<TValue>.Default.Equals(existing, value);
        }

        /// <summary>
        /// Determines whether the specified prefix is present in the trie at all (whether or not
        /// a value is stored exactly there).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bool has = map.ContainsPrefix(new[] { "eu" });
        /// </code>
        /// </example>
        public bool ContainsPrefix(TKey[] prefix)
        {
            return Resolve(prefix, nameof(prefix), create: false) != null;
        }

        // ------------------------------------------------------------------
        // Prefix projection
        // ------------------------------------------------------------------

        /// <summary>
        /// Gets the value stored exactly at the prefix, i.e. <c>GetByPrefix(key)</c> is the
        /// prefix-flavoured counterpart of <see cref="TryGetValue(TKey[],out TValue)"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// if (map.TryGetValueByPrefix(new[] { "eu", "de" }, out var v)) { }
        /// </code>
        /// </example>
        public bool TryGetValueByPrefix(TKey[] prefix, out TValue value)
        {
            return TryGetValue(prefix, out value);
        }

        /// <summary>
        /// Enumerates every entry stored under the specified prefix, including an entry stored
        /// exactly at the prefix itself (empty suffix). The keys yielded are <b>full</b> keys,
        /// optionally rebased onto <paramref name="relative"/> = <c>true</c> (suffix only).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// foreach (var (key, value) in map.GetByPrefix(new[] { "eu" }, relative: false))
        /// {
        ///     // key is the full key, e.g. ["eu","de","berlin"]
        /// }
        /// </code>
        /// </example>
        public IEnumerable<(TKey[] Key, TValue Value)> GetByPrefix(TKey[] prefix, bool relative = false)
        {
            var node = Resolve(prefix, nameof(prefix), create: false);
            if (node == null)
            {
                yield break;
            }

            var from = relative ? prefix.Length : -1;
            foreach (var entry in Enumerate(node, from, prefix))
            {
                yield return entry;
            }
        }

        /// <summary>
        /// Enumerates the suffix sequences (relative keys) of every entry under the specified
        /// prefix, including the empty suffix of an entry stored at the prefix itself.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// foreach (var suffix in map.GetSuffixes(new[] { "eu" }))
        /// {
        ///     // e.g. ["de","berlin"], ["de","munich"], ["fr","paris"]
        /// }
        /// </code>
        /// </example>
        public IEnumerable<TKey[]> GetSuffixes(TKey[] prefix)
        {
            var node = Resolve(prefix, nameof(prefix), create: false);
            if (node == null)
            {
                yield break;
            }

            foreach (var entry in Enumerate(node, 0, EmptyKey))
            {
                yield return entry.Key;
            }
        }

        /// <summary>
        /// Gets the number of entries stored under the specified prefix (including an entry
        /// stored exactly at the prefix).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// int n = map.CountOfPrefix(new[] { "eu", "de" });
        /// </code>
        /// </example>
        public int CountOfPrefix(TKey[] prefix)
        {
            var node = Resolve(prefix, nameof(prefix), create: false);
            return node?.SubtreeValueCount ?? 0;
        }

        /// <summary>
        /// Gets the distinct components that directly follow the specified prefix
        /// (the trie's branching factor at that node).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// foreach (var branch in map.GetBranches(new[] { "eu" })) { }
        /// </code>
        /// </example>
        public IEnumerable<TKey> GetBranches(TKey[] prefix)
        {
            var node = Resolve(prefix, nameof(prefix), create: false);
            if (node == null)
            {
                yield break;
            }

            foreach (var component in node.NextComponents())
            {
                yield return component;
            }
        }

        /// <summary>
        /// Exports every entry under the specified prefix as a snapshot dictionary keyed by the
        /// <b>full</b> key. The returned dictionary is independent of the map.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// IReadOnlyDictionary&lt;string[], int&gt; snapshot = map.ToDictionary(new[] { "eu" });
        /// </code>
        /// </example>
        public IReadOnlyDictionary<TKey[], TValue> ToDictionary(TKey[] prefix)
        {
            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            var dictionary = new Dictionary<TKey[], TValue>(ArrayEqualityComparer.Instance);
            foreach (var entry in GetByPrefix(prefix))
            {
                dictionary.Add(entry.Key, entry.Value);
            }

            return dictionary;
        }

        /// <summary>
        /// Exports the whole map as a snapshot dictionary keyed by the full key. The returned
        /// dictionary is independent of the map.
        /// </summary>
        /// <example>
        /// <code>
        /// IReadOnlyDictionary&lt;string[], int&gt; snapshot = map.ToDictionary();
        /// </code>
        /// </example>
        public IReadOnlyDictionary<TKey[], TValue> ToDictionary()
        {
            return ToDictionary(EmptyKey);
        }

        // ------------------------------------------------------------------
        // Remove
        // ------------------------------------------------------------------

        /// <summary>
        /// Removes the entry stored under the exact key sequence. Returns <c>true</c> when an
        /// entry was removed. The surrounding trie nodes are pruned once they become empty, so a
        /// trie never holds structure for keys that are no longer present.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bool removed = map.Remove(new[] { "eu", "de" });
        /// </code>
        /// </example>
        public bool Remove(TKey[] key)
        {
            return Remove(key, out _);
        }

        /// <summary>
        /// Removes the entry stored under the exact key sequence and returns the removed value.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// if (map.Remove(new[] { "eu", "de" }, out var value)) { }
        /// </code>
        /// </example>
        public bool Remove(TKey[] key, out TValue value)
        {
            var node = Resolve(key, nameof(key), create: false);
            if (node == null || !node.HasValue)
            {
                value = default!;
                return false;
            }

            value = node.Value!;
            node.Value = default!;
            node.HasValue = false;
            Prune(key);
            return true;
        }

        /// <summary>
        /// Removes every entry stored under the specified prefix, including an entry stored
        /// exactly at the prefix (cascade delete). Returns the number of removed entries.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// int removed = map.RemovePrefix(new[] { "eu", "de" });
        /// </code>
        /// </example>
        public int RemovePrefix(TKey[] prefix)
        {
            var node = Resolve(prefix, nameof(prefix), create: false);
            if (node == null)
            {
                return 0;
            }

            var removed = node.SubtreeValueCount;
            node.Children.Clear();
            node.NullChild = null;
            node.Value = default!;
            node.HasValue = false;
            Prune(prefix);
            return removed;
        }

        /// <summary>
        /// Removes every entry and resets the trie to a single empty root.
        /// </summary>
        /// <example>
        /// <code>
        /// map.Clear();
        /// </code>
        /// </example>
        public void Clear()
        {
            _root.Children.Clear();
            _root.NullChild = null;
            _root.Value = default!;
            _root.HasValue = false;
        }

        // ------------------------------------------------------------------
        // Copying
        // ------------------------------------------------------------------

        /// <summary>
        /// Creates a deep copy of the trie: key components and values are shared, the structure
        /// is independent.
        /// </summary>
        /// <example>
        /// <code>
        /// MultiKeyDictionary&lt;string, int&gt; copy = map.Clone();
        /// </code>
        /// </example>
        public MultiKeyDictionary<TKey, TValue> Clone()
        {
            var clone = new MultiKeyDictionary<TKey, TValue>(_comparer);
            foreach (var entry in this)
            {
                clone.Add(entry.Key, entry.Value);
            }

            return clone;
        }

        /// <summary>
        /// Returns a live read-only view of the map: enumeration and lookups reflect subsequent
        /// changes to the owning map. Mutating members are not exposed.
        /// </summary>
        /// <example>
        /// <code>
        /// IReadOnlyCollection&lt;(string[], int)&gt; view = map.AsReadOnly();
        /// </code>
        /// </example>
        public IReadOnlyCollection<(TKey[] Key, TValue Value)> AsReadOnly()
        {
            return new ReadOnlyView(this);
        }

        // ------------------------------------------------------------------
        // Enumeration
        // ------------------------------------------------------------------

        /// <summary>
        /// Enumerates every (key, value) entry in depth-first order. Each yielded key is a fresh
        /// array of the full key sequence; mutating it does not affect the map.
        /// </summary>
        /// <example>
        /// <code>
        /// foreach (var (key, value) in map) { }
        /// </code>
        /// </example>
        public IEnumerator<(TKey[] Key, TValue Value)> GetEnumerator()
        {
            return Enumerate(_root, -1, EmptyKey).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns the contents as <c>[k1,k2]:value</c> pairs, comma separated,
        /// e.g. <c>[eu,de]:1,[eu,fr]:2</c>.
        /// </summary>
        /// <example>
        /// <code>
        /// string text = map.ToString();
        /// </code>
        /// </example>
        public override string ToString()
        {
            var parts = new List<string>();
            foreach (var entry in this)
            {
                parts.Add($"[{string.Join(",", entry.Key)}]:{entry.Value}");
            }

            return string.Join(",", parts);
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        /// <summary>
        /// Walks the trie along <paramref name="key"/>. When <paramref name="create"/> is
        /// <c>true</c>, missing nodes are materialized; otherwise a missing node short-circuits
        /// to <c>null</c>, so callers never have to special-case absence.
        /// </summary>
        private Node? Resolve(TKey[] key, string paramName, bool create)
        {
            if (key == null)
            {
                throw new ArgumentNullException(paramName);
            }

            var node = _root;
            for (var i = 0; i < key.Length; i++)
            {
                var component = key[i];
                if (component == null)
                {
                    if (node.NullChild == null)
                    {
                        if (!create)
                        {
                            return null;
                        }

                        node.NullChild = new Node(_comparer);
                    }

                    node = node.NullChild;
                    continue;
                }

                if (!node.Children.TryGetValue(component, out var child))
                {
                    if (!create)
                    {
                        return null;
                    }

                    child = new Node(_comparer);
                    node.Children.Add(component, child);
                }

                node = child;
            }

            return node;
        }

        /// <summary>
        /// Drops the nodes along <paramref name="key"/> that became childless and value-less
        /// after a removal, so the trie holds no structure for keys that are no longer present.
        /// The root is never dropped.
        /// </summary>
        private void Prune(TKey[] key)
        {
            var stack = new Node[key.Length + 1];
            var node = _root;
            stack[0] = node;
            for (var i = 0; i < key.Length; i++)
            {
                node = Descend(node, key[i]);
                stack[i + 1] = node;
            }

            for (var i = key.Length; i > 0; i--)
            {
                // Stop at the first node along the path that is still needed: it either holds
                // a value or still has children. Nodes above it are ancestors of a live node,
                // so they must stay too. The walk is strictly bottom-up along a single chain,
                // which is why stopping (rather than skipping) is sufficient and why every
                // node below the stop point is guaranteed to be empty.
                if (stack[i].HasValue || stack[i].Children.Count > 0 || stack[i].NullChild != null)
                {
                    break;
                }

                var parent = stack[i - 1];
                if (key[i - 1] == null)
                {
                    parent.NullChild = null;
                }
                else
                {
                    parent.Children.Remove(key[i - 1]);
                }
            }
        }

        /// <summary>
        /// Follows a single edge. The <c>null</c> component has no dictionary entry of its own
        /// (see <see cref="Node.NullChild"/>), so it is dispatched on its own field. Callers
        /// must have established that the edge exists.
        /// </summary>
        private static Node Descend(Node node, TKey component)
        {
            return component == null ? node.NullChild! : node.Children[component];
        }

        /// <summary>
        /// Depth-first enumeration of the subtree rooted at <paramref name="node"/>. The
        /// assembled key is <paramref name="prefix"/> followed by the walked path, truncated at
        /// <paramref name="from"/> just before it is yielded. <paramref name="from"/> is
        /// <c>-1</c> when the full key should be yielded, or the prefix length in relative mode
        /// (where all but the suffix is dropped). The negative sentinel matters: relative
        /// enumeration of the root prefix also has a prefix length of <c>0</c>, so <c>0</c>
        /// can not double as "do not truncate".
        /// </summary>
        private static IEnumerable<(TKey[] Key, TValue Value)> Enumerate(
            Node node, int from, TKey[] prefix)
        {
            if (node.HasValue)
            {
                yield return (Truncate(prefix, from), node.Value!);
            }

            foreach (var child in node.Children)
            {
                var next = Append(prefix, child.Key);
                foreach (var entry in Enumerate(child.Value, from, next))
                {
                    yield return entry;
                }
            }

            if (node.NullChild != null)
            {
                var next = Append(prefix, default!);
                foreach (var entry in Enumerate(node.NullChild, from, next))
                {
                    yield return entry;
                }
            }
        }

        /// <summary>
        /// Yields the key unchanged when <paramref name="from"/> is negative, otherwise the
        /// suffix starting at <paramref name="from"/>.
        /// </summary>
        private static TKey[] Truncate(TKey[] key, int from)
        {
            if (from < 0)
            {
                return key;
            }

            var length = key.Length - from;
            if (length <= 0)
            {
                return EmptyKey;
            }

            var result = new TKey[length];
            Array.Copy(key, from, result, 0, length);
            return result;
        }

        private static TKey[] Append(TKey[] key, TKey component)
        {
            var result = new TKey[key.Length + 1];
            Array.Copy(key, result, key.Length);
            result[key.Length] = component;
            return result;
        }

        /// <summary>
        /// Equality comparer for <c>TKey[]</c> keys, used by <see cref="ToDictionary(TKey[])"/>.
        /// Sequence equality is element-wise via <see cref="EqualityComparer{TKey}.Default"/>;
        /// it deliberately does not take the trie's injected comparer, since the resulting
        /// dictionary is a plain snapshot container (mirroring
        /// <see cref="MultiList{T}.ToDictionary"/>).
        /// </summary>
        private sealed class ArrayEqualityComparer : IEqualityComparer<TKey[]?>
        {
            public static readonly ArrayEqualityComparer Instance = new ArrayEqualityComparer();

            public bool Equals(TKey[]? x, TKey[]? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x == null || y == null || x.Length != y.Length)
                {
                    return false;
                }

                var comparer = EqualityComparer<TKey>.Default;
                for (var i = 0; i < x.Length; i++)
                {
                    if (!comparer.Equals(x[i], y[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode(TKey[]? obj)
            {
                if (obj == null)
                {
                    return 0;
                }

                var hash = 17;
                var comparer = EqualityComparer<TKey>.Default;
                for (var i = 0; i < obj.Length; i++)
                {
                    var component = obj[i];
                    hash = hash * 31 + (component == null ? 0 : comparer.GetHashCode(component!));
                }

                return hash;
            }
        }

        /// <summary>
        /// A single trie node: its outgoing edges (plus the dedicated <c>null</c> bucket), the
        /// optional value stored exactly at it, and the cached sizes of its subtree.
        /// </summary>
        private sealed class Node
        {
            public Node(IEqualityComparer<TKey> comparer)
            {
                Children = new Dictionary<TKey, Node>(comparer);
            }

            /// <summary>Outgoing edges keyed by non-null component.</summary>
            public Dictionary<TKey, Node> Children { get; }

            /// <summary>
            /// Outgoing edge for the <c>null</c> key component, held in a dedicated field rather
            /// than in <see cref="Children"/> because <see cref="Dictionary{TKey,TValue}"/>
            /// rejects a <c>null</c> key. It is deliberately nullable: no allocation is paid
            /// until a <c>null</c> component is actually used at this node.
            /// </summary>
            public Node? NullChild { get; set; }

            /// <summary>Whether a value is stored exactly at this node.</summary>
            public bool HasValue { get; set; }

            /// <summary>The value stored exactly at this node (meaningful only when <see cref="HasValue"/>).</summary>
            /// <remarks>
            /// Deliberately <c>TValue</c> and not <c>TValue?</c>: a nullable annotation on an
            /// unconstrained type parameter requires C# 9 (CS8627), and this project compiles
            /// as C# 8. <c>default</c> is a fine sentinel because <see cref="HasValue"/>
            /// alone decides whether the slot is meaningful.
            /// </remarks>
            public TValue Value { get; set; } = default!;

            /// <summary>Number of stored values in this subtree (including this node).</summary>
            public int SubtreeValueCount
            {
                get
                {
                    var total = HasValue ? 1 : 0;
                    foreach (var child in Children.Values)
                    {
                        total += child.SubtreeValueCount;
                    }

                    if (NullChild != null)
                    {
                        total += NullChild.SubtreeValueCount;
                    }

                    return total;
                }
            }

            /// <summary>Number of nodes in this subtree (including this node).</summary>
            public int SubtreeNodeCount
            {
                get
                {
                    var total = 1;
                    foreach (var child in Children.Values)
                    {
                        total += child.SubtreeNodeCount;
                    }

                    if (NullChild != null)
                    {
                        total += NullChild.SubtreeNodeCount;
                    }

                    return total;
                }
            }

            /// <summary>Enumerates the distinct components reachable from this node (null branch last).</summary>
            public IEnumerable<TKey> NextComponents()
            {
                foreach (var component in Children.Keys)
                {
                    yield return component;
                }

                if (NullChild != null)
                {
                    yield return default!;
                }
            }
        }

        private sealed class ReadOnlyView : IReadOnlyCollection<(TKey[] Key, TValue Value)>
        {
            private readonly MultiKeyDictionary<TKey, TValue> _owner;

            public ReadOnlyView(MultiKeyDictionary<TKey, TValue> owner)
            {
                _owner = owner;
            }

            public int Count => _owner.Count;

            public IEnumerator<(TKey[] Key, TValue Value)> GetEnumerator()
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
