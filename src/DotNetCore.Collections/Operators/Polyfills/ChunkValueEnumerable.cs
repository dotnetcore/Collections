using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The <c>Chunk</c> operator of the P0 polyfill layer. It splits the source into fixed-size
    /// arrays, so a consumer that has to work in batches does not have to build the batches itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Chunk</c> is a BCL operator from .NET 6 onwards; on the lower targets this implementation
    /// is the polyfill and on the higher targets it is the accelerated path over a contiguous
    /// source.
    /// </para>
    /// <para>
    /// Each chunk is a fresh array. The array is handed to the consumer, so it cannot come from a
    /// pool; the last chunk of a sequence that is not a multiple of the size is resized down rather
    /// than padded, which is what <c>System.Linq</c> does as well. The operator is lazy: a consumer
    /// that stops after the first chunk never reads past it.
    /// </para>
    /// </remarks>
    /// <typeparam name="TSource">The value enumerable being chunked.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public readonly struct ChunkValueEnumerable<TSource, TEnumerator, T> : IValueEnumerable<T[], ChunkValueEnumerator<TEnumerator, T>>, IValueEnumerableHooks<T[]>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly TSource _source;
        private readonly int _size;

        internal ChunkValueEnumerable(TSource source, int size)
        {
            if (size < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            _source = source;
            _size = size;
        }

        /// <summary>Returns a fresh enumerator that splits the source into arrays.</summary>
        /// <returns>An enumerator positioned before the first chunk.</returns>
        public ChunkValueEnumerator<TEnumerator, T> GetEnumerator()
            => new ChunkValueEnumerator<TEnumerator, T>(_source.GetEnumerator(), _size);

        IEnumerator<T[]> IEnumerable<T[]>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // The number of chunks is derivable only from the element count, and the elements are not
        // contiguous storage of T[], so nothing is promised up front.
        bool IValueEnumerableHooks<T[]>.TryGetNonEnumeratedCount(out int count)
        {
            count = 0;
            return false;
        }

#if NETCOREAPP3_0_OR_GREATER
        bool IValueEnumerableHooks<T[]>.TryGetSpan(out ReadOnlySpan<T[]> span)
        {
            span = default;
            return false;
        }

        bool IValueEnumerableHooks<T[]>.TryCopyTo(Span<T[]> destination, int offset) => false;
#endif
    }

    /// <summary>
    /// Fills one array at a time from the source. The last chunk is resized down to the number of
    /// elements it actually holds.
    /// </summary>
    /// <typeparam name="TEnumerator">The value-type enumerator of the source.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    public struct ChunkValueEnumerator<TEnumerator, T> : IEnumerator<T[]>
        where TEnumerator : struct, IEnumerator<T>
    {
        private TEnumerator _enumerator;
        private readonly int _size;
        private T[]? _current;

        internal ChunkValueEnumerator(TEnumerator enumerator, int size)
        {
            _enumerator = enumerator;
            _size = size;
            _current = null;
        }

        /// <summary>Gets the current chunk.</summary>
        public T[] Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _current!;
        }

        object IEnumerator.Current => _current!;

        /// <summary>Advances to the next chunk.</summary>
        /// <returns><see langword="true"/> if a chunk was produced; otherwise
        /// <see langword="false"/>.</returns>
        public bool MoveNext()
        {
            if (!_enumerator.MoveNext())
            {
                return false;
            }

            var chunk = new T[_size];
            chunk[0] = _enumerator.Current;

            var written = 1;
            while (written < chunk.Length && _enumerator.MoveNext())
            {
                chunk[written] = _enumerator.Current;
                written++;
            }

            if (written != chunk.Length)
            {
                Array.Resize(ref chunk, written);
            }

            _current = chunk;
            return true;
        }

        /// <summary>Sets the enumerator to its initial position, before the first chunk.</summary>
        public void Reset()
        {
            _enumerator.Reset();
            _current = null;
        }

        /// <summary>Releases the source enumerator.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => _enumerator.Dispose();
    }
}
