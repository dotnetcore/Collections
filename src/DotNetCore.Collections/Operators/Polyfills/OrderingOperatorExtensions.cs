using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The keyless ordering operators of the P0 polyfill layer: <c>Order</c> and
    /// <c>OrderDescending</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both are BCL operators from .NET 7 onwards, and both answer with
    /// <see cref="IOrderedEnumerable{TElement}"/> rather than with a value-type enumerable of the
    /// engine's own. That return type is the point: it is what lets the BCL's own <c>ThenBy</c> -
    /// which has existed since .NET 3.5 and is therefore available on every target the package
    /// ships - chain onto an engine ordering, so a low-TFM caller gets the same multi-key shape a
    /// high-TFM caller gets from the platform.
    /// </para>
    /// <para>
    /// Ordering materialises its source; it is not a streaming operator. What the layer adds over
    /// a hand-rolled <c>OrderBy(x =&gt; x)</c> is that the walk into the sort uses the source's own
    /// value-type enumerator on the contiguous arms.
    /// </para>
    /// </remarks>
    public static partial class CollectionExtensions
    {
        // ----- Order -----

        /// <summary>Orders the array ascending by its elements.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The array to order.</param>
        /// <returns>The elements in ascending order.</returns>
        public static IOrderedEnumerable<T> Order<T>(this ArrayValueEnumerable<T> source)
            => new EngineOrderedEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, EngineComparisons.Ascending<T>(null));

        /// <summary>Orders the list ascending by its elements.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The list to order.</param>
        /// <returns>The elements in ascending order.</returns>
        public static IOrderedEnumerable<T> Order<T>(this ListValueEnumerable<T> source)
            => new EngineOrderedEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, EngineComparisons.Ascending<T>(null));

        /// <summary>Orders the read-only list ascending by its elements.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The read-only list to order.</param>
        /// <returns>The elements in ascending order.</returns>
        public static IOrderedEnumerable<T> Order<T>(this ReadOnlyListValueEnumerable<T> source)
            => new EngineOrderedEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, EngineComparisons.Ascending<T>(null));

        /// <summary>Orders the sequence ascending by its elements.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to order.</param>
        /// <returns>The elements in ascending order.</returns>
        public static IOrderedEnumerable<T> Order<T>(this EnumerableValueEnumerable<T> source)
        {
#if NET7_0_OR_GREATER
            return System.Linq.Enumerable.Order(source);
#else
            return new EngineOrderedEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, EngineComparisons.Ascending<T>(null));
#endif
        }

        /// <summary>Orders the array ascending by its elements, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The array to order.</param>
        /// <param name="comparer">The comparer of elements, or <see langword="null"/> for the default one.</param>
        /// <returns>The elements in ascending order.</returns>
        public static IOrderedEnumerable<T> Order<T>(this ArrayValueEnumerable<T> source, IComparer<T>? comparer)
            => new EngineOrderedEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, EngineComparisons.Ascending<T>(comparer));

        /// <summary>Orders the list ascending by its elements, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The list to order.</param>
        /// <param name="comparer">The comparer of elements, or <see langword="null"/> for the default one.</param>
        /// <returns>The elements in ascending order.</returns>
        public static IOrderedEnumerable<T> Order<T>(this ListValueEnumerable<T> source, IComparer<T>? comparer)
            => new EngineOrderedEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, EngineComparisons.Ascending<T>(comparer));

        /// <summary>Orders the read-only list ascending by its elements, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The read-only list to order.</param>
        /// <param name="comparer">The comparer of elements, or <see langword="null"/> for the default one.</param>
        /// <returns>The elements in ascending order.</returns>
        public static IOrderedEnumerable<T> Order<T>(this ReadOnlyListValueEnumerable<T> source, IComparer<T>? comparer)
            => new EngineOrderedEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, EngineComparisons.Ascending<T>(comparer));

        /// <summary>Orders the sequence ascending by its elements, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to order.</param>
        /// <param name="comparer">The comparer of elements, or <see langword="null"/> for the default one.</param>
        /// <returns>The elements in ascending order.</returns>
        public static IOrderedEnumerable<T> Order<T>(this EnumerableValueEnumerable<T> source, IComparer<T>? comparer)
        {
#if NET7_0_OR_GREATER
            return System.Linq.Enumerable.Order(source, comparer);
#else
            return new EngineOrderedEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, EngineComparisons.Ascending<T>(comparer));
