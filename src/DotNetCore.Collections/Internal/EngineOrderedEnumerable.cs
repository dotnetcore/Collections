using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DotNetCore.Collections.Internal
{
    /// <summary>
    /// The ordered sequence behind the <c>Order</c> and <c>OrderDescending</c> polyfills. It
    /// materialises its source on every enumeration and sorts it with a stable comparison, and it
    /// implements <see cref="IOrderedEnumerable{TElement}"/> so that a caller can chain the BCL's
    /// own <c>ThenBy</c> on top of it - which is the whole reason the return type is the interface
    /// and not a value-type enumerable of the engine's own.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The comparison is held as a <see cref="Comparison{T}"/> rather than as an
    /// <see cref="IComparer{T}"/>. That is deliberate: <c>IComparer&lt;T&gt;.Compare</c> is
    /// annotated <c>T? x, T? y</c> on the modern targets, and a class over an unconstrained
    /// <c>T</c> cannot restate that annotation under C# 8 - the implementation would be CS8767
    /// implicitly and CS8769 explicitly. <see cref="Comparison{T}"/> carries no such annotation,
    /// so the comparison composes without a suppression and without the engine's public shape
    /// changing: the entry points still take an <see cref="IComparer{T}"/> where the BCL does.
    /// </para>
    /// <para>
    /// Stability is not a property of the sort but of the comparison: each element is paired with
    /// the position it had in the source, and the pair comparison falls back to that position when
    /// the keys are equal. A tie can therefore never be reordered, whatever algorithm the
    /// framework's sort happens to use underneath.
    /// </para>
    /// <para>
    /// The source is walked afresh on every enumeration rather than cached. That is what
    /// <c>System.Linq</c> does, and it is observable: a caller that mutates the underlying list
    /// between two walks sees the change, so caching would be a behavioural divergence rather than
    /// an optimisation.
    /// </para>
    /// </remarks>
    /// <typeparam name="TSource">The value enumerable being ordered.</typeparam>
    /// <typeparam name="TEnumerator">The value-type enumerator of <typeparamref name="TSource"/>.</typeparam>
    /// <typeparam name="T">The type of the elements.</typeparam>
    internal sealed class EngineOrderedEnumerable<TSource, TEnumerator, T> : IOrderedEnumerable<T>
        where TSource : struct, IValueEnumerable<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        private readonly TSource _source;
        private readonly Comparison<T> _comparison;

        internal EngineOrderedEnumerable(TSource source, Comparison<T> comparison)
        {
            _source = source;
            _comparison = comparison;
        }

        /// <summary>Walks the source, sorts what it produced and hands the result out.</summary>
        /// <returns>An enumerator over the sorted elements.</returns>
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Sort()).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Appends a secondary key to the comparison. This is what the BCL's <c>ThenBy</c> calls,
        /// so the engine's ordering composes with the platform's operators in either direction.
        /// </summary>
        /// <typeparam name="TKey">The type of the secondary key.</typeparam>
        /// <param name="keySelector">The projection of an element to its secondary key.</param>
        /// <param name="comparer">The comparer of secondary keys, or <see langword="null"/> for the default one.</param>
        /// <param name="descending">Whether the secondary key orders descending.</param>
        /// <returns>A sequence ordered by the primary key and then by the secondary one.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="keySelector"/> is
        /// <see langword="null"/>.</exception>
        IOrderedEnumerable<T> IOrderedEnumerable<T>.CreateOrderedEnumerable<TKey>(
            Func<T, TKey> keySelector, IComparer<TKey>? comparer, bool descending)
        {
            if (keySelector is null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            return new EngineOrderedEnumerable<TSource, TEnumerator, T>(
                _source,
                EngineComparisons.Then(_comparison, keySelector, comparer, descending));
        }

        private T[] Sort()
        {
            var entries = new List<Entry>();

            using (var enumerator = _source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    entries.Add(new Entry(enumerator.Current, entries.Count));
                }
            }

            var comparison = _comparison;
            entries.Sort((left, right) =>
            {
                var rank = comparison(left.Item, right.Item);
                return rank != 0 ? rank : left.Position.CompareTo(right.Position);
            });

            var sorted = new T[entries.Count];
            for (var index = 0; index < sorted.Length; index++)
            {
                sorted[index] = entries[index].Item;
            }

            return sorted;
        }

        /// <summary>An element together with the position it had in the source, which is what makes
        /// the comparison stable.</summary>
        private readonly struct Entry
        {
            internal Entry(T item, int position)
            {
                Item = item;
                Position = position;
            }

            internal T Item { get; }

            internal int Position { get; }
        }
    }

    /// <summary>
    /// Builds the comparisons the ordered sequence composes. The descending form is normalised
    /// rather than negated, so that a comparer which answers <see cref="int.MinValue"/> cannot wrap
    /// around and invert the direction it was just asked for.
    /// </summary>
    internal static class EngineComparisons
    {
        /// <summary>Builds an ascending comparison over <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="comparer">The ascending comparer, or <see langword="null"/> for the default one.</param>
        /// <returns>The comparison.</returns>
        internal static Comparison<T> Ascending<T>(IComparer<T>? comparer)
        {
            var ranking = comparer ?? Comparer<T>.Default;
            return (x, y) => ranking.Compare(x, y);
        }

        /// <summary>Builds a descending comparison over <paramref name="comparer"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="comparer">The ascending comparer, or <see langword="null"/> for the default one.</param>
        /// <returns>The comparison.</returns>
        internal static Comparison<T> Descending<T>(IComparer<T>? comparer)
        {
            var ranking = comparer ?? Comparer<T>.Default;
            return (x, y) =>
            {
                var rank = ranking.Compare(x, y);
                if (rank == 0)
                {
                    return 0;
                }

                return rank < 0 ? 1 : -1;
            };
        }

        /// <summary>Chains a secondary key onto <paramref name="primary"/>.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the secondary key.</typeparam>
        /// <param name="primary">The comparison that decides first.</param>
        /// <param name="keySelector">The projection of an element to its secondary key.</param>
        /// <param name="comparer">The comparer of secondary keys, or <see langword="null"/> for the default one.</param>
        /// <param name="descending">Whether the secondary key orders descending.</param>
        /// <returns>A comparison that falls back to the secondary key when the primary ties.</returns>
        internal static Comparison<T> Then<T, TKey>(
            Comparison<T> primary, Func<T, TKey> keySelector, IComparer<TKey>? comparer, bool descending)
        {
            var ranking = comparer ?? Comparer<TKey>.Default;
            return (x, y) =>
            {
                var rank = primary(x, y);
                if (rank != 0)
                {
                    return rank;
                }

                var keyRank = ranking.Compare(keySelector(x), keySelector(y));
                if (keyRank == 0 || !descending)
                {
                    return keyRank;
                }

                return keyRank < 0 ? 1 : -1;
            };
        }
    }
}
