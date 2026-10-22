using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace DotNetCore.Collections.Benchmarks
{
    // F7-15: the operator matrix, part 4 of 5 - the join and merge tier.
    //
    // Same shape and same consumption rule as part 1. The joins pair the 10 000-element source with
    // the 1024-element second sequence keyed by x & 1023, so every key has about ten rows on the
    // outer side and one on the inner: the lookup is built once and the pass is a full probe per
    // element. The two merges take the ascending source and an ascending disjoint second sequence, so
    // both inputs are ordered - which is what OrderedMerge requires and what SortedMerge gets to sort
    // anyway.

    /// <summary>Join and merge operators of the F7-15 matrix.</summary>
    public partial class OperatorMatrix
    {
        // ----- FullJoin -----

        /// <summary><c>FullJoin</c> over the array arm.</summary>
        [Benchmark] public long FullJoinArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().FullJoin(_second, MatrixSources.Bucket, MatrixSources.Bucket, MatrixSources.Add)) { n++; } return n; }

        /// <summary><c>FullJoin</c> over the list arm.</summary>
        [Benchmark] public long FullJoinList() { long n = 0; foreach (var v in _list.ToValueEnumerable().FullJoin(_second, MatrixSources.Bucket, MatrixSources.Bucket, MatrixSources.Add)) { n++; } return n; }

        /// <summary><c>FullJoin</c> over the read-only-list arm.</summary>
        [Benchmark] public long FullJoinReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().FullJoin(_second, MatrixSources.Bucket, MatrixSources.Bucket, MatrixSources.Add)) { n++; } return n; }

        /// <summary><c>FullJoin</c> over the enumerable arm.</summary>
        [Benchmark] public long FullJoinEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().FullJoin(_second, MatrixSources.Bucket, MatrixSources.Bucket, MatrixSources.Add)) { n++; } return n; }

        // ----- LeftJoin -----

        /// <summary><c>LeftJoin</c> over the array arm.</summary>
        [Benchmark] public long LeftJoinArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().LeftJoin(_second, MatrixSources.Bucket, MatrixSources.Bucket, MatrixSources.Add)) { n++; } return n; }

        /// <summary><c>LeftJoin</c> over the list arm.</summary>
        [Benchmark] public long LeftJoinList() { long n = 0; foreach (var v in _list.ToValueEnumerable().LeftJoin(_second, MatrixSources.Bucket, MatrixSources.Bucket, MatrixSources.Add)) { n++; } return n; }

        /// <summary><c>LeftJoin</c> over the read-only-list arm.</summary>
        [Benchmark] public long LeftJoinReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().LeftJoin(_second, MatrixSources.Bucket, MatrixSources.Bucket, MatrixSources.Add)) { n++; } return n; }

        /// <summary><c>LeftJoin</c> over the enumerable arm.</summary>
        [Benchmark] public long LeftJoinEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().LeftJoin(_second, MatrixSources.Bucket, MatrixSources.Bucket, MatrixSources.Add)) { n++; } return n; }

        // ----- RightJoin -----

        /// <summary><c>RightJoin</c> over the array arm.</summary>
        [Benchmark] public long RightJoinArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().RightJoin(_second, MatrixSources.Bucket, MatrixSources.Bucket, MatrixSources.Add)) { n++; } return n; }

        /// <summary><c>RightJoin</c> over the list arm.</summary>
        [Benchmark] public long RightJoinList() { long n = 0; foreach (var v in _list.ToValueEnumerable().RightJoin(_second, MatrixSources.Bucket, MatrixSources.Bucket, MatrixSources.Add)) { n++; } return n; }

        /// <summary><c>RightJoin</c> over the read-only-list arm.</summary>
        [Benchmark] public long RightJoinReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().RightJoin(_second, MatrixSources.Bucket, MatrixSources.Bucket, MatrixSources.Add)) { n++; } return n; }

        /// <summary><c>RightJoin</c> over the enumerable arm.</summary>
        [Benchmark] public long RightJoinEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().RightJoin(_second, MatrixSources.Bucket, MatrixSources.Bucket, MatrixSources.Add)) { n++; } return n; }

        // ----- FullGroupJoin -----

        /// <summary><c>FullGroupJoin</c> over the array arm; the result selector returns the key, so
        /// the inner groups are never materialised.</summary>
        [Benchmark] public long FullGroupJoinArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().FullGroupJoin(_second, MatrixSources.Bucket, MatrixSources.Bucket, MatrixSources.KeyOf)) { n++; } return n; }

        /// <summary><c>FullGroupJoin</c> over the list arm.</summary>
        [Benchmark] public long FullGroupJoinList() { long n = 0; foreach (var v in _list.ToValueEnumerable().FullGroupJoin(_second, MatrixSources.Bucket, MatrixSources.Bucket, MatrixSources.KeyOf)) { n++; } return n; }

        /// <summary><c>FullGroupJoin</c> over the read-only-list arm.</summary>
        [Benchmark] public long FullGroupJoinReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().FullGroupJoin(_second, MatrixSources.Bucket, MatrixSources.Bucket, MatrixSources.KeyOf)) { n++; } return n; }

        /// <summary><c>FullGroupJoin</c> over the enumerable arm.</summary>
        [Benchmark] public long FullGroupJoinEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().FullGroupJoin(_second, MatrixSources.Bucket, MatrixSources.Bucket, MatrixSources.KeyOf)) { n++; } return n; }

        // ----- OrderedMerge -----

        /// <summary><c>OrderedMerge</c> over the array arm; both inputs are ascending and disjoint.</summary>
        [Benchmark] public long OrderedMergeArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().OrderedMerge(_sortedSecond, Comparer<int>.Default)) { n++; } return n; }

        /// <summary><c>OrderedMerge</c> over the list arm.</summary>
        [Benchmark] public long OrderedMergeList() { long n = 0; foreach (var v in _list.ToValueEnumerable().OrderedMerge(_sortedSecond, Comparer<int>.Default)) { n++; } return n; }

        /// <summary><c>OrderedMerge</c> over the read-only-list arm.</summary>
        [Benchmark] public long OrderedMergeReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().OrderedMerge(_sortedSecond, Comparer<int>.Default)) { n++; } return n; }

        /// <summary><c>OrderedMerge</c> over the enumerable arm.</summary>
        [Benchmark] public long OrderedMergeEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().OrderedMerge(_sortedSecond, Comparer<int>.Default)) { n++; } return n; }

        // ----- SortedMerge -----

        /// <summary><c>SortedMerge</c> over the array arm; the sort runs even though both inputs are
        /// already ordered, which is the operator's stated contract.</summary>
        [Benchmark] public long SortedMergeArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().SortedMerge(_sortedSecond, Comparer<int>.Default)) { n++; } return n; }

        /// <summary><c>SortedMerge</c> over the list arm.</summary>
        [Benchmark] public long SortedMergeList() { long n = 0; foreach (var v in _list.ToValueEnumerable().SortedMerge(_sortedSecond, Comparer<int>.Default)) { n++; } return n; }

        /// <summary><c>SortedMerge</c> over the read-only-list arm.</summary>
        [Benchmark] public long SortedMergeReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().SortedMerge(_sortedSecond, Comparer<int>.Default)) { n++; } return n; }

        /// <summary><c>SortedMerge</c> over the enumerable arm.</summary>
        [Benchmark] public long SortedMergeEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().SortedMerge(_sortedSecond, Comparer<int>.Default)) { n++; } return n; }
    }
}
