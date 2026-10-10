using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// A value-type enumerable over a <see cref="List{T}"/>. The list is captured by reference,
    /// so creating the wrapper copies nothing.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the list.</typeparam>
    public readonly struct ListValueEnumerable<T> : IValueEnumerable<T, ListValueEnumerator<T>>
    {
        private readonly List<T> _source;

        /// <summary>Wraps <paramref name="source"/> without copying it.</summary>
        /// <param name="source">The list to enumerate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public ListValueEnumerable(List<T> source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>Returns a fresh value-type enumerator over the list.</summary>
        /// <returns>An enumerator positioned before the first element.</returns>
        public ListValueEnumerator<T> GetEnumerator() => new ListValueEnumerator<T>(_source);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Walks a <see cref="List{T}"/> through its backing array and allocates nothing while doing
    /// so. The backing array is reached with the field layout of <see cref="List{T}"/> rather than
    /// with the list indexer, which skips both the bounds check performed by the indexer's slow
    /// path and the interface call that an <see cref="IList{T}"/>-typed source would otherwise cost.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the list.</typeparam>
    public struct ListValueEnumerator<T> : IEnumerator<T>
    {
        private readonly T[] _items;
        private readonly int _count;
        private int _index;

        /// <summary>Creates an enumerator positioned before the first element.</summary>
        /// <param name="source">The list to enumerate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public ListValueEnumerator(List<T> source)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            // The backing array is longer than the live element count whenever the list has
            // spare capacity, so the walk below is bounded by _count, never by _items.Length.
            _items = ListLayoutAccessor.GetItems(source);
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

                return _items[index];
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

        /// <summary>Does nothing: a list enumerator owns no resources.</summary>
        public void Dispose()
        {
        }
    }
}
