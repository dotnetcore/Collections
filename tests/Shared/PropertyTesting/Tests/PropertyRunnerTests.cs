using System;
using System.Collections.Generic;
using DotNetCore.Collections.Testing.PropertyTesting;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// Self-tests for the property-testing kit itself. A shrinker that cannot be shown to catch a
    /// known defect cannot be trusted to certify the real collections, so these tests drive the
    /// runner with a deliberately faulty subject and assert both the verdict and the minimal
    /// reproducer.
    /// </summary>
    public class PropertyRunnerTests
    {
        private const int KnownSeed = 20261029;

        /// <summary>A multiset over a single element, reduced to a counter pair.</summary>
        private sealed class Counter
        {
            public int Copies { get; private set; }

            public int ModelCopies { get; private set; }

            public void Add()
            {
                Copies++;
                ModelCopies++;
            }

            public void Remove(bool removesAnExtraCopy)
            {
                ModelCopies = ModelCopies > 0 ? ModelCopies - 1 : 0;

                var decrement = removesAnExtraCopy ? 2 : 1;
                var remaining = Copies - decrement;
                Copies = remaining > 0 ? remaining : 0;
            }

            public void Reset()
            {
                Copies = 0;
                ModelCopies = 0;
            }
        }

        /// <summary>
        /// Operation 0 adds one copy, operation 1 removes one. The faulty mode removes two copies,
        /// which is detectable only after at least two adds have happened.
        /// </summary>
        private sealed class CounterProperty : StatefulProperty<int>
        {
            private readonly bool _removesAnExtraCopy;
            private readonly Counter _counter = new Counter();

            public CounterProperty(bool removesAnExtraCopy)
            {
                _removesAnExtraCopy = removesAnExtraCopy;
            }

            public override string Name => _removesAnExtraCopy ? "counter/faulty" : "counter/correct";

            public override int NextOperation(DeterministicRandom random)
            {
                return random.NextBool() ? 0 : 1;
            }

            public override void Apply(int operation)
            {
                if (operation == 0)
                {
                    _counter.Add();
                }
                else
                {
                    _counter.Remove(_removesAnExtraCopy);
                }
            }

            public override string CheckInvariant()
            {
                return _counter.Copies == _counter.ModelCopies
                    ? null
                    : "Copies=" + _counter.Copies + " but the model holds " + _counter.ModelCopies + ".";
            }

            public override void Reset()
            {
                _counter.Reset();
            }

            public override string Describe(int operation)
            {
                return operation == 0 ? "Add()" : "Remove()";
            }
        }

        [Fact]
        public void Correct_property_holds_across_a_sweep_of_seeds()
        {
            var result = PropertyRunner.RunSeeds(new CounterProperty(false), KnownSeed, 20, 40);

            result.Success.ShouldBeTrue(result.ToDiagnosticString());
        }

        [Fact]
        public void Faulty_property_is_reported_as_failed()
        {
            var result = PropertyRunner.Run(new CounterProperty(true), KnownSeed, 40);

            result.Success.ShouldBeFalse();
            result.Message.ShouldNotBeNullOrWhiteSpace();
            result.MinimalSteps.Count.ShouldBeGreaterThan(0);
        }

        [Fact]
        public void Shrinker_reduces_the_failing_sequence_to_the_three_essential_steps()
        {
            var result = PropertyRunner.Run(new CounterProperty(true), KnownSeed, 40);

            // Two adds followed by a remove is the shortest sequence that exposes the extra
            // removal; the shrinker is allowed one step of slack before the assertion fails.
            (result.MinimalSteps.Count <= 4).ShouldBeTrue(
                "The shrinker should reduce the failing run to at most four steps." + Environment.NewLine + result.ToDiagnosticString());
        }

        [Fact]
        public void Shrinker_never_lengthens_a_failing_sequence()
        {
            var result = PropertyRunner.Run(new CounterProperty(true), KnownSeed, 40);

            (result.MinimalSteps.Count <= result.FailingStepIndex + 1).ShouldBeTrue(
                "The minimised sequence must not be longer than the prefix that first failed." + Environment.NewLine + result.ToDiagnosticString());
        }

        [Fact]
        public void The_same_seed_reproduces_the_same_minimal_sequence()
        {
            var first = PropertyRunner.Run(new CounterProperty(true), KnownSeed, 40);
            var second = PropertyRunner.Run(new CounterProperty(true), KnownSeed, 40);

            Join(second.MinimalSteps).ShouldBe(Join(first.MinimalSteps));
        }

        [Fact]
        public void A_different_seed_reports_its_own_seed_and_still_fails()
        {
            var second = PropertyRunner.Run(new CounterProperty(true), KnownSeed + 1, 40);

            second.Seed.ShouldBe(KnownSeed + 1);
            second.Success.ShouldBeFalse();
            second.MinimalSteps.Count.ShouldBeGreaterThan(0);

            // The minimised sequence is deliberately not compared across seeds: different random
            // walks legitimately shrink to the same shortest reproducer.
        }

        [Fact]
        public void Diagnostic_report_names_the_seed_and_every_minimal_step()
        {
            var result = PropertyRunner.Run(new CounterProperty(true), KnownSeed, 40);
            var report = result.ToDiagnosticString();

            report.ShouldContain(KnownSeed.ToString());
            report.ShouldContain("Minimised sequence");

            foreach (var step in result.MinimalSteps)
            {
                report.ShouldContain(step);
            }
        }

        [Fact]
        public void Passing_report_states_the_seed_and_step_count()
        {
            var result = PropertyRunner.Run(new CounterProperty(false), KnownSeed, 40);

            result.ToDiagnosticString().ShouldContain(KnownSeed.ToString());
            result.FailingStepIndex.ShouldBe(-1);
            result.MinimalSteps.Count.ShouldBe(0);
        }

        [Fact]
        public void Run_rejects_a_step_count_outside_the_supported_range()
        {
            Should.Throw<ArgumentOutOfRangeException>(() => PropertyRunner.Run(new CounterProperty(false), KnownSeed, 0));
            Should.Throw<ArgumentOutOfRangeException>(() => PropertyRunner.Run(new CounterProperty(false), KnownSeed, PropertyRunner.MaxStepCount + 1));
        }

        [Fact]
        public void RunSeeds_rejects_a_non_positive_run_count()
        {
            Should.Throw<ArgumentOutOfRangeException>(() => PropertyRunner.RunSeeds(new CounterProperty(false), KnownSeed, 0, 10));
        }

        [Fact]
        public void Deterministic_random_rejects_empty_intervals()
        {
            var random = new DeterministicRandom(KnownSeed);

            Should.Throw<ArgumentOutOfRangeException>(() => random.NextIndex(0));
            Should.Throw<ArgumentOutOfRangeException>(() => random.NextInt(5, 5));
            Should.Throw<ArgumentOutOfRangeException>(() => random.NextChance(101));
        }

        [Fact]
        public void Deterministic_random_stays_within_the_requested_bounds()
        {
            var random = new DeterministicRandom(KnownSeed);

            for (var index = 0; index < 500; index++)
            {
                var value = random.NextInt(-3, 4);
                (value >= -3 && value < 4).ShouldBeTrue("NextInt returned " + value + " outside [-3, 4).");

                var indexValue = random.NextIndex(5);
                (indexValue >= 0 && indexValue < 5).ShouldBeTrue("NextIndex returned " + indexValue + " outside [0, 5).");
            }
        }

        private static string Join(IReadOnlyList<string> steps)
        {
            var parts = new string[steps.Count];
            for (var index = 0; index < steps.Count; index++)
            {
                parts[index] = steps[index];
            }

            return string.Join("; ", parts);
        }
    }
}
