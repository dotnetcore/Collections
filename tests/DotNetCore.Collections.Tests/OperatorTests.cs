using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using DotNetCore.Collections;
using DotNetCore.Collections.Internal;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Tests
{
    /// <summary>
    /// F7-03: the filter / projection operators and the fused filter-and-project pass. The fused
    /// pass is the shape the engine exists for, so it is pinned two ways: against the BCL for
    /// behaviour, and against a counting source for the single-pass property (each source element is
    /// read exactly once, no matter how many operators the pipeline carries).
    /// </summary>
    public class OperatorTests
    {
        [Fact]
        public void Where_filters_every_arm_like_the_BCL()
        {
            var expected = new[] { 3, 4, 5 };

            IEnumerable<int> fromArray = new[] { 1, 2, 3, 4, 5 }.ToValueEnumerable().Where(value => value > 2);
            IEnumerable<int> fromList = new List<int> { 1, 2, 3, 4, 5 }.ToValueEnumerable().Where(value => value > 2);

            IReadOnlyList<int> readOnlySource = new List<int> { 1, 2, 3, 4, 5 };
            IEnumerable<int> fromReadOnly = readOnlySource.ToValueEnumerable().Where(value => value > 2);

            IEnumerable<int> enumerationSource = new Queue<int>(new[] { 1, 2, 3, 4, 5 });
            IEnumerable<int> fromEnumerable = enumerationSource.ToValueEnumerable().Where(value => value > 2);

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);
        }

        [Fact]
        public void Select_projects_every_arm_like_the_BCL()
        {
            var expected = new[] { 10, 20, 30 };

            IEnumerable<int> fromArray = new[] { 1, 2, 3 }.ToValueEnumerable().Select(value => value * 10);
            IEnumerable<int> fromList = new List<int> { 1, 2, 3 }.ToValueEnumerable().Select(value => value * 10);

            IReadOnlyList<int> readOnlySource = new List<int> { 1, 2, 3 };
            IEnumerable<int> fromReadOnly = readOnlySource.ToValueEnumerable().Select(value => value * 10);

            IEnumerable<int> enumerationSource = new Queue<int>(new[] { 1, 2, 3 });
            IEnumerable<int> fromEnumerable = enumerationSource.ToValueEnumerable().Select(value => value * 10);

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);
        }

        [Fact]
        public void WhereSelect_matches_the_BCL_and_can_change_the_element_type()
        {
            var source = new[] { 1, 2, 3, 4, 5, 6 };

            IEnumerable<string> fused = source.ToValueEnumerable()
                .WhereSelect(value => value % 3 == 0, value => "n" + value);

            fused.ShouldBe(new[] { "n3", "n6" });
        }

        [Fact]
        public void WhereSelect_matches_every_arm_like_the_BCL()
        {
            var expected = new[] { 20, 40 };

            IEnumerable<int> fromArray = new[] { 1, 2, 3, 4, 5 }.ToValueEnumerable().WhereSelect(value => value % 2 == 0, value => value * 10);
            IEnumerable<int> fromList = new List<int> { 1, 2, 3, 4, 5 }.ToValueEnumerable().WhereSelect(value => value % 2 == 0, value => value * 10);

            IReadOnlyList<int> readOnlySource = new List<int> { 1, 2, 3, 4, 5 };
            IEnumerable<int> fromReadOnly = readOnlySource.ToValueEnumerable().WhereSelect(value => value % 2 == 0, value => value * 10);

            IEnumerable<int> enumerationSource = new Queue<int>(new[] { 1, 2, 3, 4, 5 });
            IEnumerable<int> fromEnumerable = enumerationSource.ToValueEnumerable().WhereSelect(value => value % 2 == 0, value => value * 10);

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);
        }

        [Fact]
        public void The_fused_pass_reads_each_source_element_exactly_once()
        {
            var probe = new CountingEnumerable(10);
            IEnumerable<int> source = probe;

            var fused = source.ToValueEnumerable().WhereSelect(value => (value & 1) == 0, value => value * 10);

            var seen = new List<int>();
            foreach (var value in fused)
            {
                seen.Add(value);
            }

            seen.ShouldBe(new[] { 0, 20, 40, 60, 80 });

            // One enumerator request and ten element reads: the filter and the projection happened
            // inside a single walk. A Where(...).Select(...) chain would have read the ten elements
            // in the filter pass and then the five survivors again in the projection pass.
            probe.EnumeratorRequests.ShouldBe(1);
            probe.CurrentReads.ShouldBe(10);
        }

        [Fact]
        public void The_fused_pass_is_lazy_and_stops_reading_as_soon_as_it_stops_being_read()
        {
            var probe = new CountingEnumerable(10);
            IEnumerable<int> source = probe;

            var fused = source.ToValueEnumerable().WhereSelect(value => true, value => value * 2);

            // Building the pipeline touches nothing.
            probe.EnumeratorRequests.ShouldBe(0);
            probe.CurrentReads.ShouldBe(0);

            var enumerator = fused.GetEnumerator();
            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(0);
            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(2);

            // Two results, two source elements: nothing was read ahead of the consumer.
            probe.CurrentReads.ShouldBe(2);
        }

        [Fact]
        public void The_fused_enumerator_restarts_a_partially_consumed_walk_on_reset()
        {
            var source = new[] { 1, 2, 3, 4, 5 }.ToValueEnumerable()
                .WhereSelect(value => value % 2 == 1, value => value * 100);

            var enumerator = source.GetEnumerator();

            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(100);
            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(300);

            enumerator.Reset();
            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(100);
        }

        [Fact]
        public void The_fused_pass_allocates_nothing_on_the_hot_path()
        {
            var source = new int[64];
            for (var index = 0; index < source.Length; index++)
            {
                source[index] = index;
            }

            Func<int, bool> predicate = value => (value & 1) == 0;
            Func<int, int> selector = value => value * 3;

            // Warm up, so the measured pass sees fully JITed code.
            SumFused(source, predicate, selector).ShouldBe(SumOfEvenTimesThree(64));

            var before = GC.GetAllocatedBytesForCurrentThread();
            var total = SumFused(source, predicate, selector);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            total.ShouldBe(SumOfEvenTimesThree(64));
            allocated.ShouldBe(0);
        }

        [Fact]
        public void The_operators_keep_the_predicate_and_the_selector_in_struct_fields()
        {
            // Closure elimination, stated as a structural fact: the operator holds the delegate in
            // its own field instead of allocating a closure to carry it. Only Where is pinned here;
            // the other two are built the same way.
            var fields = typeof(WhereValueEnumerable<ArrayValueEnumerable<int>, ArrayValueEnumerator<int>, int>)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

            var carriesPredicate = false;
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(Func<int, bool>))
                {
                    carriesPredicate = true;
                }
            }

            carriesPredicate.ShouldBeTrue();

            typeof(WhereValueEnumerator<ArrayValueEnumerator<int>, int>).IsValueType.ShouldBeTrue();
            typeof(WhereValueEnumerator<ArrayValueEnumerator<int>, int>).IsByRefLike.ShouldBeFalse();
            typeof(SelectValueEnumerator<ArrayValueEnumerator<int>, int, string>).IsValueType.ShouldBeTrue();
            typeof(SelectValueEnumerator<ArrayValueEnumerator<int>, int, string>).IsByRefLike.ShouldBeFalse();
            typeof(WhereSelectValueEnumerator<ArrayValueEnumerator<int>, int, string>).IsValueType.ShouldBeTrue();
            typeof(WhereSelectValueEnumerator<ArrayValueEnumerator<int>, int, string>).IsByRefLike.ShouldBeFalse();
        }

        [Fact]
        public void Null_delegates_are_rejected()
        {
            var source = new[] { 1, 2, 3 }.ToValueEnumerable();

            Should.Throw<ArgumentNullException>(() => source.Where(null));
            Should.Throw<ArgumentNullException>(() => source.Select<int, int>(null));
            Should.Throw<ArgumentNullException>(() => source.WhereSelect<int, int>(null, value => value));
            Should.Throw<ArgumentNullException>(() => source.WhereSelect<int, int>(value => true, null));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int SumFused(int[] source, Func<int, bool> predicate, Func<int, int> selector)
        {
            var total = 0;
            foreach (var value in source.ToValueEnumerable().WhereSelect(predicate, selector))
            {
                total += value;
            }

            return total;
        }

        private static int SumOfEvenTimesThree(int count)
        {
            var total = 0;
            for (var index = 0; index < count; index++)
            {
                if ((index & 1) == 0)
                {
                    total += index * 3;
                }
            }

            return total;
        }
    }

    /// <summary>
    /// A sequence that records how it was consumed, so the single-pass property of the fused
    /// operator can be measured rather than inferred.
    /// </summary>
    internal sealed class CountingEnumerable : IEnumerable<int>
    {
        private readonly int _count;

        internal CountingEnumerable(int count)
        {
            _count = count;
        }

        internal int EnumeratorRequests { get; private set; }

        internal int CurrentReads { get; private set; }

        internal int MoveNextCalls { get; private set; }

        public IEnumerator<int> GetEnumerator()
        {
            EnumeratorRequests++;
            return new Cursor(this);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class Cursor : IEnumerator<int>
        {
            private readonly CountingEnumerable _owner;
            private int _index = -1;

            internal Cursor(CountingEnumerable owner) => _owner = owner;

            public int Current
            {
                get
                {
                    _owner.CurrentReads++;
                    return _index;
                }
            }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                _owner.MoveNextCalls++;
                return ++_index < _owner._count;
            }

            public void Reset() => _index = -1;

            public void Dispose()
            {
            }
        }
    }
}
