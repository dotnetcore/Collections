using System;
using System.Collections.Generic;
using DotNetCore.Collections.Multi;
using DotNetCore.Collections.Testing.PropertyTesting;
using DotNetCore.Collections.Testing.PropertyTesting.Adapters;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// The multimap properties applied to
    /// <see cref="PersistentMultiDictionary{TKey,TValue}"/>.
    /// </summary>
    /// <remarks>
    /// The same three configurations the mutable multimap is measured in, so the two families are
    /// held to one contract rather than to two interpretations of it. Every case is also a regression
    /// guard for the trie: a missed slot, a node dropped one level too early or a surviving entry
    /// that should have vanished all show up as a divergence the runner shrinks to a short sequence.
    /// </remarks>
    public class PersistentMultiDictionaryPropertyTests
    {
        private const int BaseSeed = 20261102;
        private const int SeedSweep = 20;

        private static readonly IReadOnlyList<int> IntKeys = new[] { 0, 1, 2 };
        private static readonly IReadOnlyList<int> IntValues = new[] { 10, 20, 30 };

        [Fact]
        public void Model_equivalence_holds_when_duplicate_values_are_allowed()
        {
            var property = new MultimapModelEquivalenceProperty<PersistentMultiDictionary<int, int>, int, int>(
                new PersistentMultiDictionaryAdapter<int, int>(true),
                "PersistentMultiDictionary<int,int>/duplicates-allowed",
                IntKeys,
                IntValues,
                null,
                true);

            var result = PropertyRunner.RunSeeds(property, BaseSeed, SeedSweep, PropertyRunner.DefaultStepCount);

            result.Success.ShouldBeTrue(result.ToDiagnosticString());
        }

        [Fact]
        public void Model_equivalence_holds_when_each_key_behaves_as_a_set()
        {
            var property = new MultimapModelEquivalenceProperty<PersistentMultiDictionary<int, int>, int, int>(
                new PersistentMultiDictionaryAdapter<int, int>(false),
                "PersistentMultiDictionary<int,int>/duplicates-rejected",
                IntKeys,
                IntValues,
                null,
                false);

            var result = PropertyRunner.RunSeeds(property, BaseSeed + 1, SeedSweep, PropertyRunner.DefaultStepCount);

            result.Success.ShouldBeTrue(result.ToDiagnosticString());
        }

        [Fact]
        public void Model_equivalence_holds_when_the_key_comparer_folds_case()
        {
            // "a" and "A" are one key, so the trie must place them in one slot; the comparer's hash
            // is what decides that, and a trie that hashes differently from the comparer would keep
            // two keys.
            var property = new MultimapModelEquivalenceProperty<PersistentMultiDictionary<string, int>, string, int>(
                new PersistentMultiDictionaryAdapter<string, int>(true),
                "PersistentMultiDictionary<string,int>/case-insensitive-keys",
                new[] { "a", "A", "b" },
                new[] { 1, 2 },
                StringComparer.OrdinalIgnoreCase,
                true);

            var result = PropertyRunner.RunSeeds(property, BaseSeed + 2, SeedSweep, PropertyRunner.DefaultStepCount);

            result.Success.ShouldBeTrue(result.ToDiagnosticString());
        }

        [Fact]
        public void Model_equivalence_holds_for_a_single_key()
        {
            // One key forces every copy into a single entry's value list, which exercises the list
            // path rather than the trie's slot space.
            var property = new MultimapModelEquivalenceProperty<PersistentMultiDictionary<int, int>, int, int>(
                new PersistentMultiDictionaryAdapter<int, int>(true),
                "PersistentMultiDictionary<int,int>/single-key",
                new[] { 7 },
                IntValues,
                null,
                true);

            var result = PropertyRunner.RunSeeds(property, BaseSeed + 3, SeedSweep, PropertyRunner.DefaultStepCount);

            result.Success.ShouldBeTrue(result.ToDiagnosticString());
        }

        [Fact]
        public void An_earlier_snapshot_is_unaffected_by_later_writes()
        {
            // The whole reason for structural sharing: the previous map must keep its own view while
            // the new one diverges. A node mutated in place instead of copied would break this even
            // though every single-snapshot property still passed.
            var original = new PersistentMultiDictionary<int, int>()
                .Add(1, 10)
                .Add(1, 20)
                .Add(2, 30);

            var extended = original
                .Add(1, 40)
                .Add(3, 50)
                .Remove(2, 30);

            original.KeyCount.ShouldBe(2);
            original.TotalValueCount.ShouldBe(3);
            original.ValueCount(1).ShouldBe(2);
            original.Contains(1, 40).ShouldBeFalse();
            original.ContainsKey(3).ShouldBeFalse();
            original.ContainsKey(2).ShouldBeTrue();

            extended.KeyCount.ShouldBe(2, "key 2 left with its last value and key 3 arrived");
            extended.TotalValueCount.ShouldBe(4);
            extended.ValueCount(1).ShouldBe(3);
            extended.Contains(1, 40).ShouldBeTrue();
            extended.ContainsKey(3).ShouldBeTrue();
            extended.ContainsKey(2).ShouldBeFalse();
        }

        [Fact]
        public void Add_and_remove_do_not_mutate_the_receiver()
        {
            var original = new PersistentMultiDictionary<int, int>().Add(1, 10);

            var added = original.Add(1, 20);
            var removed = original.Remove(1, 10);

            ReferenceEquals(original, added).ShouldBeFalse("a changed map must be a new instance");
            ReferenceEquals(original, removed).ShouldBeFalse();
            original.ValueCount(1).ShouldBe(1, "the receiver must still hold exactly what it did");
            original.Contains(1, 10).ShouldBeTrue();
            original.Contains(1, 20).ShouldBeFalse();
        }

        [Fact]
        public void Removing_or_adding_a_duplicate_returns_the_same_instance()
        {
            var map = new PersistentMultiDictionary<int, int>(false).Add(1, 10);

            ReferenceEquals(map, map.Add(1, 10)).ShouldBeTrue(
                "with duplicates rejected the value is already there, so nothing changes");
            ReferenceEquals(map, map.Remove(1, 99)).ShouldBeTrue(
                "removing a copy that is not present changes nothing");
            ReferenceEquals(map, map.Remove(99)).ShouldBeTrue(
                "removing an absent key changes nothing");
        }

        [Fact]
        public void Removing_the_last_value_drops_the_key()
        {
            var map = new PersistentMultiDictionary<int, int>().Add(1, 10).Add(2, 20);

            var emptied = map.Remove(1, 10);

            emptied.KeyCount.ShouldBe(1);
            emptied.TotalValueCount.ShouldBe(1);
            emptied.ContainsKey(1).ShouldBeFalse("a key with no copies must not linger");
            emptied.ValueCount(1).ShouldBe(0);
            emptied[1].ShouldBeEmpty("the indexer promises an empty collection for an absent key");
        }
    }
}
