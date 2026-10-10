using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// A value-type enumerable over a one-dimensional array. The array is captured by
    /// reference, so creating the wrapper copies nothing.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the array.</typeparam>
    public readonly struct ArrayValueEnumerable<T> : IValueEnumerable<T, ArrayValueEnumerator<T>>, IValueEnumerableHooks<T>
    {
        private readonly T[] _source;

        /// <summary>Wraps <paramref name="source"/> without copying it.</summary>
        /// <param name="source">The array to enumerate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public ArrayValueEnumerable(T[] source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>Returns a fresh value-type enumerator over the array.</summary>
        /// <returns>An enumerator positioned before the first element.</returns>
        public ArrayValueEnumerator<T> GetEnumerator() => new ArrayValueEnumerator<T>(_source);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // F7-03: an array answers every hook. Its length is the element count, it is already one
        // contiguous block, and that block can be copied in a single step.

        bool IValueEnumerableHooks<T>.TryGetNonEnumeratedCount(out int count)
        {
            count = _source.Length;
            return true;
        }

#if NETCOREAPP3_0_OR_GREATER
        bool IValueEnumerableHooks<T>.TryGetSpan(out ReadOnlySpan<T> span)
        {
            span = _source;
            return true;
        }

        bool IValueEnumerableHooks<T>.TryCopyTo(Span<T> destination, int offset)
        {
            var source = _source;
            if ((uint)offset > (uint)destination.Length)
            {
                return false;
            }

            if (destination.Length - offset < source.Length)
            {
                return false;
            }

            new ReadOnlySpan<T>(source).CopyTo(destination.Slice(offset));
            return true;
        }
#endif
    }

    /// <summary>
    /// Walks a one-dimensional array by index and allocates nothing while doing so.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the array.</typeparam>
    public struct ArrayValueEnumerator<T> : IEnumerator<T>
    {
        private readonly T[] _source;
        private int _index;

        /// <summary>Creates an enumerator positioned before the first element.</summary>
        /// <param name="source">The array to enumerate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public ArrayValueEnumerator(T[] source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _index = -1;
        }

        /// <summary>Gets the element at the current position.</summary>
        /// <exception cref="InvalidOperationException">The enumerator is positioned before the
        /// first element or after the last one.</exception>
        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var index = _index;
                if ((uint)index >= (uint)_source.Length)
                {
                    throw new InvalidOperationException("Enumeration has either not started or has already finished.");
                }

                return _source[index];
            }
        }

        // T is unconstrained, so Current is legitimately null-able whenever T is a reference
        // type; the non-generic IEnumerator.Current simply forwards it.
        object IEnumerator.Current => Current!;

        /// <summary>Advances the enumerator to the next element of the array.</summary>
        /// <returns><see langword="true"/> if the enumerator was advanced to an element;
        /// <see langword="false"/> if it passed the end of the array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var index = _index + 1;
            if ((uint)index < (uint)_source.Length)
            {
                _index = index;
                return true;
            }

            _index = _source.Length;
            return false;
        }

        /// <summary>Sets the enumerator to its initial position, before the first element.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => _index = -1;

        /// <summary>Does nothing: an array enumerator owns no resources. The call is kept so that a
        /// consumer's <see langword="foreach"/> can inline it away.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
        }
    }
}
