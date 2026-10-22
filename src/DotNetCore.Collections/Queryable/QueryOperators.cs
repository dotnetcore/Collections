using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The reference implementations the query layer replays when the BCL has no operator of the
    /// same shape on every target.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>DistinctBy</c> and <c>CountBy</c> reach <see cref="Enumerable"/> only from .NET 6 and .NET 9
    /// onwards, and the selector form of <c>Min</c> / <c>Max</c> is numeric-only before .NET 6. Rather
    /// than branching on the target - which would make the same model behave differently per TFM -
    /// the replay path calls these instead. The engine's own value-enumerable operators are not
    /// reused here on purpose: they are generic over four source arms, and the query layer only has
    /// an <see cref="IEnumerable{T}"/> at run time.
    /// </para>
    /// <para>
    /// These are internal and are reached only through <see cref="QueryExpressionComposer"/>.
    /// </para>
    /// </remarks>
    internal static class QueryOperators
    {
        internal static IEnumerable<TSource> DistinctBy<TSource, TKey>(
            IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector)
        {
            var seen = new HashSet<TKey>();

            foreach (var element in source)
            {
                if (seen.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        internal static IEnumerable<KeyValuePair<TKey, int>> CountBy<TSource, TKey>(
            IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector)
            where TKey : notnull
        {
            var counts = new Dictionary<TKey, int>();
            var order = new List<TKey>();

            foreach (var element in source)
            {
                var key = keySelector(element);

                if (counts.TryGetValue(key, out var count))
                {
                    counts[key] = count + 1;
                }
                else
                {
                    counts.Add(key, 1);
                    order.Add(key);
                }
            }

            foreach (var key in order)
            {
                yield return new KeyValuePair<TKey, int>(key, counts[key]);
            }
        }

        internal static TResult Min<TSource, TResult>(
            IEnumerable<TSource> source,
            Func<TSource, TResult> selector)
        {
            var comparer = Comparer<TResult>.Default;
            var found = false;
            var best = default(TResult)!;

            foreach (var element in source)
            {
                var candidate = selector(element);
                if (!found || comparer.Compare(candidate, best) < 0)
                {
                    best = candidate;
                    found = true;
                }
            }

            if (!found)
            {
                throw new InvalidOperationException("Sequence contains no elements.");
            }

            return best;
        }

        internal static TResult Max<TSource, TResult>(
            IEnumerable<TSource> source,
            Func<TSource, TResult> selector)
        {
            var comparer = Comparer<TResult>.Default;
            var found = false;
            var best = default(TResult)!;

            foreach (var element in source)
            {
                var candidate = selector(element);
                if (!found || comparer.Compare(candidate, best) > 0)
                {
                    best = candidate;
                    found = true;
                }
            }

            if (!found)
            {
                throw new InvalidOperationException("Sequence contains no elements.");
            }

            return best;
        }
    }
}
