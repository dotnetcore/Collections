using System;
using System.Collections.Generic;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The first batch of the engine's public operator surface: <c>Index</c>, <c>ForEach</c>,
    /// <c>TagFirstLast</c>, <c>Pairwise</c> and <c>Scan</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The operators are declared as one member per arm of the four-source dispatch
    /// (<c>T[]</c>, <c>List&lt;T&gt;</c>, <c>IReadOnlyList&lt;T&gt;</c>, <c>IEnumerable&lt;T&gt;</c>)
    /// rather than as a single method constrained on <see cref="IValueEnumerable{T,TEnumerator}"/>,
    /// because C# does not infer type arguments from a generic constraint. Each member is a one-line
    /// shim that pins the source and its enumerator to the concrete arm, which is what lets the
    /// compiler emit a constrained call instead of boxing.
    /// </para>
    /// <para>
    /// This part of <see cref="CollectionExtensions"/> lives in its own file so that the operator
    /// surface can grow without the wrapper entry points of the class being edited every time.
    /// </para>
    /// </remarks>
    public static partial class CollectionExtensions
    {
        // ----- Index -----

        /// <summary>Pairs each element of the array with its zero-based position.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The array to index.</param>
        /// <returns>The sequence of index-and-element pairs.</returns>
        public static IndexValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T> Index<T>(
            this ArrayValueEnumerable<T> source)
            => new IndexValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source);

        /// <summary>Pairs each element of the list with its zero-based position.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The list to index.</param>
        /// <returns>The sequence of index-and-element pairs.</returns>
        public static IndexValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T> Index<T>(
            this ListValueEnumerable<T> source)
            => new IndexValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source);

        /// <summary>Pairs each element of the read-only list with its zero-based position.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The read-only list to index.</param>
        /// <returns>The sequence of index-and-element pairs.</returns>
        public static IndexValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T> Index<T>(
            this ReadOnlyListValueEnumerable<T> source)
            => new IndexValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source);

        /// <summary>Pairs each element of the sequence with its zero-based position.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to index.</param>
        /// <returns>The sequence of index-and-element pairs.</returns>
        public static IndexValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T> Index<T>(
            this EnumerableValueEnumerable<T> source)
            => new IndexValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source);

        // ----- ForEach -----

        /// <summary>Immediately runs the action on every element of the array.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The array to walk.</param>
        /// <param name="action">The action to run on each element.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
        public static void ForEach<T>(this ArrayValueEnumerable<T> source, Action<T> action)
            => ValueEnumerableCore.ForEach<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, action);

        /// <summary>Immediately runs the action on every element of the list.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The list to walk.</param>
        /// <param name="action">The action to run on each element.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
        public static void ForEach<T>(this ListValueEnumerable<T> source, Action<T> action)
            => ValueEnumerableCore.ForEach<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, action);

        /// <summary>Immediately runs the action on every element of the read-only list.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The read-only list to walk.</param>
        /// <param name="action">The action to run on each element.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
        public static void ForEach<T>(this ReadOnlyListValueEnumerable<T> source, Action<T> action)
            => ValueEnumerableCore.ForEach<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, action);

        /// <summary>Immediately runs the action on every element of the sequence.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to walk.</param>
        /// <param name="action">The action to run on each element.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
        public static void ForEach<T>(this EnumerableValueEnumerable<T> source, Action<T> action)
            => ValueEnumerableCore.ForEach<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, action);

        /// <summary>Immediately runs the action on every element of the array, passing its position.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The array to walk.</param>
        /// <param name="action">The action to run on each element; its second argument is the
        /// zero-based position of the element.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
        public static void ForEach<T>(this ArrayValueEnumerable<T> source, Action<T, int> action)
            => ValueEnumerableCore.ForEachIndexed<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, action);

        /// <summary>Immediately runs the action on every element of the list, passing its position.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The list to walk.</param>
        /// <param name="action">The action to run on each element; its second argument is the
        /// zero-based position of the element.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
        public static void ForEach<T>(this ListValueEnumerable<T> source, Action<T, int> action)
            => ValueEnumerableCore.ForEachIndexed<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, action);

        /// <summary>Immediately runs the action on every element of the read-only list, passing its position.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The read-only list to walk.</param>
        /// <param name="action">The action to run on each element; its second argument is the
        /// zero-based position of the element.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
        public static void ForEach<T>(this ReadOnlyListValueEnumerable<T> source, Action<T, int> action)
            => ValueEnumerableCore.ForEachIndexed<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, action);

        /// <summary>Immediately runs the action on every element of the sequence, passing its position.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to walk.</param>
        /// <param name="action">The action to run on each element; its second argument is the
        /// zero-based position of the element.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
        public static void ForEach<T>(this EnumerableValueEnumerable<T> source, Action<T, int> action)
            => ValueEnumerableCore.ForEachIndexed<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, action);

        // ----- TagFirstLast -----

        /// <summary>Tags each element of the array as the first and/or the last of the sequence.</summary>
        /// <typeparam name="T">The type of the source elements.</typeparam>
        /// <typeparam name="TResult">The type of the projected elements.</typeparam>
        /// <param name="source">The array to tag.</param>
        /// <param name="resultSelector">The projection of an element together with its two flags.</param>
        /// <returns>The sequence of tagged elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector"/> is
        /// <see langword="null"/>.</exception>
        public static TagFirstLastValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TResult> TagFirstLast<T, TResult>(
            this ArrayValueEnumerable<T> source, Func<T, bool, bool, TResult> resultSelector)
            => new TagFirstLastValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TResult>(source, resultSelector);

        /// <summary>Tags each element of the list as the first and/or the last of the sequence.</summary>
        /// <typeparam name="T">The type of the source elements.</typeparam>
        /// <typeparam name="TResult">The type of the projected elements.</typeparam>
        /// <param name="source">The list to tag.</param>
        /// <param name="resultSelector">The projection of an element together with its two flags.</param>
        /// <returns>The sequence of tagged elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector"/> is
        /// <see langword="null"/>.</exception>
        public static TagFirstLastValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TResult> TagFirstLast<T, TResult>(
            this ListValueEnumerable<T> source, Func<T, bool, bool, TResult> resultSelector)
            => new TagFirstLastValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TResult>(source, resultSelector);

        /// <summary>Tags each element of the read-only list as the first and/or the last of the sequence.</summary>
        /// <typeparam name="T">The type of the source elements.</typeparam>
        /// <typeparam name="TResult">The type of the projected elements.</typeparam>
        /// <param name="source">The read-only list to tag.</param>
        /// <param name="resultSelector">The projection of an element together with its two flags.</param>
        /// <returns>The sequence of tagged elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector"/> is
        /// <see langword="null"/>.</exception>
        public static TagFirstLastValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TResult> TagFirstLast<T, TResult>(
            this ReadOnlyListValueEnumerable<T> source, Func<T, bool, bool, TResult> resultSelector)
            => new TagFirstLastValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TResult>(source, resultSelector);

        /// <summary>Tags each element of the sequence as the first and/or the last of the sequence.</summary>
        /// <typeparam name="T">The type of the source elements.</typeparam>
        /// <typeparam name="TResult">The type of the projected elements.</typeparam>
        /// <param name="source">The sequence to tag.</param>
        /// <param name="resultSelector">The projection of an element together with its two flags.</param>
        /// <returns>The sequence of tagged elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector"/> is
        /// <see langword="null"/>.</exception>
        public static TagFirstLastValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TResult> TagFirstLast<T, TResult>(
            this EnumerableValueEnumerable<T> source, Func<T, bool, bool, TResult> resultSelector)
            => new TagFirstLastValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TResult>(source, resultSelector);

        // ----- Pairwise -----

        /// <summary>Pairs each element of the array with its predecessor.</summary>
        /// <typeparam name="T">The type of the source elements.</typeparam>
        /// <typeparam name="TResult">The type of the projected elements.</typeparam>
        /// <param name="source">The array to pair.</param>
        /// <param name="resultSelector">The projection of a predecessor and its successor.</param>
        /// <returns>The sequence of pairs; one result fewer than the source has elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector"/> is
        /// <see langword="null"/>.</exception>
        public static PairwiseValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TResult> Pairwise<T, TResult>(
            this ArrayValueEnumerable<T> source, Func<T, T, TResult> resultSelector)
            => new PairwiseValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TResult>(source, resultSelector);

        /// <summary>Pairs each element of the list with its predecessor.</summary>
        /// <typeparam name="T">The type of the source elements.</typeparam>
        /// <typeparam name="TResult">The type of the projected elements.</typeparam>
        /// <param name="source">The list to pair.</param>
        /// <param name="resultSelector">The projection of a predecessor and its successor.</param>
        /// <returns>The sequence of pairs; one result fewer than the source has elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector"/> is
        /// <see langword="null"/>.</exception>
        public static PairwiseValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TResult> Pairwise<T, TResult>(
            this ListValueEnumerable<T> source, Func<T, T, TResult> resultSelector)
            => new PairwiseValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TResult>(source, resultSelector);

        /// <summary>Pairs each element of the read-only list with its predecessor.</summary>
        /// <typeparam name="T">The type of the source elements.</typeparam>
        /// <typeparam name="TResult">The type of the projected elements.</typeparam>
        /// <param name="source">The read-only list to pair.</param>
        /// <param name="resultSelector">The projection of a predecessor and its successor.</param>
        /// <returns>The sequence of pairs; one result fewer than the source has elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector"/> is
        /// <see langword="null"/>.</exception>
        public static PairwiseValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TResult> Pairwise<T, TResult>(
            this ReadOnlyListValueEnumerable<T> source, Func<T, T, TResult> resultSelector)
            => new PairwiseValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TResult>(source, resultSelector);

        /// <summary>Pairs each element of the sequence with its predecessor.</summary>
        /// <typeparam name="T">The type of the source elements.</typeparam>
        /// <typeparam name="TResult">The type of the projected elements.</typeparam>
        /// <param name="source">The sequence to pair.</param>
        /// <param name="resultSelector">The projection of a predecessor and its successor.</param>
        /// <returns>The sequence of pairs; one result fewer than the source has elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="resultSelector"/> is
        /// <see langword="null"/>.</exception>
        public static PairwiseValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TResult> Pairwise<T, TResult>(
            this EnumerableValueEnumerable<T> source, Func<T, T, TResult> resultSelector)
            => new PairwiseValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TResult>(source, resultSelector);

        // ----- Scan (inclusive, no seed) -----

        /// <summary>Yields the running aggregate of the array after every element.</summary>
        /// <typeparam name="T">The type of the elements, which is also the type of the aggregate.</typeparam>
        /// <param name="source">The array to scan.</param>
        /// <param name="transformation">The fold to apply to the aggregate and the next element.</param>
        /// <returns>The sequence of running aggregates; the same length as the source.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="transformation"/> is
        /// <see langword="null"/>.</exception>
        public static ScanValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T> Scan<T>(
            this ArrayValueEnumerable<T> source, Func<T, T, T> transformation)
            => new ScanValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, transformation);

        /// <summary>Yields the running aggregate of the list after every element.</summary>
        /// <typeparam name="T">The type of the elements, which is also the type of the aggregate.</typeparam>
        /// <param name="source">The list to scan.</param>
        /// <param name="transformation">The fold to apply to the aggregate and the next element.</param>
        /// <returns>The sequence of running aggregates; the same length as the source.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="transformation"/> is
        /// <see langword="null"/>.</exception>
        public static ScanValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T> Scan<T>(
            this ListValueEnumerable<T> source, Func<T, T, T> transformation)
            => new ScanValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, transformation);

        /// <summary>Yields the running aggregate of the read-only list after every element.</summary>
        /// <typeparam name="T">The type of the elements, which is also the type of the aggregate.</typeparam>
        /// <param name="source">The read-only list to scan.</param>
        /// <param name="transformation">The fold to apply to the aggregate and the next element.</param>
        /// <returns>The sequence of running aggregates; the same length as the source.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="transformation"/> is
        /// <see langword="null"/>.</exception>
        public static ScanValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T> Scan<T>(
            this ReadOnlyListValueEnumerable<T> source, Func<T, T, T> transformation)
            => new ScanValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, transformation);

        /// <summary>Yields the running aggregate of the sequence after every element.</summary>
        /// <typeparam name="T">The type of the elements, which is also the type of the aggregate.</typeparam>
        /// <param name="source">The sequence to scan.</param>
        /// <param name="transformation">The fold to apply to the aggregate and the next element.</param>
        /// <returns>The sequence of running aggregates; the same length as the source.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="transformation"/> is
        /// <see langword="null"/>.</exception>
        public static ScanValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T> Scan<T>(
            this EnumerableValueEnumerable<T> source, Func<T, T, T> transformation)
            => new ScanValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, transformation);

        // ----- Scan (inclusive, seeded) -----

        /// <summary>Yields the seed and then the running aggregate of the array after every element.</summary>
        /// <typeparam name="T">The type of the source elements.</typeparam>
        /// <typeparam name="TResult">The type of the aggregate, which may differ from the element type.</typeparam>
        /// <param name="source">The array to scan.</param>
        /// <param name="seed">The initial aggregate; it is yielded even when the source is empty.</param>
        /// <param name="transformation">The fold to apply to the aggregate and the next element.</param>
        /// <returns>The sequence of running aggregates; one more than the source has elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="transformation"/> is
        /// <see langword="null"/>.</exception>
        public static SeededScanValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TResult> Scan<T, TResult>(
            this ArrayValueEnumerable<T> source, TResult seed, Func<TResult, T, TResult> transformation)
            => new SeededScanValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TResult>(source, seed, transformation);

        /// <summary>Yields the seed and then the running aggregate of the list after every element.</summary>
        /// <typeparam name="T">The type of the source elements.</typeparam>
        /// <typeparam name="TResult">The type of the aggregate, which may differ from the element type.</typeparam>
        /// <param name="source">The list to scan.</param>
        /// <param name="seed">The initial aggregate; it is yielded even when the source is empty.</param>
        /// <param name="transformation">The fold to apply to the aggregate and the next element.</param>
        /// <returns>The sequence of running aggregates; one more than the source has elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="transformation"/> is
        /// <see langword="null"/>.</exception>
        public static SeededScanValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TResult> Scan<T, TResult>(
            this ListValueEnumerable<T> source, TResult seed, Func<TResult, T, TResult> transformation)
            => new SeededScanValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TResult>(source, seed, transformation);

        /// <summary>Yields the seed and then the running aggregate of the read-only list after every element.</summary>
        /// <typeparam name="T">The type of the source elements.</typeparam>
        /// <typeparam name="TResult">The type of the aggregate, which may differ from the element type.</typeparam>
        /// <param name="source">The read-only list to scan.</param>
        /// <param name="seed">The initial aggregate; it is yielded even when the source is empty.</param>
        /// <param name="transformation">The fold to apply to the aggregate and the next element.</param>
        /// <returns>The sequence of running aggregates; one more than the source has elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="transformation"/> is
        /// <see langword="null"/>.</exception>
        public static SeededScanValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TResult> Scan<T, TResult>(
            this ReadOnlyListValueEnumerable<T> source, TResult seed, Func<TResult, T, TResult> transformation)
            => new SeededScanValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TResult>(source, seed, transformation);

        /// <summary>Yields the seed and then the running aggregate of the sequence after every element.</summary>
        /// <typeparam name="T">The type of the source elements.</typeparam>
        /// <typeparam name="TResult">The type of the aggregate, which may differ from the element type.</typeparam>
        /// <param name="source">The sequence to scan.</param>
        /// <param name="seed">The initial aggregate; it is yielded even when the source is empty.</param>
        /// <param name="transformation">The fold to apply to the aggregate and the next element.</param>
        /// <returns>The sequence of running aggregates; one more than the source has elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="transformation"/> is
        /// <see langword="null"/>.</exception>
        public static SeededScanValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TResult> Scan<T, TResult>(
            this EnumerableValueEnumerable<T> source, TResult seed, Func<TResult, T, TResult> transformation)
            => new SeededScanValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TResult>(source, seed, transformation);
    }
}
