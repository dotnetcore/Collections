using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    public class MultiKeyDictionaryTests
    {
        // ------------------------------------------------------------------
        // Add / indexer / TryGetValue
        // ------------------------------------------------------------------

        [Fact]
        public void Add_SingleEntry_CountIsOne()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a", "b" }, 1);

            map.Count.ShouldBe(1);
            map.IsEmpty.ShouldBeFalse();
        }

        [Fact]
        public void Add_DifferentArities_AllStored()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a" }, 1);
            map.Add(new[] { "a", "b" }, 2);
            map.Add(new[] { "a", "b", "c" }, 3);

            map.Count.ShouldBe(3);
            map[new[] { "a" }].ShouldBe(1);
            map[new[] { "a", "b" }].ShouldBe(2);
            map[new[] { "a", "b", "c" }].ShouldBe(3);
        }

        [Fact]
        public void Add_SharedPrefix_SharesTrieNodes()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a", "b", "c" }, 1);
            map.Add(new[] { "a", "b", "d" }, 2);

            // root + "a" + "b" + "c" + "d" = 5 nodes for 2 entries
            map.NodeCount.ShouldBe(5);
            map.Count.ShouldBe(2);
        }

        [Fact]
        public void Add_SameKeyTwice_OverwritesByDefault()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a" }, 1);
            map.Add(new[] { "a" }, 2);

            map.Count.ShouldBe(1);
            map[new[] { "a" }].ShouldBe(2);
        }

        [Fact]
        public void Add_OverwriteFalse_WithExistingKey_Throws()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a" }, 1);

            Should.Throw<ArgumentException>(() => map.Add(new[] { "a" }, 2, overwrite: false));
            map[new[] { "a" }].ShouldBe(1);
        }

        [Fact]
        public void Add_OverwriteFalse_WithNewKey_AddsAndReturnsFalse()
        {
            var map = new MultiKeyDictionary<string, int>();

            map.Add(new[] { "a" }, 1, overwrite: false).ShouldBeFalse();
            map.Count.ShouldBe(1);
        }

        [Fact]
        public void Add_OverwriteTrue_ReturnsWhetherPreviousValueWasReplaced()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a" }, 1, overwrite: true).ShouldBeFalse();
            map.Add(new[] { "a" }, 2, overwrite: true).ShouldBeTrue();
        }

        [Fact]
        public void Add_NullKey_Throws()
        {
            var map = new MultiKeyDictionary<string, int>();

            Should.Throw<ArgumentNullException>(() => map.Add(null!, 1));
        }

        [Fact]
        public void TryAdd_NewKey_ReturnsTrueAndStores()
        {
            var map = new MultiKeyDictionary<string, int>();

            map.TryAdd(new[] { "a" }, 1).ShouldBeTrue();
            map[new[] { "a" }].ShouldBe(1);
        }

        [Fact]
        public void TryAdd_ExistingKey_ReturnsFalseAndKeepsValue()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a" }, 1);

            map.TryAdd(new[] { "a" }, 2).ShouldBeFalse();
            map[new[] { "a" }].ShouldBe(1);
        }

        [Fact]
        public void Indexer_Setter_StoresValue()
        {
            var map = new MultiKeyDictionary<string, int> { [new[] { "a", "b" }] = 7 };

            map[new[] { "a", "b" }].ShouldBe(7);
        }

        [Fact]
        public void Indexer_Getter_MissingKey_Throws()
        {
            var map = new MultiKeyDictionary<string, int>();

            Should.Throw<KeyNotFoundException>(() => map[new[] { "missing" }]);
        }

        [Fact]
        public void Indexer_Setter_OverwritesExisting()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a" }, 1);

            map[new[] { "a" }] = 9;
            map[new[] { "a" }].ShouldBe(9);
            map.Count.ShouldBe(1);
        }

        [Fact]
        public void TryGetValue_ExistingKey_ReturnsTrue()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a", "b" }, 5);

            map.TryGetValue(new[] { "a", "b" }, out var value).ShouldBeTrue();
            value.ShouldBe(5);
        }

        [Fact]
        public void TryGetValue_MissingKey_ReturnsFalseAndDefault()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a", "b" }, 5);

            map.TryGetValue(new[] { "a" }, out var value).ShouldBeFalse();
            value.ShouldBe(0);
        }

        [Fact]
        public void TryGetValue_EmptyKeyArray_ThrowsOnNull()
        {
            var map = new MultiKeyDictionary<string, int>();

            Should.Throw<ArgumentNullException>(() => map.TryGetValue(null!, out _));
        }

        [Fact]
        public void TryGetValue_EmptyKeyArray_IsValidKey()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new string[0], 42);

            map.TryGetValue(new string[0], out var value).ShouldBeTrue();
            value.ShouldBe(42);
        }

        [Fact]
        public void ContainsKey_TrueAndFalse()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a", "b" }, 1);

            map.ContainsKey(new[] { "a", "b" }).ShouldBeTrue();
            map.ContainsKey(new[] { "a" }).ShouldBeFalse();
        }

        [Fact]
        public void Contains_ValueMatch()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a" }, 1);

            map.Contains(new[] { "a" }, 1).ShouldBeTrue();
            map.Contains(new[] { "a" }, 2).ShouldBeFalse();
            map.Contains(new[] { "b" }, 1).ShouldBeFalse();
        }

        // ------------------------------------------------------------------
        // Prefix projection
        // ------------------------------------------------------------------

        [Fact]
        public void GetByPrefix_FullKeyPath_EnumeratesSubtree()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "eu", "de", "berlin" }, 1);
            map.Add(new[] { "eu", "de", "munich" }, 2);
            map.Add(new[] { "eu", "fr", "paris" }, 3);

            var keys = map.GetByPrefix(new[] { "eu", "de" })
                .Select(e => string.Join(",", e.Key))
                .OrderBy(k => k)
                .ToList();

            keys.ShouldBe(new[] { "eu,de,berlin", "eu,de,munich" });
        }

        [Fact]
        public void GetByPrefix_Relative_RebasesKeysOntoPrefix()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "eu", "de", "berlin" }, 1);
            map.Add(new[] { "eu", "de", "munich" }, 2);

            var keys = map.GetByPrefix(new[] { "eu", "de" }, relative: true)
                .Select(e => string.Join(",", e.Key))
                .OrderBy(k => k)
                .ToList();

            keys.ShouldBe(new[] { "berlin", "munich" });
        }

        [Fact]
        public void GetByPrefix_IncludesEntryStoredAtPrefixItself()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "eu", "de" }, 99);
            map.Add(new[] { "eu", "de", "berlin" }, 1);

            var entries = map.GetByPrefix(new[] { "eu", "de" }).ToList();

            entries.Count.ShouldBe(2);
            entries.ShouldContain(e => e.Key.SequenceEqual(new[] { "eu", "de" }) && e.Value == 99);
            entries.ShouldContain(e => e.Key.SequenceEqual(new[] { "eu", "de", "berlin" }) && e.Value == 1);
        }

        [Fact]
        public void GetByPrefix_Relative_EntryAtPrefix_YieldsEmptyKey()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "eu", "de" }, 99);

            var keys = map.GetByPrefix(new[] { "eu", "de" }, relative: true).ToList();

            keys.Count.ShouldBe(1);
            keys[0].Key.ShouldBeEmpty();
            keys[0].Value.ShouldBe(99);
        }

        [Fact]
        public void GetByPrefix_MissingPrefix_IsEmpty()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "eu", "de" }, 1);

            map.GetByPrefix(new[] { "us" }).ShouldBeEmpty();
        }

        [Fact]
        public void GetByPrefix_EmptyPrefix_EnumeratesEverything()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a" }, 1);
            map.Add(new[] { "a", "b" }, 2);
            map.Add(new[] { "c" }, 3);

            map.GetByPrefix(new string[0]).Count().ShouldBe(3);
        }

        [Fact]
        public void GetByPrefix_NullPrefix_Throws()
        {
            var map = new MultiKeyDictionary<string, int>();

            Should.Throw<ArgumentNullException>(() => map.GetByPrefix(null!).ToList());
        }

        [Fact]
        public void GetSuffixes_ReturnsRelativeKeysOnly()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "eu", "de", "berlin" }, 1);
            map.Add(new[] { "eu", "de" }, 2);

            var suffixes = map.GetSuffixes(new[] { "eu", "de" })
                .Select(s => string.Join(",", s))
                .ToList();

            suffixes.Count.ShouldBe(2);
            suffixes.ShouldContain("");
            suffixes.ShouldContain("berlin");
        }

        [Fact]
        public void CountOfPrefix_CountsWholeSubtree()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "eu", "de", "berlin" }, 1);
            map.Add(new[] { "eu", "de", "munich" }, 2);
            map.Add(new[] { "eu", "fr", "paris" }, 3);

            map.CountOfPrefix(new[] { "eu" }).ShouldBe(3);
            map.CountOfPrefix(new[] { "eu", "de" }).ShouldBe(2);
            map.CountOfPrefix(new[] { "eu", "de", "berlin" }).ShouldBe(1);
            map.CountOfPrefix(new[] { "us" }).ShouldBe(0);
        }

        [Fact]
        public void CountOfPrefix_EmptyPrefix_EqualsCount()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a" }, 1);
            map.Add(new[] { "b" }, 2);

            map.CountOfPrefix(new string[0]).ShouldBe(map.Count);
        }

        [Fact]
        public void ContainsPrefix_TrueForIntermediateNodeWithoutValue()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "eu", "de", "berlin" }, 1);

            map.ContainsPrefix(new[] { "eu" }).ShouldBeTrue();
            map.ContainsPrefix(new[] { "eu", "de" }).ShouldBeTrue();
            map.ContainsPrefix(new[] { "eu", "fr" }).ShouldBeFalse();
            map.ContainsKey(new[] { "eu" }).ShouldBeFalse();
        }

        [Fact]
        public void GetBranches_ReturnsDirectFollowers()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "eu", "de" }, 1);
            map.Add(new[] { "eu", "fr" }, 2);
            map.Add(new[] { "eu", "de", "berlin" }, 3);

            var branches = map.GetBranches(new[] { "eu" }).OrderBy(b => b).ToList();

            branches.ShouldBe(new[] { "de", "fr" });
        }

        [Fact]
        public void GetBranches_MissingPrefix_IsEmpty()
        {
            var map = new MultiKeyDictionary<string, int>();

            map.GetBranches(new[] { "nope" }).ShouldBeEmpty();
        }

        [Fact]
        public void TryGetValueByPrefix_MatchesTryGetValue()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a", "b" }, 8);

            map.TryGetValueByPrefix(new[] { "a", "b" }, out var value).ShouldBeTrue();
            value.ShouldBe(8);
            map.TryGetValueByPrefix(new[] { "a" }, out _).ShouldBeFalse();
        }

        // ------------------------------------------------------------------
        // Remove / RemovePrefix
        // ------------------------------------------------------------------

        [Fact]
        public void Remove_ExactKey_RemovesOnlyThatEntry()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a", "b" }, 1);
            map.Add(new[] { "a", "b", "c" }, 2);

            map.Remove(new[] { "a", "b" }).ShouldBeTrue();
            map.Count.ShouldBe(1);
            map.ContainsKey(new[] { "a", "b" }).ShouldBeFalse();
            map[new[] { "a", "b", "c" }].ShouldBe(2);
        }

        [Fact]
        public void Remove_MissingKey_ReturnsFalse()
        {
            var map = new MultiKeyDictionary<string, int>();

            map.Remove(new[] { "a" }).ShouldBeFalse();
        }

        [Fact]
        public void Remove_ReturnsRemovedValue()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a" }, 5);

            map.Remove(new[] { "a" }, out var value).ShouldBeTrue();
            value.ShouldBe(5);
            map.Remove(new[] { "a" }, out _).ShouldBeFalse();
        }

        [Fact]
        public void Remove_NullKey_Throws()
        {
            var map = new MultiKeyDictionary<string, int>();

            Should.Throw<ArgumentNullException>(() => map.Remove(null!));
        }

        [Fact]
        public void Remove_PrunesChildlessNodes()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a", "b", "c" }, 1);
            map.NodeCount.ShouldBe(4);

            map.Remove(new[] { "a", "b", "c" });

            map.NodeCount.ShouldBe(1); // only the root survives
            map.Count.ShouldBe(0);
        }

        [Fact]
        public void Remove_KeepsSharedPrefixNodes()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a", "b", "c" }, 1);
            map.Add(new[] { "a", "b", "d" }, 2);

            map.Remove(new[] { "a", "b", "c" });

            map.NodeCount.ShouldBe(4); // root + a + b + d
            map.ContainsPrefix(new[] { "a", "b" }).ShouldBeTrue();
            map.Count.ShouldBe(1);
        }

        [Fact]
        public void RemovePrefix_CascadesWholeSubtree()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "eu", "de", "berlin" }, 1);
            map.Add(new[] { "eu", "de", "munich" }, 2);
            map.Add(new[] { "eu", "fr", "paris" }, 3);

            map.RemovePrefix(new[] { "eu", "de" }).ShouldBe(2);

            map.Count.ShouldBe(1);
            map.CountOfPrefix(new[] { "eu", "de" }).ShouldBe(0);
            map.ContainsKey(new[] { "eu", "fr", "paris" }).ShouldBeTrue();
        }

        [Fact]
        public void RemovePrefix_IncludesEntryStoredAtPrefix()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "eu", "de" }, 1);
            map.Add(new[] { "eu", "de", "berlin" }, 2);

            map.RemovePrefix(new[] { "eu", "de" }).ShouldBe(2);
            map.Count.ShouldBe(0);
        }

        [Fact]
        public void RemovePrefix_MissingPrefix_ReturnsZero()
        {
            var map = new MultiKeyDictionary<string, int>();

            map.RemovePrefix(new[] { "nope" }).ShouldBe(0);
        }

        [Fact]
        public void RemovePrefix_EmptyPrefix_ClearsEverything()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a" }, 1);
            map.Add(new[] { "b", "c" }, 2);

            map.RemovePrefix(new string[0]).ShouldBe(2);
            map.Count.ShouldBe(0);
            map.NodeCount.ShouldBe(1);
        }

        [Fact]
        public void RemovePrefix_NullPrefix_Throws()
        {
            var map = new MultiKeyDictionary<string, int>();

            Should.Throw<ArgumentNullException>(() => map.RemovePrefix(null!));
        }

        [Fact]
        public void Clear_EmptiesTheTrie()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a", "b" }, 1);
            map.Add(new[] { "c" }, 2);

            map.Clear();

            map.Count.ShouldBe(0);
            map.NodeCount.ShouldBe(1);
            map.IsEmpty.ShouldBeTrue();
        }

        [Fact]
        public void RemoveThenAdd_ReusesCleanTrie()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a", "b" }, 1);
            map.Remove(new[] { "a", "b" });
            map.Add(new[] { "a", "b" }, 2);

            map.Count.ShouldBe(1);
            map[new[] { "a", "b" }].ShouldBe(2);
            map.NodeCount.ShouldBe(3);
        }

        // ------------------------------------------------------------------
        // null key components
        // ------------------------------------------------------------------

        [Fact]
        public void NullComponent_IsStoredAndRetrieved()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a", null!, "c" }, 1);

            map.TryGetValue(new[] { "a", null!, "c" }, out var value).ShouldBeTrue();
            value.ShouldBe(1);
            map.Count.ShouldBe(1);
        }

        [Fact]
        public void NullComponent_DistinctFromNonNull()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a", null! }, 1);
            map.Add(new[] { "a", "b" }, 2);

            map[new[] { "a", null! }].ShouldBe(1);
            map[new[] { "a", "b" }].ShouldBe(2);
            map.Count.ShouldBe(2);
        }

        [Fact]
        public void NullComponent_AsWholeKey_Works()
        {
            var map = new MultiKeyDictionary<string, int>();
            var nullOnly = new string[] { null! };
            map.Add(nullOnly!, 7);

            map[nullOnly!].ShouldBe(7);
            map.ContainsKey(nullOnly!).ShouldBeTrue();
        }

        [Fact]
        public void NullComponent_SurvivesEnumeration()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a", null! }, 1);

            var keys = map.Select(e => e.Key).ToList();

            keys.Count.ShouldBe(1);
            keys[0].Length.ShouldBe(2);
            keys[0][0].ShouldBe("a");
            keys[0][1].ShouldBeNull();
        }

        [Fact]
        public void NullComponent_RemovedByExactRemove()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a", null! }, 1);

            map.Remove(new[] { "a", null! }).ShouldBeTrue();
            map.Count.ShouldBe(0);
            map.NodeCount.ShouldBe(1);
        }

        [Fact]
        public void NullComponent_RemovedByPrefix()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a", null!, "c" }, 1);
            map.Add(new[] { "a", "b" }, 2);

            map.RemovePrefix(new[] { "a", null! }).ShouldBe(1);
            map.Count.ShouldBe(1);
        }

        [Fact]
        public void NullComponent_GetByPrefix_IncludesNullBranch()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a", null!, "c" }, 1);
            map.Add(new[] { "a", "b" }, 2);

            map.GetByPrefix(new[] { "a" }).Count().ShouldBe(2);
        }

        [Fact]
        public void NullComponent_GetBranches_IncludesNull()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a", null! }, 1);
            map.Add(new[] { "a", "b" }, 2);

            var branches = map.GetBranches(new[] { "a" }).ToList();

            branches.Count.ShouldBe(2);
            branches.ShouldContain(x => x == null);
            branches.ShouldContain("b");
        }

        // ------------------------------------------------------------------
        // Comparer injection
        // ------------------------------------------------------------------

        [Fact]
        public void Comparer_CaseInsensitiveComponents_Collapse()
        {
            var map = new MultiKeyDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            map.Add(new[] { "EU", "DE" }, 1);
            map.Add(new[] { "eu", "de" }, 2);

            map.Count.ShouldBe(1);
            map[new[] { "Eu", "dE" }].ShouldBe(2);
        }

        [Fact]
        public void Comparer_CaseInsensitivePrefixLookup_Matches()
        {
            var map = new MultiKeyDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            map.Add(new[] { "EU", "DE", "Berlin" }, 1);

            map.GetByPrefix(new[] { "eu", "de" }).Count().ShouldBe(1);
        }

        [Fact]
        public void Comparer_IsExposed()
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            var map = new MultiKeyDictionary<string, int>(comparer);

            map.Comparer.ShouldBeSameAs(comparer);
        }

        [Fact]
        public void Comparer_DefaultWhenNull()
        {
            var map = new MultiKeyDictionary<string, int>((IEqualityComparer<string>)null!);

            map.Comparer.ShouldBeSameAs(EqualityComparer<string>.Default);
        }

        [Fact]
        public void Comparer_CaseSensitiveByDefault_DistinctKeysStaySeparate()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "EU" }, 1);
            map.Add(new[] { "eu" }, 2);

            map.Count.ShouldBe(2);
        }

        // ------------------------------------------------------------------
        // Clone / AsReadOnly / ToDictionary
        // ------------------------------------------------------------------

        [Fact]
        public void Clone_IsIndependent()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a", "b" }, 1);

            var clone = map.Clone();
            clone.Add(new[] { "a", "c" }, 2);
            clone.Remove(new[] { "a", "b" });

            map.Count.ShouldBe(1);
            map.ContainsKey(new[] { "a", "b" }).ShouldBeTrue();
            clone.Count.ShouldBe(1);
            clone.ContainsKey(new[] { "a", "c" }).ShouldBeTrue();
        }

        [Fact]
        public void Clone_PreservesComparer()
        {
            var map = new MultiKeyDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            map.Add(new[] { "A" }, 1);

            var clone = map.Clone();

            clone.Comparer.ShouldBeSameAs(StringComparer.OrdinalIgnoreCase);
            clone[new[] { "a" }].ShouldBe(1);
        }

        [Fact]
        public void Clone_PreservesNullComponents()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a", null! }, 1);

            var clone = map.Clone();

            clone.ContainsKey(new[] { "a", null! }).ShouldBeTrue();
        }

        [Fact]
        public void AsReadOnly_ReflectsSubsequentChanges()
        {
            var map = new MultiKeyDictionary<string, int>();
            var view = map.AsReadOnly();

            view.Count.ShouldBe(0);
            map.Add(new[] { "a" }, 1);
            view.Count.ShouldBe(1);
            view.ShouldContain(e => e.Value == 1);
        }

        [Fact]
        public void AsReadOnly_ExposesNoMutatingMembers()
        {
            var map = new MultiKeyDictionary<string, int>();

            var view = map.AsReadOnly();

            view.ShouldBeAssignableTo<IReadOnlyCollection<(string[], int)>>();
            view.GetType().GetMethods()
                .Select(m => m.Name)
                .ShouldNotContain("Add");
            view.GetType().GetMethods()
                .Select(m => m.Name)
                .ShouldNotContain("Remove");
        }

        [Fact]
        public void ToDictionary_EmptyPrefix_ContainsWholeMap()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a" }, 1);
            map.Add(new[] { "a", "b" }, 2);

            var snapshot = map.ToDictionary();

            snapshot.Count.ShouldBe(2);
            snapshot[new[] { "a" }].ShouldBe(1);
            snapshot[new[] { "a", "b" }].ShouldBe(2);
        }

        [Fact]
        public void ToDictionary_Prefix_LimitsToSubtree()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "eu", "de" }, 1);
            map.Add(new[] { "us", "ny" }, 2);

            var snapshot = map.ToDictionary(new[] { "eu" });

            snapshot.Count.ShouldBe(1);
            snapshot[new[] { "eu", "de" }].ShouldBe(1);
            snapshot.ContainsKey(new[] { "us", "ny" }).ShouldBeFalse();
        }

        [Fact]
        public void ToDictionary_KeysAreSequenceEqual()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a", "b" }, 1);

            var snapshot = map.ToDictionary();

            // distinct array instances with the same content must resolve to the same entry
            snapshot[new[] { "a", "b" }].ShouldBe(1);
        }

        [Fact]
        public void ToDictionary_IsIndependentSnapshot()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a" }, 1);

            var snapshot = map.ToDictionary();
            map.Add(new[] { "b" }, 2);

            snapshot.Count.ShouldBe(1);
        }

        [Fact]
        public void ToDictionary_NullPrefix_Throws()
        {
            var map = new MultiKeyDictionary<string, int>();

            Should.Throw<ArgumentNullException>(() => map.ToDictionary(null!));
        }

        // ------------------------------------------------------------------
        // Enumeration / Keys / Values / ToString
        // ------------------------------------------------------------------

        [Fact]
        public void Enumeration_YieldsEveryEntryOnce()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a" }, 1);
            map.Add(new[] { "a", "b" }, 2);
            map.Add(new[] { "c" }, 3);

            var values = map.Select(e => e.Value).OrderBy(v => v).ToList();

            values.ShouldBe(new[] { 1, 2, 3 });
        }

        [Fact]
        public void Enumeration_KeysAreFreshArrays()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a", "b" }, 1);

            var key = map.First().Key;
            key[0] = "mutated";

            map.ContainsKey(new[] { "a", "b" }).ShouldBeTrue();
        }

        [Fact]
        public void Keys_ReturnsEveryKey()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a" }, 1);
            map.Add(new[] { "b", "c" }, 2);

            map.Keys.Count().ShouldBe(2);
        }

        [Fact]
        public void Values_ReturnsEveryValue()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a" }, 1);
            map.Add(new[] { "b" }, 2);

            map.Values.OrderBy(v => v).ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public void EmptyMap_IsEmpty()
        {
            var map = new MultiKeyDictionary<string, int>();

            map.Count.ShouldBe(0);
            map.IsEmpty.ShouldBeTrue();
            map.ShouldBeEmpty();
            map.NodeCount.ShouldBe(1);
        }

        [Fact]
        public void ToString_FormatsKeyValuePairs()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a" }, 1);

            map.ToString().ShouldBe("[a]:1");
        }

        [Fact]
        public void ToString_MultipleEntries_CommaSeparated()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a", "b" }, 1);

            map.ToString().ShouldContain("[a,b]:1");
        }

        // ------------------------------------------------------------------
        // Idempotence / interaction between operations
        // ------------------------------------------------------------------

        [Fact]
        public void Remove_IsIdempotent()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a" }, 1);

            map.Remove(new[] { "a" }).ShouldBeTrue();
            map.Remove(new[] { "a" }).ShouldBeFalse();
            map.Remove(new[] { "a" }).ShouldBeFalse();
            map.Count.ShouldBe(0);
        }

        [Fact]
        public void RemovePrefix_IsIdempotent()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a", "b" }, 1);

            map.RemovePrefix(new[] { "a" }).ShouldBe(1);
            map.RemovePrefix(new[] { "a" }).ShouldBe(0);
            map.NodeCount.ShouldBe(1);
        }

        [Fact]
        public void AddRemoveAdd_CycleKeepsCountConsistent()
        {
            var map = new MultiKeyDictionary<string, int>();

            for (var i = 0; i < 5; i++)
            {
                map.Add(new[] { "k", i.ToString() }, i);
            }

            map.Count.ShouldBe(5);

            for (var i = 0; i < 5; i++)
            {
                map.Remove(new[] { "k", i.ToString() }).ShouldBeTrue();
            }

            map.Count.ShouldBe(0);
            map.NodeCount.ShouldBe(1);
        }

        [Fact]
        public void RemovePrefix_ThenReAdd_Works()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "eu", "de" }, 1);
            map.Add(new[] { "eu", "fr" }, 2);

            map.RemovePrefix(new[] { "eu" }).ShouldBe(2);
            map.Add(new[] { "eu", "it" }, 3);

            map.Count.ShouldBe(1);
            map[new[] { "eu", "it" }].ShouldBe(3);
        }

        // ------------------------------------------------------------------
        // Value semantics
        // ------------------------------------------------------------------

        [Fact]
        public void NullValues_AreAllowed()
        {
            var map = new MultiKeyDictionary<string, string>();
            map.Add(new[] { "a" }, null!);

            map.TryGetValue(new[] { "a" }, out var value).ShouldBeTrue();
            value.ShouldBeNull();
            map.ContainsKey(new[] { "a" }).ShouldBeTrue();
        }

        [Fact]
        public void ValueTypes_DefaultValueIsStoredAndDistinguished()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new[] { "a" }, 0);

            map.ContainsKey(new[] { "a" }).ShouldBeTrue();
            map.TryGetValue(new[] { "a" }, out var value).ShouldBeTrue();
            value.ShouldBe(0);
        }

        [Fact]
        public void EmptyKeyArray_IsTheRootEntry()
        {
            var map = new MultiKeyDictionary<string, int>();
            map.Add(new string[0], 1);

            map.GetByPrefix(new string[0]).Count().ShouldBe(1);
            map.Remove(new string[0]).ShouldBeTrue();
            map.Count.ShouldBe(0);
        }
    }
}
