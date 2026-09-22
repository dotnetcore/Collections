using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// CS8714: TKey is deliberately unconstrained (null keys are rejected at runtime with
// ArgumentNullException, but the type parameter itself must stay nullable-friendly,
// e.g. TKey = string?); the "notnull" key constraint of the annotated
// SortedDictionary<TKey, TValue> (net5.0+ reference assemblies) is a false positive here.
#pragma warning disable CS8714

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Represents an <b>ordered</b> multimap: a dictionary that allows multiple values per key,
    /// where the keys are kept in ascending order and the values stored under every key are kept
    /// in ascending order as well. Adding, looking up and removing a single (key, value) pair runs
    /// in O(log n) worst case on both axes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the ordered counterpart of <see cref="MultiDictionary{TKey,TValue}"/>. The two
    /// types share their per-key value-set semantics — the argument of every set operation is a
    /// <em>set</em> of values, a value may be stored several times under one key, and an inner
    /// collection is recycled the moment it empties so the map never holds a key without values —
    /// and differ in the storage underneath and in what that storage buys:
    /// <see cref="MultiDictionary{TKey,TValue}"/> keeps hash tables and answers in O(1) but has no
    /// defined order, while this type keeps an order-statistic B+ tree and answers in O(log n)
    /// while enumerating keys and values in sorted order.
    /// </para>
    /// <para>
    /// <b>The storage layer is one B+ tree, not a dictionary of collections (F6-36).</b> Every
    /// distinct key is a slot in a leaf of <see cref="OrderStatisticTree{TKey,TPayload}"/>, holding
    /// the key, the key's value bucket and the number of values in that bucket; every node caches
    /// the number of values stored below it. Two things follow from that shape. First, enumeration
    /// is a single walk of the chained leaves, so enumerating a key costs no heap object at all —
    /// the earlier storage, a <see cref="SortedDictionary{TKey,TValue}"/> over per-key collections,
    /// built two iterator objects per key (≈112 bytes/key, measured) because the value side was
    /// reached through an interface. Second, the cached totals turn the positional reads
    /// (<see cref="GetByRank(int)"/>, <see cref="GetRank(TKey,TValue)"/>, <see cref="GetMedian"/>
    /// and <see cref="GetQuantile(double)"/>) into a single root-to-leaf descent instead of a
    /// materialised copy of the whole expanded sequence.
    /// </para>
    /// <para>
    /// <b>Ordering and identity are the same decision on each axis, and they are made by
    /// comparers.</b> The key axis takes an <see cref="IComparer{TKey}"/>, exactly like
    /// <see cref="SortedDictionary{TKey,TValue}"/>; the value axis takes an
    /// <see cref="IComparer{TValue}"/>, exactly like <see cref="OrderedMultiList{T}"/>. That is
    /// deliberately different from <see cref="MultiDictionary{TKey,TValue}"/>, whose single
    /// <see cref="IEqualityComparer{TKey}"/> supplies hash codes but no ordering: a comparison
    /// result of <c>0</c> is what makes two keys (or two values) the same one, and the value
    /// actually stored is the first of the equals that was added — the rule a comparer-keyed
    /// collection already follows.
    /// </para>
    /// <para>
    /// <b>Keys may not be <c>null</c></b> (<see cref="ArgumentNullException"/>, the standard
    /// dictionary behaviour); a key type whose comparer can not compare <c>null</c> therefore never
    /// has to. <b>Null values are supported</b> as long as the value comparer orders them: under
    /// <see cref="System.Collections.Generic.Comparer{T}.Default"/> <c>null</c> sorts first, while
    /// a custom comparer decides for itself where <c>null</c> belongs and may reject it by
    /// throwing. This type never inspects values itself, so whatever the value comparer does with
    /// <c>null</c> is what happens — the doctrine
    /// <see cref="OrderedMultiList{T}"/> already follows on its element axis. Since F6-36 both
    /// duplicate policies share one bucket type, so the <c>allowDuplicateValues: false</c> policy
    /// accepts <c>null</c> values exactly the way the duplicating one does.
    /// </para>
    /// <para>
    /// <b>Ordering guarantees.</b> <see cref="Keys"/>, enumeration and <see cref="ToString"/> walk
    /// the keys in ascending order; <see cref="this[TKey]"/>, <see cref="Values"/>,
    /// <see cref="TryGetValue(TKey, out IReadOnlyCollection{TValue})"/> and
    /// <see cref="EntrySet"/> walk the values of every key in ascending order. Values of different
    /// keys are never interleaved by order — the keys decide the outer order and each key's own
    /// values decide the inner one. The positional reads address the same sequence enumeration
    /// produces: rank <c>0</c> is the smallest value of the smallest key, and rank
    /// <see cref="TotalValueCount"/> - 1 is the largest value of the largest key.
    /// </para>
    /// <para>
    /// <b>Counts.</b> <see cref="Count"/> and <see cref="KeyCount"/> are the number of
    /// <em>keys</em>; <see cref="TotalValueCount"/> and <see cref="ValueCount(TKey)"/> are the
    /// number of stored <em>values</em>. A key that is present always holds at least one value,
    /// because a bucket is dropped the moment it empties.
    /// </para>
    /// <para>
    /// This class is one of the "multi" family of this package. Read the name as "what is
    /// multiplied": <see cref="MultiList{T}"/> multiplies <em>elements</em> (1 element &#8594; N
    /// copies), <see cref="MultiDictionary{TKey,TValue}"/> multiplies <em>values</em> per key
    /// (1 key &#8594; N values) and <see cref="MultiKeyDictionary{TKey,TValue}"/> multiplies
    /// <em>key components</em> (N components &#8594; 1 value).
    /// </para>
    /// <para>
    /// This class is not thread-safe. Wrap it with external synchronization for concurrent use.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var index = new OrderedMultiDictionary&lt;string, int&gt;();
    /// index.Add("beta", 2);
    /// index.Add("alpha", 30);
    /// index.Add("alpha", 10);
    ///
    /// foreach (var key in index.Keys) { /* "alpha", "beta" */ }
    ///
    /// foreach (var value in index["alpha"]) { /* 10, 30 */ }
    ///
    /// foreach (var pair in index) { /* ("alpha",10), ("alpha",30), ("beta",2) */ }
    ///
    /// index.GetByRank(0);                      // ("alpha", 10)
    /// index.GetMedian();                       // ("alpha", 30)
    /// index.GetRank("alpha", 30);              // 1
    /// </code>
    /// </example>
    public class OrderedMultiDictionary<TKey, TValue> :
        IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>>,
        IMultiDictionary<TKey, TValue>,
        IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private static readonly IReadOnlyCollection<TValue> EmptyValues = new TValue[0];

        /// <summary>
        /// The whole storage layer: one order-statistic B+ tree keyed by the key, each slot holding
        /// the key, the key's value bucket and the number of values in it, with the value total
        /// cached on every node so the positional reads can descend on it.
        /// </summary>
        private readonly OrderStatisticTree<TKey, OrderedMultiList<TValue>> _tree;
        private readonly IComparer<TKey> _keyComparer;
        private readonly IComparer<TValue> _valueComparer;
        private readonly bool _allowDuplicateValues;

        /// <summary>
        /// Initializes an empty <see cref="OrderedMultiDictionary{TKey,TValue}"/> that allows
        /// duplicate values per key and orders both axes with
        /// <see cref="System.Collections.Generic.Comparer{T}.Default"/>.
        /// </summary>
        public OrderedMultiDictionary()
            : this(null, null)
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="OrderedMultiDictionary{TKey,TValue}"/> with the
        /// specified key comparer, allowing duplicate values per key and ordering the value axis
        /// with <see cref="System.Collections.Generic.Comparer{T}.Default"/>.
        /// </summary>
        /// <param name="keyComparer">the comparer that defines key order and key identity, or <c>null</c> for <see cref="System.Collections.Generic.Comparer{T}.Default"/>.</param>
        public OrderedMultiDictionary(IComparer<TKey>? keyComparer)
            : this(keyComparer, null)
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="OrderedMultiDictionary{TKey,TValue}"/>. When
        /// <paramref name="allowDuplicateValues"/> is <c>false</c>, a value already stored under
        /// the key is silently ignored instead of being stored a second time.
        /// </summary>
        /// <param name="allowDuplicateValues">whether the same value may be stored more than once under one key.</param>
        public OrderedMultiDictionary(bool allowDuplicateValues)
            : this(null, null, allowDuplicateValues)
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="OrderedMultiDictionary{TKey,TValue}"/> with the
        /// specified key comparer. When <paramref name="allowDuplicateValues"/> is <c>false</c>, a
        /// value already stored under the key is silently ignored.
        /// </summary>
        /// <param name="keyComparer">the comparer that defines key order and key identity, or <c>null</c> for <see cref="System.Collections.Generic.Comparer{T}.Default"/>.</param>
        /// <param name="allowDuplicateValues">whether the same value may be stored more than once under one key.</param>
        public OrderedMultiDictionary(IComparer<TKey>? keyComparer, bool allowDuplicateValues)
            : this(keyComparer, null, allowDuplicateValues)
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="OrderedMultiDictionary{TKey,TValue}"/> ordering the two
        /// axes with the specified comparers, allowing duplicate values per key.
        /// </summary>
        /// <param name="keyComparer">the comparer that defines key order and key identity, or <c>null</c> for <see cref="System.Collections.Generic.Comparer{T}.Default"/>.</param>
        /// <param name="valueComparer">the comparer that defines value order and value identity, or <c>null</c> for <see cref="System.Collections.Generic.Comparer{T}.Default"/>.</param>
        public OrderedMultiDictionary(IComparer<TKey>? keyComparer, IComparer<TValue>? valueComparer)
            : this(keyComparer, valueComparer, true)
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="OrderedMultiDictionary{TKey,TValue}"/> ordering the two
        /// axes with the specified comparers. When <paramref name="allowDuplicateValues"/> is
        /// <c>false</c>, a value already stored under the key is silently ignored instead of being
        /// stored a second time.
        /// </summary>
        /// <param name="keyComparer">the comparer that defines key order and key identity, or <c>null</c> for <see cref="System.Collections.Generic.Comparer{T}.Default"/>.</param>
        /// <param name="valueComparer">the comparer that defines value order and value identity, or <c>null</c> for <see cref="System.Collections.Generic.Comparer{T}.Default"/>.</param>
        /// <param name="allowDuplicateValues">whether the same value may be stored more than once under one key.</param>
        public OrderedMultiDictionary(IComparer<TKey>? keyComparer, IComparer<TValue>? valueComparer, bool allowDuplicateValues = true)
        {
            _keyComparer = keyComparer ?? Comparer<TKey>.Default;
            _valueComparer = valueComparer ?? Comparer<TValue>.Default;
            _allowDuplicateValues = allowDuplicateValues;
            _tree = new OrderStatisticTree<TKey, OrderedMultiList<TValue>>(_keyComparer);
        }

        /// <summary>
        /// Gets the comparer that defines the order of the keys, and with it which keys count as
        /// the same key. This is an <see cref="IComparer{TKey}"/>, not the
        /// <see cref="IEqualityComparer{TKey}"/> that <see cref="MultiDictionary{TKey,TValue}"/>
        /// takes: an ordered structure has to know which of two keys comes first.
        /// </summary>
        public IComparer<TKey> Comparer => _keyComparer;

        /// <summary>
        /// Gets the comparer that defines the order of the values under each key, and with it
        /// which values count as the same value. This is an <see cref="IComparer{TValue}"/>, for
        /// the same reason <see cref="Comparer"/> is one.
        /// </summary>
        public IComparer<TValue> ValueComparer => _valueComparer;

        /// <summary>
        /// Gets the number of keys in the map.
        /// </summary>
        public int Count => _tree.Count;

        /// <summary>
        /// Gets the number of keys in the map (alias of <see cref="Count"/>).
        /// </summary>
        public int KeyCount => _tree.Count;

        /// <summary>
        /// Gets the total number of values across all keys. Runs in O(1): the tree caches the value
        /// total of every sub-tree, so the figure is read off the root rather than summed over the
        /// keys.
        /// </summary>
        public int TotalValueCount => _tree.ElementCount;

        /// <summary>
        /// Gets the number of values stored under the key, or <c>0</c> when the key is absent
        /// (matching the indexer, which yields an empty collection rather than throwing).
        /// </summary>
        /// <param name="key">the key to count the values of.</param>
        /// <returns>the number of values stored under <paramref name="key"/>.</returns>
        /// <remarks>
        /// Runs in O(log n): the count is the figure the tree already caches for the slot, so no
        /// bucket has to be touched. Use <see cref="ContainsKey(TKey)"/> when the absent-key case
        /// must be told apart from a key that is present with zero values — the latter can not
        /// occur, because a bucket is dropped as soon as it empties.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// int n = index.ValueCount("alpha"); // 2
        /// int missing = index.ValueCount("nope"); // 0
        /// </code>
        /// </example>
        public int ValueCount(TKey key)
        {
            ThrowIfNullKey(key);
            return _tree.TryGetCount(key, out var count) ? count : 0;
        }

        /// <summary>
        /// Gets the keys of the map, in ascending order.
        /// </summary>
        public IEnumerable<TKey> Keys
        {
            get
            {
                var entries = _tree.GetAscendingEnumerator();
                while (entries.MoveNext())
                {
                    yield return entries.Key;
                }
            }
        }

        /// <summary>
        /// Gets all values of the map, flattened across keys: the keys in ascending order, and the
        /// values of each key in ascending order.
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get
            {
                var entries = _tree.GetAscendingEnumerator();
                while (entries.MoveNext())
                {
                    var values = entries.Payload.GetAscendingEnumerator();
                    while (values.MoveNext())
                    {
                        for (var copy = 0; copy < values.Count; copy++)
                        {
                            yield return values.Key;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the values associated with the key, in ascending order. Returns an empty collection
        /// (never <c>null</c>) when the key is absent. The returned collection is a live view of
        /// the values stored for the key, and it stays sorted.
        /// </summary>
        /// <param name="key">the key to read the values of.</param>
        /// <returns>the values stored under <paramref name="key"/>, in ascending order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// foreach (var value in index["alpha"]) { /* 10, 30 */ }
        /// </code>
        /// </example>
        public IReadOnlyCollection<TValue> this[TKey key]
        {
            get
            {
                ThrowIfNullKey(key);
                return _tree.TryGetEntry(key, out var bucket, out _) ? AsReadOnlyView(bucket) : EmptyValues;
            }
        }

        /// <summary>
        /// Adds a (key, value) pair. When duplicate values are disallowed and the value already
        /// exists under the key, the call is silently ignored, exactly as in
        /// <see cref="MultiDictionary{TKey,TValue}.Add(TKey,TValue)"/>.
        /// </summary>
        /// <param name="key">the key to store the value under.</param>
        /// <param name="value">the value to store.</param>
        /// <remarks>
        /// Whether two values count as the same one is decided by <see cref="ValueComparer"/>, not
        /// by <see cref="object.Equals(object)"/>. Runs in O(log n) on both axes: one descent finds
        /// (or creates) the key's slot, one more finds the value's place inside the bucket.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// index.Add("alpha", 10);
        /// index.Add("alpha", 30);
        /// </code>
        /// </example>
        public void Add(TKey key, TValue value)
        {
            ThrowIfNullKey(key);

            if (_tree.TryGetEntry(key, out var bucket, out var count))
            {
                if (!_allowDuplicateValues && bucket.CountOf(value) != 0)
                {
                    return;
                }

                bucket.Add(value);
                SyncWeight(key, count, bucket);
                return;
            }

            var created = NewBucket();
            created.Add(value);
            _tree.AddCount(key, 1, created);
        }

        /// <summary>
        /// Adds each value of the specified collection under the key.
        /// </summary>
        /// <param name="key">the key to store the values under.</param>
        /// <param name="values">the values to store.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>, or <paramref name="values"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// index.AddRange("alpha", new[] { 10, 30, 20 });
        /// // index["alpha"] enumerates 10, 20, 30
        /// </code>
        /// </example>
        public void AddRange(TKey key, IEnumerable<TValue> values)
        {
            ThrowIfNullKey(key);
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            foreach (var value in values)
            {
                Add(key, value);
            }
        }

        /// <summary>
        /// Determines whether the map contains the key.
        /// </summary>
        /// <param name="key">the key to look for.</param>
        /// <returns><c>true</c> when the key is present.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bool has = index.ContainsKey("alpha");
        /// </code>
        /// </example>
        public bool ContainsKey(TKey key)
        {
            ThrowIfNullKey(key);
            return _tree.TryGetCount(key, out _);
        }

        /// <summary>
        /// Determines whether the specified value exists under the key.
        /// </summary>
        /// <param name="key">the key to look under.</param>
        /// <param name="value">the value to look for.</param>
        /// <returns><c>true</c> when the value is stored under the key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bool has = index.Contains("alpha", 10);
        /// </code>
        /// </example>
        public bool Contains(TKey key, TValue value)
        {
            ThrowIfNullKey(key);
            return _tree.TryGetEntry(key, out var bucket, out _) && bucket.Contains(value);
        }

        /// <summary>
        /// Determines whether the specified value exists under any key. This is a full scan of the
        /// value axis, O(total values), because a value is not part of the outer ordering.
        /// </summary>
        /// <param name="value">the value to look for.</param>
        /// <returns><c>true</c> when the value is stored under at least one key.</returns>
        /// <example>
        /// <code>
        /// bool has = index.ContainsValue(10);
        /// </code>
        /// </example>
        public bool ContainsValue(TValue value)
        {
            var entries = _tree.GetAscendingEnumerator();
            while (entries.MoveNext())
            {
                if (entries.Payload.Contains(value))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes the key together with all of its values. Returns <c>true</c> when the key was
        /// present.
        /// </summary>
        /// <param name="key">the key to remove.</param>
        /// <returns><c>true</c> when the key was present.</returns>
        /// <remarks>
        /// Runs in O(log n): the bucket hangs off the slot that is removed, so dropping the key
        /// drops its values in the same step.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bool removed = index.Remove("alpha");
        /// // removes the key with all its values
        /// </code>
        /// </example>
        public bool Remove(TKey key)
        {
            ThrowIfNullKey(key);
            return _tree.TryRemoveKey(key);
        }

        /// <summary>
        /// Removes a single occurrence of the value under the key. Returns <c>true</c> when a value
        /// was removed. The key is dropped automatically once its last value is removed.
        /// </summary>
        /// <param name="key">the key to remove the value from.</param>
        /// <param name="value">the value to remove one occurrence of.</param>
        /// <returns><c>true</c> when a value was removed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bool removed = index.Remove("alpha", 10);
        /// </code>
        /// </example>
        public bool Remove(TKey key, TValue value)
        {
            ThrowIfNullKey(key);
            if (!_tree.TryGetEntry(key, out var bucket, out _))
            {
                return false;
            }

            var before = bucket.TotalCount;
            bucket.Remove(value);
            if (bucket.TotalCount == before)
            {
                return false;
            }

            SyncWeight(key, before, bucket);
            return true;
        }

        /// <summary>
        /// Removes one occurrence of each distinct value of the specified collection from the key.
        /// Returns <c>true</c> when at least one value was removed. The key is dropped
        /// automatically once its last value is removed; a missing key is a no-op that returns
        /// <c>false</c>.
        /// </summary>
        /// <param name="key">the key to remove the values from.</param>
        /// <param name="values">the values to remove one occurrence of each.</param>
        /// <returns><c>true</c> when at least one value was removed.</returns>
        /// <remarks>
        /// <para>
        /// This mirrors <see cref="MultiDictionary{TKey,TValue}.RemoveRange(TKey,IEnumerable{TValue})"/>
        /// exactly: it is the batch form of <see cref="Remove(TKey,TValue)"/> and behaves like
        /// calling it once per <em>distinct</em> value of <paramref name="values"/> — the ordering
        /// of the map does not change that. So a value stored N times still has N-1 copies left,
        /// and <see cref="ExceptWith(TKey,IEnumerable{TValue})"/> remains the operation that drops
        /// every occurrence.
        /// </para>
        /// <para>
        /// Whether two argument values count as one is decided by
        /// <see cref="ValueComparer"/>, not by <see cref="object.Equals(object)"/>.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>, or <paramref name="values"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// index.AddRange("alpha", new[] { 10, 20, 30 });
        ///
        /// bool removed = index.RemoveRange("alpha", new[] { 20, 30 });
        /// // alpha -> [10]; removed == true
        /// </code>
        /// </example>
        public bool RemoveRange(TKey key, IEnumerable<TValue> values)
        {
            ThrowIfNullKey(key);
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (!_tree.TryGetEntry(key, out var bucket, out _))
            {
                return false;
            }

            // Materialized on purpose: the argument can be a live view over this very map's
            // values, and the loop below mutates the map.
            var removals = DistinctValuesOf(values);

            var before = bucket.TotalCount;
            foreach (var value in removals)
            {
                bucket.Remove(value);
            }

            if (bucket.TotalCount == before)
            {
                return false;
            }

            SyncWeight(key, before, bucket);
            return true;
        }

        // ------------------------------------------------------------------
        // Per-key value set operations
        // ------------------------------------------------------------------

        /// <summary>
        /// Adds each distinct value of the specified collection under the key when not already
        /// present (set-union semantics on the key's values). Creates the key when absent. With a
        /// duplicating bucket, existing duplicate values keep their multiplicities.
        /// </summary>
        /// <param name="key">the key to union the values into.</param>
        /// <param name="values">the values to union in.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>, or <paramref name="values"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// index.UnionWith("alpha", new[] { 30, 40 });
        /// </code>
        /// </example>
        public void UnionWith(TKey key, IEnumerable<TValue> values)
        {
            ThrowIfNullKey(key);
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            var incoming = ToComparerAwareSet(values);
            if (incoming.Count == 0)
            {
                return;
            }

            if (!_tree.TryGetEntry(key, out var bucket, out _))
            {
                foreach (var value in incoming)
                {
                    Add(key, value);
                }

                return;
            }

            var before = bucket.TotalCount;
            foreach (var value in incoming)
            {
                if (bucket.CountOf(value) == 0)
                {
                    bucket.Add(value);
                }
            }

            SyncWeight(key, before, bucket);
        }

        /// <summary>
        /// Keeps under the key only the values that also appear in the specified collection
        /// (set-intersection semantics on the key's values). The key is dropped when no values
        /// remain; a missing key is a no-op.
        /// </summary>
        /// <param name="key">the key to intersect the values of.</param>
        /// <param name="values">the values to keep.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>, or <paramref name="values"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// index.IntersectionWith("alpha", new[] { 10 });
        /// </code>
        /// </example>
        public void IntersectionWith(TKey key, IEnumerable<TValue> values)
        {
            ThrowIfNullKey(key);
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (!_tree.TryGetEntry(key, out var bucket, out _))
            {
                return;
            }

            var keep = ToComparerAwareSet(values);
            var before = bucket.TotalCount;
            foreach (var value in DistinctSnapshotOf(bucket))
            {
                if (!keep.Contains(value))
                {
                    bucket.RemoveAllCopies(value);
                }
            }

            SyncWeight(key, before, bucket);
        }

        /// <summary>
        /// Removes every occurrence of each value of the specified collection from the key
        /// (set-difference semantics). The key is dropped when no values remain; a missing key is a
        /// no-op.
        /// </summary>
        /// <param name="key">the key to remove the values from.</param>
        /// <param name="values">the values to remove every occurrence of.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>, or <paramref name="values"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// index.ExceptWith("alpha", new[] { 10 });
        /// </code>
        /// </example>
        public void ExceptWith(TKey key, IEnumerable<TValue> values)
        {
            ThrowIfNullKey(key);
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (!_tree.TryGetEntry(key, out var bucket, out _))
            {
                return;
            }

            var removals = ToComparerAwareSet(values);
            var before = bucket.TotalCount;
            foreach (var value in DistinctSnapshotOf(bucket))
            {
                if (removals.Contains(value))
                {
                    bucket.RemoveAllCopies(value);
                }
            }

            SyncWeight(key, before, bucket);
        }

        /// <summary>
        /// Toggles the specified values under the key: every distinct value of
        /// <paramref name="values"/> either cancels one stored occurrence — when the key already
        /// holds it — or is added when it does not. The key is created when the argument is
        /// non-empty, and dropped once no values remain; a missing key with an empty argument is a
        /// no-op.
        /// </summary>
        /// <param name="key">the key to toggle the values of.</param>
        /// <param name="values">the values to toggle.</param>
        /// <remarks>
        /// <para>
        /// The argument is a <em>set</em> of values, exactly as in
        /// <see cref="UnionWith(TKey,IEnumerable{TValue})"/>,
        /// <see cref="IntersectionWith(TKey,IEnumerable{TValue})"/> and
        /// <see cref="ExceptWith(TKey,IEnumerable{TValue})"/>: a repeated value in it does not
        /// count twice. With a deduplicating bucket this is precisely the symmetric difference of
        /// <see cref="ISet{T}"/>; with a duplicating one, a value stored N times survives with N-1
        /// copies.
        /// </para>
        /// <para>
        /// This matches <see cref="MultiDictionary{TKey,TValue}.SymmetricExceptWith(TKey,IEnumerable{TValue})"/>
        /// and therefore deliberately differs from
        /// <see cref="MultiList{T}.SymmetricExceptWith(IEnumerable{T})"/>, which treats its
        /// argument as a <em>multiset</em>. The per-key operations of one type keep one coherent
        /// convention, and for a map over an <em>ordered</em> value axis the only extra thing the
        /// ordering changes is the order the surviving values are enumerated in.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>, or <paramref name="values"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// index.SymmetricExceptWith("alpha", new[] { 10, 50 });
        /// </code>
        /// </example>
        public void SymmetricExceptWith(TKey key, IEnumerable<TValue> values)
        {
            ThrowIfNullKey(key);
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            // Materialized on purpose: the argument can be a live view over this very map's
            // values, and the loop below mutates the map.
            var toggles = DistinctValuesOf(values);

            if (!_tree.TryGetEntry(key, out var bucket, out _))
            {
                foreach (var value in toggles)
                {
                    Add(key, value);
                }

                return;
            }

            var before = bucket.TotalCount;
            foreach (var value in toggles)
            {
                // "Toggled off" is told apart from "was not stored" by the total, not by the return
                // value of Remove: removing the last copy and finding nothing both report zero
                // remaining, and only the total says which of the two happened.
                var totalBefore = bucket.TotalCount;
                bucket.Remove(value);
                if (bucket.TotalCount == totalBefore)
                {
                    // Toggled on: the value was not stored. The bucket is mutated directly and the
                    // cached total is reconciled once, at the end of the loop. Going through the
                    // public Add here would adjust the cached total as well, and the two
                    // adjustments would then count the value twice; worse, the adjustment the
                    // public Add derives its delta from is the cached total of the whole key, which
                    // an earlier iteration of this loop has already invalidated.
                    bucket.Add(value);
                }
            }

            SyncWeight(key, before, bucket);
        }

        /// <summary>
        /// Removes all keys and values.
        /// </summary>
        /// <example>
        /// <code>
        /// index.Clear();
        /// </code>
        /// </example>
        public void Clear()
        {
            _tree.Clear();
        }

        /// <summary>
        /// Gets the values associated with the key, in ascending order. When the key is absent,
        /// returns <c>false</c> and <paramref name="value"/> is <c>null</c> (check the return value
        /// before use).
        /// </summary>
        /// <param name="key">the key to read the values of.</param>
        /// <param name="value">receives the values stored under the key, in ascending order.</param>
        /// <returns><c>true</c> when the key is present.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// if (index.TryGetValue("alpha", out var values))
        /// {
        ///     foreach (var v in values) { }
        /// }
        /// </code>
        /// </example>
        public bool TryGetValue(TKey key, out IReadOnlyCollection<TValue> value)
        {
            ThrowIfNullKey(key);
            if (_tree.TryGetEntry(key, out var bucket, out _))
            {
                value = AsReadOnlyView(bucket);
                return true;
            }

            value = null!;
            return false;
        }

        // ------------------------------------------------------------------
        // Positional reads over the expanded sequence (F6-36)
        // ------------------------------------------------------------------

        /// <summary>
        /// Gets the (key, value) pair holding the copy at the specified rank of the expanded
        /// sequence, where rank zero is the smallest value of the smallest key and rank
        /// <see cref="TotalValueCount"/> - 1 the largest value of the largest key. Runs in O(log n):
        /// the walk skips whole sub-trees by their cached value totals and then reads the offset it
        /// landed on out of the key's own bucket, both in the same descent.
        /// </summary>
        /// <param name="rank">the zero-based rank, from zero to <see cref="TotalValueCount"/> - 1.</param>
        /// <returns>the pair holding that copy.</returns>
        /// <remarks>
        /// This is the positional read of the whole map, and it addresses exactly the sequence
        /// enumeration produces. Before F6-36 the type could only answer it by materialising the
        /// entire expanded sequence and indexing into that copy.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="rank"/> is negative or
        /// greater than or equal to <see cref="TotalValueCount"/>.</exception>
        /// <example>
        /// <code>
        /// // alpha -> [10, 30], beta -> [2]
        /// index.GetByRank(0);                      // ("alpha", 10)
        /// index.GetByRank(2);                      // ("beta", 2)
        /// </code>
        /// </example>
        public KeyValuePair<TKey, TValue> GetByRank(int rank)
        {
            if (!_tree.TryGetByRank(rank, out var key, out var bucket, out var offsetInBucket))
            {
                throw new ArgumentOutOfRangeException(nameof(rank), rank, "The rank must address one of the stored values, from zero to TotalValueCount - 1.");
            }

            return new KeyValuePair<TKey, TValue>(key, bucket.GetByRank(offsetInBucket));
        }

        /// <summary>
        /// Gets the rank of the first copy of the value under the key, in O(log n): the position
        /// that copy enumerates at, counting from the smallest value of the smallest key. Returns
        /// <c>-1</c> when the key is absent or holds no copy of the value, the way
        /// <see cref="OrderedMultiList{T}.GetRank(T)"/> reports an absent element.
        /// </summary>
        /// <param name="key">the key holding the value.</param>
        /// <param name="value">the value to locate.</param>
        /// <returns>the rank of the value's first copy, or <c>-1</c> when it is not stored.</returns>
        /// <remarks>
        /// Whether the argument counts as a stored value is decided by <see cref="ValueComparer"/>,
        /// so a value the comparer deems equal to a stored one addresses that one's first copy.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// // alpha -> [10, 30], beta -> [2]
        /// index.GetRank("alpha", 30);              // 1
        /// index.GetRank("alpha", 99);              // -1
        /// </code>
        /// </example>
        public int GetRank(TKey key, TValue value)
        {
            ThrowIfNullKey(key);
            if (!_tree.TryGetRankOf(key, out var rankBefore))
            {
                return -1;
            }

            if (!_tree.TryGetEntry(key, out var bucket, out _))
            {
                return -1;
            }

            var local = bucket.GetRank(value);
            return local < 0 ? -1 : rankBefore + local;
        }

        /// <summary>
        /// Gets the (key, value) pair holding the middle copy of the expanded sequence, in O(log n).
        /// With an even number of values there is no single middle one, and the pair holding the
        /// lower of the two middle copies is returned. Use <see cref="GetQuantile(double)"/> to ask
        /// for a different point of the distribution.
        /// </summary>
        /// <returns>the pair holding the middle copy.</returns>
        /// <exception cref="InvalidOperationException">The map holds no values.</exception>
        /// <example>
        /// <code>
        /// // alpha -> [10, 30], beta -> [2]
        /// index.GetMedian();                       // ("alpha", 30)
        /// </code>
        /// </example>
        public KeyValuePair<TKey, TValue> GetMedian()
        {
            var total = _tree.ElementCount;
            if (total == 0)
            {
                throw new InvalidOperationException("The map is empty, so it has no median entry.");
            }

            return GetByRank((total - 1) / 2);
        }

        /// <summary>
        /// Gets the (key, value) pair holding the copy at the specified quantile of the expanded
        /// sequence, in O(log n). The nearest-rank definition is used, the one
        /// <see cref="OrderedMultiList{T}.GetQuantile(double)"/> already follows: the answer is the
        /// pair at zero-based position &#8968;<paramref name="quantile"/> &#215;
        /// <see cref="TotalValueCount"/>&#8969; - 1, clamped to the first copy when that lands below
        /// it. A quantile therefore never interpolates between two values and never names a value
        /// this map does not hold. <see cref="GetMedian"/> and a quantile of <c>0.5</c> agree.
        /// </summary>
        /// <param name="quantile">the point of the distribution, between 0 and 1 inclusive.</param>
        /// <returns>the pair holding that copy.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="quantile"/> is not between
        /// 0 and 1, or is not a number.</exception>
        /// <exception cref="InvalidOperationException">The map holds no values.</exception>
        /// <example>
        /// <code>
        /// // alpha -> [10, 30], beta -> [2]
        /// index.GetQuantile(0.99);                 // ("beta", 2)
        /// index.GetQuantile(0);                    // ("alpha", 10)
        /// </code>
        /// </example>
        public KeyValuePair<TKey, TValue> GetQuantile(double quantile)
        {
            if (double.IsNaN(quantile) || quantile < 0d || quantile > 1d)
            {
                throw new ArgumentOutOfRangeException(nameof(quantile), quantile, "The quantile must be between 0 and 1, inclusive.");
            }

            var total = _tree.ElementCount;
            if (total == 0)
            {
                throw new InvalidOperationException("The map is empty, so it has no quantile.");
            }

            var rank = (int)Math.Ceiling(quantile * total) - 1;
            return GetByRank(rank < 0 ? 0 : rank);
        }

        /// <summary>
        /// Returns a read-only <see cref="ILookup{TKey,TValue}"/> view of the map. The lookup
        /// enumerates its groupings in ascending key order, and each grouping enumerates its values
        /// in ascending order; a missing key yields an empty grouping.
        /// </summary>
        /// <returns>a live <see cref="ILookup{TKey,TValue}"/> view of the map.</returns>
        /// <example>
        /// <code>
        /// ILookup&lt;string, int&gt; lookup = index.AsLookup();
        /// foreach (var v in lookup["alpha"]) { }
        /// </code>
        /// </example>
        public ILookup<TKey, TValue> AsLookup()
        {
            return new LookupView(this);
        }

        /// <summary>
        /// Creates a shallow copy: value references are shared, key-value associations are
        /// independent. The copy keeps both comparers and the duplicate-value policy, and it
        /// enumerates in the same order.
        /// </summary>
        /// <returns>an independent copy of the map.</returns>
        /// <example>
        /// <code>
        /// OrderedMultiDictionary&lt;string, int&gt; copy = index.Clone();
        /// </code>
        /// </example>
        public OrderedMultiDictionary<TKey, TValue> Clone()
        {
            var clone = new OrderedMultiDictionary<TKey, TValue>(_keyComparer, _valueComparer, _allowDuplicateValues);
            var entries = _tree.GetAscendingEnumerator();
            while (entries.MoveNext())
            {
                var bucket = clone.NewBucket();
                var values = entries.Payload.GetAscendingEnumerator();
                while (values.MoveNext())
                {
                    bucket.Add(values.Key, values.Count);
                }

                clone._tree.AddCount(entries.Key, entries.Count, bucket);
            }

            return clone;
        }

        /// <summary>
        /// Returns a live read-only view of the map: lookups and enumeration reflect subsequent
        /// changes to the owning map, and enumeration keeps ascending key order and ascending value
        /// order. Mutating members are not exposed.
        /// </summary>
        /// <returns>a live read-only view of the map.</returns>
        /// <example>
        /// <code>
        /// IReadOnlyDictionary&lt;string, IReadOnlyCollection&lt;int&gt;&gt; view = index.AsReadOnly();
        /// </code>
        /// </example>
        public IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>> AsReadOnly()
        {
            return new ReadOnlyDictionaryView(this);
        }

        /// <summary>
        /// Exports the contents in <b>ascending key order</b>, each key paired with its values in
        /// ascending order.
        /// </summary>
        /// <returns>an ordered sequence of key/value-collection pairs.</returns>
        /// <remarks>
        /// This is the export path of an ordered map, and the reason the type has no
        /// <c>ToDictionary()</c>. A dictionary export would have to key its result with
        /// <see cref="EqualityComparer{TKey}.Default"/>, because an <see cref="IComparer{TKey}"/>
        /// supplies no hash codes — so a map built with, say, an <c>OrdinalIgnoreCase</c> key
        /// comparer would export a dictionary that answers differently from the map it came from.
        /// A sequence carries the ordering without pretending to carry the matching rule, exactly
        /// as <see cref="OrderedMultiList{T}.EntrySet"/> does for the ordered multiset.
        /// </remarks>
        /// <example>
        /// <code>
        /// foreach (var (key, values) in index.EntrySet())
        /// {
        ///     // ascending keys, ascending values within each key
        /// }
        /// </code>
        /// </example>
        public IEnumerable<(TKey Key, IReadOnlyCollection<TValue> Values)> EntrySet()
        {
            var entries = _tree.GetAscendingEnumerator();
            while (entries.MoveNext())
            {
                yield return (entries.Key, AsReadOnlyView(entries.Payload));
            }
        }

        /// <summary>
        /// Returns the contents in expanded per-key form, comma separated, with keys and values in
        /// ascending order, e.g. <c>k1:[v1,v2],k2:[v3]</c>.
        /// </summary>
        /// <returns>the text form of the map.</returns>
        /// <example>
        /// <code>
        /// string text = index.ToString();
        /// </code>
        /// </example>
        public override string ToString()
        {
            return string.Join(",", EntrySet().Select(entry =>
                $"{entry.Key}:[{string.Join(",", entry.Values)}]"));
        }

        /// <summary>
        /// Enumerates the map as flat (key, value) pairs — one entry per value — in ascending key
        /// order, and within one key in ascending value order.
        /// </summary>
        /// <returns>an enumerator over one (key, value) pair per stored value.</returns>
        /// <remarks>
        /// The single <c>yield</c> here is the enumerator of the whole map, not of a key: the body
        /// walks the tree's leaves and each bucket through struct enumerators, so enumerating a key
        /// allocates nothing. That is the F6-36 fix — the storage this type used before built two
        /// iterator objects per key, because the value side was reached through an interface.
        /// </remarks>
        /// <example>
        /// <code>
        /// foreach (var pair in index)
        /// {
        ///     // one pair per stored value, keys and values ascending
        /// }
        /// </code>
        /// </example>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            var entries = _tree.GetAscendingEnumerator();
            while (entries.MoveNext())
            {
                var values = entries.Payload.GetAscendingEnumerator();
                while (values.MoveNext())
                {
                    for (var copy = 0; copy < values.Count; copy++)
                    {
                        yield return new KeyValuePair<TKey, TValue>(entries.Key, values.Key);
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<KeyValuePair<TKey, IReadOnlyCollection<TValue>>>
            IEnumerable<KeyValuePair<TKey, IReadOnlyCollection<TValue>>>.GetEnumerator()
        {
            var entries = _tree.GetAscendingEnumerator();
            while (entries.MoveNext())
            {
                yield return new KeyValuePair<TKey, IReadOnlyCollection<TValue>>(
                    entries.Key, AsReadOnlyView(entries.Payload));
            }
        }

        IEnumerable<IReadOnlyCollection<TValue>> IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>>.Values
        {
            get
            {
                var entries = _tree.GetAscendingEnumerator();
                while (entries.MoveNext())
                {
                    yield return AsReadOnlyView(entries.Payload);
                }
            }
        }

        private static void ThrowIfNullKey(TKey key)
        {
            // Explicit, not delegated to the comparer: the contract of this package is that a null
            // key is rejected by the map itself, whatever a comparer would make of it.
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key), "The key can not be null.");
            }
        }

        /// <summary>
        /// Creates the bucket a new key stores its values in. Both duplicate policies share this one
        /// bucket type since F6-36: the policy is enforced by the operations, which either admit a
        /// second copy or check for the value first, so the value axis is one sorted multiset with
        /// the positional reads available on either setting.
        /// </summary>
        private OrderedMultiList<TValue> NewBucket()
        {
            return new OrderedMultiList<TValue>(_valueComparer);
        }

        /// <summary>
        /// Brings the tree's per-key value total back in step after the caller mutated the bucket.
        /// The two figures must never drift: the total is what the positional reads descend on, and
        /// it is also what <see cref="ValueCount(TKey)"/> and <see cref="TotalValueCount"/> report.
        /// A bucket that emptied takes its key with it, which is what keeps the "no key without
        /// values" invariant.
        /// </summary>
        private void SyncWeight(TKey key, int before, OrderedMultiList<TValue> bucket)
        {
            var after = bucket.TotalCount;
            if (after == before)
            {
                return;
            }

            if (after == 0)
            {
                _tree.TryRemoveKey(key);
                return;
            }

            _tree.AdjustCount(key, after - before);
        }

        private IReadOnlyCollection<TValue> AsReadOnlyView(OrderedMultiList<TValue> bucket)
        {
            // Wrapped, not cast (F6-24): the view has to reach consumers through the internal
            // wrapper on every target, so the net451/net461 generation sees the same type the
            // modern one does. The bucket is a concrete OrderedMultiList<TValue> since F6-36, but
            // the wrapper is what the pinning tests and the low-generation regression tests assert.
            return new ReadOnlyCollectionView<TValue>(bucket);
        }

        /// <summary>
        /// Returns the distinct values of a bucket in ascending order. Taken before the bucket is
        /// mutated, because the set operations walk this list while removing from the bucket.
        /// </summary>
        private static List<TValue> DistinctSnapshotOf(OrderedMultiList<TValue> bucket)
        {
            var result = new List<TValue>(bucket.DistinctCount);
            var enumerator = bucket.GetAscendingEnumerator();
            while (enumerator.MoveNext())
            {
                result.Add(enumerator.Key);
            }

            return result;
        }

        /// <summary>
        /// Builds a set of values that decides membership with <see cref="ValueComparer"/> rather
        /// than with <see cref="EqualityComparer{TValue}.Default"/>. Using a
        /// <see cref="SortedSet{T}"/> here is what keeps the set operations coherent with the
        /// ordering the map itself uses: a <see cref="HashSet{T}"/> would answer "different" for
        /// two values the map considers equal.
        /// </summary>
        private SortedSet<TValue> ToComparerAwareSet(IEnumerable<TValue> values)
        {
            var set = new SortedSet<TValue>(_valueComparer);
            foreach (var value in values)
            {
                set.Add(value);
            }

            return set;
        }

        /// <summary>
        /// Returns the distinct values of a sequence in ascending order, comparing them with
        /// <see cref="ValueComparer"/>. Whether <c>null</c> is acceptable is the comparer's
        /// decision, not this method's — under <see cref="System.Collections.Generic.Comparer{T}.Default"/>
        /// it is an ordinary smallest value.
        /// </summary>
        private List<TValue> DistinctValuesOf(IEnumerable<TValue> values)
        {
            var result = new List<TValue>();
            var seen = new SortedSet<TValue>(_valueComparer);

            foreach (var value in values)
            {
                if (seen.Add(value))
                {
                    result.Add(value);
                }
            }

            return result;
        }

        // ------------------------------------------------------------------
        // Diagnostics for the test suite
        // ------------------------------------------------------------------

        /// <summary>
        /// Gets the number of edges on a root-to-leaf path of the storage tree. Exposed for
        /// invariant assertions, which is where the O(log n) guarantee is checked; every leaf sits
        /// at this depth, so it costs one descent.
        /// </summary>
        internal int TreeHeight => _tree.Height;

        /// <summary>
        /// Re-checks every structural invariant the storage layer rests on: the B+ invariants of the
        /// key tree (uniform leaf depth, occupancy, separator agreement, cached totals), the
        /// agreement between each entry's cached value total and the length of the bucket it holds,
        /// the B+ invariants of every bucket, and that the buckets add up to the tree's own total.
        /// Exposed for the test suite, which asserts them instead of relying on timing measurements
        /// (F6-36, following F6-25 / R2-02).
        /// </summary>
        internal bool ValidateTree(out string? error)
        {
            if (!_tree.Validate(out error))
            {
                return false;
            }

            var total = 0;
            var entries = _tree.GetAscendingEnumerator();
            while (entries.MoveNext())
            {
                var bucket = entries.Payload;
                if (bucket == null)
                {
                    error = "an entry carries no value bucket.";
                    return false;
                }

                if (bucket.TotalCount != entries.Count)
                {
                    error = "the cached value total of an entry disagrees with the bucket it holds.";
                    return false;
                }

                if (!bucket.ValidateTree(out var bucketError))
                {
                    error = "a value bucket is invalid: " + bucketError;
                    return false;
                }

                total += bucket.TotalCount;
            }

            if (total != _tree.ElementCount)
            {
                error = "the value buckets do not add up to the recorded value total.";
                return false;
            }

            error = null;
            return true;
        }

        private sealed class ReadOnlyDictionaryView :
            IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>>
        {
            private readonly OrderedMultiDictionary<TKey, TValue> _owner;

            public ReadOnlyDictionaryView(OrderedMultiDictionary<TKey, TValue> owner)
            {
                _owner = owner;
            }

            public int Count => _owner.Count;

            public IEnumerable<TKey> Keys => _owner.Keys;

            public IEnumerable<IReadOnlyCollection<TValue>> Values
            {
                get
                {
                    var entries = _owner._tree.GetAscendingEnumerator();
                    while (entries.MoveNext())
                    {
                        yield return new ReadOnlyCollectionView<TValue>(entries.Payload);
                    }
                }
            }

            public IReadOnlyCollection<TValue> this[TKey key] => _owner[key];

            public bool ContainsKey(TKey key)
            {
                return _owner.ContainsKey(key);
            }

            public bool TryGetValue(TKey key, out IReadOnlyCollection<TValue> value)
            {
                return _owner.TryGetValue(key, out value);
            }

            public IEnumerator<KeyValuePair<TKey, IReadOnlyCollection<TValue>>> GetEnumerator()
            {
                var entries = _owner._tree.GetAscendingEnumerator();
                while (entries.MoveNext())
                {
                    yield return new KeyValuePair<TKey, IReadOnlyCollection<TValue>>(
                        entries.Key, new ReadOnlyCollectionView<TValue>(entries.Payload));
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private sealed class LookupView : ILookup<TKey, TValue>
        {
            private readonly OrderedMultiDictionary<TKey, TValue> _owner;

            public LookupView(OrderedMultiDictionary<TKey, TValue> owner)
            {
                _owner = owner;
            }

            public int Count => _owner.Count;

            public bool Contains(TKey key) => _owner.ContainsKey(key);

            public IEnumerable<TValue> this[TKey key]
            {
                get
                {
                    if (_owner._tree.TryGetEntry(key, out var bucket, out _))
                    {
                        return new GroupingView(key, bucket);
                    }

                    return new TValue[0];
                }
            }

            public IEnumerator<IGrouping<TKey, TValue>> GetEnumerator()
            {
                var entries = _owner._tree.GetAscendingEnumerator();
                while (entries.MoveNext())
                {
                    yield return new GroupingView(entries.Key, entries.Payload);
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private sealed class GroupingView : IGrouping<TKey, TValue>
        {
            private readonly IEnumerable<TValue> _values;

            public GroupingView(TKey key, IEnumerable<TValue> values)
            {
                Key = key;
                _values = values;
            }

            public TKey Key { get; }

            public IEnumerator<TValue> GetEnumerator()
            {
                return _values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
