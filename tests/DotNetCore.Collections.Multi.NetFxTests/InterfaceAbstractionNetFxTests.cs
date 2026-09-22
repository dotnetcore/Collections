using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.NetFxTests
{
    /// <summary>
    /// F6-32: the interface abstraction layer, pinned at <em>runtime</em> on the affected .NET
    /// Framework generation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The main suite runs on net8.0 and therefore can not see this class of failure. The hazard
    /// is the one F6-24 documented: the 4.5.1 / 4.6.1-era runtimes do not carry
    /// <c>IReadOnlyCollection&lt;T&gt;</c> for the framework's own <c>HashSet&lt;T&gt;</c> /
    /// <c>SortedSet&lt;T&gt;</c>, so a bare cast of a deduplicating inner collection
    /// (<c>allowDuplicateValues: false</c>) to that interface threw
    /// <see cref="InvalidCastException"/> there and nowhere else. Adding
    /// <see cref="IMultiDictionary{TKey,TValue}"/> widens the number of code paths that reach the
    /// per-key collection through an <c>IReadOnly*</c> declaration - the interface itself inherits
    /// <c>IReadOnlyDictionary&lt;TKey, IReadOnlyCollection&lt;TValue&gt;&gt;</c>, so its indexer,
    /// its inherited enumeration and the inherited dictionary view all hand back an
    /// <c>IReadOnlyCollection&lt;TValue&gt;</c> - which makes this the generation to prove the
    /// exposure on, exactly as §6 of the 6.6 plan requires for this item.
    /// </para>
    /// <para>
    /// Every test below therefore <em>casts through the interface</em> and then reads, rather than
    /// calling the concrete member: that is the shape that would break if the exposure regressed.
    /// </para>
    /// </remarks>
    public class InterfaceAbstractionNetFxTests
    {
        // ------------------------------------------------------------------
        // IMultiSet<T>
        // ------------------------------------------------------------------

        [Fact]
        public void MultiList_CastToIMultiSet_ReadsAtRuntime()
        {
            IMultiSet<int> set = new MultiList<int>();
            set.Add(7, 3);
            set.Add(9);

            set.TotalCount.ShouldBe(4);
            set.DistinctCount.ShouldBe(2);
            set.CountOf(7).ShouldBe(3);
            set.Contains(9).ShouldBeTrue();
            set.Remove(7).ShouldBe(2);

            // The inherited IReadOnlyCollection<T> exposure - the interface F6-24's hazard lives on.
            // Three copies remain: 7, 7 and 9.
            IReadOnlyCollection<int> asCollection = set;
            asCollection.Count.ShouldBe(3);
            asCollection.Sum().ShouldBe(23);
        }

        [Fact]
        public void OrderedMultiList_CastToIMultiSet_ReadsAtRuntime()
        {
            IMultiSet<int> set = new OrderedMultiList<int>();
            set.Add(7, 3);
            set.Add(9);

            set.TotalCount.ShouldBe(4);
            set.DistinctCount.ShouldBe(2);

            IReadOnlyCollection<int> asCollection = set;
            asCollection.Count.ShouldBe(4);
        }

        [Fact]
        public void IMultiSet_EntrySet_EnumeratesAtRuntime()
        {
            IMultiSet<int> set = new MultiList<int>();
            set.Add(1, 2);
            set.Add(2, 3);

            var entries = set.EntrySet().ToList();
            entries.Count.ShouldBe(2);
            entries.Sum(e => e.Count).ShouldBe(5);
        }

        // ------------------------------------------------------------------
        // IMultiDictionary<TKey, TValue> - both inner-collection shapes
        // ------------------------------------------------------------------

        [Fact]
        public void MultiDictionary_CastToIMultiDictionary_WithListInner_ReadsAtRuntime()
        {
            IMultiDictionary<int, string> map = new MultiDictionary<int, string>();
            map.Add(1, "a");
            map.Add(1, "b");

            map.KeyCount.ShouldBe(1);
            map.TotalValueCount.ShouldBe(2);

            // Flat on the interface, grouped through the inherited view.
            map.Values.Count().ShouldBe(2);

            IReadOnlyDictionary<int, IReadOnlyCollection<string>> asDictionary = map;
            asDictionary.Count.ShouldBe(1);
            asDictionary[1].Count.ShouldBe(2);
        }

        /// <summary>
        /// The deduplicating (<c>HashSet</c>) inner collection - the exact shape that threw on the
        /// affected generations - reached through the interface.
        /// </summary>
        [Fact]
        public void MultiDictionary_CastToIMultiDictionary_WithHashSetInner_ReadsAtRuntime()
        {
            IMultiDictionary<int, string> map = new MultiDictionary<int, string>(allowDuplicateValues: false);
            map.Add(1, "a");
            map.Add(1, "b");
            map.Add(1, "b");

            map.KeyCount.ShouldBe(1);
            map.TotalValueCount.ShouldBe(2);
            map.ValueCount(1).ShouldBe(2);
            map.Contains(1, "a").ShouldBeTrue();

            // The per-key read goes through the interface's inherited indexer.
            map[1].Count.ShouldBe(2);
            map[1].ShouldContain("a");

            map.TryGetValue(1, out var values).ShouldBeTrue();
            values.ShouldContain("b");

            IReadOnlyDictionary<int, IReadOnlyCollection<string>> asDictionary = map;
            asDictionary[1].Count.ShouldBe(2);
            asDictionary.Values.Single().Count.ShouldBe(2);
        }

        /// <summary>The deduplicating (<c>SortedSet</c>) inner collection.</summary>
        [Fact]
        public void OrderedMultiDictionary_CastToIMultiDictionary_WithSortedSetInner_ReadsAtRuntime()
        {
            IMultiDictionary<int, string> map = new OrderedMultiDictionary<int, string>(allowDuplicateValues: false);
            map.Add(1, "a");
            map.Add(1, "b");

            map.KeyCount.ShouldBe(1);
            map[1].Count.ShouldBe(2);

            IReadOnlyDictionary<int, IReadOnlyCollection<string>> asDictionary = map;
            asDictionary[1].ShouldContain("a");
        }

        [Fact]
        public void IMultiDictionary_InheritedEnumeration_IsPerKeyAtRuntime()
        {
            IMultiDictionary<int, string> map = new MultiDictionary<int, string>();
            map.Add(1, "a");
            map.Add(1, "b");
            map.Add(2, "c");

            var keys = new List<int>();
            foreach (var pair in map)
            {
                keys.Add(pair.Key);
                pair.Value.Count.ShouldBe(map.ValueCount(pair.Key));
            }

            keys.Count.ShouldBe(2);
            keys.ShouldBe(new[] { 1, 2 }, ignoreOrder: true);
        }

        // ------------------------------------------------------------------
        // IBiMap<TLeft, TRight>
        // ------------------------------------------------------------------

        [Fact]
        public void BiDictionary_CastToIBiMap_ResolvesBothDirectionsAtRuntime()
        {
            IBiMap<int, string> map = new BiDictionary<int, string>();
            map.Add(1, "alice");
            map.Add(2, "bob");

            map.Count.ShouldBe(2);
            map[1].ShouldBe("alice");
            map.GetLeft("bob").ShouldBe(2);
            map.ContainsRight("alice").ShouldBeTrue();
            map.TryGetLeft("alice", out var left).ShouldBeTrue();
            left.ShouldBe(1);

            // The inherited forward dictionary view.
            IReadOnlyDictionary<int, string> asDictionary = map;
            asDictionary.Count.ShouldBe(2);
            asDictionary[2].ShouldBe("bob");
            asDictionary.Values.Count().ShouldBe(2);
        }

        // ------------------------------------------------------------------
        // The whole surface, reached only through the inherited BCL abstractions
        // ------------------------------------------------------------------

        [Fact]
        public void AllThreeInterfaces_AreReachableThroughTheInheritedBclAbstractions()
        {
            // These casts are the exposure the item has to hold on this generation: each one
            // crosses from our interface into the inherited BCL abstraction at runtime.
            IReadOnlyCollection<int> set = (IMultiSet<int>)new MultiList<int>();
            IReadOnlyCollection<int> orderedSet = (IMultiSet<int>)new OrderedMultiList<int>();
            IReadOnlyDictionary<string, IReadOnlyCollection<int>> map =
                (IMultiDictionary<string, int>)new MultiDictionary<string, int>();
            IReadOnlyDictionary<string, IReadOnlyCollection<int>> orderedMap =
                (IMultiDictionary<string, int>)new OrderedMultiDictionary<string, int>();
            IReadOnlyDictionary<int, string> biMap = (IBiMap<int, string>)new BiDictionary<int, string>();

            set.Count.ShouldBe(0);
            orderedSet.Count.ShouldBe(0);
            map.Count.ShouldBe(0);
            orderedMap.Count.ShouldBe(0);
            biMap.Count.ShouldBe(0);
        }
    }
}
