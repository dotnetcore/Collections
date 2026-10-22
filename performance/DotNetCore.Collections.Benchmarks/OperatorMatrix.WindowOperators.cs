using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace DotNetCore.Collections.Benchmarks
{
    // F7-15: the operator matrix, part 3 of 5 - the window, pairing and fan-out tier.
    //
    // Same shape and same consumption rule as part 1. Two rows in this tier need a word about what
    // they measure: Window / WindowLeft count windows and never read their contents, so the row pays
    // for the sliding buffer the operator allocates per window and not for walking each window; and
    // Cartesian multiplies the 10 000-element source by a 32-element second sequence, which keeps the
    // fan-out at 320 000 pairs instead of the 100 million a same-length second sequence would give.

    /// <summary>Window, pairing and fan-out operators of the F7-15 matrix.</summary>
    public partial class OperatorMatrix
    {
        // ----- Window -----

        /// <summary><c>Window</c> over the array arm; counts windows, not their contents.</summary>
        [Benchmark] public long WindowArray() { long n = 0; foreach (var w in _array.ToValueEnumerable().Window(MatrixSources.WindowSize)) { n++; } return n; }

        /// <summary><c>Window</c> over the list arm.</summary>
        [Benchmark] public long WindowList() { long n = 0; foreach (var w in _list.ToValueEnumerable().Window(MatrixSources.WindowSize)) { n++; } return n; }

        /// <summary><c>Window</c> over the read-only-list arm.</summary>
        [Benchmark] public long WindowReadOnlyList() { long n = 0; foreach (var w in _readOnly.ToValueEnumerable().Window(MatrixSources.WindowSize)) { n++; } return n; }

        /// <summary><c>Window</c> over the enumerable arm.</summary>
        [Benchmark] public long WindowEnumerable() { long n = 0; foreach (var w in _sequence.ToValueEnumerable().Window(MatrixSources.WindowSize)) { n++; } return n; }

        // ----- WindowLeft -----

        /// <summary><c>WindowLeft</c> over the array arm; counts windows, not their contents.</summary>
        [Benchmark] public long WindowLeftArray() { long n = 0; foreach (var w in _array.ToValueEnumerable().WindowLeft(MatrixSources.WindowSize)) { n++; } return n; }

        /// <summary><c>WindowLeft</c> over the list arm.</summary>
        [Benchmark] public long WindowLeftList() { long n = 0; foreach (var w in _list.ToValueEnumerable().WindowLeft(MatrixSources.WindowSize)) { n++; } return n; }

        /// <summary><c>WindowLeft</c> over the read-only-list arm.</summary>
        [Benchmark] public long WindowLeftReadOnlyList() { long n = 0; foreach (var w in _readOnly.ToValueEnumerable().WindowLeft(MatrixSources.WindowSize)) { n++; } return n; }

        /// <summary><c>WindowLeft</c> over the enumerable arm.</summary>
        [Benchmark] public long WindowLeftEnumerable() { long n = 0; foreach (var w in _sequence.ToValueEnumerable().WindowLeft(MatrixSources.WindowSize)) { n++; } return n; }

        // ----- GroupAdjacent -----

        /// <summary><c>GroupAdjacent</c> over the array arm: 625 runs of 16 consecutive elements.</summary>
        [Benchmark] public long GroupAdjacentArray() { long n = 0; foreach (var g in _array.ToValueEnumerable().GroupAdjacent(MatrixSources.Run, EqualityComparer<int>.Default)) { n++; } return n; }

        /// <summary><c>GroupAdjacent</c> over the list arm.</summary>
        [Benchmark] public long GroupAdjacentList() { long n = 0; foreach (var g in _list.ToValueEnumerable().GroupAdjacent(MatrixSources.Run, EqualityComparer<int>.Default)) { n++; } return n; }

        /// <summary><c>GroupAdjacent</c> over the read-only-list arm.</summary>
        [Benchmark] public long GroupAdjacentReadOnlyList() { long n = 0; foreach (var g in _readOnly.ToValueEnumerable().GroupAdjacent(MatrixSources.Run, EqualityComparer<int>.Default)) { n++; } return n; }

        /// <summary><c>GroupAdjacent</c> over the enumerable arm.</summary>
        [Benchmark] public long GroupAdjacentEnumerable() { long n = 0; foreach (var g in _sequence.ToValueEnumerable().GroupAdjacent(MatrixSources.Run, EqualityComparer<int>.Default)) { n++; } return n; }

        // ----- Split -----

        /// <summary><c>Split</c> over the array arm: a separator every 1024 elements, ten segments.</summary>
        [Benchmark] public long SplitArray() { long n = 0; foreach (var s in _array.ToValueEnumerable().Split(MatrixSources.IsSeparator)) { n++; } return n; }

        /// <summary><c>Split</c> over the list arm.</summary>
        [Benchmark] public long SplitList() { long n = 0; foreach (var s in _list.ToValueEnumerable().Split(MatrixSources.IsSeparator)) { n++; } return n; }

        /// <summary><c>Split</c> over the read-only-list arm.</summary>
        [Benchmark] public long SplitReadOnlyList() { long n = 0; foreach (var s in _readOnly.ToValueEnumerable().Split(MatrixSources.IsSeparator)) { n++; } return n; }

        /// <summary><c>Split</c> over the enumerable arm.</summary>
        [Benchmark] public long SplitEnumerable() { long n = 0; foreach (var s in _sequence.ToValueEnumerable().Split(MatrixSources.IsSeparator)) { n++; } return n; }

        // ----- Lag -----

        /// <summary><c>Lag</c> (offset 1) over the array arm.</summary>
        [Benchmark] public long LagArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().Lag(MatrixSources.PickSecond)) { n++; } return n; }

        /// <summary><c>Lag</c> (offset 1) over the list arm.</summary>
        [Benchmark] public long LagList() { long n = 0; foreach (var v in _list.ToValueEnumerable().Lag(MatrixSources.PickSecond)) { n++; } return n; }

        /// <summary><c>Lag</c> (offset 1) over the read-only-list arm.</summary>
        [Benchmark] public long LagReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().Lag(MatrixSources.PickSecond)) { n++; } return n; }

        /// <summary><c>Lag</c> (offset 1) over the enumerable arm.</summary>
        [Benchmark] public long LagEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().Lag(MatrixSources.PickSecond)) { n++; } return n; }

        // ----- Lead -----

        /// <summary><c>Lead</c> (offset 1) over the array arm.</summary>
        [Benchmark] public long LeadArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().Lead(MatrixSources.PickSecond)) { n++; } return n; }

        /// <summary><c>Lead</c> (offset 1) over the list arm.</summary>
        [Benchmark] public long LeadList() { long n = 0; foreach (var v in _list.ToValueEnumerable().Lead(MatrixSources.PickSecond)) { n++; } return n; }

        /// <summary><c>Lead</c> (offset 1) over the read-only-list arm.</summary>
        [Benchmark] public long LeadReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().Lead(MatrixSources.PickSecond)) { n++; } return n; }

        /// <summary><c>Lead</c> (offset 1) over the enumerable arm.</summary>
        [Benchmark] public long LeadEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().Lead(MatrixSources.PickSecond)) { n++; } return n; }

        // ----- Interleave -----

        /// <summary><c>Interleave</c> over the array arm, with one 32-element sequence beside it.</summary>
        [Benchmark] public long InterleaveArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().Interleave(_nested)) { n++; } return n; }

        /// <summary><c>Interleave</c> over the list arm.</summary>
        [Benchmark] public long InterleaveList() { long n = 0; foreach (var v in _list.ToValueEnumerable().Interleave(_nested)) { n++; } return n; }

        /// <summary><c>Interleave</c> over the read-only-list arm.</summary>
        [Benchmark] public long InterleaveReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().Interleave(_nested)) { n++; } return n; }

        /// <summary><c>Interleave</c> over the enumerable arm.</summary>
        [Benchmark] public long InterleaveEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().Interleave(_nested)) { n++; } return n; }

        // ----- ZipLongest -----

        /// <summary><c>ZipLongest</c> over the array arm, paired with the 1024-element sequence.</summary>
        [Benchmark] public long ZipLongestArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().ZipLongest(_second, MatrixSources.Add)) { n++; } return n; }

        /// <summary><c>ZipLongest</c> over the list arm.</summary>
        [Benchmark] public long ZipLongestList() { long n = 0; foreach (var v in _list.ToValueEnumerable().ZipLongest(_second, MatrixSources.Add)) { n++; } return n; }

        /// <summary><c>ZipLongest</c> over the read-only-list arm.</summary>
        [Benchmark] public long ZipLongestReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().ZipLongest(_second, MatrixSources.Add)) { n++; } return n; }

        /// <summary><c>ZipLongest</c> over the enumerable arm.</summary>
        [Benchmark] public long ZipLongestEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().ZipLongest(_second, MatrixSources.Add)) { n++; } return n; }

        // ----- Cartesian -----

        /// <summary><c>Cartesian</c> over the array arm: 10 000 x 32 pairs.</summary>
        [Benchmark] public long CartesianArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().Cartesian(_small, MatrixSources.Add)) { n++; } return n; }

        /// <summary><c>Cartesian</c> over the list arm.</summary>
        [Benchmark] public long CartesianList() { long n = 0; foreach (var v in _list.ToValueEnumerable().Cartesian(_small, MatrixSources.Add)) { n++; } return n; }

        /// <summary><c>Cartesian</c> over the read-only-list arm.</summary>
        [Benchmark] public long CartesianReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().Cartesian(_small, MatrixSources.Add)) { n++; } return n; }

        /// <summary><c>Cartesian</c> over the enumerable arm.</summary>
        [Benchmark] public long CartesianEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().Cartesian(_small, MatrixSources.Add)) { n++; } return n; }

        // ----- Shuffle -----

        /// <summary><c>Shuffle</c> over the array arm; returns the shuffled copy's length. The
        /// generator is a fixed-seed instance so the row is reproducible run to run.</summary>
        [Benchmark] public int ShuffleArray() => _array.ToValueEnumerable().Shuffle(_random).Count;

        /// <summary><c>Shuffle</c> over the list arm.</summary>
        [Benchmark] public int ShuffleList() => _list.ToValueEnumerable().Shuffle(_random).Count;

        /// <summary><c>Shuffle</c> over the read-only-list arm.</summary>
        [Benchmark] public int ShuffleReadOnlyList() => _readOnly.ToValueEnumerable().Shuffle(_random).Count;

        /// <summary><c>Shuffle</c> over the enumerable arm.</summary>
        [Benchmark] public int ShuffleEnumerable() => _sequence.ToValueEnumerable().Shuffle(_random).Count;
    }
}
