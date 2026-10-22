using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The <c>Memoize</c> operator: caches the source's elements on first enumeration so that
    /// subsequent enumerations replay the cache instead of re-running the source. This is the
    /// operator the Paginable module needs to avoid re-executing remote or database sequences
    /// when the paging metadata and the page fragment both walk the same source.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The cache is a shared reference type behind the value-type enumerable: the first enumerator
    /// fills it, later enumerators replay it. The cache grows on demand - a partial first
    /// enumeration followed by a full second enumeration fills the gap during the second pass.
    /// </para>
    /// <para>
    /// The operator is not in the BCL; it exists in Ix.NET and MoreLINQ.
    /// </para>
    /// </remarks>
    public readonly struct MemoizedValueEnumerable<TSource, TEnumerator, T>
        : IValueEnumerable<T, MemoizedValueEnumerator<T>>,
          IValueEnumerableHooks<T>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly MemoizeCache<T> _cache;
        private readonly TSource _source;

        internal MemoizedValueEnumerable(TSource source)
        {
            _source = source;
            _cache = new MemoizeCache<T>();
            _cache.Initialize<TSource, TEnumerator>(source);
        }

        /// <summary>Returns a fresh enumerator that replays the cache.</summary>
        /// <returns>An enumerator positioned before the first cached element.</returns>
        public MemoizedValueEnumerator<T> GetEnumerator()
            => new MemoizedValueEnumerator<T>(_cache);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        bool IValueEnumerableHooks<T>.TryGetNonEnumeratedCount(out int count)
        {
            if (_cache.IsFullyMaterialized)
            {
                count = _cache.Count;
                return true;
            }

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
    /// The shared cache behind <c>Memoize</c>. A list of already-seen elements plus a flag that
    /// says whether the source has been fully drained.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The fill path takes a lock so that two enumerators advancing at the same time do not
    /// double-fill or corrupt the buffer. The read path deliberately does not: it is the hot path
    /// by a wide margin - replaying a cached element is what every <c>Skip</c> step and every
    /// second walk does - and a monitor acquire per element costs far more than the walk it saves.
    /// A lock there made the operator a net loss for exactly the replay pattern it exists to serve,
    /// so reads go through a published fill count instead.
    /// </para>
    /// <para>
    /// The publication is a release/acquire pair. <see cref="TryGetOrAdvance"/> appends under the
    /// lock and then publishes the new length with <c>Volatile.Write</c>; a reader that sees that
    /// length with <c>Volatile.Read</c> is guaranteed to see the element it refers to. Indices
    /// below the published length are append-only, so reading them needs no lock even while another
    /// thread fills: <see cref="List{T}"/> only ever grows by copying the prefix it already holds,
    /// so either the old or the new backing array yields the same element at a published index.
    /// </para>
    /// </remarks>
    public sealed class MemoizeCache<T>
    {
        private readonly List<T> _buffer;
        private readonly object _gate;

        /// <summary>
        /// The number of elements that have been published as readable. Only ever increased, and
        /// only inside the lock, so a reader may treat it as a high-water mark.
        /// </summary>
        private int _filled;

        private IEnumerator<T>? _sourceEnumerator;
        private volatile bool _fullyMaterialized;

        internal MemoizeCache()
        {
            _buffer = new List<T>();
            _gate = new object();
        }

        internal int Count => Volatile.Read(ref _filled);

        internal bool IsFullyMaterialized => _fullyMaterialized;

        internal void Initialize<TSource, TEnumerator>(TSource source)
            where TSource : struct, IValueEnumerable<T, TEnumerator>
            where TEnumerator : struct, IEnumerator<T>
        {
            lock (_gate)
            {
                if (_sourceEnumerator == null && !_fullyMaterialized)
                {
                    _sourceEnumerator = source.GetEnumerator();
                }
            }
        }

        internal bool TryGetCached(int index, out T value)
        {
            // Lock-free fast path. The unsigned comparison folds the negative case (the enumerator
            // starts at -1 and asks for index 0 first) into the same branch.
            if ((uint) index < (uint) Volatile.Read(ref _filled))
            {
                value = _buffer[index];
                return true;
            }

            value = default!;
            return false;
        }

        /// <summary>
        /// Returns the element at <paramref name="index"/>, filling the cache forward from the
        /// source as far as that index if it is not there yet.
        /// </summary>
        /// <returns><see langword="true"/> if the source had an element at that index.</returns>
        /// <remarks>
        /// The fill is driven <b>to a requested index</b> rather than "one more element", which is
        /// what makes concurrent enumerators correct. Two enumerators can sit at the same position
        /// and both miss the cache; if the loser of that race simply advanced the source once, it
        /// would be handed the element at <c>index + 1</c> and report it at <c>index</c>. Filling
        /// until the wanted index exists makes the loser's fill a no-op instead.
        /// </remarks>
        internal bool TryGetOrAdvance(int index, out T value)
        {
            if (TryGetCached(index, out value))
            {
                return true;
            }

            lock (_gate)
            {
                // Another enumerator may have filled this index while we waited for the lock.
                if (index < _buffer.Count)
                {
                    value = _buffer[index];
                    return true;
                }

                while (_buffer.Count <= index)
                {
                    if (_fullyMaterialized || _sourceEnumerator == null)
                    {
                        value = default!;
                        return false;
                    }

                    if (!_sourceEnumerator.MoveNext())
                    {
                        _sourceEnumerator.Dispose();
                        _sourceEnumerator = null;
                        _fullyMaterialized = true;
                        value = default!;
                        return false;
                    }

                    _buffer.Add(_sourceEnumerator.Current);

                    // Publish only after the element is in the buffer: this release write pairs
                    // with the acquire read in TryGetCached.
                    Volatile.Write(ref _filled, _buffer.Count);
                }

                value = _buffer[index];
                return true;
            }
        }
    }

    /// <summary>
    /// An enumerator over a <see cref="MemoizeCache{T}"/>. It reads cached elements up to the
    /// current fill level, then tries to advance the shared source to fill the next slot. When the
    /// source is fully drained it replays the cache from the beginning on the next enumeration.
    /// </summary>
    public struct MemoizedValueEnumerator<T> : IEnumerator<T>
    {
        private readonly MemoizeCache<T> _cache;
        private int _position;
        private T _current;

        internal MemoizedValueEnumerator(MemoizeCache<T> cache)
        {
            _cache = cache;
            _position = -1;
            _current = default!;
        }

        /// <summary>Gets the current cached element.</summary>
        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current;
        }

        object IEnumerator.Current => _current!;

        /// <summary>Advances to the next element, filling the cache if needed.</summary>
        /// <returns><see langword="true"/> if an element was produced.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var next = _position + 1;

            // One call, targeted at this enumerator's own index. Splitting it into "read the cache,
            // else advance the source once" is what used to hand a concurrent enumerator the wrong
            // element; see MemoizeCache.TryGetOrAdvance.
            if (_cache.TryGetOrAdvance(next, out var value))
            {
                _position = next;
                _current = value;
                return true;
            }

            return false;
        }

        /// <summary>Sets the enumerator to its initial position.</summary>
        public void Reset()
        {
            _position = -1;
            _current = default!;
        }

        /// <summary>No-op: the shared cache owns the source enumerator.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
        }
    }
}
