using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace DotNetCore.Collections.Benchmarks
{
    // F7-15: the operator matrix, part 2 of 5 - the set, grouping and ranking tier.
    //
    // Same shape and same consumption rule as part 1: four adjacent rows per operator, one per source
    // arm, each counting the elements the operator yields. The materialising operators in this tier
    // (Maxima, Minima, PartialSortBy, Shuffle) return a List<T>, so their rows read its Count - which
    // is the operator's whole result and therefore a legitimate consumption - and their allocation
    // column carries the list they build.

    /// <summary>Set, grouping and ranking operators of the F7-15 matrix.</summary>
    public partial class OperatorMatrix
    {
        // ----- DistinctUntilChanged -----

        /// <summary><c>DistinctUntilChanged</c> over the array arm.</summary>
        [Benchmark] public long DistinctUntilChangedArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().DistinctUntilChanged()) { n++; } return n; }

        /// <summary><c>DistinctUntilChanged</c> over the list arm.</summary>
        [Benchmark] public long DistinctUntilChangedList() { long n = 0; foreach (var v in _list.ToValueEnumerable().DistinctUntilChanged()) { n++; } return n; }

        /// <summary><c>DistinctUntilChanged</c> over the read-only-list arm.</summary>
        [Benchmark] public long DistinctUntilChangedReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().DistinctUntilChanged()) { n++; } return n; }

        /// <summary><c>DistinctUntilChanged</c> over the enumerable arm.</summary>
        [Benchmark] public long DistinctUntilChangedEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().DistinctUntilChanged()) { n++; } return n; }

        // ----- Memoize -----

        /// <summary><c>Memoize</c> over the array arm.</summary>
        [Benchmark] public long MemoizeArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().Memoize()) { n++; } return n; }

        /// <summary><c>Memoize</c> over the list arm.</summary>
        [Benchmark] public long MemoizeList() { long n = 0; foreach (var v in _list.ToValueEnumerable().Memoize()) { n++; } return n; }

        /// <summary><c>Memoize</c> over the read-only-list arm.</summary>
        [Benchmark] public long MemoizeReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().Memoize()) { n++; } return n; }

        /// <summary><c>Memoize</c> over the enumerable arm.</summary>
        [Benchmark] public long MemoizeEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().Memoize()) { n++; } return n; }

        // ----- FallbackIfEmpty -----

        /// <summary><c>FallbackIfEmpty</c> over the array arm; the source is non-empty, so the
        /// fallback is never walked.</summary>
        [Benchmark] public long FallbackIfEmptyArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().FallbackIfEmpty(_second)) { n++; } return n; }

        /// <summary><c>FallbackIfEmpty</c> over the list arm.</summary>
        [Benchmark] public long FallbackIfEmptyList() { long n = 0; foreach (var v in _list.ToValueEnumerable().FallbackIfEmpty(_second)) { n++; } return n; }

        /// <summary><c>FallbackIfEmpty</c> over the read-only-list arm.</summary>
        [Benchmark] public long FallbackIfEmptyReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().FallbackIfEmpty(_second)) { n++; } return n; }

        /// <summary><c>FallbackIfEmpty</c> over the enumerable arm.</summary>
        [Benchmark] public long FallbackIfEmptyEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().FallbackIfEmpty(_second)) { n++; } return n; }

        // ----- DistinctBy -----

        /// <summary><c>DistinctBy</c> over the array arm: 1024 buckets over 10 000 elements.</summary>
        [Benchmark] public long DistinctByArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().DistinctBy(MatrixSources.Bucket)) { n++; } return n; }

        /// <summary><c>DistinctBy</c> over the list arm.</summary>
        [Benchmark] public long DistinctByList() { long n = 0; foreach (var v in _list.ToValueEnumerable().DistinctBy(MatrixSources.Bucket)) { n++; } return n; }

        /// <summary><c>DistinctBy</c> over the read-only-list arm.</summary>
        [Benchmark] public long DistinctByReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().DistinctBy(MatrixSources.Bucket)) { n++; } return n; }

        /// <summary><c>DistinctBy</c> over the enumerable arm.</summary>
        [Benchmark] public long DistinctByEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().DistinctBy(MatrixSources.Bucket)) { n++; } return n; }

        // ----- CountBy -----

        /// <summary><c>CountBy</c> over the array arm.</summary>
        [Benchmark] public long CountByArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().CountBy(MatrixSources.Bucket, EqualityComparer<int>.Default)) { n++; } return n; }

        /// <summary><c>CountBy</c> over the list arm.</summary>
        [Benchmark] public long CountByList() { long n = 0; foreach (var v in _list.ToValueEnumerable().CountBy(MatrixSources.Bucket, EqualityComparer<int>.Default)) { n++; } return n; }

        /// <summary><c>CountBy</c> over the read-only-list arm.</summary>
        [Benchmark] public long CountByReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().CountBy(MatrixSources.Bucket, EqualityComparer<int>.Default)) { n++; } return n; }

        /// <summary><c>CountBy</c> over the enumerable arm.</summary>
        [Benchmark] public long CountByEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().CountBy(MatrixSources.Bucket, EqualityComparer<int>.Default)) { n++; } return n; }

        // ----- AggregateBy -----

        /// <summary><c>AggregateBy</c> over the array arm: 32 buckets, summed.</summary>
        [Benchmark] public long AggregateByArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().AggregateBy(MatrixSources.SmallBucket, 0, MatrixSources.Add, EqualityComparer<int>.Default)) { n++; } return n; }

        /// <summary><c>AggregateBy</c> over the list arm.</summary>
        [Benchmark] public long AggregateByList() { long n = 0; foreach (var v in _list.ToValueEnumerable().AggregateBy(MatrixSources.SmallBucket, 0, MatrixSources.Add, EqualityComparer<int>.Default)) { n++; } return n; }

        /// <summary><c>AggregateBy</c> over the read-only-list arm.</summary>
        [Benchmark] public long AggregateByReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().AggregateBy(MatrixSources.SmallBucket, 0, MatrixSources.Add, EqualityComparer<int>.Default)) { n++; } return n; }

        /// <summary><c>AggregateBy</c> over the enumerable arm.</summary>
        [Benchmark] public long AggregateByEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().AggregateBy(MatrixSources.SmallBucket, 0, MatrixSources.Add, EqualityComparer<int>.Default)) { n++; } return n; }

        // ----- ExceptBy -----

        /// <summary><c>ExceptBy</c> over the array arm; the key set is the 32-element sequence, so
        /// most elements survive and the pass is not cut short.</summary>
        [Benchmark] public long ExceptByArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().ExceptBy(_small, MatrixSources.Bucket)) { n++; } return n; }

        /// <summary><c>ExceptBy</c> over the list arm.</summary>
        [Benchmark] public long ExceptByList() { long n = 0; foreach (var v in _list.ToValueEnumerable().ExceptBy(_small, MatrixSources.Bucket)) { n++; } return n; }

        /// <summary><c>ExceptBy</c> over the read-only-list arm.</summary>
        [Benchmark] public long ExceptByReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().ExceptBy(_small, MatrixSources.Bucket)) { n++; } return n; }

        /// <summary><c>ExceptBy</c> over the enumerable arm.</summary>
        [Benchmark] public long ExceptByEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().ExceptBy(_small, MatrixSources.Bucket)) { n++; } return n; }

        // ----- IntersectBy -----

        /// <summary><c>IntersectBy</c> over the array arm.</summary>
        [Benchmark] public long IntersectByArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().IntersectBy(_small, MatrixSources.Bucket)) { n++; } return n; }

        /// <summary><c>IntersectBy</c> over the list arm.</summary>
        [Benchmark] public long IntersectByList() { long n = 0; foreach (var v in _list.ToValueEnumerable().IntersectBy(_small, MatrixSources.Bucket)) { n++; } return n; }

        /// <summary><c>IntersectBy</c> over the read-only-list arm.</summary>
        [Benchmark] public long IntersectByReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().IntersectBy(_small, MatrixSources.Bucket)) { n++; } return n; }

        /// <summary><c>IntersectBy</c> over the enumerable arm.</summary>
        [Benchmark] public long IntersectByEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().IntersectBy(_small, MatrixSources.Bucket)) { n++; } return n; }

        // ----- UnionBy -----

        /// <summary><c>UnionBy</c> over the array arm: the first sequence's keys are all distinct, so
        /// the second contributes nothing and the whole input is yielded.</summary>
        [Benchmark] public long UnionByArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().UnionBy(_second, MatrixSources.Bucket)) { n++; } return n; }

        /// <summary><c>UnionBy</c> over the list arm.</summary>
        [Benchmark] public long UnionByList() { long n = 0; foreach (var v in _list.ToValueEnumerable().UnionBy(_second, MatrixSources.Bucket)) { n++; } return n; }

        /// <summary><c>UnionBy</c> over the read-only-list arm.</summary>
        [Benchmark] public long UnionByReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().UnionBy(_second, MatrixSources.Bucket)) { n++; } return n; }

        /// <summary><c>UnionBy</c> over the enumerable arm.</summary>
        [Benchmark] public long UnionByEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().UnionBy(_second, MatrixSources.Bucket)) { n++; } return n; }

        // ----- MaxBy -----

        /// <summary><c>MaxBy</c> over the array arm.</summary>
        [Benchmark] public int MaxByArray() => _array.ToValueEnumerable().MaxBy(MatrixSources.Bucket);

        /// <summary><c>MaxBy</c> over the list arm.</summary>
        [Benchmark] public int MaxByList() => _list.ToValueEnumerable().MaxBy(MatrixSources.Bucket);

        /// <summary><c>MaxBy</c> over the read-only-list arm.</summary>
        [Benchmark] public int MaxByReadOnlyList() => _readOnly.ToValueEnumerable().MaxBy(MatrixSources.Bucket);

        /// <summary><c>MaxBy</c> over the enumerable arm.</summary>
        [Benchmark] public int MaxByEnumerable() => _sequence.ToValueEnumerable().MaxBy(MatrixSources.Bucket);

        // ----- MinBy -----

        /// <summary><c>MinBy</c> over the array arm.</summary>
        [Benchmark] public int MinByArray() => _array.ToValueEnumerable().MinBy(MatrixSources.Bucket);

        /// <summary><c>MinBy</c> over the list arm.</summary>
        [Benchmark] public int MinByList() => _list.ToValueEnumerable().MinBy(MatrixSources.Bucket);

        /// <summary><c>MinBy</c> over the read-only-list arm.</summary>
        [Benchmark] public int MinByReadOnlyList() => _readOnly.ToValueEnumerable().MinBy(MatrixSources.Bucket);

        /// <summary><c>MinBy</c> over the enumerable arm.</summary>
        [Benchmark] public int MinByEnumerable() => _sequence.ToValueEnumerable().MinBy(MatrixSources.Bucket);

        // ----- Maxima -----

        /// <summary><c>Maxima</c> over the array arm; returns the number of tied maxima.</summary>
        [Benchmark] public int MaximaArray() => _array.ToValueEnumerable().Maxima(MatrixSources.Bucket, Comparer<int>.Default).Count;

        /// <summary><c>Maxima</c> over the list arm.</summary>
        [Benchmark] public int MaximaList() => _list.ToValueEnumerable().Maxima(MatrixSources.Bucket, Comparer<int>.Default).Count;

        /// <summary><c>Maxima</c> over the read-only-list arm.</summary>
        [Benchmark] public int MaximaReadOnlyList() => _readOnly.ToValueEnumerable().Maxima(MatrixSources.Bucket, Comparer<int>.Default).Count;

        /// <summary><c>Maxima</c> over the enumerable arm.</summary>
        [Benchmark] public int MaximaEnumerable() => _sequence.ToValueEnumerable().Maxima(MatrixSources.Bucket, Comparer<int>.Default).Count;

        // ----- Minima -----

        /// <summary><c>Minima</c> over the array arm; returns the number of tied minima.</summary>
        [Benchmark] public int MinimaArray() => _array.ToValueEnumerable().Minima(MatrixSources.Bucket, Comparer<int>.Default).Count;

        /// <summary><c>Minima</c> over the list arm.</summary>
        [Benchmark] public int MinimaList() => _list.ToValueEnumerable().Minima(MatrixSources.Bucket, Comparer<int>.Default).Count;

        /// <summary><c>Minima</c> over the read-only-list arm.</summary>
        [Benchmark] public int MinimaReadOnlyList() => _readOnly.ToValueEnumerable().Minima(MatrixSources.Bucket, Comparer<int>.Default).Count;

        /// <summary><c>Minima</c> over the enumerable arm.</summary>
        [Benchmark] public int MinimaEnumerable() => _sequence.ToValueEnumerable().Minima(MatrixSources.Bucket, Comparer<int>.Default).Count;

        // ----- Chunk -----

        /// <summary><c>Chunk</c> over the array arm; counts the chunks, not their contents.</summary>
        [Benchmark] public long ChunkArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().Chunk(MatrixSources.WindowSize)) { n++; } return n; }

        /// <summary><c>Chunk</c> over the list arm.</summary>
        [Benchmark] public long ChunkList() { long n = 0; foreach (var v in _list.ToValueEnumerable().Chunk(MatrixSources.WindowSize)) { n++; } return n; }

        /// <summary><c>Chunk</c> over the read-only-list arm.</summary>
        [Benchmark] public long ChunkReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().Chunk(MatrixSources.WindowSize)) { n++; } return n; }

        /// <summary><c>Chunk</c> over the enumerable arm.</summary>
        [Benchmark] public long ChunkEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().Chunk(MatrixSources.WindowSize)) { n++; } return n; }

        // ----- PartialSortBy -----

        /// <summary><c>PartialSortBy</c> over the array arm; returns the number of ranked elements.</summary>
        [Benchmark] public int PartialSortByArray() => _array.ToValueEnumerable().PartialSortBy(MatrixSources.WindowSize, MatrixSources.Identity).Count;

        /// <summary><c>PartialSortBy</c> over the list arm.</summary>
        [Benchmark] public int PartialSortByList() => _list.ToValueEnumerable().PartialSortBy(MatrixSources.WindowSize, MatrixSources.Identity).Count;

        /// <summary><c>PartialSortBy</c> over the read-only-list arm.</summary>
        [Benchmark] public int PartialSortByReadOnlyList() => _readOnly.ToValueEnumerable().PartialSortBy(MatrixSources.WindowSize, MatrixSources.Identity).Count;

        /// <summary><c>PartialSortBy</c> over the enumerable arm.</summary>
        [Benchmark] public int PartialSortByEnumerable() => _sequence.ToValueEnumerable().PartialSortBy(MatrixSources.WindowSize, MatrixSources.Identity).Count;

        // ----- AtLeast -----

        /// <summary><c>AtLeast</c> over the array arm; the bound is the true length, so the walk is
        /// the whole source.</summary>
        [Benchmark] public bool AtLeastArray() => _array.ToValueEnumerable().AtLeast(MatrixSources.Length);

        /// <summary><c>AtLeast</c> over the list arm.</summary>
        [Benchmark] public bool AtLeastList() => _list.ToValueEnumerable().AtLeast(MatrixSources.Length);

        /// <summary><c>AtLeast</c> over the read-only-list arm.</summary>
        [Benchmark] public bool AtLeastReadOnlyList() => _readOnly.ToValueEnumerable().AtLeast(MatrixSources.Length);

        /// <summary><c>AtLeast</c> over the enumerable arm.</summary>
        [Benchmark] public bool AtLeastEnumerable() => _sequence.ToValueEnumerable().AtLeast(MatrixSources.Length);

        // ----- Exactly -----

        /// <summary><c>Exactly</c> over the array arm.</summary>
        [Benchmark] public bool ExactlyArray() => _array.ToValueEnumerable().Exactly(MatrixSources.Length);

        /// <summary><c>Exactly</c> over the list arm.</summary>
        [Benchmark] public bool ExactlyList() => _list.ToValueEnumerable().Exactly(MatrixSources.Length);

        /// <summary><c>Exactly</c> over the read-only-list arm.</summary>
        [Benchmark] public bool ExactlyReadOnlyList() => _readOnly.ToValueEnumerable().Exactly(MatrixSources.Length);

        /// <summary><c>Exactly</c> over the enumerable arm.</summary>
        [Benchmark] public bool ExactlyEnumerable() => _sequence.ToValueEnumerable().Exactly(MatrixSources.Length);

        // ----- CountBetween -----

        /// <summary><c>CountBetween</c> over the array arm; the range brackets the true length.</summary>
        [Benchmark] public bool CountBetweenArray() => _array.ToValueEnumerable().CountBetween(0, MatrixSources.Length * 2);

        /// <summary><c>CountBetween</c> over the list arm.</summary>
        [Benchmark] public bool CountBetweenList() => _list.ToValueEnumerable().CountBetween(0, MatrixSources.Length * 2);

        /// <summary><c>CountBetween</c> over the read-only-list arm.</summary>
        [Benchmark] public bool CountBetweenReadOnlyList() => _readOnly.ToValueEnumerable().CountBetween(0, MatrixSources.Length * 2);

        /// <summary><c>CountBetween</c> over the enumerable arm.</summary>
        [Benchmark] public bool CountBetweenEnumerable() => _sequence.ToValueEnumerable().CountBetween(0, MatrixSources.Length * 2);

        // ----- Order -----

        /// <summary><c>Order</c> over the array arm; the source is already ascending, so the row is
        /// the sort's floor rather than its worst case.</summary>
        [Benchmark] public long OrderArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().Order()) { n++; } return n; }

        /// <summary><c>Order</c> over the list arm.</summary>
        [Benchmark] public long OrderList() { long n = 0; foreach (var v in _list.ToValueEnumerable().Order()) { n++; } return n; }

        /// <summary><c>Order</c> over the read-only-list arm.</summary>
        [Benchmark] public long OrderReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().Order()) { n++; } return n; }

        /// <summary><c>Order</c> over the enumerable arm.</summary>
        [Benchmark] public long OrderEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().Order()) { n++; } return n; }

        // ----- OrderDescending -----

        /// <summary><c>OrderDescending</c> over the array arm; the source is ascending, so this is the
        /// sort's worst case for an already-ordered input.</summary>
        [Benchmark] public long OrderDescendingArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().OrderDescending()) { n++; } return n; }

        /// <summary><c>OrderDescending</c> over the list arm.</summary>
        [Benchmark] public long OrderDescendingList() { long n = 0; foreach (var v in _list.ToValueEnumerable().OrderDescending()) { n++; } return n; }

        /// <summary><c>OrderDescending</c> over the read-only-list arm.</summary>
        [Benchmark] public long OrderDescendingReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().OrderDescending()) { n++; } return n; }

        /// <summary><c>OrderDescending</c> over the enumerable arm.</summary>
        [Benchmark] public long OrderDescendingEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().OrderDescending()) { n++; } return n; }
    }
}
