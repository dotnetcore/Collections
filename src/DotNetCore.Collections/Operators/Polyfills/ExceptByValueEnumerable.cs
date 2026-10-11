using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The <c>ExceptBy</c> operator of the P0 polyfill layer. It yields the elements of the source
    /// whose key does not appear in the given sequence of keys, and yields each key at most once.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>ExceptBy</c> is a BCL operator from .NET 6 onwards; on the lower targets this
    /// implementation is the polyfill and on the higher targets it is the accelerated path.
    /// </para>
    /// <para>
    /// The keys to exclude are collected into a set on the first pull and the source is then walked
    /// once. The same set also deduplicates the result: an element whose key was already produced
    /// is skipped, which is what makes the operator's answer "distinct by key" rather than "all
    /// non-matching elements".
    /// </para>
    /// </remarks>
    /// <typeparam name="TSource">The value enumerable being filtered.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <typeparam name="TKey">The type of the key the elements are filtered by.</typeparam>
    public readonly struct ExceptByValueEnumerable<TSource, TEnumerator, T, TKey> : IValueEnumerable<T, ExceptByValueEnumerator<TEnumerator, T, TKey>>, IValueEnumerableHooks<T>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly TSource _source;
        private readonly IEnumerable<TKey> _keys;
        private readonly Func<T, TKey> _keySelector;
        private readonly IEqualityComparer<TKey> _comparer;

        internal ExceptByValueEnumerable(TSource source, IEnumerable<TKey> keys, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            _source = source;
            _keys = keys ?? throw new ArgumentNullException(nameof(keys));
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        /// <summary>Returns a fresh enumerator that skips elements whose key is in the key sequence.</summary>
        /// <returns>An enumerator positioned before the first surviving element.</returns>
        public ExceptByValueEnumerator<TEnumerator, T, TKey> GetEnumerator()
            => new ExceptByValueEnumerator<TEnumerator, T, TKey>(_source.GetEnumerator(), _keys, _keySelector, _comparer);

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
    /// Builds the excluded-key set on the first pull and then walks the source, yielding the
    /// elements whose key is new and absent from that set.
    /// </summary>
    /// <typeparam name="TEnumerator">The value-type enumerator of the source.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <typeparam name="TKey">The type of the key the elements are filtered by.</typeparam>
    public struct ExceptByValueEnumerator<TEnumerator, T, TKey> : IEnumerator<T>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private readonly IEnumerable<TKey> _keys;
        private readonly Func<T, TKey> _keySelector;
        private readonly IEqualityComparer<TKey> _comparer;
        private HashSet<TKey>? _excluded;
        private T _current;

        internal ExceptByValueEnumerator(TEnumerator enumerator, IEnumerable<TKey> keys, Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            _enumerator = enumerator;
            _keys = keys;
            _keySelector = keySelector;
            _comparer = comparer;
            _excluded = null;
            _current = default!;
        }

        /// <summary>Gets the current surviving element.</summary>
        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        object IEnumerator.Current => _current!;

        /// <summary>Advances to the next element whose key is not excluded.</summary>
        /// <returns><see langword="true"/> if an element was produced; otherwise
        /// <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var excluded = _excluded;
            if (excluded is null)
            {
                // The key sequence is read once, at the start of the walk, and never rewound.
                excluded = new HashSet<TKey>(_keys, _comparer);
                _excluded = excluded;
            }

            while (_enumerator.MoveNext())
            {
                var candidate = _enumerator.Current;
                if (excluded.Add(_keySelector(candidate)))
                {
                    _current = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Sets the enumerator to its initial position, before the first surviving element.</summary>
        public void Reset()
        {
            _enumerator.Reset();
            _excluded = null;
            _current = default!;
        }

        /// <summary>Releases the source enumerator.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _enumerator.Dispose();
    }
}
