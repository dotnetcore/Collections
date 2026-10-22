using System;
using System.Collections.Generic;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The P3 layer of the operator surface: side effects (<c>Do</c>), fault tolerance
    /// (<c>Catch</c> / <c>Retry</c> / <c>OnErrorResumeNext</c>), sequence factories
    /// (<c>Using</c> / <c>Defer</c> / <c>Return</c> / <c>Throw</c>), repetition
    /// (<c>Repeat</c>) and ordered merging (<c>OrderedMerge</c> / <c>SortedMerge</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only <c>Do</c> stays on the value-type enumerator path, because only <c>Do</c> is a plain
    /// per-element decoration. The rest are either reference-state machines by nature (recovery
    /// operators hold several enumerators and may rebuild them) or factories with no source at all,
    /// so their results are <see cref="EnumerableValueEnumerable{T}"/> over an internal class. The
    /// four public arms are kept for those as well: they are what lets a caller stay on whichever
    /// entry wrapper it already has instead of casting back to <see cref="IEnumerable{T}"/>. That
    /// is the same trade-off <c>Interleave</c> documents, and the boxed source is paid once per
    /// query construction, not once per element.
    /// </para>
    /// <para>
    /// <c>Return</c>, <c>Throw</c>, <c>Defer</c> and <c>Using</c> are deliberately <em>not</em>
    /// extension methods: they have no receiver, and a "factory" that extends a
    /// <see cref="Func{T}"/> or an element would put engine candidates on receiver shapes the BCL
    /// owns - the exact ambiguity the namespace governance rules exist to keep out.
    /// </para>
    /// </remarks>
    public static partial class CollectionExtensions
    {
        // ----- Do -----

        /// <summary>Runs <paramref name="action"/> on every array element as it is pulled and yields the element unchanged.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The array to tap.</param>
        /// <param name="action">The side effect to run on each element.</param>
        /// <returns>The same elements, in the same order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
        public static DoValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T> Do<T>(
            this ArrayValueEnumerable<T> source, Action<T> action)
            => new DoValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, action);

        /// <summary>Runs <paramref name="action"/> on every list element as it is pulled and yields the element unchanged.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The list to tap.</param>
        /// <param name="action">The side effect to run on each element.</param>
        /// <returns>The same elements, in the same order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
        public static DoValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T> Do<T>(
            this ListValueEnumerable<T> source, Action<T> action)
            => new DoValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, action);

        /// <summary>Runs <paramref name="action"/> on every read-only-list element as it is pulled and yields the element unchanged.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The read-only list to tap.</param>
        /// <param name="action">The side effect to run on each element.</param>
        /// <returns>The same elements, in the same order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
        public static DoValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T> Do<T>(
            this ReadOnlyListValueEnumerable<T> source, Action<T> action)
            => new DoValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, action);

        /// <summary>Runs <paramref name="action"/> on every sequence element as it is pulled and yields the element unchanged.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to tap.</param>
        /// <param name="action">The side effect to run on each element.</param>
        /// <returns>The same elements, in the same order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
        public static DoValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T> Do<T>(
            this EnumerableValueEnumerable<T> source, Action<T> action)
            => new DoValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, action);

        /// <summary>Runs <paramref name="action"/> on every array element and its position as it is pulled and yields the element unchanged.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The array to tap.</param>
        /// <param name="action">The side effect to run on each element, given the element and its
        /// zero-based position in the walked sequence.</param>
        /// <returns>The same elements, in the same order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
        public static DoValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T> Do<T>(
            this ArrayValueEnumerable<T> source, Action<T, int> action)
            => new DoValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, action);

        /// <summary>Runs <paramref name="action"/> on every list element and its position as it is pulled and yields the element unchanged.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The list to tap.</param>
        /// <param name="action">The side effect to run on each element, given the element and its
        /// zero-based position in the walked sequence.</param>
        /// <returns>The same elements, in the same order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
        public static DoValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T> Do<T>(
            this ListValueEnumerable<T> source, Action<T, int> action)
            => new DoValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, action);

        /// <summary>Runs <paramref name="action"/> on every read-only-list element and its position as it is pulled and yields the element unchanged.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The read-only list to tap.</param>
        /// <param name="action">The side effect to run on each element, given the element and its
        /// zero-based position in the walked sequence.</param>
        /// <returns>The same elements, in the same order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
        public static DoValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T> Do<T>(
            this ReadOnlyListValueEnumerable<T> source, Action<T, int> action)
            => new DoValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, action);

        /// <summary>Runs <paramref name="action"/> on every sequence element and its position as it is pulled and yields the element unchanged.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to tap.</param>
        /// <param name="action">The side effect to run on each element, given the element and its
        /// zero-based position in the walked sequence.</param>
        /// <returns>The same elements, in the same order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
        public static DoValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T> Do<T>(
            this EnumerableValueEnumerable<T> source, Action<T, int> action)
            => new DoValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, action);

        // ----- Catch -----

        /// <summary>Yields the array until it fails, then yields <paramref name="second"/> in full.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="first">The sequence to walk first.</param>
        /// <param name="second">The sequence to continue with after a failure.</param>
        /// <returns>The successful prefix of <paramref name="first"/>, followed by all of
        /// <paramref name="second"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> Catch<T>(this ArrayValueEnumerable<T> first, IEnumerable<T> second)
            => CatchCore(first, second);

        /// <summary>Yields the list until it fails, then yields <paramref name="second"/> in full.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="first">The sequence to walk first.</param>
        /// <param name="second">The sequence to continue with after a failure.</param>
        /// <returns>The successful prefix of <paramref name="first"/>, followed by all of
        /// <paramref name="second"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> Catch<T>(this ListValueEnumerable<T> first, IEnumerable<T> second)
            => CatchCore(first, second);

        /// <summary>Yields the read-only list until it fails, then yields <paramref name="second"/> in full.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="first">The sequence to walk first.</param>
        /// <param name="second">The sequence to continue with after a failure.</param>
        /// <returns>The successful prefix of <paramref name="first"/>, followed by all of
        /// <paramref name="second"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> Catch<T>(this ReadOnlyListValueEnumerable<T> first, IEnumerable<T> second)
            => CatchCore(first, second);

        /// <summary>Yields the sequence until it fails, then yields <paramref name="second"/> in full.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="first">The sequence to walk first.</param>
        /// <param name="second">The sequence to continue with after a failure.</param>
        /// <returns>The successful prefix of <paramref name="first"/>, followed by all of
        /// <paramref name="second"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> Catch<T>(this EnumerableValueEnumerable<T> first, IEnumerable<T> second)
            => CatchCore(first, second);

        /// <summary>Yields the array until it fails with <typeparamref name="TException"/>, then continues with whatever the handler answers.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TException">The exception type to recover from.</typeparam>
        /// <param name="source">The sequence to walk first.</param>
        /// <param name="handler">Receives the exception and answers the sequence to continue with;
        /// <see langword="null"/> ends the result quietly, which is how a caller swallows the
        /// failure. A different exception type propagates.</param>
        /// <returns>The successful prefix of <paramref name="source"/>, followed by the handler's
        /// sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> Catch<T, TException>(
            this ArrayValueEnumerable<T> source, Func<TException, IEnumerable<T>?> handler)
            where TException : Exception
            => CatchWithHandlerCore(source, handler);

        /// <summary>Yields the list until it fails with <typeparamref name="TException"/>, then continues with whatever the handler answers.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TException">The exception type to recover from.</typeparam>
        /// <param name="source">The sequence to walk first.</param>
        /// <param name="handler">Receives the exception and answers the sequence to continue with;
        /// <see langword="null"/> ends the result quietly. A different exception type
        /// propagates.</param>
        /// <returns>The successful prefix of <paramref name="source"/>, followed by the handler's
        /// sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> Catch<T, TException>(
            this ListValueEnumerable<T> source, Func<TException, IEnumerable<T>?> handler)
            where TException : Exception
            => CatchWithHandlerCore(source, handler);

        /// <summary>Yields the read-only list until it fails with <typeparamref name="TException"/>, then continues with whatever the handler answers.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TException">The exception type to recover from.</typeparam>
        /// <param name="source">The sequence to walk first.</param>
        /// <param name="handler">Receives the exception and answers the sequence to continue with;
        /// <see langword="null"/> ends the result quietly. A different exception type
        /// propagates.</param>
        /// <returns>The successful prefix of <paramref name="source"/>, followed by the handler's
        /// sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> Catch<T, TException>(
            this ReadOnlyListValueEnumerable<T> source, Func<TException, IEnumerable<T>?> handler)
            where TException : Exception
            => CatchWithHandlerCore(source, handler);

        /// <summary>Yields the sequence until it fails with <typeparamref name="TException"/>, then continues with whatever the handler answers.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TException">The exception type to recover from.</typeparam>
        /// <param name="source">The sequence to walk first.</param>
        /// <param name="handler">Receives the exception and answers the sequence to continue with;
        /// <see langword="null"/> ends the result quietly. A different exception type
        /// propagates.</param>
        /// <returns>The successful prefix of <paramref name="source"/>, followed by the handler's
        /// sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> Catch<T, TException>(
            this EnumerableValueEnumerable<T> source, Func<TException, IEnumerable<T>?> handler)
            where TException : Exception
            => CatchWithHandlerCore(source, handler);

        // ----- Retry -----

        /// <summary>Re-walks the array from the beginning when it fails, up to <paramref name="retryCount"/> extra times.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to walk.</param>
        /// <param name="retryCount">The number of extra attempts; <c>0</c> means the first failure
        /// propagates.</param>
        /// <returns>The sequence, with duplicate elements for every restarted attempt.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="retryCount"/> is negative.</exception>
        public static EnumerableValueEnumerable<T> Retry<T>(this ArrayValueEnumerable<T> source, int retryCount)
            => RetryCore(source, retryCount);

        /// <summary>Re-walks the list from the beginning when it fails, up to <paramref name="retryCount"/> extra times.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to walk.</param>
        /// <param name="retryCount">The number of extra attempts; <c>0</c> means the first failure
        /// propagates.</param>
        /// <returns>The sequence, with duplicate elements for every restarted attempt.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="retryCount"/> is negative.</exception>
        public static EnumerableValueEnumerable<T> Retry<T>(this ListValueEnumerable<T> source, int retryCount)
            => RetryCore(source, retryCount);

        /// <summary>Re-walks the read-only list from the beginning when it fails, up to <paramref name="retryCount"/> extra times.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to walk.</param>
        /// <param name="retryCount">The number of extra attempts; <c>0</c> means the first failure
        /// propagates.</param>
        /// <returns>The sequence, with duplicate elements for every restarted attempt.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="retryCount"/> is negative.</exception>
        public static EnumerableValueEnumerable<T> Retry<T>(this ReadOnlyListValueEnumerable<T> source, int retryCount)
            => RetryCore(source, retryCount);

        /// <summary>Re-walks the sequence from the beginning when it fails, up to <paramref name="retryCount"/> extra times.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to walk.</param>
        /// <param name="retryCount">The number of extra attempts; <c>0</c> means the first failure
        /// propagates.</param>
        /// <returns>The sequence, with duplicate elements for every restarted attempt.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="retryCount"/> is negative.</exception>
        public static EnumerableValueEnumerable<T> Retry<T>(this EnumerableValueEnumerable<T> source, int retryCount)
            => RetryCore(source, retryCount);

        // ----- OnErrorResumeNext -----

        /// <summary>Concatenates <paramref name="second"/> after the array, ignoring failures on both sides.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="first">The sequence to walk first.</param>
        /// <param name="second">The sequence to walk after it.</param>
        /// <returns>Every element of both sequences up to their first failure, in order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> OnErrorResumeNext<T>(this ArrayValueEnumerable<T> first, IEnumerable<T> second)
            => OnErrorResumeNextCore(first, second);

        /// <summary>Concatenates <paramref name="second"/> after the list, ignoring failures on both sides.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="first">The sequence to walk first.</param>
        /// <param name="second">The sequence to walk after it.</param>
        /// <returns>Every element of both sequences up to their first failure, in order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> OnErrorResumeNext<T>(this ListValueEnumerable<T> first, IEnumerable<T> second)
            => OnErrorResumeNextCore(first, second);

        /// <summary>Concatenates <paramref name="second"/> after the read-only list, ignoring failures on both sides.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="first">The sequence to walk first.</param>
        /// <param name="second">The sequence to walk after it.</param>
        /// <returns>Every element of both sequences up to their first failure, in order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> OnErrorResumeNext<T>(this ReadOnlyListValueEnumerable<T> first, IEnumerable<T> second)
            => OnErrorResumeNextCore(first, second);

        /// <summary>Concatenates <paramref name="second"/> after the sequence, ignoring failures on both sides.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="first">The sequence to walk first.</param>
        /// <param name="second">The sequence to walk after it.</param>
        /// <returns>Every element of both sequences up to their first failure, in order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> OnErrorResumeNext<T>(this EnumerableValueEnumerable<T> first, IEnumerable<T> second)
            => OnErrorResumeNextCore(first, second);

        // ----- Repeat -----

        /// <summary>Yields the whole array <paramref name="count"/> times.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to repeat.</param>
        /// <param name="count">How many rounds to produce; <c>0</c> produces nothing.</param>
        /// <returns><paramref name="count"/> copies of the sequence, one after another.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
        public static EnumerableValueEnumerable<T> Repeat<T>(this ArrayValueEnumerable<T> source, int count)
            => RepeatCore(source, count);

        /// <summary>Yields the whole list <paramref name="count"/> times.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to repeat.</param>
        /// <param name="count">How many rounds to produce; <c>0</c> produces nothing.</param>
        /// <returns><paramref name="count"/> copies of the sequence, one after another.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
        public static EnumerableValueEnumerable<T> Repeat<T>(this ListValueEnumerable<T> source, int count)
            => RepeatCore(source, count);

        /// <summary>Yields the whole read-only list <paramref name="count"/> times.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to repeat.</param>
        /// <param name="count">How many rounds to produce; <c>0</c> produces nothing.</param>
        /// <returns><paramref name="count"/> copies of the sequence, one after another.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
        public static EnumerableValueEnumerable<T> Repeat<T>(this ReadOnlyListValueEnumerable<T> source, int count)
            => RepeatCore(source, count);

        /// <summary>Yields the whole sequence <paramref name="count"/> times.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to repeat.</param>
        /// <param name="count">How many rounds to produce; <c>0</c> produces nothing.</param>
        /// <returns><paramref name="count"/> copies of the sequence, one after another.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
        public static EnumerableValueEnumerable<T> Repeat<T>(this EnumerableValueEnumerable<T> source, int count)
            => RepeatCore(source, count);

        // ----- OrderedMerge -----

        /// <summary>Merges two already-ordered arrays into one ordered sequence, streaming both sides.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="first">The first ordered sequence.</param>
        /// <param name="second">The second ordered sequence.</param>
        /// <param name="comparer">The ordering to merge by; <see langword="null"/> uses the default
        /// comparer.</param>
        /// <returns>All elements of both sequences, ordered, with ties taken from
        /// <paramref name="first"/> first.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> OrderedMerge<T>(
            this ArrayValueEnumerable<T> first, IEnumerable<T> second, IComparer<T>? comparer = null)
            => OrderedMergeCore(first, second, comparer);

        /// <summary>Merges two already-ordered lists into one ordered sequence, streaming both sides.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="first">The first ordered sequence.</param>
        /// <param name="second">The second ordered sequence.</param>
        /// <param name="comparer">The ordering to merge by; <see langword="null"/> uses the default
        /// comparer.</param>
        /// <returns>All elements of both sequences, ordered, with ties taken from
        /// <paramref name="first"/> first.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> OrderedMerge<T>(
            this ListValueEnumerable<T> first, IEnumerable<T> second, IComparer<T>? comparer = null)
            => OrderedMergeCore(first, second, comparer);

        /// <summary>Merges two already-ordered read-only lists into one ordered sequence, streaming both sides.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="first">The first ordered sequence.</param>
        /// <param name="second">The second ordered sequence.</param>
        /// <param name="comparer">The ordering to merge by; <see langword="null"/> uses the default
        /// comparer.</param>
        /// <returns>All elements of both sequences, ordered, with ties taken from
        /// <paramref name="first"/> first.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> OrderedMerge<T>(
            this ReadOnlyListValueEnumerable<T> first, IEnumerable<T> second, IComparer<T>? comparer = null)
            => OrderedMergeCore(first, second, comparer);

        /// <summary>Merges two already-ordered sequences into one ordered sequence, streaming both sides.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="first">The first ordered sequence.</param>
        /// <param name="second">The second ordered sequence.</param>
        /// <param name="comparer">The ordering to merge by; <see langword="null"/> uses the default
        /// comparer.</param>
        /// <returns>All elements of both sequences, ordered, with ties taken from
        /// <paramref name="first"/> first.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> OrderedMerge<T>(
            this EnumerableValueEnumerable<T> first, IEnumerable<T> second, IComparer<T>? comparer = null)
            => OrderedMergeCore(first, second, comparer);

        /// <summary>Merges two already-ordered arrays by a key, streaming both sides.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="first">The first ordered sequence.</param>
        /// <param name="second">The second ordered sequence.</param>
        /// <param name="keySelector">The key to order by, applied to both sides.</param>
        /// <param name="comparer">The key comparer; <see langword="null"/> uses the default
        /// comparer.</param>
        /// <returns>All elements of both sequences, ordered by key, with ties taken from
        /// <paramref name="first"/> first.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> OrderedMerge<T, TKey>(
            this ArrayValueEnumerable<T> first, IEnumerable<T> second, Func<T, TKey> keySelector, IComparer<TKey>? comparer = null)
            => OrderedMergeByKeyCore(first, second, keySelector, comparer);

        /// <summary>Merges two already-ordered lists by a key, streaming both sides.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="first">The first ordered sequence.</param>
        /// <param name="second">The second ordered sequence.</param>
        /// <param name="keySelector">The key to order by, applied to both sides.</param>
        /// <param name="comparer">The key comparer; <see langword="null"/> uses the default
        /// comparer.</param>
        /// <returns>All elements of both sequences, ordered by key, with ties taken from
        /// <paramref name="first"/> first.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> OrderedMerge<T, TKey>(
            this ListValueEnumerable<T> first, IEnumerable<T> second, Func<T, TKey> keySelector, IComparer<TKey>? comparer = null)
            => OrderedMergeByKeyCore(first, second, keySelector, comparer);

        /// <summary>Merges two already-ordered read-only lists by a key, streaming both sides.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="first">The first ordered sequence.</param>
        /// <param name="second">The second ordered sequence.</param>
        /// <param name="keySelector">The key to order by, applied to both sides.</param>
        /// <param name="comparer">The key comparer; <see langword="null"/> uses the default
        /// comparer.</param>
        /// <returns>All elements of both sequences, ordered by key, with ties taken from
        /// <paramref name="first"/> first.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> OrderedMerge<T, TKey>(
            this ReadOnlyListValueEnumerable<T> first, IEnumerable<T> second, Func<T, TKey> keySelector, IComparer<TKey>? comparer = null)
            => OrderedMergeByKeyCore(first, second, keySelector, comparer);

        /// <summary>Merges two already-ordered sequences by a key, streaming both sides.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="first">The first ordered sequence.</param>
        /// <param name="second">The second ordered sequence.</param>
        /// <param name="keySelector">The key to order by, applied to both sides.</param>
        /// <param name="comparer">The key comparer; <see langword="null"/> uses the default
        /// comparer.</param>
        /// <returns>All elements of both sequences, ordered by key, with ties taken from
        /// <paramref name="first"/> first.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> OrderedMerge<T, TKey>(
            this EnumerableValueEnumerable<T> first, IEnumerable<T> second, Func<T, TKey> keySelector, IComparer<TKey>? comparer = null)
            => OrderedMergeByKeyCore(first, second, keySelector, comparer);

        // ----- SortedMerge -----

        /// <summary>Sorts the array and <paramref name="second"/>, then merges them into one ordered sequence.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="first">The first sequence, in any order.</param>
        /// <param name="second">The second sequence, in any order.</param>
        /// <param name="comparer">The ordering to sort and merge by; <see langword="null"/> uses
        /// the default comparer.</param>
        /// <returns>All elements of both sequences, stably sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> SortedMerge<T>(
            this ArrayValueEnumerable<T> first, IEnumerable<T> second, IComparer<T>? comparer = null)
            => SortedMergeCore(first, second, comparer);

        /// <summary>Sorts the list and <paramref name="second"/>, then merges them into one ordered sequence.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="first">The first sequence, in any order.</param>
        /// <param name="second">The second sequence, in any order.</param>
        /// <param name="comparer">The ordering to sort and merge by; <see langword="null"/> uses
        /// the default comparer.</param>
        /// <returns>All elements of both sequences, stably sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> SortedMerge<T>(
            this ListValueEnumerable<T> first, IEnumerable<T> second, IComparer<T>? comparer = null)
            => SortedMergeCore(first, second, comparer);

        /// <summary>Sorts the read-only list and <paramref name="second"/>, then merges them into one ordered sequence.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="first">The first sequence, in any order.</param>
        /// <param name="second">The second sequence, in any order.</param>
        /// <param name="comparer">The ordering to sort and merge by; <see langword="null"/> uses
        /// the default comparer.</param>
        /// <returns>All elements of both sequences, stably sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> SortedMerge<T>(
            this ReadOnlyListValueEnumerable<T> first, IEnumerable<T> second, IComparer<T>? comparer = null)
            => SortedMergeCore(first, second, comparer);

        /// <summary>Sorts the sequence and <paramref name="second"/>, then merges them into one ordered sequence.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="first">The first sequence, in any order.</param>
        /// <param name="second">The second sequence, in any order.</param>
        /// <param name="comparer">The ordering to sort and merge by; <see langword="null"/> uses
        /// the default comparer.</param>
        /// <returns>All elements of both sequences, stably sorted.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="second"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> SortedMerge<T>(
            this EnumerableValueEnumerable<T> first, IEnumerable<T> second, IComparer<T>? comparer = null)
            => SortedMergeCore(first, second, comparer);

        // ----- Sequence factories (not extension methods) -----

        /// <summary>Builds a sequence of exactly one element without allocating.</summary>
        /// <typeparam name="T">The type of the element.</typeparam>
        /// <param name="value">The element to yield.</param>
        /// <returns>A sequence whose only element is <paramref name="value"/>.</returns>
        public static ReturnValueEnumerable<T> Return<T>(T value) => new ReturnValueEnumerable<T>(value);

        /// <summary>Builds a sequence that fails as soon as it is walked.</summary>
        /// <typeparam name="T">The type of the elements the sequence pretends to have.</typeparam>
        /// <param name="exception">The exception to raise; the same instance is rethrown on every
        /// attempt.</param>
        /// <returns>A sequence that never yields and raises <paramref name="exception"/> instead.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
        public static ThrowValueEnumerable<T> Throw<T>(Exception exception)
            => new ThrowValueEnumerable<T>(exception ?? throw new ArgumentNullException(nameof(exception)));

        /// <summary>Builds the sequence when it is enumerated, not when it is described.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="sequenceFactory">Creates the sequence to yield; invoked once per
        /// enumeration.</param>
        /// <returns>A sequence that calls <paramref name="sequenceFactory"/> on every enumeration.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="sequenceFactory"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> Defer<T>(Func<IEnumerable<T>> sequenceFactory)
        {
            if (sequenceFactory is null)
            {
                throw new ArgumentNullException(nameof(sequenceFactory));
            }

            return new EnumerableValueEnumerable<T>(new DeferredEnumerable<T>(sequenceFactory));
        }

        /// <summary>Releases a resource when the enumeration of the sequence built from it ends.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TResource">The type of the resource being scoped.</typeparam>
        /// <param name="resourceFactory">Creates the resource at the start of each enumeration.</param>
        /// <param name="sequenceFactory">Builds the sequence to yield from the resource.</param>
        /// <returns>A sequence that owns <typeparamref name="TResource"/> for as long as it is being
        /// enumerated.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="resourceFactory"/> or
        /// <paramref name="sequenceFactory"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<T> Using<T, TResource>(
            Func<TResource> resourceFactory, Func<TResource, IEnumerable<T>> sequenceFactory)
            where TResource : IDisposable
        {
            if (resourceFactory is null)
            {
                throw new ArgumentNullException(nameof(resourceFactory));
            }

            if (sequenceFactory is null)
            {
                throw new ArgumentNullException(nameof(sequenceFactory));
            }

            return new EnumerableValueEnumerable<T>(new UsingEnumerable<T, TResource>(resourceFactory, sequenceFactory));
        }

        // The four arms differ only in the static type of the first source, so each core takes the
        // boxed sequence and does the work once. Boxing the value enumerable here costs one
        // allocation per query construction, which is the same trade-off Interleave makes.
        private static EnumerableValueEnumerable<T> CatchCore<T>(IEnumerable<T> first, IEnumerable<T> second)
        {
            if (second is null)
            {
                throw new ArgumentNullException(nameof(second));
            }

            return new EnumerableValueEnumerable<T>(new CatchEnumerable<T>(first, second));
        }

        private static EnumerableValueEnumerable<T> CatchWithHandlerCore<T, TException>(
            IEnumerable<T> source, Func<TException, IEnumerable<T>?> handler)
            where TException : Exception
        {
            if (handler is null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            return new EnumerableValueEnumerable<T>(new CatchWithHandlerEnumerable<T, TException>(source, handler));
        }

        private static EnumerableValueEnumerable<T> RetryCore<T>(IEnumerable<T> source, int retryCount)
        {
            if (retryCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retryCount), retryCount, "The retry count cannot be negative.");
            }

            return new EnumerableValueEnumerable<T>(new RetryEnumerable<T>(source, retryCount));
        }

        private static EnumerableValueEnumerable<T> OnErrorResumeNextCore<T>(IEnumerable<T> first, IEnumerable<T> second)
        {
            if (second is null)
            {
                throw new ArgumentNullException(nameof(second));
            }

            return new EnumerableValueEnumerable<T>(new OnErrorResumeNextEnumerable<T>(first, second));
        }

        private static EnumerableValueEnumerable<T> RepeatCore<T>(IEnumerable<T> source, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "The repeat count cannot be negative.");
            }

            return new EnumerableValueEnumerable<T>(new RepeatEnumerable<T>(source, count));
        }

        private static EnumerableValueEnumerable<T> OrderedMergeCore<T>(
            IEnumerable<T> first, IEnumerable<T> second, IComparer<T>? comparer)
        {
            if (second is null)
            {
                throw new ArgumentNullException(nameof(second));
            }

            var resolved = comparer ?? Comparer<T>.Default;

            // The lambda, rather than a method group, is what composes the interface call into the
            // delegate the merge holds without a nullability mismatch on the parameter types.
            return new EnumerableValueEnumerable<T>(
                new OrderedMergeEnumerable<T>(first, second, (x, y) => resolved.Compare(x, y)));
        }

        private static EnumerableValueEnumerable<T> OrderedMergeByKeyCore<T, TKey>(
            IEnumerable<T> first, IEnumerable<T> second, Func<T, TKey> keySelector, IComparer<TKey>? comparer)
        {
            if (second is null)
            {
                throw new ArgumentNullException(nameof(second));
            }

            if (keySelector is null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            var resolved = comparer ?? Comparer<TKey>.Default;
            return new EnumerableValueEnumerable<T>(
                new OrderedMergeEnumerable<T>(
                    first,
                    second,
                    (x, y) => resolved.Compare(keySelector(x), keySelector(y))));
        }

        private static EnumerableValueEnumerable<T> SortedMergeCore<T>(
            IEnumerable<T> first, IEnumerable<T> second, IComparer<T>? comparer)
        {
            if (second is null)
            {
                throw new ArgumentNullException(nameof(second));
            }

            var resolved = comparer ?? Comparer<T>.Default;
            return new EnumerableValueEnumerable<T>(
                new SortedMergeEnumerable<T>(first, second, (x, y) => resolved.Compare(x, y)));
        }
    }
}
