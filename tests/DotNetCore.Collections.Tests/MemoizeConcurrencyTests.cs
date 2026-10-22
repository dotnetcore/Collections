using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DotNetCore.Collections;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Tests
{
    /// <summary>
    /// The concurrency contract of <see cref="MemoizeCache{T}"/>, which F7-09 reworked.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The cache is shared by every enumerator the memoized sequence hands out, so several threads
    /// can be filling and replaying it at once - Paginable does exactly that, because its page cache
    /// lets different pages be materialised in parallel over one shared source. Two properties have
    /// to hold under that load: every enumerator sees the source's own sequence, and no enumerator
    /// is handed another position's element.
    /// </para>
    /// <para>
    /// The second property is the one the rework had to repair. Filling "one more element" rather
    /// than "up to the index I need" meant that of two enumerators missing the same slot, the loser
    /// of the race was given the next element and reported it at its own position. The first test
    /// below is the direct regression for that; the other two cover the wider contract.
    /// </para>
    /// <para>
    /// Threads rather than tasks on purpose: the point is to have all workers arrive at the same
    /// instant, which a dedicated thread plus a barrier gives deterministically, and to keep the
    /// assertions off the worker threads so a failure surfaces as a normal assertion.
    /// </para>
    /// </remarks>
    public class MemoizeConcurrencyTests
    {
        [Fact]
        public void ConcurrentFirstElementRequests_AllGetTheFirstElement()
        {
            const int workers = 8;
            const int rounds = 60;

            for (var round = 0; round < rounds; round++)
            {
                IEnumerable<int> shared = Enumerable.Range(0, 64).ToValueEnumerable().Memoize();

                var observed = new int[workers];
                var barrier = new Barrier(workers);
                var threads = new Thread[workers];

                for (var w = 0; w < workers; w++)
                {
                    var slot = w;
                    threads[w] = new Thread(() =>
                    {
                        barrier.SignalAndWait();
                        using (var enumerator = shared.GetEnumerator())
                        {
                            observed[slot] = enumerator.MoveNext() ? enumerator.Current : -1;
                        }
                    });
                    threads[w].Start();
                }

                foreach (var thread in threads)
                {
                    thread.Join();
                }

                // Every worker asked for index 0 at the same instant. None of them may come back
                // with a later element.
                observed.ShouldAllBe(value => value == 0, $"round {round}");
            }
        }

        [Fact]
        public void ConcurrentEnumeratorsAtDifferentOffsets_EachSeeTheirOwnSuffix()
        {
            const int size = 500;
            const int workers = 8;
            const int stride = 37;
            const int window = 20;
            const int rounds = 20;

            for (var round = 0; round < rounds; round++)
            {
                IEnumerable<int> shared = Enumerable.Range(0, size).ToValueEnumerable().Memoize();

                var results = new List<int>[workers];
                var barrier = new Barrier(workers);
                var threads = new Thread[workers];

                for (var w = 0; w < workers; w++)
                {
                    var slot = w;
                    var offset = slot * stride;

                    threads[w] = new Thread(() =>
                    {
                        var collected = new List<int>(window);
                        var skipped = 0;

                        barrier.SignalAndWait();

                        using (var enumerator = shared.GetEnumerator())
                        {
                            while (skipped < offset && enumerator.MoveNext())
                            {
                                skipped++;
                            }

                            while (collected.Count < window && enumerator.MoveNext())
                            {
                                collected.Add(enumerator.Current);
                            }
                        }

                        results[slot] = collected;
                    });

                    threads[w].Start();
                }

                foreach (var thread in threads)
                {
                    thread.Join();
                }

                for (var w = 0; w < workers; w++)
                {
                    results[w].ShouldBe(
                        Enumerable.Range(w * stride, window).ToList(),
                        $"round {round}, worker {w}");
                }
            }
        }

        [Fact]
        public void ConcurrentFullEnumerations_AllSeeTheWholeSequence()
        {
            const int size = 400;
            const int workers = 6;
            const int rounds = 15;

            var expected = Enumerable.Range(0, size).ToList();

            for (var round = 0; round < rounds; round++)
            {
                IEnumerable<int> shared = Enumerable.Range(0, size).ToValueEnumerable().Memoize();

                var results = new List<int>[workers];
                var barrier = new Barrier(workers);
                var threads = new Thread[workers];

                for (var w = 0; w < workers; w++)
                {
                    var slot = w;
                    threads[w] = new Thread(() =>
                    {
                        barrier.SignalAndWait();
                        results[slot] = shared.ToList();
                    });
                    threads[w].Start();
                }

                foreach (var thread in threads)
                {
                    thread.Join();
                }

                for (var w = 0; w < workers; w++)
                {
                    results[w].ShouldBe(expected, $"round {round}, worker {w}");
                }
            }
        }

        [Fact]
        public void AnEnumeratorPastTheEnd_SeesNoMoreElements()
        {
            IEnumerable<int> shared = new[] { 1, 2, 3 }.ToValueEnumerable().Memoize();

            using (var enumerator = shared.GetEnumerator())
            {
                for (var i = 0; i < 3; i++)
                {
                    enumerator.MoveNext().ShouldBeTrue();
                }

                enumerator.MoveNext().ShouldBeFalse();
                enumerator.MoveNext().ShouldBeFalse();
            }

            // A fresh enumerator still replays the fully materialised cache.
            shared.ShouldBe(new[] { 1, 2, 3 });
        }
    }
}
