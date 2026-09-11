using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// M6-09: the concurrent variants. The stress tests run writers on a shared key domain while
    /// readers take snapshots and whole-map reads, then compare the settled state against a
    /// serially computed expectation - every thread's contribution is exactly predictable because
    /// each thread only removes occurrences it added itself.
    /// </summary>
    public class ConcurrentVariantsStressTests
    {
        private const int Writers = 8;
        private const int AddsPerWriter = 600;
        private const int SharedKeys = 12;

        [Fact]
        public void ConcurrentMultiDictionary_SharedKeyDomain_ConcurrentReadWriteDoesNotCorrupt()
        {
            var map = new ConcurrentMultiDictionary<int, int>();
            var stop = new ManualResetEventSlim(false);
            var readerErrors = new List<Exception>();

            // Readers hammer the whole-map paths and the snapshot path while writes are in flight.
            // Only self-consistency can be asserted mid-flight: a snapshot must agree with itself.
            var readers = Enumerable.Range(0, 3).Select(_ => Task.Run(() =>
            {
                while (!stop.IsSet)
                {
                    var snapshot = map.Snapshot();
                    snapshot.TotalValueCount.ShouldBe(snapshot.Values.Count());
                    snapshot.Count.ShouldBe(snapshot.Keys.Distinct().Count());

                    var total = 0;
                    foreach (var key in snapshot.Keys)
                    {
                        total += snapshot.ValueCount(key);
                    }

                    total.ShouldBe(snapshot.TotalValueCount);

                    // No exception and no torn state is the contract here; the answer itself
                    // depends on the racing writes.
                    map.ContainsValue(-1);
                }
            })).ToArray();

            var readerWatch = Task.Run(() =>
            {
                try
                {
                    Task.WaitAll(readers);
                }
                catch (AggregateException ex)
                {
                    lock (readerErrors)
                    {
                        readerErrors.AddRange(ex.InnerExceptions);
                    }
                }
            });

            // Writers add their own value marker to random shared keys, then remove half of what
            // they added - only their own occurrences, so the settled totals are exactly computable
            // (the per-key distribution is not, and must not be asserted).
            var writers = Enumerable.Range(0, Writers).Select(w => Task.Run(() =>
            {
                var random = new Random(20260911 + w);
                var marker = w + 1;
                var added = new List<int>();
                for (var i = 0; i < AddsPerWriter; i++)
                {
                    var key = random.Next(SharedKeys);
                    map.Add(key, marker);
                    added.Add(key);
                }

                for (var i = 0; i < added.Count / 2; i++)
                {
                    map.Remove(added[i], marker).ShouldBeTrue();
                }
            })).ToArray();

            Task.WaitAll(writers);
            stop.Set();
            readerWatch.Wait();

            readerErrors.ShouldBeEmpty();

            // Settled state: every thread net-added AddsPerWriter/2 copies, and no copy outside
            // the writers' markers exists.
            var snapshot2 = map.Snapshot();
            snapshot2.TotalValueCount.ShouldBe(AddsPerWriter * Writers / 2);

            foreach (var pair in snapshot2)
            {
                pair.Key.ShouldBeInRange(0, SharedKeys - 1);
                pair.Value.ShouldBeInRange(1, Writers);
            }

            map.Count.ShouldBeLessThanOrEqualTo(SharedKeys);
            map.Count.ShouldBe(snapshot2.Count);
            map.TotalValueCount.ShouldBe(snapshot2.TotalValueCount);

            for (var k = 0; k < SharedKeys; k++)
            {
                map.ValueCount(k).ShouldBe(snapshot2.ValueCount(k));
            }
        }

        [Fact]
        public void ConcurrentMultiDictionary_DistinctKeyDomains_AreExactlyPredictable()
        {
            var map = new ConcurrentMultiDictionary<int, string>();

            // One writer per shard-agnostic domain: each thread owns keys [w*10, w*10+10) and adds
            // a predictable multiset. With disjoint keys the final state is exact, not statistical.
            var writers = Enumerable.Range(0, Writers).Select(w => Task.Run(() =>
            {
                for (var i = 0; i < 40; i++)
                {
                    map.Add(w * 10 + i % 10, "w" + w);
                }
            })).ToArray();

            Task.WaitAll(writers);

            var snapshot = map.Snapshot();
            map.Count.ShouldBe(Writers * 10);
            snapshot.Count.ShouldBe(Writers * 10);

            for (var w = 0; w < Writers; w++)
            {
                for (var k = 0; k < 10; k++)
                {
                    var key = w * 10 + k;
                    map.ValueCount(key).ShouldBe(4);
                    snapshot.ValueCount(key).ShouldBe(4);
                    map[key].ShouldBe(new[] { "w" + w, "w" + w, "w" + w, "w" + w });
                    map.ContainsKey(key).ShouldBeTrue();
                    map.Contains(key, "w" + w).ShouldBeTrue();
                }
            }

            map.TotalValueCount.ShouldBe(Writers * 10 * 4);
            map.ContainsValue("w0").ShouldBeTrue();
            map.ContainsValue("nobody").ShouldBeFalse();

            // A whole-domain removal per thread, in parallel.
            Task.WaitAll(Enumerable.Range(0, Writers).Select(w => Task.Run(() =>
            {
                for (var k = 0; k < 10; k++)
                {
                    map.Remove(w * 10 + k).ShouldBeTrue();
                }
            })).ToArray());

            map.Count.ShouldBe(0);
            map.TotalValueCount.ShouldBe(0);
        }

        [Fact]
        public void ConcurrentMultiList_ConcurrentAddRemoveKeepsCountsExact()
        {
            var bag = new ConcurrentMultiList<int>();
            const int markerBase = 100;

            var writers = Enumerable.Range(0, Writers).Select(w => Task.Run(() =>
            {
                var marker = markerBase + w;
                for (var i = 0; i < AddsPerWriter; i++)
                {
                    bag.Add(marker);
                }

                for (var i = 0; i < AddsPerWriter / 2; i++)
                {
                    // MultiList.Remove(item, times) returns the count *remaining* afterwards;
                    // the exact value races with nothing here (each marker is private to its
                    // thread) but need not be asserted - the settled totals below are the check.
                    bag.Remove(marker, 1);
                }
            })).ToArray();

            Task.WaitAll(writers);

            bag.TotalCount.ShouldBe(Writers * AddsPerWriter / 2);
            bag.DistinctCount.ShouldBe(Writers);
            for (var w = 0; w < Writers; w++)
            {
                bag.CountOf(markerBase + w).ShouldBe(AddsPerWriter / 2);
            }

            var snapshot = bag.Snapshot();
            snapshot.TotalCount.ShouldBe(bag.TotalCount);

            bag.RemoveAllCopies(markerBase);
            bag.CountOf(markerBase).ShouldBe(0);
        }
    }
}
