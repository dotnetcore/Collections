using System;
using System.Collections.Generic;

namespace DotNetCore.Collections.Internal
{
    /// <summary>
    /// The terminal half of the P0 polyfill layer: the operators that answer with a single value
    /// rather than a sequence. They are generic over the source arm and reach the sequence through
    /// the same three-hook protocol as <see cref="ValueEnumerableCore"/>, so a contiguous source
    /// takes the bulk path and a lazy one still works.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The empty-source contract is the BCL's, and it is not the same for every element type:
    /// <c>Enumerable.MaxBy</c> / <c>MinBy</c> answer <see langword="default"/> when the element type
    /// can be <see langword="null"/> and throw when it cannot. Reproducing that split is what keeps
    /// a low-TFM caller's code compiling against the polyfill unchanged when it later moves to a
    /// target that has the BCL operator.
    /// </para>
    /// <para>
    /// Ties keep the first element that reached the extreme, which is what the BCL does as well.
    /// </para>
    /// </remarks>
    internal static class PolyfillTerminals
    {
        /// <summary>Returns the element with the greatest key.</summary>
        /// <typeparam name="TSource">The value enumerable being searched.</typeparam>
        /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are ranked by.</typeparam>
        /// <param name="source">The sequence to search.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The element with the greatest key, or <see langword="default"/> when the sequence
        /// is empty and the element type can be <see langword="null"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The sequence is empty and the element type
        /// cannot be <see langword="null"/>.</exception>
        internal static T MaxBy<TSource, TEnumerator, T, TKey>(TSource source, Func<T, TKey> keySelector, IComparer<TKey>? comparer)
            where TSource : struct, IValueEnumerable<T, TEnumerator>, IValueEnumerableHooks<T>
            where TEnumerator : struct, IEnumerator<T>
        {
            if (keySelector is null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            var ranking = comparer ?? Comparer<TKey>.Default;

#if NETCOREAPP3_0_OR_GREATER
            if (source.TryGetSpan(out var span))
            {
                if (span.Length == 0)
                {
                    return Empty<T>();
                }

                var best = span[0];
                var bestKey = keySelector(best);
                for (var i = 1; i < span.Length; i++)
                {
                    var candidate = span[i];
                    var candidateKey = keySelector(candidate);
                    if (ranking.Compare(candidateKey, bestKey) > 0)
                    {
                        best = candidate;
                        bestKey = candidateKey;
                    }
                }

                return best;
            }
#endif

            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                {
                    return Empty<T>();
                }

                var best = enumerator.Current;
                var bestKey = keySelector(best);
                while (enumerator.MoveNext())
                {
                    var candidate = enumerator.Current;
                    var candidateKey = keySelector(candidate);
                    if (ranking.Compare(candidateKey, bestKey) > 0)
                    {
                        best = candidate;
                        bestKey = candidateKey;
                    }
                }

                return best;
            }
        }

        /// <summary>Returns the element with the smallest key.</summary>
        /// <typeparam name="TSource">The value enumerable being searched.</typeparam>
        /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are ranked by.</typeparam>
        /// <param name="source">The sequence to search.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The element with the smallest key, or <see langword="default"/> when the sequence
        /// is empty and the element type can be <see langword="null"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The sequence is empty and the element type
        /// cannot be <see langword="null"/>.</exception>
        internal static T MinBy<TSource, TEnumerator, T, TKey>(TSource source, Func<T, TKey> keySelector, IComparer<TKey>? comparer)
            where TSource : struct, IValueEnumerable<T, TEnumerator>, IValueEnumerableHooks<T>
            where TEnumerator : struct, IEnumerator<T>
        {
            if (keySelector is null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            var ranking = comparer ?? Comparer<TKey>.Default;

#if NETCOREAPP3_0_OR_GREATER
            if (source.TryGetSpan(out var span))
            {
                if (span.Length == 0)
                {
                    return Empty<T>();
                }

                var best = span[0];
                var bestKey = keySelector(best);
                for (var i = 1; i < span.Length; i++)
                {
                    var candidate = span[i];
                    var candidateKey = keySelector(candidate);
                    if (ranking.Compare(candidateKey, bestKey) < 0)
                    {
                        best = candidate;
                        bestKey = candidateKey;
                    }
                }

                return best;
            }
#endif

            using (var enumerator = source.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                {
                    return Empty<T>();
                }

                var best = enumerator.Current;
                var bestKey = keySelector(best);
                while (enumerator.MoveNext())
                {
                    var candidate = enumerator.Current;
                    var candidateKey = keySelector(candidate);
                    if (ranking.Compare(candidateKey, bestKey) < 0)
                    {
                        best = candidate;
                        bestKey = candidateKey;
                    }
                }

                return best;
            }
        }

        /// <summary>
        /// The BCL's answer to an empty source: <see langword="default"/> when the element type can
        /// be <see langword="null"/>, and an <see cref="InvalidOperationException"/> when it cannot.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <returns><see langword="default"/> for an element type that admits <see langword="null"/>.</returns>
        /// <exception cref="InvalidOperationException">The element type cannot be
        /// <see langword="null"/>.</exception>
        private static T Empty<T>()
        {
            if (default(T) is null)
            {
                return default!;
            }

            throw new InvalidOperationException("Sequence contains no elements.");
        }
    }
}
