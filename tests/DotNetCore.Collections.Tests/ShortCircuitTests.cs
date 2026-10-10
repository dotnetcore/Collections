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
    /// F7-03: the short-circuit terminals. What makes them worth having is not that they compute the
    /// right answer but that they stop early, so each of them is measured against a counting
    /// predicate rather than only compared to the BCL.
    /// </summary>
    public class ShortCircuitTests
    {
        [Fact]
        public void Any_answers_across_every_arm()
        {
            IEnumerable<int> arbitrary = new Queue<int>(new[] { 1, 2, 3 });
            IReadOnlyList<int> readOnly = new List<int> { 1, 2, 3 };

            new[] { 1, 2, 3 }.ToValueEnumerable().Any(value => value == 2).ShouldBeTrue();
            new[] { 1, 2, 3 }.ToValueEnumerable().Any(value => value == 9).ShouldBeFalse();

            new List<int> { 1, 2, 3 }.ToValueEnumerable().Any(value => value == 3).ShouldBeTrue();
            readOnly.ToValueEnumerable().Any(value => value == 1).ShouldBeTrue();
            arbitrary.ToValueEnumerable().Any(value => value == 3).ShouldBeTrue();
        }

        [Fact]
        public void Any_stops_at_the_first_match()
        {
            var source = new int[1000];
            for (var index = 0; index < source.Length; index++)
            {
                source[index] = index;
            }

            var calls = 0;
            var found = source.ToValueEnumerable().Any(value =>
            {
                calls++;
                return value == 0;
            });

            found.ShouldBeTrue();
            calls.ShouldBe(1);
        }

        [Fact]
        public void First_returns_the_first_match_and_stops_there()
        {
            var calls = 0;
            var value = new[] { 1, 2, 3 }.ToValueEnumerable().First(candidate =>
            {
                calls++;
                return candidate > 1;
            });

            value.ShouldBe(2);
            calls.ShouldBe(2);
        }

        [Fact]
        public void First_reports_every_arm_and_throws_when_nothing_matches()
        {
            IEnumerable<int> arbitrary = new Queue<int>(new[] { 1, 2, 3 });
            IReadOnlyList<int> readOnly = new List<int> { 1, 2, 3 };

            new List<int> { 1, 2, 3 }.ToValueEnumerable().First(value => value == 3).ShouldBe(3);
            readOnly.ToValueEnumerable().First(value => value == 3).ShouldBe(3);
            arbitrary.ToValueEnumerable().First(value => value == 3).ShouldBe(3);

            Should.Throw<InvalidOperationException>(() => new[] { 1, 2, 3 }.ToValueEnumerable().First(value => value == 9));
            Should.Throw<InvalidOperationException>(() => new List<int> { 1, 2, 3 }.ToValueEnumerable().First(value => value == 9));
            Should.Throw<InvalidOperationException>(() => readOnly.ToValueEnumerable().First(value => value == 9));
            Should.Throw<InvalidOperationException>(() => arbitrary.ToValueEnumerable().First(value => value == 9));
            Should.Throw<ArgumentNullException>(() => new[] { 1, 2, 3 }.ToValueEnumerable().First(null));
        }

        [Fact]
        public void Contains_answers_across_every_arm()
        {
            IEnumerable<string> arbitrary = new Queue<string>(new[] { "a", "b", "c" });
            IReadOnlyList<string> readOnly = new List<string> { "a", "b", "c" };

            new[] { "a", "b", "c" }.ToValueEnumerable().Contains("b").ShouldBeTrue();
            new[] { "a", "b", "c" }.ToValueEnumerable().Contains("z").ShouldBeFalse();
            new List<string> { "a", "b", "c" }.ToValueEnumerable().Contains("c").ShouldBeTrue();
            readOnly.ToValueEnumerable().Contains("a").ShouldBeTrue();
            arbitrary.ToValueEnumerable().Contains("c").ShouldBeTrue();
        }

        [Fact]
        public void ElementAt_answers_across_every_arm_and_checks_the_bounds()
        {
            IEnumerable<int> arbitrary = new Queue<int>(new[] { 10, 20, 30 });
            IReadOnlyList<int> readOnly = new List<int> { 10, 20, 30 };

            new[] { 10, 20, 30 }.ToValueEnumerable().ElementAt(1).ShouldBe(20);
            new List<int> { 10, 20, 30 }.ToValueEnumerable().ElementAt(2).ShouldBe(30);
            readOnly.ToValueEnumerable().ElementAt(0).ShouldBe(10);
            arbitrary.ToValueEnumerable().ElementAt(1).ShouldBe(20);

            Should.Throw<ArgumentOutOfRangeException>(() => new[] { 10, 20, 30 }.ToValueEnumerable().ElementAt(-1));
            Should.Throw<ArgumentOutOfRangeException>(() => new[] { 10, 20, 30 }.ToValueEnumerable().ElementAt(3));
            Should.Throw<ArgumentOutOfRangeException>(() => new List<int> { 10, 20, 30 }.ToValueEnumerable().ElementAt(3));
            Should.Throw<ArgumentOutOfRangeException>(() => readOnly.ToValueEnumerable().ElementAt(3));
            Should.Throw<ArgumentOutOfRangeException>(() => arbitrary.ToValueEnumerable().ElementAt(3));
        }

        [Fact]
        public void ElementAt_on_a_lazy_sequence_walks_no_further_than_the_index()
        {
            var probe = new CountingEnumerable(100);
            IEnumerable<int> source = probe;

            source.ToValueEnumerable().ElementAt(4).ShouldBe(4);

            // Five advances for index 4, and the element is read exactly once - on the hit.
            probe.MoveNextCalls.ShouldBe(5);
            probe.CurrentReads.ShouldBe(1);
        }

        [Fact]
        public void A_generic_short_circuit_allocates_nothing()
        {
            var source = new int[64];
            for (var index = 0; index < source.Length; index++)
            {
                source[index] = index;
            }

            Func<int, bool> predicate = value => value == 63;

            // Warm up, so the measured call sees fully JITed code.
            AnyOverArray(source.ToValueEnumerable(), predicate).ShouldBeTrue();

            var before = GC.GetAllocatedBytesForCurrentThread();
            var found = AnyOverArray(source.ToValueEnumerable(), predicate);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            found.ShouldBeTrue();
            allocated.ShouldBe(0);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AnyOverArray(ArrayValueEnumerable<int> source, Func<int, bool> predicate)
            => ValueEnumerableCore.Any<ArrayValueEnumerable<int>, ArrayValueEnumerator<int>, int>(source, predicate);
    }
}
