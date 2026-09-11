using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    // F6-01 / L-10: OrderedMultiList<T>, the ordered counterpart of MultiList<T>, backed by a
    // left-leaning red-black tree (R2-02).
    //
    // What the suite has to establish, in the order the acceptance criteria state it:
    //
    //  1. Enumeration is sorted ascending and the multiset semantics are exactly MultiList<T>'s -
    //     one distinct element with N copies, duplicates consecutive and expanded, the same set
    //     operations. The parity is not asserted case by case against hand-written expectations
    //     alone: SetOperationsMatchMultiList runs both types over the same random inputs and
    //     demands identical results, because "the same multiset semantics" is a claim about the
    //     two types agreeing, not about either one in isolation.
    //  2. O(log n) is proved by red-black invariants plus the height bound, never by timings
    //     (R2-02 explicitly rules out timing baselines - they are noise in CI). AssertBalanced
    //     re-checks the invariants and asserts height <= 2*log2(n+1); SequentialInsertsStay-
    //     Logarithmic adds the case a naive binary search tree would fail catastrophically on.
    //  3. Null elements are supported and sort first under Comparer<T>.Default, with the handling
    //     delegated to the comparer - so a custom comparer can also put null last.
    //  4. The comparer is an IComparer<T>, deliberately unlike MultiList<T>'s IEqualityComparer<T>,
    //     and it decides element identity as well as order (comparison 0 == the same element).
    public class OrderedMultiListTests
    {
        // ------------------------------------------------------------ helpers

        private static void AssertBalanced<T>(OrderedMultiList<T> list, string context)
        {
            list.ValidateTree(out var error).ShouldBeTrue(
                context + ": the red-black invariants must hold, but " + (error ?? "(no error reported)"));

            var distinct = list.DistinctCount;
            var bound = 2.0 * Math.Log(distinct + 1, 2);
            ((double)list.TreeHeight).ShouldBeLessThanOrEqualTo(
                bound + 0.000001,
                context + ": height " + list.TreeHeight + " must stay within 2*log2(n+1) = " + bound
                + " for n = " + distinct);
        }

        private static int[] Sequence(Random random, int length, int distinct)
        {
            var values = new int[length];
            for (var i = 0; i < length; i++)
            {
                values[i] = random.Next(distinct);
            }

            return values;
        }

        // Compares an ordered multiset with a plain one by content only: the whole point of the
        // pair is that they agree on what is stored while disagreeing on enumeration order.
        private static void ShouldMatch(OrderedMultiList<int> ordered, MultiList<int> plain, string operation)
        {
            ordered.TotalCount.ShouldBe(plain.TotalCount, operation + ": TotalCount");
            ordered.DistinctCount.ShouldBe(plain.DistinctCount, operation + ": DistinctCount");
            ordered.EntrySet().Select(e => e.Item + ":" + e.Count).OrderBy(s => s, StringComparer.Ordinal)
                .ShouldBe(
                    plain.EntrySet().Select(e => e.Item + ":" + e.Count).OrderBy(s => s, StringComparer.Ordinal),
                    operation + ": contents");
        }

        private sealed class NullLastComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                if (x == null && y == null)
                {
                    return 0;
                }

                if (x == null)
                {
                    return 1;
                }

                return y == null ? -1 : string.CompareOrdinal(x, y);
            }
        }

        private sealed class DescendingComparer : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return y.CompareTo(x);
            }
        }

        // ------------------------------------------------------------ construction

        [Fact]
        public void DefaultsToTheDefaultComparer()
        {
            var list = new OrderedMultiList<int>();

            list.Comparer.ShouldBeSameAs(Comparer<int>.Default);
            list.TotalCount.ShouldBe(0);
            list.DistinctCount.ShouldBe(0);
            list.IsReadOnly.ShouldBeFalse();
            list.ToList().ShouldBeEmpty();
            AssertBalanced(list, "a fresh multiset");
        }

        [Fact]
        public void KeepsTheComparerItWasGiven()
        {
            var comparer = new DescendingComparer();
            var list = new OrderedMultiList<int>(comparer);

            list.Comparer.ShouldBeSameAs(comparer);
        }

        [Fact]
        public void BuildsFromACollectionAccumulatingDuplicates()
        {
            var list = new OrderedMultiList<int>(new[] { 3, 1, 3, 2, 3 });

            list.TotalCount.ShouldBe(5);
            list.DistinctCount.ShouldBe(3);
            list.CountOf(3).ShouldBe(3);
            list.ToList().ShouldBe(new[] { 1, 2, 3, 3, 3 });
        }

        [Fact]
        public void BuildsFromACollectionWithAComparer()
        {
            var list = new OrderedMultiList<int>(new[] { 1, 2, 3 }, new DescendingComparer());

            list.Comparer.ShouldBeAssignableTo<DescendingComparer>();
            list.ToList().ShouldBe(new[] { 3, 2, 1 });
        }

        [Fact]
        public void RejectsANullCollection()
        {
            Should.Throw<ArgumentNullException>(() => new OrderedMultiList<int>((IEnumerable<int>)null));
        }

        [Fact]
        public void TreatsANullComparerAsTheDefault()
        {
            var list = new OrderedMultiList<int>((IComparer<int>)null);

            list.Comparer.ShouldBeSameAs(Comparer<int>.Default);
        }

        // ------------------------------------------------------------ sorted enumeration

        [Fact]
        public void EnumeratesAscendingWithDuplicatesExpanded()
        {
            var list = new OrderedMultiList<int>();
            list.AddRange(new[] { 5, 1, 3, 1, 5, 5 });

            list.ToList().ShouldBe(new[] { 1, 1, 3, 5, 5, 5 });
            list.ToArray().ShouldBe(new[] { 1, 1, 3, 5, 5, 5 });
            list.ToString().ShouldBe("1,1,3,5,5,5");
        }

        [Fact]
        public void ListsDistinctElementsInAscendingOrder()
        {
            var list = new OrderedMultiList<int>();
            list.AddRange(new[] { 5, 1, 3, 1 });

            list.DistinctItems().ShouldBe(new[] { 1, 3, 5 });
            list.EntrySet().ShouldBe(new[] { (1, 2), (3, 1), (5, 1) });
        }

        [Fact]
        public void CopiesToTheTargetArrayInAscendingOrder()
        {
            var list = new OrderedMultiList<int>();
            list.AddRange(new[] { 5, 1, 3, 1 });
            var array = new int[4];

            list.CopyTo(array, 0);

            array.ShouldBe(new[] { 1, 1, 3, 5 });
        }

        [Fact]
        public void CopyToValidatesItsArguments()
        {
            var list = new OrderedMultiList<int>();
            list.Add(1);

            Should.Throw<ArgumentNullException>(() => list.CopyTo(null, 0));
            Should.Throw<ArgumentOutOfRangeException>(() => list.CopyTo(new int[4], -1));
            Should.Throw<ArgumentException>(() => list.CopyTo(new int[4], 4));
        }

        [Fact]
        public void CountingAndMembershipFollowTheCopyCounts()
        {
            var list = new OrderedMultiList<int>();
            list.AddRange(new[] { 1, 1, 2 });

            list.CountOf(1).ShouldBe(2);
            list.CountOf(9).ShouldBe(0);
            list.Contains(1).ShouldBeTrue();
            list.Contains(9).ShouldBeFalse();
            list.ContainsAll(new[] { 1, 2 }).ShouldBeTrue();
            list.ContainsAll(new[] { 1, 9 }).ShouldBeFalse();
            list.TotalCount.ShouldBe(3);
            list.DistinctCount.ShouldBe(2);
        }

        [Fact]
        public void ContainsAllRejectsANullCollection()
        {
            var list = new OrderedMultiList<int>();

            Should.Throw<ArgumentNullException>(() => list.ContainsAll(null));
        }

        [Fact]
        public void AddRangeRejectsANullCollection()
        {
            var list = new OrderedMultiList<int>();

            Should.Throw<ArgumentNullException>(() => list.AddRange(null));
        }

        // ------------------------------------------------------------ ordered extras

        [Fact]
        public void GetFirstAndGetLastAreTheSmallestAndLargestElements()
        {
            var list = new OrderedMultiList<int>();
            list.AddRange(new[] { 7, 2, 9, 2 });

            list.GetFirst().ShouldBe(2);
            list.GetLast().ShouldBe(9);
        }

        [Fact]
        public void GetFirstAndGetLastThrowWhenEmpty()
        {
            var list = new OrderedMultiList<int>();

            Should.Throw<InvalidOperationException>(() => list.GetFirst());
            Should.Throw<InvalidOperationException>(() => list.GetLast());
        }

        [Fact]
        public void ReverseEnumeratesDescendingWithDuplicatesExpanded()
        {
            var list = new OrderedMultiList<int>();
            list.AddRange(new[] { 5, 1, 3, 1 });

            list.Reverse().ShouldBe(new[] { 5, 3, 1, 1 });
        }

        [Fact]
        public void ReverseOfAnEmptyMultisetIsEmpty()
        {
            new OrderedMultiList<int>().Reverse().ShouldBeEmpty();
        }

        [Fact]
        public void GetRangeIncludesBothBoundsByDefault()
        {
            var list = new OrderedMultiList<int>();
            list.AddRange(new[] { 1, 2, 3, 3, 4, 5 });

            list.GetRange(2, 4).ShouldBe(new[] { 2, 3, 3, 4 });
        }

        [Fact]
        public void GetRangeHonoursTheInclusivityFlags()
        {
            var list = new OrderedMultiList<int>();
            list.AddRange(new[] { 1, 2, 3, 4 });

            list.GetRange(2, 3, false, true).ShouldBe(new[] { 3 });
            list.GetRange(2, 3, true, false).ShouldBe(new[] { 2 });
            list.GetRange(2, 3, false, false).ShouldBeEmpty();
        }

        [Fact]
        public void GetRangeOnASingleElementHonoursTheFlags()
        {
            var list = new OrderedMultiList<int>();
            list.AddRange(new[] { 1, 2, 2, 3 });

            list.GetRange(2, 2).ShouldBe(new[] { 2, 2 });
            list.GetRange(2, 2, false, true).ShouldBeEmpty();
            list.GetRange(2, 2, true, false).ShouldBeEmpty();
        }

        [Fact]
        public void GetRangeIsEmptyWhenTheBoundsAreInverted()
        {
            var list = new OrderedMultiList<int>();
            list.AddRange(new[] { 1, 2, 3 });

            list.GetRange(3, 1).ShouldBeEmpty();
        }

        [Fact]
        public void GetRangeToleratesBoundsOutsideTheStoredRange()
        {
            var list = new OrderedMultiList<int>();
            list.AddRange(new[] { 10, 20, 30 });

            list.GetRange(-100, 100).ShouldBe(new[] { 10, 20, 30 });
            list.GetRange(-100, 15).ShouldBe(new[] { 10 });
            list.GetRange(25, 100).ShouldBe(new[] { 30 });
            list.GetRange(100, 200).ShouldBeEmpty();
        }

        [Fact]
        public void GetRangeOfAnEmptyMultisetIsEmpty()
        {
            new OrderedMultiList<int>().GetRange(0, 10).ShouldBeEmpty();
        }

        [Fact]
        public void GetRangeAgreesWithALinqFilterOverRandomBounds()
        {
            var random = new Random(20260910);
            var list = new OrderedMultiList<int>();
            for (var i = 0; i < 400; i++)
            {
                list.Add(random.Next(0, 50), random.Next(1, 4));
            }

            var everything = list.ToList();
            for (var trial = 0; trial < 300; trial++)
            {
                var from = random.Next(-5, 55);
                var to = random.Next(-5, 55);
                var inclusiveFrom = random.Next(2) == 0;
                var inclusiveTo = random.Next(2) == 0;

                var expected = everything.Where(v =>
                    (inclusiveFrom ? v >= from : v > from) &&
                    (inclusiveTo ? v <= to : v < to));

                list.GetRange(from, to, inclusiveFrom, inclusiveTo).ShouldBe(expected,
                    "GetRange(" + from + ", " + to + ", " + inclusiveFrom + ", " + inclusiveTo + ")");
            }

            AssertBalanced(list, "after 300 range queries");
        }

        [Fact]
        public void ReverseAgreesWithALinqReversal()
        {
            var random = new Random(31337);
            var list = new OrderedMultiList<int>();
            list.AddRange(Sequence(random, 500, 40));

            list.Reverse().ShouldBe(list.ToList().AsEnumerable().Reverse());
        }

        // ------------------------------------------------------------ null elements

        [Fact]
        public void NullElementsAreSupportedAndSortFirst()
        {
            var list = new OrderedMultiList<string>();
            list.Add("b");
            list.Add(null);
            list.Add("a");
            list.Add(null);

            list.TotalCount.ShouldBe(4);
            list.DistinctCount.ShouldBe(3);
            list.ToList().ShouldBe(new[] { null, null, "a", "b" });
            list.CountOf(null).ShouldBe(2);
            list.Contains(null).ShouldBeTrue();
            list.GetFirst().ShouldBeNull();
            list.GetLast().ShouldBe("b");
            list.DistinctItems().ShouldBe(new[] { null, "a", "b" });
            list.EntrySet().ShouldBe(new[] { ((string)null, 2), ("a", 1), ("b", 1) });
        }

        [Fact]
        public void NullParticipatesInRangesAndRemoval()
        {
            var list = new OrderedMultiList<string>();
            list.Add("b");
            list.Add(null);
            list.Add("a");

            list.GetRange(null, "a").ShouldBe(new[] { null, "a" });
            list.Remove(null).ShouldBe(0);
            list.Contains(null).ShouldBeFalse();
            list.DistinctCount.ShouldBe(2);
            AssertBalanced(list, "after removing the null element");
        }

        [Fact]
        public void CustomComparerDecidesWhereNullGoes()
        {
            // "Null elements are supported, but where they belong is the comparer's decision."
            // This one puts null last instead of first, and the type simply follows it.
            var list = new OrderedMultiList<string>(new NullLastComparer());
            list.Add("b");
            list.Add(null);
            list.Add("a");

            list.ToList().ShouldBe(new[] { "a", "b", null });
            list.GetFirst().ShouldBe("a");
            list.GetLast().ShouldBeNull();
        }

        // ------------------------------------------------------------ the comparer defines identity

        [Fact]
        public void ComparerEqualElementsShareOneNodeAndKeepTheFirstKey()
        {
            // StringComparer implements both IComparer<string> and IEqualityComparer<string>,
            // which makes it the natural example of a comparer that folds two spellings together.
            var list = new OrderedMultiList<string>(StringComparer.OrdinalIgnoreCase);
            list.Add("Alpha");
            list.Add("alpha");
            list.Add("ALPHA");

            list.DistinctCount.ShouldBe(1);
            list.TotalCount.ShouldBe(3);
            list.CountOf("aLpHa").ShouldBe(3);
            list.EntrySet().Single().Item.ShouldBe("Alpha");
        }

        [Fact]
        public void TheComparerDefinesTheEnumeratedOrder()
        {
            var list = new OrderedMultiList<int>(new DescendingComparer());
            list.AddRange(new[] { 1, 5, 3, 5 });

            list.ToList().ShouldBe(new[] { 5, 5, 3, 1 });
            list.GetFirst().ShouldBe(5);
            list.GetLast().ShouldBe(1);
            list.Reverse().ShouldBe(new[] { 1, 3, 5, 5 });
        }

        // ------------------------------------------------------------ removal and key lifecycle

        [Fact]
        public void RemoveTakesOneCopyAtATimeAndDropsTheElementAtZero()
        {
            var list = new OrderedMultiList<int>();
            list.Add(7, 3);

            list.Remove(7).ShouldBe(2);
            list.Remove(7, 1).ShouldBe(1);
            list.DistinctCount.ShouldBe(1);
            list.Remove(7).ShouldBe(0);
            list.DistinctCount.ShouldBe(0);
            list.TotalCount.ShouldBe(0);
            AssertBalanced(list, "after draining the last element");
        }

        [Fact]
        public void RemoveClampsToTheNumberOfCopiesPresent()
        {
            var list = new OrderedMultiList<int>();
            list.Add(7, 2);

            list.Remove(7, 10).ShouldBe(0);
            list.TotalCount.ShouldBe(0);
            list.DistinctCount.ShouldBe(0);
        }

        [Fact]
        public void RemoveOfAnAbsentElementIsANoOp()
        {
            var list = new OrderedMultiList<int>();
            list.Add(1);

            list.Remove(9).ShouldBe(0);
            list.RemoveAllCopies(9).ShouldBeFalse();
            list.TotalCount.ShouldBe(1);
        }

        [Fact]
        public void RemoveAllCopiesDropsTheElementInOneCall()
        {
            var list = new OrderedMultiList<int>();
            list.Add(7, 5);
            list.Add(8);

            list.RemoveAllCopies(7).ShouldBeTrue();

            list.DistinctCount.ShouldBe(1);
            list.TotalCount.ShouldBe(1);
            list.Contains(7).ShouldBeFalse();
            AssertBalanced(list, "after RemoveAllCopies");
        }

        [Fact]
        public void ClearEmptiesTheTree()
        {
            var list = new OrderedMultiList<int>();
            list.AddRange(Sequence(new Random(5), 200, 50));

            list.Clear();

            list.TotalCount.ShouldBe(0);
            list.DistinctCount.ShouldBe(0);
            list.TreeHeight.ShouldBe(0);
            list.ToList().ShouldBeEmpty();
            list.ValidateTree(out var error).ShouldBeTrue(error ?? "the cleared tree must be valid");
        }

        // M6-05: times <= 0 now throws ArgumentOutOfRangeException on both types, shared
        // verbatim with MultiList<T>. These tests pin the breaking change.
        [Fact]
        public void NonPositiveTimesThrowsOnAdd()
        {
            var list = new OrderedMultiList<int>();

            Should.Throw<ArgumentOutOfRangeException>(() => list.Add(1, 0));
            Should.Throw<ArgumentOutOfRangeException>(() => list.Add(2, -5));

            list.TotalCount.ShouldBe(0);
        }

        [Fact]
        public void NonPositiveTimesThrowsOnRemove()
        {
            var list = new OrderedMultiList<int>();
            list.Add(1, 3);

            Should.Throw<ArgumentOutOfRangeException>(() => list.Remove(1, 0));
            Should.Throw<ArgumentOutOfRangeException>(() => list.Remove(1, -5));

            list.CountOf(1).ShouldBe(3);
        }

        // ------------------------------------------------------------ clone and views

        [Fact]
        public void CloneIsIndependentAndKeepsTheComparer()
        {
            var list = new OrderedMultiList<int>();
            list.AddRange(new[] { 3, 1, 3 });
            var clone = list.Clone();

            clone.Comparer.ShouldBeSameAs(list.Comparer);
            clone.ToList().ShouldBe(new[] { 1, 3, 3 });

            clone.Add(2);
            list.Contains(2).ShouldBeFalse();
            list.DistinctCount.ShouldBe(2);
            clone.DistinctCount.ShouldBe(3);
            AssertBalanced(clone, "the clone after diverging");
        }

        [Fact]
        public void AsReadOnlyIsALiveSortedView()
        {
            var list = new OrderedMultiList<int>();
            list.AddRange(new[] { 3, 1 });
            var view = list.AsReadOnly();

            view.Count.ShouldBe(2);
            view.ShouldBe(new[] { 1, 3 });

            list.Add(0);

            view.Count.ShouldBe(3);
            view.ShouldBe(new[] { 0, 1, 3 });
        }

        [Fact]
        public void ExplicitCollectionMembersAgreeWithThePublicOnes()
        {
            var list = new OrderedMultiList<int>();
            list.AddRange(new[] { 2, 2 });

            ((ICollection<int>)list).Count.ShouldBe(2);
            ((IReadOnlyCollection<int>)list).Count.ShouldBe(2);
            ((ICollection<int>)list).Remove(2).ShouldBeTrue();
            ((ICollection<int>)list).Remove(9).ShouldBeFalse();
            list.TotalCount.ShouldBe(1);
        }

        // ------------------------------------------------------------ parity with MultiList

        [Fact]
        public void SetOperationsMatchMultiList()
        {
            var random = new Random(99);

            for (var trial = 0; trial < 60; trial++)
            {
                var left = Sequence(random, 30, 10);
                var right = Sequence(random, 20, 10);

                var ordered = new OrderedMultiList<int>(left);
                var plain = new MultiList<int>(left);
                ordered.UnionWith(right);
                plain.UnionWith(right);
                ShouldMatch(ordered, plain, "UnionWith");

                ordered = new OrderedMultiList<int>(left);
                plain = new MultiList<int>(left);
                ordered.IntersectionWith(right);
                plain.IntersectionWith(right);
                ShouldMatch(ordered, plain, "IntersectionWith");

                ordered = new OrderedMultiList<int>(left);
                plain = new MultiList<int>(left);
                ordered.ExceptWith(right);
                plain.ExceptWith(right);
                ShouldMatch(ordered, plain, "ExceptWith");

                ordered = new OrderedMultiList<int>(left);
                plain = new MultiList<int>(left);
                ordered.SymmetricExceptWith(right);
                plain.SymmetricExceptWith(right);
                ShouldMatch(ordered, plain, "SymmetricExceptWith");

                ordered = new OrderedMultiList<int>(left);
                plain = new MultiList<int>(left);
                ordered.IsSubsetOf(right).ShouldBe(plain.IsSubsetOf(right), "IsSubsetOf");
                ordered.IsSupersetOf(right).ShouldBe(plain.IsSupersetOf(right), "IsSupersetOf");
                ordered.IsProperSubsetOf(right).ShouldBe(plain.IsProperSubsetOf(right), "IsProperSubsetOf");
                ordered.IsProperSupersetOf(right).ShouldBe(plain.IsProperSupersetOf(right), "IsProperSupersetOf");
                ordered.Overlaps(right).ShouldBe(plain.Overlaps(right), "Overlaps");
                ordered.IsDisjointFrom(right).ShouldBe(plain.IsDisjointFrom(right), "IsDisjointFrom");
            }
        }

        [Fact]
        public void SetOperationsKeepTheMultisetSemantics()
        {
            var list = new OrderedMultiList<int>();
            list.AddRange(new[] { 1, 1, 2 });

            list.UnionWith(new[] { 1, 1, 1, 3 });
            list.CountOf(1).ShouldBe(3);
            list.ToList().ShouldBe(new[] { 1, 1, 1, 2, 3 });

            list.IntersectionWith(new[] { 1, 1, 3 });
            list.CountOf(1).ShouldBe(2);
            list.ToList().ShouldBe(new[] { 1, 1, 3 });

            list.ExceptWith(new[] { 1 });
            list.ToList().ShouldBe(new[] { 1, 3 });

            list.SymmetricExceptWith(new[] { 1, 1, 1 });
            // |1 - 3| = 2 copies of 1, |1 - 0| = 1 copy of 3
            list.ToList().ShouldBe(new[] { 1, 1, 3 });
            AssertBalanced(list, "after the set operations");
        }

        [Fact]
        public void SetOperationsRejectNullArguments()
        {
            var list = new OrderedMultiList<int>();

            Should.Throw<ArgumentNullException>(() => list.UnionWith(null));
            Should.Throw<ArgumentNullException>(() => list.IntersectionWith(null));
            Should.Throw<ArgumentNullException>(() => list.ExceptWith(null));
            Should.Throw<ArgumentNullException>(() => list.SymmetricExceptWith(null));
            Should.Throw<ArgumentNullException>(() => list.IsSubsetOf(null));
            Should.Throw<ArgumentNullException>(() => list.IsSupersetOf(null));
            Should.Throw<ArgumentNullException>(() => list.IsProperSubsetOf(null));
            Should.Throw<ArgumentNullException>(() => list.IsProperSupersetOf(null));
            Should.Throw<ArgumentNullException>(() => list.Overlaps(null));
            Should.Throw<ArgumentNullException>(() => list.IsDisjointFrom(null));
        }

        // ------------------------------------------------------------ red-black invariants

        [Fact]
        public void SequentialAscendingInsertsStayLogarithmic()
        {
            // The adversarial case for an unbalanced binary search tree: 10,000 ascending keys
            // would make it a linked list. A red-black tree must stay within 2*log2(n+1) ~ 27.
            var list = new OrderedMultiList<int>();
            const int n = 10000;
            for (var i = 0; i < n; i++)
            {
                list.Add(i);
            }

            list.DistinctCount.ShouldBe(n);
            list.TreeHeight.ShouldBeLessThan(30);
            AssertBalanced(list, "after 10,000 ascending inserts");
        }

        [Fact]
        public void SequentialDescendingInsertsStayLogarithmic()
        {
            var list = new OrderedMultiList<int>();
            const int n = 10000;
            for (var i = n - 1; i >= 0; i--)
            {
                list.Add(i);
            }

            list.TreeHeight.ShouldBeLessThan(30);
            list.ToList().ShouldBe(Enumerable.Range(0, n));
            AssertBalanced(list, "after 10,000 descending inserts");
        }

        [Fact]
        public void DuplicateCopiesDoNotGrowTheTree()
        {
            // The tree is sized by distinct elements, so adding 10,000 copies of one element must
            // leave it one node deep.
            var list = new OrderedMultiList<int>();
            list.Add(1, 10000);

            list.DistinctCount.ShouldBe(1);
            list.TreeHeight.ShouldBe(1);
            list.TotalCount.ShouldBe(10000);
            AssertBalanced(list, "after 10,000 copies of one element");
        }

        [Fact]
        public void InvariantsSurviveEverySingleRemoval()
        {
            var random = new Random(1234);
            var list = new OrderedMultiList<int>();
            var keys = new HashSet<int>();
            for (var i = 0; i < 300; i++)
            {
                var key = random.Next(0, 1000);
                keys.Add(key);
                list.Add(key, random.Next(1, 3));
            }

            AssertBalanced(list, "after 300 inserts");

            var order = keys.OrderBy(_ => random.Next()).ToList();
            var removed = 0;
            foreach (var key in order)
            {
                list.RemoveAllCopies(key).ShouldBeTrue();
                removed++;
                AssertBalanced(list, "after removing " + removed + " of " + order.Count + " keys");
            }

            list.DistinctCount.ShouldBe(0);
            list.TotalCount.ShouldBe(0);
            list.TreeHeight.ShouldBe(0);
        }

        [Fact]
        public void RandomOperationsMatchASortedModel()
        {
            var random = new Random(20260910);
            var list = new OrderedMultiList<int>();
            var model = new SortedDictionary<int, int>();

            for (var step = 0; step < 5000; step++)
            {
                var key = random.Next(0, 40);
                var times = random.Next(1, 4);

                switch (random.Next(3))
                {
                    case 0:
                        list.Add(key, times);
                        model[key] = (model.TryGetValue(key, out var current) ? current : 0) + times;
                        break;

                    case 1:
                        list.Remove(key, times);
                        if (model.TryGetValue(key, out var existing))
                        {
                            var remaining = existing - times;
                            if (remaining <= 0)
                            {
                                model.Remove(key);
                            }
                            else
                            {
                                model[key] = remaining;
                            }
                        }

                        break;

                    default:
                        list.RemoveAllCopies(key);
                        model.Remove(key);
                        break;
                }
            }

            list.EntrySet().Select(e => e.Item + ":" + e.Count)
                .ShouldBe(model.Select(p => p.Key + ":" + p.Value));
            list.TotalCount.ShouldBe(model.Values.Sum());
            list.DistinctCount.ShouldBe(model.Count);
            AssertBalanced(list, "after 5,000 random operations");
        }

        [Fact]
        public void InterleavedAddsAndRemovesKeepTheInvariants()
        {
            var random = new Random(777);
            var list = new OrderedMultiList<int>();

            for (var step = 0; step < 2000; step++)
            {
                var key = random.Next(0, 200);
                switch (random.Next(3))
                {
                    case 0:
                        list.Add(key, random.Next(1, 3));
                        break;
                    case 1:
                        list.Remove(key, random.Next(1, 3));
                        break;
                    default:
                        list.RemoveAllCopies(key);
                        break;
                }

                if (step % 50 == 0)
                {
                    AssertBalanced(list, "at step " + step);
                }
            }

            AssertBalanced(list, "after 2,000 interleaved operations");
        }

        [Fact]
        public void MixedTypeElementsAreOrderedByTheirComparer()
        {
            var list = new OrderedMultiList<DateTime>();
            var origin = new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc);
            list.Add(origin.AddDays(2));
            list.Add(origin);
            list.Add(origin.AddDays(1));
            list.Add(origin);

            list.ToList().ShouldBe(new[] { origin, origin, origin.AddDays(1), origin.AddDays(2) });
            list.GetRange(origin, origin.AddDays(1)).ShouldBe(new[] { origin, origin, origin.AddDays(1) });
            AssertBalanced(list, "with DateTime elements");
        }
    }
}
