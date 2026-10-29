using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DotNetCore.Collections.Testing.PropertyTesting
{
    /// <summary>
    /// The outcome of running one stateful property: either success, or a minimal failing
    /// operation sequence together with the seed that reproduces it.
    /// </summary>
    /// <remarks>
    /// The runner returns this object instead of throwing so that the kit stays independent of any
    /// test framework and can be linked into test projects that use different assertion styles.
    /// Test methods are expected to assert <see cref="Success"/> and pass
    /// <see cref="Message"/> as the assertion message.
    /// </remarks>
    public sealed class PropertyResult
    {
        private static readonly string[] NoSteps = new string[0];

        private PropertyResult(
            string name,
            int seed,
            bool success,
            int stepCount,
            int failingStepIndex,
            IReadOnlyList<string> minimalSteps,
            string message)
        {
            Name = name;
            Seed = seed;
            Success = success;
            StepCount = stepCount;
            FailingStepIndex = failingStepIndex;
            MinimalSteps = minimalSteps;
            Message = message;
        }

        /// <summary>
        /// Gets the property name, matching <see cref="StatefulProperty{TOperation}.Name"/>.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the seed that replays this run.
        /// </summary>
        public int Seed { get; }

        /// <summary>
        /// Gets a value indicating whether the invariant held for every step.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the number of steps the run was configured with.
        /// </summary>
        public int StepCount { get; }

        /// <summary>
        /// Gets the zero-based index of the step that first violated the invariant, or -1 on success.
        /// </summary>
        public int FailingStepIndex { get; }

        /// <summary>
        /// Gets the minimised operation sequence; empty on success.
        /// </summary>
        /// <value>
        /// The shortest sequence the shrinker could find that still violates the invariant. Index 0
        /// is the first operation to apply after <see cref="StatefulProperty{TOperation}.Reset"/>.
        /// </value>
        public IReadOnlyList<string> MinimalSteps { get; }

        /// <summary>
        /// Gets the invariant violation description, or the empty string on success.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Creates a passing result.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="seed">The seed that was used.</param>
        /// <param name="stepCount">The number of steps that were applied.</param>
        /// <returns>A result whose <see cref="Success"/> is <c>true</c>.</returns>
        internal static PropertyResult Passed(string name, int seed, int stepCount)
        {
            return new PropertyResult(name, seed, true, stepCount, -1, NoSteps, string.Empty);
        }

        /// <summary>
        /// Creates a failing result carrying the minimised sequence.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="seed">The seed that reproduces the failure.</param>
        /// <param name="stepCount">The number of steps the run was configured with.</param>
        /// <param name="failingStepIndex">The zero-based index of the first violating step.</param>
        /// <param name="minimalSteps">The shortest still-failing sequence.</param>
        /// <param name="message">The invariant violation description.</param>
        /// <returns>A result whose <see cref="Success"/> is <c>false</c>.</returns>
        internal static PropertyResult Failed(
            string name,
            int seed,
            int stepCount,
            int failingStepIndex,
            IReadOnlyList<string> minimalSteps,
            string message)
        {
            return new PropertyResult(name, seed, false, stepCount, failingStepIndex, minimalSteps, message);
        }

        /// <summary>
        /// Renders a report suitable for an assertion message or a bug description.
        /// </summary>
        /// <returns>
        /// On success, one line stating that the property held. On failure, the seed, the minimised
        /// sequence with one operation per line, the invariant violation, and a replay hint.
        /// </returns>
        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();

            if (Success)
            {
                builder.Append("Property '").Append(Name).Append("' held for ")
                       .Append(StepCount.ToString(CultureInfo.InvariantCulture))
                       .Append(" step(s) with seed ")
                       .Append(Seed.ToString(CultureInfo.InvariantCulture))
                       .Append('.');
                return builder.ToString();
            }

            builder.Append("Property '").Append(Name).Append("' failed with seed ")
                   .Append(Seed.ToString(CultureInfo.InvariantCulture))
                   .Append(" (first violation at step ")
                   .Append((FailingStepIndex + 1).ToString(CultureInfo.InvariantCulture))
                   .Append(" of ")
                   .Append(StepCount.ToString(CultureInfo.InvariantCulture))
                   .Append(").").AppendLine();

            builder.Append("Minimised sequence (").Append(MinimalSteps.Count.ToString(CultureInfo.InvariantCulture))
                   .Append(" step(s), starting from a fresh Reset):");
            for (var index = 0; index < MinimalSteps.Count; index++)
            {
                builder.AppendLine();
                builder.Append("  ").Append((index + 1).ToString(CultureInfo.InvariantCulture))
                       .Append(". ").Append(MinimalSteps[index]);
            }

            builder.AppendLine();
            builder.Append("Invariant: ").Append(Message).AppendLine();
            builder.Append("Replay with seed ").Append(Seed.ToString(CultureInfo.InvariantCulture))
                   .Append("; if the invariant no longer fails, the sequence above is the input to debug against.");

            return builder.ToString();
        }

        /// <summary>
        /// Returns the same text as <see cref="ToDiagnosticString"/>.
        /// </summary>
        /// <returns>The diagnostic report.</returns>
        public override string ToString()
        {
            return ToDiagnosticString();
        }
    }
}
