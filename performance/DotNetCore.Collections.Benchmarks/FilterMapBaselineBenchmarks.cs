using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections.Benchmarks
{
    // F7-04: the filter / map half of the baseline suite.
    //
    // Layout: one class per (family, source kind) rather than one class per family. BenchmarkDotNet
    // permits exactly one Baseline=true per benchmark class, and the Gate is read as a ratio
    // against System.Linq *within the same source kind* - so each class holds one source kind's
    // Linq arm (the baseline) and its engine arm, and the report's Ratio column is the Gate number
    // directly: engine / Linq, where <= 0.90 means the 10% bar was cleared.
    //
    // Every class also carries a LinqControl arm: a second method whose body is byte-identical to
    // the baseline's. Its Ratio is the drift probe - it measures what "no difference at all"
    // reports on this machine at this moment. A Gate reading is only trusted when the engine's
    // distance from 1.00 exceeds the control's by more than the bar; see GateConfig for why the
    // machine needs one.
    //
    // The engine arm reaches the operators through DotNetCore.Collections.Internal on purpose: F7-03
    // deliberately kept the operator surface off the public API until F7-07 settles the namespace,
    // so measuring it means measuring it as an internal. Nothing else about the arm is special.

    /// <summary>
    /// <c>Where</c> over an <see cref="System.Array"/>: the engine's array arm against
    /// <c>System.Linq</c>. The Linq arm walks the array through an interface enumerator (the array's
    /// generic enumerator is a class); the engine arm indexes the array directly.
    /// </summary>
    [MemoryDiagnoser]
    public class WhereArrayBaseline
    {
        private int[] _source;

        /// <summary>Builds the source once, outside the measured region.</summary>
        [GlobalSetup]
        public void Setup() => _source = BaselineSources.CreateArray();

        /// <summary>The reference arm.</summary>
        /// <returns>The sum of the surviving elements, so the JIT cannot eliminate the walk.</returns>
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

    /// <summary><c>Where</c> over a <see cref="List{T}"/>.</summary>
    [MemoryDiagnoser]
    public class WhereListBaseline
    {
        private List<int> _source;

        /// <summary>Builds the source once, outside the measured region.</summary>
        [GlobalSetup]
        public void Setup() => _source = BaselineSources.CreateList();

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

    /// <summary><c>Select</c> over an <see cref="System.Array"/>.</summary>
    [MemoryDiagnoser]
    public class SelectArrayBaseline
    {
        private int[] _source;

        /// <summary>Builds the source once, outside the measured region.</summary>
        [GlobalSetup]
        public void Setup() => _source = BaselineSources.CreateArray();

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

    /// <summary><c>Select</c> over a <see cref="List{T}"/>.</summary>
    [MemoryDiagnoser]
    public class SelectListBaseline
    {
        private List<int> _source;

        /// <summary>Builds the source once, outside the measured region.</summary>
        [GlobalSetup]
        public void Setup() => _source = BaselineSources.CreateList();

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

    /// <summary>
    /// The headline arm: the fused <c>WhereSelect</c> against <c>Where(...).Select(...)</c>. This is
    /// the pair the F7-04 Go/No-Go criterion names, and the only one where the engine is not merely
    /// avoiding an allocation but doing strictly less work - the fused pass never re-reads a
    /// surviving element to hand it to the selector.
    /// </summary>
    [MemoryDiagnoser]
    public class WhereSelectArrayBaseline
    {
        private int[] _source;

        /// <summary>Builds the source once, outside the measured region.</summary>
        [GlobalSetup]
        public void Setup() => _source = BaselineSources.CreateArray();

        /// <summary>The reference arm: two chained operators, so a surviving element is read
        /// twice - once by the filter and once by the projection.</summary>
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

    /// <summary>The headline arm, over a <see cref="List{T}"/>.</summary>
    [MemoryDiagnoser]
    public class WhereSelectListBaseline
    {
        private List<int> _source;

        /// <summary>Builds the source once, outside the measured region.</summary>
        [GlobalSetup]
        public void Setup() => _source = BaselineSources.CreateList();

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
}
