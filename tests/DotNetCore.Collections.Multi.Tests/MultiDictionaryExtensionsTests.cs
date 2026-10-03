using System;
using System.Collections.Generic;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// F6-45: the multimap construction entry points (<c>IndexBy</c>, <c>UniqueIndexBy</c>,
    /// <c>AsMultiDictionary</c>) that build a multimap from a sequence or a read-only dictionary.
    /// </summary>
    public class MultiDictionaryExtensionsTests
    {
        // ------------------------------------------------------------------
        // IndexBy
        // ------------------------------------------------------------------

        [Fact]
        public void IndexBy_GroupsElementsByKey()
        {
            var words = new[] { "ant", "ape", "bee" };

            var byFirstLetter = words.IndexBy(w => w[0]);

            byFirstLetter.KeyCount.ShouldBe(2);
            byFirstLetter['a'].ShouldBe(new[] { "ant", "ape" });
            byFirstLetter['b'].ShouldBe(new[] { "bee" });
        }

        [Fact]
        public void IndexBy_KeepsDuplicates()
        {
            var numbers = new[] { 1, 2, 3, 4, 5 };

            var byParity = numbers.IndexBy(n => n % 2);

            byParity[1].ShouldBe(new[] { 1, 3, 5 });
            byParity[0].ShouldBe(new[] { 2, 4 });
        }

        [Fact]
        public void IndexBy_EmptySource_ProducesEmptyMap()
        {
            var byFirstLetter = Array.Empty<string>().IndexBy(w => w[0]);

            byFirstLetter.KeyCount.ShouldBe(0);
        }

        [Fact]
        public void IndexBy_IsAnIndependentCopy()
        {
            var source = new List<string> { "ant", "bee" };

            var map = source.IndexBy(w => w[0]);

            source.Add("ape");
            map['a'].Count.ShouldBe(1);

            map.Add('c', "cat");
            source.ShouldNotContain("cat");
        }

        [Fact]
        public void IndexBy_ComparerIsUsedForKeys()
        {
            var headers = new[] { "A:1", "a:2", "B:3" };

            var byName = headers.IndexBy(h => h.Substring(0, 1), StringComparer.OrdinalIgnoreCase);

            byName.KeyCount.ShouldBe(2);
            byName["A"].Count.ShouldBe(2);
        }

        [Fact]
        public void IndexBy_NullSource_Throws()
        {
            Should.Throw<ArgumentNullException>(
                () => ((IEnumerable<string>)null!).IndexBy(w => w[0]));
        }

        [Fact]
        public void IndexBy_NullKeySelector_Throws()
        {
            Should.Throw<ArgumentNullException>(
                () => new[] { "a" }.IndexBy((Func<string, char>)null!));
        }

        [Fact]
        public void IndexBy_NullKeyFromSelector_Throws()
        {
            Should.Throw<ArgumentNullException>(
                () => new[] { "a" }.IndexBy(w => (string)null!));
        }

        // ------------------------------------------------------------------
        // UniqueIndexBy
        // ------------------------------------------------------------------

        [Fact]
        public void UniqueIndexBy_MapsEachKeyToItsElement()
        {
            var words = new[] { "ant", "bee" };

            var byWord = words.UniqueIndexBy(w => w);

            byWord.Count.ShouldBe(2);
            byWord["ant"].ShouldBe("ant");
            byWord.ContainsRight("bee").ShouldBeTrue();
        }

        [Fact]
        public void UniqueIndexBy_DuplicateKey_Throws()
        {
            var words = new[] { "ant", "ape" };

            var exception = Should.Throw<ArgumentException>(() => words.UniqueIndexBy(w => w[0]));

            exception.Message.ShouldContain("not uniquely indexed");
        }

        [Fact]
        public void UniqueIndexBy_DuplicateElementUnderDistinctKeys_Throws()
        {
            // The rows are equal (equality ignores Id) but indexed by their distinct Id, so the
            // one-to-one BiDictionary rejects the second right value.
            var rows = new[] { new Row(1, "x"), new Row(2, "x") };

            Should.Throw<ArgumentException>(() => rows.UniqueIndexBy(r => r.Id));
        }

        [Fact]
        public void UniqueIndexBy_ComparerIsUsedForKeys()
        {
            var words = new[] { "ant", "Bee" };

            var byWord = words.UniqueIndexBy(w => w, StringComparer.OrdinalIgnoreCase);

            byWord["ANT"].ShouldBe("ant");
        }

        [Fact]
        public void UniqueIndexBy_NullSource_Throws()
        {
            Should.Throw<ArgumentNullException>(
                () => ((IEnumerable<string>)null!).UniqueIndexBy(w => w));
        }

        // ------------------------------------------------------------------
        // AsMultiDictionary
        // ------------------------------------------------------------------

        [Fact]
        public void AsMultiDictionary_CopiesEveryPair()
        {
            var stock = new Dictionary<string, int> { ["widget"] = 7, ["gadget"] = 3 };

            var map = stock.AsMultiDictionary();

            map.KeyCount.ShouldBe(2);
            map["widget"].ShouldBe(new[] { 7 });
            map["gadget"].ShouldBe(new[] { 3 });
        }

        [Fact]
        public void AsMultiDictionary_IsAnIndependentCopy()
        {
            var stock = new Dictionary<string, int> { ["widget"] = 7 };

            var map = stock.AsMultiDictionary();

            map.Add("widget", 9);

            stock["widget"].ShouldBe(7);
            map["widget"].Count.ShouldBe(2);
        }

        [Fact]
        public void AsMultiDictionary_CarriesTheDictionaryComparer()
        {
            var stock = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Widget"] = 7
            };

            var map = stock.AsMultiDictionary();

            map.ContainsKey("widget").ShouldBeTrue();
        }

        [Fact]
        public void AsMultiDictionary_Null_Throws()
        {
            Should.Throw<ArgumentNullException>(
                () => ((IReadOnlyDictionary<string, int>)null!).AsMultiDictionary());
        }

        /// <summary>
        /// A type whose equality ignores <see cref="Id"/>, used to show that the one-to-one
        /// <see cref="BiDictionary{TLeft,TRight}"/> rejects a repeated element even when the keys
        /// differ.
        /// </summary>
        private sealed class Row
        {
            public Row(int id, string label)
            {
                Id = id;
                Label = label;
            }

            public int Id { get; }

            public string Label { get; }

            public override bool Equals(object? obj)
            {
                return obj is Row other && other.Label == Label;
            }

            public override int GetHashCode()
            {
                return Label.GetHashCode();
            }
        }
    }
}
