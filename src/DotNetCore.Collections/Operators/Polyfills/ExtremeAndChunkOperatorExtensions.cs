using System;
using System.Collections.Generic;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The extreme-key and chunking operators of the P0 polyfill layer: <c>MaxBy</c>, <c>MinBy</c>
    /// and <c>Chunk</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>MaxBy</c> and <c>MinBy</c> are terminals - they answer with one element rather than a
    /// sequence - and they are BCL operators from .NET 6 onwards, like the rest of the layer. Their
    /// empty-source contract is the BCL's: <see langword="default"/> when the element type admits
    /// <see langword="null"/>, and <see cref="InvalidOperationException"/> when it does not.
    /// </para>
    /// <para>
    /// <c>Chunk</c> is lazy, so it is a value-type enumerable rather than a terminal, and its size
    /// is validated when the pipeline is built, which is where the BCL validates it too.
    /// </para>
    /// </remarks>
    public static partial class CollectionExtensions
    {
        // ----- MaxBy -----

        /// <summary>Returns the element of the array with the greatest key.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are ranked by.</typeparam>
        /// <param name="source">The array to search.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The element with the greatest key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The array is empty and the element type
        /// cannot be <see langword="null"/>.</exception>
        public static T MaxBy<T, TKey>(this ArrayValueEnumerable<T> source, Func<T, TKey> keySelector)
            => PolyfillTerminals.MaxBy<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey>(source, keySelector, null);

        /// <summary>Returns the element of the list with the greatest key.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are ranked by.</typeparam>
        /// <param name="source">The list to search.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The element with the greatest key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The list is empty and the element type
        /// cannot be <see langword="null"/>.</exception>
        public static T MaxBy<T, TKey>(this ListValueEnumerable<T> source, Func<T, TKey> keySelector)
            => PolyfillTerminals.MaxBy<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey>(source, keySelector, null);

        /// <summary>Returns the element of the read-only list with the greatest key.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are ranked by.</typeparam>
        /// <param name="source">The read-only list to search.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The element with the greatest key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The read-only list is empty and the element
        /// type cannot be <see langword="null"/>.</exception>
        public static T MaxBy<T, TKey>(this ReadOnlyListValueEnumerable<T> source, Func<T, TKey> keySelector)
            => PolyfillTerminals.MaxBy<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey>(source, keySelector, null);

        /// <summary>Returns the element of the sequence with the greatest key.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are ranked by.</typeparam>
        /// <param name="source">The sequence to search.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The element with the greatest key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The sequence is empty and the element type
        /// cannot be <see langword="null"/>.</exception>
        public static T MaxBy<T, TKey>(this EnumerableValueEnumerable<T> source, Func<T, TKey> keySelector)
        {
            if (keySelector is null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

#if NET6_0_OR_GREATER
            return System.Linq.Enumerable.MaxBy(source, keySelector)!;
#else
            return PolyfillTerminals.MaxBy<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TKey>(source, keySelector, null);
#endif
        }

        /// <summary>Returns the element of the array with the greatest key, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are ranked by.</typeparam>
        /// <param name="source">The array to search.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The element with the greatest key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The array is empty and the element type
        /// cannot be <see langword="null"/>.</exception>
        public static T MaxBy<T, TKey>(this ArrayValueEnumerable<T> source, Func<T, TKey> keySelector, IComparer<TKey>? comparer)
            => PolyfillTerminals.MaxBy<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey>(source, keySelector, comparer);

        /// <summary>Returns the element of the list with the greatest key, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are ranked by.</typeparam>
        /// <param name="source">The list to search.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The element with the greatest key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The list is empty and the element type
        /// cannot be <see langword="null"/>.</exception>
        public static T MaxBy<T, TKey>(this ListValueEnumerable<T> source, Func<T, TKey> keySelector, IComparer<TKey>? comparer)
            => PolyfillTerminals.MaxBy<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey>(source, keySelector, comparer);

        /// <summary>Returns the element of the read-only list with the greatest key, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are ranked by.</typeparam>
        /// <param name="source">The read-only list to search.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The element with the greatest key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The read-only list is empty and the element
        /// type cannot be <see langword="null"/>.</exception>
        public static T MaxBy<T, TKey>(this ReadOnlyListValueEnumerable<T> source, Func<T, TKey> keySelector, IComparer<TKey>? comparer)
            => PolyfillTerminals.MaxBy<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey>(source, keySelector, comparer);

        /// <summary>Returns the element of the sequence with the greatest key, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are ranked by.</typeparam>
        /// <param name="source">The sequence to search.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The element with the greatest key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The sequence is empty and the element type
        /// cannot be <see langword="null"/>.</exception>
        public static T MaxBy<T, TKey>(this EnumerableValueEnumerable<T> source, Func<T, TKey> keySelector, IComparer<TKey>? comparer)
        {
            if (keySelector is null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

#if NET6_0_OR_GREATER
            return System.Linq.Enumerable.MaxBy(source, keySelector, comparer)!;
#else
            return PolyfillTerminals.MaxBy<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TKey>(source, keySelector, comparer);
#endif
        }

        // ----- MinBy -----

        /// <summary>Returns the element of the array with the smallest key.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are ranked by.</typeparam>
        /// <param name="source">The array to search.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The element with the smallest key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The array is empty and the element type
        /// cannot be <see langword="null"/>.</exception>
        public static T MinBy<T, TKey>(this ArrayValueEnumerable<T> source, Func<T, TKey> keySelector)
            => PolyfillTerminals.MinBy<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey>(source, keySelector, null);

        /// <summary>Returns the element of the list with the smallest key.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are ranked by.</typeparam>
        /// <param name="source">The list to search.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The element with the smallest key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The list is empty and the element type
        /// cannot be <see langword="null"/>.</exception>
        public static T MinBy<T, TKey>(this ListValueEnumerable<T> source, Func<T, TKey> keySelector)
            => PolyfillTerminals.MinBy<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey>(source, keySelector, null);

        /// <summary>Returns the element of the read-only list with the smallest key.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are ranked by.</typeparam>
        /// <param name="source">The read-only list to search.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The element with the smallest key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The read-only list is empty and the element
        /// type cannot be <see langword="null"/>.</exception>
        public static T MinBy<T, TKey>(this ReadOnlyListValueEnumerable<T> source, Func<T, TKey> keySelector)
            => PolyfillTerminals.MinBy<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey>(source, keySelector, null);

        /// <summary>Returns the element of the sequence with the smallest key.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are ranked by.</typeparam>
        /// <param name="source">The sequence to search.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The element with the smallest key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The sequence is empty and the element type
        /// cannot be <see langword="null"/>.</exception>
        public static T MinBy<T, TKey>(this EnumerableValueEnumerable<T> source, Func<T, TKey> keySelector)
        {
            if (keySelector is null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

#if NET6_0_OR_GREATER
            return System.Linq.Enumerable.MinBy(source, keySelector)!;
#else
            return PolyfillTerminals.MinBy<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TKey>(source, keySelector, null);
#endif
        }

        /// <summary>Returns the element of the array with the smallest key, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are ranked by.</typeparam>
        /// <param name="source">The array to search.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The element with the smallest key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The array is empty and the element type
        /// cannot be <see langword="null"/>.</exception>
        public static T MinBy<T, TKey>(this ArrayValueEnumerable<T> source, Func<T, TKey> keySelector, IComparer<TKey>? comparer)
            => PolyfillTerminals.MinBy<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey>(source, keySelector, comparer);

        /// <summary>Returns the element of the list with the smallest key, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are ranked by.</typeparam>
        /// <param name="source">The list to search.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The element with the smallest key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The list is empty and the element type
        /// cannot be <see langword="null"/>.</exception>
        public static T MinBy<T, TKey>(this ListValueEnumerable<T> source, Func<T, TKey> keySelector, IComparer<TKey>? comparer)
            => PolyfillTerminals.MinBy<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey>(source, keySelector, comparer);

        /// <summary>Returns the element of the read-only list with the smallest key, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are ranked by.</typeparam>
        /// <param name="source">The read-only list to search.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The element with the smallest key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The read-only list is empty and the element
        /// type cannot be <see langword="null"/>.</exception>
        public static T MinBy<T, TKey>(this ReadOnlyListValueEnumerable<T> source, Func<T, TKey> keySelector, IComparer<TKey>? comparer)
            => PolyfillTerminals.MinBy<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey>(source, keySelector, comparer);

        /// <summary>Returns the element of the sequence with the smallest key, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are ranked by.</typeparam>
        /// <param name="source">The sequence to search.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The element with the smallest key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">The sequence is empty and the element type
        /// cannot be <see langword="null"/>.</exception>
        public static T MinBy<T, TKey>(this EnumerableValueEnumerable<T> source, Func<T, TKey> keySelector, IComparer<TKey>? comparer)
        {
            if (keySelector is null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

#if NET6_0_OR_GREATER
            return System.Linq.Enumerable.MinBy(source, keySelector, comparer)!;
#else
            return PolyfillTerminals.MinBy<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TKey>(source, keySelector, comparer);
#endif
        }

        // ----- Chunk -----

        /// <summary>Splits the array into arrays of <paramref name="size"/> elements.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The array to split.</param>
        /// <param name="size">The maximum number of elements in a chunk.</param>
        /// <returns>The sequence of chunks; the last one may be shorter.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is less than one.</exception>
        public static ChunkValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T> Chunk<T>(
            this ArrayValueEnumerable<T> source, int size)
            => new ChunkValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, size);

        /// <summary>Splits the list into arrays of <paramref name="size"/> elements.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The list to split.</param>
        /// <param name="size">The maximum number of elements in a chunk.</param>
        /// <returns>The sequence of chunks; the last one may be shorter.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is less than one.</exception>
        public static ChunkValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T> Chunk<T>(
            this ListValueEnumerable<T> source, int size)
            => new ChunkValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, size);

        /// <summary>Splits the read-only list into arrays of <paramref name="size"/> elements.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The read-only list to split.</param>
        /// <param name="size">The maximum number of elements in a chunk.</param>
        /// <returns>The sequence of chunks; the last one may be shorter.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is less than one.</exception>
        public static ChunkValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T> Chunk<T>(
            this ReadOnlyListValueEnumerable<T> source, int size)
            => new ChunkValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, size);

        /// <summary>Splits the sequence into arrays of <paramref name="size"/> elements.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to split.</param>
        /// <param name="size">The maximum number of elements in a chunk.</param>
        /// <returns>The sequence of chunks; the last one may be shorter.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is less than one.</exception>
        public static EnumerableValueEnumerable<T[]> Chunk<T>(
            this EnumerableValueEnumerable<T> source, int size)
        {
            if (size < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

#if NET6_0_OR_GREATER
            return new EnumerableValueEnumerable<T[]>(System.Linq.Enumerable.Chunk(source, size));
#else
            return new EnumerableValueEnumerable<T[]>(
                new ChunkValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, size));
#endif
        }
    }
}
