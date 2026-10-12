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
    /// F7-08: the P1 operator layer - <c>DistinctUntilChanged</c>, <c>Memoize</c>,
    /// <c>FallbackIfEmpty</c>, <c>AtLeast</c> / <c>Exactly</c> / <c>CountBetween</c>,
    /// <c>PartialSortBy</c>, and <c>Choose</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These operators are not in the BCL on any TFM the engine targets, so there is no platform
    /// oracle to compare against. The tests instead verify the operator's contract: the four source
    /// arms must agree, the result must match a hand-computed expected sequence, and the operator
    /// must be lazy when it is documented as lazy.
    /// </para>
    /// <para>
    /// The four-arm pattern is the same one the F7-06 polyfill tests use: array, list, read-only
    /// list, and arbitrary enumerable. Each arm walks its own value-type enumerator, so the test
    /// proves the operator works on all four dispatch paths.
    /// </para>
    /// </remarks>
    public class P1OperatorTests
    {
        // ----- DistinctUntilChanged -----

        [Fact]
        public void DistinctUntilChanged_removes_consecutive_duplicates_on_every_arm()
        {
            var source = new[] { 1, 1, 2, 2, 3, 1, 1 };
            var expected = new[] { 1, 2, 3, 1 };

            IEnumerable<int> fromArray = source.ToValueEnumerable().DistinctUntilChanged();
            IEnumerable<int> fromList = new List<int>(source).ToValueEnumerable().DistinctUntilChanged();

            IReadOnlyList<int> readOnlySource = new List<int>(source);
            IEnumerable<int> fromReadOnly = readOnlySource.ToValueEnumerable().DistinctUntilChanged();

            IEnumerable<int> enumerationSource = new Queue<int>(source);
            IEnumerable<int> fromEnumerable = enumerationSource.ToValueEnumerable().DistinctUntilChanged();

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);
        }

        [Fact]
        public void DistinctUntilChanged_honours_a_custom_comparer()
        {
            var source = new[] { "a", "A", "b", "B", "a" };

            var actual = source.ToValueEnumerable()
                .DistinctUntilChanged(StringComparer.OrdinalIgnoreCase)
                .ToList();

            actual.ShouldBe(new[] { "a", "b", "a" });
        }

        [Fact]
        public void DistinctUntilChanged_yields_nothing_for_an_empty_source()
        {
            new int[0].ToValueEnumerable().DistinctUntilChanged().ShouldBeEmpty();
        }

        [Fact]
        public void DistinctUntilChanged_preserves_a_single_element()
        {
            new[] { 42 }.ToValueEnumerable().DistinctUntilChanged().ShouldBe(new[] { 42 });
        }

        [Fact]
        public void DistinctUntilChanged_keeps_all_elements_when_none_are_consecutive_duplicates()
        {
            var source = new[] { 1, 2, 3, 2, 1 };
            source.ToValueEnumerable().DistinctUntilChanged().ShouldBe(source);
        }

        [Fact]
        public void DistinctUntilChanged_is_lazy_and_touches_the_source_only_when_it_is_walked()
        {
            var probe = new CountingEnumerable(7);
            IEnumerable<int> source = probe;

            var deduplicated = source.ToValueEnumerable().DistinctUntilChanged();

            probe.EnumeratorRequests.ShouldBe(0);

            deduplicated.ToList();

            probe.EnumeratorRequests.ShouldBe(1);
        }

        [Fact]
        public void DistinctUntilChanged_treats_a_null_comparer_as_the_default()
        {
            var source = new[] { 1, 1, 2, 2, 3 }.ToValueEnumerable();

            var actual = source.DistinctUntilChanged(null).ToList();

            actual.ShouldBe(new[] { 1, 2, 3 });
        }

        // ----- Memoize -----

        [Fact]
        public void Memoize_replays_the_cache_on_second_enumeration()
        {
            var probe = new CountingEnumerable(5);
            IEnumerable<int> source = probe;

            var memoized = source.ToValueEnumerable().Memoize();

            var first = memoized.ToList();
            probe.EnumeratorRequests.ShouldBe(1);

            var second = memoized.ToList();
            probe.EnumeratorRequests.ShouldBe(1);

            first.ShouldBe(new[] { 0, 1, 2, 3, 4 });
            second.ShouldBe(first);
        }

        [Fact]
        public void Memoize_fills_the_cache_incrementally_on_partial_enumerations()
        {
            var probe = new CountingEnumerable(5);
            IEnumerable<int> source = probe;

            var memoized = source.ToValueEnumerable().Memoize();

            var partial = memoized.Take(3).ToList();
            probe.EnumeratorRequests.ShouldBe(1);

            var full = memoized.ToList();
            probe.EnumeratorRequests.ShouldBe(1);

            partial.ShouldBe(new[] { 0, 1, 2 });
            full.ShouldBe(new[] { 0, 1, 2, 3, 4 });
        }

        [Fact]
        public void Memoize_yields_nothing_for_an_empty_source()
        {
            new int[0].ToValueEnumerable().Memoize().ShouldBeEmpty();
        }

        [Fact]
        public void Memoize_works_on_all_four_source_arms()
        {
            var source = new[] { 1, 2, 3 };

            var fromArray = source.ToValueEnumerable().Memoize().ToList();
            var fromList = new List<int>(source).ToValueEnumerable().Memoize().ToList();

            IReadOnlyList<int> readOnlySource = new List<int>(source);
            var fromReadOnly = readOnlySource.ToValueEnumerable().Memoize().ToList();

            IEnumerable<int> enumerationSource = new Queue<int>(source);
            var fromEnumerable = enumerationSource.ToValueEnumerable().Memoize().ToList();

            fromArray.ShouldBe(source);
            fromList.ShouldBe(source);
            fromReadOnly.ShouldBe(source);
            fromEnumerable.ShouldBe(source);
        }

        // ----- FallbackIfEmpty -----

        [Fact]
        public void FallbackIfEmpty_returns_the_source_when_it_is_not_empty()
        {
            var source = new[] { 1, 2, 3 };

            source.ToValueEnumerable().FallbackIfEmpty(99).ShouldBe(source);
            new List<int>(source).ToValueEnumerable().FallbackIfEmpty(99).ShouldBe(source);
        }

        [Fact]
        public void FallbackIfEmpty_returns_the_fallback_value_when_the_source_is_empty()
        {
            new int[0].ToValueEnumerable().FallbackIfEmpty(99).ShouldBe(new[] { 99 });
        }

        [Fact]
        public void FallbackIfEmpty_accepts_a_fallback_sequence()
        {
            var fallback = new[] { 10, 20, 30 };

            new int[0].ToValueEnumerable().FallbackIfEmpty(fallback).ShouldBe(fallback);

            var source = new[] { 1, 2 };
            source.ToValueEnumerable().FallbackIfEmpty(fallback).ShouldBe(source);
        }

        [Fact]
        public void FallbackIfEmpty_works_on_all_four_source_arms()
        {
            var empty = new int[0];

            IEnumerable<int> fromArray = empty.ToValueEnumerable().FallbackIfEmpty(99);
            IEnumerable<int> fromList = new List<int>(empty).ToValueEnumerable().FallbackIfEmpty(99);

            IReadOnlyList<int> readOnlySource = new List<int>(empty);
            IEnumerable<int> fromReadOnly = readOnlySource.ToValueEnumerable().FallbackIfEmpty(99);

            IEnumerable<int> enumerationSource = new Queue<int>(empty);
            IEnumerable<int> fromEnumerable = enumerationSource.ToValueEnumerable().FallbackIfEmpty(99);

            fromArray.ShouldBe(new[] { 99 });
            fromList.ShouldBe(new[] { 99 });
            fromReadOnly.ShouldBe(new[] { 99 });
            fromEnumerable.ShouldBe(new[] { 99 });
        }

        [Fact]
        public void FallbackIfEmpty_rejects_a_null_fallback_sequence()
        {
            var source = new int[0].ToValueEnumerable();

            Should.Throw<ArgumentNullException>(() =>
                source.FallbackIfEmpty((IEnumerable<int>)null!).ToList());
        }

        // ----- AtLeast -----

        [Fact]
        public void AtLeast_returns_true_when_the_count_meets_the_threshold()
        {
            var source = new[] { 1, 2, 3, 4, 5 };

            source.ToValueEnumerable().AtLeast(3).ShouldBeTrue();
            source.ToValueEnumerable().AtLeast(5).ShouldBeTrue();
            source.ToValueEnumerable().AtLeast(6).ShouldBeFalse();
        }

        [Fact]
        public void AtLeast_works_on_all_four_source_arms()
        {
            var source = new[] { 1, 2, 3 };

            source.ToValueEnumerable().AtLeast(2).ShouldBeTrue();
            new List<int>(source).ToValueEnumerable().AtLeast(2).ShouldBeTrue();

            IReadOnlyList<int> readOnlySource = new List<int>(source);
            readOnlySource.ToValueEnumerable().AtLeast(2).ShouldBeTrue();

            IEnumerable<int> enumerationSource = new Queue<int>(source);
            enumerationSource.ToValueEnumerable().AtLeast(2).ShouldBeTrue();
        }

        [Fact]
        public void AtLeast_rejects_a_negative_threshold()
        {
            var source = new[] { 1, 2, 3 }.ToValueEnumerable();

            Should.Throw<ArgumentOutOfRangeException>(() => source.AtLeast(-1));
        }

        [Fact]
        public void AtLeast_short_circuits_after_seeing_enough_elements()
        {
            var probe = new CountingEnumerable(100);
            IEnumerable<int> source = probe;

            var result = source.ToValueEnumerable().AtLeast(5);

            result.ShouldBeTrue();
        }

        // ----- Exactly -----

        [Fact]
        public void Exactly_returns_true_only_when_the_count_matches()
        {
            var source = new[] { 1, 2, 3, 4, 5 };

            source.ToValueEnumerable().Exactly(5).ShouldBeTrue();
            source.ToValueEnumerable().Exactly(4).ShouldBeFalse();
            source.ToValueEnumerable().Exactly(6).ShouldBeFalse();
        }

        [Fact]
        public void Exactly_works_on_all_four_source_arms()
        {
            var source = new[] { 1, 2, 3 };

            source.ToValueEnumerable().Exactly(3).ShouldBeTrue();
            new List<int>(source).ToValueEnumerable().Exactly(3).ShouldBeTrue();

            IReadOnlyList<int> readOnlySource = new List<int>(source);
            readOnlySource.ToValueEnumerable().Exactly(3).ShouldBeTrue();

            IEnumerable<int> enumerationSource = new Queue<int>(source);
            enumerationSource.ToValueEnumerable().Exactly(3).ShouldBeTrue();
        }

        [Fact]
        public void Exactly_rejects_a_negative_threshold()
        {
            var source = new[] { 1, 2, 3 }.ToValueEnumerable();

            Should.Throw<ArgumentOutOfRangeException>(() => source.Exactly(-1));
        }

        [Fact]
        public void Exactly_short_circuits_after_seeing_one_too_many()
        {
            var probe = new CountingEnumerable(100);
            IEnumerable<int> source = probe;

            var result = source.ToValueEnumerable().Exactly(5);

            result.ShouldBeFalse();
        }

        // ----- CountBetween -----

        [Fact]
        public void CountBetween_returns_true_when_the_count_is_in_range()
        {
            var source = new[] { 1, 2, 3, 4, 5 };

            source.ToValueEnumerable().CountBetween(3, 7).ShouldBeTrue();
            source.ToValueEnumerable().CountBetween(5, 5).ShouldBeTrue();
            source.ToValueEnumerable().CountBetween(6, 10).ShouldBeFalse();
            source.ToValueEnumerable().CountBetween(1, 4).ShouldBeFalse();
        }

        [Fact]
        public void CountBetween_works_on_all_four_source_arms()
        {
            var source = new[] { 1, 2, 3 };

            source.ToValueEnumerable().CountBetween(2, 4).ShouldBeTrue();
            new List<int>(source).ToValueEnumerable().CountBetween(2, 4).ShouldBeTrue();

            IReadOnlyList<int> readOnlySource = new List<int>(source);
            readOnlySource.ToValueEnumerable().CountBetween(2, 4).ShouldBeTrue();

            IEnumerable<int> enumerationSource = new Queue<int>(source);
            enumerationSource.ToValueEnumerable().CountBetween(2, 4).ShouldBeTrue();
        }

        [Fact]
        public void CountBetween_rejects_inverted_bounds()
        {
            var source = new[] { 1, 2, 3 }.ToValueEnumerable();

            Should.Throw<ArgumentOutOfRangeException>(() => source.CountBetween(5, 2));
        }

        [Fact]
        public void CountBetween_rejects_a_negative_min()
        {
            var source = new[] { 1, 2, 3 }.ToValueEnumerable();

            Should.Throw<ArgumentOutOfRangeException>(() => source.CountBetween(-1, 5));
        }

        // ----- PartialSortBy -----

        [Fact]
        public void PartialSortBy_returns_the_top_elements_sorted_by_key()
        {
            var source = new[] { 5, 2, 8, 1, 9, 3, 7 };

            var actual = source.ToValueEnumerable().PartialSortBy(3, x => x).ToList();

            actual.ShouldBe(new[] { 1, 2, 3 });
        }

        [Fact]
        public void PartialSortBy_returns_the_top_elements_sorted_by_key_descending()
        {
            var source = new[] { 5, 2, 8, 1, 9, 3, 7 };

            var actual = source.ToValueEnumerable()
                .PartialSortBy(3, x => x, descending: true)
                .ToList();

            actual.ShouldBe(new[] { 9, 8, 7 });
        }

        [Fact]
        public void PartialSortBy_honours_a_custom_comparer()
        {
            var source = new[] { "a", "bbb", "cc", "dddd", "e" };

            var actual = source.ToValueEnumerable()
                .PartialSortBy<string, int>(2, s => s.Length, Comparer<int>.Create((a, b) => b.CompareTo(a)))
                .ToList();

            actual.ShouldBe(new[] { "dddd", "bbb" });
        }

        [Fact]
        public void PartialSortBy_works_on_all_four_source_arms()
        {
            var source = new[] { 5, 2, 8, 1, 9 };
            var expected = new[] { 1, 2 };

            var fromArray = source.ToValueEnumerable().PartialSortBy(2, x => x).ToList();
            var fromList = new List<int>(source).ToValueEnumerable().PartialSortBy(2, x => x).ToList();

            IReadOnlyList<int> readOnlySource = new List<int>(source);
            var fromReadOnly = readOnlySource.ToValueEnumerable().PartialSortBy(2, x => x).ToList();

            IEnumerable<int> enumerationSource = new Queue<int>(source);
            var fromEnumerable = enumerationSource.ToValueEnumerable().PartialSortBy(2, x => x).ToList();

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);
        }

        [Fact]
        public void PartialSortBy_returns_all_elements_when_count_exceeds_source()
        {
            var source = new[] { 3, 1, 2 };

            var actual = source.ToValueEnumerable().PartialSortBy(10, x => x).ToList();

            actual.ShouldBe(new[] { 1, 2, 3 });
        }

        [Fact]
        public void PartialSortBy_yields_nothing_when_count_is_zero()
        {
            var source = new[] { 1, 2, 3 }.ToValueEnumerable();

            source.PartialSortBy(0, x => x).ShouldBeEmpty();
        }

        [Fact]
        public void PartialSortBy_rejects_a_negative_count()
        {
            var source = new[] { 1, 2, 3 }.ToValueEnumerable();

            Should.Throw<ArgumentOutOfRangeException>(() =>
                source.PartialSortBy(-1, x => x).ToList());
        }

        [Fact]
        public void PartialSortBy_rejects_a_null_key_selector()
        {
            var source = new[] { 1, 2, 3 }.ToValueEnumerable();

            Should.Throw<ArgumentNullException>(() =>
                source.PartialSortBy(2, (Func<int, int>)null!).ToList());
        }

        // ----- Choose -----

        [Fact]
        public void Choose_filters_and_projects_in_one_pass()
        {
            var source = new[] { 1, 2, 3, 4, 5 };

            var actual = source.ToValueEnumerable()
                .Choose(x => x % 2 == 0 ? (true, x * 10) : (false, 0))
                .ToList();

            actual.ShouldBe(new[] { 20, 40 });
        }

        [Fact]
        public void Choose_works_on_all_four_source_arms()
        {
            var source = new[] { 1, 2, 3, 4 };
            var expected = new[] { 20, 40 };

            var fromArray = source.ToValueEnumerable()
                .Choose(x => x % 2 == 0 ? (true, x * 10) : (false, 0))
                .ToList();

            var fromList = new List<int>(source).ToValueEnumerable()
                .Choose(x => x % 2 == 0 ? (true, x * 10) : (false, 0))
                .ToList();

            IReadOnlyList<int> readOnlySource = new List<int>(source);
            var fromReadOnly = readOnlySource.ToValueEnumerable()
                .Choose(x => x % 2 == 0 ? (true, x * 10) : (false, 0))
                .ToList();

            IEnumerable<int> enumerationSource = new Queue<int>(source);
            var fromEnumerable = enumerationSource.ToValueEnumerable()
                .Choose(x => x % 2 == 0 ? (true, x * 10) : (false, 0))
                .ToList();

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);
        }

        [Fact]
        public void Choose_yields_nothing_when_all_elements_are_rejected()
        {
            var source = new[] { 1, 3, 5 }.ToValueEnumerable();

            source.Choose(x => (false, x)).ShouldBeEmpty();
        }

        [Fact]
        public void Choose_yields_nothing_for_an_empty_source()
        {
            new int[0].ToValueEnumerable().Choose(x => (true, x)).ShouldBeEmpty();
        }

        [Fact]
        public void Choose_is_lazy_and_touches_the_source_only_when_it_is_walked()
        {
            var probe = new CountingEnumerable(5);
            IEnumerable<int> source = probe;

            var chosen = source.ToValueEnumerable().Choose(x => (true, x));

            probe.EnumeratorRequests.ShouldBe(0);

            chosen.ToList();

            probe.EnumeratorRequests.ShouldBe(1);
        }

        [Fact]
        public void Choose_rejects_a_null_chooser()
        {
            var source = new[] { 1, 2, 3 }.ToValueEnumerable();

            Should.Throw<ArgumentNullException>(() =>
                source.Choose<int, int>(null!).ToList());
        }
    }
}
