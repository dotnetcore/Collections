using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DotNetCore.Collections.Internal
{
    /// <summary>
    /// <c>Where</c> and <c>Select</c> fused into a single pass. This is the shape the engine cares
    /// about most: the filter and the projection run in one walk of the source, one enumerator and
    /// one delegate pair, with no intermediate sequence to enumerate.
    /// </summary>
    /// <remarks>
    /// Version 1 of the engine exposes the fusion as its own operator instead of trying to detect
    /// <c>Where</c> followed by <c>Select</c>. Fusing at the call site is a source-transformation
    /// problem, and the research note's decision was to keep the matrix hand-written and small:
    /// only <c>Where+Select</c> is fused, and it is fused explicitly.
    /// </remarks>
    /// <typeparam name="TSource">The value enumerable being filtered and projected.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="T">The type of the source elements.</typeparam>
    /// <typeparam name="TResult">The type of the projected elements.</typeparam>
    internal readonly struct WhereSelectValueEnumerable<TSource, TEnumerator, T, TResult> : IValueEnumerable<TResult, WhereSelectValueEnumerator<TEnumerator, T, TResult>>, IValueEnumerableHooks<TResult>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly TSource _source;
        private readonly Func<T, bool> _predicate;
        private readonly Func<T, TResult> _selector;

        internal WhereSelectValueEnumerable(TSource source, Func<T, bool> predicate, Func<T, TResult> selector)
        {
            _source = source;
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
            _selector = selector ?? throw new ArgumentNullException(nameof(selector));
        }

        public WhereSelectValueEnumerator<TEnumerator, T, TResult> GetEnumerator()
            => new WhereSelectValueEnumerator<TEnumerator, T, TResult>(_source.GetEnumerator(), _predicate, _selector);

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
    /// Walks the source once, testing each element and projecting only the ones the predicate
    /// accepts. No intermediate sequence exists at any point, which is the whole point of the
    /// fusion: a <c>Where(...).Select(...)</c> chain would otherwise build a filtered sequence and
    /// then walk it a second time.
    /// </summary>
    /// <typeparam name="TEnumerator">The value-type enumerator of the source.</typeparam>
    /// <typeparam name="T">The type of the source elements.</typeparam>
    /// <typeparam name="TResult">The type of the projected elements.</typeparam>
    internal struct WhereSelectValueEnumerator<TEnumerator, T, TResult> : IEnumerator<TResult>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private readonly Func<T, bool> _predicate;
        private readonly Func<T, TResult> _selector;
        private TResult _current;

        internal WhereSelectValueEnumerator(TEnumerator enumerator, Func<T, bool> predicate, Func<T, TResult> selector)
        {
            _enumerator = enumerator;
            _predicate = predicate;
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
            while (_enumerator.MoveNext())
            {
                var candidate = _enumerator.Current;
                if (_predicate(candidate))
                {
                    _current = _selector(candidate);
                    return true;
                }
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
