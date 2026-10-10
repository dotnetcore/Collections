using System;

namespace DotNetCore.Collections.Internal
{
    /// <summary>
    /// The four entry surfaces for the three engine operators. Each arm of the four-source
    /// dispatch gets its own overload because C# does not infer type arguments from a generic
    /// constraint: <see cref="WhereValueEnumerable{TSource,TEnumerator,T}"/> and its siblings are
    /// generic over the source, and only an overload typed on the concrete arm can pin
    /// <c>TSource</c> and <c>TEnumerator</c>.
    /// </summary>
    /// <remarks>
    /// The overloads live in this assembly-internal namespace rather than in
    /// <c>DotNetCore.Collections</c>: version 1 of the engine deliberately keeps its operator
    /// surface off the public API until the namespace-governance work item fixes the final shape,
    /// which also keeps the engine from competing with <c>System.Linq</c> at every call site
    /// before that decision is made.
    /// </remarks>
    internal static class OperatorExtensions
    {
        // ----- Where -----

        internal static WhereValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T> Where<T>(
            this ArrayValueEnumerable<T> source, Func<T, bool> predicate)
            => new WhereValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, predicate);

        internal static WhereValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T> Where<T>(
            this ListValueEnumerable<T> source, Func<T, bool> predicate)
            => new WhereValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, predicate);

        internal static WhereValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T> Where<T>(
            this ReadOnlyListValueEnumerable<T> source, Func<T, bool> predicate)
            => new WhereValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, predicate);

        internal static WhereValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T> Where<T>(
            this EnumerableValueEnumerable<T> source, Func<T, bool> predicate)
            => new WhereValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, predicate);

        // ----- Select -----

        internal static SelectValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TResult> Select<T, TResult>(
            this ArrayValueEnumerable<T> source, Func<T, TResult> selector)
            => new SelectValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TResult>(source, selector);

        internal static SelectValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TResult> Select<T, TResult>(
            this ListValueEnumerable<T> source, Func<T, TResult> selector)
            => new SelectValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TResult>(source, selector);

        internal static SelectValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TResult> Select<T, TResult>(
            this ReadOnlyListValueEnumerable<T> source, Func<T, TResult> selector)
            => new SelectValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TResult>(source, selector);

        internal static SelectValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TResult> Select<T, TResult>(
            this EnumerableValueEnumerable<T> source, Func<T, TResult> selector)
            => new SelectValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TResult>(source, selector);

        // ----- WhereSelect (the fused pass) -----

        internal static WhereSelectValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TResult> WhereSelect<T, TResult>(
            this ArrayValueEnumerable<T> source, Func<T, bool> predicate, Func<T, TResult> selector)
            => new WhereSelectValueEnumerable<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T, TResult>(source, predicate, selector);

        internal static WhereSelectValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TResult> WhereSelect<T, TResult>(
            this ListValueEnumerable<T> source, Func<T, bool> predicate, Func<T, TResult> selector)
            => new WhereSelectValueEnumerable<ListValueEnumerable<T>, ListValueEnumerator<T>, T, TResult>(source, predicate, selector);

        internal static WhereSelectValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TResult> WhereSelect<T, TResult>(
            this ReadOnlyListValueEnumerable<T> source, Func<T, bool> predicate, Func<T, TResult> selector)
            => new WhereSelectValueEnumerable<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T, TResult>(source, predicate, selector);

        internal static WhereSelectValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TResult> WhereSelect<T, TResult>(
            this EnumerableValueEnumerable<T> source, Func<T, bool> predicate, Func<T, TResult> selector)
            => new WhereSelectValueEnumerable<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T, TResult>(source, predicate, selector);
    }
}
