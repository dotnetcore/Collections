using BenchmarkDotNet.Attributes;

namespace DotNetCore.Collections.Benchmarks
{
    // F7-15: the operator matrix, part 5 of 5 - the fault-tolerance and repetition tier.
    //
    // Same shape and same consumption rule as part 1. Every row here runs a source that never throws:
    // Catch and OnErrorResumeNext therefore walk the first sequence and stop before the second, and
    // Retry succeeds on its first attempt. That is the honest steady-state reading - the operators'
    // recovery paths are pinned by the F7-14 unit tests, and timing a throw would measure the
    // exception machinery rather than the operator. Repeat is the one row that multiplies the input:
    // three rounds of 10 000 elements.
    //
    // The factories themselves (Return, Throw, Defer, Using) take no element source and are therefore
    // outside the matrix by construction; the F7-15 report records that as a scope note rather than
    // leaving four operators silently missing.

    /// <summary>Fault-tolerance and repetition operators of the F7-15 matrix.</summary>
    public partial class OperatorMatrix
    {
        // ----- Catch -----

        /// <summary><c>Catch</c> over the array arm; the source does not throw, so the fallback is
        /// never walked.</summary>
        [Benchmark] public long CatchArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().Catch(_second)) { n++; } return n; }

        /// <summary><c>Catch</c> over the list arm.</summary>
        [Benchmark] public long CatchList() { long n = 0; foreach (var v in _list.ToValueEnumerable().Catch(_second)) { n++; } return n; }

        /// <summary><c>Catch</c> over the read-only-list arm.</summary>
        [Benchmark] public long CatchReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().Catch(_second)) { n++; } return n; }

        /// <summary><c>Catch</c> over the enumerable arm.</summary>
        [Benchmark] public long CatchEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().Catch(_second)) { n++; } return n; }

        // ----- Retry -----

        /// <summary><c>Retry</c> over the array arm; one spare attempt, unused because the source
        /// completes.</summary>
        [Benchmark] public long RetryArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().Retry(1)) { n++; } return n; }

        /// <summary><c>Retry</c> over the list arm.</summary>
        [Benchmark] public long RetryList() { long n = 0; foreach (var v in _list.ToValueEnumerable().Retry(1)) { n++; } return n; }

        /// <summary><c>Retry</c> over the read-only-list arm.</summary>
        [Benchmark] public long RetryReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().Retry(1)) { n++; } return n; }

        /// <summary><c>Retry</c> over the enumerable arm.</summary>
        [Benchmark] public long RetryEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().Retry(1)) { n++; } return n; }

        // ----- OnErrorResumeNext -----

        /// <summary><c>OnErrorResumeNext</c> over the array arm; with no failure the second sequence
        /// is not reached.</summary>
        [Benchmark] public long OnErrorResumeNextArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().OnErrorResumeNext(_second)) { n++; } return n; }

        /// <summary><c>OnErrorResumeNext</c> over the list arm.</summary>
        [Benchmark] public long OnErrorResumeNextList() { long n = 0; foreach (var v in _list.ToValueEnumerable().OnErrorResumeNext(_second)) { n++; } return n; }

        /// <summary><c>OnErrorResumeNext</c> over the read-only-list arm.</summary>
        [Benchmark] public long OnErrorResumeNextReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().OnErrorResumeNext(_second)) { n++; } return n; }

        /// <summary><c>OnErrorResumeNext</c> over the enumerable arm.</summary>
        [Benchmark] public long OnErrorResumeNextEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().OnErrorResumeNext(_second)) { n++; } return n; }

        // ----- Repeat -----

        /// <summary><c>Repeat</c> over the array arm: three rounds of the source.</summary>
        [Benchmark] public long RepeatArray() { long n = 0; foreach (var v in _array.ToValueEnumerable().Repeat(3)) { n++; } return n; }

        /// <summary><c>Repeat</c> over the list arm.</summary>
        [Benchmark] public long RepeatList() { long n = 0; foreach (var v in _list.ToValueEnumerable().Repeat(3)) { n++; } return n; }

        /// <summary><c>Repeat</c> over the read-only-list arm.</summary>
        [Benchmark] public long RepeatReadOnlyList() { long n = 0; foreach (var v in _readOnly.ToValueEnumerable().Repeat(3)) { n++; } return n; }

        /// <summary><c>Repeat</c> over the enumerable arm.</summary>
        [Benchmark] public long RepeatEnumerable() { long n = 0; foreach (var v in _sequence.ToValueEnumerable().Repeat(3)) { n++; } return n; }
    }
}
