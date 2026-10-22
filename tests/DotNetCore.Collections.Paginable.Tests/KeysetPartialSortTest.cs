using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.Tests
{
    /// <summary>
    /// F7-09: the in-memory keyset overloads order with the engine's <c>PartialSortBy</c> instead
    /// of a full <c>OrderBy</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="KeysetPaginationTest"/> already covers traversal, ordering direction, empty
    /// sources, tail pages and the beyond-the-last-key error, so it is the regression net for the
    /// swap itself. What it cannot cover is the one property the swap changes: a partial sort is
    /// not stable. These tests therefore pin two things - that the result is indistinguishable from
    /// a stable sort while the keys are unique (the case keyset pagination actually requires), and
    /// that when keys do tie, the page still selects the right <em>keys</em> even though the order
    /// among equals is not specified.
    /// </para>
    /// </remarks>
    public class KeysetPartialSortTest
    {
        private const int Total = 50;
        private const int PageSize = 10;

        [Fact]
        public void FirstPage_MatchesAStableSort_WhenTheKeysAreUnique()
        {
            var source = Rows(Total);

            var page = source.GetFirstPageByKeyset(x => x.Rank, PageSize);
            var expected = source.OrderBy(x => x.Rank).Take(PageSize).ToList();

            page.Members.ShouldBe(expected);
            page.HasNext.ShouldBeTrue();
            page.IsFirstPage.ShouldBeTrue();
        }

        [Fact]
        public void FirstPage_MatchesAStableSort_WhenDescending()
        {
            var source = Rows(Total);

            var page = source.GetFirstPageByKeyset(x => x.Rank, PageSize, descending: true);
            var expected = source.OrderByDescending(x => x.Rank).Take(PageSize).ToList();

            page.Members.ShouldBe(expected);
            page.HasNext.ShouldBeTrue();
        }

        [Fact]
        public void PageAfterAnAnchor_MatchesAStableSort_WhenTheKeysAreUnique()
        {
            var source = Rows(Total);
            const int anchor = 20;

            var page = source.GetPageByKeyset(x => x.Rank, anchor, PageSize);
            var expected = source
                .Where(x => x.Rank > anchor)
                .OrderBy(x => x.Rank)
                .Take(PageSize)
                .ToList();

            page.Members.ShouldBe(expected);
            page.IsFirstPage.ShouldBeFalse();
            page.HasNext.ShouldBeTrue();
        }

        [Fact]
        public void PageAfterAnAnchor_MatchesAStableSort_WhenDescending()
        {
            var source = Rows(Total);
            const int anchor = 30;

            var page = source.GetPageByKeyset(x => x.Rank, anchor, PageSize, descending: true);
            var expected = source
                .Where(x => x.Rank < anchor)
                .OrderByDescending(x => x.Rank)
                .Take(PageSize)
                .ToList();

            page.Members.ShouldBe(expected);
        }

        [Fact]
        public void TheLastPage_ReportsNoNextPage()
        {
            var source = Rows(Total);

            var page = source.GetFirstPageByKeyset(x => x.Rank, Total);

            page.Members.Count.ShouldBe(Total);
            page.HasNext.ShouldBeFalse();
        }

        [Fact]
        public void APageSizeThatExactlyFits_ReportsNoNextPage()
        {
            var source = Rows(PageSize);

            var page = source.GetFirstPageByKeyset(x => x.Rank, PageSize);

            // The extra member is what signals HasNext; with nothing left over there is none.
            page.Members.Count.ShouldBe(PageSize);
            page.HasNext.ShouldBeFalse();
        }

        [Fact]
        public void AnEmptySource_YieldsAnEmptyPage()
        {
            var page = Rows(0).GetFirstPageByKeyset(x => x.Rank, PageSize);

            page.Members.ShouldBeEmpty();
            page.HasNext.ShouldBeFalse();
        }

        // ------------------------------------------------------------------
        // The deviation: a partial sort is not stable
        // ------------------------------------------------------------------

        [Fact]
        public void TiedKeys_SelectTheSameKeysAsAStableSort()
        {
            // Ten rows share five ranks, so the page boundary falls inside a run of equal keys.
            var source = TiedRows();

            var page = source.GetFirstPageByKeyset(x => x.Rank, 4);
            var expected = source.OrderBy(x => x.Rank).Take(4).ToList();

            // Which member of a tied run survives the cut is unspecified, so the contract is about
            // the keys: the page must carry exactly the keys a stable sort would have put there.
            page.Members.Select(x => x.Rank).ShouldBe(expected.Select(x => x.Rank));
            page.Members.Count.ShouldBe(4);
        }

        [Fact]
        public void TiedKeys_StillVisitEveryMember_WhenThePageSizeMatchesTheTieRuns()
        {
            // Ranks arrive in runs of two and the page size is two, so every page boundary lands
            // between runs. The instability of the partial sort cannot show here: whichever member
            // of a run survives the cut, the anchor is still the same rank.
            var source = TiedRows();
            var seen = new List<int>();

            var page = source.GetFirstPageByKeyset(x => x.Rank, 2);
            while (true)
            {
                seen.AddRange(page.Members.Select(x => x.Id));
                if (!page.HasNext)
                {
                    break;
                }

                page = source.GetPageByKeyset(x => x.Rank, page.LastMember.Rank, 2);
            }

            seen.Count.ShouldBe(source.Count);
            seen.Distinct().Count().ShouldBe(source.Count);
            seen.OrderBy(x => x).ShouldBe(source.Select(x => x.Id).OrderBy(x => x));
        }

        [Fact]
        public void TiedKeys_LoseTheRestOfATieWhenAPageCutsThroughIt()
        {
            // The anchor filter is strictly greater, so a page boundary that falls inside a run of
            // equal keys leaves the rest of that run unreachable. That is the pre-existing keyset
            // contract - the reason the key is required to be unique-ish - and the partial sort
            // neither causes nor worsens it. Recorded here so the behaviour stays deliberate.
            var source = TiedRows();

            var page = source.GetFirstPageByKeyset(x => x.Rank, 3);
            page.Members.Select(x => x.Rank).ShouldBe(new[] { 1, 1, 2 });

            var next = source.GetPageByKeyset(x => x.Rank, page.LastMember.Rank, 3);

            // Rank 2's remaining member is skipped, exactly as it was before the sort changed.
            next.Members.Select(x => x.Rank).ShouldBe(new[] { 3, 3, 4 });
        }

        private static List<Row> Rows(int count)
            => Enumerable.Range(1, count).Select(x => new Row(x, x)).ToList();

        private static List<Row> TiedRows()
        {
            // Ranks: 1,1,2,2,3,3,4,4,5,5 - every run is a tie, ids stay unique.
            return Enumerable.Range(0, 10)
                .Select(x => new Row(x + 1, (x / 2) + 1))
                .ToList();
        }

        private sealed class Row
        {
            internal Row(int id, int rank)
            {
                Id = id;
                Rank = rank;
            }

            internal int Id { get; }

            internal int Rank { get; }
        }
    }
}
