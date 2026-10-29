using System.Collections.Generic;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// Structural stress tests for <see cref="PersistentBag{T}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The property suite drives a three-element domain, which only ever fills the trie's first level
    /// and never builds a multiplicity large enough to tell a counter from a list of copies. These
    /// cases reach the rest: enough elements to grow several sub-tries, enough removals to collapse
    /// them again, comparers that force the collision paths a normal hash never produces, and a
    /// multiplicity in the hundreds of thousands.
    /// </para>
    /// <para>
    /// Every case compares against a <see cref="Dictionary{TKey,TValue}"/> of counts or of the
    /// element sequence, which shares no code with the trie and is obviously right.
    /// </para>
    /// </remarks>
    public class PersistentBagStructureTests
    {
        [Fact]
        public void Two_thousand_elements_agree_with_a_dictionary_of_counts()
        {
            var bag = new PersistentBag<int>();
            var reference = new Dictionary<int, int>();
            var expectedTotal = 0;

            for (var item = 0; item < 2000; item++)
            {
                var copies = (item % 3) + 1;
                bag = bag.Add(item, copies);

                reference.Add(item, copies);
                expectedTotal += copies;
            }

            bag.DistinctCount.ShouldBe(reference.Count);
            bag.TotalCount.ShouldBe(expectedTotal);
            bag.Count.ShouldBe(expectedTotal, "the inherited Count is the number of copies");

            foreach (var pair in reference)
            {
                bag.CountOf(pair.Key).ShouldBe(pair.Value, "copy count for element " + pair.Key);
                bag.Contains(pair.Key).ShouldBeTrue("element " + pair.Key + " should be present");
            }
        }

        [Fact]
        public void Removing_every_element_one_by_one_empties_the_bag()
        {
            var bag = new PersistentBag<int>();

            for (var item = 0; item < 500; item++)
            {
                bag = bag.Add(item, 2);
            }

            for (var item = 0; item < 500; item++)
            {
                bag = bag.RemoveAllCopies(item);

                bag.DistinctCount.ShouldBe(499 - item, "distinct count after removing element " + item);
                bag.Contains(item).ShouldBeFalse("element " + item + " must be gone");
            }

            bag.TotalCount.ShouldBe(0);
            bag.IsEmpty.ShouldBeTrue();
            bag.Clear().TotalCount.ShouldBe(0);
        }

        [Fact]
        public void A_comparer_that_collides_every_hash_still_agrees_with_the_reference()
        {
            // Every element shares one hash, so the whole bag lives in a single collision node and
            // every operation scans it. This is the only path where lookups are not logarithmic.
            var bag = new PersistentBag<int>(new FixedHashComparer(7));

            for (var item = 0; item < 120; item++)
            {
                bag = bag.Add(item, (item % 4) + 1);
            }

            bag.DistinctCount.ShouldBe(120);

            for (var item = 0; item < 120; item++)
            {
                bag.CountOf(item).ShouldBe((item % 4) + 1, "collision lookup for element " + item);
            }

            for (var item = 0; item < 120; item += 2)
            {
                bag = bag.RemoveAllCopies(item);
            }

            bag.DistinctCount.ShouldBe(60);
            bag.Contains(0).ShouldBeFalse();
            bag.Contains(1).ShouldBeTrue();
        }

        [Fact]
        public void A_partially_colliding_comparer_splits_a_cluster_into_a_sub_trie()
        {
            // Hashes 1 and 33 differ but select the same slot at the first level, so a cluster has to
            // be carried down one level as a single child while the newcomer takes the other child.
            var bag = new PersistentBag<int>(new ParityHashComparer());

            for (var item = 0; item < 200; item++)
            {
                bag = bag.Add(item);
            }

            bag.DistinctCount.ShouldBe(200);

            for (var item = 0; item < 200; item++)
            {
                bag.Contains(item).ShouldBeTrue("element " + item + " after cluster split");
                bag.CountOf(item).ShouldBe(1);
            }

            for (var item = 0; item < 200; item++)
            {
                bag = bag.Remove(item);
                bag.DistinctCount.ShouldBe(199 - item, "distinct count after removing " + item);
            }
        }

        [Fact]
        public void A_large_multiplicity_is_one_entry_and_not_one_node_per_copy()
        {
            // A bag that kept a cell per copy would need 200000 of them here; a counter needs one.
            // The observable consequence is that every one of these calls is a single write path, so
            // the whole case finishes in the time a handful of writes take.
            var bag = new PersistentBag<int>();

            for (var round = 0; round < 1000; round++)
            {
                bag = bag.Add(1, 200);
            }

            bag.CountOf(1).ShouldBe(200000);
            bag.DistinctCount.ShouldBe(1);
            bag.TotalCount.ShouldBe(200000);

            var half = bag.Remove(1, 100000);

            half.CountOf(1).ShouldBe(100000);
            bag.CountOf(1).ShouldBe(200000, "the receiver keeps its multiplicity");

            var none = half.RemoveAllCopies(1);
            none.IsEmpty.ShouldBeTrue();
            none.Contains(1).ShouldBeFalse();
        }

        [Fact]
        public void Enumerating_a_large_multiplicity_yields_exactly_that_many_copies()
        {
            var bag = new PersistentBag<int>().Add(7, 50000).Add(8, 3);

            var counted = 0;
            var countedSeven = 0;

            foreach (var item in bag)
            {
                counted++;

                if (item == 7)
                {
                    countedSeven++;
                }
            }

            counted.ShouldBe(50003);
            countedSeven.ShouldBe(50000);
        }

        [Fact]
        public void A_builder_over_many_elements_agrees_with_a_dictionary_of_counts()
        {
            var builder = new PersistentBagBuilder<int>();
            var reference = new Dictionary<int, int>();

            for (var item = 0; item < 5000; item++)
            {
                var copies = (item % 5) + 1;
                builder.Add(item, copies);

                reference.Add(item, copies);
            }

            var bag = builder.ToBag();

            bag.DistinctCount.ShouldBe(reference.Count);
            builder.DistinctCount.ShouldBe(reference.Count);
            builder.TotalCount.ShouldBe(bag.TotalCount);

            foreach (var pair in reference)
            {
                bag.CountOf(pair.Key).ShouldBe(pair.Value, "copy count for element " + pair.Key);
            }
        }

        [Fact]
        public void A_builder_that_removes_everything_produces_an_empty_bag()
        {
            var builder = new PersistentBagBuilder<int>();

            for (var item = 0; item < 300; item++)
            {
                builder.Add(item, 2);
            }

            for (var item = 0; item < 300; item++)
            {
                builder.Remove(item);
                builder.Remove(item);
            }

            builder.TotalCount.ShouldBe(0);
            builder.DistinctCount.ShouldBe(0);
            builder.ToBag().IsEmpty.ShouldBeTrue();
        }

        [Fact]
        public void A_neighbour_in_slot_thirty_one_still_resolves()
        {
            // The four hashes below differ at the first level in two pairs and at the second level in
            // the other two, so both merge paths are exercised: two keys meeting an inline entry, and
            // a collision cluster meeting a key with a different hash. Each pair ends with slot 31 on
            // one side, and slot 31's bit is the only negative one - ordering the children by that bit
            // instead of by the slot number put the highest slot first, which sent every lookup in
            // that node to the wrong child. Both insertion orders are checked because the merge is not
            // symmetric.
            var comparer = new SlotBoundaryComparer();

            var ascending = new PersistentBag<int>(comparer);
            foreach (var item in new[] { 10, 20, 30, 40 })
            {
                ascending = ascending.Add(item);
            }

            var descending = new PersistentBag<int>(comparer);
            foreach (var item in new[] { 40, 30, 20, 10 })
            {
                descending = descending.Add(item);
            }

            foreach (var item in new[] { 10, 20, 30, 40 })
            {
                ascending.CountOf(item).ShouldBe(1, "ascending order, element " + item);
                descending.CountOf(item).ShouldBe(1, "descending order, element " + item);
            }

            ascending.DistinctCount.ShouldBe(4);
            descending.DistinctCount.ShouldBe(4);
            ascending.TotalCount.ShouldBe(4);
            descending.TotalCount.ShouldBe(4);
        }

        [Fact]
        public void A_cluster_beside_a_slot_thirty_one_neighbour_still_resolves()
        {
            // Elements 50 and 60 share one hash, so they form a collision node that then has to be
            // separated from an element one slot below the highest one.
            var bag = new PersistentBag<int>(new SlotBoundaryComparer()).Add(50).Add(60).Add(70);

            bag.CountOf(50).ShouldBe(1);
            bag.CountOf(60).ShouldBe(1);
            bag.CountOf(70).ShouldBe(1);
            bag.DistinctCount.ShouldBe(3);

            var removed = bag.Remove(70);
            removed.CountOf(70).ShouldBe(0);
            removed.CountOf(50).ShouldBe(1);
            removed.DistinctCount.ShouldBe(2);
        }

        private sealed class SlotBoundaryComparer : IEqualityComparer<int>
        {
            public bool Equals(int x, int y)
            {
                return x == y;
            }

            public int GetHashCode(int obj)
            {
                // 0 and 992 share slot 0 at the first level and differ at the second (0 versus 31);
                // 5 and 997 do the same one slot higher. 50 and 60 share 992 outright, and 70 sits at
                // the same first-level slot as that pair.
                switch (obj)
                {
                    case 10:
                        return 0;
                    case 20:
                        return 5;
                    case 30:
                        return 992;
                    case 40:
                        return 997;
                    case 50:
                        return 992;
                    case 60:
                        return 992;
                    case 70:
                        return 0;
                    default:
                        return 992;
                }
            }
        }

        private sealed class FixedHashComparer : IEqualityComparer<int>
        {
            private readonly int _hash;

            internal FixedHashComparer(int hash)
            {
                _hash = hash;
            }

            public bool Equals(int x, int y)
            {
                return x == y;
            }

            public int GetHashCode(int obj)
            {
                return _hash;
            }
        }

        private sealed class ParityHashComparer : IEqualityComparer<int>
        {
            public bool Equals(int x, int y)
            {
                return x == y;
            }

            public int GetHashCode(int obj)
            {
                // 1 and 33 differ but share the low five bits, which is exactly the case where a
                // collision cluster has to be pushed one level down.
                return obj % 2 == 0 ? 1 : 33;
            }
        }
    }
}
