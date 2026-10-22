using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The full outer joins of the P2 layer: <c>FullJoin</c> and <c>FullGroupJoin</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A full join is the one join shape the BCL has never shipped. .NET 10 added <c>LeftJoin</c> and
    /// <c>RightJoin</c>; a full join needs both sides to be allowed to go unmatched, which no release
    /// up to .NET 10 offers, so this is a polyfill on every target rather than an accelerated arm
    /// above some TFM. Following <c>R7-07</c> it is filed as a gap-filler, not as a differentiator.
    /// </para>
    /// <para>
    /// The dispatch is the four-source one the rest of the engine uses, applied to the sequence the
    /// operator drives: the receiver is the side whose order the output follows, and the other side
    /// is folded into a lookup. Unlike <c>LeftJoin</c> the contiguous arms cannot return a struct
    /// enumerable: the result is a two-phase state machine (index the inner side, stream the outer
    /// side, then emit what the inner side kept back), and the engine writes that as an iterator.
    /// All four arms therefore return the same <see cref="EnumerableValueEnumerable{T}"/>, which is
    /// what an <see cref="IEnumerable{T}"/> already needs.
    /// </para>
    /// <para>
    /// Both operators are <see langword="null"/>-key tolerant and neither constrains <c>TKey</c> to
    /// <c>notnull</c>, matching <c>LeftJoin</c> / <c>RightJoin</c>. See
    /// <see cref="OuterJoinCore"/> for what a null key means on each side.
    /// </para>
    /// </remarks>
    public static partial class CollectionExtensions
    {
        // ----- FullJoin -----

        /// <summary>Joins the array with <paramref name="inner"/>, keeping the elements of both sides
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
        /// <returns>The joined sequence: matched pairs first, in outer source order, then every
        /// unmatched inner element projected against <see langword="default"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/>,
        /// <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> FullJoin<TOuter, TInner, TKey, TResult>(
            this ArrayValueEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector)
            => FullJoinCore(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);

        /// <summary>Joins the array with <paramref name="inner"/> on keys compared with
        /// <paramref name="comparer"/>, keeping the elements of both sides that have no
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
        /// <returns>The joined sequence: matched pairs first, in outer source order, then every
        /// unmatched inner element projected against <see langword="default"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/>,
        /// <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> FullJoin<TOuter, TInner, TKey, TResult>(
            this ArrayValueEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
            => FullJoinCore(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);

        /// <summary>Joins the list with <paramref name="inner"/>, keeping the elements of both sides
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
        /// <returns>The joined sequence: matched pairs first, in outer source order, then every
        /// unmatched inner element projected against <see langword="default"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/>,
        /// <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> FullJoin<TOuter, TInner, TKey, TResult>(
            this ListValueEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector)
            => FullJoinCore(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);

        /// <summary>Joins the list with <paramref name="inner"/> on keys compared with
        /// <paramref name="comparer"/>, keeping the elements of both sides that have no
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
        /// <returns>The joined sequence: matched pairs first, in outer source order, then every
        /// unmatched inner element projected against <see langword="default"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/>,
        /// <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> FullJoin<TOuter, TInner, TKey, TResult>(
            this ListValueEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
            => FullJoinCore(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);

        /// <summary>Joins the read-only list with <paramref name="inner"/>, keeping the elements of
        /// both sides that have no match.</summary>
        /// <typeparam name="TOuter">The type of the outer elements.</typeparam>
        /// <typeparam name="TInner">The type of the inner elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are joined on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="outer">The outer sequence.</param>
        /// <param name="inner">The inner sequence.</param>
        /// <param name="outerKeySelector">The projection of an outer element to its key.</param>
        /// <param name="innerKeySelector">The projection of an inner element to its key.</param>
        /// <param name="resultSelector">The projection of a matched pair.</param>
        /// <returns>The joined sequence: matched pairs first, in outer source order, then every
        /// unmatched inner element projected against <see langword="default"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/>,
        /// <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> FullJoin<TOuter, TInner, TKey, TResult>(
            this ReadOnlyListValueEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector)
            => FullJoinCore(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);

        /// <summary>Joins the read-only list with <paramref name="inner"/> on keys compared with
        /// <paramref name="comparer"/>, keeping the elements of both sides that have no
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
        /// <returns>The joined sequence: matched pairs first, in outer source order, then every
        /// unmatched inner element projected against <see langword="default"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/>,
        /// <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> FullJoin<TOuter, TInner, TKey, TResult>(
            this ReadOnlyListValueEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
            => FullJoinCore(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);

        /// <summary>Joins the sequence with <paramref name="inner"/>, keeping the elements of both
        /// sides that have no match.</summary>
        /// <typeparam name="TOuter">The type of the outer elements.</typeparam>
        /// <typeparam name="TInner">The type of the inner elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are joined on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="outer">The outer sequence.</param>
        /// <param name="inner">The inner sequence.</param>
        /// <param name="outerKeySelector">The projection of an outer element to its key.</param>
        /// <param name="innerKeySelector">The projection of an inner element to its key.</param>
        /// <param name="resultSelector">The projection of a matched pair.</param>
        /// <returns>The joined sequence: matched pairs first, in outer source order, then every
        /// unmatched inner element projected against <see langword="default"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/>,
        /// <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> FullJoin<TOuter, TInner, TKey, TResult>(
            this EnumerableValueEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector)
            => FullJoinCore(outer, inner, outerKeySelector, innerKeySelector, resultSelector, null);

        /// <summary>Joins the sequence with <paramref name="inner"/> on keys compared with
        /// <paramref name="comparer"/>, keeping the elements of both sides that have no
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
        /// <returns>The joined sequence: matched pairs first, in outer source order, then every
        /// unmatched inner element projected against <see langword="default"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="inner"/>,
        /// <paramref name="outerKeySelector"/>, <paramref name="innerKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> FullJoin<TOuter, TInner, TKey, TResult>(
            this EnumerableValueEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
            => FullJoinCore(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer);

        // ----- FullGroupJoin -----

        /// <summary>Groups the array and <paramref name="second"/> by key and yields one row per
        /// distinct key, including the keys only one side knows.</summary>
        /// <typeparam name="TFirst">The type of the first side's elements.</typeparam>
        /// <typeparam name="TSecond">The type of the second side's elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are grouped on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="first">The first sequence.</param>
        /// <param name="second">The second sequence.</param>
        /// <param name="firstKeySelector">The projection of a first-side element to its key.</param>
        /// <param name="secondKeySelector">The projection of a second-side element to its key.</param>
        /// <param name="resultSelector">The projection of a key and the two groups under it.</param>
        /// <returns>One row per distinct key, in first-seen order across both sides; the side with no
        /// element for a key contributes an empty group.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/>,
        /// <paramref name="firstKeySelector"/>, <paramref name="secondKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> FullGroupJoin<TFirst, TSecond, TKey, TResult>(
            this ArrayValueEnumerable<TFirst> first, IEnumerable<TSecond> second,
            Func<TFirst, TKey> firstKeySelector, Func<TSecond, TKey> secondKeySelector,
            Func<TKey, IEnumerable<TFirst>, IEnumerable<TSecond>, TResult> resultSelector)
            => FullGroupJoinCore(first, second, firstKeySelector, secondKeySelector, resultSelector, null);

        /// <summary>Groups the array and <paramref name="second"/> by key compared with
        /// <paramref name="comparer"/> and yields one row per distinct key, including the keys only
        /// one side knows.</summary>
        /// <typeparam name="TFirst">The type of the first side's elements.</typeparam>
        /// <typeparam name="TSecond">The type of the second side's elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are grouped on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="first">The first sequence.</param>
        /// <param name="second">The second sequence.</param>
        /// <param name="firstKeySelector">The projection of a first-side element to its key.</param>
        /// <param name="secondKeySelector">The projection of a second-side element to its key.</param>
        /// <param name="resultSelector">The projection of a key and the two groups under it.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>One row per distinct key, in first-seen order across both sides; the side with no
        /// element for a key contributes an empty group.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/>,
        /// <paramref name="firstKeySelector"/>, <paramref name="secondKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> FullGroupJoin<TFirst, TSecond, TKey, TResult>(
            this ArrayValueEnumerable<TFirst> first, IEnumerable<TSecond> second,
            Func<TFirst, TKey> firstKeySelector, Func<TSecond, TKey> secondKeySelector,
            Func<TKey, IEnumerable<TFirst>, IEnumerable<TSecond>, TResult> resultSelector,
            IEqualityComparer<TKey>? comparer)
            => FullGroupJoinCore(first, second, firstKeySelector, secondKeySelector, resultSelector, comparer);

        /// <summary>Groups the list and <paramref name="second"/> by key and yields one row per
        /// distinct key, including the keys only one side knows.</summary>
        /// <typeparam name="TFirst">The type of the first side's elements.</typeparam>
        /// <typeparam name="TSecond">The type of the second side's elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are grouped on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="first">The first sequence.</param>
        /// <param name="second">The second sequence.</param>
        /// <param name="firstKeySelector">The projection of a first-side element to its key.</param>
        /// <param name="secondKeySelector">The projection of a second-side element to its key.</param>
        /// <param name="resultSelector">The projection of a key and the two groups under it.</param>
        /// <returns>One row per distinct key, in first-seen order across both sides; the side with no
        /// element for a key contributes an empty group.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/>,
        /// <paramref name="firstKeySelector"/>, <paramref name="secondKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> FullGroupJoin<TFirst, TSecond, TKey, TResult>(
            this ListValueEnumerable<TFirst> first, IEnumerable<TSecond> second,
            Func<TFirst, TKey> firstKeySelector, Func<TSecond, TKey> secondKeySelector,
            Func<TKey, IEnumerable<TFirst>, IEnumerable<TSecond>, TResult> resultSelector)
            => FullGroupJoinCore(first, second, firstKeySelector, secondKeySelector, resultSelector, null);

        /// <summary>Groups the list and <paramref name="second"/> by key compared with
        /// <paramref name="comparer"/> and yields one row per distinct key, including the keys only
        /// one side knows.</summary>
        /// <typeparam name="TFirst">The type of the first side's elements.</typeparam>
        /// <typeparam name="TSecond">The type of the second side's elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are grouped on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="first">The first sequence.</param>
        /// <param name="second">The second sequence.</param>
        /// <param name="firstKeySelector">The projection of a first-side element to its key.</param>
        /// <param name="secondKeySelector">The projection of a second-side element to its key.</param>
        /// <param name="resultSelector">The projection of a key and the two groups under it.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>One row per distinct key, in first-seen order across both sides; the side with no
        /// element for a key contributes an empty group.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/>,
        /// <paramref name="firstKeySelector"/>, <paramref name="secondKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> FullGroupJoin<TFirst, TSecond, TKey, TResult>(
            this ListValueEnumerable<TFirst> first, IEnumerable<TSecond> second,
            Func<TFirst, TKey> firstKeySelector, Func<TSecond, TKey> secondKeySelector,
            Func<TKey, IEnumerable<TFirst>, IEnumerable<TSecond>, TResult> resultSelector,
            IEqualityComparer<TKey>? comparer)
            => FullGroupJoinCore(first, second, firstKeySelector, secondKeySelector, resultSelector, comparer);

        /// <summary>Groups the read-only list and <paramref name="second"/> by key and yields one row
        /// per distinct key, including the keys only one side knows.</summary>
        /// <typeparam name="TFirst">The type of the first side's elements.</typeparam>
        /// <typeparam name="TSecond">The type of the second side's elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are grouped on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="first">The first sequence.</param>
        /// <param name="second">The second sequence.</param>
        /// <param name="firstKeySelector">The projection of a first-side element to its key.</param>
        /// <param name="secondKeySelector">The projection of a second-side element to its key.</param>
        /// <param name="resultSelector">The projection of a key and the two groups under it.</param>
        /// <returns>One row per distinct key, in first-seen order across both sides; the side with no
        /// element for a key contributes an empty group.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/>,
        /// <paramref name="firstKeySelector"/>, <paramref name="secondKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> FullGroupJoin<TFirst, TSecond, TKey, TResult>(
            this ReadOnlyListValueEnumerable<TFirst> first, IEnumerable<TSecond> second,
            Func<TFirst, TKey> firstKeySelector, Func<TSecond, TKey> secondKeySelector,
            Func<TKey, IEnumerable<TFirst>, IEnumerable<TSecond>, TResult> resultSelector)
            => FullGroupJoinCore(first, second, firstKeySelector, secondKeySelector, resultSelector, null);

        /// <summary>Groups the read-only list and <paramref name="second"/> by key compared with
        /// <paramref name="comparer"/> and yields one row per distinct key, including the keys only
        /// one side knows.</summary>
        /// <typeparam name="TFirst">The type of the first side's elements.</typeparam>
        /// <typeparam name="TSecond">The type of the second side's elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are grouped on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="first">The first sequence.</param>
        /// <param name="second">The second sequence.</param>
        /// <param name="firstKeySelector">The projection of a first-side element to its key.</param>
        /// <param name="secondKeySelector">The projection of a second-side element to its key.</param>
        /// <param name="resultSelector">The projection of a key and the two groups under it.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>One row per distinct key, in first-seen order across both sides; the side with no
        /// element for a key contributes an empty group.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/>,
        /// <paramref name="firstKeySelector"/>, <paramref name="secondKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> FullGroupJoin<TFirst, TSecond, TKey, TResult>(
            this ReadOnlyListValueEnumerable<TFirst> first, IEnumerable<TSecond> second,
            Func<TFirst, TKey> firstKeySelector, Func<TSecond, TKey> secondKeySelector,
            Func<TKey, IEnumerable<TFirst>, IEnumerable<TSecond>, TResult> resultSelector,
            IEqualityComparer<TKey>? comparer)
            => FullGroupJoinCore(first, second, firstKeySelector, secondKeySelector, resultSelector, comparer);

        /// <summary>Groups the sequence and <paramref name="second"/> by key and yields one row per
        /// distinct key, including the keys only one side knows.</summary>
        /// <typeparam name="TFirst">The type of the first side's elements.</typeparam>
        /// <typeparam name="TSecond">The type of the second side's elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are grouped on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="first">The first sequence.</param>
        /// <param name="second">The second sequence.</param>
        /// <param name="firstKeySelector">The projection of a first-side element to its key.</param>
        /// <param name="secondKeySelector">The projection of a second-side element to its key.</param>
        /// <param name="resultSelector">The projection of a key and the two groups under it.</param>
        /// <returns>One row per distinct key, in first-seen order across both sides; the side with no
        /// element for a key contributes an empty group.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/>,
        /// <paramref name="firstKeySelector"/>, <paramref name="secondKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> FullGroupJoin<TFirst, TSecond, TKey, TResult>(
            this EnumerableValueEnumerable<TFirst> first, IEnumerable<TSecond> second,
            Func<TFirst, TKey> firstKeySelector, Func<TSecond, TKey> secondKeySelector,
            Func<TKey, IEnumerable<TFirst>, IEnumerable<TSecond>, TResult> resultSelector)
            => FullGroupJoinCore(first, second, firstKeySelector, secondKeySelector, resultSelector, null);

        /// <summary>Groups the sequence and <paramref name="second"/> by key compared with
        /// <paramref name="comparer"/> and yields one row per distinct key, including the keys only
        /// one side knows.</summary>
        /// <typeparam name="TFirst">The type of the first side's elements.</typeparam>
        /// <typeparam name="TSecond">The type of the second side's elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are grouped on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="first">The first sequence.</param>
        /// <param name="second">The second sequence.</param>
        /// <param name="firstKeySelector">The projection of a first-side element to its key.</param>
        /// <param name="secondKeySelector">The projection of a second-side element to its key.</param>
        /// <param name="resultSelector">The projection of a key and the two groups under it.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>One row per distinct key, in first-seen order across both sides; the side with no
        /// element for a key contributes an empty group.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/>,
        /// <paramref name="firstKeySelector"/>, <paramref name="secondKeySelector"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> FullGroupJoin<TFirst, TSecond, TKey, TResult>(
            this EnumerableValueEnumerable<TFirst> first, IEnumerable<TSecond> second,
            Func<TFirst, TKey> firstKeySelector, Func<TSecond, TKey> secondKeySelector,
            Func<TKey, IEnumerable<TFirst>, IEnumerable<TSecond>, TResult> resultSelector,
            IEqualityComparer<TKey>? comparer)
            => FullGroupJoinCore(first, second, firstKeySelector, secondKeySelector, resultSelector, comparer);

        // The four arms differ only in the static type of the receiver, so the argument checks and
        // the call into the core are written once. The receiver is boxed into IEnumerable<T> at this
        // point, which is unavoidable for a two-phase join: the core has to walk the outer side and
        // then keep walking it while the inner index is probed.
        private static EnumerableValueEnumerable<TResult> FullJoinCore<TOuter, TInner, TKey, TResult>(
            IEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey>? comparer)
        {
            if (inner is null) throw new ArgumentNullException(nameof(inner));
            if (outerKeySelector is null) throw new ArgumentNullException(nameof(outerKeySelector));
            if (innerKeySelector is null) throw new ArgumentNullException(nameof(innerKeySelector));
            if (resultSelector is null) throw new ArgumentNullException(nameof(resultSelector));

            return new EnumerableValueEnumerable<TResult>(
                OuterJoinCore.FullJoin(outer, inner, outerKeySelector, innerKeySelector, resultSelector, comparer));
        }

        private static EnumerableValueEnumerable<TResult> FullGroupJoinCore<TFirst, TSecond, TKey, TResult>(
            IEnumerable<TFirst> first, IEnumerable<TSecond> second,
            Func<TFirst, TKey> firstKeySelector, Func<TSecond, TKey> secondKeySelector,
            Func<TKey, IEnumerable<TFirst>, IEnumerable<TSecond>, TResult> resultSelector,
            IEqualityComparer<TKey>? comparer)
        {
            if (second is null) throw new ArgumentNullException(nameof(second));
            if (firstKeySelector is null) throw new ArgumentNullException(nameof(firstKeySelector));
            if (secondKeySelector is null) throw new ArgumentNullException(nameof(secondKeySelector));
            if (resultSelector is null) throw new ArgumentNullException(nameof(resultSelector));

            return new EnumerableValueEnumerable<TResult>(
                OuterJoinCore.FullGroupJoin(first, second, firstKeySelector, secondKeySelector, resultSelector, comparer));
        }
    }
}
