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
    /// The multimap properties applied to <see cref="MultiDictionary{TKey,TValue}"/>. As with the
    /// bag suite, this is the acceptance baseline the persistent multimap planned for 8.1.0 will run
    /// against through its own adapter.
    /// </summary>
    public class MultiDictionaryPropertyTests
    {
        private const int BaseSeed = 20261029;
        private const int SeedSweep = 20;

        private static readonly IReadOnlyList<int> IntKeys = new[] { 0, 1, 2 };
        private static readonly IReadOnlyList<int> IntValues = new[] { 10, 20, 30 };

        [Fact]
        public void Model_equivalence_holds_when_duplicate_values_are_allowed()
        {
            var property = new MultimapModelEquivalenceProperty<MultiDictionary<int, int>, int, int>(
                new MultiDictionaryAdapter<int, int>(true),
                "MultiDictionary<int,int>/duplicates-allowed",
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
            // With duplicates disallowed a repeated Add is silently ignored, which changes both the
            // copy count and the return value of the later removals.
            var property = new MultimapModelEquivalenceProperty<MultiDictionary<int, int>, int, int>(
                new MultiDictionaryAdapter<int, int>(false),
                "MultiDictionary<int,int>/duplicates-rejected",
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
            // "a" and "A" are the same key here, so the map must hold one key with the union of both
            // spellings' values rather than two keys.
            var property = new MultimapModelEquivalenceProperty<MultiDictionary<string, int>, string, int>(
                new MultiDictionaryAdapter<string, int>(true),
                "MultiDictionary<string,int>/case-insensitive-keys",
                new[] { "a", "A", "b" },
                new[] { 1, 2 },
                StringComparer.OrdinalIgnoreCase,
                true);

            var result = PropertyRunner.RunSeeds(property, BaseSeed + 2, SeedSweep, PropertyRunner.DefaultStepCount);

            result.Success.ShouldBeTrue(result.ToDiagnosticString());
        }
    }
}
