using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// Additional coverage for paths not exercised by the other suites:
    /// ToString with null elements, capacity constructor, re-use after Clear,
    /// comparer propagation through Clone, lookup group enumeration and
    /// flattened Values semantics.
    /// </summary>
    public class AdditionalCoverageTests
    {
        // ------------------------------------------------------------------
        // MultiList
        // ------------------------------------------------------------------

        [Fact]
        public void MultiList_ToString_NullElement_RendersEmptyPosition()
        {
            var bag = new MultiList<string>();
            bag.Add("a");
            bag.Add(null);
            // The null bucket is enumerated before the dictionary keys.
            bag.ToString().ShouldBe(",a");
        }

        [Fact]
        public void MultiList_CapacityConstructor_AcceptsElements()
        {
            var bag = new MultiList<string>(16);
            bag.Add("a");
            bag.CountOf("a").ShouldBe(1);
            bag.DistinctCount.ShouldBe(1);
        }

        [Fact]
        public void MultiList_AddRange_FromAnotherMultiList_PreservesMultiplicity()
        {
            var source = new MultiList<string>();
            source.Add("x", 3);
            source.Add("y");

            var target = new MultiList<string>();
            target.AddRange(source);

            target.CountOf("x").ShouldBe(3);
            target.CountOf("y").ShouldBe(1);
            target.TotalCount.ShouldBe(4);
        }

        [Fact]
        public void MultiList_ContainsAll_AfterPartialRemoval()
        {
            var bag = new MultiList<string> { "a", "b" };
            bag.Remove("a");
            bag.ContainsAll(new[] { "a" }).ShouldBeFalse();
            bag.ContainsAll(new[] { "b" }).ShouldBeTrue();
        }

        [Fact]
        public void MultiList_Clear_EmptiesEnumeratorsAndExports()
        {
            var bag = new MultiList<string> { "a", "a" };
            bag.Clear();
            bag.DistinctItems().ShouldBeEmpty();
            bag.EntrySet().ShouldBeEmpty();
            bag.ToDictionary().ShouldBeEmpty();
        }

        [Fact]
        public void MultiList_Clone_PropagatesComparer()
        {
            var bag = new MultiList<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            var clone = bag.Clone();

            clone.Comparer.ShouldBeSameAs(StringComparer.OrdinalIgnoreCase);
            clone.Contains("a").ShouldBeTrue();
        }

        [Fact]
        public void MultiList_RemoveChain_TracksRemainingCopies()
        {
            var bag = new MultiList<string>();
            bag.Add("a", 5);
            bag.Remove("a", 2).ShouldBe(3);
            bag.Remove("a", 2).ShouldBe(1);
            bag.Remove("a", 2).ShouldBe(0);
            bag.Contains("a").ShouldBeFalse();
        }

        [Fact]
        public void MultiList_ExceptWith_DoesNotAffectOtherElements()
        {
            var bag = new MultiList<string> { "a", "a", "b" };
            bag.ExceptWith(new[] { "a", "a" });
            bag.CountOf("a").ShouldBe(0);
            bag.CountOf("b").ShouldBe(1);
            bag.TotalCount.ShouldBe(1);
        }

        [Fact]
        public void MultiList_ToDictionary_AfterClear_ReturnsEmpty()
        {
            var bag = new MultiList<string> { "a" };
            bag.Clear();
            bag.ToDictionary().ShouldBeEmpty();
        }

        [Fact]
        public void MultiList_CaseInsensitiveComparer_SetOperations()
        {
            var bag = new MultiList<string>(StringComparer.OrdinalIgnoreCase) { "a", "a" };
            bag.UnionWith(new[] { "A", "A", "A" });
            bag.CountOf("a").ShouldBe(3);

            bag.ExceptWith(new[] { "A" });
            bag.CountOf("a").ShouldBe(2);
        }

        // ------------------------------------------------------------------
        // MultiDictionary
        // ------------------------------------------------------------------

        [Fact]
        public void MultiDictionary_ToString_NullValue_RendersEmptyPosition()
        {
            var map = new MultiDictionary<string, string>();
            map.Add("k", null);
            map.ToString().ShouldBe("k:[]");
        }

        [Fact]
        public void MultiDictionary_AsLookup_GroupsCarryKeys()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("a", 1);
            map.Add("b", 2);

            var groups = map.AsLookup().ToList();
            groups.Count.ShouldBe(2);
            groups.Select(g => g.Key).ShouldContain("a");
            groups.Select(g => g.Key).ShouldContain("b");
            groups.Single(g => g.Key == "a").ShouldBe(new[] { 1 });
        }

        [Fact]
        public void MultiDictionary_Values_FlattenedWithDuplicates()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("a", 7);
            map.Add("a", 7);
            map.Add("b", 7);
            map.Add("b", 8);

            var values = map.Values.ToList();
            values.Count.ShouldBe(4);
            values.Count(v => v == 7).ShouldBe(3);
        }

        [Fact]
        public void MultiDictionary_Clear_AllowsReuse()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("a", 1);
            map.Clear();
            map.KeyCount.ShouldBe(0);
            map.TotalValueCount.ShouldBe(0);

            map.Add("b", 2);
            map.ContainsKey("a").ShouldBeFalse();
            map["b"].ShouldBe(new[] { 2 });
        }

        [Fact]
        public void MultiDictionary_ClearOnOriginal_DoesNotAffectClone()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("a", 1);
            var clone = map.Clone();

            map.Clear();
            clone["a"].ShouldBe(new[] { 1 });
            clone.ContainsKey("a").ShouldBeTrue();
        }

        [Fact]
        public void MultiDictionary_UnionWith_RespectsNoDuplicatePolicy()
        {
            var map = new MultiDictionary<string, int>(allowDuplicateValues: false);
            map.Add("k", 1);
            map.UnionWith("k", new[] { 1, 2, 2 });
            map["k"].Count.ShouldBe(2);
        }

        [Fact]
        public void MultiDictionary_ExceptWith_DoesNotAffectOtherKeys()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("a", 1);
            map.Add("b", 2);
            map.ExceptWith("a", new[] { 1 });

            map.ContainsKey("a").ShouldBeFalse();
            map["b"].ShouldBe(new[] { 2 });
        }

        [Fact]
        public void MultiDictionary_TryGetValue_PresentKey_ReturnsLiveView()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("k", 1);

            map.TryGetValue("k", out var view).ShouldBeTrue();
            view.ShouldNotBeNull();

            map.Add("k", 2);
            view.Count.ShouldBe(2);
        }

        [Fact]
        public void MultiDictionary_FlatEnumeration_AfterRemoval_Consistent()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("a", 1);
            map.Add("a", 2);
            map.Add("b", 3);
            map.Remove("a", 2);

            var flat = map.Select((KeyValuePair<string, int> p) => p).ToList();
            flat.Count.ShouldBe(map.TotalValueCount);
            flat.Count.ShouldBe(2);
        }

        [Fact]
        public void MultiDictionary_AddRange_MultipleValues_UnderOneKey()
        {
            var map = new MultiDictionary<string, int>();
            map.AddRange("k", new[] { 1, 2, 3 });
            map["k"].ShouldBe(new[] { 1, 2, 3 });
            map.TotalValueCount.ShouldBe(3);
        }
    }
}
