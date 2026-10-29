using System.Collections.Generic;
using DotNetCore.Collections.Multi;

namespace DotNetCore.Collections.Testing.PropertyTesting.Adapters
{
    /// <summary>
    /// Adapts <see cref="PersistentMultiDictionary{TKey,TValue}"/> - a structurally shared immutable
    /// multimap - to the property suite.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <remarks>
    /// Every mutating member returns the new instance rather than the one it was handed, which is the
    /// only difference from <see cref="MultiDictionaryAdapter{TKey,TValue}"/>. The property itself is
    /// unchanged: it keeps whatever instance the adapter returns.
    /// </remarks>
    public sealed class PersistentMultiDictionaryAdapter<TKey, TValue> : IMultimapUnderTest<PersistentMultiDictionary<TKey, TValue>, TKey, TValue>
    {
        private readonly bool _allowDuplicateValues;

        /// <summary>
        /// Initializes a new adapter.
        /// </summary>
        /// <param name="allowDuplicateValues">Whether the maps it creates accept duplicate values per key.</param>
        public PersistentMultiDictionaryAdapter(bool allowDuplicateValues)
        {
            _allowDuplicateValues = allowDuplicateValues;
        }

        /// <inheritdoc />
        public PersistentMultiDictionary<TKey, TValue> Create(IEqualityComparer<TKey> keyComparer)
        {
            return new PersistentMultiDictionary<TKey, TValue>(keyComparer, _allowDuplicateValues);
        }

        /// <inheritdoc />
        public PersistentMultiDictionary<TKey, TValue> Add(PersistentMultiDictionary<TKey, TValue> map, TKey key, TValue value)
        {
            return map.Add(key, value);
        }

        /// <inheritdoc />
        public PersistentMultiDictionary<TKey, TValue> RemoveValue(PersistentMultiDictionary<TKey, TValue> map, TKey key, TValue value, out bool removed)
        {
            var next = map.Remove(key, value);
            removed = !ReferenceEquals(next, map);
            return next;
        }

        /// <inheritdoc />
        public PersistentMultiDictionary<TKey, TValue> RemoveKey(PersistentMultiDictionary<TKey, TValue> map, TKey key, out bool removed)
        {
            var next = map.Remove(key);
            removed = !ReferenceEquals(next, map);
            return next;
        }

        /// <inheritdoc />
        public PersistentMultiDictionary<TKey, TValue> Clear(PersistentMultiDictionary<TKey, TValue> map)
        {
            return map.Clear();
        }

        /// <inheritdoc />
        public int KeyCount(PersistentMultiDictionary<TKey, TValue> map)
        {
            return map.KeyCount;
        }

        /// <inheritdoc />
        public int TotalValueCount(PersistentMultiDictionary<TKey, TValue> map)
        {
            return map.TotalValueCount;
        }

        /// <inheritdoc />
        public int ReportedCount(PersistentMultiDictionary<TKey, TValue> map)
        {
            return map.Count;
        }

        /// <inheritdoc />
        public int ValueCount(PersistentMultiDictionary<TKey, TValue> map, TKey key)
        {
            return map.ValueCount(key);
        }

        /// <inheritdoc />
        public bool Contains(PersistentMultiDictionary<TKey, TValue> map, TKey key, TValue value)
        {
            return map.Contains(key, value);
        }

        /// <inheritdoc />
        public bool ContainsValue(PersistentMultiDictionary<TKey, TValue> map, TValue value)
        {
            return map.ContainsValue(value);
        }

        /// <inheritdoc />
        public IReadOnlyCollection<TValue> ValuesOf(PersistentMultiDictionary<TKey, TValue> map, TKey key)
        {
            return map[key];
        }

        /// <inheritdoc />
        public IEnumerable<TKey> Keys(PersistentMultiDictionary<TKey, TValue> map)
        {
            return map.Keys;
        }

        /// <inheritdoc />
        public IEnumerable<TValue> FlattenedValues(PersistentMultiDictionary<TKey, TValue> map)
        {
            return map.Values;
        }
    }
}
