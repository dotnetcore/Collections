using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The <c>Window</c> operator of the P2 layer. It slides a fixed-size window over the source
    /// and yields one array per window, so a consumer that needs "the last <c>size</c> elements at
    /// every position" does not have to rebuild the window itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only complete windows are produced: a source of five elements with <c>size</c> three yields
    /// three windows, not five. The partial tail is dropped rather than padded, which is what
    /// <see cref="WindowLeftValueEnumerable{TSource,TEnumerator,T}"/> exists for.
    /// </para>
    /// <para>
    /// Every window is a fresh array. The array is handed to the consumer, so the buffer that the
    /// enumerator shifts cannot be the array that was yielded; a consumer that keeps the windows it
    /// has seen must not observe later shifts through them.
    /// </para>
    /// </remarks>
    /// <typeparam name="TSource">The value enumerable being windowed.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public readonly struct WindowValueEnumerable<TSource, TEnumerator, T> : IValueEnumerable<T[], WindowValueEnumerator<TEnumerator, T>>, IValueEnumerableHooks<T[]>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly TSource _source;
        private readonly int _size;

        internal WindowValueEnumerable(TSource source, int size)
        {
            if (size < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            _source = source;
            _size = size;
        }

        /// <summary>Returns a fresh enumerator that slides the window over the source.</summary>
        /// <returns>An enumerator positioned before the first window.</returns>
        public WindowValueEnumerator<TEnumerator, T> GetEnumerator()
            => new WindowValueEnumerator<TEnumerator, T>(_source.GetEnumerator(), _size);

        IEnumerator<T[]> IEnumerable<T[]>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // The window count depends on the element count, which the wrapper cannot know without
        // walking the source; nothing is promised up front.
        bool IValueEnumerableHooks<T[]>.TryGetNonEnumeratedCount(out int count)
        {
            count = 0;
            return false;
        }

#if NETCOREAPP3_0_OR_GREATER
        bool IValueEnumerableHooks<T[]>.TryGetSpan(out ReadOnlySpan<T[]> span)
        {
            span = default;
            return false;
        }

        bool IValueEnumerableHooks<T[]>.TryCopyTo(Span<T[]> destination, int offset) => false;
#endif
    }

    /// <summary>
    /// Shifts a buffer of <c>size</c> elements one position per step and yields a copy of it. The
    /// first window costs <c>size</c> reads; every later one costs a single read plus the shift.
    /// </summary>
    /// <typeparam name="TEnumerator">The value-type enumerator of the source.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public struct WindowValueEnumerator<TEnumerator, T> : IEnumerator<T[]>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private readonly int _size;
        private T[]? _buffer;
        private T[]? _current;
        private bool _started;
        private bool _done;

        internal WindowValueEnumerator(TEnumerator enumerator, int size)
        {
            _enumerator = enumerator;
            _size = size;
            _buffer = null;
            _current = null;
            _started = false;
            _done = false;
        }

        /// <summary>Gets the current window.</summary>
        public T[] Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current!;
        }

        object IEnumerator.Current => _current!;

        /// <summary>Advances to the next complete window.</summary>
        /// <returns><see langword="true"/> if a window was produced; otherwise
        /// <see langword="false"/>.</returns>
        public bool MoveNext()
        {
            if (_done)
            {
                return false;
            }

            if (!_started)
            {
                _started = true;

                var buffer = new T[_size];
                var filled = 0;
                while (filled < _size && _enumerator.MoveNext())
                {
                    buffer[filled] = _enumerator.Current;
                    filled++;
                }

                // Fewer elements than the window size: there is no complete window at all, and no
                // later pull can create one either.
                if (filled < _size)
                {
                    _done = true;
                    return false;
                }

                _buffer = buffer;
                _current = (T[])buffer.Clone();
                return true;
            }

            var existing = _buffer!;

            // The source is exhausted exactly when there is no element to slide in, which is also
            // the point at which the last window has already been produced.
            if (!_enumerator.MoveNext())
            {
                _done = true;
                return false;
            }

            Array.Copy(existing, 1, existing, 0, _size - 1);
            existing[_size - 1] = _enumerator.Current;
            _current = (T[])existing.Clone();
            return true;
        }

        /// <summary>Sets the enumerator to its initial position, before the first window.</summary>
        public void Reset()
        {
            _enumerator.Reset();
            _buffer = null;
            _current = null;
            _started = false;
            _done = false;
        }

        /// <summary>Releases the source enumerator.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _enumerator.Dispose();
    }

    /// <summary>
    /// The <c>WindowLeft</c> operator of the P2 layer. It is the left-aligned counterpart of
    /// <c>Window</c>: every source element starts a window, and a window that runs past the end of
    /// the source is padded on the right with <see langword="default"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The difference from <c>Window</c> is exactly the tail. Five elements with <c>size</c> three
    /// give three complete windows under <c>Window</c> and five windows under <c>WindowLeft</c>, the
    /// last two of which are <c>[4, 5, default]</c> and <c>[5, default, default]</c>.
    /// </para>
    /// <para>
    /// The name says "left" because the data is anchored to the left edge of the window: the first
    /// element of every window is a real element as long as the window starts inside the source.
    /// </para>
    /// </remarks>
    /// <typeparam name="TSource">The value enumerable being windowed.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public readonly struct WindowLeftValueEnumerable<TSource, TEnumerator, T> : IValueEnumerable<T[], WindowLeftValueEnumerator<TEnumerator, T>>, IValueEnumerableHooks<T[]>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly TSource _source;
        private readonly int _size;

        internal WindowLeftValueEnumerable(TSource source, int size)
        {
            if (size < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            _source = source;
            _size = size;
        }

        /// <summary>Returns a fresh enumerator that slides the left-aligned window over the source.</summary>
        /// <returns>An enumerator positioned before the first window.</returns>
        public WindowLeftValueEnumerator<TEnumerator, T> GetEnumerator()
            => new WindowLeftValueEnumerator<TEnumerator, T>(_source.GetEnumerator(), _size);

        IEnumerator<T[]> IEnumerable<T[]>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool IValueEnumerableHooks<T[]>.TryGetNonEnumeratedCount(out int count)
        {
            count = 0;
            return false;
        }

#if NETCOREAPP3_0_OR_GREATER
        bool IValueEnumerableHooks<T[]>.TryGetSpan(out ReadOnlySpan<T[]> span)
        {
            span = default;
            return false;
        }

        bool IValueEnumerableHooks<T[]>.TryCopyTo(Span<T[]> destination, int offset) => false;
#endif
    }

    /// <summary>
    /// Produces one window per source element. A ring buffer of <c>size</c> slots holds the elements
    /// read so far, and each step copies the window in source order, substituting
    /// <see langword="default"/> for the positions past the end of the source.
    /// </summary>
    /// <typeparam name="TEnumerator">The value-type enumerator of the source.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public struct WindowLeftValueEnumerator<TEnumerator, T> : IEnumerator<T[]>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private readonly int _size;
        private readonly T[] _buffer;
        private T[]? _current;
        private int _read;
        private int _next;
        private bool _started;

        internal WindowLeftValueEnumerator(TEnumerator enumerator, int size)
        {
            _enumerator = enumerator;
            _size = size;
            _buffer = new T[size];
            _current = null;
            _read = 0;
            _next = 0;
            _started = false;
        }

        /// <summary>Gets the current window.</summary>
        public T[] Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current!;
        }

        object IEnumerator.Current => _current!;

        /// <summary>Advances to the next window, which starts at the next source element.</summary>
        /// <returns><see langword="true"/> if a window was produced; otherwise
        /// <see langword="false"/>.</returns>
        public bool MoveNext()
        {
            if (!_started)
            {
                _started = true;
                Fill();
            }

            // A window starts at every element that was read, so the sequence ends exactly when the
            // next start position has run past the last element.
            if (_next >= _read)
            {
                return false;
            }

            var window = new T[_size];
            for (var i = 0; i < _size; i++)
            {
                var position = _next + i;
                window[i] = position < _read ? _buffer[position % _size] : default!;
            }

            _current = window;
            _next++;
            Fill();
            return true;
        }

        // Reads ahead until the ring buffer holds the whole window that starts at _next, or until the
        // source is exhausted.
        private void Fill()
        {
            while (_read < _next + _size && _enumerator.MoveNext())
            {
                _buffer[_read % _size] = _enumerator.Current;
                _read++;
            }
        }

        /// <summary>Sets the enumerator to its initial position, before the first window.</summary>
        public void Reset()
        {
            _enumerator.Reset();
            for (var i = 0; i < _buffer.Length; i++)
            {
                _buffer[i] = default!;
            }

            _current = null;
            _read = 0;
            _next = 0;
            _started = false;
        }

        /// <summary>Releases the source enumerator.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _enumerator.Dispose();
    }
}
