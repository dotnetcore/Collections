using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable.Tests.Models;
using Xunit;

namespace DotNetCore.Collections.Paginable.Tests {
    public class GetPageValidationTest {

        private IList<Student> EmptyListForStudents { get; }
        private IQueryable<Student> EmptyQueryForStudents { get; }

        public GetPageValidationTest() {
            EmptyListForStudents = new List<Student>();
            EmptyQueryForStudents = new List<Student>().AsQueryable();
        }

        // ----- GetPage for IEnumerable<T> -----

        [Fact]
        public void GetPageForEnumerable_ShouldThrow_WhenPageNumberIsZero() {
            Assert.Throws<IndexOutOfRangeException>(() => EmptyListForStudents.GetPage(0, 9));
        }

        [Fact]
        public void GetPageForEnumerable_ShouldThrow_WhenPageNumberIsNegative() {
            Assert.Throws<IndexOutOfRangeException>(() => EmptyListForStudents.GetPage(-1, 9));
        }

        [Fact]
        public void GetPageForEnumerable_ShouldThrow_WhenPageSizeIsZero() {
            Assert.Throws<IndexOutOfRangeException>(() => EmptyListForStudents.GetPage(1, 0));
        }

        [Fact]
        public void GetPageForEnumerable_ShouldThrow_WhenPageSizeIsNegative() {
            Assert.Throws<IndexOutOfRangeException>(() => EmptyListForStudents.GetPage(1, -1));
        }

        // ----- GetPage for IQueryable<T> -----

        [Fact]
        public void GetPageForQueryable_ShouldThrow_WhenPageNumberIsZero() {
            Assert.Throws<IndexOutOfRangeException>(() => EmptyQueryForStudents.GetPage(0, 9));
        }

        [Fact]
        public void GetPageForQueryable_ShouldThrow_WhenPageNumberIsNegative() {
            Assert.Throws<IndexOutOfRangeException>(() => EmptyQueryForStudents.GetPage(-1, 9));
        }

        [Fact]
        public void GetPageForQueryable_ShouldThrow_WhenPageSizeIsZero() {
            Assert.Throws<IndexOutOfRangeException>(() => EmptyQueryForStudents.GetPage(1, 0));
        }

        [Fact]
        public void GetPageForQueryable_ShouldThrow_WhenPageSizeIsNegative() {
            Assert.Throws<IndexOutOfRangeException>(() => EmptyQueryForStudents.GetPage(1, -1));
        }

        // ----- ToPaginable -----

        [Fact]
        public void ToPaginableForEnumerable_ShouldThrow_WhenPageSizeIsZero() {
            Assert.Throws<ArgumentOutOfRangeException>(() => EmptyListForStudents.ToPaginable(0));
        }

        [Fact]
        public void ToPaginableForEnumerable_ShouldThrow_WhenPageSizeIsNegative() {
            Assert.Throws<ArgumentOutOfRangeException>(() => EmptyListForStudents.ToPaginable(-1));
        }

        [Fact]
        public void ToPaginableForQueryable_ShouldThrow_WhenPageSizeIsZero() {
            Assert.Throws<ArgumentOutOfRangeException>(() => EmptyQueryForStudents.ToPaginable(0));
        }

        [Fact]
        public void ToPaginableForQueryable_ShouldThrow_WhenPageSizeIsNegative() {
            Assert.Throws<ArgumentOutOfRangeException>(() => EmptyQueryForStudents.ToPaginable(-1));
        }

        // ----- GetPageAsync -----

        [Fact]
        public async Task GetPageAsync_ShouldThrow_WhenPageNumberIsZero() {
            await Assert.ThrowsAsync<IndexOutOfRangeException>(
                () => Task.FromResult(EmptyQueryForStudents).GetPageAsync(0, 9));
        }

        [Fact]
        public async Task GetPageAsync_ShouldThrow_WhenPageSizeIsZero() {
            await Assert.ThrowsAsync<IndexOutOfRangeException>(
                () => Task.FromResult(EmptyQueryForStudents).GetPageAsync(1, 0));
        }

        // ----- Valid boundaries keep working -----

        [Fact]
        public void GetPageForEnumerable_ShouldNotThrow_WhenPageNumberIsOne() {
            var page = EmptyListForStudents.GetPage(1, 9);
            Assert.NotNull(page);
        }

        [Fact]
        public void GetPageForQueryable_ShouldNotThrow_WhenPageNumberIsOne() {
            var page = EmptyQueryForStudents.GetPage(1, 9);
            Assert.NotNull(page);
        }
    }
}
