using System;
using System.Collections.Generic;

namespace DotNetCore.Collections.Testing.PropertyTesting
{
    /// <summary>
    /// A deterministic pseudo-random source whose seed is part of every failure report, so a
    /// failing property can be replayed exactly by passing the same seed back.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type exists instead of using <see cref="Random"/> directly so that the seed becomes an
    /// explicit, reportable part of the API rather than an implicit constructor argument that is
    /// easy to lose. It also keeps the property-testing kit source-compatible with
    /// <c>net462</c>, which compiles this file at C# 7.3 (see the project notes in
    /// <c>tests/Shared/PropertyTesting/README.md</c>).
    /// </para>
    /// <para>
    /// Instances are stateful: create one per run, never share one across concurrently running
    /// properties.
    /// </para>
    /// </remarks>
    public sealed class DeterministicRandom
    {
        private readonly Random _random;

        /// <summary>
        /// Initializes a new source that produces the same sequence for the same
        /// <paramref name="seed"/>.
        /// </summary>
        /// <param name="seed">The seed to replay from; it is echoed in every failure report.</param>
        public DeterministicRandom(int seed)
        {
            Seed = seed;
            _random = new Random(seed);
        }

        /// <summary>
        /// Gets the seed this source was created with.
        /// </summary>
        /// <value>The value passed to the constructor.</value>
        public int Seed { get; }

        /// <summary>
        /// Returns a non-negative integer strictly below <paramref name="maxExclusive"/>.
        /// </summary>
        /// <param name="maxExclusive">The exclusive upper bound. Must be positive.</param>
        /// <returns>A value in <c>[0, maxExclusive)</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxExclusive"/> is not positive.</exception>
        public int NextIndex(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "The exclusive upper bound must be positive.");
            }

            return _random.Next(maxExclusive);
        }

        /// <summary>
        /// Returns an integer in the half-open interval <c>[minInclusive, maxExclusive)</c>.
        /// </summary>
        /// <param name="minInclusive">The inclusive lower bound.</param>
        /// <param name="maxExclusive">The exclusive upper bound. Must be greater than <paramref name="minInclusive"/>.</param>
        /// <returns>A value in the requested interval.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The interval is empty.</exception>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "The exclusive upper bound must be greater than the inclusive lower bound.");
            }

            return _random.Next(minInclusive, maxExclusive);
        }

        /// <summary>
        /// Returns <c>true</c> or <c>false</c> with equal probability.
        /// </summary>
        /// <returns>A coin flip.</returns>
        public bool NextBool()
        {
            return _random.Next(2) == 0;
        }

        /// <summary>
        /// Returns <c>true</c> for roughly <paramref name="percent"/> out of one hundred calls.
        /// </summary>
        /// <param name="percent">The probability in percent, from 0 through 100.</param>
        /// <returns>A biased coin flip.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="percent"/> is outside <c>[0, 100]</c>.</exception>
        public bool NextChance(int percent)
        {
            if (percent < 0 || percent > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(percent), percent, "The probability must be within [0, 100].");
            }

            return _random.Next(100) < percent;
        }

        /// <summary>
        /// Returns one element picked uniformly from <paramref name="items"/>.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="items">The candidates. Must contain at least one element.</param>
        /// <returns>The selected element.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="items"/> is empty.</exception>
        public T NextItem<T>(IReadOnlyList<T> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            if (items.Count == 0)
            {
                throw new ArgumentException("The candidate list must contain at least one element.", nameof(items));
            }

            return items[_random.Next(items.Count)];
        }
    }
}
