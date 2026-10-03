using System;
using System.Collections.Generic;

// CS8714: TKey is deliberately unconstrained. The keys come from an IMultiSet<T> (whose elements may
// be null for reference types) or from an IReadOnlyDictionary<TKey, TValue>; the "notnull" key
// constraint of the annotated Dictionary<TKey, TValue> (net5.0+ reference assemblies) is a false
// positive here - a null key is rejected up front with a clear exception rather than stored.
#pragma warning disable CS8714

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// The immutable, four-way partition produced by comparing two keyed collections: the keys that
    /// occur only on the left, only on the right, on both sides with an equal value, and on both
    /// sides with a different value.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of the keys. A bag contributes its distinct elements as keys and its copy counts as
    /// values; a dictionary contributes its keys and its mapped values.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of the values compared for keys present on both sides. For a bag this is
    /// <see cref="int"/> (the copy count).
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// This is the shape a "what changed between these two collections?" query wants, and it is the
    /// same four partitions either way: for two bags the values are the copy counts, so the result
    /// answers "which elements were added, removed, or changed multiplicity"; for two dictionaries
    /// the values are the mapped values, so it answers "which keys were added, removed, or changed
    /// value". The partitions are disjoint and together cover every key that occurs on either side.
    /// </para>
    /// <para>
    /// The result is a <b>snapshot</b>: it is built once from the two inputs and never observes later
    /// changes to them. It is deliberately not a live view - this library keeps its collection
    /// algebra to the read-only view / snapshot / in-place forms and does not ship writable
    /// through-views, so a difference can be inspected but not written back through.
    /// </para>
    /// <para>
    /// A <c>null</c> key can not be represented (the four partitions are dictionaries), so it is
    /// rejected with an <see cref="ArgumentException"/> rather than silently dropped - the same
    /// policy as <see cref="MultiList{T}.ToDictionary"/>.
    /// </para>
    /// <para>
    /// This type is immutable and therefore safe to share between threads once built.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var left = new MultiList&lt;string&gt;();
    /// left.Add("a", 3);
    /// left.Add("b");
    ///
    /// var right = new MultiList&lt;string&gt;();
    /// right.Add("a", 2);
    /// right.Add("c");
    ///
    /// var diff = left.Difference(right);
    /// diff.OnlyInLeft["b"];    // 1
    /// diff.OnlyInRight["c"];   // 1
    /// diff.Differing["a"];     // (3, 2)
    /// diff.AreEqual;           // false
    /// </code>
    /// </example>
    public sealed class CollectionDifference<TKey, TValue>
    {
        private CollectionDifference(
            IReadOnlyDictionary<TKey, TValue> onlyInLeft,
            IReadOnlyDictionary<TKey, TValue> onlyInRight,
            IReadOnlyDictionary<TKey, TValue> inCommon,
            IReadOnlyDictionary<TKey, (TValue Left, TValue Right)> differing)
        {
            OnlyInLeft = onlyInLeft;
            OnlyInRight = onlyInRight;
            InCommon = inCommon;
            Differing = differing;
        }

        /// <summary>
        /// Gets the keys that occur on the left side only, mapped to their left value.
        /// </summary>
        public IReadOnlyDictionary<TKey, TValue> OnlyInLeft { get; }

        /// <summary>
        /// Gets the keys that occur on the right side only, mapped to their right value.
        /// </summary>
        public IReadOnlyDictionary<TKey, TValue> OnlyInRight { get; }

        /// <summary>
        /// Gets the keys that occur on both sides with an equal value, mapped to that value.
        /// </summary>
        public IReadOnlyDictionary<TKey, TValue> InCommon { get; }

        /// <summary>
        /// Gets the keys that occur on both sides with a different value, mapped to the left and the
        /// right value in that order.
        /// </summary>
        public IReadOnlyDictionary<TKey, (TValue Left, TValue Right)> Differing { get; }

        /// <summary>
        /// Gets a value indicating whether the two compared collections held exactly the same keys
        /// with exactly the same values - that is, whether <see cref="OnlyInLeft"/>,
        /// <see cref="OnlyInRight"/> and <see cref="Differing"/> are all empty.
        /// </summary>
        public bool AreEqual => OnlyInLeft.Count == 0 && OnlyInRight.Count == 0 && Differing.Count == 0;

        /// <summary>
        /// Builds the four-way partition of two keyed sequences.
        /// </summary>
        /// <param name="left">The left sequence of key/value pairs. Keys must be distinct.</param>
        /// <param name="right">The right sequence of key/value pairs. Keys must be distinct.</param>
        /// <param name="keyComparer">
        /// The comparer used to decide whether two keys are the same, or <c>null</c> to use
        /// <see cref="EqualityComparer{T}.Default"/>.
        /// </param>
        /// <param name="valueComparer">
        /// The comparer used to decide whether two values of a shared key are equal, or <c>null</c>
        /// to use <see cref="EqualityComparer{T}.Default"/>.
        /// </param>
        /// <returns>A snapshot partition of the two inputs.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="left"/> or <paramref name="right"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">A key is <c>null</c>, or a key repeats within one side.</exception>
        public static CollectionDifference<TKey, TValue> Of(
            IEnumerable<KeyValuePair<TKey, TValue>> left,
            IEnumerable<KeyValuePair<TKey, TValue>> right,
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

            var leftMap = ToMap(left, keyComparer, nameof(left));
            var rightMap = ToMap(right, keyComparer, nameof(right));
            var valueEquals = valueComparer ?? EqualityComparer<TValue>.Default;

            var onlyInLeft = new Dictionary<TKey, TValue>(keyComparer);
            var onlyInRight = new Dictionary<TKey, TValue>(keyComparer);
            var inCommon = new Dictionary<TKey, TValue>(keyComparer);
            var differing = new Dictionary<TKey, (TValue Left, TValue Right)>(keyComparer);

            foreach (var pair in leftMap)
            {
                if (rightMap.TryGetValue(pair.Key, out var rightValue))
                {
                    if (valueEquals.Equals(pair.Value, rightValue))
                    {
                        inCommon.Add(pair.Key, pair.Value);
                    }
                    else
                    {
                        differing.Add(pair.Key, (pair.Value, rightValue));
                    }
                }
                else
                {
                    onlyInLeft.Add(pair.Key, pair.Value);
                }
            }

            foreach (var pair in rightMap)
            {
                if (!leftMap.ContainsKey(pair.Key))
                {
                    onlyInRight.Add(pair.Key, pair.Value);
                }
            }

            return new CollectionDifference<TKey, TValue>(onlyInLeft, onlyInRight, inCommon, differing);
        }

        /// <summary>
        /// Returns a one-line summary of the four partition sizes.
        /// </summary>
        /// <example>
        /// <code>
        /// diff.ToString();   // "left-only 1, right-only 1, in-common 0, differing 1"
        /// </code>
        /// </example>
        public override string ToString()
        {
            return string.Format(
                "left-only {0}, right-only {1}, in-common {2}, differing {3}",
                OnlyInLeft.Count,
                OnlyInRight.Count,
                InCommon.Count,
                Differing.Count);
        }

        /// <summary>
        /// Materializes a sequence of key/value pairs into a lookup, rejecting a null key and a
        /// repeated key with a message that names the offending side.
        /// </summary>
        private static Dictionary<TKey, TValue> ToMap(
            IEnumerable<KeyValuePair<TKey, TValue>> pairs,
            IEqualityComparer<TKey>? keyComparer,
            string side)
        {
            var map = new Dictionary<TKey, TValue>(keyComparer);
            foreach (var pair in pairs)
            {
                if (pair.Key == null)
                {
                    throw new ArgumentException(
                        "A null key can not be represented in the difference partitions.", side);
                }

                if (map.ContainsKey(pair.Key))
                {
                    throw new ArgumentException(
                        "A key occurs more than once in the same side; the inputs must be keyed by distinct keys.",
                        side);
                }

                map.Add(pair.Key, pair.Value);
            }

            return map;
        }
    }
}
