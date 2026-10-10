using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// A value-type enumerable over an arbitrary <see cref="IEnumerable{T}"/>. This is the
    /// fallback arm of the four-source dispatch: the source type is unknown, so the underlying
    /// enumerator cannot be produced without a heap allocation, but the wrapper itself and the
    /// walk over it still avoid any additional one.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the sequence.</typeparam>
    public readonly struct EnumerableValueEnumerable<T> : IValueEnumerable<T, EnumerableValueEnumerator<T>>, IValueEnumerableHooks<T>
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

        // F7-03: the static type carries no shape information, but the object behind it usually
        // does. The hooks below probe the run-time type once per call, which is the cheap half of
        // the "virtual singleton dispatch" idea: an IEnumerable<T> that really is a T[] or a
        // List<T> still reaches the bulk paths, and only a genuinely lazy sequence walks.

        bool IValueEnumerableHooks<T>.TryGetNonEnumeratedCount(out int count)
        {
            if (_source is ICollection<T> collection)
            {
                count = collection.Count;
                return true;
            }

            // IReadOnlyCollection<T> is the other contract that promises an O(1) count. It catches
            // the collections that expose a count but no mutating interface - Queue<T> and
            // Stack<T> on the modern runtimes, for instance. On .NET Framework those two do not
            // implement it, so the answer genuinely differs between platforms: the hook reports
            // what the platform guarantees, and the walk is the fallback when it does not.
            if (_source is IReadOnlyCollection<T> readOnlyCollection)
            {
                count = readOnlyCollection.Count;
                return true;
            }

            count = 0;
            return false;
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => _enumerator.Reset();

        /// <summary>Releases the underlying enumerator.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _enumerator.Dispose();
    }
}
