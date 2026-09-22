using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// F6-32: the interface abstraction layer - <see cref="IMultiSet{T}"/>,
    /// <see cref="IMultiDictionary{TKey,TValue}"/> and <see cref="IBiMap{TLeft,TRight}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every contract test runs against <em>all</em> implementations through the interface, so the
    /// suite proves three things at once:
    /// </para>
    /// <list type="number">
    /// <item><description>the existing types honour the contract (acceptance ②: adding the interface
    /// changes no behaviour - the same assertions that pass against the concrete API pass through the
    /// interface, and the <c>Count</c> / <c>Values</c> members keep their concrete meaning);</description></item>
    /// <item><description>the ordered and the unordered family really are interchangeable behind the
    /// interface, which is the whole point of the abstraction;</description></item>
    /// <item><description>a third-party implementation is possible - each interface is exercised by a
    /// hand-written fake that shares no code with this package (design constraint ③: Mock / replace /
    /// test).</description></item>
    /// </list>
    /// <para>
    /// The member sets are additionally pinned by reflection, so the deliberate exclusions (set
    /// algebra, comparer properties, view factories) can not creep back in unnoticed (acceptance ①).
    /// </para>
    /// </remarks>
    public class InterfaceAbstractionTests
    {
        // ------------------------------------------------------------------
        // Implementation registry - one entry per implementation, addressed by
        // name so the test names stay readable.
        // ------------------------------------------------------------------

        private static readonly Dictionary<string, Func<IMultiSet<int>>> MultiSets =
            new Dictionary<string, Func<IMultiSet<int>>>
            {
                ["MultiList"] = () => new MultiList<int>(),
                ["OrderedMultiList"] = () => new OrderedMultiList<int>(),
                ["ThirdPartyFake"] = () => new FakeMultiSet(),
            };

        private static readonly Dictionary<string, Func<IMultiDictionary<string, int>>> MultiMaps =
            new Dictionary<string, Func<IMultiDictionary<string, int>>>
            {
                ["MultiDictionary"] = () => new MultiDictionary<string, int>(),
                ["OrderedMultiDictionary"] = () => new OrderedMultiDictionary<string, int>(),
                ["ThirdPartyFake"] = () => new FakeMultiMap(),
            };

        private static readonly Dictionary<string, Func<IBiMap<int, string>>> BiMaps =
            new Dictionary<string, Func<IBiMap<int, string>>>
            {
                ["BiDictionary"] = () => new BiDictionary<int, string>(),
                ["ThirdPartyFake"] = () => new FakeBiMap(),
            };

        public static IEnumerable<object[]> MultiSetImplementations => MultiSets.Keys.Select(k => new object[] { k });

        public static IEnumerable<object[]> MultiMapImplementations => MultiMaps.Keys.Select(k => new object[] { k });

        public static IEnumerable<object[]> BiMapImplementations => BiMaps.Keys.Select(k => new object[] { k });

        // ==================================================================
        // IMultiSet<T>
        // ==================================================================

        /// <summary>
        /// The trap called out by acceptance ③: in a multiset <c>Count</c> is the number of copies,
        /// not the number of distinct elements - including the inherited
        /// <see cref="IReadOnlyCollection{T}.Count"/>.
        /// </summary>
        [Theory]
        [MemberData(nameof(MultiSetImplementations))]
        public void MultiSet_CountIsTheNumberOfCopies_NotTheNumberOfDistinctElements(string implementation)
        {
            var set = MultiSets[implementation]();

            set.Add(7, 3);
            set.Add(9);

            set.TotalCount.ShouldBe(4);
            set.DistinctCount.ShouldBe(2);

            // The inherited member must agree with TotalCount, never with DistinctCount.
            ((IReadOnlyCollection<int>)set).Count.ShouldBe(4);
            set.Count.ShouldBe(4);

            // And a single element with many copies still reports one distinct element.
            var single = MultiSets[implementation]();
            single.Add(1, 10);
            single.TotalCount.ShouldBe(10);
            single.DistinctCount.ShouldBe(1);
            ((IReadOnlyCollection<int>)single).Count.ShouldBe(10);
        }

        [Theory]
        [MemberData(nameof(MultiSetImplementations))]
        public void MultiSet_Remove_ReturnsTheCopiesRemaining(string implementation)
        {
            var set = MultiSets[implementation]();
            set.Add(7, 3);

            set.Remove(7).ShouldBe(2);
            set.Remove(7).ShouldBe(1);
            set.Remove(7).ShouldBe(0);

            // The element is dropped once its copy count reaches zero, so a further removal is a
            // no-op that reports zero rather than throwing.
            set.Remove(7).ShouldBe(0);
            set.TotalCount.ShouldBe(0);
            set.DistinctCount.ShouldBe(0);
        }

        [Theory]
        [MemberData(nameof(MultiSetImplementations))]
        public void MultiSet_RemoveTimes_RemovesAtMostTheCopiesAvailable(string implementation)
        {
            var set = MultiSets[implementation]();
            set.Add(7, 2);

            set.Remove(7, 5).ShouldBe(0);
            set.TotalCount.ShouldBe(0);
            set.CountOf(7).ShouldBe(0);
        }

        [Theory]
        [MemberData(nameof(MultiSetImplementations))]
        public void MultiSet_RemoveAllCopies_ReportsWhetherAnythingWent(string implementation)
        {
            var set = MultiSets[implementation]();
            set.Add(1, 3);

            set.RemoveAllCopies(1).ShouldBeTrue();
            set.DistinctCount.ShouldBe(0);
            set.TotalCount.ShouldBe(0);

            set.RemoveAllCopies(1).ShouldBeFalse();
        }

        [Theory]
        [MemberData(nameof(MultiSetImplementations))]
        public void MultiSet_MembershipQueries_AgreeWithTheCounters(string implementation)
        {
            var set = MultiSets[implementation]();
            set.Add(7, 2);

            set.Contains(7).ShouldBeTrue();
            set.Contains(99).ShouldBeFalse();
            set.CountOf(7).ShouldBe(2);
            set.CountOf(99).ShouldBe(0);

            set.ContainsAll(new[] { 7 }).ShouldBeTrue();
            set.ContainsAll(new[] { 7, 7 }).ShouldBeTrue();
            set.ContainsAll(new[] { 7, 99 }).ShouldBeFalse();

            // Vacuously true, as documented.
            set.ContainsAll(Enumerable.Empty<int>()).ShouldBeTrue();
        }

        [Theory]
        [MemberData(nameof(MultiSetImplementations))]
        public void MultiSet_EntrySet_And_DistinctItems_AgreeWithTheCounters(string implementation)
        {
            var set = MultiSets[implementation]();
            set.Add(1, 2);
            set.Add(2, 3);

            set.DistinctItems().OrderBy(x => x).ShouldBe(new[] { 1, 2 });

            var entries = set.EntrySet().ToDictionary(e => e.Item, e => e.Count);
            entries.Count.ShouldBe(set.DistinctCount);
            entries.Values.Sum().ShouldBe(set.TotalCount);
            entries[1].ShouldBe(2);
            entries[2].ShouldBe(3);
        }

        [Theory]
        [MemberData(nameof(MultiSetImplementations))]
        public void MultiSet_AddRange_And_Clear(string implementation)
        {
            var set = MultiSets[implementation]();
            set.AddRange(new[] { 1, 1, 2 });

            set.TotalCount.ShouldBe(3);
            set.DistinctCount.ShouldBe(2);

            set.Clear();
            set.TotalCount.ShouldBe(0);
            set.DistinctCount.ShouldBe(0);
            set.Contains(1).ShouldBeFalse();
        }

        [Theory]
        [MemberData(nameof(MultiSetImplementations))]
        public void MultiSet_Add_RejectsNonPositiveTimes(string implementation)
        {
            var set = MultiSets[implementation]();

            Should.Throw<ArgumentOutOfRangeException>(() => set.Add(1, 0));
            Should.Throw<ArgumentOutOfRangeException>(() => set.Add(1, -1));
            Should.Throw<ArgumentOutOfRangeException>(() => set.Remove(1, 0));
            Should.Throw<ArgumentOutOfRangeException>(() => set.Remove(1, -1));
        }

        [Fact]
        public void MultiSet_BothConcreteMultisets_AreUsableThroughTheInterface()
        {
            IMultiSet<int> unordered = new MultiList<int>();
            IMultiSet<int> ordered = new OrderedMultiList<int>();

            foreach (var set in new[] { unordered, ordered })
            {
                set.Add(5, 2);
                set.Count.ShouldBe(2);
                set.DistinctCount.ShouldBe(1);
            }

            typeof(IMultiSet<int>).IsAssignableFrom(typeof(MultiList<int>)).ShouldBeTrue();
            typeof(IMultiSet<int>).IsAssignableFrom(typeof(OrderedMultiList<int>)).ShouldBeTrue();
        }

        // ==================================================================
        // IMultiDictionary<TKey, TValue>
        // ==================================================================

        [Theory]
        [MemberData(nameof(MultiMapImplementations))]
        public void MultiMap_CountIsTheKeyCount_NotTheValueCount(string implementation)
        {
            var map = MultiMaps[implementation]();
            map.Add("a", 1);
            map.Add("a", 2);
            map.Add("b", 3);

            map.KeyCount.ShouldBe(2);
            map.TotalValueCount.ShouldBe(3);

            // The inherited dictionary view counts keys, not pairs.
            map.Count.ShouldBe(2);
            ((IReadOnlyCollection<KeyValuePair<string, IReadOnlyCollection<int>>>)map).Count.ShouldBe(2);
        }

        /// <summary>
        /// The interface's <c>Values</c> must stay the flat sequence, matching the concrete types -
        /// the inherited per-key view remains reachable by casting.
        /// </summary>
        [Theory]
        [MemberData(nameof(MultiMapImplementations))]
        public void MultiMap_ValuesOnTheInterface_IsTheFlatSequence(string implementation)
        {
            var map = MultiMaps[implementation]();
            map.Add("a", 1);
            map.Add("a", 2);
            map.Add("b", 3);

            var flat = map.Values.ToList();
            flat.Count.ShouldBe(3);
            flat.OrderBy(x => x).ShouldBe(new[] { 1, 2, 3 });

            var grouped = ((IReadOnlyDictionary<string, IReadOnlyCollection<int>>)map).Values.ToList();
            grouped.Count.ShouldBe(2);
            grouped.Select(c => c.Count).OrderBy(c => c).ShouldBe(new[] { 1, 2 });
        }

        [Theory]
        [MemberData(nameof(MultiMapImplementations))]
        public void MultiMap_InheritedDictionaryView_IsPerKey(string implementation)
        {
            var map = MultiMaps[implementation]();
            map.Add("a", 1);
            map.Add("a", 2);
            map.Add("b", 3);

            map.ContainsKey("b").ShouldBeTrue();
            map.ContainsKey("zz").ShouldBeFalse();

            // A missing key yields an empty collection, never null.
            map["zz"].ShouldBeEmpty();
            map["a"].Count.ShouldBe(2);

            map.TryGetValue("b", out var values).ShouldBeTrue();
            values.ShouldBe(new[] { 3 });
            map.TryGetValue("zz", out _).ShouldBeFalse();

            map.Keys.OrderBy(k => k).ShouldBe(new[] { "a", "b" });

            // foreach over the interface is unambiguous and yields one pair per key.
            var enumeratedKeys = new List<string>();
            foreach (var pair in map)
            {
                enumeratedKeys.Add(pair.Key);
                pair.Value.Count.ShouldBe(map.ValueCount(pair.Key));
            }

            enumeratedKeys.Count.ShouldBe(2);
        }

        [Theory]
        [MemberData(nameof(MultiMapImplementations))]
        public void MultiMap_ValueCount_And_MembershipQueries(string implementation)
        {
            var map = MultiMaps[implementation]();
            map.Add("a", 1);
            map.Add("a", 2);

            map.ValueCount("a").ShouldBe(2);
            map.ValueCount("zz").ShouldBe(0);

            map.Contains("a", 2).ShouldBeTrue();
            map.Contains("a", 99).ShouldBeFalse();
            map.Contains("zz", 1).ShouldBeFalse();

            map.ContainsValue(2).ShouldBeTrue();
            map.ContainsValue(99).ShouldBeFalse();
        }

        [Theory]
        [MemberData(nameof(MultiMapImplementations))]
        public void MultiMap_AddRange_Remove_And_RemoveRange(string implementation)
        {
            var map = MultiMaps[implementation]();
            map.AddRange("a", new[] { 1, 2, 2 });

            map.ValueCount("a").ShouldBe(3);

            // One occurrence goes per call.
            map.Remove("a", 2).ShouldBeTrue();
            map.ValueCount("a").ShouldBe(2);

            map.Remove("a", 99).ShouldBeFalse();

            // RemoveRange treats the argument as a set: one occurrence per distinct value.
            map.RemoveRange("a", new[] { 1, 2 }).ShouldBeTrue();
            map.ValueCount("a").ShouldBe(0);

            // The key is dropped once its last value goes.
            map.KeyCount.ShouldBe(0);
            map.ContainsKey("a").ShouldBeFalse();

            // A missing key is a no-op.
            map.RemoveRange("a", new[] { 1 }).ShouldBeFalse();
            map.Remove("a").ShouldBeFalse();
        }

        [Theory]
        [MemberData(nameof(MultiMapImplementations))]
        public void MultiMap_RemoveKey_DropsAllOfItsValues(string implementation)
        {
            var map = MultiMaps[implementation]();
            map.AddRange("a", new[] { 1, 2, 3 });

            map.Remove("a").ShouldBeTrue();
            map.KeyCount.ShouldBe(0);
            map.TotalValueCount.ShouldBe(0);
            map.Remove("a").ShouldBeFalse();
        }

        [Theory]
        [MemberData(nameof(MultiMapImplementations))]
        public void MultiMap_AsLookup_And_Clear(string implementation)
        {
            var map = MultiMaps[implementation]();
            map.AddRange("a", new[] { 1, 2 });

            var lookup = map.AsLookup();
            lookup["a"].Count().ShouldBe(2);
            lookup["a"].OrderBy(x => x).ShouldBe(new[] { 1, 2 });
            lookup["zz"].Count().ShouldBe(0);

            map.Clear();
            map.KeyCount.ShouldBe(0);
            map.TotalValueCount.ShouldBe(0);
        }

        [Fact]
        public void MultiMap_BothConcreteMultimaps_AreUsableThroughTheInterface()
        {
            IMultiDictionary<string, int> unordered = new MultiDictionary<string, int>();
            IMultiDictionary<string, int> ordered = new OrderedMultiDictionary<string, int>();

            foreach (var map in new[] { unordered, ordered })
            {
                map.Add("k", 1);
                map.Add("k", 2);
                map.Count.ShouldBe(1);
                map.TotalValueCount.ShouldBe(2);
                map.Values.Count().ShouldBe(2);
            }

            typeof(IMultiDictionary<string, int>).IsAssignableFrom(typeof(MultiDictionary<string, int>)).ShouldBeTrue();
            typeof(IMultiDictionary<string, int>).IsAssignableFrom(typeof(OrderedMultiDictionary<string, int>)).ShouldBeTrue();
        }

        // ==================================================================
        // IBiMap<TLeft, TRight>
        // ==================================================================

        [Theory]
        [MemberData(nameof(BiMapImplementations))]
        public void BiMap_BothDirectionsResolveTheSameBinding(string implementation)
        {
            var map = BiMaps[implementation]();
            map.Add(1, "alice");
            map.Add(2, "bob");

            map.Count.ShouldBe(2);

            // Forward.
            map[1].ShouldBe("alice");
            map.ContainsKey(1).ShouldBeTrue();
            map.TryGetValue(2, out var right).ShouldBeTrue();
            right.ShouldBe("bob");

            // Reverse.
            map.GetLeft("bob").ShouldBe(2);
            map.ContainsRight("alice").ShouldBeTrue();
            map.TryGetLeft("alice", out var left).ShouldBeTrue();
            left.ShouldBe(1);
        }

        [Theory]
        [MemberData(nameof(BiMapImplementations))]
        public void BiMap_TryAdd_ReportsConflictsWithoutThrowing(string implementation)
        {
            var map = BiMaps[implementation]();
            map.Add(1, "alice");

            map.TryAdd(1, "zed").ShouldBeFalse();
            map.TryAdd(9, "alice").ShouldBeFalse();
            map.Count.ShouldBe(1);

            map.TryAdd(9, "zed").ShouldBeTrue();
            map.Count.ShouldBe(2);
        }

        [Theory]
        [MemberData(nameof(BiMapImplementations))]
        public void BiMap_Add_ThrowsOnConflict_AndLeavesTheMapUntouched(string implementation)
        {
            var map = BiMaps[implementation]();
            map.Add(1, "alice");

            Should.Throw<ArgumentException>(() => map.Add(1, "zed"));
            Should.Throw<ArgumentException>(() => map.Add(9, "alice"));

            map.Count.ShouldBe(1);
            map[1].ShouldBe("alice");
            map.ContainsRight("zed").ShouldBeFalse();
        }

        [Theory]
        [MemberData(nameof(BiMapImplementations))]
        public void BiMap_Remove_FreesBothSides(string implementation)
        {
            var map = BiMaps[implementation]();
            map.Add(1, "alice");

            map.Remove(1).ShouldBeTrue();
            map.ContainsRight("alice").ShouldBeFalse();
            map.Count.ShouldBe(0);

            // The freed right value can be bound again.
            map.TryAdd(2, "alice").ShouldBeTrue();

            map.Remove(99).ShouldBeFalse();
        }

        [Theory]
        [MemberData(nameof(BiMapImplementations))]
        public void BiMap_RemoveRight_FreesBothSides(string implementation)
        {
            var map = BiMaps[implementation]();
            map.Add(1, "alice");

            map.RemoveRight("alice").ShouldBeTrue();
            map.ContainsKey(1).ShouldBeFalse();

            map.TryAdd(1, "bob").ShouldBeTrue();
            map.RemoveRight("zz").ShouldBeFalse();
        }

        [Theory]
        [MemberData(nameof(BiMapImplementations))]
        public void BiMap_ReadingAnUnboundSide_Throws(string implementation)
        {
            var map = BiMaps[implementation]();

            Should.Throw<KeyNotFoundException>(() => map.GetLeft("zz"));
            Should.Throw<KeyNotFoundException>(() => map[99]);
        }

        [Theory]
        [MemberData(nameof(BiMapImplementations))]
        public void BiMap_Clear_EmptiesBothDirections(string implementation)
        {
            var map = BiMaps[implementation]();
            map.Add(1, "alice");
            map.Add(2, "bob");

            map.Clear();

            map.Count.ShouldBe(0);
            map.ContainsKey(1).ShouldBeFalse();
            map.ContainsRight("alice").ShouldBeFalse();
            map.Values.ShouldBeEmpty();
            map.Keys.ShouldBeEmpty();
        }

        [Fact]
        public void BiMap_BiDictionary_IsUsableThroughTheInterface()
        {
            IBiMap<int, string> map = new BiDictionary<int, string>();
            map.Add(1, "alice");

            map.GetLeft("alice").ShouldBe(1);

            typeof(IBiMap<int, string>).IsAssignableFrom(typeof(BiDictionary<int, string>)).ShouldBeTrue();
        }

        // ==================================================================
        // Acceptance ① - the member sets are pinned
        // ==================================================================

        [Fact]
        public void IMultiSet_ExposesExactlyTheAgreedMembers()
        {
            DeclaredMemberNames(typeof(IMultiSet<>)).ShouldBe(new[]
            {
                "Add", "AddRange", "Clear", "Contains", "ContainsAll", "CountOf",
                "DistinctCount", "DistinctItems", "EntrySet", "Remove", "RemoveAllCopies", "TotalCount",
            });

            // The inherited IReadOnlyCollection<T> is the only base - notably NOT ICollection<T>,
            // whose bool Remove(T) would clash with the multiset's int Remove(T).
            typeof(IReadOnlyCollection<int>).IsAssignableFrom(typeof(IMultiSet<int>)).ShouldBeTrue();
            typeof(ICollection<int>).IsAssignableFrom(typeof(IMultiSet<int>)).ShouldBeFalse();
        }

        [Fact]
        public void IMultiDictionary_ExposesExactlyTheAgreedMembers()
        {
            DeclaredMemberNames(typeof(IMultiDictionary<,>)).ShouldBe(new[]
            {
                "Add", "AddRange", "AsLookup", "Clear", "Contains", "ContainsValue",
                "KeyCount", "Remove", "RemoveRange", "TotalValueCount", "ValueCount", "Values",
            });

            typeof(IReadOnlyDictionary<string, IReadOnlyCollection<int>>)
                .IsAssignableFrom(typeof(IMultiDictionary<string, int>))
                .ShouldBeTrue();
        }

        [Fact]
        public void IBiMap_ExposesExactlyTheAgreedMembers()
        {
            DeclaredMemberNames(typeof(IBiMap<,>)).ShouldBe(new[]
            {
                "Add", "Clear", "ContainsRight", "GetLeft", "Remove", "RemoveRight", "TryAdd", "TryGetLeft",
            });

            typeof(IReadOnlyDictionary<int, string>).IsAssignableFrom(typeof(IBiMap<int, string>)).ShouldBeTrue();
        }

        /// <summary>
        /// Names declared by the interface itself, with overloads collapsed, inherited members
        /// excluded and compiler-generated accessors (<c>get_</c> / <c>set_</c>) filtered out.
        /// </summary>
        private static string[] DeclaredMemberNames(Type interfaceDefinition)
        {
            return interfaceDefinition
                .GetMembers()
                .Where(m => m.DeclaringType == interfaceDefinition)
                .Where(m => !(m is MethodInfo method) || !method.IsSpecialName)
                .Select(m => m.Name)
                .Distinct()
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToArray();
        }

        // ==================================================================
        // Third-party implementations - no code shared with the package.
        // These are what make the interfaces worth having: an independent
        // implementation drops straight into the same contract tests above.
        // ==================================================================

        private sealed class FakeMultiSet : IMultiSet<int>
        {
            private readonly Dictionary<int, int> _counts = new Dictionary<int, int>();

            public int TotalCount { get; private set; }

            public int DistinctCount => _counts.Count;

            public int Count => TotalCount;

            public void Add(int item)
            {
                Add(item, 1);
            }

            public void Add(int item, int times)
            {
                if (times <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(times));
                }

                _counts[item] = CountOf(item) + times;
                TotalCount += times;
            }

            public void AddRange(IEnumerable<int> items)
            {
                if (items == null)
                {
                    throw new ArgumentNullException(nameof(items));
                }

                foreach (var item in items)
                {
                    Add(item);
                }
            }

            public bool Contains(int item) => _counts.ContainsKey(item);

            public bool ContainsAll(IEnumerable<int> items)
            {
                if (items == null)
                {
                    throw new ArgumentNullException(nameof(items));
                }

                foreach (var item in items)
                {
                    if (!Contains(item))
                    {
                        return false;
                    }
                }

                return true;
            }

            public int CountOf(int item) => _counts.TryGetValue(item, out var count) ? count : 0;

            public int Remove(int item) => Remove(item, 1);

            public int Remove(int item, int times)
            {
                if (times <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(times));
                }

                var current = CountOf(item);
                if (current == 0)
                {
                    return 0;
                }

                var removed = times > current ? current : times;
                var remaining = current - removed;
                if (remaining == 0)
                {
                    _counts.Remove(item);
                }
                else
                {
                    _counts[item] = remaining;
                }

                TotalCount -= removed;
                return remaining;
            }

            public bool RemoveAllCopies(int item)
            {
                var current = CountOf(item);
                if (current == 0)
                {
                    return false;
                }

                _counts.Remove(item);
                TotalCount -= current;
                return true;
            }

            public void Clear()
            {
                _counts.Clear();
                TotalCount = 0;
            }

            public IEnumerable<int> DistinctItems() => _counts.Keys;

            public IEnumerable<(int Item, int Count)> EntrySet()
            {
                foreach (var pair in _counts)
                {
                    yield return (pair.Key, pair.Value);
                }
            }

            public IEnumerator<int> GetEnumerator()
            {
                foreach (var pair in _counts)
                {
                    for (var i = 0; i < pair.Value; i++)
                    {
                        yield return pair.Key;
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class FakeMultiMap : IMultiDictionary<string, int>
        {
            private readonly Dictionary<string, List<int>> _map = new Dictionary<string, List<int>>();

            public int KeyCount => _map.Count;

            public int TotalValueCount => _map.Values.Sum(v => v.Count);

            /// <summary>The flat sequence - deliberately hides the inherited per-key view.</summary>
            public IEnumerable<int> Values => _map.Values.SelectMany(v => v);

            public int ValueCount(string key) => _map.TryGetValue(key, out var values) ? values.Count : 0;

            public void Add(string key, int value)
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                if (!_map.TryGetValue(key, out var values))
                {
                    values = new List<int>();
                    _map.Add(key, values);
                }

                values.Add(value);
            }

            public void AddRange(string key, IEnumerable<int> values)
            {
                if (values == null)
                {
                    throw new ArgumentNullException(nameof(values));
                }

                foreach (var value in values)
                {
                    Add(key, value);
                }
            }

            public bool Contains(string key, int value)
                => _map.TryGetValue(key, out var values) && values.Contains(value);

            public bool ContainsValue(int value) => _map.Values.Any(v => v.Contains(value));

            public bool Remove(string key) => _map.Remove(key);

            public bool Remove(string key, int value)
            {
                if (!_map.TryGetValue(key, out var values) || !values.Remove(value))
                {
                    return false;
                }

                if (values.Count == 0)
                {
                    _map.Remove(key);
                }

                return true;
            }

            public bool RemoveRange(string key, IEnumerable<int> values)
            {
                if (values == null)
                {
                    throw new ArgumentNullException(nameof(values));
                }

                var removed = false;
                foreach (var value in values.Distinct())
                {
                    removed |= Remove(key, value);
                }

                return removed;
            }

            public void Clear() => _map.Clear();

            public ILookup<string, int> AsLookup()
                => _map.SelectMany(pair => pair.Value, (pair, value) => new { pair.Key, Value = value })
                       .ToLookup(x => x.Key, x => x.Value);

            // ---- inherited IReadOnlyDictionary<string, IReadOnlyCollection<int>> ----

            public int Count => _map.Count;

            public IEnumerable<string> Keys => _map.Keys;

            public IReadOnlyCollection<int> this[string key]
                => _map.TryGetValue(key, out var values) ? values : (IReadOnlyCollection<int>)new int[0];

            public bool ContainsKey(string key) => _map.ContainsKey(key);

            public bool TryGetValue(string key, out IReadOnlyCollection<int> value)
            {
                if (_map.TryGetValue(key, out var values))
                {
                    value = values;
                    return true;
                }

                value = null!;
                return false;
            }

            IEnumerable<IReadOnlyCollection<int>> IReadOnlyDictionary<string, IReadOnlyCollection<int>>.Values
                => _map.Values;

            public IEnumerator<KeyValuePair<string, IReadOnlyCollection<int>>> GetEnumerator()
            {
                foreach (var pair in _map)
                {
                    yield return new KeyValuePair<string, IReadOnlyCollection<int>>(pair.Key, pair.Value);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class FakeBiMap : IBiMap<int, string>
        {
            private readonly Dictionary<int, string> _forward = new Dictionary<int, string>();
            private readonly Dictionary<string, int> _reverse = new Dictionary<string, int>();

            public int Count => _forward.Count;

            public IEnumerable<int> Keys => _forward.Keys;

            public IEnumerable<string> Values => _forward.Values;

            public string this[int left] => _forward[left];

            public bool ContainsKey(int left) => _forward.ContainsKey(left);

            public bool TryGetValue(int left, out string right) => _forward.TryGetValue(left, out right);

            public bool ContainsRight(string right) => _reverse.ContainsKey(right);

            public bool TryGetLeft(string right, out int left) => _reverse.TryGetValue(right, out left);

            public int GetLeft(string right) => _reverse[right];

            public void Add(int left, string right)
            {
                if (ContainsKey(left))
                {
                    throw new ArgumentException("The left value is already bound.", nameof(left));
                }

                if (ContainsRight(right))
                {
                    throw new ArgumentException("The right value is already bound.", nameof(right));
                }

                _forward.Add(left, right);
                _reverse.Add(right, left);
            }

            public bool TryAdd(int left, string right)
            {
                if (ContainsKey(left) || ContainsRight(right))
                {
                    return false;
                }

                _forward.Add(left, right);
                _reverse.Add(right, left);
                return true;
            }

            public bool Remove(int left)
            {
                if (!_forward.TryGetValue(left, out var right))
                {
                    return false;
                }

                _forward.Remove(left);
                _reverse.Remove(right);
                return true;
            }

            public bool RemoveRight(string right)
            {
                if (!_reverse.TryGetValue(right, out var left))
                {
                    return false;
                }

                _reverse.Remove(right);
                _forward.Remove(left);
                return true;
            }

            public void Clear()
            {
                _forward.Clear();
                _reverse.Clear();
            }

            public IEnumerator<KeyValuePair<int, string>> GetEnumerator() => _forward.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
