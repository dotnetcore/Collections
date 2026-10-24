using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable.Internal;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.OrmQueryTests
{
    /// <summary>
    /// The second tier of the streaming face: <c>ToPagesAsync</c> over a provider query, where each
    /// page is fetched by the provider's own asynchronous call instead of by a synchronous fetch
    /// wearing an asynchronous shape.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Three claims separate this tier from the core one, and each is asserted here rather than
    /// described: the pages it produces are the pages the synchronous path produces (same content,
    /// same metadata), the member count is read once for the whole walk rather than once per page,
    /// and the walk stops where the consumer stops.
    /// </para>
    /// <para>
    /// No database is contacted. EF Core's in-memory provider is a real
    /// <see cref="IQueryable{T}"/> implementation, so <c>CountAsync</c> / <c>ToListAsync</c> run
    /// through the same code path they would against a server; the counting double wraps that
    /// provider so "once" and "per page" are observed, not assumed.
    /// </para>
    /// </remarks>
    public class AsyncStreamingTests
    {
        private const int PageSize = 2;
        private const int MemberCount = 5;

        [Fact]
        public async Task The_streamed_pages_carry_the_same_content_and_metadata_as_the_synchronous_pages()
        {
            using var context = InMemoryContext();
            var query = context.Samples.OrderBy(x => x.Id);

            var synchronousPageSet = query.ToPaginable(PageSize);

            var streamed = await Collect(query.ToPagesAsync(PageSize));

            streamed.Count.ShouldBe(synchronousPageSet.PageCount);
            streamed.Select(page => page.CurrentPageNumber).ShouldBe(new[] { 1, 2, 3 });

            foreach (var page in streamed)
            {
                var expected = synchronousPageSet.GetPage(page.CurrentPageNumber);

                page.TotalMemberCount.ShouldBe(expected.TotalMemberCount);
                page.TotalPageCount.ShouldBe(expected.TotalPageCount);
                page.PageSize.ShouldBe(expected.PageSize);
                page.CurrentPageSize.ShouldBe(expected.CurrentPageSize);
                page.HasPrevious.ShouldBe(expected.HasPrevious);
                page.HasNext.ShouldBe(expected.HasNext);
                page.Select(member => member.Value.Id).ToArray()
                    .ShouldBe(expected.Select(member => member.Value.Id).ToArray());
            }

            streamed[2].CurrentPageSize.ShouldBe(1, "five members at two per page leaves one on the last page");
            streamed[2][0].Value.Name.ShouldBe("echo");
        }

        [Fact]
        public async Task The_member_count_is_read_once_and_one_page_is_fetched_at_a_time()
        {
            using var context = InMemoryContext();
            var (recorded, recorder) = CountingOver(context);

            var pages = await Collect(recorded.ToPagesAsync(PageSize));

            pages.Count.ShouldBe(3);
            recorder.CountExecutions.ShouldBe(1, "the member count belongs to the walk, not to each page");
            recorder.Enumerations.ShouldBe(3, "one provider round trip per page and no prefetch");
        }

        [Fact]
        public async Task Breaking_out_of_the_loop_stops_the_walk_where_it_stands()
        {
            using var context = InMemoryContext();
            var (recorded, recorder) = CountingOver(context);

            var seen = 0;

            await foreach (var page in recorded.ToPagesAsync(PageSize))
            {
                page.ShouldNotBeNull();
                if (++seen == 2)
                    break;
            }

            seen.ShouldBe(2);
            recorder.CountExecutions.ShouldBe(1, "the count is read before the first page, once");
            recorder.Enumerations.ShouldBe(2, "the third page must not be fetched at all");
        }

        [Fact]
        public async Task The_stream_over_a_page_set_reuses_the_member_count_it_already_knows()
        {
            using var context = InMemoryContext();
            var (recorded, recorder) = CountingOver(context);

            var pageSet = new PaginableQueryable<Sample>(
                recorded, PageSize, PaginableCalc.GetRealPageCount(MemberCount, PageSize), MemberCount);

            var pages = await Collect(pageSet.ToPagesAsync());

            pages.Count.ShouldBe(3);
            recorder.CountExecutions.ShouldBe(0, "a page set was already counted when it was built");
            recorder.Enumerations.ShouldBe(3);
            pageSet.MemberCount.ShouldBe(MemberCount);
        }

        [Fact]
        public async Task The_page_size_of_a_page_set_stream_is_the_page_size_of_the_page_set()
        {
            using var context = InMemoryContext();
            var (recorded, _) = CountingOver(context);

            var pageSet = new PaginableQueryable<Sample>(
                recorded, PageSize, PaginableCalc.GetRealPageCount(MemberCount, PageSize), MemberCount);

            var pages = await Collect(pageSet.ToPagesAsync());

            pages.ShouldAllBe(page => page.PageSize == PageSize);
            pageSet.Queryable.ShouldBeSameAs(recorded);
            pageSet.PageSize.ShouldBe(PageSize);
        }

        [Fact]
        public async Task A_cancelled_token_stops_the_walk_before_the_member_count_is_read()
        {
            using var context = InMemoryContext();
            var (recorded, recorder) = CountingOver(context);

            using var source = new CancellationTokenSource();
            source.Cancel();

            var seen = 0;

            await Should.ThrowAsync<OperationCanceledException>(async () =>
            {
                await foreach (var page in recorded.ToPagesAsync(PageSize, source.Token))
                {
                    seen++;
                    page.ShouldNotBeNull();
                }
            });

            seen.ShouldBe(0);
            recorder.CountExecutions.ShouldBe(0, "an already cancelled token must not start a round trip");
            recorder.Enumerations.ShouldBe(0);
        }

        [Fact]
        public async Task Cancelling_between_pages_stops_before_the_next_page_is_fetched()
        {
            using var context = InMemoryContext();
            var (recorded, recorder) = CountingOver(context);

            using var source = new CancellationTokenSource();
            var seen = 0;

            await Should.ThrowAsync<OperationCanceledException>(async () =>
            {
                await foreach (var page in recorded.ToPagesAsync(PageSize, source.Token))
                {
                    seen++;
                    page.ShouldNotBeNull();
                    source.Cancel();
                }
            });

            seen.ShouldBe(1, "the page already handed to the consumer is not withdrawn");
            recorder.Enumerations.ShouldBe(1, "the cancelled page must not be fetched");
        }

        [Fact]
        public void Argument_validation_happens_at_the_call_site()
        {
            // Same contract as the GetPage family: the exception leaves the call, not the first
            // MoveNextAsync. A consumer that passes a bad page size should not have to enumerate to
            // find out.
            IQueryable<Sample> missing = null;
            Should.Throw<ArgumentNullException>(() => { _ = missing.ToPagesAsync(); });

            Should.Throw<ArgumentOutOfRangeException>(
                () => { _ = new List<Sample>().AsQueryable().ToPagesAsync(0); });

            PaginableQueryable<Sample> missingPageSet = null;
            Should.Throw<ArgumentNullException>(() => { _ = missingPageSet.ToPagesAsync(); });
        }

        // ----- helpers -------------------------------------------------------

        private static async Task<List<IPage<T>>> Collect<T>(IAsyncEnumerable<IPage<T>> pages)
        {
            var collected = new List<IPage<T>>();

            await foreach (var page in pages)
                collected.Add(page);

            return collected;
        }

        /// <summary>
        /// An ordered EF Core query whose provider counts member-count reads and page fetches, so
        /// "once" and "per page" become observations rather than assumptions.
        /// </summary>
        private static (IQueryable<Sample> Query, AsyncSeamTests.RecordingQueryProvider Recorder) CountingOver(SampleContext context)
        {
            // Ordered before wrapping: calling OrderBy on the wrapper would be ambiguous, because it
            // implements both IQueryable<T> and IAsyncEnumerable<T> and .NET ships an OrderBy for each.
            var ordered = context.Samples.AsQueryable().OrderBy(x => x.Id);
            var recorder = new AsyncSeamTests.RecordingQueryProvider(ordered.Provider);

            return (new AsyncSeamTests.RecordingQuery<Sample>(recorder, ordered), recorder);
        }

        private static SampleContext InMemoryContext()
        {
            var options = new DbContextOptionsBuilder<SampleContext>()
                .UseInMemoryDatabase("f8-stream-" + Guid.NewGuid().ToString("N"))
                .Options;

            var context = new SampleContext(options);
            context.Samples.AddRange(OrmFixture.Rows);
            context.SaveChanges();
            return context;
        }
    }
}
