using System;
using System.Collections.Generic;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The P1 remaining operators of the engine: <c>DistinctUntilChanged</c>, <c>Memoize</c>,
    /// <c>FallbackIfEmpty</c>, <c>AtLeast</c> / <c>Exactly</c> / <c>CountBetween</c>,
    /// <c>PartialSortBy</c> and <c>Choose</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// None of these operators is in the BCL, so there is no high-TFM fallback to
    /// <c>System.Linq.Enumerable</c>. The four-source dispatch is the same shape the rest of the
    /// engine uses: the three contiguous arms take the value-type path, and the arbitrary-sequence
    /// arm is the one that wraps the result back into an <see cref="EnumerableValueEnumerable{T}"/>.
    /// </para>
    /// </remarks>
    public static partial class CollectionExtensions
    {
        // ----- DistinctUntilChanged -----

        /// <summary>Removes consecutive duplicates from the array.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The array to deduplicate.</param>
        /// <returns>The sequence with consecutive duplicates removed.</returns>
        public static DistinctUntilChangedValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T> DistinctUntilChanged<T>(
            this ArrayValueEnumerable<T> source)
            => new DistinctUntilChangedValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, null);

        /// <summary>Removes consecutive duplicates from the list.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The list to deduplicate.</param>
        /// <returns>The sequence with consecutive duplicates removed.</returns>
        public static DistinctUntilChangedValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T> DistinctUntilChanged<T>(
            this ListValueEnumerable<T> source)
            => new DistinctUntilChangedValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, null);

        /// <summary>Removes consecutive duplicates from the read-only list.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The read-only list to deduplicate.</param>
        /// <returns>The sequence with consecutive duplicates removed.</returns>
        public static DistinctUntilChangedValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T> DistinctUntilChanged<T>(
            this ReadOnlyListValueEnumerable<T> source)
            => new DistinctUntilChangedValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, null);

        /// <summary>Removes consecutive duplicates from the sequence.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to deduplicate.</param>
        /// <returns>The sequence with consecutive duplicates removed.</returns>
        public static DistinctUntilChangedValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T> DistinctUntilChanged<T>(
            this EnumerableValueEnumerable<T> source)
            => new DistinctUntilChangedValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, null);

        /// <summary>Removes consecutive duplicates from the array, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The array to deduplicate.</param>
        /// <param name="comparer">The comparer of elements, or <see langword="null"/> for the
        /// default one.</param>
        /// <returns>The sequence with consecutive duplicates removed.</returns>
        public static DistinctUntilChangedValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T> DistinctUntilChanged<T>(
            this ArrayValueEnumerable<T> source, IEqualityComparer<T>? comparer)
            => new DistinctUntilChangedValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, comparer);

        /// <summary>Removes consecutive duplicates from the list, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The list to deduplicate.</param>
        /// <param name="comparer">The comparer of elements, or <see langword="null"/> for the
        /// default one.</param>
        /// <returns>The sequence with consecutive duplicates removed.</returns>
        public static DistinctUntilChangedValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T> DistinctUntilChanged<T>(
            this ListValueEnumerable<T> source, IEqualityComparer<T>? comparer)
            => new DistinctUntilChangedValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, comparer);

        /// <summary>Removes consecutive duplicates from the read-only list, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The read-only list to deduplicate.</param>
        /// <param name="comparer">The comparer of elements, or <see langword="null"/> for the
        /// default one.</param>
        /// <returns>The sequence with consecutive duplicates removed.</returns>
        public static DistinctUntilChangedValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T> DistinctUntilChanged<T>(
            this ReadOnlyListValueEnumerable<T> source, IEqualityComparer<T>? comparer)
            => new DistinctUntilChangedValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, comparer);

        /// <summary>Removes consecutive duplicates from the sequence, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to deduplicate.</param>
        /// <param name="comparer">The comparer of elements, or <see langword="null"/> for the
        /// default one.</param>
        /// <returns>The sequence with consecutive duplicates removed.</returns>
        public static DistinctUntilChangedValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T> DistinctUntilChanged<T>(
            this EnumerableValueEnumerable<T> source, IEqualityComparer<T>? comparer)
            => new DistinctUntilChangedValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, comparer);

        // ----- Memoize -----

        /// <summary>Caches the array's elements so that subsequent enumerations replay the cache
        /// instead of re-running the source.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The array to memoize.</param>
        /// <returns>A memoized view of the array.</returns>
        public static MemoizedValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T> Memoize<T>(
            this ArrayValueEnumerable<T> source)
            => new MemoizedValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source);

        /// <summary>Caches the list's elements so that subsequent enumerations replay the cache
        /// instead of re-running the source.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The list to memoize.</param>
        /// <returns>A memoized view of the list.</returns>
        public static MemoizedValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T> Memoize<T>(
            this ListValueEnumerable<T> source)
            => new MemoizedValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source);

        /// <summary>Caches the read-only list's elements so that subsequent enumerations replay the
        /// cache instead of re-running the source.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The read-only list to memoize.</param>
        /// <returns>A memoized view of the read-only list.</returns>
        public static MemoizedValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T> Memoize<T>(
            this ReadOnlyListValueEnumerable<T> source)
            => new MemoizedValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source);

        /// <summary>Caches the sequence's elements so that subsequent enumerations replay the cache
        /// instead of re-running the source.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to memoize.</param>
        /// <returns>A memoized view of the sequence.</returns>
        public static MemoizedValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T> Memoize<T>(
            this EnumerableValueEnumerable<T> source)
            => new MemoizedValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source);

        // ----- FallbackIfEmpty (single value) -----

        /// <summary>Yields the array's elements, or <paramref name="fallback"/> when the array is
        /// empty.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The array.</param>
        /// <param name="fallback">The value to yield when the source is empty.</param>
        /// <returns>The source, or the fallback.</returns>
        public static FallbackIfEmptyValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T> FallbackIfEmpty<T>(
            this ArrayValueEnumerable<T> source, T fallback)
            => new FallbackIfEmptyValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, fallback);

        /// <summary>Yields the list's elements, or <paramref name="fallback"/> when the list is
        /// empty.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The list.</param>
        /// <param name="fallback">The value to yield when the source is empty.</param>
        /// <returns>The source, or the fallback.</returns>
        public static FallbackIfEmptyValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T> FallbackIfEmpty<T>(
            this ListValueEnumerable<T> source, T fallback)
            => new FallbackIfEmptyValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, fallback);

        /// <summary>Yields the read-only list's elements, or <paramref name="fallback"/> when the
        /// list is empty.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The read-only list.</param>
        /// <param name="fallback">The value to yield when the source is empty.</param>
        /// <returns>The source, or the fallback.</returns>
        public static FallbackIfEmptyValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T> FallbackIfEmpty<T>(
            this ReadOnlyListValueEnumerable<T> source, T fallback)
            => new FallbackIfEmptyValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, fallback);

        /// <summary>Yields the sequence's elements, or <paramref name="fallback"/> when the
        /// sequence is empty.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence.</param>
        /// <param name="fallback">The value to yield when the source is empty.</param>
        /// <returns>The source, or the fallback.</returns>
        public static FallbackIfEmptyValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T> FallbackIfEmpty<T>(
            this EnumerableValueEnumerable<T> source, T fallback)
            => new FallbackIfEmptyValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, fallback);

        // ----- FallbackIfEmpty (sequence) -----

        /// <summary>Yields the array's elements, or <paramref name="fallback"/> when the array is
        /// empty.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The array.</param>
        /// <param name="fallback">The sequence to yield when the source is empty.</param>
        /// <returns>The source, or the fallback.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fallback"/> is
        /// <see langword="null"/>.</exception>
        public static FallbackIfEmptyValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T> FallbackIfEmpty<T>(
            this ArrayValueEnumerable<T> source, IEnumerable<T> fallback)
        {
            if (fallback is null) throw new ArgumentNullException(nameof(fallback));
            return new FallbackIfEmptyValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, fallback);
        }

        /// <summary>Yields the list's elements, or <paramref name="fallback"/> when the list is
        /// empty.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The list.</param>
        /// <param name="fallback">The sequence to yield when the source is empty.</param>
        /// <returns>The source, or the fallback.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fallback"/> is
        /// <see langword="null"/>.</exception>
        public static FallbackIfEmptyValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T> FallbackIfEmpty<T>(
            this ListValueEnumerable<T> source, IEnumerable<T> fallback)
        {
            if (fallback is null) throw new ArgumentNullException(nameof(fallback));
            return new FallbackIfEmptyValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, fallback);
        }

        /// <summary>Yields the read-only list's elements, or <paramref name="fallback"/> when the
        /// list is empty.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The read-only list.</param>
        /// <param name="fallback">The sequence to yield when the source is empty.</param>
        /// <returns>The source, or the fallback.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fallback"/> is
        /// <see langword="null"/>.</exception>
        public static FallbackIfEmptyValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T> FallbackIfEmpty<T>(
            this ReadOnlyListValueEnumerable<T> source, IEnumerable<T> fallback)
        {
            if (fallback is null) throw new ArgumentNullException(nameof(fallback));
            return new FallbackIfEmptyValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, fallback);
        }

        /// <summary>Yields the sequence's elements, or <paramref name="fallback"/> when the
        /// sequence is empty.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence.</param>
        /// <param name="fallback">The sequence to yield when the source is empty.</param>
        /// <returns>The source, or the fallback.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fallback"/> is
        /// <see langword="null"/>.</exception>
        public static FallbackIfEmptyValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T> FallbackIfEmpty<T>(
            this EnumerableValueEnumerable<T> source, IEnumerable<T> fallback)
        {
            if (fallback is null) throw new ArgumentNullException(nameof(fallback));
            return new FallbackIfEmptyValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, fallback);
        }

        // ----- AtLeast -----

        /// <summary>Determines whether the array has at least <paramref name="count"/>
        /// elements.</summary>
        /// <param name="source">The array to count.</param>
        /// <param name="count">The minimum number of elements required.</param>
        /// <returns><see langword="true"/> when the source has at least <paramref name="count"/>
        /// elements.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is
        /// negative.</exception>
        public static bool AtLeast<T>(this ArrayValueEnumerable<T> source, int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            return CountPredicateCore.AtLeast<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, count);
        }

        /// <summary>Determines whether the list has at least <paramref name="count"/>
        /// elements.</summary>
        /// <param name="source">The list to count.</param>
        /// <param name="count">The minimum number of elements required.</param>
        /// <returns><see langword="true"/> when the source has at least <paramref name="count"/>
        /// elements.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is
        /// negative.</exception>
        public static bool AtLeast<T>(this ListValueEnumerable<T> source, int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            return CountPredicateCore.AtLeast<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, count);
        }

        /// <summary>Determines whether the read-only list has at least <paramref name="count"/>
        /// elements.</summary>
        /// <param name="source">The read-only list to count.</param>
        /// <param name="count">The minimum number of elements required.</param>
        /// <returns><see langword="true"/> when the source has at least <paramref name="count"/>
        /// elements.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is
        /// negative.</exception>
        public static bool AtLeast<T>(this ReadOnlyListValueEnumerable<T> source, int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            return CountPredicateCore.AtLeast<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, count);
        }

        /// <summary>Determines whether the sequence has at least <paramref name="count"/>
        /// elements.</summary>
        /// <param name="source">The sequence to count.</param>
        /// <param name="count">The minimum number of elements required.</param>
        /// <returns><see langword="true"/> when the source has at least <paramref name="count"/>
        /// elements.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is
        /// negative.</exception>
        public static bool AtLeast<T>(this EnumerableValueEnumerable<T> source, int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            return CountPredicateCore.AtLeast<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, count);
        }

        // ----- Exactly -----

        /// <summary>Determines whether the array has exactly <paramref name="count"/>
        /// elements.</summary>
        /// <param name="source">The array to count.</param>
        /// <param name="count">The exact number of elements required.</param>
        /// <returns><see langword="true"/> when the source has exactly <paramref name="count"/>
        /// elements.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is
        /// negative.</exception>
        public static bool Exactly<T>(this ArrayValueEnumerable<T> source, int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            return CountPredicateCore.Exactly<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, count);
        }

        /// <summary>Determines whether the list has exactly <paramref name="count"/>
        /// elements.</summary>
        /// <param name="source">The list to count.</param>
        /// <param name="count">The exact number of elements required.</param>
        /// <returns><see langword="true"/> when the source has exactly <paramref name="count"/>
        /// elements.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is
        /// negative.</exception>
        public static bool Exactly<T>(this ListValueEnumerable<T> source, int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            return CountPredicateCore.Exactly<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, count);
        }

        /// <summary>Determines whether the read-only list has exactly <paramref name="count"/>
        /// elements.</summary>
        /// <param name="source">The read-only list to count.</param>
        /// <param name="count">The exact number of elements required.</param>
        /// <returns><see langword="true"/> when the source has exactly <paramref name="count"/>
        /// elements.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is
        /// negative.</exception>
        public static bool Exactly<T>(this ReadOnlyListValueEnumerable<T> source, int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            return CountPredicateCore.Exactly<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, count);
        }

        /// <summary>Determines whether the sequence has exactly <paramref name="count"/>
        /// elements.</summary>
        /// <param name="source">The sequence to count.</param>
        /// <param name="count">The exact number of elements required.</param>
        /// <returns><see langword="true"/> when the source has exactly <paramref name="count"/>
        /// elements.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is
        /// negative.</exception>
        public static bool Exactly<T>(this EnumerableValueEnumerable<T> source, int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            return CountPredicateCore.Exactly<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, count);
        }

        // ----- CountBetween -----

        /// <summary>Determines whether the array's element count is between
        /// <paramref name="min"/> and <paramref name="max"/> (inclusive).</summary>
        /// <param name="source">The array to count.</param>
        /// <param name="min">The minimum count (inclusive).</param>
        /// <param name="max">The maximum count (inclusive).</param>
        /// <returns><see langword="true"/> when the source count is in
        /// [<paramref name="min"/>, <paramref name="max"/>].</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="min"/> is negative or
        /// <paramref name="max"/> is less than <paramref name="min"/>.</exception>
        public static bool CountBetween<T>(this ArrayValueEnumerable<T> source, int min, int max)
        {
            if (min < 0) throw new ArgumentOutOfRangeException(nameof(min));
            if (max < min) throw new ArgumentOutOfRangeException(nameof(max));
            return CountPredicateCore.CountBetween<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, min, max);
        }

        /// <summary>Determines whether the list's element count is between
        /// <paramref name="min"/> and <paramref name="max"/> (inclusive).</summary>
        /// <param name="source">The list to count.</param>
        /// <param name="min">The minimum count (inclusive).</param>
        /// <param name="max">The maximum count (inclusive).</param>
        /// <returns><see langword="true"/> when the source count is in
        /// [<paramref name="min"/>, <paramref name="max"/>].</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="min"/> is negative or
        /// <paramref name="max"/> is less than <paramref name="min"/>.</exception>
        public static bool CountBetween<T>(this ListValueEnumerable<T> source, int min, int max)
        {
            if (min < 0) throw new ArgumentOutOfRangeException(nameof(min));
            if (max < min) throw new ArgumentOutOfRangeException(nameof(max));
            return CountPredicateCore.CountBetween<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, min, max);
        }

        /// <summary>Determines whether the read-only list's element count is between
        /// <paramref name="min"/> and <paramref name="max"/> (inclusive).</summary>
        /// <param name="source">The read-only list to count.</param>
        /// <param name="min">The minimum count (inclusive).</param>
        /// <param name="max">The maximum count (inclusive).</param>
        /// <returns><see langword="true"/> when the source count is in
        /// [<paramref name="min"/>, <paramref name="max"/>].</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="min"/> is negative or
        /// <paramref name="max"/> is less than <paramref name="min"/>.</exception>
        public static bool CountBetween<T>(this ReadOnlyListValueEnumerable<T> source, int min, int max)
        {
            if (min < 0) throw new ArgumentOutOfRangeException(nameof(min));
            if (max < min) throw new ArgumentOutOfRangeException(nameof(max));
            return CountPredicateCore.CountBetween<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, min, max);
        }

        /// <summary>Determines whether the sequence's element count is between
        /// <paramref name="min"/> and <paramref name="max"/> (inclusive).</summary>
        /// <param name="source">The sequence to count.</param>
        /// <param name="min">The minimum count (inclusive).</param>
        /// <param name="max">The maximum count (inclusive).</param>
        /// <returns><see langword="true"/> when the source count is in
        /// [<paramref name="min"/>, <paramref name="max"/>].</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="min"/> is negative or
        /// <paramref name="max"/> is less than <paramref name="min"/>.</exception>
        public static bool CountBetween<T>(this EnumerableValueEnumerable<T> source, int min, int max)
        {
            if (min < 0) throw new ArgumentOutOfRangeException(nameof(min));
            if (max < min) throw new ArgumentOutOfRangeException(nameof(max));
            return CountPredicateCore.CountBetween<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, min, max);
        }

        // ----- PartialSortBy -----

        /// <summary>Returns the top <paramref name="count"/> elements of the array, sorted
        /// ascending by key.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the sort key.</typeparam>
        /// <param name="source">The array to sort partially.</param>
        /// <param name="count">The number of top elements to return.</param>
        /// <param name="keySelector">The projection of an element to its sort key.</param>
        /// <returns>A list of the top <paramref name="count"/> elements, sorted ascending by
        /// key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is
        /// negative.</exception>
        public static List<T> PartialSortBy<T, TKey>(
            this ArrayValueEnumerable<T> source, int count, Func<T, TKey> keySelector)
        {
            if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            return PartialSortByCore.Execute<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey>(
                source, count, keySelector, null, PartialSortByCore.SortDirection.Ascending);
        }

        /// <summary>Returns the top <paramref name="count"/> elements of the list, sorted
        /// ascending by key.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the sort key.</typeparam>
        /// <param name="source">The list to sort partially.</param>
        /// <param name="count">The number of top elements to return.</param>
        /// <param name="keySelector">The projection of an element to its sort key.</param>
        /// <returns>A list of the top <paramref name="count"/> elements, sorted ascending by
        /// key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is
        /// negative.</exception>
        public static List<T> PartialSortBy<T, TKey>(
            this ListValueEnumerable<T> source, int count, Func<T, TKey> keySelector)
        {
            if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            return PartialSortByCore.Execute<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey>(
                source, count, keySelector, null, PartialSortByCore.SortDirection.Ascending);
        }

        /// <summary>Returns the top <paramref name="count"/> elements of the read-only list,
        /// sorted ascending by key.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the sort key.</typeparam>
        /// <param name="source">The read-only list to sort partially.</param>
        /// <param name="count">The number of top elements to return.</param>
        /// <param name="keySelector">The projection of an element to its sort key.</param>
        /// <returns>A list of the top <paramref name="count"/> elements, sorted ascending by
        /// key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is
        /// negative.</exception>
        public static List<T> PartialSortBy<T, TKey>(
            this ReadOnlyListValueEnumerable<T> source, int count, Func<T, TKey> keySelector)
        {
            if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            return PartialSortByCore.Execute<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey>(
                source, count, keySelector, null, PartialSortByCore.SortDirection.Ascending);
        }

        /// <summary>Returns the top <paramref name="count"/> elements of the sequence, sorted
        /// ascending by key.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the sort key.</typeparam>
        /// <param name="source">The sequence to sort partially.</param>
        /// <param name="count">The number of top elements to return.</param>
        /// <param name="keySelector">The projection of an element to its sort key.</param>
        /// <returns>A list of the top <paramref name="count"/> elements, sorted ascending by
        /// key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is
        /// negative.</exception>
        public static List<T> PartialSortBy<T, TKey>(
            this EnumerableValueEnumerable<T> source, int count, Func<T, TKey> keySelector)
        {
            if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            return PartialSortByCore.Execute<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TKey>(
                source, count, keySelector, null, PartialSortByCore.SortDirection.Ascending);
        }

        /// <summary>Returns the top <paramref name="count"/> elements of the array, sorted by key
        /// using <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the sort key.</typeparam>
        /// <param name="source">The array to sort partially.</param>
        /// <param name="count">The number of top elements to return.</param>
        /// <param name="keySelector">The projection of an element to its sort key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default
        /// one.</param>
        /// <returns>A list of the top <paramref name="count"/> elements, sorted by key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is
        /// negative.</exception>
        public static List<T> PartialSortBy<T, TKey>(
            this ArrayValueEnumerable<T> source, int count, Func<T, TKey> keySelector, IComparer<TKey>? comparer)
        {
            if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            return PartialSortByCore.Execute<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey>(
                source, count, keySelector, comparer, PartialSortByCore.SortDirection.Ascending);
        }

        /// <summary>Returns the top <paramref name="count"/> elements of the list, sorted by key
        /// using <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the sort key.</typeparam>
        /// <param name="source">The list to sort partially.</param>
        /// <param name="count">The number of top elements to return.</param>
        /// <param name="keySelector">The projection of an element to its sort key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default
        /// one.</param>
        /// <returns>A list of the top <paramref name="count"/> elements, sorted by key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is
        /// negative.</exception>
        public static List<T> PartialSortBy<T, TKey>(
            this ListValueEnumerable<T> source, int count, Func<T, TKey> keySelector, IComparer<TKey>? comparer)
        {
            if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            return PartialSortByCore.Execute<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey>(
                source, count, keySelector, comparer, PartialSortByCore.SortDirection.Ascending);
        }

        /// <summary>Returns the top <paramref name="count"/> elements of the read-only list,
        /// sorted by key using <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the sort key.</typeparam>
        /// <param name="source">The read-only list to sort partially.</param>
        /// <param name="count">The number of top elements to return.</param>
        /// <param name="keySelector">The projection of an element to its sort key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default
        /// one.</param>
        /// <returns>A list of the top <paramref name="count"/> elements, sorted by key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is
        /// negative.</exception>
        public static List<T> PartialSortBy<T, TKey>(
            this ReadOnlyListValueEnumerable<T> source, int count, Func<T, TKey> keySelector, IComparer<TKey>? comparer)
        {
            if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            return PartialSortByCore.Execute<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey>(
                source, count, keySelector, comparer, PartialSortByCore.SortDirection.Ascending);
        }

        /// <summary>Returns the top <paramref name="count"/> elements of the sequence, sorted by
        /// key using <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the sort key.</typeparam>
        /// <param name="source">The sequence to sort partially.</param>
        /// <param name="count">The number of top elements to return.</param>
        /// <param name="keySelector">The projection of an element to its sort key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default
        /// one.</param>
        /// <returns>A list of the top <paramref name="count"/> elements, sorted by key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is
        /// negative.</exception>
        public static List<T> PartialSortBy<T, TKey>(
            this EnumerableValueEnumerable<T> source, int count, Func<T, TKey> keySelector, IComparer<TKey>? comparer)
        {
            if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            return PartialSortByCore.Execute<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TKey>(
                source, count, keySelector, comparer, PartialSortByCore.SortDirection.Ascending);
        }

        /// <summary>Returns the top <paramref name="count"/> elements of the array, sorted by key
        /// in the specified direction.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the sort key.</typeparam>
        /// <param name="source">The array to sort partially.</param>
        /// <param name="count">The number of top elements to return.</param>
        /// <param name="keySelector">The projection of an element to its sort key.</param>
        /// <param name="descending"><see langword="true"/> to sort descending;
        /// <see langword="false"/> to sort ascending.</param>
        /// <returns>A list of the top <paramref name="count"/> elements, sorted by key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is
        /// negative.</exception>
        public static List<T> PartialSortBy<T, TKey>(
            this ArrayValueEnumerable<T> source, int count, Func<T, TKey> keySelector, bool descending)
        {
            if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            var direction = descending
                ? PartialSortByCore.SortDirection.Descending
                : PartialSortByCore.SortDirection.Ascending;
            return PartialSortByCore.Execute<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey>(
                source, count, keySelector, null, direction);
        }

        /// <summary>Returns the top <paramref name="count"/> elements of the list, sorted by key
        /// in the specified direction.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the sort key.</typeparam>
        /// <param name="source">The list to sort partially.</param>
        /// <param name="count">The number of top elements to return.</param>
        /// <param name="keySelector">The projection of an element to its sort key.</param>
        /// <param name="descending"><see langword="true"/> to sort descending;
        /// <see langword="false"/> to sort ascending.</param>
        /// <returns>A list of the top <paramref name="count"/> elements, sorted by key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is
        /// negative.</exception>
        public static List<T> PartialSortBy<T, TKey>(
            this ListValueEnumerable<T> source, int count, Func<T, TKey> keySelector, bool descending)
        {
            if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            var direction = descending
                ? PartialSortByCore.SortDirection.Descending
                : PartialSortByCore.SortDirection.Ascending;
            return PartialSortByCore.Execute<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey>(
                source, count, keySelector, null, direction);
        }

        /// <summary>Returns the top <paramref name="count"/> elements of the read-only list,
        /// sorted by key in the specified direction.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the sort key.</typeparam>
        /// <param name="source">The read-only list to sort partially.</param>
        /// <param name="count">The number of top elements to return.</param>
        /// <param name="keySelector">The projection of an element to its sort key.</param>
        /// <param name="descending"><see langword="true"/> to sort descending;
        /// <see langword="false"/> to sort ascending.</param>
        /// <returns>A list of the top <paramref name="count"/> elements, sorted by key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is
        /// negative.</exception>
        public static List<T> PartialSortBy<T, TKey>(
            this ReadOnlyListValueEnumerable<T> source, int count, Func<T, TKey> keySelector, bool descending)
        {
            if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            var direction = descending
                ? PartialSortByCore.SortDirection.Descending
                : PartialSortByCore.SortDirection.Ascending;
            return PartialSortByCore.Execute<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey>(
                source, count, keySelector, null, direction);
        }

        /// <summary>Returns the top <paramref name="count"/> elements of the sequence, sorted by
        /// key in the specified direction.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the sort key.</typeparam>
        /// <param name="source">The sequence to sort partially.</param>
        /// <param name="count">The number of top elements to return.</param>
        /// <param name="keySelector">The projection of an element to its sort key.</param>
        /// <param name="descending"><see langword="true"/> to sort descending;
        /// <see langword="false"/> to sort ascending.</param>
        /// <returns>A list of the top <paramref name="count"/> elements, sorted by key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is
        /// negative.</exception>
        public static List<T> PartialSortBy<T, TKey>(
            this EnumerableValueEnumerable<T> source, int count, Func<T, TKey> keySelector, bool descending)
        {
            if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            var direction = descending
                ? PartialSortByCore.SortDirection.Descending
                : PartialSortByCore.SortDirection.Ascending;
            return PartialSortByCore.Execute<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TKey>(
                source, count, keySelector, null, direction);
        }

        // ----- Choose -----

        /// <summary>Applies a combined filter and projection to every element of the array,
        /// keeping only the elements whose chooser flag is <see langword="true"/>.</summary>
        /// <typeparam name="T">The type of the source elements.</typeparam>
        /// <typeparam name="TResult">The type of the projected elements.</typeparam>
        /// <param name="source">The array to choose from.</param>
        /// <param name="chooser">The projection that decides whether to keep an element and to what
        /// value.</param>
        /// <returns>The sequence of kept, projected elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="chooser"/> is
        /// <see langword="null"/>.</exception>
        public static ChooseValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TResult> Choose<T, TResult>(
            this ArrayValueEnumerable<T> source, Func<T, (bool Keep, TResult Value)> chooser)
        {
            if (chooser is null) throw new ArgumentNullException(nameof(chooser));
            return new ChooseValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TResult>(source, chooser);
        }

        /// <summary>Applies a combined filter and projection to every element of the list,
        /// keeping only the elements whose chooser flag is <see langword="true"/>.</summary>
        /// <typeparam name="T">The type of the source elements.</typeparam>
        /// <typeparam name="TResult">The type of the projected elements.</typeparam>
        /// <param name="source">The list to choose from.</param>
        /// <param name="chooser">The projection that decides whether to keep an element and to what
        /// value.</param>
        /// <returns>The sequence of kept, projected elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="chooser"/> is
        /// <see langword="null"/>.</exception>
        public static ChooseValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TResult> Choose<T, TResult>(
            this ListValueEnumerable<T> source, Func<T, (bool Keep, TResult Value)> chooser)
        {
            if (chooser is null) throw new ArgumentNullException(nameof(chooser));
            return new ChooseValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TResult>(source, chooser);
        }

        /// <summary>Applies a combined filter and projection to every element of the read-only
        /// list, keeping only the elements whose chooser flag is <see langword="true"/>.</summary>
        /// <typeparam name="T">The type of the source elements.</typeparam>
        /// <typeparam name="TResult">The type of the projected elements.</typeparam>
        /// <param name="source">The read-only list to choose from.</param>
        /// <param name="chooser">The projection that decides whether to keep an element and to what
        /// value.</param>
        /// <returns>The sequence of kept, projected elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="chooser"/> is
        /// <see langword="null"/>.</exception>
        public static ChooseValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TResult> Choose<T, TResult>(
            this ReadOnlyListValueEnumerable<T> source, Func<T, (bool Keep, TResult Value)> chooser)
        {
            if (chooser is null) throw new ArgumentNullException(nameof(chooser));
            return new ChooseValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TResult>(source, chooser);
        }

        /// <summary>Applies a combined filter and projection to every element of the sequence,
        /// keeping only the elements whose chooser flag is <see langword="true"/>.</summary>
        /// <typeparam name="T">The type of the source elements.</typeparam>
        /// <typeparam name="TResult">The type of the projected elements.</typeparam>
        /// <param name="source">The sequence to choose from.</param>
        /// <param name="chooser">The projection that decides whether to keep an element and to what
        /// value.</param>
        /// <returns>The sequence of kept, projected elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="chooser"/> is
        /// <see langword="null"/>.</exception>
        public static ChooseValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TResult> Choose<T, TResult>(
            this EnumerableValueEnumerable<T> source, Func<T, (bool Keep, TResult Value)> chooser)
        {
            if (chooser is null) throw new ArgumentNullException(nameof(chooser));
            return new ChooseValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TResult>(source, chooser);
        }
    }
}
