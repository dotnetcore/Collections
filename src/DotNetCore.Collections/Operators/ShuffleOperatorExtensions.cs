using System;
using System.Collections.Generic;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The randomization operator of the P2 layer: <c>Shuffle</c>, a Fisher-Yates permutation of the
    /// source.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The permutation is the inside-out-free form of Fisher-Yates: walk the buffer from the last
    /// slot down and swap each slot with a uniformly drawn earlier-or-equal one. That is O(n) with
    /// n - 1 draws and no auxiliary array, and unlike a "sort by random key" trick it is exactly
    /// uniform rather than merely approximately so.
    /// </para>
    /// <para>
    /// The operator is eager and returns a <see cref="List{T}"/>: a shuffle cannot be streamed,
    /// because the last element of the source can legitimately end up first. The walk into the
    /// buffer still runs through the value-type enumerator, so the four-source dispatch is worth
    /// keeping even though the answer is materialized.
    /// </para>
    /// <para>
    /// <c>random</c> exists so that a caller can pass a seeded generator and get a
    /// reproducible permutation. Left null, a fresh <see cref="Random"/> is created per call, which
    /// is deliberate: a shared one would make two calls in the same tick correlated, and the point
    /// of a shuffle is that they are not.
    /// </para>
    /// </remarks>
    public static partial class CollectionExtensions
    {
        /// <summary>Returns a uniformly random permutation of the array.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The array to shuffle.</param>
        /// <param name="random">The generator to draw from, or <see langword="null"/> to create one
        /// for this call.</param>
        /// <returns>A new list holding every element of the source exactly once, in random
        /// order.</returns>
        public static List<T> Shuffle<T>(this ArrayValueEnumerable<T> source, Random? random = null)
            => ShuffleCore<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, random);

        /// <summary>Returns a uniformly random permutation of the list.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The list to shuffle.</param>
        /// <param name="random">The generator to draw from, or <see langword="null"/> to create one
        /// for this call.</param>
        /// <returns>A new list holding every element of the source exactly once, in random
        /// order.</returns>
        public static List<T> Shuffle<T>(this ListValueEnumerable<T> source, Random? random = null)
            => ShuffleCore<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, random);

        /// <summary>Returns a uniformly random permutation of the read-only list.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The read-only list to shuffle.</param>
        /// <param name="random">The generator to draw from, or <see langword="null"/> to create one
        /// for this call.</param>
        /// <returns>A new list holding every element of the source exactly once, in random
        /// order.</returns>
        public static List<T> Shuffle<T>(this ReadOnlyListValueEnumerable<T> source, Random? random = null)
            => ShuffleCore<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, random);

        /// <summary>Returns a uniformly random permutation of the sequence.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to shuffle.</param>
        /// <param name="random">The generator to draw from, or <see langword="null"/> to create one
        /// for this call.</param>
        /// <returns>A new list holding every element of the source exactly once, in random
        /// order.</returns>
        public static List<T> Shuffle<T>(this EnumerableValueEnumerable<T> source, Random? random = null)
            => ShuffleCore<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, random);

        private static List<T> ShuffleCore<TEnumerable, TEnumerator, T>(TEnumerable source, Random? random)
            where TEnumerable : struct, IValueEnumerable<T, TEnumerator>, IValueEnumerableHooks<T>
            where TEnumerator : struct, IEnumerator<T>
        {
            // The buffer is filled through the value-type enumerator; the count hook only saves the
            // growth steps, so a source that cannot answer it still walks the same path.
            var buffer = source.TryGetNonEnumeratedCount(out var known)
                ? new List<T>(known)
                : new List<T>();

            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    buffer.Add(enumerator.Current);
                }
            }

            var rng = random ?? new Random();
            for (var i = buffer.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                if (i == j)
                {
                    continue;
                }

                var swap = buffer[i];
                buffer[i] = buffer[j];
                buffer[j] = swap;
            }

            return buffer;
        }
    }
}
