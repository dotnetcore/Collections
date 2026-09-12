using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// F6-03: <see cref="BiDictionary{TLeft,TRight}"/> - the strict one-to-one map with O(1)
    /// lookups on both axes. Covers the R3-01 conflict policy (Add throws, TryAdd reports,
    /// no silently-overwriting setter), the dedicated null buckets on both sides, the
    /// reverse-index maintenance on every write path, the live reverse view and the
    /// ToDictionary export (which omits a null-left binding).
    /// </summary>
    public class BiDictionaryTests
    {
        [Fact]
        public void Add_ForwardLookup_AndCount_BehaveLikeADictionary()
        {
            var map = new BiDictionary<int, string>();

            map.Add(1, "alice");
            map.Add(2, "bob");

            map.Count.ShouldBe(2);
            map.IsEmpty.ShouldBeFalse();
            map[1].ShouldBe("alice");
            map[2].ShouldBe("bob");
            map.ContainsKey(1).ShouldBeTrue();
            map.ContainsKey(3).ShouldBeFalse();
            map.TryGetValue(2, out var value).ShouldBeTrue();
            value.ShouldBe("bob");
            map.TryGetValue(3, out _).ShouldBeFalse();
        }

        [Fact]
        public void Add_DuplicateLeft_ThrowsArgumentException()
        {
            var map = new BiDictionary<int, string>();
            map.Add(1, "alice");

            Should.Throw<ArgumentException>(() => map.Add(1, "bob"));
            map.Count.ShouldBe(1);
            map[1].ShouldBe("alice");
        }

        [Fact]
        public void Add_RightBoundToDifferentLeft_ThrowsArgumentException_NoSilentOverwrite()
        {
            var map = new BiDictionary<int, string>();
            map.Add(1, "alice");

            // The core R3-01 decision: the right value is taken, so adding (2, "alice") must
            // throw instead of silently unbinding left 1.
            var exception = Should.Throw<ArgumentException>(() => map.Add(2, "alice"));
            exception.Message.ShouldContain("Remove the existing binding first");

            map.Count.ShouldBe(1);
            map[1].ShouldBe("alice");
            map.GetLeft("alice").ShouldBe(1);
        }

        [Fact]
        public void Add_DuplicateLeft_IsReportedBeforeTheRightConflict()
        {
            var map = new BiDictionary<int, string>();
            map.Add(1, "alice");

            // (1, "alice") exists: adding (1, "alice") again reports the duplicate left
            // (documented precedence), not the right conflict.
            var exception = Should.Throw<ArgumentException>(() => map.Add(1, "alice"));
            exception.Message.ShouldContain("same left value");
        }

        [Fact]
        public void TryAdd_ReportsConflicts_WithoutThrowing()
        {
            var map = new BiDictionary<int, string>();
            map.Add(1, "alice");

            map.TryAdd(1, "bob").ShouldBeFalse();     // left taken
            map.TryAdd(2, "alice").ShouldBeFalse();   // right taken by a different left
            map.TryAdd(2, "bob").ShouldBeTrue();      // both sides free

            map.Count.ShouldBe(2);
            map[1].ShouldBe("alice");
            map[2].ShouldBe("bob");
        }

        [Fact]
        public void Remove_DropsBothAxes_AndFreesTheRightValueForRebinding()
        {
            var map = new BiDictionary<int, string>();
            map.Add(1, "alice");

            map.Remove(1).ShouldBeTrue();

            map.Count.ShouldBe(0);
            map.ContainsKey(1).ShouldBeFalse();
            map.ContainsRight("alice").ShouldBeFalse();

            // The freed right value can be bound to a new left: the reverse index followed.
            map.TryAdd(2, "alice").ShouldBeTrue();
            map.GetLeft("alice").ShouldBe(2);
        }

        [Fact]
        public void Remove_MissingLeft_ReturnsFalse()
        {
            var map = new BiDictionary<int, string>();
            map.Add(1, "alice");

            map.Remove(2).ShouldBeFalse();
            map.Remove(1).ShouldBeTrue();
            map.Remove(1).ShouldBeFalse();   // already gone
        }

        [Fact]
        public void RemoveRight_DropsBothAxes_AndFreesTheLeftValue()
        {
            var map = new BiDictionary<int, string>();
            map.Add(1, "alice");

            map.RemoveRight("alice").ShouldBeTrue();

            map.Count.ShouldBe(0);
            map.ContainsKey(1).ShouldBeFalse();
            map.ContainsRight("alice").ShouldBeFalse();

            map.TryAdd(1, "carol").ShouldBeTrue();
            map[1].ShouldBe("carol");
        }

        [Fact]
        public void RemoveRight_MissingRight_ReturnsFalse()
        {
            var map = new BiDictionary<int, string>();
            map.Add(1, "alice");

            map.RemoveRight("bob").ShouldBeFalse();
            map.Count.ShouldBe(1);
        }

        [Fact]
        public void NullLeft_IsAccepted_ThroughItsDedicatedBucket()
        {
            var map = new BiDictionary<int?, string>();

            map.Add(1, "alice");
            map.Add(null, "nobody");

            map.Count.ShouldBe(2);
            map.ContainsKey(null).ShouldBeTrue();
            map[null].ShouldBe("nobody");
            map.TryGetValue(null, out var value).ShouldBeTrue();
            value.ShouldBe("nobody");
            map.Keys.ShouldContain((int?)null);
            map.ContainsRight("nobody").ShouldBeTrue();
        }

        [Fact]
        public void NullRight_IsAccepted_ThroughItsDedicatedBucket()
        {
            var map = new BiDictionary<int, string>();

            map.Add(1, "alice");
            map.Add(2, null);

            map.Count.ShouldBe(2);
            map.ContainsRight(null).ShouldBeTrue();
            map[2].ShouldBe(null);
            map.GetLeft(null).ShouldBe(2);
            map.Values.ShouldContain((string)null);
        }

        [Fact]
        public void NullBoth_IsExpressible_AsASingleEntry()
        {
            var map = new BiDictionary<string, string>();

            map.Add(null, null);

            map.Count.ShouldBe(1);
            map.ContainsKey(null).ShouldBeTrue();
            map.ContainsRight(null).ShouldBeTrue();
            map[null].ShouldBe(null);
            map.GetLeft(null).ShouldBe(null);

            map.Remove(null).ShouldBeTrue();
            map.Count.ShouldBe(0);
            map.ContainsKey(null).ShouldBeFalse();
            map.ContainsRight(null).ShouldBeFalse();
            map.Remove(null).ShouldBeFalse();
        }

        [Fact]
        public void NullLeft_SecondBinding_Throws()
        {
            var map = new BiDictionary<string, string>();
            map.Add(null, "nobody");

            Should.Throw<ArgumentException>(() => map.Add(null, "other"));
            map.Count.ShouldBe(1);
        }

        [Fact]
        public void NullRight_SecondBinding_Throws()
        {
            var map = new BiDictionary<int, string>();
            map.Add(1, null);

            Should.Throw<ArgumentException>(() => map.Add(2, null));
            map.Count.ShouldBe(1);
        }

        [Fact]
        public void NullRight_RebindsAfterRemove()
        {
            var map = new BiDictionary<int, string>();
            map.Add(1, null);

            map.Remove(1).ShouldBeTrue();
            Should.NotThrow(() => map.Add(2, null));
            map.GetLeft(null).ShouldBe(2);
        }

        [Fact]
        public void MissingLookups_ThrowKeyNotFoundException_OnBothSurfaces()
        {
            var map = new BiDictionary<int, string>();
            map.Add(1, "alice");

            Should.Throw<KeyNotFoundException>(() => map[2]);
            Should.Throw<KeyNotFoundException>(() => map.GetLeft("bob"));

            var reverse = map.AsReverse();
            Should.Throw<KeyNotFoundException>(() => reverse["bob"]);
        }

        [Fact]
        public void ReverseView_AnswersEverythingIn_Oof1_FromTheOwnerState()
        {
            var map = new BiDictionary<int, string>();
            map.Add(1, "alice");
            map.Add(2, "bob");

            var reverse = map.AsReverse();

            reverse.Count.ShouldBe(2);
            reverse["alice"].ShouldBe(1);
            reverse["bob"].ShouldBe(2);
            reverse.ContainsKey("alice").ShouldBeTrue();
            reverse.ContainsKey("carol").ShouldBeFalse();
            reverse.TryGetValue("bob", out var left).ShouldBeTrue();
            left.ShouldBe(2);
            reverse.TryGetValue("carol", out _).ShouldBeFalse();

            reverse.Keys.ShouldBe(map.Values, ignoreOrder: true);
            reverse.Values.ShouldBe(map.Keys, ignoreOrder: true);

            var pairs = reverse.ToList();
            pairs.Count.ShouldBe(2);
            pairs.ShouldContain(new KeyValuePair<string, int>("alice", 1));
            pairs.ShouldContain(new KeyValuePair<string, int>("bob", 2));
        }

        [Fact]
        public void ReverseView_IsLive_ReflectsSubsequentChanges()
        {
            var map = new BiDictionary<int, string>();
            map.Add(1, "alice");

            var reverse = map.AsReverse();

            map.Add(2, "bob");
            reverse.Count.ShouldBe(2);
            reverse.ContainsKey("bob").ShouldBeTrue();

            map.Remove(1);
            reverse.Count.ShouldBe(1);
            reverse.ContainsKey("alice").ShouldBeFalse();
        }

        [Fact]
        public void Enumeration_CoversEveryBinding_IncludingTheNullLeftEntry()
        {
            var map = new BiDictionary<int?, string>();
            map.Add(1, "alice");
            map.Add(null, "nobody");
            map.Add(2, null);

            var pairs = map.ToList();

            pairs.Count.ShouldBe(3);
            pairs.ShouldContain(new KeyValuePair<int?, string>(1, "alice"));
            pairs.ShouldContain(new KeyValuePair<int?, string>(null, "nobody"));
            pairs.ShouldContain(new KeyValuePair<int?, string>(2, null));

            // Forward and reverse agree on every entry: the indexes are kept in step.
            foreach (var pair in pairs)
            {
                map.GetLeft(pair.Value!).ShouldBe(pair.Key);
            }
        }

        [Fact]
        public void Keys_AndValues_ProjectBothSides()
        {
            var map = new BiDictionary<int, string>();
            map.Add(1, "alice");
            map.Add(2, "bob");

            map.Keys.OrderBy(k => k).ShouldBe(new[] { 1, 2 });
            map.Values.OrderBy(v => v).ShouldBe(new[] { "alice", "bob" });
        }

        [Fact]
        public void ToDictionary_IsAnIndependentSnapshot()
        {
            var map = new BiDictionary<int, string>();
            map.Add(1, "alice");
            map.Add(2, "bob");

            var snapshot = map.ToDictionary();

            snapshot.Count.ShouldBe(2);
            snapshot[1].ShouldBe("alice");
            snapshot[2].ShouldBe("bob");

            // Mutating the source does not move the snapshot.
            map.Add(3, "carol");
            map.Remove(1);
            snapshot.Count.ShouldBe(2);
            snapshot.ContainsKey(1).ShouldBeTrue();
            snapshot.ContainsKey(3).ShouldBeFalse();
        }

        [Fact]
        public void ToDictionary_OmitsTheNullLeftBinding_AsDocumented()
        {
            var map = new BiDictionary<string, int>();
            map.Add("a", 1);
            map.Add(null, 2);

            var snapshot = map.ToDictionary();

            snapshot.Count.ShouldBe(1);
            snapshot.ContainsKey("a").ShouldBeTrue();
            // A Dictionary<string, int> can not even be asked for a null key (it throws),
            // so the omission is asserted by counting null keys in the exported key set.
            snapshot.Keys.Count(k => k == null).ShouldBe(0);

            // The source still holds both bindings.
            map.Count.ShouldBe(2);
            map[null!].ShouldBe(2);
        }

        [Fact]
        public void Clone_IsIndependent_OfItsSource()
        {
            var map = new BiDictionary<int?, string>();
            map.Add(1, "alice");
            map.Add(null, "nobody");

            var clone = map.Clone();

            clone.Count.ShouldBe(2);
            map.Add(2, "bob");
            clone.Count.ShouldBe(2);
            clone.ContainsKey(2).ShouldBeFalse();

            clone.Remove(1);
            map.ContainsKey(1).ShouldBeTrue();
        }

        [Fact]
        public void Clear_ResetsEverything_IncludingTheNullBuckets()
        {
            var map = new BiDictionary<string, string>();
            map.Add("a", "1");
            map.Add(null, "2");
            map.Add("b", null);

            map.Clear();

            map.Count.ShouldBe(0);
            map.IsEmpty.ShouldBeTrue();
            map.ContainsKey(null).ShouldBeFalse();
            map.ContainsRight(null).ShouldBeFalse();
            map.Keys.ShouldBeEmpty();
            map.Values.ShouldBeEmpty();

            map.TryAdd(null, null).ShouldBeTrue();
            map.Count.ShouldBe(1);
        }

        [Fact]
        public void CustomComparers_AreHonouredOnBothSides()
        {
            var map = new BiDictionary<string, string>(
                StringComparer.OrdinalIgnoreCase,
                StringComparer.OrdinalIgnoreCase);

            map.Add("Alice", "Wonderland");

            // Left comparer: "alice" is the same left value, so TryAdd reports false and Add throws.
            map.ContainsKey("alice").ShouldBeTrue();
            map.TryAdd("alice", "other").ShouldBeFalse();
            Should.Throw<ArgumentException>(() => map.Add("alice", "other"));

            // Right comparer: "WONDERLAND" is the same right value, bound to a different left.
            map.ContainsRight("WONDERLAND").ShouldBeTrue();
            map.TryAdd("other", "WONDERLAND").ShouldBeFalse();

            map.GetLeft("wonderland").ShouldBe("Alice");
        }

        [Fact]
        public void RandomizedOperations_StayInStepWithTwoDictionaryModels()
        {
            // Differential test: the forward index must always equal a Dictionary<TLeft,TRight>
            // model and the reverse index a Dictionary<TRight,TLeft> model, whatever the
            // operation mix. Non-null value domain here; the null buckets are covered above.
            var random = new Random(20260912);
            var map = new BiDictionary<int, int>();
            var forward = new Dictionary<int, int>();
            var reverse = new Dictionary<int, int>();

            for (var step = 0; step < 2000; step++)
            {
                var left = random.Next(30);
                var right = random.Next(30);

                switch (random.Next(4))
                {
                    case 0:
                    {
                        var free = !forward.ContainsKey(left) && !reverse.ContainsKey(right);
                        map.TryAdd(left, right).ShouldBe(free);
                        if (free)
                        {
                            forward.Add(left, right);
                            reverse.Add(right, left);
                        }

                        break;
                    }

                    case 1:
                    {
                        var conflict = forward.ContainsKey(left) || reverse.ContainsKey(right);
                        if (conflict)
                        {
                            Should.Throw<ArgumentException>(() => map.Add(left, right));
                        }
                        else
                        {
                            map.Add(left, right);
                            forward.Add(left, right);
                            reverse.Add(right, left);
                        }

                        break;
                    }

                    case 2:
                    {
                        var had = forward.ContainsKey(left);
                        map.Remove(left).ShouldBe(had);
                        if (had)
                        {
                            reverse.Remove(forward[left]);
                            forward.Remove(left);
                        }

                        break;
                    }

                    default:
                    {
                        var had = reverse.ContainsKey(right);
                        map.RemoveRight(right).ShouldBe(had);
                        if (had)
                        {
                            forward.Remove(reverse[right]);
                            reverse.Remove(right);
                        }

                        break;
                    }
                }

                map.Count.ShouldBe(forward.Count);
            }

            // Full end-state agreement in both directions.
            map.Count.ShouldBe(forward.Count);
            foreach (var pair in forward)
            {
                map.ContainsKey(pair.Key).ShouldBeTrue();
                map[pair.Key].ShouldBe(pair.Value);
                map.GetLeft(pair.Value).ShouldBe(pair.Key);
                map.ContainsRight(pair.Value).ShouldBeTrue();
            }

            foreach (var pair in reverse)
            {
                map.TryGetLeft(pair.Key, out var left).ShouldBeTrue();
                left.ShouldBe(pair.Value);
            }
        }

        [Fact]
        public void ToString_FormIsLeftColonRightPairs()
        {
            var map = new BiDictionary<int, string>();
            map.Add(1, "alice");
            map.Add(2, "bob");

            map.ToString().ShouldBe("1:alice,2:bob");
        }
    }
}
