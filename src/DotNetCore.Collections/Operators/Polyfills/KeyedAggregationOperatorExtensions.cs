using System;
using System.Collections.Generic;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The keyed-aggregation operators of the P0 polyfill layer: <c>CountBy</c> and
    /// <c>AggregateBy</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These two are BCL operators from .NET 9 onwards, and the layer exists for the targets that
    /// predate it. The dispatch below is the four-source one the rest of the engine uses: the three
    /// contiguous arms take the engine's value-type path, and the arbitrary-sequence arm is the one
    /// that is allowed to fall back to the BCL.
    /// </para>
    /// <para>
    /// The fallback is a compile-time decision, not a run-time probe. On a target where the BCL
    /// already ships the operator the arbitrary-sequence arm hands the call straight to
    /// <c>System.Linq.Enumerable</c> and wraps the result, so the engine never re-implements
    /// semantics the platform already provides; on a target where it does not, that arm uses the
    /// engine's own implementation, which is the polyfill proper.
    /// </para>
    /// <para>
    /// The comparer is an optional parameter on both operators, which is the shape the BCL
    /// declares. <c>AggregateBy</c> has two overloads - a shared seed and a seed factory - and they
    /// are the two the BCL declares as well.
    /// </para>
    /// </remarks>
    public static partial class CollectionExtensions
    {
        // ----- CountBy -----

        /// <summary>Counts how often each key occurs in the array.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are counted by.</typeparam>
        /// <param name="source">The array to count.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="keyComparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of keys with their counts.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        public static CountByValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey> CountBy<T, TKey>(
            this ArrayValueEnumerable<T> source, Func<T, TKey> keySelector, IEqualityComparer<TKey>? keyComparer = null)
            where TKey : notnull
            => new CountByValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey>(source, keySelector, keyComparer);

        /// <summary>Counts how often each key occurs in the list.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are counted by.</typeparam>
        /// <param name="source">The list to count.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="keyComparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of keys with their counts.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        public static CountByValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey> CountBy<T, TKey>(
            this ListValueEnumerable<T> source, Func<T, TKey> keySelector, IEqualityComparer<TKey>? keyComparer = null)
            where TKey : notnull
            => new CountByValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey>(source, keySelector, keyComparer);

        /// <summary>Counts how often each key occurs in the read-only list.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are counted by.</typeparam>
        /// <param name="source">The read-only list to count.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="keyComparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of keys with their counts.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        public static CountByValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey> CountBy<T, TKey>(
            this ReadOnlyListValueEnumerable<T> source, Func<T, TKey> keySelector, IEqualityComparer<TKey>? keyComparer = null)
            where TKey : notnull
            => new CountByValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey>(source, keySelector, keyComparer);

        /// <summary>Counts how often each key occurs in the sequence.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are counted by.</typeparam>
        /// <param name="source">The sequence to count.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="keyComparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of keys with their counts.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<KeyValuePair<TKey, int>> CountBy<T, TKey>(
            this EnumerableValueEnumerable<T> source, Func<T, TKey> keySelector, IEqualityComparer<TKey>? keyComparer = null)
            where TKey : notnull
        {
            if (keySelector is null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

#if NET9_0_OR_GREATER
            return new EnumerableValueEnumerable<KeyValuePair<TKey, int>>(System.Linq.Enumerable.CountBy(source, keySelector, keyComparer));
#else
            return new EnumerableValueEnumerable<KeyValuePair<TKey, int>>(
                new CountByValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TKey>(source, keySelector, keyComparer));
#endif
        }

        // ----- AggregateBy (shared seed) -----

        /// <summary>Folds every element of the array into a per-key accumulator that starts from
        /// <paramref name="seed"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are grouped by.</typeparam>
        /// <typeparam name="TAccumulate">The type of the per-key accumulator.</typeparam>
        /// <param name="source">The array to aggregate.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="seed">The initial accumulator of every key.</param>
        /// <param name="func">The fold to apply to an accumulator and the next element.</param>
        /// <param name="keyComparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of keys with their accumulators.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or
        /// <paramref name="func"/> is <see langword="null"/>.</exception>
        public static AggregateByValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey, TAccumulate> AggregateBy<T, TKey, TAccumulate>(
            this ArrayValueEnumerable<T> source, Func<T, TKey> keySelector, TAccumulate seed, Func<TAccumulate, T, TAccumulate> func, IEqualityComparer<TKey>? keyComparer = null)
            where TKey : notnull
            => new AggregateByValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey, TAccumulate>(source, keySelector, seed, func, keyComparer);

        /// <summary>Folds every element of the list into a per-key accumulator that starts from
        /// <paramref name="seed"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are grouped by.</typeparam>
        /// <typeparam name="TAccumulate">The type of the per-key accumulator.</typeparam>
        /// <param name="source">The list to aggregate.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="seed">The initial accumulator of every key.</param>
        /// <param name="func">The fold to apply to an accumulator and the next element.</param>
        /// <param name="keyComparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of keys with their accumulators.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or
        /// <paramref name="func"/> is <see langword="null"/>.</exception>
        public static AggregateByValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey, TAccumulate> AggregateBy<T, TKey, TAccumulate>(
            this ListValueEnumerable<T> source, Func<T, TKey> keySelector, TAccumulate seed, Func<TAccumulate, T, TAccumulate> func, IEqualityComparer<TKey>? keyComparer = null)
            where TKey : notnull
            => new AggregateByValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey, TAccumulate>(source, keySelector, seed, func, keyComparer);

        /// <summary>Folds every element of the read-only list into a per-key accumulator that starts
        /// from <paramref name="seed"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are grouped by.</typeparam>
        /// <typeparam name="TAccumulate">The type of the per-key accumulator.</typeparam>
        /// <param name="source">The read-only list to aggregate.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="seed">The initial accumulator of every key.</param>
        /// <param name="func">The fold to apply to an accumulator and the next element.</param>
        /// <param name="keyComparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of keys with their accumulators.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or
        /// <paramref name="func"/> is <see langword="null"/>.</exception>
        public static AggregateByValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey, TAccumulate> AggregateBy<T, TKey, TAccumulate>(
            this ReadOnlyListValueEnumerable<T> source, Func<T, TKey> keySelector, TAccumulate seed, Func<TAccumulate, T, TAccumulate> func, IEqualityComparer<TKey>? keyComparer = null)
            where TKey : notnull
            => new AggregateByValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey, TAccumulate>(source, keySelector, seed, func, keyComparer);

        /// <summary>Folds every element of the sequence into a per-key accumulator that starts from
        /// <paramref name="seed"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are grouped by.</typeparam>
        /// <typeparam name="TAccumulate">The type of the per-key accumulator.</typeparam>
        /// <param name="source">The sequence to aggregate.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="seed">The initial accumulator of every key.</param>
        /// <param name="func">The fold to apply to an accumulator and the next element.</param>
        /// <param name="keyComparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of keys with their accumulators.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> or
        /// <paramref name="func"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<T, TKey, TAccumulate>(
            this EnumerableValueEnumerable<T> source, Func<T, TKey> keySelector, TAccumulate seed, Func<TAccumulate, T, TAccumulate> func, IEqualityComparer<TKey>? keyComparer = null)
            where TKey : notnull
        {
            if (keySelector is null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            if (func is null)
            {
                throw new ArgumentNullException(nameof(func));
            }

#if NET9_0_OR_GREATER
            return new EnumerableValueEnumerable<KeyValuePair<TKey, TAccumulate>>(System.Linq.Enumerable.AggregateBy(source, keySelector, seed, func, keyComparer));
#else
            return new EnumerableValueEnumerable<KeyValuePair<TKey, TAccumulate>>(
                new AggregateByValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TKey, TAccumulate>(source, keySelector, seed, func, keyComparer));
#endif
        }

        // ----- AggregateBy (seed factory) -----

        /// <summary>Folds every element of the array into a per-key accumulator that starts from a
        /// seed the key produces.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are grouped by.</typeparam>
        /// <typeparam name="TAccumulate">The type of the per-key accumulator.</typeparam>
        /// <param name="source">The array to aggregate.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="seedFactory">The projection of a key to the initial accumulator of that key.</param>
        /// <param name="func">The fold to apply to an accumulator and the next element.</param>
        /// <param name="keyComparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of keys with their accumulators.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/>,
        /// <paramref name="seedFactory"/> or <paramref name="func"/> is <see langword="null"/>.</exception>
        public static AggregateByValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey, TAccumulate> AggregateBy<T, TKey, TAccumulate>(
            this ArrayValueEnumerable<T> source, Func<T, TKey> keySelector, Func<TKey, TAccumulate> seedFactory, Func<TAccumulate, T, TAccumulate> func, IEqualityComparer<TKey>? keyComparer = null)
            where TKey : notnull
            => new AggregateByValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey, TAccumulate>(source, keySelector, seedFactory, func, keyComparer);

        /// <summary>Folds every element of the list into a per-key accumulator that starts from a
        /// seed the key produces.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are grouped by.</typeparam>
        /// <typeparam name="TAccumulate">The type of the per-key accumulator.</typeparam>
        /// <param name="source">The list to aggregate.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="seedFactory">The projection of a key to the initial accumulator of that key.</param>
        /// <param name="func">The fold to apply to an accumulator and the next element.</param>
        /// <param name="keyComparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of keys with their accumulators.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/>,
        /// <paramref name="seedFactory"/> or <paramref name="func"/> is <see langword="null"/>.</exception>
        public static AggregateByValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey, TAccumulate> AggregateBy<T, TKey, TAccumulate>(
            this ListValueEnumerable<T> source, Func<T, TKey> keySelector, Func<TKey, TAccumulate> seedFactory, Func<TAccumulate, T, TAccumulate> func, IEqualityComparer<TKey>? keyComparer = null)
            where TKey : notnull
            => new AggregateByValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey, TAccumulate>(source, keySelector, seedFactory, func, keyComparer);

        /// <summary>Folds every element of the read-only list into a per-key accumulator that starts
        /// from a seed the key produces.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are grouped by.</typeparam>
        /// <typeparam name="TAccumulate">The type of the per-key accumulator.</typeparam>
        /// <param name="source">The read-only list to aggregate.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="seedFactory">The projection of a key to the initial accumulator of that key.</param>
        /// <param name="func">The fold to apply to an accumulator and the next element.</param>
        /// <param name="keyComparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of keys with their accumulators.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/>,
        /// <paramref name="seedFactory"/> or <paramref name="func"/> is <see langword="null"/>.</exception>
        public static AggregateByValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey, TAccumulate> AggregateBy<T, TKey, TAccumulate>(
            this ReadOnlyListValueEnumerable<T> source, Func<T, TKey> keySelector, Func<TKey, TAccumulate> seedFactory, Func<TAccumulate, T, TAccumulate> func, IEqualityComparer<TKey>? keyComparer = null)
            where TKey : notnull
            => new AggregateByValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey, TAccumulate>(source, keySelector, seedFactory, func, keyComparer);

        /// <summary>Folds every element of the sequence into a per-key accumulator that starts from a
        /// seed the key produces.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are grouped by.</typeparam>
        /// <typeparam name="TAccumulate">The type of the per-key accumulator.</typeparam>
        /// <param name="source">The sequence to aggregate.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="seedFactory">The projection of a key to the initial accumulator of that key.</param>
        /// <param name="func">The fold to apply to an accumulator and the next element.</param>
        /// <param name="keyComparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of keys with their accumulators.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/>,
        /// <paramref name="seedFactory"/> or <paramref name="func"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<T, TKey, TAccumulate>(
            this EnumerableValueEnumerable<T> source, Func<T, TKey> keySelector, Func<TKey, TAccumulate> seedFactory, Func<TAccumulate, T, TAccumulate> func, IEqualityComparer<TKey>? keyComparer = null)
            where TKey : notnull
        {
            if (keySelector is null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            if (seedFactory is null)
            {
                throw new ArgumentNullException(nameof(seedFactory));
            }

            if (func is null)
            {
                throw new ArgumentNullException(nameof(func));
            }

#if NET9_0_OR_GREATER
            return new EnumerableValueEnumerable<KeyValuePair<TKey, TAccumulate>>(System.Linq.Enumerable.AggregateBy(source, keySelector, seedFactory, func, keyComparer));
#else
            return new EnumerableValueEnumerable<KeyValuePair<TKey, TAccumulate>>(
                new AggregateByValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TKey, TAccumulate>(source, keySelector, seedFactory, func, keyComparer));
#endif
        }
    }
}
