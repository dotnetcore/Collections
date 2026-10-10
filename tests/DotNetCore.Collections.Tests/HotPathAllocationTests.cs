using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DotNetCore.Collections;
using DotNetCore.Collections.Internal;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Tests
{
    /// <summary>
    /// F7-04: the allocation half of the Gate, stated as a test rather than read off a benchmark
    /// table.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Gate asks for zero allocation on the walk hot path, and BenchmarkDotNet reports it - but
    /// its number is a per-operation average whose granularity moves with the iteration's operation
    /// count: the same engine code reports <c>0 B</c> per op when a measured iteration carries 2048
    /// operations and <c>1 B</c> per op when it carries 656. A quantity that changes with the
    /// harness's block size is not a property of the code under test, so the benchmark column can
    /// show the two orders of magnitude between the arms but cannot settle whether the engine's
    /// floor is exactly zero.
    /// </para>
    /// <para>
    /// <see cref="GC.GetAllocatedBytesForCurrentThread"/> settles it: it is the allocator's own
    /// counter, and over thousands of walks it reports exactly what the walk allocated. Each fact
    /// warms the path first, then measures a batch of walks with every delegate built outside the
    /// measured region, so the only bytes that can land in the window are the walk's own.
    /// </para>
    /// <para>
    /// The source length matches the benchmark suite's, so "zero" here is zero on the same shape the
    /// Gate was read from.
    /// </para>
    /// </remarks>
    public class HotPathAllocationTests
    {
        /// <summary>The element count of every source, matching the benchmark suite.</summary>
        private const int Length = 100000;

        /// <summary>Walks per measured batch. Large enough that a per-call allocation could not hide:
        /// one 24-byte box per walk would show as tens of kilobytes.</summary>
        private const int Walks = 200;

        [Fact]
        public void Where_allocates_nothing_on_either_source_kind()
        {
            var array = BuildArray();
            var list = BuildList();
            Func<int, bool> predicate = IsEven;

            long allocated;
            using (var batch = new AllocationBatch())
            {
                allocated = batch.Measure(
                    () => SumOverArray(array, predicate) + SumOverList(list, predicate),
                    ExpectedWhereSum * 2);
            }

            allocated.ShouldBe(0);
        }

        [Fact]
        public void Select_allocates_nothing_on_either_source_kind()
        {
            var array = BuildArray();
            var list = BuildList();
            Func<int, int> selector = Increment;

            long allocated;
            using (var batch = new AllocationBatch())
            {
                allocated = batch.Measure(
                    () => SumProjectedOverArray(array, selector) + SumProjectedOverList(list, selector),
                    ExpectedSelectSum * 2);
            }

            allocated.ShouldBe(0);
        }

        [Fact]
        public void The_fused_pass_allocates_nothing_on_any_of_the_three_arms()
        {
            var array = BuildArray();
            var list = BuildList();
            IReadOnlyList<int> readOnly = new List<int>(list);
            Func<int, bool> predicate = IsEven;
            Func<int, int> selector = Increment;

            long allocated;
            using (var batch = new AllocationBatch())
            {
                allocated = batch.Measure(
                    () => SumFusedOverArray(array, predicate, selector)
                        + SumFusedOverList(list, predicate, selector)
                        + SumFusedOverReadOnlyList(readOnly, predicate, selector),
                    ExpectedFusedSum * 3);
            }

            allocated.ShouldBe(0);
        }

        [Fact]
        public void Any_allocates_nothing_on_either_source_kind()
        {
            var array = BuildArray();
            var list = BuildList();
            Func<int, bool> predicate = IsNegative;

            long allocated;
            using (var batch = new AllocationBatch())
            {
                // The predicate never matches, so both arms walk the whole sequence - the same shape
                // the benchmark row measures.
                allocated = batch.Measure(
                    () => (AnyOverArray(array, predicate) || AnyOverList(list, predicate)) ? 1L : 0L,
                    0L);
            }

            allocated.ShouldBe(0);
        }

        [Fact]
        public void The_count_hook_allocates_nothing_on_either_source_kind()
        {
            var array = BuildArray();
            var list = BuildList();

            long allocated;
            using (var batch = new AllocationBatch())
            {
                // Counting through the hook the way the engine's own terminals do: a constrained
                // generic call. An interface-typed call would box the wrapper and this fact would
                // fail - which is the point of pinning it here.
                allocated = batch.Measure(
                    () => CountThroughHook<ArrayValueEnumerable<int>, ArrayValueEnumerator<int>>(array.ToValueEnumerable())
                        + CountThroughHook<ListValueEnumerable<int>, ListValueEnumerator<int>>(list.ToValueEnumerable()),
                    (long)Length * 2);
            }

            allocated.ShouldBe(0);
        }

        /// <summary>
        /// Runs one hot path many times and reports the bytes it allocated.
        /// </summary>
        /// <remarks>
        /// Wraps the measurement so every fact states only its path and its expected value. The
        /// warm-up walk is deliberately inside this helper and outside the measured window.
        /// </remarks>
        private sealed class AllocationBatch : IDisposable
        {
            private readonly long _before;

            internal AllocationBatch() {
                _before = GC.GetAllocatedBytesForCurrentThread();
            }

            /// <summary>Warms the path, then measures it over <see cref="Walks"/> walks.</summary>
            /// <param name="walk">One walk of the hot path under test.</param>
            /// <param name="expectedPerWalk">What one walk must produce, so the loop cannot be
            /// discarded as dead code and a silently broken pipeline cannot pass.</param>
            /// <returns>The bytes the measured walks allocated.</returns>
            internal long Measure(Func<long> walk, long expectedPerWalk) {
                // Warm up outside the measured region: the first walk is the one that pays for
                // tiered JIT and any one-time initialisation.
                walk().ShouldBe(expectedPerWalk);

                var before = GC.GetAllocatedBytesForCurrentThread();
                long sink = 0;
                for (var index = 0; index < Walks; index++) {
                    sink += walk();
                }

                var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

                sink.ShouldBe(expectedPerWalk * Walks);
                return allocated;
            }

            /// <summary>Kept so the batch cannot be optimised into a no-op.</summary>
            public void Dispose() => GC.KeepAlive(_before);
        }

        private static int[] BuildArray() {
            var values = new int[Length];
            for (var index = 0; index < values.Length; index++) {
                values[index] = index;
            }

            return values;
        }

        private static List<int> BuildList() {
            var values = new List<int>(Length);
            for (var index = 0; index < Length; index++) {
                values.Add(index);
            }

            return values;
        }

        /// <summary>The sum of the even elements of 0..Length-1, by a plain loop.</summary>
        internal static readonly long ExpectedWhereSum = ExpectedWhere();

        /// <summary>The sum of every element incremented, by a plain loop.</summary>
        internal static readonly long ExpectedSelectSum = ExpectedSelect();

        /// <summary>The sum of the even elements incremented, by a plain loop.</summary>
        internal static readonly long ExpectedFusedSum = ExpectedFused();

        private static long ExpectedWhere() {
            long total = 0;
            for (var index = 0; index < Length; index++) {
                if ((index & 1) == 0) {
                    total += index;
                }
            }

            return total;
        }

        private static long ExpectedSelect() {
            long total = 0;
            for (var index = 0; index < Length; index++) {
                total += index + 1;
            }

            return total;
        }

        private static long ExpectedFused() {
            long total = 0;
            for (var index = 0; index < Length; index++) {
                if ((index & 1) == 0) {
                    total += index + 1;
                }
            }

            return total;
        }

        private static bool IsEven(int value) => (value & 1) == 0;

        private static bool IsNegative(int value) => value < 0;

        private static int Increment(int value) => value + 1;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static long SumOverArray(int[] source, Func<int, bool> predicate) {
            long total = 0;
            foreach (var value in source.ToValueEnumerable().Where(predicate)) {
                total += value;
            }

            return total;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static long SumOverList(List<int> source, Func<int, bool> predicate) {
            long total = 0;
            foreach (var value in source.ToValueEnumerable().Where(predicate)) {
                total += value;
            }

            return total;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static long SumProjectedOverArray(int[] source, Func<int, int> selector) {
            long total = 0;
            foreach (var value in source.ToValueEnumerable().Select(selector)) {
                total += value;
            }

            return total;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static long SumProjectedOverList(List<int> source, Func<int, int> selector) {
            long total = 0;
            foreach (var value in source.ToValueEnumerable().Select(selector)) {
                total += value;
            }

            return total;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static long SumFusedOverArray(int[] source, Func<int, bool> predicate, Func<int, int> selector) {
            long total = 0;
            foreach (var value in source.ToValueEnumerable().WhereSelect(predicate, selector)) {
                total += value;
            }

            return total;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static long SumFusedOverList(List<int> source, Func<int, bool> predicate, Func<int, int> selector) {
            long total = 0;
            foreach (var value in source.ToValueEnumerable().WhereSelect(predicate, selector)) {
                total += value;
            }

            return total;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static long SumFusedOverReadOnlyList(IReadOnlyList<int> source, Func<int, bool> predicate, Func<int, int> selector) {
            long total = 0;
            foreach (var value in source.ToValueEnumerable().WhereSelect(predicate, selector)) {
                total += value;
            }

            return total;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AnyOverArray(int[] source, Func<int, bool> predicate) => source.ToValueEnumerable().Any(predicate);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AnyOverList(List<int> source, Func<int, bool> predicate) => source.ToValueEnumerable().Any(predicate);

        private static int CountThroughHook<TSource, TEnumerator>(TSource source)
            where TSource : struct, IValueEnumerable<int, TEnumerator>, IValueEnumerableHooks<int>
            where TEnumerator : struct, IEnumerator<int>
            => source.TryGetNonEnumeratedCount(out var count) ? count : -1;
    }
}
