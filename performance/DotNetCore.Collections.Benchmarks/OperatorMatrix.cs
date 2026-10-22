using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections.Benchmarks
{
    // F7-15: the operator matrix, part 1 of 5 - the core tier.
    //
    // Shape: one [Benchmark] per (operator, source arm); the four arms of an operator sit together
    // and end in Array / List / ReadOnlyList / Enumerable, so a report row's operator is the name
    // with that suffix trimmed. There is no [Params] source-kind switch and no shared drain helper,
    // on purpose. The engine declares its operators one member per arm (C# does not infer a type
    // argument from a generic constraint - see Operators/CollectionExtensions.cs), so a drain typed
    // on IValueEnumerable<T, TEnumerator> would walk its argument through IEnumerator<T>, boxing the
    // struct enumerable and every int. That is the one cost this matrix exists to prove absent, so
    // each body is written against the concrete wrapper and foreach binds to its public value-type
    // GetEnumerator. The bodies are written on one line each for the same reason the arms are
    // adjacent: 200 rows read as a table, not as 200 methods.
    //
    // Consumption rule: every sequence-yielding row counts the elements the operator yields and
    // nothing else. Inner sequences (Window's arrays, Chunk's chunks, Split's segments,
    // FullGroupJoin's groups) are never walked, so a row measures the operator's own pass rather
    // than the cost of materialising its children. The rule is uniform across all 200 rows; the
    // report says so, and the rows whose children would dominate are read as outer-pass costs.
    //
    // No `using System.Linq` on purpose: the matrix never calls a BCL operator, and leaving it out
    // means no engine call can silently bind to a BCL extension instead of the engine's own.

    /// <summary>
    /// The F7-15 operator matrix: every public operator name the engine exposes over an element
    /// source, measured on all four source arms.
    /// </summary>
    /// <remarks>
    /// The class is split across five files by tier - core, set and ranking, window and pairing,
    /// join and merge, fault and factory - so each file stays readable. Fields and setup live here.
    /// </remarks>
    [MemoryDiagnoser]
    [Config(typeof(MatrixConfig))]
    public partial class OperatorMatrix
    {
        private int[] _array;
        private List<int> _list;
        private IReadOnlyList<int> _readOnly;
        private IEnumerable<int> _sequence;

        private int[] _second;
        private int[] _small;
        private int[] _sortedSecond;
        private IEnumerable<int>[] _nested;
        private System.Random _random;

        /// <summary>Builds every source once, outside the measured region.</summary>
        [GlobalSetup]
        public void Setup() {
            _array = MatrixSources.CreateArray();
            _list = MatrixSources.CreateList();
            _readOnly = MatrixSources.CreateReadOnlyList(_list);
            _sequence = new SequenceSource(MatrixSources.Length);

            _second = MatrixSources.CreateSecond();
            _small = MatrixSources.CreateSmall();
            _sortedSecond = MatrixSources.CreateSortedSecond();

            // Built element by element rather than as an array literal: the Framework leg of this
            // suite must not depend on a static data blob (see BaselineSources for the CLR defect).
            _nested = new IEnumerable<int>[1];
            _nested[0] = _small;

            _random = new System.Random(20261023);
        }

        // ----- Index -----

        /// <summary><c>Index</c> over the array arm.</summary>
        [Benchmark] public long IndexArray() { long n = 0; foreach (var p in _array.ToValueEnumerable().Index()) { n++; } return n; }

        /// <summary><c>Index</c> over the list arm.</summary>
        [Benchmark] public long IndexList() { long n = 0; foreach (var p in _list.ToValueEnumerable().Index()) { n++; } return n; }

        /// <summary><c>Index</c> over the read-only-list arm.</summary>
        [Benchmark] public long IndexReadOnlyList() { long n = 0; foreach (var p in _readOnly.ToValueEnumerable().Index()) { n++; } return n; }

        /// <summary><c>Index</c> over the enumerable arm.</summary>
        [Benchmark] public long IndexEnumerable() { long n = 0; foreach (var p in _sequence.ToValueEnumerable().Index()) { n++; } return n; }

        // ----- ForEach -----

        /// <summary><c>ForEach</c> over the array arm; returns the last element visited.</summary>
        [Benchmark] public int ForEachArray() { _array.ToValueEnumerable().ForEach(MatrixSources.Observe); return MatrixSources.Sink; }

        /// <summary><c>ForEach</c> over the list arm; returns the last element visited.</summary>
        [Benchmark] public int ForEachList() { _list.ToValueEnumerable().ForEach(MatrixSources.Observe); return MatrixSources.Sink; }

        /// <summary><c>ForEach</c> over the read-only-list arm; returns the last element visited.</summary>
        [Benchmark] public int ForEachReadOnlyList() { _readOnly.ToValueEnumerable().ForEach(MatrixSources.Observe); return MatrixSources.Sink; }

        /// <summary><c>ForEach</c> over the enumerable arm; returns the last element visited.</summary>
        [Benchmark] public int ForEachEnumerable() { _sequence.ToValueEnumerable().ForEach(MatrixSources.Observe); return MatrixSources.Sink; }

        // ----- TagFirstLast -----

        /// <summary><c>TagFirstLast</c> over the array arm.</summary>
        [Benchmark] public long TagFirstLastArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().TagFirstLast(MatrixSources.Tag)) { n++; } return n; }

        /// <summary><c>TagFirstLast</c> over the list arm.</summary>
        [Benchmark] public long TagFirstLastList() { long n = 0; foreach (var v in _list.ToValueEnumerable().TagFirstLast(MatrixSources.Tag)) { n++; } return n; }

        /// <summary><c>TagFirstLast</c> over the read-only-list arm.</summary>
        [Benchmark] public long TagFirstLastReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().TagFirstLast(MatrixSources.Tag)) { n++; } return n; }

        /// <summary><c>TagFirstLast</c> over the enumerable arm.</summary>
        [Benchmark] public long TagFirstLastEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().TagFirstLast(MatrixSources.Tag)) { n++; } return n; }

        // ----- Pairwise -----

        /// <summary><c>Pairwise</c> over the array arm.</summary>
        [Benchmark] public long PairwiseArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().Pairwise(MatrixSources.Add)) { n++; } return n; }

        /// <summary><c>Pairwise</c> over the list arm.</summary>
        [Benchmark] public long PairwiseList() { long n = 0; foreach (var v in _list.ToValueEnumerable().Pairwise(MatrixSources.Add)) { n++; } return n; }

        /// <summary><c>Pairwise</c> over the read-only-list arm.</summary>
        [Benchmark] public long PairwiseReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().Pairwise(MatrixSources.Add)) { n++; } return n; }

        /// <summary><c>Pairwise</c> over the enumerable arm.</summary>
        [Benchmark] public long PairwiseEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().Pairwise(MatrixSources.Add)) { n++; } return n; }

        // ----- Scan (unseeded) -----

        /// <summary><c>Scan</c> without a seed over the array arm.</summary>
        [Benchmark] public long ScanArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().Scan(MatrixSources.Add)) { n++; } return n; }

        /// <summary><c>Scan</c> without a seed over the list arm.</summary>
        [Benchmark] public long ScanList() { long n = 0; foreach (var v in _list.ToValueEnumerable().Scan(MatrixSources.Add)) { n++; } return n; }

        /// <summary><c>Scan</c> without a seed over the read-only-list arm.</summary>
        [Benchmark] public long ScanReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().Scan(MatrixSources.Add)) { n++; } return n; }

        /// <summary><c>Scan</c> without a seed over the enumerable arm.</summary>
        [Benchmark] public long ScanEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().Scan(MatrixSources.Add)) { n++; } return n; }

        // ----- Where (the internal P0 filter) -----

        /// <summary>The internal <c>Where</c> over the array arm.</summary>
        [Benchmark] public long WhereArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().Where(MatrixSources.IsEven)) { n++; } return n; }

        /// <summary>The internal <c>Where</c> over the list arm.</summary>
        [Benchmark] public long WhereList() { long n = 0; foreach (var v in _list.ToValueEnumerable().Where(MatrixSources.IsEven)) { n++; } return n; }

        /// <summary>The internal <c>Where</c> over the read-only-list arm.</summary>
        [Benchmark] public long WhereReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().Where(MatrixSources.IsEven)) { n++; } return n; }

        /// <summary>The internal <c>Where</c> over the enumerable arm.</summary>
        [Benchmark] public long WhereEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().Where(MatrixSources.IsEven)) { n++; } return n; }

        // ----- Select (the internal P0 projection) -----

        /// <summary>The internal <c>Select</c> over the array arm.</summary>
        [Benchmark] public long SelectArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().Select(MatrixSources.Increment)) { n++; } return n; }

        /// <summary>The internal <c>Select</c> over the list arm.</summary>
        [Benchmark] public long SelectList() { long n = 0; foreach (var v in _list.ToValueEnumerable().Select(MatrixSources.Increment)) { n++; } return n; }

        /// <summary>The internal <c>Select</c> over the read-only-list arm.</summary>
        [Benchmark] public long SelectReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().Select(MatrixSources.Increment)) { n++; } return n; }

        /// <summary>The internal <c>Select</c> over the enumerable arm.</summary>
        [Benchmark] public long SelectEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().Select(MatrixSources.Increment)) { n++; } return n; }

        // ----- WhereSelect (the fused P0 pass) -----

        /// <summary>The fused <c>WhereSelect</c> over the array arm.</summary>
        [Benchmark] public long WhereSelectArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().WhereSelect(MatrixSources.IsEven, MatrixSources.Increment)) { n++; } return n; }

        /// <summary>The fused <c>WhereSelect</c> over the list arm.</summary>
        [Benchmark] public long WhereSelectList() { long n = 0; foreach (var v in _list.ToValueEnumerable().WhereSelect(MatrixSources.IsEven, MatrixSources.Increment)) { n++; } return n; }

        /// <summary>The fused <c>WhereSelect</c> over the read-only-list arm.</summary>
        [Benchmark] public long WhereSelectReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().WhereSelect(MatrixSources.IsEven, MatrixSources.Increment)) { n++; } return n; }

        /// <summary>The fused <c>WhereSelect</c> over the enumerable arm.</summary>
        [Benchmark] public long WhereSelectEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().WhereSelect(MatrixSources.IsEven, MatrixSources.Increment)) { n++; } return n; }

        // ----- Do -----

        /// <summary><c>Do</c> over the array arm.</summary>
        [Benchmark] public long DoArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().Do(MatrixSources.Observe)) { n++; } return n; }

        /// <summary><c>Do</c> over the list arm.</summary>
        [Benchmark] public long DoList() { long n = 0; foreach (var v in _list.ToValueEnumerable().Do(MatrixSources.Observe)) { n++; } return n; }

        /// <summary><c>Do</c> over the read-only-list arm.</summary>
        [Benchmark] public long DoReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().Do(MatrixSources.Observe)) { n++; } return n; }

        /// <summary><c>Do</c> over the enumerable arm.</summary>
        [Benchmark] public long DoEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().Do(MatrixSources.Observe)) { n++; } return n; }

        // ----- Choose -----

        /// <summary><c>Choose</c> over the array arm.</summary>
        [Benchmark] public long ChooseArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().Choose(MatrixSources.TryHalf)) { n++; } return n; }

        /// <summary><c>Choose</c> over the list arm.</summary>
        [Benchmark] public long ChooseList() { long n = 0; foreach (var v in _list.ToValueEnumerable().Choose(MatrixSources.TryHalf)) { n++; } return n; }

        /// <summary><c>Choose</c> over the read-only-list arm.</summary>
        [Benchmark] public long ChooseReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().Choose(MatrixSources.TryHalf)) { n++; } return n; }

        /// <summary><c>Choose</c> over the enumerable arm.</summary>
        [Benchmark] public long ChooseEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().Choose(MatrixSources.TryHalf)) { n++; } return n; }
    }
}
