using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DotNetCore.Collections.Internal
{
    /// <summary>
    /// The <c>Select</c> operator. The projection is held in a struct field, so a chain that stays
    /// inside the engine never allocates to carry it.
    /// </summary>
    /// <typeparam name="TSource">The value enumerable being projected.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="T">The type of the source elements.</typeparam>
    /// <typeparam name="TResult">The type of the projected elements.</typeparam>
    internal readonly struct SelectValueEnumerable<TSource, TEnumerator, T, TResult> : IValueEnumerable<TResult, SelectValueEnumerator<TEnumerator, T, TResult>>, IValueEnumerableHooks<TResult>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly TSource _source;
        private readonly Func<T, TResult> _selector;

        internal SelectValueEnumerable(TSource source, Func<T, TResult> selector)
        {
            _source = source;
            _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        }

        public SelectValueEnumerator<TEnumerator, T, TResult> GetEnumerator()
            => new SelectValueEnumerator<TEnumerator, T, TResult>(_source.GetEnumerator(), _selector);

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
    /// Projects each source element as it is advanced and parks the result, so the projection runs
    /// once per element rather than once per read of <see cref="Current"/>.
    /// </summary>
    /// <typeparam name="TEnumerator">The value-type enumerator of the source.</typeparam>
    /// <typeparam name="T">The type of the source elements.</typeparam>
    /// <typeparam name="TResult">The type of the projected elements.</typeparam>
    internal struct SelectValueEnumerator<TEnumerator, T, TResult> : IEnumerator<TResult>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private readonly Func<T, TResult> _selector;
        private TResult _current;

        internal SelectValueEnumerator(TEnumerator enumerator, Func<T, TResult> selector)
        {
            _enumerator = enumerator;
            _selector = selector;
            _current = default!;
        }

        public TResult Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        // TResult is unconstrained, so the element is legitimately null-able whenever TResult is a
        // reference type; the non-generic IEnumerator.Current simply forwards it.
        object IEnumerator.Current => _current!;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (_enumerator.MoveNext())
            {
                _current = _selector(_enumerator.Current);
                return true;
            }

            return false;
        }

        public void Reset()
        {
            _enumerator.Reset();
            _current = default!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _enumerator.Dispose();
    }
}
