using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The windowing, grouping, splitting, time-series and combination operators of the P2 layer:
    /// <c>Window</c>, <c>WindowLeft</c>, <c>GroupAdjacent</c>, <c>Split</c>, <c>Lag</c>,
    /// <c>Lead</c>, <c>ZipLongest</c> and <c>Interleave</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every operator here is engine-only: none of them exists in any TFM of the BCL, so unlike the
    /// P0 polyfill layer there is no high-target arm to forward to. The four-source dispatch is
    /// still the same, because the point of it is the value-type enumerator, not the forwarding.
    /// </para>
    /// <para>
    /// The three arms over contiguous storage return a value enumerable whose enumerator is a
    /// struct; the arbitrary-sequence arm wraps its result in <see cref="EnumerableValueEnumerable{T}"/>,
    /// because an <see cref="IEnumerable{T}"/> source can only be walked through an interface.
    /// </para>
    /// </remarks>
    public static partial class CollectionExtensions
    {
        // ----- Window -----

        /// <summary>Slides a window of <paramref name="size"/> elements over the array and yields one array per complete window.</summary>
        public static WindowValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T> Window<T>(
            this ArrayValueEnumerable<T> source, int size)
            => new WindowValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, size);

        /// <summary>Slides a window of <paramref name="size"/> elements over the list and yields one array per complete window.</summary>
        public static WindowValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T> Window<T>(
            this ListValueEnumerable<T> source, int size)
            => new WindowValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, size);

        /// <summary>Slides a window of <paramref name="size"/> elements over the read-only list and yields one array per complete window.</summary>
        public static WindowValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T> Window<T>(
            this ReadOnlyListValueEnumerable<T> source, int size)
            => new WindowValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, size);

        /// <summary>Slides a window of <paramref name="size"/> elements over the sequence and yields one array per complete window.</summary>
        public static EnumerableValueEnumerable<T[]> Window<T>(
            this EnumerableValueEnumerable<T> source, int size)
            => new EnumerableValueEnumerable<T[]>(
                new WindowValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, size));

        // ----- WindowLeft -----

        /// <summary>Yields one left-aligned window of <paramref name="size"/> elements per array element, padding the tail with <see langword="default"/>.</summary>
        public static WindowLeftValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T> WindowLeft<T>(
            this ArrayValueEnumerable<T> source, int size)
            => new WindowLeftValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, size);

        /// <summary>Yields one left-aligned window of <paramref name="size"/> elements per list element, padding the tail with <see langword="default"/>.</summary>
        public static WindowLeftValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T> WindowLeft<T>(
            this ListValueEnumerable<T> source, int size)
            => new WindowLeftValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, size);

        /// <summary>Yields one left-aligned window of <paramref name="size"/> elements per read-only-list element, padding the tail with <see langword="default"/>.</summary>
        public static WindowLeftValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T> WindowLeft<T>(
            this ReadOnlyListValueEnumerable<T> source, int size)
            => new WindowLeftValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, size);

        /// <summary>Yields one left-aligned window of <paramref name="size"/> elements per sequence element, padding the tail with <see langword="default"/>.</summary>
        public static EnumerableValueEnumerable<T[]> WindowLeft<T>(
            this EnumerableValueEnumerable<T> source, int size)
            => new EnumerableValueEnumerable<T[]>(
                new WindowLeftValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, size));

        // ----- GroupAdjacent -----

        /// <summary>Groups runs of adjacent array elements that share a key.</summary>
        public static GroupAdjacentValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey> GroupAdjacent<T, TKey>(
            this ArrayValueEnumerable<T> source, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer = null)
            => new GroupAdjacentValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TKey>(source, keySelector, comparer);

        /// <summary>Groups runs of adjacent list elements that share a key.</summary>
        public static GroupAdjacentValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey> GroupAdjacent<T, TKey>(
            this ListValueEnumerable<T> source, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer = null)
            => new GroupAdjacentValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TKey>(source, keySelector, comparer);

        /// <summary>Groups runs of adjacent read-only-list elements that share a key.</summary>
        public static GroupAdjacentValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey> GroupAdjacent<T, TKey>(
            this ReadOnlyListValueEnumerable<T> source, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer = null)
            => new GroupAdjacentValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TKey>(source, keySelector, comparer);

        /// <summary>Groups runs of adjacent sequence elements that share a key.</summary>
        public static EnumerableValueEnumerable<IGrouping<TKey, T>> GroupAdjacent<T, TKey>(
            this EnumerableValueEnumerable<T> source, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer = null)
            => new EnumerableValueEnumerable<IGrouping<TKey, T>>(
                new GroupAdjacentValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TKey>(source, keySelector, comparer));

        // ----- Split (by separator value) -----

        /// <summary>Cuts the array wherever <paramref name="separator"/> occurs and yields the segments between occurrences.</summary>
        public static SplitValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T> Split<T>(
            this ArrayValueEnumerable<T> source, T separator, IEqualityComparer<T>? comparer = null)
            => new SplitValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, separator, comparer);

        /// <summary>Cuts the list wherever <paramref name="separator"/> occurs and yields the segments between occurrences.</summary>
        public static SplitValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T> Split<T>(
            this ListValueEnumerable<T> source, T separator, IEqualityComparer<T>? comparer = null)
            => new SplitValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, separator, comparer);

        /// <summary>Cuts the read-only list wherever <paramref name="separator"/> occurs and yields the segments between occurrences.</summary>
        public static SplitValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T> Split<T>(
            this ReadOnlyListValueEnumerable<T> source, T separator, IEqualityComparer<T>? comparer = null)
            => new SplitValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, separator, comparer);

        /// <summary>Cuts the sequence wherever <paramref name="separator"/> occurs and yields the segments between occurrences.</summary>
        public static EnumerableValueEnumerable<T[]> Split<T>(
            this EnumerableValueEnumerable<T> source, T separator, IEqualityComparer<T>? comparer = null)
            => new EnumerableValueEnumerable<T[]>(
                new SplitValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, separator, comparer));

        // ----- Split (by separator predicate) -----

        /// <summary>Cuts the array wherever <paramref name="separatorPredicate"/> answers <see langword="true"/>.</summary>
        public static SplitValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T> Split<T>(
            this ArrayValueEnumerable<T> source, Func<T, bool> separatorPredicate)
            => new SplitValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, separatorPredicate);

        /// <summary>Cuts the list wherever <paramref name="separatorPredicate"/> answers <see langword="true"/>.</summary>
        public static SplitValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T> Split<T>(
            this ListValueEnumerable<T> source, Func<T, bool> separatorPredicate)
            => new SplitValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, separatorPredicate);

        /// <summary>Cuts the read-only list wherever <paramref name="separatorPredicate"/> answers <see langword="true"/>.</summary>
        public static SplitValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T> Split<T>(
            this ReadOnlyListValueEnumerable<T> source, Func<T, bool> separatorPredicate)
            => new SplitValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, separatorPredicate);

        /// <summary>Cuts the sequence wherever <paramref name="separatorPredicate"/> answers <see langword="true"/>.</summary>
        public static EnumerableValueEnumerable<T[]> Split<T>(
            this EnumerableValueEnumerable<T> source, Func<T, bool> separatorPredicate)
            => new EnumerableValueEnumerable<T[]>(
                new SplitValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, separatorPredicate));

        // ----- Lag -----

        /// <summary>Pairs every array element with the one immediately before it.</summary>
        public static LagValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TResult> Lag<T, TResult>(
            this ArrayValueEnumerable<T> source, Func<T, T, TResult> resultSelector)
            => new LagValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TResult>(source, 1, resultSelector);

        /// <summary>Pairs every array element with the one <paramref name="offset"/> positions before it.</summary>
        public static LagValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TResult> Lag<T, TResult>(
            this ArrayValueEnumerable<T> source, int offset, Func<T, T, TResult> resultSelector)
            => new LagValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TResult>(source, offset, resultSelector);

        /// <summary>Pairs every list element with the one immediately before it.</summary>
        public static LagValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TResult> Lag<T, TResult>(
            this ListValueEnumerable<T> source, Func<T, T, TResult> resultSelector)
            => new LagValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TResult>(source, 1, resultSelector);

        /// <summary>Pairs every list element with the one <paramref name="offset"/> positions before it.</summary>
        public static LagValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TResult> Lag<T, TResult>(
            this ListValueEnumerable<T> source, int offset, Func<T, T, TResult> resultSelector)
            => new LagValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TResult>(source, offset, resultSelector);

        /// <summary>Pairs every read-only-list element with the one immediately before it.</summary>
        public static LagValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TResult> Lag<T, TResult>(
            this ReadOnlyListValueEnumerable<T> source, Func<T, T, TResult> resultSelector)
            => new LagValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TResult>(source, 1, resultSelector);

        /// <summary>Pairs every read-only-list element with the one <paramref name="offset"/> positions before it.</summary>
        public static LagValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TResult> Lag<T, TResult>(
            this ReadOnlyListValueEnumerable<T> source, int offset, Func<T, T, TResult> resultSelector)
            => new LagValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TResult>(source, offset, resultSelector);

        /// <summary>Pairs every sequence element with the one immediately before it.</summary>
        public static EnumerableValueEnumerable<TResult> Lag<T, TResult>(
            this EnumerableValueEnumerable<T> source, Func<T, T, TResult> resultSelector)
            => new EnumerableValueEnumerable<TResult>(
                new LagValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TResult>(source, 1, resultSelector));

        /// <summary>Pairs every sequence element with the one <paramref name="offset"/> positions before it.</summary>
        public static EnumerableValueEnumerable<TResult> Lag<T, TResult>(
            this EnumerableValueEnumerable<T> source, int offset, Func<T, T, TResult> resultSelector)
            => new EnumerableValueEnumerable<TResult>(
                new LagValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TResult>(source, offset, resultSelector));

        // ----- Lead -----

        /// <summary>Pairs every array element with the one immediately after it.</summary>
        public static LeadValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TResult> Lead<T, TResult>(
            this ArrayValueEnumerable<T> source, Func<T, T, TResult> resultSelector)
            => new LeadValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TResult>(source, 1, resultSelector);

        /// <summary>Pairs every array element with the one <paramref name="offset"/> positions after it.</summary>
        public static LeadValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TResult> Lead<T, TResult>(
            this ArrayValueEnumerable<T> source, int offset, Func<T, T, TResult> resultSelector)
            => new LeadValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TResult>(source, offset, resultSelector);

        /// <summary>Pairs every list element with the one immediately after it.</summary>
        public static LeadValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TResult> Lead<T, TResult>(
            this ListValueEnumerable<T> source, Func<T, T, TResult> resultSelector)
            => new LeadValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TResult>(source, 1, resultSelector);

        /// <summary>Pairs every list element with the one <paramref name="offset"/> positions after it.</summary>
        public static LeadValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TResult> Lead<T, TResult>(
            this ListValueEnumerable<T> source, int offset, Func<T, T, TResult> resultSelector)
            => new LeadValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TResult>(source, offset, resultSelector);

        /// <summary>Pairs every read-only-list element with the one immediately after it.</summary>
        public static LeadValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TResult> Lead<T, TResult>(
            this ReadOnlyListValueEnumerable<T> source, Func<T, T, TResult> resultSelector)
            => new LeadValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TResult>(source, 1, resultSelector);

        /// <summary>Pairs every read-only-list element with the one <paramref name="offset"/> positions after it.</summary>
        public static LeadValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TResult> Lead<T, TResult>(
            this ReadOnlyListValueEnumerable<T> source, int offset, Func<T, T, TResult> resultSelector)
            => new LeadValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TResult>(source, offset, resultSelector);

        /// <summary>Pairs every sequence element with the one immediately after it.</summary>
        public static EnumerableValueEnumerable<TResult> Lead<T, TResult>(
            this EnumerableValueEnumerable<T> source, Func<T, T, TResult> resultSelector)
            => new EnumerableValueEnumerable<TResult>(
                new LeadValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TResult>(source, 1, resultSelector));

        /// <summary>Pairs every sequence element with the one <paramref name="offset"/> positions after it.</summary>
        public static EnumerableValueEnumerable<TResult> Lead<T, TResult>(
            this EnumerableValueEnumerable<T> source, int offset, Func<T, T, TResult> resultSelector)
            => new EnumerableValueEnumerable<TResult>(
                new LeadValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TResult>(source, offset, resultSelector));

        // ----- ZipLongest -----

        /// <summary>Zips the array with <paramref name="second"/> to the longer of the two, substituting <see langword="default"/> for the shorter side.</summary>
        public static ZipLongestValueEnumerable<TFirst, ArrayValueEnumerator<TFirst>, TSecond, TResult> ZipLongest<TFirst, TSecond, TResult>(
            this ArrayValueEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
            => new ZipLongestValueEnumerable<TFirst, ArrayValueEnumerator<TFirst>, TSecond, TResult>(first.GetEnumerator(), second, resultSelector);

        /// <summary>Zips the list with <paramref name="second"/> to the longer of the two, substituting <see langword="default"/> for the shorter side.</summary>
        public static ZipLongestValueEnumerable<TFirst, ListValueEnumerator<TFirst>, TSecond, TResult> ZipLongest<TFirst, TSecond, TResult>(
            this ListValueEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
            => new ZipLongestValueEnumerable<TFirst, ListValueEnumerator<TFirst>, TSecond, TResult>(first.GetEnumerator(), second, resultSelector);

        /// <summary>Zips the read-only list with <paramref name="second"/> to the longer of the two, substituting <see langword="default"/> for the shorter side.</summary>
        public static ZipLongestValueEnumerable<TFirst, ReadOnlyListValueEnumerator<TFirst>, TSecond, TResult> ZipLongest<TFirst, TSecond, TResult>(
            this ReadOnlyListValueEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
            => new ZipLongestValueEnumerable<TFirst, ReadOnlyListValueEnumerator<TFirst>, TSecond, TResult>(first.GetEnumerator(), second, resultSelector);

        /// <summary>Zips the sequence with <paramref name="second"/> to the longer of the two, substituting <see langword="default"/> for the shorter side.</summary>
        public static EnumerableValueEnumerable<TResult> ZipLongest<TFirst, TSecond, TResult>(
            this EnumerableValueEnumerable<TFirst> first, IEnumerable<TSecond> second, Func<TFirst, TSecond, TResult> resultSelector)
            => new EnumerableValueEnumerable<TResult>(
                new ZipLongestValueEnumerable<TFirst, EnumerableValueEnumerator<TFirst>, TSecond, TResult>(first.GetEnumerator(), second, resultSelector));

        // ----- Interleave -----

        /// <summary>Walks the array and then each of <paramref name="others"/> in turn, one element per round.</summary>
        public static EnumerableValueEnumerable<T> Interleave<T>(
            this ArrayValueEnumerable<T> source, params IEnumerable<T>[] others)
            => new EnumerableValueEnumerable<T>(InterleaveCore(source, others));

        /// <summary>Walks the list and then each of <paramref name="others"/> in turn, one element per round.</summary>
        public static EnumerableValueEnumerable<T> Interleave<T>(
            this ListValueEnumerable<T> source, params IEnumerable<T>[] others)
            => new EnumerableValueEnumerable<T>(InterleaveCore(source, others));

        /// <summary>Walks the read-only list and then each of <paramref name="others"/> in turn, one element per round.</summary>
        public static EnumerableValueEnumerable<T> Interleave<T>(
            this ReadOnlyListValueEnumerable<T> source, params IEnumerable<T>[] others)
            => new EnumerableValueEnumerable<T>(InterleaveCore(source, others));

        /// <summary>Walks the sequence and then each of <paramref name="others"/> in turn, one element per round.</summary>
        public static EnumerableValueEnumerable<T> Interleave<T>(
            this EnumerableValueEnumerable<T> source, params IEnumerable<T>[] others)
            => new EnumerableValueEnumerable<T>(InterleaveCore(source, others));

        // The four arms differ only in the static type of the first source, so the array building is
        // shared. Boxing the value enumerable once here is cheaper than four copies of the same code.
        private static InterleaveEnumerable<T> InterleaveCore<T>(IEnumerable<T> first, IEnumerable<T>[] others)
        {
            if (others is null)
            {
                throw new ArgumentNullException(nameof(others));
            }

            var sources = new IEnumerable<T>[others.Length + 1];
            sources[0] = first;
            Array.Copy(others, 0, sources, 1, others.Length);
            return new InterleaveEnumerable<T>(sources);
        }
    }
}
