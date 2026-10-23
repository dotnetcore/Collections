using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable.Tests.Models;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.Tests
{
    // F8-01: the streaming face (ToPagesAsync). The four contracts pinned here are the four the
    // documentation promises, in the same order: (1) the stream yields exactly the pages the
    // synchronous API yields; (2) it is lazy - consuming N pages walks N pages, nothing is
    // prefetched; (3) cancellation is observed between pages; (4) argument validation is
    // synchronous, exactly like the GetPage family.
    //
    // Every asynchronous test carries a timeout, and the cancellation tests bound their own
    // token with a deadline: a broken stream must fail the test, never hang the runner.
    public class ToPagesAsyncTest
    {
        private const int Total = 12;
        private const int PageSize = 5;
        private const int PageCount = 3; // 5 + 5 + 2

        private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(20);

        private static List<Student> Students(int count) =>
            Enumerable.Range(0, count)
                .Select(x => new Student { Id = x, Name = $"Student-{x}" })
                .ToList();

        private static int[] Ids(IPage<Student> page) =>
            page.Select(m => m.Value.Id).ToArray();

        private static int[] Ids(IEnumerable<Student> items) =>
            items.Select(x => x.Id).ToArray();

        // ---------------------------------------------------------------- shape

        [Fact(Timeout = 30000)]
        public async Task StreamsEveryPageOfAnEnumerableSource()
        {
            var pages = new List<IPage<Student>>();
            await foreach (var page in Students(Total).ToPagesAsync(PageSize))
            {
                pages.Add(page);
            }

            pages.Count.ShouldBe(PageCount);

            for (var i = 0; i < PageCount; i++)
            {
                var expected = Students(Total).GetPage(i + 1, PageSize);
                Ids(pages[i]).ShouldBe(Ids(expected));
                pages[i].CurrentPageNumber.ShouldBe(expected.CurrentPageNumber);
                pages[i].CurrentPageSize.ShouldBe(expected.CurrentPageSize);
                pages[i].TotalMemberCount.ShouldBe(expected.TotalMemberCount);
            }
        }

        [Fact(Timeout = 30000)]
        public async Task StreamingAnExistingPageSetAgreesWithEnumeratingItSynchronously()
        {
            var paginable = Students(Total).ToPaginable(PageSize);

            var sync = paginable.Select(Ids).ToList();

            var streamed = new List<int[]>();
            await foreach (var page in paginable.ToPagesAsync())
            {
                streamed.Add(Ids(page));
            }

            streamed.Count.ShouldBe(sync.Count);
            for (var i = 0; i < sync.Count; i++)
            {
                streamed[i].ShouldBe(sync[i]);
            }
        }

        [Fact(Timeout = 30000)]
        public async Task StreamingAQueryablePageSetWalksOneQueryPerPage()
        {
            // The queryable page set keeps its provider translation, so every page is a
            // Skip/Take over the same source rather than a materialised snapshot.
            var paginable = Students(Total).AsQueryable().ToPaginable(PageSize);

            var streamed = new List<int[]>();
            await foreach (var page in paginable.ToPagesAsync())
            {
                streamed.Add(Ids(page));
            }

            streamed.Count.ShouldBe(PageCount);
            streamed[0].ShouldBe(new[] { 0, 1, 2, 3, 4 });
            streamed[2].ShouldBe(new[] { 10, 11 });
        }

        [Fact(Timeout = 30000)]
        public async Task AnEmptySourceStreamsNothing()
        {
            var pages = 0;
            await foreach (var page in new List<Student>().ToPagesAsync(PageSize))
            {
                pages++;
            }

            pages.ShouldBe(0);
        }

        [Fact(Timeout = 30000)]
        public async Task TheDefaultPageSizeOverloadUsesTheConfiguredSetting()
        {
            var original = PaginableSettingsManager.Settings.DefaultPageSize;
            PaginableSettingsManager.UpdateSettings(new PaginableSettings { DefaultPageSize = 4 });

            try
            {
                var pages = new List<IPage<Student>>();
                await foreach (var page in Students(Total).ToPagesAsync())
                {
                    pages.Add(page);
                }

                pages.Count.ShouldBe(3);
                pages.All(p => p.PageSize == 4).ShouldBeTrue();
                pages[0].CurrentPageSize.ShouldBe(4);
                pages[2].CurrentPageSize.ShouldBe(4); // 12 members divide exactly into 3 pages of 4
            }
            finally
            {
                PaginableSettingsManager.UpdateSettings(new PaginableSettings { DefaultPageSize = original });
            }
        }

        // ------------------------------------------------------- lazy, no prefetch

        [Fact(Timeout = 30000)]
        public async Task ConsumingTwoOfThreePagesWalksTwoPages()
        {
            var paginable = new CountingPaginable(PageCount, PageSize);

            var seen = 0;
            await foreach (var page in paginable.ToPagesAsync())
            {
                seen++;
                if (seen == 2)
                    break;
            }

            seen.ShouldBe(2);
            paginable.EnumeratedPages.ShouldBe(2); // page three was never touched
        }

        [Fact(Timeout = 30000)]
        public async Task BreakingOutOfTheStreamDisposesTheEnumerator()
        {
            var paginable = new CountingPaginable(PageCount, PageSize);

            await foreach (var page in paginable.ToPagesAsync())
            {
                break;
            }

            paginable.EnumeratedPages.ShouldBe(1);
            paginable.EnumeratorDisposed.ShouldBeTrue();
        }

        [Fact(Timeout = 30000)]
        public async Task TheStreamDoesNotWalkTheSourceMoreThanTheSynchronousApiDoes()
        {
            // A lazy source is re-walked by the paging machinery itself (one member-count pass,
            // then one walk per page read). The asynchronous shape must not add walks of its own.
            var syncSource = new CountingSource(Total);
            var syncSeen = 0;
            foreach (var page in syncSource.ToPaginable(PageSize))
            {
                syncSeen++;
                if (syncSeen == 2)
                    break;
            }

            var asyncSource = new CountingSource(Total);
            var asyncSeen = 0;
            await foreach (var page in asyncSource.ToPagesAsync(PageSize))
            {
                asyncSeen++;
                if (asyncSeen == 2)
                    break;
            }

            asyncSeen.ShouldBe(syncSeen);
            asyncSource.Walks.ShouldBe(syncSource.Walks);
            asyncSource.Steps.ShouldBe(syncSource.Steps);
        }

        // ------------------------------------------------------------ cancellation

        [Fact(Timeout = 30000)]
        public async Task AnAlreadyCancelledTokenStopsTheStreamBeforeTheFirstPage()
        {
            var paginable = new CountingPaginable(PageCount, PageSize);
            using var cts = new CancellationTokenSource(Deadline);
            cts.Cancel();

            await Should.ThrowAsync<OperationCanceledException>(async () =>
            {
                await foreach (var page in paginable.ToPagesAsync(cts.Token))
                {
                    // must not be reached
                }
            });

            paginable.EnumeratedPages.ShouldBe(0);
        }

        [Fact(Timeout = 30000)]
        public async Task CancellingBetweenPagesStopsTheStream()
        {
            var paginable = new CountingPaginable(PageCount, PageSize);
            using var cts = new CancellationTokenSource(Deadline);

            var seen = 0;
            await Should.ThrowAsync<OperationCanceledException>(async () =>
            {
                await foreach (var page in paginable.ToPagesAsync(cts.Token))
                {
                    seen++;
                    cts.Cancel();
                }
            });

            seen.ShouldBe(1);
            paginable.EnumeratedPages.ShouldBe(1);
        }

        [Fact(Timeout = 30000)]
        public async Task APageThatIsAlreadyMaterialisedIsNotInterruptedByALaterCancellation()
        {
            // Cancellation is observed *between* pages: the page in hand is complete, and the
            // stream stops before the next one is fetched.
            var paginable = new CountingPaginable(PageCount, PageSize);
            using var cts = new CancellationTokenSource(Deadline);

            IPage<Student> first = null;
            await Should.ThrowAsync<OperationCanceledException>(async () =>
            {
                await foreach (var page in paginable.ToPagesAsync(cts.Token))
                {
                    first = page;
                    cts.Cancel();
                }
            });

            first.ShouldNotBeNull();
            Ids(first).ShouldBe(new[] { 0, 1, 2, 3, 4 });
            paginable.EnumeratedPages.ShouldBe(1);
        }

        // -------------------------------------------- validation is synchronous

        [Fact]
        public void NullSourceThrowsSynchronouslyOnEveryOverload()
        {
            List<Student> source = null;
            IPaginable<Student> paginable = null;

            Should.Throw<ArgumentNullException>(() => source.ToPagesAsync()).ParamName.ShouldBe("source");
            Should.Throw<ArgumentNullException>(() => source.ToPagesAsync(PageSize)).ParamName.ShouldBe("source");
            Should.Throw<ArgumentNullException>(() => source.ToPagesAsync(PageSize, CancellationToken.None)).ParamName.ShouldBe("source");
            Should.Throw<ArgumentNullException>(() => paginable.ToPagesAsync()).ParamName.ShouldBe("paginable");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void APageSizeBelowOneThrowsSynchronously(int pageSize)
        {
            Should.Throw<ArgumentOutOfRangeException>(() => Students(Total).ToPagesAsync(pageSize)).ParamName.ShouldBe("pageSize");
        }

        // ------------------------------------------------------------------ doubles

        // A page set that counts how many pages the consumer actually pulled, and whether the
        // enumerator it handed out was disposed. Both are the observable halves of "lazy" and
        // "released on break"; a real page set hides them behind its cache.
        private sealed class CountingPaginable : IPaginable<Student>
        {
            private readonly List<IPage<Student>> _pages;

            public CountingPaginable(int pageCount, int pageSize)
            {
                PageSize = pageSize;
                _pages = Enumerable.Range(1, pageCount)
                    .Select(n => Paginable.CreatePage(
                        Students(Total).Skip((n - 1) * pageSize).Take(pageSize), n, pageSize, Total))
                    .ToList();
            }

            public int PageSize { get; }

            public int MemberCount => Total;

            public int EnumeratedPages { get; private set; }

            public bool EnumeratorDisposed { get; private set; }

            public IPage<Student> GetPage(int pageNumber)
            {
                EnumeratedPages++;
                return _pages[pageNumber - 1];
            }

            public IEnumerator<IPage<Student>> GetEnumerator() => new TrackingEnumerator(this);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class TrackingEnumerator : IEnumerator<IPage<Student>>
            {
                private readonly CountingPaginable _owner;
                private readonly List<IPage<Student>> _pages;
                private int _index = -1;

                public TrackingEnumerator(CountingPaginable owner)
                {
                    _owner = owner;
                    _pages = owner._pages;
                }

                public IPage<Student> Current => _pages[_index];

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _index++;
                    if (_index >= _pages.Count)
                        return false;

                    _owner.EnumeratedPages++;
                    return true;
                }

                public void Reset() => _index = -1;

                public void Dispose() => _owner.EnumeratorDisposed = true;
            }
        }

        // A lazy (non-ICollection) source that counts its walks and steps, so a test can assert
        // the asynchronous shape introduces no extra traversal of the underlying sequence.
        private sealed class CountingSource : IEnumerable<int>
        {
            private readonly int _count;

            public CountingSource(int count) => _count = count;

            public int Walks { get; private set; }

            public int Steps { get; private set; }

            public IEnumerator<int> GetEnumerator()
            {
                Walks++;
                for (var i = 0; i < _count; i++)
                {
                    Steps++;
                    yield return i;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
