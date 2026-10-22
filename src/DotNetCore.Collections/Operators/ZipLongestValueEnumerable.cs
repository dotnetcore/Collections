using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The <c>ZipLongest</c> operator of the P2 layer. It pairs two sequences position by position
    /// and runs until <em>both</em> are exhausted, substituting <see langword="default"/> for the
    /// shorter side.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The BCL's <c>Zip</c> truncates at the shorter sequence; this operator is what a consumer
    /// needs when the two sides are expected to line up but one of them may be short - aligning a
    /// header row with a data row, joining a sparse series against a dense one.
    /// </para>
    /// <para>
    /// The result length is the longer of the two lengths. Neither side is buffered, so a consumer
    /// that stops early never reads the rest of either sequence.
    /// </para>
    /// </remarks>
    /// <typeparam name="TFirst">The type of the elements of the value enumerable.</typeparam>
    /// <typeparam name="TFirstEnumerator">The value-type enumerator of the value enumerable.</typeparam>
    /// <typeparam name="TSecond">The type of the elements of the second sequence.</typeparam>
    /// <typeparam name="TResult">The type of the projected pairs.</typeparam>
    public readonly struct ZipLongestValueEnumerable<TFirst, TFirstEnumerator, TSecond, TResult> : IValueEnumerable<TResult, ZipLongestValueEnumerator<TFirst, TFirstEnumerator, TSecond, TResult>>, IValueEnumerableHooks<TResult>
        where TFirstEnumerator : struct, IEnumerator<TFirst>
    {
        private readonly TFirstEnumerator _first;
        private readonly IEnumerable<TSecond> _second;
        private readonly Func<TFirst, TSecond, TResult> _resultSelector;

        internal ZipLongestValueEnumerable(TFirstEnumerator first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
        {
            _first = first;
            _second = second ?? throw new ArgumentNullException(nameof(second));
            _resultSelector = resultSelector ?? throw new ArgumentNullException(nameof(resultSelector));
        }

        /// <summary>Returns a fresh enumerator that zips the two sides to the longer one.</summary>
        /// <returns>An enumerator positioned before the first pair.</returns>
        public ZipLongestValueEnumerator<TFirst, TFirstEnumerator, TSecond, TResult> GetEnumerator()
            => new ZipLongestValueEnumerator<TFirst, TFirstEnumerator, TSecond, TResult>(_first, _second.GetEnumerator(), _resultSelector);

        IEnumerator<TResult> IEnumerable<TResult>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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
    /// Advances both sides on every step and stops only when both have answered
    /// <see langword="false"/>. The side that has already ended contributes
    /// <see langword="default"/> and is not advanced again.
    /// </summary>
    /// <typeparam name="TFirst">The type of the elements of the first side.</typeparam>
    /// <typeparam name="TFirstEnumerator">The value-type enumerator of the first side.</typeparam>
    /// <typeparam name="TSecond">The type of the elements of the second side.</typeparam>
    /// <typeparam name="TResult">The type of the projected pairs.</typeparam>
    public struct ZipLongestValueEnumerator<TFirst, TFirstEnumerator, TSecond, TResult> : IEnumerator<TResult>
        where TFirstEnumerator : struct, IEnumerator<TFirst>
    {
        private TFirstEnumerator _first;
        private IEnumerator<TSecond> _second;
        private readonly Func<TFirst, TSecond, TResult> _resultSelector;
        private TResult _current;
        private bool _firstDone;
        private bool _secondDone;

        internal ZipLongestValueEnumerator(TFirstEnumerator first, IEnumerator<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
        {
            _first = first;
            _second = second;
            _resultSelector = resultSelector;
            _current = default!;
            _firstDone = false;
            _secondDone = false;
        }

        /// <summary>Gets the current pair.</summary>
        public TResult Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        object IEnumerator.Current => _current!;

        /// <summary>Advances both sides and pairs whatever each one produced.</summary>
        /// <returns><see langword="true"/> if a pair was produced; otherwise
        /// <see langword="false"/>.</returns>
        public bool MoveNext()
        {
            if (!_firstDone && !_first.MoveNext())
            {
                _firstDone = true;
            }

            if (!_secondDone && !_second.MoveNext())
            {
                _secondDone = true;
            }

            if (_firstDone && _secondDone)
            {
                return false;
            }

            var first = _firstDone ? default! : _first.Current;
            var second = _secondDone ? default! : _second.Current;
            _current = _resultSelector(first, second);
            return true;
        }

        /// <summary>Sets the enumerator to its initial position, before the first pair.</summary>
        public void Reset()
        {
            _first.Reset();
            _second.Reset();
            _current = default!;
            _firstDone = false;
            _secondDone = false;
        }

        /// <summary>Releases both source enumerators.</summary>
        public void Dispose()
        {
            _first.Dispose();
            _second.Dispose();
        }
    }
}
