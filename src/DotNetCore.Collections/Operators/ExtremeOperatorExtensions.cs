using System;
using System.Collections.Generic;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The tied-extreme operators of the P2 layer: <c>Maxima</c> and <c>Minima</c>, which return
    /// every element that shares the extreme key rather than one winner.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The BCL's <c>MaxBy</c> / <c>MinBy</c> answer "which one element wins"; these answer "who
    /// tied". That is a different question with a different answer shape, so the two do not overlap
    /// and neither is a replacement for the other: a leaderboard needs the ties, a top-1 query needs
    /// the winner.
    /// </para>
    /// <para>
    /// Both operators are eager, because the last element of the source can always turn out to tie
    /// with the best so far and there is no way to know before the walk ends. The result is a
    /// <see cref="List{T}"/> for the same reason <c>PartialSortBy</c> returns one: the answer is
    /// already materialized, so handing back a lazy view over it would only add a layer. The walk
    /// itself still runs through the value-type enumerator, which is what the four-source dispatch
    /// is for.
    /// </para>
    /// <para>
    /// An empty source yields an empty list rather than throwing, which is the one place these
    /// deliberately differ from <c>MaxBy</c> / <c>MinBy</c>: "no element is maximal" is a truthful
    /// answer to "which elements are maximal", and the caller who needs the BCL's contract can ask
    /// for it with <c>AtLeast(1)</c> first.
    /// </para>
    /// </remarks>
    public static partial class CollectionExtensions
    {
        // ----- Maxima -----

        /// <summary>Returns every element of the array whose key is the largest.</summary>
        /// <typeparam name="TSource">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key being compared.</typeparam>
        /// <param name="source">The array to scan.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>Every element that shares the largest key, in source order; empty when the source
        /// is empty.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        public static List<TSource> Maxima<TSource, TKey>(
            this ArrayValueEnumerable<TSource> source, Func<TSource, TKey> keySelector,
            IComparer<TKey>? comparer = null)
            => ExtremesCore<ArrayValueEnumerable<TSource>, ArrayValueEnumerator<TSource>, TSource, TKey>(
                source, keySelector, comparer, true);

        /// <summary>Returns every element of the list whose key is the largest.</summary>
        /// <typeparam name="TSource">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key being compared.</typeparam>
        /// <param name="source">The list to scan.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>Every element that shares the largest key, in source order; empty when the source
        /// is empty.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        public static List<TSource> Maxima<TSource, TKey>(
            this ListValueEnumerable<TSource> source, Func<TSource, TKey> keySelector,
            IComparer<TKey>? comparer = null)
            => ExtremesCore<ListValueEnumerable<TSource>, ListValueEnumerator<TSource>, TSource, TKey>(
                source, keySelector, comparer, true);

        /// <summary>Returns every element of the read-only list whose key is the largest.</summary>
        /// <typeparam name="TSource">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key being compared.</typeparam>
        /// <param name="source">The read-only list to scan.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>Every element that shares the largest key, in source order; empty when the source
        /// is empty.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        public static List<TSource> Maxima<TSource, TKey>(
            this ReadOnlyListValueEnumerable<TSource> source, Func<TSource, TKey> keySelector,
            IComparer<TKey>? comparer = null)
            => ExtremesCore<ReadOnlyListValueEnumerable<TSource>, ReadOnlyListValueEnumerator<TSource>, TSource, TKey>(
                source, keySelector, comparer, true);

        /// <summary>Returns every element of the sequence whose key is the largest.</summary>
        /// <typeparam name="TSource">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key being compared.</typeparam>
        /// <param name="source">The sequence to scan.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>Every element that shares the largest key, in source order; empty when the source
        /// is empty.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        public static List<TSource> Maxima<TSource, TKey>(
            this EnumerableValueEnumerable<TSource> source, Func<TSource, TKey> keySelector,
            IComparer<TKey>? comparer = null)
            => ExtremesCore<EnumerableValueEnumerable<TSource>, EnumerableValueEnumerator<TSource>, TSource, TKey>(
                source, keySelector, comparer, true);

        // ----- Minima -----

        /// <summary>Returns every element of the array whose key is the smallest.</summary>
        /// <typeparam name="TSource">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key being compared.</typeparam>
        /// <param name="source">The array to scan.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>Every element that shares the smallest key, in source order; empty when the source
        /// is empty.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        public static List<TSource> Minima<TSource, TKey>(
            this ArrayValueEnumerable<TSource> source, Func<TSource, TKey> keySelector,
            IComparer<TKey>? comparer = null)
            => ExtremesCore<ArrayValueEnumerable<TSource>, ArrayValueEnumerator<TSource>, TSource, TKey>(
                source, keySelector, comparer, false);

        /// <summary>Returns every element of the list whose key is the smallest.</summary>
        /// <typeparam name="TSource">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key being compared.</typeparam>
        /// <param name="source">The list to scan.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>Every element that shares the smallest key, in source order; empty when the source
        /// is empty.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        public static List<TSource> Minima<TSource, TKey>(
            this ListValueEnumerable<TSource> source, Func<TSource, TKey> keySelector,
            IComparer<TKey>? comparer = null)
            => ExtremesCore<ListValueEnumerable<TSource>, ListValueEnumerator<TSource>, TSource, TKey>(
                source, keySelector, comparer, false);

        /// <summary>Returns every element of the read-only list whose key is the smallest.</summary>
        /// <typeparam name="TSource">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key being compared.</typeparam>
        /// <param name="source">The read-only list to scan.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>Every element that shares the smallest key, in source order; empty when the source
        /// is empty.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        public static List<TSource> Minima<TSource, TKey>(
            this ReadOnlyListValueEnumerable<TSource> source, Func<TSource, TKey> keySelector,
            IComparer<TKey>? comparer = null)
            => ExtremesCore<ReadOnlyListValueEnumerable<TSource>, ReadOnlyListValueEnumerator<TSource>, TSource, TKey>(
                source, keySelector, comparer, false);

        /// <summary>Returns every element of the sequence whose key is the smallest.</summary>
        /// <typeparam name="TSource">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key being compared.</typeparam>
        /// <param name="source">The sequence to scan.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>Every element that shares the smallest key, in source order; empty when the source
        /// is empty.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        public static List<TSource> Minima<TSource, TKey>(
            this EnumerableValueEnumerable<TSource> source, Func<TSource, TKey> keySelector,
            IComparer<TKey>? comparer = null)
            => ExtremesCore<EnumerableValueEnumerable<TSource>, EnumerableValueEnumerator<TSource>, TSource, TKey>(
                source, keySelector, comparer, false);

        /// <summary>
        /// The one scan behind both operators: keeps the best key seen so far and the elements that
        /// share it, and restarts the tie list whenever a strictly better key turns up.
        /// </summary>
        /// <remarks>
        /// The keys are compared with <see cref="IComparer{T}"/>.Compare rather than with
        /// <c>Comparer&lt;T&gt;.Default.Compare</c> per element, so a caller-supplied comparer costs
        /// one virtual call per element and not a delegate dispatch on top of it. A tie is
        /// <c>Compare(...) == 0</c>, which is what makes a null key comparable with a non-null one
        /// on the types where the default comparer orders nulls.
        /// </remarks>
        private static List<TSource> ExtremesCore<TEnumerable, TEnumerator, TSource, TKey>(
            TEnumerable source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer, bool maximum)
            where TEnumerable : struct, IValueEnumerable<TSource, TEnumerator>
            where TEnumerator : struct, IEnumerator<TSource>
        {
            if (keySelector is null) throw new ArgumentNullException(nameof(keySelector));

            var cmp = comparer ?? Comparer<TKey>.Default;
            var winners = new List<TSource>();
            var best = default(TKey)!;
            var hasBest = false;

            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var item = enumerator.Current;
                    var key = keySelector(item);

                    if (!hasBest)
                    {
                        best = key;
                        hasBest = true;
                        winners.Add(item);
                        continue;
                    }

                    var order = cmp.Compare(key, best);
                    if (maximum ? order > 0 : order < 0)
                    {
                        best = key;
                        winners.Clear();
                        winners.Add(item);
                    }
                    else if (order == 0)
                    {
                        winners.Add(item);
                    }
                }
            }

            return winners;
        }
    }
}
