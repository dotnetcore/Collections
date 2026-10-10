using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections.Benchmarks
{
    // F7-04: the terminal half of the baseline suite. Same layout rule as the filter / map file -
    // one class per (family, source kind), one Baseline=true per class, so the report's Ratio column
    // is the Gate number (engine / Linq, <= 0.90 clears the 10% bar) - and the same LinqControl
    // drift probe in every class.

    /// <summary>
    /// <c>Any</c> over an <see cref="System.Array"/>. The predicate never matches, so both arms walk
    /// the whole sequence and the row measures the walk rather than the short circuit. Short-circuit
    /// behaviour is pinned by the F7-03 unit tests, not by a timing row.
    /// </summary>
    [MemoryDiagnoser]
    public class AnyArrayBaseline
    {
        private int[] _source;

        /// <summary>Builds the source once, outside the measured region.</summary>
        [GlobalSetup]
        public void Setup() => _source = BaselineSources.CreateArray();

        /// <summary>The reference arm.</summary>
        /// <returns>Always <see langword="false"/> for this source; the value only stops the JIT
        /// from eliminating the walk.</returns>
        [Benchmark(Baseline = true)]
        public bool Linq() => _source.Any(BaselineSources.IsNegative);

        /// <summary>The drift probe; see the file header.</summary>
        /// <returns>Always <see langword="false"/> for this source.</returns>
        [Benchmark]
        public bool LinqControl() => _source.Any(BaselineSources.IsNegative);

        /// <summary>The engine arm.</summary>
        /// <returns>Always <see langword="false"/> for this source.</returns>
        [Benchmark]
        public bool Engine() => _source.ToValueEnumerable().Any(BaselineSources.IsNegative);
    }

    /// <summary><c>Any</c> over a <see cref="List{T}"/>, walking the whole sequence.</summary>
    [MemoryDiagnoser]
    public class AnyListBaseline
    {
        private List<int> _source;

        /// <summary>Builds the source once, outside the measured region.</summary>
        [GlobalSetup]
        public void Setup() => _source = BaselineSources.CreateList();

        /// <summary>The reference arm.</summary>
        /// <returns>Always <see langword="false"/> for this source.</returns>
        [Benchmark(Baseline = true)]
        public bool Linq() => _source.Any(BaselineSources.IsNegative);

        /// <summary>The drift probe; see the file header.</summary>
        /// <returns>Always <see langword="false"/> for this source.</returns>
        [Benchmark]
        public bool LinqControl() => _source.Any(BaselineSources.IsNegative);

        /// <summary>The engine arm.</summary>
        /// <returns>Always <see langword="false"/> for this source.</returns>
        [Benchmark]
        public bool Engine() => _source.ToValueEnumerable().Any(BaselineSources.IsNegative);
    }

    /// <summary>
    /// The count capability over an <see cref="System.Array"/>. F7-03 ships no <c>Count</c> terminal
    /// - the count capability is the <c>TryGetNonEnumeratedCount</c> hook, which is what the engine's
    /// own terminals and the paging layer consume - so the engine arm here calls the hook the way
    /// they do, and the Linq arm is <c>Enumerable.Count()</c>, which resolves an array through its
    /// own <c>ICollection&lt;T&gt;</c> fast path and is therefore O(1) as well. A parity result is the
    /// expected outcome; the row exists to prove the hook does not cost more than the BCL's own
    /// no-walk path.
    /// </summary>
    [MemoryDiagnoser]
    public class CountArrayBaseline
    {
        private int[] _source;

        /// <summary>Builds the source once, outside the measured region.</summary>
        [GlobalSetup]
        public void Setup() => _source = BaselineSources.CreateArray();

        /// <summary>The reference arm.</summary>
        /// <returns>The element count.</returns>
        [Benchmark(Baseline = true)]
        public int Linq() => _source.Count();

        /// <summary>The drift probe; see the file header.</summary>
        /// <returns>The element count.</returns>
        [Benchmark]
        public int LinqControl() => _source.Count();

        /// <summary>The engine arm, through the count hook.</summary>
        /// <returns>The element count.</returns>
        [Benchmark]
        public int Engine() => CountHook<ArrayValueEnumerable<int>, ArrayValueEnumerator<int>>(_source.ToValueEnumerable());

        /// <summary>
        /// Consumes the count hook exactly the way the engine's own terminals do: through a
        /// constrained generic call, which the JIT devirtualises. Calling the hook through an
        /// interface reference instead would box the wrapper struct, and that allocation would land
        /// in the measurement rather than in the engine.
        /// </summary>
        /// <typeparam name="TSource">The value enumerable being counted.</typeparam>
        /// <typeparam name="TEnumerator">Its value-type enumerator.</typeparam>
        /// <param name="source">The sequence to count.</param>
        /// <returns>The count the hook reported, or -1 when it declined.</returns>
        private static int CountHook<TSource, TEnumerator>(TSource source)
            where TSource : struct, IValueEnumerable<int, TEnumerator>, IValueEnumerableHooks<int>
            where TEnumerator : struct, IEnumerator<int>
            => source.TryGetNonEnumeratedCount(out var count) ? count : -1;
    }

    /// <summary>
    /// The count capability over a <see cref="List{T}"/>. Same shape as the array class;
    /// <c>Enumerable.Count()</c> reaches a list's own <c>Count</c>, and the engine's list arm answers
    /// the hook with the same value.
    /// </summary>
    [MemoryDiagnoser]
    public class CountListBaseline
    {
        private List<int> _source;

        /// <summary>Builds the source once, outside the measured region.</summary>
        [GlobalSetup]
        public void Setup() => _source = BaselineSources.CreateList();

        /// <summary>The reference arm.</summary>
        /// <returns>The element count.</returns>
        [Benchmark(Baseline = true)]
        public int Linq() => _source.Count();

        /// <summary>The drift probe; see the file header.</summary>
        /// <returns>The element count.</returns>
        [Benchmark]
        public int LinqControl() => _source.Count();

        /// <summary>The engine arm, through the count hook.</summary>
        /// <returns>The element count.</returns>
        [Benchmark]
        public int Engine() => CountHook<ListValueEnumerable<int>, ListValueEnumerator<int>>(_source.ToValueEnumerable());

        /// <summary>See <see cref="CountArrayBaseline"/>: the constrained call is the point.</summary>
        /// <typeparam name="TSource">The value enumerable being counted.</typeparam>
        /// <typeparam name="TEnumerator">Its value-type enumerator.</typeparam>
        /// <param name="source">The sequence to count.</param>
        /// <returns>The count the hook reported, or -1 when it declined.</returns>
        private static int CountHook<TSource, TEnumerator>(TSource source)
            where TSource : struct, IValueEnumerable<int, TEnumerator>, IValueEnumerableHooks<int>
            where TEnumerator : struct, IEnumerator<int>
            => source.TryGetNonEnumeratedCount(out var count) ? count : -1;
    }
}
