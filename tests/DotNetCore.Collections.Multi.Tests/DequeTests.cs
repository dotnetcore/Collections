using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// F6-31: <see cref="Deque{T}"/> - the ring-buffer double-ended queue. The suite is built
    /// around the two things a ring buffer can get wrong and a linear structure can not: the wrap
    /// (the head and the tail cross the end of the array and come back, so every read and every
    /// copy has two shapes), and the growth (the ring is re-laid-out into a fresh array). Both are
    /// checked against a <see cref="List{T}"/> model rather than against hand-written expected
    /// sequences, including under randomized interleavings of the four end operations.
    /// </summary>
    public class DequeTests
    {
        // ------------------------------------------------------------------
        // Construction
        // ------------------------------------------------------------------

        [Fact]
        public void Ctor_Default_IsEmptyWithZeroCapacity()
        {
            var deque = new Deque<int>();

            deque.Count.ShouldBe(0);
            deque.IsEmpty.ShouldBeTrue();
            deque.Capacity.ShouldBe(0);
        }

        [Fact]
        public void Ctor_Capacity_SizesTheBufferWithoutHoldingElements()
        {
            var deque = new Deque<int>(8);

            deque.Capacity.ShouldBe(8);
            deque.Count.ShouldBe(0);
            deque.IsEmpty.ShouldBeTrue();
        }

        [Fact]
        public void Ctor_NegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            Should.Throw<ArgumentOutOfRangeException>(() => new Deque<int>(-1));
        }

        [Fact]
        public void Ctor_Collection_CopiesInEnumerationOrderWithTheFirstAtTheHead()
        {
            var deque = new Deque<string>(new[] { "a", "b", "c" });

            deque.Count.ShouldBe(3);
            deque.GetFirst().ShouldBe("a");
            deque.GetLast().ShouldBe("c");
            deque.ToArray().ShouldBe(new[] { "a", "b", "c" });
        }

        [Fact]
        public void Ctor_Collection_SizesTheBufferFromAKnownLengthSource()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3, 4, 5 });

            // One allocation, not the doubling path: the source told the constructor its length.
            deque.Capacity.ShouldBe(5);
        }

        [Fact]
        public void Ctor_NullCollection_ThrowsArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() => new Deque<int>(null));
        }

        [Fact]
        public void Ctor_CollectionInitializer_AppendsAtTheBack()
        {
            var deque = new Deque<string> { "a", "b" };

            // The collection initializer calls Add, which is AddLast: same direction as List<T>.
            deque.ToArray().ShouldBe(new[] { "a", "b" });
        }

        // ------------------------------------------------------------------
        // The two ends
        // ------------------------------------------------------------------

        [Fact]
        public void AddFirst_PutsTheElementAtTheFront()
        {
            var deque = new Deque<string>(new[] { "b", "c" });
            deque.AddFirst("a");

            deque.GetFirst().ShouldBe("a");
            deque.ToArray().ShouldBe(new[] { "a", "b", "c" });
        }

        [Fact]
        public void AddLast_PutsTheElementAtTheBack()
        {
            var deque = new Deque<string>(new[] { "a", "b" });
            deque.AddLast("c");

            deque.GetLast().ShouldBe("c");
            deque.ToArray().ShouldBe(new[] { "a", "b", "c" });
        }

        [Fact]
        public void Add_IsAddLast()
        {
            var deque = new Deque<string>(new[] { "a" });
            deque.Add("b");

            deque.ToArray().ShouldBe(new[] { "a", "b" });
        }

        [Fact]
        public void RemoveFirst_ReturnsAndRemovesTheFront()
        {
            var deque = new Deque<string>(new[] { "a", "b", "c" });

            deque.RemoveFirst().ShouldBe("a");
            deque.ToArray().ShouldBe(new[] { "b", "c" });
            deque.Count.ShouldBe(2);
        }

        [Fact]
        public void RemoveLast_ReturnsAndRemovesTheBack()
        {
            var deque = new Deque<string>(new[] { "a", "b", "c" });

            deque.RemoveLast().ShouldBe("c");
            deque.ToArray().ShouldBe(new[] { "a", "b" });
            deque.Count.ShouldBe(2);
        }

        [Fact]
        public void RemoveFirst_OnEmpty_ThrowsInvalidOperationException()
        {
            var deque = new Deque<int>();

            Should.Throw<InvalidOperationException>(() => deque.RemoveFirst());
        }

        [Fact]
        public void RemoveLast_OnEmpty_ThrowsInvalidOperationException()
        {
            var deque = new Deque<int>();

            Should.Throw<InvalidOperationException>(() => deque.RemoveLast());
        }

        [Fact]
        public void GetFirst_AndGetLast_OnEmpty_ThrowInvalidOperationException()
        {
            var deque = new Deque<int>();

            Should.Throw<InvalidOperationException>(() => deque.GetFirst());
            Should.Throw<InvalidOperationException>(() => deque.GetLast());
        }

        [Fact]
        public void GetFirst_AndGetLast_DoNotRemove()
        {
            var deque = new Deque<string>(new[] { "a", "b", "c" });

            deque.GetFirst().ShouldBe("a");
            deque.GetLast().ShouldBe("c");
            deque.Count.ShouldBe(3);
        }

        [Fact]
        public void TryRemoveFirst_OnEmpty_ReturnsFalseAndChangesNothing()
        {
            var deque = new Deque<int>();

            deque.TryRemoveFirst(out var item).ShouldBeFalse();
            item.ShouldBe(default(int));
            deque.Count.ShouldBe(0);
        }

        [Fact]
        public void TryRemoveLast_OnEmpty_ReturnsFalseAndChangesNothing()
        {
            var deque = new Deque<int>();

            deque.TryRemoveLast(out var item).ShouldBeFalse();
            item.ShouldBe(default(int));
            deque.Count.ShouldBe(0);
        }

        [Fact]
        public void TryRemoveFirst_AndTryRemoveLast_ReturnTheEndsAndRemoveThem()
        {
            var deque = new Deque<string>(new[] { "a", "b", "c" });

            deque.TryRemoveFirst(out var first).ShouldBeTrue();
            deque.TryRemoveLast(out var last).ShouldBeTrue();
            first.ShouldBe("a");
            last.ShouldBe("c");
            deque.ToArray().ShouldBe(new[] { "b" });
        }

        [Fact]
        public void TryGetFirst_AndTryGetLast_OnEmpty_ReturnFalse()
        {
            var deque = new Deque<int>();

            deque.TryGetFirst(out var first).ShouldBeFalse();
            deque.TryGetLast(out var last).ShouldBeFalse();
            first.ShouldBe(default(int));
            last.ShouldBe(default(int));
        }

        [Fact]
        public void TryGetFirst_AndTryGetLast_DoNotRemove()
        {
            var deque = new Deque<string>(new[] { "a", "b" });

            deque.TryGetFirst(out var first).ShouldBeTrue();
            deque.TryGetLast(out var last).ShouldBeTrue();
            first.ShouldBe("a");
            last.ShouldBe("b");
            deque.Count.ShouldBe(2);
        }

        // ------------------------------------------------------------------
        // The ring: wrap-around and growth
        // ------------------------------------------------------------------

        [Fact]
        public void Ring_WrapsAroundWithoutLosingOrder()
        {
            // A buffer of four, drained from the front twice and refilled twice: the tail now sits
            // at a lower slot than the head, so every read and every copy runs the wrapped shape.
            var deque = new Deque<int>(4);
            deque.AddLast(1);
            deque.AddLast(2);
            deque.AddLast(3);
            deque.AddLast(4);

            deque.RemoveFirst().ShouldBe(1);
            deque.RemoveFirst().ShouldBe(2);

            deque.AddLast(5);
            deque.AddLast(6);

            deque.Capacity.ShouldBe(4);
            deque.ToArray().ShouldBe(new[] { 3, 4, 5, 6 });
            deque.GetFirst().ShouldBe(3);
            deque.GetLast().ShouldBe(6);
            deque[0].ShouldBe(3);
            deque[3].ShouldBe(6);
        }

        [Fact]
        public void Ring_GrowthReLaysTheWrappedSequenceOutFromSlotZero()
        {
            var deque = new Deque<int>(4);
            deque.AddLast(1);
            deque.AddLast(2);
            deque.AddLast(3);
            deque.AddLast(4);
            deque.RemoveFirst();
            deque.RemoveFirst();
            deque.AddLast(5);
            deque.AddLast(6);

            // Full and wrapped: head at slot 2, tail at slot 0. The next add has to grow, and a
            // growth copies the ring in two runs - the wrapped shape is exactly what it re-lays.
            deque.Capacity.ShouldBe(4);
            deque.ToArray().ShouldBe(new[] { 3, 4, 5, 6 });

            deque.AddLast(7);

            deque.Capacity.ShouldBe(8);
            deque.ToArray().ShouldBe(new[] { 3, 4, 5, 6, 7 });
            deque[0].ShouldBe(3);
            deque[4].ShouldBe(7);
        }

        [Fact]
        public void Ring_InterleavedEndOperations_StayInStepWithAListModel()
        {
            var deque = new Deque<int>();
            var model = new List<int>();

            // A fixed interleaving that crosses the wrap point repeatedly in both directions.
            for (var round = 0; round < 40; round++)
            {
                deque.AddLast(round);
                model.Add(round);

                deque.AddFirst(-round);
                model.Insert(0, -round);

                if (round % 3 == 0)
                {
                    deque.RemoveLast().ShouldBe(model[model.Count - 1]);
                    model.RemoveAt(model.Count - 1);
                }

                if (round % 5 == 0)
                {
                    deque.RemoveFirst().ShouldBe(model[0]);
                    model.RemoveAt(0);
                }
            }

            deque.ToArray().ShouldBe(model.ToArray());
            deque.Count.ShouldBe(model.Count);
        }

        [Fact]
        public void Ring_RandomizedEndOperations_MatchAListModelAtEveryStep()
        {
            var random = new Random(20310923);
            var deque = new Deque<int>();
            var model = new List<int>();

            for (var step = 0; step < 4000; step++)
            {
                switch (random.Next(0, 6))
                {
                    case 0:
                        deque.AddFirst(step);
                        model.Insert(0, step);
                        break;

                    case 1:
                        deque.AddLast(step);
                        model.Add(step);
                        break;

                    case 2:
                        if (model.Count > 0)
                        {
                            deque.RemoveFirst().ShouldBe(model[0]);
                            model.RemoveAt(0);
                        }

                        break;

                    case 3:
                        if (model.Count > 0)
                        {
                            deque.RemoveLast().ShouldBe(model[model.Count - 1]);
                            model.RemoveAt(model.Count - 1);
                        }

                        break;

                    case 4:
                        deque.TrimExcess();
                        break;

                    default:
                        deque.Clear();
                        model.Clear();
                        break;
                }

                deque.Count.ShouldBe(model.Count);

                // Checked on every step, not only at the end: a ring that drifts would still be
                // self-consistent on Count alone, and the contents are what the model pins down.
                if (step % 37 == 0)
                {
                    deque.ToArray().ShouldBe(model.ToArray());
                    for (var index = 0; index < model.Count; index++)
                    {
                        deque[index].ShouldBe(model[index]);
                    }
                }
            }

            deque.ToArray().ShouldBe(model.ToArray());
        }

        [Fact]
        public void Growth_CopiesFewerThanTwoElementsPerAdd()
        {
            // The structural stand-in for "O(1) amortized": the only super-constant work an add
            // does is the copy a growth triggers, and a doubling buffer copies fewer than two
            // elements per element ever added. Asserted from observable capacity changes rather
            // than from a timing measurement.
            const int elements = 10000;
            var deque = new Deque<int>();
            var copied = 0L;
            var capacity = deque.Capacity;

            for (var i = 0; i < elements; i++)
            {
                deque.AddLast(i);
                if (deque.Capacity != capacity)
                {
                    copied += i;
                    capacity = deque.Capacity;
                }
            }

            copied.ShouldBeLessThan(2L * elements);
            deque.Capacity.ShouldBeLessThanOrEqualTo(2 * elements);
            deque.Count.ShouldBe(elements);
        }

        // ------------------------------------------------------------------
        // Positional access - the list face
        // ------------------------------------------------------------------

        [Fact]
        public void Indexer_ReadsEveryPositionFromHeadToTail()
        {
            var deque = new Deque<int>(4);
            for (var i = 0; i < 9; i++)
            {
                deque.AddLast(i);
            }

            // Crosses the wrap and at least one growth, so both index shapes are exercised.
            for (var i = 0; i < deque.Count; i++)
            {
                deque[i].ShouldBe(i);
            }
        }

        [Fact]
        public void Indexer_WritesInPlaceWithoutShifting()
        {
            var deque = new Deque<string>(new[] { "a", "b", "c" });

            deque[1] = "B";

            deque.ToArray().ShouldBe(new[] { "a", "B", "c" });
            deque.Count.ShouldBe(3);
        }

        [Fact]
        public void Indexer_OutOfRange_ThrowsArgumentOutOfRangeException()
        {
            var deque = new Deque<int>(new[] { 1, 2 });

            Should.Throw<ArgumentOutOfRangeException>(() => { var ignored = deque[2]; });
            Should.Throw<ArgumentOutOfRangeException>(() => { var ignored = deque[-1]; });
            Should.Throw<ArgumentOutOfRangeException>(() => deque[2] = 0);
            Should.Throw<ArgumentOutOfRangeException>(() => deque[-1] = 0);
        }

        [Fact]
        public void IndexOf_ReturnsTheFirstMatchCountingFromTheHead()
        {
            var deque = new Deque<string>(new[] { "b", "c", "b" });

            deque.IndexOf("b").ShouldBe(0);
            deque.IndexOf("c").ShouldBe(1);
            deque.IndexOf("z").ShouldBe(-1);
        }

        [Fact]
        public void Contains_AgreesWithIndexOf()
        {
            var deque = new Deque<string>(new[] { "b", "c" });

            deque.Contains("b").ShouldBeTrue();
            deque.Contains("z").ShouldBeFalse();
        }

        [Fact]
        public void Insert_AtTheFront_BehavesLikeAddFirst()
        {
            var deque = new Deque<string>(new[] { "b" });
            deque.Insert(0, "a");

            deque.ToArray().ShouldBe(new[] { "a", "b" });
        }

        [Fact]
        public void Insert_AtTheBack_BehavesLikeAddLast()
        {
            var deque = new Deque<string>(new[] { "a" });
            deque.Insert(1, "b");

            deque.ToArray().ShouldBe(new[] { "a", "b" });
        }

        [Fact]
        public void Insert_AndRemoveAt_KeepTheWholeOrderAcrossTheWrap()
        {
            var deque = new Deque<int>(4);
            var model = new List<int>();
            for (var i = 0; i < 6; i++)
            {
                deque.AddLast(i);
                model.Add(i);
            }

            deque.RemoveFirst();
            model.RemoveAt(0);
            deque.RemoveFirst();
            model.RemoveAt(0);
            deque.AddLast(6);
            model.Add(6);
            deque.AddLast(7);
            model.Add(7);
            deque.AddLast(8);
            model.Add(8);

            // Wrapped: the tail sits at a lower slot than the head, so both shifting directions
            // below run their two-segment shape. Checked against the model rather than by hand,
            // because the expected sequence is what a hand-written one gets wrong.
            deque.ToArray().ShouldBe(model.ToArray());

            var insertions = new[] { (1, 99), (5, 98), (0, 97) };
            foreach (var (index, value) in insertions)
            {
                deque.Insert(index, value);
                model.Insert(index, value);
                deque.ToArray().ShouldBe(model.ToArray());
            }

            // The last slot is named by -1 rather than by Count - 1, because the array literal
            // would freeze Count at the value it had before any removal ran.
            foreach (var offset in new[] { 0, 4, -1 })
            {
                var index = offset < 0 ? deque.Count - 1 : offset;
                deque.RemoveAt(index);
                model.RemoveAt(index);
                deque.ToArray().ShouldBe(model.ToArray());
            }
        }

        [Fact]
        public void Insert_OutOfRange_ThrowsArgumentOutOfRangeException()
        {
            var deque = new Deque<int>(new[] { 1, 2 });

            Should.Throw<ArgumentOutOfRangeException>(() => deque.Insert(3, 0));
            Should.Throw<ArgumentOutOfRangeException>(() => deque.Insert(-1, 0));
        }

        [Fact]
        public void RemoveAt_DropsTheElementAtEitherEndOrInTheMiddle()
        {
            var deque = new Deque<int>(4);
            for (var i = 0; i < 6; i++)
            {
                deque.AddLast(i);
            }

            deque.RemoveAt(0);
            deque.ToArray().ShouldBe(new[] { 1, 2, 3, 4, 5 });

            deque.RemoveAt(deque.Count - 1);
            deque.ToArray().ShouldBe(new[] { 1, 2, 3, 4 });

            deque.RemoveAt(2);
            deque.ToArray().ShouldBe(new[] { 1, 2, 4 });
            deque.Count.ShouldBe(3);
        }

        [Fact]
        public void RemoveAt_OutOfRange_ThrowsArgumentOutOfRangeException()
        {
            var deque = new Deque<int>(new[] { 1, 2 });

            Should.Throw<ArgumentOutOfRangeException>(() => deque.RemoveAt(2));
            Should.Throw<ArgumentOutOfRangeException>(() => deque.RemoveAt(-1));
        }

        [Fact]
        public void Remove_DropsTheFirstMatchAndReportsWhetherItFoundOne()
        {
            var deque = new Deque<string>(new[] { "b", "c", "b" });

            deque.Remove("b").ShouldBeTrue();
            deque.ToArray().ShouldBe(new[] { "c", "b" });
            deque.Remove("z").ShouldBeFalse();
            deque.Count.ShouldBe(2);
        }

        // ------------------------------------------------------------------
        // Null elements - the MultiList convention
        // ------------------------------------------------------------------

        [Fact]
        public void NullElements_AreStoredLikeAnyOtherElement()
        {
            var deque = new Deque<string>();

            deque.AddFirst(null);
            deque.AddLast("b");
            deque.AddLast(null);

            deque.Count.ShouldBe(3);
            deque.GetFirst().ShouldBeNull();
            deque.GetLast().ShouldBeNull();
            deque[0].ShouldBeNull();
            deque[1].ShouldBe("b");
            deque.IndexOf(null).ShouldBe(0);
            deque.Contains(null).ShouldBeTrue();
            deque.ToArray().ShouldBe(new[] { null, "b", null });
        }

        [Fact]
        public void NullElements_SurviveWrappingAndGrowth()
        {
            var deque = new Deque<string>(4);
            deque.AddLast(null);
            deque.AddLast("b");
            deque.AddLast(null);
            deque.AddLast("d");
            deque.RemoveFirst();
            deque.AddLast(null);
            deque.AddLast("f");

            deque.ToArray().ShouldBe(new[] { "b", null, "d", null, "f" });
            deque.Remove(null).ShouldBeTrue();
            deque.ToArray().ShouldBe(new[] { "b", "d", null, "f" });
        }

        [Fact]
        public void NullElement_IsDistinguishableFromAnAbsentOne()
        {
            var deque = new Deque<string>(new[] { "a" });

            deque.IndexOf(null).ShouldBe(-1);
            deque.Contains(null).ShouldBeFalse();
            deque.Remove(null).ShouldBeFalse();
        }

        // ------------------------------------------------------------------
        // Capacity
        // ------------------------------------------------------------------

        [Fact]
        public void Capacity_GrowsByDoublingFromFour()
        {
            var deque = new Deque<int>();
            var seen = new List<int>();

            for (var i = 0; i < 20; i++)
            {
                deque.AddLast(i);
                if (seen.Count == 0 || seen[seen.Count - 1] != deque.Capacity)
                {
                    seen.Add(deque.Capacity);
                }
            }

            seen.ShouldBe(new[] { 4, 8, 16, 32 });
        }

        [Fact]
        public void Capacity_BelowCount_IsRejected()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });

            Should.Throw<ArgumentOutOfRangeException>(() => deque.Capacity = 2);
            Should.Throw<ArgumentOutOfRangeException>(() => deque.Capacity = -1);
        }

        [Fact]
        public void Capacity_SetAboveCount_KeepsTheOrder()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            deque.Capacity = 16;

            deque.Capacity.ShouldBe(16);
            deque.ToArray().ShouldBe(new[] { 1, 2, 3 });
        }

        [Fact]
        public void Capacity_SetBelowTheCurrentBuffer_ShrinksAndKeepsTheOrder()
        {
            var deque = new Deque<int>(16);
            deque.AddLast(1);
            deque.AddLast(2);
            deque.AddLast(3);
            deque.RemoveFirst();
            deque.AddLast(4);

            deque.Capacity = 4;
            deque.ToArray().ShouldBe(new[] { 2, 3, 4 });
            deque.Capacity.ShouldBe(4);
        }

        [Fact]
        public void TrimExcess_ReclaimsTheSlackOfADrainedDeque()
        {
            var deque = new Deque<int>(64);
            for (var i = 0; i < 64; i++)
            {
                deque.AddLast(i);
            }

            while (deque.Count > 3)
            {
                deque.RemoveFirst();
            }

            deque.Capacity.ShouldBe(64);
            deque.TrimExcess();
            deque.Capacity.ShouldBe(3);
            deque.ToArray().ShouldBe(new[] { 61, 62, 63 });
        }

        [Fact]
        public void TrimExcess_LeavesANearlyFullBufferAlone()
        {
            var deque = new Deque<int>(10);
            for (var i = 0; i < 10; i++)
            {
                deque.AddLast(i);
            }

            deque.TrimExcess();

            // 10 of 10 - above the nine-tenths threshold, so no copy happens.
            deque.Capacity.ShouldBe(10);
        }

        [Fact]
        public void Clear_EmptiesTheDequeAndKeepsTheBuffer()
        {
            var deque = new Deque<int>(8);
            for (var i = 0; i < 8; i++)
            {
                deque.AddLast(i);
            }

            deque.Clear();

            deque.Count.ShouldBe(0);
            deque.IsEmpty.ShouldBeTrue();
            deque.Capacity.ShouldBe(8);
            deque.ToArray().ShouldBeEmpty();
        }

        [Fact]
        public void Clear_OnAWrappedRing_IsFollowedByAUsableDeque()
        {
            var deque = new Deque<int>(4);
            deque.AddLast(1);
            deque.AddLast(2);
            deque.AddLast(3);
            deque.RemoveFirst();
            deque.AddLast(4);
            deque.Clear();

            deque.AddFirst(9);
            deque.AddLast(8);

            deque.ToArray().ShouldBe(new[] { 9, 8 });
        }

        [Fact]
        public void ToArray_ReturnsAnIndependentSnapshot()
        {
            var deque = new Deque<int>(new[] { 1, 2 });
            var snapshot = deque.ToArray();

            deque.AddLast(3);
            deque[0] = 99;

            snapshot.ShouldBe(new[] { 1, 2 });
        }

        // ------------------------------------------------------------------
        // Enumeration
        // ------------------------------------------------------------------

        [Fact]
        public void GetEnumerator_WalksFromHeadToTail()
        {
            var deque = new Deque<int>(4);
            for (var i = 0; i < 7; i++)
            {
                deque.AddLast(i);
            }

            deque.ShouldBe(Enumerable.Range(0, 7));
        }

        [Fact]
        public void GetEnumerator_IsAStructSoAForeachAllocatesNothing()
        {
            typeof(Deque<int>.Enumerator).IsValueType.ShouldBeTrue();

            // The typed method has to return the struct: that is what lets `foreach` over a
            // Deque<int> bind to it instead of boxing through IEnumerable<int>.
            typeof(Deque<int>).GetMethod("GetEnumerator", Type.EmptyTypes).ReturnType
                .ShouldBe(typeof(Deque<int>.Enumerator));
        }

        [Fact]
        public void Enumerator_IsInvalidatedByMutation()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            using var enumerator = deque.GetEnumerator();

            enumerator.MoveNext().ShouldBeTrue();
            deque.AddLast(4);

            Should.Throw<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Fact]
        public void Enumerator_ResetReturnsToTheHead()
        {
            var deque = new Deque<int>(new[] { 1, 2 });
            var enumerator = deque.GetEnumerator();

            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(1);
            enumerator.Reset();
            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(1);
        }

        [Fact]
        public void NonGenericEnumerator_WalksTheSameSequence()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            var seen = new List<int>();

            foreach (var item in (IEnumerable)deque)
            {
                seen.Add((int)item);
            }

            seen.ShouldBe(new[] { 1, 2, 3 });
        }

        // ------------------------------------------------------------------
        // View, clone and the interface faces
        // ------------------------------------------------------------------

        [Fact]
        public void AsReadOnly_IsLiveAndExposesPositions()
        {
            var deque = new Deque<string>(new[] { "a", "b" });
            var view = deque.AsReadOnly();

            view.Count.ShouldBe(2);
            view[0].ShouldBe("a");

            deque.AddLast("c");

            // Live: the view reflects the change, positions included.
            view.Count.ShouldBe(3);
            view[2].ShouldBe("c");
        }

        [Fact]
        public void AsReadOnly_IsStillAssignableToIReadOnlyCollection()
        {
            var deque = new Deque<int>(new[] { 1, 2 });

            // The declared return type is the wider IReadOnlyList<T>; nothing stops a caller from
            // taking the narrower view the rest of the package hands out.
            IReadOnlyCollection<int> view = deque.AsReadOnly();
            view.Count.ShouldBe(2);
        }

        [Fact]
        public void Clone_IsAnIndependentSnapshotInTheSameOrder()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            var clone = deque.Clone();

            clone.ToArray().ShouldBe(new[] { 1, 2, 3 });

            clone.AddFirst(0);
            deque.RemoveFirst();

            clone.ToArray().ShouldBe(new[] { 0, 1, 2, 3 });
            deque.ToArray().ShouldBe(new[] { 2, 3 });
        }

        [Fact]
        public void ToString_JoinsTheElementsFromHeadToTail()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });

            deque.ToString().ShouldBe("1,2,3");
        }

        [Fact]
        public void Deque_ImplementsBothListInterfaces()
        {
            var deque = new Deque<int>(new[] { 2, 3 });

            (deque is IReadOnlyList<int>).ShouldBeTrue();
            (deque is IList<int>).ShouldBeTrue();
            (deque is ICollection<int>).ShouldBeTrue();
            (deque is IReadOnlyCollection<int>).ShouldBeTrue();
        }

        [Fact]
        public void IListFace_ReadsAndWritesThroughTheSamePositions()
        {
            IList<int> list = new Deque<int>(new[] { 1, 2, 3 });

            list.Count.ShouldBe(3);
            list.IsReadOnly.ShouldBeFalse();
            list[0] = 9;
            list.IndexOf(3).ShouldBe(2);
            list.Insert(0, 0);
            list.ToArray().ShouldBe(new[] { 0, 9, 2, 3 });

            list.RemoveAt(0);
            list.Remove(2).ShouldBeTrue();
            list.Contains(3).ShouldBeTrue();

            var buffer = new int[2];
            list.CopyTo(buffer, 0);
            buffer.ShouldBe(new[] { 9, 3 });
        }

        [Fact]
        public void IReadOnlyListFace_AddressesTheSequenceFromTheHead()
        {
            IReadOnlyList<string> list = new Deque<string>(new[] { "a", "b", "c" });

            list.Count.ShouldBe(3);
            list[0].ShouldBe("a");
            list[2].ShouldBe("c");
        }

        [Fact]
        public void CopyTo_RejectsATargetThatIsTooSmallOrMisplaced()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });

            Should.Throw<ArgumentNullException>(() => deque.CopyTo(null, 0));
            Should.Throw<ArgumentOutOfRangeException>(() => deque.CopyTo(new int[3], -1));
            Should.Throw<ArgumentException>(() => deque.CopyTo(new int[3], 1));
        }

        [Fact]
        public void CopyTo_WritesAtTheRequestedOffset()
        {
            var deque = new Deque<int>(new[] { 1, 2, 3 });
            var buffer = new int[5];

            deque.CopyTo(buffer, 2);

            buffer.ShouldBe(new[] { 0, 0, 1, 2, 3 });
        }
    }
}
