using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable.Tests.Models;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.Tests {
    // Phase 1 regression tests:
    // - R1: lazy (non-IList) IEnumerable sources are materialized via a single Skip/Take
    // - R4: direct GetPage path extracts page & total count in one enumeration
    // - CurrentPageSize fix: last page of an evenly-divisible source must not be empty
    public class GetPageRegressionTest {

        private IList<Student> TenItemsForStudents { get; set; }
        private IList<Student> EightItemsForStudents { get; set; }

        public GetPageRegressionTest() {
            TenItemsForStudents = Enumerable.Range(0, 10)
                .Select(x => new Student { Id = x, Name = $"Student-{x}" })
                .ToList();
            EightItemsForStudents = Enumerable.Range(0, 8)
                .Select(x => new Student { Id = x, Name = $"Student-{x}" })
                .ToList();
        }

        [Fact]
        public void EvenlyDivisibleLastPageTest() {
            // 10 items, page size 5: the last page (page 2) must contain 5 members.
            // Previously CurrentPageSize was computed as t % skip = 10 % 5 = 0,
            // which wrongly produced an empty last page.
            var page = TenItemsForStudents.GetPage(2, 5);
            page.TotalPageCount.ShouldBe(2);
            page.TotalMemberCount.ShouldBe(10);
            page.CurrentPageSize.ShouldBe(5);
            page.HasNext.ShouldBeFalse();
            page.HasPrevious.ShouldBeTrue();

            var members = page.ToList();
            members.Count.ShouldBe(5);
            members[0].Value.Id.ShouldBe(5);
            members[4].Value.Id.ShouldBe(9);
            members[0].ItemNumber.ShouldBe(6);
            members[4].ItemNumber.ShouldBe(10);
        }

        [Fact]
        public void EvenlyDivisibleLastPageTest_ViaSet() {
            var set = EightItemsForStudents.ToPaginable(4);
            var lastPage = set.GetPage(2);
            lastPage.CurrentPageSize.ShouldBe(4);
            lastPage.ToList().Count.ShouldBe(4);
            lastPage.ToList()[0].Value.Id.ShouldBe(4);
            lastPage.ToList()[3].Value.Id.ShouldBe(7);
        }

        [Fact]
        public void EvenlyDivisibleLastPageTest_ForQuery() {
            var page = TenItemsForStudents.AsQueryable().GetPage(2, 5);
            page.TotalPageCount.ShouldBe(2);
            page.CurrentPageSize.ShouldBe(5);

            var members = page.ToList();
            members.Count.ShouldBe(5);
            members[0].Value.Id.ShouldBe(5);
            members[4].Value.Id.ShouldBe(9);
        }

        [Fact]
        public void LazySourceDeepPageTest() {
            // Non-ICollection (lazy) source: deep page must be materialized correctly
            // with a single Skip/Take enumeration (R1) instead of ElementAt per member.
            IEnumerable<Student> lazySource = Enumerable.Range(0, 100)
                .Select(x => new Student { Id = x, Name = $"Student-{x}" });

            var page = lazySource.GetPage(9, 5);
            page.TotalPageCount.ShouldBe(20);
            page.TotalMemberCount.ShouldBe(100);
            page.CurrentPageNumber.ShouldBe(9);
            page.CurrentPageSize.ShouldBe(5);
            page.HasNext.ShouldBeTrue();
            page.HasPrevious.ShouldBeTrue();

            var members = page.ToList();
            members.Count.ShouldBe(5);
            members[0].Value.Id.ShouldBe(40);
            members[4].Value.Id.ShouldBe(44);
            members[0].ItemNumber.ShouldBe(41);
            members[4].ItemNumber.ShouldBe(45);
        }

        [Fact]
        public void PageOutOfRangeTest() {
            // Out-of-range pages now throw eagerly (previously the IEnumerable path
            // crashed lazily during enumeration with ArgumentOutOfRangeException).
            Should.Throw<IndexOutOfRangeException>(() => TenItemsForStudents.GetPage(3, 5));
            Should.Throw<IndexOutOfRangeException>(() => TenItemsForStudents.GetPage(99, 5));
        }

        [Fact]
        public void LazySourcePageOutOfRangeTest() {
            IEnumerable<Student> lazySource = Enumerable.Range(0, 100)
                .Select(x => new Student { Id = x, Name = $"Student-{x}" });

            Should.Throw<IndexOutOfRangeException>(() => lazySource.GetPage(21, 5));
        }
    }
}
