using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.Tests
{
    /// F6-11: PageCreationOptions.Strict turns the 6.1 short-fragment tolerance into an
    /// ArgumentException, while every default entry point keeps the lenient behaviour.
    public class PageCreationOptionsStrictTest
    {
        // ------------------------------------------------------------------
        // default entries stay lenient (6.1 behaviour untouched)
        // ------------------------------------------------------------------

        [Fact]
        public void DefaultOverloadToleratesShortFragment()
        {
            var page = Paginable.CreatePage(new[] { 11 }, pageNumber: 3, pageSize: 5, totalMemberCount: 12);

            page.CurrentPageSize.ShouldBe(2);
            page.TotalMemberCount.ShouldBe(12);
        }

        [Fact]
        public void MetadataOverloadToleratesShortFragment()
        {
            var info = new PageFragmentInfo(1, 5, 10);

            var page = Paginable.CreatePage(new int[0], info);

            // CurrentPageSize keeps reporting the metadata value
            page.CurrentPageSize.ShouldBe(5);
            page.GetMetadata().TotalMemberCount.ShouldBe(10);
        }

        [Fact]
        public void ExplicitLenientMatchesDefaultBehaviour()
        {
            var info = new PageFragmentInfo(1, 5, 10);

            var page = Paginable.CreatePage(new int[0], info, PageCreationOptions.Lenient);

            page.CurrentPageSize.ShouldBe(5);
        }

        // ------------------------------------------------------------------
        // strict mode
        // ------------------------------------------------------------------

        [Fact]
        public void StrictAcceptsFragmentOfExactExpectedLength()
        {
            // last page: expected = 12 - 2 * 5 = 2
            var page = Paginable.CreatePage(new[] { 11, 12 }, pageNumber: 3, pageSize: 5, totalMemberCount: 12, PageCreationOptions.Strict);

            page.CurrentPageSize.ShouldBe(2);
        }

        [Fact]
        public void StrictAcceptsFragmentOfFullPageSize()
        {
            var page = Paginable.CreatePage(new[] { 1, 2, 3, 4, 5 }, pageNumber: 1, pageSize: 5, totalMemberCount: 10, PageCreationOptions.Strict);

            page.CurrentPageSize.ShouldBe(5);
        }

        [Fact]
        public void StrictThrowsWhenFragmentIsShorterThanExpected()
        {
            // expected 5, fragment carries 3
            var ex = Should.Throw<ArgumentException>(() =>
                Paginable.CreatePage(new[] { 1, 2, 3 }, pageNumber: 1, pageSize: 5, totalMemberCount: 10, PageCreationOptions.Strict));

            ex.ParamName.ShouldBe("fragment");
            ex.Message.ShouldContain("Strict mode");
        }

        [Fact]
        public void StrictThrowsOnEmptyFragmentWhenPageShouldHoldMembers()
        {
            Should.Throw<ArgumentException>(() =>
                Paginable.CreatePage(new int[0], pageNumber: 1, pageSize: 5, totalMemberCount: 10, PageCreationOptions.Strict));
        }

        [Fact]
        public void StrictThrowsThroughTheMetadataOverload()
        {
            var info = new PageFragmentInfo(1, 5, 10);

            Should.Throw<ArgumentException>(() =>
                Paginable.CreatePage(new[] { 1, 2, 3 }, info, PageCreationOptions.Strict));
        }

        [Fact]
        public void StrictThrowsThroughTheToPageSugar()
        {
            Should.Throw<ArgumentException>(() =>
                new[] { 1, 2, 3 }.ToPage(pageNumber: 1, pageSize: 5, totalMemberCount: 10, PageCreationOptions.Strict));

            // and the exact-length fragment passes
            var page = new[] { 1, 2, 3, 4, 5 }.ToPage(pageNumber: 1, pageSize: 5, totalMemberCount: 10, PageCreationOptions.Strict);
            page.CurrentPageSize.ShouldBe(5);
        }

        [Fact]
        public void StrictStillRejectsAnOverLongFragment()
        {
            // 6 members for a page size of 5: rejected in both modes
            Should.Throw<ArgumentException>(() =>
                Paginable.CreatePage(new[] { 1, 2, 3, 4, 5, 6 }, pageNumber: 1, pageSize: 5, totalMemberCount: 20, PageCreationOptions.Strict));
        }

        [Fact]
        public void StrictDoesNotFireWhenExpectedIsZero()
        {
            // a genuinely empty source: expected = 0, an empty fragment matches it exactly
            var page = Paginable.CreatePage(new int[0], pageNumber: 1, pageSize: 5, totalMemberCount: 0, PageCreationOptions.Strict);

            page.CurrentPageSize.ShouldBe(0);
        }

        // ------------------------------------------------------------------
        // argument checking
        // ------------------------------------------------------------------

        [Fact]
        public void NullOptionsThrow()
        {
            Should.Throw<ArgumentNullException>(() =>
                Paginable.CreatePage(new[] { 1 }, new PageFragmentInfo(1, 5, 10), null!));

            Should.Throw<ArgumentNullException>(() =>
                Paginable.CreatePage(new[] { 1 }, 1, 5, 10, null!));

            Should.Throw<ArgumentNullException>(() =>
                new[] { 1 }.ToPage(1, 5, 10, null!));
        }
    }
}
