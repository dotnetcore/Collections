using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    public class MultiDictionaryValueSetOperationsTests
    {
        private static MultiDictionary<string, int> NewDict()
        {
            return new MultiDictionary<string, int>();
        }

        [Fact]
        public void UnionWith_AddsOnlyAbsentValues()
        {
            var dict = NewDict();
            dict.AddRange("a", new[] { 1, 2 });

            dict.UnionWith("a", new[] { 2, 3 });

            dict["a"].OrderBy(x => x).ShouldBe(new[] { 1, 2, 3 });
            dict.TotalValueCount.ShouldBe(3);
        }

        [Fact]
        public void UnionWith_CreatesKey_WhenAbsent()
        {
            var dict = NewDict();

            dict.UnionWith("k", new[] { 1, 1, 2 });

            dict.ContainsKey("k").ShouldBeTrue();
            dict["k"].OrderBy(x => x).ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public void UnionWith_DoesNotDuplicateInputValues()
        {
            var dict = NewDict();
            dict.Add("a", 1);

            dict.UnionWith("a", new[] { 1, 1, 2, 2 });

            dict["a"].OrderBy(x => x).ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public void UnionWith_NullValues_ThrowsArgumentNullException()
        {
            var dict = NewDict();
            Should.Throw<ArgumentNullException>(() => dict.UnionWith("k", null));
        }

        [Fact]
        public void IntersectionWith_KeepsCommonValues()
        {
            var dict = NewDict();
            dict.AddRange("a", new[] { 1, 2, 3 });

            dict.IntersectionWith("a", new[] { 2, 3, 4 });

            dict["a"].OrderBy(x => x).ShouldBe(new[] { 2, 3 });
            dict.ContainsKey("a").ShouldBeTrue();
        }

        [Fact]
        public void IntersectionWith_EmptyResult_RemovesKey()
        {
            var dict = NewDict();
            dict.AddRange("a", new[] { 1, 2 });

            dict.IntersectionWith("a", new[] { 3 });

            dict.ContainsKey("a").ShouldBeFalse();
        }

        [Fact]
        public void IntersectionWith_MissingKey_NoOp()
        {
            var dict = NewDict();

            dict.IntersectionWith("missing", new[] { 1 });

            dict.ContainsKey("missing").ShouldBeFalse();
        }

        [Fact]
        public void ExceptWith_RemovesAllOccurrences()
        {
            var dict = NewDict();
            dict.Add("a", 1);
            dict.Add("a", 1);
            dict.Add("a", 2);

            dict.ExceptWith("a", new[] { 1 });

            dict["a"].ShouldBe(new[] { 2 });
            dict.TotalValueCount.ShouldBe(1);
        }

        [Fact]
        public void ExceptWith_EmptyResult_RemovesKey()
        {
            var dict = NewDict();
            dict.Add("a", 1);

            dict.ExceptWith("a", new[] { 1, 9 });

            dict.ContainsKey("a").ShouldBeFalse();
        }

        [Fact]
        public void ExceptWith_MissingKey_NoOp()
        {
            var dict = NewDict();

            dict.ExceptWith("missing", new[] { 1 });

            dict.ContainsKey("missing").ShouldBeFalse();
        }

        [Fact]
        public void ExceptWith_RemovesNullValue()
        {
            var dict = new MultiDictionary<string, string>();
            dict.Add("k", null);
            dict.Add("k", "v");

            dict.ExceptWith("k", new[] { (string)null });

            dict["k"].ShouldBe(new[] { "v" });
        }

        [Fact]
        public void UnionWith_WithDuplicatingInner_KeepsExistingMultiplicities()
        {
            var dict = new MultiDictionary<string, int>(true);
            dict.Add("a", 1);
            dict.Add("a", 1);
            dict.Add("a", 2);

            dict.UnionWith("a", new[] { 2, 3 });

            dict["a"].Count.ShouldBe(4);
            dict.TotalValueCount.ShouldBe(4);
        }

        // ------------------------------------------------------------------
        // SymmetricExceptWith (M6-02): per key, each distinct value of the argument "toggles" --
        // it cancels one stored occurrence, or is added when none is stored. The argument is a
        // *set*, matching UnionWith / IntersectionWith / ExceptWith on this class; this is
        // deliberately not MultiList<T>.SymmetricExceptWith's multiset convention.
        // ------------------------------------------------------------------

        [Fact]
        public void SymmetricExceptWith_CancelsValuesPresentOnBothSides()
        {
            var dict = NewDict();
            dict.AddRange("a", new[] { 1, 2 });

            dict.SymmetricExceptWith("a", new[] { 2, 3 });

            Sorted(dict["a"]).ShouldBe(new[] { 1, 3 });
        }

        [Fact]
        public void SymmetricExceptWith_CancelsOneStoredOccurrencePerDistinctValue()
        {
            var dict = NewDict();
            dict.AddRange("a", new[] { 1, 1, 1 });

            dict.SymmetricExceptWith("a", new[] { 1 });

            // N stored copies minus one cancelled occurrence.
            Sorted(dict["a"]).ShouldBe(new[] { 1, 1 });
            dict.TotalValueCount.ShouldBe(2);
        }

        [Fact]
        public void SymmetricExceptWith_ArgumentDuplicatesCountOnlyOnce()
        {
            var dict = NewDict();
            dict.Add("a", 1);

            dict.SymmetricExceptWith("a", new[] { 1, 1, 1 });

            // The single stored copy is cancelled and the repeats do not re-add it.
            dict.ContainsKey("a").ShouldBeFalse();
            dict.Count.ShouldBe(0);
        }

        [Fact]
        public void SymmetricExceptWith_KeyDroppedWhenEverythingCancels()
        {
            var dict = NewDict();
            dict.AddRange("a", new[] { 1, 2 });

            dict.SymmetricExceptWith("a", new[] { 1, 2 });

            dict.ContainsKey("a").ShouldBeFalse();
            dict.Count.ShouldBe(0);
        }

        [Fact]
        public void SymmetricExceptWith_KeyCreated_WhenAbsentAndArgumentNonEmpty()
        {
            var dict = NewDict();

            dict.SymmetricExceptWith("a", new[] { 1, 1, 2 });

            dict.ContainsKey("a").ShouldBeTrue();
            Sorted(dict["a"]).ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public void SymmetricExceptWith_EmptyArgument_OnMissingKey_DoesNotCreateKey()
        {
            var dict = NewDict();

            dict.SymmetricExceptWith("a", new int[0]);

            dict.ContainsKey("a").ShouldBeFalse();
            dict.Count.ShouldBe(0);
        }

        [Fact]
        public void SymmetricExceptWith_EmptyArgument_OnExistingKey_KeepsValues()
        {
            var dict = NewDict();
            dict.AddRange("a", new[] { 1, 2 });

            dict.SymmetricExceptWith("a", new int[0]);

            Sorted(dict["a"]).ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public void SymmetricExceptWith_NullArgument_Throws()
        {
            var dict = NewDict();

            Should.Throw<ArgumentNullException>(() => dict.SymmetricExceptWith("a", null));
        }

        [Fact]
        public void SymmetricExceptWith_OtherKeysAreUntouched()
        {
            var dict = NewDict();
            dict.AddRange("a", new[] { 1, 2 });
            dict.AddRange("b", new[] { 9 });

            dict.SymmetricExceptWith("a", new[] { 2 });

            Sorted(dict["a"]).ShouldBe(new[] { 1 });
            Sorted(dict["b"]).ShouldBe(new[] { 9 });
            dict.Count.ShouldBe(2);
        }

        [Fact]
        public void SymmetricExceptWith_NullValuesCancelAndSurviveCorrectly()
        {
            var dict = new MultiDictionary<string, string>();
            dict.Add("k", null);
            dict.Add("k", "v");

            dict.SymmetricExceptWith("k", new[] { (string)null });

            dict["k"].ShouldBe(new[] { "v" });
        }

        [Fact]
        public void SymmetricExceptWith_NullOnlyValuesCancel_RemovesKey()
        {
            var dict = new MultiDictionary<string, string>();
            dict.Add("k", null);

            dict.SymmetricExceptWith("k", new[] { (string)null });

            dict.ContainsKey("k").ShouldBeFalse();
        }

        [Fact]
        public void SymmetricExceptWith_NullValuesComeFromTheArgumentToo()
        {
            var dict = new MultiDictionary<string, string>();
            dict.Add("k", "v");

            dict.SymmetricExceptWith("k", new[] { (string)null });

            var values = dict["k"].ToList();
            values.Count.ShouldBe(2);
            values.ShouldContain("v");
            values.ShouldContain(x => x == null);
        }

        [Fact]
        public void SymmetricExceptWith_KeyComparerIsHonoured()
        {
            var dict = new MultiDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            dict.AddRange("Key", new[] { 1, 2 });

            dict.SymmetricExceptWith("kEY", new[] { 2, 3 });

            Sorted(dict["KEY"]).ShouldBe(new[] { 1, 3 });
            dict.Count.ShouldBe(1);
        }

        [Fact]
        public void SymmetricExceptWith_DeduplicatingInner_IsExactlyISetSymmetricDifference()
        {
            var dict = new MultiDictionary<string, int>(allowDuplicateValues: false);
            dict.AddRange("a", new[] { 1, 1, 2 });

            dict.SymmetricExceptWith("a", new[] { 2, 2, 3 });

            // Oracle: the BCL set operation on the same two sets.
            var expected = new HashSet<int> { 1, 2 };
            expected.SymmetricExceptWith(new[] { 2, 2, 3 });

            Sorted(dict["a"]).ShouldBe(expected.OrderBy(x => x).ToArray());
            Sorted(dict["a"]).ShouldBe(new[] { 1, 3 });
        }

        [Fact]
        public void SymmetricExceptWith_DeduplicatingInner_IsSelfInverse()
        {
            var dict = new MultiDictionary<string, int>(allowDuplicateValues: false);
            dict.AddRange("a", new[] { 1, 2, 3 });
            var other = new[] { 2, 4 };

            dict.SymmetricExceptWith("a", other);
            Sorted(dict["a"]).ShouldBe(new[] { 1, 3, 4 });

            dict.SymmetricExceptWith("a", other);
            Sorted(dict["a"]).ShouldBe(new[] { 1, 2, 3 });
        }

        [Theory]
        [InlineData(new[] { 1, 2 }, new[] { 2, 3 })]
        [InlineData(new[] { 1 }, new[] { 1 })]
        [InlineData(new[] { 1, 1, 1 }, new[] { 1 })]
        [InlineData(new[] { 1 }, new[] { 1, 1, 1 })]
        [InlineData(new int[0], new[] { 1, 2 })]
        [InlineData(new[] { 1, 2, 3 }, new int[0])]
        [InlineData(new[] { 1, 1, 2, 3, 3, 3 }, new[] { 1, 2, 2, 4 })]
        [InlineData(new[] { 5, 5, 5, 5 }, new[] { 5, 5 })]
        [InlineData(new[] { 7, 7 }, new[] { 8, 8 })]
        public void SymmetricExceptWith_MatchesAnIndependentReferenceImplementation(
            int[] here, int[] there)
        {
            var dict = NewDict();
            dict.AddRange("a", here);

            dict.SymmetricExceptWith("a", there);

            var actual = dict.ContainsKey("a") ? dict["a"] : new int[0];
            Sorted(actual).ShouldBe(ReferenceToggle(here, there));
        }

        [Theory]
        [InlineData(new[] { 1, 2 }, new[] { 2, 3 }, new[] { 1, 3 })]
        [InlineData(new[] { 1, 1, 2, 3, 3, 3 }, new[] { 1, 2, 2, 4 }, new[] { 1, 3, 3, 3, 4 })]
        [InlineData(new[] { 1, 1, 1 }, new[] { 1, 1 }, new[] { 1, 1 })]
        public void SymmetricExceptWith_ProducesTheExpectedValues(
            int[] here, int[] there, int[] expected)
        {
            var dict = NewDict();
            dict.AddRange("a", here);

            dict.SymmetricExceptWith("a", there);

            var actual = dict.ContainsKey("a") ? dict["a"] : new int[0];
            Sorted(actual).ShouldBe(Sorted(expected));
            dict.TotalValueCount.ShouldBe(expected.Length);
            dict["a"].Count.ShouldBe(expected.Length);
        }

        [Fact]
        public void SymmetricExceptWith_TreatsTheArgumentAsASet_UnlikeMultiList()
        {
            // Pinned on purpose: this is a real, deliberate divergence between the two types.
            // MultiDictionary treats the argument as a set (like its sibling operations), so a
            // repeated argument value toggles once; MultiList treats it as a multiset and keeps
            // the absolute count difference.
            var dict = NewDict();
            dict.AddRange("a", new[] { 1, 2 });

            dict.SymmetricExceptWith("a", new[] { 2, 2 });

            Sorted(dict["a"]).ShouldBe(new[] { 1 });

            var bag = new MultiList<int>();
            bag.AddRange(new[] { 1, 2 });

            bag.SymmetricExceptWith(new[] { 2, 2 });

            bag.OrderBy(x => x).ShouldBe(new[] { 1, 2 });
        }

        private static int[] Sorted(IEnumerable<int> values)
        {
            return values.OrderBy(x => x).ToArray();
        }

        // Independent oracle: every distinct value of the argument toggles exactly one
        // occurrence -- cancelled when the value is stored, added when it is not -- while values
        // absent from the argument are left alone. The argument is a set, so its duplicates are
        // collapsed before anything is toggled.
        private static int[] ReferenceToggle(IEnumerable<int> here, IEnumerable<int> there)
        {
            var stored = here.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
            var toggles = new HashSet<int>(there);

            var result = new List<int>();
            foreach (var value in stored.Keys.Union(toggles))
            {
                var count = stored.TryGetValue(value, out var c) ? c : 0;
                var target = toggles.Contains(value) ? Math.Abs(count - 1) : count;
                for (var i = 0; i < target; i++)
                {
                    result.Add(value);
                }
            }

            return result.OrderBy(x => x).ToArray();
        }
    }
}
