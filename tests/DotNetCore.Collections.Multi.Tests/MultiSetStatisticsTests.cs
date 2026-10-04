using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// F6-18: the bag statistics helpers <c>Mode()</c> / <c>Median()</c> / <c>Entropy()</c>. Pins the
    /// chosen semantics - all tied modes are returned, the median is the upper middle <em>copy</em>
    /// under the caller's order, the entropy is in bits - and that they read through
    /// <c>EntrySet()</c> (copy counts, not distinct elements) on any <c>IMultiSet&lt;T&gt;</c>.
    /// </summary>
    public class MultiSetStatisticsTests
    {
        // ------------------------------------------------------------------
        // Mode()
        // ------------------------------------------------------------------

        [Fact]
        public void Mode_EmptyBag_ReturnsEmptyList()
        {
            var bag = new MultiList<string>();

            bag.Mode().ShouldBeEmpty();
        }

        [Fact]
        public void Mode_SingleDistinctElement_ReturnsIt()
        {
            var bag = new MultiList<string>();
            bag.Add("a", 5);

            bag.Mode().ShouldBe(new[] { "a" });
        }

        [Fact]
        public void Mode_ReturnsTheSingleMostFrequentElement()
        {
            var bag = new MultiList<string>();
            bag.Add("a", 3);
            bag.Add("b", 1);
            bag.Add("c", 2);

            bag.Mode().ShouldBe(new[] { "a" });
        }

        [Fact]
        public void Mode_Tie_ReturnsEveryTiedElement()
        {
            var bag = new MultiList<string>();
            bag.Add("a", 3);
            bag.Add("b", 3);
            bag.Add("c", 1);

            bag.Mode().ShouldBe(new[] { "a", "b" });
        }

        [Fact]
        public void Mode_WhenEveryElementAppearsOnce_ReturnsEveryElement()
        {
            var bag = new MultiList<string>();
            bag.Add("a");
            bag.Add("b");
            bag.Add("c");

            bag.Mode().ShouldBe(new[] { "a", "b", "c" });
        }

        [Fact]
        public void Mode_NullElementCanBeAMode()
        {
            var bag = new MultiList<string>();
            bag.Add(null, 3);
            bag.Add("x");

            bag.Mode().ShouldBe(new[] { (string)null });
        }

        [Fact]
        public void Mode_NullBag_Throws()
        {
            IMultiSet<string> bag = null;

            Should.Throw<ArgumentNullException>(() => bag.Mode());
        }

        [Fact]
        public void Mode_WorksOnAnyMultiSetImplementation()
        {
            var bag = new OrderedMultiList<int>();
            bag.Add(7, 4);
            bag.Add(9, 4);
            bag.Add(1, 2);

            bag.Mode().ShouldBe(new[] { 7, 9 });
        }

        // ------------------------------------------------------------------
        // Median()
        // ------------------------------------------------------------------

        [Fact]
        public void Median_OddNumberOfCopies_ReturnsTheMiddleElement()
        {
            var bag = new MultiList<int>();
            bag.Add(1);
            bag.Add(2);
            bag.Add(3);

            bag.Median(Comparer<int>.Default).ShouldBe(2);
        }

        [Fact]
        public void Median_EvenNumberOfCopies_ReturnsTheUpperMiddleElement()
        {
            var bag = new MultiList<int>();
            bag.Add(1);
            bag.Add(2);

            bag.Median(Comparer<int>.Default).ShouldBe(2);
        }

        [Fact]
        public void Median_EvenNumberOfCopiesWithRepeatedElements_ReturnsTheUpperMiddle()
        {
            var bag = new MultiList<int>();
            bag.Add(1, 2);
            bag.Add(3, 2);   // copies in order: 1, 1, 3, 3 - upper middle is 3

            bag.Median(Comparer<int>.Default).ShouldBe(3);
        }

        [Fact]
        public void Median_SingleCopy_ReturnsIt()
        {
            var bag = new MultiList<int>();
            bag.Add(42);

            bag.Median(Comparer<int>.Default).ShouldBe(42);
        }

        [Fact]
        public void Median_WeighsCopiesNotDistinctElements()
        {
            var bag = new MultiList<int>();
            bag.Add(1);
            bag.Add(2);
            bag.Add(3, 5);   // copies in order: 1, 2, 3, 3, 3, 3, 3 - upper middle is 3

            bag.Median(Comparer<int>.Default).ShouldBe(3);
        }

        [Fact]
        public void Median_RespectsTheSuppliedComparer()
        {
            var bag = new MultiList<int>();
            bag.Add(1);
            bag.Add(2);
            bag.Add(3);
            bag.Add(4);

            // Ascending: copies 1, 2, 3, 4 -> upper middle is 3. Descending: 4, 3, 2, 1 -> 2.
            bag.Median(Comparer<int>.Default).ShouldBe(3);
            bag.Median(new ReverseComparer<int>()).ShouldBe(2);
        }

        [Fact]
        public void Median_IsAlwaysAnElementOfTheBag()
        {
            var bag = new MultiList<int>();
            bag.Add(10);
            bag.Add(20);

            // Never an interpolated 15: a multiset need not be numeric.
            bag.Median(Comparer<int>.Default).ShouldBe(20);
        }

        [Fact]
        public void Median_EmptyBag_Throws()
        {
            var bag = new MultiList<int>();

            Should.Throw<InvalidOperationException>(() => bag.Median(Comparer<int>.Default));
        }

        [Fact]
        public void Median_NullBag_Throws()
        {
            IMultiSet<int> bag = null;

            Should.Throw<ArgumentNullException>(() => bag.Median(Comparer<int>.Default));
        }

        [Fact]
        public void Median_NullComparer_Throws()
        {
            var bag = new MultiList<int>();
            bag.Add(1);

            Should.Throw<ArgumentNullException>(() => bag.Median(null));
        }

        [Fact]
        public void Median_WorksOnAnyMultiSetImplementation()
        {
            var bag = new OrderedMultiList<int>();
            bag.Add(5);
            bag.Add(6);
            bag.Add(7);

            bag.Median(Comparer<int>.Default).ShouldBe(6);
        }

        // ------------------------------------------------------------------
        // Entropy()
        // ------------------------------------------------------------------

        [Fact]
        public void Entropy_EmptyBag_IsZero()
        {
            var bag = new MultiList<string>();

            bag.Entropy().ShouldBe(0d, 1e-12);
        }

        [Fact]
        public void Entropy_SingleDistinctElement_IsZero()
        {
            var bag = new MultiList<string>();
            bag.Add("a", 100);

            bag.Entropy().ShouldBe(0d, 1e-12);
        }

        [Fact]
        public void Entropy_TwoEqualCopies_IsOneBit()
        {
            var bag = new MultiList<string>();
            bag.Add("a", 2);
            bag.Add("b", 2);

            bag.Entropy().ShouldBe(1d, 1e-12);
        }

        [Fact]
        public void Entropy_UniformOverFourElements_IsTwoBits()
        {
            var bag = new MultiList<string>();
            bag.Add("a");
            bag.Add("b");
            bag.Add("c");
            bag.Add("d");

            bag.Entropy().ShouldBe(2d, 1e-12);
        }

        [Fact]
        public void Entropy_SkewedDistribution_MatchesTheShannonFormula()
        {
            var bag = new MultiList<string>();
            bag.Add("a", 3);
            bag.Add("b", 1);   // p = 0.75 / 0.25

            var expected = -(0.75 * Math.Log(0.75, 2d) + 0.25 * Math.Log(0.25, 2d));

            bag.Entropy().ShouldBe(expected, 1e-12);
        }

        [Fact]
        public void Entropy_NullBag_Throws()
        {
            IMultiSet<string> bag = null;

            Should.Throw<ArgumentNullException>(() => bag.Entropy());
        }

        [Fact]
        public void Entropy_WorksOnAnyMultiSetImplementation()
        {
            var bag = new OrderedMultiList<int>();
            bag.Add(1, 4);
            bag.Add(2, 4);

            bag.Entropy().ShouldBe(1d, 1e-12);
        }

        // ------------------------------------------------------------------

        private sealed class ReverseComparer<T> : IComparer<T>
        {
            private readonly IComparer<T> _inner = Comparer<T>.Default;

            public int Compare(T x, T y) => _inner.Compare(y, x);
        }
    }
}
