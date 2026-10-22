using System;
using System.Collections.Generic;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The cartesian-product operator of the P2 layer: <c>Cartesian</c>, over two and over three
    /// sources.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The operator exists to collapse the <c>SelectMany</c> nesting a hand-written product needs:
    /// <c>a.Cartesian(b, (x, y) =&gt; ...)</c> instead of
    /// <c>a.SelectMany(x =&gt; b, (x, y) =&gt; ...)</c>. Two sources cover that case; the three-source
    /// overloads exist because the pair-of-pairs form forces an intermediate tuple through the
    /// result selector, and because the third source is common enough in practice to be worth the
    /// four arms.
    /// </para>
    /// <para>
    /// Higher arities are reached by chaining: the result is itself an
    /// <see cref="EnumerableValueEnumerable{T}"/>, so <c>a.Cartesian(b, ...).Cartesian(c, ...)</c>
    /// composes without leaving the engine. That is deliberate - every arity up to eight would
    /// otherwise need its own four arms, and the arity is an overload axis here while the
    /// four-source dispatch is the receiver axis.
    /// </para>
    /// <para>
    /// Two properties are worth stating because they are easy to assume away. Every source is
    /// enumerated once per element of the sources before it, so a source must be re-enumerable (a
    /// sequence, not a one-shot cursor). And an empty source yields an empty product, because a
    /// product with a zero factor has no rows.
    /// </para>
    /// <para>
    /// The receiver is engine-owned, so the product is entered through the usual
    /// <c>ToValueEnumerable()</c> wrappers. The secondary sources are plain
    /// <see cref="IEnumerable{T}"/>, which is what they would be boxed into anyway - there is no
    /// single value-type enumerator to carry when there are two or three of them.
    /// </para>
    /// </remarks>
    public static partial class CollectionExtensions
    {
        // ----- Cartesian (two sources) -----

        /// <summary>Produces the product of the array and <paramref name="second"/>, projecting each
        /// pair.</summary>
        /// <typeparam name="TFirst">The type of the first source's elements.</typeparam>
        /// <typeparam name="TSecond">The type of the second source's elements.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="first">The first source, outermost in the output order.</param>
        /// <param name="second">The second source, innermost in the output order.</param>
        /// <param name="resultSelector">The projection of a pair.</param>
        /// <returns>Every pair, first source slowest-varying.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> Cartesian<TFirst, TSecond, TResult>(
            this ArrayValueEnumerable<TFirst> first, IEnumerable<TSecond> second,
            Func<TFirst, TSecond, TResult> resultSelector)
            => CartesianCore(first, second, resultSelector);

        /// <summary>Produces the product of the list and <paramref name="second"/>, projecting each
        /// pair.</summary>
        /// <typeparam name="TFirst">The type of the first source's elements.</typeparam>
        /// <typeparam name="TSecond">The type of the second source's elements.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="first">The first source, outermost in the output order.</param>
        /// <param name="second">The second source, innermost in the output order.</param>
        /// <param name="resultSelector">The projection of a pair.</param>
        /// <returns>Every pair, first source slowest-varying.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> Cartesian<TFirst, TSecond, TResult>(
            this ListValueEnumerable<TFirst> first, IEnumerable<TSecond> second,
            Func<TFirst, TSecond, TResult> resultSelector)
            => CartesianCore(first, second, resultSelector);

        /// <summary>Produces the product of the read-only list and <paramref name="second"/>,
        /// projecting each pair.</summary>
        /// <typeparam name="TFirst">The type of the first source's elements.</typeparam>
        /// <typeparam name="TSecond">The type of the second source's elements.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="first">The first source, outermost in the output order.</param>
        /// <param name="second">The second source, innermost in the output order.</param>
        /// <param name="resultSelector">The projection of a pair.</param>
        /// <returns>Every pair, first source slowest-varying.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> Cartesian<TFirst, TSecond, TResult>(
            this ReadOnlyListValueEnumerable<TFirst> first, IEnumerable<TSecond> second,
            Func<TFirst, TSecond, TResult> resultSelector)
            => CartesianCore(first, second, resultSelector);

        /// <summary>Produces the product of the sequence and <paramref name="second"/>, projecting
        /// each pair.</summary>
        /// <typeparam name="TFirst">The type of the first source's elements.</typeparam>
        /// <typeparam name="TSecond">The type of the second source's elements.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="first">The first source, outermost in the output order.</param>
        /// <param name="second">The second source, innermost in the output order.</param>
        /// <param name="resultSelector">The projection of a pair.</param>
        /// <returns>Every pair, first source slowest-varying.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> or
        /// <paramref name="resultSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> Cartesian<TFirst, TSecond, TResult>(
            this EnumerableValueEnumerable<TFirst> first, IEnumerable<TSecond> second,
            Func<TFirst, TSecond, TResult> resultSelector)
            => CartesianCore(first, second, resultSelector);

        // ----- Cartesian (three sources) -----

        /// <summary>Produces the product of the array, <paramref name="second"/> and
        /// <paramref name="third"/>, projecting each triple.</summary>
        /// <typeparam name="TFirst">The type of the first source's elements.</typeparam>
        /// <typeparam name="TSecond">The type of the second source's elements.</typeparam>
        /// <typeparam name="TThird">The type of the third source's elements.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="first">The first source, outermost in the output order.</param>
        /// <param name="second">The second source, in the middle.</param>
        /// <param name="third">The third source, innermost in the output order.</param>
        /// <param name="resultSelector">The projection of a triple.</param>
        /// <returns>Every triple, first source slowest-varying.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/>,
        /// <paramref name="third"/> or <paramref name="resultSelector"/> is
        /// <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> Cartesian<TFirst, TSecond, TThird, TResult>(
            this ArrayValueEnumerable<TFirst> first, IEnumerable<TSecond> second, IEnumerable<TThird> third,
            Func<TFirst, TSecond, TThird, TResult> resultSelector)
            => CartesianCore(first, second, third, resultSelector);

        /// <summary>Produces the product of the list, <paramref name="second"/> and
        /// <paramref name="third"/>, projecting each triple.</summary>
        /// <typeparam name="TFirst">The type of the first source's elements.</typeparam>
        /// <typeparam name="TSecond">The type of the second source's elements.</typeparam>
        /// <typeparam name="TThird">The type of the third source's elements.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="first">The first source, outermost in the output order.</param>
        /// <param name="second">The second source, in the middle.</param>
        /// <param name="third">The third source, innermost in the output order.</param>
        /// <param name="resultSelector">The projection of a triple.</param>
        /// <returns>Every triple, first source slowest-varying.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/>,
        /// <paramref name="third"/> or <paramref name="resultSelector"/> is
        /// <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> Cartesian<TFirst, TSecond, TThird, TResult>(
            this ListValueEnumerable<TFirst> first, IEnumerable<TSecond> second, IEnumerable<TThird> third,
            Func<TFirst, TSecond, TThird, TResult> resultSelector)
            => CartesianCore(first, second, third, resultSelector);

        /// <summary>Produces the product of the read-only list, <paramref name="second"/> and
        /// <paramref name="third"/>, projecting each triple.</summary>
        /// <typeparam name="TFirst">The type of the first source's elements.</typeparam>
        /// <typeparam name="TSecond">The type of the second source's elements.</typeparam>
        /// <typeparam name="TThird">The type of the third source's elements.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="first">The first source, outermost in the output order.</param>
        /// <param name="second">The second source, in the middle.</param>
        /// <param name="third">The third source, innermost in the output order.</param>
        /// <param name="resultSelector">The projection of a triple.</param>
        /// <returns>Every triple, first source slowest-varying.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/>,
        /// <paramref name="third"/> or <paramref name="resultSelector"/> is
        /// <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> Cartesian<TFirst, TSecond, TThird, TResult>(
            this ReadOnlyListValueEnumerable<TFirst> first, IEnumerable<TSecond> second, IEnumerable<TThird> third,
            Func<TFirst, TSecond, TThird, TResult> resultSelector)
            => CartesianCore(first, second, third, resultSelector);

        /// <summary>Produces the product of the sequence, <paramref name="second"/> and
        /// <paramref name="third"/>, projecting each triple.</summary>
        /// <typeparam name="TFirst">The type of the first source's elements.</typeparam>
        /// <typeparam name="TSecond">The type of the second source's elements.</typeparam>
        /// <typeparam name="TThird">The type of the third source's elements.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="first">The first source, outermost in the output order.</param>
        /// <param name="second">The second source, in the middle.</param>
        /// <param name="third">The third source, innermost in the output order.</param>
        /// <param name="resultSelector">The projection of a triple.</param>
        /// <returns>Every triple, first source slowest-varying.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/>,
        /// <paramref name="third"/> or <paramref name="resultSelector"/> is
        /// <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> Cartesian<TFirst, TSecond, TThird, TResult>(
            this EnumerableValueEnumerable<TFirst> first, IEnumerable<TSecond> second, IEnumerable<TThird> third,
            Func<TFirst, TSecond, TThird, TResult> resultSelector)
            => CartesianCore(first, second, third, resultSelector);

        private static EnumerableValueEnumerable<TResult> CartesianCore<TFirst, TSecond, TResult>(
            IEnumerable<TFirst> first, IEnumerable<TSecond> second,
            Func<TFirst, TSecond, TResult> resultSelector)
        {
            if (second is null) throw new ArgumentNullException(nameof(second));
            if (resultSelector is null) throw new ArgumentNullException(nameof(resultSelector));

            return new EnumerableValueEnumerable<TResult>(Cartesian2(first, second, resultSelector));
        }

        private static EnumerableValueEnumerable<TResult> CartesianCore<TFirst, TSecond, TThird, TResult>(
            IEnumerable<TFirst> first, IEnumerable<TSecond> second, IEnumerable<TThird> third,
            Func<TFirst, TSecond, TThird, TResult> resultSelector)
        {
            if (second is null) throw new ArgumentNullException(nameof(second));
            if (third is null) throw new ArgumentNullException(nameof(third));
            if (resultSelector is null) throw new ArgumentNullException(nameof(resultSelector));

            return new EnumerableValueEnumerable<TResult>(Cartesian3(first, second, third, resultSelector));
        }

        // The loops are stacked without braces on purpose: that is the shape a product has, and
        // bracing each level would push the innermost statement of a wide product off to the right
        // for no gain in clarity.
        private static IEnumerable<TResult> Cartesian2<TFirst, TSecond, TResult>(
            IEnumerable<TFirst> first, IEnumerable<TSecond> second,
            Func<TFirst, TSecond, TResult> resultSelector)
        {
            foreach (var a in first)
            foreach (var b in second)
                yield return resultSelector(a, b);
        }

        private static IEnumerable<TResult> Cartesian3<TFirst, TSecond, TThird, TResult>(
            IEnumerable<TFirst> first, IEnumerable<TSecond> second, IEnumerable<TThird> third,
            Func<TFirst, TSecond, TThird, TResult> resultSelector)
        {
            foreach (var a in first)
            foreach (var b in second)
            foreach (var c in third)
                yield return resultSelector(a, b, c);
        }
    }
}
