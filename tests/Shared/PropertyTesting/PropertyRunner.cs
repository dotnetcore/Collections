using System;
using System.Collections.Generic;

namespace DotNetCore.Collections.Testing.PropertyTesting
{
    /// <summary>
    /// Runs a <see cref="StatefulProperty{TOperation}"/> from a fixed seed and, when the invariant
    /// is violated, shrinks the failing operation sequence to a minimal reproducer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The shrinker works in two passes. The first keeps the shortest failing <em>prefix</em> of the
    /// generated sequence; it scans linearly rather than binary-searching, because a stateful
    /// invariant is not guaranteed to stay violated once more operations are applied (a later
    /// operation may mask the damage). The second pass greedily removes individual operations for
    /// as long as the remaining sequence still fails, which is what turns a forty-step random walk
    /// into the three or four steps that actually matter.
    /// </para>
    /// <para>
    /// Every replay starts from <see cref="StatefulProperty{TOperation}.Reset"/>, so the reported
    /// sequence is reproducible from a fresh instance.
    /// </para>
    /// </remarks>
    public static class PropertyRunner
    {
        /// <summary>
        /// The step count used when a caller does not specify one.
        /// </summary>
        /// <value>Forty operations, which is long enough for copy-count and key-removal
        /// interactions to appear while keeping a failing run cheap to shrink.</value>
        public const int DefaultStepCount = 40;

        /// <summary>
        /// The largest step count <see cref="Run{TOperation}(StatefulProperty{TOperation}, int, int)"/>
        /// accepts, as a guard against accidentally quadratic shrink times.
        /// </summary>
        public const int MaxStepCount = 500;

        private const int ReplayBudget = 20000;

        /// <summary>
        /// Runs the property for a fixed number of steps from the given seed.
        /// </summary>
        /// <typeparam name="TOperation">The operation type.</typeparam>
        /// <param name="property">The property to drive. It is reset before the run.</param>
        /// <param name="seed">The seed to run from; the same seed always reproduces the same run.</param>
        /// <param name="stepCount">The maximum number of operations to apply.</param>
        /// <returns>The run outcome, including a minimised sequence when the invariant was violated.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="property"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="stepCount"/> is outside <c>[1, MaxStepCount]</c>.</exception>
        public static PropertyResult Run<TOperation>(StatefulProperty<TOperation> property, int seed, int stepCount)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            if (stepCount < 1 || stepCount > MaxStepCount)
            {
                throw new ArgumentOutOfRangeException(nameof(stepCount), stepCount, "The step count must be within [1, MaxStepCount].");
            }

            var generated = new List<TOperation>();
            var random = new DeterministicRandom(seed);
            var failingStepIndex = -1;
            string failure = null;

            property.Reset();

            for (var step = 0; step < stepCount; step++)
            {
                var operation = property.NextOperation(random);
                generated.Add(operation);
                property.Apply(operation);

                failure = property.CheckInvariant();
                if (!string.IsNullOrEmpty(failure))
                {
                    failingStepIndex = step;
                    break;
                }
            }

            if (string.IsNullOrEmpty(failure))
            {
                return PropertyResult.Passed(property.Name, seed, generated.Count);
            }

            var budget = ReplayBudget;
            var minimal = ShrinkToShortestPrefix(property, generated, ref budget);
            minimal = ShrinkByRemovingSteps(property, minimal, ref budget);

            string finalFailure;
            Replay(property, minimal, minimal.Count, out finalFailure);

            var descriptions = new List<string>(minimal.Count);
            for (var index = 0; index < minimal.Count; index++)
            {
                descriptions.Add(property.Describe(minimal[index]));
            }

            return PropertyResult.Failed(
                property.Name,
                seed,
                generated.Count,
                failingStepIndex,
                descriptions,
                string.IsNullOrEmpty(finalFailure) ? failure : finalFailure);
        }

        /// <summary>
        /// Runs the property using <see cref="DefaultStepCount"/>.
        /// </summary>
        /// <typeparam name="TOperation">The operation type.</typeparam>
        /// <param name="property">The property to drive.</param>
        /// <param name="seed">The seed to run from.</param>
        /// <returns>The run outcome.</returns>
        public static PropertyResult Run<TOperation>(StatefulProperty<TOperation> property, int seed)
        {
            return Run(property, seed, DefaultStepCount);
        }

        /// <summary>
        /// Runs the property once per seed in <c>[baseSeed, baseSeed + runs)</c> and returns the
        /// first failing outcome, or the last passing one when every run held.
        /// </summary>
        /// <typeparam name="TOperation">The operation type.</typeparam>
        /// <param name="property">The property to drive.</param>
        /// <param name="baseSeed">The first seed to try.</param>
        /// <param name="runs">The number of seeds to try. Must be positive.</param>
        /// <param name="stepCount">The maximum number of operations per run.</param>
        /// <returns>The first failing outcome, or a passing outcome when all runs held.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="runs"/> is not positive.</exception>
        /// <remarks>
        /// A single seed can miss an interaction that only appears with a particular operation mix.
        /// Sweeping a small block of consecutive seeds keeps the suite deterministic while raising
        /// the chance of catching it, and a failure still reports its own seed for replay.
        /// </remarks>
        public static PropertyResult RunSeeds<TOperation>(StatefulProperty<TOperation> property, int baseSeed, int runs, int stepCount)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            if (runs < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(runs), runs, "At least one run is required.");
            }

            PropertyResult last = null;

            for (var index = 0; index < runs; index++)
            {
                var seed = unchecked(baseSeed + index);
                var result = Run(property, seed, stepCount);
                if (!result.Success)
                {
                    return result;
                }

                last = result;
            }

            return last;
        }

        private static List<TOperation> ShrinkToShortestPrefix<TOperation>(
            StatefulProperty<TOperation> property,
            List<TOperation> generated,
            ref int budget)
        {
            for (var length = 1; length < generated.Count; length++)
            {
                if (budget <= 0)
                {
                    break;
                }

                budget--;

                string ignored;
                if (Replay(property, generated, length, out ignored))
                {
                    return generated.GetRange(0, length);
                }
            }

            return generated;
        }

        private static List<TOperation> ShrinkByRemovingSteps<TOperation>(
            StatefulProperty<TOperation> property,
            List<TOperation> minimal,
            ref int budget)
        {
            var current = minimal;
            var improved = true;

            while (improved && current.Count > 1 && budget > 0)
            {
                improved = false;

                for (var index = 0; index < current.Count; index++)
                {
                    if (budget <= 0)
                    {
                        break;
                    }

                    var candidate = new List<TOperation>(current);
                    candidate.RemoveAt(index);
                    budget--;

                    string ignored;
                    if (Replay(property, candidate, candidate.Count, out ignored))
                    {
                        current = candidate;
                        improved = true;
                        break;
                    }
                }
            }

            return current;
        }

        private static bool Replay<TOperation>(
            StatefulProperty<TOperation> property,
            List<TOperation> operations,
            int count,
            out string failure)
        {
            property.Reset();

            for (var index = 0; index < count; index++)
            {
                property.Apply(operations[index]);

                var message = property.CheckInvariant();
                if (!string.IsNullOrEmpty(message))
                {
                    failure = message;
                    return true;
                }
            }

            failure = null;
            return false;
        }
    }
}
