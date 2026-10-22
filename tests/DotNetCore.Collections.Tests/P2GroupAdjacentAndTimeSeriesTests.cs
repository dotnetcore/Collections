using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Tests
{
    /// <summary>
    /// F7-10 (second half): the grouping, time-series and combination operators of the P2 layer -
    /// <c>GroupAdjacent</c>, <c>Lag</c>, <c>Lead</c>, <c>ZipLongest</c> and <c>Interleave</c>.
    /// </summary>
    /// <remarks>
    /// These are engine-only operators with no BCL counterpart, so the tests pin the semantics
    /// directly. The three contracts worth watching are: <c>GroupAdjacent</c> groups runs rather than
    /// all matches, <c>Lag</c> / <c>Lead</c> keep the row and substitute <see langword="default"/>
    /// at the edge (SQL window semantics), and <c>ZipLongest</c> runs to the longer side instead of
    /// truncating like <c>Zip</c>.
    /// </remarks>
    public class P2GroupAdjacentAndTimeSeriesTests
    {
        // ----- GroupAdjacent -----

        [Fact]
        public void GroupAdjacent_groups_runs_rather_than_all_matches()
        {
            var groups = new[] { 1, 1, 2, 1 }.ToValueEnumerable().GroupAdjacent(value => value).ToArray();

            // Three runs, not two groups: the trailing 1 is a run of its own.
            groups.Length.ShouldBe(3);
            groups[0].Key.ShouldBe(1);
            groups[0].ShouldBe(new[] { 1, 1 });
            groups[1].Key.ShouldBe(2);
            groups[1].ShouldBe(new[] { 2 });
            groups[2].Key.ShouldBe(1);
            groups[2].ShouldBe(new[] { 1 });
        }

        [Fact]
        public void GroupAdjacent_agrees_across_the_four_arms()
        {
            var expected = new[] { 1, 1, 2, 1 }.ToValueEnumerable().GroupAdjacent(value => value).ToArray();

            var fromList = new List<int> { 1, 1, 2, 1 }.ToValueEnumerable().GroupAdjacent(value => value).ToArray();

            IReadOnlyList<int> readOnlySource = new List<int> { 1, 1, 2, 1 };
            var fromReadOnly = readOnlySource.ToValueEnumerable().GroupAdjacent(value => value).ToArray();

            IEnumerable<int> enumerationSource = new Queue<int>(new[] { 1, 1, 2, 1 });
            var fromEnumerable = enumerationSource.ToValueEnumerable().GroupAdjacent(value => value).ToArray();

            fromList.Length.ShouldBe(expected.Length);
            fromReadOnly.Length.ShouldBe(expected.Length);
            fromEnumerable.Length.ShouldBe(expected.Length);

            for (var i = 0; i < expected.Length; i++)
            {
                fromList[i].ShouldBe(expected[i]);
                fromReadOnly[i].ShouldBe(expected[i]);
                fromEnumerable[i].ShouldBe(expected[i]);
            }
        }

        [Fact]
        public void GroupAdjacent_uses_the_given_comparer_for_the_key()
        {
            var groups = new[] { "a", "A", "b", "B", "B" }
                .ToValueEnumerable()
                .GroupAdjacent(text => text, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            groups.Length.ShouldBe(2);
            groups[0].ShouldBe(new[] { "a", "A" });
            groups[1].ShouldBe(new[] { "b", "B", "B" });
        }

        [Fact]
        public void GroupAdjacent_answers_nothing_for_an_empty_source()
        {
            Array.Empty<int>().ToValueEnumerable().GroupAdjacent(value => value).ShouldBeEmpty();
        }

        [Fact]
        public void GroupAdjacent_rejects_a_null_key_selector()
        {
            Should.Throw<ArgumentNullException>(
                () => new[] { 1 }.ToValueEnumerable().GroupAdjacent((Func<int, int>)null!));
        }

        // ----- Lag -----

        [Fact]
        public void Lag_pairs_each_element_with_its_predecessor()
        {
            var actual = new[] { 10, 20, 30 }
                .ToValueEnumerable()
                .Lag((current, lagged) => current + "-" + lagged)
                .ToArray();

            // The first element has no predecessor, so the lagged side is default (0 here) - the row
            // is kept, which is what LAG does in SQL.
            actual.ShouldBe(new[] { "10-0", "20-10", "30-20" });
        }

        [Fact]
        public void Lag_with_an_offset_of_two_reaches_two_positions_back()
        {
            var actual = new[] { 1, 2, 3, 4 }
                .ToValueEnumerable()
                .Lag(2, (current, lagged) => current * 100 + lagged)
                .ToArray();

            actual.ShouldBe(new[] { 100, 200, 301, 402 });
        }

        [Fact]
        public void Lag_with_a_zero_offset_pairs_an_element_with_itself()
        {
            var actual = new[] { 1, 2, 3 }
                .ToValueEnumerable()
                .Lag(0, (current, lagged) => current + "-" + lagged)
                .ToArray();

            actual.ShouldBe(new[] { "1-1", "2-2", "3-3" });
        }

        [Fact]
        public void Lag_agrees_across_the_four_arms()
        {
            var expected = new[] { 1, 2, 3 }.ToValueEnumerable().Lag((c, l) => c - l).ToArray();

            var fromList = new List<int> { 1, 2, 3 }.ToValueEnumerable().Lag((c, l) => c - l).ToArray();

            IReadOnlyList<int> readOnlySource = new List<int> { 1, 2, 3 };
            var fromReadOnly = readOnlySource.ToValueEnumerable().Lag((c, l) => c - l).ToArray();

            IEnumerable<int> enumerationSource = new Queue<int>(new[] { 1, 2, 3 });
            var fromEnumerable = enumerationSource.ToValueEnumerable().Lag((c, l) => c - l).ToArray();

            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);
            expected.ShouldBe(new[] { 1, 1, 1 });
        }

        [Fact]
        public void Lag_rejects_a_negative_offset()
        {
            Should.Throw<ArgumentOutOfRangeException>(
                () => new[] { 1 }.ToValueEnumerable().Lag(-1, (c, l) => c));
        }

        // ----- Lead -----

        [Fact]
        public void Lead_pairs_each_element_with_its_successor()
        {
            var actual = new[] { 10, 20, 30 }
                .ToValueEnumerable()
                .Lead((current, lead) => current + "-" + lead)
                .ToArray();

            actual.ShouldBe(new[] { "10-20", "20-30", "30-0" });
        }

        [Fact]
        public void Lead_with_an_offset_of_two_reaches_two_positions_ahead()
        {
            var actual = new[] { 1, 2, 3, 4 }
                .ToValueEnumerable()
                .Lead(2, (current, lead) => current * 100 + lead)
                .ToArray();

            // 1 -> 3, 2 -> 4, then the last two rows have no successor.
            actual.ShouldBe(new[] { 103, 204, 300, 400 });
        }

        [Fact]
        public void Lead_agrees_across_the_four_arms()
        {
            var expected = new[] { 1, 2, 3 }.ToValueEnumerable().Lead((c, l) => c - l).ToArray();

            var fromList = new List<int> { 1, 2, 3 }.ToValueEnumerable().Lead((c, l) => c - l).ToArray();

            IReadOnlyList<int> readOnlySource = new List<int> { 1, 2, 3 };
            var fromReadOnly = readOnlySource.ToValueEnumerable().Lead((c, l) => c - l).ToArray();

            IEnumerable<int> enumerationSource = new Queue<int>(new[] { 1, 2, 3 });
            var fromEnumerable = enumerationSource.ToValueEnumerable().Lead((c, l) => c - l).ToArray();

            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);
            expected.ShouldBe(new[] { -1, -1, 3 });
        }

        [Fact]
        public void Lead_rejects_a_negative_offset()
        {
            Should.Throw<ArgumentOutOfRangeException>(
                () => new[] { 1 }.ToValueEnumerable().Lead(-1, (c, l) => c));
        }

        // ----- ZipLongest -----

        [Fact]
        public void ZipLongest_runs_to_the_longer_first_side()
        {
            var actual = new[] { 1, 2, 3 }
                .ToValueEnumerable()
                .ZipLongest(new[] { "a", "b" }, (number, text) => number + text)
                .ToArray();

            // The BCL's Zip would stop at "2b"; this one keeps the third row with a default right side.
            actual.ShouldBe(new[] { "1a", "2b", "3" });
        }

        [Fact]
        public void ZipLongest_runs_to_the_longer_second_side()
        {
            var actual = new[] { 1 }
                .ToValueEnumerable()
                .ZipLongest(new[] { "a", "b" }, (number, text) => number + text)
                .ToArray();

            actual.ShouldBe(new[] { "1a", "0b" });
        }

        [Fact]
        public void ZipLongest_answers_nothing_when_both_sides_are_empty()
        {
            Array.Empty<int>()
                .ToValueEnumerable()
                .ZipLongest(Array.Empty<string>(), (number, text) => number + text)
                .ShouldBeEmpty();
        }

        [Fact]
        public void ZipLongest_agrees_across_the_four_arms()
        {
            var second = new[] { "a", "b" };

            var expected = new[] { 1, 2, 3 }.ToValueEnumerable().ZipLongest(second, (n, t) => n + t).ToArray();
            var fromList = new List<int> { 1, 2, 3 }.ToValueEnumerable().ZipLongest(second, (n, t) => n + t).ToArray();

            IReadOnlyList<int> readOnlySource = new List<int> { 1, 2, 3 };
            var fromReadOnly = readOnlySource.ToValueEnumerable().ZipLongest(second, (n, t) => n + t).ToArray();

            IEnumerable<int> enumerationSource = new Queue<int>(new[] { 1, 2, 3 });
            var fromEnumerable = enumerationSource.ToValueEnumerable().ZipLongest(second, (n, t) => n + t).ToArray();

            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);
        }

        [Fact]
        public void ZipLongest_rejects_a_null_second_sequence()
        {
            Should.Throw<ArgumentNullException>(
                () => new[] { 1 }.ToValueEnumerable().ZipLongest((IEnumerable<string>)null!, (int n, string t) => n));
        }

        // ----- Interleave -----

        [Fact]
        public void Interleave_rotates_over_the_sources()
        {
            var actual = new[] { 1, 2, 3 }
                .ToValueEnumerable()
                .Interleave(new[] { 10, 20, 30 }, new[] { 100 })
                .ToArray();

            // One element per source per round; the third source runs dry after its only element.
            actual.ShouldBe(new[] { 1, 10, 100, 2, 20, 3, 30 });
        }

        [Fact]
        public void Interleave_drops_a_finished_source_instead_of_stalling()
        {
            var actual = new[] { 1 }
                .ToValueEnumerable()
                .Interleave(new[] { 10, 20, 30 })
                .ToArray();

            actual.ShouldBe(new[] { 1, 10, 20, 30 });
        }

        [Fact]
        public void Interleave_with_no_other_source_returns_the_source()
        {
            var actual = new[] { 1, 2, 3 }.ToValueEnumerable().Interleave().ToArray();

            actual.ShouldBe(new[] { 1, 2, 3 });
        }

        [Fact]
        public void Interleave_agrees_across_the_four_arms()
        {
            var second = new[] { 10, 20 };

            var expected = new[] { 1, 2, 3 }.ToValueEnumerable().Interleave(second).ToArray();
            var fromList = new List<int> { 1, 2, 3 }.ToValueEnumerable().Interleave(second).ToArray();

            IReadOnlyList<int> readOnlySource = new List<int> { 1, 2, 3 };
            var fromReadOnly = readOnlySource.ToValueEnumerable().Interleave(second).ToArray();

            IEnumerable<int> enumerationSource = new Queue<int>(new[] { 1, 2, 3 });
            var fromEnumerable = enumerationSource.ToValueEnumerable().Interleave(second).ToArray();

            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);
            expected.ShouldBe(new[] { 1, 10, 2, 20, 3 });
        }
    }
}
