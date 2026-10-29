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
    /// The bag properties applied to <see cref="MultiList{T}"/>. These are the acceptance baseline
    /// for the persistent bag planned for 8.1.0: that type will register its own adapter and run
    /// the same properties unchanged.
    /// </summary>
    public class MultiListPropertyTests
    {
        private const int BaseSeed = 20261029;
        private const int SeedSweep = 20;

        private static readonly IReadOnlyList<int> IntDomain = new[] { 0, 1, 2, 3 };

        [Fact]
        public void Model_equivalence_holds_for_the_default_comparer()
        {
            var property = new BagModelEquivalenceProperty<MultiList<int>, int>(
                new MultiListBagAdapter<int>(),
                "MultiList<int>/default-comparer",
                IntDomain,
                null);

            var result = PropertyRunner.RunSeeds(property, BaseSeed, SeedSweep, PropertyRunner.DefaultStepCount);

            result.Success.ShouldBeTrue(result.ToDiagnosticString());
        }

        [Fact]
        public void Model_equivalence_holds_for_a_single_element_domain()
        {
            var property = new BagModelEquivalenceProperty<MultiList<int>, int>(
                new MultiListBagAdapter<int>(),
                "MultiList<int>/single-element",
                new[] { 7 },
                null);

            var result = PropertyRunner.RunSeeds(property, BaseSeed + 1, SeedSweep, PropertyRunner.DefaultStepCount);

            result.Success.ShouldBeTrue(result.ToDiagnosticString());
        }

        [Fact]
        public void Model_equivalence_holds_when_the_comparer_folds_case()
        {
            // Four strings but only two equivalence classes: a comparer-blind implementation would
            // report four distinct elements instead of two.
            var property = new BagModelEquivalenceProperty<MultiList<string>, string>(
                new MultiListBagAdapter<string>(),
                "MultiList<string>/ordinal-ignore-case",
                new[] { "a", "A", "b", "B" },
                StringComparer.OrdinalIgnoreCase);

            var result = PropertyRunner.RunSeeds(property, BaseSeed + 2, SeedSweep, PropertyRunner.DefaultStepCount);

            result.Success.ShouldBeTrue(result.ToDiagnosticString());
        }

        [Fact]
        public void Model_equivalence_holds_when_null_is_a_valid_element()
        {
            // MultiList tracks null outside its dictionary, so this path is structurally different
            // from every other element and deserves its own property run.
            var property = new BagModelEquivalenceProperty<MultiList<string>, string>(
                new MultiListBagAdapter<string>(),
                "MultiList<string>/null-element",
                new[] { "a", "b", null },
                null);

            var result = PropertyRunner.RunSeeds(property, BaseSeed + 3, SeedSweep, PropertyRunner.DefaultStepCount);

            result.Success.ShouldBeTrue(result.ToDiagnosticString());
        }
    }
}
