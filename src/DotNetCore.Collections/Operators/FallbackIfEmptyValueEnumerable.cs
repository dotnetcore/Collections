using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The <c>FallbackIfEmpty</c> operator: when the source is empty, yields a fallback sequence
    /// (or a single fallback value) instead. When the source has at least one element the fallback
    /// is never touched.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The operator is not in the BCL; <c>DefaultIfEmpty</c> fills only <c>default(T)</c>, whereas
    /// this operator accepts an arbitrary replacement. It exists in MoreLINQ.
    /// </para>
    /// </remarks>
    public readonly struct FallbackIfEmptyValueEnumerable<TSource, TEnumerator, T>
        : IValueEnumerable<T, FallbackIfEmptyValueEnumerator<TEnumerator, T>>,
          IValueEnumerableHooks<T>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly TSource _source;
        private readonly IEnumerable<T>? _fallback;
        private readonly T _singleFallback;
        private readonly bool _useSingle;

        internal FallbackIfEmptyValueEnumerable(TSource source, IEnumerable<T> fallback)
        {
            _source = source;
            _fallback = fallback;
            _singleFallback = default!;
            _useSingle = false;
        }

        internal FallbackIfEmptyValueEnumerable(TSource source, T fallback)
        {
            _source = source;
            _fallback = null;
            _singleFallback = fallback;
            _useSingle = true;
        }

        /// <summary>Returns a fresh enumerator that falls back when the source is empty.</summary>
        /// <returns>An enumerator positioned before the first element.</returns>
        public FallbackIfEmptyValueEnumerator<TEnumerator, T> GetEnumerator()
        {
            if (_useSingle)
            {
                return new FallbackIfEmptyValueEnumerator<TEnumerator, T>(
                    _source.GetEnumerator(), _singleFallback, null, true);
            }

            return new FallbackIfEmptyValueEnumerator<TEnumerator, T>(
                _source.GetEnumerator(), default!, _fallback, false);
        }

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
    /// Drains the source; if at least one element was produced, the enumerator is done. Otherwise
    /// it switches to the fallback (either a single value or an arbitrary sequence) and drains that.
    /// </summary>
    public struct FallbackIfEmptyValueEnumerator<TEnumerator, T> : IEnumerator<T>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _sourceEnumerator;
        private IEnumerator<T>? _fallbackEnumerator;
        private readonly T _singleFallback;
        private readonly bool _useSingle;
        private bool _sourceHadElements;
        private bool _onFallback;
        private T _current;

        internal FallbackIfEmptyValueEnumerator(
            TEnumerator sourceEnumerator,
            T singleFallback,
            IEnumerable<T>? fallbackSequence,
            bool useSingle)
        {
            _sourceEnumerator = sourceEnumerator;
            _singleFallback = singleFallback;
            _useSingle = useSingle;
            _fallbackEnumerator = fallbackSequence?.GetEnumerator();
            _sourceHadElements = false;
            _onFallback = false;
            _current = default!;
        }

        /// <summary>Gets the current element.</summary>
        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        object IEnumerator.Current => _current!;

        /// <summary>Advances to the next element.</summary>
        /// <returns><see langword="true"/> if an element was produced.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (!_onFallback)
            {
                if (_sourceEnumerator.MoveNext())
                {
                    _sourceHadElements = true;
                    _current = _sourceEnumerator.Current;
                    return true;
                }

                if (_sourceHadElements)
                {
                    return false;
                }

                _sourceEnumerator.Dispose();
                _onFallback = true;

                if (_useSingle)
                {
                    _current = _singleFallback;
                    return true;
                }
            }

            if (_fallbackEnumerator != null && _fallbackEnumerator.MoveNext())
            {
                _current = _fallbackEnumerator.Current;
                return true;
            }

            return false;
        }

        /// <summary>Sets the enumerator to its initial position.</summary>
        public void Reset()
        {
            _sourceEnumerator.Reset();
            _fallbackEnumerator?.Reset();
            _sourceHadElements = false;
            _onFallback = false;
            _current = default!;
        }

        /// <summary>Releases the source and fallback enumerators.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (!_onFallback)
            {
                _sourceEnumerator.Dispose();
            }

            _fallbackEnumerator?.Dispose();
        }
    }
}
