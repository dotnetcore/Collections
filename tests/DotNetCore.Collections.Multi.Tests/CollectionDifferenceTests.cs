using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// F6-14 (with 45-a): the four-way difference of two bags or two dictionaries. Covers the four
    /// partitions (only-left / only-right / in-common / differing), the bag overload that compares by
    /// copy count, the dictionary overload that compares by mapped value, the comparers, the snapshot
    /// guarantee and the input validation.
    /// </summary>
    public class CollectionDifferenceTests
    {
        // ------------------------------------------------------------------
        // CollectionDifference.Of - the four partitions
        // ------------------------------------------------------------------

        [Fact]
        public void Of_DisjointKeys_LandInTheOnlyPartitions()
        {
            var left = new Dictionary<string, int> { ["a"] = 1 };
            var right = new Dictionary<string, int> { ["b"] = 2 };

            var diff = CollectionDifference<string, int>.Of(left, right);

            diff.OnlyInLeft.Count.ShouldBe(1);
            diff.OnlyInLeft["a"].ShouldBe(1);
            diff.OnlyInRight.Count.ShouldBe(1);
            diff.OnlyInRight["b"].ShouldBe(2);
            diff.InCommon.ShouldBeEmpty();
            diff.Differing.ShouldBeEmpty();
            diff.AreEqual.ShouldBeFalse();
        }

        [Fact]
        public void Of_SharedKeyWithEqualValue_LandsInCommon()
        {
            var left = new Dictionary<string, int> { ["a"] = 7 };
            var right = new Dictionary<string, int> { ["a"] = 7 };

            var diff = CollectionDifference<string, int>.Of(left, right);

            diff.InCommon["a"].ShouldBe(7);
            diff.OnlyInLeft.ShouldBeEmpty();
            diff.OnlyInRight.ShouldBeEmpty();
            diff.Differing.ShouldBeEmpty();
            diff.AreEqual.ShouldBeTrue();
        }

        [Fact]
        public void Of_SharedKeyWithDifferentValue_LandsInDifferingWithBothSides()
        {
            var left = new Dictionary<string, int> { ["a"] = 1 };
            var right = new Dictionary<string, int> { ["a"] = 2 };

            var diff = CollectionDifference<string, int>.Of(left, right);

            diff.Differing["a"].ShouldBe((1, 2));
            diff.AreEqual.ShouldBeFalse();
        }

        [Fact]
        public void Of_EmptyInputs_AreEqual()
        {
            var diff = CollectionDifference<string, int>.Of(
                new Dictionary<string, int>(),
                new Dictionary<string, int>());

            diff.AreEqual.ShouldBeTrue();
            diff.OnlyInLeft.ShouldBeEmpty();
            diff.OnlyInRight.ShouldBeEmpty();
            diff.InCommon.ShouldBeEmpty();
            diff.Differing.ShouldBeEmpty();
        }

        [Fact]
        public void Of_PartitionsAreDisjointAndCoverEveryKey()
        {
            var left = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2, ["c"] = 3 };
            var right = new Dictionary<string, int> { ["a"] = 1, ["b"] = 9, ["d"] = 4 };

            var diff = CollectionDifference<string, int>.Of(left, right);

            var keys = diff.OnlyInLeft.Keys
                .Concat(diff.OnlyInRight.Keys)
                .Concat(diff.InCommon.Keys)
                .Concat(diff.Differing.Keys)
                .ToList();

            keys.Count.ShouldBe(4);                                   // a, b, c, d
            keys.Distinct().Count().ShouldBe(keys.Count);             // disjoint
            keys.ShouldBe(new[] { "a", "b", "c", "d" }, ignoreOrder: true);
        }

        [Fact]
        public void Of_IsASnapshot_UnaffectedByLaterChangesToTheInputs()
        {
            var left = new Dictionary<string, int> { ["a"] = 1 };
            var right = new Dictionary<string, int> { ["b"] = 2 };

            var diff = CollectionDifference<string, int>.Of(left, right);

            left["z"] = 99;
            right.Remove("b");
            right["a"] = 5;

            diff.OnlyInLeft.Keys.ShouldBe(new[] { "a" });
            diff.OnlyInRight.Keys.ShouldBe(new[] { "b" });
        }

        [Fact]
        public void Of_NullLeftOrRight_ThrowsArgumentNullException()
        {
            var map = new Dictionary<string, int>();
            Should.Throw<ArgumentNullException>(
                () => CollectionDifference<string, int>.Of(null!, map));
            Should.Throw<ArgumentNullException>(
                () => CollectionDifference<string, int>.Of(map, null!));
        }

        [Fact]
        public void Of_NullKey_ThrowsArgumentException()
        {
            var left = new[] { new KeyValuePair<string, int>(null!, 1) };
            var right = new Dictionary<string, int>();

            Should.Throw<ArgumentException>(() => CollectionDifference<string, int>.Of(left, right));
        }

        [Fact]
        public void Of_RepeatedKey_ThrowsArgumentException()
        {
            var left = new[]
            {
                new KeyValuePair<string, int>("a", 1),
                new KeyValuePair<string, int>("a", 2),
            };

            Should.Throw<ArgumentException>(
                () => CollectionDifference<string, int>.Of(left, new Dictionary<string, int>()));
        }

        [Fact]
        public void Of_KeyComparer_DrivesKeyIdentity()
        {
            var left = new Dictionary<string, int> { ["A"] = 1 };
            var right = new Dictionary<string, int> { ["a"] = 1 };

            var diff = CollectionDifference<string, int>.Of(left, right, StringComparer.OrdinalIgnoreCase);

            diff.InCommon.Count.ShouldBe(1);
            diff.AreEqual.ShouldBeTrue();
        }

        [Fact]
        public void Of_ValueComparer_DrivesValueEquality()
        {
            var left = new Dictionary<string, string> { ["a"] = "ABC" };
            var right = new Dictionary<string, string> { ["a"] = "abc" };

            var same = CollectionDifference<string, string>.Of(left, right, null, StringComparer.OrdinalIgnoreCase);
            same.InCommon["a"].ShouldBe("ABC");
            same.AreEqual.ShouldBeTrue();

            var different = CollectionDifference<string, string>.Of(left, right);
            different.Differing["a"].ShouldBe(("ABC", "abc"));
        }

        [Fact]
        public void ToString_SummarizesTheFourPartitionSizes()
        {
            var left = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
            var right = new Dictionary<string, int> { ["b"] = 9, ["c"] = 3 };

            var diff = CollectionDifference<string, int>.Of(left, right);

            diff.ToString().ShouldBe("left-only 1, right-only 1, in-common 0, differing 1");
        }

        // ------------------------------------------------------------------
        // Bag overload - compared by copy count
        // ------------------------------------------------------------------

        [Fact]
        public void Difference_Bags_ComparesByCopyCount()
        {
            var left = new MultiList<string>();
            left.Add("a", 3);
            left.Add("b");

            var right = new MultiList<string>();
            right.Add("a", 2);
            right.Add("c");

            var diff = left.Difference(right);

            diff.OnlyInLeft["b"].ShouldBe(1);
            diff.OnlyInRight["c"].ShouldBe(1);
            diff.Differing["a"].ShouldBe((3, 2));
            diff.InCommon.ShouldBeEmpty();
            diff.AreEqual.ShouldBeFalse();
        }

        [Fact]
        public void Difference_Bags_EqualCounts_LandInCommon()
        {
            var left = new MultiList<string>();
            left.Add("a", 2);
            var right = new MultiList<string>();
            right.Add("a", 2);

            var diff = left.Difference(right);

            diff.InCommon["a"].ShouldBe(2);
            diff.AreEqual.ShouldBeTrue();
        }

        [Fact]
        public void Difference_Bags_EmptyAndNonEmpty()
        {
            var left = new MultiList<string>();
            var right = new MultiList<string>();
            right.Add("a", 4);

            var diff = left.Difference(right);

            diff.OnlyInRight["a"].ShouldBe(4);
            diff.OnlyInLeft.ShouldBeEmpty();
            diff.AreEqual.ShouldBeFalse();
        }

        [Fact]
        public void Difference_Bags_ComparerIsHonoured()
        {
            var left = new MultiList<string>();
            left.Add("A", 2);
            var right = new MultiList<string>();
            right.Add("a", 2);

            var diff = left.Difference(right, StringComparer.OrdinalIgnoreCase);

            diff.InCommon.Count.ShouldBe(1);
            diff.AreEqual.ShouldBeTrue();
        }

        [Fact]
        public void Difference_WorksOnAnyMultiSet_NotJustMultiList()
        {
            var left = new OrderedMultiList<string>();
            left.Add("a", 1);
            var right = new OrderedMultiList<string>();
            right.Add("a", 1);

            var diff = left.Difference(right);

            diff.AreEqual.ShouldBeTrue();
            diff.InCommon["a"].ShouldBe(1);
        }

        [Fact]
        public void Difference_Bags_NullLeftOrRight_ThrowsArgumentNullException()
        {
            var bag = new MultiList<string>();
            Should.Throw<ArgumentNullException>(() => bag.Difference(null!));
            Should.Throw<ArgumentNullException>(
                () => CollectionDifferenceExtensions.Difference<string>(null!, bag));
        }

        [Fact]
        public void Difference_Bags_NullElement_ThrowsArgumentException()
        {
            var left = new MultiList<string>();
            left.Add(null!);
            var right = new MultiList<string>();

            Should.Throw<ArgumentException>(() => left.Difference(right));
        }

        // ------------------------------------------------------------------
        // Dictionary overload
        // ------------------------------------------------------------------

        [Fact]
        public void Difference_Dictionaries_ComparesByMappedValue()
        {
            var left = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" };
            var right = new Dictionary<string, string> { ["a"] = "1", ["b"] = "9", ["c"] = "3" };

            var diff = left.Difference(right);

            diff.InCommon["a"].ShouldBe("1");
            diff.Differing["b"].ShouldBe(("2", "9"));
            diff.OnlyInRight["c"].ShouldBe("3");
            diff.AreEqual.ShouldBeFalse();
        }

        [Fact]
        public void Difference_Dictionaries_NullLeftOrRight_ThrowsArgumentNullException()
        {
            var map = new Dictionary<string, int>();
            Should.Throw<ArgumentNullException>(() => map.Difference(null!));
            Should.Throw<ArgumentNullException>(
                () => CollectionDifferenceExtensions.Difference<string, int>(null!, map));
        }

        [Fact]
        public void Difference_MultimapSatisfiesTheDictionaryOverload()
        {
            var left = new MultiDictionary<string, int>();
            left.Add("k", 1);
            var right = new MultiDictionary<string, int>();
            right.Add("k", 1);
            right.Add("other", 2);

            var diff = left.Difference(right);

            diff.OnlyInRight.ContainsKey("other").ShouldBeTrue();
            diff.OnlyInLeft.ShouldBeEmpty();
        }
    }
}
