using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    public class MultiListSetOperationsTests
    {
        private static MultiList<string> Bag(params (string Item, int Count)[] entries)
        {
            var bag = new MultiList<string>();
            foreach (var entry in entries)
            {
                bag.Add(entry.Item, entry.Count);
            }

            return bag;
        }

        [Fact]
        public void UnionWith_TakesMaximumCopyCount()
        {
            var a = Bag(("a", 2), ("b", 1));

            a.UnionWith(new[] { "a", "c", "c", "c" });

            a.CountOf("a").ShouldBe(2);
            a.CountOf("b").ShouldBe(1);
            a.CountOf("c").ShouldBe(3);
            a.TotalCount.ShouldBe(6);
        }

        [Fact]
        public void UnionWith_WithMultiList_PreservesMultiplicity()
        {
            var a = Bag(("a", 2));
            var b = Bag(("a", 1), ("c", 3));

            a.UnionWith(b);

            a.CountOf("a").ShouldBe(2);
            a.CountOf("c").ShouldBe(3);
            a.TotalCount.ShouldBe(5);
        }

        [Fact]
        public void UnionWith_Self_IsNoOp()
        {
            var a = Bag(("a", 2), ("b", 1));

            a.UnionWith(a);

            a.CountOf("a").ShouldBe(2);
            a.CountOf("b").ShouldBe(1);
        }

        [Fact]
        public void UnionWith_Empty_IsNoOp()
        {
            var a = Bag(("a", 2));

            a.UnionWith(new string[0]);

            a.TotalCount.ShouldBe(2);
        }

        [Fact]
        public void UnionWith_Null_ThrowsArgumentNullException()
        {
            var a = new MultiList<string>();
            Should.Throw<ArgumentNullException>(() => a.UnionWith(null));
        }

        [Fact]
        public void IntersectionWith_TakesMinimumCopyCount()
        {
            var a = Bag(("a", 2), ("b", 1));

            a.IntersectionWith(new[] { "a", "c", "c" });

            a.CountOf("a").ShouldBe(1);
            a.Contains("b").ShouldBeFalse();
            a.TotalCount.ShouldBe(1);
        }

        [Fact]
        public void IntersectionWith_Disjoint_EmptiesBag()
        {
            var a = Bag(("a", 2));

            a.IntersectionWith(new[] { "b" });

            a.TotalCount.ShouldBe(0);
            a.DistinctCount.ShouldBe(0);
        }

        [Fact]
        public void IntersectionWith_Self_IsNoOp()
        {
            var a = Bag(("a", 2), ("b", 1));

            a.IntersectionWith(a);

            a.CountOf("a").ShouldBe(2);
            a.CountOf("b").ShouldBe(1);
        }

        [Fact]
        public void ExceptWith_SubtractsCounts()
        {
            var a = Bag(("a", 3), ("b", 1));

            a.ExceptWith(new[] { "a", "a", "c" });

            a.CountOf("a").ShouldBe(1);
            a.CountOf("b").ShouldBe(1);
            a.TotalCount.ShouldBe(2);
        }

        [Fact]
        public void ExceptWith_LargerOther_RemovesAllCopies()
        {
            var a = Bag(("a", 2));

            a.ExceptWith(new[] { "a", "a", "a", "a" });

            a.CountOf("a").ShouldBe(0);
            a.TotalCount.ShouldBe(0);
        }

        [Fact]
        public void ExceptWith_Self_EmptiesBag()
        {
            var a = Bag(("a", 2), ("b", 1));

            a.ExceptWith(a);

            a.TotalCount.ShouldBe(0);
        }

        [Fact]
        public void SymmetricExceptWith_TakesAbsoluteDifference()
        {
            var a = Bag(("a", 2), ("b", 1));

            a.SymmetricExceptWith(new[] { "a", "c", "c", "c" });

            a.CountOf("a").ShouldBe(1);
            a.CountOf("b").ShouldBe(1);
            a.CountOf("c").ShouldBe(3);
            a.TotalCount.ShouldBe(5);
        }

        [Fact]
        public void SymmetricExceptWith_Self_EmptiesBag()
        {
            var a = Bag(("a", 2));

            a.SymmetricExceptWith(a);

            a.TotalCount.ShouldBe(0);
        }

        [Fact]
        public void IsSubsetOf_RespectsMultiplicities()
        {
            Bag(("a", 2)).IsSubsetOf(new[] { "a", "a", "b" }).ShouldBeTrue();
            Bag(("a", 3)).IsSubsetOf(new[] { "a", "a" }).ShouldBeFalse();
        }

        [Fact]
        public void IsSubsetOf_IgnoresExtraElements()
        {
            Bag(("a", 1)).IsSubsetOf(new[] { "a", "b", "b", "b", "b", "b" }).ShouldBeTrue();
        }

        [Fact]
        public void IsSupersetOf_RespectsMultiplicities()
        {
            Bag(("a", 3)).IsSupersetOf(new[] { "a", "a" }).ShouldBeTrue();
            Bag(("a", 1)).IsSupersetOf(new[] { "a", "a" }).ShouldBeFalse();
        }

        [Fact]
        public void IsProperSubsetOf_True_WhenStrictlySmaller()
        {
            Bag(("a", 1)).IsProperSubsetOf(new[] { "a", "b" }).ShouldBeTrue();
        }

        [Fact]
        public void IsProperSubsetOf_EqualBags_False()
        {
            Bag(("a", 1), ("b", 1)).IsProperSubsetOf(new[] { "a", "b" }).ShouldBeFalse();
        }

        [Fact]
        public void IsProperSupersetOf_True_WhenStrictlyLarger()
        {
            Bag(("a", 2)).IsProperSupersetOf(new[] { "a" }).ShouldBeTrue();
        }

        [Fact]
        public void IsProperSupersetOf_EqualBags_False()
        {
            Bag(("a", 1)).IsProperSupersetOf(new[] { "a" }).ShouldBeFalse();
        }

        [Fact]
        public void Overlaps_True_WhenSharingAnyElement()
        {
            Bag(("a", 1)).Overlaps(new[] { "b", "a" }).ShouldBeTrue();
        }

        [Fact]
        public void Overlaps_False_WhenDisjoint()
        {
            Bag(("a", 1)).Overlaps(new[] { "b", "c" }).ShouldBeFalse();
        }

        [Fact]
        public void Overlaps_Null_ThrowsArgumentNullException()
        {
            var a = Bag(("a", 1));
            Should.Throw<ArgumentNullException>(() => a.Overlaps(null));
        }

        [Fact]
        public void IsDisjointFrom_TrueAndFalse()
        {
            Bag(("a", 1)).IsDisjointFrom(new[] { "b", "c" }).ShouldBeTrue();
            Bag(("a", 1)).IsDisjointFrom(new[] { "b", "a" }).ShouldBeFalse();
        }

        [Fact]
        public void SetOperations_UseComparer()
        {
            var a = new MultiList<string>(StringComparer.OrdinalIgnoreCase);
            a.Add("a", 2);

            a.IntersectionWith(new[] { "A" });

            a.CountOf("a").ShouldBe(1);
        }

        [Fact]
        public void SetOperations_HandleNullElements()
        {
            var a = new MultiList<string>();
            a.Add(null, 2);
            a.Add("x");

            a.UnionWith(new[] { null, "y" });
            a.CountOf(null).ShouldBe(2);
            a.CountOf("y").ShouldBe(1);

            a.IsSubsetOf(new string[] { null, null, "x", "y" }).ShouldBeTrue();

            // ExceptWith removes up to the copy count present in the other collection:
            // 2 null copies - 2 supplied copies = 0 remaining.
            a.ExceptWith(new string[] { null, null });
            a.Contains(null).ShouldBeFalse();
            a.CountOf("x").ShouldBe(1);
        }
    }
}
