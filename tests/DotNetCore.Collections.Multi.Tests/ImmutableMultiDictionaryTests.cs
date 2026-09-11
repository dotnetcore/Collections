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
    /// per-key semantics parity with <see cref="MultiDictionary{TKey,TValue}"/> for
    /// <see cref="ImmutableMultiDictionary{TKey,TValue}"/>.
    /// </summary>
    public class ImmutableMultiDictionaryTests
    {
        [Fact]
        public void MutationMethods_ReturnFreshInstances_AndLeaveTheReceiverUntouched()
        {
            var map = new ImmutableMultiDictionary<string, int>();
            map = map.Add("a", 1).Add("a", 2);

            var ranged = map.AddRange("b", new[] { 3, 4 });
            var keyRemoved = map.Remove("a");
            var valueRemoved = map.Remove("a", 1);
            var rangeRemoved = ranged.RemoveRange("b", new[] { 3 });
            var cleared = map.Clear();

            map.KeyCount.ShouldBe(1);
            map.ValueCount("a").ShouldBe(2);
            map.ContainsKey("b").ShouldBeFalse();

            ranged.ValueCount("b").ShouldBe(2);
            keyRemoved.Count.ShouldBe(0);
            valueRemoved.ValueCount("a").ShouldBe(1);
            rangeRemoved.ValueCount("b").ShouldBe(1);
            cleared.Count.ShouldBe(0);

            keyRemoved.ShouldNotBeSameAs(map);
            cleared.ShouldNotBeSameAs(map);
        }

        [Fact]
        public void ToBuilder_DoesNotCopyTheData_AndAnUntouchedBuilder_FreezesBackToTheSameInstance()
        {
            var map = new ImmutableMultiDictionary<int, int>();
            var builder = map.ToBuilder();
            for (var i = 0; i < 512; i++)
            {
                builder.Add(i, i);
            }

            var bag = builder.ToImmutable();

            var before = GC.GetAllocatedBytesForCurrentThread();
            var roundTrip = bag.ToBuilder().ToImmutable();
            var after = GC.GetAllocatedBytesForCurrentThread();

            (after - before).ShouldBeLessThan(512);
            ReferenceEquals(roundTrip, bag).ShouldBeTrue();
        }

        [Fact]
        public void Builder_FirstWrite_TakesTheCopyOnWrite_AndNeverTouchesTheSource()
        {
            var map = new ImmutableMultiDictionary<string, int>(new[] { new KeyValuePair<string, int>("a", 1) });
            var builder = map.ToBuilder();

            builder.Add("a", 2);
            builder.Add("b", 3);
            var frozen = builder.ToImmutable();

            map.KeyCount.ShouldBe(1);            // source untouched
            map.ValueCount("a").ShouldBe(1);
            frozen.ValueCount("a").ShouldBe(2);
            frozen.ContainsKey("b").ShouldBeTrue();
            frozen.ShouldNotBeSameAs(map);
        }

        [Fact]
        public void Builder_Clone_IsIndependentInBothDirections()
        {
            var builder = new ImmutableMultiDictionary<string, int>.Builder();
            builder.Add("a", 1);
            var clone = builder.Clone();

            builder.Add("x", 9);
            clone.Add("y", 8);

            builder.ContainsKey("y").ShouldBeFalse();
            clone.ContainsKey("x").ShouldBeFalse();
            clone.ValueCount("a").ShouldBe(1);

            builder.ToImmutable().ContainsKey("y").ShouldBeFalse();
            clone.ToImmutable().ContainsKey("x").ShouldBeFalse();
        }

        [Fact]
        public void PerKeySemantics_ParityWithTheMutableType_IncludingTheEmptyInnerInvariant()
        {
            var map = new ImmutableMultiDictionary<string, int>(new[] { new KeyValuePair<string, int>("a", 1) });

            var afterRemove = map.Remove("a", 1);

            // The "a key whose value collection has emptied is dropped" invariant carries over.
            afterRemove.ContainsKey("a").ShouldBeFalse();
            afterRemove.Count.ShouldBe(0);

            // Missing-key reads behave like the mutable type: an absent key yields an empty
            // collection rather than an exception.
            map["zzz"].Count.ShouldBe(0);
            map.ValueCount("zzz").ShouldBe(0);
            map.TryGetValue("zzz", out _).ShouldBeFalse();
        }

        [Fact]
        public void DuplicateValuesPolicy_IsHonoured()
        {
            var allowing = new ImmutableMultiDictionary<string, int>().Add("a", 1).Add("a", 1);
            var forbidding = new ImmutableMultiDictionary<string, int>(false).Add("a", 1);

            allowing.ValueCount("a").ShouldBe(2);

            forbidding.ValueCount("a").ShouldBe(1);
            forbidding.TotalValueCount.ShouldBe(1);

            // The builder carries the same policy.
            var builder = new ImmutableMultiDictionary<string, int>.Builder(false);
            builder.Add("a", 1);
            builder.Add("a", 1);
            builder.ToImmutable().ValueCount("a").ShouldBe(1);
        }

        [Fact]
        public void ContainsValue_AndLookups_AgreeWithContents()
        {
            var map = new ImmutableMultiDictionary<string, int>()
                .Add("a", 1).Add("b", 2).Add("b", 2);

            map.ContainsValue(2).ShouldBeTrue();
            map.ContainsValue(9).ShouldBeFalse();
            map.Contains("b", 2).ShouldBeTrue();
            map.Contains("a", 2).ShouldBeFalse();
            map.TotalValueCount.ShouldBe(3);
            map["b"].ShouldBe(new[] { 2, 2 });

            map.AsLookup()["b"].Count().ShouldBe(2);
            map.Keys.OrderBy(k => k).ShouldBe(new[] { "a", "b" });
            map.Values.OrderBy(v => v).ShouldBe(new[] { 1, 2, 2 });
        }

        [Fact]
        public void NullValues_AreOrdinaryValues()
        {
            var map = new ImmutableMultiDictionary<string, string>()
                .Add("a", null).Add("a", null).Add("a", "x");

            map.ValueCount("a").ShouldBe(3);
            map.ContainsValue(null).ShouldBeTrue();
            map.Contains("a", null).ShouldBeTrue();

            var after = map.Remove("a", null);
            after.ValueCount("a").ShouldBe(2);
            after.ContainsValue(null).ShouldBeTrue();
        }

        [Fact]
        public void Exports_AreIndependentSnapshots()
        {
            var map = new ImmutableMultiDictionary<string, int>()
                .Add("a", 1).Add("a", 2);

            var asMultiDictionary = map.ToMultiDictionary();
            var asDictionary = map.ToDictionary();
            var asModel = map.ToSerializableModel();

            asMultiDictionary.Add("a", 99);
            map.ValueCount("a").ShouldBe(2);

            asDictionary["a"].Count().ShouldBe(2);
            asModel.Keys.Count.ShouldBe(1);
            asModel.Values[0].Count.ShouldBe(2);

            var roundTrip = ImmutableMultiDictionary<string, int>.FromModel(asModel, map.Comparer, true);
            roundTrip.Count.ShouldBe(1);
            roundTrip.ValueCount("a").ShouldBe(2);
        }

        [Fact]
        public void Clear_KeepsConfiguration()
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            var map = new ImmutableMultiDictionary<string, int>(comparer, false).Add("A", 1);

            var cleared = map.Clear();

            cleared.Count.ShouldBe(0);
            cleared.Comparer.ShouldBe(comparer);
            // The duplicate-values policy survives: adding twice stores once.
            cleared.Add("a", 1).Add("a", 1).ValueCount("a").ShouldBe(1);
        }

        [Fact]
        public void Enumeration_YieldsOnePairPerStoredValue()
        {
            var map = new ImmutableMultiDictionary<string, int>()
                .Add("a", 1).Add("a", 2).Add("b", 3);

            map.OrderBy(p => p.Key).ThenBy(p => p.Value)
                .ShouldBe(new[] { new KeyValuePair<string, int>("a", 1), new KeyValuePair<string, int>("a", 2), new KeyValuePair<string, int>("b", 3) });
        }
    }
}
