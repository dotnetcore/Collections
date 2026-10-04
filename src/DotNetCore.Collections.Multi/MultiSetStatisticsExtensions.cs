using System;
using System.Collections.Generic;

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Extension methods that summarise a multiset (bag) as a frequency distribution: the most
    /// frequent elements (<see cref="Mode{T}"/>), the middle element of the expanded copies
    /// (<see cref="Median{T}"/>), and the Shannon entropy of the distribution
    /// (<see cref="Entropy{T}"/>).
    /// </summary>
    /// <remarks>
    /// Every method reads the bag through <see cref="IMultiSet{T}.EntrySet"/> - the distinct
    /// elements together with their copy counts - so none of them expands the copies into a list of
    /// <see cref="IMultiSet{T}.TotalCount"/> items. They therefore work on any bag implementation
    /// and need no generic-math support: the counts are plain <see cref="int"/>, and the only order
    /// any of them requires is the one the caller hands to <see cref="Median{T}"/>. These are
    /// read-only projections; like the rest of the family they return a value or a fresh list and
    /// never a live view of the bag.
    /// </remarks>
    public static class MultiSetStatisticsExtensions
    {
        /// <summary>
        /// Returns every element whose copy count equals the largest copy count in the bag - the
        /// modes of the multiset.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="bag">The multiset to summarise.</param>
        /// <returns>
        /// The elements tied for the highest copy count, in the order the bag enumerates its
        /// distinct elements; an empty list when the bag is empty.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="bag"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// var bag = new MultiList&lt;string&gt;();
        /// bag.Add("a", 3);
        /// bag.Add("b", 3);
        /// bag.Add("c", 1);
        ///
        /// bag.Mode();   // ["a", "b"] - both hold the maximum of three copies
        /// </code>
        /// </example>
        public static IReadOnlyList<T> Mode<T>(this IMultiSet<T> bag)
        {
            if (bag == null)
            {
                throw new ArgumentNullException(nameof(bag));
            }

            var modes = new List<T>();
            var best = 0;

            foreach (var entry in bag.EntrySet())
            {
                if (entry.Count > best)
                {
                    best = entry.Count;
                    modes.Clear();
                    modes.Add(entry.Item);
                }
                else if (entry.Count == best)
                {
                    modes.Add(entry.Item);
                }
            }

            return modes;
        }

        /// <summary>
        /// Returns the upper median of the bag's copies: the element that would sit in the middle
        /// if every copy were laid out in ascending order.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="bag">The multiset to summarise.</param>
        /// <param name="comparer">The order that decides which copy is the middle one.</param>
        /// <returns>
        /// With an odd number of copies, the single middle element; with an even number, the larger
        /// of the two middle elements (the upper median). The result is one of the bag's own
        /// elements, never an interpolated value - a multiset need not be numeric, so no average is
        /// taken.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="bag"/> or <paramref name="comparer"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">The bag is empty and has no median.</exception>
        /// <remarks>
        /// The copies are not expanded: the distinct elements are ordered once with
        /// <paramref name="comparer"/> and then walked, accumulating copy counts, until the target
        /// rank is reached. The cost is therefore O(d log d) time and O(d) space for d distinct
        /// elements, independent of how many copies each holds.
        /// </remarks>
        /// <example>
        /// <code>
        /// var bag = new MultiList&lt;int&gt;();
        /// bag.Add(1, 2);
        /// bag.Add(2, 1);
        /// bag.Add(3, 1);   // copies in order: 1, 1, 2, 3
        ///
        /// bag.Median(Comparer&lt;int&gt;.Default);   // 2 - the upper of the two middle copies
        /// </code>
        /// </example>
        public static T Median<T>(this IMultiSet<T> bag, IComparer<T> comparer)
        {
            if (bag == null)
            {
                throw new ArgumentNullException(nameof(bag));
            }

            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            var total = bag.TotalCount;
            if (total == 0)
            {
                throw new InvalidOperationException(
                    "The multiset is empty and therefore has no median.");
            }

            var entries = new List<KeyValuePair<T, int>>(bag.DistinctCount);
            foreach (var entry in bag.EntrySet())
            {
                entries.Add(new KeyValuePair<T, int>(entry.Item, entry.Count));
            }

            entries.Sort((x, y) => comparer.Compare(x.Key, y.Key));

            // The upper median is the copy at 0-based rank total / 2 among the expanded copies:
            // an odd total lands on the single middle copy, an even total on the larger of the two.
            var target = total / 2;
            var seen = 0;

            for (var i = 0; i < entries.Count; i++)
            {
                seen += entries[i].Value;
                if (seen > target)
                {
                    return entries[i].Key;
                }
            }

            // Unreachable: the accumulated counts reach TotalCount, which is greater than target.
            throw new InvalidOperationException(
                "The multiset reported a total count it did not enumerate.");
        }

        /// <summary>
        /// Returns the Shannon entropy, in bits, of the bag's copy-count distribution - a measure of
        /// how evenly the copies are spread over the distinct elements.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="bag">The multiset to summarise.</param>
        /// <returns>
        /// <c>-Σ p·log2(p)</c> over the distinct elements, where <c>p</c> is that element's copy
        /// count divided by <see cref="IMultiSet{T}.TotalCount"/>. A bag holding a single distinct
        /// element has entropy 0 however many copies it holds; a bag spreading its copies uniformly
        /// over <c>d</c> distinct elements has entropy <c>log2(d)</c>; an empty bag has entropy 0.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="bag"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// var bag = new MultiList&lt;string&gt;();
        /// bag.Add("a", 2);
        /// bag.Add("b", 2);   // two distinct elements, two copies each
        ///
        /// bag.Entropy();   // 1.0 - one bit, the maximum for two equally likely outcomes
        /// </code>
        /// </example>
        public static double Entropy<T>(this IMultiSet<T> bag)
        {
            if (bag == null)
            {
                throw new ArgumentNullException(nameof(bag));
            }

            var total = bag.TotalCount;
            if (total == 0)
            {
                return 0d;
            }

            var entropy = 0d;

            foreach (var entry in bag.EntrySet())
            {
                var p = entry.Count / (double)total;
                entropy -= p * Math.Log(p, 2d);
            }

            return entropy;
        }
    }
}
