using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable.Tests.Models;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.Tests
{
    // P6-04: the async-shaped twins of the fragment entry points. They exist so a fragment path can
    // be awaited like any other, they complete synchronously (the fragment is already in memory),
    // and these tests pin both halves of that contract - including the part callers get wrong.
    public class CreatePageAsyncTest
    {
        // ---------------------------------------------------------------- helpers

        private const int Total = 12;
        private const int PageSize = 5;

        private static List<Student> Students(int count) =>
            Enumerable.Range(0, count)
                .Select(x => new Student { Id = x, Name = $"Student-{x}" })
                .ToList();

        private static List<Student> Page(int pageNumber) =>
            Students(Total).Skip((pageNumber - 1) * PageSize).Take(PageSize).ToList();

        private static int[] Ids(IPage<Student> page) =>
            page.Select(m => m.Value.Id).ToArray();

        private static void ShouldHaveSameMetadata(IPage expected, IPage actual)
        {
            var e = expected.GetMetadata();
            var a = actual.GetMetadata();

            a.TotalPageCount.ShouldBe(e.TotalPageCount);
            a.RealPageCount.ShouldBe(e.RealPageCount);
            a.TotalMemberCount.ShouldBe(e.TotalMemberCount);
            a.CurrentPageNumber.ShouldBe(e.CurrentPageNumber);
            a.PageSize.ShouldBe(e.PageSize);
            a.CurrentPageSize.ShouldBe(e.CurrentPageSize);
            a.HasPrevious.ShouldBe(e.HasPrevious);
            a.HasNext.ShouldBe(e.HasNext);
        }

        // ------------------------------------------------------- the sync-completed shape

        [Fact]
        public void TheReturnedTaskIsAlreadyCompleted()
        {
            var task = Paginable.CreatePageAsync(Page(2), new PageFragmentInfo(2, PageSize, Total));

            task.IsCompleted.ShouldBeTrue();
            task.Status.ShouldBe(TaskStatus.RanToCompletion);
        }

        [Fact]
        public async Task AwaitingGivesTheSamePageCreatePageWouldReturn()
        {
            var fragment = Page(2);
            var info = new PageFragmentInfo(pageNumber: 2, pageSize: PageSize, totalMemberCount: Total);

            var expected = Paginable.CreatePage(fragment, info);
            var actual = await Paginable.CreatePageAsync(fragment, info);

            ShouldHaveSameMetadata(expected, actual);
            Ids(actual).ShouldBe(Ids(expected));
            actual.Select(m => m.ItemNumber).ShouldBe(expected.Select(m => m.ItemNumber));
        }

        [Fact]
        public async Task EveryOverloadAgreesWithItsSynchronousCounterpart()
        {
            var fragment = Page(2);
            var info = new PageFragmentInfo(pageNumber: 2, pageSize: PageSize, totalMemberCount: Total);
            var expected = Paginable.CreatePage(fragment, 2, PageSize, Total);

            var pages = new[]
            {
                await Paginable.CreatePageAsync(fragment, info),
                await Paginable.CreatePageAsync(fragment, info, PageCreationOptions.Lenient),
                await Paginable.CreatePageAsync(fragment, 2, PageSize, Total),
                await Paginable.CreatePageAsync(fragment, 2, PageSize, Total, PageCreationOptions.Lenient)
            };

            foreach (var page in pages)
            {
                ShouldHaveSameMetadata(expected, page);
                Ids(page).ShouldBe(Ids(expected));
            }
        }

        [Fact]
        public async Task StrictModeIsHonouredThroughTheAsyncOverloads()
        {
            var info = new PageFragmentInfo(pageNumber: 3, pageSize: PageSize, totalMemberCount: Total);

            var byMetadata = await Paginable.CreatePageAsync(Page(3), info, PageCreationOptions.Strict);
            var byNumbers = await Paginable.CreatePageAsync(Page(3), 3, PageSize, Total, PageCreationOptions.Strict);

            Ids(byMetadata).ShouldBe(new[] { 10, 11 });
            Ids(byNumbers).ShouldBe(new[] { 10, 11 });
        }

        // ------------------------------------------- validation is synchronous, not faulted

        [Fact]
        public void AShortFragmentUnderStrictThrowsFromTheCallNotFromTheAwait()
        {
            // The trap this documents: there is no faulted task to await. Task.FromResult
            // evaluates its argument first, so the exception leaves the method call itself.
            var shortFragment = Page(3).Take(1).ToList();
            var info = new PageFragmentInfo(pageNumber: 3, pageSize: PageSize, totalMemberCount: Total);

            var ex = Should.Throw<ArgumentException>(
                () => Paginable.CreatePageAsync(shortFragment, info, PageCreationOptions.Strict));

            ex.ParamName.ShouldBe("fragment");
        }

        [Fact]
        public void NullFragmentThrowsSynchronouslyOnEveryOverload()
        {
            List<Student> fragment = null;
            var info = new PageFragmentInfo(1, PageSize, Total);

            Should.Throw<ArgumentNullException>(() => Paginable.CreatePageAsync(fragment, info)).ParamName.ShouldBe("fragment");
            Should.Throw<ArgumentNullException>(() => Paginable.CreatePageAsync(fragment, info, PageCreationOptions.Strict)).ParamName.ShouldBe("fragment");
            Should.Throw<ArgumentNullException>(() => Paginable.CreatePageAsync(fragment, 1, PageSize, Total)).ParamName.ShouldBe("fragment");
            Should.Throw<ArgumentNullException>(() => Paginable.CreatePageAsync(fragment, 1, PageSize, Total, PageCreationOptions.Strict)).ParamName.ShouldBe("fragment");
        }

        [Fact]
        public void NullMetadataAndNullOptionsThrowSynchronously()
        {
            PageFragmentInfo info = null;
            PageCreationOptions options = null;
            var fragment = Page(1);

            Should.Throw<ArgumentNullException>(() => Paginable.CreatePageAsync(fragment, info)).ParamName.ShouldBe("metadata");
            Should.Throw<ArgumentNullException>(() => Paginable.CreatePageAsync(fragment, new PageFragmentInfo(1, PageSize, Total), options)).ParamName.ShouldBe("options");
            Should.Throw<ArgumentNullException>(() => Paginable.CreatePageAsync(fragment, 1, PageSize, Total, options)).ParamName.ShouldBe("options");
        }

        [Theory]
        [InlineData(0, PageSize, Total)]
        [InlineData(1, 0, Total)]
        [InlineData(1, PageSize, -1)]
        public void OutOfRangeArgumentsThrowSynchronously(int pageNumber, int pageSize, int totalMemberCount)
        {
            var ex = Should.Throw<ArgumentOutOfRangeException>(
                () => Paginable.CreatePageAsync(Page(1), pageNumber, pageSize, totalMemberCount));

            ex.ShouldNotBeNull();
        }

        [Fact]
        public void AnOverLongFragmentThrowsSynchronously()
        {
            // the whole source is not a page: 12 members against a page size of 5
            var ex = Should.Throw<ArgumentException>(
                () => Paginable.CreatePageAsync(Students(Total), 1, PageSize, Total));

            ex.ParamName.ShouldBe("fragment");
        }

        // ------------------------------------------------------------- cancellation token

        [Fact]
        public async Task AnAlreadyCancelledTokenIsNotObserved()
        {
            // documented as unused; a cancelled token must not turn a pure in-memory wrap into a
            // cancellation, which would be a silent behaviour change for callers reusing one token
            using (var source = new CancellationTokenSource())
            {
                source.Cancel();

                var page = await Paginable.CreatePageAsync(Page(2), 2, PageSize, Total, source.Token);

                Ids(page).ShouldBe(new[] { 5, 6, 7, 8, 9 });
            }
        }

        // ---------------------------------------------------------------------- edges

        [Fact]
        public async Task AnEmptyFragmentYieldsOneEmptyPage()
        {
            var page = await Paginable.CreatePageAsync(new List<Student>(), 1, PageSize, 0);

            page.CurrentPageSize.ShouldBe(0);
            page.ToOriginalItems().ShouldBeEmpty();
        }

        [Fact]
        public async Task ThePageDoesNotFollowLaterMutationsOfTheFragment()
        {
            var fragment = Page(1);
            var page = await Paginable.CreatePageAsync(fragment, 1, PageSize, Total);

            fragment.Clear();

            page.ToOriginalItems().Count().ShouldBe(PageSize);
            Ids(page).ShouldBe(new[] { 0, 1, 2, 3, 4 });
        }

        [Fact]
        public async Task ParallelAwaitsAreConsistent()
        {
            var fragment = Page(2);
            var info = new PageFragmentInfo(pageNumber: 2, pageSize: PageSize, totalMemberCount: Total);
            var expected = Ids(Paginable.CreatePage(fragment, info));

            var results = await Task.WhenAll(
                Enumerable.Range(0, 100).Select(_ => Task.Run(async () => Ids(await Paginable.CreatePageAsync(fragment, info)))));

            foreach (var ids in results)
            {
                ids.ShouldBe(expected);
            }
        }
    }
}
