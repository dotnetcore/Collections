using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable.Tests.Models;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.Tests {
    public class GetPageForQueryTest {

        private IQueryable<Student> EmptyQueryForStudents { get; set; }
        private IQueryable<Student> OneItemForStudents { get; set; }
        private IQueryable<Student> EightItemsForStudents { get; set; }

        public GetPageForQueryTest() {
            EmptyQueryForStudents = new List<Student>().AsQueryable();
            OneItemForStudents = new List<Student> {new Student {Id = 1, Name = "Alex"}}.AsQueryable();
            EightItemsForStudents = new List<Student> {
                new Student {Id = 2, Name = "Zhaomin"},
                new Student {Id = 3, Name = "Zhaomin"},
                new Student {Id = 4, Name = "Zhaomin"},
                new Student {Id = 5, Name = "Zhaomin"},
                new Student {Id = 6, Name = "Zhaomin"},
                new Student {Id = 7, Name = "Zhaomin"},
                new Student {Id = 8, Name = "Zhaomin"},
                new Student {Id = 9, Name = "Zhaomin"},
            }.AsQueryable();
        }

        [Fact]
        public void EmptyQueryTest() {
            var page = EmptyQueryForStudents.GetPage(1, 9);
            page.TotalPageCount.ShouldBe(1);
            page.TotalMemberCount.ShouldBe(0);
            page.CurrentPageNumber.ShouldBe(1);
            page.PageSize.ShouldBe(9);
            page.CurrentPageSize.ShouldBe(0);//应该是0
            page.HasNext.ShouldBeFalse();
            page.HasPrevious.ShouldBeFalse();
        }

        [Fact]
        public void EmptyQueryToOriginTest() {
            var page = EmptyQueryForStudents.GetPage(1, 9);
            var origins = page.ToOriginalItems();
            origins.ShouldNotBeNull();
            origins.ShouldBeEmpty();

            var counter = 0;
            foreach (var item in origins) counter++;
            counter.ShouldBe(0);
        }

        [Fact]
        public void OneItemTest() {
            var page = OneItemForStudents.GetPage(1, 9);
            page.TotalPageCount.ShouldBe(1);
            page.TotalMemberCount.ShouldBe(1);
            page.CurrentPageNumber.ShouldBe(1);
            page.PageSize.ShouldBe(9);
            page.CurrentPageSize.ShouldBe(1);
            page.HasNext.ShouldBeFalse();
            page.HasPrevious.ShouldBeFalse();
        }

        [Fact]
        public void OneItemToOriginTest() {
            var page = OneItemForStudents.GetPage(1, 9);
            var origins = page.ToOriginalItems();
            origins.ShouldNotBeNull();
            origins.Count().ShouldBe(1);

            var counter = 0;
            foreach (var item in origins) counter++;
            counter.ShouldBe(1);
        }

        [Fact]
        public void OnePageTest() {
            var page = EightItemsForStudents.GetPage(1, 9);
            page.TotalPageCount.ShouldBe(1);
            page.TotalMemberCount.ShouldBe(8);
            page.CurrentPageNumber.ShouldBe(1);
            page.PageSize.ShouldBe(9);
            page.CurrentPageSize.ShouldBe(8);
            page.HasNext.ShouldBeFalse();
            page.HasPrevious.ShouldBeFalse();
        }

        [Fact]
        public void OnePageoOriginTest() {
            var page = EightItemsForStudents.GetPage(1, 9);
            var origins = page.ToOriginalItems();
            origins.ShouldNotBeNull();
            origins.Count().ShouldBe(8);

            var counter = 0;
            foreach (var item in origins) counter++;
            counter.ShouldBe(8);
        }

        [Fact]
        public void SeveralPagesTest() {
            var page = EightItemsForStudents.GetPage(1, 2);
            page.TotalPageCount.ShouldBe(4);
            page.TotalMemberCount.ShouldBe(8);
            page.CurrentPageNumber.ShouldBe(1);
            page.PageSize.ShouldBe(2);
            page.CurrentPageSize.ShouldBe(2);
            page.HasNext.ShouldBeTrue();
            page.HasPrevious.ShouldBeFalse();
        }

        [Fact]
        public void SeveralPagesOriginTest() {
            var page = EightItemsForStudents.GetPage(1, 2);
            var origins = page.ToOriginalItems();
            origins.ShouldNotBeNull();
            origins.Count().ShouldBe(2);

            var counter = 0;
            foreach (var item in origins) counter++;
            counter.ShouldBe(2);
        }

    }
}