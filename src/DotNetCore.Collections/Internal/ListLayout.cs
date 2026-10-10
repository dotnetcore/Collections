using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
#if NET5_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace DotNetCore.Collections.Internal
{
    /// <summary>
    /// Mirrors the leading field of <see cref="List{T}"/>. The first field has been
    /// <c>T[] _items</c> on every supported runtime (.NET Framework 4.x and .NET Core / .NET 5+),
    /// so a single view is correct across the whole target matrix. The view is only ever read
    /// from, never written to, and the instance it points at stays rooted through the original
    /// reference. Later fields (<c>_size</c>, <c>_version</c>) are deliberately not mirrored:
    /// the count is read through <see cref="List{T}.Count"/>, which is already a direct field
    /// read when the static type is <see cref="List{T}"/>.
    /// </summary>
    /// <typeparam name="T">The element type of the list.</typeparam>
    internal sealed class ListLayout<T>
    {
        /// <summary>The backing store, mirroring <c>List&lt;T&gt;._items</c>. Its length is the
        /// capacity, which is greater than or equal to the number of live elements.</summary>
        public T[] Items = null!;
    }

    /// <summary>
    /// Reaches the storage that <see cref="List{T}"/> keeps private, so the four-source
    /// dispatch can index a list without going through an interface call.
    /// </summary>
    internal static class ListLayoutAccessor
    {
        /// <summary>
        /// Returns the backing array of <paramref name="list"/> without copying. Its length is
        /// the capacity, so callers must bound their walk by <see cref="List{T}.Count"/>.
        /// </summary>
        /// <typeparam name="T">The element type of the list.</typeparam>
        /// <param name="list">The list whose storage is required.</param>
        /// <returns>The backing array of the list.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T[] GetItems<T>(List<T> list) => Unsafe.As<List<T>, ListLayout<T>>(ref list).Items;

        /// <summary>
        /// Returns the live elements of <paramref name="list"/> as a read-only span. On net5.0
        /// and above this is the sanctioned <c>CollectionsMarshal.AsSpan</c>
        /// view; on the lower targets it is built over the backing array reached through the
        /// field layout. The span is internal because the public span-shaped hook belongs to the
        /// three-hook protocol of the next work item.
        /// </summary>
        /// <typeparam name="T">The element type of the list.</typeparam>
        /// <param name="list">The list to expose as a span.</param>
        /// <returns>A read-only span over the live elements of the list.</returns>
        internal static ReadOnlySpan<T> AsReadOnlySpan<T>(List<T> list)
        {
#if NET5_0_OR_GREATER
            return CollectionsMarshal.AsSpan(list);
#else
            return new ReadOnlySpan<T>(GetItems(list), 0, list.Count);
#endif
        }
    }
}
