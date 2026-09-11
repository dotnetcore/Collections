using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable.Tests.Models;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.Tests {
    // Phase 4 keyset (seek) pagination coverage:
    // - forward traversal via lastKey chaining must visit every member exactly once
    // - ascending & descending ordering
    // - IQueryable path composes the same semantics through expression trees
    //   (verified against LINQ-to-Objects EnumerableQuery; provider translation is
    //   exercised by the EF Core integration, see DbTests)
    // - empty sources, empty tail pages and argument validation
    public class KeysetPaginationTest {

        private const int Total = 100;

        private static List<Student> BuildStudents(int count)
            => Enumerable.Range(1, count)
                .Select(x => new Student { Id = x, Name = $"Student-{x}" })
                .ToList();

        #region IEnumerable path

        [Fact]
        public void EnumerableForwardTraversalTest() {
            var source = BuildStudents(Total);
            var visited = new List<int>();

            var page = source.GetFirstPageByKeyset(s => s.Id, 10);
            while (true) {
                page.Members.Count.ShouldBeLessThanOrEqualTo(10);
                visited.AddRange(page.Members.Select(s => s.Id));
                if (!page.HasNext) {
                    break;
                }
                page = source.GetPageByKeyset(s => s.Id, page.LastMember.Id, 10);
            }

            visited.Count.ShouldBe(Total);
            visited.ShouldBe(Enumerable.Range(1, Total));
        }

        [Fact]
        public void EnumerableDescendingTraversalTest() {
            var source = BuildStudents(Total);
            var page = source.GetFirstPageByKeyset(s => s.Id, 10, descending: true);

            page.IsFirstPage.ShouldBeTrue();
            page.Members[0].Id.ShouldBe(100);
            page.Members[9].Id.ShouldBe(91);

            var second = source.GetPageByKeyset(s => s.Id, page.LastMember.Id, 10, descending: true);
            second.Members[0].Id.ShouldBe(90);
            second.IsFirstPage.ShouldBeFalse();
            second.HasNext.ShouldBeTrue();
        }

        [Fact]
        public void EnumerableEmptySourceTest() {
            var page = BuildStudents(0).GetFirstPageByKeyset(s => s.Id, 10);
            page.Members.ShouldBeEmpty();
            page.CurrentPageSize.ShouldBe(0);
            page.HasNext.ShouldBeFalse();
        }

        [Fact]
        public void EnumerablePartialTailPageTest() {
            // 23 items, page size 10: pages of 10 / 10 / 3.
            var source = BuildStudents(23);
            var p1 = source.GetFirstPageByKeyset(s => s.Id, 10);
            var p2 = source.GetPageByKeyset(s => s.Id, p1.LastMember.Id, 10);
            var p3 = source.GetPageByKeyset(s => s.Id, p2.LastMember.Id, 10);

            p1.Members.Count.ShouldBe(10);
            p1.HasNext.ShouldBeTrue();
            p2.Members.Count.ShouldBe(10);
            p2.HasNext.ShouldBeTrue();
            p3.Members.Count.ShouldBe(3);
            p3.HasNext.ShouldBeFalse();
            p3.Members[2].Id.ShouldBe(23);
        }

        [Fact]
        public void EnumerableKeysetBeyondLastKeyTest() {
            // Anchoring behind the last member yields an empty page, not a throw:
            // keyset pagination is direction-agnostic about the anchor position.
            var source = BuildStudents(10);
            var page = source.GetPageByKeyset(s => s.Id, 999, 10);
            page.Members.ShouldBeEmpty();
            page.HasNext.ShouldBeFalse();
        }

        #endregion

        #region IQueryable path

        [Fact]
        public void QueryableForwardTraversalTest() {
            var source = BuildStudents(Total).AsQueryable();
            var visited = new List<int>();

            var page = source.GetFirstPageByKeyset(s => s.Id, 10);
            while (true) {
                visited.AddRange(page.Members.Select(s => s.Id));
                if (!page.HasNext) {
                    break;
                }
                page = source.GetPageByKeyset(s => s.Id, page.LastMember.Id, 10);
            }

            visited.Count.ShouldBe(Total);
            visited.ShouldBe(Enumerable.Range(1, Total));
        }

        [Fact]
        public void QueryableDescendingFirstPageTest() {
            var source = BuildStudents(Total).AsQueryable();
            var page = source.GetFirstPageByKeyset(s => s.Id, 7, descending: true);
            page.Members.Count.ShouldBe(7);
            page.Members[0].Id.ShouldBe(100);
            page.Members[6].Id.ShouldBe(94);
            page.HasNext.ShouldBeTrue();
        }

        [Fact]
        public void QueryableTailPageTest() {
            var source = BuildStudents(23).AsQueryable();
            var p1 = source.GetFirstPageByKeyset(s => s.Id, 10);
            var p2 = source.GetPageByKeyset(s => s.Id, p1.LastMember.Id, 10);
            var p3 = source.GetPageByKeyset(s => s.Id, p2.LastMember.Id, 10);

            p3.Members.Count.ShouldBe(3);
            p3.HasNext.ShouldBeFalse();
        }

        #endregion

        #region validation

        [Fact]
        public void ValidationTest() {
            var source = BuildStudents(10);

            Should.Throw<ArgumentNullException>(() => ((IEnumerable<Student>)null).GetFirstPageByKeyset(s => s.Id, 10));
            Should.Throw<ArgumentNullException>(() => source.GetFirstPageByKeyset((Func<Student, int>)null, 10));
            Should.Throw<ArgumentNullException>(() => ((IQueryable<Student>)null).GetFirstPageByKeyset(s => s.Id, 10));
            Should.Throw<ArgumentNullException>(() => source.AsQueryable().GetFirstPageByKeyset((System.Linq.Expressions.Expression<Func<Student, int>>)null, 10));

            Should.Throw<ArgumentOutOfRangeException>(() => source.GetFirstPageByKeyset(s => s.Id, 0));
            Should.Throw<ArgumentOutOfRangeException>(() => source.AsQueryable().GetPageByKeyset(s => s.Id, 1, 0));
        }

        #endregion
    }
}
