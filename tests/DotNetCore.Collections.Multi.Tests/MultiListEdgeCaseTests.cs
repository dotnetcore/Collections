using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// Edge-condition coverage for <see cref="MultiList{T}"/>:
    /// argument validation, null-element lifecycle, comparer injection,
    /// explicit interface behaviour and self-referencing set operations.
    /// </summary>
    public class MultiListEdgeCaseTests
    {
        private static MultiList<string> Bag(params string[] items)
        {
            var bag = new MultiList<string>();
            foreach (var item in items)
            {
                bag.Add(item);
            }

            return bag;
        }

        // ------------------------------------------------------------------
        // Constructor / argument validation
        // ------------------------------------------------------------------

        [Fact]
        public void Constructor_NegativeCapacity_ThrowsArgumentOutOfRange()
        {
            Should.Throw<ArgumentOutOfRangeException>(() => new MultiList<string>(-1));
        }

        [Fact]
        public void Constructor_NullCollection_ThrowsArgumentNull()
        {
            Should.Throw<ArgumentNullException>(() => new MultiList<string>((IEnumerable<string>)null));
        }

        [Fact]
        public void AddRange_Null_ThrowsArgumentNull()
        {
            Should.Throw<ArgumentNullException>(() => new MultiList<string>().AddRange(null));
        }

        [Fact]
        public void ContainsAll_Null_ThrowsArgumentNull()
        {
            Should.Throw<ArgumentNullException>(() => new MultiList<string>().ContainsAll(null));
        }

        [Fact]
        public void ContainsAll_EmptyInput_ReturnsTrue()
        {
            Bag("a").ContainsAll(new string[0]).ShouldBeTrue();
        }

        [Fact]
        public void ContainsAll_AllPresent_ReturnsTrue_EvenWithRepeatedInput()
        {
            Bag("a", "a", "b").ContainsAll(new[] { "a", "a", "b", "a" }).ShouldBeTrue();
        }

        [Fact]
        public void ContainsAll_SingleMissingCopy_ReturnsFalse()
        {
            // ContainsAll is presence-based, not multiplicity-based.
            Bag("a").ContainsAll(new[] { "a", "a" }).ShouldBeTrue();
            Bag("a").ContainsAll(new[] { "a", "b" }).ShouldBeFalse();
        }

        // ------------------------------------------------------------------
        // Non-positive times coercion (legacy behaviour)
        // ------------------------------------------------------------------

        [Fact]
        public void Add_ZeroTimes_CoercedToOneCopy()
        {
            var bag = new MultiList<string>();
            bag.Add("a", 0);
            bag.CountOf("a").ShouldBe(1);
            bag.TotalCount.ShouldBe(1);
        }

        [Fact]
        public void Add_NegativeTimes_CoercedToOneCopy()
        {
            var bag = new MultiList<string>();
            bag.Add("a", -7);
            bag.CountOf("a").ShouldBe(1);
        }

        [Fact]
        public void Remove_ZeroTimes_CoercedToOneCopy()
        {
            var bag = Bag("a", "a");
            bag.Remove("a", 0);
            bag.CountOf("a").ShouldBe(1);
        }

        // ------------------------------------------------------------------
        // Remove / RemoveAllCopies edge behaviour
        // ------------------------------------------------------------------

        [Fact]
        public void Remove_Absent_ReturnsZero_TotalCountUnchanged()
        {
            var bag = Bag("a");
            bag.Remove("zzz").ShouldBe(0);
            bag.TotalCount.ShouldBe(1);
        }

        [Fact]
        public void Remove_TimesExceedsCopies_RemovesAll_ReturnsZero()
        {
            var bag = Bag("a", "a", "a");
            bag.Remove("a", 99).ShouldBe(0);
            bag.CountOf("a").ShouldBe(0);
            bag.Contains("a").ShouldBeFalse();
            bag.DistinctCount.ShouldBe(0);
            bag.TotalCount.ShouldBe(0);
        }

        [Fact]
        public void Remove_LastCopy_DropsDistinctEntry()
        {
            var bag = Bag("a", "b");
            bag.Remove("a");
            bag.DistinctCount.ShouldBe(1);
            bag.DistinctItems().ShouldBe(new[] { "b" });
        }

        [Fact]
        public void RemoveAllCopies_Absent_ReturnsFalse()
        {
            Bag("a").RemoveAllCopies("zzz").ShouldBeFalse();
        }

        [Fact]
        public void Clear_ResetsAllCounters()
        {
            var bag = Bag("a", "a", null);
            bag.Clear();
            bag.TotalCount.ShouldBe(0);
            bag.DistinctCount.ShouldBe(0);
            bag.Contains("a").ShouldBeFalse();
            bag.Contains(null).ShouldBeFalse();
            bag.ToList().ShouldBeEmpty();
        }

        // ------------------------------------------------------------------
        // Null-element lifecycle
        // ------------------------------------------------------------------

        [Fact]
        public void NullElement_FullLifecycle_TrackedSeparately()
        {
            var bag = new MultiList<string>();
            bag.Add(null);
            bag.Add(null);
            bag.Add("x");

            bag.Contains(null).ShouldBeTrue();
            bag.CountOf(null).ShouldBe(2);
            bag.DistinctCount.ShouldBe(2);
            bag.TotalCount.ShouldBe(3);

            bag.Remove(null);
            bag.CountOf(null).ShouldBe(1);

            bag.RemoveAllCopies(null).ShouldBeTrue();
            bag.Contains(null).ShouldBeFalse();
            bag.DistinctCount.ShouldBe(1);
        }

        [Fact]
        public void NullElement_AppearsInExpandedEnumeration()
        {
            var bag = Bag("a", null, null);
            var expanded = bag.ToList();
            expanded.Count.ShouldBe(3);
            expanded.Count(x => x == null).ShouldBe(2);
        }

        [Fact]
        public void NullElement_AppearsInEntrySet()
        {
            var bag = Bag(null, null);
            var entry = bag.EntrySet().Single();
            entry.Item.ShouldBeNull();
            entry.Count.ShouldBe(2);
        }

        // ------------------------------------------------------------------
        // Comparer injection
        // ------------------------------------------------------------------

        [Fact]
        public void CaseInsensitiveComparer_ElementsCountedTogether()
        {
            var bag = new MultiList<string>(StringComparer.OrdinalIgnoreCase);
            bag.Add("Apple");
            bag.Add("APPLE");
            bag.Add("apple");

            bag.CountOf("aPPle").ShouldBe(3);
            bag.DistinctCount.ShouldBe(1);
            bag.TotalCount.ShouldBe(3);
        }

        [Fact]
        public void CaseInsensitiveComparer_RespectedAcrossAllOperations()
        {
            var bag = new MultiList<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            bag.Contains("a").ShouldBeTrue();
            bag.Remove("a");
            bag.CountOf("A").ShouldBe(0);
            bag.Comparer.ShouldBeSameAs(StringComparer.OrdinalIgnoreCase);
        }

        // ------------------------------------------------------------------
        // Explicit interface implementations
        // ------------------------------------------------------------------

        [Fact]
        public void ICollectionExplicit_Remove_Absent_ReturnsFalse()
        {
            ICollection<string> bag = Bag("a");
            bag.Remove("zzz").ShouldBeFalse();
            bag.Remove("a").ShouldBeTrue();
        }

        [Fact]
        public void ICollectionExplicit_Count_ReturnsTotalCopies()
        {
            ICollection<string> bag = Bag("a", "a", "b");
            bag.Count.ShouldBe(3);
        }

        [Fact]
        public void IsReadOnly_AlwaysFalse()
        {
            ((ICollection<string>)Bag()).IsReadOnly.ShouldBeFalse();
        }

        // ------------------------------------------------------------------
        // CopyTo edge cases
        // ------------------------------------------------------------------

        [Fact]
        public void CopyTo_NullArray_ThrowsArgumentNull()
        {
            Should.Throw<ArgumentNullException>(() => Bag("a").CopyTo(null, 0));
        }

        [Fact]
        public void CopyTo_NegativeIndex_ThrowsArgumentOutOfRange()
        {
            Should.Throw<ArgumentOutOfRangeException>(() => Bag("a").CopyTo(new string[1], -1));
        }

        [Fact]
        public void CopyTo_InsufficientSpace_ThrowsArgument()
        {
            Should.Throw<ArgumentException>(() => Bag("a", "a", "b").CopyTo(new string[2], 0));
        }

        [Fact]
        public void CopyTo_ExactSpace_Succeeds()
        {
            var bag = Bag("a", "a", "b");
            var target = new string[3];
            bag.CopyTo(target, 0);
            target.Count(x => x == "a").ShouldBe(2);
            target.Count(x => x == "b").ShouldBe(1);
        }

        [Fact]
        public void CopyTo_EmptyBag_ZeroCopies_ValidIndexAtEnd_Succeeds()
        {
            var target = new string[2];
            Bag().CopyTo(target, 2);
            target.ShouldBe(new[] { default(string), default(string) });
        }

        // ------------------------------------------------------------------
        // Self-referencing set operations
        // ------------------------------------------------------------------

        [Fact]
        public void UnionWith_Self_Unchanged()
        {
            var bag = Bag("a", "a", "b");
            bag.UnionWith(bag);
            bag.CountOf("a").ShouldBe(2);
            bag.CountOf("b").ShouldBe(1);
        }

        [Fact]
        public void IntersectionWith_Self_Unchanged()
        {
            var bag = Bag("a", "a", "b");
            bag.IntersectionWith(bag);
            bag.CountOf("a").ShouldBe(2);
            bag.CountOf("b").ShouldBe(1);
        }

        [Fact]
        public void ExceptWith_Self_Empties()
        {
            var bag = Bag("a", "a", "b");
            bag.ExceptWith(bag);
            bag.TotalCount.ShouldBe(0);
            bag.DistinctCount.ShouldBe(0);
        }

        [Fact]
        public void SymmetricExceptWith_Self_Empties()
        {
            var bag = Bag("a", "a", "b");
            bag.SymmetricExceptWith(bag);
            bag.TotalCount.ShouldBe(0);
        }

        // ------------------------------------------------------------------
        // Subset / superset edge cases
        // ------------------------------------------------------------------

        [Fact]
        public void IsProperSubsetOf_EqualMultisets_ReturnsFalse()
        {
            Bag("a", "a").IsProperSubsetOf(new[] { "a", "a" }).ShouldBeFalse();
        }

        [Fact]
        public void IsProperSupersetOf_Self_ReturnsFalse()
        {
            var bag = Bag("a", "b");
            bag.IsProperSupersetOf(bag).ShouldBeFalse();
        }

        [Fact]
        public void IsSubsetOf_Self_ReturnsTrue()
        {
            var bag = Bag("a", "b");
            bag.IsSubsetOf(bag).ShouldBeTrue();
        }

        [Fact]
        public void EmptyBag_IsSubsetOf_Anything()
        {
            Bag().IsSubsetOf(new string[0]).ShouldBeTrue();
            Bag().IsSubsetOf(new[] { "a" }).ShouldBeTrue();
        }

        [Fact]
        public void NonEmptyBag_IsProperSupersetOf_Empty()
        {
            Bag("a").IsProperSupersetOf(new string[0]).ShouldBeTrue();
        }

        [Fact]
        public void Subset_RequiresFullMultiplicity()
        {
            Bag("a", "a").IsSubsetOf(new[] { "a" }).ShouldBeFalse();
            Bag("a", "a").IsSubsetOf(new[] { "a", "a", "b" }).ShouldBeTrue();
        }

        // ------------------------------------------------------------------
        // Overlap / disjointness with nulls and empty inputs
        // ------------------------------------------------------------------

        [Fact]
        public void Overlaps_EmptyInput_ReturnsFalse()
        {
            Bag("a").Overlaps(new string[0]).ShouldBeFalse();
        }

        [Fact]
        public void Overlaps_NullElementsShared_ReturnsTrue()
        {
            Bag(new string[] { null }).Overlaps(new[] { null, "x" }).ShouldBeTrue();
        }

        [Fact]
        public void IsDisjointFrom_NullOther_ThrowsArgumentNull()
        {
            Should.Throw<ArgumentNullException>(() => Bag("a").IsDisjointFrom(null));
        }

        // ------------------------------------------------------------------
        // Set operations: null argument validation (all go through Snapshot)
        // ------------------------------------------------------------------

        [Theory]
        [InlineData("union")]
        [InlineData("intersection")]
        [InlineData("except")]
        [InlineData("symmetricExcept")]
        [InlineData("isSubset")]
        [InlineData("isSuperset")]
        [InlineData("isProperSubset")]
        [InlineData("isProperSuperset")]
        public void SetOperations_NullOther_ThrowsArgumentNull(string operation)
        {
            var bag = Bag("a");
            Action act = operation switch
            {
                "union" => () => bag.UnionWith(null),
                "intersection" => () => bag.IntersectionWith(null),
                "except" => () => bag.ExceptWith(null),
                "symmetricExcept" => () => bag.SymmetricExceptWith(null),
                "isSubset" => () => bag.IsSubsetOf(null),
                "isSuperset" => () => bag.IsSupersetOf(null),
                "isProperSubset" => () => bag.IsProperSubsetOf(null),
                _ => () => bag.IsProperSupersetOf(null)
            };

            Should.Throw<ArgumentNullException>(act);
        }

        // ------------------------------------------------------------------
        // Enumeration stability
        // ------------------------------------------------------------------

        [Fact]
        public void ExpandedEnumeration_MatchesTotalCount()
        {
            var bag = Bag("a", "a", "b", null);
            bag.ToList().Count.ShouldBe(bag.TotalCount);
            bag.ToArray().Length.ShouldBe(bag.TotalCount);
        }

        [Fact]
        public void EntrySet_CoversExactlyDistinctElements()
        {
            var bag = Bag("a", "a", "b", null);
            bag.EntrySet().Select(e => e.Item).OrderBy(x => x ?? string.Empty)
                .ShouldBe(new[] { null, "a", "b" }.OrderBy(x => x ?? string.Empty));
            bag.EntrySet().Sum(e => e.Count).ShouldBe(bag.TotalCount);
        }
    }
}
