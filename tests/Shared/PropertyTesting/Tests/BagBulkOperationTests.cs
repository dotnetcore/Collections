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
    /// Bulk-operation and boundary properties for the bag contract: the parts a random walk over
    /// single-element operations reaches only by accident, if at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>IMultiSet&lt;T&gt;.AddRange</c> promises one copy per occurrence and says nothing about
    /// order, so the suite builds the same sequence in two orders and requires the same bag. It also
    /// requires the bulk form to agree with folding <c>Add</c> over the sequence, because an
    /// implementation may route one through the other and a defect in the shortcut would otherwise
    /// stay invisible.
    /// </para>
    /// <para>
    /// The counted removal form carries two contract points that a contents-only comparison would
    /// miss: it removes <em>at most</em> the requested number of copies and returns the number
    /// remaining, so asking for more than is held must empty the element and report zero rather
    /// than throw or go negative. Both overloads reject a non-positive count.
    /// </para>
    /// <para>
    /// Sequences come from <see cref="DeterministicRandom"/> rather than a bare <c>Random</c> so a
    /// failure names the seed that produced it.
    /// </para>
    /// </remarks>
    public class BagBulkOperationTests
    {
        private const int BaseSeed = 20261101;
        private const int RunCount = 25;

        private static readonly IReadOnlyList<int> IntDomain = new[] { 0, 1, 2, 3 };

        [Fact]
        public void AddRange_is_independent_of_the_order_of_the_input()
        {
            var adapter = new MultiListBagAdapter<int>();

            for (var run = 0; run < RunCount; run++)
            {
                var seed = BaseSeed + run;
                var items = GenerateSequence(seed, IntDomain);
                var reversed = new List<int>(items);
                reversed.Reverse();

                var model = new MultisetModel<int>(null);
                model.AddRange(items);

                var forward = adapter.AddRange(adapter.Create(null), items);
                var backward = adapter.AddRange(adapter.Create(null), reversed);

                AssertAgreesWithModel(adapter, forward, model, IntDomain, seed, "forward");
                AssertAgreesWithModel(adapter, backward, model, IntDomain, seed, "reversed");

                adapter.TotalCount(backward).ShouldBe(
                    adapter.TotalCount(forward),
                    "seed " + seed + ": the same sequence in reverse order produced a different total");
            }
        }

        [Fact]
        public void AddRange_agrees_with_adding_the_elements_one_by_one()
        {
            var adapter = new MultiListBagAdapter<int>();

            for (var run = 0; run < RunCount; run++)
            {
                var seed = BaseSeed + 100 + run;
                var items = GenerateSequence(seed, IntDomain);

                var model = new MultisetModel<int>(null);
                model.AddRange(items);

                var viaRange = adapter.AddRange(adapter.Create(null), items);

                var viaAdd = adapter.Create(null);
                for (var index = 0; index < items.Count; index++)
                {
                    viaAdd = adapter.Add(viaAdd, items[index]);
                }

                AssertAgreesWithModel(adapter, viaRange, model, IntDomain, seed, "add-range");
                AssertAgreesWithModel(adapter, viaAdd, model, IntDomain, seed, "one-by-one");
            }
        }

        [Fact]
        public void AddRange_of_an_empty_sequence_leaves_the_bag_unchanged()
        {
            var adapter = new MultiListBagAdapter<int>();
            var model = new MultisetModel<int>(null);
            model.Add(7, 2);

            var bag = adapter.AddCopies(adapter.Create(null), 7, 2);
            var after = adapter.AddRange(bag, new int[0]);

            AssertAgreesWithModel(adapter, after, model, IntDomain, BaseSeed, "empty-range");
        }

        [Fact]
        public void AddRange_rejects_a_null_sequence()
        {
            var adapter = new MultiListBagAdapter<int>();

            Should.Throw<ArgumentNullException>(() => adapter.AddRange(adapter.Create(null), null));
        }

        [Fact]
        public void AddRange_handles_a_null_element()
        {
            // MultiList keeps null outside its dictionary, so the bulk form takes a structurally
            // different path from every other element.
            var adapter = new MultiListBagAdapter<string>();
            var items = new[] { "a", null, "b", null, "a" };

            var model = new MultisetModel<string>(null);
            model.AddRange(items);

            var bag = adapter.AddRange(adapter.Create(null), items);

            AssertAgreesWithModel(
                adapter,
                bag,
                model,
                new[] { "a", "b", null },
                BaseSeed,
                "null-element");
        }

        [Fact]
        public void RemoveCopies_beyond_the_copy_count_empties_the_element_and_reports_zero()
        {
            var adapter = new MultiListBagAdapter<int>();
            var bag = adapter.AddCopies(adapter.Create(null), 5, 3);

            int remaining;
            var after = adapter.RemoveCopies(bag, 5, 10, out remaining);

            remaining.ShouldBe(0, "asking for more copies than are held must remove what exists and report none left");
            adapter.CountOf(after, 5).ShouldBe(0);
            adapter.Contains(after, 5).ShouldBeFalse("an element whose last copy went must leave the bag");
            adapter.DistinctCount(after).ShouldBe(0);
            adapter.TotalCount(after).ShouldBe(0);
            adapter.ReportedCount(after).ShouldBe(0);
        }

        [Fact]
        public void RemoveCopies_of_an_absent_element_reports_zero_and_changes_nothing()
        {
            var adapter = new MultiListBagAdapter<int>();
            var bag = adapter.AddCopies(adapter.Create(null), 5, 2);

            int remaining;
            var after = adapter.RemoveCopies(bag, 9, 4, out remaining);

            remaining.ShouldBe(0);
            adapter.TotalCount(after).ShouldBe(2, "removing an absent element must not disturb the ones present");
            adapter.CountOf(after, 5).ShouldBe(2);

            var afterOne = adapter.RemoveOne(bag, 9, out remaining);
            remaining.ShouldBe(0);
            adapter.TotalCount(afterOne).ShouldBe(2);
        }

        [Fact]
        public void RemoveCopies_of_exactly_the_copy_count_reports_zero_remaining()
        {
            var adapter = new MultiListBagAdapter<int>();
            var bag = adapter.AddCopies(adapter.Create(null), 5, 3);

            int remaining;
            var after = adapter.RemoveCopies(bag, 5, 3, out remaining);

            remaining.ShouldBe(0);
            adapter.DistinctCount(after).ShouldBe(0);
            adapter.TotalCount(after).ShouldBe(0);
        }

        [Fact]
        public void RemoveCopies_of_a_strict_subset_keeps_the_element()
        {
            var adapter = new MultiListBagAdapter<int>();
            var bag = adapter.AddCopies(adapter.Create(null), 5, 3);

            int remaining;
            var after = adapter.RemoveCopies(bag, 5, 2, out remaining);

            remaining.ShouldBe(1);
            adapter.CountOf(after, 5).ShouldBe(1);
            adapter.Contains(after, 5).ShouldBeTrue();
            adapter.DistinctCount(after).ShouldBe(1);
            adapter.TotalCount(after).ShouldBe(1);
        }

        [Fact]
        public void AddCopies_rejects_a_non_positive_count()
        {
            var adapter = new MultiListBagAdapter<int>();
            var bag = adapter.Create(null);

            Should.Throw<ArgumentOutOfRangeException>(() => adapter.AddCopies(bag, 1, 0));
            Should.Throw<ArgumentOutOfRangeException>(() => adapter.AddCopies(bag, 1, -1));
            adapter.TotalCount(bag).ShouldBe(0, "a rejected call must not mutate the bag");
        }

        [Fact]
        public void RemoveCopies_rejects_a_non_positive_count()
        {
            var adapter = new MultiListBagAdapter<int>();
            var bag = adapter.AddCopies(adapter.Create(null), 1, 2);

            var ignored = 0;
            Should.Throw<ArgumentOutOfRangeException>(() => adapter.RemoveCopies(bag, 1, 0, out ignored));
            Should.Throw<ArgumentOutOfRangeException>(() => adapter.RemoveCopies(bag, 1, -1, out ignored));
            adapter.CountOf(bag, 1).ShouldBe(2, "a rejected call must not remove any copy");
        }

        private static List<int> GenerateSequence(int seed, IReadOnlyList<int> domain)
        {
            var random = new DeterministicRandom(seed);
            var length = random.NextInt(0, 25);
            var items = new List<int>(length);

            for (var index = 0; index < length; index++)
            {
                items.Add(random.NextItem(domain));
            }

            return items;
        }

        private static void AssertAgreesWithModel<T>(
            IBagUnderTest<MultiList<T>, T> adapter,
            MultiList<T> bag,
            MultisetModel<T> model,
            IReadOnlyList<T> domain,
            int seed,
            string variant)
        {
            var prefix = "seed " + seed + " (" + variant + ")";

            adapter.TotalCount(bag).ShouldBe(model.TotalCount, prefix + ": TotalCount");
            adapter.ReportedCount(bag).ShouldBe(model.TotalCount, prefix + ": inherited Count");
            adapter.DistinctCount(bag).ShouldBe(model.DistinctCount, prefix + ": DistinctCount");

            for (var index = 0; index < domain.Count; index++)
            {
                var item = domain[index];
                adapter.CountOf(bag, item).ShouldBe(
                    model.CountOf(item),
                    prefix + ": CountOf(" + Format(item) + ")");
                adapter.Contains(bag, item).ShouldBe(
                    model.Contains(item),
                    prefix + ": Contains(" + Format(item) + ")");
            }
        }

        private static string Format<T>(T item)
        {
            return ReferenceEquals(item, null) ? "null" : item.ToString();
        }
    }
}
