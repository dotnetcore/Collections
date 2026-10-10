using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The <c>Scan</c> operator without a seed: an inclusive prefix aggregation. It yields the
    /// running aggregate after every element, so the sequence of intermediate results is visible
    /// rather than only the final one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "Inclusive" means the result has the same length as the source: the first element is the
    /// first aggregate, and the transformation is applied from the second element on. An empty
    /// source yields nothing. The seeded sibling <see cref="SeededScanValueEnumerable{TSource,TEnumerator,T,TResult}"/>
    /// shifts every result by one and always yields at least the seed.
    /// </para>
    /// <para>
    /// The aggregate is computed once per element as the walk advances and then parked, so reading
    /// <see cref="IEnumerator{T}.Current"/> repeatedly does not re-run the transformation.
    /// </para>
    /// </remarks>
    /// <typeparam name="TSource">The value enumerable being scanned.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="T">The type of the elements, which is also the type of the aggregate.</typeparam>
    public readonly struct ScanValueEnumerable<TSource, TEnumerator, T> : IValueEnumerable<T, ScanValueEnumerator<TEnumerator, T>>, IValueEnumerableHooks<T>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly TSource _source;
        private readonly Func<T, T, T> _transformation;

        internal ScanValueEnumerable(TSource source, Func<T, T, T> transformation)
        {
            _source = source;
            _transformation = transformation ?? throw new ArgumentNullException(nameof(transformation));
        }

        /// <summary>Returns a fresh enumerator of running aggregates.</summary>
        /// <returns>An enumerator positioned before the first aggregate.</returns>
        public ScanValueEnumerator<TEnumerator, T> GetEnumerator()
            => new ScanValueEnumerator<TEnumerator, T>(_source.GetEnumerator(), _transformation);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // A running aggregate has neither a length the source could report nor storage to expose.
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
    /// Carries the running aggregate of the seedless scan. The first source element is adopted as
    /// the initial aggregate without the transformation being applied to it.
    /// </summary>
    /// <typeparam name="TEnumerator">The value-type enumerator of the source.</typeparam>
    /// <typeparam name="T">The type of the elements, which is also the type of the aggregate.</typeparam>
    public struct ScanValueEnumerator<TEnumerator, T> : IEnumerator<T>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private readonly Func<T, T, T> _transformation;
        private bool _started;
        private T _accumulator;
        private T _current;

        internal ScanValueEnumerator(TEnumerator enumerator, Func<T, T, T> transformation)
        {
            _enumerator = enumerator;
            _transformation = transformation;
            _started = false;
            _accumulator = default!;
            _current = default!;
        }

        /// <summary>Gets the current running aggregate.</summary>
        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        // T is unconstrained, so the element is legitimately null-able whenever T is a reference
        // type; the non-generic IEnumerator.Current simply forwards it.
        object IEnumerator.Current => _current!;

        /// <summary>Advances to the next running aggregate.</summary>
        /// <returns><see langword="true"/> if an aggregate was produced; otherwise
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

                // The first aggregate is the first element, with no transformation applied.
                _accumulator = _enumerator.Current;
                _current = _accumulator;
                return true;
            }

            if (_enumerator.MoveNext())
            {
                _accumulator = _transformation(_accumulator, _enumerator.Current);
                _current = _accumulator;
                return true;
            }

            return false;
        }

        /// <summary>Sets the enumerator to its initial position, before the first aggregate.</summary>
        public void Reset()
        {
            _enumerator.Reset();
            _started = false;
            _accumulator = default!;
            _current = default!;
        }

        /// <summary>Releases the source enumerator.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _enumerator.Dispose();
    }

    /// <summary>
    /// The seeded form of <c>Scan</c>. The seed is the first result and every source element then
    /// folds into it, so a source of <c>n</c> elements produces <c>n + 1</c> results and an empty
    /// source still produces the seed.
    /// </summary>
    /// <typeparam name="TSource">The value enumerable being scanned.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="T">The type of the source elements.</typeparam>
    /// <typeparam name="TResult">The type of the aggregate, which may differ from the element type.</typeparam>
    public readonly struct SeededScanValueEnumerable<TSource, TEnumerator, T, TResult> : IValueEnumerable<TResult, SeededScanValueEnumerator<TEnumerator, T, TResult>>, IValueEnumerableHooks<TResult>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly TSource _source;
        private readonly TResult _seed;
        private readonly Func<TResult, T, TResult> _transformation;

        internal SeededScanValueEnumerable(TSource source, TResult seed, Func<TResult, T, TResult> transformation)
        {
            _source = source;
            _seed = seed;
            _transformation = transformation ?? throw new ArgumentNullException(nameof(transformation));
        }

        /// <summary>Returns a fresh enumerator that starts from the seed.</summary>
        /// <returns>An enumerator positioned before the seed.</returns>
        public SeededScanValueEnumerator<TEnumerator, T, TResult> GetEnumerator()
            => new SeededScanValueEnumerator<TEnumerator, T, TResult>(_source.GetEnumerator(), _seed, _transformation);

        IEnumerator<TResult> IEnumerable<TResult>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // One result more than the source has elements, and the seed is not storage the caller can
        // reach, so nothing is promised up front.
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
    /// Carries the running aggregate of the seeded scan. The seed is handed out before the source is
    /// touched, which is what makes an empty source yield exactly one result.
    /// </summary>
    /// <typeparam name="TEnumerator">The value-type enumerator of the source.</typeparam>
    /// <typeparam name="T">The type of the source elements.</typeparam>
    /// <typeparam name="TResult">The type of the aggregate, which may differ from the element type.</typeparam>
    public struct SeededScanValueEnumerator<TEnumerator, T, TResult> : IEnumerator<TResult>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private readonly TResult _seed;
        private readonly Func<TResult, T, TResult> _transformation;
        private bool _started;
        private TResult _accumulator;
        private TResult _current;

        internal SeededScanValueEnumerator(TEnumerator enumerator, TResult seed, Func<TResult, T, TResult> transformation)
        {
            _enumerator = enumerator;
            _seed = seed;
            _transformation = transformation;
            _started = false;
            _accumulator = seed;
            _current = seed;
        }

        /// <summary>Gets the current running aggregate.</summary>
        public TResult Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        // TResult is unconstrained, so the element is legitimately null-able whenever TResult is a
        // reference type; the non-generic IEnumerator.Current simply forwards it.
        object IEnumerator.Current => _current!;

        /// <summary>Advances to the next running aggregate.</summary>
        /// <returns><see langword="true"/> if an aggregate was produced; otherwise
        /// <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (!_started)
            {
                _started = true;
                _accumulator = _seed;
                _current = _seed;
                return true;
            }

            if (_enumerator.MoveNext())
            {
                _accumulator = _transformation(_accumulator, _enumerator.Current);
                _current = _accumulator;
                return true;
            }

            return false;
        }

        /// <summary>Sets the enumerator to its initial position, before the seed.</summary>
        public void Reset()
        {
            _enumerator.Reset();
            _started = false;
            _accumulator = _seed;
            _current = _seed;
        }

        /// <summary>Releases the source enumerator.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _enumerator.Dispose();
    }
}
