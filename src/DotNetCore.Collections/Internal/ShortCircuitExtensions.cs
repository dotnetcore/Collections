using System;
using System.Collections.Generic;

namespace DotNetCore.Collections.Internal
{
    /// <summary>
    /// The short-circuit terminals of the engine, written once per arm of the four-source dispatch.
    /// Each of them stops at the first answer instead of counting the sequence, and each reaches
    /// the contiguous hooks when the arm can offer them.
    /// </summary>
    internal static class ShortCircuitExtensions
    {
        // ----- Any(predicate) -----

        internal static bool Any<T>(this ArrayValueEnumerable<T> source, Func<T, bool> predicate)
            => ValueEnumerableCore.Any<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, predicate);

        internal static bool Any<T>(this ListValueEnumerable<T> source, Func<T, bool> predicate)
            => ValueEnumerableCore.Any<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, predicate);

        internal static bool Any<T>(this ReadOnlyListValueEnumerable<T> source, Func<T, bool> predicate)
            => ValueEnumerableCore.Any<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, predicate);

        internal static bool Any<T>(this EnumerableValueEnumerable<T> source, Func<T, bool> predicate)
            => ValueEnumerableCore.Any<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, predicate);

        // ----- First(predicate) -----

        internal static T First<T>(this ArrayValueEnumerable<T> source, Func<T, bool> predicate)
            => ValueEnumerableCore.First<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, predicate);

        internal static T First<T>(this ListValueEnumerable<T> source, Func<T, bool> predicate)
            => ValueEnumerableCore.First<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, predicate);

        internal static T First<T>(this ReadOnlyListValueEnumerable<T> source, Func<T, bool> predicate)
            => ValueEnumerableCore.First<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, predicate);

        internal static T First<T>(this EnumerableValueEnumerable<T> source, Func<T, bool> predicate)
            => ValueEnumerableCore.First<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, predicate);

        // ----- Contains -----

        internal static bool Contains<T>(this ArrayValueEnumerable<T> source, T value)
            => ValueEnumerableCore.Contains<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, value);

        internal static bool Contains<T>(this ListValueEnumerable<T> source, T value)
            => ValueEnumerableCore.Contains<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, value);

        internal static bool Contains<T>(this ReadOnlyListValueEnumerable<T> source, T value)
            => ValueEnumerableCore.Contains<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, value);

        internal static bool Contains<T>(this EnumerableValueEnumerable<T> source, T value)
            => ValueEnumerableCore.Contains<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, value);

        // ----- ElementAt -----

        internal static T ElementAt<T>(this ArrayValueEnumerable<T> source, int index)
            => ValueEnumerableCore.ElementAt<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, index);

        internal static T ElementAt<T>(this ListValueEnumerable<T> source, int index)
            => ValueEnumerableCore.ElementAt<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, index);

        internal static T ElementAt<T>(this ReadOnlyListValueEnumerable<T> source, int index)
            => ValueEnumerableCore.ElementAt<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, index);

        internal static T ElementAt<T>(this EnumerableValueEnumerable<T> source, int index)
            => ValueEnumerableCore.ElementAt<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, index);
    }
}
