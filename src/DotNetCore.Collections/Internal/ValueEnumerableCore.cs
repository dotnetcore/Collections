using System;
using System.Buffers;
using System.Collections.Generic;

namespace DotNetCore.Collections.Internal
{
    /// <summary>
    /// The terminal half of the engine: the consumers that turn a value enumerable into an answer.
    /// Every method here is generic over the source arm and reaches the sequence through the
    /// three-hook protocol of <see cref="IValueEnumerableHooks{T}"/>, so a bulk-capable source
    /// never pays for an element-by-element walk and a lazy source still works.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The type parameters are explicit because C# cannot infer them from a generic constraint;
    /// the per-arm entry surfaces in <see cref="ShortCircuitExtensions"/> and
    /// <see cref="PoolingExtensions"/> exist to supply them. Because the constraint names a struct,
    /// the calls into <see cref="IValueEnumerable{T,TEnumerator}.GetEnumerator"/> and into the hooks
    /// are constrained calls - the JIT devirtualises them and nothing is boxed.
    /// </para>
    /// <para>
    /// The short-circuit methods deliberately stop at the first answer rather than counting the
    /// sequence first: this is the shape the paging layer needs for its "is there another page"
    /// probe, where a full walk would be the whole cost.
    /// </para>
    /// </remarks>
    internal static class ValueEnumerableCore
    {
        /// <summary>Determines whether any element satisfies <paramref name="predicate"/>.</summary>
        /// <typeparam name="TSource">The value enumerable being tested.</typeparam>
        /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to test.</param>
        /// <param name="predicate">The test to apply to each element.</param>
        /// <returns><see langword="true"/> as soon as one element passes the test.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <see langword="null"/>.</exception>
        internal static bool Any<TSource, TEnumerator, T>(TSource source, Func<T, bool> predicate)
            where TSource : struct, IValueEnumerable<T, TEnumerator>, IValueEnumerableHooks<T>
            where TEnumerator : struct, IEnumerator<T>
        {
            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

#if NETCOREAPP3_0_OR_GREATER
            if (source.TryGetSpan(out var span))
            {
                for (var i = 0; i < span.Length; i++)
                {
                    if (predicate(span[i]))
                    {
                        return true;
                    }
                }

                return false;
            }
#endif

            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (predicate(enumerator.Current))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Returns the first element that satisfies <paramref name="predicate"/>.</summary>
        /// <typeparam name="TSource">The value enumerable being searched.</typeparam>
        /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to search.</param>
        /// <param name="predicate">The test to apply to each element.</param>
        /// <returns>The first matching element.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">No element satisfies the test.</exception>
        internal static T First<TSource, TEnumerator, T>(TSource source, Func<T, bool> predicate)
            where TSource : struct, IValueEnumerable<T, TEnumerator>, IValueEnumerableHooks<T>
            where TEnumerator : struct, IEnumerator<T>
        {
            if (predicate is null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

#if NETCOREAPP3_0_OR_GREATER
            if (source.TryGetSpan(out var span))
            {
                for (var i = 0; i < span.Length; i++)
                {
                    var candidate = span[i];
                    if (predicate(candidate))
                    {
                        return candidate;
                    }
                }

                throw new InvalidOperationException("Sequence contains no matching element.");
            }
#endif

            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var candidate = enumerator.Current;
                    if (predicate(candidate))
                    {
                        return candidate;
                    }
                }
            }

            throw new InvalidOperationException("Sequence contains no matching element.");
        }

