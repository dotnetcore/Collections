using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// F6-21: the second-axis reverse index (<c>K2 &#8594; set of K1</c>) that backs
    /// <c>GetBySecondKey</c> / <c>CountOfSecondKey</c> / <c>ContainsSecondKey</c> /
    /// <c>RemoveBySecondKey</c>. These tests are about <em>structural agreement</em> rather than
    /// speed: the index is a second source of truth, so every write path (add, add-with-overwrite-
    /// check, try-add, remove, both cascade deletes, clear) has to keep it in step with the trie,
    /// and the pairwise-null cases have to be served from the dedicated null bucket.
    /// </summary>
    public class TwoKeyDictionarySecondAxisIndexTests
    {
        // ------------------------------------------------------------------
        // Agreement with the trie: reads
        // ------------------------------------------------------------------

        [Fact]
        public void GetBySecondKey_ReturnsOnlyTheRequestedSlice()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(2, "USD", 1.05m);
            map.Add(1, "EUR", 0.92m);

            var slice = map.GetBySecondKey("USD").ToList();

            slice.Count.ShouldBe(2);
            slice.ShouldContain(e => e.Key1 == 1 && e.Value == 1.00m);
            slice.ShouldContain(e => e.Key1 == 2 && e.Value == 1.05m);
            slice.ShouldNotContain(e => e.Key1 == 1 && e.Value == 0.92m);
        }

        [Fact]
        public void CountOfSecondKey_MatchesTheEnumeratedSlice()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(2, "USD", 1.05m);
            map.Add(1, "EUR", 0.92m);

            map.CountOfSecondKey("USD").ShouldBe(map.GetBySecondKey("USD").Count());
            map.CountOfSecondKey("JPY").ShouldBe(0);
            map.GetBySecondKey("JPY").ShouldBeEmpty();
        }

        [Fact]
        public void ContainsSecondKey_AgreesWithTheSlice()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            map.ContainsSecondKey("USD").ShouldBeTrue();
            map.ContainsSecondKey("EUR").ShouldBeFalse();
        }

        // ------------------------------------------------------------------
        // Agreement with the trie: writes
        // ------------------------------------------------------------------

        [Fact]
        public void OverwritingAnExistingPair_DoesNotDoubleCountInTheIndex()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(1, "USD", 1.10m);

            map.Count.ShouldBe(1);
            map.CountOfSecondKey("USD").ShouldBe(1);
            map[1, "USD"].ShouldBe(1.10m);
        }

        [Fact]
        public void TryAdd_ExistingPair_LeavesTheIndexUntouched()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            map.TryAdd(1, "USD", 9.99m).ShouldBeFalse();

            map.CountOfSecondKey("USD").ShouldBe(1);
            map[1, "USD"].ShouldBe(1.00m);
        }

        [Fact]
        public void Add_NonOverwriting_ThrowsAndLeavesTheIndexUntouched()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            // The rejected write must not register a phantom entry in the reverse index.
            Should.Throw<ArgumentException>(() => map.Add(1, "USD", 9.99m, overwrite: false));

            map.CountOfSecondKey("USD").ShouldBe(1);
            map.ContainsSecondKey("EUR").ShouldBeFalse();
            map.GetBySecondKey("USD").Select(e => e.Key1).ShouldBe(new[] { 1 });
        }

        [Fact]
        public void Remove_KeepsTheIndexInStep()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(2, "USD", 1.05m);

            map.Remove(1, "USD").ShouldBeTrue();

            map.ContainsSecondKey("USD").ShouldBeTrue();
            map.CountOfSecondKey("USD").ShouldBe(1);
            map.GetBySecondKey("USD").Select(e => e.Key1).ShouldBe(new[] { 2 });

            map.Remove(2, "USD").ShouldBeTrue();
            map.ContainsSecondKey("USD").ShouldBeFalse();
            map.CountOfSecondKey("USD").ShouldBe(0);
        }

        [Fact]
        public void Remove_WithValue_Pattern_KeepsTheIndexInStep()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            map.Remove(1, "USD", out var value).ShouldBeTrue();
            value.ShouldBe(1.00m);

            map.ContainsSecondKey("USD").ShouldBeFalse();
        }

        [Fact]
        public void RemoveByFirstKey_DropsTheWholeSliceFromTheIndex()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(1, "EUR", 0.92m);
            map.Add(2, "EUR", 0.95m);

            map.RemoveByFirstKey(1).ShouldBe(2);

            // "USD" existed only under key 1: a stale index would still claim it.
            map.ContainsSecondKey("USD").ShouldBeFalse();
            map.CountOfSecondKey("USD").ShouldBe(0);
            map.GetBySecondKey("USD").ShouldBeEmpty();

            map.ContainsSecondKey("EUR").ShouldBeTrue();
            map.CountOfSecondKey("EUR").ShouldBe(1);
            map.GetBySecondKey("EUR").Select(e => e.Key1).ShouldBe(new[] { 2 });
        }

        [Fact]
        public void RemoveByFirstKey_MissingKey_LeavesTheIndexUntouched()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            map.RemoveByFirstKey(99).ShouldBe(0);

            map.CountOfSecondKey("USD").ShouldBe(1);
        }

        [Fact]
        public void RemoveBySecondKey_DropsTheSliceAndItsIndexEntry()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(2, "USD", 1.05m);
            map.Add(1, "EUR", 0.92m);

            map.RemoveBySecondKey("USD").ShouldBe(2);

            map.ContainsSecondKey("USD").ShouldBeFalse();
            map.CountOfSecondKey("USD").ShouldBe(0);
            map.Count.ShouldBe(1);
            map.CountOfSecondKey("EUR").ShouldBe(1);
        }

        [Fact]
        public void RemoveBySecondKey_MissingKey_ReturnsZeroWithoutTouchingTheTrie()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            map.RemoveBySecondKey("JPY").ShouldBe(0);
            map.Count.ShouldBe(1);
            map.CountOfSecondKey("USD").ShouldBe(1);
        }

        [Fact]
        public void Clear_ResetsTheIndex()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(2, "EUR", 1.05m);

            map.Clear();

            map.ContainsSecondKey("USD").ShouldBeFalse();
            map.ContainsSecondKey("EUR").ShouldBeFalse();
            map.CountOfSecondKey("USD").ShouldBe(0);
            map.GetBySecondKey("USD").ShouldBeEmpty();
        }

        [Fact]
        public void RemoveThenReAdd_RebuildsTheIndexEntry()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            map.RemoveBySecondKey("USD").ShouldBe(1);
            map.RemoveBySecondKey("USD").ShouldBe(0);
            map.ContainsSecondKey("USD").ShouldBeFalse();

            map.Add(1, "USD", 1.11m);

            map.ContainsSecondKey("USD").ShouldBeTrue();
            map.CountOfSecondKey("USD").ShouldBe(1);
            map.GetBySecondKey("USD").Single().Value.ShouldBe(1.11m);
        }

        // ------------------------------------------------------------------
        // Null components: the dedicated second-axis bucket
        // ------------------------------------------------------------------

        [Fact]
        public void NullSecondKey_IsServedFromTheNullBucket()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, null, 1.00m);
            map.Add(2, null, 2.00m);
            map.Add(1, "USD", 3.00m);

            map.ContainsSecondKey(null).ShouldBeTrue();
            map.CountOfSecondKey(null).ShouldBe(2);
            map.GetBySecondKey(null).Select(e => e.Key1).OrderBy(k => k).ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public void NullSecondKey_IsDistinctFromAnyNonNullKey()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, null, 1.00m);

            map.ContainsSecondKey("").ShouldBeFalse();
            map.ContainsSecondKey(null).ShouldBeTrue();
            map.CountOfSecondKey("").ShouldBe(0);
        }

        [Fact]
        public void NullSecondKey_RemovalDrainsTheNullBucket()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, null, 1.00m);

            map.Remove(1, null).ShouldBeTrue();

            map.ContainsSecondKey(null).ShouldBeFalse();
            map.CountOfSecondKey(null).ShouldBe(0);
            map.GetBySecondKey(null).ShouldBeEmpty();
        }

        [Fact]
        public void NullSecondKey_RemoveBySecondKey_Works()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, null, 1.00m);
            map.Add(2, null, 2.00m);
            map.Add(1, "USD", 3.00m);

            map.RemoveBySecondKey(null).ShouldBe(2);

            map.ContainsSecondKey(null).ShouldBeFalse();
            map.CountOfSecondKey(null).ShouldBe(0);
            map.CountOfSecondKey("USD").ShouldBe(1);
            map.Count.ShouldBe(1);
        }

        [Fact]
        public void NullSecondKey_RemoveBySecondKey_MissingKey_ReturnsZero()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);

            map.RemoveBySecondKey(null).ShouldBe(0);
            map.Count.ShouldBe(1);
        }

        [Fact]
        public void NullFirstKey_IsIndexedUnderItsSecondKey()
        {
            var map = new TwoKeyDictionary<string, string, decimal>();
            map.Add(null, "USD", 1.00m);
            map.Add("X", "USD", 2.00m);

            map.ContainsSecondKey("USD").ShouldBeTrue();
            map.CountOfSecondKey("USD").ShouldBe(2);
            map.GetBySecondKey("USD").Count().ShouldBe(2);

            map.Remove(null, "USD").ShouldBeTrue();

            map.CountOfSecondKey("USD").ShouldBe(1);
            map.GetBySecondKey("USD").Select(e => e.Key1).ShouldBe(new[] { "X" });
        }

        [Fact]
        public void BothComponentsNull_IsAValidIndexedPair()
        {
            var map = new TwoKeyDictionary<string, string, decimal>();
            map.Add(null, null, 1.00m);

            map.ContainsSecondKey(null).ShouldBeTrue();
            map.CountOfSecondKey(null).ShouldBe(1);
            map.GetBySecondKey(null).Single().Key1.ShouldBeNull();

            map.RemoveBySecondKey(null).ShouldBe(1);
            map.ContainsSecondKey(null).ShouldBeFalse();
        }

        [Fact]
        public void RemoveByFirstKey_WithNullFirstKey_DrainsTheNullBucket()
        {
            var map = new TwoKeyDictionary<string, string, decimal>();
            map.Add(null, "USD", 1.00m);
            map.Add(null, null, 2.00m);
            map.Add("X", null, 3.00m);

            map.RemoveByFirstKey(null).ShouldBe(2);

            map.CountOfSecondKey("USD").ShouldBe(0);
            map.CountOfSecondKey(null).ShouldBe(1);
            map.GetBySecondKey(null).Select(e => e.Key1).ShouldBe(new[] { "X" });
        }

        // ------------------------------------------------------------------
        // Comparers: the index has to use the injected ones, not the defaults
        // ------------------------------------------------------------------

        [Fact]
        public void SecondAxisComparer_IsUsedByTheIndex()
        {
            var map = new TwoKeyDictionary<int, string, decimal>(null, StringComparer.OrdinalIgnoreCase);
            map.Add(1, "USD", 1.00m);

            map.ContainsSecondKey("usd").ShouldBeTrue();
            map.CountOfSecondKey("uSd").ShouldBe(1);
            map.GetBySecondKey("USD").Count().ShouldBe(1);

            map.RemoveBySecondKey("usd").ShouldBe(1);
            map.ContainsSecondKey("USD").ShouldBeFalse();
        }

        [Fact]
        public void FirstAxisComparer_CollapsesTheIndexedSetMembership()
        {
            // With a case-insensitive first-axis comparer, ("a","USD") and ("A","USD") are the
            // *same* entry — the trie says so via AxisComparer. The reverse index therefore has to
            // hash the K1 set with the same comparer, otherwise it would report two first keys for
            // one stored entry and CountOfSecondKey would disagree with the trie.
            var map = new TwoKeyDictionary<string, string, decimal>(StringComparer.OrdinalIgnoreCase, null);
            map.Add("a", "USD", 1.00m);
            map.Add("A", "USD", 2.00m);

            map.Count.ShouldBe(1);
            map.CountOfSecondKey("USD").ShouldBe(1);
            map.GetBySecondKey("USD").Count().ShouldBe(1);
            map.GetBySecondKey("USD").Single().Value.ShouldBe(2.00m);

            map.Remove("A", "USD").ShouldBeTrue();
            map.ContainsSecondKey("USD").ShouldBeFalse();
        }

        [Fact]
        public void Clone_CarriesTheIndexOver()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            map.Add(1, "USD", 1.00m);
            map.Add(2, "USD", 1.05m);

            var clone = map.Clone();
            clone.RemoveByFirstKey(1).ShouldBe(1);

            clone.ContainsSecondKey("USD").ShouldBeTrue();
            clone.CountOfSecondKey("USD").ShouldBe(1);

            // Independence: the source still answers from its own index.
            map.ContainsSecondKey("USD").ShouldBeTrue();
            map.CountOfSecondKey("USD").ShouldBe(2);
        }

        // ------------------------------------------------------------------
        // Differential test against a naive model
        // ------------------------------------------------------------------

        [Fact]
        public void RandomOperations_KeepTheFourSecondAxisQueriesEqualToANaiveScan()
        {
            var map = new TwoKeyDictionary<int, string, decimal>();
            var model = new Dictionary<(int, string), decimal>();

            // A null second component is part of the key space on purpose: it is the case the
            // index can not hold in its dictionary and has to serve from the dedicated bucket.
            var firstKeys = new[] { 1, 2, 3 };
            var secondKeys = new[] { null, "USD", "EUR" };
            var random = new Random(20260911);

            for (var step = 0; step < 3000; step++)
            {
                var k1 = firstKeys[random.Next(firstKeys.Length)];
                var k2 = secondKeys[random.Next(secondKeys.Length)];

                switch (random.Next(6))
                {
                    case 0:
                        map.Add(k1, k2, step);
                        model[(k1, k2)] = step;
                        break;

                    case 1:
                        var added = map.TryAdd(k1, k2, step);
                        added.ShouldBe(!model.ContainsKey((k1, k2)));
                        if (added)
                        {
                            model[(k1, k2)] = step;
                        }

                        break;

                    case 2:
                        map.Remove(k1, k2).ShouldBe(model.Remove((k1, k2)));
                        break;

                    case 3:
                        var removedByFirst = map.RemoveByFirstKey(k1);
                        var expectedByFirst = model.Keys.Count(k => k.Item1 == k1);
                        removedByFirst.ShouldBe(expectedByFirst);
                        foreach (var key in model.Keys.Where(k => k.Item1 == k1).ToList())
                        {
                            model.Remove(key);
                        }

                        break;

                    case 4:
                        var removedBySecond = map.RemoveBySecondKey(k2);
                        var expectedBySecond = model.Keys.Count(k => k.Item2 == k2);
                        removedBySecond.ShouldBe(expectedBySecond);
                        foreach (var key in model.Keys.Where(k => k.Item2 == k2).ToList())
                        {
                            model.Remove(key);
                        }

                        break;

                    default:
                        if (random.Next(50) == 0)
                        {
                            map.Clear();
                            model.Clear();
                        }

                        break;
                }

                // The four indexed queries are checked against a fresh scan of the model after
                // every single step, so any path that forgets to maintain the index shows up
                // immediately (and the failing step is reproducible from the fixed seed).
                foreach (var probe in new[] { null, "USD", "EUR" })
                {
                    var expected = model.Where(pair => pair.Key.Item2 == probe)
                        .Select(pair => (pair.Key.Item1, pair.Value))
                        .OrderBy(e => e.Item1)
                        .ToList();

                    map.ContainsSecondKey(probe).ShouldBe(expected.Count > 0);
                    map.CountOfSecondKey(probe).ShouldBe(expected.Count);
                    map.GetBySecondKey(probe)
                        .Select(e => (e.Key1, e.Value))
                        .OrderBy(e => e.Item1)
                        .ToList()
                        .ShouldBe(expected);
                }
            }
        }
    }
}
