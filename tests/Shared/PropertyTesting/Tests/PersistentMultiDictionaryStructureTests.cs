using System.Collections.Generic;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// Structural stress tests for <see cref="PersistentMultiDictionary{TKey,TValue}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The property suite drives a three-key domain, which only ever fills the trie's first level.
    /// These cases exist to reach the rest of it: enough keys to grow several sub-tries, enough
    /// removals to collapse them again, and comparers that force the collision paths a normal hash
    /// never produces.
    /// </para>
    /// <para>
    /// Every case compares against a <see cref="Dictionary{TKey,TValue}"/> of lists, which shares no
    /// code with the trie and is obviously right.
    /// </para>
    /// </remarks>
    public class PersistentMultiDictionaryStructureTests
    {
        [Fact]
        public void Two_thousand_keys_agree_with_a_dictionary_of_lists()
        {
            var map = new PersistentMultiDictionary<int, int>();
            var reference = new Dictionary<int, List<int>>();
            var expectedTotal = 0;

            for (var key = 0; key < 2000; key++)
            {
                var copies = (key % 3) + 1;

                for (var copy = 0; copy < copies; copy++)
                {
                    var value = (key * 10) + copy;
                    map = map.Add(key, value);

                    List<int> list;
                    if (!reference.TryGetValue(key, out list))
                    {
                        list = new List<int>();
                        reference.Add(key, list);
                    }

                    list.Add(value);
                    expectedTotal++;
                }
            }

            map.KeyCount.ShouldBe(reference.Count);
            map.Count.ShouldBe(reference.Count);
            map.TotalValueCount.ShouldBe(expectedTotal);

            foreach (var pair in reference)
            {
                map.ContainsKey(pair.Key).ShouldBeTrue("key " + pair.Key + " should be present");
                map.ValueCount(pair.Key).ShouldBe(pair.Value.Count, "copy count for key " + pair.Key);
                map[pair.Key].ShouldBe(pair.Value, "values for key " + pair.Key + " in insertion order");
            }
        }

        [Fact]
        public void Removing_every_key_one_by_one_empties_the_map()
        {
            var map = new PersistentMultiDictionary<int, int>();
            var reference = new Dictionary<int, List<int>>();

            for (var key = 0; key < 500; key++)
            {
                map = map.Add(key, key).Add(key, key + 1000);
                reference.Add(key, new List<int> { key, key + 1000 });
            }

            for (var key = 0; key < 500; key++)
            {
                map = map.Remove(key);
                reference.Remove(key);

                map.KeyCount.ShouldBe(reference.Count, "key count after removing key " + key);
                map.ContainsKey(key).ShouldBeFalse("key " + key + " must be gone");
            }

            map.KeyCount.ShouldBe(0);
            map.TotalValueCount.ShouldBe(0);
            map.Clear().KeyCount.ShouldBe(0);
        }

        [Fact]
        public void A_comparer_that_collides_every_hash_still_agrees_with_the_reference()
        {
            // Every key shares one hash, so the whole map lives in a single collision node and every
            // operation scans it. This is the only path where lookups are not logarithmic.
            var map = new PersistentMultiDictionary<int, int>(new FixedHashComparer(7));
            var reference = new Dictionary<int, List<int>>();

            for (var key = 0; key < 120; key++)
            {
                map = map.Add(key, key * 2);
                reference.Add(key, new List<int> { key * 2 });
            }

            map.KeyCount.ShouldBe(120);

            foreach (var pair in reference)
            {
                map.ValueCount(pair.Key).ShouldBe(1, "collision lookup for key " + pair.Key);
            }

            for (var key = 0; key < 120; key += 2)
            {
                map = map.Remove(key);
            }

            map.KeyCount.ShouldBe(60);
            map.ContainsKey(0).ShouldBeFalse();
            map.ContainsKey(1).ShouldBeTrue();
        }

        [Fact]
        public void A_partially_colliding_comparer_splits_a_cluster_into_a_sub_trie()
        {
            // Hashes 1 and 33 differ but select the same slot at the first level, so a cluster has to
            // be carried down one level as a single child while the newcomer takes the other child.
            var map = new PersistentMultiDictionary<int, int>(new ParityHashComparer());
            var reference = new Dictionary<int, List<int>>();

            for (var key = 0; key < 200; key++)
            {
                map = map.Add(key, key);
                reference.Add(key, new List<int> { key });
            }

            map.KeyCount.ShouldBe(200);

            foreach (var pair in reference)
            {
                map.ContainsKey(pair.Key).ShouldBeTrue("key " + pair.Key + " after cluster split");
                map.ValueCount(pair.Key).ShouldBe(1);
            }

            for (var key = 0; key < 200; key++)
            {
                map = map.Remove(key);
                map.KeyCount.ShouldBe(199 - key, "key count after removing " + key);
            }
        }

        [Fact]
        public void Copy_counts_survive_a_long_run_of_adds_and_removes()
        {
            // One key, many copies: exercises the value list rather than the trie, including the
            // "remove the newest copy first" rule that keeps the survivors in insertion order.
            var map = new PersistentMultiDictionary<int, int>();

            for (var value = 0; value < 100; value++)
            {
                map = map.Add(1, value);
            }

            map.ValueCount(1).ShouldBe(100);
            map.TotalValueCount.ShouldBe(100);

            for (var value = 99; value >= 50; value--)
            {
                map = map.Remove(1, value);
            }

            map.ValueCount(1).ShouldBe(50);
            map.Contains(1, 99).ShouldBeFalse();
            map.Contains(1, 49).ShouldBeTrue();
            map[1].Count.ShouldBe(50, "the surviving copies must still be reachable through the indexer");
            map.Contains(1, 0).ShouldBeTrue("the oldest copies must survive");
            map.Contains(1, 49).ShouldBeTrue("the newest surviving copy must survive");
        }

        [Fact]
        public void A_neighbour_in_slot_thirty_one_still_resolves()
        {
            // The four hashes below differ at the first level in two pairs and at the second level in
            // the other two, so both merge paths are exercised: two keys meeting an inline entry, and
            // a collision cluster meeting a key with a different hash. Each pair ends with slot 31 on
            // one side, whose bit is the only negative one - ordering children by that bit rather than
            // by the slot number put the highest slot first and sent every lookup in that node to the
            // wrong child. Both insertion orders are checked because the merge is not symmetric.
            var comparer = new SlotBoundaryComparer();

            var ascending = new PersistentMultiDictionary<int, int>(comparer);
            foreach (var key in new[] { 10, 20, 30, 40 })
            {
                ascending = ascending.Add(key, key * 2);
            }

            var descending = new PersistentMultiDictionary<int, int>(comparer);
            foreach (var key in new[] { 40, 30, 20, 10 })
            {
                descending = descending.Add(key, key * 2);
            }

            foreach (var key in new[] { 10, 20, 30, 40 })
            {
                ascending.ContainsKey(key).ShouldBeTrue("ascending order, key " + key);
                descending.ContainsKey(key).ShouldBeTrue("descending order, key " + key);
                ascending.Contains(key, key * 2).ShouldBeTrue("ascending order, value of key " + key);
                descending.Contains(key, key * 2).ShouldBeTrue("descending order, value of key " + key);
            }

            ascending.KeyCount.ShouldBe(4);
            descending.KeyCount.ShouldBe(4);
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
