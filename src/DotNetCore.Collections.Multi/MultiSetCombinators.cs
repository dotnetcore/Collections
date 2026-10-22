using System;
using System.Collections.Generic;

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Set combinators over <see cref="IMultiSet{T}"/>: the cross product, the interleaving and the
    /// full outer join, each preserving multiplicity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These are the Multi module's consumption points for the engine in
    /// <c>DotNetCore.Collections</c> (rule C: the internals run on this engine instead of falling
    /// back to BCL LINQ). Each method is a bag-aware adapter over exactly one engine operator -
    /// <c>Cartesian</c>, <c>Interleave</c> and <c>FullJoin</c> respectively - so the combinatorial
    /// work itself is the engine's, not a second hand-written implementation.
    /// </para>
    /// <para>
    /// The three differ in what multiplicity means, and that is deliberate:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// <see cref="CrossJoin{TFirst, TSecond}"/> multiplies multiplicities: two copies of
    /// <c>a</c> crossed with three copies of <c>b</c> yields six copies of <c>(a, b)</c>.
    /// </description></item>
    /// <item><description>
    /// <see cref="Interleave{T}"/> keeps each side's multiplicities and only decides the order, so
    /// its result is a sequence rather than a bag.
    /// </description></item>
    /// <item><description>
    /// <see cref="FullJoin{TLeft, TRight, TKey, TResult}"/> emits the cross product of the values
    /// sharing a key, and projects the values whose key exists on one side only against
    /// <see langword="default"/> for the missing side.
    /// </description></item>
    /// </list>
    /// <para>
    /// None of them mutate their inputs; like the rest of the family they return a fresh value.
    /// </para>
    /// </remarks>
    public static class MultiSetCombinators
    {
        /// <summary>
        /// Returns the cross product of two multisets, with multiplicities multiplied.
        /// </summary>
        /// <typeparam name="TFirst">The element type of the first multiset.</typeparam>
        /// <typeparam name="TSecond">The element type of the second multiset.</typeparam>
        /// <param name="first">The first multiset, slowest-varying in the result.</param>
        /// <param name="second">The second multiset, fastest-varying in the result.</param>
        /// <returns>
        /// A multiset holding one entry per distinct pair, whose copy count is the product of the
        /// two sides' copy counts. Empty when either side is empty.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="first"/> or <paramref name="second"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="OverflowException">
        /// A pair's copy count exceeds <see cref="int.MaxValue"/>.
        /// </exception>
        /// <remarks>
        /// The pairs come from the engine's <c>Cartesian</c> over the two <b>distinct</b> element
        /// sequences, and the copy count is then taken from each side's <c>CountOf</c>. Enumerating
        /// the expanded copies instead would produce the same multiset but would materialise
        /// <c>TotalCount(first) x TotalCount(second)</c> tuples to count them.
        /// </remarks>
        /// <example>
        /// <code>
        /// var left = new MultiList&lt;string&gt;();
        /// left.Add("a", 2);
        ///
        /// var right = new MultiList&lt;int&gt;();
        /// right.Add(1, 3);
        ///
        /// var product = left.CrossJoin(right);
        /// product.CountOf(("a", 1));   // 6 - two copies of "a" x three copies of 1
        /// </code>
        /// </example>
        public static MultiList<(TFirst First, TSecond Second)> CrossJoin<TFirst, TSecond>(
            this IMultiSet<TFirst> first, IMultiSet<TSecond> second)
        {
            if (first == null)
            {
                throw new ArgumentNullException(nameof(first));
            }

            if (second == null)
            {
                throw new ArgumentNullException(nameof(second));
            }

            var result = new MultiList<(TFirst First, TSecond Second)>();

            if (first.DistinctCount == 0 || second.DistinctCount == 0)
            {
                return result;
            }

            var pairs = first.DistinctItems()
                .ToValueEnumerable()
                .Cartesian(second.DistinctItems(), (a, b) => (First: a, Second: b));

            foreach (var pair in pairs)
            {
                var copies = checked(first.CountOf(pair.First) * second.CountOf(pair.Second));
                result.Add(pair, copies);
            }

            return result;
        }

        /// <summary>
        /// Returns the elements of two multisets taken alternately, one per round.
        /// </summary>
        /// <typeparam name="T">The element type shared by both multisets.</typeparam>
        /// <param name="first">The multiset that contributes first in every round.</param>
        /// <param name="second">The multiset that contributes second in every round.</param>
        /// <returns>
        /// The interleaved elements: the first copy of <paramref name="first"/>, the first copy of
        /// <paramref name="second"/>, the second copy of <paramref name="first"/>, and so on, with
        /// whichever side runs out leaving the other to continue alone. The length is
        /// <c>TotalCount(first) + TotalCount(second)</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="first"/> or <paramref name="second"/> is <c>null</c>.
        /// </exception>
        /// <remarks>
        /// Both sides are enumerated as their expanded copies, in the order each multiset already
        /// enumerates in, and the engine's <c>Interleave</c> does the alternating. The result is a
        /// read-only list rather than a multiset because the interleaving order is the whole point
        /// of the operation, and a multiset does not carry order - <see cref="MultiList{T}"/>'s
        /// <c>UnionWith</c> is the order-free counterpart.
        /// </remarks>
        /// <example>
        /// <code>
        /// var left = new MultiList&lt;string&gt;();
        /// left.Add("a", 2);
        ///
        /// var right = new MultiList&lt;string&gt;();
        /// right.Add("b", 3);
        ///
        /// left.Interleave(right);   // a, b, a, b, b
        /// </code>
        /// </example>
        public static IReadOnlyList<T> Interleave<T>(this IMultiSet<T> first, IMultiSet<T> second)
        {
            if (first == null)
            {
                throw new ArgumentNullException(nameof(first));
            }

            if (second == null)
            {
                throw new ArgumentNullException(nameof(second));
            }

            return new List<T>(first.ToValueEnumerable().Interleave(second));
        }

        /// <summary>
        /// Returns the full outer join of two multisets on a key, keeping the elements of both
        /// sides that have no match.
        /// </summary>
        /// <typeparam name="TLeft">The element type of the left multiset.</typeparam>
        /// <typeparam name="TRight">The element type of the right multiset.</typeparam>
        /// <typeparam name="TKey">The type of the key the two sides are joined on.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="left">The left multiset, whose order the matched results follow.</param>
        /// <param name="right">The right multiset.</param>
        /// <param name="leftKeySelector">The projection of a left element to its key.</param>
        /// <param name="rightKeySelector">The projection of a right element to its key.</param>
        /// <param name="resultSelector">
        /// The projection of a pair. The unmatched side of a one-sided row arrives as
        /// <see langword="default"/>, which is the engine's <c>FullJoin</c> contract.
        /// </param>
        /// <param name="comparer">
        /// The comparer of keys, or <see langword="null"/> for the default one.
        /// </param>
        /// <returns>
        /// The joined rows: every left element in left-multiset order, where a matched left element
        /// expands to one row per matching right element and an unmatched one to a single row with
        /// <see langword="default"/> on the right; then every right element whose key no left
        /// element claimed, again with <see langword="default"/> on the left. Multiplicity is
        /// respected - two left values and three right values sharing a key produce six matched
        /// rows.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="left"/>, <paramref name="right"/>, <paramref name="leftKeySelector"/>,
        /// <paramref name="rightKeySelector"/> or <paramref name="resultSelector"/> is
        /// <c>null</c>.
        /// </exception>
        /// <remarks>
        /// A key that is <see langword="null"/> never matches, but the element carrying it is still
        /// emitted as a one-sided row - the same split the engine applies to its own
        /// <c>FullJoin</c> and <c>FullGroupJoin</c>.
        /// </remarks>
        /// <example>
        /// <code>
        /// var left = new MultiList&lt;int&gt;();
        /// left.Add(1);
        /// left.Add(2);
        ///
        /// var right = new MultiList&lt;int&gt;();
        /// right.Add(2);
        /// right.Add(3);
        ///
        /// var rows = left.FullJoin(right, x =&gt; x, x =&gt; x, (l, r) =&gt; $"{l}-{r}");
        /// // ["1-0", "2-2", "0-3"] - 0 is default(int) on the unmatched side
        /// </code>
        /// </example>
        public static IReadOnlyList<TResult> FullJoin<TLeft, TRight, TKey, TResult>(
            this IMultiSet<TLeft> left,
            IMultiSet<TRight> right,
            Func<TLeft, TKey> leftKeySelector,
            Func<TRight, TKey> rightKeySelector,
            Func<TLeft, TRight, TResult> resultSelector,
            IEqualityComparer<TKey>? comparer = null)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            if (leftKeySelector == null)
            {
                throw new ArgumentNullException(nameof(leftKeySelector));
            }

            if (rightKeySelector == null)
            {
                throw new ArgumentNullException(nameof(rightKeySelector));
            }

            if (resultSelector == null)
            {
                throw new ArgumentNullException(nameof(resultSelector));
            }

            var joined = left.ToValueEnumerable().FullJoin(
                right,
                leftKeySelector,
                rightKeySelector,
                resultSelector,
                comparer);

            return new List<TResult>(joined);
        }
    }
}
