using System;
using System.Collections.Generic;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The keyed-set operators of the P0 polyfill layer: <c>DistinctBy</c>, <c>ExceptBy</c>,
    /// <c>IntersectBy</c> and <c>UnionBy</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These four are BCL operators from .NET 6 onwards, and the layer exists for the targets that
    /// predate it. The dispatch below is the four-source one the rest of the engine uses: the three
    /// contiguous arms take the engine's value-type path, and the arbitrary-sequence arm is the one
    /// that is allowed to fall back to the BCL.
    /// </para>
    /// <para>
    /// The fallback is a compile-time decision, not a run-time probe. On a target where the BCL
    /// already ships the operator the arbitrary-sequence arm hands the call straight to
    /// <c>System.Linq.Enumerable</c> and wraps the result, so the engine never re-implements
    /// semantics the platform already provides; on a target where it does not, that arm uses the
    /// engine's own implementation, which is the polyfill proper.
    /// </para>
    /// <para>
    /// A comparer overload exists wherever the BCL has one, so that the shape a caller binds
    /// against is the same shape on every target.
    /// </para>
    /// </remarks>
    public static partial class CollectionExtensions
    {
        // ----- DistinctBy -----

        /// <summary>Yields the first element of the array for every distinct key.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are deduplicated by.</typeparam>
        /// <param name="source">The array to deduplicate.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The sequence of elements with distinct keys.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        public static DistinctByValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey> DistinctBy<T, TKey>(
            this ArrayValueEnumerable<T> source, Func<T, TKey> keySelector)
            => new DistinctByValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey>(source, keySelector, null);

        /// <summary>Yields the first element of the list for every distinct key.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are deduplicated by.</typeparam>
        /// <param name="source">The list to deduplicate.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The sequence of elements with distinct keys.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        public static DistinctByValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey> DistinctBy<T, TKey>(
            this ListValueEnumerable<T> source, Func<T, TKey> keySelector)
            => new DistinctByValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey>(source, keySelector, null);

        /// <summary>Yields the first element of the read-only list for every distinct key.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are deduplicated by.</typeparam>
        /// <param name="source">The read-only list to deduplicate.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The sequence of elements with distinct keys.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        public static DistinctByValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey> DistinctBy<T, TKey>(
            this ReadOnlyListValueEnumerable<T> source, Func<T, TKey> keySelector)
            => new DistinctByValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey>(source, keySelector, null);

        /// <summary>Yields the first element of the sequence for every distinct key.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are deduplicated by.</typeparam>
        /// <param name="source">The sequence to deduplicate.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The sequence of elements with distinct keys.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> DistinctBy<T, TKey>(
            this EnumerableValueEnumerable<T> source, Func<T, TKey> keySelector)
        {
            if (keySelector is null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

#if NET6_0_OR_GREATER
            return new EnumerableValueEnumerable<T>(System.Linq.Enumerable.DistinctBy(source, keySelector));
#else
            return new EnumerableValueEnumerable<T>(
                new DistinctByValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TKey>(source, keySelector, null));
#endif
        }

        /// <summary>Yields the first element of the array for every distinct key, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are deduplicated by.</typeparam>
        /// <param name="source">The array to deduplicate.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of elements with distinct keys.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        public static DistinctByValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey> DistinctBy<T, TKey>(
            this ArrayValueEnumerable<T> source, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
            => new DistinctByValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey>(source, keySelector, comparer);

        /// <summary>Yields the first element of the list for every distinct key, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are deduplicated by.</typeparam>
        /// <param name="source">The list to deduplicate.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of elements with distinct keys.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        public static DistinctByValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey> DistinctBy<T, TKey>(
            this ListValueEnumerable<T> source, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
            => new DistinctByValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey>(source, keySelector, comparer);

        /// <summary>Yields the first element of the read-only list for every distinct key, compared
        /// with <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are deduplicated by.</typeparam>
        /// <param name="source">The read-only list to deduplicate.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of elements with distinct keys.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        public static DistinctByValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey> DistinctBy<T, TKey>(
            this ReadOnlyListValueEnumerable<T> source, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
            => new DistinctByValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey>(source, keySelector, comparer);

        /// <summary>Yields the first element of the sequence for every distinct key, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are deduplicated by.</typeparam>
        /// <param name="source">The sequence to deduplicate.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of elements with distinct keys.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> DistinctBy<T, TKey>(
            this EnumerableValueEnumerable<T> source, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            if (keySelector is null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

#if NET6_0_OR_GREATER
            return new EnumerableValueEnumerable<T>(System.Linq.Enumerable.DistinctBy(source, keySelector, comparer));
#else
            return new EnumerableValueEnumerable<T>(
                new DistinctByValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TKey>(source, keySelector, comparer));
#endif
        }

        // ----- ExceptBy -----

        /// <summary>Yields the elements of the array whose key is not in <paramref name="keys"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are filtered by.</typeparam>
        /// <param name="source">The array to filter.</param>
        /// <param name="keys">The keys to exclude.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The sequence of elements with distinct, non-excluded keys.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static ExceptByValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey> ExceptBy<T, TKey>(
            this ArrayValueEnumerable<T> source, IEnumerable<TKey> keys, Func<T, TKey> keySelector)
            => new ExceptByValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey>(source, keys, keySelector, null);

        /// <summary>Yields the elements of the list whose key is not in <paramref name="keys"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are filtered by.</typeparam>
        /// <param name="source">The list to filter.</param>
        /// <param name="keys">The keys to exclude.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The sequence of elements with distinct, non-excluded keys.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static ExceptByValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey> ExceptBy<T, TKey>(
            this ListValueEnumerable<T> source, IEnumerable<TKey> keys, Func<T, TKey> keySelector)
            => new ExceptByValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey>(source, keys, keySelector, null);

        /// <summary>Yields the elements of the read-only list whose key is not in
        /// <paramref name="keys"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are filtered by.</typeparam>
        /// <param name="source">The read-only list to filter.</param>
        /// <param name="keys">The keys to exclude.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The sequence of elements with distinct, non-excluded keys.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static ExceptByValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey> ExceptBy<T, TKey>(
            this ReadOnlyListValueEnumerable<T> source, IEnumerable<TKey> keys, Func<T, TKey> keySelector)
            => new ExceptByValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey>(source, keys, keySelector, null);

        /// <summary>Yields the elements of the sequence whose key is not in <paramref name="keys"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are filtered by.</typeparam>
        /// <param name="source">The sequence to filter.</param>
        /// <param name="keys">The keys to exclude.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The sequence of elements with distinct, non-excluded keys.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> ExceptBy<T, TKey>(
            this EnumerableValueEnumerable<T> source, IEnumerable<TKey> keys, Func<T, TKey> keySelector)
        {
            if (keys is null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            if (keySelector is null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

#if NET6_0_OR_GREATER
            return new EnumerableValueEnumerable<T>(System.Linq.Enumerable.ExceptBy(source, keys, keySelector));
#else
            return new EnumerableValueEnumerable<T>(
                new ExceptByValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TKey>(source, keys, keySelector, null));
#endif
        }

        /// <summary>Yields the elements of the array whose key is not in <paramref name="keys"/>,
        /// compared with <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are filtered by.</typeparam>
        /// <param name="source">The array to filter.</param>
        /// <param name="keys">The keys to exclude.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of elements with distinct, non-excluded keys.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static ExceptByValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey> ExceptBy<T, TKey>(
            this ArrayValueEnumerable<T> source, IEnumerable<TKey> keys, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
            => new ExceptByValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey>(source, keys, keySelector, comparer);

        /// <summary>Yields the elements of the list whose key is not in <paramref name="keys"/>,
        /// compared with <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are filtered by.</typeparam>
        /// <param name="source">The list to filter.</param>
        /// <param name="keys">The keys to exclude.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of elements with distinct, non-excluded keys.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static ExceptByValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey> ExceptBy<T, TKey>(
            this ListValueEnumerable<T> source, IEnumerable<TKey> keys, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
            => new ExceptByValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey>(source, keys, keySelector, comparer);

        /// <summary>Yields the elements of the read-only list whose key is not in
        /// <paramref name="keys"/>, compared with <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are filtered by.</typeparam>
        /// <param name="source">The read-only list to filter.</param>
        /// <param name="keys">The keys to exclude.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of elements with distinct, non-excluded keys.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static ExceptByValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey> ExceptBy<T, TKey>(
            this ReadOnlyListValueEnumerable<T> source, IEnumerable<TKey> keys, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
            => new ExceptByValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey>(source, keys, keySelector, comparer);

        /// <summary>Yields the elements of the sequence whose key is not in <paramref name="keys"/>,
        /// compared with <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are filtered by.</typeparam>
        /// <param name="source">The sequence to filter.</param>
        /// <param name="keys">The keys to exclude.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of elements with distinct, non-excluded keys.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> ExceptBy<T, TKey>(
            this EnumerableValueEnumerable<T> source, IEnumerable<TKey> keys, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            if (keys is null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            if (keySelector is null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

#if NET6_0_OR_GREATER
            return new EnumerableValueEnumerable<T>(System.Linq.Enumerable.ExceptBy(source, keys, keySelector, comparer));
#else
            return new EnumerableValueEnumerable<T>(
                new ExceptByValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TKey>(source, keys, keySelector, comparer));
#endif
        }

        // ----- IntersectBy -----

        /// <summary>Yields the elements of the array whose key is in <paramref name="keys"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are filtered by.</typeparam>
        /// <param name="source">The array to filter.</param>
        /// <param name="keys">The keys to keep.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The sequence of elements with distinct, matching keys.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static IntersectByValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey> IntersectBy<T, TKey>(
            this ArrayValueEnumerable<T> source, IEnumerable<TKey> keys, Func<T, TKey> keySelector)
            => new IntersectByValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey>(source, keys, keySelector, null);

        /// <summary>Yields the elements of the list whose key is in <paramref name="keys"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are filtered by.</typeparam>
        /// <param name="source">The list to filter.</param>
        /// <param name="keys">The keys to keep.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The sequence of elements with distinct, matching keys.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static IntersectByValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey> IntersectBy<T, TKey>(
            this ListValueEnumerable<T> source, IEnumerable<TKey> keys, Func<T, TKey> keySelector)
            => new IntersectByValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey>(source, keys, keySelector, null);

        /// <summary>Yields the elements of the read-only list whose key is in
        /// <paramref name="keys"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are filtered by.</typeparam>
        /// <param name="source">The read-only list to filter.</param>
        /// <param name="keys">The keys to keep.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The sequence of elements with distinct, matching keys.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static IntersectByValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey> IntersectBy<T, TKey>(
            this ReadOnlyListValueEnumerable<T> source, IEnumerable<TKey> keys, Func<T, TKey> keySelector)
            => new IntersectByValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey>(source, keys, keySelector, null);

        /// <summary>Yields the elements of the sequence whose key is in <paramref name="keys"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are filtered by.</typeparam>
        /// <param name="source">The sequence to filter.</param>
        /// <param name="keys">The keys to keep.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The sequence of elements with distinct, matching keys.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> IntersectBy<T, TKey>(
            this EnumerableValueEnumerable<T> source, IEnumerable<TKey> keys, Func<T, TKey> keySelector)
        {
            if (keys is null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            if (keySelector is null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

#if NET6_0_OR_GREATER
            return new EnumerableValueEnumerable<T>(System.Linq.Enumerable.IntersectBy(source, keys, keySelector));
#else
            return new EnumerableValueEnumerable<T>(
                new IntersectByValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TKey>(source, keys, keySelector, null));
#endif
        }

        /// <summary>Yields the elements of the array whose key is in <paramref name="keys"/>,
        /// compared with <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are filtered by.</typeparam>
        /// <param name="source">The array to filter.</param>
        /// <param name="keys">The keys to keep.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of elements with distinct, matching keys.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static IntersectByValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey> IntersectBy<T, TKey>(
            this ArrayValueEnumerable<T> source, IEnumerable<TKey> keys, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
            => new IntersectByValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey>(source, keys, keySelector, comparer);

        /// <summary>Yields the elements of the list whose key is in <paramref name="keys"/>,
        /// compared with <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are filtered by.</typeparam>
        /// <param name="source">The list to filter.</param>
        /// <param name="keys">The keys to keep.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of elements with distinct, matching keys.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static IntersectByValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey> IntersectBy<T, TKey>(
            this ListValueEnumerable<T> source, IEnumerable<TKey> keys, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
            => new IntersectByValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey>(source, keys, keySelector, comparer);

        /// <summary>Yields the elements of the read-only list whose key is in
        /// <paramref name="keys"/>, compared with <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are filtered by.</typeparam>
        /// <param name="source">The read-only list to filter.</param>
        /// <param name="keys">The keys to keep.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of elements with distinct, matching keys.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static IntersectByValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey> IntersectBy<T, TKey>(
            this ReadOnlyListValueEnumerable<T> source, IEnumerable<TKey> keys, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
            => new IntersectByValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey>(source, keys, keySelector, comparer);

        /// <summary>Yields the elements of the sequence whose key is in <paramref name="keys"/>,
        /// compared with <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are filtered by.</typeparam>
        /// <param name="source">The sequence to filter.</param>
        /// <param name="keys">The keys to keep.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of elements with distinct, matching keys.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> IntersectBy<T, TKey>(
            this EnumerableValueEnumerable<T> source, IEnumerable<TKey> keys, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            if (keys is null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            if (keySelector is null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

#if NET6_0_OR_GREATER
            return new EnumerableValueEnumerable<T>(System.Linq.Enumerable.IntersectBy(source, keys, keySelector, comparer));
#else
            return new EnumerableValueEnumerable<T>(
                new IntersectByValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TKey>(source, keys, keySelector, comparer));
#endif
        }

        // ----- UnionBy -----

        /// <summary>Yields the elements of the array and then those of <paramref name="second"/>,
        /// dropping elements whose key has already been produced.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are deduplicated by.</typeparam>
        /// <param name="source">The first array.</param>
        /// <param name="second">The second sequence.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The sequence of distinct elements of both sides.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static UnionByValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey> UnionBy<T, TKey>(
            this ArrayValueEnumerable<T> source, IEnumerable<T> second, Func<T, TKey> keySelector)
            => new UnionByValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey>(source, second, keySelector, null);

        /// <summary>Yields the elements of the list and then those of <paramref name="second"/>,
        /// dropping elements whose key has already been produced.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are deduplicated by.</typeparam>
        /// <param name="source">The first list.</param>
        /// <param name="second">The second sequence.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The sequence of distinct elements of both sides.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static UnionByValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey> UnionBy<T, TKey>(
            this ListValueEnumerable<T> source, IEnumerable<T> second, Func<T, TKey> keySelector)
            => new UnionByValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey>(source, second, keySelector, null);

        /// <summary>Yields the elements of the read-only list and then those of
        /// <paramref name="second"/>, dropping elements whose key has already been produced.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are deduplicated by.</typeparam>
        /// <param name="source">The first read-only list.</param>
        /// <param name="second">The second sequence.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The sequence of distinct elements of both sides.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static UnionByValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey> UnionBy<T, TKey>(
            this ReadOnlyListValueEnumerable<T> source, IEnumerable<T> second, Func<T, TKey> keySelector)
            => new UnionByValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey>(source, second, keySelector, null);

        /// <summary>Yields the elements of the sequence and then those of <paramref name="second"/>,
        /// dropping elements whose key has already been produced.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are deduplicated by.</typeparam>
        /// <param name="source">The first sequence.</param>
        /// <param name="second">The second sequence.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The sequence of distinct elements of both sides.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> UnionBy<T, TKey>(
            this EnumerableValueEnumerable<T> source, IEnumerable<T> second, Func<T, TKey> keySelector)
        {
            if (second is null)
            {
                throw new ArgumentNullException(nameof(second));
            }

            if (keySelector is null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

#if NET6_0_OR_GREATER
            return new EnumerableValueEnumerable<T>(System.Linq.Enumerable.UnionBy(source, second, keySelector));
#else
            return new EnumerableValueEnumerable<T>(
                new UnionByValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TKey>(source, second, keySelector, null));
#endif
        }

        /// <summary>Yields the elements of the array and then those of <paramref name="second"/>,
        /// dropping elements whose key has already been produced, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are deduplicated by.</typeparam>
        /// <param name="source">The first array.</param>
        /// <param name="second">The second sequence.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of distinct elements of both sides.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static UnionByValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey> UnionBy<T, TKey>(
            this ArrayValueEnumerable<T> source, IEnumerable<T> second, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
            => new UnionByValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey>(source, second, keySelector, comparer);

        /// <summary>Yields the elements of the list and then those of <paramref name="second"/>,
        /// dropping elements whose key has already been produced, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are deduplicated by.</typeparam>
        /// <param name="source">The first list.</param>
        /// <param name="second">The second sequence.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of distinct elements of both sides.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static UnionByValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey> UnionBy<T, TKey>(
            this ListValueEnumerable<T> source, IEnumerable<T> second, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
            => new UnionByValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey>(source, second, keySelector, comparer);

        /// <summary>Yields the elements of the read-only list and then those of
        /// <paramref name="second"/>, dropping elements whose key has already been produced,
        /// compared with <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are deduplicated by.</typeparam>
        /// <param name="source">The first read-only list.</param>
        /// <param name="second">The second sequence.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of distinct elements of both sides.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static UnionByValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey> UnionBy<T, TKey>(
            this ReadOnlyListValueEnumerable<T> source, IEnumerable<T> second, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
            => new UnionByValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey>(source, second, keySelector, comparer);

        /// <summary>Yields the elements of the sequence and then those of <paramref name="second"/>,
        /// dropping elements whose key has already been produced, compared with
        /// <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key the elements are deduplicated by.</typeparam>
        /// <param name="source">The first sequence.</param>
        /// <param name="second">The second sequence.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <param name="comparer">The comparer of keys, or <see langword="null"/> for the default one.</param>
        /// <returns>The sequence of distinct elements of both sides.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> UnionBy<T, TKey>(
            this EnumerableValueEnumerable<T> source, IEnumerable<T> second, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            if (second is null)
            {
                throw new ArgumentNullException(nameof(second));
            }

            if (keySelector is null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

#if NET6_0_OR_GREATER
            return new EnumerableValueEnumerable<T>(System.Linq.Enumerable.UnionBy(source, second, keySelector, comparer));
#else
            return new EnumerableValueEnumerable<T>(
                new UnionByValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TKey>(source, second, keySelector, comparer));
#endif
        }
    }
}
