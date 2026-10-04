using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// F6-13: <c>Snapshot()</c> on the two core types. Pins that it is an independent copy (changes
    /// on either side are invisible to the other), that it carries the configuration over, and - the
    /// point of the member - that a snapshot can be enumerated while the original is still being
    /// mutated.
    /// </summary>
    public class SnapshotTests
    {
        // ------------------------------------------------------------------
        // MultiList<T>
        // ------------------------------------------------------------------

        [Fact]
        public void Bag_Snapshot_IsADifferentInstanceWithTheSameContents()
        {
            var bag = new MultiList<string>();
            bag.Add("a", 2);
            bag.Add("b");

            var snapshot = bag.Snapshot();

            snapshot.ShouldNotBeSameAs(bag);
            snapshot.TotalCount.ShouldBe(3);
            snapshot.DistinctCount.ShouldBe(2);
            snapshot.CountOf("a").ShouldBe(2);
            snapshot.CountOf("b").ShouldBe(1);
        }

        [Fact]
        public void Bag_Snapshot_IsUnaffectedByLaterChangesToTheOriginal()
        {
            var bag = new MultiList<string> { "a" };
            var snapshot = bag.Snapshot();

            bag.Add("b");
            bag.Remove("a");
            bag.Clear();

            snapshot.TotalCount.ShouldBe(1);
            snapshot.CountOf("a").ShouldBe(1);
            snapshot.CountOf("b").ShouldBe(0);
        }

        [Fact]
        public void Bag_Snapshot_ChangesDoNotReachBackToTheOriginal()
        {
            var bag = new MultiList<string> { "a" };
            var snapshot = bag.Snapshot();

            snapshot.Add("b");
            snapshot.Remove("a");

            bag.TotalCount.ShouldBe(1);
            bag.CountOf("a").ShouldBe(1);
            bag.CountOf("b").ShouldBe(0);
        }

        [Fact]
        public void Bag_EnumeratingASnapshot_IsSafeWhileTheOriginalIsMutated()
        {
            var bag = new MultiList<string> { "a", "b" };
            var snapshot = bag.Snapshot();
            var seen = new List<string>();

            foreach (var item in snapshot)
            {
                seen.Add(item);

                // This would invalidate a live enumeration of bag; the snapshot is detached, so it
                // is unaffected and keeps yielding the two original elements.
                bag.Add("c");
            }

            seen.Count.ShouldBe(2);
            seen.ShouldBe(new[] { "a", "b" }, ignoreOrder: true);
        }

        [Fact]
        public void Bag_Snapshot_PreservesTheComparer()
        {
            var bag = new MultiList<string>(StringComparer.OrdinalIgnoreCase) { "a" };
            var snapshot = bag.Snapshot();

            snapshot.CountOf("A").ShouldBe(1);
            snapshot.Comparer.ShouldBeSameAs(bag.Comparer);
        }

        [Fact]
        public void Bag_Snapshot_CarriesANullElement()
        {
            var bag = new MultiList<string> { "a" };
            bag.Add(null!);

            var snapshot = bag.Snapshot();

            snapshot.CountOf(null!).ShouldBe(1);
            snapshot.TotalCount.ShouldBe(2);
        }

        [Fact]
        public void Bag_SnapshotOfAnEmptyBag_IsEmpty()
        {
            var bag = new MultiList<string>();

            var snapshot = bag.Snapshot();

            snapshot.ShouldNotBeSameAs(bag);
            snapshot.TotalCount.ShouldBe(0);
            snapshot.DistinctCount.ShouldBe(0);
        }

        // ------------------------------------------------------------------
        // MultiDictionary<TKey, TValue>
        // ------------------------------------------------------------------

        [Fact]
        public void Map_Snapshot_IsADifferentInstanceWithTheSameContents()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("k", 1);
            map.Add("k", 2);
            map.Add("other", 3);

            var snapshot = map.Snapshot();

            snapshot.ShouldNotBeSameAs(map);
            snapshot.Count.ShouldBe(2);
            snapshot.TotalValueCount.ShouldBe(3);
            snapshot.ValueCount("k").ShouldBe(2);
        }

        [Fact]
        public void Map_Snapshot_IsUnaffectedByLaterChangesToTheOriginal()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("k", 1);

            var snapshot = map.Snapshot();

            map.Add("k", 2);
            map.Add("other", 3);
            map.Remove("k");

            snapshot.Count.ShouldBe(1);
            snapshot.ValueCount("k").ShouldBe(1);
            snapshot.ContainsKey("other").ShouldBeFalse();
        }

        [Fact]
        public void Map_Snapshot_ChangesDoNotReachBackToTheOriginal()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("k", 1);

            var snapshot = map.Snapshot();

            snapshot.Add("k", 9);
            snapshot.Add("other", 3);

            map.ValueCount("k").ShouldBe(1);
            map.ContainsKey("other").ShouldBeFalse();
        }

        [Fact]
        public void Map_Snapshot_InnerCollectionsAreIndependent()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("k", 1);

            var snapshot = map.Snapshot();

            // The inner value collections are copies too, not shared views.
            snapshot["k"].Count.ShouldBe(1);
            map.Add("k", 2);
            snapshot["k"].Count.ShouldBe(1);
        }

        [Fact]
        public void Map_EnumeratingASnapshot_IsSafeWhileTheOriginalIsMutated()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("a", 1);
            map.Add("b", 2);

            var snapshot = map.Snapshot();
            var seen = new List<string>();

            foreach (var pair in snapshot)
            {
                seen.Add(pair.Key);

                // Would invalidate a live enumeration of map; the snapshot is detached.
                map.Add("c", 3);
            }

            seen.Count.ShouldBe(2);
            seen.ShouldBe(new[] { "a", "b" }, ignoreOrder: true);
        }

        [Fact]
        public void Map_Snapshot_PreservesTheComparer()
        {
            var map = new MultiDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            map.Add("k", 1);

            var snapshot = map.Snapshot();

            snapshot.ContainsKey("K").ShouldBeTrue();
            snapshot.Comparer.ShouldBeSameAs(map.Comparer);
        }

        [Fact]
        public void Map_Snapshot_PreservesTheDuplicateValuesPolicy()
        {
            var map = new MultiDictionary<string, int>(allowDuplicateValues: false);
            map.Add("k", 1);

            var snapshot = map.Snapshot();
            snapshot.Add("k", 1);

            snapshot.ValueCount("k").ShouldBe(1);
        }

        [Fact]
        public void Map_SnapshotOfAnEmptyMap_IsEmpty()
        {
            var map = new MultiDictionary<string, int>();

            var snapshot = map.Snapshot();

            snapshot.ShouldNotBeSameAs(map);
            snapshot.Count.ShouldBe(0);
            snapshot.TotalValueCount.ShouldBe(0);
        }
    }
}
