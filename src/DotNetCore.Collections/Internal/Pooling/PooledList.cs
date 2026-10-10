using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace DotNetCore.Collections.Internal
{
    /// <summary>
    /// A growable buffer that rents its storage from <see cref="ArrayPool{T}.Shared"/> and hands it
    /// back on <see cref="Dispose"/>. It is the materialisation target for a value enumerable: the
    /// engine's producers can fill a pooled buffer instead of growing a <see cref="System.Collections.Generic.List{T}"/>,
    /// which is what the grouping work in <c>DotNetCore.Collections.Multi</c> and the page buffers
    /// in <c>DotNetCore.Collections.Paginable</c> need.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a class rather than a struct on purpose. A mutable struct that owns a rented array
    /// must be disposed exactly once, and a copy of a struct silently yields two owners for one
    /// array; the pool contract does not survive that mistake. The single allocation for the
    /// wrapper is cheap next to the buffer it manages, and it does not appear on the zero-allocation
    /// hot path - that path is the enumerator walk, not materialisation.
    /// </para>
    /// <para>
    /// <see cref="Dispose"/> is idempotent and safe on an instance that never rented anything.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of the elements held by the buffer.</typeparam>
    internal sealed class PooledList<T> : IDisposable
    {
        private const int MinimumRentedCapacity = 4;

        // net451 has no Array.Empty<T>() (it arrived in .NET 4.6), so the empty case gets a shared
        // zero-length array of its own. Its length is the signal that nothing was rented, so it is
        // never handed back to the pool.
        private static readonly T[] Empty = new T[0];

        // Value-type buffers need no clearing on return; buffers that can hold references do.
        // RuntimeHelpers.IsReferenceOrContainsReferences is .NET Standard 2.1 / .NET Core 2.0 and
        // up, so the lower targets take the safe answer instead of the fast one.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        private static readonly bool ClearOnReturn = RuntimeHelpers.IsReferenceOrContainsReferences<T>();
#else
        private static readonly bool ClearOnReturn = true;
#endif

        private T[] _buffer;
        private int _count;

        /// <summary>Creates an empty buffer that rents nothing until the first element arrives.</summary>
        internal PooledList()
        {
            _buffer = Empty;
            _count = 0;
        }

        /// <summary>Creates an empty buffer with room for <paramref name="capacity"/> elements.</summary>
        /// <param name="capacity">The number of elements to reserve room for.</param>
        internal PooledList(int capacity)
        {
            _buffer = capacity <= 0 ? Empty : ArrayPool<T>.Shared.Rent(capacity);
            _count = 0;
        }

        /// <summary>Gets the number of elements written so far.</summary>
        internal int Count => _count;

        /// <summary>Gets the element at <paramref name="index"/>.</summary>
        /// <param name="index">The index of the element to read.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the
        /// written elements.</exception>
        internal T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return _buffer[index];
            }
        }

        /// <summary>Appends <paramref name="item"/>, growing the rented buffer when it is full.</summary>
        /// <param name="item">The element to append.</param>
        internal void Add(T item)
        {
            if (_count == _buffer.Length)
            {
                EnsureCapacity(_buffer.Length == 0 ? MinimumRentedCapacity : _buffer.Length * 2);
            }

            _buffer[_count] = item;
            _count++;
        }

        /// <summary>Appends a contiguous block in one step.</summary>
        /// <param name="items">The elements to append.</param>
        internal void AddRange(ReadOnlySpan<T> items)
        {
            if (items.Length == 0)
            {
                return;
            }

            EnsureCapacity(_count + items.Length);

            var destination = new Span<T>(_buffer, _count, items.Length);
            for (var i = 0; i < items.Length; i++)
            {
                destination[i] = items[i];
            }

            _count += items.Length;
        }

        /// <summary>Exposes the written elements as one contiguous block.</summary>
        /// <returns>A read-only view over the elements written so far.</returns>
        internal ReadOnlySpan<T> AsSpan() => new ReadOnlySpan<T>(_buffer, 0, _count);

        /// <summary>Copies the written elements into a right-sized array.</summary>
        /// <returns>A new array holding exactly the written elements.</returns>
        internal T[] ToArray()
        {
            var result = new T[_count];
            Array.Copy(_buffer, 0, result, 0, _count);
            return result;
        }

        /// <summary>Returns the rented storage to the pool. Safe to call more than once.</summary>
        public void Dispose()
        {
            var buffer = _buffer;
            _buffer = Empty;
            _count = 0;

            if (buffer.Length != 0)
            {
                ArrayPool<T>.Shared.Return(buffer, ClearOnReturn);
            }
        }

        private void EnsureCapacity(int capacity)
        {
            if (capacity <= _buffer.Length)
            {
                return;
            }

            var rented = ArrayPool<T>.Shared.Rent(capacity);
            Array.Copy(_buffer, 0, rented, 0, _count);

            var previous = _buffer;
            _buffer = rented;

            if (previous.Length != 0)
            {
                ArrayPool<T>.Shared.Return(previous, ClearOnReturn);
            }
        }
    }
}
