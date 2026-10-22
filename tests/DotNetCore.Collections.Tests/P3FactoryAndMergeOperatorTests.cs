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
    /// F7-14 (second half): the sequence factories, the repetition operator and the two-way merges -
    /// <c>Return</c>, <c>Throw</c>, <c>Defer</c>, <c>Using</c>, <c>Repeat</c>, <c>OrderedMerge</c>
    /// and <c>SortedMerge</c>.
    /// </summary>
    /// <remarks>
    /// The two things these tests fence off are laziness and stability. A factory that runs when it
    /// is called instead of when it is enumerated is useless, and a merge that buffers an unbounded
    /// stream or reorders equal keys is worse than the sort it is meant to replace. Both are
    /// asserted from the outside: the factories are observed through their call counts,
    /// <c>OrderedMerge</c> through an endless source, and the sorts through a tagged element whose
    /// key is equal for every element it carries.
    /// </remarks>
    public class P3FactoryAndMergeOperatorTests
    {
        // ----- Return -----

        [Fact]
        public void Return_yields_exactly_one_element_and_can_be_walked_repeatedly()
        {
            var sequence = CollectionExtensions.Return(42);

            sequence.ToArray().ShouldBe(new[] { 42 });
            sequence.ToArray().ShouldBe(new[] { 42 });
        }

        [Fact]
        public void Return_answers_the_count_and_copy_hooks()
        {
            IValueEnumerableHooks<int> hooks = CollectionExtensions.Return(42);

            hooks.TryGetNonEnumeratedCount(out var count).ShouldBeTrue();
            count.ShouldBe(1);

            var destination = new int[3];
            hooks.TryCopyTo(destination, 1).ShouldBeTrue();
            destination.ShouldBe(new[] { 0, 42, 0 });

            // No room for the one element: the hook declines instead of writing half of it.
            hooks.TryCopyTo(new int[1], 1).ShouldBeFalse();
        }

        // ----- Throw -----

        [Fact]
        public void Throw_defers_the_failure_until_the_sequence_is_walked()
        {
            var failure = new InvalidOperationException("boom");

            // Building the sequence must not throw: the whole point is to hand it to a handler.
            var sequence = CollectionExtensions.Throw<int>(failure);

            var raised = Should.Throw<InvalidOperationException>(() => sequence.ToArray());

            raised.ShouldBeSameAs(failure);
        }

        [Fact]
        public void Throw_rejects_a_null_exception()
        {
            Should.Throw<ArgumentNullException>(() => CollectionExtensions.Throw<int>(null!));
        }

        // ----- Defer -----

        [Fact]
        public void Defer_runs_the_factory_once_per_enumeration()
        {
            var calls = 0;

            var deferred = CollectionExtensions.Defer(() =>
            {
                calls++;
                return new[] { 1, 2 };
            });

            calls.ShouldBe(0);

            deferred.ToArray().ShouldBe(new[] { 1, 2 });
            deferred.ToArray().ShouldBe(new[] { 1, 2 });

            calls.ShouldBe(2);
        }

        [Fact]
        public void Defer_reports_a_factory_that_answers_null()
        {
            var deferred = CollectionExtensions.Defer<int>(() => null);

            Should.Throw<InvalidOperationException>(() => deferred.ToArray());
        }

        // ----- Using -----

        [Fact]
        public void Using_releases_the_resource_after_a_full_walk()
        {
            var resource = new TrackedResource();

            var result = CollectionExtensions.Using(() => resource, r => new[] { 1, 2, r.Id }).ToArray();

            result.ShouldBe(new[] { 1, 2, 7 });
            resource.Disposals.ShouldBe(1);
        }

        [Fact]
        public void Using_releases_the_resource_when_the_walk_stops_early()
        {
            var resource = new TrackedResource();

            var enumerator = CollectionExtensions.Using(() => resource, r => new[] { 1, 2, 3 }).GetEnumerator();
            enumerator.MoveNext().ShouldBeTrue();

            // A break out of a foreach disposes the enumerator; that has to release the resource.
            enumerator.Dispose();

            resource.Disposals.ShouldBe(1);
        }

        [Fact]
        public void Using_releases_the_resource_when_the_sequence_factory_fails()
        {
            var resource = new TrackedResource();

            Should.Throw<InvalidOperationException>(() => CollectionExtensions
                .Using<int, TrackedResource>(() => resource, _ => throw new InvalidOperationException("no sequence"))
                .ToArray());

            resource.Disposals.ShouldBe(1);
        }

        [Fact]
        public void Using_creates_the_resource_once_per_enumeration()
        {
            var created = 0;

            var sequence = CollectionExtensions.Using(
                () =>
                {
                    created++;
                    return new TrackedResource();
                },
                _ => new[] { 1 });

            sequence.ToArray().ShouldBe(new[] { 1 });
            sequence.ToArray().ShouldBe(new[] { 1 });

            created.ShouldBe(2);
        }

        // ----- Repeat -----

        [Fact]
        public void Repeat_yields_the_whole_sequence_the_requested_number_of_times()
        {
            new[] { 1, 2 }.ToValueEnumerable().Repeat(3).ToArray().ShouldBe(new[] { 1, 2, 1, 2, 1, 2 });
            new[] { 1, 2 }.ToValueEnumerable().Repeat(1).ToArray().ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public void Repeat_with_zero_or_an_empty_source_yields_nothing()
        {
            new[] { 1, 2 }.ToValueEnumerable().Repeat(0).ShouldBeEmpty();
            new int[0].ToValueEnumerable().Repeat(5).ShouldBeEmpty();
        }

        [Fact]
        public void Repeat_rejects_a_negative_count()
        {
            Should.Throw<ArgumentOutOfRangeException>(() => new[] { 1 }.ToValueEnumerable().Repeat(-1));
        }

        [Fact]
        public void Repeat_asks_the_source_for_a_fresh_enumerator_per_round()
        {
            var source = new ProbeEnumerable();

            var result = source.ToValueEnumerable().Repeat(3).ToArray();

            result.ShouldBe(new[] { 1, 2, 3, 1, 2, 3, 1, 2, 3 });
            source.EnumeratorRequests.ShouldBe(3);
        }

        // ----- OrderedMerge -----

        [Fact]
        public void OrderedMerge_interleaves_two_ordered_sequences()
        {
            var result = new[] { 1, 3, 5 }
                .ToValueEnumerable()
                .OrderedMerge(new[] { 2, 4, 6 })
                .ToArray();

            result.ShouldBe(new[] { 1, 2, 3, 4, 5, 6 });
        }

        [Fact]
        public void OrderedMerge_takes_ties_from_the_first_side()
        {
            var first = new[] { new Tagged(1, "a1"), new Tagged(2, "a2") };
            var second = new[] { new Tagged(2, "b2"), new Tagged(3, "b3") };

            var result = first
                .ToValueEnumerable()
                .OrderedMerge(second, element => element.Key)
                .Select(element => element.Tag)
                .ToArray();

            result.ShouldBe(new[] { "a1", "a2", "b2", "b3" });
        }

        [Fact]
        public void OrderedMerge_honours_an_explicit_comparer()
        {
            var descending = Comparer<int>.Create((x, y) => y.CompareTo(x));

            var result = new[] { 5, 3, 1 }
                .ToValueEnumerable()
                .OrderedMerge(new[] { 6, 4, 2 }, descending)
                .ToArray();

            result.ShouldBe(new[] { 6, 5, 4, 3, 2, 1 });
        }

        [Fact]
        public void OrderedMerge_copes_with_an_empty_side()
        {
            new[] { 1, 2 }.ToValueEnumerable().OrderedMerge(new int[0]).ToArray().ShouldBe(new[] { 1, 2 });
            new int[0].ToValueEnumerable().OrderedMerge(new[] { 1, 2 }).ToArray().ShouldBe(new[] { 1, 2 });
            new int[0].ToValueEnumerable().OrderedMerge(new int[0]).ShouldBeEmpty();
        }

        [Fact]
        public void OrderedMerge_streams_an_endless_source()
        {
            IEnumerable<int> Naturals()
            {
                var value = 0;
                while (true)
                {
                    yield return value++;
                }
            }

            // Buffering either side would hang here; a streaming merge answers immediately.
            var result = Naturals()
                .ToValueEnumerable()
                .OrderedMerge(new[] { 10, 20 })
                .Take(4)
                .ToArray();

            result.ShouldBe(new[] { 0, 1, 2, 3 });
        }

        // ----- SortedMerge -----

        [Fact]
        public void SortedMerge_sorts_both_sides_before_merging()
        {
            var result = new[] { 3, 1 }
                .ToValueEnumerable()
                .SortedMerge(new[] { 4, 2 })
                .ToArray();

            result.ShouldBe(new[] { 1, 2, 3, 4 });
        }

        [Fact]
        public void SortedMerge_keeps_duplicates()
        {
            var result = new[] { 2, 2 }
                .ToValueEnumerable()
                .SortedMerge(new[] { 2, 1 })
                .ToArray();

            result.ShouldBe(new[] { 1, 2, 2, 2 });
        }

        [Fact]
        public void SortedMerge_is_stable_within_each_side()
        {
            // Every element carries the same key, so the only thing that can decide the order is the
            // sort's stability: the first side's elements keep their input order, and so do the
            // second side's.
            var first = new[] { new Tagged(1, "a2"), new Tagged(1, "a1") };
            var second = new[] { new Tagged(1, "b2"), new Tagged(1, "b1") };
            var byKey = Comparer<Tagged>.Create((x, y) => x.Key.CompareTo(y.Key));

            var result = first
                .ToValueEnumerable()
                .SortedMerge(second, byKey)
                .Select(element => element.Tag)
                .ToArray();

            result.ShouldBe(new[] { "a2", "a1", "b2", "b1" });
        }

        [Fact]
        public void SortedMerge_honours_an_explicit_comparer()
        {
            var descending = Comparer<int>.Create((x, y) => y.CompareTo(x));

            var result = new[] { 1, 3 }
                .ToValueEnumerable()
                .SortedMerge(new[] { 2, 4 }, descending)
                .ToArray();

            result.ShouldBe(new[] { 4, 3, 2, 1 });
        }

        [Fact]
        public void SortedMerge_copes_with_an_empty_side()
        {
            new[] { 2, 1 }.ToValueEnumerable().SortedMerge(new int[0]).ToArray().ShouldBe(new[] { 1, 2 });
            new int[0].ToValueEnumerable().SortedMerge(new[] { 2, 1 }).ToArray().ShouldBe(new[] { 1, 2 });
            new int[0].ToValueEnumerable().SortedMerge(new int[0]).ShouldBeEmpty();
        }
    }

    /// <summary>
    /// An element whose ordering key is decoupled from its identity, so a test can tell <em>which</em>
    /// of several equal-keyed elements came out first.
    /// </summary>
    internal readonly struct Tagged
    {
        internal Tagged(int key, string tag)
        {
            Key = key;
            Tag = tag;
        }

        internal int Key { get; }

        internal string Tag { get; }
    }

    /// <summary>
    /// A resource that counts how often it was released, so the lifetime rules of <c>Using</c> can be
    /// asserted instead of assumed.
    /// </summary>
    internal sealed class TrackedResource : IDisposable
    {
        internal int Disposals { get; private set; }

        internal int Id => 7;

        public void Dispose() => Disposals++;
    }
}
