using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    public class OrderedMultiDictionaryTests
    {
        // ------------------------------------------------------------------
        // ordering: both axes
        // ------------------------------------------------------------------

        [Fact]
        public void KeysEnumerateInAscendingOrder()
        {
            var map = NewMap();
            map.Add("pear", 1);
            map.Add("apple", 1);
            map.Add("fig", 1);

            map.Keys.ShouldBe(new[] { "apple", "fig", "pear" });
            map.Count.ShouldBe(3);
            map.KeyCount.ShouldBe(3);
        }

        [Fact]
        public void InnerValuesEnumerateInAscendingOrder()
        {
            var map = NewMap();
            map.Add("alpha", 30);
            map.Add("alpha", 10);
            map.Add("alpha", 20);

            map["alpha"].ShouldBe(new[] { 10, 20, 30 });
        }

        [Fact]
        public void DuplicatesAreConsecutiveAndSortedTogether()
        {
            var map = NewMap();
            map.AddRange("k", new[] { 5, 1, 5, 2, 5, 1 });

            map["k"].ShouldBe(new[] { 1, 1, 2, 5, 5, 5 });
            map.ValueCount("k").ShouldBe(6);
            map.TotalValueCount.ShouldBe(6);
        }

        [Fact]
        public void ValuesFlattenAcrossKeysInKeyOrderThenValueOrder()
        {
            var map = NewMap();
            map.Add("b", 9);
            map.Add("a", 7);
            map.Add("b", 3);

            map.Values.ShouldBe(new[] { 7, 3, 9 });
        }

        [Fact]
        public void PairsEnumerateKeyAscendingValueAscendingExpanded()
        {
            var map = NewMap();
            map.Add("b", 2);
            map.Add("b", 1);
            map.Add("a", 4);

            map.Select((KeyValuePair<string, int> p) => p.Key).ShouldBe(new[] { "a", "b", "b" });
            map.Select((KeyValuePair<string, int> p) => p.Value).ShouldBe(new[] { 4, 1, 2 });
        }

        [Fact]
        public void EntrySetWalksKeysAscendingWithSortedValues()
        {
            var map = NewMap();
            map.Add("b", 2);
            map.Add("a", 1);
            map.Add("b", 1);

            var entries = map.EntrySet().ToList();
            entries.Count.ShouldBe(2);
            entries[0].Key.ShouldBe("a");
            entries[1].Key.ShouldBe("b");
            entries[1].Values.ShouldBe(new[] { 1, 2 });
        }

        // ------------------------------------------------------------------
        // indexer / TryGetValue / Contains
        // ------------------------------------------------------------------

        [Fact]
        public void IndexerOfAbsentKeyReturnsEmptyNonNullCollection()
        {
            var map = NewMap();

            var values = map["missing"];
            values.ShouldNotBeNull();
            values.ShouldBeEmpty();
            map.TryGetValue("missing", out _).ShouldBeFalse();
        }

        [Fact]
        public void TryGetValueReturnsLiveSortedView()
        {
            var map = NewMap();
            map.Add("k", 2);
            map.Add("k", 1);

            map.TryGetValue("k", out var values).ShouldBeTrue();
            values.ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public void AddNullKeyThrows()
        {
            var map = NewMap();

            Should.Throw<ArgumentNullException>(() => map.Add(null!, 1));
            Should.Throw<ArgumentNullException>(() => map.ContainsKey(null!));
            Should.Throw<ArgumentNullException>(() => map[null!]);
        }

        [Fact]
        public void NullValueIsSupportedAndSortsFirstUnderDefaultComparer()
        {
            var map = new OrderedMultiDictionary<string, string>();
            map.Add("k", "a");
            map.Add("k", null);

            map["k"].ShouldBe(new[] { null, "a" });
            map.Contains("k", null).ShouldBeTrue();
            map.ContainsValue(null).ShouldBeTrue();
        }

        // ------------------------------------------------------------------
        // duplicate vs deduplicating inner collection
        // ------------------------------------------------------------------

        [Fact]
        public void DuplicateModeStoresRepeatedValues()
        {
            var map = NewMap();
            map.Add("k", 7);
            map.Add("k", 7);

            map["k"].ShouldBe(new[] { 7, 7 });
            map.ValueCount("k").ShouldBe(2);
        }

        [Fact]
        public void UniqueModeIgnoresRepeatedValueSilently()
        {
            var map = new OrderedMultiDictionary<string, int>(false);
            map.Add("k", 7);
            map.Add("k", 7);

            map["k"].ShouldBe(new[] { 7 });
            map.ValueCount("k").ShouldBe(1);
            map.TotalValueCount.ShouldBe(1);
        }

        // ------------------------------------------------------------------
        // comparers define identity on both axes
        // ------------------------------------------------------------------

        [Fact]
        public void CustomValueComparerMergesEqualValuesIntoOneMultisetEntry()
        {
            // values equal when |a| == |b|: the comparer decides *identity*, not dedup —
            // equal values merge into one entry whose multiplicity counts, the stored
            // element is the first added, and enumeration expands the copies.
            var map = new OrderedMultiDictionary<string, int>(
                null,
                Comparer<int>.Create((a, b) => Math.Abs(a).CompareTo(Math.Abs(b))));
            map.Add("k", 3);
            map.Add("k", -3);
            map.Add("k", 2);

            map["k"].ShouldBe(new[] { 2, 3, 3 });
            map.ValueCount("k").ShouldBe(3);
            map.Contains("k", -3).ShouldBeTrue();

            map.Remove("k", -3).ShouldBeTrue();
            map["k"].ShouldBe(new[] { 2, 3 });
        }

        [Fact]
        public void CustomKeyComparerCollapsesKeysItDeemsEqual()
        {
            var map = new OrderedMultiDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            map.Add("Alpha", 1);
            map.Add("ALPHA", 2);

            map.Count.ShouldBe(1);
            map.ContainsKey("alpha").ShouldBeTrue();
            map["Alpha"].ShouldBe(new[] { 1, 2 });
        }

        // ------------------------------------------------------------------
        // remove / the empty-inner auto-delete invariant
        // ------------------------------------------------------------------

        [Fact]
        public void RemovePairDropsOneOccurrenceAndKeyWhenEmptied()
        {
            var map = NewMap();
            map.Add("k", 1);
            map.Add("k", 1);

            map.Remove("k", 1).ShouldBeTrue();
            map.ContainsKey("k").ShouldBeTrue();
            map.Remove("k", 1).ShouldBeTrue();
            map.ContainsKey("k").ShouldBeFalse();
            map.Remove("k", 1).ShouldBeFalse();
            map.TotalValueCount.ShouldBe(0);
        }

        [Fact]
        public void RemoveWholeKeyRemovesAllValues()
        {
            var map = NewMap();
            map.AddRange("k", new[] { 1, 2, 3 });
            map.Add("other", 9);

            map.Remove("k").ShouldBeTrue();
            map.ContainsKey("k").ShouldBeFalse();
            map.Keys.ShouldBe(new[] { "other" });
        }

        [Fact]
        public void RemoveRangeTakesOneOccurrencePerDistinctValue()
        {
            var map = NewMap();
            map.AddRange("k", new[] { 5, 5, 5, 6 });

            // the argument is a set: a repeated value removes one occurrence per distinct value
            map.RemoveRange("k", new[] { 5, 5, 9 }).ShouldBeTrue();
            map["k"].ShouldBe(new[] { 5, 5, 6 });
        }

        [Fact]
        public void RemoveRangeDropsKeyWhenEmptiedAndMissingKeyIsNoOp()
        {
            var map = NewMap();
            map.AddRange("k", new[] { 1, 2 });

            map.RemoveRange("k", new[] { 1, 2, 3 }).ShouldBeTrue();
            map.ContainsKey("k").ShouldBeFalse();
            map.RemoveRange("k", new[] { 1 }).ShouldBeFalse();
        }

        [Fact]
        public void NullArgumentsThrow()
        {
            var map = NewMap();

            Should.Throw<ArgumentNullException>(() => map.AddRange("k", null!));
            Should.Throw<ArgumentNullException>(() => map.RemoveRange(null!, new[] { 1 }));
            Should.Throw<ArgumentNullException>(() => map.UnionWith("k", null!));
            Should.Throw<ArgumentNullException>(() => map.IntersectionWith("k", null!));
            Should.Throw<ArgumentNullException>(() => map.ExceptWith("k", null!));
            Should.Throw<ArgumentNullException>(() => map.SymmetricExceptWith("k", null!));
        }

        // ------------------------------------------------------------------
        // per-key value set operations (semantics shared with MultiDictionary)
        // ------------------------------------------------------------------

        [Fact]
        public void UnionWithAddsDistinctAbsentValuesAndKeepsMultiplicities()
        {
            var map = NewMap();
            map.AddRange("k", new[] { 1, 1 });

            map.UnionWith("k", new[] { 1, 2, 2, 3 });

            // stored duplicates survive; each distinct absent value added once
            map["k"].ShouldBe(new[] { 1, 1, 2, 3 });
        }

        [Fact]
        public void UnionWithCreatesMissingKey()
        {
            var map = NewMap();

            map.UnionWith("new", new[] { 1, 1, 2 });

            map.ContainsKey("new").ShouldBeTrue();
            map["new"].ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public void IntersectionWithKeepsListedValuesAndDropsEmptyKey()
        {
            var map = NewMap();
            map.AddRange("k", new[] { 1, 1, 2, 3 });

            map.IntersectionWith("k", new[] { 1, 3, 99 });

            // multiplicity of the kept value survives (both copies of 1)
            map["k"].ShouldBe(new[] { 1, 1, 3 });
        }

        [Fact]
        public void IntersectionWithDropsKeyWhenNothingRemains()
        {
            var map = NewMap();
            map.Add("k", 1);

            map.IntersectionWith("k", new[] { 2 });

            map.ContainsKey("k").ShouldBeFalse();
        }

        [Fact]
        public void IntersectionWithOnMissingKeyIsNoOp()
        {
            var map = NewMap();

            map.IntersectionWith("missing", new[] { 1 });

            map.Count.ShouldBe(0);
        }

        [Fact]
        public void ExceptWithRemovesEveryOccurrence()
        {
            var map = NewMap();
            map.AddRange("k", new[] { 1, 1, 1, 2 });

            map.ExceptWith("k", new[] { 1 });

            map["k"].ShouldBe(new[] { 2 });
        }

        [Fact]
        public void ExceptWithDropsKeyWhenEmptied()
        {
            var map = NewMap();
            map.Add("k", 1);

            map.ExceptWith("k", new[] { 1, 2 });

            map.ContainsKey("k").ShouldBeFalse();
            map.TotalValueCount.ShouldBe(0);
        }

        [Fact]
        public void SymmetricExceptWithTogglesStoredValues()
        {
            var map = NewMap();
            map.AddRange("k", new[] { 1, 1, 2 });

            // 1 toggles one copy off (N -> N-1); 2 toggles off entirely; 3 toggles on
            map.SymmetricExceptWith("k", new[] { 1, 2, 3 });

            map["k"].ShouldBe(new[] { 1, 3 });
        }

        [Fact]
        public void SymmetricExceptWithCreatesKeyWhenAbsent()
        {
            var map = NewMap();

            map.SymmetricExceptWith("new", new[] { 1, 1, 2 });

            map["new"].ShouldBe(new[] { 1, 2 });
        }

        // ------------------------------------------------------------------
        // clone / views
        // ------------------------------------------------------------------

        [Fact]
        public void CloneIsIndependentAndKeepsBothComparers()
        {
            var keyComparer = StringComparer.OrdinalIgnoreCase;
            var valueComparer = Comparer<int>.Create((a, b) => Math.Abs(a).CompareTo(Math.Abs(b)));
            var map = new OrderedMultiDictionary<string, int>(keyComparer, valueComparer);
            map.Add("alpha", -3);

            var clone = map.Clone();
            clone.Comparer.ShouldBeSameAs(keyComparer);
            clone.ValueComparer.ShouldBeSameAs(valueComparer);

            clone.Add("alpha", 1);
            map.ValueCount("alpha").ShouldBe(1);
            clone.ValueCount("alpha").ShouldBe(2);
        }

        [Fact]
        public void AsReadOnlyReflectsLaterMutations()
        {
            var map = NewMap();
            var view = map.AsReadOnly();

            map.Add("k", 1);
            view.Count.ShouldBe(1);
            view.ContainsKey("k").ShouldBeTrue();
            view["k"].ShouldBe(new[] { 1 });
        }

        [Fact]
        public void AsLookupGroupsValuesPerKey()
        {
            var map = NewMap();
            map.Add("b", 2);
            map.Add("a", 1);
            map.Add("b", 1);

            var lookup = map.AsLookup();
            lookup.Count.ShouldBe(2);
            lookup["b"].ShouldBe(new[] { 1, 2 });
            lookup.Contains("a").ShouldBeTrue();
        }

        [Fact]
        public void ClearEmptiesEverything()
        {
            var map = NewMap();
            map.AddRange("k", new[] { 1, 2 });

            map.Clear();

            map.Count.ShouldBe(0);
            map.TotalValueCount.ShouldBe(0);
            map.Keys.ShouldBeEmpty();
        }

        // ------------------------------------------------------------------
        // randomized model check: behaves like a sorted map of sorted multisets
        // ------------------------------------------------------------------

        [Fact]
        public void RandomizedOperationsMatchSortedModel()
        {
            var random = new Random(20260911);
            var map = new OrderedMultiDictionary<int, int>();
            var model = new SortedDictionary<int, List<int>>();

            for (var step = 0; step < 3000; step++)
            {
                var key = random.Next(0, 12);
                var value = random.Next(0, 8);

                switch (random.Next(0, 4))
                {
                    case 0:
                        map.Add(key, value);
                        if (!model.TryGetValue(key, out var list))
                        {
                            list = new List<int>();
                            model[key] = list;
                        }
                        list.Add(value);
                        break;

                    case 1:
                        var removedOne = false;
                        if (model.TryGetValue(key, out var mlist) && mlist.Remove(value))
                        {
                            removedOne = true;
                            if (mlist.Count == 0)
                            {
                                model.Remove(key);
                            }
                        }
                        map.Remove(key, value).ShouldBe(removedOne);
                        break;

                    case 2:
                        map.ContainsKey(key).ShouldBe(model.ContainsKey(key));
                        break;

                    case 3:
                        var expected = model.TryGetValue(key, out var l) ? l.OrderBy(v => v).ToList() : new List<int>();
                        map[key].ShouldBe(expected, ignoreOrder: false);
                        break;
                }
            }

            // whole-map drain: enumeration matches the model on both axes
            var modelPairs = model.SelectMany(kv => kv.Value.OrderBy(v => v), (kv, v) => (kv.Key, v)).ToList();
            map.Select((KeyValuePair<int, int> p) => (p.Key, p.Value)).ShouldBe(modelPairs);
        }

        private static OrderedMultiDictionary<string, int> NewMap()
        {
            return new OrderedMultiDictionary<string, int>();
        }
    }
}
