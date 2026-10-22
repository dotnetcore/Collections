using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections.Benchmarks
{
    // F7-15: the P0 Gate on the two source arms the F7-04 suite never covered.
    //
    // F7-04 measured Where / Select / WhereSelect / Any / Count on T[] and List<T> only, because
    // those were the two arms whose override was in question. F7-15's brief is the four-arm matrix,
    // so the Gate has to reach the other two arms as well - and they are not filler:
    //
    //   * IReadOnlyList<T> is the arm the Paginable layer hands the engine, and it is the only arm
    //     whose per-element cost is an interface indexer call rather than a field load, so it is where
    //     a value-type enumerator can lose rather than win.
    //   * IEnumerable<T> is the arm that has no override at all: the engine delegates to the source's
    //     own enumerator, so a row here reads the cost of the wrapper and nothing else. It is the
    //     control that tells the other three arms' numbers apart from the cost of the walk itself.
    //
    // Same layout rule as the F7-04 files: one class per (family, source arm), one Baseline=true per
    // class, so the report's Ratio column is engine / Linq for that arm directly, and every class
    // carries a LinqControl drift probe whose body is byte-identical to the baseline's.
    //
    // Count is absent from the Enumerable arm on purpose: the engine's count hook answers from
    // ICollection<T> / IReadOnlyCollection<T> when the source carries one and declines otherwise, and
    // a plain sequence carries neither. Declining is the contract, not a slow path, so there is no
    // engine number to compare with Enumerable.Count()'s walk. The report states that rather than
    // publishing a row whose ratio would compare a refusal with a walk.

    /// <summary><c>Where</c> over an <see cref="IReadOnlyList{T}"/>.</summary>
    [MemoryDiagnoser]
    [Config(typeof(GateConfig))]
    public class WhereReadOnlyListBaseline
    {
        private IReadOnlyList<int> _source;

        /// <summary>Builds the source once, outside the measured region.</summary>
        [GlobalSetup]
        public void Setup() => _source = BaselineSources.CreateReadOnlyList();

        /// <summary>The reference arm.</summary>
        /// <returns>The sum of the surviving elements.</returns>
        [Benchmark(Baseline = true)]
        public int Linq() {
            var sum = 0;
            foreach (var value in _source.Where(BaselineSources.IsEven)) {
                sum += value;
            }

            return sum;
        }

        /// <summary>The drift probe; see the file header.</summary>
        /// <returns>The sum of the surviving elements.</returns>
        [Benchmark]
        public int LinqControl() {
            var sum = 0;
            foreach (var value in _source.Where(BaselineSources.IsEven)) {
                sum += value;
            }

            return sum;
        }

        /// <summary>The engine arm.</summary>
        /// <returns>The sum of the surviving elements.</returns>
        [Benchmark]
        public int Engine() {
            var sum = 0;
            foreach (var value in _source.ToValueEnumerable().Where(BaselineSources.IsEven)) {
                sum += value;
            }

            return sum;
        }
    }

    /// <summary><c>Where</c> over a plain <see cref="IEnumerable{T}"/>.</summary>
    [MemoryDiagnoser]
    [Config(typeof(GateConfig))]
    public class WhereEnumerableBaseline
    {
        private IEnumerable<int> _source;

        /// <summary>Builds the source once, outside the measured region.</summary>
        [GlobalSetup]
        public void Setup() => _source = BaselineSources.CreateSequence();

        /// <summary>The reference arm.</summary>
        /// <returns>The sum of the surviving elements.</returns>
        [Benchmark(Baseline = true)]
        public int Linq() {
            var sum = 0;
            foreach (var value in _source.Where(BaselineSources.IsEven)) {
                sum += value;
            }

            return sum;
        }

        /// <summary>The drift probe; see the file header.</summary>
        /// <returns>The sum of the surviving elements.</returns>
        [Benchmark]
        public int LinqControl() {
            var sum = 0;
            foreach (var value in _source.Where(BaselineSources.IsEven)) {
                sum += value;
            }

            return sum;
        }

        /// <summary>The engine arm.</summary>
        /// <returns>The sum of the surviving elements.</returns>
        [Benchmark]
        public int Engine() {
            var sum = 0;
            foreach (var value in _source.ToValueEnumerable().Where(BaselineSources.IsEven)) {
                sum += value;
            }

            return sum;
        }
    }

    /// <summary><c>Select</c> over an <see cref="IReadOnlyList{T}"/>.</summary>
    [MemoryDiagnoser]
    [Config(typeof(GateConfig))]
    public class SelectReadOnlyListBaseline
    {
        private IReadOnlyList<int> _source;

        /// <summary>Builds the source once, outside the measured region.</summary>
        [GlobalSetup]
        public void Setup() => _source = BaselineSources.CreateReadOnlyList();

        /// <summary>The reference arm.</summary>
        /// <returns>The sum of the projected elements.</returns>
        [Benchmark(Baseline = true)]
        public int Linq() {
            var sum = 0;
            foreach (var value in _source.Select(BaselineSources.Increment)) {
                sum += value;
            }

            return sum;
        }

        /// <summary>The drift probe; see the file header.</summary>
        /// <returns>The sum of the projected elements.</returns>
        [Benchmark]
        public int LinqControl() {
            var sum = 0;
            foreach (var value in _source.Select(BaselineSources.Increment)) {
                sum += value;
            }

            return sum;
        }

        /// <summary>The engine arm.</summary>
        /// <returns>The sum of the projected elements.</returns>
        [Benchmark]
        public int Engine() {
            var sum = 0;
            foreach (var value in _source.ToValueEnumerable().Select(BaselineSources.Increment)) {
                sum += value;
            }

            return sum;
        }
    }

    /// <summary><c>Select</c> over a plain <see cref="IEnumerable{T}"/>.</summary>
    [MemoryDiagnoser]
    [Config(typeof(GateConfig))]
    public class SelectEnumerableBaseline
    {
        private IEnumerable<int> _source;

        /// <summary>Builds the source once, outside the measured region.</summary>
        [GlobalSetup]
        public void Setup() => _source = BaselineSources.CreateSequence();

        /// <summary>The reference arm.</summary>
        /// <returns>The sum of the projected elements.</returns>
        [Benchmark(Baseline = true)]
        public int Linq() {
            var sum = 0;
            foreach (var value in _source.Select(BaselineSources.Increment)) {
                sum += value;
            }

            return sum;
        }

        /// <summary>The drift probe; see the file header.</summary>
        /// <returns>The sum of the projected elements.</returns>
        [Benchmark]
        public int LinqControl() {
            var sum = 0;
            foreach (var value in _source.Select(BaselineSources.Increment)) {
                sum += value;
            }

            return sum;
        }

        /// <summary>The engine arm.</summary>
        /// <returns>The sum of the projected elements.</returns>
        [Benchmark]
        public int Engine() {
            var sum = 0;
            foreach (var value in _source.ToValueEnumerable().Select(BaselineSources.Increment)) {
                sum += value;
            }

            return sum;
        }
    }

    /// <summary>The fused <c>WhereSelect</c> over an <see cref="IReadOnlyList{T}"/>.</summary>
    [MemoryDiagnoser]
    [Config(typeof(GateConfig))]
    public class WhereSelectReadOnlyListBaseline
    {
        private IReadOnlyList<int> _source;

        /// <summary>Builds the source once, outside the measured region.</summary>
        [GlobalSetup]
        public void Setup() => _source = BaselineSources.CreateReadOnlyList();

        /// <summary>The reference arm.</summary>
        /// <returns>The sum of the projected survivors.</returns>
        [Benchmark(Baseline = true)]
        public int Linq() {
            var sum = 0;
            foreach (var value in _source.Where(BaselineSources.IsEven).Select(BaselineSources.Increment)) {
                sum += value;
            }

            return sum;
        }

        /// <summary>The drift probe; see the file header.</summary>
        /// <returns>The sum of the projected survivors.</returns>
        [Benchmark]
        public int LinqControl() {
            var sum = 0;
            foreach (var value in _source.Where(BaselineSources.IsEven).Select(BaselineSources.Increment)) {
                sum += value;
            }

            return sum;
        }

        /// <summary>The fused engine arm.</summary>
        /// <returns>The sum of the projected survivors.</returns>
        [Benchmark]
        public int Engine() {
            var sum = 0;
            foreach (var value in _source.ToValueEnumerable().WhereSelect(BaselineSources.IsEven, BaselineSources.Increment)) {
                sum += value;
            }

            return sum;
        }
    }

    /// <summary>The fused <c>WhereSelect</c> over a plain <see cref="IEnumerable{T}"/>.</summary>
    [MemoryDiagnoser]
    [Config(typeof(GateConfig))]
    public class WhereSelectEnumerableBaseline
    {
        private IEnumerable<int> _source;

        /// <summary>Builds the source once, outside the measured region.</summary>
        [GlobalSetup]
        public void Setup() => _source = BaselineSources.CreateSequence();

        /// <summary>The reference arm.</summary>
        /// <returns>The sum of the projected survivors.</returns>
        [Benchmark(Baseline = true)]
        public int Linq() {
            var sum = 0;
            foreach (var value in _source.Where(BaselineSources.IsEven).Select(BaselineSources.Increment)) {
                sum += value;
            }

            return sum;
        }

        /// <summary>The drift probe; see the file header.</summary>
        /// <returns>The sum of the projected survivors.</returns>
        [Benchmark]
        public int LinqControl() {
            var sum = 0;
            foreach (var value in _source.Where(BaselineSources.IsEven).Select(BaselineSources.Increment)) {
                sum += value;
            }

            return sum;
        }

        /// <summary>The fused engine arm.</summary>
        /// <returns>The sum of the projected survivors.</returns>
        [Benchmark]
        public int Engine() {
            var sum = 0;
            foreach (var value in _source.ToValueEnumerable().WhereSelect(BaselineSources.IsEven, BaselineSources.Increment)) {
                sum += value;
            }

            return sum;
        }
    }

    /// <summary><c>Any</c> over an <see cref="IReadOnlyList{T}"/>; the predicate never matches, so
    /// the row measures the walk rather than the short circuit.</summary>
    [MemoryDiagnoser]
    [Config(typeof(GateConfig))]
    public class AnyReadOnlyListBaseline
    {
        private IReadOnlyList<int> _source;

        /// <summary>Builds the source once, outside the measured region.</summary>
        [GlobalSetup]
        public void Setup() => _source = BaselineSources.CreateReadOnlyList();

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

    /// <summary><c>Any</c> over a plain <see cref="IEnumerable{T}"/>.</summary>
    [MemoryDiagnoser]
    [Config(typeof(GateConfig))]
    public class AnyEnumerableBaseline
    {
        private IEnumerable<int> _source;

        /// <summary>Builds the source once, outside the measured region.</summary>
        [GlobalSetup]
        public void Setup() => _source = BaselineSources.CreateSequence();

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
    /// The count capability over an <see cref="IReadOnlyList{T}"/>. The Enumerable arm is deliberately
    /// absent; see the file header.
    /// </summary>
    [MemoryDiagnoser]
    [Config(typeof(GateConfig))]
    public class CountReadOnlyListBaseline
    {
        private IReadOnlyList<int> _source;

        /// <summary>Builds the source once, outside the measured region.</summary>
        [GlobalSetup]
        public void Setup() => _source = BaselineSources.CreateReadOnlyList();

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
        public int Engine() => CountHook<ReadOnlyListValueEnumerable<int>, ReadOnlyListValueEnumerator<int>>(_source.ToValueEnumerable());

        /// <summary>Consumes the count hook through a constrained generic call, as the engine's own
        /// terminals do; an interface reference would box the wrapper struct.</summary>
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
