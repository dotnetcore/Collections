using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The <c>AggregateBy</c> operator of the P0 polyfill layer. It folds every element of the
    /// source into a per-key accumulator and answers with one
    /// <see cref="KeyValuePair{TKey,TValue}"/> per distinct key, which is the "group and aggregate"
    /// shape without the intermediate groups.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>AggregateBy</c> is a BCL operator from .NET 9 onwards. On net451 through net8.0 the BCL has
    /// no such method, which is what makes this a polyfill: the implementation below is the one a
    /// low-TFM consumer reaches, and on the higher targets it is the accelerated path over a
    /// contiguous source.
    /// </para>
    /// <para>
    /// The accumulator of a key starts from either a shared seed or a seed the key itself produces.
    /// The two are one operator in the BCL and one operator here; the seed overload is the seed
    /// factory that ignores its key. The distinction matters for an accumulator that is mutable:
    /// a shared seed is handed to every key, so a caller that means "one fresh accumulator per key"
    /// has to use the factory overload.
    /// </para>
    /// <para>
    /// The operator is deferred but not streaming. Nothing is read when the pipeline is built, and
    /// the first pull drains the whole source into a dictionary; the results are then handed out
    /// one key at a time. The dictionary is created on that first pull rather than in the
    /// constructor, so an enumerable that is built and never walked allocates nothing.
    /// </para>
    /// <para>
    /// The keys come out in the order the dictionary holds them, which is the order in which they
    /// were first seen. That is the same order <c>System.Linq.Enumerable.AggregateBy</c> produces,
    /// so a low-TFM consumer sees the sequence a high-TFM consumer sees.
    /// </para>
    /// <para>
    /// The comparison is <see cref="EqualityComparer{T}.Default"/> unless a comparer is supplied,
    /// which is the rule the BCL operator follows.
    /// </para>
    /// </remarks>
    /// <typeparam name="TSource">The value enumerable being aggregated.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <typeparam name="TKey">The type of the key the elements are grouped by. The BCL operator
    /// constrains it to <see langword="notnull"/>, and so does this one.</typeparam>
    /// <typeparam name="TAccumulate">The type of the per-key accumulator.</typeparam>
    public readonly struct AggregateByValueEnumerable<TSource, TEnumerator, T, TKey, TAccumulate> : IValueEnumerable<KeyValuePair<TKey, TAccumulate>, AggregateByValueEnumerator<TEnumerator, T, TKey, TAccumulate>>, IValueEnumerableHooks<KeyValuePair<TKey, TAccumulate>>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
        where TKey : notnull
    {
        private readonly TSource _source;
        private readonly Func<T, TKey> _keySelector;
        private readonly TAccumulate _seed;
        private readonly Func<TKey, TAccumulate>? _seedFactory;
        private readonly Func<TAccumulate, T, TAccumulate> _accumulator;
        private readonly IEqualityComparer<TKey> _comparer;

        internal AggregateByValueEnumerable(TSource source, Func<T, TKey> keySelector, TAccumulate seed, Func<TAccumulate, T, TAccumulate> accumulator, IEqualityComparer<TKey>? comparer)
        {
            _source = source;
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            _seed = seed;
            _seedFactory = null;
            _accumulator = accumulator ?? throw new ArgumentNullException(nameof(accumulator));
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        internal AggregateByValueEnumerable(TSource source, Func<T, TKey> keySelector, Func<TKey, TAccumulate> seedFactory, Func<TAccumulate, T, TAccumulate> accumulator, IEqualityComparer<TKey>? comparer)
        {
            _source = source;
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            _seed = default!;
            _seedFactory = seedFactory ?? throw new ArgumentNullException(nameof(seedFactory));
            _accumulator = accumulator ?? throw new ArgumentNullException(nameof(accumulator));
            _comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        /// <summary>Returns a fresh enumerator that aggregates the source by key.</summary>
        /// <returns>An enumerator positioned before the first key.</returns>
        public AggregateByValueEnumerator<TEnumerator, T, TKey, TAccumulate> GetEnumerator()
            => new AggregateByValueEnumerator<TEnumerator, T, TKey, TAccumulate>(_source.GetEnumerator(), _keySelector, _seed, _seedFactory, _accumulator, _comparer);

        IEnumerator<KeyValuePair<TKey, TAccumulate>> IEnumerable<KeyValuePair<TKey, TAccumulate>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // The number of results is the number of distinct keys, which cannot be known before the
        // source has been drained, and the results are not stored as one contiguous block.
        bool IValueEnumerableHooks<KeyValuePair<TKey, TAccumulate>>.TryGetNonEnumeratedCount(out int count)
        {
            count = 0;
            return false;
        }

#if NETCOREAPP3_0_OR_GREATER
        bool IValueEnumerableHooks<KeyValuePair<TKey, TAccumulate>>.TryGetSpan(out ReadOnlySpan<KeyValuePair<TKey, TAccumulate>> span)
        {
            span = default;
            return false;
        }

        bool IValueEnumerableHooks<KeyValuePair<TKey, TAccumulate>>.TryCopyTo(Span<KeyValuePair<TKey, TAccumulate>> destination, int offset) => false;
#endif
    }

    /// <summary>
    /// Drains the source into a dictionary of accumulators on the first pull and then hands the
    /// results out one key at a time.
    /// </summary>
    /// <typeparam name="TEnumerator">The value-type enumerator of the source.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <typeparam name="TKey">The type of the key the elements are grouped by. The BCL operator
    /// constrains it to <see langword="notnull"/>, and so does this one.</typeparam>
    /// <typeparam name="TAccumulate">The type of the per-key accumulator.</typeparam>
    public struct AggregateByValueEnumerator<TEnumerator, T, TKey, TAccumulate> : IEnumerator<KeyValuePair<TKey, TAccumulate>>
        where TEnumerator : struct, IEnumerator<T>
        where TKey : notnull
    {
        private TEnumerator _source;
        private readonly Func<T, TKey> _keySelector;
        private readonly TAccumulate _seed;
        private readonly Func<TKey, TAccumulate>? _seedFactory;
        private readonly Func<TAccumulate, T, TAccumulate> _accumulator;
        private readonly IEqualityComparer<TKey> _comparer;
        private Dictionary<TKey, TAccumulate>? _results;
        private Dictionary<TKey, TAccumulate>.Enumerator _cursor;
        private KeyValuePair<TKey, TAccumulate> _current;

        internal AggregateByValueEnumerator(TEnumerator source, Func<T, TKey> keySelector, TAccumulate seed, Func<TKey, TAccumulate>? seedFactory, Func<TAccumulate, T, TAccumulate> accumulator, IEqualityComparer<TKey> comparer)
        {
            _source = source;
            _keySelector = keySelector;
            _seed = seed;
            _seedFactory = seedFactory;
            _accumulator = accumulator;
            _comparer = comparer;
            _results = null;
            _cursor = default;
            _current = default;
        }

        /// <summary>Gets the current key and its accumulator.</summary>
        public KeyValuePair<TKey, TAccumulate> Current
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
            var results = _results;
            if (results is null)
            {
                results = new Dictionary<TKey, TAccumulate>(_comparer);
                var seedFactory = _seedFactory;
                var seed = _seed;
                var accumulator = _accumulator;
                while (_source.MoveNext())
                {
                    var element = _source.Current;
                    var key = _keySelector(element);
                    if (results.TryGetValue(key, out var aggregate))
                    {
                        results[key] = accumulator(aggregate, element);
                    }
                    else
                    {
                        // The seed overload is the seed factory that ignores its key.
                        results.Add(key, accumulator(seedFactory is null ? seed : seedFactory(key), element));
                    }
                }

                _results = results;
                _cursor = results.GetEnumerator();
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
            _results = null;
            _cursor = default;
            _current = default;
        }

        /// <summary>Releases the source enumerator.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _source.Dispose();
    }
}
