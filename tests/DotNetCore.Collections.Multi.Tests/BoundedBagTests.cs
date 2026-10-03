using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// F6-16: <see cref="BoundedBag{T}"/> - the bounded bag. Covers the capacity contract, the
    /// first-in-first-out eviction policy (including ring wrap-around), the bag semantics shared
    /// with the rest of the family (counting, copy-expanded enumeration, removal returning the
    /// remaining copies), the projection members, <c>null</c> handling and the comparer.
    /// </summary>
    public class BoundedBagTests
    {
        // ------------------------------------------------------------------
        // Construction and counts
        // ------------------------------------------------------------------

        [Fact]
        public void Ctor_Empty_CountsAreZero()
        {
            var bag = new BoundedBag<int>(4);

            bag.Capacity.ShouldBe(4);
            bag.Count.ShouldBe(0);
            bag.TotalCount.ShouldBe(0);
            bag.DistinctCount.ShouldBe(0);
            bag.IsEmpty.ShouldBeTrue();
            bag.IsFull.ShouldBeFalse();
            bag.ShouldBeEmpty();
        }

        [Fact]
        public void Ctor_NonPositiveCapacity_ThrowsArgumentOutOfRangeException()
        {
            Should.Throw<ArgumentOutOfRangeException>(() => new BoundedBag<int>(0));
            Should.Throw<ArgumentOutOfRangeException>(() => new BoundedBag<int>(-1));
        }

        [Fact]
        public void Ctor_WithoutComparer_UsesDefault()
        {
            var bag = new BoundedBag<string>(3);

            bag.Comparer.ShouldBeSameAs(EqualityComparer<string>.Default);
        }

        [Fact]
        public void Ctor_WithComparer_UsesIt()
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            var bag = new BoundedBag<string>(3, comparer);

            bag.Comparer.ShouldBeSameAs(comparer);
        }

        [Fact]
        public void Count_IsAliasOfTotalCount()
        {
            var bag = new BoundedBag<string>(3);
            bag.Add("a");
            bag.Add("a");

            bag.Count.ShouldBe(bag.TotalCount);
            bag.Count.ShouldBe(2);
        }

        // ------------------------------------------------------------------
        // Add and first-in-first-out eviction
        // ------------------------------------------------------------------

        [Fact]
        public void Add_WithinCapacity_AccumulatesWithoutEviction()
        {
            var bag = new BoundedBag<string>(3);
            bag.Add("a");
            bag.Add("a");
            bag.Add("b");

            bag.TotalCount.ShouldBe(3);
            bag.DistinctCount.ShouldBe(2);
            bag.CountOf("a").ShouldBe(2);
            bag.CountOf("b").ShouldBe(1);
            bag.IsFull.ShouldBeTrue();
        }

        [Fact]
        public void Add_BeyondCapacity_EvictsOldestCopy()
        {
            var bag = new BoundedBag<string>(3);
            bag.AddRange(new[] { "a", "b", "c" });
            bag.Add("d");

            bag.TotalCount.ShouldBe(3);
            bag.Contains("a").ShouldBeFalse();
            bag.ToList().ShouldBe(new[] { "b", "c", "d" });
        }

        [Fact]
        public void Add_BeyondCapacity_EvictsOnlyOneCopyOfTheOldestElement()
        {
            var bag = new BoundedBag<string>(4);
            bag.AddRange(new[] { "a", "a", "b", "b" });
            bag.Add("c");

            // "a" loses its single oldest copy, the other copy survives.
            bag.CountOf("a").ShouldBe(1);
            bag.CountOf("b").ShouldBe(2);
            bag.CountOf("c").ShouldBe(1);
            bag.ToList().ShouldBe(new[] { "a", "b", "b", "c" });
        }

        [Fact]
        public void Add_WithTimes_AccumulatesMultiplicities()
        {
            var bag = new BoundedBag<string>(5);
            bag.Add("a", 3);

            bag.CountOf("a").ShouldBe(3);
            bag.TotalCount.ShouldBe(3);
            bag.DistinctCount.ShouldBe(1);
        }

        [Fact]
        public void Add_NonPositiveTimes_ThrowsArgumentOutOfRangeException()
        {
            var bag = new BoundedBag<string>(3);
            Should.Throw<ArgumentOutOfRangeException>(() => bag.Add("a", 0));
            Should.Throw<ArgumentOutOfRangeException>(() => bag.Add("a", -1));
        }

        [Fact]
        public void Add_TimesEqualToCapacity_FillsTheBagWithThatElement()
        {
            var bag = new BoundedBag<string>(3);
            bag.AddRange(new[] { "x", "y", "z" });
            bag.Add("a", 3);

            bag.TotalCount.ShouldBe(3);
            bag.DistinctCount.ShouldBe(1);
            bag.CountOf("a").ShouldBe(3);
            bag.ToList().ShouldBe(new[] { "a", "a", "a" });
        }

        [Fact]
        public void Add_TimesGreaterThanCapacity_DoesNotLoopForeverAndFillsTheBag()
        {
            var bag = new BoundedBag<string>(2);
            bag.AddRange(new[] { "x", "y" });
            bag.Add("a", int.MaxValue);

            bag.TotalCount.ShouldBe(2);
            bag.DistinctCount.ShouldBe(1);
            bag.CountOf("a").ShouldBe(2);
            bag.ToList().ShouldBe(new[] { "a", "a" });
        }

        [Fact]
        public void Add_CapacityOne_AlwaysKeepsTheNewestCopy()
        {
            var bag = new BoundedBag<string>(1);
            bag.Add("a");
            bag.Add("b");

            bag.TotalCount.ShouldBe(1);
            bag.Contains("a").ShouldBeFalse();
            bag.ToList().ShouldBe(new[] { "b" });
        }

        [Fact]
        public void Add_WrappedAroundManyTimes_KeepsTheMostRecentWindow()
        {
            var bag = new BoundedBag<int>(3);
            for (var i = 1; i <= 10; i++)
            {
                bag.Add(i);
            }

            bag.TotalCount.ShouldBe(3);
            bag.ToList().ShouldBe(new[] { 8, 9, 10 });
            bag.Contains(7).ShouldBeFalse();
        }

        [Fact]
        public void AddRange_Null_ThrowsArgumentNullException()
        {
            var bag = new BoundedBag<int>(3);
            Should.Throw<ArgumentNullException>(() => bag.AddRange(null!));
        }

        [Fact]
        public void AddRange_OrderMatters_EarlierElementsAreEvictedFirst()
        {
            var bag = new BoundedBag<string>(2);
            bag.AddRange(new[] { "a", "b", "c" });

            bag.ToList().ShouldBe(new[] { "b", "c" });
        }

        // ------------------------------------------------------------------
        // Lookup
        // ------------------------------------------------------------------

        [Fact]
        public void Contains_And_CountOf_ReportPresenceAndMultiplicity()
        {
            var bag = new BoundedBag<string>(4);
            bag.AddRange(new[] { "a", "a", "b" });

            bag.Contains("a").ShouldBeTrue();
            bag.Contains("b").ShouldBeTrue();
            bag.Contains("c").ShouldBeFalse();
            bag.CountOf("a").ShouldBe(2);
            bag.CountOf("c").ShouldBe(0);
        }

        // ------------------------------------------------------------------
        // Remove
        // ------------------------------------------------------------------

        [Fact]
        public void Remove_OneCopy_ReturnsRemainingAndKeepsOrder()
        {
            var bag = new BoundedBag<string>(4);
            bag.AddRange(new[] { "a", "b", "c" });

            bag.Remove("b").ShouldBe(0);
            bag.TotalCount.ShouldBe(2);
            bag.ToList().ShouldBe(new[] { "a", "c" });
        }

        [Fact]
        public void Remove_AbsentElement_ReturnsZeroAndChangesNothing()
        {
            var bag = new BoundedBag<string>(4);
            bag.Add("a");

            bag.Remove("z").ShouldBe(0);
            bag.TotalCount.ShouldBe(1);
        }

        [Fact]
        public void Remove_MultipleCopies_RemovesOldestFirst()
        {
            var bag = new BoundedBag<string>(5);
            bag.AddRange(new[] { "a", "b", "a", "a" });

            bag.Remove("a", 2).ShouldBe(1);
            bag.TotalCount.ShouldBe(2);
            bag.ToList().ShouldBe(new[] { "b", "a" });
        }

        [Fact]
        public void Remove_MoreCopiesThanStored_RemovesEverythingOfThatElement()
        {
            var bag = new BoundedBag<string>(5);
            bag.AddRange(new[] { "a", "a", "b" });

            bag.Remove("a", 5).ShouldBe(0);
            bag.TotalCount.ShouldBe(1);
            bag.ToList().ShouldBe(new[] { "b" });
        }

        [Fact]
        public void Remove_NonPositiveTimes_ThrowsArgumentOutOfRangeException()
        {
            var bag = new BoundedBag<string>(3);
            Should.Throw<ArgumentOutOfRangeException>(() => bag.Remove("a", 0));
        }

        [Fact]
        public void Remove_FreesASlot_SoTheNextAddDoesNotEvict()
        {
            var bag = new BoundedBag<string>(2);
            bag.AddRange(new[] { "a", "b" });
            bag.Remove("a");
            bag.Add("c");

            bag.TotalCount.ShouldBe(2);
            bag.ToList().ShouldBe(new[] { "b", "c" });
        }

        [Fact]
        public void RemoveAllCopies_RemovesEveryCopyAndReportsWhetherAnyWasRemoved()
        {
            var bag = new BoundedBag<string>(5);
            bag.AddRange(new[] { "a", "b", "a" });

            bag.RemoveAllCopies("a").ShouldBeTrue();
            bag.Contains("a").ShouldBeFalse();
            bag.ToList().ShouldBe(new[] { "b" });
            bag.RemoveAllCopies("a").ShouldBeFalse();
        }

        [Fact]
        public void Clear_ResetsEverythingAndKeepsCapacity()
        {
            var bag = new BoundedBag<string>(3);
            bag.AddRange(new[] { "a", "b", "c" });

            bag.Clear();

            bag.Capacity.ShouldBe(3);
            bag.TotalCount.ShouldBe(0);
            bag.IsEmpty.ShouldBeTrue();
            bag.Contains("a").ShouldBeFalse();
        }

        [Fact]
        public void Clear_ThenAdd_StartsFromTheFirstSlotAgain()
        {
            var bag = new BoundedBag<string>(3);
            bag.AddRange(new[] { "a", "b", "c", "d" }); // wraps once
            bag.Clear();
            bag.AddRange(new[] { "x", "y", "z" });

            bag.ToList().ShouldBe(new[] { "x", "y", "z" });
        }

        // ------------------------------------------------------------------
        // Projection and copying
        // ------------------------------------------------------------------

        [Fact]
        public void Enumeration_IsCopyExpandedAndOldestFirst()
        {
            var bag = new BoundedBag<string>(5);
            bag.AddRange(new[] { "a", "b", "a" });

            bag.ToList().ShouldBe(new[] { "a", "b", "a" });
            bag.ToArray().ShouldBe(new[] { "a", "b", "a" });
        }

        [Fact]
        public void DistinctItems_AreInFirstAppearanceOrder()
        {
            var bag = new BoundedBag<string>(5);
            bag.AddRange(new[] { "b", "a", "b", "c" });

            bag.DistinctItems().ShouldBe(new[] { "b", "a", "c" });
        }

        [Fact]
        public void EntrySet_GroupsByElementInFirstAppearanceOrder()
        {
            var bag = new BoundedBag<string>(6);
            bag.AddRange(new[] { "b", "a", "b", "c", "b" });

            bag.EntrySet().ShouldBe(new[] { ("b", 3), ("a", 1), ("c", 1) });
            bag.EntrySet().Sum(e => e.Count).ShouldBe(bag.TotalCount);
            bag.EntrySet().Count().ShouldBe(bag.DistinctCount);
        }

        [Fact]
        public void ToDictionary_ReturnsASnapshot()
        {
            var bag = new BoundedBag<string>(4);
            bag.AddRange(new[] { "a", "a", "b" });

            var dictionary = bag.ToDictionary();
            dictionary["a"].ShouldBe(2);
            dictionary["b"].ShouldBe(1);

            bag.Add("a");
            dictionary["a"].ShouldBe(2); // snapshot, unaffected
        }

        [Fact]
        public void Clone_IsIndependent()
        {
            var bag = new BoundedBag<string>(3);
            bag.AddRange(new[] { "a", "b" });
            var clone = bag.Clone();

            clone.Capacity.ShouldBe(bag.Capacity);
            clone.ToList().ShouldBe(bag.ToList());

            clone.Add("c");
            clone.Add("d"); // evicts "a" in the clone only

            clone.ToList().ShouldBe(new[] { "b", "c", "d" });
            bag.ToList().ShouldBe(new[] { "a", "b" });
        }

        [Fact]
        public void ToString_IsCopyExpandedCommaSeparated()
        {
            var bag = new BoundedBag<string>(4);
            bag.AddRange(new[] { "a", "a", "b" });

            bag.ToString().ShouldBe("a,a,b");
        }

        // ------------------------------------------------------------------
        // null handling
        // ------------------------------------------------------------------

        [Fact]
        public void Null_IsAValidElement()
        {
            var bag = new BoundedBag<string>(4);
            bag.Add(null!);
            bag.Add("a");
            bag.Add(null!);

            bag.Contains(null!).ShouldBeTrue();
            bag.CountOf(null!).ShouldBe(2);
            bag.DistinctCount.ShouldBe(2);
            bag.ToList().ShouldBe(new[] { null, "a", null });
            bag.EntrySet().ShouldBe(new[] { (null, 2), ("a", 1) });
        }

        [Fact]
        public void Null_IsEvictedLikeAnyOtherElement()
        {
            var bag = new BoundedBag<string>(2);
            bag.Add(null!);
            bag.Add("a");
            bag.Add("b");

            bag.Contains(null!).ShouldBeFalse();
            bag.ToList().ShouldBe(new[] { "a", "b" });
        }

        [Fact]
        public void ToDictionary_WithNullElement_ThrowsInvalidOperationException()
        {
            var bag = new BoundedBag<string>(3);
            bag.Add(null!);

            Should.Throw<InvalidOperationException>(() => bag.ToDictionary());
        }

        // ------------------------------------------------------------------
        // Comparer
        // ------------------------------------------------------------------

        [Fact]
        public void Comparer_DrivesElementIdentity()
        {
            var bag = new BoundedBag<string>(4, StringComparer.OrdinalIgnoreCase);
            bag.Add("A");
            bag.Add("a");

            bag.CountOf("a").ShouldBe(2);
            bag.Contains("A").ShouldBeTrue();
            bag.DistinctCount.ShouldBe(1);
            bag.Remove("a").ShouldBe(1);
        }

        // ------------------------------------------------------------------
        // Boundary / contract
        // ------------------------------------------------------------------

        [Fact]
        public void Type_DoesNotImplementIMultiSet()
        {
            // Deliberate: IMultiSet<T> documents that AddRange order does not affect the result,
            // which is false for a bounded bag (earlier elements are evicted first). This guard
            // keeps that decision from being silently reversed.
            typeof(IMultiSet<string>).IsAssignableFrom(typeof(BoundedBag<string>)).ShouldBeFalse();
        }

        [Fact]
        public void IsReadOnly_IsFalse()
        {
            new BoundedBag<string>(3).IsReadOnly.ShouldBeFalse();
        }
    }
}
