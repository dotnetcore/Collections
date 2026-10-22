using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using DotNetCore.Collections.Paginable;

namespace DotNetCore.Collections.Paginable.Benchmarks
{
    /// <summary>
    /// F7-09: what <see cref="PaginableSettings.MemoizeLazySources"/> actually buys on a lazy source.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="PaginableBenchmark"/> builds a fresh paginable inside every benchmark method, so
    /// it can only ever measure one page of one paging set - and that shape hides the cost this
    /// benchmark exists to expose. A paging set over a lazy sequence walks the sequence once for the
    /// member count and then once more per page, each time from the beginning; the total pull count
    /// over P pages is <c>memberCount + sum(skip + pageSize)</c>, which grows quadratically with P.
    /// Memoizing the source collapses that to <c>memberCount</c>.
    /// </para>
    /// <para>
    /// The two benchmarks below are the two shapes that matter. <see cref="AllPages"/> is the worst
    /// case - every page of one paging set - and is where the quadratic term dominates.
    /// <see cref="DeepPage"/> is the single-request case a web endpoint would make: the count pass
    /// plus one deep page, which memoization still halves.
    /// </para>
    /// <para>
    /// Watch both columns, not just the time: the switch is a trade, and the allocation column is
    /// the other half of it. Memoizing turns walks into memory, so a lazy in-memory source trades a
    /// larger allocation for fewer pulls. On a remote or database-backed sequence - where a walk is
    /// a round trip rather than a loop iteration - that trade is overwhelmingly worth it, which is
    /// why the switch exists; on a cheap iterator it is not, which is why it defaults to off.
    /// </para>
    /// </remarks>
    [MemoryDiagnoser]
    [MaxColumn, MinColumn]
    public class MemoizeLazySourceBenchmark
    {
        private const int Total = 2_000;
        private const int PageSize = 50;

        /// <summary>
        /// The switch itself, as a benchmark parameter so that both sides of the trade land in the
        /// same table.
        /// </summary>
        [Params(false, true)]
        public bool MemoizeLazySources { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            PaginableSettingsManager.UpdateSettings(new PaginableSettings
            {
                MemoizeLazySources = MemoizeLazySources,
            });
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            // Leave the process on the shipped defaults: the other benchmark classes in this
            // assembly share the same global snapshot.
            PaginableSettingsManager.UpdateSettings(new PaginableSettings());
        }

        /// <summary>
        /// Every page of one paging set over a lazy iterator: 40 pages, each restarting at the
        /// beginning of the source unless the source is memoized.
        /// </summary>
        [Benchmark(Baseline = true)]
        public long AllPages()
        {
            var paginable = BuildIterator().ToPaginable(PageSize);

            long sum = 0;
            for (var pageNumber = 1; pageNumber <= paginable.PageCount; pageNumber++)
            {
                sum += SumOfIds(paginable.GetPage(pageNumber));
            }

            return sum;
        }

        /// <summary>
        /// One deep page of a fresh paging set: the member-count pass plus the page's own walk,
        /// which memoization turns into the count pass alone.
        /// </summary>
        [Benchmark]
        public long DeepPage()
        {
            var paginable = BuildIterator().ToPaginable(PageSize);

            return SumOfIds(paginable.GetPage(paginable.PageCount));
        }

        /// <summary>
        /// A lazy source that is deliberately not an <see cref="ICollection{T}"/>, so it takes the
        /// path the switch governs. Built fresh per invocation, because a paging set owns its cache:
        /// reusing one source across invocations would measure the cache rather than the paging.
        /// </summary>
        private static IEnumerable<Row> BuildIterator()
        {
            for (var i = 0; i < Total; i++)
            {
                yield return new Row { Id = i };
            }
        }

        private static long SumOfIds(IPage<Row> page)
        {
            long sum = 0;
            foreach (var member in page)
            {
                sum += member.Value.Id;
            }

            return sum;
        }

        private sealed class Row
        {
            public int Id { get; set; }
        }
    }
}
