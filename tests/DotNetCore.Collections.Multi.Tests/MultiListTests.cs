using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    public class MultiListTests
    {
        private sealed class SameHashCodeElement
        {
            public SameHashCodeElement(int value)
            {
                Value = value;
            }

            public int Value { get; }

            public override int GetHashCode() => 42;

            public override bool Equals(object obj)
                => obj is SameHashCodeElement other && other.Value == Value;
        }

        [Fact]
        public void Add_Single_IncreasesCountOfAndTotalCount()
        {
            var list = new MultiList<string>();
            list.Add("a");

            list.CountOf("a").ShouldBe(1);
            list.TotalCount.ShouldBe(1);
            list.DistinctCount.ShouldBe(1);
        }

        [Fact]
        public void Add_Times_IncreasesCopyCount()
        {
            var list = new MultiList<string>();
            list.Add("a", 3);

            list.CountOf("a").ShouldBe(3);
            list.TotalCount.ShouldBe(3);
            list.DistinctCount.ShouldBe(1);
        }

        [Fact]
        public void Add_NonPositiveTimes_IsCoercedToOne()
        {
            var list = new MultiList<string>();
            list.Add("a", 0);
            list.CountOf("a").ShouldBe(1);

            list.Add("b", -5);
            list.CountOf("b").ShouldBe(1);
        }

        [Fact]
        public void AddRange_AddsEachElementOnce()
        {
            var list = new MultiList<string>();
            list.AddRange(new[] { "a", "b", "a" });

            list.CountOf("a").ShouldBe(2);
            list.CountOf("b").ShouldBe(1);
            list.TotalCount.ShouldBe(3);
        }

        [Fact]
        public void Contains_ReturnsTrueForPresentAndFalseForAbsent()
        {
            var list = new MultiList<string>();
            list.Add("a");

            list.Contains("a").ShouldBeTrue();
            list.Contains("b").ShouldBeFalse();
        }

        [Fact]
        public void ContainsAll_True_WhenAllPresent()
        {
            var list = new MultiList<string>();
            list.Add("a");
            list.Add("b");

            list.ContainsAll(new[] { "a", "b" }).ShouldBeTrue();
        }

        [Fact]
        public void ContainsAll_False_WhenAnyMissing()
        {
            var list = new MultiList<string>();
            list.Add("a");

            list.ContainsAll(new[] { "a", "b" }).ShouldBeFalse();
        }

        [Fact]
        public void ContainsAll_Null_ThrowsArgumentNullException()
        {
            var list = new MultiList<string>();
            Should.Throw<ArgumentNullException>(() => list.ContainsAll(null));
        }

        [Fact]
        public void CountOf_ReturnsZeroForAbsentElement()
        {
            var list = new MultiList<string>();
            list.CountOf("missing").ShouldBe(0);
        }

        [Fact]
        public void TotalCount_AggregatesAcrossElements()
        {
            var list = new MultiList<int>();
            list.Add(1, 3);
            list.Add(2, 4);

            list.TotalCount.ShouldBe(7);
        }

        [Fact]
        public void DistinctCount_CountsUniqueElements()
        {
            var list = new MultiList<int>();
            list.Add(1, 3);
            list.Add(2);
            list.Add(2);

            list.DistinctCount.ShouldBe(2);
        }

        [Fact]
        public void Remove_SingleCopy_ReturnsRemainingCount()
        {
            var list = new MultiList<string>();
            list.Add("a", 3);

            list.Remove("a").ShouldBe(2);
            list.CountOf("a").ShouldBe(2);
        }

        [Fact]
        public void Remove_RemovesKey_WhenCountReachesZero()
        {
            var list = new MultiList<string>();
            list.Add("a");
            list.Remove("a");

            list.Contains("a").ShouldBeFalse();
            list.DistinctCount.ShouldBe(0);
        }

        [Fact]
        public void Remove_NonPositiveTimes_IsCoercedToOne()
        {
            var list = new MultiList<string>();
            list.Add("a", 3);

            list.Remove("a", 0).ShouldBe(2);
        }

        [Fact]
        public void Remove_TimesMoreThanCount_RemovesAllCopies()
        {
            var list = new MultiList<string>();
            list.Add("a", 2);

            list.Remove("a", 5).ShouldBe(0);
            list.Contains("a").ShouldBeFalse();
            list.TotalCount.ShouldBe(0);
        }

        [Fact]
        public void Remove_AbsentElement_ReturnsZero()
        {
            var list = new MultiList<string>();
            list.Remove("missing").ShouldBe(0);
        }

        [Fact]
        public void RemoveAllCopies_True_WhenPresent()
        {
            var list = new MultiList<string>();
            list.Add("a", 3);
            list.Add("b");

            list.RemoveAllCopies("a").ShouldBeTrue();
            list.Contains("a").ShouldBeFalse();
            list.TotalCount.ShouldBe(1);
        }

        [Fact]
        public void RemoveAllCopies_False_WhenAbsent()
        {
            var list = new MultiList<string>();
            list.RemoveAllCopies("missing").ShouldBeFalse();
        }

        [Fact]
        public void Clear_ResetsAllCounts()
        {
            var list = new MultiList<string>();
            list.Add("a", 3);

            list.Clear();

            list.TotalCount.ShouldBe(0);
            list.DistinctCount.ShouldBe(0);
            list.Contains("a").ShouldBeFalse();
        }

        [Fact]
        public void ToList_ExpandsAllCopies()
        {
            var list = new MultiList<string>();
            list.Add("a", 3);
            list.Add("b", 2);

            var expanded = list.ToList();
            expanded.Count.ShouldBe(5);
            expanded.Count(x => x == "a").ShouldBe(3);
            expanded.Count(x => x == "b").ShouldBe(2);
        }

        [Fact]
        public void ToArray_ExpandsAllCopies()
        {
            var list = new MultiList<int>();
            list.Add(1, 2);
            list.Add(2, 1);

            var array = list.ToArray();
            array.Length.ShouldBe(3);
            array.Count(x => x == 1).ShouldBe(2);
        }

        [Fact]
        public void DistinctItems_ReturnsUniqueElementsOnly()
        {
            var list = new MultiList<string>();
            list.Add("a", 3);
            list.Add("b", 2);

            list.DistinctItems().OrderBy(x => x).ShouldBe(new[] { "a", "b" });
        }

        [Fact]
        public void EntrySet_ReturnsItemAndCountPairs()
        {
            var list = new MultiList<string>();
            list.Add("a", 3);
            list.Add("b");

            var entries = list.EntrySet().ToDictionary(x => x.Item, x => x.Count);
            entries["a"].ShouldBe(3);
            entries["b"].ShouldBe(1);
        }

        [Fact]
        public void Enumeration_ExpandsCopies()
        {
            var list = new MultiList<int>();
            list.Add(1, 3);
            list.Add(2);

            list.Count().ShouldBe(list.TotalCount);
            list.Count(x => x == 1).ShouldBe(3);
        }

        [Fact]
        public void CollectionInterfaces_Count_MatchTotalCount()
        {
            var list = new MultiList<string>();
            list.Add("a", 3);

            ((ICollection<string>)list).Count.ShouldBe(3);
            ((IReadOnlyCollection<string>)list).Count.ShouldBe(3);
        }

        [Fact]
        public void ICollectionRemove_ReturnsTrue_WhenCopyRemoved()
        {
            ICollection<string> list = new MultiList<string>();
            list.Add("a");

            list.Remove("a").ShouldBeTrue();
            list.Remove("a").ShouldBeFalse();
        }

        [Fact]
        public void IsReadOnly_IsFalse()
        {
            new MultiList<string>().IsReadOnly.ShouldBeFalse();
        }

        [Fact]
        public void CopyTo_CopiesExpandedSequence()
        {
            var list = new MultiList<string>();
            list.Add("a", 2);
            list.Add("b");

            var array = new string[3];
            list.CopyTo(array, 0);

            array.Count(x => x == "a").ShouldBe(2);
            array.Count(x => x == "b").ShouldBe(1);
        }

        [Fact]
        public void CopyTo_InsufficientSpace_ThrowsArgumentException()
        {
            var list = new MultiList<string>();
            list.Add("a", 3);

            Should.Throw<ArgumentException>(() => list.CopyTo(new string[2], 0));
        }

        [Fact]
        public void Clone_IsIndependent()
        {
            var original = new MultiList<string>();
            original.Add("a", 2);

            var clone = original.Clone();
            clone.Add("a");
            clone.Add("b");

            original.CountOf("a").ShouldBe(2);
            original.Contains("b").ShouldBeFalse();
        }

        [Fact]
        public void Comparer_IsUsedForElementEquality()
        {
            var list = new MultiList<string>(StringComparer.OrdinalIgnoreCase);
            list.Add("a");
            list.Add("A");

            list.DistinctCount.ShouldBe(1);
            list.CountOf("A").ShouldBe(2);
        }

        // Regression test for the former hashcode-only keying defect (M0-1).
        [Fact]
        public void SameHashCode_DifferentElements_AreTrackedSeparately()
        {
            var list = new MultiList<SameHashCodeElement>();
            var one = new SameHashCodeElement(1);
            var two = new SameHashCodeElement(2);

            list.Add(one, 2);
            list.Add(two, 3);

            list.CountOf(one).ShouldBe(2);
            list.CountOf(two).ShouldBe(3);
            list.DistinctCount.ShouldBe(2);
            list.TotalCount.ShouldBe(5);

            list.Remove(one, 2).ShouldBe(0);
            list.CountOf(two).ShouldBe(3);
        }

        [Fact]
        public void NullElements_AreSupported()
        {
            var list = new MultiList<string>();
            list.Add(null, 2);
            list.Add("a");

            list.Contains(null).ShouldBeTrue();
            list.CountOf(null).ShouldBe(2);
            list.DistinctCount.ShouldBe(2);
            list.TotalCount.ShouldBe(3);
            list.ToList().Count(x => x == null).ShouldBe(2);
            list.Remove(null).ShouldBe(1);
            list.RemoveAllCopies(null).ShouldBeTrue();
            list.Contains(null).ShouldBeFalse();
        }

        [Fact]
        public void Constructor_FromCollection_SeedCounts()
        {
            var list = new MultiList<string>(new[] { "a", "b", "a" });

            list.CountOf("a").ShouldBe(2);
            list.CountOf("b").ShouldBe(1);
        }

        [Fact]
        public void Constructor_NegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            Should.Throw<ArgumentOutOfRangeException>(() => new MultiList<string>(-1));
        }

        [Fact]
        public void ToString_ContainsExpandedElements()
        {
            var list = new MultiList<string>();
            list.Add("a", 2);

            list.ToString().ShouldBe("a,a");
        }
    }
}
