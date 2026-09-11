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
    /// This is the ordered counterpart of <see cref="MultiDictionary{TKey,TValue}"/> and the
    /// equivalent of PowerCollections' <c>OrderedMultiDictionary&lt;TKey, TValue&gt;</c>. The two
    /// types share their per-key value-set semantics — the argument of every set operation is a
    /// <em>set</em> of values, a value may be stored several times under one key, and an inner
    /// collection is recycled the moment it empties so the map never holds a key without values —
    /// and differ in the storage underneath and in what that storage buys:
    /// <see cref="MultiDictionary{TKey,TValue}"/> keeps hash tables and answers in O(1) but has no
    /// defined order, while this type keeps a sorted dictionary over a red-black-tree-backed
    /// multiset and answers in O(log n) while enumerating keys and values in sorted order.
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
    /// <see cref="OrderedMultiList{T}"/> already follows on its element axis.
    /// </para>
    /// <para>
    /// <b>Ordering guarantees.</b> <see cref="Keys"/>, enumeration and <see cref="ToString"/> walk
    /// the keys in ascending order; <see cref="this[TKey]"/>, <see cref="Values"/>,
    /// <see cref="TryGetValue(TKey, out IReadOnlyCollection{TValue})"/> and
    /// <see cref="EntrySet"/> walk the values of every key in ascending order. Values of different
    /// keys are never interleaved by order — the keys decide the outer order and each key's own
    /// values decide the inner one.
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
    /// </code>
    /// </example>
    public class OrderedMultiDictionary<TKey, TValue> :
        IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>>,
        IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private static readonly IReadOnlyCollection<TValue> EmptyValues = new TValue[0];

        private readonly SortedDictionary<TKey, ICollection<TValue>> _map;
        private readonly IComparer<TKey> _keyComparer;
        private readonly IComparer<TValue> _valueComparer;
        private readonly Func<ICollection<TValue>> _innerFactory;

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
            : this(
                keyComparer ?? Comparer<TKey>.Default,
                valueComparer ?? Comparer<TValue>.Default,
                allowDuplicateValues
                    ? (Func<ICollection<TValue>>)(() => new OrderedMultiList<TValue>(valueComparer ?? Comparer<TValue>.Default))
                    : (Func<ICollection<TValue>>)(() => new SortedSet<TValue>(valueComparer ?? Comparer<TValue>.Default)))
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="OrderedMultiDictionary{TKey,TValue}"/> with fully
        /// resolved comparers and inner collection factory. Kept private on purpose: the factory
        /// is part of the ordering guarantee, so it must not be passable from outside —
        /// <see cref="Clone"/> is the only caller that supplies one.
        /// </summary>
        private OrderedMultiDictionary(IComparer<TKey> keyComparer, IComparer<TValue> valueComparer, Func<ICollection<TValue>> innerFactory)
        {
            _keyComparer = keyComparer;
            _valueComparer = valueComparer;
            _innerFactory = innerFactory;
            _map = new SortedDictionary<TKey, ICollection<TValue>>(keyComparer);
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
        public int Count => _map.Count;

        /// <summary>
        /// Gets the number of keys in the map (alias of <see cref="Count"/>).
        /// </summary>
        public int KeyCount => _map.Count;

        /// <summary>
        /// Gets the total number of values across all keys.
        /// </summary>
        public int TotalValueCount
        {
            get
            {
                var total = 0;
                foreach (var collection in _map.Values)
                {
                    total += collection.Count;
                }

                return total;
            }
        }

        /// <summary>
        /// Gets the number of values stored under the key, or <c>0</c> when the key is absent
        /// (matching the indexer, which yields an empty collection rather than throwing).
        /// </summary>
        /// <param name="key">the key to count the values of.</param>
        /// <returns>the number of values stored under <paramref name="key"/>.</returns>
        /// <remarks>
        /// Runs in O(log n) to locate the key plus O(1) to read its count. Use
        /// <see cref="ContainsKey(TKey)"/> when the absent-key case must be told apart from a key
        /// that is present with zero values — the latter can not occur, because an inner
        /// collection is dropped as soon as it empties.
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
            return _map.TryGetValue(key, out var collection) ? collection.Count : 0;
        }

        /// <summary>
        /// Gets the keys of the map, in ascending order.
        /// </summary>
        public IEnumerable<TKey> Keys => _map.Keys;

        /// <summary>
        /// Gets all values of the map, flattened across keys: the keys in ascending order, and the
        /// values of each key in ascending order.
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get
            {
                foreach (var collection in _map.Values)
                {
                    foreach (var value in collection)
                    {
                        yield return value;
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
                return _map.TryGetValue(key, out var collection) ? AsReadOnlyView(collection) : EmptyValues;
            }
        }

        /// <summary>
        /// Adds a (key, value) pair. When duplicate values are disallowed and the value already
        /// exists under the key, the call is silently ignored, exactly as in
        /// <see cref="MultiDictionary{TKey,TValue}.Add(TKey,TValue)"/>.
        /// </summary>
        /// <param name="key">the key to store the value under.</param>
        /// <param name="value">the value to store.</param>
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

            if (!_map.TryGetValue(key, out var collection))
            {
                collection = _innerFactory();
                _map.Add(key, collection);
            }

            collection.Add(value);
            if (collection.Count == 0)
            {
                // A deduplicating collection that refused the very first value would leave the
                // key without values; drop it to preserve the "no value-less key" invariant.
                _map.Remove(key);
            }
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
            return _map.ContainsKey(key);
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
            return _map.TryGetValue(key, out var collection) && collection.Contains(value);
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
            foreach (var collection in _map.Values)
            {
                if (collection.Contains(value))
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
            return _map.Remove(key);
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
            if (!_map.TryGetValue(key, out var collection))
            {
                return false;
            }

            if (!collection.Remove(value))
            {
                return false;
            }

            if (collection.Count == 0)
            {
                _map.Remove(key);
            }

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

            if (!_map.TryGetValue(key, out var collection))
            {
                return false;
            }

            // Materialized on purpose: the argument can be a live view over this very map's
            // values, and the loop below mutates the map.
            var removals = DistinctValuesOf(values);

            var removedAny = false;
            foreach (var value in removals)
            {
                if (collection.Remove(value))
                {
                    removedAny = true;
                }
            }

            if (collection.Count == 0)
            {
                _map.Remove(key);
            }

            return removedAny;
        }

        // ------------------------------------------------------------------
        // Per-key value set operations
        // ------------------------------------------------------------------

        /// <summary>
        /// Adds each distinct value of the specified collection under the key when not already
        /// present (set-union semantics on the key's values). Creates the key when absent. With a
        /// duplicating inner collection, existing duplicate values keep their multiplicities.
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

            var seen = new SortedSet<TValue>(_valueComparer);
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

            if (!_map.TryGetValue(key, out var collection))
            {
                return;
            }

            var keep = ToComparerAwareSet(values);
            var snapshot = SnapshotOf(collection);
            foreach (var value in snapshot)
            {
                if (!keep.Contains(value))
                {
                    collection.Remove(value);
                }
            }

            if (collection.Count == 0)
            {
                _map.Remove(key);
            }
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

            if (!_map.TryGetValue(key, out var collection))
            {
                return;
            }

            var removals = ToComparerAwareSet(values);
            var snapshot = SnapshotOf(collection);
            foreach (var value in snapshot)
            {
                if (removals.Contains(value))
                {
                    while (collection.Remove(value))
                    {
                    }
                }
            }

            if (collection.Count == 0)
            {
                _map.Remove(key);
            }
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
        /// count twice. With a deduplicating inner collection this is precisely the symmetric
        /// difference of <see cref="ISet{T}"/>; with a duplicating one, a value stored N times
        /// survives with N-1 copies.
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

            _map.TryGetValue(key, out var collection);

            foreach (var value in toggles)
            {
                if (collection == null || !collection.Remove(value))
                {
                    // Toggled on: either the key is absent or the value was not stored. Going
                    // through the public Add keeps the "no value-less key" invariant in a single
                    // place.
                    Add(key, value);
                }
            }

            if (collection != null && collection.Count == 0)
            {
                _map.Remove(key);
            }
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
            _map.Clear();
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
            if (_map.TryGetValue(key, out var collection))
            {
                value = AsReadOnlyView(collection);
                return true;
            }

            value = null!;
            return false;
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
            var clone = new OrderedMultiDictionary<TKey, TValue>(_keyComparer, _valueComparer, _innerFactory);
            foreach (var pair in _map)
            {
                var collection = clone._innerFactory();
                foreach (var value in pair.Value)
                {
                    collection.Add(value);
                }

                clone._map.Add(pair.Key, collection);
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
            foreach (var pair in _map)
            {
                yield return (pair.Key, AsReadOnlyView(pair.Value));
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
            return string.Join(",", _map.Select(pair =>
                $"{pair.Key}:[{string.Join(",", pair.Value)}]"));
        }

        /// <summary>
        /// Enumerates the map as flat (key, value) pairs — one entry per value — in ascending key
        /// order, and within one key in ascending value order.
        /// </summary>
        /// <returns>an enumerator over one (key, value) pair per stored value.</returns>
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
            foreach (var pair in _map)
            {
                foreach (var value in pair.Value)
                {
                    yield return new KeyValuePair<TKey, TValue>(pair.Key, value);
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
            foreach (var pair in _map)
            {
                yield return new KeyValuePair<TKey, IReadOnlyCollection<TValue>>(pair.Key, AsReadOnlyView(pair.Value));
            }
        }

        IEnumerable<IReadOnlyCollection<TValue>> IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>>.Values
        {
            get
            {
                foreach (var collection in _map.Values)
                {
                    yield return AsReadOnlyView(collection);
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

        private IReadOnlyCollection<TValue> AsReadOnlyView(ICollection<TValue> collection)
        {
            // OrderedMultiList<T> and SortedSet<T> (the two built-in inner collections) implement
            // IReadOnlyCollection<TValue>; the cast is a contract, not a conversion.
            return (IReadOnlyCollection<TValue>)collection;
        }

        /// <summary>
        /// Copies the values of an inner collection into a list before that collection is mutated.
        /// </summary>
        private static List<TValue> SnapshotOf(ICollection<TValue> collection)
        {
            return new List<TValue>(collection);
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

            public IEnumerable<IReadOnlyCollection<TValue>> Values =>
                _owner._map.Values.Select(AsView);

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
                foreach (var pair in _owner._map)
                {
                    yield return new KeyValuePair<TKey, IReadOnlyCollection<TValue>>(
                        pair.Key, AsView(pair.Value));
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private static IReadOnlyCollection<TValue> AsView(ICollection<TValue> collection)
            {
                return (IReadOnlyCollection<TValue>)collection;
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
                    if (_owner._map.TryGetValue(key, out var collection))
                    {
                        return new GroupingView(key, collection);
                    }

                    return new TValue[0];
                }
            }

            public IEnumerator<IGrouping<TKey, TValue>> GetEnumerator()
            {
                foreach (var pair in _owner._map)
                {
                    yield return new GroupingView(pair.Key, pair.Value);
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
