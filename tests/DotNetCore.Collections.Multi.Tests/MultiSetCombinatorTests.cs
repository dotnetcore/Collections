using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// F7-09: <see cref="MultiSetCombinators"/> - the Multi module's consumption points for the
    /// engine's <c>Cartesian</c>, <c>Interleave</c> and <c>FullJoin</c>.
    /// </summary>
    /// <remarks>
    /// The combinator is the engine's; what these tests pin is the bag-aware adapter around it.
    /// That adapter makes one decision per operator, and each decision is what the assertions
    /// below are really about: <c>CrossJoin</c> multiplies copy counts, <c>Interleave</c> keeps them
    /// and only fixes an order, and <c>FullJoin</c> expands the cross product per key and projects
    /// the one-sided rows against <see langword="default"/>.
    /// </remarks>
    public class MultiSetCombinatorTests
    {
        // ------------------------------------------------------------------
        // CrossJoin - the engine's Cartesian, with multiplicities multiplied
        // ------------------------------------------------------------------

        [Fact]
        public void CrossJoin_MultipliesMultiplicities()
        {
            var left = Bag(("a", 2));
            var right = Bag((1, 3));

            var product = left.CrossJoin(right);

            product.CountOf((First: "a", Second: 1)).ShouldBe(6);
            product.TotalCount.ShouldBe(6);
            product.DistinctCount.ShouldBe(1);
        }

        [Fact]
        public void CrossJoin_ProducesEveryDistinctPair()
        {
            var left = Bag(("a", 1), ("b", 1));
            var right = Bag((1, 1), (2, 1), (3, 1));

            var product = left.CrossJoin(right);

            product.DistinctCount.ShouldBe(6);
            product.TotalCount.ShouldBe(6);
        }

        [Fact]
        public void CrossJoin_WithAnEmptySide_IsEmpty()
        {
            var left = Bag(("a", 2));
            var empty = new MultiList<int>();

            left.CrossJoin(empty).TotalCount.ShouldBe(0);
            empty.CrossJoin(left).TotalCount.ShouldBe(0);
        }

        [Fact]
        public void CrossJoin_HandlesTheNullElement()
        {
            var left = new MultiList<string> { null, null };
            var right = Bag((1, 1));

            var product = left.CrossJoin(right);

            product.CountOf((First: null, Second: 1)).ShouldBe(2);
        }

        [Fact]
        public void CrossJoin_ThrowsOnNull()
        {
            var left = Bag(("a", 1));
            var right = Bag((1, 1));
            IMultiSet<string> nullBag = null;

            Should.Throw<ArgumentNullException>(() => nullBag.CrossJoin(right));
            Should.Throw<ArgumentNullException>(() => left.CrossJoin((IMultiSet<int>)null));
        }

        // ------------------------------------------------------------------
        // Interleave - the engine's Interleave over the expanded copies
        // ------------------------------------------------------------------

        [Fact]
        public void Interleave_AlternatesOnePerRound()
        {
            var left = Bag(("a", 2));
            var right = Bag(("b", 3));

            var interleaved = left.Interleave(right);

            interleaved.ShouldBe(new[] { "a", "b", "a", "b", "b" });
        }

        [Fact]
        public void Interleave_ContinuesWithTheLongerSide()
        {
            var left = Bag(("a", 1));
            var right = Bag(("b", 4));

            left.Interleave(right).ShouldBe(new[] { "a", "b", "b", "b", "b" });
        }

        [Fact]
        public void Interleave_KeepsTheTotalCountOfBothSides()
        {
            var left = Bag((1, 3), (2, 1));
            var right = Bag((3, 2));

            var interleaved = left.Interleave(right);

            interleaved.Count.ShouldBe(left.TotalCount + right.TotalCount);
        }

        [Fact]
        public void Interleave_OfTwoEmptyMultisetsIsEmpty()
        {
            new MultiList<int>().Interleave(new MultiList<int>()).ShouldBeEmpty();
        }

        [Fact]
        public void Interleave_ThrowsOnNull()
        {
            var bag = Bag((1, 1));
            IMultiSet<int> nullBag = null;

            Should.Throw<ArgumentNullException>(() => nullBag.Interleave(bag));
            Should.Throw<ArgumentNullException>(() => bag.Interleave((IMultiSet<int>)null));
        }

        // ------------------------------------------------------------------
        // FullJoin - the engine's FullJoin, keyed
        // ------------------------------------------------------------------

        [Fact]
        public void FullJoin_MatchesOnKeyAndKeepsBothOneSidedRows()
        {
            var left = Bag((1, 1), (2, 1));
            var right = Bag((2, 1), (3, 1));

            var rows = left.FullJoin(right, x => x, x => x, (l, r) => (Left: l, Right: r));

            rows.Count.ShouldBe(3);
            rows.Count(row => row.Left == 2 && row.Right == 2).ShouldBe(1);
            rows.Count(row => row.Left == 1 && row.Right == 0).ShouldBe(1);
            rows.Count(row => row.Left == 0 && row.Right == 3).ShouldBe(1);
        }

        [Fact]
        public void FullJoin_RespectsMultiplicityOnBothSides()
        {
            var left = Bag((1, 2));
            var right = Bag((1, 3));

            var rows = left.FullJoin(right, x => x, x => x, (l, r) => (Left: l, Right: r));

            rows.Count.ShouldBe(6);
            rows.ShouldAllBe(row => row.Left == 1 && row.Right == 1);
        }

        [Fact]
        public void FullJoin_WithOnlyOneSide_EmitsThatSide()
        {
            var left = Bag((1, 1), (2, 1));

            var rows = left.FullJoin(new MultiList<int>(), x => x, x => x, (l, r) => (Left: l, Right: r));

            rows.Count.ShouldBe(2);
            rows.ShouldAllBe(row => row.Right == 0);
        }

        [Fact]
        public void FullJoin_WithBothSidesEmpty_IsEmpty()
        {
            var rows = new MultiList<int>()
                .FullJoin(new MultiList<int>(), x => x, x => x, (l, r) => l + r);

            rows.ShouldBeEmpty();
        }

        [Fact]
        public void FullJoin_HonoursACustomComparer()
        {
            var left = new MultiList<string> { "A" };
            var right = new MultiList<string> { "a" };

            var rows = left.FullJoin(
                right,
                x => x,
                x => x,
                (l, r) => (Left: l, Right: r),
                StringComparer.OrdinalIgnoreCase);

            rows.Count.ShouldBe(1);
            rows[0].Left.ShouldBe("A");
            rows[0].Right.ShouldBe("a");
        }

        [Fact]
        public void FullJoin_NeverMatchesANullKeyButStillEmitsIt()
        {
            var left = new MultiList<string> { "a", null };
            var right = new MultiList<string> { null, "a" };

            var rows = left.FullJoin(right, x => x, x => x, (l, r) => (Left: l, Right: r));

            // "a" pairs up once; the null on each side has no partner and is emitted alone.
            rows.Count.ShouldBe(3);
            rows.Count(row => row.Left == "a" && row.Right == "a").ShouldBe(1);
            rows.Count(row => row.Left == null && row.Right == null).ShouldBe(2);
        }

        [Fact]
        public void FullJoin_ThrowsOnNullArguments()
        {
            var left = Bag((1, 1));
            var right = Bag((1, 1));
            IMultiSet<int> nullBag = null;

            Should.Throw<ArgumentNullException>(
                () => nullBag.FullJoin(right, x => x, x => x, (l, r) => l));
            Should.Throw<ArgumentNullException>(
                () => left.FullJoin((IMultiSet<int>)null, x => x, x => x, (l, r) => l));
            Should.Throw<ArgumentNullException>(
                () => left.FullJoin(right, (Func<int, int>)null, x => x, (l, r) => l));
            Should.Throw<ArgumentNullException>(
                () => left.FullJoin(right, x => x, (Func<int, int>)null, (l, r) => l));
            Should.Throw<ArgumentNullException>(
                () => left.FullJoin(right, x => x, x => x, (Func<int, int, int>)null));
        }

        private static MultiList<T> Bag<T>(params (T Item, int Count)[] entries)
        {
            var bag = new MultiList<T>();
            foreach (var (item, count) in entries)
            {
                bag.Add(item, count);
            }

            return bag;
        }
    }
}
