using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// Coverage for the Phase 2 API surface: <c>ToDictionary()</c>,
    /// <c>AsReadOnly()</c> and <c>ToString()</c> on both types.
    /// </summary>
    public class Phase2ApiTests
    {
        // ------------------------------------------------------------------
        // MultiList.ToDictionary
        // ------------------------------------------------------------------

        [Fact]
        public void MultiList_ToDictionary_MapsElementToCopyCount()
        {
            var bag = new MultiList<string>();
            bag.Add("a", 3);
            bag.Add("b", 1);

            var dict = bag.ToDictionary();
            dict["a"].ShouldBe(3);
            dict["b"].ShouldBe(1);
            dict.Count.ShouldBe(2);
        }

        [Fact]
        public void MultiList_ToDictionary_Snapshot_IndependentOfBag()
        {
            var bag = new MultiList<string> { "a" };
            var dict = bag.ToDictionary();

            bag.Add("a", 2);
            bag.Add("b");

            dict["a"].ShouldBe(1);
            dict.ContainsKey("b").ShouldBeFalse();
        }

        [Fact]
        public void MultiList_ToDictionary_RespectsComparer()
        {
            var bag = new MultiList<string>(StringComparer.OrdinalIgnoreCase);
            bag.Add("a");
            bag.Add("A");

            var dict = bag.ToDictionary();
            dict.Count.ShouldBe(1);
            dict["a"].ShouldBe(2);
        }

        [Fact]
        public void MultiList_ToDictionary_NullElement_ThrowsInvalidOperation()
        {
            var bag = new MultiList<string> { "a", null };
            Should.Throw<InvalidOperationException>(() => bag.ToDictionary());
        }

        [Fact]
        public void MultiList_ToDictionary_EmptyBag_ReturnsEmpty()
        {
            new MultiList<string>().ToDictionary().ShouldBeEmpty();
        }

        // ------------------------------------------------------------------
        // MultiList.AsReadOnly
        // ------------------------------------------------------------------

        [Fact]
        public void MultiList_AsReadOnly_LiveView_ReflectsChanges()
        {
            var bag = new MultiList<string> { "a" };
            var view = bag.AsReadOnly();

            bag.Add("a", 2);
            view.Count.ShouldBe(3);
            view.Count(x => x == "a").ShouldBe(3);

            bag.RemoveAllCopies("a");
            view.Count.ShouldBe(0);
        }

        [Fact]
        public void MultiList_AsReadOnly_Count_MatchesTotalCount()
        {
            var bag = new MultiList<string>();
            bag.Add("x", 4);
            bag.AsReadOnly().Count.ShouldBe(bag.TotalCount);
        }

        // ------------------------------------------------------------------
        // MultiList.ToString
        // ------------------------------------------------------------------

        [Fact]
        public void MultiList_ToString_ExpandedForm()
        {
            var bag = new MultiList<string>();
            bag.Add("a", 2);
            bag.Add("b");
            bag.ToString().ShouldBe("a,a,b");
        }

        [Fact]
        public void MultiList_ToString_Empty_ReturnsEmptyString()
        {
            new MultiList<string>().ToString().ShouldBe(string.Empty);
        }

        // ------------------------------------------------------------------
        // MultiDictionary.ToDictionary
        // ------------------------------------------------------------------

        [Fact]
        public void MultiDictionary_ToDictionary_MapsKeyToValueCollection()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("a", 1);
            map.Add("a", 2);
            map.Add("b", 3);

            var dict = map.ToDictionary();
            dict["a"].ShouldBe(new[] { 1, 2 });
            dict["b"].ShouldBe(new[] { 3 });
            dict.Count.ShouldBe(2);
        }

        [Fact]
        public void MultiDictionary_ToDictionary_OuterSnapshot_InnerLive()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("a", 1);
            var dict = map.ToDictionary();

            // New keys are not visible in the snapshot.
            map.Add("b", 2);
            dict.ContainsKey("b").ShouldBeFalse();

            // Values under an existing key are a live view.
            map.Add("a", 9);
            dict["a"].Count.ShouldBe(2);
        }

        [Fact]
        public void MultiDictionary_ToDictionary_Empty_ReturnsEmpty()
        {
            new MultiDictionary<string, int>().ToDictionary().ShouldBeEmpty();
        }

        // ------------------------------------------------------------------
        // MultiDictionary.AsReadOnly
        // ------------------------------------------------------------------

        [Fact]
        public void MultiDictionary_AsReadOnly_LiveView_ReflectsChanges()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("a", 1);
            var view = map.AsReadOnly();

            map.Add("a", 2);
            map.Add("b", 3);

            view.Count.ShouldBe(2);
            view["a"].Count.ShouldBe(2);
            view.ContainsKey("b").ShouldBeTrue();

            map.Remove("b");
            view.ContainsKey("b").ShouldBeFalse();
        }

        [Fact]
        public void MultiDictionary_AsReadOnly_ExposesNoMutatingMembers()
        {
            object view = new MultiDictionary<string, int>().AsReadOnly();

            // The view must not be castable back to the concrete (mutable) type,
            // nor to any ICollection-based mutable surface.
            (view is MultiDictionary<string, int>).ShouldBeFalse();
            (view is ICollection<KeyValuePair<string, IReadOnlyCollection<int>>>).ShouldBeFalse();
        }

        [Fact]
        public void MultiDictionary_AsReadOnly_TryGetValue_MissingKey_ReturnsFalse()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("a", 1);
            var view = map.AsReadOnly();
            view.TryGetValue("missing", out var value).ShouldBeFalse();
            value.ShouldBeNull();
        }

        // ------------------------------------------------------------------
        // MultiDictionary.ToString
        // ------------------------------------------------------------------

        [Fact]
        public void MultiDictionary_ToString_PerKeyExpandedForm()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("a", 1);
            map.Add("a", 2);
            map.Add("b", 3);
            map.ToString().ShouldBe("a:[1,2],b:[3]");
        }

        [Fact]
        public void MultiDictionary_ToString_Empty_ReturnsEmptyString()
        {
            new MultiDictionary<string, int>().ToString().ShouldBe(string.Empty);
        }
    }
}
