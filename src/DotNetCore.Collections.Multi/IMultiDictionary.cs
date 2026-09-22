using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Defines the contract of a <em>multimap</em>: a dictionary that associates multiple values
    /// with a single key.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys. The type parameter is deliberately unconstrained; <c>null</c> keys are rejected at runtime by the implementations.</typeparam>
    /// <typeparam name="TValue">The type of the values. The type parameter is deliberately unconstrained so that <c>null</c> values are supported.</typeparam>
    /// <remarks>
    /// <para>
    /// This is the read/write abstraction shared by the unordered and the ordered multimap
    /// implementations of this package (<see cref="MultiDictionary{TKey,TValue}"/> and
    /// <see cref="OrderedMultiDictionary{TKey,TValue}"/>). Code that only needs multimap semantics
    /// - and not the ordering or the per-key set algebra offered by the concrete types - can be
    /// written against this interface, which makes it possible to mock, replace or swap the
    /// implementation.
    /// </para>
    /// <para>
    /// <b>The inherited dictionary view.</b> This interface extends
    /// <see cref="IReadOnlyDictionary{TKey,TValue}"/> instantiated as
    /// <c>IReadOnlyDictionary&lt;TKey, IReadOnlyCollection&lt;TValue&gt;&gt;</c> - the same
    /// interface both implementations already declare. That instantiation is where the following
    /// members come from, and their meaning is <em>per key</em>, not per (key, value) pair:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The inherited <c>Count</c> is the number of <b>keys</b> (same as <see cref="KeyCount"/>), never the number of values.</description></item>
    /// <item><description><see cref="IReadOnlyDictionary{TKey,TValue}.Keys"/> is the sequence of keys.</description></item>
    /// <item><description>The indexer <c>this[key]</c> returns the values stored under the key as an <see cref="IReadOnlyCollection{TValue}"/>, or an empty collection (never <c>null</c>) when the key is absent.</description></item>
    /// <item><description><see cref="IReadOnlyDictionary{TKey,TValue}.ContainsKey"/> tests for a key only.</description></item>
    /// <item><description><see cref="IReadOnlyDictionary{TKey,TValue}.TryGetValue"/> yields the per-key collection; <c>false</c> means the key is absent and the out parameter is <c>null</c>.</description></item>
    /// <item><description>Enumerating the interface yields one <c>KeyValuePair&lt;TKey, IReadOnlyCollection&lt;TValue&gt;&gt;</c> <b>per key</b>.</description></item>
    /// </list>
    /// <para>
    /// <b>Two enumeration shapes.</b> Note that the inherited enumeration is grouped by key, whereas
    /// the concrete types enumerate one <c>KeyValuePair&lt;TKey, TValue&gt;</c> <em>per value</em>
    /// (their own <c>GetEnumerator</c> is public and therefore wins over the inherited one). To avoid
    /// a silent change of meaning when code is re-typed from a concrete type to this interface, the
    /// flat sequence is exposed here explicitly as <see cref="Values"/>; the grouped form remains
    /// reachable through the inherited members listed above. For the same reason this interface
    /// deliberately does <em>not</em> re-declare <c>IEnumerable&lt;KeyValuePair&lt;TKey, TValue&gt;&gt;</c>:
    /// inheriting two enumerable shapes would make <c>foreach</c> over the interface ambiguous.
    /// </para>
    /// <para>
    /// <b>Scope.</b> The member set is intentionally minimal: it contains only the semantics that
    /// every multimap implementation can honour. Per-key set algebra (<c>UnionWith</c>,
    /// <c>IntersectionWith</c>, <c>ExceptWith</c>, <c>SymmetricExceptWith</c>) and the
    /// <c>EntrySet</c> projection are <em>not</em> part of this contract: the concurrent and
    /// immutable variants do not offer the algebra at all, and <c>EntrySet</c> is not available on
    /// every implementation (for example <see cref="MultiDictionary{TKey,TValue}"/> does not expose
    /// it). Likewise the comparer properties are excluded, because the key comparer type differs
    /// between implementations. Those members remain available on the concrete types.
    /// </para>
    /// <para>
    /// <b>No empty keys.</b> Every implementation maintains the invariant that a key exists only
    /// while it has at least one value: removing the last value drops the key automatically, so
    /// <see cref="KeyCount"/> and <see cref="TotalValueCount"/> can never disagree about an empty
    /// key.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// void Dump(IMultiDictionary&lt;string, int&gt; map)
    /// {
    ///     Console.WriteLine(map.KeyCount + " keys, " + map.TotalValueCount + " values");
    ///     foreach (var key in map.Keys)
    ///     {
    ///         Console.WriteLine(key + ": " + string.Join(", ", map[key]));
    ///     }
    ///
    ///     foreach (var value in map.Values) { /* flat: every value of every key */ }
    /// }
    ///
    /// Dump(new MultiDictionary&lt;string, int&gt;());
    /// Dump(new OrderedMultiDictionary&lt;string, int&gt;());
    /// </code>
    /// </example>
    public interface IMultiDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>>
    {
        /// <summary>
        /// Gets the number of keys held by the multimap.
        /// </summary>
        /// <value>
        /// The number of keys that currently have at least one value. This is the same value that
        /// the inherited <c>Count</c> reports.
        /// </value>
        int KeyCount { get; }

        /// <summary>
        /// Gets the total number of (key, value) pairs held by the multimap.
        /// </summary>
        /// <value>
        /// The sum, over every key, of the number of values stored under that key.
        /// </value>
        int TotalValueCount { get; }

        /// <summary>
        /// Returns the number of values stored under the key.
        /// </summary>
        /// <param name="key">The key to inspect.</param>
        /// <returns>The number of values stored under the key, or zero when the key is absent.</returns>
        int ValueCount(TKey key);

        /// <summary>
        /// Gets all values of the multimap, flattened across keys.
        /// </summary>
        /// <value>
        /// A sequence yielding every value of every key; its length equals
        /// <see cref="TotalValueCount"/>. The enumeration order is unspecified for an unordered
        /// multimap and is the key order for an ordered one.
        /// </value>
        /// <remarks>
        /// This member deliberately hides the inherited
        /// <see cref="IReadOnlyDictionary{TKey,TValue}.Values"/>, which yields one collection
        /// <em>per key</em>. Hiding is required so that <c>map.Values</c> keeps the same meaning -
        /// the flat sequence - whether <c>map</c> is typed as a concrete multimap or as this
        /// interface.
        /// </remarks>
        new IEnumerable<TValue> Values { get; }

        /// <summary>
        /// Adds the value under the key.
        /// </summary>
        /// <param name="key">The key to add the value to.</param>
        /// <param name="value">The value to add. <c>null</c> is a valid value for reference types.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
        /// <remarks>
        /// When the implementation is configured to disallow duplicate values and the value is
        /// already stored under the key, the call is silently ignored (the multimap is treated as a
        /// mapping to a set for that key). Otherwise a duplicate value adds a second occurrence.
        /// </remarks>
        void Add(TKey key, TValue value);

        /// <summary>
        /// Adds each value of the specified collection under the key.
        /// </summary>
        /// <param name="key">The key to add the values to.</param>
        /// <param name="values">The values to add.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="values"/> is <c>null</c>.</exception>
        /// <remarks>
        /// Equivalent to calling <see cref="Add(TKey,TValue)"/> once per element of
        /// <paramref name="values"/>, so duplicates inside the argument are honoured.
        /// </remarks>
        void AddRange(TKey key, IEnumerable<TValue> values);

        /// <summary>
        /// Determines whether the specified value is stored under the key.
        /// </summary>
        /// <param name="key">The key to inspect.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns><c>true</c> when the value is stored under the key; otherwise <c>false</c>.</returns>
        bool Contains(TKey key, TValue value);

        /// <summary>
        /// Determines whether the specified value is stored under any key.
        /// </summary>
        /// <param name="value">The value to locate.</param>
        /// <returns><c>true</c> when the value is stored under at least one key; otherwise <c>false</c>.</returns>
        bool ContainsValue(TValue value);

        /// <summary>
        /// Removes the key together with all of its values.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns><c>true</c> when the key was present; otherwise <c>false</c>.</returns>
        bool Remove(TKey key);

        /// <summary>
        /// Removes a single occurrence of the value under the key.
        /// </summary>
        /// <param name="key">The key to remove the value from.</param>
        /// <param name="value">The value to remove.</param>
        /// <returns><c>true</c> when an occurrence was removed; otherwise <c>false</c>.</returns>
        /// <remarks>
        /// The key is dropped automatically once its last value is removed.
        /// </remarks>
        bool Remove(TKey key, TValue value);

        /// <summary>
        /// Removes one occurrence of each <em>distinct</em> value of the specified collection from
        /// the key.
        /// </summary>
        /// <param name="key">The key to remove the values from.</param>
        /// <param name="values">The values to remove. Treated as a set: a repeated value does not count twice.</param>
        /// <returns><c>true</c> when at least one value was removed; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is <c>null</c>.</exception>
        /// <remarks>
        /// This is the batch form of <see cref="Remove(TKey,TValue)"/>. A missing key, an empty
        /// collection, or a collection holding only values the key does not store is a no-op that
        /// returns <c>false</c>. The key is dropped automatically once its last value is removed.
        /// </remarks>
        bool RemoveRange(TKey key, IEnumerable<TValue> values);

        /// <summary>
        /// Removes all keys and values.
        /// </summary>
        void Clear();

        /// <summary>
        /// Returns a read-only <see cref="ILookup{TKey,TValue}"/> view of the multimap.
        /// </summary>
        /// <returns>A lookup in which a missing key yields an empty grouping.</returns>
        ILookup<TKey, TValue> AsLookup();
    }
}
