using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// A value-type enumerable over an <see cref="IReadOnlyList{T}"/>. The list is captured by
    /// reference, so creating the wrapper copies nothing.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the list.</typeparam>
    public readonly struct ReadOnlyListValueEnumerable<T> : IValueEnumerable<T, ReadOnlyListValueEnumerator<T>>, IValueEnumerableHooks<T>
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

        // F7-03: the interface states only that the sequence can be indexed, so the count hook is
        // always answerable while the two contiguous hooks are not - unless the object behind the
        // interface happens to be an array or a List<T>, which is the common case when a paging
        // caller hands over its own storage. That run-time probe is the cheap half of the
        // "virtual singleton dispatch" idea: it costs one type test and removes a per-element
        // interface call from every bulk path.

        bool IValueEnumerableHooks<T>.TryGetNonEnumeratedCount(out int count)
        {
            count = _source.Count;
            return true;
        }

#if NETCOREAPP3_0_OR_GREATER
        bool IValueEnumerableHooks<T>.TryGetSpan(out ReadOnlySpan<T> span) => TryGetSpanCore(out span);

        bool IValueEnumerableHooks<T>.TryCopyTo(Span<T> destination, int offset)
        {
            if (!TryGetSpanCore(out var span))
            {
                return false;
            }

            if ((uint)offset > (uint)destination.Length)
            {
                return false;
            }

            if (destination.Length - offset < span.Length)
            {
                return false;
            }

            span.CopyTo(destination.Slice(offset));
            return true;
        }

        // Reached directly by both hooks above; kept separate so the run-time probe runs once and
        // never goes through an interface reference (which would box this struct).
        private bool TryGetSpanCore(out ReadOnlySpan<T> span)
        {
            var source = _source;
            if (source is T[] array)
            {
                span = array;
                return true;
            }

            if (source is List<T> list)
            {
                span = ListLayoutAccessor.AsReadOnlySpan(list);
                return true;
            }

            span = default;
            return false;
        }
#endif
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => _index = -1;

        /// <summary>Does nothing: the enumerator owns no resources; the source does. The call is
        /// kept so that a consumer's <see langword="foreach"/> can inline it away.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
        }
    }
}
