using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The <c>DistinctUntilChanged</c> operator: removes consecutive duplicate elements
    /// (run-length encoding semantics). An element is suppressed when it compares equal to
    /// the one immediately before it; a change of value restarts the output.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The operator is not in the BCL; it exists in Ix.NET and MoreLINQ. The engine's own
    /// implementation is the only one that runs on every target framework, so there is no
    /// high-TFM BCL fallback.
    /// </para>
    /// </remarks>
    public readonly struct DistinctUntilChangedValueEnumerable<TSource, TEnumerator, T>
        : IValueEnumerable<T, DistinctUntilChangedValueEnumerator<TEnumerator, T>>,
          IValueEnumerableHooks<T>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly TSource _source;
        private readonly IEqualityComparer<T>? _comparer;

        internal DistinctUntilChangedValueEnumerable(TSource source, IEqualityComparer<T>? comparer)
        {
            _source = source;
            _comparer = comparer;
        }

        /// <summary>Returns a fresh enumerator that skips consecutive duplicates.</summary>
        /// <returns>An enumerator positioned before the first non-duplicate element.</returns>
        public DistinctUntilChangedValueEnumerator<TEnumerator, T> GetEnumerator()
            => new DistinctUntilChangedValueEnumerator<TEnumerator, T>(_source.GetEnumerator(), _comparer);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool IValueEnumerableHooks<T>.TryGetNonEnumeratedCount(out int count)
        {
            count = 0;
            return false;
        }

#if NETCOREAPP3_0_OR_GREATER
        bool IValueEnumerableHooks<T>.TryGetSpan(out ReadOnlySpan<T> span)
        {
            span = default;
            return false;
        }

        bool IValueEnumerableHooks<T>.TryCopyTo(Span<T> destination, int offset) => false;
#endif
    }

    /// <summary>
    /// Walks the source, remembers the previous element and suppresses the current one when it
    /// compares equal to it. The first element is always produced.
    /// </summary>
    public struct DistinctUntilChangedValueEnumerator<TEnumerator, T> : IEnumerator<T>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private readonly IEqualityComparer<T> _comparer;
        private T _previous;
        private bool _hasPrevious;
        private T _current;

        internal DistinctUntilChangedValueEnumerator(TEnumerator enumerator, IEqualityComparer<T>? comparer)
        {
            _enumerator = enumerator;
            _comparer = comparer ?? EqualityComparer<T>.Default;
            _previous = default!;
            _hasPrevious = false;
            _current = default!;
        }

        /// <summary>Gets the current element.</summary>
        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        object IEnumerator.Current => _current!;

        /// <summary>Advances to the next non-duplicate element.</summary>
        /// <returns><see langword="true"/> if a non-duplicate element was found.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (_enumerator.MoveNext())
            {
                var candidate = _enumerator.Current;
                if (!_hasPrevious || !_comparer.Equals(_previous, candidate))
                {
                    _previous = candidate;
                    _hasPrevious = true;
                    _current = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Sets the enumerator to its initial position, before the first element.</summary>
        public void Reset()
        {
            _enumerator.Reset();
            _previous = default!;
            _hasPrevious = false;
            _current = default!;
        }

        /// <summary>Releases the source enumerator.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _enumerator.Dispose();
    }
}
