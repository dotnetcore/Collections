using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// F6-43: the read-only projections of <see cref="ConcurrentMultiList{T}"/>. Every member
    /// delegates to <see cref="ConcurrentMultiList{T}.Snapshot"/>, so these tests pin two things:
    /// the projection agrees with the equivalent <see cref="MultiList{T}"/> member, and the result
    /// is detached from the bag rather than a live window onto it.
    /// </summary>
    public class ConcurrentMultiListTests
    {
        private static ConcurrentMultiList<string> Bag()
        {
            var bag = new ConcurrentMultiList<string>();
            bag.Add("apple", 2);
            bag.Add("banana");
            return bag;
        }

        // ------------------------------------------------------------------
        // Agreement with the serial sibling
        // ------------------------------------------------------------------

        [Fact]
        public void DistinctItems_MatchesMultiList()
        {
            Bag().DistinctItems().ShouldBe(Bag().Snapshot().DistinctItems(), ignoreOrder: true);
        }

        [Fact]
        public void EntrySet_MatchesMultiList()
        {
            var actual = Bag().EntrySet().OrderBy(e => e.Item).ToList();
            var expected = Bag().Snapshot().EntrySet().OrderBy(e => e.Item).ToList();

            actual.ShouldBe(expected);
        }

        [Fact]
        public void EntrySet_PairsEachItemWithItsCopyCount()
        {
            var bag = Bag();

            var counts = bag.EntrySet().ToDictionary(e => e.Item, e => e.Count);

            counts["apple"].ShouldBe(2);
            counts["banana"].ShouldBe(1);
        }

        [Fact]
        public void ToList_KeepsEveryCopy()
        {
            Bag().ToList().ShouldBe(new[] { "apple", "apple", "banana" }, ignoreOrder: true);
        }

        [Fact]
        public void ToArray_KeepsEveryCopy()
        {
            Bag().ToArray().ShouldBe(new[] { "apple", "apple", "banana" }, ignoreOrder: true);
        }

        [Fact]
        public void ToDictionary_MapsDistinctItemToCopyCount()
        {
            var counts = Bag().ToDictionary();

            counts.Count.ShouldBe(2);
            counts["apple"].ShouldBe(2);
            counts["banana"].ShouldBe(1);
        }

        // ------------------------------------------------------------------
        // Snapshot semantics
        // ------------------------------------------------------------------

        [Fact]
        public void ToList_IsDetachedFromTheBag()
        {
            var bag = Bag();

            var list = bag.ToList();
            bag.Add("cherry");

            list.ShouldBe(new[] { "apple", "apple", "banana" }, ignoreOrder: true);
        }

        [Fact]
        public void ToArray_IsDetachedFromTheBag()
        {
            var bag = Bag();

            var array = bag.ToArray();
            bag.Clear();

            array.Length.ShouldBe(3);
        }

        [Fact]
        public void ToDictionary_IsDetachedFromTheBag()
        {
            var bag = Bag();

            var counts = bag.ToDictionary();
            bag.RemoveAllCopies("apple");

            counts["apple"].ShouldBe(2);
        }

        [Fact]
        public void DistinctItems_ReflectsTheBagAtCallTime()
        {
            var bag = Bag();

            bag.Add("cherry");

            bag.DistinctItems().ShouldBe(new[] { "apple", "banana", "cherry" }, ignoreOrder: true);
        }

        // ------------------------------------------------------------------
        // Edge cases
        // ------------------------------------------------------------------

        [Fact]
        public void Projections_OnEmptyBag_AreEmpty()
        {
            var bag = new ConcurrentMultiList<string>();

            bag.DistinctItems().ShouldBeEmpty();
            bag.EntrySet().ShouldBeEmpty();
            bag.ToList().ShouldBeEmpty();
            bag.ToArray().ShouldBeEmpty();
            bag.ToDictionary().ShouldBeEmpty();
        }

        [Fact]
        public void ToDictionary_WithNullElement_Throws()
        {
            // A null can not be a dictionary key; MultiList.ToDictionary throws for the same
            // reason, and the concurrent variant must not quietly diverge.
            var bag = new ConcurrentMultiList<string>();
            bag.Add("a");
            bag.Add(null);

            Should.Throw<InvalidOperationException>(() => bag.ToDictionary());
        }

        [Fact]
        public void EntrySet_WithNullElement_IsNullSafe()
        {
            // The null-safe export path: EntrySet carries the null like any other element.
            var bag = new ConcurrentMultiList<string>();
            bag.Add("a");
            bag.Add(null);

            bag.EntrySet().Count().ShouldBe(2);
        }

        [Fact]
        public void Projections_HonourTheComparer()
        {
            var bag = new ConcurrentMultiList<string>(StringComparer.OrdinalIgnoreCase);
            bag.Add("Apple", 2);

            bag.DistinctItems().ShouldBe(new[] { "Apple" });
            bag.ToDictionary().Count.ShouldBe(1);
            bag.EntrySet().Single().Count.ShouldBe(2);
        }
    }
}
