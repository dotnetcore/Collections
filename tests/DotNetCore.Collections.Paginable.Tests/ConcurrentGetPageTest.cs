using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable.Tests.Models;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.Tests {
    // Phase 3 concurrency coverage:
    // The pined-page cache inside PaginableSetBase is a ConcurrentDictionary since 6.0,
    // so parallel GetPage calls (typical for API list endpoints serving different pages)
    // must neither corrupt the cache nor produce wrong page contents.
    public class ConcurrentGetPageTest {

        private const int Total = 200;
        private const int PageSize = 4;
        private const int PageCount = Total / PageSize;

        private static PaginableEnumerable<Student> BuildSet() {
            IEnumerable<Student> Iterator() {
                for (var i = 0; i < Total; i++) {
                    yield return new Student { Id = i, Name = $"Student-{i}" };
                }
            }
            return Iterator().ToPaginable(PageSize);
        }

        [Fact]
        public async Task ParallelGetPageProducesCorrectContentTest() {
            var set = BuildSet();
            var results = new System.Collections.Concurrent.ConcurrentDictionary<int, int[]>();

            await Task.WhenAll(Enumerable.Range(1, PageCount).Select(pageNumber => Task.Run(() => {
                // repeat access to stress the cache: first call populates, second reads back
                for (var attempt = 0; attempt < 3; attempt++) {
                    var ids = set.GetPage(pageNumber).Select(m => m.Value.Id).ToArray();
                    results.AddOrUpdate(pageNumber, _ => ids, (_, __) => ids);
                }
            })));

            results.Count.ShouldBe(PageCount);
            foreach (var pair in results) {
                var pageNumber = pair.Key;
                var ids = pair.Value;
                var expected = Enumerable.Range((pageNumber - 1) * PageSize, PageSize).ToArray();
                ids.ShouldBe(expected);
            }
        }

        [Fact]
        public void ParallelEnumerationOfSetTest() {
            // Iterating the whole set concurrently across different enumerators
            // must not throw nor mix page contents (each enumerator is independent).
            var set = BuildSet();

            Should.NotThrow(() => Parallel.For(0, 4, _ => {
                foreach (var page in set) {
                    var ids = page.Select(m => m.Value.Id).ToArray();
                    ids.Length.ShouldBeLessThanOrEqualTo(PageSize);
                }
            }));
        }
    }
}
