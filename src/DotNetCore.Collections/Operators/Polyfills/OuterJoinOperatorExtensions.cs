#if !NET10_0_OR_GREATER
using System;
using System.Collections.Generic;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The outer-join operators of the P0 polyfill layer: <c>LeftJoin</c> and <c>RightJoin</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These two are BCL operators from .NET 10 onwards, and they are the only members of the layer
    /// that are polyfilled on the lower targets and nowhere else. The other eleven operators gain an
    /// accelerated arm above the target that introduced them; these two deliberately do not, because
    /// .NET 10 declares the same signature and a second one here would only compete with it. That is
    /// why the whole file sits behind <c>#if !NET10_0_OR_GREATER</c>.
    /// </para>
    /// <para>
    /// The dispatch is the four-source one the rest of the engine uses, applied to the sequence the
    /// operator drives: <c>LeftJoin</c> drives on the outer sequence and folds the inner one into a
    /// lookup, <c>RightJoin</c> drives on the inner sequence and folds the outer one. The three
    /// contiguous arms take the engine's value-type path; the arbitrary-sequence arm wraps the
    /// engine's own walk, because there is no platform method to hand the call to.
    /// </para>
    /// <para>
    /// A comparer overload exists alongside the default-comparer one, which is the pair of overloads
    /// the BCL declares.
    /// </para>
    /// </remarks>
    public static partial class CollectionExtensions
    {
        // ----- LeftJoin -----

        /// <summary>Joins the array with <paramref name="inner"/>, keeping the array's elements that
        /// have no match.</summary>
        /// <typeparam name="TOuter">The type of the outer elements.</typeparam>
        /// <typeparam name="TInner">The type of the inner elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are joined on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="outer">The outer sequence.</param>
        /// <param name="inner">The inner sequence.</param>
        /// <param name="outerKeySelector">The projection of an outer element to its key.</param>
        /// <param name="innerKeySelector">The projection of an inner element to its key.</param>
        /// <param name="resultSelector">The projection of a matched pair.</param>
        /// <returns>The joined sequence; an unmatched outer element is projected against
        /// <see langword="default"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/>,
        /// <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static LeftJoinValueEnumerable<ArrayValueEnumerable<TOuter>, ArrayValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult> LeftJoin<TOuter, TInner, TKey, TResult>(
            this ArrayValueEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector)
            => new LeftJoinValueEnumerable<ArrayValueEnumerable<TOuter>, ArrayValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);

        /// <summary>Joins the array with <paramref name="inner"/> on keys compared with
        /// <paramref name="comparer"/>, keeping the array's elements that have no match.</summary>
        /// <typeparam name="TOuter">The type of the outer elements.</typeparam>
        /// <typeparam name="TInner">The type of the inner elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are joined on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="outer">The outer sequence.</param>
        /// <param name="inner">The inner sequence.</param>
        /// <param name="outerKeySelector">The projection of an outer element to its key.</param>
        /// <param name="innerKeySelector">The projection of an inner element to its key.</param>
        /// <param name="resultSelector">The projection of a matched pair.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The joined sequence; an unmatched outer element is projected against
        /// <see langword="default"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/>,
        /// <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static LeftJoinValueEnumerable<ArrayValueEnumerable<TOuter>, ArrayValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult> LeftJoin<TOuter, TInner, TKey, TResult>(
            this ArrayValueEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
            => new LeftJoinValueEnumerable<ArrayValueEnumerable<TOuter>, ArrayValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);

        /// <summary>Joins the list with <paramref name="inner"/>, keeping the list's elements that
        /// have no match.</summary>
        /// <typeparam name="TOuter">The type of the outer elements.</typeparam>
        /// <typeparam name="TInner">The type of the inner elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are joined on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="outer">The outer sequence.</param>
        /// <param name="inner">The inner sequence.</param>
        /// <param name="outerKeySelector">The projection of an outer element to its key.</param>
        /// <param name="innerKeySelector">The projection of an inner element to its key.</param>
        /// <param name="resultSelector">The projection of a matched pair.</param>
        /// <returns>The joined sequence; an unmatched outer element is projected against
        /// <see langword="default"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/>,
        /// <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static LeftJoinValueEnumerable<ListValueEnumerable<TOuter>, ListValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult> LeftJoin<TOuter, TInner, TKey, TResult>(
            this ListValueEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector)
            => new LeftJoinValueEnumerable<ListValueEnumerable<TOuter>, ListValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);

        /// <summary>Joins the list with <paramref name="inner"/> on keys compared with
        /// <paramref name="comparer"/>, keeping the list's elements that have no match.</summary>
        /// <typeparam name="TOuter">The type of the outer elements.</typeparam>
        /// <typeparam name="TInner">The type of the inner elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are joined on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="outer">The outer sequence.</param>
        /// <param name="inner">The inner sequence.</param>
        /// <param name="outerKeySelector">The projection of an outer element to its key.</param>
        /// <param name="innerKeySelector">The projection of an inner element to its key.</param>
        /// <param name="resultSelector">The projection of a matched pair.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The joined sequence; an unmatched outer element is projected against
        /// <see langword="default"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/>,
        /// <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static LeftJoinValueEnumerable<ListValueEnumerable<TOuter>, ListValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult> LeftJoin<TOuter, TInner, TKey, TResult>(
            this ListValueEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
            => new LeftJoinValueEnumerable<ListValueEnumerable<TOuter>, ListValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);

        /// <summary>Joins the read-only list with <paramref name="inner"/>, keeping the read-only
        /// list's elements that have no match.</summary>
        /// <typeparam name="TOuter">The type of the outer elements.</typeparam>
        /// <typeparam name="TInner">The type of the inner elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are joined on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="outer">The outer sequence.</param>
        /// <param name="inner">The inner sequence.</param>
        /// <param name="outerKeySelector">The projection of an outer element to its key.</param>
        /// <param name="innerKeySelector">The projection of an inner element to its key.</param>
        /// <param name="resultSelector">The projection of a matched pair.</param>
        /// <returns>The joined sequence; an unmatched outer element is projected against
        /// <see langword="default"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/>,
        /// <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static LeftJoinValueEnumerable<ReadOnlyListValueEnumerable<TOuter>, ReadOnlyListValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult> LeftJoin<TOuter, TInner, TKey, TResult>(
            this ReadOnlyListValueEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector)
            => new LeftJoinValueEnumerable<ReadOnlyListValueEnumerable<TOuter>, ReadOnlyListValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);

        /// <summary>Joins the read-only list with <paramref name="inner"/> on keys compared with
        /// <paramref name="comparer"/>, keeping the read-only list's elements that have no
        /// match.</summary>
        /// <typeparam name="TOuter">The type of the outer elements.</typeparam>
        /// <typeparam name="TInner">The type of the inner elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are joined on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="outer">The outer sequence.</param>
        /// <param name="inner">The inner sequence.</param>
        /// <param name="outerKeySelector">The projection of an outer element to its key.</param>
        /// <param name="innerKeySelector">The projection of an inner element to its key.</param>
        /// <param name="resultSelector">The projection of a matched pair.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The joined sequence; an unmatched outer element is projected against
        /// <see langword="default"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/>,
        /// <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static LeftJoinValueEnumerable<ReadOnlyListValueEnumerable<TOuter>, ReadOnlyListValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult> LeftJoin<TOuter, TInner, TKey, TResult>(
            this ReadOnlyListValueEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
            => new LeftJoinValueEnumerable<ReadOnlyListValueEnumerable<TOuter>, ReadOnlyListValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);

        /// <summary>Joins the sequence with <paramref name="inner"/>, keeping the sequence's elements
        /// that have no match.</summary>
        /// <typeparam name="TOuter">The type of the outer elements.</typeparam>
        /// <typeparam name="TInner">The type of the inner elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are joined on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="outer">The outer sequence.</param>
        /// <param name="inner">The inner sequence.</param>
        /// <param name="outerKeySelector">The projection of an outer element to its key.</param>
        /// <param name="innerKeySelector">The projection of an inner element to its key.</param>
        /// <param name="resultSelector">The projection of a matched pair.</param>
        /// <returns>The joined sequence; an unmatched outer element is projected against
        /// <see langword="default"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/>,
        /// <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(
            this EnumerableValueEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector)
            => new EnumerableValueEnumerable<TResult>(
                new LeftJoinValueEnumerable<EnumerableValueEnumerable<TOuter>, EnumerableValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null));

        /// <summary>Joins the sequence with <paramref name="inner"/> on keys compared with
        /// <paramref name="comparer"/>, keeping the sequence's elements that have no match.</summary>
        /// <typeparam name="TOuter">The type of the outer elements.</typeparam>
        /// <typeparam name="TInner">The type of the inner elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are joined on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="outer">The outer sequence.</param>
        /// <param name="inner">The inner sequence.</param>
        /// <param name="outerKeySelector">The projection of an outer element to its key.</param>
        /// <param name="innerKeySelector">The projection of an inner element to its key.</param>
        /// <param name="resultSelector">The projection of a matched pair.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The joined sequence; an unmatched outer element is projected against
        /// <see langword="default"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/>,
        /// <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> LeftJoin<TOuter, TInner, TKey, TResult>(
            this EnumerableValueEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
            => new EnumerableValueEnumerable<TResult>(
                new LeftJoinValueEnumerable<EnumerableValueEnumerable<TOuter>, EnumerableValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer));

        // ----- RightJoin -----

        /// <summary>Joins the array with <paramref name="inner"/>, keeping <paramref name="inner"/>'s
        /// elements that have no match.</summary>
        /// <typeparam name="TOuter">The type of the outer elements.</typeparam>
        /// <typeparam name="TInner">The type of the inner elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are joined on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="outer">The outer sequence.</param>
        /// <param name="inner">The inner sequence.</param>
        /// <param name="outerKeySelector">The projection of an outer element to its key.</param>
        /// <param name="innerKeySelector">The projection of an inner element to its key.</param>
        /// <param name="resultSelector">The projection of a matched pair.</param>
        /// <returns>The joined sequence; an unmatched inner element is projected against
        /// <see langword="default"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/>,
        /// <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static RightJoinValueEnumerable<ArrayValueEnumerable<TOuter>, ArrayValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult> RightJoin<TOuter, TInner, TKey, TResult>(
            this ArrayValueEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector)
            => new RightJoinValueEnumerable<ArrayValueEnumerable<TOuter>, ArrayValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);

        /// <summary>Joins the array with <paramref name="inner"/> on keys compared with
        /// <paramref name="comparer"/>, keeping <paramref name="inner"/>'s elements that have no
        /// match.</summary>
        /// <typeparam name="TOuter">The type of the outer elements.</typeparam>
        /// <typeparam name="TInner">The type of the inner elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are joined on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="outer">The outer sequence.</param>
        /// <param name="inner">The inner sequence.</param>
        /// <param name="outerKeySelector">The projection of an outer element to its key.</param>
        /// <param name="innerKeySelector">The projection of an inner element to its key.</param>
        /// <param name="resultSelector">The projection of a matched pair.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The joined sequence; an unmatched inner element is projected against
        /// <see langword="default"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/>,
        /// <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static RightJoinValueEnumerable<ArrayValueEnumerable<TOuter>, ArrayValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult> RightJoin<TOuter, TInner, TKey, TResult>(
            this ArrayValueEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
            => new RightJoinValueEnumerable<ArrayValueEnumerable<TOuter>, ArrayValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);

        /// <summary>Joins the list with <paramref name="inner"/>, keeping <paramref name="inner"/>'s
        /// elements that have no match.</summary>
        /// <typeparam name="TOuter">The type of the outer elements.</typeparam>
        /// <typeparam name="TInner">The type of the inner elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are joined on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="outer">The outer sequence.</param>
        /// <param name="inner">The inner sequence.</param>
        /// <param name="outerKeySelector">The projection of an outer element to its key.</param>
        /// <param name="innerKeySelector">The projection of an inner element to its key.</param>
        /// <param name="resultSelector">The projection of a matched pair.</param>
        /// <returns>The joined sequence; an unmatched inner element is projected against
        /// <see langword="default"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/>,
        /// <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static RightJoinValueEnumerable<ListValueEnumerable<TOuter>, ListValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult> RightJoin<TOuter, TInner, TKey, TResult>(
            this ListValueEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector)
            => new RightJoinValueEnumerable<ListValueEnumerable<TOuter>, ListValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);

        /// <summary>Joins the list with <paramref name="inner"/> on keys compared with
        /// <paramref name="comparer"/>, keeping <paramref name="inner"/>'s elements that have no
        /// match.</summary>
        /// <typeparam name="TOuter">The type of the outer elements.</typeparam>
        /// <typeparam name="TInner">The type of the inner elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are joined on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="outer">The outer sequence.</param>
        /// <param name="inner">The inner sequence.</param>
        /// <param name="outerKeySelector">The projection of an outer element to its key.</param>
        /// <param name="innerKeySelector">The projection of an inner element to its key.</param>
        /// <param name="resultSelector">The projection of a matched pair.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The joined sequence; an unmatched inner element is projected against
        /// <see langword="default"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/>,
        /// <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static RightJoinValueEnumerable<ListValueEnumerable<TOuter>, ListValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult> RightJoin<TOuter, TInner, TKey, TResult>(
            this ListValueEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
            => new RightJoinValueEnumerable<ListValueEnumerable<TOuter>, ListValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);

        /// <summary>Joins the read-only list with <paramref name="inner"/>, keeping
        /// <paramref name="inner"/>'s elements that have no match.</summary>
        /// <typeparam name="TOuter">The type of the outer elements.</typeparam>
        /// <typeparam name="TInner">The type of the inner elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are joined on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="outer">The outer sequence.</param>
        /// <param name="inner">The inner sequence.</param>
        /// <param name="outerKeySelector">The projection of an outer element to its key.</param>
        /// <param name="innerKeySelector">The projection of an inner element to its key.</param>
        /// <param name="resultSelector">The projection of a matched pair.</param>
        /// <returns>The joined sequence; an unmatched inner element is projected against
        /// <see langword="default"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/>,
        /// <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static RightJoinValueEnumerable<ReadOnlyListValueEnumerable<TOuter>, ReadOnlyListValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult> RightJoin<TOuter, TInner, TKey, TResult>(
            this ReadOnlyListValueEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector)
            => new RightJoinValueEnumerable<ReadOnlyListValueEnumerable<TOuter>, ReadOnlyListValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);

        /// <summary>Joins the read-only list with <paramref name="inner"/> on keys compared with
        /// <paramref name="comparer"/>, keeping <paramref name="inner"/>'s elements that have no
        /// match.</summary>
        /// <typeparam name="TOuter">The type of the outer elements.</typeparam>
        /// <typeparam name="TInner">The type of the inner elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are joined on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="outer">The outer sequence.</param>
        /// <param name="inner">The inner sequence.</param>
        /// <param name="outerKeySelector">The projection of an outer element to its key.</param>
        /// <param name="innerKeySelector">The projection of an inner element to its key.</param>
        /// <param name="resultSelector">The projection of a matched pair.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The joined sequence; an unmatched inner element is projected against
        /// <see langword="default"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/>,
        /// <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static RightJoinValueEnumerable<ReadOnlyListValueEnumerable<TOuter>, ReadOnlyListValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult> RightJoin<TOuter, TInner, TKey, TResult>(
            this ReadOnlyListValueEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
            => new RightJoinValueEnumerable<ReadOnlyListValueEnumerable<TOuter>, ReadOnlyListValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);

        /// <summary>Joins the sequence with <paramref name="inner"/>, keeping
        /// <paramref name="inner"/>'s elements that have no match.</summary>
        /// <typeparam name="TOuter">The type of the outer elements.</typeparam>
        /// <typeparam name="TInner">The type of the inner elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are joined on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="outer">The outer sequence.</param>
        /// <param name="inner">The inner sequence.</param>
        /// <param name="outerKeySelector">The projection of an outer element to its key.</param>
        /// <param name="innerKeySelector">The projection of an inner element to its key.</param>
        /// <param name="resultSelector">The projection of a matched pair.</param>
        /// <returns>The joined sequence; an unmatched inner element is projected against
        /// <see langword="default"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/>,
        /// <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> RightJoin<TOuter, TInner, TKey, TResult>(
            this EnumerableValueEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector)
            => new EnumerableValueEnumerable<TResult>(
                new RightJoinValueEnumerable<EnumerableValueEnumerable<TOuter>, EnumerableValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null));

        /// <summary>Joins the sequence with <paramref name="inner"/> on keys compared with
        /// <paramref name="comparer"/>, keeping <paramref name="inner"/>'s elements that have no
        /// match.</summary>
        /// <typeparam name="TOuter">The type of the outer elements.</typeparam>
        /// <typeparam name="TInner">The type of the inner elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are joined on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="outer">The outer sequence.</param>
        /// <param name="inner">The inner sequence.</param>
        /// <param name="outerKeySelector">The projection of an outer element to its key.</param>
        /// <param name="innerKeySelector">The projection of an inner element to its key.</param>
        /// <param name="resultSelector">The projection of a matched pair.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The joined sequence; an unmatched inner element is projected against
        /// <see langword="default"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/>,
        /// <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> RightJoin<TOuter, TInner, TKey, TResult>(
            this EnumerableValueEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
            => new EnumerableValueEnumerable<TResult>(
                new RightJoinValueEnumerable<EnumerableValueEnumerable<TOuter>, EnumerableValueEnumerator<TOuter>, TOuter, TInner, TKey, TResult>(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer));
    }
}
#endif
