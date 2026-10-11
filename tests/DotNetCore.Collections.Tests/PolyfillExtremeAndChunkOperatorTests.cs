using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Tests
{
    /// <summary>
    /// F7-06 (second batch): the extreme-key and chunking operators of the P0 polyfill layer -
    /// <c>MaxBy</c>, <c>MinBy</c> and <c>Chunk</c>.
    /// </summary>
    /// <remarks>
    /// The suite runs on net8.0, where the BCL ships all three, so each engine answer is compared
    /// with the <c>System.Linq</c> expression it claims to be equivalent to. The empty-source
    /// contract of <c>MaxBy</c> / <c>MinBy</c> is the interesting one: it is not uniform across
    /// element types, and the tests pin both halves of it.
    /// </remarks>
    public class PolyfillExtremeAndChunkOperatorTests
    {
        private static readonly int[] Numbers = { 4, 1, 9, 2, 7 };

        private static int KeyOf(int value) => value;

        // ----- MaxBy -----

        [Fact]
        public void MaxBy_answers_with_the_greatest_key_on_every_arm()
        {
            var expected = Numbers.MaxBy(KeyOf);

            var fromArray = Numbers.ToValueEnumerable().MaxBy(KeyOf);
            var fromList = new List<int>(Numbers).ToValueEnumerable().MaxBy(KeyOf);

            IReadOnlyList<int> readOnlySource = new List<int>(Numbers);
            var fromReadOnly = readOnlySource.ToValueEnumerable().MaxBy(KeyOf);

            IEnumerable<int> enumerationSource = new Queue<int>(Numbers);
            var fromEnumerable = enumerationSource.ToValueEnumerable().MaxBy(KeyOf);

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);

            expected.ShouldBe(9);
        }

        [Fact]
        public void MaxBy_keeps_the_first_element_that_reached_the_extreme()
        {
            var source = new[] { "bb", "a", "cc", "d" };

            var expected = source.MaxBy(text => text.Length);
            var actual = source.ToValueEnumerable().MaxBy(text => text.Length);

            actual.ShouldBe(expected);
            actual.ShouldBe("bb");
        }

        [Fact]
        public void MaxBy_honours_a_custom_comparer()
        {
            var source = new[] { "a", "bb", "ccc" };

            var expected = source.MaxBy(text => text, StringComparer.OrdinalIgnoreCase);
            var actual = source.ToValueEnumerable().MaxBy(text => text, StringComparer.OrdinalIgnoreCase);

            actual.ShouldBe(expected);
            actual.ShouldBe("ccc");
        }

        [Fact]
        public void MaxBy_throws_on_an_empty_value_typed_source_and_answers_null_on_a_reference_typed_one()
        {
            // The BCL's contract is not "throw on empty": it throws only when the element type
            // cannot be null, because there is no value to hand back. Both halves are pinned here
            // because the polyfill has to reproduce them for a low-TFM caller's code to keep its
            // meaning when it moves to a target that has the BCL operator.
            Should.Throw<InvalidOperationException>(() => new int[0].ToValueEnumerable().MaxBy(KeyOf))
                .Message.ShouldBe("Sequence contains no elements.");

            new string[0].ToValueEnumerable().MaxBy(text => text).ShouldBeNull();
        }

        // ----- MinBy -----

        [Fact]
        public void MinBy_answers_with_the_smallest_key_on_every_arm()
        {
            var expected = Numbers.MinBy(KeyOf);

            var fromArray = Numbers.ToValueEnumerable().MinBy(KeyOf);
            var fromList = new List<int>(Numbers).ToValueEnumerable().MinBy(KeyOf);

            IReadOnlyList<int> readOnlySource = new List<int>(Numbers);
            var fromReadOnly = readOnlySource.ToValueEnumerable().MinBy(KeyOf);

            IEnumerable<int> enumerationSource = new Queue<int>(Numbers);
            var fromEnumerable = enumerationSource.ToValueEnumerable().MinBy(KeyOf);

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);

            expected.ShouldBe(1);
        }

        [Fact]
        public void MinBy_keeps_the_first_element_that_reached_the_extreme()
        {
            var source = new[] { "bb", "a", "cc", "d" };

            var expected = source.MinBy(text => text.Length);
            var actual = source.ToValueEnumerable().MinBy(text => text.Length);

            actual.ShouldBe(expected);
            actual.ShouldBe("a");
        }

        [Fact]
        public void MinBy_throws_on_an_empty_value_typed_source_and_answers_null_on_a_reference_typed_one()
        {
            Should.Throw<InvalidOperationException>(() => new int[0].ToValueEnumerable().MinBy(KeyOf))
                .Message.ShouldBe("Sequence contains no elements.");

            new string[0].ToValueEnumerable().MinBy(text => text).ShouldBeNull();
        }

        [Fact]
        public void The_extremes_read_each_element_once()
        {
            var probe = new CountingEnumerable(16);
            IEnumerable<int> source = probe;

            source.ToValueEnumerable().MaxBy(value => -value).ShouldBe(0);

            probe.EnumeratorRequests.ShouldBe(1);
            probe.MoveNextCalls.ShouldBe(17);
        }

        // ----- Chunk -----

        [Fact]
        public void Chunk_splits_the_source_on_every_arm()
        {
            var expected = Describe(new[] { 1, 2, 3, 4, 5 }.Chunk(2));

            var fromArray = Describe(new[] { 1, 2, 3, 4, 5 }.ToValueEnumerable().Chunk(2));
            var fromList = Describe(new List<int> { 1, 2, 3, 4, 5 }.ToValueEnumerable().Chunk(2));

            IReadOnlyList<int> readOnlySource = new List<int> { 1, 2, 3, 4, 5 };
            var fromReadOnly = Describe(readOnlySource.ToValueEnumerable().Chunk(2));

            IEnumerable<int> enumerationSource = new Queue<int>(new[] { 1, 2, 3, 4, 5 });
            var fromEnumerable = Describe(enumerationSource.ToValueEnumerable().Chunk(2));

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);

            expected.ShouldBe(new[] { "1,2", "3,4", "5" });
        }

        [Fact]
        public void Chunk_resizes_the_last_chunk_rather_than_padding_it()
        {
            var chunks = new[] { 1, 2, 3 }.ToValueEnumerable().Chunk(2).ToList();

            chunks.Count.ShouldBe(2);
            chunks[0].Length.ShouldBe(2);
            chunks[1].Length.ShouldBe(1);
        }

        [Fact]
        public void Chunk_yields_nothing_for_an_empty_source()
        {
            new int[0].ToValueEnumerable().Chunk(3).ShouldBeEmpty();
        }

        [Fact]
        public void Chunk_rejects_a_size_below_one_when_the_pipeline_is_built()
        {
            var source = new[] { 1, 2, 3 }.ToValueEnumerable();

            Should.Throw<ArgumentOutOfRangeException>(() => source.Chunk(0));
            Should.Throw<ArgumentOutOfRangeException>(() => source.Chunk(-1));

            IEnumerable<int> raw = new Queue<int>(new[] { 1, 2, 3 });
            Should.Throw<ArgumentOutOfRangeException>(() => raw.ToValueEnumerable().Chunk(0));
        }

        [Fact]
        public void Chunk_is_lazy_and_reads_only_what_the_consumer_asks_for()
        {
            var probe = new CountingEnumerable(64);
            IEnumerable<int> source = probe;

            var chunks = source.ToValueEnumerable().Chunk(4);

            probe.EnumeratorRequests.ShouldBe(0);

            chunks.First().Length.ShouldBe(4);
            probe.EnumeratorRequests.ShouldBe(1);
            probe.MoveNextCalls.ShouldBe(4);
        }

        [Fact]
        public void Null_key_selectors_are_rejected()
        {
            var source = Numbers.ToValueEnumerable();

            Should.Throw<ArgumentNullException>(() => source.MaxBy<int, int>(null));
            Should.Throw<ArgumentNullException>(() => source.MinBy<int, int>(null));

            IEnumerable<int> raw = new Queue<int>(Numbers);
            var enumerableSource = raw.ToValueEnumerable();

            Should.Throw<ArgumentNullException>(() => enumerableSource.MaxBy<int, int>(null));
            Should.Throw<ArgumentNullException>(() => enumerableSource.MinBy<int, int>(null));
        }

        private static List<string> Describe(IEnumerable<int[]> chunks)
            => chunks.Select(chunk => string.Join(",", chunk)).ToList();
    }
}
