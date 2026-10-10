using System.Buffers;

namespace DotNetCore.Collections.Internal
{
    /// <summary>
    /// The materialisation terminals of the engine, written once per arm of the four-source
    /// dispatch. Both of them size their buffer from the count hook when the arm can answer it, so
    /// a fixed-size source is rented once and never grown.
    /// </summary>
    internal static class PoolingExtensions
    {
        // ----- ToPooledList -----

        internal static PooledList<T> ToPooledList<T>(this ArrayValueEnumerable<T> source)
            => ValueEnumerableCore.Materialize<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source);

        internal static PooledList<T> ToPooledList<T>(this ListValueEnumerable<T> source)
            => ValueEnumerableCore.Materialize<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source);

        internal static PooledList<T> ToPooledList<T>(this ReadOnlyListValueEnumerable<T> source)
            => ValueEnumerableCore.Materialize<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source);

        internal static PooledList<T> ToPooledList<T>(this EnumerableValueEnumerable<T> source)
            => ValueEnumerableCore.Materialize<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source);

        // ----- ToArrayPool -----

        internal static T[] ToArrayPool<T>(this ArrayValueEnumerable<T> source, out int length)
            => ValueEnumerableCore.MaterializeArray<ArrayValueEnumerable<T>, ArrayValueEnumerator<T>, T>(source, out length);

        internal static T[] ToArrayPool<T>(this ListValueEnumerable<T> source, out int length)
            => ValueEnumerableCore.MaterializeArray<ListValueEnumerable<T>, ListValueEnumerator<T>, T>(source, out length);

        internal static T[] ToArrayPool<T>(this ReadOnlyListValueEnumerable<T> source, out int length)
            => ValueEnumerableCore.MaterializeArray<ReadOnlyListValueEnumerable<T>, ReadOnlyListValueEnumerator<T>, T>(source, out length);

        internal static T[] ToArrayPool<T>(this EnumerableValueEnumerable<T> source, out int length)
            => ValueEnumerableCore.MaterializeArray<EnumerableValueEnumerable<T>, EnumerableValueEnumerator<T>, T>(source, out length);
    }
}
