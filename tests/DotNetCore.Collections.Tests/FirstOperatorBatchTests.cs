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
    /// F7-05: the first batch of the engine's public operator surface -
    /// <c>Index</c>, <c>ForEach</c>, <c>TagFirstLast</c>, <c>Pairwise</c> and <c>Scan</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The reference for each operator is stated rather than assumed. <c>Index</c> has a BCL
    /// counterpart from .NET 9 onwards, and this suite pins the engine's answer against the
    /// equivalent <c>System.Linq</c> expression instead of against a literal, so the shape and the
    /// values are both checked against the thing the engine claims to agree with. The other four
    /// come from MoreLINQ, whose definitions are restated in each test as the expected sequence.
    /// </para>
    /// <para>
    /// Everything here goes through the public entry points - <c>ToValueEnumerable()</c> followed by
    /// the operator - because that is the surface this work item exists to publish.
    /// </para>
    /// </remarks>
    public class FirstOperatorBatchTests
    {
        // ----- Index -----

        [Fact]
        public void Index_pairs_every_element_with_its_position_on_every_arm()
        {
            // The oracle is the System.Linq expression the BCL's own Index is documented to be
            // equivalent to. The test project targets net8.0, which predates Enumerable.Index, so
            // the expression is spelled out instead of calling the BCL operator - the engine's
            // Index is the polyfill on that target, not a wrapper around one.
            var expected = new[] { 10, 20, 30 }.Select((item, index) => (Index: index, Item: item)).ToList();

            IEnumerable<(int Index, int Item)> fromArray = new[] { 10, 20, 30 }.ToValueEnumerable().Index();
            IEnumerable<(int Index, int Item)> fromList = new List<int> { 10, 20, 30 }.ToValueEnumerable().Index();

            IReadOnlyList<int> readOnlySource = new List<int> { 10, 20, 30 };
            IEnumerable<(int Index, int Item)> fromReadOnly = readOnlySource.ToValueEnumerable().Index();

            IEnumerable<int> enumerationSource = new Queue<int>(new[] { 10, 20, 30 });
            IEnumerable<(int Index, int Item)> fromEnumerable = enumerationSource.ToValueEnumerable().Index();

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);
        }

        [Fact]
        public void Index_names_its_tuple_elements_the_way_the_BCL_does()
        {
            // Compile-time assertions: pair.Index and pair.Item only bind if the engine declares the
            // same element names the BCL uses, which is what lets a selector written against one be
            // reused against the other.
            var seen = new List<string>();
            foreach (var pair in new[] { 7, 8 }.ToValueEnumerable().Index())
            {
                seen.Add(pair.Index + ":" + pair.Item);
            }

            seen.ShouldBe(new[] { "0:7", "1:8" });
        }

        // ----- ForEach -----

        [Fact]
        public void ForEach_runs_the_action_on_every_element_of_every_arm()
        {
            var expected = new[] { 10, 20, 30 };

            var fromArray = new List<int>();
            new[] { 10, 20, 30 }.ToValueEnumerable().ForEach(fromArray.Add);

            var fromList = new List<int>();
            new List<int> { 10, 20, 30 }.ToValueEnumerable().ForEach(fromList.Add);

            IReadOnlyList<int> readOnlySource = new List<int> { 10, 20, 30 };
            var fromReadOnly = new List<int>();
            readOnlySource.ToValueEnumerable().ForEach(fromReadOnly.Add);

            IEnumerable<int> enumerationSource = new Queue<int>(new[] { 10, 20, 30 });
            var fromEnumerable = new List<int>();
            enumerationSource.ToValueEnumerable().ForEach(fromEnumerable.Add);

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);
        }

        [Fact]
        public void ForEach_passes_the_zero_based_position_on_every_arm()
        {
            var expected = new[] { "0:10", "1:20", "2:30" };

            var fromArray = new List<string>();
            new[] { 10, 20, 30 }.ToValueEnumerable().ForEach((value, index) => fromArray.Add(index + ":" + value));

            var fromList = new List<string>();
            new List<int> { 10, 20, 30 }.ToValueEnumerable().ForEach((value, index) => fromList.Add(index + ":" + value));

            IReadOnlyList<int> readOnlySource = new List<int> { 10, 20, 30 };
            var fromReadOnly = new List<string>();
            readOnlySource.ToValueEnumerable().ForEach((value, index) => fromReadOnly.Add(index + ":" + value));

            IEnumerable<int> enumerationSource = new Queue<int>(new[] { 10, 20, 30 });
            var fromEnumerable = new List<string>();
            enumerationSource.ToValueEnumerable().ForEach((value, index) => fromEnumerable.Add(index + ":" + value));

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);
        }

        [Fact]
        public void ForEach_finds_every_element_in_a_long_sequence()
        {
            // The position is handed to a caller's action, so the operator itself only has to walk;
            // this pins that the walk covers the whole sequence and not just its first page.
            var probe = new CountingEnumerable(10);
            IEnumerable<int> source = probe;

            var count = 0;
            source.ToValueEnumerable().ForEach((value, index) => count += index + 1);

            count.ShouldBe(55);
            probe.EnumeratorRequests.ShouldBe(1);
        }

        // ----- TagFirstLast -----

        [Fact]
        public void TagFirstLast_tags_the_ends_of_every_arm()
        {
            var expected = new[] { (1, true, false), (2, false, false), (3, false, true) };

            IEnumerable<(int Value, bool IsFirst, bool IsLast)> fromArray =
                new[] { 1, 2, 3 }.ToValueEnumerable().TagFirstLast((value, first, last) => (value, first, last));
            IEnumerable<(int Value, bool IsFirst, bool IsLast)> fromList =
                new List<int> { 1, 2, 3 }.ToValueEnumerable().TagFirstLast((value, first, last) => (value, first, last));

            IReadOnlyList<int> readOnlySource = new List<int> { 1, 2, 3 };
            IEnumerable<(int Value, bool IsFirst, bool IsLast)> fromReadOnly =
                readOnlySource.ToValueEnumerable().TagFirstLast((value, first, last) => (value, first, last));

            IEnumerable<int> enumerationSource = new Queue<int>(new[] { 1, 2, 3 });
            IEnumerable<(int Value, bool IsFirst, bool IsLast)> fromEnumerable =
                enumerationSource.ToValueEnumerable().TagFirstLast((value, first, last) => (value, first, last));

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);
        }

        [Fact]
        public void TagFirstLast_reports_a_single_element_as_both_first_and_last()
        {
            var tagged = new[] { 7 }.ToValueEnumerable()
                .TagFirstLast((value, first, last) => (value, first, last))
                .ToList();

            tagged.ShouldBe(new[] { (7, true, true) });
        }

        [Fact]
        public void TagFirstLast_yields_nothing_for_an_empty_source()
        {
            var tagged = new int[0].ToValueEnumerable()
                .TagFirstLast((value, first, last) => (value, first, last))
                .ToList();

            tagged.ShouldBeEmpty();
        }

        [Fact]
        public void TagFirstLast_pulls_one_element_ahead_of_what_it_hands_out()
        {
            // The lookahead is the operator's defining structure: an element's "last" flag is only
            // known once the element after it has been pulled. A walk of n elements therefore asks
            // the source for an element n + 1 times, and the source is never rewound.
            var probe = new CountingEnumerable(3);
            IEnumerable<int> source = probe;

            var tagged = source.ToValueEnumerable()
                .TagFirstLast((value, first, last) => (value, first, last))
                .ToList();

            tagged.Count.ShouldBe(3);
            probe.EnumeratorRequests.ShouldBe(1);
            probe.MoveNextCalls.ShouldBe(4);
        }

        [Fact]
        public void TagFirstLast_survives_a_reset()
        {
            var enumerator = new[] { 1, 2, 3 }.ToValueEnumerable()
                .TagFirstLast((value, first, last) => value * 100 + (first ? 10 : 0) + (last ? 1 : 0))
                .GetEnumerator();

            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(110);
            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(200);

            enumerator.Reset();

            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(110);
        }

        // ----- Pairwise -----

        [Fact]
        public void Pairwise_pairs_each_element_with_its_predecessor_on_every_arm()
        {
            var expected = new[] { "ab", "bc", "cd" };

            IEnumerable<string> fromArray =
                new[] { "a", "b", "c", "d" }.ToValueEnumerable().Pairwise((previous, value) => previous + value);
            IEnumerable<string> fromList =
                new List<string> { "a", "b", "c", "d" }.ToValueEnumerable().Pairwise((previous, value) => previous + value);

            IReadOnlyList<string> readOnlySource = new List<string> { "a", "b", "c", "d" };
            IEnumerable<string> fromReadOnly =
                readOnlySource.ToValueEnumerable().Pairwise((previous, value) => previous + value);

            IEnumerable<string> enumerationSource = new Queue<string>(new[] { "a", "b", "c", "d" });
            IEnumerable<string> fromEnumerable =
                enumerationSource.ToValueEnumerable().Pairwise((previous, value) => previous + value);

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);
        }

        [Fact]
        public void Pairwise_yields_one_result_fewer_than_the_source_has_elements()
        {
            var source = new[] { 1, 2, 3, 4 }.ToValueEnumerable();

            source.Pairwise((previous, value) => previous + value).ToList().ShouldBe(new[] { 3, 5, 7 });
            source.Pairwise((previous, value) => previous + value).Count().ShouldBe(3);
        }

        [Fact]
        public void Pairwise_yields_nothing_when_there_is_no_pair_to_form()
        {
            new int[0].ToValueEnumerable().Pairwise((previous, value) => previous + value).ShouldBeEmpty();
            new[] { 1 }.ToValueEnumerable().Pairwise((previous, value) => previous + value).ShouldBeEmpty();
        }

        [Fact]
        public void Pairwise_reads_each_source_element_once()
        {
            // The element that is the "current" of one pair is the "previous" of the next. Reading
            // Current again to fill that role would double the reads; the operator reads it into a
            // local and serves both roles from there.
            var probe = new CountingEnumerable(4);
            IEnumerable<int> source = probe;

            source.ToValueEnumerable().Pairwise((previous, value) => value - previous).ToList().ShouldBe(new[] { 1, 1, 1 });

            probe.EnumeratorRequests.ShouldBe(1);
            probe.CurrentReads.ShouldBe(4);
        }

        [Fact]
        public void Pairwise_survives_a_reset()
        {
            var enumerator = new[] { 1, 2, 3 }.ToValueEnumerable()
                .Pairwise((previous, value) => previous + value)
                .GetEnumerator();

            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(3);
            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(5);

            enumerator.Reset();

            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(3);
        }

        // ----- Scan -----

        [Fact]
        public void Scan_yields_the_running_aggregate_on_every_arm()
        {
            var expected = new[] { 1, 3, 6, 10 };

            IEnumerable<int> fromArray = new[] { 1, 2, 3, 4 }.ToValueEnumerable().Scan((accumulator, value) => accumulator + value);
            IEnumerable<int> fromList = new List<int> { 1, 2, 3, 4 }.ToValueEnumerable().Scan((accumulator, value) => accumulator + value);

            IReadOnlyList<int> readOnlySource = new List<int> { 1, 2, 3, 4 };
            IEnumerable<int> fromReadOnly = readOnlySource.ToValueEnumerable().Scan((accumulator, value) => accumulator + value);

            IEnumerable<int> enumerationSource = new Queue<int>(new[] { 1, 2, 3, 4 });
            IEnumerable<int> fromEnumerable = enumerationSource.ToValueEnumerable().Scan((accumulator, value) => accumulator + value);

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);
        }

        [Fact]
        public void Scan_seeds_the_aggregate_with_the_first_element()
        {
            // Inclusive scan: the first element is the first aggregate, and the transformation is
            // applied from the second element on. The transformation must therefore never see the
            // first element as its right-hand argument.
            var invocations = 0;
            var scanned = new[] { 1, 2, 3, 4 }.ToValueEnumerable()
                .Scan((accumulator, value) =>
                {
                    invocations++;
                    return accumulator + value;
                })
                .ToList();

            scanned.ShouldBe(new[] { 1, 3, 6, 10 });
            invocations.ShouldBe(3);
        }

        [Fact]
        public void Scan_yields_nothing_for_an_empty_source_and_the_element_for_a_single_one()
        {
            new int[0].ToValueEnumerable().Scan((accumulator, value) => accumulator + value).ShouldBeEmpty();
            new[] { 5 }.ToValueEnumerable().Scan((accumulator, value) => accumulator + value).ShouldBe(new[] { 5 });
        }

        [Fact]
        public void Scan_with_a_seed_yields_the_seed_first_on_every_arm()
        {
            var expected = new[] { 0, 1, 3, 6 };

            IEnumerable<int> fromArray = new[] { 1, 2, 3 }.ToValueEnumerable().Scan(0, (accumulator, value) => accumulator + value);
            IEnumerable<int> fromList = new List<int> { 1, 2, 3 }.ToValueEnumerable().Scan(0, (accumulator, value) => accumulator + value);

            IReadOnlyList<int> readOnlySource = new List<int> { 1, 2, 3 };
            IEnumerable<int> fromReadOnly = readOnlySource.ToValueEnumerable().Scan(0, (accumulator, value) => accumulator + value);

            IEnumerable<int> enumerationSource = new Queue<int>(new[] { 1, 2, 3 });
            IEnumerable<int> fromEnumerable = enumerationSource.ToValueEnumerable().Scan(0, (accumulator, value) => accumulator + value);

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);
        }

        [Fact]
        public void Scan_with_a_seed_yields_the_seed_even_for_an_empty_source()
        {
            var scanned = new int[0].ToValueEnumerable().Scan(42, (accumulator, value) => accumulator + value).ToList();

            scanned.ShouldBe(new[] { 42 });
        }

        [Fact]
        public void Scan_with_a_seed_may_change_the_element_type()
        {
            var scanned = new[] { 1, 2, 3 }.ToValueEnumerable()
                .Scan(string.Empty, (accumulator, value) => accumulator + value)
                .ToList();

            scanned.ShouldBe(new[] { "", "1", "12", "123" });
        }

        [Fact]
        public void Scan_survives_a_reset()
        {
            var enumerator = new[] { 1, 2, 3 }.ToValueEnumerable()
                .Scan((accumulator, value) => accumulator + value)
                .GetEnumerator();

            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(1);
            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(3);

            enumerator.Reset();

            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(1);
        }

        // ----- cross-cutting contracts -----

        [Fact]
        public void The_batch_is_lazy_and_touches_the_source_only_when_it_is_walked()
        {
            var probe = new CountingEnumerable(4);
            IEnumerable<int> source = probe;

            var indexed = source.ToValueEnumerable().Index();
            var tagged = source.ToValueEnumerable().TagFirstLast((value, first, last) => value);
            var paired = source.ToValueEnumerable().Pairwise((previous, value) => value - previous);
            var scanned = source.ToValueEnumerable().Scan((accumulator, value) => accumulator + value);
            var seeded = source.ToValueEnumerable().Scan(0, (accumulator, value) => accumulator + value);

            probe.EnumeratorRequests.ShouldBe(0);
            probe.MoveNextCalls.ShouldBe(0);
            probe.CurrentReads.ShouldBe(0);

            // Creating an enumerator for one of them asks the source for its own enumerator - that is
            // where the fallback arm's one unavoidable allocation lives - but reads nothing.
            var cursor = indexed.GetEnumerator();
            probe.EnumeratorRequests.ShouldBe(1);
            probe.MoveNextCalls.ShouldBe(0);
            probe.CurrentReads.ShouldBe(0);
            cursor.Dispose();
        }

        [Fact]
        public void The_batch_declines_the_three_hooks()
        {
            var source = new[] { 3, 1, 2 }.ToValueEnumerable();

            IValueEnumerableHooks<(int Index, int Item)> indexed = source.Index();
            IValueEnumerableHooks<int> tagged = source.TagFirstLast((value, first, last) => value);
            IValueEnumerableHooks<int> paired = source.Pairwise((previous, value) => previous + value);
            IValueEnumerableHooks<int> scanned = source.Scan((accumulator, value) => accumulator + value);
            IValueEnumerableHooks<string> seeded = source.Scan(string.Empty, (accumulator, value) => accumulator + value);

            indexed.TryGetNonEnumeratedCount(out _).ShouldBeFalse();
            tagged.TryGetNonEnumeratedCount(out _).ShouldBeFalse();
            paired.TryGetNonEnumeratedCount(out _).ShouldBeFalse();
            scanned.TryGetNonEnumeratedCount(out _).ShouldBeFalse();
            seeded.TryGetNonEnumeratedCount(out _).ShouldBeFalse();

#if NETCOREAPP3_0_OR_GREATER
            indexed.TryGetSpan(out _).ShouldBeFalse();
            tagged.TryGetSpan(out _).ShouldBeFalse();
            paired.TryGetSpan(out _).ShouldBeFalse();
            scanned.TryGetSpan(out _).ShouldBeFalse();
            seeded.TryGetSpan(out _).ShouldBeFalse();
#endif
        }

        [Fact]
        public void The_batch_enumerators_are_plain_value_types()
        {
            // The shape borrowed from the rest of the engine: a plain struct enumerator, never a
            // ref struct, so it can be a type argument and can be captured by an async state machine
            // on every target the package ships.
            typeof(IndexValueEnumerator<ArrayValueEnumerator<int>, int>).IsValueType.ShouldBeTrue();
            typeof(IndexValueEnumerator<ArrayValueEnumerator<int>, int>).IsByRefLike.ShouldBeFalse();

            typeof(TagFirstLastValueEnumerator<ArrayValueEnumerator<int>, int, int>).IsValueType.ShouldBeTrue();
            typeof(TagFirstLastValueEnumerator<ArrayValueEnumerator<int>, int, int>).IsByRefLike.ShouldBeFalse();

            typeof(PairwiseValueEnumerator<ArrayValueEnumerator<int>, int, int>).IsValueType.ShouldBeTrue();
            typeof(PairwiseValueEnumerator<ArrayValueEnumerator<int>, int, int>).IsByRefLike.ShouldBeFalse();

            typeof(ScanValueEnumerator<ArrayValueEnumerator<int>, int>).IsValueType.ShouldBeTrue();
            typeof(ScanValueEnumerator<ArrayValueEnumerator<int>, int>).IsByRefLike.ShouldBeFalse();

            typeof(SeededScanValueEnumerator<ArrayValueEnumerator<int>, int, string>).IsValueType.ShouldBeTrue();
            typeof(SeededScanValueEnumerator<ArrayValueEnumerator<int>, int, string>).IsByRefLike.ShouldBeFalse();
        }

        [Fact]
        public void Null_delegates_are_rejected_by_every_operator_that_takes_one()
        {
            var source = new[] { 1, 2, 3 }.ToValueEnumerable();

            Should.Throw<ArgumentNullException>(() => source.ForEach((Action<int>)null));
            Should.Throw<ArgumentNullException>(() => source.ForEach((Action<int, int>)null));
            Should.Throw<ArgumentNullException>(() => source.TagFirstLast<int, int>(null));
            Should.Throw<ArgumentNullException>(() => source.Pairwise<int, int>(null));
            Should.Throw<ArgumentNullException>(() => source.Scan(null));
            Should.Throw<ArgumentNullException>(() => source.Scan(0, null));
        }

        [Fact]
        public void The_batch_allocates_nothing_on_the_hot_path()
        {
            const int Count = 64;
            var source = new int[Count];
            for (var index = 0; index < source.Length; index++)
            {
                source[index] = index;
            }

            // Warm up, so the measured pass sees fully JITed code. Every expectation is derived by an
            // independent loop rather than by hand, because the value a streaming operator sums to is
            // not the value its source sums to: a scan sums to the sum of its prefix sums, and a
            // pairwise pass sums each interior element twice.
            SumIndexed(source).ShouldBe(SumOfIndexPlusValue(Count));
            SumTagged(source, TagSelector).ShouldBe(SumOfValue(Count));
            SumPaired(source, PairSelector).ShouldBe(SumOfAdjacentPairSums(Count));
            SumScanned(source, Folder).ShouldBe(SumOfPrefixSums(Count));
            SumSeededScanned(source, 0, Folder).ShouldBe(SumOfPrefixSums(Count));
            ForEachSum(source, AccumulateAction).ShouldBe(SumOfValue(Count));
            ForEachIndexedSum(source, AccumulateIndexedAction).ShouldBe(SumOfIndexPlusValue(Count));

            var before = GC.GetAllocatedBytesForCurrentThread();
            var indexed = SumIndexed(source);
            var tagged = SumTagged(source, TagSelector);
            var paired = SumPaired(source, PairSelector);
            var scanned = SumScanned(source, Folder);
            var seeded = SumSeededScanned(source, 0, Folder);
            var enumerated = ForEachSum(source, AccumulateAction);
            var enumeratedWithIndex = ForEachIndexedSum(source, AccumulateIndexedAction);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            allocated.ShouldBe(0);

            // The results are asserted so that nothing above can be read as dead code.
            indexed.ShouldBe(SumOfIndexPlusValue(Count));
            tagged.ShouldBe(SumOfValue(Count));
            paired.ShouldBe(SumOfAdjacentPairSums(Count));
            scanned.ShouldBe(SumOfPrefixSums(Count));
            seeded.ShouldBe(SumOfPrefixSums(Count));
            enumerated.ShouldBe(SumOfValue(Count));
            enumeratedWithIndex.ShouldBe(SumOfIndexPlusValue(Count));
        }

        // ----- allocation fixtures -----

        private static readonly Func<int, bool, bool, int> TagSelector = (value, first, last) => value;

        private static readonly Func<int, int, int> PairSelector = (previous, value) => previous + value;

        private static readonly Func<int, int, int> Folder = (accumulator, value) => accumulator + value;

        private static readonly Action<int> AccumulateAction = Accumulate;

        private static readonly Action<int, int> AccumulateIndexedAction = AccumulateIndexed;

        private static int _sink;

        private static void Accumulate(int value) => _sink += value;

        private static void AccumulateIndexed(int value, int index) => _sink += value + index;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int SumIndexed(int[] source)
        {
            var total = 0;
            foreach (var pair in source.ToValueEnumerable().Index())
            {
                total += pair.Index + pair.Item;
            }

            return total;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int SumTagged(int[] source, Func<int, bool, bool, int> resultSelector)
        {
            var total = 0;
            foreach (var value in source.ToValueEnumerable().TagFirstLast(resultSelector))
            {
                total += value;
            }

            return total;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int SumPaired(int[] source, Func<int, int, int> resultSelector)
        {
            var total = 0;
            foreach (var value in source.ToValueEnumerable().Pairwise(resultSelector))
            {
                total += value;
            }

            return total;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int SumScanned(int[] source, Func<int, int, int> transformation)
        {
            var total = 0;
            foreach (var value in source.ToValueEnumerable().Scan(transformation))
            {
                total += value;
            }

            return total;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int SumSeededScanned(int[] source, int seed, Func<int, int, int> transformation)
        {
            var total = 0;
            foreach (var value in source.ToValueEnumerable().Scan(seed, transformation))
            {
                total += value;
            }

            return total;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ForEachSum(int[] source, Action<int> action)
        {
            _sink = 0;
            source.ToValueEnumerable().ForEach(action);
            return _sink;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ForEachIndexedSum(int[] source, Action<int, int> action)
        {
            _sink = 0;
            source.ToValueEnumerable().ForEach(action);
            return _sink;
        }

        private static int SumOfValue(int count)
        {
            var total = 0;
            for (var index = 0; index < count; index++)
            {
                total += index;
            }

            return total;
        }

        private static int SumOfIndexPlusValue(int count)
        {
            var total = 0;
            for (var index = 0; index < count; index++)
            {
                total += index + index;
            }

            return total;
        }

        /// <summary>The total of a pairwise pass over <c>0 .. count - 1</c>: every element but the
        /// ends takes part in two pairs.</summary>
        private static int SumOfAdjacentPairSums(int count)
        {
            var total = 0;
            for (var index = 1; index < count; index++)
            {
                total += (index - 1) + index;
            }

            return total;
        }

        /// <summary>The total of a seedless scan over <c>0 .. count - 1</c>: a scan yields a result per
        /// element, so summing its results sums the prefix sums.</summary>
        private static int SumOfPrefixSums(int count)
        {
            var total = 0;
            var running = 0;
            for (var index = 0; index < count; index++)
            {
                running += index;
                total += running;
            }

            return total;
        }
    }
}
