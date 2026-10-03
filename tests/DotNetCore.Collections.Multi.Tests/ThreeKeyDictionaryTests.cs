using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// M6-13: <see cref="ThreeKeyDictionary{K1,K2,K3,V}"/> as a thin wrapper over
    /// <see cref="MultiKeyDictionary{TKey,TValue}"/> - the typed three-axis indexer, the per-axis
    /// projections, the first-axis prefix slice, and the axis-tag scheme that keeps three
    /// same-typed axes from colliding.
    /// </summary>
    public class ThreeKeyDictionaryTests
    {
        [Fact]
        public void Indexer_Add_TryAdd_AndOverwriteSemantics_MatchTheTwoKeyWrapper()
        {
            var map = new ThreeKeyDictionary<int, string, int, decimal>();

            map[2026, "09", 11] = 1.00m;
            map[2026, "09", 11] = 1.01m;              // overwrites

            map.Count.ShouldBe(1);
            map[2026, "09", 11].ShouldBe(1.01m);
            map.ContainsKey(2026, "09", 11).ShouldBeTrue();
            map.TryAdd(2026, "09", 11, 9.99m).ShouldBeFalse();
            map[2026, "09", 11].ShouldBe(1.01m);
            map.TryAdd(2026, "09", 12, 9.99m).ShouldBeTrue();

            Should.Throw<ArgumentException>(() => map.Add(2026, "09", 11, 2.00m, overwrite: false));
            map.Add(2026, "09", 11, 2.00m, overwrite: true).ShouldBeTrue();

            Should.Throw<KeyNotFoundException>(() => map[2026, "09", 13]);
        }

        [Fact]
        public void FirstAxis_IsAPrefix_SliceQueriesAreNativelyAnswered()
        {
            var map = new ThreeKeyDictionary<int, string, int, decimal>();
            map.Add(1, "USD", 2026, 1.00m);
            map.Add(1, "USD", 2025, 0.99m);
            map.Add(1, "EUR", 2026, 0.92m);
            map.Add(2, "USD", 2026, 1.00m);

            map.ContainsFirstKey(1).ShouldBeTrue();
            map.ContainsFirstKey(3).ShouldBeFalse();
            map.CountOfFirstKey(1).ShouldBe(3);
            map.CountOfFirstKey(2).ShouldBe(1);

            var slice = map.GetByFirstKey(1).ToList();
            slice.Count.ShouldBe(3);
            slice.ShouldContain((Key2: "USD", Key3: 2026, Value: 1.00m));
            slice.ShouldContain((Key2: "USD", Key3: 2025, Value: 0.99m));
            slice.ShouldContain((Key2: "EUR", Key3: 2026, Value: 0.92m));

            map.RemoveByFirstKey(1).ShouldBe(3);
            map.Count.ShouldBe(1);
            map.ContainsKey(1, "USD", 2026).ShouldBeFalse();
            map.ContainsKey(2, "USD", 2026).ShouldBeTrue();
        }

        [Fact]
        public void SecondAndThirdAxis_AreNotPrefixes_AreDocumentedAsOofN()
        {
            var map = new ThreeKeyDictionary<int, string, int, decimal>();
            map.Add(1, "USD", 2026, 1.00m);
            map.Add(2, "USD", 2025, 0.99m);
            map.Add(3, "EUR", 2026, 0.92m);

            map.ContainsSecondKey("USD").ShouldBeTrue();
            map.ContainsSecondKey("GBP").ShouldBeFalse();
            map.CountOfSecondKey("USD").ShouldBe(2);

            var secondSlice = map.GetBySecondKey("USD").ToList();
            secondSlice.Count.ShouldBe(2);
            secondSlice.ShouldContain((Key1: 1, Key3: 2026, Value: 1.00m));
            secondSlice.ShouldContain((Key1: 2, Key3: 2025, Value: 0.99m));

            map.ContainsThirdKey(2026).ShouldBeTrue();
            map.ContainsThirdKey(2024).ShouldBeFalse();
            map.CountOfThirdKey(2026).ShouldBe(2);
            map.GetByThirdKey(2026).Select(e => e.Key1).ShouldBe(new[] { 1, 3 });

            map.RemoveBySecondKey("USD").ShouldBe(2);
            map.Count.ShouldBe(1);
            map.ContainsKey(3, "EUR", 2026).ShouldBeTrue();

            map.RemoveByThirdKey(2026).ShouldBe(1);
            map.IsEmpty.ShouldBeTrue();
        }

        [Fact]
        public void AxisTags_KeepThreeSameTypedAxesIndependent()
        {
            // The common overlap case: all three axes are strings. A plain trie keyed on boxed
            // components would treat ("a" in axis 1) as the same node as ("a" in axis 2).
            var map = new ThreeKeyDictionary<string, string, string, int>();
            map.Add("a", "a", "a", 1);
            map.Add("a", "a", "b", 2);
            map.Add("a", "b", "a", 3);
            map.Add("b", "a", "a", 4);

            map.Count.ShouldBe(4);
            map["a", "a", "a"].ShouldBe(1);
            map["a", "a", "b"].ShouldBe(2);
            map["a", "b", "a"].ShouldBe(3);
            map["b", "a", "a"].ShouldBe(4);
            map.ContainsKey("a", "a", "a").ShouldBeTrue();
            map.ContainsKey("b", "b", "b").ShouldBeFalse();

            map.Remove("a", "a", "a").ShouldBeTrue();
            map.ContainsKey("a", "a", "b").ShouldBeTrue(); // siblings survive
            map.ContainsKey("a", "a", "a").ShouldBeFalse();
        }

        [Fact]
        public void AxisComparers_AreInjectedPerPosition()
        {
            var map = new ThreeKeyDictionary<string, string, string, int>(
                StringComparer.OrdinalIgnoreCase, StringComparer.Ordinal, StringComparer.OrdinalIgnoreCase);

            map.Add("A", "a", "C", 1);
            map.Add("a", "b", "c", 2);

            // Axis 1 and 3 are case-insensitive; axis 2 is not.
            map.ContainsKey("a", "a", "c").ShouldBeTrue();
            map.ContainsKey("A", "A", "C").ShouldBeFalse(); // axis 2 is ordinal: "A" != "a"
            map["a", "a", "C"].ShouldBe(1);
            map.Count.ShouldBe(2);

            map.Comparer1.ShouldBe(StringComparer.OrdinalIgnoreCase);
            map.Comparer2.ShouldBe(StringComparer.Ordinal);
            map.Comparer3.ShouldBe(StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void NullComponents_AreOrdinaryComponents()
        {
            var map = new ThreeKeyDictionary<string, string, string, int>();
            map.Add(null, null, null, 7);

            map.Count.ShouldBe(1);
            map.ContainsKey(null, null, null).ShouldBeTrue();
            map[null, null, null].ShouldBe(7);
            map.ContainsKey("a", null, null).ShouldBeFalse();

            map.Add("a", null, null, 8);
            map.CountOfSecondKey(null).ShouldBe(2);
            map.Remove(null, null, null).ShouldBeTrue();
            map.Count.ShouldBe(1);
        }

        [Fact]
        public void AxisProjections_EnumerateDistinctComponents()
        {
            var map = new ThreeKeyDictionary<int, string, int, decimal>();
            map.Add(1, "USD", 2026, 1.00m);
            map.Add(1, "EUR", 2026, 0.92m);
            map.Add(2, "USD", 2025, 1.00m);

            map.Keys1.ShouldBe(new[] { 1, 2 });
            map.Keys2.OrderBy(k => k).ShouldBe(new[] { "EUR", "USD" });
            map.Keys3.OrderBy(k => k).ShouldBe(new[] { 2025, 2026 });
        }

        [Fact]
        public void Exports_AreIndependentCopies()
        {
            var map = new ThreeKeyDictionary<int, string, int, decimal>();
            map.Add(1, "USD", 2026, 1.00m);

            var clone = map.Clone();
            var asDictionary = map.ToDictionary();
            var asTrie = map.AsTrie();

            clone.Add(2, "EUR", 2025, 9.99m);
            map.ContainsKey(2, "EUR", 2025).ShouldBeFalse();

            asDictionary[(1, "USD", 2026)].ShouldBe(1.00m);
            asTrie.Count.ShouldBe(1);

            var view = map.AsReadOnly();
            map.Add(3, "GBP", 2024, 2.00m);
            view.Count.ShouldBe(2); // live view reflects the owner
        }

        [Fact]
        public void Enumeration_AndToString_CarryEveryEntry()
        {
            var map = new ThreeKeyDictionary<int, string, int, decimal>();
            map.Add(1, "USD", 2026, 1.00m);
            map.Add(2, "EUR", 2025, 0.92m);

            map.Select(e => (e.Key1, e.Key2, e.Key3)).OrderBy(e => e.Key1)
                .ShouldBe(new[] { (1, "USD", 2026), (2, "EUR", 2025) });

            map.ToString().ShouldBe("(1,USD,2026):1.00,(2,EUR,2025):0.92");
        }

        // ------------------------------------------------------------------
        // Values / ContainsValue (F6-39)
        // ------------------------------------------------------------------

        [Fact]
        public void Values_YieldsOneValuePerEntry()
        {
            var map = new ThreeKeyDictionary<int, string, int, decimal>();
            map.Add(1, "USD", 2026, 1.00m);
            map.Add(1, "USD", 2025, 0.98m);
            map.Add(2, "EUR", 2025, 0.92m);

            map.Values.ShouldBe(new[] { 1.00m, 0.98m, 0.92m }, ignoreOrder: true);
            map.Values.Count().ShouldBe(map.Count);
        }

        [Fact]
        public void Values_Empty_IsEmpty()
        {
            var map = new ThreeKeyDictionary<int, string, int, decimal>();

            map.Values.ShouldBeEmpty();
        }

        [Fact]
        public void Values_RepeatsAValueStoredUnderSeveralTriples()
        {
            var map = new ThreeKeyDictionary<int, string, int, int>();
            map.Add(1, "a", 1, 7);
            map.Add(2, "b", 2, 7);

            map.Values.Count().ShouldBe(2);
            map.Values.ShouldAllBe(v => v == 7);
        }

        [Fact]
        public void ContainsValue_FindsAValueUnderAnyTriple()
        {
            var map = new ThreeKeyDictionary<int, string, int, decimal>();
            map.Add(1, "USD", 2026, 1.00m);
            map.Add(2, "EUR", 2025, 0.92m);

            map.ContainsValue(0.92m).ShouldBeTrue();
            map.ContainsValue(1.00m).ShouldBeTrue();
            map.ContainsValue(9.99m).ShouldBeFalse();
        }

        [Fact]
        public void ContainsValue_Empty_IsFalse()
        {
            var map = new ThreeKeyDictionary<int, string, int, decimal>();

            map.ContainsValue(1.00m).ShouldBeFalse();
        }

        [Fact]
        public void ContainsValue_NullValue_IsAnOrdinaryValue()
        {
            var map = new ThreeKeyDictionary<int, string, int, string>();
            map.Add(1, "a", 1, null);

            map.ContainsValue(null).ShouldBeTrue();
            map.ContainsValue("x").ShouldBeFalse();
        }

        [Fact]
        public void ContainsValue_AfterRemove_StopsFinding()
        {
            var map = new ThreeKeyDictionary<int, string, int, int>();
            map.Add(1, "a", 1, 42);

            map.Remove(1, "a", 1);

            map.ContainsValue(42).ShouldBeFalse();
            map.Values.ShouldBeEmpty();
        }

        [Fact]
        public void Values_IsLive()
        {
            var map = new ThreeKeyDictionary<int, string, int, int>();
            map.Add(1, "a", 1, 1);

            var values = map.Values;
            map.Add(2, "b", 2, 2);

            values.ShouldBe(new[] { 1, 2 }, ignoreOrder: true);
        }

        // ------------------------------------------------------------------
        // AsReverse (F6-40, copy semantics)
        // ------------------------------------------------------------------

        [Fact]
        public void AsReverse_ReKeysEntriesAsKey3ThenKey2ThenKey1()
        {
            var map = new ThreeKeyDictionary<int, string, string, decimal>();
            map.Add(1, "USD", "2026Q1", 1.00m);
            map.Add(2, "USD", "2026Q1", 1.05m);
            map.Add(1, "EUR", "2026Q2", 0.92m);

            var reversed = map.AsReverse();

            reversed["2026Q1", "USD", 1].ShouldBe(1.00m);
            reversed["2026Q1", "USD", 2].ShouldBe(1.05m);
            reversed["2026Q2", "EUR", 1].ShouldBe(0.92m);
            reversed.Count.ShouldBe(3);
        }

        [Fact]
        public void AsReverse_GetByFirstKey_AnswersTheOriginalThirdAxisSlice()
        {
            var map = new ThreeKeyDictionary<int, string, string, decimal>();
            map.Add(1, "USD", "2026Q1", 1.00m);
            map.Add(2, "USD", "2026Q1", 1.05m);
            map.Add(1, "EUR", "2026Q2", 0.92m);

            var reversed = map.AsReverse();

            reversed.CountOfFirstKey("2026Q1").ShouldBe(map.CountOfThirdKey("2026Q1"));

            // The reversed first-axis slice holds the same entries as the original third-axis
            // slice; only the tuple component order differs, so project it back to (k1, k2, v).
            reversed.GetByFirstKey("2026Q1")
                .Select(e => (e.Key3, e.Key2, e.Value))
                .ShouldBe(map.GetByThirdKey("2026Q1"), ignoreOrder: true);
        }

        [Fact]
        public void AsReverse_MiddleAxisIsUnchanged()
        {
            var map = new ThreeKeyDictionary<int, string, string, decimal>();
            map.Add(1, "USD", "2026Q1", 1.00m);
            map.Add(2, "USD", "2026Q1", 1.05m);

            var reversed = map.AsReverse();

            // K2 stays the middle axis, so its per-axis count still answers the same way.
            reversed.CountOfSecondKey("USD").ShouldBe(map.CountOfSecondKey("USD"));

            // Same entries, mirrored component labels: canonicalise both slices to (k1, k3, v).
            reversed.GetBySecondKey("USD")
                .Select(e => (e.Key3, e.Key1, e.Value))
                .ShouldBe(
                    map.GetBySecondKey("USD").Select(e => (e.Key1, e.Key3, e.Value)),
                    ignoreOrder: true);
        }

        [Fact]
        public void AsReverse_Empty_IsEmpty()
        {
            var map = new ThreeKeyDictionary<int, string, int, int>();

            var reversed = map.AsReverse();

            reversed.IsEmpty.ShouldBeTrue();
            reversed.Count.ShouldBe(0);
        }

        [Fact]
        public void AsReverse_IsACopy_MutationsDoNotPropagateEitherWay()
        {
            var map = new ThreeKeyDictionary<int, string, int, int>();
            map.Add(1, "a", 1, 42);

            var reversed = map.AsReverse();

            map.Add(2, "b", 2, 43);
            reversed.Count.ShouldBe(1);
            reversed.ContainsKey(2, "b", 2).ShouldBeFalse();

            reversed.Add(3, "c", 3, 44);
            map.Count.ShouldBe(2);
            map.ContainsKey(3, "c", 3).ShouldBeFalse();
        }

        [Fact]
        public void AsReverse_CarriesTheComparersAcrossTheAxes()
        {
            var map = new ThreeKeyDictionary<int, string, string, decimal>(
                comparer1: null,
                comparer2: null,
                comparer3: StringComparer.OrdinalIgnoreCase);
            map.Add(1, "USD", "2026Q1", 1.00m);

            var reversed = map.AsReverse();

            // The reversed first axis is the original third axis, so it must still be
            // case-insensitive - the comparer travels with the axis.
            reversed["2026q1", "USD", 1].ShouldBe(1.00m);
            reversed.ContainsFirstKey("2026q1").ShouldBeTrue();
        }

        [Fact]
        public void AsReverse_NullComponents_AreSupported()
        {
            var map = new ThreeKeyDictionary<int, string, string, int>();
            map.Add(1, "a", null, 42);

            var reversed = map.AsReverse();

            reversed[null, "a", 1].ShouldBe(42);
            reversed.ContainsKey(null, "a", 1).ShouldBeTrue();
        }

        [Fact]
        public void AsReverse_EntriesAreTheMirroredSourceEntries()
        {
            var map = new ThreeKeyDictionary<int, string, int, int>();
            map.Add(1, "a", 10, 100);
            map.Add(2, "b", 20, 200);

            var reversed = map.AsReverse();

            reversed.EntrySet()
                .Select(e => (e.Key1, e.Key2, e.Key3, e.Value))
                .ShouldBe(
                    new[] { (10, "a", 1, 100), (20, "b", 2, 200) },
                    ignoreOrder: true);
        }

        [Fact]
        public void Entries_ObsoleteAlias_AgreesWithEntrySet()
        {
            var map = new ThreeKeyDictionary<int, string, int, int>();
            map.Add(1, "a", 10, 100);
            map.Add(2, "b", 20, 200);

#pragma warning disable CS0618 // Entries is the deprecated alias under test.
            var alias = map.Entries.ToList();
#pragma warning restore CS0618

            alias.ShouldBe(map.EntrySet().ToList());
        }
    }
}
