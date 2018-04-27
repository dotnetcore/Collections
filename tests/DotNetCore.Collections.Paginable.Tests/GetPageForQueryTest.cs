using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable.Tests.Models;
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
            Assert.Equal(1, page.TotalPageCount);
            Assert.Equal(0, page.TotalMemberCount);
            Assert.Equal(1, page.CurrentPageNumber);
            Assert.Equal(9, page.PageSize);
            Assert.Equal(0, page.CurrentPageSize);
            Assert.False(page.HasNext);
            Assert.False(page.HasPrevious);
        }

        [Fact]
        public void OneItemTest() {
            var page = OneItemForStudents.GetPage(1, 9);
            Assert.Equal(1, page.TotalPageCount);
            Assert.Equal(1, page.TotalMemberCount);
            Assert.Equal(1, page.CurrentPageNumber);
            Assert.Equal(9, page.PageSize);
            Assert.Equal(1, page.CurrentPageSize);
            Assert.False(page.HasNext);
            Assert.False(page.HasPrevious);
        }

        [Fact]
        public void OnePageTest() {
            var page = EightItemsForStudents.GetPage(1, 9);
            Assert.Equal(1, page.TotalPageCount);
            Assert.Equal(8, page.TotalMemberCount);
            Assert.Equal(1, page.CurrentPageNumber);
            Assert.Equal(9, page.PageSize);
            Assert.Equal(8, page.CurrentPageSize);
            Assert.False(page.HasNext);
            Assert.False(page.HasPrevious);
        }
        
        [Fact]
        public void SeveralPagesTest() {
            var page = EightItemsForStudents.GetPage(1, 2);
            Assert.Equal(4, page.TotalPageCount);
            Assert.Equal(8, page.TotalMemberCount);
            Assert.Equal(1, page.CurrentPageNumber);
            Assert.Equal(2, page.PageSize);
            Assert.Equal(2, page.CurrentPageSize);
            Assert.True(page.HasNext);
            Assert.False(page.HasPrevious);
        }
        
    }
}