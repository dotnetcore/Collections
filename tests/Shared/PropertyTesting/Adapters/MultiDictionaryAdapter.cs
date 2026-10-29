using System.Collections.Generic;
using DotNetCore.Collections.Multi;

namespace DotNetCore.Collections.Testing.PropertyTesting.Adapters
{
    /// <summary>
    /// Adapts <see cref="MultiDictionary{TKey,TValue}"/> - a mutable multimap - to the property
    /// suite.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <remarks>
    /// The duplicate-value mode is a construction-time decision rather than a per-call one, so the
    /// adapter carries it and uses it when creating instances. It must match the mode handed to
    /// <see cref="MultimapModelEquivalenceProperty{TMap,TKey,TValue}"/>, otherwise every run would
    /// report the same expected divergence.
    /// </remarks>
    public sealed class MultiDictionaryAdapter<TKey, TValue> : IMultimapUnderTest<MultiDictionary<TKey, TValue>, TKey, TValue>
    {
        private readonly bool _allowDuplicateValues;

        /// <summary>
        /// Initializes a new adapter.
        /// </summary>
        /// <param name="allowDuplicateValues">Whether the maps it creates accept duplicate values per key.</param>
        public MultiDictionaryAdapter(bool allowDuplicateValues)
        {
            _allowDuplicateValues = allowDuplicateValues;
        }

        /// <inheritdoc />
        public MultiDictionary<TKey, TValue> Create(IEqualityComparer<TKey> keyComparer)
        {
            return new MultiDictionary<TKey, TValue>(keyComparer, _allowDuplicateValues);
        }

        /// <inheritdoc />
        public MultiDictionary<TKey, TValue> Add(MultiDictionary<TKey, TValue> map, TKey key, TValue value)
        {
            map.Add(key, value);
            return map;
        }

        /// <inheritdoc />
        public MultiDictionary<TKey, TValue> RemoveValue(MultiDictionary<TKey, TValue> map, TKey key, TValue value, out bool removed)
        {
            removed = map.Remove(key, value);
            return map;
        }

        /// <inheritdoc />
        public MultiDictionary<TKey, TValue> RemoveKey(MultiDictionary<TKey, TValue> map, TKey key, out bool removed)
        {
            removed = map.Remove(key);
            return map;
        }

        /// <inheritdoc />
        public MultiDictionary<TKey, TValue> Clear(MultiDictionary<TKey, TValue> map)
        {
            map.Clear();
            return map;
        }

        /// <inheritdoc />
        public int KeyCount(MultiDictionary<TKey, TValue> map)
        {
            return map.KeyCount;
        }

        /// <inheritdoc />
        public int TotalValueCount(MultiDictionary<TKey, TValue> map)
        {
            return map.TotalValueCount;
        }

        /// <inheritdoc />
        public int ReportedCount(MultiDictionary<TKey, TValue> map)
        {
            return map.Count;
        }

        /// <inheritdoc />
        public int ValueCount(MultiDictionary<TKey, TValue> map, TKey key)
        {
            return map.ValueCount(key);
        }

        /// <inheritdoc />
        public bool Contains(MultiDictionary<TKey, TValue> map, TKey key, TValue value)
        {
            return map.Contains(key, value);
        }

        /// <inheritdoc />
        public bool ContainsValue(MultiDictionary<TKey, TValue> map, TValue value)
        {
            return map.ContainsValue(value);
        }

        /// <inheritdoc />
        /// <remarks>
        /// The indexer is the member the contract pins to "empty collection for an absent key", so
        /// the property verifies that promise through this adapter rather than through a helper that
        /// would soften it.
        /// </remarks>
        public IReadOnlyCollection<TValue> ValuesOf(MultiDictionary<TKey, TValue> map, TKey key)
        {
            return map[key];
        }

        /// <inheritdoc />
        public IEnumerable<TKey> Keys(MultiDictionary<TKey, TValue> map)
        {
            return map.Keys;
        }

        /// <inheritdoc />
        public IEnumerable<TValue> FlattenedValues(MultiDictionary<TKey, TValue> map)
        {
            return map.Values;
        }
    }
}
