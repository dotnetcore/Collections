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
    /// F7-06 (fourth batch): the keyed-aggregation operators of the P0 polyfill layer -
    /// <c>CountBy</c> and <c>AggregateBy</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These two are BCL operators from .NET 9 onwards, and this suite runs on net8.0, where the
    /// BCL's own implementation does not exist. That makes the oracle a hand-written fold rather
    /// than the platform: the expected sequence is built with <c>GroupBy</c> plus <c>Count</c> or
    /// <c>Aggregate</c>, which is exactly the "group and then aggregate" shape the operator is
    /// meant to replace. <c>GroupBy</c> yields its groups in first-seen order, which is the order a
    /// <see cref="Dictionary{TKey,TValue}"/> enumerates, so the comparison covers the order too.
    /// </para>
    /// <para>
    /// On net8.0 the engine takes its own implementation on every arm, so this suite exercises the
    /// polyfill proper - the code a net451 consumer compiles against - rather than the pass-through
    /// to the platform.
    /// </para>
    /// </remarks>
    public class PolyfillKeyedAggregationOperatorTests
    {
        private static readonly int[] Numbers = { 1, 2, 3, 4, 5, 6 };

        private static int KeyOf(int value) => value % 3;

        // ----- CountBy -----

        [Fact]
        public void CountBy_counts_every_key_on_every_arm()
        {
            var expected = Numbers
                .GroupBy(KeyOf)
                .Select(group => new KeyValuePair<int, int>(group.Key, group.Count()))
                .ToList();

            IEnumerable<KeyValuePair<int, int>> fromArray = Numbers.ToValueEnumerable().CountBy(KeyOf);
            IEnumerable<KeyValuePair<int, int>> fromList = new List<int>(Numbers).ToValueEnumerable().CountBy(KeyOf);

            IReadOnlyList<int> readOnlySource = new List<int>(Numbers);
            IEnumerable<KeyValuePair<int, int>> fromReadOnly = readOnlySource.ToValueEnumerable().CountBy(KeyOf);

            IEnumerable<int> enumerationSource = new Queue<int>(Numbers);
            IEnumerable<KeyValuePair<int, int>> fromEnumerable = enumerationSource.ToValueEnumerable().CountBy(KeyOf);

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);

            expected.ShouldBe(new[]
            {
                new KeyValuePair<int, int>(1, 2),
                new KeyValuePair<int, int>(2, 2),
                new KeyValuePair<int, int>(0, 2)
            });
        }

        [Fact]
        public void CountBy_keys_by_a_projected_key_rather_than_the_element()
        {
            var source = new[] { "a", "bb", "cc", "d", "eee" };

            var expected = source
                .GroupBy(text => text.Length)
                .Select(group => new KeyValuePair<int, int>(group.Key, group.Count()))
                .ToList();

            var actual = source.ToValueEnumerable().CountBy(text => text.Length).ToList();

            actual.ShouldBe(expected);
            actual.ShouldBe(new[]
            {
                new KeyValuePair<int, int>(1, 2),
                new KeyValuePair<int, int>(2, 2),
                new KeyValuePair<int, int>(3, 1)
            });
        }

        [Fact]
        public void CountBy_honours_a_custom_key_comparer()
        {
            var source = new[] { "a", "A", "b", "B", "c" };

            var expected = source
                .GroupBy(text => text, StringComparer.OrdinalIgnoreCase)
                .Select(group => new KeyValuePair<string, int>(group.Key, group.Count()))
                .ToList();

            var actual = source.ToValueEnumerable()
                .CountBy(text => text, StringComparer.OrdinalIgnoreCase)
                .ToList();

            actual.ShouldBe(expected);
            actual.ShouldBe(new[]
            {
                new KeyValuePair<string, int>("a", 2),
                new KeyValuePair<string, int>("b", 2),
                new KeyValuePair<string, int>("c", 1)
            });
        }

        [Fact]
        public void CountBy_yields_nothing_for_an_empty_source()
        {
            new int[0].ToValueEnumerable().CountBy(KeyOf).ShouldBeEmpty();
        }

        [Fact]
        public void CountBy_is_deferred_and_drains_the_source_on_the_first_pull()
        {
            var probe = new CountingEnumerable(6);
            IEnumerable<int> source = probe;

            var counted = source.ToValueEnumerable().CountBy(value => value % 3);

            // Nothing is read when the pipeline is built.
            probe.EnumeratorRequests.ShouldBe(0);
            probe.MoveNextCalls.ShouldBe(0);

            // A count cannot be known before the source has been seen to its end, so the first
            // pull drains it; the walk is a single pass, never a rewind.
            counted.Count().ShouldBe(3);
            probe.EnumeratorRequests.ShouldBe(1);
            probe.MoveNextCalls.ShouldBe(7);
        }

        [Fact]
        public void CountBy_survives_a_reset()
        {
            var enumerator = Numbers.ToValueEnumerable().CountBy(KeyOf).GetEnumerator();

            enumerator.MoveNext().ShouldBeTrue();
            var first = enumerator.Current;
            first.ShouldBe(new KeyValuePair<int, int>(1, 2));

            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(new KeyValuePair<int, int>(2, 2));

            enumerator.Reset();

            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(first);
        }

        // ----- AggregateBy (shared seed) -----

        [Fact]
        public void AggregateBy_folds_every_key_on_every_arm()
        {
            var expected = Numbers
                .GroupBy(KeyOf)
                .Select(group => new KeyValuePair<int, int>(group.Key, group.Aggregate(0, (sum, value) => sum + value)))
                .ToList();

            IEnumerable<KeyValuePair<int, int>> fromArray = Numbers.ToValueEnumerable()
                .AggregateBy(KeyOf, 0, (sum, value) => sum + value);
            IEnumerable<KeyValuePair<int, int>> fromList = new List<int>(Numbers).ToValueEnumerable()
                .AggregateBy(KeyOf, 0, (sum, value) => sum + value);

            IReadOnlyList<int> readOnlySource = new List<int>(Numbers);
            IEnumerable<KeyValuePair<int, int>> fromReadOnly = readOnlySource.ToValueEnumerable()
                .AggregateBy(KeyOf, 0, (sum, value) => sum + value);

            IEnumerable<int> enumerationSource = new Queue<int>(Numbers);
            IEnumerable<KeyValuePair<int, int>> fromEnumerable = enumerationSource.ToValueEnumerable()
                .AggregateBy(KeyOf, 0, (sum, value) => sum + value);

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);

            expected.ShouldBe(new[]
            {
                new KeyValuePair<int, int>(1, 5),
                new KeyValuePair<int, int>(2, 7),
                new KeyValuePair<int, int>(0, 9)
            });
        }

        [Fact]
        public void AggregateBy_with_a_shared_seed_hands_the_same_accumulator_to_every_key()
        {
            // The seed overload is the seed factory that ignores its key, so a mutable seed is one
            // object shared by every key. That is the BCL's contract, and it is worth pinning down:
            // a caller who means "one fresh accumulator per key" has to use the factory overload.
            var shared = new List<int>();

            var grouped = Numbers.ToValueEnumerable()
                .AggregateBy(KeyOf, shared, (list, value) =>
                {
                    list.Add(value);
                    return list;
                })
                .ToList();

            grouped.Count.ShouldBe(3);

            // Every key reports the one list, and that list saw all six elements.
            grouped.Select(pair => pair.Value).Distinct().Count().ShouldBe(1);
            shared.Count.ShouldBe(6);
        }

        // ----- AggregateBy (seed factory) -----

        [Fact]
        public void AggregateBy_with_a_seed_factory_gives_every_key_its_own_start()
        {
            var grouped = Numbers.ToValueEnumerable()
                .AggregateBy(KeyOf, _ => new List<int>(), (list, value) =>
                {
                    list.Add(value);
                    return list;
                })
                .ToList();

            grouped.Count.ShouldBe(3);

            // A fresh list per key: three keys, three distinct instances.
            grouped.Select(pair => pair.Value).Distinct().Count().ShouldBe(3);

            grouped[0].Key.ShouldBe(1);
            grouped[0].Value.ShouldBe(new[] { 1, 4 });
            grouped[1].Key.ShouldBe(2);
            grouped[1].Value.ShouldBe(new[] { 2, 5 });
            grouped[2].Key.ShouldBe(0);
            grouped[2].Value.ShouldBe(new[] { 3, 6 });
        }

        [Fact]
        public void AggregateBy_with_a_seed_factory_agrees_with_the_shared_seed_for_an_immutable_accumulator()
        {
            var source = new[] { "a", "bb", "cc", "d" };

            var fromSeed = source.ToValueEnumerable()
                .AggregateBy(text => text.Length, 0, (total, text) => total + text.Length)
                .ToList();

            var fromFactory = source.ToValueEnumerable()
                .AggregateBy(text => text.Length, _ => 0, (total, text) => total + text.Length)
                .ToList();

            fromFactory.ShouldBe(fromSeed);
            fromFactory.ShouldBe(new[]
            {
                new KeyValuePair<int, int>(1, 2),
                new KeyValuePair<int, int>(2, 4)
            });
        }

        [Fact]
        public void AggregateBy_honours_a_custom_key_comparer()
        {
            var source = new[] { "a", "A", "bb" };

            var expected = source
                .GroupBy(text => text, StringComparer.OrdinalIgnoreCase)
                .Select(group => new KeyValuePair<string, string>(group.Key, group.Aggregate(string.Empty, (joined, text) => joined + text)))
                .ToList();

            var actual = source.ToValueEnumerable()
                .AggregateBy(text => text, string.Empty, (joined, text) => joined + text, StringComparer.OrdinalIgnoreCase)
                .ToList();

            actual.ShouldBe(expected);
        }

        [Fact]
        public void AggregateBy_yields_nothing_for_an_empty_source()
        {
            new int[0].ToValueEnumerable()
                .AggregateBy(KeyOf, 0, (sum, value) => sum + value)
                .ShouldBeEmpty();
        }

        [Fact]
        public void AggregateBy_is_deferred_and_drains_the_source_on_the_first_pull()
        {
            var probe = new CountingEnumerable(6);
            IEnumerable<int> source = probe;

            var aggregated = source.ToValueEnumerable().AggregateBy(value => value % 3, 0, (sum, value) => sum + value);

            probe.EnumeratorRequests.ShouldBe(0);
            probe.MoveNextCalls.ShouldBe(0);

            aggregated.Count().ShouldBe(3);
            probe.EnumeratorRequests.ShouldBe(1);
            probe.MoveNextCalls.ShouldBe(7);
        }

        [Fact]
        public void AggregateBy_survives_a_reset()
        {
            var enumerator = Numbers.ToValueEnumerable()
                .AggregateBy(KeyOf, 0, (sum, value) => sum + value)
                .GetEnumerator();

            enumerator.MoveNext().ShouldBeTrue();
            var first = enumerator.Current;
            first.ShouldBe(new KeyValuePair<int, int>(1, 5));

            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(new KeyValuePair<int, int>(2, 7));

            enumerator.Reset();

            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(first);
        }

        // ----- cross-cutting contracts -----

        [Fact]
        public void Null_arguments_are_rejected_by_every_operator()
        {
            var source = Numbers.ToValueEnumerable();

            Should.Throw<ArgumentNullException>(() => source.CountBy<int, int>(null));

            Should.Throw<ArgumentNullException>(() => source.AggregateBy<int, int, int>(null, 0, (sum, value) => sum + value));
            Should.Throw<ArgumentNullException>(() => source.AggregateBy<int, int, int>(KeyOf, 0, null));
            Should.Throw<ArgumentNullException>(() => source.AggregateBy<int, int, int>(null, _ => 0, (sum, value) => sum + value));
            Should.Throw<ArgumentNullException>(() => source.AggregateBy<int, int, int>(KeyOf, (Func<int, int>)null, (sum, value) => sum + value));
            Should.Throw<ArgumentNullException>(() => source.AggregateBy<int, int, int>(KeyOf, _ => 0, null));
        }

        [Fact]
        public void Null_arguments_are_rejected_on_the_arbitrary_sequence_arm_too()
        {
            IEnumerable<int> raw = new Queue<int>(Numbers);
            var source = raw.ToValueEnumerable();

            Should.Throw<ArgumentNullException>(() => source.CountBy<int, int>(null));

            Should.Throw<ArgumentNullException>(() => source.AggregateBy<int, int, int>(null, 0, (sum, value) => sum + value));
            Should.Throw<ArgumentNullException>(() => source.AggregateBy<int, int, int>(KeyOf, 0, null));
            Should.Throw<ArgumentNullException>(() => source.AggregateBy<int, int, int>(null, _ => 0, (sum, value) => sum + value));
            Should.Throw<ArgumentNullException>(() => source.AggregateBy<int, int, int>(KeyOf, (Func<int, int>)null, (sum, value) => sum + value));
            Should.Throw<ArgumentNullException>(() => source.AggregateBy<int, int, int>(KeyOf, _ => 0, null));
        }

        [Fact]
        public void The_layer_declines_the_three_hooks()
        {
            var source = Numbers.ToValueEnumerable();

            IValueEnumerableHooks<KeyValuePair<int, int>> counted = source.CountBy(KeyOf);
            IValueEnumerableHooks<KeyValuePair<int, int>> aggregated = source.AggregateBy(KeyOf, 0, (sum, value) => sum + value);

            counted.TryGetNonEnumeratedCount(out _).ShouldBeFalse();
            aggregated.TryGetNonEnumeratedCount(out _).ShouldBeFalse();

#if NETCOREAPP3_0_OR_GREATER
            counted.TryGetSpan(out _).ShouldBeFalse();
            aggregated.TryGetSpan(out _).ShouldBeFalse();
#endif
        }

        [Fact]
        public void The_layer_enumerators_are_plain_value_types()
        {
            typeof(CountByValueEnumerator<ArrayValueEnumerator<int>, int, int>).IsValueType.ShouldBeTrue();
            typeof(CountByValueEnumerator<ArrayValueEnumerator<int>, int, int>).IsByRefLike.ShouldBeFalse();

            typeof(AggregateByValueEnumerator<ArrayValueEnumerator<int>, int, int, int>).IsValueType.ShouldBeTrue();
            typeof(AggregateByValueEnumerator<ArrayValueEnumerator<int>, int, int, int>).IsByRefLike.ShouldBeFalse();
        }
    }
}
