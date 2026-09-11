using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable.Tests.Models;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.Tests {
    // F6-10: every GetPage-family rejection of an out-of-range argument now reports
    // ArgumentOutOfRangeException instead of IndexOutOfRangeException (the 6.1 fragment API
    // already reported the former, so the two input shapes now agree). These tests pin both the
    // exact exception type and the offending parameter name, so the shape can not drift back,
    // and they re-check that the rejection *conditions* were not touched by the retyping.
    public class GetPageExceptionTypeTest {

        private static IList<Student> BuildStudents(int count)
            => Enumerable.Range(0, count)
                .Select(x => new Student { Id = x, Name = $"Student-{x}" })
                .ToList();

        private static IEnumerable<Student> BuildLazyStudents(int count)
            => BuildStudents(count);

        #region IEnumerable<T> path

        [Fact]
        public void EnumerablePageNumberTooSmall_ReportsPageNumber() {
            var ex = Should.Throw<ArgumentOutOfRangeException>(() => BuildStudents(10).GetPage(0, 5));
            ex.ParamName.ShouldBe("pageNumber");
        }

        [Fact]
        public void EnumerablePageSizeTooSmall_ReportsPageSize() {
            var ex = Should.Throw<ArgumentOutOfRangeException>(() => BuildStudents(10).GetPage(1, 0));
            ex.ParamName.ShouldBe("pageSize");
        }

        [Fact]
        public void EnumerablePagePastTheEnd_ReportsPageNumber() {
            var ex = Should.Throw<ArgumentOutOfRangeException>(() => BuildStudents(10).GetPage(3, 5));
            ex.ParamName.ShouldBe("pageNumber");
        }

        [Fact]
        public void EnumerableLazySourcePagePastTheEnd_ReportsPageNumber() {
            var ex = Should.Throw<ArgumentOutOfRangeException>(() => BuildLazyStudents(10).GetPage(99, 5));
            ex.ParamName.ShouldBe("pageNumber");
        }

        #endregion

        #region IQueryable<T> path

        [Fact]
        public void QueryablePageNumberTooSmall_ReportsPageNumber() {
            var ex = Should.Throw<ArgumentOutOfRangeException>(() => BuildStudents(10).AsQueryable().GetPage(0, 5));
            ex.ParamName.ShouldBe("pageNumber");
        }

        [Fact]
        public void QueryablePageSizeTooSmall_ReportsPageSize() {
            var ex = Should.Throw<ArgumentOutOfRangeException>(() => BuildStudents(10).AsQueryable().GetPage(1, 0));
            ex.ParamName.ShouldBe("pageSize");
        }

        [Fact]
        public void QueryablePagePastTheEnd_ReportsPageNumber() {
            var ex = Should.Throw<ArgumentOutOfRangeException>(() => BuildStudents(10).AsQueryable().GetPage(99, 5));
            ex.ParamName.ShouldBe("pageNumber");
        }

        #endregion

        #region Task<IQueryable<T>> path

        [Fact]
        public async Task TaskQueryablePageNumberTooSmall_ReportsPageNumber() {
            var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => Task.FromResult(BuildStudents(10).AsQueryable()).GetPageAsync(0, 5));
            ex.ParamName.ShouldBe("pageNumber");
        }

        [Fact]
        public async Task TaskQueryablePageSizeTooSmall_ReportsPageSize() {
            var ex = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => Task.FromResult(BuildStudents(10).AsQueryable()).GetPageAsync(1, 0));
            ex.ParamName.ShouldBe("pageSize");
        }

        #endregion

        #region keyset (seek) paths

        [Fact]
        public void EnumerableKeysetPageSizeTooSmall_ReportsPageSize() {
            var ex = Should.Throw<ArgumentOutOfRangeException>(
                () => BuildStudents(10).GetFirstPageByKeyset(x => x.Id, 0));
            ex.ParamName.ShouldBe("pageSize");
        }

        [Fact]
        public void QueryableKeysetPageSizeTooSmall_ReportsPageSize() {
            var ex = Should.Throw<ArgumentOutOfRangeException>(
                () => BuildStudents(10).AsQueryable().GetPageByKeyset(x => x.Id, lastKey: 5, pageSize: 0));
            ex.ParamName.ShouldBe("pageSize");
        }

        #endregion

        #region the point of the change

        [Fact]
        public void OutOfRangeRejectionIsAnArgumentException_NoLongerAnIndexException() {
            var ex = Should.Throw<ArgumentOutOfRangeException>(() => BuildStudents(10).GetPage(0, 5));

            // An out-of-range *argument* is an argument problem, which is what callers of the
            // 6.1 fragment API already had to catch - and what the whole family now reports.
            // The index type is checked reflectively: it is sealed, so an `is` pattern would be
            // a compile-time constant and the compiler would flag it (CS0184).
            ex.ShouldBeAssignableTo<ArgumentException>();
            ex.GetType().ShouldBe(typeof(ArgumentOutOfRangeException));
            typeof(IndexOutOfRangeException).IsInstanceOfType(ex).ShouldBeFalse();
        }

        [Fact]
        public void FragmentApiAndGetPageNowAgreeOnTheTypeAndParameterName() {
            var enumerableEx = Should.Throw<ArgumentOutOfRangeException>(() => BuildStudents(10).GetPage(0, 5));
            var fragmentEx = Should.Throw<ArgumentOutOfRangeException>(
                () => Paginable.CreatePage(BuildStudents(5), pageNumber: 0, pageSize: 5, totalMemberCount: 10));

            fragmentEx.GetType().ShouldBe(enumerableEx.GetType());
            fragmentEx.ParamName.ShouldBe(enumerableEx.ParamName);
        }

        #endregion

        #region rejection conditions are untouched by the retyping

        [Fact]
        public void ValidBoundaryStillSucceeds() {
            var page = BuildStudents(10).GetPage(2, 5);
            page.CurrentPageSize.ShouldBe(5);
            page.CurrentPageNumber.ShouldBe(2);
            page[0].Value.Id.ShouldBe(5);
        }

        [Fact]
        public void EmptySourceStillYieldsOneEmptyPage() {
            // The empty source is *not* an out-of-range argument: total 0 means the
            // past-the-end guard is skipped, exactly as it was before F6-10.
            var page = BuildStudents(0).GetPage(1, 5);
            page.TotalPageCount.ShouldBe(1);
            page.CurrentPageSize.ShouldBe(0);
            page.ToList().ShouldBeEmpty();
        }

        #endregion
    }
}
