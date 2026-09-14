using System.Collections.Generic;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// F6-24 revert guard: the inner collections of the multimaps must reach consumers through
    /// the internal live <see cref="ReadOnlyCollectionView{T}"/> wrapper, never through a bare
    /// cast. A cast compiles and passes on modern runtimes (their HashSet&lt;T&gt; carries the
    /// interface), so only a structural check like this catches a revert to the pre-fix
    /// exposure - the runtime failure mode lives on the net451/net461 generation, pinned by the
    /// NetFxTests project.
    /// </summary>
    public class ReadOnlyViewWrapperPinningTests
    {
        [Fact]
        public void MultiDictionary_Indexer_WrapsTheInnerCollection()
        {
            var deduplicating = new MultiDictionary<int, string>(allowDuplicateValues: false);
            deduplicating.Add(1, "a");
            deduplicating[1].ShouldBeOfType<ReadOnlyCollectionView<string>>();

            var duplicating = new MultiDictionary<int, string>();
            duplicating.Add(1, "a");
            duplicating[1].ShouldBeOfType<ReadOnlyCollectionView<string>>();
        }

        [Fact]
        public void OrderedMultiDictionary_Indexer_WrapsTheInnerCollection()
        {
            var deduplicating = new OrderedMultiDictionary<int, string>(allowDuplicateValues: false);
            deduplicating.Add(1, "a");
            deduplicating[1].ShouldBeOfType<ReadOnlyCollectionView<string>>();

            var duplicating = new OrderedMultiDictionary<int, string>();
            duplicating.Add(1, "a");
            duplicating[1].ShouldBeOfType<ReadOnlyCollectionView<string>>();
        }

        [Fact]
        public void MultiKeyMultiDictionary_Indexer_WrapsTheInnerCollection()
        {
            var map = new MultiKeyMultiDictionary<string, int>(allowDuplicateValues: false);
            map.Add(new[] { "eu", "de" }, 1);
            map[new[] { "eu", "de" }].ShouldBeOfType<ReadOnlyCollectionView<int>>();
        }
    }
}