        /// <summary>Determines whether the sequence contains <paramref name="value"/>.</summary>
        /// <typeparam name="TSource">The value enumerable being searched.</typeparam>
        /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to search.</param>
        /// <param name="value">The value to look for.</param>
        /// <returns><see langword="true"/> as soon as the value is found.</returns>
        /// <remarks>
        /// The comparison uses <see cref="EqualityComparer{T}.Default"/>, which is what
        /// <c>System.Linq.Enumerable.Contains</c> uses; the span path below is a hand-written loop
        /// for that reason and not a call to the span search helpers, whose comparison rules are
        /// narrower.
        /// </remarks>
        internal static bool Contains<TSource, TEnumerator, T>(TSource source, T value)
            where TSource : struct, IValueEnumerable<T, TEnumerator>, IValueEnumerableHooks<T>
            where TEnumerator : struct, IEnumerator<T>
        {
            var comparer = EqualityComparer<T>.Default;

#if NETCOREAPP3_0_OR_GREATER
            if (source.TryGetSpan(out var span))
            {
                for (var i = 0; i < span.Length; i++)
                {
                    if (comparer.Equals(span[i], value))
                    {
                        return true;
                    }
                }

                return false;
            }
#endif

            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (comparer.Equals(enumerator.Current, value))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Returns the element at <paramref name="index"/>.</summary>
        /// <typeparam name="TSource">The value enumerable being indexed.</typeparam>
        /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to index.</param>
        /// <param name="index">The zero-based index of the element to return.</param>
        /// <returns>The element at <paramref name="index"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative or
        /// past the end of the sequence.</exception>
        internal static T ElementAt<TSource, TEnumerator, T>(TSource source, int index)
            where TSource : struct, IValueEnumerable<T, TEnumerator>, IValueEnumerableHooks<T>
            where TEnumerator : struct, IEnumerator<T>
        {
#if NETCOREAPP3_0_OR_GREATER
            if (source.TryGetSpan(out var span))
            {
                if ((uint)index >= (uint)span.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return span[index];
            }
#endif

            if (index >= 0)
            {
                using (var enumerator = source.GetEnumerator())
                {
                    var position = 0;
                    while (enumerator.MoveNext())
                    {
                        if (position == index)
                        {
                            return enumerator.Current;
                        }

                        position++;
                    }
                }
            }

            throw new ArgumentOutOfRangeException(nameof(index));
        }

        /// <summary>Immediately runs <paramref name="action"/> on every element of the sequence.</summary>
        /// <typeparam name="TSource">The value enumerable being walked.</typeparam>
        /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to walk.</param>
        /// <param name="action">The action to run on each element.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// A terminal, not an operator: it returns nothing, and it walks the whole sequence even if
        /// the action throws only on the last element's behalf. The span path exists because the
        /// action is the whole cost of the walk - skipping the enumerator's own bookkeeping is worth
        /// having when the source is already contiguous.
        /// </remarks>
        internal static void ForEach<TSource, TEnumerator, T>(TSource source, Action<T> action)
            where TSource : struct, IValueEnumerable<T, TEnumerator>, IValueEnumerableHooks<T>
            where TEnumerator : struct, IEnumerator<T>
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

#if NETCOREAPP3_0_OR_GREATER
            if (source.TryGetSpan(out var span))
            {
                for (var i = 0; i < span.Length; i++)
                {
                    action(span[i]);
                }

                return;
            }
#endif

            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    action(enumerator.Current);
                }
            }
        }

        /// <summary>Immediately runs <paramref name="action"/> on every element, passing the
        /// zero-based position as well.</summary>
        /// <typeparam name="TSource">The value enumerable being walked.</typeparam>
        /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to walk.</param>
        /// <param name="action">The action to run on each element; its second argument is the
        /// zero-based position of the element.</param>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// The counter is deliberately not wrapped in <see langword="checked"/>: <c>ForEach</c> hands
        /// the position to a caller's action rather than producing an indexed element, so there is no
        /// sequence whose indices have to stay valid and no <see cref="OverflowException"/> contract
        /// to keep. Use <c>Index</c> when the position has to become part of the sequence.
        /// </remarks>
        internal static void ForEachIndexed<TSource, TEnumerator, T>(TSource source, Action<T, int> action)
            where TSource : struct, IValueEnumerable<T, TEnumerator>, IValueEnumerableHooks<T>
            where TEnumerator : struct, IEnumerator<T>
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

#if NETCOREAPP3_0_OR_GREATER
            if (source.TryGetSpan(out var span))
            {
                for (var i = 0; i < span.Length; i++)
                {
                    action(span[i], i);
                }

                return;
            }
#endif

