using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// M6-06: the explicit serialization entry points - <c>ToSerializableModel()</c> /
    /// <c>FromModel()</c> on <c>MultiList&lt;T&gt;</c> and <c>MultiDictionary&lt;TKey,TValue&gt;</c>.
    /// The models are plain POCOs with no attributes, so the suite uses <c>System.Text.Json</c> (a
    /// test-only reference, in-box on net8.0) to prove a consumer can serialize them - and asserts
    /// that the library itself takes no such dependency.
    /// </summary>
    public class SerializationModelTests
    {
        // ------------------------------------------------------------------
        // MultiList<T>: shape and round trip
        // ------------------------------------------------------------------

        [Fact]
        public void MultiList_ToSerializableModel_ListsDistinctElementsWithTheirCounts()
        {
            var bag = new MultiList<string> { "apple", "apple", "banana" };

            var model = bag.ToSerializableModel();

            model.Items.ShouldBe(new[] { "apple", "banana" });
            model.Counts.ShouldBe(new[] { 2, 1 });
        }

        [Fact]
        public void MultiList_RoundTrip_PreservesEveryCopyCount()
        {
            var bag = new MultiList<string>();
            bag.Add("x", 5);
            bag.Add("y", 1);
            bag.Add("z", 7);

            var restored = MultiList<string>.FromModel(bag.ToSerializableModel());

            restored.TotalCount.ShouldBe(13);
            restored.DistinctCount.ShouldBe(3);
            restored.CountOf("x").ShouldBe(5);
            restored.CountOf("z").ShouldBe(7);
            restored.Equals(bag).ShouldBeTrue();
        }

        [Fact]
        public void MultiList_RoundTrip_PreservesANullElement()
        {
            var bag = new MultiList<string>();
            bag.Add(null, 4);
            bag.Add("a", 1);

            var model = bag.ToSerializableModel();
            var restored = MultiList<string>.FromModel(model);

            restored.CountOf(null).ShouldBe(4);
            restored.CountOf("a").ShouldBe(1);
            restored.TotalCount.ShouldBe(5);
        }

        [Fact]
        public void MultiList_RoundTrip_AllNulls()
        {
            var bag = new MultiList<string>();
            bag.Add(null, 3);

            var model = bag.ToSerializableModel();
            var restored = MultiList<string>.FromModel(model);

            model.Items.Count.ShouldBe(1);
            model.Items[0].ShouldBeNull();
            restored.CountOf(null).ShouldBe(3);
            restored.DistinctCount.ShouldBe(1);
        }

        [Fact]
        public void MultiList_RoundTrip_Empty()
        {
            var bag = new MultiList<string>();

            var model = bag.ToSerializableModel();
            var restored = MultiList<string>.FromModel(model);

            model.Items.ShouldBeEmpty();
            model.Counts.ShouldBeEmpty();
            restored.TotalCount.ShouldBe(0);
            restored.DistinctCount.ShouldBe(0);
        }

        [Fact]
        public void MultiList_FromModel_UsesTheSuppliedComparer()
        {
            var model = new MultiListModel<string>
            {
                Items = new List<string> { "Apple" },
                Counts = new List<int> { 2 },
            };

            var restored = MultiList<string>.FromModel(model, StringComparer.OrdinalIgnoreCase);

            restored.CountOf("APPLE").ShouldBe(2);
            restored.Contains("apple").ShouldBeTrue();
            restored.DistinctCount.ShouldBe(1);
        }

        [Fact]
        public void MultiList_FromModel_MergesElementsTheComparerCallsEqual()
        {
            var model = new MultiListModel<string>
            {
                Items = new List<string> { "a", "A" },
                Counts = new List<int> { 1, 2 },
            };

            var restored = MultiList<string>.FromModel(model, StringComparer.OrdinalIgnoreCase);

            restored.DistinctCount.ShouldBe(1);
            restored.CountOf("A").ShouldBe(3);
        }

        [Fact]
        public void MultiList_ModelIsASnapshot_WhileAsReadOnlyIsALiveView()
        {
            var bag = new MultiList<string> { "a" };
            var view = bag.AsReadOnly();
            var model = bag.ToSerializableModel();

            bag.Add("b");

            view.Count.ShouldBe(2);
            model.Items.Count.ShouldBe(1);
            model.Counts[0].ShouldBe(1);
        }

        [Fact]
        public void MultiList_ModelCarriesNulls_WhileToDictionaryRefuses()
        {
            var bag = new MultiList<string>();
            bag.Add(null, 2);

            // ToDictionary() can not represent a null element as a dictionary key ...
            Should.Throw<InvalidOperationException>(() => bag.ToDictionary());

            // ... the model represents it like any other element.
            var model = bag.ToSerializableModel();
            model.Items.Single().ShouldBeNull();
            model.Counts.Single().ShouldBe(2);
        }

        [Fact]
        public void MultiList_FromModel_RejectsANullModel()
        {
            Should.Throw<ArgumentNullException>(() => MultiList<string>.FromModel(null));
        }

        [Fact]
        public void MultiList_FromModel_RejectsANullItemsList()
        {
            var model = new MultiListModel<string> { Items = null, Counts = new List<int>() };

            Should.Throw<ArgumentException>(() => MultiList<string>.FromModel(model));
        }

        [Fact]
        public void MultiList_FromModel_RejectsANullCountsList()
        {
            var model = new MultiListModel<string> { Items = new List<string>(), Counts = null };

            Should.Throw<ArgumentException>(() => MultiList<string>.FromModel(model));
        }

        [Fact]
        public void MultiList_FromModel_RejectsLengthMismatch()
        {
            var model = new MultiListModel<string>
            {
                Items = new List<string> { "a", "b" },
                Counts = new List<int> { 1 },
            };

            Should.Throw<ArgumentException>(() => MultiList<string>.FromModel(model));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public void MultiList_FromModel_RejectsANonPositiveCount(int count)
        {
            var model = new MultiListModel<string>
            {
                Items = new List<string> { "a" },
                Counts = new List<int> { count },
            };

            Should.Throw<ArgumentException>(() => MultiList<string>.FromModel(model));
        }

        [Fact]
        public void MultiList_FromModel_DoesNotMutateTheModel()
        {
            var model = new MultiListModel<string>
            {
                Items = new List<string> { "b", "a" },
                Counts = new List<int> { 2, 1 },
            };

            var restored = MultiList<string>.FromModel(model);

            // The model is read, never rewritten or reordered.
            model.Items.ShouldBe(new[] { "b", "a" });
            model.Counts.ShouldBe(new[] { 2, 1 });
            restored.CountOf("b").ShouldBe(2);
        }

        // ------------------------------------------------------------------
        // MultiList<T>: JSON round trip through System.Text.Json
        // ------------------------------------------------------------------

        [Fact]
        public void MultiList_JsonRoundTrip_IsLossless()
        {
            var bag = new MultiList<string>();
            bag.Add("apple", 2);
            bag.Add("banana", 1);

            var json = JsonSerializer.Serialize(bag.ToSerializableModel());
            var typed = JsonSerializer.Deserialize<MultiListModel<string>>(json);
            var restored = MultiList<string>.FromModel(typed);

            restored.Equals(bag).ShouldBeTrue();
        }

        [Fact]
        public void MultiList_JsonRoundTrip_CarriesNullElements()
        {
            var bag = new MultiList<string>();
            bag.Add(null, 3);
            bag.Add("a", 1);

            var json = JsonSerializer.Serialize(bag.ToSerializableModel());
            var typed = JsonSerializer.Deserialize<MultiListModel<string>>(json);
            var restored = MultiList<string>.FromModel(typed);

            restored.CountOf(null).ShouldBe(3);
            restored.CountOf("a").ShouldBe(1);
        }

        [Fact]
        public void MultiList_JsonRoundTrip_ComparerIsSuppliedSeparately()
        {
            var bag = new MultiList<string>(StringComparer.OrdinalIgnoreCase);
            bag.Add("Apple", 2);

            var json = JsonSerializer.Serialize(bag.ToSerializableModel());
            var typed = JsonSerializer.Deserialize<MultiListModel<string>>(json);
            var restored = MultiList<string>.FromModel(typed, StringComparer.OrdinalIgnoreCase);

            restored.CountOf("APPLE").ShouldBe(2);
        }

        // ------------------------------------------------------------------
        // MultiDictionary<TKey, TValue>: shape and round trip
        // ------------------------------------------------------------------

        [Fact]
        public void MultiDictionary_ToSerializableModel_ListsKeysAndTheirValuesInParallel()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("orders", 1001);
            map.Add("orders", 1002);
            map.Add("carts", 7);

            var model = map.ToSerializableModel();

            model.Keys.ShouldBe(new[] { "orders", "carts" });
            model.Values.Count.ShouldBe(2);
            model.Values[0].ShouldBe(new[] { 1001, 1002 });
            model.Values[1].ShouldBe(new[] { 7 });
        }

        [Fact]
        public void MultiDictionary_RoundTrip_PreservesKeysAndValues()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("orders", 1001);
            map.Add("orders", 1002);
            map.Add("carts", 7);

            var restored = MultiDictionary<string, int>.FromModel(map.ToSerializableModel());

            restored.Count.ShouldBe(2);
            restored.ValueCount("orders").ShouldBe(2);
            restored["orders"].ShouldBe(new[] { 1001, 1002 });
            restored.ValueCount("carts").ShouldBe(1);
        }

        [Fact]
        public void MultiDictionary_RoundTrip_PreservesNullValues()
        {
            var map = new MultiDictionary<string, string>();
            map.Add("k", null);
            map.Add("k", "v");

            var restored = MultiDictionary<string, string>.FromModel(map.ToSerializableModel());

            restored.ValueCount("k").ShouldBe(2);
            restored.Contains("k", null).ShouldBeTrue();
            restored.ValueCount("k").ShouldBe(map.ValueCount("k"));
        }

        [Fact]
        public void MultiDictionary_RoundTrip_PreservesKeyAndValueOrder()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("b", 2);
            map.Add("a", 1);
            map.Add("b", 3);

            var restored = MultiDictionary<string, int>.FromModel(map.ToSerializableModel());

            restored.Keys.ShouldBe(new[] { "b", "a" });
            restored["b"].ShouldBe(new[] { 2, 3 });
        }

        [Fact]
        public void MultiDictionary_RoundTrip_Empty()
        {
            var map = new MultiDictionary<string, int>();

            var model = map.ToSerializableModel();
            var restored = MultiDictionary<string, int>.FromModel(model);

            model.Keys.ShouldBeEmpty();
            model.Values.ShouldBeEmpty();
            restored.Count.ShouldBe(0);
            restored.TotalValueCount.ShouldBe(0);
        }

        [Fact]
        public void MultiDictionary_FromModel_UsesTheSuppliedComparer()
        {
            var model = new MultiDictionaryModel<string, int>
            {
                Keys = new List<string> { "Orders" },
                Values = new List<List<int>> { new List<int> { 1 } },
            };

            var restored = MultiDictionary<string, int>.FromModel(model, StringComparer.OrdinalIgnoreCase);

            restored.ContainsKey("ORDERS").ShouldBeTrue();
            restored.ValueCount("orders").ShouldBe(1);
        }

        [Fact]
        public void MultiDictionary_FromModel_CanCollapseDuplicateValues()
        {
            var model = new MultiDictionaryModel<string, int>
            {
                Keys = new List<string> { "k" },
                Values = new List<List<int>> { new List<int> { 1, 1, 2 } },
            };

            var restored = MultiDictionary<string, int>.FromModel(model, null, allowDuplicateValues: false);

            restored.ValueCount("k").ShouldBe(2);
            restored["k"].ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public void MultiDictionary_ModelIsASnapshot_WhileToDictionaryInnerCollectionsAreLive()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("k", 1);

            var snapshot = map.ToDictionary();
            var view = map.AsReadOnly();
            var model = map.ToSerializableModel();

            map.Add("k", 2);

            snapshot["k"].Count.ShouldBe(2);
            view["k"].Count.ShouldBe(2);
            map.ValueCount("k").ShouldBe(2);

            // The model is the one export that does not move.
            model.Values[0].Count.ShouldBe(1);
        }

        [Fact]
        public void MultiDictionary_FromModel_RejectsANullModel()
        {
            Should.Throw<ArgumentNullException>(() => MultiDictionary<string, int>.FromModel(null));
        }

        [Fact]
        public void MultiDictionary_FromModel_RejectsANullKeysList()
        {
            var model = new MultiDictionaryModel<string, int> { Keys = null, Values = new List<List<int>>() };

            Should.Throw<ArgumentException>(() => MultiDictionary<string, int>.FromModel(model));
        }

        [Fact]
        public void MultiDictionary_FromModel_RejectsANullValuesList()
        {
            var model = new MultiDictionaryModel<string, int> { Keys = new List<string>(), Values = null };

            Should.Throw<ArgumentException>(() => MultiDictionary<string, int>.FromModel(model));
        }

        [Fact]
        public void MultiDictionary_FromModel_RejectsLengthMismatch()
        {
            var model = new MultiDictionaryModel<string, int>
            {
                Keys = new List<string> { "a", "b" },
                Values = new List<List<int>> { new List<int> { 1 } },
            };

            Should.Throw<ArgumentException>(() => MultiDictionary<string, int>.FromModel(model));
        }

        [Fact]
        public void MultiDictionary_FromModel_RejectsANullInnerValueList()
        {
            var model = new MultiDictionaryModel<string, int>
            {
                Keys = new List<string> { "a" },
                Values = new List<List<int>> { null },
            };

            Should.Throw<ArgumentException>(() => MultiDictionary<string, int>.FromModel(model));
        }

        [Fact]
        public void MultiDictionary_FromModel_RejectsANullKey()
        {
            var model = new MultiDictionaryModel<string, int>
            {
                Keys = new List<string> { null },
                Values = new List<List<int>> { new List<int> { 1 } },
            };

            // A multimap rejects null keys; the model can hold one, so the rebuild surfaces the
            // target type's own contract rather than inventing a laxer rule.
            Should.Throw<ArgumentNullException>(() => MultiDictionary<string, int>.FromModel(model));
        }

        // ------------------------------------------------------------------
        // MultiDictionary<TKey, TValue>: JSON round trip
        // ------------------------------------------------------------------

        [Fact]
        public void MultiDictionary_JsonRoundTrip_IsLossless()
        {
            var map = new MultiDictionary<string, int>();
            map.Add("orders", 1001);
            map.Add("orders", 1002);
            map.Add("carts", 7);

            var json = JsonSerializer.Serialize(map.ToSerializableModel());
            var typed = JsonSerializer.Deserialize<MultiDictionaryModel<string, int>>(json);
            var restored = MultiDictionary<string, int>.FromModel(typed);

            restored.Keys.ShouldBe(map.Keys);
            restored.ValueCount("orders").ShouldBe(2);
            restored["orders"].ShouldBe(new[] { 1001, 1002 });
        }

        [Fact]
        public void MultiDictionary_JsonRoundTrip_CarriesNullValues()
        {
            var map = new MultiDictionary<string, string>();
            map.Add("k", null);
            map.Add("k", "v");

            var json = JsonSerializer.Serialize(map.ToSerializableModel());
            var typed = JsonSerializer.Deserialize<MultiDictionaryModel<string, string>>(json);
            var restored = MultiDictionary<string, string>.FromModel(typed);

            restored.ValueCount("k").ShouldBe(2);
            restored.Contains("k", null).ShouldBeTrue();
        }

        // ------------------------------------------------------------------
        // The models force no serializer on anyone
        // ------------------------------------------------------------------

        [Fact]
        public void Models_CarryNoSerializerAttributes()
        {
            // The shape is plain data: any serializer can consume it and none is required. The
            // compiler is allowed to add its own nullability metadata
            // ([NullableContext]/[Nullable]) - that is what "Nullable enable" does - but nothing
            // that names, targets or configures a serializer may appear.
            var serializerNamespaces = new[]
            {
                "System.Text.Json",
                "Newtonsoft.Json",
                "System.Runtime.Serialization",
                "System.Xml.Serialization",
                "System.ComponentModel.DataAnnotations",
            };

            SerializerAttributesOn(typeof(MultiListModel<string>)).ShouldBeEmpty();
            SerializerAttributesOn(typeof(MultiDictionaryModel<string, int>)).ShouldBeEmpty();

            foreach (var property in typeof(MultiListModel<string>).GetProperties())
            {
                SerializerAttributesOn(property).ShouldBeEmpty();
            }

            foreach (var property in typeof(MultiDictionaryModel<string, int>).GetProperties())
            {
                SerializerAttributesOn(property).ShouldBeEmpty();
            }

            object[] SerializerAttributesOn(MemberInfo member)
            {
                return member.GetCustomAttributes(inherit: false)
                    .Cast<Attribute>()
                    .Where(attribute => serializerNamespaces.Any(
                        ns => attribute.GetType().Namespace != null
                              && attribute.GetType().Namespace.StartsWith(ns, StringComparison.Ordinal)))
                    .Cast<object>()
                    .ToArray();
            }
        }

        [Fact]
        public void Assembly_DoesNotReferenceAnySerializer()
        {
            // "System.Text.Json is an optional path" is a claim about the shipped assembly, so it is
            // asserted against the shipped assembly's metadata rather than trusted.
            var referenced = typeof(MultiList<>).Assembly
                .GetReferencedAssemblies()
                .Select(name => name.Name)
                .ToList();

            referenced.ShouldNotContain("System.Text.Json");
            referenced.ShouldNotContain("Newtonsoft.Json");
            referenced.ShouldNotContain("System.Runtime.Serialization.Json");
        }
    }
}
