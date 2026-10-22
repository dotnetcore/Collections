using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The backing implementation of <c>FullJoin</c> and <c>FullGroupJoin</c>, the two outer-join
    /// operators of the P2 layer that no target framework in the matrix ships.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>FullJoin</c> is the only join whose two sides are both allowed to be unmatched, so it needs
    /// two phases: the inner side is indexed first (the index is also what records which inner
    /// elements were matched), then the outer side streams past, and finally the inner elements that
    /// nothing claimed are emitted. That two-phase state machine is why these operators are written
    /// as iterator methods rather than as hand-rolled value-type enumerators: the struct form would
    /// have to carry the index, the matched flags and the phase cursor, and the allocation saved
    /// would be one state machine per walk.
    /// </para>
    /// <para>
    /// The output order is: every outer element in source order (a matched outer element expands to
    /// one row per matching inner element), then the unmatched inner elements in their own source
    /// order.
    /// </para>
    /// <para>
    /// Both operators hash through <see cref="JoinLookup{TKey,TValue}"/> rather than a
    /// <see cref="Dictionary{TKey,TValue}"/>, because the join operators must not constrain
    /// <c>TKey</c> to <c>notnull</c> - <c>LeftJoin</c> / <c>RightJoin</c> do not, and neither does
    /// the BCL. The two differ in what a null key means, and both follow the platform: <c>FullJoin</c>
    /// never matches a null key (the rule <c>Lookup.CreateForJoin</c> applies), so a null-keyed
    /// element is emitted as unmatched on its own side, while <c>FullGroupJoin</c> gives a null key
    /// a group of its own (the rule <c>GroupBy</c> applies).
    /// </para>
    /// </remarks>
    internal static class OuterJoinCore
    {
        internal static IEnumerable<TResult> FullJoin<TOuter, TInner, TKey, TResult>(
            IEnumerable<TOuter> outer,
            IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector,
            Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector,
            IEqualityComparer<TKey>? comparer)
        {
            // The inner side is indexed by position rather than by value so that the second phase can
            // tell which of its elements nobody claimed. The positions are the index into
            // innerItems, which is also the index into matched.
            var innerItems = new List<TInner>();
            var matched = new List<bool>();
            var index = new JoinLookup<TKey, int>(comparer ?? EqualityComparer<TKey>.Default);

            foreach (var item in inner)
            {
                var key = innerKeySelector(item);
                var position = innerItems.Count;
                innerItems.Add(item);
                matched.Add(false);

                // A null key matches nothing, so it never enters the index. The element is still
                // remembered above, which is what lets the tail emit it as unmatched.
                if (!(key is null))
                {
                    index.Add(key, position);
                }
            }

            foreach (var outerItem in outer)
            {
                var key = outerKeySelector(outerItem);
                if (!(key is null) && index.TryGetGroup(key, out var positions))
                {
                    var found = positions!;
                    for (var i = 0; i < found.Count; i++)
                    {
                        var position = found[i];
                        matched[position] = true;
                        yield return resultSelector(outerItem, innerItems[position]);
                    }
                }
                else
                {
                    // The outer element has no partner at all; the inner side is default rather than
                    // the row being dropped, which is what makes this a full join.
                    yield return resultSelector(outerItem, default!);
                }
            }

            for (var i = 0; i < innerItems.Count; i++)
            {
                if (!matched[i])
                {
                    yield return resultSelector(default!, innerItems[i]);
                }
            }
        }

        internal static IEnumerable<TResult> FullGroupJoin<TFirst, TSecond, TKey, TResult>(
            IEnumerable<TFirst> first,
            IEnumerable<TSecond> second,
            Func<TFirst, TKey> firstKeySelector,
            Func<TSecond, TKey> secondKeySelector,
            Func<TKey, IEnumerable<TFirst>, IEnumerable<TSecond>, TResult> resultSelector,
            IEqualityComparer<TKey>? comparer)
        {
            var comparerToUse = comparer ?? EqualityComparer<TKey>.Default;
            var firstGroups = new JoinLookup<TKey, TFirst>(comparerToUse);
            var secondGroups = new JoinLookup<TKey, TSecond>(comparerToUse);
            var order = new List<TKey>();

            foreach (var item in first)
            {
                var key = firstKeySelector(item);

                // Unlike FullJoin, a null key is grouped rather than ignored: a group join groups the
                // way GroupBy does, and GroupBy gives a null key a group of its own.
                if (!firstGroups.TryGetGroup(key, out _))
                {
                    order.Add(key);
                }

                firstGroups.Add(key, item);
            }

            foreach (var item in second)
            {
                var key = secondKeySelector(item);
                var isNewOnSecond = !secondGroups.TryGetGroup(key, out _);

                // A key that only the second side knows still gets a row, and it lands after the
                // keys the first side introduced - the row order is first-seen across both sides.
                if (isNewOnSecond && !firstGroups.TryGetGroup(key, out _))
                {
                    order.Add(key);
                }

                secondGroups.Add(key, item);
            }

            // One row per distinct key. A side that has no element for the key contributes an empty
            // group rather than the row being skipped.
            foreach (var key in order)
            {
                firstGroups.TryGetGroup(key, out var left);
                secondGroups.TryGetGroup(key, out var right);

                yield return resultSelector(
                    key,
                    left ?? Enumerable.Empty<TFirst>(),
                    right ?? Enumerable.Empty<TSecond>());
            }
        }
    }
}
