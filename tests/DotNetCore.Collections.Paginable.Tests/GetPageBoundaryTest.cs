using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable.Tests.Models;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.Tests {
    // Phase 3 boundary coverage:
    // - out-of-range page numbers on all three entry paths (IEnumerable / IQueryable / PaginableSet)
    // - limitedMemberCount boundary behavior
    // - last (partial) page real member count for every remainder
    public class GetPageBoundaryTest {

        private static IList<Student> BuildStudents(int count)
            => Enumerable.Range(0, count)
                .Select(x => new Student { Id = x, Name = $"Student-{x}" })
                .ToList();

        #region out-of-range

        [Fact]
        public void QueryablePathOutOfRangeTest() {
            var students = BuildStudents(10);
            Should.Throw<ArgumentOutOfRangeException>(() => students.AsQueryable().GetPage(3, 5));
            Should.Throw<ArgumentOutOfRangeException>(() => students.AsQueryable().GetPage(99, 5));
        }

        [Fact]
        public void QueryablePathOutOfRangeTest_EmptySource() {
            // Empty source: any page request yields a single empty page rather than a throw
            // (current direct-path semantics: one empty page; note the set path reports 0 pages).
            var page = BuildStudents(0).AsQueryable().GetPage(1, 5);
            page.TotalPageCount.ShouldBe(1);
            page.CurrentPageSize.ShouldBe(0);
            page.ToList().ShouldBeEmpty();
        }

        [Fact]
        public void SetPathOutOfRangeTest() {
            var set = BuildStudents(10).ToPaginable(5);
            Should.Throw<ArgumentOutOfRangeException>(() => set.GetPage(0));
            Should.Throw<ArgumentOutOfRangeException>(() => set.GetPage(3));
        }

        [Fact]
        public void EnumerablePathOutOfRangeTest_EmptySource() {
            var page = BuildStudents(0).GetPage(1, 5);
            page.TotalPageCount.ShouldBe(1);
            page.CurrentPageSize.ShouldBe(0);
            page.ToList().ShouldBeEmpty();
        }

        #endregion

        #region limitedMemberCount

        [Fact]
        public void LimitedMemberCountTruncatesSourceTest() {
            // 10 items limited to 7 with pageSize 5: two pages, second page has 2 members.
            var set = BuildStudents(10).ToPaginable(5, 7);
            set.MemberCount.ShouldBe(7);
            set.PageCount.ShouldBe(2);

            var lastPage = set.GetPage(2);
            lastPage.CurrentPageSize.ShouldBe(2);
            lastPage.HasNext.ShouldBeFalse();

            var members = lastPage.ToList();
            members.Count.ShouldBe(2);
            members[0].Value.Id.ShouldBe(5);
            members[1].Value.Id.ShouldBe(6);
            members[0].ItemNumber.ShouldBe(6);
        }

        [Fact]
        public void LimitedMemberCountGreaterThanRealCountClampsTest() {
            // Limited count greater than the real count must clamp to the real count.
            var set = BuildStudents(10).ToPaginable(5, 100);
            set.MemberCount.ShouldBe(10);
            set.PageCount.ShouldBe(2);
        }

        [Fact]
        public void LimitedMemberCountEqualRealCountTest() {
            var set = BuildStudents(10).ToPaginable(5, 10);
            set.MemberCount.ShouldBe(10);
            set.PageCount.ShouldBe(2);
        }

        [Fact]
        public void LimitedMemberCountZeroYieldsEmptySetTest() {
            // limitedMemberCount == 0 is treated as "limit to zero members" (empty set),
            // matching the factory's current documented behavior.
            var set = BuildStudents(10).ToPaginable(5, 0);
            set.PageCount.ShouldBe(0);
            set.GetPage(1).ToList().ShouldBeEmpty();
        }

        [Fact]
        public void LimitedMemberCountForQueryableTest() {
            var set = BuildStudents(10).AsQueryable().ToPaginable(5, 7);
            set.MemberCount.ShouldBe(7);
            set.PageCount.ShouldBe(2);
            set.GetPage(2).ToList().Count.ShouldBe(2);
        }

        #endregion

        #region last partial page

        [Theory]
        [InlineData(10, 5, 2, 5)] // exact division: full last page
        [InlineData(11, 4, 3, 3)] // remainder 3
        [InlineData(12, 4, 3, 4)] // exact division
        [InlineData(13, 4, 4, 1)] // remainder 1
        [InlineData(14, 4, 4, 2)] // remainder 2
        public void LastPartialPageMemberCountTest(int total, int pageSize, int expectedPages, int expectedLastPageSize) {
            var students = BuildStudents(total);

            var page = students.GetPage(expectedPages, pageSize);
            page.TotalPageCount.ShouldBe(expectedPages);
            page.CurrentPageSize.ShouldBe(expectedLastPageSize);
            page.HasNext.ShouldBeFalse();
            page.HasPrevious.ShouldBe(expectedPages > 1);
            page.ToList().Count.ShouldBe(expectedLastPageSize);

            // the set path must agree with the direct path
            var set = students.ToPaginable(pageSize);
            set.PageCount.ShouldBe(expectedPages);
            set.GetPage(expectedPages).ToList().Count.ShouldBe(expectedLastPageSize);

            // and the queryable path too
            var queryPage = students.AsQueryable().GetPage(expectedPages, pageSize);
            queryPage.CurrentPageSize.ShouldBe(expectedLastPageSize);
            queryPage.ToList().Count.ShouldBe(expectedLastPageSize);
        }

        #endregion
    }
}
