using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DotNetCore.Collections
{
    /// <summary>
    /// A value-type enumerable over an <see cref="IReadOnlyList{T}"/>. The list is captured by
    /// reference, so creating the wrapper copies nothing.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the list.</typeparam>
    public readonly struct ReadOnlyListValueEnumerable<T> : IValueEnumerable<T, ReadOnlyListValueEnumerator<T>>
    {
        private readonly IReadOnlyList<T> _source;

        /// <summary>Wraps <paramref name="source"/> without copying it.</summary>
        /// <param name="source">The list to enumerate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public ReadOnlyListValueEnumerable(IReadOnlyList<T> source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>Returns a fresh value-type enumerator over the list.</summary>
        /// <returns>An enumerator positioned before the first element.</returns>
        public ReadOnlyListValueEnumerator<T> GetEnumerator() => new ReadOnlyListValueEnumerator<T>(_source);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Walks an <see cref="IReadOnlyList{T}"/> by index. When the static type of a source is only
    /// the interface, the indexer is reached through the interface, and the element count is read
    /// once when the enumerator is created rather than on every step.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the list.</typeparam>
    public struct ReadOnlyListValueEnumerator<T> : IEnumerator<T>
    {
        private readonly IReadOnlyList<T> _source;
        private readonly int _count;
        private int _index;

        /// <summary>Creates an enumerator positioned before the first element. The element count
        /// is captured here, so a structural change to the list after this point is not observed.</summary>
        /// <param name="source">The list to enumerate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public ReadOnlyListValueEnumerator(IReadOnlyList<T> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _source = source;
            _count = source.Count;
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
                if ((uint)index >= (uint)_count)
                {
                    throw new InvalidOperationException("Enumeration has either not started or has already finished.");
                }

                return _source[index];
            }
        }

        // T is unconstrained, so Current is legitimately null-able whenever T is a reference
        // type; the non-generic IEnumerator.Current simply forwards it.
        object IEnumerator.Current => Current!;

        /// <summary>Advances the enumerator to the next element of the list.</summary>
        /// <returns><see langword="true"/> if the enumerator was advanced to an element;
        /// <see langword="false"/> if it passed the end of the list.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var index = _index + 1;
            if ((uint)index < (uint)_count)
            {
                _index = index;
                return true;
            }

            _index = _count;
            return false;
        }

        /// <summary>Sets the enumerator to its initial position, before the first element.</summary>
        public void Reset() => _index = -1;

        /// <summary>Does nothing: the enumerator owns no resources; the source does.</summary>
        public void Dispose()
        {
        }
    }
}
