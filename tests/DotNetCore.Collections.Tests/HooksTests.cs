using System.Collections.Generic;
using System.Collections.ObjectModel;
using DotNetCore.Collections;
using DotNetCore.Collections.Internal;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Tests
{
    /// <summary>
    /// F7-03: the three-hook protocol. Every arm of the four-source dispatch has to answer the hook
    /// it can honestly answer and decline the ones it cannot, because a hook is a promise about the
    /// shape of the sequence - a wrong "yes" would send a consumer down a bulk path that reads the
    /// wrong data.
    /// </summary>
    /// <remarks>
    /// The hooks are reached through the interface here, which boxes the wrapper. That is fine for
    /// an assertion about values; the zero-allocation requirement is about the generic consumers,
    /// and it is pinned by
    /// <see cref="ShortCircuitTests.A_generic_short_circuit_allocates_nothing"/> instead.
    /// </remarks>
    public class HooksTests
    {
        [Fact]
        public void Array_arm_answers_every_hook()
        {
            var source = new[] { 1, 2, 3 };
            IValueEnumerableHooks<int> hooks = source.ToValueEnumerable();

            hooks.TryGetNonEnumeratedCount(out var count).ShouldBeTrue();
            count.ShouldBe(3);

            hooks.TryGetSpan(out var span).ShouldBeTrue();
            span.Length.ShouldBe(3);
            span[0].ShouldBe(1);
            span[1].ShouldBe(2);
            span[2].ShouldBe(3);
        }

        [Fact]
        public void Array_copy_hook_respects_the_offset_and_declines_a_short_destination()
        {
            var source = new[] { 1, 2, 3 };
            IValueEnumerableHooks<int> hooks = source.ToValueEnumerable();

            var destination = new int[5];
            hooks.TryCopyTo(destination, 1).ShouldBeTrue();
            destination.ShouldBe(new[] { 0, 1, 2, 3, 0 });

            var tooSmall = new int[2];
            hooks.TryCopyTo(tooSmall, 0).ShouldBeFalse();
            tooSmall.ShouldBe(new[] { 0, 0 });

            // The offset counts against the capacity: three elements starting at index 1 need four
            // slots, so a three-element buffer is one short and must be declined.
            hooks.TryCopyTo(new int[3], 1).ShouldBeFalse();

            // ...and a buffer that fits exactly is accepted.
            var exact = new int[4];
            hooks.TryCopyTo(exact, 1).ShouldBeTrue();
            exact.ShouldBe(new[] { 0, 1, 2, 3 });

            hooks.TryCopyTo(new int[3], 4).ShouldBeFalse();
        }

        [Fact]
        public void List_arm_reports_the_live_count_and_the_live_span_not_the_capacity()
        {
            var source = new List<int>(64);
            source.Add(7);
            source.Add(8);
            source.Add(9);

            ListLayoutAccessor.GetItems(source).Length.ShouldBeGreaterThan(source.Count);

            IValueEnumerableHooks<int> hooks = source.ToValueEnumerable();

            hooks.TryGetNonEnumeratedCount(out var count).ShouldBeTrue();
            count.ShouldBe(3);

            hooks.TryGetSpan(out var span).ShouldBeTrue();
            span.Length.ShouldBe(3);

            var destination = new int[4];
            hooks.TryCopyTo(destination, 1).ShouldBeTrue();
            destination.ShouldBe(new[] { 0, 7, 8, 9 });
        }

        [Fact]
        public void Read_only_list_backed_by_a_list_still_offers_a_span()
        {
            IReadOnlyList<int> source = new List<int> { 4, 5, 6 };
            IValueEnumerableHooks<int> hooks = source.ToValueEnumerable();

            hooks.TryGetNonEnumeratedCount(out var count).ShouldBeTrue();
            count.ShouldBe(3);

            // The static type says nothing about storage, but the run-time probe finds the list.
            hooks.TryGetSpan(out var span).ShouldBeTrue();
            span.Length.ShouldBe(3);
            span[2].ShouldBe(6);
        }

        [Fact]
        public void Read_only_list_backed_by_an_array_still_offers_a_span()
        {
            IReadOnlyList<int> source = new[] { 4, 5, 6 };
            IValueEnumerableHooks<int> hooks = source.ToValueEnumerable();

            hooks.TryGetSpan(out var span).ShouldBeTrue();
            span.Length.ShouldBe(3);

            var destination = new int[3];
            hooks.TryCopyTo(destination, 0).ShouldBeTrue();
            destination.ShouldBe(new[] { 4, 5, 6 });
        }

        [Fact]
        public void Read_only_list_of_an_opaque_type_reports_a_count_but_declines_the_spans()
        {
            // A ReadOnlyCollection<T> is an IReadOnlyList<T> whose storage is neither an array nor
            // a List<T>, so the run-time probe must fail and the spans must be declined.
            IReadOnlyList<int> source = new ReadOnlyCollection<int>(new List<int> { 1, 2, 3 });
            IValueEnumerableHooks<int> hooks = source.ToValueEnumerable();

            hooks.TryGetNonEnumeratedCount(out var count).ShouldBeTrue();
            count.ShouldBe(3);

            hooks.TryGetSpan(out _).ShouldBeFalse();
            hooks.TryCopyTo(new int[3], 0).ShouldBeFalse();
        }

        [Fact]
        public void Fallback_arm_probes_the_run_time_type_for_both_hooks()
        {
            IEnumerable<int> asArray = new[] { 1, 2, 3 };
            var arrayHooks = (IValueEnumerableHooks<int>)asArray.ToValueEnumerable();
            arrayHooks.TryGetNonEnumeratedCount(out var arrayCount).ShouldBeTrue();
            arrayCount.ShouldBe(3);
            arrayHooks.TryGetSpan(out var arraySpan).ShouldBeTrue();
            arraySpan.Length.ShouldBe(3);

            IEnumerable<int> asList = new List<int> { 1, 2, 3 };
            var listHooks = (IValueEnumerableHooks<int>)asList.ToValueEnumerable();
            listHooks.TryGetNonEnumeratedCount(out var listCount).ShouldBeTrue();
            listCount.ShouldBe(3);
            listHooks.TryGetSpan(out var listSpan).ShouldBeTrue();
            listSpan.Length.ShouldBe(3);

            // A HashSet is an ICollection<T> but not contiguous, so exactly one hook answers.
            IEnumerable<int> asSet = new HashSet<int> { 1, 2, 3 };
            var setHooks = (IValueEnumerableHooks<int>)asSet.ToValueEnumerable();
            setHooks.TryGetNonEnumeratedCount(out var setCount).ShouldBeTrue();
            setCount.ShouldBe(3);
            setHooks.TryGetSpan(out _).ShouldBeFalse();
            setHooks.TryCopyTo(new int[3], 0).ShouldBeFalse();
        }

        [Fact]
        public void Fallback_arm_also_honours_the_read_only_collection_contract()
        {
            // Queue<T> exposes an O(1) count but no mutating interface, so IReadOnlyCollection<T> is
            // the only contract it satisfies here. It still must not promise a span.
            IEnumerable<int> source = new Queue<int>(new[] { 1, 2, 3 });
            var hooks = (IValueEnumerableHooks<int>)source.ToValueEnumerable();

            hooks.TryGetNonEnumeratedCount(out var count).ShouldBeTrue();
            count.ShouldBe(3);
            hooks.TryGetSpan(out _).ShouldBeFalse();
        }

        [Fact]
        public void Fallback_arm_declines_every_contiguous_hook_for_a_lazy_sequence()
        {
            IEnumerable<int> source = EvenNumbers();
            IValueEnumerableHooks<int> hooks = source.ToValueEnumerable();

            hooks.TryGetNonEnumeratedCount(out _).ShouldBeFalse();
            hooks.TryGetSpan(out _).ShouldBeFalse();
            hooks.TryCopyTo(new int[16], 0).ShouldBeFalse();
        }

        /// <summary>An iterator block: a sequence that can only answer by being walked.</summary>
        private static IEnumerable<int> EvenNumbers()
        {
            for (var value = 0; value < 10; value += 2)
            {
                yield return value;
            }
        }

        [Fact]
        public void Operator_results_decline_every_hook_but_keep_the_protocol_total()
        {
            var source = new[] { 1, 2, 3, 4 }.ToValueEnumerable();

            IValueEnumerableHooks<int> filtered = source.Where(value => value > 1);
            filtered.TryGetNonEnumeratedCount(out _).ShouldBeFalse();
            filtered.TryGetSpan(out _).ShouldBeFalse();
            filtered.TryCopyTo(new int[16], 0).ShouldBeFalse();

            IValueEnumerableHooks<int> projected = source.Select(value => value * 2);
            projected.TryGetNonEnumeratedCount(out _).ShouldBeFalse();
            projected.TryGetSpan(out _).ShouldBeFalse();

            IValueEnumerableHooks<int> fused = source.WhereSelect(value => value > 1, value => value * 2);
            fused.TryGetNonEnumeratedCount(out _).ShouldBeFalse();
            fused.TryGetSpan(out _).ShouldBeFalse();
            fused.TryCopyTo(new int[16], 0).ShouldBeFalse();
        }

        [Fact]
        public void Every_arm_agrees_with_the_protocol_it_promises()
        {
            var array = new[] { 1, 2, 3, 4 };
            var list = new List<int> { 1, 2, 3, 4 };
            IReadOnlyList<int> readOnly = list;
            IEnumerable<int> arbitrary = new Queue<int>(new[] { 1, 2, 3, 4 });

            AssertHooksAgreeWithTheWalk((IValueEnumerableHooks<int>)array.ToValueEnumerable(), array);
            AssertHooksAgreeWithTheWalk((IValueEnumerableHooks<int>)list.ToValueEnumerable(), list);
            AssertHooksAgreeWithTheWalk((IValueEnumerableHooks<int>)readOnly.ToValueEnumerable(), list);
            AssertHooksAgreeWithTheWalk((IValueEnumerableHooks<int>)arbitrary.ToValueEnumerable(), list);
        }

        /// <summary>
        /// Verifies the hooks by the only standard that matters: a "yes" has to describe the same
        /// sequence the enumerator walks.
        /// </summary>
        private static void AssertHooksAgreeWithTheWalk(IValueEnumerableHooks<int> hooks, IEnumerable<int> expected)
        {
            var walk = new List<int>();
            foreach (var value in expected)
            {
                walk.Add(value);
            }

            if (hooks.TryGetNonEnumeratedCount(out var count))
            {
                count.ShouldBe(walk.Count);
            }

            if (hooks.TryGetSpan(out var span))
            {
                span.Length.ShouldBe(walk.Count);
                for (var index = 0; index < walk.Count; index++)
                {
                    span[index].ShouldBe(walk[index]);
                }
            }

            var destination = new int[walk.Count + 2];
            if (hooks.TryCopyTo(destination, 1))
            {
                for (var index = 0; index < walk.Count; index++)
                {
                    destination[index + 1].ShouldBe(walk[index]);
                }
            }
        }
    }
}
