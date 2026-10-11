using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The <c>DistinctBy</c> operator of the P0 polyfill layer. It yields the first element of the
    /// source for every distinct key and drops the rest, so a caller that has a key selector does
    /// not have to project, deduplicate and project back.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>DistinctBy</c> is a BCL operator from .NET 6 onwards. On the lower targets - net451,
    /// net461, net47, net48 and netstandard2.0 - the BCL has no such method, which is what makes
    /// this a polyfill: the implementation below is the one a low-TFM consumer reaches, and on the
    /// higher targets it is the accelerated path over a contiguous source.
    /// </para>
    /// <para>
    /// The operator is lazy and streaming. It keeps a set of the keys it has already produced and
    /// yields an element only when its key is new, so an infinite source can be deduplicated and a
    /// short consumer stops the walk. The set is created on the first <see cref="IEnumerator.MoveNext"/>
    /// rather than in the constructor, so an enumerable that is built and never walked allocates
    /// nothing.
    /// </para>
    /// <para>
    /// The comparison is <see cref="EqualityComparer{T}.Default"/> unless a comparer is supplied,
    /// which is the rule <c>System.Linq.Enumerable.DistinctBy</c> follows.
    /// </para>
    /// </remarks>
    /// <typeparam name="TSource">The value enumerable being deduplicated.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <typeparam name="TKey">The type of the key the elements are deduplicated by.</typeparam>
    public readonly struct DistinctByValueEnumerable<TSource, TEnumerator, T, TKey> : IValueEnumerable<T, DistinctByValueEnumerator<TEnumerator, T, TKey>>, IValueEnumerableHooks<T>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly TSource _source;
        private readonly Func<T, TKey> _keySelector;
        private readonly IEqualityComparer<TKey> _comparer;

        internal DistinctByValueEnumerable(TSource source, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            _source = source;
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        /// <summary>Returns a fresh enumerator that drops elements whose key has already been seen.</summary>
        /// <returns>An enumerator positioned before the first distinct element.</returns>
        public DistinctByValueEnumerator<TEnumerator, T, TKey> GetEnumerator()
            => new DistinctByValueEnumerator<TEnumerator, T, TKey>(_source.GetEnumerator(), _keySelector, _comparer);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // Deduplication changes which elements come out, never how many, so the count is unknown
        // up front and there is no contiguous storage to expose.
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
    /// Walks the source and yields the elements whose key has not been produced yet.
    /// </summary>
    /// <typeparam name="TEnumerator">The value-type enumerator of the source.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <typeparam name="TKey">The type of the key the elements are deduplicated by.</typeparam>
    public struct DistinctByValueEnumerator<TEnumerator, T, TKey> : IEnumerator<T>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private readonly Func<T, TKey> _keySelector;
        private readonly IEqualityComparer<TKey> _comparer;
        private HashSet<TKey>? _seen;
        private T _current;

        internal DistinctByValueEnumerator(TEnumerator enumerator, Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            _enumerator = enumerator;
            _keySelector = keySelector;
            _comparer = comparer;
            _seen = null;
            _current = default!;
        }

        /// <summary>Gets the current distinct element.</summary>
        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        // T is unconstrained, so the element is legitimately null-able whenever T is a reference
        // type; the non-generic IEnumerator.Current simply forwards it.
        object IEnumerator.Current => _current!;

        /// <summary>Advances to the next element whose key has not been produced yet.</summary>
        /// <returns><see langword="true"/> if a distinct element was produced; otherwise
        /// <see langword="false"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            // The set is allocated on the first pull, not when the pipeline is built.
            var seen = _seen;
            if (seen is null)
            {
                seen = new HashSet<TKey>(_comparer);
                _seen = seen;
            }

            while (_enumerator.MoveNext())
            {
                var candidate = _enumerator.Current;
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
            _seen = null;
            _current = default!;
        }

        /// <summary>Releases the source enumerator.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _enumerator.Dispose();
    }
}
