using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// CS8714: V is deliberately unconstrained (a null value from the source map is a legitimate
// entry of this index and lives in a dedicated bucket, so the type parameter itself must stay
// nullable-friendly, e.g. V = string?); the "notnull" key constraint of the annotated
// Dictionary<TKey, TValue> (net5.0+ reference assemblies) is a false positive here.
#pragma warning disable CS8714

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Represents the inverse of a <see cref="MultiDictionary{TKey,TValue}"/>: a snapshot that
    /// maps every stored value to the set of keys that hold it, so "which keys store this value?"
    /// is answered in O(1). Adding, looking up and removing a single (value, key) binding runs
    /// in O(1).
    /// </summary>
    /// <typeparam name="V">the value axis of the inverted relation - the entries of this index.
    /// May be <c>null</c> (dedicated bucket), exactly as a <see cref="MultiDictionary{TKey,TValue}"/>
    /// accepts <c>null</c> values.</typeparam>
    /// <typeparam name="K">the key axis of the inverted relation - the keys stored under each
    /// value. Must not be <c>null</c>, exactly as a <see cref="MultiDictionary{TKey,TValue}"/>
    /// rejects <c>null</c> keys.</typeparam>
    /// <remarks>
    /// <para>
    /// This is the <b>snapshot</b> half of the two inversion entry points (R3-02): the
    /// constructors copy the bindings out of the source map into a fully self-contained instance.
    /// No reference chain is kept, so the snapshot stays exactly as it was built while the source
    /// map can change or be dropped freely afterwards - which also makes it safe to serialize.
    /// For a read-only view that keeps answering from the live map instead, use
    /// <see cref="MultiDictionary{TKey,TValue}.AsReverse()"/>; the two agree at any point in time.
    /// The instance is also a standalone mutable collection of its own: entries can be added,
    /// removed and cleared after construction, and the "no empty inner collection" invariant of
    /// <see cref="MultiDictionary{TKey,TValue}"/> is mirrored - a value disappears from the index
    /// automatically once its last key is removed.
    /// </para>
    /// <para>
    /// Per value the keys form a set: a key that stores one value several times (a duplicating
    /// source map) is listed once, so the inversion collapses multiplicities. Value equality on
    /// the indexed axis is <see cref="EqualityComparer{V}.Default"/> - the same notion the source
    /// map's own backwards index uses, which is what keeps a snapshot and the live view
    /// interchangeable. Key equality on the stored axis is the
    /// <see cref="IEqualityComparer{K}"/> supplied at construction (default:
    /// <see cref="EqualityComparer{K}.Default"/>); building from a source map defaults to the
    /// source's own key comparer, so a snapshot looks its keys up exactly like the source does.
    /// </para>
    /// <para>
    /// The member names mirror <see cref="MultiDictionary{TKey,TValue}"/> with the axes swapped:
    /// <see cref="Count"/> (alias <see cref="ValueCount"/>) counts the distinct values where
    /// <c>MultiDictionary.Count</c> counts keys, <see cref="TotalKeyCount"/> is the mirror of
    /// <c>TotalValueCount</c>, <see cref="KeyCount(V)"/> is the mirror of
    /// <c>ValueCount(key)</c>, <see cref="ContainsValue(V)"/> is the O(1) presence check (the
    /// mirror of <c>ContainsKey</c>) and <see cref="ContainsKey(K)"/> scans the stored keys (the
    /// mirror of <c>ContainsValue</c>). A missing value yields an empty collection rather than an
    /// exception, matching the <c>MultiDictionary</c> indexer.
    /// </para>
    /// <para>
    /// This class is not thread-safe. Wrap it with external synchronization for concurrent use.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var map = new MultiDictionary&lt;string, int&gt;();
    /// map.Add("orders", 1001);
    /// map.Add("customers", 1001);
    /// map.Add("customers", 1002);
    ///
    /// var inverted = new ReverseMultiDictionary&lt;int, string&gt;(map);
    /// inverted[1001];              // ["orders", "customers"] - who stores 1001?
    /// inverted[1002];              // ["customers"]
    /// inverted.ValueCount;         // 2 distinct values
    /// inverted.TotalKeyCount;      // 3 distinct (value, key) bindings
    /// inverted.ContainsKey(1001);  // true - some value stores key "customers"
    /// </code>
    /// </example>
    public class ReverseMultiDictionary<V, K> :
        IReadOnlyDictionary<V, IReadOnlyCollection<K>>,
        IEnumerable<KeyValuePair<V, K>>
    {
        private static readonly IReadOnlyCollection<K> EmptyKeys = new K[0];

        private readonly Dictionary<V, KeySet> _index;
        private readonly IEqualityComparer<K> _comparer;

        /// <summary>
        /// The <c>null</c>-value bucket, held outside <see cref="_index"/> because
        /// <see cref="Dictionary{TKey,TValue}"/> rejects a <c>null</c> key while the inverted
        /// relation accepts a <c>null</c> value. No allocation is paid until a <c>null</c> value
        /// is actually stored.
        /// </summary>
        private KeySet? _nullValueKeys;

        /// <summary>
        /// Cached total number of (value, key) bindings across all values, kept in step by every
        /// mutation instead of being recomputed on each read.
        /// </summary>
        private int _totalKeyCount;

        /// <summary>
        /// Initializes an empty <see cref="ReverseMultiDictionary{V,K}"/> that can be filled by
        /// hand through <see cref="Add(V,K)"/> / <see cref="AddRange(V,IEnumerable{K})"/>.
        /// </summary>
        public ReverseMultiDictionary() : this((IEqualityComparer<K>?)null)
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="ReverseMultiDictionary{V,K}"/> with the specified
        /// comparer for the stored keys.
        /// </summary>
        /// <param name="comparer">the key comparer; <c>null</c> selects
        /// <see cref="EqualityComparer{K}.Default"/>.</param>
        public ReverseMultiDictionary(IEqualityComparer<K>? comparer)
        {
            _comparer = comparer ?? EqualityComparer<K>.Default;
            _index = new Dictionary<V, KeySet>();
        }

        /// <summary>
        /// Initializes a self-contained snapshot of the source map's inverted relation: value
        /// &#8594; the set of keys that store it.
        /// </summary>
        /// <param name="source">the map to invert. The bindings are copied out; afterwards the
        /// two evolve independently.</param>
        /// <remarks>
        /// The snapshot's key comparer defaults to the source's own key comparer, so the
        /// snapshot looks its keys up exactly like the source map does. Value equality on the
        /// indexed axis is <see cref="EqualityComparer{V}.Default"/>, the notion the source's
        /// backwards index uses.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
        public ReverseMultiDictionary(MultiDictionary<K, V> source) : this(source, null)
        {
        }

        /// <summary>
        /// Initializes a self-contained snapshot of the source map's inverted relation: value
        /// &#8594; the set of keys that store it.
        /// </summary>
        /// <param name="source">the map to invert. The bindings are copied out; afterwards the
        /// two evolve independently.</param>
        /// <param name="comparer">the key comparer of the snapshot; <c>null</c> selects the
        /// source's own key comparer.</param>
        /// <remarks>
        /// A key that stores one value several times (a duplicating source map) is listed once:
        /// the inversion collapses multiplicities. A stored <c>null</c> value of the source
        /// surfaces as a <c>null</c> entry of the snapshot.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
        public ReverseMultiDictionary(MultiDictionary<K, V> source, IEqualityComparer<K>? comparer)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _comparer = comparer ?? source.Comparer;
            _index = new Dictionary<V, KeySet>();

            foreach (var pair in source)
            {
                Add(pair.Value, pair.Key);
            }
        }

        /// <summary>
        /// Gets the comparer used to determine equality of the stored keys.
        /// </summary>
        public IEqualityComparer<K> Comparer => _comparer;

        /// <summary>
        /// Gets the number of distinct values in the index.
        /// </summary>
        public int Count => _index.Count + (_nullValueKeys != null ? 1 : 0);

        /// <summary>
        /// Gets the number of distinct values in the index (alias of <see cref="Count"/>).
        /// </summary>
        /// <remarks>
        /// The mirror of <c>MultiDictionary.KeyCount</c>: with the axes swapped, the argless
        /// alias counts the indexed (value) axis and the per-entry counter
        /// <see cref="KeyCount(V)"/> counts the stored (key) axis.
        /// </remarks>
        public int ValueCount => Count;

        /// <summary>
        /// Gets the total number of distinct (value, key) bindings across all values.
        /// </summary>
        /// <remarks>
        /// Runs in O(1): the total is a cached count that every mutation maintains. This is the
        /// mirror of <c>MultiDictionary.TotalValueCount</c>; note that it counts distinct
        /// memberships, so a source key that stores one value several times contributes once.
        /// </remarks>
        public int TotalKeyCount => _totalKeyCount;

        /// <summary>
        /// Gets the number of keys stored under the value, or <c>0</c> when the value is absent
        /// (matching the indexer, which yields an empty collection rather than throwing).
        /// </summary>
        /// <example>
        /// <code>
        /// int n = inverted.KeyCount(1001); // 2 - two keys store 1001
        /// int missing = inverted.KeyCount(9999); // 0
        /// </code>
        /// </example>
        public int KeyCount(V value)
        {
            if (value == null)
            {
                return _nullValueKeys?.Count ?? 0;
            }

            return _index.TryGetValue(value, out var keys) ? keys.Count : 0;
        }

        /// <summary>
        /// Gets the distinct values of the index (the entries of the inverted relation). A stored
        /// <c>null</c> value enumerates last.
        /// </summary>
        public IEnumerable<V> Values
        {
            get
            {
                foreach (var value in _index.Keys)
                {
                    yield return value;
                }

                if (_nullValueKeys != null)
                {
                    yield return default!;
                }
            }
        }

        /// <summary>
        /// Gets all keys of the index, flattened across values - one entry per (value, key)
        /// binding.
        /// </summary>
        public IEnumerable<K> Keys
        {
            get
            {
                foreach (var pair in _index)
                {
                    foreach (var key in pair.Value)
                    {
                        yield return key;
                    }
                }

                if (_nullValueKeys != null)
                {
                    foreach (var key in _nullValueKeys)
                    {
                        yield return key;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the keys stored under the value. Returns an empty collection (never
        /// <c>null</c>) when the value is absent. The returned collection is a live view of the
        /// keys stored for the value.
        /// </summary>
        public IReadOnlyCollection<K> this[V value]
        {
            get
            {
                if (value == null)
                {
                    return _nullValueKeys ?? EmptyKeys;
                }

                return _index.TryGetValue(value, out var keys) ? keys : EmptyKeys;
            }
        }

        /// <summary>
        /// Adds a (value, key) binding. A binding that already exists is silently ignored (the
        /// stored keys form a set, so multiplicities collapse).
        /// </summary>
        /// <remarks>
        /// A <c>null</c> value is stored in a dedicated bucket, since a
        /// <see cref="Dictionary{TKey,TValue}"/> can not key on <c>null</c>. A <c>null</c> key is
        /// rejected - the exact mirror of the source map, which allows <c>null</c> values but
        /// rejects <c>null</c> keys.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// inverted.Add(1001, "orders");
        /// inverted.Add(1001, "customers");
        /// </code>
        /// </example>
        public void Add(V value, K key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                var bucket = _nullValueKeys ?? (_nullValueKeys = new KeySet(_comparer));
                if (bucket.Add(key))
                {
                    _totalKeyCount++;
                }

                return;
            }

            if (!_index.TryGetValue(value, out var keys))
            {
                keys = new KeySet(_comparer);
                _index.Add(value, keys);
            }

            if (keys.Add(key))
            {
                _totalKeyCount++;
            }
        }

        /// <summary>
        /// Adds each key of the specified collection under the value.
        /// </summary>
        /// <remarks>
        /// A <c>null</c> element in the collection is rejected like any other <c>null</c> key.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> is <c>null</c>.</exception>
        public void AddRange(V value, IEnumerable<K> keys)
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            foreach (var key in keys)
            {
                Add(value, key);
            }
        }

        /// <summary>
        /// Determines whether the value is present in the index, i.e. whether any key stores it.
        /// </summary>
        /// <remarks>
        /// Runs in O(1). This is the O(1) presence check of the indexed (value) axis - the exact
        /// mirror of <c>MultiDictionary.ContainsKey</c>; the membership scan of the stored (key)
        /// axis is <see cref="ContainsKey(K)"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// bool has = inverted.ContainsValue(1001);
        /// </code>
        /// </example>
        public bool ContainsValue(V value)
        {
            return value == null ? _nullValueKeys != null : _index.ContainsKey(value);
        }

        /// <summary>
        /// Determines whether any value stores the key.
        /// </summary>
        /// <remarks>
        /// Runs in O(entries) - a scan over every stored key set, exactly what the mirror member
        /// <c>MultiDictionary.ContainsValue</c> would cost without its backwards index. The O(1)
        /// presence check of the indexed (value) axis is <see cref="ContainsValue(V)"/>.
        /// </remarks>
        public bool ContainsKey(K key)
        {
            if (key == null)
            {
                return false;
            }

            if (_nullValueKeys != null && _nullValueKeys.Contains(key))
            {
                return true;
            }

            foreach (var pair in _index)
            {
                if (pair.Value.Contains(key))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether the (value, key) binding exists.
        /// </summary>
        /// <remarks>
        /// Runs in O(1). A <c>null</c> value consults the dedicated bucket; the binding can
        /// never hold for a <c>null</c> key, since <see cref="Add(V,K)"/> rejects one.
        /// </remarks>
        public bool Contains(V value, K key)
        {
            if (value == null)
            {
                return _nullValueKeys != null && _nullValueKeys.Contains(key);
            }

            return _index.TryGetValue(value, out var keys) && keys.Contains(key);
        }

        /// <summary>
        /// Removes the value together with all of the keys stored under it. Returns <c>true</c>
        /// when the value was present.
        /// </summary>
        /// <example>
        /// <code>
        /// bool removed = inverted.Remove(1001);
        /// // removes the value with all its keys
        /// </code>
        /// </example>
        public bool Remove(V value)
        {
            if (value == null)
            {
                if (_nullValueKeys == null)
                {
                    return false;
                }

                _totalKeyCount -= _nullValueKeys.Count;
                _nullValueKeys = null;
                return true;
            }

            if (!_index.TryGetValue(value, out var keys))
            {
                return false;
            }

            _totalKeyCount -= keys.Count;
            return _index.Remove(value);
        }

        /// <summary>
        /// Removes a single (value, key) binding. Returns <c>true</c> when the binding existed.
        /// The value is dropped automatically once its last key is removed - the mirror of the
        /// source map's "no value-less key" invariant.
        /// </summary>
        /// <example>
        /// <code>
        /// bool removed = inverted.Remove(1001, "orders");
        /// </code>
        /// </example>
        public bool Remove(V value, K key)
        {
            if (value == null)
            {
                if (_nullValueKeys == null || !_nullValueKeys.Remove(key))
                {
                    return false;
                }

                _totalKeyCount--;
                if (_nullValueKeys.Count == 0)
                {
                    _nullValueKeys = null;
                }

                return true;
            }

            if (!_index.TryGetValue(value, out var keys) || !keys.Remove(key))
            {
                return false;
            }

            _totalKeyCount--;
            if (keys.Count == 0)
            {
                // The mirror of the source map's invariant: an inner collection is dropped as
                // soon as it empties, so the index never holds a value without keys.
                _index.Remove(value);
            }

            return true;
        }

        /// <summary>
        /// Removes each key of the specified collection from the value. Returns <c>true</c> when
        /// at least one binding was removed; a missing value is a no-op that returns
        /// <c>false</c>.
        /// </summary>
        /// <remarks>
        /// The stored keys form a set, so each listed key either is removed or was not present -
        /// a repeated element in the argument changes nothing. A <c>null</c> element can never
        /// match, since <see cref="Add(V,K)"/> rejects <c>null</c> keys. The argument is
        /// materialized before the removals start, so it may be a live view of this very index.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> is <c>null</c>.</exception>
        public bool RemoveRange(V value, IEnumerable<K> keys)
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            // Materialized on purpose: the argument can be a live view over this very index's
            // keys, and the loop below mutates the index.
            var removals = new List<K>(keys);

            var removedAny = false;
            foreach (var key in removals)
            {
                if (Remove(value, key))
                {
                    removedAny = true;
                }
            }

            return removedAny;
        }

        /// <summary>
        /// Removes all values and bindings.
        /// </summary>
        public void Clear()
        {
            _index.Clear();
            _nullValueKeys = null;
            _totalKeyCount = 0;
        }

        /// <summary>
        /// Gets the keys stored under the value. When the value is absent, returns
        /// <c>false</c> and <paramref name="keys"/> is <c>null</c> (check the return value
        /// before use).
        /// </summary>
        public bool TryGetValue(V value, out IReadOnlyCollection<K> keys)
        {
            if (ContainsValue(value))
            {
                keys = this[value];
                return true;
            }

            keys = null!;
            return false;
        }

        /// <summary>
        /// Creates a shallow copy: key references are shared, (value, key) associations are
        /// independent.
        /// </summary>
        public ReverseMultiDictionary<V, K> Clone()
        {
            // Built through the public Add so the copy's bookkeeping follows the same single set
            // of rules as any other instance (the Clone convention of MultiDictionary).
            var clone = new ReverseMultiDictionary<V, K>(_comparer);
            foreach (var pair in this)
            {
                clone.Add(pair.Key, pair.Value);
            }

            return clone;
        }

        /// <summary>
        /// Returns the contents in per-value form, comma separated,
        /// e.g. <c>v1:[k1,k2],v2:[k3]</c>; a stored <c>null</c> value renders as <c>null:[k1]</c>
        /// and enumerates last.
        /// </summary>
        public override string ToString()
        {
            var entries = new List<string>(_index.Count + (_nullValueKeys != null ? 1 : 0));
            foreach (var pair in _index)
            {
                entries.Add($"{pair.Key}:[{string.Join(",", pair.Value)}]");
            }

            if (_nullValueKeys != null)
            {
                entries.Add($"null:[{string.Join(",", _nullValueKeys)}]");
            }

            return string.Join(",", entries);
        }

        /// <summary>
        /// Enumerates the index as flat (value, key) pairs - one entry per binding. A stored
        /// <c>null</c> value enumerates last.
        /// </summary>
        public IEnumerator<KeyValuePair<V, K>> GetEnumerator()
        {
            foreach (var pair in _index)
            {
                foreach (var key in pair.Value)
                {
                    yield return new KeyValuePair<V, K>(pair.Key, key);
                }
            }

            if (_nullValueKeys != null)
            {
                foreach (var key in _nullValueKeys)
                {
                    yield return new KeyValuePair<V, K>(default!, key);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<KeyValuePair<V, IReadOnlyCollection<K>>>
            IEnumerable<KeyValuePair<V, IReadOnlyCollection<K>>>.GetEnumerator()
        {
            foreach (var pair in _index)
            {
                yield return new KeyValuePair<V, IReadOnlyCollection<K>>(pair.Key, pair.Value);
            }

            if (_nullValueKeys != null)
            {
                yield return new KeyValuePair<V, IReadOnlyCollection<K>>(default!, _nullValueKeys);
            }
        }

        IEnumerable<IReadOnlyCollection<K>> IReadOnlyDictionary<V, IReadOnlyCollection<K>>.Values
        {
            get
            {
                foreach (var pair in _index)
                {
                    yield return pair.Value;
                }

                if (_nullValueKeys != null)
                {
                    yield return _nullValueKeys;
                }
            }
        }

        IEnumerable<V> IReadOnlyDictionary<V, IReadOnlyCollection<K>>.Keys => Values;

        bool IReadOnlyDictionary<V, IReadOnlyCollection<K>>.ContainsKey(V value)
        {
            return ContainsValue(value);
        }

        /// <summary>
        /// The set of keys stored under one value. A thin wrapper over
        /// <see cref="HashSet{K}"/> that implements <see cref="IReadOnlyCollection{K}"/> itself:
        /// on net451 / net461 the framework's own <c>HashSet&lt;T&gt;</c> does not declare that
        /// interface (neither statically nor at runtime), so exposing the raw set would fail on
        /// exactly those targets the package supports. Owning the wrapper keeps the index
        /// readable on every TFM without per-read allocations.
        /// </summary>
        private sealed class KeySet : IReadOnlyCollection<K>
        {
            private readonly HashSet<K> _set;

            public KeySet(IEqualityComparer<K> comparer)
            {
                _set = new HashSet<K>(comparer);
            }

            public int Count => _set.Count;

            public bool Add(K key)
            {
                return _set.Add(key);
            }

            public bool Remove(K key)
            {
                return _set.Remove(key);
            }

            public bool Contains(K key)
            {
                return _set.Contains(key);
            }

            public IEnumerator<K> GetEnumerator()
            {
                return _set.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
