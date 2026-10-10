using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The <c>Pairwise</c> operator. It applies a selector to each element together with its
    /// predecessor, which is the shape a "compare each element with the one before it" pass needs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The first element has no predecessor, so it is only used as the predecessor of the second:
    /// a source of <c>n</c> elements yields <c>n - 1</c> results, a source of one element yields
    /// nothing, and an empty source yields nothing. No element is buffered, so the memory the
    /// operator adds does not grow with the sequence.
    /// </para>
    /// <para>
    /// Each source element is read exactly once: <see cref="IEnumerator{T}.Current"/> is a property
    /// whose cost is the source's business, and reading it twice to use the same element as both
    /// the "current" and the next iteration's "previous" would double that cost for no reason. The
    /// element is read into a local and both roles are served from there.
    /// </para>
    /// </remarks>
    /// <typeparam name="TSource">The value enumerable being paired.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="T">The type of the source elements.</typeparam>
    /// <typeparam name="TResult">The type of the projected elements.</typeparam>
    public readonly struct PairwiseValueEnumerable<TSource, TEnumerator, T, TResult> : IValueEnumerable<TResult, PairwiseValueEnumerator<TEnumerator, T, TResult>>, IValueEnumerableHooks<TResult>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly TSource _source;
        private readonly Func<T, T, TResult> _resultSelector;

        internal PairwiseValueEnumerable(TSource source, Func<T, T, TResult> resultSelector)
        {
            _source = source;
            _resultSelector = resultSelector ?? throw new ArgumentNullException(nameof(resultSelector));
        }

        /// <summary>Returns a fresh enumerator that pairs each element with its predecessor.</summary>
        /// <returns>An enumerator of pairs, positioned before the first one.</returns>
        public PairwiseValueEnumerator<TEnumerator, T, TResult> GetEnumerator()
            => new PairwiseValueEnumerator<TEnumerator, T, TResult>(_source.GetEnumerator(), _resultSelector);

        IEnumerator<TResult> IEnumerable<TResult>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // One result fewer than the source has elements, and nothing is stored, so there is no count
        // or storage to promise up front.
        bool IValueEnumerableHooks<TResult>.TryGetNonEnumeratedCount(out int count)
        {
            count = 0;
            return false;
        }

#if NETCOREAPP3_0_OR_GREATER
        bool IValueEnumerableHooks<TResult>.TryGetSpan(out ReadOnlySpan<TResult> span)
        {
            span = default;
            return false;
        }

        bool IValueEnumerableHooks<TResult>.TryCopyTo(Span<TResult> destination, int offset) => false;
#endif
    }

    /// <summary>
    /// Carries the element that will act as the next predecessor and the projection of the pair
    /// that has just been completed.
    /// </summary>
    /// <typeparam name="TEnumerator">The value-type enumerator of the source.</typeparam>
    /// <typeparam name="T">The type of the source elements.</typeparam>
    /// <typeparam name="TResult">The type of the projected elements.</typeparam>
    public struct PairwiseValueEnumerator<TEnumerator, T, TResult> : IEnumerator<TResult>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private readonly Func<T, T, TResult> _resultSelector;
        private bool _started;
        private T _previous;
        private TResult _current;

        internal PairwiseValueEnumerator(TEnumerator enumerator, Func<T, T, TResult> resultSelector)
        {
            _enumerator = enumerator;
            _resultSelector = resultSelector;
            _started = false;
            _previous = default!;
            _current = default!;
        }

        /// <summary>Gets the current projected element.</summary>
        public TResult Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        // TResult is unconstrained, so the element is legitimately null-able whenever TResult is a
        // reference type; the non-generic IEnumerator.Current simply forwards it.
        object IEnumerator.Current => _current!;

        /// <summary>Advances to the next pair.</summary>
        /// <returns><see langword="true"/> if a pair was produced; otherwise
        /// <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (!_started)
            {
                _started = true;
                if (!_enumerator.MoveNext())
                {
                    return false;
                }

                // The first element is only ever a predecessor: it produces no pair of its own.
                _previous = _enumerator.Current;
            }

            if (_enumerator.MoveNext())
            {
                var element = _enumerator.Current;
                _current = _resultSelector(_previous, element);
                _previous = element;
                return true;
            }

            return false;
        }

        /// <summary>Sets the enumerator to its initial position, before the first pair.</summary>
        public void Reset()
        {
            _enumerator.Reset();
            _started = false;
            _previous = default!;
            _current = default!;
        }

        /// <summary>Releases the source enumerator.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _enumerator.Dispose();
    }
}
