using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DotNetCore.Collections;
using DotNetCore.Collections.Internal;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Tests
{
    /// <summary>
    /// F7-02: the value-type enumerator abstraction and the four-source dispatch. Each arm is
    /// checked for the same observable behaviour as the BCL over the same data, and the shape of
    /// the enumerators is pinned structurally (a plain struct, never a by-ref-like struct).
    /// </summary>
    public class ValueEnumerableTests
    {
        [Fact]
        public void Array_source_yields_the_same_sequence_as_the_BCL()
        {
            var source = new[] { 3, 1, 4, 1, 5, 9, 2, 6 };

            Walk.FromArray(source.ToValueEnumerable()).ShouldBe(source);
            Walk.ViaArrayEnumerator(source.ToValueEnumerable().GetEnumerator()).ShouldBe(source);
        }

        [Fact]
        public void List_source_yields_the_same_sequence_as_the_BCL()
        {
            var source = new List<int> { 3, 1, 4, 1, 5, 9, 2, 6 };

            Walk.FromList(source.ToValueEnumerable()).ShouldBe(source);
            Walk.ViaListEnumerator(source.ToValueEnumerable().GetEnumerator()).ShouldBe(source);
        }

        [Fact]
        public void Read_only_list_source_yields_the_same_sequence_as_the_BCL()
        {
            IReadOnlyList<int> source = new List<int> { 3, 1, 4, 1, 5, 9, 2, 6 };

            Walk.FromReadOnlyList(source.ToValueEnumerable()).ShouldBe(source);
            Walk.ViaReadOnlyListEnumerator(source.ToValueEnumerable().GetEnumerator()).ShouldBe(source);
        }

        [Fact]
        public void Enumerable_source_yields_the_same_sequence_as_the_BCL()
        {
            IEnumerable<int> source = Enumerable.Range(1, 8).Where(value => value % 2 == 0);

            Walk.FromEnumerable(source.ToValueEnumerable()).ShouldBe(source);
            Walk.ViaEnumerableEnumerator(source.ToValueEnumerable().GetEnumerator()).ShouldBe(source);
        }

        [Fact]
        public void List_source_walks_only_the_live_elements_when_capacity_is_spare()
        {
            // The backing array keeps spare capacity after a shrink, so the walk has to be
            // bounded by Count rather than by the length of the array reached through the layout.
            var source = new List<int>(64) { 7, 8, 9 };
            ListLayoutAccessor.GetItems(source).Length.ShouldBeGreaterThan(source.Count);

            Walk.FromList(source.ToValueEnumerable()).ShouldBe(new[] { 7, 8, 9 });
        }

        [Fact]
        public void List_source_tracks_shrinkage_from_the_front_and_the_back()
        {
            var source = new List<int> { 1, 2, 3, 4, 5 };
            source.RemoveAt(4);
            source.RemoveAt(0);

            Walk.FromList(source.ToValueEnumerable()).ShouldBe(new[] { 2, 3, 4 });
            Walk.FromList(source.ToValueEnumerable()).ShouldBe(source);
        }

        [Fact]
        public void A_list_that_grew_over_several_capacity_steps_still_matches_the_BCL()
        {
            var source = new List<int>();
            for (var value = 0; value < 1000; value++)
            {
                source.Add(value);
            }

            Walk.FromList(source.ToValueEnumerable()).ShouldBe(source);
        }

        [Fact]
        public void Empty_sources_enumerate_to_nothing()
        {
            IReadOnlyList<int> emptyReadOnly = new List<int>();

            Walk.FromArray(new int[0].ToValueEnumerable()).ShouldBeEmpty();
            Walk.FromList(new List<int>().ToValueEnumerable()).ShouldBeEmpty();
            Walk.FromReadOnlyList(emptyReadOnly.ToValueEnumerable()).ShouldBeEmpty();
            Walk.FromEnumerable(Enumerable.Empty<int>().ToValueEnumerable()).ShouldBeEmpty();
        }

        [Fact]
        public void Reference_type_sources_forward_null_elements()
        {
            var list = new List<string> { "a", null, "c" };

            Walk.FromList(list.ToValueEnumerable()).ShouldBe(new List<string> { "a", null, "c" });
            Walk.FromArray(new[] { "x", null }.ToValueEnumerable()).ShouldBe(new[] { "x", null });
        }

        [Fact]
        public void Array_enumerator_reports_its_position_and_reset_restarts_it()
        {
            var enumerator = new[] { 10, 20, 30 }.ToValueEnumerable().GetEnumerator();

            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(10);
            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(20);

            // Reset rewinds to before the first element, so the next MoveNext lands on the head
            // again instead of continuing past it.
            enumerator.Reset();
            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(10);

            // A drain resumes after the element the last MoveNext landed on, so it sees the tail
            // only. Resetting and then draining in one step is a different arrangement, and the
            // full walk from a fresh enumerator is covered by the four source-shape facts above.
            Walk.ViaArrayEnumerator(enumerator).ShouldBe(new[] { 20, 30 });
        }

        [Fact]
        public void Array_enumerator_current_throws_outside_the_sequence()
        {
            var enumerator = new[] { 1, 2 }.ToValueEnumerable().GetEnumerator();

            Should.Throw<InvalidOperationException>(() => { var unused = enumerator.Current; });

            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(1);
            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(2);
            enumerator.MoveNext().ShouldBeFalse();

            Should.Throw<InvalidOperationException>(() => { var unused = enumerator.Current; });
        }

        [Fact]
        public void List_enumerator_current_throws_outside_the_sequence()
        {
            var enumerator = new List<int>(16) { 1, 2 }.ToValueEnumerable().GetEnumerator();

            Should.Throw<InvalidOperationException>(() => { var unused = enumerator.Current; });

            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(1);
            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(2);
            enumerator.MoveNext().ShouldBeFalse();

            Should.Throw<InvalidOperationException>(() => { var unused = enumerator.Current; });
        }

        [Fact]
        public void Read_only_list_enumerator_current_throws_outside_the_sequence()
        {
            IReadOnlyList<int> source = new List<int> { 1, 2 };
            var enumerator = source.ToValueEnumerable().GetEnumerator();

            Should.Throw<InvalidOperationException>(() => { var unused = enumerator.Current; });

            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(1);
            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(2);
            enumerator.MoveNext().ShouldBeFalse();

            Should.Throw<InvalidOperationException>(() => { var unused = enumerator.Current; });
        }

        [Fact]
        public void Wrapping_is_lazy_and_the_fallback_arm_requests_one_enumerator()
        {
            var probe = new ProbeEnumerable();
            var wrapped = ((IEnumerable<int>)probe).ToValueEnumerable();

            probe.EnumeratorRequests.ShouldBe(0);

            Walk.FromEnumerable(wrapped).ShouldBe(new[] { 1, 2, 3 });
            probe.EnumeratorRequests.ShouldBe(1);
        }

        [Fact]
        public void Fallback_arm_forwards_dispose_to_the_source_enumerator()
        {
            var probe = new ProbeEnumerable();
            var enumerator = ((IEnumerable<int>)probe).ToValueEnumerable().GetEnumerator();

            enumerator.Dispose();

            probe.DisposeCount.ShouldBe(1);
        }

        [Fact]
        public void Most_specific_overload_wins_for_each_source_shape()
        {
            var array = new[] { 1, 2, 3 };
            var list = new List<int> { 1, 2, 3 };
            IReadOnlyList<int> readOnly = new List<int> { 1, 2, 3 };
            IEnumerable<int> arbitrary = new HashSet<int> { 1, 2, 3 };

            // The static type of the source selects the arm; assigning to the concrete wrapper
            // type makes the selection a compile-time assertion rather than a runtime check.
            ArrayValueEnumerable<int> fromArray = array.ToValueEnumerable();
            ListValueEnumerable<int> fromList = list.ToValueEnumerable();
            ReadOnlyListValueEnumerable<int> fromReadOnly = readOnly.ToValueEnumerable();
            EnumerableValueEnumerable<int> fromArbitrary = arbitrary.ToValueEnumerable();

            fromArray.GetEnumerator().ShouldBeOfType<ArrayValueEnumerator<int>>();
            fromList.GetEnumerator().ShouldBeOfType<ListValueEnumerator<int>>();
            fromReadOnly.GetEnumerator().ShouldBeOfType<ReadOnlyListValueEnumerator<int>>();
            fromArbitrary.GetEnumerator().ShouldBeOfType<EnumerableValueEnumerator<int>>();

            // An array seen through IReadOnlyList<T> must take the read-only-list arm, not the
            // array arm: the dispatch follows the static type, not the runtime type.
            IReadOnlyList<int> arrayAsInterface = array;
            ReadOnlyListValueEnumerable<int> throughInterface = arrayAsInterface.ToValueEnumerable();
            Walk.FromReadOnlyList(throughInterface).ShouldBe(array);
        }

        [Fact]
        public void Factories_mirror_the_extension_entry_points()
        {
            var array = new[] { 1, 2, 3 };
            var list = new List<int> { 1, 2, 3 };
            IReadOnlyList<int> readOnly = list;
            IEnumerable<int> arbitrary = Enumerable.Range(1, 3);

            Walk.FromArray(ValueEnumerable.From(array)).ShouldBe(array);
            Walk.FromList(ValueEnumerable.From(list)).ShouldBe(list);
            Walk.FromReadOnlyList(ValueEnumerable.From(readOnly)).ShouldBe(readOnly);
            Walk.FromEnumerable(ValueEnumerable.From(arbitrary)).ShouldBe(new[] { 1, 2, 3 });
        }

        [Fact]
        public void Wrappers_can_be_consumed_through_the_BCL_interfaces()
        {
            var source = new[] { 1, 2, 3 };

            IEnumerable<int> asEnumerable = source.ToValueEnumerable();
            asEnumerable.Count().ShouldBe(3);
            asEnumerable.ShouldBe(source);

            IValueEnumerable<int, ArrayValueEnumerator<int>> asValueEnumerable = source.ToValueEnumerable();
            Walk.ViaArrayEnumerator(asValueEnumerable.GetEnumerator()).ShouldBe(source);
        }

        [Fact]
        public void The_eight_source_types_are_plain_structs_never_by_ref_like()
        {
            // The design constraint is explicit: a by-ref-like enumerator cannot be a type
            // argument and cannot be captured by an async state machine, which rules it out for
            // the .NET Framework 4.5.1 floor.
            typeof(ArrayValueEnumerable<int>).IsValueType.ShouldBeTrue();
            typeof(ListValueEnumerable<int>).IsValueType.ShouldBeTrue();
            typeof(ReadOnlyListValueEnumerable<int>).IsValueType.ShouldBeTrue();
            typeof(EnumerableValueEnumerable<int>).IsValueType.ShouldBeTrue();

            typeof(ArrayValueEnumerator<int>).IsValueType.ShouldBeTrue();
            typeof(ListValueEnumerator<int>).IsValueType.ShouldBeTrue();
            typeof(ReadOnlyListValueEnumerator<int>).IsValueType.ShouldBeTrue();
            typeof(EnumerableValueEnumerator<int>).IsValueType.ShouldBeTrue();

            typeof(ArrayValueEnumerator<int>).IsByRefLike.ShouldBeFalse();
            typeof(ListValueEnumerator<int>).IsByRefLike.ShouldBeFalse();
            typeof(ReadOnlyListValueEnumerator<int>).IsByRefLike.ShouldBeFalse();
            typeof(EnumerableValueEnumerator<int>).IsByRefLike.ShouldBeFalse();
        }

        [Fact]
        public void Walking_through_a_value_type_enumerator_allocates_nothing()
        {
            var source = new[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            // Warm up first, so the measurement below sees fully JITed code.
            Sum(source.ToValueEnumerable()).ShouldBe(36);

            var before = GC.GetAllocatedBytesForCurrentThread();
            var total = Sum(source.ToValueEnumerable());
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            total.ShouldBe(36);
            allocated.ShouldBe(0);
        }

        [Fact]
        public void Null_sources_are_rejected()
        {
            Should.Throw<ArgumentNullException>(() => ValueEnumerable.From((int[])null));
            Should.Throw<ArgumentNullException>(() => ValueEnumerable.From((List<int>)null));
            Should.Throw<ArgumentNullException>(() => ValueEnumerable.From((IReadOnlyList<int>)null));
            Should.Throw<ArgumentNullException>(() => ValueEnumerable.From((IEnumerable<int>)null));

            Should.Throw<ArgumentNullException>(() => new ArrayValueEnumerable<int>(null));
            Should.Throw<ArgumentNullException>(() => new ListValueEnumerable<int>(null));
            Should.Throw<ArgumentNullException>(() => new ReadOnlyListValueEnumerable<int>(null));
            Should.Throw<ArgumentNullException>(() => new EnumerableValueEnumerable<int>(null));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Sum(ArrayValueEnumerable<int> source)
        {
            var total = 0;
            foreach (var value in source)
            {
                total += value;
            }

            return total;
        }
    }
}
