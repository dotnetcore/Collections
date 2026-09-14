using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.NetFxTests
{
    /// <summary>
    /// F6-24: runtime regression tests pinning the inner-collection exposure on the affected
    /// .NET Framework generation. The framework's <c>HashSet&lt;T&gt;</c> and
    /// <c>SortedSet&lt;T&gt;</c> do not declare <c>IReadOnlyCollection&lt;T&gt;</c> on the
    /// net451 / net461 <em>reference assemblies</em> (so a direct assignment fails to compile
    /// there), and the 4.5.1 / 4.6.1-era runtimes lack the interface itself - a machine still
    /// running one of those generations would throw <see cref="InvalidCastException"/> on the
    /// pre-fix bare cast, exactly on the deduplicating inner collections
    /// (<c>allowDuplicateValues: false</c>) of <see cref="MultiDictionary{TKey,TValue}"/> and
    /// <see cref="OrderedMultiDictionary{TKey,TValue}"/>. A machine whose runtime has since been
    /// upgraded in place (4.8) happens to carry the interface, which is why a net8.0-only test
    /// run can never see any of this; these tests execute against a .NET Framework target and
    /// hold the exposure working at runtime there. The exposure now goes through an internal
    /// live wrapper (no interface declaration needed at all), and the wrapper itself is pinned
    /// structurally by the main net8.0 suite.
    /// </summary>
    public class ReadOnlyViewNetFxRegressionTests
    {
        // ------------------------------------------------------------------
        // MultiDictionary - the deduplicating (HashSet) inner collection
        // ------------------------------------------------------------------

        [Fact]
        public void MultiDictionary_Indexer_WithHashSetInner_ReadsAtRuntime()
        {
            var map = new MultiDictionary<int, string>(allowDuplicateValues: false);
            map.Add(1, "a");
            map.Add(1, "b");

            var values = map[1];
            values.ShouldNotBeNull();
            values.Count.ShouldBe(2);
            values.ShouldContain("a");
            values.ShouldContain("b");
        }

        [Fact]
        public void MultiDictionary_TryGetValue_WithHashSetInner_ReadsAtRuntime()
        {
            var map = new MultiDictionary<int, string>(allowDuplicateValues: false);
            map.Add(1, "a");

            map.TryGetValue(1, out var values).ShouldBeTrue();
            values.ShouldNotBeNull();
            values.ShouldContain("a");
        }

        [Fact]
        public void MultiDictionary_AsReadOnly_WithHashSetInner_EnumeratesAtRuntime()
        {
            var map = new MultiDictionary<int, string>(allowDuplicateValues: false);
            map.Add(1, "a");
            map.Add(2, "b");

            var view = map.AsReadOnly();
            view.Count.ShouldBe(2);
            view.ContainsKey(2).ShouldBeTrue();
            view[2].ShouldContain("b");
            view.Single(pair => pair.Key == 1).Value.ShouldContain("a");
        }

        [Fact]
        public void MultiDictionary_AsReverse_WithHashSetInner_KeySetsReadAtRuntime()
        {
            var map = new MultiDictionary<int, string>(allowDuplicateValues: false);
            map.Add(1, "a");
            map.Add(2, "a");

            var inverted = map.AsReverse();
            inverted.Count.ShouldBe(1);
            inverted["a"].ShouldBe(new[] { 1, 2 }, ignoreOrder: true);
        }

        [Fact]
        public void MultiDictionary_Indexer_ViewIsLive()
        {
            var map = new MultiDictionary<int, string>(allowDuplicateValues: false);
            map.Add(1, "a");

            var values = map[1];
            map.Add(1, "b");

            // The wrapper is a live view over the inner collection, like the cast was on the
            // modern targets.
            values.Count.ShouldBe(2);
            values.ShouldContain("b");
        }

        // ------------------------------------------------------------------
        // OrderedMultiDictionary - the deduplicating (SortedSet) inner collection
        // ------------------------------------------------------------------

        [Fact]
        public void OrderedMultiDictionary_Indexer_WithSortedSetInner_ReadsAtRuntime()
        {
            var map = new OrderedMultiDictionary<int, string>(allowDuplicateValues: false);
            map.Add(1, "a");
            map.Add(1, "b");

            var values = map[1];
            values.ShouldNotBeNull();
            values.Count.ShouldBe(2);
            values.ShouldContain("a");
        }

        [Fact]
        public void OrderedMultiDictionary_AsReadOnly_WithSortedSetInner_EnumeratesAtRuntime()
        {
            var map = new OrderedMultiDictionary<int, string>(allowDuplicateValues: false);
            map.Add(1, "a");
            map.Add(2, "b");

            var view = map.AsReadOnly();
            view.Count.ShouldBe(2);
            view[1].ShouldContain("a");
        }

        // ------------------------------------------------------------------
        // F6-05 / F6-06 regression: the newer types expose through the wrapper from day one
        // ------------------------------------------------------------------

        [Fact]
        public void MultiKeyMultiDictionary_Indexer_WithHashSetInner_ReadsAtRuntime()
        {
            var map = new MultiKeyMultiDictionary<string, int>(allowDuplicateValues: false);
            map.Add(new[] { "eu", "de" }, 1);
            map.Add(new[] { "eu", "de" }, 2);

            var values = map[new[] { "eu", "de" }];
            values.ShouldNotBeNull();
            values.Count.ShouldBe(2);
            values.ShouldContain(1);
        }

        [Fact]
        public void MultiDictionary_Indexer_WithListInner_StillReadsAtRuntime()
        {
            // The duplicating (List) path always worked on the framework targets - pin that it
            // keeps working after the wrapper change.
            var map = new MultiDictionary<int, string>();
            map.Add(1, "a");
            map.Add(1, "a");

            map[1].Count.ShouldBe(2);
        }
    }
}
