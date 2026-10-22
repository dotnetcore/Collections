using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.Tests
{
    /// <summary>
    /// F7-09: the <see cref="PaginableSettings.MemoizeLazySources"/> switch.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A paginable built from a lazy sequence walks that sequence once for the member count and
    /// then once more per page, each time from the beginning. The switch makes all of those walks
    /// share the engine's <c>Memoize</c> cache. The tests here pin the claim in the only unit that
    /// actually matters - how many times the source is pulled - rather than in elapsed time, and
    /// then pin the two guards (materialised collections and queryables stay out of the cache) via
    /// the snapshot semantics the cache introduces.
    /// </para>
    /// <para>
    /// The switch is a process-wide setting, so every test restores the previous snapshot in a
    /// <c>finally</c> and this class shares a collection with
    /// <see cref="PaginableSettingsTest"/> so the two cannot interleave.
    /// </para>
    /// </remarks>
    [Collection("PaginableSettings")]
    public class MemoizeLazySourceTest
    {
        private const int Total = 20;
        private const int PageSize = 5;

        [Fact]
        public void MemoizeLazySources_IsOffByDefault()
        {
            new PaginableSettings().MemoizeLazySources.ShouldBeFalse();
        }

        [Fact]
        public void WithTheSwitchOff_EveryPageWalksTheSourceAgain()
        {
            WithSettings(memoize: false, () =>
            {
                var source = new CountingSource<int>(Enumerable.Range(1, Total));
                var paginable = source.ToPaginable(pageSize: PageSize);

                // The member-count pass is one full walk, and it happens eagerly.
                source.PullCount.ShouldBe(Total);

                for (var pageNumber = 1; pageNumber <= paginable.PageCount; pageNumber++)
                {
                    _ = paginable.GetPage(pageNumber).ToOriginalItems().ToList();
                }

                // Four more walks: each page restarts at the beginning of the source.
                source.PullCount.ShouldBeGreaterThan(Total);
            });
        }

        [Fact]
        public void WithTheSwitchOn_EveryPageReplaysOneWalk()
        {
            WithSettings(memoize: true, () =>
            {
                var source = new CountingSource<int>(Enumerable.Range(1, Total));
                var paginable = source.ToPaginable(pageSize: PageSize);

                for (var pageNumber = 1; pageNumber <= paginable.PageCount; pageNumber++)
                {
                    _ = paginable.GetPage(pageNumber).ToOriginalItems().ToList();
                }

                // The count pass filled the cache; every page replays it and never touches the
                // source again.
                source.PullCount.ShouldBe(Total);
            });
        }

        [Fact]
        public void WithTheSwitchOn_ThePagesHoldTheSameMembers()
        {
            var expected = Enumerable.Range(1, Total).ToList();

            WithSettings(memoize: true, () =>
            {
                var paginable = new CountingSource<int>(expected).ToPaginable(pageSize: PageSize);

                var actual = new List<int>();
                for (var pageNumber = 1; pageNumber <= paginable.PageCount; pageNumber++)
                {
                    actual.AddRange(paginable.GetPage(pageNumber).ToOriginalItems());
                }

                actual.ShouldBe(expected);
                paginable.PageCount.ShouldBe(4);
                paginable.MemberCount.ShouldBe(Total);
            });
        }

        [Fact]
        public void WithTheSwitchOff_TheSourceIsWalkedAgainOnEveryPage()
        {
            var items = Enumerable.Range(1, 10).ToList();
            var lazy = items.Select(x => x);

            WithSettings(memoize: false, () =>
            {
                var paginable = lazy.ToPaginable(pageSize: 5);

                items[0] = 99;

                paginable.GetPage(1).ToOriginalItems().First().ShouldBe(99);
            });
        }

        [Fact]
        public void WithTheSwitchOn_TheSourceIsSnapshottedAtTheFirstWalk()
        {
            var items = Enumerable.Range(1, 10).ToList();
            var lazy = items.Select(x => x);

            WithSettings(memoize: true, () =>
            {
                var paginable = lazy.ToPaginable(pageSize: 5);

                items[0] = 99;

                // The cache was filled by the count pass, so the page replays the value the source
                // held at that moment rather than the current one.
                paginable.GetPage(1).ToOriginalItems().First().ShouldBe(1);
            });
        }

        [Fact]
        public void WithTheSwitchOn_AMaterialisedCollectionIsNotSnapshotted()
        {
            var items = Enumerable.Range(1, 10).ToList();

            WithSettings(memoize: true, () =>
            {
                var paginable = items.ToPaginable(pageSize: 5);

                items[0] = 99;

                // List<T> is an ICollection<T>: it re-enumerates cheaply and counts in O(1), so the
                // switch deliberately leaves it alone and the page sees the edit.
                paginable.GetPage(1).ToOriginalItems().First().ShouldBe(99);
            });
        }

        [Fact]
        public void WithTheSwitchOn_AQueryableTypedAsEnumerableIsNotSnapshotted()
        {
            var items = Enumerable.Range(1, 10).ToList();
            var queryable = items.AsQueryable();

            WithSettings(memoize: true, () =>
            {
                // Deliberately reached through the IEnumerable entry point, which is the only way a
                // queryable can get as far as the memoization decision - the IQueryable entry point
                // builds a PaginableQueryable and never consults the switch.
                var paginable = ((IEnumerable<int>)queryable).ToPaginable(pageSize: 5);

                items[0] = 99;

                // Memoizing a queryable would evaluate the whole result set on the client and throw
                // away the provider's Skip/Take, so it is excluded by shape.
                paginable.GetPage(1).ToOriginalItems().First().ShouldBe(99);
            });
        }

        [Fact]
        public void WithTheSwitchOn_AMaterialisedSourcePaysNoCache()
        {
            // The allocation side of the same guard: if the ICollection<T> exclusion ever stopped
            // working, this path would start paying for a cache of 500 references - several
            // kilobytes - on every call. The tolerance only absorbs warm-up noise between the two
            // measurement runs, not a cache.
            var items = Enumerable.Range(1, 500).ToList();

            var without = MeasurePageAllocations(items, memoize: false);
            var with = MeasurePageAllocations(items, memoize: true);

            with.ShouldBeLessThanOrEqualTo(without + 64);
        }

        private static long MeasurePageAllocations(List<int> items, bool memoize)
        {
            long perCall = 0;

            WithSettings(memoize, () =>
            {
                // Warm-up: let the JIT and the lazily-initialised comparer statics settle, so they
                // are not charged to the measured window.
                for (var i = 0; i < 3; i++)
                {
                    _ = items.ToPaginable(pageSize: 50).GetPage(1).ToOriginalItems().ToList();
                }

                const int iterations = 100;
                var before = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < iterations; i++)
                {
                    _ = items.ToPaginable(pageSize: 50).GetPage(1).ToOriginalItems().ToList();
                }

                perCall = (GC.GetAllocatedBytesForCurrentThread() - before) / iterations;
            });

            return perCall;
        }

        private static void WithSettings(bool memoize, Action action)
        {
            var original = PaginableSettingsManager.Settings;

            try
            {
                PaginableSettingsManager.UpdateSettings(new PaginableSettings
                {
                    DefaultPageSize = original.DefaultPageSize,
                    MaxMemberItems = original.MaxMemberItems,
                    MemoizeLazySources = memoize,
                });

                action();
            }
            finally
            {
                PaginableSettingsManager.UpdateSettings(original);
            }
        }

        /// <summary>
        /// An <see cref="IEnumerable{T}"/> that counts how many elements have been pulled out of it,
        /// so that "how many times was the source walked" becomes an assertion rather than a guess.
        /// It is deliberately not an <see cref="ICollection{T}"/>, which is what puts it on the lazy
        /// path the switch governs.
        /// </summary>
        private sealed class CountingSource<T> : IEnumerable<T>
        {
            private readonly List<T> _items;
            private int _pullCount;

            internal CountingSource(IEnumerable<T> items)
            {
                _items = items.ToList();
            }

            /// <summary>
            /// The number of elements this source has yielded so far, across every enumerator it
            /// has handed out.
            /// </summary>
            internal int PullCount => _pullCount;

            public IEnumerator<T> GetEnumerator()
            {
                foreach (var item in _items)
                {
                    _pullCount++;
                    yield return item;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
