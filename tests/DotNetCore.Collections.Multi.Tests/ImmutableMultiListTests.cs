using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// M6-09: Builder-to-freeze semantics, copy-on-write observability, clone independence and
    /// the immutability contract of <see cref="ImmutableMultiList{T}"/>.
    /// </summary>
    public class ImmutableMultiListTests
    {
        [Fact]
        public void MutationMethods_ReturnFreshInstances_AndLeaveTheReceiverUntouched()
        {
            var bag = new ImmutableMultiList<string>(new[] { "a", "b" });

            var added = bag.Add("c");
            var addedMany = bag.Add("d", 3);
            var ranged = bag.AddRange(new[] { "e", "e" });
            var removed = bag.Remove("a", 1);
            var removedAll = bag.RemoveAllCopies("b");
            var cleared = bag.Clear();

            bag.TotalCount.ShouldBe(2);
            bag.CountOf("a").ShouldBe(1);
            bag.CountOf("b").ShouldBe(1);
            bag.Contains("c").ShouldBeFalse();

            added.TotalCount.ShouldBe(3);
            addedMany.TotalCount.ShouldBe(5);
            ranged.CountOf("e").ShouldBe(2);
            removed.TotalCount.ShouldBe(1);
            removedAll.TotalCount.ShouldBe(1); // from the original bag, only "b" is gone
            cleared.TotalCount.ShouldBe(0);

            // Every mutation is a distinct object; none of them is the receiver.
            added.ShouldNotBeSameAs(bag);
            addedMany.ShouldNotBeSameAs(added);
            cleared.ShouldNotBeSameAs(bag);
        }

        [Fact]
        public void ToBuilder_DoesNotCopyTheData_AndAnUntouchedBuilder_FreezesBackToTheSameInstance()
        {
            var bag = new ImmutableMultiList<int>(Enumerable.Range(0, 4096));

            var before = GC.GetAllocatedBytesForCurrentThread();
            var builder = bag.ToBuilder();
            var frozen = builder.ToImmutable();
            var after = GC.GetAllocatedBytesForCurrentThread();

            // Structural sharing, made observable: no copy of 4096 entries was taken (a deep copy
            // of this bag allocates tens of kilobytes), and the freeze is an identity check.
            (after - before).ShouldBeLessThan(512);
            ReferenceEquals(frozen, bag).ShouldBeTrue();
        }

        [Fact]
        public void Builder_FirstWrite_TakesTheCopyOnWrite_AndNeverTouchesTheSource()
        {
            var bag = new ImmutableMultiList<string>(new[] { "a", "b" });
            var builder = bag.ToBuilder();

            builder.Add("c");
            var frozen = builder.ToImmutable();

            bag.TotalCount.ShouldBe(2);          // source untouched
            bag.Contains("c").ShouldBeFalse();
            frozen.TotalCount.ShouldBe(3);
            frozen.ShouldNotBeSameAs(bag);
            ReferenceEquals(frozen, bag).ShouldBeFalse();
        }

        [Fact]
        public void Builder_AfterClear_FreezesToAnEmptyBagNotToTheSource()
        {
            var bag = new ImmutableMultiList<string>(new[] { "a" });
            var builder = bag.ToBuilder();
            builder.Clear();

            var frozen = builder.ToImmutable();

            frozen.TotalCount.ShouldBe(0);
            frozen.ShouldNotBeSameAs(bag);
        }

        [Fact]
        public void Builder_ReadsBeforeTheFirstWrite_AgreeWithTheSource()
        {
            var bag = new ImmutableMultiList<string>(new[] { "a", "a", "b" });
            var builder = bag.ToBuilder();

            builder.TotalCount.ShouldBe(3);
            builder.DistinctCount.ShouldBe(2);
            builder.CountOf("a").ShouldBe(2);
            builder.Contains("b").ShouldBeTrue();
            builder.Comparer.ShouldBe(bag.Comparer);
        }

        [Fact]
        public void Builder_Clone_IsIndependentInBothDirections()
        {
            var builder = new ImmutableMultiList<string>.Builder(new[] { "a" });
            var clone = builder.Clone();

            builder.Add("x");
            clone.Add("y");

            builder.CountOf("y").ShouldBe(0);
            clone.CountOf("x").ShouldBe(0);
            clone.CountOf("a").ShouldBe(1);

            // And each freezes to a distinct bag carrying only its own writes.
            builder.ToImmutable().ToList().ShouldNotContain("y");
            clone.ToImmutable().ToList().ShouldNotContain("x");
        }

        [Fact]
        public void StandaloneBuilder_FollowsTheSameLifecycle()
        {
            var builder = new ImmutableMultiList<int>.Builder();
            builder.AddRange(new[] { 1, 2, 2 });

            var bag = builder.ToImmutable();

            bag.TotalCount.ShouldBe(3);
            bag.CountOf(2).ShouldBe(2);

            // A standalone builder has no source to freeze back to: the second freeze is still a
            // fresh object, and writes after the first freeze do not leak into it.
            builder.Add(3);
            var again = builder.ToImmutable();
            again.TotalCount.ShouldBe(4);
            bag.Contains(3).ShouldBeFalse();
        }

        [Fact]
        public void Comparer_AndNullElements_AreCarriedThrough()
        {
            var bag = new ImmutableMultiList<string>(
                new[] { "A", "a", null, null }, StringComparer.OrdinalIgnoreCase);

            bag.Comparer.ShouldBe(StringComparer.OrdinalIgnoreCase);
            bag.CountOf("a").ShouldBe(2);
            bag.CountOf(null).ShouldBe(2);
            bag.DistinctCount.ShouldBe(2);

            var rebuilt = bag.Add(null).Add("A");
            rebuilt.CountOf("A").ShouldBe(3);
            rebuilt.CountOf(null).ShouldBe(3);

            var withComparer = bag.WithComparer(StringComparer.Ordinal);
            // The case-insensitive bag stored the first-seen spelling "A" with 2 copies, so under
            // ordinal equality the rebuilt bag holds "A" x2 and no "a" at all - copies are keyed
            // by their stored spelling, not by the querying spelling.
            withComparer.CountOf("A").ShouldBe(2);
            withComparer.CountOf("a").ShouldBe(0);
            withComparer.CountOf(null).ShouldBe(2);
        }

        [Fact]
        public void SetJudgments_AndEquality_MatchMultisetSemantics()
        {
            var left = new ImmutableMultiList<int>(new[] { 1, 1, 2 });
            var right = new ImmutableMultiList<int>(new[] { 1, 2, 1 });
            var other = new ImmutableMultiList<int>(new[] { 1, 2 });

            left.Equals(right).ShouldBeTrue();
            (left == null ? false : left.Equals(other)).ShouldBeFalse();
            left.GetHashCode().ShouldBe(right.GetHashCode());
            left.Equals((object)right).ShouldBeTrue();

            left.IsSubsetOf(new[] { 1, 1, 2, 3 }).ShouldBeTrue();
            left.IsProperSubsetOf(new[] { 1, 1, 2, 3 }).ShouldBeTrue();
            left.IsSupersetOf(new[] { 1, 2 }).ShouldBeTrue();
            left.IsProperSupersetOf(new[] { 1, 2 }).ShouldBeTrue();
            left.Overlaps(new[] { 2, 9 }).ShouldBeTrue();
            left.Overlaps(new[] { 9 }).ShouldBeFalse();
        }

        [Fact]
        public void Exports_AreIndependentSnapshots()
        {
            var bag = new ImmutableMultiList<string>(new[] { "a", "a", "b" });

            var asMultiList = bag.ToMultiList();
            var asDictionary = bag.ToDictionary();
            var asModel = bag.ToSerializableModel();

            asMultiList.Add("zzz");
            bag.Contains("zzz").ShouldBeFalse();

            asDictionary["a"].ShouldBe(2);
            asModel.Items.Count.ShouldBe(2);          // distinct elements, first-seen spelling
            asModel.Counts.Sum().ShouldBe(3);         // total copies across all distinct elements

            // The model round-trips through the mutable type and back.
            var roundTrip = ImmutableMultiList<string>.FromModel(bag.ToSerializableModel(), bag.Comparer);
            roundTrip.Equals(bag).ShouldBeTrue();
        }

        [Fact]
        public void NonPositiveTimes_Throws_MirroringTheMutableType()
        {
            var bag = new ImmutableMultiList<int>();
            var builder = new ImmutableMultiList<int>.Builder();

            Should.Throw<ArgumentOutOfRangeException>(() => bag.Add(1, 0));
            Should.Throw<ArgumentOutOfRangeException>(() => builder.Add(1, -1));
        }

        [Fact]
        public void Enumeration_YieldsEveryCopy_InSnapshotSafety()
        {
            var bag = new ImmutableMultiList<int>(new[] { 1, 1, 2, 3, 3, 3 });

            bag.OrderBy(x => x).ShouldBe(new[] { 1, 1, 2, 3, 3, 3 });
            bag.DistinctItems().OrderBy(x => x).ShouldBe(new[] { 1, 2, 3 });
            bag.EntrySet().Count().ShouldBe(3);
        }
    }
}
