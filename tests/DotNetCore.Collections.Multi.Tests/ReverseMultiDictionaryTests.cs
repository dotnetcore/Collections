using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// F6-04: <see cref="ReverseMultiDictionary{V,K}"/> and <see cref="MultiDictionary{TKey,TValue}.AsReverse"/>.
    /// Covers the R3-02 split (self-contained snapshot vs live view served from the private
    /// backwards index), the dedicated null-value bucket, the mirrored "no empty inner
    /// collection" invariant, the multiplicity collapse and the axis-swapped member naming.
    /// </summary>
    public class ReverseMultiDictionaryTests
    {
        private static MultiDictionary<int, string> CreateSource()
        {
            var map = new MultiDictionary<int, string>();
            map.Add(1, "orders");
            map.Add(2, "orders");
            map.Add(2, "customers");
            map.Add(3, "invoices");
            return map;
        }

        // ------------------------------------------------------------------
        // Construction and snapshot semantics
        // ------------------------------------------------------------------

        [Fact]
        public void Ctor_FromSource_InvertsValueToKeys()
        {
            var inverted = new ReverseMultiDictionary<string, int>(CreateSource());

            inverted.Count.ShouldBe(3);
            inverted[("orders")].ShouldBe(new[] { 1, 2 }, ignoreOrder: true);
            inverted["customers"].ShouldBe(new[] { 2 }, ignoreOrder: true);
            inverted["invoices"].ShouldBe(new[] { 3 }, ignoreOrder: true);
        }

        [Fact]
        public void Ctor_NullSource_ThrowsArgumentNullException()
        {
            Should.Throw<ArgumentNullException>(
                () => new ReverseMultiDictionary<string, int>((MultiDictionary<int, string>)null!));
        }

        [Fact]
        public void Ctor_EmptySource_YieldsEmptySnapshot()
        {
            var inverted = new ReverseMultiDictionary<string, int>(new MultiDictionary<int, string>());

            inverted.Count.ShouldBe(0);
            inverted.ValueCount.ShouldBe(0);
            inverted.TotalKeyCount.ShouldBe(0);
            inverted["anything"].ShouldBeEmpty();
            inverted.ContainsValue("anything").ShouldBeFalse();
            inverted.ContainsValue(null).ShouldBeFalse();
        }

        [Fact]
        public void Ctor_FromSource_IsIndependentOfSource()
        {
            var source = CreateSource();
            var inverted = new ReverseMultiDictionary<string, int>(source);

            // Mutating the source must not leak into the snapshot (no reference chain).
            source.Add(1, "orders");
            source.Add(4, "orders");
            source.Remove(2, "customers");

            inverted["orders"].ShouldBe(new[] { 1, 2 }, ignoreOrder: true);
            inverted["customers"].ShouldBe(new[] { 2 }, ignoreOrder: true);
            inverted.ContainsValue("orders").ShouldBeTrue();

            // And mutating the snapshot must not leak back into the source.
            inverted.Remove("orders", 1);

            source.Contains(1, "orders").ShouldBeTrue();
            source.Contains(4, "orders").ShouldBeTrue();
        }

        [Fact]
        public void Ctor_FromSource_CollapsesMultiplicities()
        {
            var source = new MultiDictionary<int, string>();   // duplicating inner collection
            source.Add(1, "tag");
            source.Add(1, "tag");
            source.Add(1, "tag");

            var inverted = new ReverseMultiDictionary<string, int>(source);

            inverted["tag"].ShouldBe(new[] { 1 }, ignoreOrder: true);
            inverted.TotalKeyCount.ShouldBe(1);
            inverted.KeyCount("tag").ShouldBe(1);
        }

        [Fact]
        public void Ctor_FromSource_DefaultsToTheSourceKeyComparer()
        {
            // The source maps string keys to ints, so its own comparer governs the string axis
            // - and the snapshot inherits it for the stored keys.
            var source = new MultiDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            source.Add("KEY", 1);

            var inverted = new ReverseMultiDictionary<int, string>(source);

            // "key" and the stored "KEY" are one membership under the inherited comparer.
            inverted.Add(9, "key");
            inverted[9].ShouldBe(new[] { "key" }, ignoreOrder: true);
            inverted.Contains(9, "KEY").ShouldBeTrue();
            inverted.Comparer.ShouldBe(StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void ValueCount_IsAnAliasOfCount()
        {
            var inverted = new ReverseMultiDictionary<string, int>(CreateSource());
            inverted.ValueCount.ShouldBe(inverted.Count);
        }

        // ------------------------------------------------------------------
        // Manual building: Add / AddRange
        // ------------------------------------------------------------------

        [Fact]
        public void Add_BuildsTheIndexByHand()
        {
            var inverted = new ReverseMultiDictionary<string, int>();
            inverted.Add("tag", 1);
            inverted.Add("tag", 2);

            inverted.Count.ShouldBe(1);
            inverted.TotalKeyCount.ShouldBe(2);
            inverted["tag"].ShouldBe(new[] { 1, 2 }, ignoreOrder: true);
        }

        [Fact]
        public void Add_NullKey_ThrowsArgumentNullException()
        {
            // K = string so that a null key is expressible at all.
            var inverted = new ReverseMultiDictionary<int, string>();

            Should.Throw<ArgumentNullException>(() => inverted.Add(1, null!));
            inverted.Count.ShouldBe(0);
        }

        [Fact]
        public void Add_DuplicateMembership_IsSilentlyIgnored()
        {
            var inverted = new ReverseMultiDictionary<string, int>();
            inverted.Add("tag", 1);

            inverted.Add("tag", 1);

            inverted["tag"].ShouldBe(new[] { 1 }, ignoreOrder: true);
            inverted.TotalKeyCount.ShouldBe(1);
        }

        [Fact]
        public void Add_NullValue_GoesIntoTheDedicatedBucket()
        {
            var inverted = new ReverseMultiDictionary<string, int>();
            inverted.Add(null, 1);
            inverted.Add(null, 2);
            inverted.Add("real", 3);

            inverted.ContainsValue(null).ShouldBeTrue();
            inverted[null].ShouldBe(new[] { 1, 2 }, ignoreOrder: true);
            inverted.KeyCount(null).ShouldBe(2);
            inverted.Count.ShouldBe(2);                       // the null bucket counts as one value
            inverted.TotalKeyCount.ShouldBe(3);
            inverted.Values.ShouldContain((string)null);      // null enumerates last
            inverted.Values.Last().ShouldBeNull();
        }

        [Fact]
        public void AddRange_AddsMultipleKeys_UnderOneValue()
        {
            var inverted = new ReverseMultiDictionary<string, int>();

            inverted.AddRange("tag", new[] { 1, 2, 3 });

            inverted["tag"].ShouldBe(new[] { 1, 2, 3 }, ignoreOrder: true);
            inverted.TotalKeyCount.ShouldBe(3);
        }

        [Fact]
        public void AddRange_NullArgument_ThrowsArgumentNullException()
        {
            var inverted = new ReverseMultiDictionary<string, int>();
            Should.Throw<ArgumentNullException>(() => inverted.AddRange("tag", null!));
        }

        // ------------------------------------------------------------------
        // Queries
        // ------------------------------------------------------------------

        [Fact]
        public void Indexer_MissingValue_ReturnsEmptyCollection_NeverNull()
        {
            var inverted = new ReverseMultiDictionary<string, int>(CreateSource());

            var keys = inverted["nope"];
            keys.ShouldNotBeNull();
            keys.ShouldBeEmpty();
        }

        [Fact]
        public void KeyCount_PerValue_AndMissing()
        {
            var inverted = new ReverseMultiDictionary<string, int>(CreateSource());

            inverted.KeyCount("orders").ShouldBe(2);
            inverted.KeyCount("customers").ShouldBe(1);
            inverted.KeyCount("nope").ShouldBe(0);
        }

        [Fact]
        public void TotalKeyCount_TracksEveryMutation()
        {
            var inverted = new ReverseMultiDictionary<string, int>();
            inverted.TotalKeyCount.ShouldBe(0);

            inverted.Add("a", 1);
            inverted.Add("a", 2);
            inverted.Add("b", 1);
            inverted.TotalKeyCount.ShouldBe(3);

            inverted.Remove("a", 1);
            inverted.TotalKeyCount.ShouldBe(2);

            inverted.Remove("a");      // drops both remaining semantics: value "a" holds 1 key
            inverted.TotalKeyCount.ShouldBe(1);

            inverted.Clear();
            inverted.TotalKeyCount.ShouldBe(0);
        }

        [Fact]
        public void Contains_BindingCheck_IncludingNullValue()
        {
            var inverted = new ReverseMultiDictionary<string, int>();
            inverted.Add("tag", 1);
            inverted.Add(null, 2);

            inverted.Contains("tag", 1).ShouldBeTrue();
            inverted.Contains("tag", 2).ShouldBeFalse();
            inverted.Contains("nope", 1).ShouldBeFalse();
            inverted.Contains(null, 2).ShouldBeTrue();
            inverted.Contains(null, 1).ShouldBeFalse();
        }

        [Fact]
        public void ContainsValue_IsTheO1PresenceCheckOfTheValueAxis()
        {
            var inverted = new ReverseMultiDictionary<string, int>(CreateSource());

            inverted.ContainsValue("orders").ShouldBeTrue();
            inverted.ContainsValue("customers").ShouldBeTrue();
            inverted.ContainsValue("nope").ShouldBeFalse();
        }

        [Fact]
        public void ContainsKey_ScansTheStoredKeyAxis()
        {
            var inverted = new ReverseMultiDictionary<string, int>(CreateSource());

            // Key 2 is stored under two values; key 9 under none.
            inverted.ContainsKey(2).ShouldBeTrue();
            inverted.ContainsKey(1).ShouldBeTrue();
            inverted.ContainsKey(9).ShouldBeFalse();
        }

        [Fact]
        public void TryGetValue_HitAndMiss()
        {
            var inverted = new ReverseMultiDictionary<string, int>(CreateSource());

            inverted.TryGetValue("orders", out var keys).ShouldBeTrue();
            keys.ShouldBe(new[] { 1, 2 }, ignoreOrder: true);

            inverted.TryGetValue("nope", out var missing).ShouldBeFalse();
            missing.ShouldBeNull();
        }

        [Fact]
        public void Values_AndKeys_EnumerateTheAxes()
        {
            var inverted = new ReverseMultiDictionary<string, int>(CreateSource());

            inverted.Values.ShouldBe(new[] { "orders", "customers", "invoices" }, ignoreOrder: true);
            inverted.Keys.ShouldBe(new[] { 1, 2, 2, 3 }, ignoreOrder: true);   // one entry per binding
        }

        [Fact]
        public void IReadOnlyDictionary_Interface_Surface()
        {
            IReadOnlyDictionary<string, IReadOnlyCollection<int>> inverted =
                new ReverseMultiDictionary<string, int>(CreateSource());

            inverted.Count.ShouldBe(3);
            inverted.ContainsKey("orders").ShouldBeTrue();
            inverted.ContainsKey("nope").ShouldBeFalse();
            inverted["orders"].ShouldBe(new[] { 1, 2 }, ignoreOrder: true);
            inverted.Keys.ShouldBe(new[] { "orders", "customers", "invoices" }, ignoreOrder: true);
            inverted.Values.SelectMany(v => v).ShouldBe(new[] { 1, 2, 2, 3 }, ignoreOrder: true);

            var pairs = inverted.ToList();
            pairs.Count.ShouldBe(3);
        }

        [Fact]
        public void CustomKeyComparer_IsAppliedToTheStoredKeys()
        {
            // V = int (the indexed value axis), K = string (the stored keys) - the comparer
            // governs the stored key axis.
            var inverted = new ReverseMultiDictionary<int, string>(StringComparer.OrdinalIgnoreCase);
            inverted.Add(1, "ABC");
            inverted.Add(1, "abc");   // the same key under the comparer - deduplicated

            inverted[1].ShouldBe(new[] { "ABC" }, ignoreOrder: true);
            inverted.KeyCount(1).ShouldBe(1);
            inverted.Contains(1, "abc").ShouldBeTrue();
            inverted.ContainsKey("abc").ShouldBeTrue();
        }

        [Fact]
        public void ValueEquality_UsesTheDefaultComparer()
        {
            var inverted = new ReverseMultiDictionary<string, int>();
            inverted.Add(new string('a', 3), 1);   // a distinct string instance...
            inverted.Add("aaa", 2);                // ...equal to "aaa" under the default comparer

            inverted.Count.ShouldBe(1);
            inverted["aaa"].ShouldBe(new[] { 1, 2 }, ignoreOrder: true);
        }

        // ------------------------------------------------------------------
        // Removal: the mirrored "no empty inner collection" invariant
        // ------------------------------------------------------------------

        [Fact]
        public void Remove_SingleBinding_LastKeyDropsTheValue()
        {
            var inverted = new ReverseMultiDictionary<string, int>();
            inverted.Add("tag", 1);
            inverted.Add("tag", 2);

            inverted.Remove("tag", 1).ShouldBeTrue();

            inverted.ContainsValue("tag").ShouldBeTrue();
            inverted.Count.ShouldBe(1);

            inverted.Remove("tag", 2).ShouldBeTrue();

            // The invariant mirrored from MultiDictionary: the value disappears on its own
            // once its last key is gone.
            inverted.ContainsValue("tag").ShouldBeFalse();
            inverted.Count.ShouldBe(0);
            inverted["tag"].ShouldBeEmpty();
        }

        [Fact]
        public void Remove_SingleBinding_Missing_ReturnsFalse()
        {
            var inverted = new ReverseMultiDictionary<string, int>();
            inverted.Add("tag", 1);

            inverted.Remove("tag", 2).ShouldBeFalse();
            inverted.Remove("nope", 1).ShouldBeFalse();
            inverted.TotalKeyCount.ShouldBe(1);
        }

        [Fact]
        public void Remove_WholeValue_RemovesAllItsKeys()
        {
            var inverted = new ReverseMultiDictionary<string, int>(CreateSource());

            inverted.Remove("orders").ShouldBeTrue();

            inverted["orders"].ShouldBeEmpty();
            inverted.ContainsValue("orders").ShouldBeFalse();
            inverted.ContainsKey(1).ShouldBeFalse();     // key 1 stored only "orders"
            inverted.ContainsKey(2).ShouldBeTrue();      // key 2 also stores "customers"
            inverted.TotalKeyCount.ShouldBe(2);
        }

        [Fact]
        public void Remove_MissingValue_ReturnsFalse()
        {
            var inverted = new ReverseMultiDictionary<string, int>();
            inverted.Add("tag", 1);

            inverted.Remove("nope").ShouldBeFalse();
            inverted.Count.ShouldBe(1);
        }

        [Fact]
        public void Remove_NullValueBindings_BucketAutoDrops()
        {
            var inverted = new ReverseMultiDictionary<string, int>();
            inverted.Add(null, 1);
            inverted.Add(null, 2);

            inverted.Remove(null, 1).ShouldBeTrue();
            inverted.ContainsValue(null).ShouldBeTrue();     // one key left
            inverted.Count.ShouldBe(1);

            inverted.Remove(null, 2).ShouldBeTrue();

            // The bucket itself disappears once empty - exactly like a regular entry.
            inverted.ContainsValue(null).ShouldBeFalse();
            inverted.Count.ShouldBe(0);
            inverted.TotalKeyCount.ShouldBe(0);
        }

        [Fact]
        public void Remove_NullValue_Missing_ReturnsFalse()
        {
            var inverted = new ReverseMultiDictionary<string, int>();
            inverted.Add("tag", 1);

            inverted.Remove(null).ShouldBeFalse();
            inverted.Remove(null, 1).ShouldBeFalse();
        }

        [Fact]
        public void RemoveRange_RemovesListedKeys_AndReports()
        {
            var inverted = new ReverseMultiDictionary<string, int>();
            inverted.AddRange("tag", new[] { 1, 2, 3 });

            inverted.RemoveRange("tag", new[] { 1, 3 }).ShouldBeTrue();
            inverted["tag"].ShouldBe(new[] { 2 }, ignoreOrder: true);

            inverted.RemoveRange("tag", new[] { 9 }).ShouldBeFalse();
            inverted.RemoveRange("nope", new[] { 1 }).ShouldBeFalse();
        }

        [Fact]
        public void RemoveRange_NullArgument_ThrowsArgumentNullException()
        {
            var inverted = new ReverseMultiDictionary<string, int>();
            Should.Throw<ArgumentNullException>(() => inverted.RemoveRange("tag", null!));
        }

        [Fact]
        public void Clear_RemovesEverything()
        {
            var inverted = new ReverseMultiDictionary<string, int>();
            inverted.Add("a", 1);
            inverted.Add(null, 2);

            inverted.Clear();

            inverted.Count.ShouldBe(0);
            inverted.TotalKeyCount.ShouldBe(0);
            inverted.ContainsValue("a").ShouldBeFalse();
            inverted.ContainsValue(null).ShouldBeFalse();
        }

        [Fact]
        public void Clone_IsIndependent()
        {
            var inverted = new ReverseMultiDictionary<string, int>(CreateSource());

            var clone = inverted.Clone();
            clone.Remove("orders");

            inverted.ContainsValue("orders").ShouldBeTrue();
            clone.ContainsValue("orders").ShouldBeFalse();
        }

        [Fact]
        public void ToString_RendersPerValueEntries_AndTheNullBucket()
        {
            var inverted = new ReverseMultiDictionary<string, int>();
            inverted.Add("tag", 1);
            inverted.Add(null, 2);

            var text = inverted.ToString();

            text.ShouldContain("tag:[1]");
            text.ShouldContain("null:[2]");
        }

        // ------------------------------------------------------------------
        // MultiDictionary.AsReverse() - the live view
        // ------------------------------------------------------------------

        [Fact]
        public void AsReverse_InvertsValueToKeys()
        {
            IReadOnlyDictionary<string, IReadOnlyCollection<int>> view = CreateSource().AsReverse();

            view.Count.ShouldBe(3);
            view["orders"].ShouldBe(new[] { 1, 2 }, ignoreOrder: true);
            view["customers"].ShouldBe(new[] { 2 }, ignoreOrder: true);
        }

        [Fact]
        public void AsReverse_ReflectsMutationsImmediately()
        {
            var source = CreateSource();
            var view = source.AsReverse();

            source.Add(4, "orders");
            view["orders"].ShouldBe(new[] { 1, 2, 4 }, ignoreOrder: true);
            view.Count.ShouldBe(3);

            source.Remove(2);                       // drops the whole key 2
            view["orders"].ShouldBe(new[] { 1, 4 }, ignoreOrder: true);
            view["customers"].ShouldBeEmpty();      // key 2 was its only holder
            view.Count.ShouldBe(2);

            source.Clear();
            view.Count.ShouldBe(0);
            view.ContainsKey("orders").ShouldBeFalse();
        }

        [Fact]
        public void AsReverse_MissingValue_YieldsEmptyCollection_NeverThrows()
        {
            var view = CreateSource().AsReverse();

            var keys = view["nope"];
            keys.ShouldNotBeNull();
            keys.ShouldBeEmpty();
            view.ContainsKey("nope").ShouldBeFalse();
            view.TryGetValue("nope", out _).ShouldBeFalse();
        }

        [Fact]
        public void AsReverse_NullValue_SurfacesAsNullEntry()
        {
            var source = new MultiDictionary<int, string>();
            source.Add(1, null);
            source.Add(2, null);
            source.Add(3, "real");

            var view = source.AsReverse();

            view.Count.ShouldBe(2);                          // "real" + the null entry
            view.ContainsKey(null).ShouldBeTrue();
            view[null].ShouldBe(new[] { 1, 2 }, ignoreOrder: true);
            view.Keys.Last().ShouldBeNull();                 // the null entry enumerates last
        }

        [Fact]
        public void AsReverse_CollapsesMultiplicities()
        {
            var source = new MultiDictionary<int, string>();
            source.Add(1, "tag");
            source.Add(1, "tag");

            source.AsReverse()["tag"].ShouldBe(new[] { 1 }, ignoreOrder: true);
        }

        [Fact]
        public void AsReverse_TryGetValue_HitAndMiss()
        {
            var view = CreateSource().AsReverse();

            view.TryGetValue("orders", out var keys).ShouldBeTrue();
            keys.ShouldBe(new[] { 1, 2 }, ignoreOrder: true);
            view.TryGetValue("nope", out var missing).ShouldBeFalse();
            missing.ShouldBeNull();
        }

        [Fact]
        public void AsReverse_UsesTheSourceKeyComparerForTheStoredKeys()
        {
            // The sets behind the view compare with the source's own key comparer: "KEY" and
            // "key" are one membership under the OrdinalIgnoreCase comparer, so the second Add
            // folds into the first form and the set stays a singleton.
            var source = new MultiDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            source.Add("KEY", 1);
            source.Add("key", 1);

            var view = source.AsReverse();

            view.Count.ShouldBe(1);
            view[1].ShouldBe(new[] { "KEY" }, ignoreOrder: true);
        }

        [Fact]
        public void AsReverse_AgreesWithTheSnapshot()
        {
            var source = CreateSource();

            // R3-02: the live view and the snapshot agree at any point in time.
            var snapshot = new ReverseMultiDictionary<string, int>(source);
            var view = source.AsReverse();

            view.Count.ShouldBe(snapshot.Count);
            view.Keys.ShouldBe(snapshot.Values, ignoreOrder: true);
            var snapshotView = (IReadOnlyDictionary<string, IReadOnlyCollection<int>>)snapshot;
            foreach (var pair in snapshotView)
            {
                view[pair.Key].ShouldBe(pair.Value.ToList(), ignoreOrder: true);
            }
        }

        // ------------------------------------------------------------------
        // Random differential test
        // ------------------------------------------------------------------

        [Fact]
        public void RandomOps_AllViewsAgreeWithTheSourceProjection()
        {
            // V = string (null values exercised), K = int. Both the lockstep-maintained
            // snapshot and the live view must equal a freshly recomputed projection of the
            // source after every step, and a freshly built snapshot must equal them too.
            var source = new MultiDictionary<int, string>();
            var reverse = new ReverseMultiDictionary<string, int>();
            var random = new Random(20260914);

            for (var step = 0; step < 2000; step++)
            {
                var key = random.Next(12);
                var value = random.Next(5) == 4 ? null : ((char)('a' + random.Next(4))).ToString();

                switch (random.Next(5))
                {
                    case 0:
                    case 1: // bind (k, v)
                        source.Add(key, value);
                        reverse.Add(value, key);
                        break;
                    case 2: // unbind the pair: every copy of v under k must go
                        source.ExceptWith(key, new[] { value });
                        reverse.Remove(value, key);
                        break;
                    case 3: // unbind the whole key
                        foreach (var storedValue in source[key].ToList())
                        {
                            reverse.Remove(storedValue, key);
                        }

                        source.Remove(key);
                        break;
                    case 4: // unbind the whole value
                        foreach (var storedKey in reverse[value].ToList())
                        {
                            source.ExceptWith(storedKey, new[] { value });
                        }

                        reverse.Remove(value);
                        break;
                }

                if (step % 25 == 0)
                {
                    AssertInvertedMatches(source.AsReverse(), source);
                    AssertInvertedMatches(reverse, source);
                    AssertInvertedMatches(new ReverseMultiDictionary<string, int>(source), source);
                }
            }

            AssertInvertedMatches(source.AsReverse(), source);
            AssertInvertedMatches(reverse, source);
            AssertInvertedMatches(new ReverseMultiDictionary<string, int>(source), source);
        }

        /// <summary>
        /// Asserts that an inverted view (the snapshot, the lockstep copy or the live view - all
        /// <see cref="IReadOnlyDictionary{V,IReadOnlyCollection{K}}"/> shapes) equals a freshly
        /// recomputed projection of the source: value &#8594; set of keys, multiplicities
        /// collapsed, the <c>null</c> value as its own entry.
        /// </summary>
        private static void AssertInvertedMatches(
            IReadOnlyDictionary<string, IReadOnlyCollection<int>> inverted,
            MultiDictionary<int, string> source)
        {
            var expected = new Dictionary<string, HashSet<int>>();
            var expectedNull = new HashSet<int>();
            foreach (var pair in source)
            {
                if (pair.Value == null)
                {
                    expectedNull.Add(pair.Key);
                }
                else if (!expected.TryGetValue(pair.Value, out var keys))
                {
                    expected.Add(pair.Value, new HashSet<int> { pair.Key });
                }
                else
                {
                    keys.Add(pair.Key);
                }
            }

            inverted.Count.ShouldBe(expected.Count + (expectedNull.Count > 0 ? 1 : 0));
            inverted.ContainsKey(null).ShouldBe(expectedNull.Count > 0);
            if (expectedNull.Count > 0)
            {
                inverted[null].ShouldBe(expectedNull, ignoreOrder: true);
            }

            foreach (var pair in expected)
            {
                inverted.ContainsKey(pair.Key).ShouldBeTrue();
                inverted[pair.Key].ShouldBe(pair.Value, ignoreOrder: true);
            }

            // And nothing beyond the expected entries.
            foreach (var pair in inverted)
            {
                if (pair.Key == null)
                {
                    pair.Value.ShouldBe(expectedNull, ignoreOrder: true);
                }
                else
                {
                    pair.Value.ShouldBe(expected[pair.Key], ignoreOrder: true);
                }
            }
        }
    }
}
