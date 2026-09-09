using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// Edge-condition coverage for <see cref="MultiDictionary{TKey,TValue}"/>:
    /// argument validation, missing-key behaviour, empty-inner-collection
    /// invariant, comparer injection, live views and clone independence.
    /// </summary>
    public class MultiDictionaryEdgeCaseTests
    {
        // ------------------------------------------------------------------
        // Argument validation
        // ------------------------------------------------------------------

        [Fact]
        public void Add_NullKey_ThrowsArgumentNull()
        {
            var map = new MultiDictionary<string, int>();
            Should.Throw<ArgumentNullException>(() => map.Add(null, 1));
        }

        [Fact]
        public void AddRange_NullValues_ThrowsArgumentNull()
        {
            Should.Throw<ArgumentNullException>(() =>
                new MultiDictionary<string, int>().AddRange("k", null));
        }

        [Fact]
        public void Indexer_NullKey_ThrowsArgumentNull()
        {
            Should.Throw<ArgumentNullException>(() => new MultiDictionary<string, int>()[null]);
        }

        [Fact]
        public void ContainsKey_NullKey_ThrowsArgumentNull()
        {
            Should.Throw<ArgumentNullException>(() => new MultiDictionary<string, int>().ContainsKey(null));
        }

        // ------------------------------------------------------------------
        // Missing-key behaviour
        // ------------------------------------------------------------------

        [Fact]
        public void Indexer_MissingKey_ReturnsEmptyNonNullCollection()
        {
            var map = new MultiDictionary<string, int>();
            var values = map["missing"];
            values.ShouldNotBeNull();
            values.Count.ShouldBe(0);
        }

        [Fact]
        public void TryGetValue_MissingKey_ReturnsFalse_NullOut()
        {
            var map = new MultiDictionary<string, int>();
            map.TryGetValue("missing", out var value).ShouldBeFalse();
            value.ShouldBeNull();
        }

        [Fact]
        public void Contains_MissingKey_ReturnsFalse()
        {
            new MultiDictionary<string, int>().Contains("k", 1).ShouldBeFalse();
        }

        [Fact]
        public void ContainsValue_Missing_ReturnsFalse()
        {
            new MultiDictionary<string, int> { { "k", 1 } }.ContainsValue(99).ShouldBeFalse();
        }

        [Fact]
        public void ContainsValue_NullValue_Found()
        {
            var map = new MultiDictionary<string, string>();
            map.Add("k", null);
            map.ContainsValue(null).ShouldBeTrue();
        }

        [Fact]
        public void RemoveKeyValue_MissingKey_ReturnsFalse()
        {
            new MultiDictionary<string, int>().Remove("k", 1).ShouldBeFalse();
        }

        [Fact]
        public void RemoveKeyValue_MissingValue_ReturnsFalse_KeyKept()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("k", 1);
            map.Remove("k", 99).ShouldBeFalse();
            map.ContainsKey("k").ShouldBeTrue();
            map["k"].Count.ShouldBe(1);
        }

        [Fact]
        public void RemoveKeyValue_LastValue_DropsKey()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("k", 1);
            map.Remove("k", 1).ShouldBeTrue();
            map.ContainsKey("k").ShouldBeFalse();
            map.KeyCount.ShouldBe(0);
            map.TotalValueCount.ShouldBe(0);
        }

        [Fact]
        public void Remove_MissingKey_ReturnsFalse()
        {
            new MultiDictionary<string, int>().Remove("k").ShouldBeFalse();
        }

        // ------------------------------------------------------------------
        // Null values
        // ------------------------------------------------------------------

        [Fact]
        public void NullValue_FullLifecycle()
        {
            var map = new MultiDictionary<string, string>();
            map.Add("k", null);

            map.ContainsKey("k").ShouldBeTrue();
            map.Contains("k", null).ShouldBeTrue();
            map["k"].Count.ShouldBe(1);
            map.ContainsValue(null).ShouldBeTrue();
            map.Values.Count(v => v == null).ShouldBe(1);

            map.Remove("k", null).ShouldBeTrue();
            map.ContainsKey("k").ShouldBeFalse();
        }

        // ------------------------------------------------------------------
        // Empty-inner-collection invariant
        // ------------------------------------------------------------------

        [Fact]
        public void IntersectionWith_RemovingAll_DropsKey()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("k", 1);
            map.Add("k", 2);
            map.IntersectionWith("k", new[] { 3, 4 });
            map.ContainsKey("k").ShouldBeFalse();
        }

        [Fact]
        public void ExceptWith_RemovingAll_DropsKey()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("k", 1);
            map.ExceptWith("k", new[] { 1 });
            map.ContainsKey("k").ShouldBeFalse();
        }

        [Fact]
        public void UnionWith_MissingKey_CreatesKey()
        {
            var map = new MultiDictionary<string, int>();
            map.UnionWith("k", new[] { 1 });
            map.ContainsKey("k").ShouldBeTrue();
            map["k"].ShouldBe(new[] { 1 });
        }

        [Fact]
        public void IntersectionWith_MissingKey_NoOp()
        {
            var map = new MultiDictionary<string, int>();
            map.IntersectionWith("k", new[] { 1 });
            map.ContainsKey("k").ShouldBeFalse();
            map.KeyCount.ShouldBe(0);
        }

        [Fact]
        public void ExceptWith_MissingKey_NoOp()
        {
            var map = new MultiDictionary<string, int>();
            map.ExceptWith("k", new[] { 1 });
            map.ContainsKey("k").ShouldBeFalse();
        }

        [Fact]
        public void UnionWith_NullValues_ThrowsArgumentNull()
        {
            Should.Throw<ArgumentNullException>(() =>
                new MultiDictionary<string, int>().UnionWith("k", null));
        }

        [Fact]
        public void IntersectionWith_NullValues_ThrowsArgumentNull()
        {
            Should.Throw<ArgumentNullException>(() =>
                new MultiDictionary<string, int>().IntersectionWith("k", null));
        }

        [Fact]
        public void ExceptWith_NullValues_ThrowsArgumentNull()
        {
            Should.Throw<ArgumentNullException>(() =>
                new MultiDictionary<string, int>().ExceptWith("k", null));
        }

        // ------------------------------------------------------------------
        // Duplicate-value policies
        // ------------------------------------------------------------------

        [Fact]
        public void AllowDuplicateValues_False_DuplicateIgnored()
        {
            var map = new MultiDictionary<string, int>(allowDuplicateValues: false);
            map.Add("k", 1);
            map.Add("k", 1);
            map["k"].Count.ShouldBe(1);
            map.TotalValueCount.ShouldBe(1);
        }

        [Fact]
        public void AllowDuplicateValues_False_RemoveThenReAdd()
        {
            var map = new MultiDictionary<string, int>(allowDuplicateValues: false);
            map.Add("k", 1);
            map.Remove("k", 1);
            map.Add("k", 1);
            map["k"].Count.ShouldBe(1);
        }

        [Fact]
        public void AllowDuplicateValues_False_NullValues_Deduplicated()
        {
            var map = new MultiDictionary<string, string>(allowDuplicateValues: false);
            map.Add("k", null);
            map.Add("k", null);
            map["k"].Count.ShouldBe(1);
        }

        [Fact]
        public void AllowDuplicateValues_True_DuplicatesKept()
        {
            var map = new MultiDictionary<string, int>(allowDuplicateValues: true);
            map.Add("k", 1);
            map.Add("k", 1);
            map["k"].Count.ShouldBe(2);
        }

        [Fact]
        public void UnionWith_DuplicatesInInput_AddedOnce()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("k", 1);
            map.UnionWith("k", new[] { 1, 2, 2, 3 });
            map["k"].Count.ShouldBe(3);
        }

        [Fact]
        public void ExceptWith_RemovesEveryOccurrenceOfValue()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("k", 1);
            map.Add("k", 1);
            map.Add("k", 2);
            map.ExceptWith("k", new[] { 1 });
            map["k"].ShouldBe(new[] { 2 });
        }

        // ------------------------------------------------------------------
        // Comparer injection
        // ------------------------------------------------------------------

        [Fact]
        public void CaseInsensitiveComparer_KeysUnified()
        {
            var map = new MultiDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            map.Add("Key", 1);
            map.Add("KEY", 2);

            map.KeyCount.ShouldBe(1);
            map["key"].Count.ShouldBe(2);
            map.Comparer.ShouldBeSameAs(StringComparer.OrdinalIgnoreCase);
        }

        // ------------------------------------------------------------------
        // Live views
        // ------------------------------------------------------------------

        [Fact]
        public void Indexer_ReturnsLiveView()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("k", 1);
            var view = map["k"];

            map.Add("k", 2);
            view.Count.ShouldBe(2);

            map.Remove("k", 1);
            view.Count.ShouldBe(1);
        }

        [Fact]
        public void AsLookup_MissingKey_YieldsEmptyGrouping()
        {
            var lookup = new MultiDictionary<string, int>().AsLookup();
            lookup["missing"].ShouldBeEmpty();
            lookup.Contains("missing").ShouldBeFalse();
        }

        [Fact]
        public void AsLookup_Count_MatchesKeyCount()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("a", 1);
            map.Add("b", 2);
            map.AsLookup().Count.ShouldBe(2);
        }

        [Fact]
        public void AsLookup_EnumeratesAllGroups()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("a", 1);
            map.Add("a", 2);
            map.Add("b", 3);

            var lookup = map.AsLookup();
            lookup.Count.ShouldBe(2);
            lookup["a"].OrderBy(x => x).ShouldBe(new[] { 1, 2 });
            lookup["b"].ShouldBe(new[] { 3 });
        }

        // ------------------------------------------------------------------
        // Clone independence
        // ------------------------------------------------------------------

        [Fact]
        public void Clone_IndependentOfOriginal()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("k", 1);

            var clone = map.Clone();
            clone.Add("k", 2);
            clone.Add("other", 3);

            map["k"].Count.ShouldBe(1);
            map.ContainsKey("other").ShouldBeFalse();

            clone.Remove("k", 1);
            map["k"].Count.ShouldBe(1);
        }

        [Fact]
        public void Clone_PreservesComparerAndPolicy()
        {
            var map = new MultiDictionary<string, int>(
                StringComparer.OrdinalIgnoreCase, allowDuplicateValues: false);
            var clone = map.Clone();

            clone.Comparer.ShouldBeSameAs(StringComparer.OrdinalIgnoreCase);
            clone.Add("k", 1);
            clone.Add("K", 1);
            clone["k"].Count.ShouldBe(1);
        }

        [Fact]
        public void Clone_EmptyMap_ReturnsEmpty()
        {
            new MultiDictionary<string, int>().Clone().KeyCount.ShouldBe(0);
        }

        // ------------------------------------------------------------------
        // Counters and enumeration
        // ------------------------------------------------------------------

        [Fact]
        public void Counters_AggregatedCorrectly()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("a", 1);
            map.Add("a", 2);
            map.Add("b", 3);

            map.Count.ShouldBe(2);
            map.KeyCount.ShouldBe(2);
            map.TotalValueCount.ShouldBe(3);
        }

        [Fact]
        public void FlatEnumeration_CountEqualsTotalValueCount()
        {
            var map = new MultiDictionary<string, string>();
            map.Add("a", "x");
            map.Add("a", "y");
            map.Add("a", "y");
            map.Add("b", null);

            var flat = map.Select((KeyValuePair<string, string> p) => p).ToList();
            flat.Count.ShouldBe(map.TotalValueCount);
            flat.Count(p => p.Key == "a").ShouldBe(3);
            flat.Count(p => p.Key == "b" && p.Value == null).ShouldBe(1);
        }

        [Fact]
        public void IReadOnlyDictionary_ExplicitView_Consistent()
        {
            IReadOnlyDictionary<string, IReadOnlyCollection<int>> view =
                new MultiDictionary<string, int>();
            // (populated separately through the concrete type)
            var map = (MultiDictionary<string, int>)view;
            map.Add("a", 1);
            map.Add("b", 2);

            view.Count.ShouldBe(2);
            view.ContainsKey("a").ShouldBeTrue();
            view["a"].ShouldBe(new[] { 1 });
            view.Keys.ShouldContain("a");
            view.Keys.ShouldContain("b");
            view.Values.SelectMany(v => v).ShouldBe(new[] { 1, 2 });
            view.FirstOrDefault(p => p.Key == "a").Value.ShouldBe(new[] { 1 });
        }
    }
}
