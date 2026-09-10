using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// CS8714: TKey is deliberately unconstrained (null key *values* are rejected at
// runtime, but the type parameter itself must stay nullable-friendly, e.g.
// TKey = string?); the "notnull" key constraint of the annotated
// Dictionary<TKey, TValue> (net5.0+ reference assemblies) is a false positive here.
#pragma warning disable CS8714

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Represents a multimap: a dictionary that allows multiple values to be associated with a
    /// single key. Adding, looking up and removing a single (key, value) pair runs in O(1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Key equality is determined by the <see cref="IEqualityComparer{TKey}"/> supplied at
    /// construction (default: <see cref="EqualityComparer{TKey}.Default"/>). Keys are never
    /// keyed by their hash code alone, so hash collisions between distinct keys can not
    /// corrupt the map. <c>null</c> keys are rejected with <see cref="ArgumentNullException"/>
    /// (standard <see cref="Dictionary{TKey,TValue}"/> behaviour); <c>null</c> values are
    /// allowed.
    /// </para>
    /// <para>
    /// The inner per-key collection is created by a configurable factory: by default a
    /// <see cref="List{TValue}"/> that allows duplicate values, or a <see cref="HashSet{TValue}"/>
    /// when <c>allowDuplicateValues</c> is <c>false</c>. An inner collection is dropped
    /// automatically once it becomes empty, so the map never holds value-less keys.
    /// </para>
    /// <para>
    /// This class is one of the three orthogonal "multi" types of this package:
    /// <see cref="MultiList{T}"/> multiplies <em>elements</em> (1 element &#8594; N copies),
    /// <see cref="MultiDictionary{TKey,TValue}"/> multiplies <em>values</em> per key
    /// (1 key &#8594; N values, this type) and <see cref="MultiKeyDictionary{TKey,TValue}"/>
    /// multiplies <em>key components</em> (N components &#8594; 1 value). Pick the type by asking
    /// what is allowed to repeat, never by name similarity.
    /// </para>
    /// <para>
    /// This class is not thread-safe. Wrap it with external synchronization for concurrent use.
    /// </para>
    /// </remarks>
    public class MultiDictionary<TKey, TValue> :
        IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>>,
        IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private static readonly IReadOnlyCollection<TValue> EmptyValues = new TValue[0];

        private readonly Dictionary<TKey, ICollection<TValue>> _dict;
        private readonly IEqualityComparer<TKey> _comparer;
        private readonly Func<ICollection<TValue>> _innerFactory;

        /// <summary>
        /// Initializes an empty <see cref="MultiDictionary{TKey,TValue}"/> that allows duplicate
        /// values per key (inner <see cref="List{TValue}"/>).
        /// </summary>
        public MultiDictionary() : this((IEqualityComparer<TKey>?)null, (Func<ICollection<TValue>>?)null)
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="MultiDictionary{TKey,TValue}"/> with the specified
        /// key comparer, allowing duplicate values per key.
        /// </summary>
        public MultiDictionary(IEqualityComparer<TKey>? comparer) : this(comparer, (Func<ICollection<TValue>>?)null)
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="MultiDictionary{TKey,TValue}"/>. When
        /// <paramref name="allowDuplicateValues"/> is <c>false</c>, duplicate values under the
        /// same key are silently ignored (inner <see cref="HashSet{TValue}"/>).
        /// </summary>
        public MultiDictionary(bool allowDuplicateValues) : this((IEqualityComparer<TKey>?)null, allowDuplicateValues)
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="MultiDictionary{TKey,TValue}"/> with the specified key
        /// comparer. When <paramref name="allowDuplicateValues"/> is <c>false</c>, duplicate
        /// values under the same key are silently ignored (inner <see cref="HashSet{TValue}"/>).
        /// </summary>
        public MultiDictionary(IEqualityComparer<TKey>? comparer, bool allowDuplicateValues)
            : this(comparer, allowDuplicateValues
                ? (Func<ICollection<TValue>>)(() => new List<TValue>())
                : (Func<ICollection<TValue>>)(() => new HashSet<TValue>()))
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="MultiDictionary{TKey,TValue}"/> with the specified key
        /// comparer and inner collection factory.
        /// </summary>
        public MultiDictionary(IEqualityComparer<TKey>? comparer, Func<ICollection<TValue>>? innerFactory)
        {
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
            _innerFactory = innerFactory ?? (() => new List<TValue>());
            _dict = new Dictionary<TKey, ICollection<TValue>>(_comparer);
        }

        /// <summary>
        /// Gets the comparer used to determine key equality.
        /// </summary>
        public IEqualityComparer<TKey> Comparer => _comparer;

        /// <summary>
        /// Gets the number of keys in the map.
        /// </summary>
        public int Count => _dict.Count;

        /// <summary>
        /// Gets the number of keys in the map (alias of <see cref="Count"/>).
        /// </summary>
        public int KeyCount => _dict.Count;

        /// <summary>
        /// Gets the total number of values across all keys.
        /// </summary>
        public int TotalValueCount
        {
            get
            {
                var total = 0;
                foreach (var collection in _dict.Values)
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
        /// <remarks>
        /// Runs in O(1): the count is read straight off the inner collection. Use
        /// <see cref="ContainsKey(TKey)"/> when the absent-key case must be told apart from a key
        /// that is present with zero values — the latter can not occur, because an inner
        /// collection is dropped as soon as it empties.
        /// </remarks>
        /// <example>
        /// <code>
        /// int n = map.ValueCount("orders"); // 2
        /// int missing = map.ValueCount("nope"); // 0
        /// </code>
        /// </example>
        public int ValueCount(TKey key)
        {
            return _dict.TryGetValue(key, out var collection) ? collection.Count : 0;
        }

        /// <summary>
        /// Gets the keys of the map.
        /// </summary>
        public IEnumerable<TKey> Keys => _dict.Keys;

        /// <summary>
        /// Gets all values of the map, flattened across keys.
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get
            {
                foreach (var collection in _dict.Values)
                {
                    foreach (var value in collection)
                    {
                        yield return value;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the values associated with the key. Returns an empty collection (never
        /// <c>null</c>) when the key is absent. The returned collection is a live view of the
        /// values stored for the key.
        /// </summary>
        public IReadOnlyCollection<TValue> this[TKey key] =>
            _dict.TryGetValue(key, out var collection) ? AsReadOnlyView(collection) : EmptyValues;

        /// <summary>
        /// Adds a (key, value) pair. When duplicate values are disallowed and the value already
        /// exists under the key, the call is silently ignored.
        /// </summary>
        /// <example>
        /// <code>
        /// map.Add("orders", 1001);
        /// map.Add("orders", 1002);
        /// </code>
        /// </example>
        public void Add(TKey key, TValue value)
        {
            if (!_dict.TryGetValue(key, out var collection))
            {
                collection = _innerFactory();
                _dict.Add(key, collection);
            }

            var before = collection.Count;
            collection.Add(value);
            if (before == 0 && collection.Count == 0)
            {
                // The (deduplicating) factory rejected the value into a fresh, still-empty
                // collection: drop the key to preserve the "no empty inner collections" invariant.
                _dict.Remove(key);
            }
        }

        /// <summary>
        /// Adds each value of the specified collection under the key.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is <c>null</c>.</exception>
        public void AddRange(TKey key, IEnumerable<TValue> values)
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

        /// <summary>
        /// Determines whether the map contains the key.
        /// </summary>
        /// <example>
        /// <code>
        /// bool has = map.ContainsKey("orders");
        /// </code>
        /// </example>
        public bool ContainsKey(TKey key)
        {
            return _dict.ContainsKey(key);
        }

        /// <summary>
        /// Determines whether the specified value exists under the key.
        /// </summary>
        /// <example>
        /// <code>
        /// bool has = map.Contains("orders", 1001);
        /// </code>
        /// </example>
        public bool Contains(TKey key, TValue value)
        {
            return _dict.TryGetValue(key, out var collection) && collection.Contains(value);
        }

        /// <summary>
        /// Determines whether the specified value exists under any key.
        /// </summary>
        /// <example>
        /// <code>
        /// bool has = map.ContainsValue(1001);
        /// </code>
        /// </example>
        public bool ContainsValue(TValue value)
        {
            foreach (var collection in _dict.Values)
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
        /// <example>
        /// <code>
        /// bool removed = map.Remove("orders");
        /// // removes the key with all its values
        /// </code>
        /// </example>
        public bool Remove(TKey key)
        {
            return _dict.Remove(key);
        }

        /// <summary>
        /// Removes a single occurrence of the value under the key. Returns <c>true</c> when a
        /// value was removed. The key is dropped automatically once its last value is removed.
        /// </summary>
        /// <example>
        /// <code>
        /// bool removed = map.Remove("orders", 1001);
        /// </code>
        /// </example>
        public bool Remove(TKey key, TValue value)
        {
            if (!_dict.TryGetValue(key, out var collection))
            {
                return false;
            }

            if (!collection.Remove(value))
            {
                return false;
            }

            if (collection.Count == 0)
            {
                _dict.Remove(key);
            }

            return true;
        }

        /// <summary>
        /// Removes one occurrence of each distinct value of the specified collection from the key.
        /// Returns <c>true</c> when at least one value was removed. The key is dropped
        /// automatically once its last value is removed; a missing key is a no-op that returns
        /// <c>false</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is the batch form of <see cref="Remove(TKey, TValue)"/> and behaves exactly like
        /// calling it once per <em>distinct</em> element of <paramref name="values"/>: with a
        /// duplicating inner collection (<c>allowDuplicateValues: true</c>), a value stored N times
        /// still has N-1 copies left, because one occurrence is removed per distinct argument
        /// value. Use <see cref="ExceptWith(TKey, IEnumerable{TValue})"/> when every occurrence
        /// must go.
        /// </para>
        /// <para>
        /// The argument is a <em>set</em> of values, exactly as in
        /// <see cref="UnionWith(TKey, IEnumerable{TValue})"/>,
        /// <see cref="IntersectionWith(TKey, IEnumerable{TValue})"/>,
        /// <see cref="ExceptWith(TKey, IEnumerable{TValue})"/> and
        /// <see cref="SymmetricExceptWith(TKey, IEnumerable{TValue})"/>: a repeated value in it
        /// does not count twice, and a <c>null</c> element is an ordinary value. An empty
        /// collection (or one holding only values the key does not store) is a no-op that leaves
        /// the key in place.
        /// </para>
        /// <para>
        /// This is deliberately named <c>RemoveRange</c> rather than being an overload
        /// <c>Remove(TKey, IEnumerable{TValue})</c>, mirroring <see cref="AddRange(TKey, IEnumerable{TValue})"/>.
        /// The overload would be a source-breaking change: <c>map.Remove(key, null)</c> — the
        /// documented way of removing a stored <c>null</c> value — would become ambiguous
        /// (CS0121) because <c>null</c> converts to both <c>TValue</c> and
        /// <c>IEnumerable&lt;TValue&gt;</c> whenever <c>TValue</c> is a reference type.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// map.AddRange("orders", new[] { 1001, 1002, 1003 });
        ///
        /// bool removed = map.RemoveRange("orders", new[] { 1002, 1003 });
        /// // orders -> [1001]; removed == true
        ///
        /// bool noOp = map.RemoveRange("orders", new[] { 9999 });
        /// // nothing stored matches; noOp == false, orders -> [1001]
        /// </code>
        /// </example>
        public bool RemoveRange(TKey key, IEnumerable<TValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (!_dict.TryGetValue(key, out var collection))
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
                _dict.Remove(key);
            }

            return removedAny;
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
        /// map.UnionWith("orders", new[] { 1003, 1004 });
        /// </code>
        /// </example>
        public void UnionWith(TKey key, IEnumerable<TValue> values)
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
        /// map.IntersectionWith("orders", new[] { 1001 });
        /// </code>
        /// </example>
        public void IntersectionWith(TKey key, IEnumerable<TValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (!_dict.TryGetValue(key, out var collection))
            {
                return;
            }

            var keep = new HashSet<TValue>(values);
            var snapshot = new List<TValue>(collection);
            foreach (var value in snapshot)
            {
                if (!keep.Contains(value))
                {
                    collection.Remove(value);
                }
            }

            if (collection.Count == 0)
            {
                _dict.Remove(key);
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
        /// map.ExceptWith("orders", new[] { 1001 });
        /// </code>
        /// </example>
        public void ExceptWith(TKey key, IEnumerable<TValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (!_dict.TryGetValue(key, out var collection))
            {
                return;
            }

            var removals = new HashSet<TValue>(values);
            var snapshot = new List<TValue>(collection);
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
                _dict.Remove(key);
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
        /// <para>
        /// The argument is a <em>set</em> of values, exactly as in
        /// <see cref="UnionWith(TKey, IEnumerable{TValue})"/>,
        /// <see cref="IntersectionWith(TKey, IEnumerable{TValue})"/> and
        /// <see cref="ExceptWith(TKey, IEnumerable{TValue})"/>: a repeated value in it does not
        /// count twice. With a deduplicating inner collection (<c>allowDuplicateValues: false</c>)
        /// this is precisely the symmetric difference of <see cref="ISet{T}"/>. With a
        /// duplicating inner collection a value stored N times survives with N-1 copies, because
        /// one occurrence is cancelled per distinct argument value.
        /// </para>
        /// <para>
        /// This deliberately differs from
        /// <see cref="MultiList{T}.SymmetricExceptWith(IEnumerable{T})"/>, which treats its
        /// argument as a <em>multiset</em> and keeps the absolute count difference of the two
        /// sides. Every per-key operation of this class treats its argument as a set, and keeping
        /// that convention coherent within one type was judged more important than mirroring
        /// <see cref="MultiList{T}"/>'s multiplicity handling.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// map.Add("orders", 1001);
        /// map.Add("orders", 1001);
        /// map.Add("orders", 1002);
        ///
        /// map.SymmetricExceptWith("orders", new[] { 1001, 1003 });
        /// // 1001 is stored       -> one of its two copies is cancelled
        /// // 1002 is not listed   -> untouched
        /// // 1003 is not stored   -> added
        /// // result: orders -> [1001, 1002, 1003]
        /// </code>
        /// </example>
        public void SymmetricExceptWith(TKey key, IEnumerable<TValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            // Materialized on purpose: the argument can be a live view over this very map's
            // values, and the loop below mutates the map.
            var toggles = DistinctValuesOf(values);

            _dict.TryGetValue(key, out var collection);

            foreach (var value in toggles)
            {
                if (collection == null || !collection.Remove(value))
                {
                    // Toggled on: either the key is absent or the value was not stored. Going
                    // through the public Add keeps the "no empty inner collection" invariant in
                    // a single place.
                    Add(key, value);
                }
            }

            if (collection != null && collection.Count == 0)
            {
                _dict.Remove(key);
            }
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
            _dict.Clear();
        }

        /// <summary>
        /// Gets the values associated with the key. When the key is absent, returns
        /// <c>false</c> and <paramref name="value"/> is <c>null</c> (check the return value
        /// before use).
        /// </summary>
        /// <example>
        /// <code>
        /// if (map.TryGetValue("orders", out var values))
        /// {
        ///     foreach (var v in values) { }
        /// }
        /// </code>
        /// </example>
        public bool TryGetValue(TKey key, out IReadOnlyCollection<TValue> value)
        {
            if (_dict.TryGetValue(key, out var collection))
            {
                value = AsReadOnlyView(collection);
                return true;
            }

            value = null!;
            return false;
        }

        /// <summary>
        /// Returns a read-only <see cref="ILookup{TKey,TValue}"/> view of the map
        /// (a missing key yields an empty grouping).
        /// </summary>
        /// <example>
        /// <code>
        /// ILookup&lt;string, int&gt; lookup = map.AsLookup();
        /// foreach (var v in lookup["orders"]) { }
        /// </code>
        /// </example>
        public ILookup<TKey, TValue> AsLookup()
        {
            return new LookupView(this);
        }

        /// <summary>
        /// Creates a shallow copy: value references are shared, key-value associations are
        /// independent.
        /// </summary>
        /// <example>
        /// <code>
        /// MultiDictionary&lt;string, int&gt; copy = map.Clone();
        /// </code>
        /// </example>
        public MultiDictionary<TKey, TValue> Clone()
        {
            var clone = new MultiDictionary<TKey, TValue>(_comparer, _innerFactory);
            foreach (var pair in _dict)
            {
                var collection = clone._innerFactory();
                foreach (var value in pair.Value)
                {
                    collection.Add(value);
                }

                clone._dict.Add(pair.Key, collection);
            }

            return clone;
        }

        /// <summary>
        /// Exports the map as a snapshot dictionary from key to its value collection. The
        /// outer dictionary is independent of the map; the inner collections are shared
        /// (live views of the values stored for each key).
        /// </summary>
        /// <example>
        /// <code>
        /// IReadOnlyDictionary&lt;string, IReadOnlyCollection&lt;int&gt;&gt; snapshot = map.ToDictionary();
        /// </code>
        /// </example>
        public IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>> ToDictionary()
        {
            var dictionary = new Dictionary<TKey, IReadOnlyCollection<TValue>>(_comparer);
            foreach (var pair in _dict)
            {
                dictionary.Add(pair.Key, AsReadOnlyView(pair.Value));
            }

            return dictionary;
        }

        /// <summary>
        /// Returns a live read-only view of the map: lookups and enumeration reflect subsequent
        /// changes to the owning map. Mutating members are not exposed.
        /// </summary>
        /// <example>
        /// <code>
        /// IReadOnlyDictionary&lt;string, IReadOnlyCollection&lt;int&gt;&gt; view = map.AsReadOnly();
        /// </code>
        /// </example>
        public IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>> AsReadOnly()
        {
            return new ReadOnlyDictionaryView(this);
        }

        /// <summary>
        /// Returns the contents in expanded per-key form, comma separated,
        /// e.g. <c>k1:[v1,v2],k2:[v3]</c>.
        /// </summary>
        /// <example>
        /// <code>
        /// string text = map.ToString();
        /// </code>
        /// </example>
        public override string ToString()
        {
            return string.Join(",", _dict.Select(pair =>
                $"{pair.Key}:[{string.Join(",", pair.Value)}]"));
        }

        /// <summary>
        /// Enumerates the map as flat (key, value) pairs — one entry per value.
        /// </summary>
        /// <example>
        /// <code>
        /// foreach (var pair in map)
        /// {
        ///     // one pair per stored value
        /// }
        /// </code>
        /// </example>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var pair in _dict)
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
            foreach (var pair in _dict)
            {
                yield return new KeyValuePair<TKey, IReadOnlyCollection<TValue>>(pair.Key, AsReadOnlyView(pair.Value));
            }
        }

        IEnumerable<IReadOnlyCollection<TValue>> IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>>.Values
        {
            get
            {
                foreach (var collection in _dict.Values)
                {
                    yield return AsReadOnlyView(collection);
                }
            }
        }

        private IReadOnlyCollection<TValue> AsReadOnlyView(ICollection<TValue> collection)
        {
            // List<T> / HashSet<T> (the built-in factories) and any well-behaved custom factory
            // implement IReadOnlyCollection<T>; the cast is a contract, not a conversion.
            return (IReadOnlyCollection<TValue>)collection;
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

        private sealed class ReadOnlyDictionaryView :
            IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>>
        {
            private readonly MultiDictionary<TKey, TValue> _owner;

            public ReadOnlyDictionaryView(MultiDictionary<TKey, TValue> owner)
            {
                _owner = owner;
            }

            public int Count => _owner.Count;

            public IEnumerable<TKey> Keys => _owner.Keys;

            public IEnumerable<IReadOnlyCollection<TValue>> Values =>
                _owner._dict.Values.Select(AsView);

            public IReadOnlyCollection<TValue> this[TKey key] => _owner[key];

            public bool ContainsKey(TKey key)
            {
                return _owner.ContainsKey(key);
            }

            public bool TryGetValue(TKey key, out IReadOnlyCollection<TValue> value)
            {
                if (_owner.ContainsKey(key))
                {
                    value = _owner[key];
                    return true;
                }

                value = null!;
                return false;
            }

            public IEnumerator<KeyValuePair<TKey, IReadOnlyCollection<TValue>>> GetEnumerator()
            {
                foreach (var pair in _owner._dict)
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
            private readonly MultiDictionary<TKey, TValue> _owner;

            public LookupView(MultiDictionary<TKey, TValue> owner)
            {
                _owner = owner;
            }

            public int Count => _owner.Count;

            public bool Contains(TKey key) => _owner.ContainsKey(key);

            public IEnumerable<TValue> this[TKey key]
            {
                get
                {
                    if (_owner._dict.TryGetValue(key, out var collection))
                    {
                        return new GroupingView(key, collection);
                    }

                    return new TValue[0];
                }
            }

            public IEnumerator<IGrouping<TKey, TValue>> GetEnumerator()
            {
                foreach (var pair in _owner._dict)
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
