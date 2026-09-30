using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// F6-41: the two entry points <see cref="ConcurrentMultiDictionary{TKey,TValue}"/> gained for
    /// the family's reverse direction and serialization coverage. Both are <b>snapshot</b>-based,
    /// which is this type's own contract: its whole read surface (enumeration, <c>Keys</c>,
    /// <c>Values</c>, <c>Count</c>, <c>Snapshot</c>) already reads a consistent copy rather than
    /// the live shards.
    /// </summary>
    public class ConcurrentMultiDictionaryTests
    {
        // ------------------------------------------------------------------
        // AsReverse
        // ------------------------------------------------------------------

        [Fact]
        public void AsReverse_PairsEachValueWithTheKeysThatStoreIt()
        {
            var map = new ConcurrentMultiDictionary<string, int>();
            map.Add("orders", 1001);
            map.Add("customers", 1001);
            map.Add("customers", 1002);

            var byValue = map.AsReverse();

            byValue[1001].ShouldBe(new[] { "orders", "customers" }, ignoreOrder: true);
            byValue[1002].ShouldBe(new[] { "customers" });
            byValue.ValueCount.ShouldBe(2);
            byValue.TotalKeyCount.ShouldBe(3);
        }

        [Fact]
        public void AsReverse_IsASnapshot_NotALiveView()
        {
            var map = new ConcurrentMultiDictionary<string, int>();
            map.Add("orders", 1001);

            var byValue = map.AsReverse();

            map.Add("invoices", 1001);

            byValue[1001].ShouldBe(new[] { "orders" });
            byValue.ValueCount.ShouldBe(1);
        }

        [Fact]
        public void AsReverse_ResultIsIndependentOfTheSource()
        {
            var map = new ConcurrentMultiDictionary<string, int>();
            map.Add("orders", 1001);

            var byValue = map.AsReverse();
            byValue.Add(2002, "refunds");

            map.ContainsKey("refunds").ShouldBeFalse();
            map.KeyCount.ShouldBe(1);
        }

        [Fact]
        public void AsReverse_NullValue_IsAnOrdinaryValue()
        {
            var map = new ConcurrentMultiDictionary<string, string>();
            map.Add("a", null);

            var byValue = map.AsReverse();

            byValue[null].ShouldBe(new[] { "a" });
        }

        [Fact]
        public void AsReverse_Empty_IsEmpty()
        {
            var map = new ConcurrentMultiDictionary<string, int>();

            var byValue = map.AsReverse();

            byValue.ValueCount.ShouldBe(0);
            byValue[1].ShouldBeEmpty();
        }

        [Fact]
        public void AsReverse_CarriesTheKeyComparerOntoTheReversedKeyAxis()
        {
            var map = new ConcurrentMultiDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            map.Add("USD", 1);

            var byValue = map.AsReverse();

            // The stored keys keep the source's key comparer, so they are still case-insensitive.
            byValue.ContainsValue(1).ShouldBeTrue();
            byValue.ContainsKey("usd").ShouldBeTrue();
            byValue[1].ShouldBe(new[] { "USD" });
        }

        // ------------------------------------------------------------------
        // ToSerializableModel / FromModel
        // ------------------------------------------------------------------

        [Fact]
        public void ToSerializableModel_HoldsEveryKeyWithItsValues()
        {
            var map = new ConcurrentMultiDictionary<string, int>();
            map.Add("orders", 1001);
            map.Add("orders", 1002);
            map.Add("customers", 1001);

            var model = map.ToSerializableModel();

            model.Keys.Count.ShouldBe(model.Values.Count);
            for (var i = 0; i < model.Keys.Count; i++)
            {
                model.Values[i].ShouldBe(map[model.Keys[i]]);
            }
        }

        [Fact]
        public void ToSerializableModel_IsAPointInTimeCopy()
        {
            var map = new ConcurrentMultiDictionary<string, int>();
            map.Add("orders", 1001);

            var model = map.ToSerializableModel();

            map.Add("orders", 1002);
            map.Add("customers", 2001);

            model.Keys.ShouldBe(new[] { "orders" });
            model.Values.Single().ShouldBe(new[] { 1001 });
        }

        [Fact]
        public void FromModel_RoundTripsTheMap()
        {
            var map = new ConcurrentMultiDictionary<string, int>();
            map.Add("orders", 1001);
            map.Add("orders", 1002);
            map.Add("customers", 1001);

            var restored = ConcurrentMultiDictionary<string, int>.FromModel(map.ToSerializableModel());

            restored.KeyCount.ShouldBe(map.KeyCount);
            restored.TotalValueCount.ShouldBe(map.TotalValueCount);
            restored["orders"].ShouldBe(map["orders"], ignoreOrder: true);
            restored["customers"].ShouldBe(map["customers"], ignoreOrder: true);
        }

        [Fact]
        public void FromModel_HonoursTheComparer()
        {
            var model = new MultiDictionaryModel<string, int>
            {
                Keys = { "USD" },
                Values = { new List<int> { 1 } }
            };

            var restored = ConcurrentMultiDictionary<string, int>.FromModel(
                model, StringComparer.OrdinalIgnoreCase);

            restored.ContainsKey("usd").ShouldBeTrue();
        }

        [Fact]
        public void FromModel_HonoursTheShardCountAndDuplicatePolicy()
        {
            var model = new MultiDictionaryModel<string, int>
            {
                Keys = { "k" },
                Values = { new List<int> { 1, 1 } }
            };

            var duplicating = ConcurrentMultiDictionary<string, int>.FromModel(
                model, comparer: null, allowDuplicateValues: true, shardCount: 4);
            duplicating.ShardCount.ShouldBe(4);
            duplicating.ValueCount("k").ShouldBe(2);

            var distinct = ConcurrentMultiDictionary<string, int>.FromModel(
                model, comparer: null, allowDuplicateValues: false);
            distinct.ShardCount.ShouldBe(8);
            distinct.ValueCount("k").ShouldBe(1);
        }

        [Fact]
        public void FromModel_NullModel_Throws()
        {
            Should.Throw<ArgumentNullException>(
                () => ConcurrentMultiDictionary<string, int>.FromModel(null!));
        }

        public static IEnumerable<object[]> MalformedModels()
        {
            yield return new object[] { new MultiDictionaryModel<string, int> { Keys = null!, Values = { } } };
            yield return new object[] { new MultiDictionaryModel<string, int> { Keys = { "a" }, Values = null! } };
            yield return new object[] { new MultiDictionaryModel<string, int> { Keys = { "a" }, Values = { } } };
            yield return new object[]
            {
                new MultiDictionaryModel<string, int>
                {
                    Keys = { "a" },
                    Values = { null! }
                }
            };
        }

        [Theory]
        [MemberData(nameof(MalformedModels))]
        public void FromModel_MalformedModel_Throws(MultiDictionaryModel<string, int> model)
        {
            Should.Throw<ArgumentException>(
                () => ConcurrentMultiDictionary<string, int>.FromModel(model));
        }

        [Fact]
        public void FromModel_EmptyModel_IsEmpty()
        {
            var restored = ConcurrentMultiDictionary<string, int>.FromModel(
                new MultiDictionaryModel<string, int>());

            restored.KeyCount.ShouldBe(0);
        }
    }
}
