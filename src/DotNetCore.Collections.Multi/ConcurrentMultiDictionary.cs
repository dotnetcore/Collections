using System;
using System.Collections;
using System.Collections.Generic;

// CS8714: TKey is deliberately unconstrained, matching MultiDictionary<TKey, TValue>
// (a null key is rejected on every write, hash-spreads to shard 0, and enumerates).
#pragma warning disable CS8714

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Represents a thread-safe multimap: a dictionary in which one key genuinely owns several
    /// values. Writes and single-key reads are serialized per shard only, so threads working on
    /// different keys proceed in parallel; whole-map reads take a consistent snapshot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The map is partitioned into a fixed number of shards, each an independent
    /// <see cref="MultiDictionary{TKey,TValue}"/> guarded by its own lock. A key is routed to its
    /// shard by its comparer hash code, so all operations on one key always hit the same shard,
    /// with the same per-key semantics as <see cref="MultiDictionary{TKey,TValue}"/> (duplicates
    /// per key allowed or collapsed by policy, a key whose value collection has emptied is
    /// dropped).
    /// </para>
    /// <para>
    /// Reads that span shards - <see cref="Count"/>, <see cref="TotalValueCount"/>,
    /// <see cref="ContainsValue"/>, enumeration - take a consistent snapshot: every shard is
    /// locked once, in index order, so no write can interleave inside a snapshot. These are the
    /// expensive paths; the per-key operations are not.
    /// </para>
    /// <para>
    /// Enumeration is over a snapshot: the enumerator is immune to concurrent writes, at the cost
    /// of copying the map when enumeration starts. The same snapshot backs <see cref="Keys"/> and
    /// <see cref="Values"/>.
    /// </para>
    /// </remarks>
    public class ConcurrentMultiDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly MultiDictionary<TKey, TValue>[] _shards;
        private readonly object[] _locks;
        private readonly int _mask;
        private readonly IEqualityComparer<TKey> _comparer;

        /// <summary>
        /// Initializes an empty concurrent multimap that allows duplicate values per key, with the
        /// default shard count (8) and the default key comparer.
        /// </summary>
        public ConcurrentMultiDictionary() : this(0, null, true)
        {
        }

        /// <summary>
        /// Initializes an empty concurrent multimap with the specified key comparer, allowing
        /// duplicate values per key.
        /// </summary>
        public ConcurrentMultiDictionary(IEqualityComparer<TKey>? comparer) : this(0, comparer, true)
        {
        }

        /// <summary>
        /// Initializes an empty concurrent multimap that allows or forbids duplicate values per
        /// key.
        /// </summary>
        public ConcurrentMultiDictionary(bool allowDuplicateValues) : this(0, null, allowDuplicateValues)
        {
        }

        /// <summary>
        /// Initializes an empty concurrent multimap with the specified shard count, key comparer
        /// and duplicate values policy. A non-positive <paramref name="shardCount"/> selects the
        /// default (8); other counts are rounded up to the next power of two, because shard
        /// routing masks hash bits.
        /// </summary>
        public ConcurrentMultiDictionary(int shardCount, IEqualityComparer<TKey>? comparer, bool allowDuplicateValues)
        {
            if (shardCount <= 0)
            {
                shardCount = 8;
            }

            var size = 1;
            while (size < shardCount)
            {
                size <<= 1;
            }

            _comparer = comparer ?? EqualityComparer<TKey>.Default;
            _shards = new MultiDictionary<TKey, TValue>[size];
            _locks = new object[size];
            for (var i = 0; i < size; i++)
            {
                _shards[i] = new MultiDictionary<TKey, TValue>(_comparer, allowDuplicateValues);
                _locks[i] = new object();
            }

            _mask = size - 1;
        }

        /// <summary>
        /// Gets the comparer that determines key equality.
        /// </summary>
        public IEqualityComparer<TKey> Comparer => _comparer;

        /// <summary>
        /// Gets the number of shards the map is partitioned into.
        /// </summary>
        public int ShardCount => _shards.Length;

        /// <summary>
        /// Gets the number of distinct keys stored across all shards. Takes a consistent snapshot
        /// of the whole map.
        /// </summary>
        public int Count
        {
            get
            {
                var total = 0;
                for (var i = 0; i < _shards.Length; i++)
                {
                    lock (_locks[i])
                    {
                        total += _shards[i].Count;
                    }
                }

                return total;
            }
        }

        /// <summary>
        /// Gets the number of distinct keys stored across all shards. Alias of <see cref="Count"/>,
        /// named the way <see cref="MultiDictionary{TKey,TValue}.KeyCount"/> is. Takes a
        /// consistent snapshot of the whole map.
        /// </summary>
        public int KeyCount => Count;

        /// <summary>
        /// Gets the total number of values stored across all keys and shards. Takes a consistent
        /// snapshot of the whole map.
        /// </summary>
        public int TotalValueCount
        {
            get
            {
                var total = 0;
                for (var i = 0; i < _shards.Length; i++)
                {
                    lock (_locks[i])
                    {
                        total += _shards[i].TotalValueCount;
                    }
                }

                return total;
            }
        }

        /// <summary>
        /// Gets the value collection stored for the given key. An absent key yields an empty
        /// collection (never <c>null</c> and never an exception), mirroring
        /// <see cref="MultiDictionary{TKey,TValue}.this[TKey]"/>.
        /// </summary>
        public IReadOnlyCollection<TValue> this[TKey key]
        {
            get
            {
                return Shard(key)[key];
            }
        }

        private MultiDictionary<TKey, TValue> Shard(TKey key)
        {
            var hash = key == null ? 0 : _comparer.GetHashCode(key);
            hash ^= hash >> 16;
            return _shards[hash & _mask];
        }

        private object LockOf(TKey key)
        {
            var hash = key == null ? 0 : _comparer.GetHashCode(key);
            hash ^= hash >> 16;
            return _locks[hash & _mask];
        }

        /// <summary>
        /// Appends the given value to the key's value collection. A duplicate value on a key that
        /// forbids them throws <see cref="ArgumentException"/>, mirroring
        /// <see cref="MultiDictionary{TKey,TValue}.Add"/>.
        /// </summary>
        public void Add(TKey key, TValue value)
        {
            lock (LockOf(key))
            {
                Shard(key).Add(key, value);
            }
        }

        /// <summary>
        /// Appends the given values to the key's value collection, under one lock acquisition.
        /// </summary>
        public void AddRange(TKey key, IEnumerable<TValue> values)
        {
            lock (LockOf(key))
            {
                Shard(key).AddRange(key, values);
            }
        }

        /// <summary>
        /// Removes the given key and all its values, and returns whether the key was present.
        /// </summary>
        public bool Remove(TKey key)
        {
            lock (LockOf(key))
            {
                return Shard(key).Remove(key);
            }
        }

        /// <summary>
        /// Removes one occurrence of the given value from the key's value collection, and returns
        /// whether an occurrence was removed. A key whose collection has emptied is dropped.
        /// </summary>
        public bool Remove(TKey key, TValue value)
        {
            lock (LockOf(key))
            {
                return Shard(key).Remove(key, value);
            }
        }

        /// <summary>
        /// Removes one occurrence of each distinct argument value from the key's value collection,
        /// and returns whether anything was removed.
        /// </summary>
        public bool RemoveRange(TKey key, IEnumerable<TValue> values)
        {
            lock (LockOf(key))
            {
                return Shard(key).RemoveRange(key, values);
            }
        }

        /// <summary>
        /// Gets the number of values stored for the given key (0 when the key is absent). Reads a
        /// single shard.
        /// </summary>
        public int ValueCount(TKey key)
        {
            lock (LockOf(key))
            {
                return Shard(key).ValueCount(key);
            }
        }

        /// <summary>
        /// Determines whether the given key is stored. Reads a single shard.
        /// </summary>
        public bool ContainsKey(TKey key)
        {
            lock (LockOf(key))
            {
                return Shard(key).ContainsKey(key);
            }
        }

        /// <summary>
        /// Determines whether the given key holds at least one occurrence of the given value.
        /// Reads a single shard.
        /// </summary>
        public bool Contains(TKey key, TValue value)
        {
            lock (LockOf(key))
            {
                return Shard(key).Contains(key, value);
            }
        }

        /// <summary>
        /// Determines whether any key holds at least one occurrence of the given value. Takes a
        /// consistent snapshot of the whole map.
        /// </summary>
        public bool ContainsValue(TValue value)
        {
            for (var i = 0; i < _shards.Length; i++)
            {
                lock (_locks[i])
                {
                    if (_shards[i].ContainsValue(value))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the value collection stored for the given key. Reads a single shard.
        /// </summary>
        public bool TryGetValue(TKey key, out IReadOnlyCollection<TValue> value)
        {
            lock (LockOf(key))
            {
                return Shard(key).TryGetValue(key, out value);
            }
        }

        /// <summary>
        /// Removes all keys and values, across every shard.
        /// </summary>
        public void Clear()
        {
            for (var i = 0; i < _shards.Length; i++)
            {
                lock (_locks[i])
                {
                    _shards[i].Clear();
                }
            }
        }

        /// <summary>
        /// Takes a consistent snapshot of the whole map as an independent
        /// <see cref="MultiDictionary{TKey,TValue}"/>: every shard is locked once, in index order,
        /// and its contents copied. The snapshot is immune to concurrent writes; enumerations of
        /// this map are snapshots too.
        /// </summary>
        public MultiDictionary<TKey, TValue> Snapshot()
        {
            var snapshot = new MultiDictionary<TKey, TValue>(_comparer);
            for (var i = 0; i < _shards.Length; i++)
            {
                lock (_locks[i])
                {
                    foreach (var pair in _shards[i])
                    {
                        snapshot.Add(pair.Key, pair.Value);
                    }
                }
            }

            return snapshot;
        }

        /// <summary>
        /// Returns the reverse (value &#8594; keys) direction as an independent
        /// <see cref="ReverseMultiDictionary{V,K}"/> built from a consistent snapshot of the whole
        /// map.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is a <b>snapshot</b>, not a live view, and the type it lives on is why. The family's
        /// other <c>AsReverse</c> members (<see cref="MultiDictionary{TKey,TValue}.AsReverse()"/>,
        /// <see cref="BiDictionary{TLeft,TRight}.AsReverse()"/>) can hand back a zero-copy live view
        /// because those types each maintain a reverse index. This one does not: it keeps N
        /// independent <see cref="MultiDictionary{TKey,TValue}"/> shards and no structure above
        /// them, so a live reverse view could only be (a) an index built once and then silently
        /// stale, (b) rebuilt on every read at O(n) while holding shard locks, or (c) a scan of
        /// every shard per access - since a value is not what routes a key to its shard. None of
        /// those is acceptable, and all of them would be a surprise in a type whose entire read
        /// surface (enumeration, <see cref="Keys"/>, <see cref="Values"/>, <see cref="Count"/>,
        /// <see cref="Snapshot"/>) is already snapshot-based.
        /// </para>
        /// <para>
        /// The result is detached: writes to this map afterwards do not appear in it, and the
        /// returned dictionary can be mutated without affecting this map.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var map = new ConcurrentMultiDictionary&lt;string, int&gt;();
        /// map.Add("orders", 1001);
        /// map.Add("customers", 1001);
        ///
        /// ReverseMultiDictionary&lt;int, string&gt; byValue = map.AsReverse();
        /// byValue[1001];   // ["orders", "customers"] - a snapshot, unaffected by later writes
        /// </code>
        /// </example>
        public ReverseMultiDictionary<TValue, TKey> AsReverse()
        {
            return new ReverseMultiDictionary<TValue, TKey>(Snapshot());
        }

        /// <summary>
        /// Exports a consistent snapshot of the whole map as a plain data model for external
        /// serialization.
        /// </summary>
        /// <remarks>
        /// Taken from <see cref="Snapshot"/>, so the model is a point-in-time copy: concurrent
        /// writers running after the call do not alter it. The model's shape is described by
        /// <see cref="MultiDictionaryModel{TKey,TValue}"/>; use <see cref="FromModel"/> to rebuild
        /// a map from one.
        /// </remarks>
        /// <example>
        /// <code>
        /// MultiDictionaryModel&lt;string, int&gt; model = map.ToSerializableModel();
        /// string json = System.Text.Json.JsonSerializer.Serialize(model);
        /// </code>
        /// </example>
        public MultiDictionaryModel<TKey, TValue> ToSerializableModel()
        {
            return Snapshot().ToSerializableModel();
        }

        /// <summary>
        /// Rebuilds a concurrent multimap from a serializable model.
        /// </summary>
        /// <param name="model">the model to read: either one produced by <see cref="ToSerializableModel"/> or one built by hand.</param>
        /// <param name="comparer">the key comparer of the rebuilt map; <c>null</c> selects <see cref="EqualityComparer{TKey}.Default"/>. The comparer is configuration rather than data, so it is not part of the model and has to be supplied here.</param>
        /// <param name="allowDuplicateValues">whether the rebuilt map keeps duplicate values under one key (<c>true</c>, the default) or collapses them. Also configuration, so it has to be supplied here.</param>
        /// <param name="shardCount">the number of shards to partition the rebuilt map into; a non-positive value selects the default (8). Shard count is configuration too, and has no bearing on the data.</param>
        /// <returns>a new map holding the model's keys with the model's values.</returns>
        /// <remarks>
        /// Values are added in the order the model lists them. Validation of the model follows
        /// <see cref="MultiDictionary{TKey,TValue}.FromModel"/>, and each binding is then written
        /// through the ordinary shard-routed <see cref="Add"/> path.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="model"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="model"/> is malformed: either list is
        /// <c>null</c>, the two lists have different lengths, or one of the inner value lists is
        /// <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// var map = ConcurrentMultiDictionary&lt;string, int&gt;.FromModel(model);
        /// </code>
        /// </example>
        public static ConcurrentMultiDictionary<TKey, TValue> FromModel(
            MultiDictionaryModel<TKey, TValue> model,
            IEqualityComparer<TKey>? comparer = null,
            bool allowDuplicateValues = true,
            int shardCount = 0)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (model.Keys == null)
            {
                throw new ArgumentException("The model is malformed: its Keys list is null.", nameof(model));
            }

            if (model.Values == null)
            {
                throw new ArgumentException("The model is malformed: its Values list is null.", nameof(model));
            }

            if (model.Keys.Count != model.Values.Count)
            {
                throw new ArgumentException(
                    "The model is malformed: Keys and Values have different lengths.", nameof(model));
            }

            var result = new ConcurrentMultiDictionary<TKey, TValue>(shardCount, comparer, allowDuplicateValues);
            for (var i = 0; i < model.Keys.Count; i++)
            {
                var values = model.Values[i];
                if (values == null)
                {
                    throw new ArgumentException(
                        "The model is malformed: the value list of entry " + i + " is null.", nameof(model));
                }

                result.AddRange(model.Keys[i], values);
            }

            return result;
        }

        /// <summary>
        /// Enumerates a consistent snapshot of the whole map (see <see cref="Snapshot"/>). The
        /// enumerator is immune to concurrent writes.
        /// </summary>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return Snapshot().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Enumerates the stored keys of a consistent snapshot of the whole map.
        /// </summary>
        public IEnumerable<TKey> Keys
        {
            get
            {
                return Snapshot().Keys;
            }
        }

        /// <summary>
        /// Enumerates all stored values of a consistent snapshot of the whole map, key by key.
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get
            {
                return Snapshot().Values;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Snapshot().ToString();
        }
    }
}
