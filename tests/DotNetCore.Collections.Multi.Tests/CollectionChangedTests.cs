using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// F6-12: the self-authored change notification on <see cref="MultiList{T}"/> and
    /// <see cref="MultiDictionary{TKey,TValue}"/>. Covers what each mutation reports (kind, subject,
    /// count moved and resulting count), that a no-op stays silent, that bulk members raise once per
    /// element/value, and that the handler runs against the already-updated collection.
    /// </summary>
    public class CollectionChangedTests
    {
        // ------------------------------------------------------------------
        // MultiList<T> - what each mutation reports
        // ------------------------------------------------------------------

        [Fact]
        public void Bag_Add_ReportsAddedCopiesAndNewCount()
        {
            var bag = new MultiList<string>();
            var events = new List<CollectionChangedEventArgs<string>>();
            bag.CollectionChanged += (sender, e) => events.Add(e);

            bag.Add("a", 3);

            var e0 = events.ShouldHaveSingleItem();
            e0.ChangeType.ShouldBe(CollectionChangeType.Add);
            e0.Key.ShouldBe("a");
            e0.Count.ShouldBe(3);
            e0.NewCount.ShouldBe(3);
        }

        [Fact]
        public void Bag_AddAgain_ReportsTheNewTotal()
        {
            var bag = new MultiList<string>();
            bag.Add("a", 3);

            var events = new List<CollectionChangedEventArgs<string>>();
            bag.CollectionChanged += (sender, e) => events.Add(e);

            bag.Add("a");

            var e0 = events.ShouldHaveSingleItem();
            e0.ChangeType.ShouldBe(CollectionChangeType.Add);
            e0.Count.ShouldBe(1);
            e0.NewCount.ShouldBe(4);
        }

        [Fact]
        public void Bag_Remove_ReportsRemovedCopiesAndRemaining()
        {
            var bag = new MultiList<string>();
            bag.Add("a", 3);

            var events = new List<CollectionChangedEventArgs<string>>();
            bag.CollectionChanged += (sender, e) => events.Add(e);

            bag.Remove("a");

            var e0 = events.ShouldHaveSingleItem();
            e0.ChangeType.ShouldBe(CollectionChangeType.Remove);
            e0.Key.ShouldBe("a");
            e0.Count.ShouldBe(1);
            e0.NewCount.ShouldBe(2);
        }

        [Fact]
        public void Bag_RemoveMoreThanHeld_ReportsOnlyWhatWasThere()
        {
            var bag = new MultiList<string>();
            bag.Add("a", 2);

            var events = new List<CollectionChangedEventArgs<string>>();
            bag.CollectionChanged += (sender, e) => events.Add(e);

            bag.Remove("a", 5);

            var e0 = events.ShouldHaveSingleItem();
            e0.Count.ShouldBe(2);
            e0.NewCount.ShouldBe(0);
        }

        [Fact]
        public void Bag_RemoveAbsentElement_IsSilent()
        {
            var bag = new MultiList<string>();
            var raised = 0;
            bag.CollectionChanged += (sender, e) => raised++;

            bag.Remove("nope").ShouldBe(0);

            raised.ShouldBe(0);
        }

        [Fact]
        public void Bag_RemoveAllCopies_ReportsTheWholeBucket()
        {
            var bag = new MultiList<string>();
            bag.Add("a", 4);

            var events = new List<CollectionChangedEventArgs<string>>();
            bag.CollectionChanged += (sender, e) => events.Add(e);

            bag.RemoveAllCopies("a").ShouldBeTrue();

            var e0 = events.ShouldHaveSingleItem();
            e0.ChangeType.ShouldBe(CollectionChangeType.Remove);
            e0.Count.ShouldBe(4);
            e0.NewCount.ShouldBe(0);
        }

        [Fact]
        public void Bag_RemoveAllCopiesAbsentElement_IsSilent()
        {
            var bag = new MultiList<string>();
            var raised = 0;
            bag.CollectionChanged += (sender, e) => raised++;

            bag.RemoveAllCopies("nope").ShouldBeFalse();

            raised.ShouldBe(0);
        }

        [Fact]
        public void Bag_Clear_ReportsReset()
        {
            var bag = new MultiList<string> { "a", "b" };
            var events = new List<CollectionChangedEventArgs<string>>();
            bag.CollectionChanged += (sender, e) => events.Add(e);

            bag.Clear();

            var e0 = events.ShouldHaveSingleItem();
            e0.ChangeType.ShouldBe(CollectionChangeType.Reset);
            e0.Count.ShouldBe(0);
            e0.NewCount.ShouldBe(0);
            e0.Key.ShouldBeNull();
        }

        [Fact]
        public void Bag_ClearOnEmptyBag_IsSilent()
        {
            var bag = new MultiList<string>();
            var raised = 0;
            bag.CollectionChanged += (sender, e) => raised++;

            bag.Clear();

            raised.ShouldBe(0);
        }

        [Fact]
        public void Bag_NullElement_IsReportedLikeAnyOther()
        {
            var bag = new MultiList<string>();
            var events = new List<CollectionChangedEventArgs<string>>();
            bag.CollectionChanged += (sender, e) => events.Add(e);

            bag.Add(null!);

            var e0 = events.ShouldHaveSingleItem();
            e0.ChangeType.ShouldBe(CollectionChangeType.Add);
            e0.Key.ShouldBeNull();
            e0.NewCount.ShouldBe(1);
        }

        [Fact]
        public void Bag_AddRange_RaisesOncePerElement()
        {
            var bag = new MultiList<string>();
            var events = new List<CollectionChangedEventArgs<string>>();
            bag.CollectionChanged += (sender, e) => events.Add(e);

            bag.AddRange(new[] { "a", "a", "b" });

            events.Count.ShouldBe(3);
            events.Select(e => e.Key).ShouldBe(new[] { "a", "a", "b" });
            events.Select(e => e.NewCount).ShouldBe(new[] { 1, 2, 1 });
        }

        [Fact]
        public void Bag_SetOperations_RaiseForTheElementsTheyTouch()
        {
            var bag = new MultiList<string> { "a" };
            var events = new List<CollectionChangedEventArgs<string>>();
            bag.CollectionChanged += (sender, e) => events.Add(e);

            bag.UnionWith(new[] { "a", "b" });

            // Union is max-per-element: "a" is unchanged (no event), "b" is added once.
            var e0 = events.ShouldHaveSingleItem();
            e0.ChangeType.ShouldBe(CollectionChangeType.Add);
            e0.Key.ShouldBe("b");
            e0.NewCount.ShouldBe(1);
        }

        [Fact]
        public void Bag_ExceptWith_ReportsRemovals()
        {
            var bag = new MultiList<string> { "a", "a", "b" };
            var events = new List<CollectionChangedEventArgs<string>>();
            bag.CollectionChanged += (sender, e) => events.Add(e);

            // MultiList treats the argument as a multiset: two argument copies cancel both stored
            // copies of "a", which the set is applied as one bucket-wide removal.
            bag.ExceptWith(new[] { "a", "a" });

            var e0 = events.ShouldHaveSingleItem();
            e0.ChangeType.ShouldBe(CollectionChangeType.Remove);
            e0.Key.ShouldBe("a");
            e0.Count.ShouldBe(2);
            e0.NewCount.ShouldBe(0);
        }

        [Fact]
        public void Bag_ExceptWith_PartialOverlap_ReportsOnlyTheCopiesCancelled()
        {
            var bag = new MultiList<string> { "a", "a", "b" };
            var events = new List<CollectionChangedEventArgs<string>>();
            bag.CollectionChanged += (sender, e) => events.Add(e);

            bag.ExceptWith(new[] { "a" });

            var e0 = events.ShouldHaveSingleItem();
            e0.ChangeType.ShouldBe(CollectionChangeType.Remove);
            e0.Count.ShouldBe(1);
            e0.NewCount.ShouldBe(1);
        }

        [Fact]
        public void Bag_Sender_IsTheBag()
        {
            var bag = new MultiList<string>();
            object sender = null;
            bag.CollectionChanged += (s, e) => sender = s;

            bag.Add("a");

            sender.ShouldBeSameAs(bag);
        }

        [Fact]
        public void Bag_Handler_ObservesTheAlreadyUpdatedState()
        {
            var bag = new MultiList<string>();
            var seen = 0;
            bag.CollectionChanged += (sender, e) => seen = bag.CountOf(e.Key);

            bag.Add("a", 3);

            seen.ShouldBe(3);
        }

        [Fact]
        public void Bag_MultipleHandlers_AllRun()
        {
            var bag = new MultiList<string>();
            var first = 0;
            var second = 0;
            bag.CollectionChanged += (sender, e) => first++;
            bag.CollectionChanged += (sender, e) => second++;

            bag.Add("a");

            first.ShouldBe(1);
            second.ShouldBe(1);
        }

        [Fact]
        public void Bag_UnsubscribingDuringRaise_DoesNotSkipTheHandlerAlreadyRunning()
        {
            var bag = new MultiList<string>();
            var second = 0;
            EventHandler<CollectionChangedEventArgs<string>> secondHandler = (sender, e) => second++;
            EventHandler<CollectionChangedEventArgs<string>> firstHandler = null;
            firstHandler = (sender, e) => bag.CollectionChanged -= secondHandler;
            bag.CollectionChanged += firstHandler;
            bag.CollectionChanged += secondHandler;

            bag.Add("a");

            // The handler list is snapshotted before the raise, so removing during the raise does
            // not reach into the notification already in flight.
            second.ShouldBe(1);
        }

        // ------------------------------------------------------------------
        // MultiDictionary<TKey, TValue> - what each mutation reports
        // ------------------------------------------------------------------

        [Fact]
        public void Map_Add_ReportsOneValueUnderTheKey()
        {
            var map = new MultiDictionary<string, int>();
            var events = new List<CollectionChangedEventArgs<string>>();
            map.CollectionChanged += (sender, e) => events.Add(e);

            map.Add("k", 1);

            var e0 = events.ShouldHaveSingleItem();
            e0.ChangeType.ShouldBe(CollectionChangeType.Add);
            e0.Key.ShouldBe("k");
            e0.Count.ShouldBe(1);
            e0.NewCount.ShouldBe(1);
        }

        [Fact]
        public void Map_AddSecondValue_ReportsTheGrowingValueCount()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("k", 1);

            var events = new List<CollectionChangedEventArgs<string>>();
            map.CollectionChanged += (sender, e) => events.Add(e);

            map.Add("k", 2);

            var e0 = events.ShouldHaveSingleItem();
            e0.Count.ShouldBe(1);
            e0.NewCount.ShouldBe(2);
        }

        [Fact]
        public void Map_AddDuplicateValue_WhenDeduplicated_IsSilent()
        {
            var map = new MultiDictionary<string, int>(allowDuplicateValues: false);
            map.Add("k", 1);

            var raised = 0;
            map.CollectionChanged += (sender, e) => raised++;

            map.Add("k", 1);

            raised.ShouldBe(0);
        }

        [Fact]
        public void Map_RemoveValue_ReportsOneValueRemoved()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("k", 1);
            map.Add("k", 2);

            var events = new List<CollectionChangedEventArgs<string>>();
            map.CollectionChanged += (sender, e) => events.Add(e);

            map.Remove("k", 1).ShouldBeTrue();

            var e0 = events.ShouldHaveSingleItem();
            e0.ChangeType.ShouldBe(CollectionChangeType.Remove);
            e0.Key.ShouldBe("k");
            e0.Count.ShouldBe(1);
            e0.NewCount.ShouldBe(1);
        }

        [Fact]
        public void Map_RemoveValueAbsent_IsSilent()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("k", 1);

            var raised = 0;
            map.CollectionChanged += (sender, e) => raised++;

            map.Remove("k", 99).ShouldBeFalse();

            raised.ShouldBe(0);
        }

        [Fact]
        public void Map_RemoveKey_ReportsTheWholeKey()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("k", 1);
            map.Add("k", 2);

            var events = new List<CollectionChangedEventArgs<string>>();
            map.CollectionChanged += (sender, e) => events.Add(e);

            map.Remove("k").ShouldBeTrue();

            var e0 = events.ShouldHaveSingleItem();
            e0.ChangeType.ShouldBe(CollectionChangeType.Remove);
            e0.Key.ShouldBe("k");
            e0.Count.ShouldBe(2);
            e0.NewCount.ShouldBe(0);
        }

        [Fact]
        public void Map_RemoveAbsentKey_IsSilent()
        {
            var map = new MultiDictionary<string, int>();
            var raised = 0;
            map.CollectionChanged += (sender, e) => raised++;

            map.Remove("nope").ShouldBeFalse();

            raised.ShouldBe(0);
        }

        [Fact]
        public void Map_Clear_ReportsReset()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("k", 1);

            var events = new List<CollectionChangedEventArgs<string>>();
            map.CollectionChanged += (sender, e) => events.Add(e);

            map.Clear();

            var e0 = events.ShouldHaveSingleItem();
            e0.ChangeType.ShouldBe(CollectionChangeType.Reset);
            e0.Count.ShouldBe(0);
            e0.NewCount.ShouldBe(0);
        }

        [Fact]
        public void Map_ClearOnEmptyMap_IsSilent()
        {
            var map = new MultiDictionary<string, int>();
            var raised = 0;
            map.CollectionChanged += (sender, e) => raised++;

            map.Clear();

            raised.ShouldBe(0);
        }

        [Fact]
        public void Map_AddRange_RaisesOncePerValue()
        {
            var map = new MultiDictionary<string, int>();
            var events = new List<CollectionChangedEventArgs<string>>();
            map.CollectionChanged += (sender, e) => events.Add(e);

            map.AddRange("k", new[] { 1, 2, 3 });

            events.Count.ShouldBe(3);
            events.Select(e => e.NewCount).ShouldBe(new[] { 1, 2, 3 });
        }

        [Fact]
        public void Map_ExceptWith_RaisesForTheRemovedValues()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("k", 1);
            map.Add("k", 2);

            var events = new List<CollectionChangedEventArgs<string>>();
            map.CollectionChanged += (sender, e) => events.Add(e);

            map.ExceptWith("k", new[] { 1 });

            var e0 = events.ShouldHaveSingleItem();
            e0.ChangeType.ShouldBe(CollectionChangeType.Remove);
            e0.Key.ShouldBe("k");
            e0.NewCount.ShouldBe(1);
        }

        [Fact]
        public void Map_Sender_IsTheMap()
        {
            var map = new MultiDictionary<string, int>();
            object sender = null;
            map.CollectionChanged += (s, e) => sender = s;

            map.Add("k", 1);

            sender.ShouldBeSameAs(map);
        }

        // ------------------------------------------------------------------
        // The event-args type itself
        // ------------------------------------------------------------------

        [Fact]
        public void EventArgs_ToString_SummarizesTheChange()
        {
            var added = new CollectionChangedEventArgs<string>(CollectionChangeType.Add, "a", 3, 3);
            added.ToString().ShouldBe("Add a x3 -> 3");

            var reset = new CollectionChangedEventArgs<string>(CollectionChangeType.Reset, null!, 0, 0);
            reset.ToString().ShouldBe("Reset");
        }
    }
}
