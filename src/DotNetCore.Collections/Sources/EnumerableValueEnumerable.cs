using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DotNetCore.Collections
{
    /// <summary>
    /// A value-type enumerable over an arbitrary <see cref="IEnumerable{T}"/>. This is the
    /// fallback arm of the four-source dispatch: the source type is unknown, so the underlying
    /// enumerator cannot be produced without a heap allocation, but the wrapper itself and the
    /// walk over it still avoid any additional one.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the sequence.</typeparam>
    public readonly struct EnumerableValueEnumerable<T> : IValueEnumerable<T, EnumerableValueEnumerator<T>>
    {
        private readonly IEnumerable<T> _source;

        /// <summary>Wraps <paramref name="source"/> without copying it.</summary>
        /// <param name="source">The sequence to enumerate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public EnumerableValueEnumerable(IEnumerable<T> source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>Returns a fresh value-type enumerator over the sequence.</summary>
        /// <returns>An enumerator positioned before the first element.</returns>
        public EnumerableValueEnumerator<T> GetEnumerator() => new EnumerableValueEnumerator<T>(_source);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Wraps the enumerator of an arbitrary <see cref="IEnumerable{T}"/>. The walk delegates to
    /// that enumerator, and <see cref="Dispose"/> forwards to it so an iterator-based source
    /// releases its <see langword="finally"/> blocks exactly as it would under a plain
    /// <see langword="foreach"/>.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the sequence.</typeparam>
    public struct EnumerableValueEnumerator<T> : IEnumerator<T>
    {
        private readonly IEnumerator<T> _enumerator;

        /// <summary>Creates an enumerator over <paramref name="source"/> by taking its own
        /// enumerator. That call is the one allocation this arm cannot avoid.</summary>
        /// <param name="source">The sequence to enumerate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public EnumerableValueEnumerator(IEnumerable<T> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _enumerator = source.GetEnumerator();
        }

        /// <summary>Gets the element at the current position.</summary>
        public T Current => _enumerator.Current;

        // T is unconstrained, so the element is legitimately null-able whenever T is a
        // reference type; the non-generic IEnumerator.Current simply forwards it.
        object IEnumerator.Current => _enumerator.Current!;

        /// <summary>Advances the enumerator to the next element of the sequence.</summary>
        /// <returns><see langword="true"/> if the enumerator was advanced to an element;
        /// <see langword="false"/> if it passed the end of the sequence.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => _enumerator.MoveNext();

        /// <summary>Sets the enumerator to its initial position, if the underlying enumerator
        /// supports it.</summary>
        /// <exception cref="NotSupportedException">The underlying enumerator does not support
        /// being reset.</exception>
        public void Reset() => _enumerator.Reset();

        /// <summary>Releases the underlying enumerator.</summary>
        public void Dispose() => _enumerator.Dispose();
    }
}
