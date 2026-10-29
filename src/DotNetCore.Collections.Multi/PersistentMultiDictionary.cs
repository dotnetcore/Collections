using System;
using System.Collections.Generic;

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// An immutable multimap whose updates share the untouched parts of the structure, so adding or
    /// removing one value costs a handful of nodes rather than a copy of the whole collection.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <remarks>
    /// <para>
    /// <b>"Persistent" means structural sharing, not persistence to disk.</b> Every update returns a
    /// new map and leaves the receiver usable, but the two share every subtree the update did not
    /// touch. That is the difference from <see cref="ImmutableMultiDictionary{TKey,TValue}"/>, which
    /// is equally immutable but clones its whole snapshot on every write and therefore costs
    /// O(<c>N</c>) per update - O(<c>N</c><sup>2</sup>) to build a collection of <c>N</c> values.
    /// </para>
    /// <para>
    /// Keys are hashed with the comparer supplied at construction and stored in a compressed
    /// hash-array mapped trie: each level spends five bits, entries that are alone in a slot stay
    /// inline, and only slots that collide allocate a sub-trie. A lookup therefore touches at most
    /// seven nodes, and a write copies at most that many - O(log<sub>32</sub> <c>N</c>) expected,
    /// against O(<c>N</c>) for the clone-on-write family.
    /// </para>
    /// <para>
    /// Copies of one value are kept in a linked list, so adding <c>m</c> copies under a single key is
    /// O(<c>m</c>) rather than O(<c>m</c><sup>2</sup>). A key disappears as soon as its last copy is
    /// removed, which is why <see cref="KeyCount"/> and <see cref="TotalValueCount"/> can never
    /// disagree about an empty key.
    /// </para>
    /// <para>
    /// <b>Enumerating yields one pair per value, not per key.</b> A map holding two values under one
    /// key produces two <c>KeyValuePair</c> items, which is the same shape
    /// <see cref="ImmutableMultiDictionary{TKey,TValue}"/> and <see cref="MultiDictionary{TKey,TValue}"/>
    /// expose. <see cref="Count"/> nevertheless reports the number of keys, matching
    /// <see cref="IMultiDictionary{TKey,TValue}"/>'s inherited dictionary view.
    /// </para>
    /// <para>
    /// This type deliberately does not implement <see cref="IMultiDictionary{TKey,TValue}"/>: that
    /// interface declares <c>void Add</c> and <c>bool Remove</c>, which no immutable type can honour.
    /// The same choice was made for <see cref="ImmutableMultiDictionary{TKey,TValue}"/>.
    /// </para>
    /// </remarks>
    public sealed class PersistentMultiDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private static readonly TValue[] NoValues = new TValue[0];

        /// <summary>
        /// The payload kind this collection stores under a key: one cell per copy, so that copies of
        /// two different values stay distinguishable.
        /// </summary>
        private static readonly PersistentPayloadFactory<TKey, TValue> Payloads = PersistentPayloadFactory<TKey, TValue>.ValueCells;

        private readonly PersistentNode<TKey, TValue>? _root;
        private readonly IEqualityComparer<TKey> _keyComparer;
        private readonly bool _allowDuplicateValues;
        private readonly int _keyCount;
        private readonly int _valueCount;

        /// <summary>
        /// Initializes a new, empty map that accepts duplicate values and uses the default key
        /// comparer.
        /// </summary>
        public PersistentMultiDictionary()
            : this(null, true)
        {
        }

        /// <summary>
        /// Initializes a new, empty map that accepts duplicate values.
        /// </summary>
        /// <param name="comparer">The key comparer, or <c>null</c> for the default one.</param>
        public PersistentMultiDictionary(IEqualityComparer<TKey>? comparer)
            : this(comparer, true)
        {
        }

        /// <summary>
        /// Initializes a new, empty map that uses the default key comparer.
        /// </summary>
        /// <param name="allowDuplicateValues">Whether a value may be stored twice under the same key.</param>
        public PersistentMultiDictionary(bool allowDuplicateValues)
            : this(null, allowDuplicateValues)
        {
        }

        /// <summary>
        /// Initializes a new, empty map.
        /// </summary>
        /// <param name="comparer">The key comparer, or <c>null</c> for the default one.</param>
        /// <param name="allowDuplicateValues">Whether a value may be stored twice under the same key.</param>
        public PersistentMultiDictionary(IEqualityComparer<TKey>? comparer, bool allowDuplicateValues)
        {
            _root = null;
            _keyComparer = comparer ?? EqualityComparer<TKey>.Default;
            _allowDuplicateValues = allowDuplicateValues;
            _keyCount = 0;
            _valueCount = 0;
        }

        private PersistentMultiDictionary(
            PersistentNode<TKey, TValue>? root,
            IEqualityComparer<TKey> keyComparer,
            bool allowDuplicateValues,
            int keyCount,
            int valueCount)
        {
            _root = root;
            _keyComparer = keyComparer;
            _allowDuplicateValues = allowDuplicateValues;
            _keyCount = keyCount;
            _valueCount = valueCount;
        }

        /// <summary>
        /// Gets the comparer used to decide key equality.
        /// </summary>
        /// <value>The comparer supplied at construction, or <see cref="EqualityComparer{T}.Default"/>.</value>
        public IEqualityComparer<TKey> Comparer => _keyComparer;

        /// <summary>
        /// Gets the number of keys, matching the inherited dictionary view rather than the number of
        /// values.
        /// </summary>
        /// <value>The number of distinct keys.</value>
        public int Count => _keyCount;

        /// <summary>
        /// Gets the number of keys.
        /// </summary>
        /// <value>The number of distinct keys.</value>
        public int KeyCount => _keyCount;

        /// <summary>
        /// Gets the total number of values across all keys.
        /// </summary>
        /// <value>The number of stored values.</value>
        public int TotalValueCount => _valueCount;

        /// <summary>
        /// Gets the values stored under a key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <value>An empty collection when the key is absent; never <c>null</c>.</value>
        public IReadOnlyCollection<TValue> this[TKey key] => ValuesOf(key);

        /// <summary>
        /// Returns the number of values stored under a key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>The number of values, or zero when the key is absent.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public int ValueCount(TKey key)
        {
            PersistentEntry<TKey, TValue> entry;
            return TryFindEntry(key, out entry) ? entry.ValueCount : 0;
        }

        /// <summary>
        /// Gets every key exactly once.
        /// </summary>
        /// <value>The distinct keys, in unspecified order.</value>
        public IEnumerable<TKey> Keys => CollectKeys();

        /// <summary>
        /// Gets every value, flattened across keys.
        /// </summary>
        /// <value>One item per stored value, in unspecified key order.</value>
        public IEnumerable<TValue> Values => CollectValues();

        /// <summary>
        /// Returns a new map with one more copy of a value under a key.
        /// </summary>
        /// <param name="key">The key to add under.</param>
        /// <param name="value">The value to add; <c>null</c> is an ordinary value.</param>
        /// <returns>
        /// A new map, or the receiver when the value was rejected as a duplicate of one already
        /// stored under this key.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public PersistentMultiDictionary<TKey, TValue> Add(TKey key, TValue value)
        {
            var hash = HashOf(key);

            if (_root == null)
            {
                var first = new PersistentEntry<TKey, TValue>(hash, key, Payloads.Create(value, 1));
                return new PersistentMultiDictionary<TKey, TValue>(
                    new CollisionNode<TKey, TValue>(hash, new[] { first }),
                    _keyComparer,
                    _allowDuplicateValues,
                    1,
                    1);
            }

            var hadKey = _root.TryGetEntry(hash, key, _keyComparer, out _);

            bool changed;
            var root = _root.Add(0, hash, key, value, _keyComparer, _allowDuplicateValues, Payloads, 1, out changed);

            if (!changed)
            {
                return this;
            }

            return new PersistentMultiDictionary<TKey, TValue>(
                root,
                _keyComparer,
                _allowDuplicateValues,
                hadKey ? _keyCount : _keyCount + 1,
                _valueCount + 1);
        }

        /// <summary>
        /// Returns a new map with one more copy of each value under a key.
        /// </summary>
        /// <param name="key">The key to add under.</param>
        /// <param name="values">The values to add, one copy each.</param>
        /// <returns>A new map, or the receiver when no value was added.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="key"/> or <paramref name="values"/> is <c>null</c>.
        /// </exception>
        public PersistentMultiDictionary<TKey, TValue> AddRange(TKey key, IEnumerable<TValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            var result = this;

            foreach (var value in values)
            {
                result = result.Add(key, value);
            }

            return result;
        }

        /// <summary>
        /// Returns a new map with every copy of every value removed from under a key.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>A new map, or the receiver when the key was absent.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public PersistentMultiDictionary<TKey, TValue> Remove(TKey key)
        {
            if (_root == null)
            {
                return this;
            }

            var hash = HashOf(key);

            int removedValues;
            var root = _root.RemoveKey(0, hash, key, _keyComparer, out removedValues);

            if (removedValues == 0)
            {
                return this;
            }

            return new PersistentMultiDictionary<TKey, TValue>(
                root,
                _keyComparer,
                _allowDuplicateValues,
                _keyCount - 1,
                _valueCount - removedValues);
        }

        /// <summary>
        /// Returns a new map with one copy of a value removed from under a key.
        /// </summary>
        /// <param name="key">The key to remove from.</param>
        /// <param name="value">The value to remove one copy of.</param>
        /// <returns>A new map, or the receiver when no such copy was present.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public PersistentMultiDictionary<TKey, TValue> Remove(TKey key, TValue value)
        {
            if (_root == null)
            {
                return this;
            }

            var hash = HashOf(key);

            int removed;
            var root = _root.RemoveValue(0, hash, key, value, 1, _keyComparer, out removed);

            if (removed == 0)
            {
                return this;
            }

            var keyGone = root == null || !root.TryGetEntry(hash, key, _keyComparer, out _);

            return new PersistentMultiDictionary<TKey, TValue>(
                root,
                _keyComparer,
                _allowDuplicateValues,
                keyGone ? _keyCount - 1 : _keyCount,
                _valueCount - removed);
        }

        /// <summary>
        /// Returns a new map with one copy of each value removed from under a key.
        /// </summary>
        /// <param name="key">The key to remove from.</param>
        /// <param name="values">The values whose copies should be removed.</param>
        /// <returns>A new map, or the receiver when nothing was removed.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="key"/> or <paramref name="values"/> is <c>null</c>.
        /// </exception>
        public PersistentMultiDictionary<TKey, TValue> RemoveRange(TKey key, IEnumerable<TValue> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            var result = this;

            foreach (var value in values)
            {
                result = result.Remove(key, value);
            }

            return result;
        }

        /// <summary>
        /// Returns a new, empty map with the same comparer and duplicate-value mode.
        /// </summary>
        /// <returns>An empty map.</returns>
        public PersistentMultiDictionary<TKey, TValue> Clear()
        {
            if (_root == null)
            {
                return this;
            }

            return new PersistentMultiDictionary<TKey, TValue>(null, _keyComparer, _allowDuplicateValues, 0, 0);
        }

        /// <summary>
        /// Determines whether the map holds at least one value under a key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns><c>true</c> when the key is present.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public bool ContainsKey(TKey key)
        {
            PersistentEntry<TKey, TValue> entry;
            return TryFindEntry(key, out entry);
        }

        /// <summary>
        /// Determines whether a value is stored under a key.
        /// </summary>
        /// <param name="key">The key to look under.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns><c>true</c> when at least one copy is present.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public bool Contains(TKey key, TValue value)
        {
            PersistentEntry<TKey, TValue> entry;

            if (!TryFindEntry(key, out entry) || entry.Payload == null)
            {
                return false;
            }

            return entry.Payload.Contains(value);
        }

        /// <summary>
        /// Determines whether a value is stored under any key.
        /// </summary>
        /// <param name="value">The value to locate.</param>
        /// <returns><c>true</c> when at least one copy is present.</returns>
        public bool ContainsValue(TValue value)
        {
            foreach (var entry in CollectEntries())
            {
                if (entry.Payload != null && entry.Payload.Contains(value))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the values stored under a key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <returns>An empty collection when the key is absent; never <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public IReadOnlyCollection<TValue> ValuesOf(TKey key)
        {
            PersistentEntry<TKey, TValue> entry;

            if (!TryFindEntry(key, out entry))
            {
                return NoValues;
            }

            return Materialize(entry);
        }

        /// <summary>
        /// Attempts to read the values stored under a key.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        /// <param name="values">Receives the values when the key is present.</param>
        /// <returns><c>true</c> when the key is present.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        public bool TryGetValue(TKey key, out IReadOnlyCollection<TValue> values)
        {
            PersistentEntry<TKey, TValue> entry;

            if (!TryFindEntry(key, out entry))
            {
                values = NoValues;
                return false;
            }

            values = Materialize(entry);
            return true;
        }

        /// <summary>
        /// Returns an enumerator over one pair per stored value.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var entry in CollectEntries())
            {
                if (entry.Payload == null)
                {
                    continue;
                }

                foreach (var value in entry.Payload.Enumerate())
                {
                    yield return new KeyValuePair<TKey, TValue>(entry.Key, value);
                }
            }
        }

        /// <summary>
        /// Returns an enumerator over one pair per stored value.
        /// </summary>
        /// <returns>The enumerator.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a description of the map's size.
        /// </summary>
        /// <returns>A string of the form <c>PersistentMultiDictionary&lt;TKey, TValue&gt;(keys, values)</c>.</returns>
        public override string ToString()
        {
            return "PersistentMultiDictionary<" + typeof(TKey).Name + ", " + typeof(TValue).Name + ">(" + _keyCount + " keys, " + _valueCount + " values)";
        }

        private bool TryFindEntry(TKey key, out PersistentEntry<TKey, TValue> entry)
        {
            if (_root == null)
            {
                entry = default(PersistentEntry<TKey, TValue>);
                return false;
            }

            return _root.TryGetEntry(HashOf(key), key, _keyComparer, out entry);
        }

        private int HashOf(TKey key)
        {
            if (ReferenceEquals(key, null))
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _keyComparer.GetHashCode(key) & 0x7FFFFFFF;
        }

        private List<PersistentEntry<TKey, TValue>> CollectEntries()
        {
            var sink = new List<PersistentEntry<TKey, TValue>>(_root == null ? 0 : _keyCount);
            _root?.CollectEntries(sink);
            return sink;
        }

        private IEnumerable<TKey> CollectKeys()
        {
            foreach (var entry in CollectEntries())
            {
                yield return entry.Key;
            }
        }

        private IEnumerable<TValue> CollectValues()
        {
            foreach (var entry in CollectEntries())
            {
                if (entry.Payload == null)
                {
                    continue;
                }

                foreach (var value in entry.Payload.Enumerate())
                {
                    yield return value;
                }
            }
        }

        private static TValue[] Materialize(PersistentEntry<TKey, TValue> entry)
        {
            if (entry.Payload == null || entry.Payload.Count == 0)
            {
                return NoValues;
            }

            var result = new TValue[entry.Payload.Count];
            entry.Payload.Expand(result);
            return result;
        }
    }
}
