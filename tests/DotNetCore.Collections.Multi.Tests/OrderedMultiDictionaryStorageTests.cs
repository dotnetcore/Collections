using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// F6-36 evidence for the ordered multimap's storage layer: the positional reads over the
    /// expanded sequence, and the structural invariants the storage rests on. The invariants are
    /// asserted rather than the timings, following F6-25 / R2-02 — a rank read that is O(log n)
    /// because the tree is balanced is proven by the tree being balanced, not by a stopwatch.
    ///
    /// The expanded sequence is the contract the positional reads address: rank <c>0</c> is the
    /// smallest value of the smallest key, and rank <c>n - 1</c> the largest value of the largest
    /// key, which is exactly the order <see cref="OrderedMultiDictionary{TKey,TValue}"/> enumerates
    /// in. Several tests below therefore assert a rank read against the enumerated sequence itself,
    /// so the two can not drift apart.
    /// </summary>
    public class OrderedMultiDictionaryStorageTests
    {
        // ------------------------------------------------------------------
        // positional reads: the expanded sequence
        // ------------------------------------------------------------------

        [Fact]
        public void GetByRankWalksTheExpandedSequenceInKeyThenValueOrder()
        {
            var map = NewMap();
            map.AddRange("b", new[] { 9, 3 });
            map.Add("a", 7);
            map.AddRange("c", new[] { 5, 5 });

            var expanded = ExpandedPairs(map);
            expanded.Count.ShouldBe(5);

            for (var rank = 0; rank < expanded.Count; rank++)
            {
                map.GetByRank(rank).ShouldBe(expanded[rank]);
            }

            map.GetByRank(0).ShouldBe(new KeyValuePair<string, int>("a", 7));
            map.GetByRank(4).ShouldBe(new KeyValuePair<string, int>("c", 5));
        }

        [Fact]
        public void GetByRankRejectsRanksOutsideTheSequence()
        {
            var map = NewMap();
            map.Add("k", 1);

            Should.Throw<ArgumentOutOfRangeException>(() => map.GetByRank(-1));
            Should.Throw<ArgumentOutOfRangeException>(() => map.GetByRank(1));

            var empty = NewMap();
            Should.Throw<ArgumentOutOfRangeException>(() => empty.GetByRank(0));
        }

        [Fact]
        public void GetRankIsTheInverseOfGetByRank()
        {
            // Every value here is distinct. GetRank addresses a repeated value's first copy (pinned
            // by GetRankOfARepeatedValueAddressesItsFirstCopy), so the two are exact inverses only
            // over distinct values - with duplicates, GetByRank(r) can hand back a later copy whose
            // GetRank is the rank of the first one.
            var map = NewMap();
            map.AddRange("k", new[] { 5, 1, 2 });
            map.Add("other", 9);

            for (var rank = 0; rank < map.TotalValueCount; rank++)
            {
                var pair = map.GetByRank(rank);
                map.GetRank(pair.Key, pair.Value).ShouldBe(rank);
            }
        }

        [Fact]
        public void GetRankReportsAnAbsentValueOrKeyAsMinusOne()
        {
            var map = NewMap();
            map.Add("k", 1);

            map.GetRank("k", 99).ShouldBe(-1);
            map.GetRank("missing", 1).ShouldBe(-1);
        }

        [Fact]
        public void GetRankOfARepeatedValueAddressesItsFirstCopy()
        {
            var map = NewMap();
            map.AddRange("k", new[] { 1, 1, 2 });

            map.GetRank("k", 1).ShouldBe(0);
            map.GetRank("k", 2).ShouldBe(2);
        }

        [Fact]
        public void GetRankCountsValuesOfEarlierKeys()
        {
            var map = NewMap();
            map.AddRange("a", new[] { 1, 2 });
            map.AddRange("b", new[] { 3, 4 });

            map.GetRank("a", 1).ShouldBe(0);
            map.GetRank("b", 3).ShouldBe(2);
            map.GetRank("b", 4).ShouldBe(3);
        }

        [Fact]
        public void GetMedianReturnsTheLowerOfTwoMiddleCopies()
        {
            var even = NewMap();
            even.AddRange("k", new[] { 1, 2, 3, 4 });

            even.GetMedian().ShouldBe(new KeyValuePair<string, int>("k", 2));

            var odd = NewMap();
            odd.AddRange("k", new[] { 1, 2, 3 });

            odd.GetMedian().ShouldBe(new KeyValuePair<string, int>("k", 2));
        }

        [Fact]
        public void GetMedianAndAHalfQuantileAgree()
        {
            var map = NewMap();
            map.AddRange("a", new[] { 1, 2, 3 });
            map.Add("b", 4);

            map.GetMedian().ShouldBe(map.GetQuantile(0.5));
        }

        [Fact]
        public void GetQuantileUsesTheNearestRankDefinition()
        {
            var map = NewMap();
            // 100 copies of 1 under "a", then 2 under "b": the 99th percentile stays inside "a"
            map.AddRange("a", Enumerable.Repeat(1, 100));
            map.Add("b", 2);

            map.TotalValueCount.ShouldBe(101);
            map.GetQuantile(0.99).ShouldBe(new KeyValuePair<string, int>("a", 1));
            map.GetQuantile(1).ShouldBe(new KeyValuePair<string, int>("b", 2));
        }

        [Fact]
        public void GetQuantileClampsToTheFirstCopy()
        {
            var map = NewMap();
            map.AddRange("k", new[] { 1, 2, 3 });

            // ceil(0 * 3) - 1 = -1, which has to land on the first copy rather than throw
            map.GetQuantile(0).ShouldBe(new KeyValuePair<string, int>("k", 1));
        }

        [Fact]
        public void GetQuantileRejectsValuesOutsideZeroToOne()
        {
            var map = NewMap();
            map.Add("k", 1);

            Should.Throw<ArgumentOutOfRangeException>(() => map.GetQuantile(-0.1));
            Should.Throw<ArgumentOutOfRangeException>(() => map.GetQuantile(1.1));
            Should.Throw<ArgumentOutOfRangeException>(() => map.GetQuantile(double.NaN));
        }

        [Fact]
        public void MedianAndQuantileThrowOnAMapWithoutValues()
        {
            var map = NewMap();

            Should.Throw<InvalidOperationException>(() => map.GetMedian());
            Should.Throw<InvalidOperationException>(() => map.GetQuantile(0.5));
        }

        [Fact]
        public void PositionalReadsUseTheValueComparerForIdentity()
        {
            // values equal when |a| == |b|: the comparer decides identity, so -3 addresses the
            // copy of 3, and the value reported is the one the bucket actually stores.
            var map = new OrderedMultiDictionary<string, int>(
                null,
                Comparer<int>.Create((a, b) => Math.Abs(a).CompareTo(Math.Abs(b))));
            map.Add("k", 3);
            map.Add("k", 2);

            map.GetRank("k", -3).ShouldBe(1);
            map.GetByRank(0).ShouldBe(new KeyValuePair<string, int>("k", 2));
            map.GetByRank(1).ShouldBe(new KeyValuePair<string, int>("k", 3));

            // the rank -3 resolves to is the rank of the stored copy, so reading that rank back
            // hands over the value the bucket actually holds, not the argument that addressed it
            map.GetByRank(map.GetRank("k", -3)).Value.ShouldBe(3);
            map.TotalValueCount.ShouldBe(2);
        }

        [Fact]
        public void PositionalReadsHonourTheDeduplicatingPolicy()
        {
            var map = new OrderedMultiDictionary<string, int>(allowDuplicateValues: false);
            map.Add("k", 7);
            map.Add("k", 7);
            map.Add("k", 5);

            map.TotalValueCount.ShouldBe(2);
            map.GetByRank(0).ShouldBe(new KeyValuePair<string, int>("k", 5));
            map.GetByRank(1).ShouldBe(new KeyValuePair<string, int>("k", 7));
            map.GetRank("k", 7).ShouldBe(1);
            map.GetMedian().ShouldBe(new KeyValuePair<string, int>("k", 5));
        }

        [Fact]
        public void PositionalReadsFollowTheKeyComparer()
        {
            var map = new OrderedMultiDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            map.Add("Alpha", 2);
            map.Add("ALPHA", 1);

            map.Count.ShouldBe(1);
            map.GetByRank(0).ShouldBe(new KeyValuePair<string, int>("Alpha", 1));
            map.GetRank("alpha", 2).ShouldBe(1);
        }

        // ------------------------------------------------------------------
        // the cached value total
        // ------------------------------------------------------------------

        [Fact]
        public void TotalValueCountStaysInStepWithEveryMutationShape()
        {
            var map = NewMap();
            var expected = 0;

            map.AddRange("k", new[] { 1, 1, 2 });
            expected = 3;
            map.TotalValueCount.ShouldBe(expected);

            map.Remove("k", 1);
            expected = 2;
            map.TotalValueCount.ShouldBe(expected);

            map.UnionWith("k", new[] { 1, 2, 3 });
            expected = 3;
            map.TotalValueCount.ShouldBe(expected);

            map.IntersectionWith("k", new[] { 2 });
            expected = 1;
            map.TotalValueCount.ShouldBe(expected);

            map.SymmetricExceptWith("k", new[] { 2, 9 });
            expected = 1;
            map.TotalValueCount.ShouldBe(expected);

            map.ExceptWith("k", new[] { 9 });
            expected = 0;
            map.TotalValueCount.ShouldBe(expected);

            map.ContainsKey("k").ShouldBeFalse();
            map.KeyCount.ShouldBe(0);
        }

        [Fact]
        public void TotalValueCountMatchesTheEnumeratedSequenceUnderRandomizedChurn()
        {
            var random = new Random(20260923);
            var map = new OrderedMultiDictionary<int, int>();
            var model = new SortedDictionary<int, List<int>>();

            for (var step = 0; step < 2000; step++)
            {
                var key = random.Next(0, 10);
                var value = random.Next(0, 6);

                switch (random.Next(0, 7))
                {
                    case 0:
                    case 1:
                        map.Add(key, value);
                        if (!model.TryGetValue(key, out var list))
                        {
                            list = new List<int>();
                            model[key] = list;
                        }

                        list.Add(value);
                        break;

                    case 2:
                        if (model.TryGetValue(key, out var mlist) && mlist.Remove(value) && mlist.Count == 0)
                        {
                            model.Remove(key);
                        }

                        map.Remove(key, value);
                        break;

                    case 3:
                        map.Remove(key);
                        model.Remove(key);
                        break;

                    case 4:
                        map.Contains(key, value).ShouldBe(model.TryGetValue(key, out var cl) && cl.Contains(value));
                        break;

                    case 5:
                        // the toggling shape: a stored copy is cancelled, an absent value is added.
                        // This is the operation whose per-value and bulk reconciliations once
                        // disagreed (F6-36), so the model exercises it on every seventh step.
                        map.SymmetricExceptWith(key, new[] { value });
                        if (model.TryGetValue(key, out var toggled) && toggled.Remove(value))
                        {
                            if (toggled.Count == 0)
                            {
                                model.Remove(key);
                            }
                        }
                        else
                        {
                            if (!model.TryGetValue(key, out var added))
                            {
                                added = new List<int>();
                                model[key] = added;
                            }

                            added.Add(value);
                        }

                        break;

                    default:
                        map.ValueCount(key).ShouldBe(model.TryGetValue(key, out var vl) ? vl.Count : 0);
                        break;
                }

                map.TotalValueCount.ShouldBe(model.Sum(pair => pair.Value.Count));
                map.ValidateTree(out var error).ShouldBeTrue(error ?? "the storage must stay valid");
            }

            // the two sequences still agree element by element at the end of the churn
            var expectedPairs = model
                .SelectMany(pair => pair.Value.OrderBy(v => v), (pair, v) => new KeyValuePair<int, int>(pair.Key, v))
                .ToList();
            ExpandedPairs(map).ShouldBe(expectedPairs);

            // and every rank read agrees with the sequence it addresses
            for (var rank = 0; rank < expectedPairs.Count; rank++)
            {
                map.GetByRank(rank).ShouldBe(expectedPairs[rank]);
            }
        }

        // ------------------------------------------------------------------
        // structural invariants
        // ------------------------------------------------------------------

        [Fact]
        public void InvariantsHoldForManyKeysWithOneValueEach()
        {
            var map = NewMap();
            for (var i = 0; i < 5000; i++)
            {
                map.Add("k" + i.ToString("D5"), i);
            }

            map.Count.ShouldBe(5000);
            map.TotalValueCount.ShouldBe(5000);
            map.ValidateTree(out var error).ShouldBeTrue(error ?? "a wide tree must be valid");
            AssertHeightWithinLogBound(map, 5000);
        }

        [Fact]
        public void InvariantsHoldForOneKeyHoldingManyValues()
        {
            var map = NewMap();
            map.AddRange("k", Enumerable.Range(0, 5000));

            map.Count.ShouldBe(1);
            map.TotalValueCount.ShouldBe(5000);
            map.ValidateTree(out var error).ShouldBeTrue(error ?? "a deep bucket must be valid");
            map.GetByRank(4999).ShouldBe(new KeyValuePair<string, int>("k", 4999));
        }

        [Fact]
        public void InvariantsSurviveDrainingAndRefilling()
        {
            var map = NewMap();
            for (var i = 0; i < 2000; i++)
            {
                map.Add("k" + i.ToString("D4"), i);
            }

            for (var i = 0; i < 2000; i += 2)
            {
                map.Remove("k" + i.ToString("D4")).ShouldBeTrue();
            }

            map.Count.ShouldBe(1000);
            map.TotalValueCount.ShouldBe(1000);
            map.ValidateTree(out var error).ShouldBeTrue(error ?? "a drained tree must be valid");

            for (var i = 0; i < 2000; i += 2)
            {
                map.Add("k" + i.ToString("D4"), i);
            }

            map.Count.ShouldBe(2000);
            map.ValidateTree(out error).ShouldBeTrue(error ?? "a refilled tree must be valid");
        }

        [Fact]
        public void InvariantsHoldForTheDeduplicatingPolicyUnderChurn()
        {
            var random = new Random(20260924);
            var map = new OrderedMultiDictionary<int, int>(allowDuplicateValues: false);

            for (var step = 0; step < 3000; step++)
            {
                var key = random.Next(0, 8);
                var value = random.Next(0, 6);

                switch (random.Next(0, 4))
                {
                    case 0:
                    case 1:
                        map.Add(key, value);
                        break;

                    case 2:
                        map.Remove(key, value);
                        break;

                    default:
                        map.SymmetricExceptWith(key, new[] { value });
                        break;
                }

                map.ValidateTree(out var error).ShouldBeTrue(error ?? "the deduplicating storage must stay valid");

                // the policy is what it says: no key ever holds a value twice
                foreach (var entry in map.EntrySet())
                {
                    entry.Values.Count.ShouldBe(entry.Values.Distinct().Count());
                }
            }
        }

        [Fact]
        public void InvariantsHoldAfterClearAndReuse()
        {
            var map = NewMap();
            map.AddRange("k", new[] { 1, 2, 3 });

            map.Clear();

            map.Count.ShouldBe(0);
            map.TotalValueCount.ShouldBe(0);
            map.TreeHeight.ShouldBe(0);
            map.ValidateTree(out var error).ShouldBeTrue(error ?? "the cleared storage must be valid");

            map.Add("k", 1);
            map.ValidateTree(out error).ShouldBeTrue(error ?? "a reused storage must be valid");
        }

        [Fact]
        public void BucketAndKeyTotalsStayInStepWhenABucketEmpties()
        {
            var map = NewMap();
            map.AddRange("a", new[] { 1, 2 });
            map.Add("b", 3);

            map.Remove("a", 1);
            map.Remove("a", 2);

            // the key left with the last value, so the tree holds no key whose bucket is empty
            map.ContainsKey("a").ShouldBeFalse();
            map.Keys.ShouldBe(new[] { "b" });
            map.ValidateTree(out var error).ShouldBeTrue(error ?? "a dropped bucket must leave a valid tree");
        }

        // ------------------------------------------------------------------
        // revert guards for the storage rework itself
        // ------------------------------------------------------------------

        [Fact]
        public void TheStorageIsTheOrderStatisticTreeAndNotADictionaryOfCollections()
        {
            var fields = typeof(OrderedMultiDictionary<string, int>)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

            fields.ShouldNotContain(
                field => field.FieldType.IsGenericType
                    && field.FieldType.GetGenericTypeDefinition() == typeof(SortedDictionary<,>),
                "the storage layer must not go back to a SortedDictionary of per-key collections (F6-36)");

            fields.ShouldContain(
                field => field.FieldType.IsGenericType
                    && field.FieldType.GetGenericTypeDefinition() == typeof(OrderStatisticTree<,>),
                "the storage layer must be the order-statistic tree (F6-36)");
        }

        [Fact]
        public void BothEnumerationPathsUseStructEnumerators()
        {
            // The ≈112 bytes/key the F6-36 item removes came from two yield iterators built per key.
            // A value-type enumerator is what rules that out structurally: a revert to a yield would
            // change these return types back to an interface, which this pins.
            typeof(OrderStatisticTree<string, OrderedMultiList<int>>)
                .GetMethod("GetAscendingEnumerator", BindingFlags.Instance | BindingFlags.NonPublic)!
                .ReturnType.IsValueType.ShouldBeTrue("the tree enumerator must stay a struct");

            typeof(OrderedMultiList<int>)
                .GetMethod("GetAscendingEnumerator", BindingFlags.Instance | BindingFlags.NonPublic)!
                .ReturnType.IsValueType.ShouldBeTrue("the bucket enumerator must stay a struct");
        }

        private static void AssertHeightWithinLogBound(OrderedMultiDictionary<string, int> map, int count)
        {
            // 15 is the engine's minimum occupancy; a tree of height h holds at least 2 * 15^h
            // entries, so the height can not exceed log15(n / 2) + 1. Asserting the bound is how the
            // O(log n) claim is checked without a stopwatch.
            const double MinimumEntries = 15d;
            var bound = (int)Math.Ceiling(Math.Log(count / 2d, MinimumEntries)) + 1;
            map.TreeHeight.ShouldBeLessThanOrEqualTo(
                bound,
                "height " + map.TreeHeight + " must stay within log" + MinimumEntries + "(n/2) + 1 = " + bound);
        }

        private static OrderedMultiDictionary<string, int> NewMap()
        {
            return new OrderedMultiDictionary<string, int>();
        }

        private static List<KeyValuePair<TKey, TValue>> ExpandedPairs<TKey, TValue>(
            OrderedMultiDictionary<TKey, TValue> map)
        {
            // The map exposes two IEnumerable<T> faces - the flat key/value pair sequence, and the
            // dictionary's key-to-collection pairs - so an extension method can not infer its type
            // argument over it. The cast names the face this suite addresses: the expanded
            // key-then-value sequence the positional reads are indexed against.
            var pairs = new List<KeyValuePair<TKey, TValue>>();
            foreach (var pair in (IEnumerable<KeyValuePair<TKey, TValue>>)map)
            {
                pairs.Add(pair);
            }

            return pairs;
        }
    }
}
