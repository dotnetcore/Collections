using System;
using System.Collections.Generic;

// CS8714: TKey is deliberately unconstrained, matching the collections being compared (a bag's
// elements may be null for reference types). The "notnull" key constraint of the annotated
// Dictionary<TKey, TValue> (net5.0+ reference assemblies) is a false positive here - a null key is
// rejected by CollectionDifference.Of rather than stored.
#pragma warning disable CS8714

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Extension methods that compare two bags or two dictionaries and produce the four-way
    /// <see cref="CollectionDifference{TKey, TValue}"/> partition.
    /// </summary>
    public static class CollectionDifferenceExtensions
    {
        /// <summary>
        /// Compares two multisets (bags) by copy count and returns the four-way partition: elements
        /// present only on the left, only on the right, on both with an equal count, and on both
        /// with a different count.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="left">The left multiset.</param>
        /// <param name="right">The right multiset.</param>
        /// <param name="comparer">
        /// The comparer used to decide whether two elements are the same, or <c>null</c> to use
        /// <see cref="EqualityComparer{T}.Default"/>. <see cref="IMultiSet{T}"/> does not expose the
        /// comparer its implementation was built with, so it is supplied here.
        /// </param>
        /// <returns>The four-way partition, keyed by element with the copy count as the value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">An element is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// var left = new MultiList&lt;string&gt;();
        /// left.Add("a", 3);
        /// var right = new MultiList&lt;string&gt;();
        /// right.Add("a", 2);
        ///
        /// left.Difference(right).Differing["a"];   // (3, 2)
        /// </code>
        /// </example>
        public static CollectionDifference<T, int> Difference<T>(
            this IMultiSet<T> left,
            IMultiSet<T> right,
            IEqualityComparer<T>? comparer = null)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            return CollectionDifference<T, int>.Of(Counts(left), Counts(right), comparer);
        }

        /// <summary>
        /// Compares two read-only dictionaries by mapped value and returns the four-way partition:
        /// keys present only on the left, only on the right, on both with an equal value, and on
        /// both with a different value.
        /// </summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="left">The left dictionary.</param>
        /// <param name="right">The right dictionary.</param>
        /// <param name="keyComparer">
        /// The comparer used to decide whether two keys are the same, or <c>null</c> to use
        /// <see cref="EqualityComparer{T}.Default"/>.
        /// </param>
        /// <param name="valueComparer">
        /// The comparer used to decide whether two values of a shared key are equal, or <c>null</c>
        /// to use <see cref="EqualityComparer{T}.Default"/>.
        /// </param>
        /// <returns>The four-way partition of the two dictionaries.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">A key is <c>null</c>.</exception>
        /// <remarks>
        /// A multimap (<c>MultiDictionary&lt;TKey, TValue&gt;</c>) also satisfies this overload, with
        /// <c>TValue</c> being the per-key value collection; the value comparer then compares those
        /// collections, which for the concrete types is reference equality unless one is supplied.
        /// </remarks>
        /// <example>
        /// <code>
        /// var left = new Dictionary&lt;string, int&gt; { ["a"] = 1 };
        /// var right = new Dictionary&lt;string, int&gt; { ["a"] = 2 };
        ///
        /// left.Difference(right).Differing["a"];   // (1, 2)
        /// </code>
        /// </example>
        public static CollectionDifference<TKey, TValue> Difference<TKey, TValue>(
            this IReadOnlyDictionary<TKey, TValue> left,
            IReadOnlyDictionary<TKey, TValue> right,
            IEqualityComparer<TKey>? keyComparer = null,
            IEqualityComparer<TValue>? valueComparer = null)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            return CollectionDifference<TKey, TValue>.Of(left, right, keyComparer, valueComparer);
        }

        /// <summary>
        /// Projects a multiset onto its distinct elements and copy counts, the key/value shape the
        /// difference partitions are built from.
        /// </summary>
        private static IEnumerable<KeyValuePair<T, int>> Counts<T>(IMultiSet<T> bag)
        {
            foreach (var entry in bag.EntrySet())
            {
                yield return new KeyValuePair<T, int>(entry.Item, entry.Count);
            }
        }
    }
}
