using System;
using System.Collections;
using System.Collections.Generic;

// CS8714: TValue is deliberately unconstrained (null values are supported and stored like any
// other value, but the type parameter itself must stay nullable-friendly, e.g. TValue = string?);
// the "notnull" key constraint of the annotated HashSet<T> (net5.0+ reference assemblies) is a
// false positive here — the HashSet<TValue> instances are local argument deduplication sets and
// tolerate null members.
#pragma warning disable CS8714

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Represents a composite-key multimap: a dictionary whose keys are <em>sequences</em> of
    /// components of a single type (<c>TKey[]</c>) and each complete key maps to a
    /// <em>collection</em> of values. It is the combination of
    /// <see cref="MultiKeyDictionary{TKey,TValue}"/> (N components &#8594; 1 value, a trie) and
    /// <see cref="MultiDictionary{TKey,TValue}"/> (1 key &#8594; N values, a multimap).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The storage is a trie over the key components; each complete key holds a value collection
    /// built by a configurable factory — by default a <see cref="List{TValue}"/> that keeps
    /// duplicates, or a <see cref="HashSet{TValue}"/> when <c>allowDuplicateValues</c> is
    /// <c>false</c>. Adding, looking up or removing a single (key, value) pair costs
    /// O(key length) for the trie walk plus O(1) for the collection operation. The per-key value
    /// set operations (<see cref="UnionWith(TKey[],IEnumerable{TValue})"/> and siblings) treat
    /// their argument as a <b>set</b>, exactly as <see cref="MultiDictionary{TKey,TValue}"/>
    /// does, and the map never holds value-less keys: a key disappears automatically once its
    /// last value is removed.
    /// </para>
    /// <para>
    /// Prefix projection carries over from the trie: <see cref="GetByPrefix(TKey[],bool)"/>
    /// enumerates every complete key stored under a partial key together with its values,
    /// <see cref="CountOfPrefix(TKey[])"/> counts those keys, <see cref="GetBranches(TKey[])"/>
    /// lists the next distinct components, and <see cref="RemovePrefix(TKey[])"/> deletes a whole
    /// subtree at once. This is what a <see cref="Dictionary{TKey,TValue}"/> keyed by a tuple can
    /// not do: a tuple key can only ever be addressed by the complete key.
    /// </para>
    /// <para>
    /// Taxonomy: the composite key of this type is a <b>trie key</b> — a homogeneous component
    /// sequence of any arity, addressable by prefix — not the fixed-arity, per-axis-typed scheme
    /// of <see cref="TwoKeyDictionary{K1,K2,V}"/> / <see cref="ThreeKeyDictionary{K1,K2,K3,V}"/>,
    /// which are thin facades that box two or three differently typed components into an
    /// <c>object</c> trie (the axis-labelling approach). Pick this type when the components share
    /// one type (or the arity varies) and prefix queries matter; pick the strongly typed facades
    /// for two or three differently typed components that are always addressed in full. Within
    /// the package's three orthogonal "multi" axes this type multiplies <em>two</em> of them at
    /// once: the key components (N components form one key) and the values of each complete key
    /// (1 complete key &#8594; N values).
    /// </para>
    /// <para>
    /// Key equality is determined per component by the <see cref="IEqualityComparer{TKey}"/>
    /// supplied at construction (default: <see cref="EqualityComparer{TKey}.Default"/>).
    /// Components are never keyed by their hash code alone, so hash collisions between distinct
    /// components can not corrupt the trie. <c>null</c> components are supported and tracked in a
    /// dedicated per-node bucket (mirroring <see cref="MultiKeyDictionary{TKey,TValue}"/>,
    /// because <see cref="Dictionary{TKey,TValue}"/> rejects null keys); a <c>null</c> key
    /// <em>array</em> is rejected with <see cref="ArgumentNullException"/>;
    /// <c>null</c> values are ordinary values.
    /// </para>
    /// <para>
    /// This class is not thread-safe. Wrap it with external synchronization for concurrent use.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var map = new MultiKeyMultiDictionary&lt;string, int&gt;();
    /// map.Add(new[] { "eu", "de", "berlin" }, 1);
    /// map.Add(new[] { "eu", "de", "berlin" }, 2);   // a second value under the same key
    /// map.Add(new[] { "eu", "de", "munich" }, 3);
    ///
    /// map.Count;                                     // 2  (complete keys)
    /// map.TotalValueCount;                           // 3  (values across all keys)
    /// map[new[] { "eu", "de", "berlin" }];           // [1, 2]
    /// map.CountOfPrefix(new[] { "eu", "de" });       // 2  (complete keys under the prefix)
    /// </code>
    /// </example>
    public class MultiKeyMultiDictionary<TKey, TValue> :
        IEnumerable<(TKey[] Key, TValue Value)>
    {
        private static readonly IReadOnlyCollection<TValue> EmptyValues = new TValue[0];

        private readonly MultiKeyDictionary<TKey, ICollection<TValue>> _trie;
        private readonly Func<ICollection<TValue>> _innerFactory;

        /// <summary>
        /// Cached total number of values across all keys, kept in step by every mutation instead
        /// of being recomputed on each read (L-06, mirroring
        /// <see cref="MultiDictionary{TKey,TValue}.TotalValueCount"/>).
        /// </summary>
        private int _totalValueCount;

        /// <summary>
        /// Initializes an empty <see cref="MultiKeyMultiDictionary{TKey,TValue}"/> using the
        /// default component comparer and allowing duplicate values per key (inner
        /// <see cref="List{TValue}"/>).
        /// </summary>
        public MultiKeyMultiDictionary()
            : this((IEqualityComparer<TKey>?)null, (Func<ICollection<TValue>>?)null)
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="MultiKeyMultiDictionary{TKey,TValue}"/> with the
        /// specified component comparer, allowing duplicate values per key.
        /// </summary>
        public MultiKeyMultiDictionary(IEqualityComparer<TKey>? comparer)
            : this(comparer, (Func<ICollection<TValue>>?)null)
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="MultiKeyMultiDictionary{TKey,TValue}"/> using the
        /// default component comparer. When <paramref name="allowDuplicateValues"/> is
        /// <c>false</c>, duplicate values under the same key are silently ignored (inner
        /// <see cref="HashSet{TValue}"/>).
        /// </summary>
        public MultiKeyMultiDictionary(bool allowDuplicateValues)
            : this((IEqualityComparer<TKey>?)null, allowDuplicateValues)
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="MultiKeyMultiDictionary{TKey,TValue}"/> with the
        /// specified component comparer. When <paramref name="allowDuplicateValues"/> is
        /// <c>false</c>, duplicate values under the same key are silently ignored (inner
        /// <see cref="HashSet{TValue}"/>).
        /// </summary>
        public MultiKeyMultiDictionary(IEqualityComparer<TKey>? comparer, bool allowDuplicateValues)
            : this(comparer, allowDuplicateValues
                ? (Func<ICollection<TValue>>)(() => new List<TValue>())
                : (Func<ICollection<TValue>>)(() => new HashSet<TValue>()))
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="MultiKeyMultiDictionary{TKey,TValue}"/> with the
        /// specified component comparer and inner collection factory.
        /// </summary>
        public MultiKeyMultiDictionary(IEqualityComparer<TKey>? comparer, Func<ICollection<TValue>>? innerFactory)
        {
            _trie = new MultiKeyDictionary<TKey, ICollection<TValue>>(comparer);
            _innerFactory = innerFactory ?? (() => new List<TValue>());
        }

        /// <summary>
        /// Gets the comparer used to determine equality of individual key components.
        /// </summary>
        public IEqualityComparer<TKey> Comparer => _trie.Comparer;

        /// <summary>
        /// Gets the number of complete keys in the map. A key exists only while it stores at
        /// least one value, so this is also the number of stored value collections.
        /// </summary>
        public int Count => _trie.Count;

        /// <summary>
        /// Gets the number of complete keys in the map (alias of <see cref="Count"/>).
        /// </summary>
        public int KeyCount => _trie.Count;

        /// <summary>
        /// Gets the total number of values across all keys.
        /// </summary>
        /// <remarks>
        /// Runs in O(1): the total is a cached count that every mutation maintains, rather than a
        /// per-read walk over the value collections.
        /// </remarks>
        public int TotalValueCount => _totalValueCount;

        /// <summary>
        /// Gets a value indicating whether the map is empty.
        /// </summary>
        public bool IsEmpty => _trie.IsEmpty;

        /// <summary>
        /// Gets the number of distinct trie nodes (every prefix actually materialized, including
        /// the root). Useful for observing prefix sharing; a key is pruned together with its
        /// values, so no node is held for absent keys.
        /// </summary>
        public int NodeCount => _trie.NodeCount;

        /// <summary>
        /// Gets every stored complete key, materialized as a fresh array. Keys never enumerate
        /// without values, because a key is dropped once its last value is removed.
        /// </summary>
        public IEnumerable<TKey[]> Keys
        {
            get
            {
                foreach (var pair in _trie)
                {
                    yield return pair.Key;
                }
            }
        }

        /// <summary>
        /// Gets all values of the map, flattened across keys, in depth-first key order.
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get
            {
                foreach (var pair in _trie)
                {
                    foreach (var value in pair.Value)
                    {
                        yield return value;
                    }
                }
            }
        }

        // ------------------------------------------------------------------
        // Add
        // ------------------------------------------------------------------

        /// <summary>
        /// Adds a value under the specified key sequence, creating the key when absent. When
        /// duplicate values are disallowed and the value already exists under the key, the call
        /// is silently ignored.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// map.Add(new[] { "eu", "de", "berlin" }, 1);
        /// </code>
        /// </example>
        public void Add(TKey[] key, TValue value)
        {
            var isNewKey = !_trie.TryGetValue(key, out var collection);
            if (isNewKey)
            {
                collection = _innerFactory();
                _trie.Add(key, collection!);
            }

            var before = collection!.Count;
            collection.Add(value);
            var after = collection.Count;

            if (isNewKey && after == 0)
            {
                // The (deduplicating) factory rejected the value into a fresh, still-empty
                // collection: drop the key to preserve the "no value-less key" invariant.
                // Nothing entered the total, so it is untouched.
                _trie.Remove(key);
                return;
            }

            _totalValueCount += after - before;
        }

        /// <summary>
        /// Adds each value of the specified collection under the key, creating the key when
        /// absent.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is <c>null</c>.</exception>
        public void AddRange(TKey[] key, IEnumerable<TValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            foreach (var value in values)
            {
                Add(key, value);
            }
        }

        // ------------------------------------------------------------------
        // Lookup
        // ------------------------------------------------------------------

        /// <summary>
        /// Gets the values stored under the complete key. Returns an empty collection (never
        /// <c>null</c>) when the key is absent. The returned collection is a live view of the
        /// values stored for the key.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// IReadOnlyCollection&lt;int&gt; values = map[new[] { "eu", "de", "berlin" }];
        /// </code>
        /// </example>
        public IReadOnlyCollection<TValue> this[TKey[] key] =>
            _trie.TryGetValue(key, out var collection) ? AsView(collection!) : EmptyValues;

        /// <summary>
        /// Gets the values stored under the complete key. When the key is absent, returns
        /// <c>false</c> and <paramref name="value"/> is <c>null</c> (check the return value
        /// before use).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// if (map.TryGetValue(new[] { "eu", "de", "berlin" }, out var values)) { }
        /// </code>
        /// </example>
        public bool TryGetValue(TKey[] key, out IReadOnlyCollection<TValue> value)
        {
            if (_trie.TryGetValue(key, out var collection))
            {
                value = AsView(collection!);
                return true;
            }

            value = null!;
            return false;
        }

        /// <summary>
        /// Determines whether the map contains the complete key (with at least one value).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bool has = map.ContainsKey(new[] { "eu", "de", "berlin" });
        /// </code>
        /// </example>
        public bool ContainsKey(TKey[] key)
        {
            return _trie.ContainsKey(key);
        }

        /// <summary>
        /// Determines whether the specified value exists under the complete key.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bool has = map.Contains(new[] { "eu", "de", "berlin" }, 1);
        /// </code>
        /// </example>
        public bool Contains(TKey[] key, TValue value)
        {
            return _trie.TryGetValue(key, out var collection) && collection!.Contains(value);
        }

        /// <summary>
        /// Gets the number of values stored under the complete key, or <c>0</c> when the key is
        /// absent (matching the indexer, which yields an empty collection rather than throwing).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// int n = map.ValueCount(new[] { "eu", "de", "berlin" }); // 2
        /// int missing = map.ValueCount(new[] { "nope" });         // 0
        /// </code>
        /// </example>
        public int ValueCount(TKey[] key)
        {
            return _trie.TryGetValue(key, out var collection) ? collection!.Count : 0;
        }

        // ------------------------------------------------------------------
        // Prefix projection
        // ------------------------------------------------------------------

        /// <summary>
        /// Determines whether the specified prefix is present in the trie at all (whether or not
        /// a key is stored exactly there). Because a key is pruned once its last value is
        /// removed, a present prefix always has at least one complete key under it.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bool has = map.ContainsPrefix(new[] { "eu" });
        /// </code>
        /// </example>
        public bool ContainsPrefix(TKey[] prefix)
        {
            return _trie.ContainsPrefix(prefix);
        }

        /// <summary>
        /// Gets the values stored exactly at the prefix, i.e.
        /// <c>TryGetValueByPrefix(key)</c> is the prefix-flavoured counterpart of
        /// <see cref="TryGetValue(TKey[],out IReadOnlyCollection{TValue})"/>. To reach
        /// <em>everything under</em> the prefix instead, use
        /// <see cref="GetByPrefix(TKey[],bool)"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// if (map.TryGetValueByPrefix(new[] { "eu", "de" }, out var values)) { }
        /// </code>
        /// </example>
        public bool TryGetValueByPrefix(TKey[] prefix, out IReadOnlyCollection<TValue> value)
        {
            return TryGetValue(prefix, out value);
        }

        /// <summary>
        /// Enumerates every complete key stored under the specified prefix — including a key
        /// stored exactly at the prefix itself (empty suffix) — together with its live value
        /// collection. The keys yielded are <b>full</b> keys, optionally rebased onto
        /// <paramref name="relative"/> = <c>true</c> (suffix only).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// foreach (var (key, values) in map.GetByPrefix(new[] { "eu" }, relative: false))
        /// {
        ///     // key is the full key, e.g. ["eu","de","berlin"]; values is that key's live collection
        /// }
        /// </code>
        /// </example>
        public IEnumerable<(TKey[] Key, IReadOnlyCollection<TValue> Values)> GetByPrefix(
            TKey[] prefix, bool relative = false)
        {
            foreach (var pair in _trie.GetByPrefix(prefix, relative))
            {
                yield return (pair.Key, AsView(pair.Value));
            }
        }

        /// <summary>
        /// Enumerates the suffix sequences (relative keys) of every complete key under the
        /// specified prefix, including the empty suffix of a key stored at the prefix itself.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// foreach (var suffix in map.GetSuffixes(new[] { "eu" }))
        /// {
        ///     // e.g. ["de","berlin"], ["de","munich"]
        /// }
        /// </code>
        /// </example>
        public IEnumerable<TKey[]> GetSuffixes(TKey[] prefix)
        {
            return _trie.GetSuffixes(prefix);
        }

        /// <summary>
        /// Gets the distinct components that directly follow the specified prefix (the trie's
        /// branching factor at that node).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// foreach (var branch in map.GetBranches(new[] { "eu" })) { }
        /// </code>
        /// </example>
        public IEnumerable<TKey> GetBranches(TKey[] prefix)
        {
            return _trie.GetBranches(prefix);
        }

        /// <summary>
        /// Gets the number of complete keys stored under the specified prefix (including a key
        /// stored exactly at the prefix). For the number of <em>values</em> under the prefix,
        /// sum the collections of <see cref="GetByPrefix(TKey[],bool)"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// int n = map.CountOfPrefix(new[] { "eu", "de" }); // 2
        /// </code>
        /// </example>
        public int CountOfPrefix(TKey[] prefix)
        {
            return _trie.CountOfPrefix(prefix);
        }

        // ------------------------------------------------------------------
        // Remove
        // ------------------------------------------------------------------

        /// <summary>
        /// Removes the complete key together with all of its values. Returns <c>true</c> when
        /// the key was present. The surrounding trie nodes are pruned once they become empty, so
        /// a trie never holds structure for keys that are no longer present.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bool removed = map.Remove(new[] { "eu", "de", "berlin" }); // removes the key with all its values
        /// </code>
        /// </example>
        public bool Remove(TKey[] key)
        {
            if (!_trie.TryGetValue(key, out var collection))
            {
                return false;
            }

            _totalValueCount -= collection!.Count;
            return _trie.Remove(key);
        }

        /// <summary>
        /// Removes a single occurrence of the value under the key. Returns <c>true</c> when a
        /// value was removed. The key is dropped automatically once its last value is removed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bool removed = map.Remove(new[] { "eu", "de", "berlin" }, 1);
        /// </code>
        /// </example>
        public bool Remove(TKey[] key, TValue value)
        {
            if (!_trie.TryGetValue(key, out var collection))
            {
                return false;
            }

            return RemoveOccurrence(key, collection!, value);
        }

        /// <summary>
        /// Removes one occurrence of each distinct value of the specified collection from the
        /// key. Returns <c>true</c> when at least one value was removed. The key is dropped
        /// automatically once its last value is removed; a missing key is a no-op that returns
        /// <c>false</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the batch form of <see cref="Remove(TKey[],TValue)"/> and behaves exactly like
        /// calling it once per <em>distinct</em> element of <paramref name="values"/>: with a
        /// duplicating inner collection (<c>allowDuplicateValues: true</c>), a value stored N
        /// times still has N-1 copies left, because one occurrence is removed per distinct
        /// argument value. Use <see cref="ExceptWith(TKey[],IEnumerable{TValue})"/> when every
        /// occurrence must go.
        /// </para>
        /// <para>
        /// The argument is a <em>set</em> of values, exactly as in
        /// <see cref="UnionWith(TKey[],IEnumerable{TValue})"/>,
        /// <see cref="IntersectionWith(TKey[],IEnumerable{TValue})"/>,
        /// <see cref="ExceptWith(TKey[],IEnumerable{TValue})"/> and
        /// <see cref="SymmetricExceptWith(TKey[],IEnumerable{TValue})"/>: a repeated value in it
        /// does not count twice, and a <c>null</c> element is an ordinary value.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// map.AddRange(new[] { "eu", "de" }, new[] { 1, 2, 3 });
        ///
        /// bool removed = map.RemoveRange(new[] { "eu", "de" }, new[] { 2, 3 });
        /// // eu,de -> [1]; removed == true
        /// </code>
        /// </example>
        public bool RemoveRange(TKey[] key, IEnumerable<TValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (!_trie.TryGetValue(key, out var collection))
            {
                return false;
            }

            // Materialized on purpose: the argument can be a live view over this very key's
            // values, and the loop below mutates the map.
            var removals = DistinctValuesOf(values);

            var removedAny = false;
            foreach (var value in removals)
            {
                if (RemoveOccurrence(key, collection!, value))
                {
                    removedAny = true;
                }
            }

            return removedAny;
        }

        /// <summary>
        /// Removes every key stored under the specified prefix, together with all of their
        /// values, including a key stored exactly at the prefix (cascade delete). Returns the
        /// number of <b>values</b> removed — the payload destroyed; call
        /// <see cref="CountOfPrefix(TKey[])"/> beforehand to learn how many complete keys are
        /// affected.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// int removedValues = map.RemovePrefix(new[] { "eu", "de" }); // drops the whole subtree at once
        /// </code>
        /// </example>
        public int RemovePrefix(TKey[] prefix)
        {
            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            // Counted before the removal: the trie reports keys, not values, and the number this
            // method returns is the number of values destroyed.
            var removed = 0;
            foreach (var pair in _trie.GetByPrefix(prefix))
            {
                removed += pair.Value.Count;
            }

            _trie.RemovePrefix(prefix);
            _totalValueCount -= removed;
            return removed;
        }

        /// <summary>
        /// Removes all keys and values.
        /// </summary>
        /// <example>
        /// <code>
        /// map.Clear();
        /// </code>
        /// </example>
        public void Clear()
        {
            _trie.Clear();
            _totalValueCount = 0;
        }

        // ------------------------------------------------------------------
        // Per-key value set operations
        // ------------------------------------------------------------------

        /// <summary>
        /// Adds each distinct value of the specified collection under the key when not already
        /// present (set-union semantics on the key's values). Creates the key when absent. With
        /// a duplicating inner collection, existing duplicate values keep their multiplicities.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// map.UnionWith(new[] { "eu", "de" }, new[] { 3, 4 });
        /// </code>
        /// </example>
        public void UnionWith(TKey[] key, IEnumerable<TValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            var seen = new HashSet<TValue>();
            foreach (var value in values)
            {
                if (seen.Add(value) && !Contains(key, value))
                {
                    Add(key, value);
                }
            }
        }

        /// <summary>
        /// Keeps under the key only the values that also appear in the specified collection
        /// (set-intersection semantics on the key's values). The key is dropped when no values
        /// remain; a missing key is a no-op.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// map.IntersectionWith(new[] { "eu", "de" }, new[] { 1 });
        /// </code>
        /// </example>
        public void IntersectionWith(TKey[] key, IEnumerable<TValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (!_trie.TryGetValue(key, out var collection))
            {
                return;
            }

            var keep = new HashSet<TValue>(values);
            var snapshot = new List<TValue>(collection!);
            foreach (var value in snapshot)
            {
                if (!keep.Contains(value))
                {
                    RemoveOccurrence(key, collection!, value);
                }
            }
        }

        /// <summary>
        /// Removes every occurrence of each value of the specified collection from the key
        /// (set-difference semantics). The key is dropped when no values remain; a missing key
        /// is a no-op.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// map.ExceptWith(new[] { "eu", "de" }, new[] { 1 }); // drops every occurrence of 1
        /// </code>
        /// </example>
        public void ExceptWith(TKey[] key, IEnumerable<TValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (!_trie.TryGetValue(key, out var collection))
            {
                return;
            }

            var removals = new HashSet<TValue>(values);
            var snapshot = new List<TValue>(collection!);
            foreach (var value in snapshot)
            {
                if (removals.Contains(value))
                {
                    while (RemoveOccurrence(key, collection!, value))
                    {
                    }
                }
            }
        }

        /// <summary>
        /// Toggles the specified values under the key: every distinct value of
        /// <paramref name="values"/> either cancels one stored occurrence — when the key already
        /// holds it — or is added when it does not. The key is created when the argument is
        /// non-empty, and dropped once no values remain; a missing key with an empty argument is
        /// a no-op.
        /// </summary>
        /// <remarks>
        /// The argument is a <em>set</em> of values, exactly as in
        /// <see cref="UnionWith(TKey[],IEnumerable{TValue})"/>,
        /// <see cref="IntersectionWith(TKey[],IEnumerable{TValue})"/> and
        /// <see cref="ExceptWith(TKey[],IEnumerable{TValue})"/>: a repeated value in it does not
        /// count twice. With a deduplicating inner collection
        /// (<c>allowDuplicateValues: false</c>) this is precisely the symmetric difference of
        /// <see cref="ISet{T}"/>; with a duplicating inner collection a value stored N times
        /// survives with N-1 copies, because one occurrence is cancelled per distinct argument
        /// value. This mirrors <see cref="MultiDictionary{TKey,TValue}.SymmetricExceptWith"/> and
        /// deliberately differs from <see cref="MultiList{T}.SymmetricExceptWith(IEnumerable{T})"/>,
        /// which treats its argument as a multiset.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// map.Add(new[] { "eu", "de" }, 1);
        /// map.Add(new[] { "eu", "de" }, 2);
        ///
        /// map.SymmetricExceptWith(new[] { "eu", "de" }, new[] { 2, 3 });
        /// // 2 is stored   -> cancelled
        /// // 3 is not stored -> added
        /// // result: eu,de -> [1, 3]
        /// </code>
        /// </example>
        public void SymmetricExceptWith(TKey[] key, IEnumerable<TValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            // Materialized on purpose: the argument can be a live view over this very key's
            // values, and the loop below mutates the map.
            var toggles = DistinctValuesOf(values);

            foreach (var value in toggles)
            {
                // Re-read the key each round: a removal may have dropped it (RemoveOccurrence
                // removes the key once nothing is left under it), and the adding branch recreates
                // it. Going through the public Add keeps the "no value-less key" invariant in a
                // single place.
                if (!_trie.TryGetValue(key, out var collection) || !RemoveOccurrence(key, collection!, value))
                {
                    Add(key, value);
                }
            }
        }

        // ------------------------------------------------------------------
        // Copying
        // ------------------------------------------------------------------

        /// <summary>
        /// Creates a deep copy of the map: key components and values are shared, the structure
        /// is independent.
        /// </summary>
        /// <example>
        /// <code>
        /// MultiKeyMultiDictionary&lt;string, int&gt; copy = map.Clone();
        /// </code>
        /// </example>
        public MultiKeyMultiDictionary<TKey, TValue> Clone()
        {
            // Built through the public Add (rather than by reaching into the clone's trie) so the
            // clone's cached total is maintained by the same single set of rules as any other
            // instance - no second, hand-written copy of the bookkeeping to keep in step.
            var clone = new MultiKeyMultiDictionary<TKey, TValue>(_trie.Comparer, _innerFactory);
            foreach (var pair in _trie)
            {
                foreach (var value in pair.Value)
                {
                    clone.Add(pair.Key, value);
                }
            }

            return clone;
        }

        /// <summary>
        /// Returns a live read-only view of the map: enumeration reflects subsequent changes to
        /// the owning map, one (key, value) pair per stored value. Mutating members are not
        /// exposed.
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

        /// <summary>
        /// Exports every entry under the specified prefix as a snapshot dictionary keyed by the
        /// <b>full</b> key. The outer dictionary is independent of the map; each inner collection
        /// is a live view of the values stored for that key.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="prefix"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// IReadOnlyDictionary&lt;string[], IReadOnlyCollection&lt;int&gt;&gt; snapshot =
        ///     map.ToDictionary(new[] { "eu" });
        /// </code>
        /// </example>
        public IReadOnlyDictionary<TKey[], IReadOnlyCollection<TValue>> ToDictionary(TKey[] prefix)
        {
            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            var dictionary = new Dictionary<TKey[], IReadOnlyCollection<TValue>>(ArrayEqualityComparer.Instance);
            foreach (var pair in _trie.GetByPrefix(prefix))
            {
                dictionary.Add(pair.Key, AsView(pair.Value));
            }

            return dictionary;
        }

        /// <summary>
        /// Exports the whole map as a snapshot dictionary keyed by the full key. The outer
        /// dictionary is independent of the map; each inner collection is a live view of the
        /// values stored for that key.
        /// </summary>
        /// <example>
        /// <code>
        /// IReadOnlyDictionary&lt;string[], IReadOnlyCollection&lt;int&gt;&gt; snapshot = map.ToDictionary();
        /// </code>
        /// </example>
        public IReadOnlyDictionary<TKey[], IReadOnlyCollection<TValue>> ToDictionary()
        {
            return ToDictionary(new TKey[0]);
        }

        // ------------------------------------------------------------------
        // Enumeration
        // ------------------------------------------------------------------

        /// <summary>
        /// Enumerates the map as flat (key, value) pairs in depth-first key order — one entry per
        /// stored value. Each yielded key is a fresh array of the full key sequence; mutating it
        /// does not affect the map.
        /// </summary>
        /// <example>
        /// <code>
        /// foreach (var (key, value) in map) { }
        /// </code>
        /// </example>
        public IEnumerator<(TKey[] Key, TValue Value)> GetEnumerator()
        {
            foreach (var pair in _trie)
            {
                foreach (var value in pair.Value)
                {
                    yield return (pair.Key, value);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns the contents in expanded per-key form, comma separated,
        /// e.g. <c>[eu,de]:[1,2],[eu,fr]:[3]</c>.
        /// </summary>
        /// <example>
        /// <code>
        /// string text = map.ToString();
        /// </code>
        /// </example>
        public override string ToString()
        {
            var parts = new List<string>();
            foreach (var pair in _trie)
            {
                parts.Add($"[{string.Join(",", pair.Key)}]:[{string.Join(",", pair.Value)}]");
            }

            return string.Join(",", parts);
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        /// <summary>
        /// Removes a single occurrence of <paramref name="value"/> from the key's collection and
        /// drops the key once nothing is left under it (the trie prunes the empty node).
        /// Returns <c>true</c> when an occurrence was actually removed.
        /// </summary>
        private bool RemoveOccurrence(TKey[] key, ICollection<TValue> collection, TValue value)
        {
            if (!collection.Remove(value))
            {
                return false;
            }

            _totalValueCount--;
            if (collection.Count == 0)
            {
                _trie.Remove(key);
            }

            return true;
        }

        /// <summary>
        /// Wraps the key's inner collection as a live <see cref="IReadOnlyCollection{TValue}"/>.
        /// Wrapped, not cast: the framework's <see cref="HashSet{T}"/> (the
        /// <c>allowDuplicateValues: false</c> factory) does not declare that interface on
        /// net451 / net461 (neither statically nor at runtime), so a direct cast would fail on
        /// exactly those targets the package supports (the F6-24 hazard).
        /// </summary>
        private static IReadOnlyCollection<TValue> AsView(ICollection<TValue> collection)
        {
            return new ReadOnlyCollectionView<TValue>(collection);
        }

        /// <summary>
        /// Returns the distinct values of a sequence, tolerating <c>null</c>. A <c>null</c> is an
        /// ordinary value here but can not be a key of the <see cref="HashSet{T}"/> used for the
        /// remaining values, so it is tracked by a flag — the same strategy
        /// <see cref="MultiList{T}"/> uses for its null bucket.
        /// </summary>
        private static List<TValue> DistinctValuesOf(IEnumerable<TValue> values)
        {
            var result = new List<TValue>();
            var seen = new HashSet<TValue>();
            var hasNull = false;

            foreach (var value in values)
            {
                if (value == null)
                {
                    if (!hasNull)
                    {
                        hasNull = true;
                        result.Add(value);
                    }
                }
                else if (seen.Add(value))
                {
                    result.Add(value);
                }
            }

            return result;
        }

        /// <summary>
        /// Equality comparer for <c>TKey[]</c> keys, used by
        /// <see cref="ToDictionary(TKey[])"/>. Sequence equality is element-wise via
        /// <see cref="EqualityComparer{TKey}.Default"/>; it deliberately does not take the trie's
        /// injected comparer, since the resulting dictionary is a plain snapshot container
        /// (mirroring <see cref="MultiKeyDictionary{TKey,TValue}.ToDictionary(TKey[])"/>).
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

        private sealed class ReadOnlyView : IReadOnlyCollection<(TKey[] Key, TValue Value)>
        {
            private readonly MultiKeyMultiDictionary<TKey, TValue> _owner;

            public ReadOnlyView(MultiKeyMultiDictionary<TKey, TValue> owner)
            {
                _owner = owner;
            }

            // The view enumerates flat (key, value) pairs - one per stored value - so its Count
            // is the value total, not the key count.
            public int Count => _owner.TotalValueCount;

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