#endif
        }

        // ----- OrderDescending -----

        /// <summary>Orders the array descending by its elements.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The array to order.</param>
        /// <returns>The elements in descending order.</returns>
        public static IOrderedEnumerable<T> OrderDescending<T>(this ArrayValueEnumerable<T> source)
            => new EngineOrderedEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, EngineComparisons.Descending<T>(null));

        /// <summary>Orders the list descending by its elements.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The list to order.</param>
        /// <returns>The elements in descending order.</returns>
        public static IOrderedEnumerable<T> OrderDescending<T>(this ListValueEnumerable<T> source)
            => new EngineOrderedEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, EngineComparisons.Descending<T>(null));

        /// <summary>Orders the read-only list descending by its elements.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The read-only list to order.</param>
        /// <returns>The elements in descending order.</returns>
        public static IOrderedEnumerable<T> OrderDescending<T>(this ReadOnlyListValueEnumerable<T> source)
            => new EngineOrderedEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, EngineComparisons.Descending<T>(null));

        /// <summary>Orders the sequence descending by its elements.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to order.</param>
        /// <returns>The elements in descending order.</returns>
        public static IOrderedEnumerable<T> OrderDescending<T>(this EnumerableValueEnumerable<T> source)
        {
#if NET7_0_OR_GREATER
            return System.Linq.Enumerable.OrderDescending(source);
#else
            return new EngineOrderedEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, EngineComparisons.Descending<T>(null));
#endif
        }

        /// <summary>Orders the array descending by its elements, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The array to order.</param>
        /// <param name="comparer">The comparer of elements, or <see langword="null"/> for the default one.</param>
        /// <returns>The elements in descending order.</returns>
        public static IOrderedEnumerable<T> OrderDescending<T>(this ArrayValueEnumerable<T> source, IComparer<T>? comparer)
            => new EngineOrderedEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, EngineComparisons.Descending<T>(comparer));

        /// <summary>Orders the list descending by its elements, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The list to order.</param>
        /// <param name="comparer">The comparer of elements, or <see langword="null"/> for the default one.</param>
        /// <returns>The elements in descending order.</returns>
        public static IOrderedEnumerable<T> OrderDescending<T>(this ListValueEnumerable<T> source, IComparer<T>? comparer)
            => new EngineOrderedEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, EngineComparisons.Descending<T>(comparer));

        /// <summary>Orders the read-only list descending by its elements, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The read-only list to order.</param>
        /// <param name="comparer">The comparer of elements, or <see langword="null"/> for the default one.</param>
        /// <returns>The elements in descending order.</returns>
        public static IOrderedEnumerable<T> OrderDescending<T>(this ReadOnlyListValueEnumerable<T> source, IComparer<T>? comparer)
            => new EngineOrderedEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, EngineComparisons.Descending<T>(comparer));

        /// <summary>Orders the sequence descending by its elements, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to order.</param>
        /// <param name="comparer">The comparer of elements, or <see langword="null"/> for the default one.</param>
        /// <returns>The elements in descending order.</returns>
        public static IOrderedEnumerable<T> OrderDescending<T>(this EnumerableValueEnumerable<T> source, IComparer<T>? comparer)
        {
#if NET7_0_OR_GREATER
            return System.Linq.Enumerable.OrderDescending(source, comparer);
#else
            return new EngineOrderedEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, EngineComparisons.Descending<T>(comparer));
#endif
        }
    }
}
