using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// M6-07: the two caches <c>MultiDictionary&lt;TKey,TValue&gt;</c> now keeps - the cached
    /// <c>TotalValueCount</c> (L-06) and the backwards value index behind <c>ContainsValue</c>
    /// (L-05).
    /// </summary>
    /// <remarks>
    /// A cache is only worth having if it can not be wrong, so the bulk of this suite is exactness:
    /// every mutation the type exposes is exercised and then compared against a plain model, and a
    /// 3,000-step randomized run re-checks the caches after every single step against a naive
    /// recomputation. The cost of the optimisation is measured deterministically too, with a value
    /// type that counts its own equality and hash operations: complexity is proved by counting, not
    /// by timing, because timings are noise on CI.
    /// </remarks>
    public class MultiDictionaryCacheTests
    {
        // ------------------------------------------------------------------
        // TotalValueCount: exactness after every mutation
        // ------------------------------------------------------------------

        [Fact]
        public void TotalValueCount_StartsAtZero()
        {
            new MultiDictionary<string, int>().TotalValueCount.ShouldBe(0);
        }

        [Fact]
        public void TotalValueCount_TracksAddAndAddRange()
        {
            var map = new MultiDictionary<string, int>();

            map.Add("a", 1);
            map.TotalValueCount.ShouldBe(1);

            map.Add("a", 2);
            map.TotalValueCount.ShouldBe(2);

            map.AddRange("b", new[] { 3, 4, 5 });
            map.TotalValueCount.ShouldBe(5);
        }

        [Fact]
        public void TotalValueCount_IgnoresADuplicateTheInnerCollectionSuppresses()
        {
            var map = new MultiDictionary<string, int>(allowDuplicateValues: false);

            map.Add("a", 1);
            map.Add("a", 1);
            map.TotalValueCount.ShouldBe(1);
            map.ValueCount("a").ShouldBe(1);
        }

        [Fact]
        public void TotalValueCount_TracksRemoveKey()
        {
            var map = new MultiDictionary<string, int>();
            map.AddRange("a", new[] { 1, 2, 3 });
            map.Add("b", 4);

            map.Remove("a").ShouldBeTrue();

            map.TotalValueCount.ShouldBe(1);
            map.Remove("a").ShouldBeFalse();
            map.TotalValueCount.ShouldBe(1);
        }

        [Fact]
        public void TotalValueCount_TracksRemovePair()
        {
            var map = new MultiDictionary<string, int>();
            map.AddRange("a", new[] { 1, 1, 2 });

            map.Remove("a", 1).ShouldBeTrue();
            map.TotalValueCount.ShouldBe(2);

            map.Remove("a", 1).ShouldBeTrue();
            map.TotalValueCount.ShouldBe(1);

            // No occurrence left: the call reports false and the cache does not move.
            map.Remove("a", 1).ShouldBeFalse();
            map.TotalValueCount.ShouldBe(1);
        }

        [Fact]
        public void TotalValueCount_TracksRemoveRange()
        {
            var map = new MultiDictionary<string, int>();
            map.AddRange("a", new[] { 1, 1, 2, 3 });

            map.RemoveRange("a", new[] { 1, 3 }).ShouldBeTrue();

            // One occurrence per *distinct* argument value: the second 1 survives, so the cache
            // must drop by two (one 1, one 3) and not by the whole value.
            map.TotalValueCount.ShouldBe(2);
            map["a"].ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public void TotalValueCount_TracksUnionWith()
        {
            var map = new MultiDictionary<string, int>();
            map.AddRange("a", new[] { 1, 2 });

            map.UnionWith("a", new[] { 2, 3, 3 });

            // 2 is already there (set union adds it once, and it is there once), 3 is added.
            map.TotalValueCount.ShouldBe(3);
        }

        [Fact]
        public void TotalValueCount_TracksIntersectionWith()
        {
            var map = new MultiDictionary<string, int>();
            map.AddRange("a", new[] { 1, 2, 3 });

            map.IntersectionWith("a", new[] { 1, 3 });

            map.TotalValueCount.ShouldBe(2);

            map.IntersectionWith("a", new int[0]);
            map.TotalValueCount.ShouldBe(0);
            map.ContainsKey("a").ShouldBeFalse();
        }

        [Fact]
        public void TotalValueCount_TracksExceptWith()
        {
            var map = new MultiDictionary<string, int>();
            map.AddRange("a", new[] { 1, 1, 2 });

            map.ExceptWith("a", new[] { 1 });

            map.TotalValueCount.ShouldBe(1);
            map["a"].ShouldBe(new[] { 2 });
        }

        [Fact]
        public void TotalValueCount_TracksSymmetricExceptWith()
        {
            var map = new MultiDictionary<string, int>();
            map.AddRange("a", new[] { 1, 1, 2 });

            map.SymmetricExceptWith("a", new[] { 1, 3 });

            // 1 loses one of its two copies, 2 is untouched, 3 is added.
            map.TotalValueCount.ShouldBe(3);
        }

        [Fact]
        public void TotalValueCount_IsResetByClear()
        {
            var map = new MultiDictionary<string, int>();
            map.AddRange("a", new[] { 1, 2 });
            map.Add("b", 3);

            map.Clear();

            map.TotalValueCount.ShouldBe(0);
            map.Count.ShouldBe(0);
        }

        [Fact]
        public void TotalValueCount_IsCarriedByClone()
        {
            var map = new MultiDictionary<string, int>();
            map.AddRange("a", new[] { 1, 2 });
            map.Add("b", 3);

            var clone = map.Clone();

            clone.TotalValueCount.ShouldBe(3);
            clone.ContainsValue(2).ShouldBeTrue();

            clone.Add("c", 4);
            clone.TotalValueCount.ShouldBe(4);
            map.TotalValueCount.ShouldBe(3);
        }

        [Fact]
        public void TotalValueCount_CountsWhatANonEmptyCustomFactoryBringsIn()
        {
            // A custom factory is allowed to hand back a non-empty collection, and everything it
            // brings in becomes visible with the key - so it has to be counted and indexed too.
            var map = new MultiDictionary<string, int>(null, () => new List<int> { 99 });

            map.Add("a", 1);

            map.TotalValueCount.ShouldBe(2);
            map["a"].ShouldBe(new[] { 99, 1 });
            map.ContainsValue(99).ShouldBeTrue();
        }

        [Fact]
        public void TotalValueCount_AlwaysEqualsTheEnumeratedValues()
        {
            var map = new MultiDictionary<int, int>(allowDuplicateValues: false);
            for (var i = 0; i < 50; i++)
            {
                map.AddRange(i % 7, Enumerable.Range(0, 10));
            }

            map.TotalValueCount.ShouldBe(map.Values.Count());
            map.TotalValueCount.ShouldBe(Enumerable.Range(0, 7).Sum(k => map.ValueCount(k)));
        }

        // ------------------------------------------------------------------
        // ContainsValue: exactness
        // ------------------------------------------------------------------

        [Fact]
        public void ContainsValue_HitAndMiss()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("a", 1);

            map.ContainsValue(1).ShouldBeTrue();
            map.ContainsValue(2).ShouldBeFalse();
            new MultiDictionary<string, int>().ContainsValue(1).ShouldBeFalse();
        }

        [Fact]
        public void ContainsValue_FindsAValueStoredUnderAnyKey()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("a", 1);
            map.Add("b", 2);

            map.ContainsValue(1).ShouldBeTrue();
            map.ContainsValue(2).ShouldBeTrue();
        }

        [Fact]
        public void ContainsValue_HandlesNullValues()
        {
            var map = new MultiDictionary<string, string>();

            map.ContainsValue(null).ShouldBeFalse();

            map.Add("a", null);

            map.ContainsValue(null).ShouldBeTrue();
            map.ContainsValue("v").ShouldBeFalse();
        }

        [Fact]
        public void ContainsValue_StaysTrueWhileAnotherKeyStillHoldsTheValue()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("a", 1);
            map.Add("b", 1);

            map.Remove("a", 1).ShouldBeTrue();
            map.ContainsValue(1).ShouldBeTrue();

            map.Remove("b", 1).ShouldBeTrue();
            map.ContainsValue(1).ShouldBeFalse();
        }

        [Fact]
        public void ContainsValue_GoesFalseWhenTheOwningKeyIsRemoved()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("a", 1);

            map.Remove("a").ShouldBeTrue();

            map.ContainsValue(1).ShouldBeFalse();
        }

        [Fact]
        public void ContainsValue_GoesFalseAfterAnOperationThatEmptiesTheKey()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("a", 1);

            map.ExceptWith("a", new[] { 1 });

            map.ContainsValue(1).ShouldBeFalse();
            map.ContainsKey("a").ShouldBeFalse();
        }

        [Fact]
        public void ContainsValue_GoesFalseAfterClear()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("a", 1);
            map.Add("b", 2);

            map.Clear();

            map.ContainsValue(1).ShouldBeFalse();
            map.ContainsValue(2).ShouldBeFalse();
        }

        [Fact]
        public void ContainsValue_SurvivesAReAdd()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("a", 1);
            map.Remove("a", 1);
            map.ContainsValue(1).ShouldBeFalse();

            map.Add("a", 1);

            map.ContainsValue(1).ShouldBeTrue();
        }

        [Fact]
        public void ContainsValue_WithADeduplicatingInnerCollection()
        {
            var map = new MultiDictionary<string, int>(allowDuplicateValues: false);
            map.Add("a", 1);

            map.ContainsValue(1).ShouldBeTrue();

            map.Remove("a", 1);

            map.ContainsValue(1).ShouldBeFalse();
        }

        [Fact]
        public void ContainsValue_HandlesNullValuesWithSeveralKeys()
        {
            var map = new MultiDictionary<string, string>();
            map.Add("a", null);
            map.Add("b", null);

            map.Remove("a", null).ShouldBeTrue();
            map.ContainsValue(null).ShouldBeTrue();

            map.Remove("b").ShouldBeTrue();
            map.ContainsValue(null).ShouldBeFalse();
        }

        [Fact]
        public void ContainsValue_UsesDefaultValueEquality()
        {
            var a = new List<int> { 1 };
            var b = new List<int> { 1 };
            var map = new MultiDictionary<string, List<int>>();
            map.Add("k", a);

            // Reference type using the default comparer: structurally equal but distinct instances
            // are different values, exactly as List<T>.Contains would have judged them.
            map.ContainsValue(a).ShouldBeTrue();
            map.ContainsValue(b).ShouldBeFalse();
        }

        [Fact]
        public void ContainsValue_UsesTheKeyComparerWhenIndexingKeys()
        {
            var map = new MultiDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            map.Add("A", 1);

            map.ContainsValue(1).ShouldBeTrue();

            // "a" and "A" are the same key under the injected comparer, so removing by either
            // spelling has to clear the index entry.
            map.Remove("a", 1).ShouldBeTrue();

            map.ContainsValue(1).ShouldBeFalse();
        }

        // ------------------------------------------------------------------
        // Quantified evidence: constant cost, measured by counting
        // ------------------------------------------------------------------

        [Theory]
        [InlineData(64)]
        [InlineData(4096)]
        public void ContainsValue_CostDoesNotGrowWithTheNumberOfValues(int size)
        {
            var map = new MultiDictionary<string, CountingValue>();
            var values = new List<CountingValue>(size);
            for (var i = 0; i < size; i++)
            {
                var value = new CountingValue(i);
                values.Add(value);
                map.Add("k", value);
            }

            CountingValue.Reset();
            map.ContainsValue(values[size / 2]).ShouldBeTrue();
            var hitComparisons = CountingValue.Comparisons;

            CountingValue.Reset();
            map.ContainsValue(new CountingValue(size + 1)).ShouldBeFalse();
            var missComparisons = CountingValue.Comparisons;

            // The pre-M6-07 implementation walked every inner collection, so these numbers used to
            // be O(size): ~size/2 for the hit, ~size for the miss. The index answers in a constant
            // few, and the bound below is the same at 64 values and at 4096 - which is the whole
            // point of the index, expressed without a stopwatch.
            hitComparisons.ShouldBeLessThan(16);
            missComparisons.ShouldBeLessThan(16);
        }

        [Fact]
        public void Add_CostOfMaintainingTheIndexIsConstant()
        {
            var map = new MultiDictionary<string, CountingValue>();
            map.Add("k", new CountingValue(0));

            for (var i = 1; i < 4096; i++)
            {
                map.Add("k", new CountingValue(i));
            }

            // Measured increment for one further add to an existing key: a hash and an equality
            // check against the value index, in addition to appending to the inner list. Constant,
            // not proportional to the 4096 values already stored.
            CountingValue.Reset();
            map.Add("k", new CountingValue(5000));

            CountingValue.Comparisons.ShouldBeLessThan(16);
            CountingValue.Hashes.ShouldBeLessThan(16);
        }

        [Fact]
        public void ReadPaths_DoNotAllocate()
        {
            var map = new MultiDictionary<string, int>();
            for (var i = 0; i < 1000; i++)
            {
                map.Add("k" + (i % 10), i);
            }

            // Warm up so the measurement is not charged for first-call JIT work.
            map.ContainsValue(999).ShouldBeTrue();
            var total = map.TotalValueCount;
            total.ShouldBe(1000);

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 20_000; i++)
            {
                map.ContainsValue(i % 1000);
                _ = map.TotalValueCount;
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            allocated.ShouldBeLessThan(1024);
        }

        // ------------------------------------------------------------------
        // Differential test against a plain model
        // ------------------------------------------------------------------

        [Fact]
        public void RandomOperations_KeepBothCachesEqualToANaiveRecomputation()
        {
            var map = new MultiDictionary<int, int>();
            var model = new Dictionary<int, List<int>>();
            var random = new Random(20260911);
            var recentSteps = new Queue<string>();

            for (var step = 0; step < 3000; step++)
            {
                var key = random.Next(4);
                var candidates = new[] { 1, 2, 3 };
                var argument = Enumerable.Range(0, random.Next(3))
                    .Select(_ => candidates[random.Next(candidates.Length)])
                    .ToList();
                var op = random.Next(9);

                switch (op)
                {
                    case 0:
                        var added = candidates[random.Next(candidates.Length)];
                        map.Add(key, added);
                        Bucket(model, key).Add(added);
                        break;

                    case 1:
                        map.AddRange(key, argument);
                        Bucket(model, key).AddRange(argument);
                        break;

                    case 2:
                        var expectedRemoved = model.Remove(key);
                        map.Remove(key).ShouldBe(expectedRemoved);
                        break;

                    case 3:
                        var pair = candidates[random.Next(candidates.Length)];
                        map.Remove(key, pair).ShouldBe(Bucket(model, key).Remove(pair));
                        break;

                    case 4:
                        // The return value is computed from the model *before* anything moves, so
                        // the map is called exactly once per step - calling it again inside the
                        // assertion would silently mutate the map a second time and desynchronise
                        // it from the model.
                        var rangeValues = new HashSet<int>(argument);
                        var rangeHits = Bucket(model, key).Any(value => rangeValues.Contains(value));
                        map.RemoveRange(key, argument).ShouldBe(rangeHits);

                        // Same contract as the single-value Remove: one occurrence per distinct
                        // argument value (List.Remove takes the first matching one).
                        foreach (var value in argument.Distinct())
                        {
                            Bucket(model, key).Remove(value);
                        }

                        break;

                    case 5:
                        var union = new HashSet<int>(argument);
                        map.UnionWith(key, argument);
                        foreach (var value in union)
                        {
                            if (!Bucket(model, key).Contains(value))
                            {
                                Bucket(model, key).Add(value);
                            }
                        }

                        break;

                    case 6:
                        var intersection = new HashSet<int>(argument);
                        map.IntersectionWith(key, argument);
                        Bucket(model, key).RemoveAll(value => !intersection.Contains(value));
                        break;

                    case 7:
                        var difference = new HashSet<int>(argument);
                        map.ExceptWith(key, argument);
                        Bucket(model, key).RemoveAll(value => difference.Contains(value));
                        break;

                    case 8:
                        var toggle = new HashSet<int>(argument);
                        map.SymmetricExceptWith(key, argument);
                        foreach (var value in toggle)
                        {
                            if (!Bucket(model, key).Remove(value))
                            {
                                Bucket(model, key).Add(value);
                            }
                        }

                        break;
                }

                DropEmptyKeys(model);

                // Deliberately carried into every assertion below: a 3,000-step differential is
                // only debuggable if a failure says *which* step, operation, key and argument
                // diverged, together with the state on both sides and the few steps that led to
                // it. The window is kept short so the message stays readable.
                var description = $"step {step}, op {op}, key {key}, argument [{string.Join(",", argument)}], "
                                  + $"model {{{Describe(model)}}}, map {{{map}}}, "
                                  + $"previous steps: {string.Join(" | ", recentSteps)}";

                if (recentSteps.Count == 6)
                {
                    recentSteps.Dequeue();
                }

                recentSteps.Enqueue($"#{step} op{op} k{key} arg[{string.Join(",", argument)}] "
                                    + $"map{{{map}}} model{{{Describe(model)}}}");

                map.Count.ShouldBe(model.Count, description);
                map.TotalValueCount.ShouldBe(model.Values.Sum(bucket => bucket.Count), description);

                foreach (var probe in new[] { 1, 2, 3, 4 })
                {
                    map.ContainsValue(probe).ShouldBe(
                        model.Values.Any(bucket => bucket.Contains(probe)), description);
                }

                foreach (var probeKey in new[] { 0, 1, 2, 3, 4 })
                {
                    // Read-only lookup: the probe must not grow the model with empty buckets.
                    var expectedCount = model.TryGetValue(probeKey, out var bucket) ? bucket.Count : 0;
                    map.ValueCount(probeKey).ShouldBe(expectedCount, description);
                }
            }
        }

        private static List<int> Bucket(Dictionary<int, List<int>> model, int key)
        {
            if (!model.TryGetValue(key, out var bucket))
            {
                bucket = new List<int>();
                model.Add(key, bucket);
            }

            return bucket;
        }

        private static void DropEmptyKeys(Dictionary<int, List<int>> model)
        {
            foreach (var key in model.Where(pair => pair.Value.Count == 0).Select(pair => pair.Key).ToList())
            {
                model.Remove(key);
            }
        }

        private static string Describe(Dictionary<int, List<int>> model)
        {
            return string.Join(" ", model.OrderBy(pair => pair.Key)
                .Select(pair => $"{pair.Key}:[{string.Join(",", pair.Value)}]"));
        }

        /// <summary>
        /// A value type that counts the equality and hash operations it is asked to perform, turning
        /// "this lookup is O(1)" into an assertion instead of a stopwatch reading.
        /// </summary>
        private sealed class CountingValue : IEquatable<CountingValue>
        {
            private readonly int _id;

            public CountingValue(int id)
            {
                _id = id;
            }

            public static int Comparisons { get; private set; }

            public static int Hashes { get; private set; }

            public static void Reset()
            {
                Comparisons = 0;
                Hashes = 0;
            }

            public bool Equals(CountingValue other)
            {
                Comparisons++;
                return other != null && other._id == _id;
            }

            public override bool Equals(object obj)
            {
                // Deliberately not counted again: EqualityComparer<T>.Default routes through
                // IEquatable<T>.Equals, and counting the object overload too would double every
                // comparison.
                return Equals(obj as CountingValue);
            }

            public override int GetHashCode()
            {
                Hashes++;
                return _id;
            }
        }
    }
}
