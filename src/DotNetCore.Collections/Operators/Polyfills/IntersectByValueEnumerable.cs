using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The <c>IntersectBy</c> operator of the P0 polyfill layer. It yields the elements of the
    /// source whose key appears in the given sequence of keys, and yields each key at most once.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>IntersectBy</c> is a BCL operator from .NET 6 onwards; on the lower targets this
    /// implementation is the polyfill and on the higher targets it is the accelerated path.
    /// </para>
    /// <para>
    /// The keys to keep are collected into a set on the first pull. An element is produced only
    /// when its key is still in that set, and the key is then removed - which is how the operator
    /// answers with one element per key even when both sides hold duplicates. Removing rather than
    /// tracking a second "already produced" set is what <c>System.Linq</c> does as well, and it is
    /// what keeps the walk to a single pass over the source.
    /// </para>
    /// </remarks>
    /// <typeparam name="TSource">The value enumerable being filtered.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <typeparam name="TKey">The type of the key the elements are filtered by.</typeparam>
    public readonly struct IntersectByValueEnumerable<TSource, TEnumerator, T, TKey> : IValueEnumerable<T, IntersectByValueEnumerator<TEnumerator, T, TKey>>, IValueEnumerableHooks<T>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly TSource _source;
        private readonly IEnumerable<TKey> _keys;
        private readonly Func<T, TKey> _keySelector;
        private readonly IEqualityComparer<TKey> _comparer;

        internal IntersectByValueEnumerable(TSource source, IEnumerable<TKey> keys, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            _source = source;
            _keys = keys ?? throw new ArgumentNullException(nameof(keys));
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        /// <summary>Returns a fresh enumerator that keeps elements whose key is in the key sequence.</summary>
        /// <returns>An enumerator positioned before the first matching element.</returns>
        public IntersectByValueEnumerator<TEnumerator, T, TKey> GetEnumerator()
            => new IntersectByValueEnumerator<TEnumerator, T, TKey>(_source.GetEnumerator(), _keys, _keySelector, _comparer);

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
    /// Builds the kept-key set on the first pull and then walks the source, yielding an element
    /// when its key is still in that set and consuming the key as it goes.
    /// </summary>
    /// <typeparam name="TEnumerator">The value-type enumerator of the source.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <typeparam name="TKey">The type of the key the elements are filtered by.</typeparam>
    public struct IntersectByValueEnumerator<TEnumerator, T, TKey> : IEnumerator<T>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private readonly IEnumerable<TKey> _keys;
        private readonly Func<T, TKey> _keySelector;
        private readonly IEqualityComparer<TKey> _comparer;
        private HashSet<TKey>? _remaining;
        private T _current;

        internal IntersectByValueEnumerator(TEnumerator enumerator, IEnumerable<TKey> keys, Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            _enumerator = enumerator;
            _keys = keys;
            _keySelector = keySelector;
            _comparer = comparer;
            _remaining = null;
            _current = default!;
        }

        /// <summary>Gets the current matching element.</summary>
        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        object IEnumerator.Current => _current!;

        /// <summary>Advances to the next element whose key is still available.</summary>
        /// <returns><see langword="true"/> if an element was produced; otherwise
        /// <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var remaining = _remaining;
            if (remaining is null)
            {
                // The key sequence is read once, at the start of the walk, and never rewound.
                remaining = new HashSet<TKey>(_keys, _comparer);
                _remaining = remaining;
            }

            while (_enumerator.MoveNext())
            {
                var candidate = _enumerator.Current;
                if (remaining.Remove(_keySelector(candidate)))
                {
                    _current = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Sets the enumerator to its initial position, before the first matching element.</summary>
        public void Reset()
        {
            _enumerator.Reset();
            _remaining = null;
            _current = default!;
        }

        /// <summary>Releases the source enumerator.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _enumerator.Dispose();
    }
}
