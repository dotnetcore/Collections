using DotNetCore.Collections.Paginable.Internal;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.Tests
{
    /// <summary>
    /// Pins the values shown in the <c>PaginableCalc</c> XML examples (P6-02).
    /// <para>
    /// The examples used to carry three arguments for a two-argument method, so the sample
    /// IntelliSense rendered could not compile. These tests hold the numbers the corrected
    /// examples claim, so a future edit to either the code or the docs has to keep them in step.
    /// </para>
    /// </summary>
    public class PaginableCalcTest
    {
        [Fact]
        public void LimiterBelowSourceCountWins()
        {
            // <example>: GetRealMemberCount(50, 120) is 50, not 120
            PaginableCalc.GetRealMemberCount(50, 120).ShouldBe(50);
        }

        [Fact]
        public void NullLimiterKeepsTheSourceCount()
        {
            // <example>: null means unlimited
            PaginableCalc.GetRealMemberCount(null, 120).ShouldBe(120);
        }

        [Fact]
        public void LimiterAboveSourceCountIsClampedToTheSourceCount()
        {
            // the limiter can only ever remove members, never conjure them
            PaginableCalc.GetRealMemberCount(500, 120).ShouldBe(120);
        }

        [Fact]
        public void PageCountChainsOntoTheLimitedMemberCount()
        {
            // <example>: 50 members at 5 per page is 10 pages
            PaginableCalc.GetRealPageCount(50, 5).ShouldBe(10);
        }

        [Fact]
        public void PartialLastPageStillCountsAsAPage()
        {
            // <example>: 120 members at 50 per page is 3 pages (100 + 20), not 2
            PaginableCalc.GetRealPageCount(120, 50).ShouldBe(3);
        }

        [Fact]
        public void TheTwoExamplesAreACoherentChain()
        {
            // GetRealPageCount's parameter is documented as "may have been limited by
            // GetRealMemberCount", so feeding one into the other must be meaningful.
            var realMemberCount = PaginableCalc.GetRealMemberCount(50, 120);
            PaginableCalc.GetRealPageCount(realMemberCount, 5).ShouldBe(10);
        }
    }
}
