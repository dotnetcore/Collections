using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// F6-07: <see cref="SpanBag{T}"/> - the stack-only temporary bag. Covers the borrowed-span
    /// construction, the capacity/bool-Add contract, the MultiList-mirrored bag semantics on
    /// stack storage, the reuse-after-Clear semantics and the compiler-enforced ref-struct
    /// lifetime contract (pinned via <see cref="Type.IsByRefLike"/>).
    /// </summary>
    public class SpanBagTests
    {
        // ------------------------------------------------------------------
        // Construction and the borrowed-span contract
        // ------------------------------------------------------------------

        [Fact]
        public void Ctor_MismatchedSpanLengths_ThrowsArgumentException()
        {
            Span<int> values = stackalloc int[8];
            Span<int> counts = stackalloc int[4];

            // A span can not be captured by the Should.Throw lambda (ref struct), so the
            // construction is guarded inline.
            ArgumentException? caught = null;
            try
            {
                var ignored = new SpanBag<int>(values, counts);
            }
            catch (ArgumentException ex)
            {
                caught = ex;
            }

            caught.ShouldNotBeNull();
        }

        [Fact]
        public void Ctor_EmptySpans_EmptyBagWithZeroCapacity()
        {
            Span<int> values = stackalloc int[0];
            Span<int> counts = stackalloc int[0];
            var bag = new SpanBag<int>(values, counts);

            bag.Capacity.ShouldBe(0);
            bag.Remaining.ShouldBe(0);
            bag.IsEmpty.ShouldBeTrue();
            bag.Add(1).ShouldBeFalse();
        }

        [Fact]
        public void IsByRefLike_CompilerEnforcedStackOnlyContract()
        {
            // The lifetime constraints (no fields, no boxing, no capture, no await/yield escape)
            // are enforced by the compiler through the ref-struct kind; pin it so a future edit
            // can not silently drop the guarantee.
            typeof(SpanBag<int>).IsByRefLike.ShouldBeTrue();
        }

        // ------------------------------------------------------------------
        // Add and lookup
        // ------------------------------------------------------------------

        [Fact]
        public void Add_CountsAccumulateWithinCapacity()
        {
            Span<int> values = stackalloc int[8];
            Span<int> counts = stackalloc int[8];
            var bag = new SpanBag<int>(values, counts);

            bag.Add(3).ShouldBeTrue();
            bag.Add(3).ShouldBeTrue();
            bag.Add(5).ShouldBeTrue();

            bag.CountOf(3).ShouldBe(2);
            bag.CountOf(5).ShouldBe(1);
            bag.TotalCount.ShouldBe(3);
            bag.DistinctCount.ShouldBe(2);
            bag.Remaining.ShouldBe(6);
            bag.IsEmpty.ShouldBeFalse();
        }

        [Fact]
        public void Add_ExistingElement_AlwaysSucceedsEvenWhenFull()
        {
            Span<int> values = stackalloc int[2];
            Span<int> counts = stackalloc int[2];
            var bag = new SpanBag<int>(values, counts);

            bag.Add(1).ShouldBeTrue();
            bag.Add(2).ShouldBeTrue();
            bag.Remaining.ShouldBe(0);

            bag.Add(3).ShouldBeFalse(); // a NEW element does not fit
            bag.Add(1).ShouldBeTrue();  // an existing one always does
            bag.CountOf(1).ShouldBe(2);
            bag.DistinctCount.ShouldBe(2);
        }

        [Fact]
        public void Add_BeyondCapacity_ReturnsFalseWithoutChangingState()
        {
            Span<char> values = stackalloc char[2];
            Span<int> counts = stackalloc int[2];
            var bag = new SpanBag<char>(values, counts);

            bag.Add('a').ShouldBeTrue();
            bag.Add('b').ShouldBeTrue();
            bag.Add('c').ShouldBeFalse();

            bag.DistinctCount.ShouldBe(2);
            bag.TotalCount.ShouldBe(2);
            bag.Contains('c').ShouldBeFalse();
        }

        [Fact]
        public void Add_EnumElements_WorkWithoutBoxingStorage()
        {
            Span<DayOfWeek> values = stackalloc DayOfWeek[4];
            Span<int> counts = stackalloc int[4];
            var bag = new SpanBag<DayOfWeek>(values, counts);

            bag.Add(DayOfWeek.Monday).ShouldBeTrue();
            bag.Add(DayOfWeek.Monday).ShouldBeTrue();
            bag.Add(DayOfWeek.Friday).ShouldBeTrue();

            bag.CountOf(DayOfWeek.Monday).ShouldBe(2);
            bag.TotalCount.ShouldBe(3);
        }

        [Fact]
        public void Contains_PresentAndAbsent()
        {
            Span<int> values = stackalloc int[4];
            Span<int> counts = stackalloc int[4];
            var bag = new SpanBag<int>(values, counts);
            bag.Add(7);

            bag.Contains(7).ShouldBeTrue();
            bag.Contains(8).ShouldBeFalse();
            bag.CountOf(8).ShouldBe(0);
        }

        // ------------------------------------------------------------------
        // Remove and packing
        // ------------------------------------------------------------------

        [Fact]
        public void Remove_ReturnsRemainingCopies()
        {
            Span<int> values = stackalloc int[4];
            Span<int> counts = stackalloc int[4];
            var bag = new SpanBag<int>(values, counts);
            bag.Add(1);
            bag.Add(1);
            bag.Add(1);

            bag.Remove(1).ShouldBe(2);
            bag.Remove(1).ShouldBe(1);
            bag.Remove(1).ShouldBe(0); // last copy drops the entry
            bag.Remove(1).ShouldBe(0); // absent: nothing removed
            bag.IsEmpty.ShouldBeTrue();
            bag.DistinctCount.ShouldBe(0);
        }

        [Fact]
        public void Remove_LastCopy_KeepsStoragePacked()
        {
            Span<char> values = stackalloc char[8];
            Span<int> counts = stackalloc int[8];
            var bag = new SpanBag<char>(values, counts);
            bag.Add('a');
            bag.Add('b');
            bag.Add('c');

            bag.RemoveAllCopies('b').ShouldBeTrue();

            // First-encounter order of the survivors is preserved over the hole.
            CollectEntries(bag).ShouldBe(new[] { ('a', 1), ('c', 1) });
            bag.DistinctCount.ShouldBe(2);
            bag.Contains('b').ShouldBeFalse();
        }

        private static List<(char Value, int Count)> CollectEntries(SpanBag<char> bag)
        {
            var entries = new List<(char Value, int Count)>();
            foreach (var entry in bag)
            {
                entries.Add((entry.Value, entry.Count));
            }

            return entries;
        }

        [Fact]
        public void RemoveAllCopies_Absent_ReturnsFalse()
        {
            Span<int> values = stackalloc int[4];
            Span<int> counts = stackalloc int[4];
            var bag = new SpanBag<int>(values, counts);

            bag.RemoveAllCopies(9).ShouldBeFalse();
        }

        // ------------------------------------------------------------------
        // Reuse semantics
        // ------------------------------------------------------------------

        [Fact]
        public void Clear_AllowsReusingTheSameStackStorage()
        {
            Span<int> values = stackalloc int[4];
            Span<int> counts = stackalloc int[4];
            var bag = new SpanBag<int>(values, counts);

            bag.Add(1);
            bag.Add(2);
            bag.Clear();

            bag.IsEmpty.ShouldBeTrue();
            bag.DistinctCount.ShouldBe(0);
            bag.TotalCount.ShouldBe(0);
            bag.Remaining.ShouldBe(4);

            // A second counting round on the same borrowed storage, no allocator involved.
            bag.Add(9).ShouldBeTrue();
            bag.CountOf(9).ShouldBe(1);
        }

        // ------------------------------------------------------------------
        // Enumeration
        // ------------------------------------------------------------------

        [Fact]
        public void Enumeration_YieldsPackedEntriesInFirstEncounterOrder()
        {
            Span<char> values = stackalloc char[8];
            Span<int> counts = stackalloc int[8];
            var bag = new SpanBag<char>(values, counts);
            bag.Add('b');
            bag.Add('a');
            bag.Add('b');

            var entries = new List<(char Value, int Count)>();
            foreach (var entry in bag)
            {
                entries.Add((entry.Value, entry.Count));
            }

            entries.ShouldBe(new[] { ('b', 2), ('a', 1) });
        }

        [Fact]
        public void Enumeration_OfEmptyBag_YieldsNothing()
        {
            Span<int> values = stackalloc int[4];
            Span<int> counts = stackalloc int[4];
            var bag = new SpanBag<int>(values, counts);

            var entries = new List<(int Value, int Count)>();
            foreach (var entry in bag)
            {
                entries.Add((entry.Value, entry.Count));
            }

            entries.ShouldBeEmpty();
        }

        // ------------------------------------------------------------------
        // Randomized cross-check against a Dictionary model
        // ------------------------------------------------------------------

        [Fact]
        public void RandomizedOperations_MatchDictionaryModel()
        {
            const int steps = 800;
            var random = new Random(20260915);
            Span<int> values = stackalloc int[32];
            Span<int> counts = stackalloc int[32];
            var bag = new SpanBag<int>(values, counts);
            var model = new Dictionary<int, int>();

            for (var step = 0; step < steps; step++)
            {
                var value = random.Next(16);
                switch (random.Next(4))
                {
                    case 0:
                    case 1:
                        bag.Add(value).ShouldBeTrue(); // 16 distinct, capacity 32: always fits
                        model[value] = model.TryGetValue(value, out var c1) ? c1 + 1 : 1;
                        break;

                    case 2:
                        var had = model.TryGetValue(value, out var c2) ? c2 : 0;
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

                    case 3:
                        var expectedAll = model.TryGetValue(value, out var c3) ? c3 : 0;
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
