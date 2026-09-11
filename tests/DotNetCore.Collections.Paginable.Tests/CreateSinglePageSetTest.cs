using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable.Tests.Models;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.Tests
{
    // P6-03: a single-page IPaginable<T> wrapper, for callers whose signature wants the set
    // shape (or IEnumerable<IPage<T>>) while the data in hand is one already-assembled page.
    public class CreateSinglePageSetTest
    {
        // ---------------------------------------------------------------- helpers

        private const int Total = 12;
        private const int PageSize = 5;

        private static List<Student> Students(int count) =>
            Enumerable.Range(0, count)
                .Select(x => new Student { Id = x, Name = $"Student-{x}" })
                .ToList();

        private static List<Student> Fragment(List<Student> source, int pageNumber, int pageSize) =>
            source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        /// <summary>One real page out of the 12-member source - never the whole source.</summary>
        private static List<Student> Page(int pageNumber) =>
            Fragment(Students(Total), pageNumber, PageSize);

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

        // ------------------------------------------------------------ the set shape

        [Fact]
        public void SinglePageFragmentReportsOnePage()
        {
            var all = Students(3);
            var set = Paginable.CreateSinglePageSet(all, pageNumber: 1, pageSize: 10, totalMemberCount: 3);

            set.PageCount.ShouldBe(1);
            set.PageSize.ShouldBe(10);
            set.MemberCount.ShouldBe(3);
        }

        [Fact]
        public void PageCountStaysOneEvenWhenTheWrappedPageIsAMiddlePage()
        {
            // The deliberate divergence: the set holds one page, so it can only ever serve one
            // page, while the page itself keeps the source-wide numbering.
            var all = Students(12);
            var set = Paginable.CreateSinglePageSet(Fragment(all, 3, 5), pageNumber: 3, pageSize: 5, totalMemberCount: 12);

            set.PageCount.ShouldBe(1);

            var page = set.GetPage(1);
            page.TotalPageCount.ShouldBe(3);      // the source really does have three pages
            page.CurrentPageNumber.ShouldBe(3);   // and this page is the third of them
            page.CurrentPageSize.ShouldBe(2);
            page.HasNext.ShouldBeFalse();
        }

        [Fact]
        public void TheSetIsStillAnIPaginableOfT()
        {
            var set = Paginable.CreateSinglePageSet(Students(3), 1, 10, 3);

            IPaginable<Student> asInterface = set;
            IEnumerable<IPage<Student>> asPages = set;

            asInterface.PageSize.ShouldBe(10);
            asInterface.MemberCount.ShouldBe(3);
            asPages.Count().ShouldBe(1);
        }

        // --------------------------------------------------- parity with CreatePage

        [Fact]
        public void TheWrappedPageIsExactlyWhatCreatePageWouldBuild()
        {
            var all = Students(12);
            var fragment = Fragment(all, 2, 5);

            var expected = Paginable.CreatePage(fragment, 2, 5, 12);
            var actual = Paginable.CreateSinglePageSet(fragment, 2, 5, 12).GetPage(1);

            ShouldHaveSameMetadata(expected, actual);
            Ids(actual).ShouldBe(Ids(expected));
            actual.Select(m => m.ItemNumber).ShouldBe(expected.Select(m => m.ItemNumber));
        }

        [Fact]
        public void TheMetadataOverloadMatchesTheFourArgumentOverload()
        {
            var all = Students(12);
            var fragment = Fragment(all, 2, 5);
            var info = new PageFragmentInfo(pageNumber: 2, pageSize: 5, totalMemberCount: 12);

            var byInfo = Paginable.CreateSinglePageSet(fragment, info);
            var byNumbers = Paginable.CreateSinglePageSet(fragment, 2, 5, 12);

            ShouldHaveSameMetadata(byInfo.GetPage(1), byNumbers.GetPage(1));
            Ids(byInfo.GetPage(1)).ShouldBe(Ids(byNumbers.GetPage(1)));
        }

        [Fact]
        public void MetadataRoundTripFromAnExistingPageStillWorks()
        {
            var all = Students(12);
            var sourcePage = all.GetPage(2, 5);

            var info = PageFragmentInfo.FromMetadata(sourcePage.GetMetadata());
            var rebuilt = Paginable.CreateSinglePageSet(sourcePage.ToOriginalItems(), info).GetPage(1);

            ShouldHaveSameMetadata(sourcePage, rebuilt);
            Ids(rebuilt).ShouldBe(Ids(sourcePage));
        }

        // ------------------------------------------------------------------ GetPage

        [Fact]
        public void GetPageOneIsStableAcrossCalls()
        {
            var set = Paginable.CreateSinglePageSet(Page(1), 1, PageSize, Total);

            var first = set.GetPage(1);
            var second = set.GetPage(1);

            first.ShouldBeSameAs(second);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(2)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        public void AnyPageNumberOtherThanOneThrows(int pageNumber)
        {
            var set = Paginable.CreateSinglePageSet(Page(3), 3, PageSize, Total);

            var ex = Should.Throw<ArgumentOutOfRangeException>(() => set.GetPage(pageNumber));
            ex.ParamName.ShouldBe("pageNumber");
        }

        // ------------------------------------------------------------- enumeration

        [Fact]
        public void EnumerationYieldsTheSinglePageExactlyOnce()
        {
            var set = Paginable.CreateSinglePageSet(Page(2), 2, PageSize, Total);

            var pages = set.ToList();

            pages.Count.ShouldBe(1);
            pages[0].ShouldBeSameAs(set.GetPage(1));

            // PageCount and enumeration must agree - that is why PageCount can not be the
            // page's own TotalPageCount.
            pages.Count.ShouldBe(set.PageCount);
        }

        [Fact]
        public void NonGenericEnumerationYieldsTheSinglePageExactlyOnce()
        {
            System.Collections.IEnumerable set = Paginable.CreateSinglePageSet(Page(2), 2, PageSize, Total);

            var count = 0;
            foreach (var _ in set)
            {
                count++;
            }

            count.ShouldBe(1);
        }

        // ------------------------------------------------------------------ edges

        [Fact]
        public void EmptyFragmentStillYieldsOneEmptyPage()
        {
            var set = Paginable.CreateSinglePageSet(new List<Student>(), 1, 5, 0);

            set.PageCount.ShouldBe(1);
            set.MemberCount.ShouldBe(0);

            var page = set.GetPage(1);
            page.CurrentPageSize.ShouldBe(0);
            page.ToOriginalItems().ShouldBeEmpty();
        }

        [Fact]
        public void LenientKeepsAShortFragmentBuildable()
        {
            var all = Students(12);
            var shortFragment = Fragment(all, 3, 5).Take(1).ToList();   // metadata says 2

            var set = Paginable.CreateSinglePageSet(shortFragment, 3, 5, 12);

            set.GetPage(1).CurrentPageSize.ShouldBe(2);   // the metadata value, as documented
            set.GetPage(1).ToOriginalItems().Count().ShouldBe(1);
        }

        [Fact]
        public void StrictRejectsAShortFragment()
        {
            var all = Students(12);
            var shortFragment = Fragment(all, 3, 5).Take(1).ToList();

            var ex = Should.Throw<ArgumentException>(
                () => Paginable.CreateSinglePageSet(shortFragment, 3, 5, 12, PageCreationOptions.Strict));
            ex.ParamName.ShouldBe("fragment");
        }

        [Fact]
        public void StrictAcceptsAnExactFragment()
        {
            var all = Students(12);
            var set = Paginable.CreateSinglePageSet(
                Fragment(all, 3, 5), 3, 5, 12, PageCreationOptions.Strict);

            set.PageCount.ShouldBe(1);
            Ids(set.GetPage(1)).ShouldBe(new[] { 10, 11 });
        }

        // ------------------------------------------------------------ null guards

        [Fact]
        public void NullFragmentThrowsOnEveryOverload()
        {
            List<Student> fragment = null;
            var info = new PageFragmentInfo(1, 5, 12);

            Should.Throw<ArgumentNullException>(() => Paginable.CreateSinglePageSet(fragment, info)).ParamName.ShouldBe("fragment");
            Should.Throw<ArgumentNullException>(() => Paginable.CreateSinglePageSet(fragment, info, PageCreationOptions.Strict)).ParamName.ShouldBe("fragment");
            Should.Throw<ArgumentNullException>(() => Paginable.CreateSinglePageSet(fragment, 1, 5, 12)).ParamName.ShouldBe("fragment");
            Should.Throw<ArgumentNullException>(() => Paginable.CreateSinglePageSet(fragment, 1, 5, 12, PageCreationOptions.Strict)).ParamName.ShouldBe("fragment");
        }

        [Fact]
        public void NullMetadataThrows()
        {
            PageFragmentInfo info = null;

            // the empty fragment keeps this case independent of fragment validation
            Should.Throw<ArgumentNullException>(() => Paginable.CreateSinglePageSet(new List<Student>(), info)).ParamName.ShouldBe("metadata");
        }

        [Fact]
        public void NullOptionsThrows()
        {
            var fragment = Students(12);
            var info = new PageFragmentInfo(1, 5, 12);
            PageCreationOptions options = null;

            Should.Throw<ArgumentNullException>(() => Paginable.CreateSinglePageSet(fragment, info, options)).ParamName.ShouldBe("options");
            Should.Throw<ArgumentNullException>(() => Paginable.CreateSinglePageSet(fragment, 1, 5, 12, options)).ParamName.ShouldBe("options");
        }

        // ---------------------------------------------------------- concurrency

        [Fact]
        public async Task ParallelGetPageAndEnumerationAreStable()
        {
            var set = Paginable.CreateSinglePageSet(Page(2), 2, PageSize, Total);
            var expected = Ids(set.GetPage(1));

            var results = await Task.WhenAll(Enumerable.Range(0, 100).Select(_ => Task.Run(() =>
            {
                var byGetter = Ids(set.GetPage(1));
                var byEnumeration = Ids(set.Single());
                return (byGetter, byEnumeration);
            })));

            foreach (var (byGetter, byEnumeration) in results)
            {
                byGetter.ShouldBe(expected);
                byEnumeration.ShouldBe(expected);
            }
        }
    }
}
