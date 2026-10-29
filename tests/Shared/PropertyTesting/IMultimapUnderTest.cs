using System.Collections.Generic;

namespace DotNetCore.Collections.Testing.PropertyTesting
{
    /// <summary>
    /// Adapts one multimap implementation to the vocabulary the property suite speaks, mirroring
    /// <see cref="IBagUnderTest{TBag,T}"/> for the keyed family.
    /// </summary>
    /// <typeparam name="TMap">The concrete multimap type.</typeparam>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <remarks>
    /// Every mutating member takes the current instance and returns the instance to use afterwards,
    /// so a mutable adapter returns the same object and a persistent adapter returns the new one.
    /// Removal members expose the contract's return value through <c>out</c> parameters because
    /// that value is part of what is under test.
    /// </remarks>
    public interface IMultimapUnderTest<TMap, TKey, TValue>
    {
        /// <summary>
        /// Creates an empty multimap.
        /// </summary>
        /// <param name="keyComparer">The key comparer, or <c>null</c> for the default one.</param>
        /// <returns>An empty multimap.</returns>
        TMap Create(IEqualityComparer<TKey> keyComparer);

        /// <summary>
        /// Adds the value under the key.
        /// </summary>
        /// <param name="map">The current instance.</param>
        /// <param name="key">The key to add under.</param>
        /// <param name="value">The value to add.</param>
        /// <returns>The instance to use afterwards.</returns>
        TMap Add(TMap map, TKey key, TValue value);

        /// <summary>
        /// Removes a single occurrence of the value under the key.
        /// </summary>
        /// <param name="map">The current instance.</param>
        /// <param name="key">The key to remove from.</param>
        /// <param name="value">The value to remove.</param>
        /// <param name="removed">The value the contract promises as the return value.</param>
        /// <returns>The instance to use afterwards.</returns>
        TMap RemoveValue(TMap map, TKey key, TValue value, out bool removed);

        /// <summary>
        /// Removes the key together with all of its values.
        /// </summary>
        /// <param name="map">The current instance.</param>
        /// <param name="key">The key to remove.</param>
        /// <param name="removed">The value the contract promises as the return value.</param>
        /// <returns>The instance to use afterwards.</returns>
        TMap RemoveKey(TMap map, TKey key, out bool removed);

        /// <summary>
        /// Removes every key and value.
        /// </summary>
        /// <param name="map">The current instance.</param>
        /// <returns>The instance to use afterwards.</returns>
        TMap Clear(TMap map);

        /// <summary>
        /// Returns the number of keys that currently have at least one value.
        /// </summary>
        /// <param name="map">The current instance.</param>
        /// <returns>The number of keys.</returns>
        int KeyCount(TMap map);

        /// <summary>
        /// Returns the total number of (key, value) pairs.
        /// </summary>
        /// <param name="map">The current instance.</param>
        /// <returns>The number of values across all keys.</returns>
        int TotalValueCount(TMap map);

        /// <summary>
        /// Returns the value of the inherited <c>Count</c>, which the multimap contract pins to the
        /// number of keys rather than the number of values.
        /// </summary>
        /// <param name="map">The current instance.</param>
        /// <returns>The value the collection reports as its count.</returns>
        int ReportedCount(TMap map);

        /// <summary>
        /// Returns the number of values stored under the key.
        /// </summary>
        /// <param name="map">The current instance.</param>
        /// <param name="key">The key to inspect.</param>
        /// <returns>The number of values, or zero when the key is absent.</returns>
        int ValueCount(TMap map, TKey key);

        /// <summary>
        /// Determines whether the value is stored under the key.
        /// </summary>
        /// <param name="map">The current instance.</param>
        /// <param name="key">The key to inspect.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns><c>true</c> when stored under the key.</returns>
        bool Contains(TMap map, TKey key, TValue value);

        /// <summary>
        /// Determines whether the value is stored under any key.
        /// </summary>
        /// <param name="map">The current instance.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns><c>true</c> when stored under at least one key.</returns>
        bool ContainsValue(TMap map, TValue value);

        /// <summary>
        /// Returns the values stored under the key, which must be empty for a missing key rather
        /// than throwing.
        /// </summary>
        /// <param name="map">The current instance.</param>
        /// <param name="key">The key to inspect.</param>
        /// <returns>The values, or an empty collection.</returns>
        IReadOnlyCollection<TValue> ValuesOf(TMap map, TKey key);

        /// <summary>
        /// Returns the keys that currently have at least one value.
        /// </summary>
        /// <param name="map">The current instance.</param>
        /// <returns>The keys.</returns>
        IEnumerable<TKey> Keys(TMap map);

        /// <summary>
        /// Returns every value of every key, flattened.
        /// </summary>
        /// <param name="map">The current instance.</param>
        /// <returns>The flattened values.</returns>
        IEnumerable<TValue> FlattenedValues(TMap map);
    }
}
