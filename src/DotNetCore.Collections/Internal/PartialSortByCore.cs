using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetCore.Collections.Internal
{
    /// <summary>
    /// The core of <c>PartialSortBy</c>: returns the top elements of the source, sorted by a key.
    /// The implementation uses a partial sort (a heap of size k) rather than a full sort, so the
    /// cost is O(n log k) instead of O(n log n).
    /// </summary>
    internal static class PartialSortByCore
    {
        internal static List<T> Execute<TSource, TEnumerator, T, TKey>(
            TSource source,
            int count,
            Func<T, TKey> keySelector,
            IComparer<TKey>? comparer,
            SortDirection direction)
            where TSource : struct, IValueEnumerable<T, TEnumerator>, IValueEnumerableHooks<T>
            where TEnumerator : struct, IEnumerator<T>
        {
            if (count <= 0)
            {
                return new List<T>();
            }

            var cmp = comparer ?? Comparer<TKey>.Default;

            if (source.TryGetNonEnumeratedCount(out var known) && known <= count)
            {
                var all = new List<T>(known);
                using (var enumerator = source.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        all.Add(enumerator.Current);
                    }
                }

                all.Sort((a, b) => cmp.Compare(keySelector(a), keySelector(b)));
                if (direction == SortDirection.Descending)
                {
                    all.Reverse();
                }
                return all;
            }

            var top = new List<T>(count);
            using (var e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    var item = e.Current;
                    var key = keySelector(item);

                    if (top.Count < count)
                    {
                        top.Add(item);
                        SiftUp(top, keySelector, cmp, top.Count - 1, direction);
                    }
                    else
                    {
                        var shouldReplace = direction == SortDirection.Ascending
                            ? cmp.Compare(key, keySelector(top[0])) < 0
                            : cmp.Compare(key, keySelector(top[0])) > 0;

                        if (shouldReplace)
                        {
                            top[0] = item;
                            SiftDown(top, keySelector, cmp, 0, count, direction);
                        }
                    }
                }
            }

            top.Sort((a, b) => cmp.Compare(keySelector(a), keySelector(b)));
            if (direction == SortDirection.Descending)
            {
                top.Reverse();
            }
            return top;
        }

        private static void SiftUp<T, TKey>(List<T> heap, Func<T, TKey> keySelector, IComparer<TKey> comparer, int index, SortDirection direction)
        {
            while (index > 0)
            {
                var parent = (index - 1) / 2;
                var comparison = comparer.Compare(keySelector(heap[index]), keySelector(heap[parent]));

                var shouldSwap = direction == SortDirection.Ascending
                    ? comparison > 0
                    : comparison < 0;

                if (!shouldSwap)
                {
                    break;
                }

                (heap[index], heap[parent]) = (heap[parent], heap[index]);
                index = parent;
            }
        }

        private static void SiftDown<T, TKey>(List<T> heap, Func<T, TKey> keySelector, IComparer<TKey> comparer, int index, int count, SortDirection direction)
        {
            while (true)
            {
                var left = 2 * index + 1;
                var right = 2 * index + 2;
                var target = index;

                var leftComparison = left < count && (direction == SortDirection.Ascending
                    ? comparer.Compare(keySelector(heap[left]), keySelector(heap[target])) > 0
                    : comparer.Compare(keySelector(heap[left]), keySelector(heap[target])) < 0);

                if (leftComparison)
                {
                    target = left;
                }

                var rightComparison = right < count && (direction == SortDirection.Ascending
                    ? comparer.Compare(keySelector(heap[right]), keySelector(heap[target])) > 0
                    : comparer.Compare(keySelector(heap[right]), keySelector(heap[target])) < 0);

                if (rightComparison)
                {
                    target = right;
                }

                if (target == index) break;

                (heap[index], heap[target]) = (heap[target], heap[index]);
                index = target;
            }
        }

        internal enum SortDirection
        {
            Ascending,
            Descending,
        }
    }
}
