using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable.Tests.Models;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.Tests {
    public class GetPageForEnumerableTest {

        private IList<Student> EmptyListForStudents { get; set; }
        private IList<Student> OneItemForStudents { get; set; }
        private IList<Student> EightItemsForStudents { get; set; }

        public GetPageForEnumerableTest() {
            EmptyListForStudents = new List<Student>();
            OneItemForStudents = new List<Student> {new Student {Id = 1, Name = "Alex"}};
            EightItemsForStudents = new List<Student> {
                new Student {Id = 2, Name = "Zhaomin"},
                new Student {Id = 3, Name = "Zhaomin"},
                new Student {Id = 4, Name = "Zhaomin"},
                new Student {Id = 5, Name = "Zhaomin"},
                new Student {Id = 6, Name = "Zhaomin"},
                new Student {Id = 7, Name = "Zhaomin"},
                new Student {Id = 8, Name = "Zhaomin"},
                new Student {Id = 9, Name = "Zhaomin"},
            };
        }

        [Fact]
        public void EmptyListTest() {
            var page = EmptyListForStudents.GetPage(1, 9);
            page.TotalPageCount.ShouldBe(1);
            page.TotalMemberCount.ShouldBe(0);
            page.CurrentPageNumber.ShouldBe(1);
            page.PageSize.ShouldBe(9);
            page.CurrentPageSize.ShouldBe(0); //应该是0
            page.HasNext.ShouldBeFalse();
            page.HasPrevious.ShouldBeFalse();
        }

        [Fact]
        public void EmptyListToOriginTest() {
            var page = EmptyListForStudents.GetPage(1, 9);
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
        public void OnePageOriginTest() {
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
            var page0 = EightItemsForStudents.GetPage(1, 2);
            var page1 = EightItemsForStudents.GetPage(2, 2);

            page0.TotalPageCount.ShouldBe(4);
            page0.TotalMemberCount.ShouldBe(8);
            page0.CurrentPageNumber.ShouldBe(1);
            page0.PageSize.ShouldBe(2);
            page0.CurrentPageSize.ShouldBe(2);
            page0.HasNext.ShouldBeTrue();
            page0.HasPrevious.ShouldBeFalse();

            page1.TotalPageCount.ShouldBe(4);
            page1.TotalMemberCount.ShouldBe(8);
            page1.CurrentPageNumber.ShouldBe(2);
            page1.PageSize.ShouldBe(2);
            page1.CurrentPageSize.ShouldBe(2);
            page1.HasNext.ShouldBeTrue();
            page1.HasPrevious.ShouldBeTrue();
        }

        [Fact]
        public void SeveralPagesOriginTest() {
            var page0 = EightItemsForStudents.GetPage(1, 2);
            var page1 = EightItemsForStudents.GetPage(2, 2);

            var origins0 = page0.ToOriginalItems();
            origins0.ShouldNotBeNull();
            origins0.Count().ShouldBe(2);

            var counter0 = 0;
            foreach (var item in origins0) counter0++;
            counter0.ShouldBe(2);

            var origins1 = page1.ToOriginalItems();
            origins1.ShouldNotBeNull();
            origins1.Count().ShouldBe(2);

            var counter1 = 0;
            foreach (var item in origins1) counter1++;
            counter1.ShouldBe(2);
        }

        [Fact]
        public void SeveralPagesOriginTest_NonFullMode() {
            //假设为地20页的8条记录
            //又，假设每页8条记录，一共502页，当前第23页，一共有 8 * 502 - 3 = 4013 条记录
            var originalSource = EightItemsForStudents;

            var page0 = new EnumerablePage<Student>(originalSource, 23, 8, 8 * 502 - 3, false);

            page0.TotalPageCount.ShouldBe(502);
            page0.TotalMemberCount.ShouldBe(4013);
            page0.CurrentPageNumber.ShouldBe(23);
            page0.PageSize.ShouldBe(8);
            page0.CurrentPageSize.ShouldBe(8);
            page0.HasNext.ShouldBeTrue();
            page0.HasPrevious.ShouldBeTrue();

            var origins0 = page0.ToOriginalItems();
            origins0.ShouldNotBeNull();
            origins0.Count().ShouldBe(8);

            var counter0 = 0;
            foreach (var item in origins0) counter0++;
            counter0.ShouldBe(8);
        }
    }
}