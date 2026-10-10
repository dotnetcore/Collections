using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DotNetCore.Collections.Internal
{
    /// <summary>
    /// The <c>Where</c> operator. It wraps any value enumerable of this engine, keeps the
    /// predicate in a struct field, and yields a value-type enumerator - so the whole pipeline
    /// stays on the stack and no closure class is created to carry the predicate.
    /// </summary>
    /// <remarks>
    /// The engine writes one operator over a generic source rather than one per source shape. The
    /// four entry surfaces in <see cref="OperatorExtensions"/> exist only because C# will not infer
    /// type arguments from a generic constraint; each of them is a one-line shim that pins
    /// <typeparamref name="TSource"/> and <typeparamref name="TEnumerator"/> to a concrete arm.
    /// </remarks>
    /// <typeparam name="TSource">The value enumerable being filtered.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    internal readonly struct WhereValueEnumerable<TSource, TEnumerator, T> : IValueEnumerable<T, WhereValueEnumerator<TEnumerator, T>>, IValueEnumerableHooks<T>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly TSource _source;
        private readonly Func<T, bool> _predicate;

        internal WhereValueEnumerable(TSource source, Func<T, bool> predicate)
        {
            _source = source;
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        public WhereValueEnumerator<TEnumerator, T> GetEnumerator()
            => new WhereValueEnumerator<TEnumerator, T>(_source.GetEnumerator(), _predicate);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // A filtered sequence knows nothing about its own length or storage, so it declines both
        // contiguous hooks. It does keep the protocol total, which is what lets the engine's
        // generic consumers accept any operator result without a special case.
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
    /// Walks the source and forwards only the elements the predicate accepts. The accepted element
    /// is parked in a field when it is found, so the predicate runs once per element no matter how
    /// many times <see cref="Current"/> is read.
    /// </summary>
    /// <typeparam name="TEnumerator">The value-type enumerator of the source.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    internal struct WhereValueEnumerator<TEnumerator, T> : IEnumerator<T>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private readonly Func<T, bool> _predicate;
        private T _current;

        internal WhereValueEnumerator(TEnumerator enumerator, Func<T, bool> predicate)
        {
            _enumerator = enumerator;
            _predicate = predicate;
            _current = default!;
        }

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        // T is unconstrained, so the element is legitimately null-able whenever T is a reference
        // type; the non-generic IEnumerator.Current simply forwards it.
        object IEnumerator.Current => _current!;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            while (_enumerator.MoveNext())
            {
                var candidate = _enumerator.Current;
                if (_predicate(candidate))
                {
                    _current = candidate;
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
