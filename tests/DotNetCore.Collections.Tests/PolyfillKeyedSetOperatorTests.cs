using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections;
using DotNetCore.Collections.Internal;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Tests
{
    /// <summary>
    /// F7-06 (first batch): the keyed-set operators of the P0 polyfill layer -
    /// <c>DistinctBy</c>, <c>ExceptBy</c>, <c>IntersectBy</c> and <c>UnionBy</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These four are BCL operators from .NET 6 onwards, and this suite runs on net8.0, where the
    /// BCL's own implementation is available. That makes the oracle exact: every engine answer is
    /// compared with the <c>System.Linq</c> expression it claims to be equivalent to, rather than
    /// with a literal somebody typed. The comparison is what the layer exists for - a low-TFM
    /// consumer must not be able to tell the polyfill from the platform.
    /// </para>
    /// <para>
    /// Every operator is exercised on all four source arms. The arbitrary-sequence arm is the one
    /// the layer is allowed to hand straight to the BCL on this target, so on net8.0 that arm is
    /// the platform's answer by construction; the other three are the engine's own walk.
    /// </para>
    /// </remarks>
    public class PolyfillKeyedSetOperatorTests
    {
        private static readonly int[] Numbers = { 1, 2, 3, 4, 5, 6 };

        private static int KeyOf(int value) => value % 3;

        // ----- DistinctBy -----

        [Fact]
        public void DistinctBy_keeps_the_first_element_of_every_key_on_every_arm()
        {
            var expected = Numbers.DistinctBy(KeyOf).ToList();

            IEnumerable<int> fromArray = Numbers.ToValueEnumerable().DistinctBy(KeyOf);
            IEnumerable<int> fromList = new List<int>(Numbers).ToValueEnumerable().DistinctBy(KeyOf);

            IReadOnlyList<int> readOnlySource = new List<int>(Numbers);
            IEnumerable<int> fromReadOnly = readOnlySource.ToValueEnumerable().DistinctBy(KeyOf);

            IEnumerable<int> enumerationSource = new Queue<int>(Numbers);
            IEnumerable<int> fromEnumerable = enumerationSource.ToValueEnumerable().DistinctBy(KeyOf);

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);

            expected.ShouldBe(new[] { 1, 2, 3 });
        }

        [Fact]
        public void DistinctBy_agrees_with_the_BCL_on_an_object_key()
        {
            var source = new[] { "a", "bb", "cc", "d", "eee" };

            var expected = source.DistinctBy(text => text.Length).ToList();

            var actual = source.ToValueEnumerable().DistinctBy(text => text.Length).ToList();

            actual.ShouldBe(expected);
            actual.ShouldBe(new[] { "a", "bb", "eee" });
        }

        [Fact]
        public void DistinctBy_honours_a_custom_comparer()
        {
            var source = new[] { "a", "A", "b", "B" };

            var expected = source.DistinctBy(text => text, StringComparer.OrdinalIgnoreCase).ToList();
            var actual = source.ToValueEnumerable()
                .DistinctBy(text => text, StringComparer.OrdinalIgnoreCase)
                .ToList();

            actual.ShouldBe(expected);
            actual.ShouldBe(new[] { "a", "b" });
        }

        [Fact]
        public void DistinctBy_yields_nothing_for_an_empty_source()
        {
            new int[0].ToValueEnumerable().DistinctBy(KeyOf).ShouldBeEmpty();
        }

        [Fact]
        public void DistinctBy_is_lazy_and_touches_the_source_only_when_it_is_walked()
        {
            var probe = new CountingEnumerable(6);
            IEnumerable<int> source = probe;

            var deduplicated = source.ToValueEnumerable().DistinctBy(KeyOf);

            probe.EnumeratorRequests.ShouldBe(0);
            probe.MoveNextCalls.ShouldBe(0);

            deduplicated.Count().ShouldBe(3);
            probe.EnumeratorRequests.ShouldBe(1);
        }

        [Fact]
        public void DistinctBy_survives_a_reset()
        {
            var enumerator = Numbers.ToValueEnumerable().DistinctBy(KeyOf).GetEnumerator();

            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(1);
            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(2);

            enumerator.Reset();

            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(1);
        }

        // ----- ExceptBy -----

        [Fact]
        public void ExceptBy_drops_the_excluded_keys_on_every_arm()
        {
            var source = new[] { 1, 1, 2, 2, 3, 3, 4 };
            var keys = new[] { 2, 4 };

            var expected = source.ExceptBy(keys, value => value).ToList();

            IEnumerable<int> fromArray = source.ToValueEnumerable().ExceptBy(keys, value => value);
            IEnumerable<int> fromList = new List<int>(source).ToValueEnumerable().ExceptBy(keys, value => value);

            IReadOnlyList<int> readOnlySource = new List<int>(source);
            IEnumerable<int> fromReadOnly = readOnlySource.ToValueEnumerable().ExceptBy(keys, value => value);

            IEnumerable<int> enumerationSource = new Queue<int>(source);
            IEnumerable<int> fromEnumerable = enumerationSource.ToValueEnumerable().ExceptBy(keys, value => value);

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);

            expected.ShouldBe(new[] { 1, 3 });
        }

        [Fact]
        public void ExceptBy_yields_every_element_when_the_key_sequence_is_empty()
        {
            var expected = new[] { 1, 1, 2 }.ExceptBy(new int[0], value => value).ToList();
            var actual = new[] { 1, 1, 2 }.ToValueEnumerable().ExceptBy(new int[0], value => value).ToList();

            actual.ShouldBe(expected);
            actual.ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public void ExceptBy_reads_the_key_sequence_once_and_lazily()
        {
            var probe = new CountingEnumerable(6);
            IEnumerable<int> keys = probe;

            var filtered = Numbers.ToValueEnumerable().ExceptBy(keys, value => value);

            // Nothing is read until the result is walked.
            probe.EnumeratorRequests.ShouldBe(0);

            // The probe holds the keys 0..5, so only 6 survives the exclusion.
            filtered.Count().ShouldBe(1);
            probe.EnumeratorRequests.ShouldBe(1);
        }

        // ----- IntersectBy -----

        [Fact]
        public void IntersectBy_keeps_the_matching_keys_once_on_every_arm()
        {
            var source = new[] { 1, 1, 2, 3, 3, 4 };
            var keys = new[] { 3, 4, 5 };

            var expected = source.IntersectBy(keys, value => value).ToList();

            IEnumerable<int> fromArray = source.ToValueEnumerable().IntersectBy(keys, value => value);
            IEnumerable<int> fromList = new List<int>(source).ToValueEnumerable().IntersectBy(keys, value => value);

            IReadOnlyList<int> readOnlySource = new List<int>(source);
            IEnumerable<int> fromReadOnly = readOnlySource.ToValueEnumerable().IntersectBy(keys, value => value);

            IEnumerable<int> enumerationSource = new Queue<int>(source);
            IEnumerable<int> fromEnumerable = enumerationSource.ToValueEnumerable().IntersectBy(keys, value => value);

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);

            expected.ShouldBe(new[] { 3, 4 });
        }

        [Fact]
        public void IntersectBy_yields_nothing_when_the_key_sequence_is_empty()
        {
            new[] { 1, 2, 3 }.ToValueEnumerable().IntersectBy(new int[0], value => value).ShouldBeEmpty();
        }

        // ----- UnionBy -----

        [Fact]
        public void UnionBy_concatenates_then_deduplicates_on_every_arm()
        {
            var first = new[] { 1, 2, 3 };
            var second = new[] { 3, 4, 5 };

            var expected = first.UnionBy(second, value => value).ToList();

            IEnumerable<int> fromArray = first.ToValueEnumerable().UnionBy(second, value => value);
            IEnumerable<int> fromList = new List<int>(first).ToValueEnumerable().UnionBy(second, value => value);

            IReadOnlyList<int> readOnlySource = new List<int>(first);
            IEnumerable<int> fromReadOnly = readOnlySource.ToValueEnumerable().UnionBy(second, value => value);

            IEnumerable<int> enumerationSource = new Queue<int>(first);
            IEnumerable<int> fromEnumerable = enumerationSource.ToValueEnumerable().UnionBy(second, value => value);

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);

            expected.ShouldBe(new[] { 1, 2, 3, 4, 5 });
        }

        [Fact]
        public void UnionBy_deduplicates_across_the_two_sides_by_key()
        {
            var first = new[] { "a", "bb" };
            var second = new[] { "c", "dd" };

            var expected = first.UnionBy(second, text => text.Length).ToList();
            var actual = first.ToValueEnumerable().UnionBy(second, text => text.Length).ToList();

            actual.ShouldBe(expected);
            actual.ShouldBe(new[] { "a", "bb" });
        }

        [Fact]
        public void UnionBy_leaves_the_second_sequence_untouched_when_the_first_never_runs_out()
        {
            var probe = new CountingEnumerable(3);
            IEnumerable<int> second = probe;

            var unioned = new[] { 1, 2, 3 }.ToValueEnumerable().UnionBy(second, value => value);

            // Taking the first three results drains the source and stops there.
            unioned.Take(3).ToList().ShouldBe(new[] { 1, 2, 3 });

            probe.EnumeratorRequests.ShouldBe(0);
        }

        // ----- cross-cutting contracts -----

        [Fact]
        public void Null_arguments_are_rejected_by_every_operator()
        {
            var source = Numbers.ToValueEnumerable();

            Should.Throw<ArgumentNullException>(() => source.DistinctBy<int, int>(null));
            Should.Throw<ArgumentNullException>(() => source.ExceptBy<int, int>(new int[0], null));
            Should.Throw<ArgumentNullException>(() => source.ExceptBy<int, int>(null, value => value));
            Should.Throw<ArgumentNullException>(() => source.IntersectBy<int, int>(new int[0], null));
            Should.Throw<ArgumentNullException>(() => source.IntersectBy<int, int>(null, value => value));
            Should.Throw<ArgumentNullException>(() => source.UnionBy<int, int>(new int[0], null));
            Should.Throw<ArgumentNullException>(() => source.UnionBy<int, int>(null, value => value));
        }

        [Fact]
        public void Null_arguments_are_rejected_on_the_arbitrary_sequence_arm_too()
        {
            IEnumerable<int> raw = new Queue<int>(Numbers);
            var source = raw.ToValueEnumerable();

            Should.Throw<ArgumentNullException>(() => source.DistinctBy<int, int>(null));
            Should.Throw<ArgumentNullException>(() => source.ExceptBy<int, int>(new int[0], null));
            Should.Throw<ArgumentNullException>(() => source.ExceptBy<int, int>(null, value => value));
            Should.Throw<ArgumentNullException>(() => source.IntersectBy<int, int>(new int[0], null));
            Should.Throw<ArgumentNullException>(() => source.IntersectBy<int, int>(null, value => value));
            Should.Throw<ArgumentNullException>(() => source.UnionBy<int, int>(new int[0], null));
            Should.Throw<ArgumentNullException>(() => source.UnionBy<int, int>(null, value => value));
        }

        [Fact]
        public void The_layer_declines_the_three_hooks()
        {
            var source = Numbers.ToValueEnumerable();

            IValueEnumerableHooks<int> distinct = source.DistinctBy(KeyOf);
            IValueEnumerableHooks<int> except = source.ExceptBy(new[] { 2 }, value => value);
            IValueEnumerableHooks<int> intersect = source.IntersectBy(new[] { 2 }, value => value);
            IValueEnumerableHooks<int> union = source.UnionBy(new[] { 7 }, value => value);

            distinct.TryGetNonEnumeratedCount(out _).ShouldBeFalse();
            except.TryGetNonEnumeratedCount(out _).ShouldBeFalse();
            intersect.TryGetNonEnumeratedCount(out _).ShouldBeFalse();
            union.TryGetNonEnumeratedCount(out _).ShouldBeFalse();

#if NETCOREAPP3_0_OR_GREATER
            distinct.TryGetSpan(out _).ShouldBeFalse();
            except.TryGetSpan(out _).ShouldBeFalse();
            intersect.TryGetSpan(out _).ShouldBeFalse();
            union.TryGetSpan(out _).ShouldBeFalse();
#endif
        }

        [Fact]
        public void The_layer_enumerators_are_plain_value_types()
        {
            typeof(DistinctByValueEnumerator<ArrayValueEnumerator<int>, int, int>).IsValueType.ShouldBeTrue();
            typeof(DistinctByValueEnumerator<ArrayValueEnumerator<int>, int, int>).IsByRefLike.ShouldBeFalse();

            typeof(ExceptByValueEnumerator<ArrayValueEnumerator<int>, int, int>).IsValueType.ShouldBeTrue();
            typeof(ExceptByValueEnumerator<ArrayValueEnumerator<int>, int, int>).IsByRefLike.ShouldBeFalse();

            typeof(IntersectByValueEnumerator<ArrayValueEnumerator<int>, int, int>).IsValueType.ShouldBeTrue();
            typeof(IntersectByValueEnumerator<ArrayValueEnumerator<int>, int, int>).IsByRefLike.ShouldBeFalse();

            typeof(UnionByValueEnumerator<ArrayValueEnumerator<int>, int, int>).IsValueType.ShouldBeTrue();
            typeof(UnionByValueEnumerator<ArrayValueEnumerator<int>, int, int>).IsByRefLike.ShouldBeFalse();
        }

        [Fact]
        public void The_layer_walks_a_contiguous_source_once_and_needs_no_second_pass()
        {
            // These four operators keep auxiliary state (a set of keys), so they are not
            // allocation-free the way a pure streaming operator is; what they must not do is walk
            // the source twice. The probe counts the enumerator requests and the pulls.
            var probe = new CountingEnumerable(64);
            IEnumerable<int> source = probe;

            var distinct = source.ToValueEnumerable().DistinctBy(value => value % 8).ToList();
            var requestsAfterDistinct = probe.EnumeratorRequests;

            var except = source.ToValueEnumerable().ExceptBy(new[] { 3, 5 }, value => value % 8).ToList();

            distinct.Count.ShouldBe(8);
            except.Count.ShouldBe(6);

            // One enumerator per walk, never a rewind.
            requestsAfterDistinct.ShouldBe(1);
            probe.EnumeratorRequests.ShouldBe(2);
            probe.MoveNextCalls.ShouldBe(130);
        }
    }
}
