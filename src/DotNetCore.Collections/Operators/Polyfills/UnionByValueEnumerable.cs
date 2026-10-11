using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The <c>UnionBy</c> operator of the P0 polyfill layer. It yields the elements of the source
    /// followed by the elements of a second sequence, dropping any element whose key has already
    /// been produced.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>UnionBy</c> is a BCL operator from .NET 6 onwards; on the lower targets this
    /// implementation is the polyfill and on the higher targets it is the accelerated path.
    /// </para>
    /// <para>
    /// One set of keys carries the whole walk: the source is drained first and the second sequence
    /// is drained only after it, so the operator stays streaming and a consumer that stops early
    /// never touches the second sequence at all.
    /// </para>
    /// </remarks>
    /// <typeparam name="TSource">The value enumerable being unioned.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <typeparam name="TKey">The type of the key the elements are deduplicated by.</typeparam>
    public readonly struct UnionByValueEnumerable<TSource, TEnumerator, T, TKey> : IValueEnumerable<T, UnionByValueEnumerator<TEnumerator, T, TKey>>, IValueEnumerableHooks<T>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly TSource _source;
        private readonly IEnumerable<T> _second;
        private readonly Func<T, TKey> _keySelector;
        private readonly IEqualityComparer<TKey> _comparer;

        internal UnionByValueEnumerable(TSource source, IEnumerable<T> second, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            _source = source;
            _second = second ?? throw new ArgumentNullException(nameof(second));
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        /// <summary>Returns a fresh enumerator that unions the two sequences by key.</summary>
        /// <returns>An enumerator positioned before the first distinct element.</returns>
        public UnionByValueEnumerator<TEnumerator, T, TKey> GetEnumerator()
            => new UnionByValueEnumerator<TEnumerator, T, TKey>(_source.GetEnumerator(), _second, _keySelector, _comparer);

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
    /// Walks the source and then the second sequence, sharing one set of keys so that an element
    /// whose key was already produced on either side is dropped.
    /// </summary>
    /// <typeparam name="TEnumerator">The value-type enumerator of the source.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <typeparam name="TKey">The type of the key the elements are deduplicated by.</typeparam>
    public struct UnionByValueEnumerator<TEnumerator, T, TKey> : IEnumerator<T>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private readonly IEnumerable<T> _second;
        private readonly Func<T, TKey> _keySelector;
        private readonly IEqualityComparer<TKey> _comparer;
        private HashSet<TKey>? _seen;
        private IEnumerator<T>? _secondEnumerator;
        private bool _drainingSecond;
        private T _current;

        internal UnionByValueEnumerator(TEnumerator enumerator, IEnumerable<T> second, Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            _enumerator = enumerator;
            _second = second;
            _keySelector = keySelector;
            _comparer = comparer;
            _seen = null;
            _secondEnumerator = null;
            _drainingSecond = false;
            _current = default!;
        }

        /// <summary>Gets the current distinct element.</summary>
        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        object IEnumerator.Current => _current!;

        /// <summary>Advances to the next distinct element of either sequence.</summary>
        /// <returns><see langword="true"/> if an element was produced; otherwise
        /// <see langword="false"/>.</returns>
        public bool MoveNext()
        {
            var seen = _seen;
            if (seen is null)
            {
                // The set is allocated on the first pull, not when the pipeline is built.
                seen = new HashSet<TKey>(_comparer);
                _seen = seen;
            }

            while (!_drainingSecond)
            {
                if (_enumerator.MoveNext())
                {
                    var candidate = _enumerator.Current;
                    if (seen.Add(_keySelector(candidate)))
                    {
                        _current = candidate;
                        return true;
                    }

                    continue;
                }

                // The source is exhausted: switch to the second sequence and keep the same set.
                _drainingSecond = true;
                _secondEnumerator = _second.GetEnumerator();
            }

            var second = _secondEnumerator!;
            while (second.MoveNext())
            {
                var candidate = second.Current;
                if (seen.Add(_keySelector(candidate)))
                {
                    _current = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Sets the enumerator to its initial position, before the first distinct element.</summary>
        public void Reset()
        {
            _enumerator.Reset();
            _secondEnumerator?.Dispose();
            _secondEnumerator = null;
            _drainingSecond = false;
            _seen = null;
            _current = default!;
        }

        /// <summary>Releases both source enumerators.</summary>
        public void Dispose()
        {
            _enumerator.Dispose();
            _secondEnumerator?.Dispose();
        }
    }
}
