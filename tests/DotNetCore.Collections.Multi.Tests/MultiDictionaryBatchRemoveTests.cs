using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    // M6-04 / L-04: MultiDictionary.RemoveRange(key, IEnumerable<V>) batch delete + ValueCount(key).
    //
    // The batch delete is the batch form of Remove(key, value): it removes ONE occurrence per
    // distinct argument value (so Remove(key, v) == RemoveRange(key, new[] { v })), it treats the
    // argument as a set, it tolerates null elements, and it recycles the key once the inner
    // collection empties. Removing every occurrence stays the job of ExceptWith(key, values).
    //
    // Naming: the plan (L-04) asked for an overload Remove(key, IEnumerable<V>). That is a
    // source-breaking change - map.Remove(key, null) becomes CS0121-ambiguous - so the batch form
    // is called RemoveRange instead, mirroring the existing AddRange(key, IEnumerable<V>).
    public class MultiDictionaryBatchRemoveTests
    {
        private static MultiDictionary<string, int> Map()
        {
            return new MultiDictionary<string, int>();
        }

        private static MultiDictionary<string, int> MapWith(string key, params int[] values)
        {
            var map = Map();
            map.AddRange(key, values);
            return map;
        }

        private static void ShouldHold(string key, MultiDictionary<string, int> map, params int[] expected)
        {
            map[key].OrderBy(v => v).ShouldBe(expected.OrderBy(v => v));
        }

        // ------------------------------------------------------------ basics

        [Fact]
        public void RemovesOnlyTheListedValues()
        {
            var map = MapWith("orders", 1001, 1002, 1003, 1004);

            var removed = map.RemoveRange("orders", new[] { 1002, 1004 });

            removed.ShouldBeTrue();
            ShouldHold("orders", map, 1001, 1003);
            map.ValueCount("orders").ShouldBe(2);
        }

        [Fact]
        public void RemovesEveryListedValueInOneCall()
        {
            var map = MapWith("orders", 1001, 1002, 1003);

            map.RemoveRange("orders", map["orders"].ToArray());

            map.ContainsKey("orders").ShouldBeFalse();
            map.Count.ShouldBe(0);
        }

        [Fact]
        public void ReturnsTrueWhenAtLeastOneValueWasRemoved()
        {
            var map = MapWith("orders", 1001, 1002);

            // One hit, one miss: still a successful removal.
            map.RemoveRange("orders", new[] { 1001, 9999 }).ShouldBeTrue();
        }

        [Fact]
        public void ReturnsFalseWhenNothingMatches()
        {
            var map = MapWith("orders", 1001, 1002);

            var removed = map.RemoveRange("orders", new[] { 7001, 7002 });

            removed.ShouldBeFalse();
            ShouldHold("orders", map, 1001, 1002);
        }

        [Fact]
        public void MissingKeyIsANoOpReturningFalse()
        {
            var map = MapWith("orders", 1001);

            var removed = map.RemoveRange("invoices", new[] { 1001 });

            removed.ShouldBeFalse();
            map.Count.ShouldBe(1);
            ShouldHold("orders", map, 1001);
        }

        [Fact]
        public void NullArgumentThrows()
        {
            var map = MapWith("orders", 1001);

            Should.Throw<ArgumentNullException>(() => map.RemoveRange("orders", null!));
            ShouldHold("orders", map, 1001);
        }

        [Fact]
        public void EmptyArgumentIsANoOpThatKeepsTheKey()
        {
            var map = MapWith("orders", 1001);

            var removed = map.RemoveRange("orders", new int[0]);

            removed.ShouldBeFalse();
            map.ContainsKey("orders").ShouldBeTrue();
            ShouldHold("orders", map, 1001);
        }

        // ------------------------------------------------------------ set semantics

        [Fact]
        public void ArgumentIsASetSoRepeatedValuesCountOnce()
        {
            var map = new MultiDictionary<string, int>(allowDuplicateValues: true);
            map.AddRange("orders", new[] { 1001, 1001, 1001 });

            // Three identical argument values must cancel one stored occurrence, not three:
            // this is the set convention shared by every per-key operation of this class.
            var removed = map.RemoveRange("orders", new[] { 1001, 1001, 1001 });

            removed.ShouldBeTrue();
            ShouldHold("orders", map, 1001, 1001);
        }

        [Fact]
        public void OneOccurrenceIsRemovedPerDistinctValueWithDuplicatesAllowed()
        {
            var map = new MultiDictionary<string, int>(allowDuplicateValues: true);
            map.AddRange("orders", new[] { 1001, 1001, 1002 });

            map.RemoveRange("orders", new[] { 1001, 1002 });

            // 1001 keeps one of its two copies, 1002 disappears entirely.
            ShouldHold("orders", map, 1001);
        }

        [Fact]
        public void EquivalentToLoopingTheSingleValueRemove()
        {
            var values = new[] { 1001, 1002, 9999, 1001, 1003 };

            var batch = new MultiDictionary<string, int>(allowDuplicateValues: true);
            batch.AddRange("orders", new[] { 1001, 1001, 1001, 1002, 1003 });

            var looped = new MultiDictionary<string, int>(allowDuplicateValues: true);
            looped.AddRange("orders", new[] { 1001, 1001, 1001, 1002, 1003 });

            var batchResult = batch.RemoveRange("orders", values);

            // The loop side must dedupe too: the batch form is the batch form of the *contract*
            // (one removal per distinct value), not a literal foreach over the raw sequence.
            var loopResult = values.Distinct().Aggregate(false, (acc, v) => looped.Remove("orders", v) || acc);

            batchResult.ShouldBe(loopResult);
            batch["orders"].OrderBy(v => v).ShouldBe(looped["orders"].OrderBy(v => v));
            batch.KeyCount.ShouldBe(looped.KeyCount);
        }

        [Fact]
        public void RepeatedValuesInTheArgumentDoNotRemoveTwiceUnlikeANaiveLoop()
        {
            var values = new[] { 1001, 1001 };
            var stored = new[] { 1001, 1001, 1001 };

            var batch = new MultiDictionary<string, int>(allowDuplicateValues: true);
            batch.AddRange("orders", stored);

            var naive = new MultiDictionary<string, int>(allowDuplicateValues: true);
            naive.AddRange("orders", stored);

            foreach (var v in values)
            {
                naive.Remove("orders", v);
            }

            batch.RemoveRange("orders", values);

            // Set semantics: one occurrence per distinct argument value...
            ShouldHold("orders", batch, 1001, 1001);

            // ...which is deliberately weaker than a naive loop that would take two.
            ShouldHold("orders", naive, 1001);
        }

        [Fact]
        public void SingleValueOverloadAgreesWithTheBatchOverload()
        {
            var single = MapWith("orders", 1001, 1002);
            var batch = MapWith("orders", 1001, 1002);

            single.Remove("orders", 1001).ShouldBe(batch.RemoveRange("orders", new[] { 1001 }));
            single["orders"].OrderBy(v => v).ShouldBe(batch["orders"].OrderBy(v => v));
        }

        [Fact]
        public void TheSingleValueOverloadStillWinsForANullLiteralValue()
        {
            // R-18 regression pin: with the batch form named Remove(key, IEnumerable<V>) this very
            // line would be CS0121-ambiguous, because null converts to both string and
            // IEnumerable<string>. RemoveRange keeps the documented null-removal idiom compiling.
            var map = new MultiDictionary<string, string>();
            map.Add("k", null);

            map.Remove("k", null).ShouldBeTrue();
            map.ContainsKey("k").ShouldBeFalse();
        }

        // ------------------------------------------------------------ key recycling

        [Fact]
        public void KeyIsDroppedWhenItsLastValueIsRemoved()
        {
            var map = MapWith("orders", 1001, 1002);

            map.RemoveRange("orders", new[] { 1001, 1002 });

            map.ContainsKey("orders").ShouldBeFalse();
            map.KeyCount.ShouldBe(0);
            map.TotalValueCount.ShouldBe(0);
            map["orders"].ShouldBeEmpty();
            map.ValueCount("orders").ShouldBe(0);
        }

        [Fact]
        public void KeyIsKeptWhileAnyValueRemains()
        {
            var map = MapWith("orders", 1001, 1002, 1003);

            map.RemoveRange("orders", new[] { 1001, 1003 }).ShouldBeTrue();

            map.ContainsKey("orders").ShouldBeTrue();
            ShouldHold("orders", map, 1002);
        }

        [Fact]
        public void DrainingAKeyOneCallAtATimeEventuallyDropsIt()
        {
            var map = new MultiDictionary<string, int>(allowDuplicateValues: true);
            map.AddRange("orders", new[] { 1001, 1001, 1002 });

            map.RemoveRange("orders", new[] { 1002 }).ShouldBeTrue();
            map.ContainsKey("orders").ShouldBeTrue();

            map.RemoveRange("orders", new[] { 1001 }).ShouldBeTrue();
            map.ContainsKey("orders").ShouldBeTrue();

            map.RemoveRange("orders", new[] { 1001 }).ShouldBeTrue();
            map.ContainsKey("orders").ShouldBeFalse();
        }

        [Fact]
        public void RemovalDoesNotPromoteAKeyBackIntoExistence()
        {
            var map = MapWith("orders", 1001);

            map.RemoveRange("orders", new[] { 1001 }).ShouldBeTrue();

            // A second batch on the now-absent key is a plain no-op.
            map.RemoveRange("orders", new[] { 1001 }).ShouldBeFalse();
            map.KeyCount.ShouldBe(0);
        }

        // ------------------------------------------------------------ null handling

        [Fact]
        public void AStoredNullValueCanBeRemovedInABatch()
        {
            var map = new MultiDictionary<string, int?>();
            map.Add("orders", 1001);
            map.Add("orders", null);

            var removed = map.RemoveRange("orders", new int?[] { null });

            removed.ShouldBeTrue();
            map["orders"].ShouldBe(new int?[] { 1001 });
        }

        [Fact]
        public void NullElementsInTheArgumentAreToleratedAlongsideRealValues()
        {
            var map = new MultiDictionary<string, int?>();
            map.Add("orders", 1001);
            map.Add("orders", null);

            // The null element matches the stored null; the duplicate null counts once.
            map.RemoveRange("orders", new int?[] { null, null, 1001 }).ShouldBeTrue();

            map.ContainsKey("orders").ShouldBeFalse();
        }

        [Fact]
        public void NullElementsInTheArgumentAreHarmlessWhenNothingMatches()
        {
            var map = new MultiDictionary<string, int?>();
            map.Add("orders", 1001);

            map.RemoveRange("orders", new int?[] { null }).ShouldBeFalse();
            map.ContainsKey("orders").ShouldBeTrue();
        }

        // ------------------------------------------------------------ inner collection shapes

        [Fact]
        public void DeduplicatingInnerCollectionBehavesTheSameWay()
        {
            var map = new MultiDictionary<string, int>(allowDuplicateValues: false);
            map.AddRange("orders", new[] { 1001, 1001, 1002 });

            map.ValueCount("orders").ShouldBe(2);

            map.RemoveRange("orders", new[] { 1001, 1001 }).ShouldBeTrue();
            ShouldHold("orders", map, 1002);
        }

        [Fact]
        public void ValueMatchingFollowsTheInnerCollectionComparer()
        {
            // A custom factory that folds case: the batch must go through the inner collection's
            // own comparer instead of comparing values itself.
            var map = new MultiDictionary<string, string>(
                StringComparer.Ordinal,
                () => new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            map.Add("orders", "Paid");

            map.RemoveRange("orders", new[] { "PAID" }).ShouldBeTrue();
            map.ContainsKey("orders").ShouldBeFalse();
        }

        [Fact]
        public void CustomInnerComparerMakesTheArgumentSetCollapseDifferently()
        {
            // The argument dedup uses value equality, the inner collection uses its own comparer:
            // both "Paid" and "PAID" are listed, but the second removal simply finds nothing left.
            var map = new MultiDictionary<string, string>(
                StringComparer.Ordinal,
                () => new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            map.Add("orders", "Paid");

            map.RemoveRange("orders", new[] { "Paid", "PAID" }).ShouldBeTrue();
            map.ContainsKey("orders").ShouldBeFalse();
        }

        // ------------------------------------------------------------ key comparer & scope

        [Fact]
        public void KeyLookupUsesTheInjectedKeyComparer()
        {
            var map = new MultiDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            map.AddRange("Orders", new[] { 1001, 1002 });

            map.RemoveRange("ORDERS", new[] { 1001 }).ShouldBeTrue();

            map.ContainsKey("orders").ShouldBeTrue();
            ShouldHold("orders", map, 1002);
        }

        [Fact]
        public void OtherKeysAreUntouched()
        {
            var map = MapWith("orders", 1001, 1002);
            map.AddRange("invoices", new[] { 1001, 2002 });

            map.RemoveRange("orders", new[] { 1001, 2002 }).ShouldBeTrue();

            ShouldHold("orders", map, 1002);
            ShouldHold("invoices", map, 1001, 2002);
            map.KeyCount.ShouldBe(2);
            map.TotalValueCount.ShouldBe(3);
        }

        [Fact]
        public void ValuesBorrowedFromAnotherKeyMatchOnlyUnderTheTargetKey()
        {
            var map = MapWith("orders", 1001);
            map.AddRange("invoices", new[] { 1002, 1003 });

            map.RemoveRange("orders", map["invoices"]).ShouldBeFalse();

            ShouldHold("orders", map, 1001);
            ShouldHold("invoices", map, 1002, 1003);
        }

        // ------------------------------------------------------------ live views as arguments

        [Fact]
        public void TheKeysOwnLiveViewCanBePassedAsTheArgument()
        {
            var map = MapWith("orders", 1001, 1002, 1003);

            // map["orders"] is a live view over the very collection being mutated; the argument
            // is materialized before any mutation, so this must neither throw nor skip entries.
            map.RemoveRange("orders", map["orders"]).ShouldBeTrue();

            map.ContainsKey("orders").ShouldBeFalse();
        }

        [Fact]
        public void TheFlattenedValuesViewCanBePassedAsTheArgument()
        {
            var map = MapWith("orders", 1001, 1002);

            map.RemoveRange("orders", map.Values).ShouldBeTrue();

            map.ContainsKey("orders").ShouldBeFalse();
        }

        [Fact]
        public void ALazyArgumentIsEnumeratedExactlyOnce()
        {
            var map = MapWith("orders", 1001, 1002);
            var enumerations = 0;

            IEnumerable<int> Values()
            {
                enumerations++;
                yield return 1001;
                yield return 1002;
            }

            map.RemoveRange("orders", Values()).ShouldBeTrue();

            enumerations.ShouldBe(1);
            map.ContainsKey("orders").ShouldBeFalse();
        }

        // ------------------------------------------------------------ cross-view consistency

        [Fact]
        public void RemovalIsVisibleThroughTheReadOnlyViews()
        {
            var map = MapWith("orders", 1001, 1002);

            map.RemoveRange("orders", new[] { 1001 });

            map.AsReadOnly()["orders"].OrderBy(v => v).ShouldBe(new[] { 1002 });
            map.ToDictionary()["orders"].OrderBy(v => v).ShouldBe(new[] { 1002 });
            map.AsLookup()["orders"].OrderBy(v => v).ShouldBe(new[] { 1002 });
            map.ShouldBe(new[] { new KeyValuePair<string, int>("orders", 1002) });
        }

        [Fact]
        public void CloneIsUnaffectedByLaterBatchRemovals()
        {
            var map = MapWith("orders", 1001, 1002);
            var clone = map.Clone();

            map.RemoveRange("orders", new[] { 1001 });

            ShouldHold("orders", map, 1002);
            clone["orders"].OrderBy(v => v).ShouldBe(new[] { 1001, 1002 });
        }

        // ------------------------------------------------------------ metadata

        [Fact]
        public void MetadataStaysConsistentAfterABatchRemoval()
        {
            var map = MapWith("orders", 1001, 1002);
            map.AddRange("invoices", new[] { 2001 });

            map.RemoveRange("orders", new[] { 1001, 1002 });

            map.Count.ShouldBe(1);
            map.KeyCount.ShouldBe(1);
            map.TotalValueCount.ShouldBe(1);
            map.ValueCount("orders").ShouldBe(0);
            map.ValueCount("invoices").ShouldBe(1);
        }

        // ------------------------------------------------------------ ValueCount

        [Fact]
        public void ValueCountCountsStoredOccurrences()
        {
            var map = new MultiDictionary<string, int>(allowDuplicateValues: true);
            map.AddRange("orders", new[] { 1001, 1001, 1002 });

            map.ValueCount("orders").ShouldBe(3);
        }

        [Fact]
        public void ValueCountRespectsTheDeduplicatingInnerCollection()
        {
            var map = new MultiDictionary<string, int>(allowDuplicateValues: false);
            map.AddRange("orders", new[] { 1001, 1001, 1002 });

            map.ValueCount("orders").ShouldBe(2);
        }

        [Fact]
        public void ValueCountIsZeroForAMissingKeyInsteadOfThrowing()
        {
            var map = MapWith("orders", 1001);

            map.ValueCount("invoices").ShouldBe(0);
            map.ValueCount("orders").ShouldBe(1);
        }

        [Fact]
        public void ValueCountTracksEveryMutationKind()
        {
            var map = MapWith("orders", 1001);

            map.ValueCount("orders").ShouldBe(1);

            map.Add("orders", 1002);
            map.ValueCount("orders").ShouldBe(2);

            map.AddRange("orders", new[] { 1003, 1004 });
            map.ValueCount("orders").ShouldBe(4);

            map.Remove("orders", 1003);
            map.ValueCount("orders").ShouldBe(3);

            map.RemoveRange("orders", new[] { 1001, 1004 });
            map.ValueCount("orders").ShouldBe(1);

            map.Remove("orders");
            map.ValueCount("orders").ShouldBe(0);
        }

        [Fact]
        public void ValueCountAgreesWithTheIndexerAndWithTotalValueCount()
        {
            var map = new MultiDictionary<string, int>(allowDuplicateValues: true);
            map.AddRange("orders", new[] { 1001, 1001 });
            map.AddRange("invoices", new[] { 2001, 2002, 2003 });
            map.Add("notes", 3001);

            map.ValueCount("orders").ShouldBe(map["orders"].Count);
            map.ValueCount("invoices").ShouldBe(map["invoices"].Count);
            map.ValueCount("notes").ShouldBe(map["notes"].Count);

            map.Keys.Sum(key => map.ValueCount(key)).ShouldBe(map.TotalValueCount);
        }

        [Fact]
        public void ValueCountIsZeroForAnEmptyMap()
        {
            var map = Map();

            map.ValueCount("orders").ShouldBe(0);
            map.TotalValueCount.ShouldBe(0);
        }

        [Fact]
        public void ValueCountUsesTheInjectedKeyComparer()
        {
            var map = new MultiDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            map.AddRange("Orders", new[] { 1001, 1002 });

            map.ValueCount("ORDERS").ShouldBe(2);
        }

        [Fact]
        public void ValueCountRejectsANullKeyLikeTheUnderlyingDictionary()
        {
            var map = MapWith("orders", 1001);

            Should.Throw<ArgumentNullException>(() => map.ValueCount(null!));
        }

        [Fact]
        public void ValueCountIsZeroAfterClear()
        {
            var map = MapWith("orders", 1001, 1002);

            map.Clear();

            map.ValueCount("orders").ShouldBe(0);
            map.KeyCount.ShouldBe(0);
        }

        // ------------------------------------------------------------ element shapes

        [Fact]
        public void ReferenceTypeValuesCompareByValue()
        {
            var a = new Uri("https://example.org/a");
            var b = new Uri("https://example.org/b");
            var map = new MultiDictionary<string, Uri>();
            map.Add("links", a);
            map.Add("links", b);

            // Uri overrides Equals, so a distinct instance with the same text still matches.
            map.RemoveRange("links", new[] { new Uri("https://example.org/a") }).ShouldBeTrue();

            map["links"].ShouldBe(new[] { b });
        }

        [Fact]
        public void AWholeKeyCanBeDrainedWithItsOwnValues()
        {
            var map = new MultiDictionary<string, int>(allowDuplicateValues: true);
            map.AddRange("orders", Enumerable.Range(1, 50));
            map.AddRange("invoices", Enumerable.Range(1, 50));

            map.RemoveRange("orders", map["orders"]).ShouldBeTrue();

            map.ContainsKey("orders").ShouldBeFalse();
            map.ValueCount("invoices").ShouldBe(50);
            map.TotalValueCount.ShouldBe(50);
        }
    }
}
