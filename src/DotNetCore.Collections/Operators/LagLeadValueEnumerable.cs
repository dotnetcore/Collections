using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The <c>Lag</c> operator of the P2 layer. It pairs every element with the element
    /// <c>offset</c> positions before it, the way the SQL window function of the same name does.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The first <c>offset</c> elements have no predecessor, so the second argument of the result
    /// selector is <see langword="default"/> for them. That is the SQL semantics: <c>LAG</c> over the
    /// first row of a partition answers <c>NULL</c>, it does not skip the row.
    /// </para>
    /// <para>
    /// The operator keeps a ring buffer of <c>offset</c> elements, so the memory it holds does not
    /// grow with the source. <c>Lag</c> is the cheap direction: it only has to remember the past.
    /// </para>
    /// </remarks>
    /// <typeparam name="TSource">The value enumerable being read.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <typeparam name="TResult">The type of the projected pairs.</typeparam>
    public readonly struct LagValueEnumerable<TSource, TEnumerator, T, TResult> : IValueEnumerable<TResult, LagValueEnumerator<TEnumerator, T, TResult>>, IValueEnumerableHooks<TResult>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly TSource _source;
        private readonly int _offset;
        private readonly Func<T, T, TResult> _resultSelector;

        internal LagValueEnumerable(TSource source, int offset, Func<T, T, TResult> resultSelector)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            _source = source;
            _offset = offset;
            _resultSelector = resultSelector ?? throw new ArgumentNullException(nameof(resultSelector));
        }

        /// <summary>Returns a fresh enumerator that pairs each element with its predecessor.</summary>
        /// <returns>An enumerator positioned before the first pair.</returns>
        public LagValueEnumerator<TEnumerator, T, TResult> GetEnumerator()
            => new LagValueEnumerator<TEnumerator, T, TResult>(_source.GetEnumerator(), _offset, _resultSelector);

        IEnumerator<TResult> IEnumerable<TResult>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // One result per element, so the count is the source count - but only a source that knows
        // its own count could answer without walking, and the wrapper does not ask it here.
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
    /// Walks the source once, keeping the last <c>offset</c> elements in a ring buffer. Position
    /// <c>i</c> reads back position <c>i - offset</c>, which is still in the buffer because the
    /// buffer holds exactly <c>offset</c> slots.
    /// </summary>
    /// <typeparam name="TEnumerator">The value-type enumerator of the source.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <typeparam name="TResult">The type of the projected pairs.</typeparam>
    public struct LagValueEnumerator<TEnumerator, T, TResult> : IEnumerator<TResult>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private readonly int _offset;
        private readonly Func<T, T, TResult> _resultSelector;
        private readonly T[] _buffer;
        private TResult _current;
        private int _count;

        // net451 has no Array.Empty<T>() (it arrived in .NET 4.6); an offset of zero needs no
        // buffer at all, so it shares one empty array across instances instead of allocating.
        private static readonly T[] EmptyBuffer = new T[0];

        internal LagValueEnumerator(TEnumerator enumerator, int offset, Func<T, T, TResult> resultSelector)
        {
            _enumerator = enumerator;
            _offset = offset;
            _resultSelector = resultSelector;
            _buffer = offset == 0 ? EmptyBuffer : new T[offset];
            _current = default!;
            _count = 0;
        }

        /// <summary>Gets the current pair.</summary>
        public TResult Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        object IEnumerator.Current => _current!;

        /// <summary>Advances to the next element and pairs it with its predecessor.</summary>
        /// <returns><see langword="true"/> if a pair was produced; otherwise
        /// <see langword="false"/>.</returns>
        public bool MoveNext()
        {
            if (!_enumerator.MoveNext())
            {
                return false;
            }

            var current = _enumerator.Current;

            // With no offset the predecessor is the element itself; that is what LAG(x, 0) means.
            if (_offset == 0)
            {
                _current = _resultSelector(current, current);
                _count++;
                return true;
            }

            var slot = _count % _offset;
            var lagged = _count < _offset ? default! : _buffer[slot];
            _buffer[slot] = current;
            _count++;

            _current = _resultSelector(current, lagged);
            return true;
        }

        /// <summary>Sets the enumerator to its initial position, before the first pair.</summary>
        public void Reset()
        {
            _enumerator.Reset();
            for (var i = 0; i < _buffer.Length; i++)
            {
                _buffer[i] = default!;
            }

            _current = default!;
            _count = 0;
        }

        /// <summary>Releases the source enumerator.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _enumerator.Dispose();
    }

    /// <summary>
    /// The <c>Lead</c> operator of the P2 layer. It pairs every element with the element
    /// <c>offset</c> positions after it, the mirror image of <c>Lag</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The last <c>offset</c> elements have no successor, so the second argument of the result
    /// selector is <see langword="default"/> for them - again the SQL semantics rather than a
    /// truncation.
    /// </para>
    /// <para>
    /// Unlike <c>Lag</c>, this direction has to look ahead, so the enumerator keeps a window of
    /// <c>offset + 1</c> elements. The source is pulled only as far as the lookahead requires.
    /// </para>
    /// </remarks>
    /// <typeparam name="TSource">The value enumerable being read.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <typeparam name="TResult">The type of the projected pairs.</typeparam>
    public readonly struct LeadValueEnumerable<TSource, TEnumerator, T, TResult> : IValueEnumerable<TResult, LeadValueEnumerator<TEnumerator, T, TResult>>, IValueEnumerableHooks<TResult>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly TSource _source;
        private readonly int _offset;
        private readonly Func<T, T, TResult> _resultSelector;

        internal LeadValueEnumerable(TSource source, int offset, Func<T, T, TResult> resultSelector)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            _source = source;
            _offset = offset;
            _resultSelector = resultSelector ?? throw new ArgumentNullException(nameof(resultSelector));
        }

        /// <summary>Returns a fresh enumerator that pairs each element with its successor.</summary>
        /// <returns>An enumerator positioned before the first pair.</returns>
        public LeadValueEnumerator<TEnumerator, T, TResult> GetEnumerator()
            => new LeadValueEnumerator<TEnumerator, T, TResult>(_source.GetEnumerator(), _offset, _resultSelector);

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
    /// Keeps a sliding window of <c>offset + 1</c> elements. The head is the current element and
    /// the element at index <c>offset</c> is its successor; the window is refilled from the source
    /// one element per step until the source runs dry.
    /// </summary>
    /// <typeparam name="TEnumerator">The value-type enumerator of the source.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <typeparam name="TResult">The type of the projected pairs.</typeparam>
    public struct LeadValueEnumerator<TEnumerator, T, TResult> : IEnumerator<TResult>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private readonly int _offset;
        private readonly Func<T, T, TResult> _resultSelector;
        private readonly List<T> _window;
        private readonly int _windowSize;
        private TResult _current;
        private bool _started;

        internal LeadValueEnumerator(TEnumerator enumerator, int offset, Func<T, T, TResult> resultSelector)
        {
            _enumerator = enumerator;
            _offset = offset;
            _resultSelector = resultSelector;
            _windowSize = offset + 1;
            _window = new List<T>(_windowSize);
            _current = default!;
            _started = false;
        }

        /// <summary>Gets the current pair.</summary>
        public TResult Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        object IEnumerator.Current => _current!;

        /// <summary>Advances to the next element and pairs it with its successor.</summary>
        /// <returns><see langword="true"/> if a pair was produced; otherwise
        /// <see langword="false"/>.</returns>
        public bool MoveNext()
        {
            if (!_started)
            {
                _started = true;
                while (_window.Count < _windowSize && _enumerator.MoveNext())
                {
                    _window.Add(_enumerator.Current);
                }
            }

            if (_window.Count == 0)
            {
                return false;
            }

            var current = _window[0];

            // The successor is the element exactly offset positions ahead, which is the window slot
            // at that index; when the window has already run past the end of the source it is absent.
            var lead = _window.Count > _offset ? _window[_offset] : default!;

            if (_enumerator.MoveNext())
            {
                _window.Add(_enumerator.Current);
            }

            _window.RemoveAt(0);

            _current = _resultSelector(current, lead);
            return true;
        }

        /// <summary>Sets the enumerator to its initial position, before the first pair.</summary>
        public void Reset()
        {
            _enumerator.Reset();
            _window.Clear();
            _current = default!;
            _started = false;
        }

        /// <summary>Releases the source enumerator.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _enumerator.Dispose();
    }
}
