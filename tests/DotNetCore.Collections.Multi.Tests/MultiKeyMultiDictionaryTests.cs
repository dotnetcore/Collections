using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// F6-05: <see cref="MultiKeyMultiDictionary{TKey,TValue}"/> - the composite-key multimap
    /// combining <see cref="MultiKeyDictionary{TKey,TValue}"/>'s trie (N components &#8594; 1
    /// key, prefix projection) with <see cref="MultiDictionary{TKey,TValue}"/>'s per-key value
    /// collections (1 key &#8594; N values, inner factory, "no value-less key" invariant, set
    /// operations).
    /// </summary>
    public class MultiKeyMultiDictionaryTests
    {
        private static readonly string[] EmptyKey = new string[0];

        // ------------------------------------------------------------------
        // Construction and counts
        // ------------------------------------------------------------------

        [Fact]
        public void Ctor_Default_AllowsDuplicateValues()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            var key = new[] { "eu", "de" };
            map.Add(key, 1);
            map.Add(key, 1);

            map.ValueCount(key).ShouldBe(2);
            map.TotalValueCount.ShouldBe(2);
        }

        [Fact]
        public void Ctor_AllowDuplicateValuesFalse_SilentlyIgnoresDuplicates()
        {
            var map = new MultiKeyMultiDictionary<string, int>(allowDuplicateValues: false);
            var key = new[] { "eu", "de" };
            map.Add(key, 1);
            map.Add(key, 1);
            map.Add(key, 2);

            map[key].ShouldBe(new[] { 1, 2 });
            map.TotalValueCount.ShouldBe(2);
        }

        [Fact]
        public void Ctor_CustomComparer_ComponentsCompareByComparer()
        {
            var map = new MultiKeyMultiDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var upper = new[] { "EU", "DE", "BERLIN" };
            var lower = new[] { "eu", "de", "berlin" };

            map.Add(upper, 1);
            map.Add(lower, 2);

            // Same complete key under the injected comparer: one key, two values.
            map.Count.ShouldBe(1);
            map.KeyCount.ShouldBe(1);
            map.TotalValueCount.ShouldBe(2);
            map.ValueCount(lower).ShouldBe(2);
            map.Comparer.ShouldBeSameAs(StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void Ctor_CustomInnerFactory_ValuesEnumerateThroughIt()
        {
            var map = new MultiKeyMultiDictionary<string, int>(
                null, () => new SortedSet<int>());
            var key = new[] { "eu", "de" };
            map.Add(key, 3);
            map.Add(key, 1);
            map.Add(key, 2);

            // A factory-produced SortedSet enumerates ascending.
            map[key].ShouldBe(new[] { 1, 2, 3 });
        }

        [Fact]
        public void Ctor_EmptyMap_CountsAreZero()
        {
            var map = new MultiKeyMultiDictionary<string, int>();

            map.Count.ShouldBe(0);
            map.KeyCount.ShouldBe(0);
            map.TotalValueCount.ShouldBe(0);
            map.NodeCount.ShouldBe(1); // the root
            map.IsEmpty.ShouldBeTrue();
            map.Keys.ShouldBeEmpty();
            map.Values.ShouldBeEmpty();
        }

        // ------------------------------------------------------------------
        // Add and basic lookup
        // ------------------------------------------------------------------

        [Fact]
        public void Add_MultipleValuesUnderSameKey_IndexerReturnsAll()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            var key = new[] { "eu", "de", "berlin" };
            map.Add(key, 1);
            map.Add(key, 2);
            map.Add(key, 3);

            map[key].ShouldBe(new[] { 1, 2, 3 });
            map[key].Count.ShouldBe(3);
        }

        [Fact]
        public void Add_PreservesInsertionOrderOfValues()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            var key = new[] { "eu", "de" };
            map.Add(key, 30);
            map.Add(key, 10);
            map.Add(key, 20);

            map[key].ShouldBe(new[] { 30, 10, 20 });
        }

        [Fact]
        public void AddRange_AddsEachValueUnderTheKey()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            var key = new[] { "eu", "de" };
            map.AddRange(key, new[] { 1, 2, 3 });

            map[key].ShouldBe(new[] { 1, 2, 3 });
            map.TotalValueCount.ShouldBe(3);
        }

        [Fact]
        public void AddRange_NullArgument_ThrowsArgumentNullException()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            Should.Throw<ArgumentNullException>(() => map.AddRange(new[] { "eu" }, null!));
        }

        [Fact]
        public void Add_NullKey_ThrowsArgumentNullException()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            Should.Throw<ArgumentNullException>(() => map.Add(null!, 1));
        }

        [Fact]
        public void Add_NullComponent_SupportedAtAnyPosition()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.Add(new[] { null, "de" }, 1);
            map.Add(new[] { "eu", null }, 2);
            map.Add(new[] { "eu", "de", null }, 3);
            map.Add(new[] { "eu", "de" }, 4);

            map[new[] { null, "de" }].ShouldBe(new[] { 1 });
            map[new[] { "eu", null }].ShouldBe(new[] { 2 });
            map[new[] { "eu", "de", null }].ShouldBe(new[] { 3 });
            map.Count.ShouldBe(4);
            map.TotalValueCount.ShouldBe(4);
        }

        [Fact]
        public void Add_NullValue_SupportedAsOrdinaryValue()
        {
            var map = new MultiKeyMultiDictionary<string, int?>();
            var key = new[] { "eu", "de" };
            map.Add(key, 1);
            map.Add(key, null);
            map.Add(key, null);

            map.ValueCount(key).ShouldBe(3);
            map.TotalValueCount.ShouldBe(3);
            map.Contains(key, null).ShouldBeTrue();
            map[key].ShouldBe(new int?[] { 1, null, null });
        }

        [Fact]
        public void Add_EmptyKeyArray_StoresAtTheRoot()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.Add(EmptyKey, 1);
            map.Add(EmptyKey, 2);
            map.Add(new[] { "eu" }, 3);

            map[EmptyKey].ShouldBe(new[] { 1, 2 });
            map.Count.ShouldBe(2);
            map.TotalValueCount.ShouldBe(3);
            // An empty prefix enumerates the whole map; the root entry is the one with no components.
            map.GetByPrefix(EmptyKey).Single(p => p.Key.Length == 0).Values.ShouldBe(new[] { 1, 2 });
            map.RemovePrefix(EmptyKey).ShouldBe(3);
            map.IsEmpty.ShouldBeTrue();
        }

        [Fact]
        public void Indexer_MissingKey_ReturnsEmptyCollection()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map[new[] { "missing" }].ShouldBeEmpty();
            map[new[] { "missing" }].ShouldNotBeNull();
        }

        [Fact]
        public void TryGetValue_PresentAndAbsent()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            var key = new[] { "eu", "de" };
            map.Add(key, 1);

            map.TryGetValue(key, out var values).ShouldBeTrue();
            values.ShouldBe(new[] { 1 });

            map.TryGetValue(new[] { "missing" }, out var absent).ShouldBeFalse();
            absent.ShouldBeNull();
        }

        [Fact]
        public void TryGetValueByPrefix_MatchesTryGetValueAtTheExactPrefix()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            var key = new[] { "eu", "de" };
            map.Add(key, 1);
            map.Add(new[] { "eu", "de", "berlin" }, 2);

            map.TryGetValueByPrefix(key, out var atPrefix).ShouldBeTrue();
            atPrefix.ShouldBe(new[] { 1 });

            map.TryGetValueByPrefix(new[] { "eu" }, out var abovePrefix).ShouldBeFalse();
            abovePrefix.ShouldBeNull();
        }

        [Fact]
        public void ContainsKey_PresentAndAbsent()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            var key = new[] { "eu", "de" };
            map.Add(key, 1);

            map.ContainsKey(key).ShouldBeTrue();
            map.ContainsKey(new[] { "eu" }).ShouldBeFalse(); // a pure prefix is not a key
            map.ContainsKey(new[] { "missing" }).ShouldBeFalse();
        }

        [Fact]
        public void Contains_PresentAndAbsent()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            var key = new[] { "eu", "de" };
            map.Add(key, 1);

            map.Contains(key, 1).ShouldBeTrue();
            map.Contains(key, 2).ShouldBeFalse();
            map.Contains(new[] { "missing" }, 1).ShouldBeFalse();
        }

        [Fact]
        public void ValueCount_PresentAndAbsent()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            var key = new[] { "eu", "de" };
            map.Add(key, 1);
            map.Add(key, 2);

            map.ValueCount(key).ShouldBe(2);
            map.ValueCount(new[] { "missing" }).ShouldBe(0);
        }

        [Fact]
        public void NodeCount_ObservesPrefixSharing()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.Add(new[] { "a", "b" }, 1);
            map.Add(new[] { "a", "c" }, 2);

            // root, a, b, c - the "a" node is shared.
            map.NodeCount.ShouldBe(4);

            map.Add(new[] { "a", "b", "d" }, 3);
            map.NodeCount.ShouldBe(5);
        }

        // ------------------------------------------------------------------
        // Remove and the "no value-less key" invariant
        // ------------------------------------------------------------------

        [Fact]
        public void RemoveKey_RemovesKeyWithAllValues()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            var key = new[] { "eu", "de" };
            map.Add(key, 1);
            map.Add(key, 2);
            map.Add(new[] { "eu", "fr" }, 3);

            map.Remove(key).ShouldBeTrue();

            map.ContainsKey(key).ShouldBeFalse();
            map.ValueCount(key).ShouldBe(0);
            map[key].ShouldBeEmpty();
            map.TotalValueCount.ShouldBe(1);
            map.ContainsKey(new[] { "eu", "fr" }).ShouldBeTrue();
        }

        [Fact]
        public void RemoveKey_MissingKey_ReturnsFalse()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.Remove(new[] { "missing" }).ShouldBeFalse();
        }

        [Fact]
        public void RemoveKeyValue_RemovesOneOccurrence()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            var key = new[] { "eu", "de" };
            map.Add(key, 7);
            map.Add(key, 7);
            map.Add(key, 8);

            map.Remove(key, 7).ShouldBeTrue();

            map[key].ShouldBe(new[] { 7, 8 });
            map.TotalValueCount.ShouldBe(2);
        }

        [Fact]
        public void RemoveKeyValue_LastValueDropsKey()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            var key = new[] { "eu", "de" };
            map.Add(key, 1);

            map.Remove(key, 1).ShouldBeTrue();

            map.ContainsKey(key).ShouldBeFalse();
            map.IsEmpty.ShouldBeTrue();
            map.NodeCount.ShouldBe(1);
        }

        [Fact]
        public void RemoveKeyValue_PrunesNowEmptySubtree()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.Add(new[] { "a", "b" }, 1);
            map.Add(new[] { "a", "c" }, 2);

            map.Remove(new[] { "a", "b" }, 1).ShouldBeTrue();

            // The "b" node is pruned; "a" survives because "a","c" still needs it.
            map.NodeCount.ShouldBe(3);
            map.ContainsPrefix(new[] { "a", "b" }).ShouldBeFalse();
            map.GetByPrefix(new[] { "a", "b" }).ShouldBeEmpty();
            map.ContainsKey(new[] { "a", "c" }).ShouldBeTrue();
        }

        [Fact]
        public void RemoveKeyValue_MissingKeyOrValue_ReturnsFalse()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            var key = new[] { "eu", "de" };
            map.Add(key, 1);

            map.Remove(new[] { "missing" }, 1).ShouldBeFalse();
            map.Remove(key, 99).ShouldBeFalse();
            map.ValueCount(key).ShouldBe(1); // untouched
        }

        [Fact]
        public void RemoveKeyValue_NullValue_RemovesIt()
        {
            var map = new MultiKeyMultiDictionary<string, int?>();
            var key = new[] { "eu", "de" };
            map.Add(key, null);
            map.Add(key, 1);

            map.Remove(key, null).ShouldBeTrue();

            map[key].ShouldBe(new[] { (int?)1 });
        }

        [Fact]
        public void RemoveRange_RemovesOneOccurrencePerDistinctValue()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            var key = new[] { "eu", "de" };
            map.AddRange(key, new[] { 1, 1, 2, 3 });

            map.RemoveRange(key, new[] { 1, 3, 99 }).ShouldBeTrue();

            // One occurrence of 1 removed (of two stored), 3 removed entirely, 99 never stored.
            map[key].ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public void RemoveRange_MissingKey_ReturnsFalse()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.RemoveRange(new[] { "missing" }, new[] { 1 }).ShouldBeFalse();
        }

        [Fact]
        public void RemoveRange_NoMatchingValue_ReturnsFalseAndLeavesKey()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            var key = new[] { "eu", "de" };
            map.Add(key, 1);

            map.RemoveRange(key, new[] { 99 }).ShouldBeFalse();
            map.ContainsKey(key).ShouldBeTrue();
            map.ValueCount(key).ShouldBe(1);
        }

        [Fact]
        public void RemoveRange_NullArgument_ThrowsArgumentNullException()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            Should.Throw<ArgumentNullException>(() => map.RemoveRange(new[] { "eu" }, null!));
        }

        // ------------------------------------------------------------------
        // Prefix projection
        // ------------------------------------------------------------------

        [Fact]
        public void GetByPrefix_EnumeratesFullKeysWithTheirValues()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.Add(new[] { "eu", "de", "berlin" }, 1);
            map.Add(new[] { "eu", "de", "berlin" }, 2);
            map.Add(new[] { "eu", "de", "munich" }, 3);
            map.Add(new[] { "eu", "fr", "paris" }, 4);
            map.Add(new[] { "us", "nyc" }, 5);

            var slice = map.GetByPrefix(new[] { "eu", "de" }).ToList();

            slice.Count.ShouldBe(2);
            slice.Single(p => p.Key.SequenceEqual(new[] { "eu", "de", "berlin" }))
                .Values.ShouldBe(new[] { 1, 2 });
            slice.Single(p => p.Key.SequenceEqual(new[] { "eu", "de", "munich" }))
                .Values.ShouldBe(new[] { 3 });
        }

        [Fact]
        public void GetByPrefix_IncludesKeyStoredExactlyAtThePrefix()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.Add(new[] { "eu", "de" }, 1);
            map.Add(new[] { "eu", "de", "berlin" }, 2);

            var keys = map.GetByPrefix(new[] { "eu", "de" }).Select(p => p.Key).ToArray();
            keys.Length.ShouldBe(2);
            keys.ShouldContain(k => k.SequenceEqual(new[] { "eu", "de" }));
            keys.ShouldContain(k => k.SequenceEqual(new[] { "eu", "de", "berlin" }));
        }

        [Fact]
        public void GetByPrefix_Relative_YieldsSuffixKeys()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.Add(new[] { "eu", "de", "berlin" }, 1);
            map.Add(new[] { "eu", "fr", "paris" }, 2);

            var suffixes = map.GetByPrefix(new[] { "eu" }, relative: true)
                .Select(p => string.Join("/", p.Key))
                .ToArray();

            suffixes.ShouldBe(new[] { "de/berlin", "fr/paris" });
        }

        [Fact]
        public void GetByPrefix_MissingPrefix_YieldsEmpty()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.Add(new[] { "eu", "de" }, 1);

            map.GetByPrefix(new[] { "asia" }).ShouldBeEmpty();
        }

        [Fact]
        public void GetByPrefix_PrefixWithNoValuesUnderneath_YieldsEmpty()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.Add(new[] { "eu", "de" }, 1);

            map.GetByPrefix(new[] { "eu", "de", "berlin", "mitte" }).ShouldBeEmpty();
        }

        [Fact]
        public void GetByPrefix_NullPrefix_ThrowsOnEnumeration()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            Should.Throw<ArgumentNullException>(() => map.GetByPrefix(null!).ToList());
        }

        [Fact]
        public void CountOfPrefix_CountsCompleteKeysNotValues()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.Add(new[] { "eu", "de", "berlin" }, 1);
            map.Add(new[] { "eu", "de", "berlin" }, 2);
            map.Add(new[] { "eu", "de", "munich" }, 3);
            map.Add(new[] { "eu", "fr", "paris" }, 4);

            map.CountOfPrefix(new[] { "eu", "de" }).ShouldBe(2);
            map.CountOfPrefix(new[] { "eu" }).ShouldBe(3);
            map.CountOfPrefix(new[] { "eu", "de", "berlin" }).ShouldBe(1);
            map.CountOfPrefix(new[] { "asia" }).ShouldBe(0);
        }

        [Fact]
        public void ContainsPrefix_TrueForPurePrefixNodes()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.Add(new[] { "eu", "de", "berlin" }, 1);

            map.ContainsPrefix(new[] { "eu" }).ShouldBeTrue();
            map.ContainsPrefix(new[] { "eu", "de" }).ShouldBeTrue();
            map.ContainsPrefix(new[] { "eu", "dz" }).ShouldBeFalse();
        }

        [Fact]
        public void GetSuffixes_ReturnsRelativeKeySequences()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.Add(new[] { "eu", "de", "berlin" }, 1);
            map.Add(new[] { "eu", "de", "munich" }, 2);
            map.Add(new[] { "eu", "fr", "paris" }, 3);

            var suffixes = map.GetSuffixes(new[] { "eu" })
                .Select(suffix => string.Join("/", suffix))
                .ToArray();

            suffixes.ShouldBe(new[] { "de/berlin", "de/munich", "fr/paris" });
        }

        [Fact]
        public void GetBranches_ReturnsDistinctNextComponents()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.Add(new[] { "eu", "de" }, 1);
            map.Add(new[] { "eu", "fr" }, 2);

            map.GetBranches(new[] { "eu" }).ShouldBe(new[] { "de", "fr" });
            map.GetBranches(new[] { "asia" }).ShouldBeEmpty();
        }

        [Fact]
        public void RemovePrefix_CascadeDeletesAndReturnsValueCount()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.Add(new[] { "eu", "de", "berlin" }, 1);
            map.Add(new[] { "eu", "de", "berlin" }, 2);
            map.Add(new[] { "eu", "de", "munich" }, 3);
            map.Add(new[] { "eu", "fr", "paris" }, 4);

            // 3 values under two keys are destroyed; the return value counts values, not keys.
            map.RemovePrefix(new[] { "eu", "de" }).ShouldBe(3);

            map.ContainsKey(new[] { "eu", "de", "berlin" }).ShouldBeFalse();
            map.ContainsPrefix(new[] { "eu", "de" }).ShouldBeFalse();
            map.ContainsKey(new[] { "eu", "fr", "paris" }).ShouldBeTrue();
            map.TotalValueCount.ShouldBe(1);
            map.Count.ShouldBe(1);
        }

        [Fact]
        public void RemovePrefix_WithKeyStoredAtPrefix_RemovesItToo()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.Add(new[] { "eu", "de" }, 1);
            map.Add(new[] { "eu", "de", "berlin" }, 2);

            map.RemovePrefix(new[] { "eu", "de" }).ShouldBe(2);
            map.IsEmpty.ShouldBeTrue();
        }

        [Fact]
        public void RemovePrefix_MissingPrefix_ReturnsZero()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.Add(new[] { "eu", "de" }, 1);

            map.RemovePrefix(new[] { "asia" }).ShouldBe(0);
            map.TotalValueCount.ShouldBe(1);
        }

        [Fact]
        public void RemovePrefix_NullPrefix_ThrowsEagerly()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            Should.Throw<ArgumentNullException>(() => map.RemovePrefix(null!));
        }

        // ------------------------------------------------------------------
        // Per-key value set operations
        // ------------------------------------------------------------------

        [Fact]
        public void UnionWith_AddsOnlyMissingValuesAndCreatesTheKey()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            var key = new[] { "eu", "de" };
            map.Add(key, 1);

            map.UnionWith(key, new[] { 1, 2, 2, 3 });

            map[key].ShouldBe(new[] { 1, 2, 3 });
            map.TotalValueCount.ShouldBe(3);
        }

        [Fact]
        public void UnionWith_MissingKey_CreatesIt()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            var key = new[] { "eu", "de" };

            map.UnionWith(key, new[] { 1, 2 });

            map.ContainsKey(key).ShouldBeTrue();
            map[key].ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public void UnionWith_NullArgument_ThrowsArgumentNullException()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            Should.Throw<ArgumentNullException>(() => map.UnionWith(new[] { "eu" }, null!));
        }

        [Fact]
        public void IntersectionWith_KeepsOnlySharedValues()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            var key = new[] { "eu", "de" };
            map.AddRange(key, new[] { 1, 2, 2, 3 });

            map.IntersectionWith(key, new[] { 2, 3, 4 });

            // Both copies of 2 survive: the argument is a set, the inner collection keeps its
            // multiplicities.
            map[key].ShouldBe(new[] { 2, 2, 3 });
        }

        [Fact]
        public void IntersectionWith_NoValuesLeft_DropsTheKey()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            var key = new[] { "eu", "de" };
            map.Add(key, 1);

            map.IntersectionWith(key, new[] { 99 });

            map.ContainsKey(key).ShouldBeFalse();
            map.IsEmpty.ShouldBeTrue();
        }

        [Fact]
        public void IntersectionWith_MissingKey_IsNoOp()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.IntersectionWith(new[] { "missing" }, new[] { 1 });
            map.IsEmpty.ShouldBeTrue();
        }

        [Fact]
        public void ExceptWith_RemovesEveryOccurrence()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            var key = new[] { "eu", "de" };
            map.AddRange(key, new[] { 1, 1, 1, 2 });

            map.ExceptWith(key, new[] { 1 });

            map[key].ShouldBe(new[] { 2 });
            map.ExceptWith(key, new[] { 2 });
            map.ContainsKey(key).ShouldBeFalse();
        }

        [Fact]
        public void ExceptWith_MissingKey_IsNoOp()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.ExceptWith(new[] { "missing" }, new[] { 1 });
            map.IsEmpty.ShouldBeTrue();
        }

        [Fact]
        public void SymmetricExceptWith_TogglesStoredAndMissingValues()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            var key = new[] { "eu", "de" };
            map.Add(key, 1);
            map.Add(key, 1);
            map.Add(key, 2);

            map.SymmetricExceptWith(key, new[] { 1, 3 });

            // 1 is stored -> one of its two copies cancelled; 2 not listed -> untouched;
            // 3 is not stored -> added.
            map[key].ShouldBe(new[] { 1, 2, 3 });
        }

        [Fact]
        public void SymmetricExceptWith_DeduplicatingInner_YieldsTheSymmetricDifference()
        {
            var map = new MultiKeyMultiDictionary<string, int>(allowDuplicateValues: false);
            var key = new[] { "eu", "de" };
            map.AddRange(key, new[] { 1, 2, 3 });

            map.SymmetricExceptWith(key, new[] { 2, 4 });

            map[key].ShouldBe(new[] { 1, 3, 4 }, ignoreOrder: true);
        }

        [Fact]
        public void SymmetricExceptWith_EmptyArgumentAndMissingKey_IsNoOp()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.SymmetricExceptWith(new[] { "missing" }, new int[0]);
            map.IsEmpty.ShouldBeTrue();
        }

        [Fact]
        public void SymmetricExceptWith_NullArgument_ThrowsArgumentNullException()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            Should.Throw<ArgumentNullException>(() => map.SymmetricExceptWith(new[] { "eu" }, null!));
        }

        // ------------------------------------------------------------------
        // Enumeration, views and export
        // ------------------------------------------------------------------

        [Fact]
        public void GetEnumerator_YieldsOneFlatPairPerValue()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.Add(new[] { "eu", "de", "berlin" }, 1);
            map.Add(new[] { "eu", "de", "berlin" }, 2);
            map.Add(new[] { "eu", "fr", "paris" }, 3);

            var pairs = map.ToList();

            pairs.Count.ShouldBe(3);
            pairs.Count(p => p.Key.SequenceEqual(new[] { "eu", "de", "berlin" })).ShouldBe(2);
            pairs.Count(p => p.Key.SequenceEqual(new[] { "eu", "fr", "paris" })).ShouldBe(1);
        }

        [Fact]
        public void KeysAndValues_EnumerateAllKeysAndFlattenedValues()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.Add(new[] { "eu", "de", "berlin" }, 1);
            map.Add(new[] { "eu", "de", "berlin" }, 2);
            map.Add(new[] { "eu", "fr", "paris" }, 3);

            map.Keys.Count().ShouldBe(2);
            map.Keys.ShouldContain(k => k.SequenceEqual(new[] { "eu", "de", "berlin" }));
            map.Keys.ShouldContain(k => k.SequenceEqual(new[] { "eu", "fr", "paris" }));
            map.Values.OrderBy(v => v).ShouldBe(new[] { 1, 2, 3 });
        }

        [Fact]
        public void ToString_FormatsExpandedPerKey()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.Add(new[] { "eu", "de" }, 1);
            map.Add(new[] { "eu", "de" }, 2);
            map.Add(new[] { "eu", "fr" }, 3);

            map.ToString().ShouldBe("[eu,de]:[1,2],[eu,fr]:[3]");
        }

        [Fact]
        public void AsReadOnly_IsLiveAndCountsValues()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            var key = new[] { "eu", "de" };
            map.Add(key, 1);
            var view = map.AsReadOnly();

            view.Count.ShouldBe(1);
            map.Add(key, 2);
            map.Add(new[] { "eu", "fr" }, 3);

            view.Count.ShouldBe(3);
            view.Select(p => p.Value).OrderBy(v => v).ShouldBe(new[] { 1, 2, 3 });
        }

        [Fact]
        public void Clone_IsIndependentOfTheOriginal()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            var key = new[] { "eu", "de" };
            map.Add(key, 1);

            var clone = map.Clone();
            clone.Add(key, 2);
            map.Remove(key);

            map.IsEmpty.ShouldBeTrue();
            clone[key].ShouldBe(new[] { 1, 2 });
            clone.TotalValueCount.ShouldBe(2);
        }

        [Fact]
        public void ToDictionary_OuterSnapshotWithLiveInnerCollections()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            var key = new[] { "eu", "de", "berlin" };
            map.Add(key, 1);
            map.Add(new[] { "us", "nyc" }, 9);

            var snapshot = map.ToDictionary();

            snapshot.Count.ShouldBe(2);
            snapshot.ContainsKey(key).ShouldBeTrue();
            snapshot.ContainsKey(new[] { "us", "nyc" }).ShouldBeTrue();

            // The outer dictionary is independent: later keys do not appear in it ...
            map.Add(new[] { "asia", "tokyo" }, 5);
            snapshot.Count.ShouldBe(2);

            // ... while the inner collections stay live views.
            map.Add(key, 2);
            snapshot[key].ShouldBe(new[] { 1, 2 });
            map.Remove(key, 1);
            snapshot[key].ShouldBe(new[] { 2 });
        }

        [Fact]
        public void ToDictionary_PrefixVariant_ExportsOnlyTheSubtree()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            map.Add(new[] { "eu", "de", "berlin" }, 1);
            map.Add(new[] { "eu", "fr", "paris" }, 2);
            map.Add(new[] { "us", "nyc" }, 3);

            var snapshot = map.ToDictionary(new[] { "eu" });

            snapshot.Count.ShouldBe(2);
            snapshot.ContainsKey(new[] { "eu", "de", "berlin" }).ShouldBeTrue();
            snapshot.ContainsKey(new[] { "us", "nyc" }).ShouldBeFalse();
        }

        [Fact]
        public void ToDictionary_NullPrefix_ThrowsArgumentNullException()
        {
            var map = new MultiKeyMultiDictionary<string, int>();
            Should.Throw<ArgumentNullException>(() => map.ToDictionary(null!));
        }

        // ------------------------------------------------------------------
        // Randomized cross-check against a Dictionary model
        // ------------------------------------------------------------------

        [Fact]
        public void RandomizedOperations_MatchDictionaryModel()
        {
            const int steps = 1500;
            var random = new Random(20260914);
            var map = new MultiKeyMultiDictionary<string, int>();
            var model = new Dictionary<string, List<int>>();
            var components = new[] { "a", "b", "c", "d" };

            string RandomKey()
            {
                var length = random.Next(1, 4);
                return string.Join("|", Enumerable.Range(0, length).Select(_ => components[random.Next(components.Length)]));
            }

            string[] KeyComponents(string key) => key.Split('|');

            for (var step = 0; step < steps; step++)
            {
                var key = RandomKey();
                var components_ = KeyComponents(key);
                switch (random.Next(8))
                {
                    case 0:
                    case 1:
                    case 2:
                        var value = random.Next(10);
                        map.Add(components_, value);
                        if (!model.TryGetValue(key, out var list))
                        {
                            list = new List<int>();
                            model.Add(key, list);
                        }

                        list.Add(value);
                        break;

                    case 3:
                        var stored = model.ContainsKey(key) && model[key].Count > 0
                            ? model[key][random.Next(model[key].Count)]
                            : random.Next(10);
                        var expectedRemove = false;
                        if (model.TryGetValue(key, out var m1))
                        {
                            expectedRemove = m1.Remove(stored);
                            if (m1.Count == 0)
                            {
                                // Mirror the "no value-less key" invariant of the map.
                                model.Remove(key);
                            }
                        }

                        map.Remove(components_, stored).ShouldBe(expectedRemove);
                        break;

                    case 4:
                        map.Remove(components_).ShouldBe(model.Remove(key));
                        break;

                    case 5:
                        var first = random.Next(10);
                        var second = random.Next(10);
                        map.AddRange(components_, new[] { first, second });
                        if (!model.TryGetValue(key, out var m2))
                        {
                            m2 = new List<int>();
                            model.Add(key, m2);
                        }

                        m2.Add(first);
                        m2.Add(second);
                        break;

                    case 6:
                    case 7:
                        var prefixLength = Math.Min(components_.Length, random.Next(1, 3));
                        var prefix = string.Join("|", components_.Take(prefixLength));
                        var expected = model.Where(pair => pair.Key.StartsWith(prefix, StringComparison.Ordinal))
                            .ToList();
                        var expectedValues = expected.Sum(pair => pair.Value.Count);
                        map.RemovePrefix(prefix.Split('|')).ShouldBe(expectedValues);
                        foreach (var pair in expected)
                        {
                            model.Remove(pair.Key);
                        }

                        break;
                }

                // Whole-map invariants after every step.
                map.Count.ShouldBe(model.Count);
                map.TotalValueCount.ShouldBe(model.Values.Sum(l => l.Count));
                map.IsEmpty.ShouldBe(model.Count == 0);

                // Sampled per-key agreement, including the flat (key, value) enumeration order.
                var sampledKey = RandomKey();
                var sampledComponents = KeyComponents(sampledKey);
                map.ContainsKey(sampledComponents).ShouldBe(model.ContainsKey(sampledKey));
                if (model.TryGetValue(sampledKey, out var modelValues))
                {
                    map[sampledComponents].ShouldBe(modelValues);
                    foreach (var v in modelValues.Distinct())
                    {
                        map.Contains(sampledComponents, v).ShouldBeTrue();
                    }
                }
            }
        }
    }
}
