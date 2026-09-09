using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable.Tests.Models;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.Tests {
    // Phase 3 source-variety coverage:
    // GetPage / ToPaginable must behave consistently across IEnumerable implementations:
    //   - IList fast path (List<T>, T[])
    //   - non-list ICollection<T> (HashSet<T>)
    //   - lazy iterator (yield return)
    //   - LINQ-to-Objects chain (Select/Where composition)
    //   - IReadOnlyList<T> wrapper (NOT ICollection<T>, so it takes the lazy path)
    public class GetPageSourceVarietyTest {

        private const int Total = 53;
        private const int PageSize = 7;

        private static IEnumerable<Student> Build(int count)
            => Enumerable.Range(0, count).Select(x => new Student { Id = x, Name = $"Student-{x}" });

        private static void AssertPageContent(IPage<Student> page, int pageNumber) {
            var expectedCount = Math.Min(PageSize, Total - (pageNumber - 1) * PageSize);
            page.TotalPageCount.ShouldBe((int)Math.Ceiling((double)Total / PageSize));
            page.TotalMemberCount.ShouldBe(Total);
            page.CurrentPageNumber.ShouldBe(pageNumber);
            page.CurrentPageSize.ShouldBe(expectedCount);

            var members = page.ToList();
            members.Count.ShouldBe(expectedCount);
            members[0].Value.Id.ShouldBe((pageNumber - 1) * PageSize);
            members[members.Count - 1].Value.Id.ShouldBe((pageNumber - 1) * PageSize + expectedCount - 1);
            members[0].ItemNumber.ShouldBe((pageNumber - 1) * PageSize + 1);
        }

        [Fact]
        public void ListSourceTest() {
            IList<Student> source = Build(Total).ToList();
            AssertPageContent(source.GetPage(5, PageSize), 5);
        }

        [Fact]
        public void ArraySourceTest() {
            var source = Build(Total).ToArray();
            AssertPageContent(source.GetPage(5, PageSize), 5);
        }

        [Fact]
        public void HashSetSourceTest() {
            // HashSet<T> is ICollection<T> but not IList<T>: takes the Skip/Take path.
            var source = new HashSet<Student>(Build(Total));
            var page = source.GetPage(5, PageSize);
            page.TotalPageCount.ShouldBe((int)Math.Ceiling((double)Total / PageSize));
            page.TotalMemberCount.ShouldBe(Total);
            page.CurrentPageSize.ShouldBe(PageSize);
            page.ToList().Count.ShouldBe(PageSize);
        }

        [Fact]
        public void IteratorSourceTest() {
            IEnumerable<Student> Iterator() {
                for (var i = 0; i < Total; i++) {
                    yield return new Student { Id = i, Name = $"Student-{i}" };
                }
            }

            AssertPageContent(Iterator().GetPage(5, PageSize), 5);
        }

        [Fact]
        public void LinqChainSourceTest() {
            // A composed LINQ-to-Objects chain must not be enumerated more than once per page.
            IEnumerable<Student> source = Build(Total * 2)
                .Where(x => x.Id % 2 == 0)
                .Select(x => new Student { Id = x.Id / 2, Name = $"Student-{x.Id / 2}" });

            AssertPageContent(source.GetPage(5, PageSize), 5);
        }

        [Fact]
        public void IReadOnlyListWrapperSourceTest() {
            // IReadOnlyList<T> is not ICollection<T>: it goes through the lazy single-enumeration path.
            IReadOnlyList<Student> source = Build(Total).ToList();
            AssertPageContent(((IEnumerable<Student>)source).GetPage(5, PageSize), 5);
        }

        [Fact]
        public void AllSourcesAgreeOnSetPathTest() {
            IEnumerable<Student> Iterator() {
                for (var i = 0; i < Total; i++) {
                    yield return new Student { Id = i, Name = $"Student-{i}" };
                }
            }

            var fromList = Build(Total).ToList().ToPaginable(PageSize).GetPage(5);
            var fromIterator = Iterator().ToPaginable(PageSize).GetPage(5);
            var fromLinqChain = Build(Total * 2).Where(x => x.Id % 2 == 0)
                .Select(x => new Student { Id = x.Id / 2, Name = $"Student-{x.Id / 2}" })
                .ToPaginable(PageSize)
                .GetPage(5);

            fromList.CurrentPageSize.ShouldBe(fromIterator.CurrentPageSize);
            fromList.TotalPageCount.ShouldBe(fromIterator.TotalPageCount);
            fromList.Select(m => m.Value.Id).ShouldBe(fromIterator.Select(m => m.Value.Id));
            fromList.Select(m => m.Value.Id).ShouldBe(fromLinqChain.Select(m => m.Value.Id));
        }
    }
}
