using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Tests
{
    /// <summary>
    /// F7-10 (first half): the windowing and splitting operators of the P2 layer - <c>Window</c>,
    /// <c>WindowLeft</c> and <c>Split</c>.
    /// </summary>
    /// <remarks>
    /// None of these operators exists in the BCL, so there is no <c>System.Linq</c> oracle to compare
    /// against. The tests therefore pin the semantics directly, and the two that are easy to get
    /// wrong get their own cases: <c>Window</c> drops the partial tail while <c>WindowLeft</c> pads
    /// it, and <c>Split</c> preserves empty segments instead of dropping them.
    /// </remarks>
    public class P2WindowAndSplitOperatorTests
    {
        private static readonly int[] Numbers = { 4, 1, 9, 2, 7 };

        // ----- Window -----

        [Fact]
        public void Window_produces_only_complete_windows_on_every_arm()
        {
            var expected = new[]
            {
                new[] { 4, 1, 9 },
                new[] { 1, 9, 2 },
                new[] { 9, 2, 7 },
            };

            var fromArray = Numbers.ToValueEnumerable().Window(3).ToArray();
            var fromList = new List<int>(Numbers).ToValueEnumerable().Window(3).ToArray();

            IReadOnlyList<int> readOnlySource = new List<int>(Numbers);
            var fromReadOnly = readOnlySource.ToValueEnumerable().Window(3).ToArray();

            IEnumerable<int> enumerationSource = new Queue<int>(Numbers);
            var fromEnumerable = enumerationSource.ToValueEnumerable().Window(3).ToArray();

            foreach (var actual in new[] { fromArray, fromList, fromReadOnly, fromEnumerable })
            {
                actual.Length.ShouldBe(3);
                for (var i = 0; i < expected.Length; i++)
                {
                    actual[i].ShouldBe(expected[i]);
                }
            }
        }

        [Fact]
        public void Window_yields_nothing_when_the_source_is_shorter_than_the_size()
        {
            Numbers.ToValueEnumerable().Window(5).Count().ShouldBe(1);
            Numbers.ToValueEnumerable().Window(6).ShouldBeEmpty();
            Array.Empty<int>().ToValueEnumerable().Window(1).ShouldBeEmpty();
        }

        [Fact]
        public void Window_yields_independent_arrays()
        {
            var windows = Numbers.ToValueEnumerable().Window(2).ToArray();

            // Holding on to the first window must not let a later shift mutate it. This is the whole
            // reason each window is a fresh array instead of a view over the buffer.
            windows[0].ShouldBe(new[] { 4, 1 });
            windows[1].ShouldBe(new[] { 1, 9 });
        }

        [Fact]
        public void Window_rejects_a_size_below_one()
        {
            Should.Throw<ArgumentOutOfRangeException>(() => Numbers.ToValueEnumerable().Window(0));
            Should.Throw<ArgumentOutOfRangeException>(() => Numbers.ToValueEnumerable().Window(-1));
        }

        // ----- WindowLeft -----

        [Fact]
        public void WindowLeft_produces_one_window_per_element_and_pads_the_tail()
        {
            var expected = new[]
            {
                new[] { 1, 2, 3 },
                new[] { 2, 3, 4 },
                new[] { 3, 4, 5 },
                new[] { 4, 5, 0 },
                new[] { 5, 0, 0 },
            };

            var actual = new[] { 1, 2, 3, 4, 5 }.ToValueEnumerable().WindowLeft(3).ToArray();

            actual.Length.ShouldBe(5);
            for (var i = 0; i < expected.Length; i++)
            {
                actual[i].ShouldBe(expected[i]);
            }
        }

        [Fact]
        public void WindowLeft_pads_everything_when_the_source_is_shorter_than_the_size()
        {
            var actual = new[] { 1, 2 }.ToValueEnumerable().WindowLeft(3).ToArray();

            actual.Length.ShouldBe(2);
            actual[0].ShouldBe(new[] { 1, 2, 0 });
            actual[1].ShouldBe(new[] { 2, 0, 0 });
        }

        [Fact]
        public void WindowLeft_answers_nothing_for_an_empty_source()
        {
            Array.Empty<int>().ToValueEnumerable().WindowLeft(3).ShouldBeEmpty();
        }

        [Fact]
        public void WindowLeft_agrees_across_the_four_arms()
        {
            var expected = new[] { 1, 2, 3, 4 }.ToValueEnumerable().WindowLeft(2).ToArray();

            var fromList = new List<int> { 1, 2, 3, 4 }.ToValueEnumerable().WindowLeft(2).ToArray();

            IReadOnlyList<int> readOnlySource = new List<int> { 1, 2, 3, 4 };
            var fromReadOnly = readOnlySource.ToValueEnumerable().WindowLeft(2).ToArray();

            IEnumerable<int> enumerationSource = new Queue<int>(new[] { 1, 2, 3, 4 });
            var fromEnumerable = enumerationSource.ToValueEnumerable().WindowLeft(2).ToArray();

            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);
        }

        // ----- Split (by separator value) -----

        [Fact]
        public void Split_cuts_on_the_separator_and_keeps_empty_segments()
        {
            var actual = new[] { 1, 0, 2, 0, 0, 3 }.ToValueEnumerable().Split(0).ToArray();

            actual.Length.ShouldBe(4);
            actual[0].ShouldBe(new[] { 1 });
            actual[1].ShouldBe(new[] { 2 });
            actual[2].ShouldBeEmpty();
            actual[3].ShouldBe(new[] { 3 });
        }

        [Fact]
        public void Split_keeps_a_trailing_empty_segment()
        {
            var actual = new[] { 1, 0 }.ToValueEnumerable().Split(0).ToArray();

            actual.Length.ShouldBe(2);
            actual[0].ShouldBe(new[] { 1 });
            actual[1].ShouldBeEmpty();
        }

        [Fact]
        public void Split_on_an_empty_source_yields_no_segment_at_all()
        {
            Array.Empty<int>().ToValueEnumerable().Split(0).ShouldBeEmpty();
        }

        [Fact]
        public void Split_on_a_source_that_is_only_separators_yields_empty_segments()
        {
            var actual = new[] { 0, 0 }.ToValueEnumerable().Split(0).ToArray();

            actual.Length.ShouldBe(3);
            actual.ShouldAllBe(segment => segment.Length == 0);
        }

        [Fact]
        public void Split_compares_the_separator_with_the_given_comparer()
        {
            var actual = new[] { "a", "X", "b", "x", "c" }
                .ToValueEnumerable()
                .Split("x", StringComparer.OrdinalIgnoreCase)
                .Select(segment => string.Join(string.Empty, segment))
                .ToArray();

            actual.ShouldBe(new[] { "a", "b", "c" });
        }

        // ----- Split (by separator predicate) -----

        [Fact]
        public void Split_by_predicate_cuts_wherever_the_predicate_holds()
        {
            var actual = new[] { 1, 2, 3, 4, 5 }
                .ToValueEnumerable()
                .Split(value => value % 2 == 0)
                .Select(segment => string.Join(",", segment))
                .ToArray();

            actual.ShouldBe(new[] { "1", "3", "5" });
        }

        [Fact]
        public void Split_by_predicate_agrees_across_the_four_arms()
        {
            static bool IsSeparator(int value) => value == 0;

            var fromArray = new[] { 1, 0, 2 }.ToValueEnumerable().Split(IsSeparator).ToArray();
            var fromList = new List<int> { 1, 0, 2 }.ToValueEnumerable().Split(IsSeparator).ToArray();

            IReadOnlyList<int> readOnlySource = new List<int> { 1, 0, 2 };
            var fromReadOnly = readOnlySource.ToValueEnumerable().Split(IsSeparator).ToArray();

            IEnumerable<int> enumerationSource = new Queue<int>(new[] { 1, 0, 2 });
            var fromEnumerable = enumerationSource.ToValueEnumerable().Split(IsSeparator).ToArray();

            fromList.ShouldBe(fromArray);
            fromReadOnly.ShouldBe(fromArray);
            fromEnumerable.ShouldBe(fromArray);
            fromArray.Length.ShouldBe(2);
        }

        [Fact]
        public void Split_rejects_a_null_predicate()
        {
            Should.Throw<ArgumentNullException>(
                () => Numbers.ToValueEnumerable().Split((Func<int, bool>)null!));
        }
    }
}
