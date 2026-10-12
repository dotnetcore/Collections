using System;
using System.Collections.Generic;

namespace DotNetCore.Collections.Internal
{
    /// <summary>
    /// Short-circuit count predicates: <c>AtLeast</c>, <c>Exactly</c> and <c>CountBetween</c>.
    /// Each stops walking the source as soon as the answer is determined, which is the property
    /// the Paginable module needs for its "is there a next page" probe.
    /// </summary>
    internal static class CountPredicateCore
    {
        internal static bool AtLeast<TSource, TEnumerator, T>(TSource source, int count)
            where TSource : struct, IValueEnumerable<T, TEnumerator>, IValueEnumerableHooks<T>
            where TEnumerator : struct, IEnumerator<T>
        {
            if (count <= 0) return true;

            if (source.TryGetNonEnumeratedCount(out var known) && known >= count)
            {
                return true;
            }

            var remaining = count;

#if NETCOREAPP3_0_OR_GREATER
            if (source.TryGetSpan(out var span))
            {
                return span.Length >= count;
            }
#endif

            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    remaining--;
                    if (remaining == 0) return true;
                }
            }

            return false;
        }

        internal static bool Exactly<TSource, TEnumerator, T>(TSource source, int count)
            where TSource : struct, IValueEnumerable<T, TEnumerator>, IValueEnumerableHooks<T>
            where TEnumerator : struct, IEnumerator<T>
        {
            if (count < 0) return false;

            if (source.TryGetNonEnumeratedCount(out var known))
            {
                return known == count;
            }

#if NETCOREAPP3_0_OR_GREATER
            if (source.TryGetSpan(out var span))
            {
                return span.Length == count;
            }
#endif

            using (var enumerator = source.GetEnumerator())
            {
                var seen = 0;
                while (enumerator.MoveNext())
                {
                    seen++;
                    if (seen > count) return false;
                }

                return seen == count;
            }
        }

        internal static bool CountBetween<TSource, TEnumerator, T>(TSource source, int min, int max)
            where TSource : struct, IValueEnumerable<T, TEnumerator>, IValueEnumerableHooks<T>
            where TEnumerator : struct, IEnumerator<T>
        {
            if (min > max) return false;
            if (min <= 0 && source.TryGetNonEnumeratedCount(out var known))
            {
                return known >= min && known <= max;
            }

#if NETCOREAPP3_0_OR_GREATER
            if (source.TryGetSpan(out var span))
            {
                return span.Length >= min && span.Length <= max;
            }
#endif

            using (var enumerator = source.GetEnumerator())
            {
                var seen = 0;
                while (enumerator.MoveNext())
                {
                    seen++;
                    if (seen > max) return false;
                }

                return seen >= min;
            }
        }
    }
}
