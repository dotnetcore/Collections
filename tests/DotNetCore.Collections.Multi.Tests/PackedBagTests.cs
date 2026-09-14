using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// F6-06: <see cref="PackedBag{T}"/> - the packed value-type counting histogram. Covers the
    /// MultiList-mirrored bag semantics (counting, copy-expanded enumeration, removal returns
    /// remaining copies), the packed entry array (no zero-count holes, first-encounter order),
    /// the struct constraint boundary and the packing behavior on removal.
    /// </summary>
    public class PackedBagTests
    {
        // ------------------------------------------------------------------
        // Construction and counts
        // ------------------------------------------------------------------

        [Fact]
        public void Ctor_Empty_CountsAreZero()
        {
            var bag = new PackedBag<int>();

            bag.Count.ShouldBe(0);
            bag.TotalCount.ShouldBe(0);
            bag.DistinctCount.ShouldBe(0);
            bag.IsEmpty.ShouldBeTrue();
            bag.ShouldBeEmpty();
        }

        [Fact]
        public void Ctor_NegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            Should.Throw<ArgumentOutOfRangeException>(() => new PackedBag<int>(-1));
        }

        [Fact]
        public void Ctor_Capacity_IsPreallocated()
        {
            new PackedBag<int>(64).Capacity.ShouldBe(64);
        }

        [Fact]
        public void Ctor_NullCollection_ThrowsArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() => new PackedBag<int>(null!));
        }

        [Fact]
        public void Ctor_Collection_CountsOneCopyPerElement()
        {
            var bag = new PackedBag<int>(new[] { 1, 2, 2, 3 });

            bag.Count.ShouldBe(4);
            bag.DistinctCount.ShouldBe(3);
            bag.CountOf(2).ShouldBe(2);
        }

        // ------------------------------------------------------------------
        // Add and lookup
        // ------------------------------------------------------------------

        [Fact]
        public void Add_NewAndExisting_CountsAccumulate()
        {
            var bag = new PackedBag<int>();
            bag.Add(1);
            bag.Add(1);
            bag.Add(2);

            bag.CountOf(1).ShouldBe(2);
            bag.CountOf(2).ShouldBe(1);
            bag.TotalCount.ShouldBe(3);
            bag.DistinctCount.ShouldBe(2);
        }

        [Fact]
        public void Add_WithTimes_AccumulatesMultiplicities()
        {
            var bag = new PackedBag<DayOfWeek>();
            bag.Add(DayOfWeek.Monday, 3);

            bag.CountOf(DayOfWeek.Monday).ShouldBe(3);
            bag.TotalCount.ShouldBe(3);
            bag.DistinctCount.ShouldBe(1);
        }

        [Fact]
        public void Add_NonPositiveTimes_ThrowsArgumentOutOfRangeException()
        {
            var bag = new PackedBag<int>();
            Should.Throw<ArgumentOutOfRangeException>(() => bag.Add(1, 0));
            Should.Throw<ArgumentOutOfRangeException>(() => bag.Add(1, -2));
        }

        [Fact]
        public void AddRange_AddsOneCopyPerElement()
        {
            var bag = new PackedBag<int>();
            bag.AddRange(new[] { 1, 1, 2 });

            bag.CountOf(1).ShouldBe(2);
            bag.CountOf(2).ShouldBe(1);
        }

        [Fact]
        public void AddRange_NullArgument_ThrowsArgumentNullException()
        {
            var bag = new PackedBag<int>();
            Should.Throw<ArgumentNullException>(() => bag.AddRange(null!));
        }

        [Fact]
        public void Add_GrowsCapacity_PreservesEntries()
        {
            var bag = new PackedBag<int>(2);
            for (var i = 0; i < 32; i++)
            {
                bag.Add(i);
            }

            bag.DistinctCount.ShouldBe(32);
            bag.Capacity.ShouldBeGreaterThanOrEqualTo(32);
            bag.Count.ShouldBe(32);
            for (var i = 0; i < 32; i++)
            {
                bag.CountOf(i).ShouldBe(1);
            }
        }

        [Fact]
        public void Contains_PresentAndAbsent()
        {
            var bag = new PackedBag<int>(new[] { 7 });

            bag.Contains(7).ShouldBeTrue();
            bag.Contains(8).ShouldBeFalse();
        }

        [Fact]
        public void CountOf_Absent_IsZero()
        {
            new PackedBag<int>().CountOf(42).ShouldBe(0);
        }

        [Fact]
        public void Add_CustomStructWithoutBoxing_StoresByValue()
        {
            var bag = new PackedBag<Point>();
            bag.Add(new Point(1, 2));
            bag.Add(new Point(1, 2));
            bag.Add(new Point(3, 4));

            bag.CountOf(new Point(1, 2)).ShouldBe(2);
            bag.DistinctCount.ShouldBe(2);
        }

        private readonly struct Point : IEquatable<Point>
        {
            public Point(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X { get; }
            public int Y { get; }

            public bool Equals(Point other) => X == other.X && Y == other.Y;
        }

        // ------------------------------------------------------------------
        // Remove and packing
        // ------------------------------------------------------------------

        [Fact]
        public void Remove_ReturnsRemainingCopies()
        {
            var bag = new PackedBag<int>(new[] { 1, 1, 1 });

            bag.Remove(1).ShouldBe(2);
            bag.CountOf(1).ShouldBe(2);

            bag.Remove(1, 5).ShouldBe(0); // clamps to everything stored
            bag.CountOf(1).ShouldBe(0);
        }

        [Fact]
        public void Remove_NonPositiveTimes_ThrowsArgumentOutOfRangeException()
        {
            var bag = new PackedBag<int>(new[] { 1 });
            Should.Throw<ArgumentOutOfRangeException>(() => bag.Remove(1, 0));
            Should.Throw<ArgumentOutOfRangeException>(() => bag.Remove(1, -1));
        }

        [Fact]
        public void Remove_AbsentElement_ReturnsZero()
        {
            var bag = new PackedBag<int>(new[] { 1 });
            bag.Remove(99).ShouldBe(0);
            bag.TotalCount.ShouldBe(1);
        }

        [Fact]
        public void Remove_LastCopy_DropsEntryAndKeepsArrayPacked()
        {
            var bag = new PackedBag<int>();
            bag.Add(1);
            bag.Add(2);
            bag.Add(3);

            bag.Remove(2).ShouldBe(0);

            // The entry for 2 is gone and the tail shifted over the hole: first-encounter order
            // of the survivors is preserved and no zero-count entry lingers.
            bag.DistinctCount.ShouldBe(2);
            bag.EntrySet().ShouldBe(new[] { (1, 1), (3, 1) });
            bag.Contains(2).ShouldBeFalse();
            bag.Contains(3).ShouldBeTrue();
            bag.TotalCount.ShouldBe(2);
        }

        [Fact]
        public void RemoveAllCopies_DropsTheWholeEntry()
        {
            var bag = new PackedBag<int>(new[] { 1, 1, 2 });

            bag.RemoveAllCopies(1).ShouldBeTrue();
            bag.Contains(1).ShouldBeFalse();
            bag.RemoveAllCopies(1).ShouldBeFalse();
            bag.TotalCount.ShouldBe(1);
        }

        [Fact]
        public void Clear_ResetsEverything()
        {
            var bag = new PackedBag<int>(new[] { 1, 1, 2 });
            bag.Clear();

            bag.IsEmpty.ShouldBeTrue();
            bag.DistinctCount.ShouldBe(0);
            bag.TotalCount.ShouldBe(0);
            bag.Count.ShouldBe(0);
        }

        // ------------------------------------------------------------------
        // Projection, enumeration and copying
        // ------------------------------------------------------------------

        [Fact]
        public void EntrySet_EnumeratesInFirstEncounterOrder()
        {
            var bag = new PackedBag<char>();
            bag.AddRange(new[] { 'b', 'a', 'b', 'c', 'a', 'b' });

            bag.EntrySet().ShouldBe(new[] { ('b', 3), ('a', 2), ('c', 1) });
        }

        [Fact]
        public void DistinctItems_EnumeratesEachElementOnce()
        {
            var bag = new PackedBag<char>();
            bag.AddRange(new[] { 'b', 'a', 'b' });

            bag.DistinctItems().ShouldBe(new[] { 'b', 'a' });
        }

        [Fact]
        public void Enumeration_IsCopyExpanded()
        {
            // Insertion order b(3), a(1): copies of one element stay consecutive, exactly like
            // MultiList's copy-expanded enumeration.
            var bag = new PackedBag<char>();
            bag.AddRange(new[] { 'b', 'a', 'b', 'b' });

            bag.ToList().ShouldBe(new[] { 'b', 'b', 'b', 'a' });
            bag.ToArray().ShouldBe(new[] { 'b', 'b', 'b', 'a' });
            bag.ToString().ShouldBe("b,b,b,a");
        }

        [Fact]
        public void ToDictionary_ExportsIndependentSnapshot()
        {
            var bag = new PackedBag<int>(new[] { 1, 1, 2 });
            var histogram = bag.ToDictionary();

            histogram.Count.ShouldBe(2);
            histogram[1].ShouldBe(2);
            histogram[2].ShouldBe(1);

            bag.Add(1);
            histogram[1].ShouldBe(2); // the snapshot does not follow the bag
        }

        [Fact]
        public void Clone_IsIndependentOfTheOriginal()
        {
            var bag = new PackedBag<int>(new[] { 1, 1, 2 });
            var clone = bag.Clone();

            clone.Add(3);
            bag.Remove(1, 2);

            bag.CountOf(1).ShouldBe(0);
            bag.DistinctCount.ShouldBe(1);
            clone.Count.ShouldBe(4);
            clone.CountOf(1).ShouldBe(2);
            clone.CountOf(3).ShouldBe(1);
        }

        [Fact]
        public void TrimExcess_ReleasesSpareCapacity()
        {
            var bag = new PackedBag<int>(64);
            bag.Add(1);
            bag.TrimExcess();

            bag.Capacity.ShouldBe(1);
            bag.CountOf(1).ShouldBe(1);
        }

        [Fact]
        public void RandomizedOperations_MatchDictionaryModel()
        {
            const int steps = 1500;
            var random = new Random(20260915);
            var bag = new PackedBag<int>();
            var model = new Dictionary<int, int>();

            for (var step = 0; step < steps; step++)
            {
                var value = random.Next(16);
                switch (random.Next(6))
                {
                    case 0:
                    case 1:
                    case 2:
                        bag.Add(value);
                        model[value] = model.TryGetValue(value, out var c1) ? c1 + 1 : 1;
                        break;

                    case 3:
                        var had = model.TryGetValue(value, out var c2) ? c2 : 0;
                        // Remove takes one copy away and returns the number remaining.
                        bag.Remove(value).ShouldBe(had > 0 ? had - 1 : 0);
                        if (had > 0)
                        {
                            if (had == 1)
                            {
                                model.Remove(value);
                            }
                            else
                            {
                                model[value] = had - 1;
                            }
                        }

                        break;

                    case 4:
                        var times = random.Next(1, 4);
                        bag.Add(value, times);
                        model[value] = model.TryGetValue(value, out var c4) ? c4 + times : times;
                        break;

                    case 5:
                        var expectedAll = model.TryGetValue(value, out var c5) ? c5 : 0;
                        bag.RemoveAllCopies(value).ShouldBe(expectedAll > 0);
                        model.Remove(value);
                        break;
                }

                bag.TotalCount.ShouldBe(model.Values.Sum());
                bag.DistinctCount.ShouldBe(model.Count);
                bag.IsEmpty.ShouldBe(model.Count == 0);
            }
        }
    }
}
