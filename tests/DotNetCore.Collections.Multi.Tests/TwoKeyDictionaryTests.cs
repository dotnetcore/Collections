using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    public class TwoKeyDictionaryTests
    {
        // ------------------------------------------------------------------
        // Indexer / Add / TryGetValue
        // ------------------------------------------------------------------

        [Fact]
        public void Indexer_SetterThenGetter_ReturnsValue()
        {
            var map = new TwoKeyDictionary<int, string, decimal> { [1, "USD"] = 1.00m };

            map[1, "USD"].ShouldBe(1.00m);
            map.Count.ShouldBe(1);
        }

        [Fact]
        public void Indexer_Getter_MissingPair_Throws()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();

            Should.Throw<KeyNotFoundException>(() => map[1, "USD"]);
        }

        [Fact]
        public void Indexer_Setter_OverwritesExisting()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map[1, "USD"] = 1.00m;
            map[1, "USD"] = 1.05m;

            map.Count.ShouldBe(1);
            map[1, "USD"].ShouldBe(1.05m);
        }

        [Fact]
        public void Add_ThenCountIncrements()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(1, "EUR", 0.92m);

            map.Count.ShouldBe(2);
            map.IsEmpty.ShouldBeFalse();
        }

        [Fact]
        public void Add_OverwriteFalse_WithExistingPair_Throws()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            Should.Throw<ArgumentException>(() => map.Add(1, "USD", 2.00m, overwrite: false));
            map[1, "USD"].ShouldBe(1.00m);
        }

        [Fact]
        public void Add_OverwriteTrue_ReportsReplacement()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();

            map.Add(1, "USD", 1.00m, overwrite: true).ShouldBeFalse();
            map.Add(1, "USD", 1.05m, overwrite: true).ShouldBeTrue();
        }

        [Fact]
        public void TryAdd_ExistingPair_ReturnsFalse()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            map.TryAdd(1, "USD", 9.99m).ShouldBeFalse();
            map[1, "USD"].ShouldBe(1.00m);
        }

        [Fact]
        public void TryAdd_NewPair_ReturnsTrue()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();

            map.TryAdd(1, "USD", 1.00m).ShouldBeTrue();
            map[1, "USD"].ShouldBe(1.00m);
        }

        [Fact]
        public void TryGetValue_HitAndMiss()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            map.TryGetValue(1, "USD", out var hit).ShouldBeTrue();
            hit.ShouldBe(1.00m);
            map.TryGetValue(1, "EUR", out _).ShouldBeFalse();
        }

        [Fact]
        public void ContainsKey_HitAndMiss()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            map.ContainsKey(1, "USD").ShouldBeTrue();
            map.ContainsKey(2, "USD").ShouldBeFalse();
            map.ContainsKey(1, "EUR").ShouldBeFalse();
        }

        [Fact]
        public void Contains_ValueMatch()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            map.Contains(1, "USD", 1.00m).ShouldBeTrue();
            map.Contains(1, "USD", 2.00m).ShouldBeFalse();
        }

        [Fact]
        public void Add_SameK2DifferentK1_BothStored()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(2, "USD", 1.05m);

            map[1, "USD"].ShouldBe(1.00m);
            map[2, "USD"].ShouldBe(1.05m);
            map.Count.ShouldBe(2);
        }

        [Fact]
        public void Add_SameK1DifferentK2_BothStored()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(1, "EUR", 0.92m);

            map[1, "USD"].ShouldBe(1.00m);
            map[1, "EUR"].ShouldBe(0.92m);
        }

        // ------------------------------------------------------------------
        // Per-axis projection
        // ------------------------------------------------------------------

        [Fact]
        public void GetByFirstKey_ReturnsWholeSlice()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(1, "EUR", 0.92m);
            map.Add(2, "USD", 1.05m);

            var slice = map.GetByFirstKey(1).ToList();

            slice.Count.ShouldBe(2);
            slice.ShouldContain(e => e.Key2 == "USD" && e.Value == 1.00m);
            slice.ShouldContain(e => e.Key2 == "EUR" && e.Value == 0.92m);
        }

        [Fact]
        public void GetByFirstKey_MissingKey_IsEmpty()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            map.GetByFirstKey(99).ShouldBeEmpty();
        }

        [Fact]
        public void GetByFirstKey_DoesNotLeakOtherSlices()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(2, "USD", 1.05m);

            map.GetByFirstKey(1).Count().ShouldBe(1);
            map.GetByFirstKey(1).First().Value.ShouldBe(1.00m);
        }

        [Fact]
        public void GetBySecondKey_ReturnsWholeSlice()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(1, "EUR", 0.92m);
            map.Add(2, "USD", 1.05m);

            var slice = map.GetBySecondKey("USD").ToList();

            slice.Count.ShouldBe(2);
            slice.ShouldContain(e => e.Key1 == 1 && e.Value == 1.00m);
            slice.ShouldContain(e => e.Key1 == 2 && e.Value == 1.05m);
        }

        [Fact]
        public void GetBySecondKey_MissingKey_IsEmpty()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            map.GetBySecondKey("JPY").ShouldBeEmpty();
        }

        [Fact]
        public void GetBySecondKey_BothAxesReachable()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(1, "EUR", 0.92m);

            map.GetByFirstKey(1).Count().ShouldBe(2);
            map.GetBySecondKey("EUR").Count().ShouldBe(1);
        }

        [Fact]
        public void CountOfFirstKey_CountsSlice()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(1, "EUR", 0.92m);
            map.Add(2, "USD", 1.05m);

            map.CountOfFirstKey(1).ShouldBe(2);
            map.CountOfFirstKey(2).ShouldBe(1);
            map.CountOfFirstKey(99).ShouldBe(0);
        }

        [Fact]
        public void CountOfSecondKey_CountsAcrossFirstKeys()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(2, "USD", 1.05m);
            map.Add(1, "EUR", 0.92m);

            map.CountOfSecondKey("USD").ShouldBe(2);
            map.CountOfSecondKey("EUR").ShouldBe(1);
            map.CountOfSecondKey("JPY").ShouldBe(0);
        }

        [Fact]
        public void ContainsFirstKey_TrueAndFalse()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            map.ContainsFirstKey(1).ShouldBeTrue();
            map.ContainsFirstKey(2).ShouldBeFalse();
        }

        [Fact]
        public void ContainsSecondKey_TrueAndFalse()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            map.ContainsSecondKey("USD").ShouldBeTrue();
            map.ContainsSecondKey("EUR").ShouldBeFalse();
        }

        // ------------------------------------------------------------------
        // Axis projections
        // ------------------------------------------------------------------

        [Fact]
        public void Keys1_ReturnsDistinctFirstAxisValues()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(1, "EUR", 0.92m);
            map.Add(2, "USD", 1.05m);

            map.Keys1.OrderBy(k => k).ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public void Keys2_ReturnsDistinctSecondAxisValues()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(1, "EUR", 0.92m);
            map.Add(2, "USD", 1.05m);

            map.Keys2.OrderBy(k => k).ShouldBe(new[] { "EUR", "USD" });
        }

        [Fact]
        public void Keys1_EmptyMap_IsEmpty()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();

            map.Keys1.ShouldBeEmpty();
            map.Keys2.ShouldBeEmpty();
        }

        // ------------------------------------------------------------------
        // Empty axis / empty map
        // ------------------------------------------------------------------

        [Fact]
        public void EmptyMap_IsEmpty()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();

            map.Count.ShouldBe(0);
            map.IsEmpty.ShouldBeTrue();
            map.ShouldBeEmpty();
        }

        [Fact]
        public void EmptyAxis_AfterRemoval_ReportsEmpty()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            map.Remove(1, "USD");

            map.GetByFirstKey(1).ShouldBeEmpty();
            map.GetBySecondKey("USD").ShouldBeEmpty();
            map.CountOfFirstKey(1).ShouldBe(0);
            map.ContainsFirstKey(1).ShouldBeFalse();
        }

        // ------------------------------------------------------------------
        // null axis values
        // ------------------------------------------------------------------

        [Fact]
        public void NullFirstKey_IsSupported()
        {
            var map = new TwoKeyDictionary<string, string, int>();
            map.Add(null!, "b", 1);

            map[null!, "b"].ShouldBe(1);
            map.ContainsKey(null!, "b").ShouldBeTrue();
        }

        [Fact]
        public void NullSecondKey_IsSupported()
        {
            var map = new TwoKeyDictionary<string, string, int>();
            map.Add("a", null!, 1);

            map["a", null!].ShouldBe(1);
            map.ContainsKey("a", null!).ShouldBeTrue();
        }

        [Fact]
        public void NullSecondKey_DistinctFromNonNull()
        {
            var map = new TwoKeyDictionary<string, string, int>();
            map.Add("a", null!, 1);
            map.Add("a", "b", 2);

            map["a", null!].ShouldBe(1);
            map["a", "b"].ShouldBe(2);
            map.Count.ShouldBe(2);
        }

        [Fact]
        public void NullFirstKey_GetByFirstKey_Works()
        {
            var map = new TwoKeyDictionary<string, string, int>();
            map.Add(null!, "b", 1);
            map.Add(null!, "c", 2);

            map.GetByFirstKey(null!).Count().ShouldBe(2);
            map.CountOfFirstKey(null!).ShouldBe(2);
        }

        [Fact]
        public void NullSecondKey_GetBySecondKey_Works()
        {
            var map = new TwoKeyDictionary<string, string, int>();
            map.Add("a", null!, 1);
            map.Add("b", null!, 2);

            map.GetBySecondKey(null!).Count().ShouldBe(2);
        }

        [Fact]
        public void BothKeysNull_IsAValidPair()
        {
            var map = new TwoKeyDictionary<string, string, int>();
            map.Add(null!, null!, 7);

            map[null!, null!].ShouldBe(7);
            map.Count.ShouldBe(1);
        }

        [Fact]
        public void NullFirstKey_Removable()
        {
            var map = new TwoKeyDictionary<string, string, int>();
            map.Add(null!, "b", 1);

            map.Remove(null!, "b").ShouldBeTrue();
            map.Count.ShouldBe(0);
        }

        [Fact]
        public void NullSecondKey_RemoveBySecondKey_Works()
        {
            var map = new TwoKeyDictionary<string, string, int>();
            map.Add("a", null!, 1);
            map.Add("b", null!, 2);
            map.Add("b", "c", 3);

            map.RemoveBySecondKey(null!).ShouldBe(2);
            map.Count.ShouldBe(1);
        }

        [Fact]
        public void NullableValueType_Keys_Work()
        {
            var map = new TwoKeyDictionary<int?, string, int>();
            map.Add(null, "b", 1);
            map.Add(2, "b", 2);

            map[null, "b"].ShouldBe(1);
            map[2, "b"].ShouldBe(2);
            map.Count.ShouldBe(2);
        }

        // ------------------------------------------------------------------
        // Comparer injection
        // ------------------------------------------------------------------

        [Fact]
        public void Comparer_FirstAxis_CaseInsensitive()
        {
            var map = new TwoKeyDictionary<string, string, int>(
                StringComparer.OrdinalIgnoreCase, null);
            map.Add("A", "b", 1);

            map["a", "b"].ShouldBe(1);
            map.Count.ShouldBe(1);
        }

        [Fact]
        public void Comparer_SecondAxis_CaseInsensitive()
        {
            var map = new TwoKeyDictionary<string, string, int>(
                null, StringComparer.OrdinalIgnoreCase);
            map.Add("a", "B", 1);

            map["a", "b"].ShouldBe(1);
            map.Count.ShouldBe(1);
        }

        [Fact]
        public void Comparer_BothAxes_CaseInsensitive()
        {
            var map = new TwoKeyDictionary<string, string, int>(
                StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase);
            map.Add("A", "B", 1);
            map.Add("a", "b", 2);

            map.Count.ShouldBe(1);
            map["A", "B"].ShouldBe(2);
        }

        [Fact]
        public void Comparer_SecondAxis_UsedByGetBySecondKey()
        {
            var map = new TwoKeyDictionary<string, string, int>(
                null, StringComparer.OrdinalIgnoreCase);
            map.Add("a", "USD", 1);
            map.Add("b", "USD", 2);

            // The comparer governs matching, not normalization: both entries keep their
            // original casing, and any-cased lookup finds the same axis value.
            map.GetBySecondKey("Usd").Count().ShouldBe(2);
            map.ContainsSecondKey("usd").ShouldBeTrue();
            map.CountOfSecondKey("uSd").ShouldBe(2);
        }

        [Fact]
        public void Comparer_PropertiesExposed()
        {
            var c1 = StringComparer.OrdinalIgnoreCase;
            var c2 = StringComparer.Ordinal;
            var map = new TwoKeyDictionary<string, string, int>(c1, c2);

            map.Comparer1.ShouldBeSameAs(c1);
            map.Comparer2.ShouldBeSameAs(c2);
        }

        [Fact]
        public void Comparer_DefaultsWhenNull()
        {
            var map = new TwoKeyDictionary<string, string, int>(null, null);

            map.Comparer1.ShouldBeSameAs(EqualityComparer<string>.Default);
            map.Comparer2.ShouldBeSameAs(EqualityComparer<string>.Default);
        }

        [Fact]
        public void Comparer_ClonePreservesBoth()
        {
            var map = new TwoKeyDictionary<string, string, int>(
                StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase);
            map.Add("A", "B", 1);

            var clone = map.Clone();

            clone.Comparer1.ShouldBeSameAs(StringComparer.OrdinalIgnoreCase);
            clone.Comparer2.ShouldBeSameAs(StringComparer.OrdinalIgnoreCase);
            clone["a", "b"].ShouldBe(1);
        }

        [Fact]
        public void Comparer_FirstAxisDistinctTypes_DoNotCollide()
        {
            // With K = object, the axis tag (not the runtime type) decides which comparer is
            // consulted, so an int component can never be mistaken for a string one, or vice
            // versa. This test guards the behavioural consequence: prior implementations that
            // dispatched on runtime type mis-routed these.
            var map = new TwoKeyDictionary<object, string, int>();
            map.Add(1, "k", 1);
            map.Add("1", "k", 2);

            map[1, "k"].ShouldBe(1);
            map["1", "k"].ShouldBe(2);
            map.Count.ShouldBe(2);
        }

        [Fact]
        public void Comparer_GenericFirstAxis_Works()
        {
            // ReferenceEquals-comparable generic K1 must be routable too (regression guard:
            // a type-pattern-based dispatch gets this wrong when K1 is itself a Type).
            var map = new TwoKeyDictionary<Type, string, int>();
            map.Add(typeof(string), "k", 1);
            map.Add(typeof(int), "k", 2);

            map[typeof(string), "k"].ShouldBe(1);
            map[typeof(int), "k"].ShouldBe(2);
            map.Count.ShouldBe(2);
        }

        [Fact]
        public void Comparer_SameTypeBothAxes_IndependentComparers()
        {
            // K1 == K2 == string with *different* comparers: the first axis folds case, the
            // second does not. Only position-based dispatch can honour both.
            var map = new TwoKeyDictionary<string, string, int>(
                StringComparer.OrdinalIgnoreCase, StringComparer.Ordinal);
            map.Add("A", "B", 1);
            map.Add("a", "b", 2);

            // Folding first axis + casing second axis => keys are ("A","B") and ("A","b").
            map.Count.ShouldBe(2);
            map["a", "B"].ShouldBe(1);
            map["a", "b"].ShouldBe(2);
        }

        // ------------------------------------------------------------------
        // Remove
        // ------------------------------------------------------------------

        [Fact]
        public void Remove_ExistingPair_ReturnsTrue()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            map.Remove(1, "USD").ShouldBeTrue();
            map.Count.ShouldBe(0);
        }

        [Fact]
        public void Remove_MissingPair_ReturnsFalse()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();

            map.Remove(1, "USD").ShouldBeFalse();
        }

        [Fact]
        public void Remove_ReturnsRemovedValue()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            map.Remove(1, "USD", out var value).ShouldBeTrue();
            value.ShouldBe(1.00m);
        }

        [Fact]
        public void Remove_LeavesOtherPairsIntact()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(1, "EUR", 0.92m);

            map.Remove(1, "USD");

            map.Count.ShouldBe(1);
            map[1, "EUR"].ShouldBe(0.92m);
        }

        [Fact]
        public void RemoveByFirstKey_CascadesSlice()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(1, "EUR", 0.92m);
            map.Add(2, "USD", 1.05m);

            map.RemoveByFirstKey(1).ShouldBe(2);

            map.Count.ShouldBe(1);
            map.CountOfFirstKey(1).ShouldBe(0);
            map.ContainsKey(2, "USD").ShouldBeTrue();
        }

        [Fact]
        public void RemoveByFirstKey_MissingKey_ReturnsZero()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();

            map.RemoveByFirstKey(99).ShouldBe(0);
        }

        [Fact]
        public void RemoveBySecondKey_RemovesAcrossFirstKeys()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(2, "USD", 1.05m);
            map.Add(1, "EUR", 0.92m);

            map.RemoveBySecondKey("USD").ShouldBe(2);

            map.Count.ShouldBe(1);
            map.ContainsKey(1, "EUR").ShouldBeTrue();
        }

        [Fact]
        public void RemoveBySecondKey_MissingKey_ReturnsZero()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();

            map.RemoveBySecondKey("JPY").ShouldBe(0);
        }

        [Fact]
        public void Remove_IsIdempotent()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            map.Remove(1, "USD").ShouldBeTrue();
            map.Remove(1, "USD").ShouldBeFalse();
            map.Remove(1, "USD").ShouldBeFalse();
        }

        [Fact]
        public void Clear_EmptiesTheDictionary()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(2, "EUR", 0.92m);

            map.Clear();

            map.Count.ShouldBe(0);
            map.IsEmpty.ShouldBeTrue();
        }

        // ------------------------------------------------------------------
        // Clone / AsReadOnly / ToDictionary
        // ------------------------------------------------------------------

        [Fact]
        public void Clone_IsIndependent()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            var clone = map.Clone();
            clone.Add(2, "EUR", 0.92m);
            clone.Remove(1, "USD");

            map.Count.ShouldBe(1);
            map.ContainsKey(1, "USD").ShouldBeTrue();
            clone.Count.ShouldBe(1);
            clone.ContainsKey(2, "EUR").ShouldBeTrue();
        }

        [Fact]
        public void Clone_PreservesNullKeys()
        {
            var map = new TwoKeyDictionary<string, string, int>();
            map.Add(null!, "b", 1);

            var clone = map.Clone();

            clone.ContainsKey(null!, "b").ShouldBeTrue();
        }

        [Fact]
        public void AsReadOnly_ReflectsChanges()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            var view = map.AsReadOnly();

            view.Count.ShouldBe(0);
            map.Add(1, "USD", 1.00m);
            view.Count.ShouldBe(1);
            view.ShouldContain(e => e.Key1 == 1 && e.Key2 == "USD" && e.Value == 1.00m);
        }

        [Fact]
        public void AsReadOnly_ExposesNoMutators()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            var view = map.AsReadOnly();

            var methods = view.GetType().GetMethods().Select(m => m.Name).ToList();
            methods.ShouldNotContain("Add");
            methods.ShouldNotContain("Remove");
        }

        [Fact]
        public void ToDictionary_SnapshotsAllEntries()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(2, "EUR", 0.92m);

            var snapshot = map.ToDictionary();

            snapshot.Count.ShouldBe(2);
            snapshot[(1, "USD")].ShouldBe(1.00m);
            snapshot[(2, "EUR")].ShouldBe(0.92m);
        }

        [Fact]
        public void ToDictionary_IsIndependent()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            var snapshot = map.ToDictionary();
            map.Add(2, "EUR", 0.92m);

            snapshot.Count.ShouldBe(1);
        }

        [Fact]
        public void AsTrie_ExposesUnderlyingTrie()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            var trie = map.AsTrie();

            trie.Count.ShouldBe(1);
            trie.ContainsKey(new object[] { 1, "USD" }).ShouldBeTrue();
        }

        // ------------------------------------------------------------------
        // Enumeration
        // ------------------------------------------------------------------

        [Fact]
        public void Enumeration_YieldsEveryEntry()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(1, "EUR", 0.92m);
            map.Add(2, "USD", 1.05m);

            map.Count().ShouldBe(3);
            map.Select(e => e.Value).OrderBy(v => v).ShouldBe(new[] { 0.92m, 1.00m, 1.05m });
        }

        [Fact]
        public void Entries_YieldsTuplesWithBothKeysTyped()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            var entry = map.Entries.Single();

            entry.Key1.ShouldBe(1);
            entry.Key2.ShouldBe("USD");
            entry.Value.ShouldBe(1.00m);
        }

        [Fact]
        public void ToString_FormatsPairs()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            map.ToString().ShouldBe("(1,USD):1.00");
        }

        [Fact]
        public void ValueTypes_DefaultValueStoredAndFound()
        {
            var map = new TwoKeyDictionary<int, string, int>();
            map.Add(1, "a", 0);

            map.ContainsKey(1, "a").ShouldBeTrue();
            map[1, "a"].ShouldBe(0);
        }

        [Fact]
        public void NullValues_AreAllowed()
        {
            var map = new TwoKeyDictionary<int, string, string>();
            map.Add(1, "a", null!);

            map.TryGetValue(1, "a", out var value).ShouldBeTrue();
            value.ShouldBeNull();
            map.ContainsKey(1, "a").ShouldBeTrue();
        }

        // ------------------------------------------------------------------
        // Cross-axis interaction
        // ------------------------------------------------------------------

        [Fact]
        public void BothAxes_ConsistentAfterMixedOperations()
        {
            var map = new TwoKeyDictionary<int, string, int>();
            map.Add(1, "a", 1);
            map.Add(1, "b", 2);
            map.Add(2, "a", 3);
            map.Add(2, "b", 4);

            map.Remove(1, "a");
            map.Add(3, "a", 5);

            map.Count.ShouldBe(4);
            map.CountOfFirstKey(1).ShouldBe(1);
            map.CountOfFirstKey(2).ShouldBe(2);
            map.CountOfSecondKey("a").ShouldBe(2);
            map.CountOfSecondKey("b").ShouldBe(2);
        }

        [Fact]
        public void AddRemoveAdd_CycleStaysConsistent()
        {
            var map = new TwoKeyDictionary<int, string, int>();

            for (var i = 0; i < 5; i++)
            {
                map.Add(1, i.ToString(), i);
            }

            map.Count.ShouldBe(5);

            for (var i = 0; i < 5; i++)
            {
                map.Remove(1, i.ToString()).ShouldBeTrue();
            }

            map.Count.ShouldBe(0);
            map.IsEmpty.ShouldBeTrue();
        }

        [Fact]
        public void RemoveByFirstKey_ThenReAdd_Works()
        {
            var map = new TwoKeyDictionary<int, string, int>();
            map.Add(1, "a", 1);
            map.Add(1, "b", 2);

            map.RemoveByFirstKey(1).ShouldBe(2);
            map.Add(1, "c", 3);

            map.Count.ShouldBe(1);
            map[1, "c"].ShouldBe(3);
        }

        [Fact]
        public void VoidK1_MeansEmptyFirstAxisKey()
        {
            // The wrapper is not a "1..N key" type: arity is exactly 2. But an empty string
            // first key is an ordinary value and must round-trip.
            var map = new TwoKeyDictionary<string, string, int>();
            map.Add(string.Empty, "b", 1);

            map[string.Empty, "b"].ShouldBe(1);
            map.CountOfFirstKey(string.Empty).ShouldBe(1);
        }
    }
}
