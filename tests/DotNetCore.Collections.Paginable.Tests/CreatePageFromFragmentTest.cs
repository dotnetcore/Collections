using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable.Tests.Models;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.Tests {
    // Issue #8 / P6-01: build a page from an already-materialized fragment plus paging
    // metadata. Cases are numbered U-01..U-22 to match the evaluation report
    // (reports/2026-09-10-Paginable-Issue8片段分页评估与6.1方案.md, section 5). A handful of
    // extra cases cover behaviour the report left open (short fragments, empty pages).
    public class CreatePageFromFragmentTest {

        // ---------------------------------------------------------------- helpers

        private static List<Student> Students(int count) =>
            Enumerable.Range(0, count)
                .Select(x => new Student { Id = x, Name = $"Student-{x}" })
                .ToList();

        private static List<Student> Fragment(List<Student> source, int pageNumber, int pageSize) =>
            source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        private static int[] Ids(IPage<Student> page) =>
            page.Select(m => m.Value.Id).ToArray();

        private static int[] ItemNumbers(IPage<Student> page) =>
            page.Select(m => m.ItemNumber).ToArray();

        private static void ShouldHaveSameMetadata(IPage expected, IPage actual) {
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

        // ------------------------------------------------- U-01 first page

        [Fact]
        public void U01_FirstPage() {
            var all = Students(12);
            var page = Paginable.CreatePage(Fragment(all, 1, 5), 1, 5, 12);

            page.TotalPageCount.ShouldBe(3);
            page.TotalMemberCount.ShouldBe(12);
            page.CurrentPageNumber.ShouldBe(1);
            page.PageSize.ShouldBe(5);
            page.CurrentPageSize.ShouldBe(5);
            page.HasPrevious.ShouldBeFalse();
            page.HasNext.ShouldBeTrue();

            Ids(page).ShouldBe(new[] { 0, 1, 2, 3, 4 });
            ItemNumbers(page).ShouldBe(new[] { 1, 2, 3, 4, 5 });
        }

        // ------------------------------------------------- U-02 middle page

        [Fact]
        public void U02_MiddlePage() {
            var all = Students(12);
            var page = Paginable.CreatePage(Fragment(all, 2, 5), 2, 5, 12);

            page.TotalPageCount.ShouldBe(3);
            page.CurrentPageSize.ShouldBe(5);
            page.HasPrevious.ShouldBeTrue();
            page.HasNext.ShouldBeTrue();

            Ids(page).ShouldBe(new[] { 5, 6, 7, 8, 9 });
            ItemNumbers(page).ShouldBe(new[] { 6, 7, 8, 9, 10 });
        }

        // ------------------------------------------------- U-03 short last page

        [Fact]
        public void U03_ShortLastPage() {
            var all = Students(12);
            var page = Paginable.CreatePage(Fragment(all, 3, 5), 3, 5, 12);

            page.TotalPageCount.ShouldBe(3);
            page.CurrentPageSize.ShouldBe(2);
            page.HasNext.ShouldBeFalse();
            page.IsLast().ShouldBeTrue();
            page.HasPrevious.ShouldBeTrue();

            Ids(page).ShouldBe(new[] { 10, 11 });
            ItemNumbers(page).ShouldBe(new[] { 11, 12 });
            page.ToMemberNumber().ShouldBe(12);
        }

        // ------------------------------------------------- U-04 evenly divisible last page

        [Fact]
        public void U04_EvenlyDivisibleLastPageIsNotEmpty() {
            // Guards the 6.0 CurrentPageSize fix: the last page of an evenly divisible
            // source must hold pageSize members, not zero.
            var all = Students(10);
            var page = Paginable.CreatePage(Fragment(all, 2, 5), 2, 5, 10);

            page.TotalPageCount.ShouldBe(2);
            page.CurrentPageSize.ShouldBe(5);
            page.HasNext.ShouldBeFalse();

            Ids(page).ShouldBe(new[] { 5, 6, 7, 8, 9 });
            ItemNumbers(page).ShouldBe(new[] { 6, 7, 8, 9, 10 });
        }

        // ------------------------------------------------- U-05 single page

        [Fact]
        public void U05_SinglePage() {
            var all = Students(3);
            var page = Paginable.CreatePage(Fragment(all, 1, 10), 1, 10, 3);

            page.TotalPageCount.ShouldBe(1);
            page.CurrentPageSize.ShouldBe(3);
            page.IsFirst().ShouldBeTrue();
            page.IsLast().ShouldBeTrue();
            page.FromMemberNumber().ShouldBe(1);
            page.ToMemberNumber().ShouldBe(3);
        }

        // ------------------------------------------------- U-06 empty source

        [Fact]
        public void U06_EmptySource() {
            var page = Paginable.CreatePage(new List<Student>(), 1, 5, 0);

            page.TotalPageCount.ShouldBe(1);
            page.GetMetadata().RealPageCount.ShouldBe(0);
            page.TotalMemberCount.ShouldBe(0);
            page.CurrentPageSize.ShouldBe(0);
            page.HasPrevious.ShouldBeFalse();
            page.HasNext.ShouldBeFalse();
            page.ToOriginalItems().ShouldBeEmpty();
            page.ToList().ShouldBeEmpty();
        }

        // ------------------------------------------------- U-07 both overloads agree

        [Fact]
        public void U07_MetadataOverloadEqualsArgumentOverload() {
            var all = Students(12);
            var metadata = new PageFragmentInfo(2, 5, 12);

            var fromArguments = Paginable.CreatePage(Fragment(all, 2, 5), 2, 5, 12);
            var fromMetadata = Paginable.CreatePage(Fragment(all, 2, 5), metadata);

            ShouldHaveSameMetadata(fromArguments, fromMetadata);
            Ids(fromMetadata).ShouldBe(Ids(fromArguments));
            ItemNumbers(fromMetadata).ShouldBe(ItemNumbers(fromArguments));
        }

        // ------------------------------------------------- U-08 metadata round trip

        [Fact]
        public void U08_MetadataRoundTrip() {
            var all = Students(12);
            var original = all.GetPage(3, 5);

            var info = PageFragmentInfo.FromMetadata(original.GetMetadata());
            info.PageNumber.ShouldBe(3);
            info.PageSize.ShouldBe(5);
            info.TotalMemberCount.ShouldBe(12);

            // The metadata object round trips on its own ...
            var asMetadata = info.ToMetadata();
            asMetadata.TotalPageCount.ShouldBe(original.GetMetadata().TotalPageCount);
            asMetadata.RealPageCount.ShouldBe(original.GetMetadata().RealPageCount);
            asMetadata.CurrentPageNumber.ShouldBe(3);
            asMetadata.CurrentPageSize.ShouldBe(2);
            asMetadata.HasPrevious.ShouldBeTrue();
            asMetadata.HasNext.ShouldBeFalse();
            asMetadata.ToString().ShouldBe(original.GetMetadata().ToString());

            // ... and so does the page rebuilt from it.
            var rebuilt = Paginable.CreatePage(original.ToOriginalItems(), info);
            ShouldHaveSameMetadata(original, rebuilt);
            Ids(rebuilt).ShouldBe(Ids(original));
            ItemNumbers(rebuilt).ShouldBe(ItemNumbers(original));
        }

        // ------------------------------------------------- U-09 rebuild from an existing page

        [Fact]
        public void U09_RebuildFromExistingPage() {
            var all = Students(12);
            var original = all.GetPage(2, 5);

            var rebuilt = Paginable.CreatePage(original.ToOriginalItems(), 2, 5, all.Count);

            ShouldHaveSameMetadata(original, rebuilt);
            Ids(rebuilt).ShouldBe(Ids(original));
            ItemNumbers(rebuilt).ShouldBe(ItemNumbers(original));
        }

        // ------------------------------------------------- U-10 page number past the last page

        [Fact]
        public void U10_PageOutOfRangeThrowsEagerly_BeforeTheFragmentIsTouched() {
            var touched = false;

            IEnumerable<Student> FragmentNotToBeRead() {
                touched = true;
                yield return new Student { Id = 0 };
            }

            Should.Throw<ArgumentOutOfRangeException>(() => Paginable.CreatePage(FragmentNotToBeRead(), 4, 5, 12));

            // Eager: the metadata is rejected while the page is built, not while it is enumerated.
            touched.ShouldBeFalse();
        }

        // ------------------------------------------------- U-11 / U-12 / U-13 invalid numbers

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void U11_PageNumberLessThanOneThrows(int pageNumber) {
            Should.Throw<ArgumentOutOfRangeException>(() => Paginable.CreatePage(new List<Student>(), pageNumber, 5, 12));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void U12_PageSizeLessThanOneThrows(int pageSize) {
            Should.Throw<ArgumentOutOfRangeException>(() => Paginable.CreatePage(new List<Student>(), 1, pageSize, 12));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void U13_NegativeTotalMemberCountThrows(int totalMemberCount) {
            Should.Throw<ArgumentOutOfRangeException>(() => Paginable.CreatePage(new List<Student>(), 1, 5, totalMemberCount));
        }

        [Fact]
        public void U13b_TotalMemberCountAboveMaxMemberItemsThrows() {
            var max = Math.Min(PaginableSettingsManager.Settings.MaxMemberItems, int.MaxValue - 1);

            Should.Throw<ArgumentOutOfRangeException>(
                () => Paginable.CreatePage(new List<Student>(), 1, 5, (int) max + 1));
        }

        // ------------------------------------------------- U-14 null arguments

        [Fact]
        public void U14_NullArgumentsThrow() {
            Should.Throw<ArgumentNullException>(() => Paginable.CreatePage<Student>(null, 1, 5, 12));
            Should.Throw<ArgumentNullException>(() => Paginable.CreatePage<Student>(null, new PageFragmentInfo(1, 5, 12)));
            Should.Throw<ArgumentNullException>(() => Paginable.CreatePage(new List<Student>(), (PageFragmentInfo) null));
            Should.Throw<ArgumentNullException>(() => PageFragmentInfo.FromMetadata(null));
            Should.Throw<ArgumentNullException>(() => ((IEnumerable<Student>) null).ToPage(1, 5, 12));
        }

        // ------------------------------------------------- U-15 fragment longer than the page size

        [Fact]
        public void U15_FragmentLongerThanPageSizeThrows() {
            var all = Students(12);
            var tooLong = all.Take(7).ToList();

            var exception = Should.Throw<ArgumentException>(() => Paginable.CreatePage(tooLong, 1, 5, 12));
            exception.GetType().ShouldBe(typeof(ArgumentException));
            exception.Message.ShouldContain("page size");
        }

        // ------------------------------------------------- U-16 fragment longer than the metadata says

        [Fact]
        public void U16_FragmentLongerThanTheMetadataSaysThrows() {
            // The last page of a 12/5 split holds 2 members, so 4 is one too many - yet still
            // within pageSize, which exercises the metadata-driven check rather than the cap.
            var all = Students(12);
            var tooLong = Fragment(all, 3, 5).Concat(all.Take(2)).ToList();
            tooLong.Count.ShouldBe(4);

            var exception = Should.Throw<ArgumentException>(() => Paginable.CreatePage(tooLong, 3, 5, 12));
            exception.GetType().ShouldBe(typeof(ArgumentException));
            exception.Message.ShouldContain("at most 2");
        }

        [Fact]
        public void U16b_FragmentLongerThanPageSizeOnAMiddlePageThrowsToo() {
            // The exact example from the report: page 2 of 12/5 holds 5 members, 6 were given.
            var all = Students(12);
            var tooLong = all.Take(6).ToList();

            Should.Throw<ArgumentException>(() => Paginable.CreatePage(tooLong, 2, 5, 12));
        }

        // ------------------------------------------------- U-17 materialization defends against mutation

        [Fact]
        public void U17_MutatingTheSourceListAfterwardsDoesNotChangeThePage() {
            var all = Students(12);
            var fragment = Fragment(all, 1, 5);

            var page = Paginable.CreatePage(fragment, 1, 5, 12);

            fragment.Clear();
            fragment.Add(new Student { Id = 999, Name = "intruder" });

            page.CurrentPageSize.ShouldBe(5);
            page.ToList().Count.ShouldBe(5);
            Ids(page).ShouldBe(new[] { 0, 1, 2, 3, 4 });
        }

        // ------------------------------------------------- U-18 the fragment is enumerated once

        [Fact]
        public void U18_LazyFragmentIsEnumeratedExactlyOnce() {
            var enumerations = 0;

            IEnumerable<Student> LazyFragment() {
                enumerations++;
                for (var i = 0; i < 5; i++) {
                    yield return new Student { Id = i };
                }
            }

            var page = Paginable.CreatePage(LazyFragment(), 1, 5, 12);

            page.ToList().Count.ShouldBe(5);
            enumerations.ShouldBe(1);
        }

        // ------------------------------------------------- U-19 equivalence with full-source paging

        [Theory]
        [InlineData(12, 5, 1)]
        [InlineData(12, 5, 2)]
        [InlineData(12, 5, 3)]
        [InlineData(10, 5, 2)]  // evenly divisible last page
        [InlineData(3, 10, 1)]  // single page
        [InlineData(20, 4, 4)]
        [InlineData(7, 7, 1)]
        public void U19_MatchesFullSourcePagination(int total, int pageSize, int pageNumber) {
            var all = Students(total);

            var fromFullSource = all.GetPage(pageNumber, pageSize);
            var fromFragment = Paginable.CreatePage(Fragment(all, pageNumber, pageSize), pageNumber, pageSize, total);

            ShouldHaveSameMetadata(fromFullSource, fromFragment);
            Ids(fromFragment).ShouldBe(Ids(fromFullSource));
            ItemNumbers(fromFragment).ShouldBe(ItemNumbers(fromFullSource));
            fromFragment.ToOriginalItems().Select(x => x.Id).ShouldBe(fromFullSource.ToOriginalItems().Select(x => x.Id));
        }

        // ------------------------------------------------- U-20 any fragment source shape

        [Fact]
        public void U20_EveryFragmentSourceShapeIsAccepted() {
            var all = Students(12);
            var baseline = Fragment(all, 2, 5);
            var expectedIds = baseline.Select(s => s.Id).OrderBy(x => x).ToArray();

            IEnumerable<IEnumerable<Student>> Shapes() {
                yield return new List<Student>(baseline);                        // List<T>
                yield return baseline.ToArray();                                 // array
                yield return new ReadOnlyCollection<Student>(baseline);           // IReadOnlyList<T>
                yield return new HashSet<Student>(baseline);                     // set (reference identity)
                yield return baseline.Where(_ => true);                          // LINQ chain
                yield return baseline.AsQueryable();                             // IQueryable<T>
                yield return Iterator(baseline);                                 // hand-written iterator
            }

            var shapes = Shapes().ToList();
            shapes.Count.ShouldBe(7);

            foreach (var shape in shapes) {
                var page = Paginable.CreatePage(shape, 2, 5, 12);

                page.TotalPageCount.ShouldBe(3);
                page.TotalMemberCount.ShouldBe(12);
                page.CurrentPageNumber.ShouldBe(2);
                page.CurrentPageSize.ShouldBe(5);
                page.ToList().Count.ShouldBe(5);

                // Order is only guaranteed for the ordered shapes, so compare as a set.
                page.Select(m => m.Value.Id).OrderBy(x => x).ShouldBe(expectedIds);
                page.Select(m => m.ItemNumber).ShouldBe(new[] { 6, 7, 8, 9, 10 });
            }
        }

        private static IEnumerable<Student> Iterator(IEnumerable<Student> source) {
            foreach (var item in source) {
                yield return item;
            }
        }

        // ------------------------------------------------- U-21 the extension method agrees

        [Fact]
        public void U21_ToPageExtensionEqualsTheFactory() {
            var all = Students(12);
            var fragment = Fragment(all, 2, 5);

            var viaExtension = fragment.ToPage(2, 5, 12);
            var viaFactory = Paginable.CreatePage(fragment, 2, 5, 12);

            ShouldHaveSameMetadata(viaExtension, viaFactory);
            Ids(viaExtension).ShouldBe(Ids(viaFactory));
            ItemNumbers(viaExtension).ShouldBe(ItemNumbers(viaFactory));
        }

        // ------------------------------------------------- U-22 concurrent enumeration

        [Fact]
        public void U22_ConcurrentEnumerationOfOnePageIsSafe() {
            var page = Paginable.CreatePage(Students(12).Take(5).ToList(), 1, 5, 12);
            var results = new ConcurrentBag<int[]>();

            Parallel.For(0, 100, _ => results.Add(Ids(page)));

            results.Count.ShouldBe(100);
            foreach (var ids in results) {
                ids.ShouldBe(new[] { 0, 1, 2, 3, 4 });
            }
        }

        // ------------------------------------------------- extra: behaviour the report left open

        [Fact]
        public void ShortFragmentIsToleratedAndMetadataWins() {
            // R-02: a fragment shorter than the metadata expects must not make the page
            // unbuildable (the upstream row may have been deleted between COUNT(*) and FETCH).
            // CurrentPageSize keeps reporting the metadata value; enumeration yields what
            // actually arrived.
            var all = Students(12);
            var shortFragment = Fragment(all, 2, 5).Take(3).ToList();

            var page = Paginable.CreatePage(shortFragment, 2, 5, 12);

            page.CurrentPageSize.ShouldBe(5);
            page.ToList().Count.ShouldBe(3);
            page.TotalMemberCount.ShouldBe(12);
            ItemNumbers(page).ShouldBe(new[] { 6, 7, 8 });
        }

        [Fact]
        public void CreateEmptyPageHasNoMembers() {
            var page = Paginable.CreateEmptyPage<Student>();

            page.TotalPageCount.ShouldBe(1);
            page.TotalMemberCount.ShouldBe(0);
            page.CurrentPageSize.ShouldBe(0);
            page.IsFirst().ShouldBeTrue();
            page.IsLast().ShouldBeTrue();
            page.ToOriginalItems().ShouldBeEmpty();
        }

        [Fact]
        public void PageFragmentInfoHandsBackWhatItWasGiven() {
            var info = new PageFragmentInfo(3, 5, 12);

            info.PageNumber.ShouldBe(3);
            info.PageSize.ShouldBe(5);
            info.TotalMemberCount.ShouldBe(12);
        }
    }
}
