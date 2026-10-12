using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    /// says whether the source has been fully drained. A lock protects the fill path so that two
    /// enumerators advancing at the same time do not double-fill or corrupt the buffer.
    /// </summary>
    public sealed class MemoizeCache<T>
    {
        private readonly List<T> _buffer;
        private readonly object _gate;
        private IEnumerator<T>? _sourceEnumerator;
        private bool _fullyMaterialized;

        internal MemoizeCache()
        {
            _buffer = new List<T>();
            _gate = new object();
        }

        internal int Count
        {
            get { lock (_gate) { return _buffer.Count; } }
        }

        internal bool IsFullyMaterialized
        {
            get { lock (_gate) { return _fullyMaterialized; } }
        }

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
            lock (_gate)
            {
                if (index < _buffer.Count)
                {
                    value = _buffer[index];
                    return true;
                }

                value = default!;
                return false;
            }
        }

        /// <summary>
        /// Tries to advance the source enumerator and cache the next element. Returns
        /// <see langword="true"/> if an element was produced.
        /// </summary>
        internal bool TryAdvance(out T value)
        {
            lock (_gate)
            {
                if (_fullyMaterialized || _sourceEnumerator == null)
                {
                    value = default!;
                    return false;
                }

                if (_sourceEnumerator.MoveNext())
                {
                    value = _sourceEnumerator.Current;
                    _buffer.Add(value);
                    return true;
                }

                _sourceEnumerator.Dispose();
                _sourceEnumerator = null;
                _fullyMaterialized = true;
                value = default!;
                return false;
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

            if (_cache.TryGetCached(next, out var cached))
            {
                _position = next;
                _current = cached;
                return true;
            }

            if (_cache.TryAdvance(out var advanced))
            {
                _position = next;
                _current = advanced;
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
