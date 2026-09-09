using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    public class MultiDictionaryTests
    {
        [Fact]
        public void Add_SingleValue_IndexerReturnsIt()
        {
            var dict = new MultiDictionary<string, int>();
            dict.Add("k", 1);

            dict["k"].ShouldContain(1);
            dict["k"].Count.ShouldBe(1);
        }

        [Fact]
        public void Add_MultipleValues_SameKey_IndexerContainsAll()
        {
            var dict = new MultiDictionary<string, int>();
            dict.Add("k", 1);
            dict.Add("k", 2);
            dict.Add("k", 3);

            dict["k"].OrderBy(x => x).ShouldBe(new[] { 1, 2, 3 });
        }

        [Fact]
        public void AddRange_AddsAllValuesUnderKey()
        {
            var dict = new MultiDictionary<string, int>();
            dict.AddRange("k", new[] { 1, 2, 3 });

            dict["k"].Count.ShouldBe(3);
            dict.TotalValueCount.ShouldBe(3);
        }

        [Fact]
        public void Indexer_MissingKey_ReturnsEmptyCollection()
        {
            var dict = new MultiDictionary<string, int>();

            dict["missing"].ShouldBeEmpty();
            dict["missing"].ShouldNotBeNull();
        }

        [Fact]
        public void ContainsKey_TrueAndFalse()
        {
            var dict = new MultiDictionary<string, int>();
            dict.Add("k", 1);

            dict.ContainsKey("k").ShouldBeTrue();
            dict.ContainsKey("other").ShouldBeFalse();
        }

        [Fact]
        public void Contains_KeyValue_TrueAndFalse()
        {
            var dict = new MultiDictionary<string, int>();
            dict.Add("k", 1);

            dict.Contains("k", 1).ShouldBeTrue();
            dict.Contains("k", 2).ShouldBeFalse();
            dict.Contains("other", 1).ShouldBeFalse();
        }

        [Fact]
        public void ContainsValue_SearchesAcrossKeys()
        {
            var dict = new MultiDictionary<string, int>();
            dict.Add("a", 1);
            dict.Add("b", 2);

            dict.ContainsValue(2).ShouldBeTrue();
            dict.ContainsValue(3).ShouldBeFalse();
        }

        [Fact]
        public void Remove_Key_RemovesAllValues()
        {
            var dict = new MultiDictionary<string, int>();
            dict.AddRange("k", new[] { 1, 2, 3 });

            dict.Remove("k").ShouldBeTrue();

            dict.ContainsKey("k").ShouldBeFalse();
            dict.TotalValueCount.ShouldBe(0);
        }

        [Fact]
        public void Remove_KeyValue_RemovesOneOccurrence()
        {
            var dict = new MultiDictionary<string, int>();
            dict.Add("k", 1);
            dict.Add("k", 1);

            dict.Remove("k", 1).ShouldBeTrue();

            dict.ContainsKey("k").ShouldBeTrue();
            dict["k"].Count.ShouldBe(1);

            dict.Remove("k", 1).ShouldBeTrue();
            dict.ContainsKey("k").ShouldBeFalse();
        }

        [Fact]
        public void Remove_KeyValue_AutoRemovesEmptyKey()
        {
            var dict = new MultiDictionary<string, int>();
            dict.Add("k", 1);
            dict.Remove("k", 1);

            dict.ContainsKey("k").ShouldBeFalse();
            dict.Count.ShouldBe(0);
        }

        [Fact]
        public void Remove_MissingKey_ReturnsFalse()
        {
            var dict = new MultiDictionary<string, int>();
            dict.Remove("missing").ShouldBeFalse();
            dict.Remove("missing", 1).ShouldBeFalse();
        }

        [Fact]
        public void Count_EqualsNumberOfKeys()
        {
            var dict = new MultiDictionary<string, int>();
            dict.AddRange("a", new[] { 1, 2, 3 });
            dict.AddRange("b", new[] { 4, 5 });

            dict.Count.ShouldBe(2);
        }

        [Fact]
        public void TotalValueCount_SumsAllValues()
        {
            var dict = new MultiDictionary<string, int>();
            dict.AddRange("a", new[] { 1, 2, 3 });
            dict.AddRange("b", new[] { 4, 5 });

            dict.TotalValueCount.ShouldBe(5);
        }

        [Fact]
        public void KeyCount_EqualsCount()
        {
            var dict = new MultiDictionary<string, int>();
            dict.Add("a", 1);

            dict.KeyCount.ShouldBe(dict.Count);
        }

        [Fact]
        public void Keys_EnumeratesAllKeys()
        {
            var dict = new MultiDictionary<string, int>();
            dict.Add("a", 1);
            dict.Add("b", 2);

            dict.Keys.OrderBy(x => x).ShouldBe(new[] { "a", "b" });
        }

        [Fact]
        public void Values_IsFlattened()
        {
            var dict = new MultiDictionary<string, int>();
            dict.AddRange("a", new[] { 1, 2 });
            dict.Add("b", 3);

            dict.Values.OrderBy(x => x).ShouldBe(new[] { 1, 2, 3 });
        }

        [Fact]
        public void FlatEnumeration_YieldsPairPerValue()
        {
            var dict = new MultiDictionary<string, int>();
            dict.AddRange("a", new[] { 1, 2 });

            var flat = ((IEnumerable<KeyValuePair<string, int>>)dict).ToList();

            flat.Count.ShouldBe(2);
            flat.Count(x => x.Key == "a" && x.Value == 1).ShouldBe(1);
            flat.Count(x => x.Key == "a" && x.Value == 2).ShouldBe(1);
        }

        [Fact]
        public void IReadOnlyDictionaryEnumeration_YieldsPairPerKey()
        {
            IReadOnlyDictionary<string, IReadOnlyCollection<int>> dict = new MultiDictionary<string, int>();
            ((MultiDictionary<string, int>)dict).AddRange("a", new[] { 1, 2 });
            ((MultiDictionary<string, int>)dict).Add("b", 3);

            var pairs = dict.ToList();

            pairs.Count.ShouldBe(2);
            pairs.First(x => x.Key == "a").Value.OrderBy(x => x).ShouldBe(new[] { 1, 2 });
            pairs.First(x => x.Key == "b").Value.ShouldBe(new[] { 3 });
        }

        [Fact]
        public void TryGetValue_TrueAndFalse()
        {
            var dict = new MultiDictionary<string, int>();
            dict.Add("k", 1);

            dict.TryGetValue("k", out var values).ShouldBeTrue();
            values.ShouldContain(1);

            dict.TryGetValue("missing", out var missing).ShouldBeFalse();
            missing.ShouldBeNull();
        }

        [Fact]
        public void AsLookup_Count_And_Contains()
        {
            var dict = new MultiDictionary<string, int>();
            dict.Add("a", 1);
            dict.Add("b", 2);

            var lookup = dict.AsLookup();

            lookup.Count.ShouldBe(2);
            lookup.Contains("a").ShouldBeTrue();
            lookup.Contains("missing").ShouldBeFalse();
        }

        [Fact]
        public void AsLookup_Indexer_ReturnsGroupValues()
        {
            var dict = new MultiDictionary<string, int>();
            dict.AddRange("a", new[] { 1, 2 });

            var lookup = dict.AsLookup();
            lookup["a"].OrderBy(x => x).ShouldBe(new[] { 1, 2 });

            var group = lookup.First(g => g.Key == "a");
            group.Key.ShouldBe("a");
            group.OrderBy(x => x).ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public void AsLookup_MissingKey_ReturnsEmptyGroup()
        {
            var dict = new MultiDictionary<string, int>();
            var lookup = dict.AsLookup();

            lookup["missing"].ShouldBeEmpty();
        }

        [Fact]
        public void AllowDuplicateValues_True_ListAllowsDuplicates()
        {
            var dict = new MultiDictionary<string, int>(true);
            dict.Add("k", 1);
            dict.Add("k", 1);

            dict["k"].Count.ShouldBe(2);
            dict.TotalValueCount.ShouldBe(2);
        }

        [Fact]
        public void AllowDuplicateValues_False_HashSetDeduplicates()
        {
            var dict = new MultiDictionary<string, int>(false);
            dict.Add("k", 1);
            dict.Add("k", 1);

            dict["k"].Count.ShouldBe(1);
            dict.TotalValueCount.ShouldBe(1);
        }

        [Fact]
        public void AllowDuplicateValues_False_DuplicateOnMissingKey_DoesNotCreateKey()
        {
            var dict = new MultiDictionary<string, int>(false);
            dict.Add("k", 1);
            dict.Add("k", 1);
            dict.Remove("k", 1);

            dict.ContainsKey("k").ShouldBeFalse();
        }

        [Fact]
        public void CustomInnerFactory_IsUsed()
        {
            // A HashSet factory without allowDuplicateValues=true must still deduplicate,
            // proving the custom factory is in effect.
            var dict = new MultiDictionary<string, int>(null, () => new HashSet<int>());
            dict.Add("k", 1);
            dict.Add("k", 1);

            dict["k"].Count.ShouldBe(1);
        }

        [Fact]
        public void Comparer_IsUsedForKeyEquality()
        {
            var dict = new MultiDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            dict.Add("Key", 1);
            dict.Add("key", 2);

            dict.Count.ShouldBe(1);
            dict["KEY"].OrderBy(x => x).ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public void NullKey_ThrowsArgumentNullException()
        {
            var dict = new MultiDictionary<string, int>();
            Should.Throw<ArgumentNullException>(() => dict.Add(null, 1));
            Should.Throw<ArgumentNullException>(() => dict.ContainsKey(null));
        }

        [Fact]
        public void NullValue_IsAllowed()
        {
            var dict = new MultiDictionary<string, string>();
            dict.Add("k", null);

            dict.Contains("k", null).ShouldBeTrue();
            dict.ContainsValue(null).ShouldBeTrue();
            dict.Remove("k", null).ShouldBeTrue();
            dict.ContainsKey("k").ShouldBeFalse();
        }

        [Fact]
        public void AddRange_Null_ThrowsArgumentNullException()
        {
            var dict = new MultiDictionary<string, int>();
            Should.Throw<ArgumentNullException>(() => dict.AddRange("k", null));
        }

        [Fact]
        public void Clear_RemovesEverything()
        {
            var dict = new MultiDictionary<string, int>();
            dict.AddRange("a", new[] { 1, 2 });
            dict.Add("b", 3);

            dict.Clear();

            dict.Count.ShouldBe(0);
            dict.TotalValueCount.ShouldBe(0);
            dict.Values.ShouldBeEmpty();
        }

        [Fact]
        public void Clone_IsIndependent()
        {
            var original = new MultiDictionary<string, int>();
            original.Add("a", 1);

            var clone = original.Clone();
            clone.Add("a", 2);
            clone.Add("b", 3);

            original["a"].Count.ShouldBe(1);
            original.ContainsKey("b").ShouldBeFalse();
            clone["a"].Count.ShouldBe(2);
        }
    }
}
