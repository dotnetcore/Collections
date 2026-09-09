using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;

namespace DotNetCore.Collections.Paginable.Benchmarks {
    /// <summary>
    /// R10 benchmark rewrite.
    /// <para>
    /// The previous benchmark only measured <c>TotalPageCount</c>, which never enumerates
    /// page members and therefore completely masked the old O(skip²) ElementAt trap (R1).
    /// Every benchmark here forces REAL member materialization (summing member ids), and
    /// covers a SHALLOW page and a DEEP page (large skip) to expose skip-scaling behavior.
    /// </para>
    /// Sources compared:
    /// - <see cref="List{T}"/>: IList fast path (O(1) indexing + O(1) Count)
    /// - lazy iterator: non-ICollection IEnumerable (single Skip/Take materialization since 6.0)
    /// - LINQ chain: composed Select/Where over a list (non-ICollection)
    /// - EF Core InMemory IQueryable: exercises the provider-translated Skip/Take/Count path
    ///   (honest note: InMemory is not a real database; use the DbTests project against SQL
    ///   Server for provider-level verification)
    /// </summary>
    [MemoryDiagnoser]
    [MaxColumn, MinColumn]
    public class PaginableBenchmark {

        private const int Total = 100_000;
        private const int PageSize = 50;

        private List<IntRow> _list;
        private IEnumerable<IntRow> _iterator;
        private IEnumerable<IntRow> _linqChain;
        private IQueryable<IntRow> _efInMemoryQueryable;

        /// <summary>Shallow page: skip = 50. Deep page: skip = 49_950.</summary>
        [Params(2, 1000)]
        public int PageNumber { get; set; }

        [GlobalSetup]
        public void Setup() {
            _list = Enumerable.Range(0, Total).Select(id => new IntRow { Id = id }).ToList();

            IEnumerable<IntRow> Iterator() {
                for (var i = 0; i < Total; i++) {
                    yield return new IntRow { Id = i };
                }
            }
            _iterator = Iterator();

            // Composed LINQ-to-Objects chain over a list (non-ICollection after Select).
            _linqChain = _list
                .Where(x => x.Id >= 0)
                .Select(x => x);

            _efInMemoryQueryable = BuildEfInMemoryQueryable();
        }

        private static readonly string EfDbName = $"paginable-benchmark-{Guid.NewGuid():N}";
        private static readonly object EfSeedLock = new();

        private static IQueryable<IntRow> BuildEfInMemoryQueryable() {
            var options = new DbContextOptionsBuilder<BenchmarkDbContext>()
                .UseInMemoryDatabase(EfDbName)
                .Options;
            lock (EfSeedLock) {
                using (var context = new BenchmarkDbContext(options)) {
                    // Reset unconditionally: GlobalSetup may run more than once per process
                    // (per benchmark case), so re-seed from a clean store every time.
                    context.Database.EnsureDeleted();
                    context.Database.EnsureCreated();
                    // NOTE: ids start at 1 — an explicit key value of 0 is treated as
                    // "unset" by EF's generated-key convention and would collide with
                    // the generated value for the next row.
                    context.Rows.AddRange(Enumerable.Range(1, Total).Select(id => new IntRow { Id = id }));
                    context.SaveChanges();
                }
            }
            return new BenchmarkDbContext(options).Rows.AsNoTracking().AsQueryable();
        }

        // --- direct GetPage path: materialize the page members ---

        [Benchmark(Baseline = true)]
        public long GetPage_List()
            => SumOfIds(_list.GetPage(PageNumber, PageSize));

        [Benchmark]
        public long GetPage_Iterator()
            => SumOfIds(_iterator.GetPage(PageNumber, PageSize));

        [Benchmark]
        public long GetPage_LinqChain()
            => SumOfIds(_linqChain.GetPage(PageNumber, PageSize));

        [Benchmark]
        public long GetPage_EfInMemoryQueryable()
            => SumOfIds(_efInMemoryQueryable.GetPage(PageNumber, PageSize));

        // --- ToPaginable set path: page cache + lazy page materialization ---

        [Benchmark]
        public long ToPaginable_GetPage_List()
            => SumOfIds(_list.ToPaginable(PageSize).GetPage(PageNumber));

        [Benchmark]
        public long ToPaginable_GetPage_Iterator()
            => SumOfIds(_iterator.ToPaginable(PageSize).GetPage(PageNumber));

        private static long SumOfIds(IPage<IntRow> page) {
            long sum = 0;
            foreach (var member in page) {
                sum += member.Value.Id;
            }
            return sum;
        }

        private sealed class IntRow {
            public int Id { get; set; }
        }

        private class BenchmarkDbContext : DbContext {
            public BenchmarkDbContext(DbContextOptions<BenchmarkDbContext> options) : base(options) { }

            public DbSet<IntRow> Rows { get; set; }
        }
    }
}
