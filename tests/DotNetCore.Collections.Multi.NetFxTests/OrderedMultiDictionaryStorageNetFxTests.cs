using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.NetFxTests
{
    /// <summary>
    /// F6-36: the ordered multimap's reworked storage layer, pinned at <em>runtime</em> on the
    /// affected .NET Framework generation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The main suite runs on net8.0 and therefore can not see this class of failure. The storage
    /// rework replaced the per-key collection with a bucket reached through a slot of the
    /// order-statistic tree, and every path that hands values to a caller still goes through the
    /// internal <c>ReadOnlyCollectionView&lt;T&gt;</c> wrapper — the exposure F6-24 documented, where
    /// the 4.5.1 / 4.6.1-era runtimes do not carry <c>IReadOnlyCollection&lt;T&gt;</c> for the
    /// framework's own collections and a bare cast threw <see cref="System.InvalidCastException"/>
    /// there and nowhere else.
    /// </para>
    /// <para>
    /// Every test below therefore <em>casts through the interface</em> and then reads, rather than
    /// calling a concrete member: that is the shape that would break if the exposure regressed.
    /// </para>
    /// </remarks>
    public class OrderedMultiDictionaryStorageNetFxTests
    {
        [Fact]
        public void TheDeduplicatingIndexerReachesItsValuesThroughIReadOnlyCollection()
        {
            var map = new OrderedMultiDictionary<int, string>(allowDuplicateValues: false);
            map.Add(1, "b");
            map.Add(1, "a");
            map.Add(1, "a");

            IReadOnlyCollection<string> values = map[1];
            values.Count.ShouldBe(2);
            values.ToArray().ShouldBe(new[] { "a", "b" });
        }

        [Fact]
        public void TheDuplicatingIndexerReachesItsValuesThroughIReadOnlyCollection()
        {
            var map = new OrderedMultiDictionary<int, string>();
            map.Add(1, "a");
            map.Add(1, "a");

            IReadOnlyCollection<string> values = map[1];
            values.Count.ShouldBe(2);
            values.ToArray().ShouldBe(new[] { "a", "a" });
        }

        [Fact]
        public void BothPoliciesEnumerateTheirPairsThroughTheInterface()
        {
            IMultiDictionary<int, string> deduplicating = new OrderedMultiDictionary<int, string>(allowDuplicateValues: false);
            deduplicating.Add(2, "b");
            deduplicating.Add(1, "a");
            deduplicating.Add(1, "a");

            deduplicating.KeyCount.ShouldBe(2);
            deduplicating.TotalValueCount.ShouldBe(2);
            deduplicating.Values.ToArray().ShouldBe(new[] { "a", "b" });

            IMultiDictionary<int, string> duplicating = new OrderedMultiDictionary<int, string>();
            duplicating.Add(1, "a");
            duplicating.Add(1, "a");

            duplicating.TotalValueCount.ShouldBe(2);
            duplicating.Values.ToArray().ShouldBe(new[] { "a", "a" });
        }

        [Fact]
        public void TheFlatPairSequenceStaysOrderedOnTheLowGeneration()
        {
            var map = new OrderedMultiDictionary<int, int>();
            for (var i = 1999; i >= 0; i--)
            {
                map.Add(i, i);
                map.Add(i, -i);
            }

            var pairs = ExpandedPairs(map);
            pairs.Count.ShouldBe(4000);
            pairs[0].ShouldBe(new KeyValuePair<int, int>(0, 0));
            pairs[3999].ShouldBe(new KeyValuePair<int, int>(1999, 1999));

            var ordered = true;
            for (var i = 1; i < pairs.Count; i++)
            {
                if (pairs[i - 1].Key > pairs[i].Key
                    || (pairs[i - 1].Key == pairs[i].Key && pairs[i - 1].Value > pairs[i].Value))
                {
                    ordered = false;
                    break;
                }
            }

            ordered.ShouldBeTrue("the expanded sequence must enumerate in key order then value order");
        }

        [Fact]
        public void ThePositionalReadsWorkOnTheLowGeneration()
        {
            var map = new OrderedMultiDictionary<string, int>();
            map.AddRange("b", new[] { 9, 3 });
            map.Add("a", 7);

            map.TotalValueCount.ShouldBe(3);
            map.GetByRank(0).ShouldBe(new KeyValuePair<string, int>("a", 7));
            map.GetByRank(1).ShouldBe(new KeyValuePair<string, int>("b", 3));
            map.GetByRank(2).ShouldBe(new KeyValuePair<string, int>("b", 9));
            map.GetRank("b", 9).ShouldBe(2);
            map.GetMedian().ShouldBe(new KeyValuePair<string, int>("b", 3));
            map.GetQuantile(1).ShouldBe(new KeyValuePair<string, int>("b", 9));
        }

        [Fact]
        public void TheDictionaryViewHandsBackWrappedCollections()
        {
            var map = new OrderedMultiDictionary<int, string>(allowDuplicateValues: false);
            map.Add(1, "a");
            map.Add(2, "b");

            var view = map.AsReadOnly();
            view.Count.ShouldBe(2);

            var seen = new List<string>();
            foreach (IReadOnlyCollection<string> values in view.Values)
            {
                seen.AddRange(values);
            }

            seen.ShouldBe(new[] { "a", "b" });
        }

        [Fact]
        public void EntrySetAndLookupReachTheirValuesThroughIReadOnlyCollection()
        {
            var map = new OrderedMultiDictionary<int, string>(allowDuplicateValues: false);
            map.Add(1, "a");
            map.Add(2, "b");

            var entries = map.EntrySet().ToList();
            entries.Count.ShouldBe(2);

            IReadOnlyCollection<string> first = entries[0].Values;
            first.Count.ShouldBe(1);
            first.ToArray().ShouldBe(new[] { "a" });

            var lookup = map.AsLookup();
            lookup.Count.ShouldBe(2);
            lookup[1].ToArray().ShouldBe(new[] { "a" });
        }

        [Fact]
        public void ABucketDroppedByItsLastRemovalLeavesAReadableMap()
        {
            var map = new OrderedMultiDictionary<int, string>(allowDuplicateValues: false);
            map.Add(1, "a");
            map.Add(2, "b");

            map.Remove(1, "a").ShouldBeTrue();

            map.ContainsKey(1).ShouldBeFalse();
            map.TotalValueCount.ShouldBe(1);

            IReadOnlyCollection<string> remaining = map[2];
            remaining.Count.ShouldBe(1);
            remaining.ToArray().ShouldBe(new[] { "b" });
        }

        private static List<KeyValuePair<TKey, TValue>> ExpandedPairs<TKey, TValue>(
            OrderedMultiDictionary<TKey, TValue> map)
        {
            // The map exposes two IEnumerable<T> faces, so an extension method can not infer its
            // type argument over it; the cast names the flat key/value pair sequence.
            var pairs = new List<KeyValuePair<TKey, TValue>>();
            foreach (var pair in (IEnumerable<KeyValuePair<TKey, TValue>>)map)
            {
                pairs.Add(pair);
            }

            return pairs;
        }
    }
}
