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
    }
}
