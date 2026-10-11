using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The <c>CountBy</c> operator of the P0 polyfill layer. It walks the source once, counts how
    /// often each key occurs, and then answers with one <see cref="KeyValuePair{TKey,TValue}"/> per
    /// distinct key, so a caller that has a key selector does not have to group and then count the
    /// groups.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>CountBy</c> is a BCL operator from .NET 9 onwards. On net451 through net8.0 the BCL has no
    /// such method, which is what makes this a polyfill: the implementation below is the one a
    /// low-TFM consumer reaches, and on the higher targets it is the accelerated path over a
    /// contiguous source.
    /// </para>
    /// <para>
    /// The operator is deferred but not streaming. Nothing is read when the pipeline is built, and
    /// the first pull drains the whole source into a dictionary; the results are then handed out
    /// one key at a time. A consumer that stops early still paid for the full walk, because a count
    /// cannot be known before the sequence has been seen to its end. The dictionary is created on
    /// that first pull rather than in the constructor, so an enumerable that is built and never
    /// walked allocates nothing.
    /// </para>
    /// <para>
    /// The keys come out in the order the dictionary holds them, which is the order in which they
    /// were first seen. That is the same order <c>System.Linq.Enumerable.CountBy</c> produces, so a
    /// low-TFM consumer sees the sequence a high-TFM consumer sees.
    /// </para>
    /// <para>
    /// The comparison is <see cref="EqualityComparer{T}.Default"/> unless a comparer is supplied,
    /// which is the rule the BCL operator follows.
    /// </para>
    /// </remarks>
    /// <typeparam name="TSource">The value enumerable being counted.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <typeparam name="TKey">The type of the key the elements are counted by. The BCL operator
    /// constrains it to <see langword="notnull"/>, and so does this one.</typeparam>
    public readonly struct CountByValueEnumerable<TSource, TEnumerator, T, TKey> : IValueEnumerable<KeyValuePair<TKey, int>, CountByValueEnumerator<TEnumerator, T, TKey>>, IValueEnumerableHooks<KeyValuePair<TKey, int>>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
        where TKey : notnull
    {
        private readonly TSource _source;
        private readonly Func<T, TKey> _keySelector;
        private readonly IEqualityComparer<TKey> _comparer;

        internal CountByValueEnumerable(TSource source, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            _source = source;
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        /// <summary>Returns a fresh enumerator that counts the source by key.</summary>
        /// <returns>An enumerator positioned before the first key.</returns>
        public CountByValueEnumerator<TEnumerator, T, TKey> GetEnumerator()
            => new CountByValueEnumerator<TEnumerator, T, TKey>(_source.GetEnumerator(), _keySelector, _comparer);

        IEnumerator<KeyValuePair<TKey, int>> IEnumerable<KeyValuePair<TKey, int>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // The number of results is the number of distinct keys, which cannot be known before the
        // source has been drained, and the results are not stored as one contiguous block.
        bool IValueEnumerableHooks<KeyValuePair<TKey, int>>.TryGetNonEnumeratedCount(out int count)
        {
            count = 0;
            return false;
        }

#if NETCOREAPP3_0_OR_GREATER
        bool IValueEnumerableHooks<KeyValuePair<TKey, int>>.TryGetSpan(out ReadOnlySpan<KeyValuePair<TKey, int>> span)
        {
            span = default;
            return false;
        }

        bool IValueEnumerableHooks<KeyValuePair<TKey, int>>.TryCopyTo(Span<KeyValuePair<TKey, int>> destination, int offset) => false;
#endif
    }

    /// <summary>
    /// Drains the source into a dictionary on the first pull and then hands the counts out one key
    /// at a time.
    /// </summary>
    /// <typeparam name="TEnumerator">The value-type enumerator of the source.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <typeparam name="TKey">The type of the key the elements are counted by. The BCL operator
    /// constrains it to <see langword="notnull"/>, and so does this one.</typeparam>
    public struct CountByValueEnumerator<TEnumerator, T, TKey> : IEnumerator<KeyValuePair<TKey, int>>
        where TEnumerator : struct, IEnumerator<T>
        where TKey : notnull
    {
        private TEnumerator _source;
        private readonly Func<T, TKey> _keySelector;
        private readonly IEqualityComparer<TKey> _comparer;
        private Dictionary<TKey, int>? _counts;
        private Dictionary<TKey, int>.Enumerator _cursor;
        private KeyValuePair<TKey, int> _current;

        internal CountByValueEnumerator(TEnumerator source, Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            _source = source;
            _keySelector = keySelector;
            _comparer = comparer;
            _counts = null;
            _cursor = default;
            _current = default;
        }

        /// <summary>Gets the current key and its count.</summary>
        public KeyValuePair<TKey, int> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        object IEnumerator.Current => _current;

        /// <summary>Advances to the next key.</summary>
        /// <returns><see langword="true"/> if a key was produced; otherwise
        /// <see langword="false"/>.</returns>
        public bool MoveNext()
        {
            // The dictionary is filled on the first pull, not when the pipeline is built.
            var counts = _counts;
            if (counts is null)
            {
                counts = new Dictionary<TKey, int>(_comparer);
                while (_source.MoveNext())
                {
                    var key = _keySelector(_source.Current);
                    if (counts.TryGetValue(key, out var seen))
                    {
                        counts[key] = seen + 1;
                    }
                    else
                    {
                        counts.Add(key, 1);
                    }
                }

                _counts = counts;
                _cursor = counts.GetEnumerator();
            }

            if (_cursor.MoveNext())
            {
                _current = _cursor.Current;
                return true;
            }

            return false;
        }

        /// <summary>Sets the enumerator to its initial position, before the first key.</summary>
        public void Reset()
        {
            _source.Reset();
            _counts = null;
            _cursor = default;
            _current = default;
        }

        /// <summary>Releases the source enumerator.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _source.Dispose();
    }
}
