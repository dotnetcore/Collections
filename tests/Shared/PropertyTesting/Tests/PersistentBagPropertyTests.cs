using System;
using System.Collections.Generic;
using DotNetCore.Collections.Multi;
using DotNetCore.Collections.Testing.PropertyTesting;
using DotNetCore.Collections.Testing.PropertyTesting.Adapters;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// The bag properties applied to <see cref="PersistentBag{T}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The same configurations <see cref="MultiList{T}"/> is measured in, so the mutable and the
    /// persistent multiset are held to one contract rather than to two interpretations of it: a
    /// random operation sequence has to agree with an independent model on copy counts, distinct
    /// counts, the inherited <c>Count</c>, containment and the bulk forms.
    /// </para>
    /// <para>
    /// Beyond that, these cases cover what only an immutable type can get wrong - a snapshot that
    /// changes under the caller, a removal that mutates the receiver, a builder that leaks back into
    /// the bag it started from.
    /// </para>
    /// </remarks>
    public class PersistentBagPropertyTests
    {
        private const int BaseSeed = 20261103;
        private const int SeedSweep = 20;

        private static readonly IReadOnlyList<int> IntDomain = new[] { 0, 1, 2 };

        [Fact]
        public void Model_equivalence_holds_for_distinct_elements()
        {
            var property = new BagModelEquivalenceProperty<PersistentBag<int>, int>(
                new PersistentBagAdapter<int>(),
                "PersistentBag<int>",
                IntDomain,
                null);

            var result = PropertyRunner.RunSeeds(property, BaseSeed, SeedSweep, PropertyRunner.DefaultStepCount);

            result.Success.ShouldBeTrue(result.ToDiagnosticString());
        }

        [Fact]
        public void Model_equivalence_holds_when_the_element_comparer_folds_case()
        {
            // "a" and "A" are one element, so they must share one entry and one multiplicity; a bag
            // that hashed differently from its comparer would keep two.
            var property = new BagModelEquivalenceProperty<PersistentBag<string>, string>(
                new PersistentBagAdapter<string>(),
                "PersistentBag<string>/case-insensitive",
                new[] { "a", "A", "b" },
                StringComparer.OrdinalIgnoreCase);

            var result = PropertyRunner.RunSeeds(property, BaseSeed + 1, SeedSweep, PropertyRunner.DefaultStepCount);

            result.Success.ShouldBeTrue(result.ToDiagnosticString());
        }

        [Fact]
        public void Model_equivalence_holds_for_a_single_element()
        {
            // One element forces every operation onto the counter path, where the multiplicity - not
            // the trie's slot space - is the thing under test.
            var property = new BagModelEquivalenceProperty<PersistentBag<int>, int>(
                new PersistentBagAdapter<int>(),
                "PersistentBag<int>/single-element",
                new[] { 7 },
                null);

            var result = PropertyRunner.RunSeeds(property, BaseSeed + 2, SeedSweep, PropertyRunner.DefaultStepCount);

            result.Success.ShouldBeTrue(result.ToDiagnosticString());
        }

        [Fact]
        public void Model_equivalence_holds_with_a_null_element()
        {
            // A null element lives outside the trie, so it takes a structurally different path from
            // every other element.
            var property = new BagModelEquivalenceProperty<PersistentBag<string>, string>(
                new PersistentBagAdapter<string>(),
                "PersistentBag<string>/null-element",
                new[] { "a", null, "b" },
                null);

            var result = PropertyRunner.RunSeeds(property, BaseSeed + 3, SeedSweep, PropertyRunner.DefaultStepCount);

            result.Success.ShouldBeTrue(result.ToDiagnosticString());
        }

        [Fact]
        public void An_earlier_snapshot_is_unaffected_by_later_writes()
        {
            // The whole reason for structural sharing: the previous bag must keep its own view while
            // the new one diverges. A payload or node mutated in place instead of copied would break
            // this even though every single-snapshot property still passed.
            var original = new PersistentBag<int>().Add(1, 3).Add(2);

            var extended = original.Add(1).Add(4).Remove(2, 1).Remove(1, 2);

            original.TotalCount.ShouldBe(4);
            original.CountOf(1).ShouldBe(3);
            original.CountOf(2).ShouldBe(1);
            original.Contains(4).ShouldBeFalse();

            extended.TotalCount.ShouldBe(3, "one copy of 1 was added, one of 4 arrived, one of 2 left and two of 1 left");
            extended.CountOf(1).ShouldBe(2);
            extended.CountOf(2).ShouldBe(0);
            extended.Contains(4).ShouldBeTrue();
            extended.DistinctCount.ShouldBe(2);
        }

        [Fact]
        public void Adding_and_removing_do_not_mutate_the_receiver()
        {
            var original = new PersistentBag<int>().Add(1);

            var added = original.Add(1);
            var removed = original.Remove(1);

            ReferenceEquals(original, added).ShouldBeFalse("a changed bag must be a new instance");
            ReferenceEquals(original, removed).ShouldBeFalse();
            original.CountOf(1).ShouldBe(1, "the receiver must still hold exactly what it did");
        }

        [Fact]
        public void Removing_a_copy_that_is_not_present_returns_the_same_instance()
        {
            var bag = new PersistentBag<int>().Add(1, 2);

            ReferenceEquals(bag, bag.Remove(9)).ShouldBeTrue("an absent element changes nothing");
            ReferenceEquals(bag, bag.RemoveAllCopies(9)).ShouldBeTrue();
            ReferenceEquals(bag, bag.Remove(1, 5)).ShouldBeFalse("copies were there to remove");
            ReferenceEquals(bag.Clear(), bag).ShouldBeFalse();

            var empty = bag.Clear();
            ReferenceEquals(empty.Clear(), empty).ShouldBeTrue("clearing an empty bag changes nothing");
        }

        [Fact]
        public void Removing_the_last_copy_drops_the_element()
        {
            var bag = new PersistentBag<int>().Add(1, 2).Add(2, 3);

            var emptied = bag.Remove(1, 2);

            emptied.DistinctCount.ShouldBe(1);
            emptied.TotalCount.ShouldBe(3);
            emptied.Contains(1).ShouldBeFalse("an element with no copies must not linger");
            emptied.CountOf(1).ShouldBe(0);
        }

        [Fact]
        public void The_counted_add_and_remove_overloads_reject_a_non_positive_count()
        {
            var bag = new PersistentBag<int>().Add(1, 2);

            Should.Throw<ArgumentOutOfRangeException>(() => bag.Add(1, 0));
            Should.Throw<ArgumentOutOfRangeException>(() => bag.Add(1, -1));
            Should.Throw<ArgumentOutOfRangeException>(() => bag.Remove(1, 0));
            Should.Throw<ArgumentOutOfRangeException>(() => bag.Remove(1, -1));

            bag.TotalCount.ShouldBe(2, "a rejected call must not change the bag");
        }

        [Fact]
        public void Removing_more_copies_than_are_held_empties_the_element()
        {
            var bag = new PersistentBag<int>().Add(5, 3);

            var after = bag.Remove(5, 10);

            after.CountOf(5).ShouldBe(0);
            after.DistinctCount.ShouldBe(0);
            after.TotalCount.ShouldBe(0);
            after.IsEmpty.ShouldBeTrue();
            bag.CountOf(5).ShouldBe(3, "the receiver keeps its copies");
        }

        [Fact]
        public void Enumerating_yields_every_copy()
        {
            var bag = new PersistentBag<string>().Add("a", 3).Add("b", 2);

            var copies = new List<string>(bag);
            var counted = new MultisetModel<string>(null);
            counted.AddRange(copies);

            copies.Count.ShouldBe(5);
            counted.CountOf("a").ShouldBe(3);
            counted.CountOf("b").ShouldBe(2);
        }

        [Fact]
        public void A_null_element_survives_the_round_trip_through_copies()
        {
            var bag = new PersistentBag<string>().Add(null, 2).Add("a");

            bag.CountOf(null).ShouldBe(2);
            bag.DistinctCount.ShouldBe(2);
            bag.Contains(null).ShouldBeTrue();

            var after = bag.Remove(null);
            after.CountOf(null).ShouldBe(1);
            after.Contains(null).ShouldBeTrue();

            var none = after.RemoveAllCopies(null);
            none.Contains(null).ShouldBeFalse();
            none.DistinctCount.ShouldBe(1);
        }

        [Fact]
        public void The_builder_agrees_with_folding_add_over_the_same_elements()
        {
            var random = new DeterministicRandom(BaseSeed + 10);
            var items = new List<int>();

            for (var index = 0; index < 200; index++)
            {
                items.Add(random.NextItem(IntDomain));
            }

            var folded = new PersistentBag<int>();
            foreach (var item in items)
            {
                folded = folded.Add(item);
            }

            var builder = new PersistentBagBuilder<int>();
            builder.AddRange(items);
            var built = builder.ToBag();

            built.TotalCount.ShouldBe(folded.TotalCount);
            built.DistinctCount.ShouldBe(folded.DistinctCount);

            for (var index = 0; index < IntDomain.Count; index++)
            {
                built.CountOf(IntDomain[index]).ShouldBe(folded.CountOf(IntDomain[index]));
            }
        }

        [Fact]
        public void The_builder_starts_from_a_snapshot_and_never_writes_back_into_it()
        {
            var bag = new PersistentBag<string>().Add("a", 2).Add("b").Add(null);

            var builder = bag.ToBuilder();
            builder.Add("a", 3);
            builder.Remove("b");
            builder.Add("c");

            var built = builder.ToBag();

            bag.CountOf("a").ShouldBe(2, "the bag the builder started from must be untouched");
            bag.CountOf("b").ShouldBe(1);
            bag.CountOf("c").ShouldBe(0);
            bag.CountOf(null).ShouldBe(1);

            built.CountOf("a").ShouldBe(5);
            built.CountOf("b").ShouldBe(0);
            built.CountOf("c").ShouldBe(1);
            built.CountOf(null).ShouldBe(1, "the null bucket has to survive the round trip");
            built.DistinctCount.ShouldBe(3);
        }

        [Fact]
        public void The_builder_can_be_asked_for_a_bag_more_than_once()
        {
            var builder = new PersistentBagBuilder<int>();
            builder.Add(1, 2);

            var first = builder.ToBag();
            builder.Add(1);

            var second = builder.ToBag();

            first.CountOf(1).ShouldBe(2, "an earlier snapshot must not follow later changes");
            second.CountOf(1).ShouldBe(3);
            builder.CountOf(1).ShouldBe(3);
        }

        [Fact]
        public void Clear_makes_a_new_empty_bag_that_keeps_the_comparer()
        {
            var bag = new PersistentBag<string>(StringComparer.OrdinalIgnoreCase).Add("a", 2);

            var cleared = bag.Clear();

            cleared.TotalCount.ShouldBe(0);
            cleared.DistinctCount.ShouldBe(0);
            cleared.IsEmpty.ShouldBeTrue();
            cleared.Comparer.ShouldBeSameAs(bag.Comparer, "a cleared bag must keep deciding equality the same way");
            cleared.Add("A").CountOf("a").ShouldBe(1, "the comparer still folds case after Clear");
        }
    }
}
