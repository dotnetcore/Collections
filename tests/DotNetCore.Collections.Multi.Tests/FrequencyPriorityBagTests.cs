using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// F6-08: <see cref="FrequencyPriorityBag{T}"/> - the frequency priority bag. Covers the
    /// most-frequent-first access, the documented tie policy (equal counts: the element that
    /// reached its current count earliest pops first), the lazy-heap correctness under churn
    /// (stale snapshots skipped, heap compacted), the MultiList-mirrored bag semantics and the
    /// first-class null element.
    /// </summary>
    public class FrequencyPriorityBagTests
    {
        // ------------------------------------------------------------------
        // Construction, counting, bag semantics
        // ------------------------------------------------------------------

        [Fact]
        public void Ctor_Collection_CountsOneCopyPerElement()
        {
            var bag = new FrequencyPriorityBag<string>(new[] { "a", "b", "b" });

            bag.TotalCount.ShouldBe(3);
            bag.DistinctCount.ShouldBe(2);
            bag.CountOf("b").ShouldBe(2);
        }

        [Fact]
        public void Ctor_NullCollection_ThrowsArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(() => new FrequencyPriorityBag<string>(null!));
        }

        [Fact]
        public void Add_CountsAccumulate()
        {
            var bag = new FrequencyPriorityBag<string>();
            bag.Add("a");
            bag.Add("a");
            bag.Add("a", 2);

            bag.CountOf("a").ShouldBe(4);
            bag.TotalCount.ShouldBe(4);
            bag.DistinctCount.ShouldBe(1);
        }

        [Fact]
        public void Add_NonPositiveTimes_ThrowsArgumentOutOfRangeException()
        {
            var bag = new FrequencyPriorityBag<string>();
            Should.Throw<ArgumentOutOfRangeException>(() => bag.Add("a", 0));
            Should.Throw<ArgumentOutOfRangeException>(() => bag.Add("a", -1));
        }

        [Fact]
        public void AddRange_AddsOneCopyPerElement()
        {
            var bag = new FrequencyPriorityBag<string>();
            bag.AddRange(new[] { "a", "b", "b" });

            bag.CountOf("b").ShouldBe(2);
        }

        [Fact]
        public void AddRange_NullArgument_ThrowsArgumentNullException()
        {
            var bag = new FrequencyPriorityBag<string>();
            Should.Throw<ArgumentNullException>(() => bag.AddRange(null!));
        }

        [Fact]
        public void Remove_ReturnsRemainingCopies()
        {
            var bag = new FrequencyPriorityBag<string>(new[] { "a", "a", "a" });

            bag.Remove("a").ShouldBe(2);
            bag.Remove("a").ShouldBe(1);
            bag.Remove("a").ShouldBe(0);
            bag.Remove("a").ShouldBe(0);
            bag.IsEmpty.ShouldBeTrue();
        }

        [Fact]
        public void RemoveAllCopies_RemovesEverythingOfTheElement()
        {
            var bag = new FrequencyPriorityBag<string>(new[] { "a", "a", "b" });

            bag.RemoveAllCopies("a").ShouldBeTrue();
            bag.RemoveAllCopies("a").ShouldBeFalse();
            bag.Contains("a").ShouldBeFalse();
            bag.TotalCount.ShouldBe(1);
        }

        // ------------------------------------------------------------------
        // Priority access and the tie policy
        // ------------------------------------------------------------------

        [Fact]
        public void PopMost_ReturnsTheMostFrequentElement()
        {
            var bag = new FrequencyPriorityBag<string>();
            bag.Add("a", 2);
            bag.Add("b", 5);
            bag.Add("c", 3);

            bag.PeekMost().ShouldBe("b");
            bag.PopMost().ShouldBe("b");

            // One copy removed: b is now at 4 and still most frequent ...
            bag.PeekMost().ShouldBe("b");
            bag.PopMost().ShouldBe("b");

            // ... popping b again leaves b at 3, c at 3 and a at 2.
            bag.PeekMost().ShouldBe("c");
            bag.TotalCount.ShouldBe(8);
        }

        [Fact]
        public void PopMost_Empty_ThrowsInvalidOperationException()
        {
            var bag = new FrequencyPriorityBag<string>();
            Should.Throw<InvalidOperationException>(() => bag.PopMost());
            Should.Throw<InvalidOperationException>(() => bag.PeekMost());
        }

        [Fact]
        public void TryPopMost_Empty_ReturnsFalse()
        {
            var bag = new FrequencyPriorityBag<string>();
            bag.TryPopMost(out var item).ShouldBeFalse();
            item.ShouldBeNull();
            bag.TryPeekMost(out var peeked).ShouldBeFalse();
            peeked.ShouldBeNull();
        }

        [Fact]
        public void TiePolicy_EqualCounts_TheElementThatReachedTheCountEarliestPopsFirst()
        {
            var bag = new FrequencyPriorityBag<string>();
            bag.Add("a");           // a: 1 (stamp 1)
            bag.Add("b");           // b: 1 (stamp 2)
            bag.Add("a");           // a: 2 (stamp 3) - a reaches 2 first
            bag.Add("b");           // b: 2 (stamp 4)

            bag.PopMost().ShouldBe("a");
            bag.PopMost().ShouldBe("b");
        }

        [Fact]
        public void TiePolicy_ReachedCountEarliest_TracksTheLastCountChange()
        {
            var bag = new FrequencyPriorityBag<string>();
            bag.Add("a", 2);        // a: 2 early
            bag.Add("b");           // b: 1
            bag.Add("b");           // b: 2 later than a

            bag.PeekMost().ShouldBe("a");

            // Touch a: removing and re-adding one copy restamps a at 2, later than b's stamp.
            bag.Remove("a").ShouldBe(1);
            bag.Add("a");

            // Both at 2 again, but b reached 2 earlier now.
            bag.PeekMost().ShouldBe("b");
        }

        [Fact]
        public void PopMost_DrainsInDescendingFrequencyOrder()
        {
            var bag = new FrequencyPriorityBag<string>();
            bag.Add("a", 3);
            bag.Add("b", 2);
            bag.Add("c", 1);

            // a:3 pops first; after each pop the element re-stamps at its new count, so the a:2
            // snapshot is now LATER than b's original 2 - b (still at 2, earlier stamp) outranks
            // it. The drain order demonstrates the tie policy at work.
            var drained = new List<string> { bag.PopMost(), bag.PopMost(), bag.PopMost(), bag.PopMost(), bag.PopMost(), bag.PopMost() };

            drained.ShouldBe(new[] { "a", "b", "a", "c", "b", "a" });
            bag.IsEmpty.ShouldBeTrue();
        }

        [Fact]
        public void PopMost_AfterElementRemovedFully_SkipsIt()
        {
            var bag = new FrequencyPriorityBag<string>();
            bag.Add("a", 5);
            bag.Add("b", 2);

            bag.RemoveAllCopies("a");

            // a's stale snapshots must be skipped; b is the most frequent survivor.
            bag.PeekMost().ShouldBe("b");
            bag.PopMost().ShouldBe("b");
            bag.PopMost().ShouldBe("b");
            bag.TryPeekMost(out _).ShouldBeFalse();
        }

        [Fact]
        public void PopMost_UnderHeavyChurn_StaysCorrect()
        {
            // Many count changes leave stale snapshots behind (pruned only on pops / compaction);
            // the priority answers must stay exact throughout. "a" oscillates 10 -> 9 -> 10 every
            // round (100 count changes of churn), "c" climbs 4 -> 54, "b" stays at 10.
            var bag = new FrequencyPriorityBag<string>();
            bag.Add("a", 10);
            bag.Add("b", 10);
            bag.Add("c", 4);
            for (var round = 0; round < 50; round++)
            {
                bag.Add("c");
                bag.Remove("a").ShouldBe(9);
                bag.Add("a");
            }

            bag.CountOf("a").ShouldBe(10);
            bag.PeekMost().ShouldBe("c");
            bag.PopMost().ShouldBe("c");
            bag.PopMost().ShouldBe("c");
        }

        // ------------------------------------------------------------------
        // Null element
        // ------------------------------------------------------------------

        [Fact]
        public void NullElement_IsFirstClass()
        {
            var bag = new FrequencyPriorityBag<string>();
            bag.Add(null!, 3);
            bag.Add("a", 2);

            bag.Contains(null).ShouldBeTrue();
            bag.CountOf(null).ShouldBe(3);
            bag.DistinctCount.ShouldBe(2);
            bag.PeekMost().ShouldBeNull();
            bag.PopMost().ShouldBeNull();
            bag.PeekMost().ShouldBe("a");
        }

        [Fact]
        public void NullElement_ParticipatesInTheTiePolicy()
        {
            var bag = new FrequencyPriorityBag<string>();
            bag.Add(null!);         // null: 1 (stamp 1) - earliest
            bag.Add("a");           // a: 1 (stamp 2)

            bag.PeekMost().ShouldBeNull();
            bag.PopMost().ShouldBeNull();
            bag.PeekMost().ShouldBe("a");
        }

        [Fact]
        public void NullElement_CanBeRemoved()
        {
            var bag = new FrequencyPriorityBag<string>();
            bag.Add(null!, 2);

            bag.Remove(null).ShouldBe(1);
            bag.CountOf(null).ShouldBe(1);
            bag.RemoveAllCopies(null).ShouldBeTrue();
            bag.Contains(null).ShouldBeFalse();
            bag.DistinctCount.ShouldBe(0);
        }

        // ------------------------------------------------------------------
        // Enumeration, projection, copying
        // ------------------------------------------------------------------

        [Fact]
        public void Enumeration_IsCopyExpanded()
        {
            var bag = new FrequencyPriorityBag<string>();
            bag.Add("a", 2);
            bag.Add("b", 1);

            bag.ToList().ShouldBe(new[] { "a", "a", "b" });
            bag.ToString().ShouldBe("a,a,b");
        }

        [Fact]
        public void EntrySet_IncludesEveryDistinctElementAndNull()
        {
            var bag = new FrequencyPriorityBag<string>();
            bag.Add("a", 2);
            bag.Add(null!, 1);

            bag.EntrySet().ShouldContain(("a", 2));
            bag.EntrySet().ShouldContain(((string)null!, 1));
            bag.EntrySet().Count().ShouldBe(2);
        }

        [Fact]
        public void Clone_IsIndependentOfTheOriginal()
        {
            var bag = new FrequencyPriorityBag<string>();
            bag.Add("a", 3);
            var clone = bag.Clone();

            clone.Add("b", 5);
            bag.Remove("a");

            clone.PeekMost().ShouldBe("b");
            bag.PeekMost().ShouldBe("a");
            bag.CountOf("a").ShouldBe(2);
        }

        [Fact]
        public void Clear_ResetsEverything()
        {
            var bag = new FrequencyPriorityBag<string>();
            bag.Add("a", 2);
            bag.Add(null!);
            bag.Clear();

            bag.IsEmpty.ShouldBeTrue();
            bag.DistinctCount.ShouldBe(0);
            bag.TotalCount.ShouldBe(0);
            bag.TryPeekMost(out _).ShouldBeFalse();
        }

        // ------------------------------------------------------------------
        // Randomized cross-check against a tracked model
        // ------------------------------------------------------------------

        [Fact]
        public void RandomizedOperations_MatchTrackedModel()
        {
            const int steps = 2000;
            var random = new Random(20260915);
            var bag = new FrequencyPriorityBag<string>();
            var model = new Dictionary<string, (int Count, long Seq)>();
            long seq = 0;

            // Expected most frequent: max count, ties by the smallest (earliest) stamp.
            string? ExpectedMost()
            {
                string? best = null;
                var bestCount = 0;
                var bestSeq = long.MaxValue;
                foreach (var pair in model)
                {
                    if (pair.Value.Count > bestCount
                        || (pair.Value.Count == bestCount && pair.Value.Count > 0 && pair.Value.Seq < bestSeq))
                    {
                        best = pair.Key;
                        bestCount = pair.Value.Count;
                        bestSeq = pair.Value.Seq;
                    }
                }

                return best;
            }

            for (var step = 0; step < steps; step++)
            {
                var key = "e" + random.Next(12);
                switch (random.Next(5))
                {
                    case 0:
                    case 1:
                        bag.Add(key);
                        var current = model.TryGetValue(key, out var e1) ? e1.Count : 0;
                        model[key] = (current + 1, ++seq);
                        break;

                    case 2:
                        var had = model.TryGetValue(key, out var e2) ? e2.Count : 0;
                        var expectedRemaining = had > 0 ? had - 1 : 0;
                        bag.Remove(key).ShouldBe(expectedRemaining);
                        if (had > 0)
                        {
                            if (had == 1)
                            {
                                model.Remove(key);
                            }
                            else
                            {
                                model[key] = (had - 1, ++seq);
                            }
                        }

                        break;

                    case 3:
                        var expectedAll = model.TryGetValue(key, out var e3) ? e3.Count : 0;
                        bag.RemoveAllCopies(key).ShouldBe(expectedAll > 0);
                        model.Remove(key);
                        break;

                    case 4:
                        var expected = ExpectedMost();
                        var actual = bag.TryPopMost(out var popped) ? popped : null;
                        actual.ShouldBe(expected);
                        if (expected != null)
                        {
                            var remaining = model[expected].Count - 1;
                            if (remaining == 0)
                            {
                                model.Remove(expected);
                            }
                            else
                            {
                                model[expected] = (remaining, ++seq);
                            }
                        }

                        break;
                }

                bag.TotalCount.ShouldBe(model.Values.Sum(e => e.Count));
                bag.DistinctCount.ShouldBe(model.Count);
            }
        }
    }
}
