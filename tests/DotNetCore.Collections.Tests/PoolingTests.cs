using System;
using System.Buffers;
using System.Collections.Generic;
using DotNetCore.Collections;
using DotNetCore.Collections.Internal;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Tests
{
    /// <summary>
    /// F7-03: materialisation into pooled storage. The pool itself is not observable from the
    /// outside, so the assertions are about the contract a caller can rely on - the right elements
    /// in the right order, a buffer that is at least as long as the sequence, and a buffer that is
    /// a copy rather than a view of the source.
    /// </summary>
    public class PoolingTests
    {
        [Fact]
        public void ToPooledList_answers_across_every_arm()
        {
            var expected = new[] { 1, 2, 3, 4 };

            AssertPooledList(new[] { 1, 2, 3, 4 }.ToValueEnumerable().ToPooledList(), expected);
            AssertPooledList(new List<int> { 1, 2, 3, 4 }.ToValueEnumerable().ToPooledList(), expected);

            IReadOnlyList<int> readOnly = new List<int> { 1, 2, 3, 4 };
            AssertPooledList(readOnly.ToValueEnumerable().ToPooledList(), expected);

            IEnumerable<int> arbitrary = new Queue<int>(new[] { 1, 2, 3, 4 });
            AssertPooledList(arbitrary.ToValueEnumerable().ToPooledList(), expected);
        }

        [Fact]
        public void ToPooledList_walks_only_the_live_elements_of_a_list_with_spare_capacity()
        {
            var source = new List<int>(64);
            source.Add(11);
            source.Add(12);
            source.Add(13);

            ListLayoutAccessor.GetItems(source).Length.ShouldBeGreaterThan(source.Count);

            AssertPooledList(source.ToValueEnumerable().ToPooledList(), new[] { 11, 12, 13 });
        }

        [Fact]
        public void ToPooledList_of_a_lazy_sequence_grows_the_buffer_and_keeps_every_element()
        {
            var probe = new CountingEnumerable(100);
            IEnumerable<int> source = probe;

            using (var list = source.ToValueEnumerable().ToPooledList())
            {
                list.Count.ShouldBe(100);
                for (var index = 0; index < 100; index++)
                {
                    list[index].ShouldBe(index);
                }
            }

            probe.EnumeratorRequests.ShouldBe(1);
        }

        [Fact]
        public void ToPooledList_of_an_empty_source_is_empty()
        {
            using (var list = new int[0].ToValueEnumerable().ToPooledList())
            {
                list.Count.ShouldBe(0);
                list.AsSpan().Length.ShouldBe(0);
                list.ToArray().ShouldBeEmpty();
            }
        }

        [Fact]
        public void Pooled_list_grows_past_every_capacity_step()
        {
            using (var list = new PooledList<int>(1))
            {
                for (var index = 0; index < 1000; index++)
                {
                    list.Add(index * 2);
                    list.Count.ShouldBe(index + 1);
                }

                for (var index = 0; index < 1000; index++)
                {
                    list[index].ShouldBe(index * 2);
                }

                list.ToArray().Length.ShouldBe(1000);
            }
        }

        [Fact]
        public void Pooled_list_appends_a_span_in_one_step()
        {
            using (var list = new PooledList<string>(2))
            {
                list.Add("a");

                var block = new string[3];
                block[0] = "b";
                block[1] = "c";
                block[2] = "d";

                list.AddRange(block);
                list.AddRange(new string[0]);

                list.Count.ShouldBe(4);
                list.AsSpan().ToArray().ShouldBe(new[] { "a", "b", "c", "d" });
            }
        }

        [Fact]
        public void Pooled_list_indexer_checks_the_bounds()
        {
            using (var list = new PooledList<int>())
            {
                list.Add(5);

                list[0].ShouldBe(5);
                Should.Throw<ArgumentOutOfRangeException>(() => { var unused = list[-1]; });
                Should.Throw<ArgumentOutOfRangeException>(() => { var unused = list[1]; });
            }
        }

        [Fact]
        public void Disposing_a_pooled_list_twice_is_safe_and_leaves_it_empty()
        {
            var list = new[] { 1, 2, 3 }.ToValueEnumerable().ToPooledList();

            list.Dispose();
            list.Dispose();

            list.Count.ShouldBe(0);
            list.ToArray().ShouldBeEmpty();
        }

        [Fact]
        public void ToArrayPool_returns_a_right_sized_copy_of_the_sequence()
        {
            var source = new[] { 1, 2, 3 };

            var buffer = source.ToValueEnumerable().ToArrayPool(out var length);
            try
            {
                length.ShouldBe(3);

                // The pool contract: the array is at least as long as what was asked for, which is
                // exactly why the caller needs the length back.
                buffer.Length.ShouldBeGreaterThanOrEqualTo(3);
                buffer[0].ShouldBe(1);
                buffer[1].ShouldBe(2);
                buffer[2].ShouldBe(3);

                // A copy, not a view: writing through the buffer must not touch the source.
                buffer[0] = 99;
                source[0].ShouldBe(1);
            }
            finally
            {
                ArrayPool<int>.Shared.Return(buffer);
            }
        }

        [Fact]
        public void ToArrayPool_answers_across_every_arm()
        {
            var fromList = new List<int> { 1, 2, 3 }.ToValueEnumerable().ToArrayPool(out var listLength);
            AssertArrayPool(fromList, listLength, 1, 2, 3);
            ArrayPool<int>.Shared.Return(fromList);

            IReadOnlyList<int> readOnly = new List<int> { 1, 2, 3 };
            var fromReadOnly = readOnly.ToValueEnumerable().ToArrayPool(out var readOnlyLength);
            AssertArrayPool(fromReadOnly, readOnlyLength, 1, 2, 3);
            ArrayPool<int>.Shared.Return(fromReadOnly);

            IEnumerable<int> arbitrary = new Queue<int>(new[] { 1, 2, 3 });
            var fromEnumerable = arbitrary.ToValueEnumerable().ToArrayPool(out var enumerableLength);
            AssertArrayPool(fromEnumerable, enumerableLength, 1, 2, 3);
            ArrayPool<int>.Shared.Return(fromEnumerable);
        }

        [Fact]
        public void ToArrayPool_sizes_the_buffer_from_the_walk_when_the_count_is_unknown()
        {
            var probe = new CountingEnumerable(7);
            IEnumerable<int> source = probe;

            var buffer = source.ToValueEnumerable().ToArrayPool(out var length);
            try
            {
                length.ShouldBe(7);
                buffer.Length.ShouldBeGreaterThanOrEqualTo(7);
                for (var index = 0; index < length; index++)
                {
                    buffer[index].ShouldBe(index);
                }
            }
            finally
            {
                ArrayPool<int>.Shared.Return(buffer);
            }

            probe.EnumeratorRequests.ShouldBe(1);
        }

        [Fact]
        public void ToArrayPool_of_an_empty_source_reports_no_elements()
        {
            var buffer = new int[0].ToValueEnumerable().ToArrayPool(out var length);
            try
            {
                length.ShouldBe(0);

                // Still a pool array rather than a shared zero-length instance, so the caller's
                // Return is always valid.
                buffer.Length.ShouldBeGreaterThanOrEqualTo(1);
            }
            finally
            {
                ArrayPool<int>.Shared.Return(buffer);
            }
        }

        private static void AssertPooledList(PooledList<int> list, int[] expected)
        {
            using (list)
            {
                list.Count.ShouldBe(expected.Length);

                for (var index = 0; index < expected.Length; index++)
                {
                    list[index].ShouldBe(expected[index]);
                }

                list.AsSpan().ToArray().ShouldBe(expected);
                list.ToArray().ShouldBe(expected);
            }
        }

        private static void AssertArrayPool(int[] buffer, int length, int first, int second, int third)
        {
            length.ShouldBe(3);
            buffer.Length.ShouldBeGreaterThanOrEqualTo(3);
            buffer[0].ShouldBe(first);
            buffer[1].ShouldBe(second);
            buffer[2].ShouldBe(third);
        }
    }
}