            using (var enumerator = source.GetEnumerator())
            {
                var index = 0;
                while (enumerator.MoveNext())
                {
                    action(enumerator.Current, index);
                    index++;
                }
            }
        }

        /// <summary>Copies the whole sequence into a buffer that is rented from the shared pool.</summary>
        /// <typeparam name="TSource">The value enumerable being copied.</typeparam>
        /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to copy.</param>
        /// <returns>A pooled list owning the copy; the caller disposes it.</returns>
        internal static PooledList<T> Materialize<TSource, TEnumerator, T>(TSource source)
            where TSource : struct, IValueEnumerable<T, TEnumerator>, IValueEnumerableHooks<T>
            where TEnumerator : struct, IEnumerator<T>
        {
            if (source.TryGetNonEnumeratedCount(out var count) && count > 0)
            {
                // The count hook is exact whenever it answers, so the buffer is rented once at the
                // final size and never has to grow.
                var sized = new PooledList<T>(count);

#if NETCOREAPP3_0_OR_GREATER
                if (source.TryGetSpan(out var span))
                {
                    sized.AddRange(span);
                    return sized;
                }
#endif

                using (var enumerator = source.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        sized.Add(enumerator.Current);
                    }
                }

                return sized;
            }

            var list = new PooledList<T>();
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    list.Add(enumerator.Current);
                }
            }

            return list;
        }

        /// <summary>
        /// Copies the whole sequence into an array rented from the shared pool. The caller must
        /// return the array with <see cref="ArrayPool{T}.Return(T[],bool)"/> when it is done with it.
        /// </summary>
        /// <typeparam name="TSource">The value enumerable being copied.</typeparam>
        /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to copy.</param>
        /// <param name="length">The number of elements actually copied; the returned array may be
        /// longer, because a rented array is at least the requested size.</param>
        /// <returns>A pooled array whose first <paramref name="length"/> elements are the sequence.</returns>
        internal static T[] MaterializeArray<TSource, TEnumerator, T>(TSource source, out int length)
            where TSource : struct, IValueEnumerable<T, TEnumerator>, IValueEnumerableHooks<T>
            where TEnumerator : struct, IEnumerator<T>
        {
            if (source.TryGetNonEnumeratedCount(out var count))
            {
                // Rent at least one element: a zero-length request is not a pool operation, and a
                // uniform "always pooled" answer is easier for a caller to reason about.
                var buffer = ArrayPool<T>.Shared.Rent(count < 1 ? 1 : count);

#if NETCOREAPP3_0_OR_GREATER
                if (source.TryCopyTo(buffer, 0))
                {
                    length = count;
                    return buffer;
                }
#endif

                var written = 0;
                var faithful = true;
                using (var enumerator = source.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (written == count)
                        {
                            // The source lied about its length, so the buffer cannot hold it.
                            faithful = false;
                            break;
                        }

                        buffer[written] = enumerator.Current;
                        written++;
                    }
                }

                if (faithful)
                {
                    length = written;
                    return buffer;
                }

                ArrayPool<T>.Shared.Return(buffer, true);
            }

            // Unknown length: grow into a pooled list, then rent once more at the final size.
            using (var list = Materialize<TSource, TEnumerator, T>(source))
            {
                length = list.Count;
                var result = ArrayPool<T>.Shared.Rent(length < 1 ? 1 : length);
                var span = list.AsSpan();
                for (var i = 0; i < span.Length; i++)
                {
                    result[i] = span[i];
                }

                return result;
            }
        }
    }
}
