using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    public class MultiDictionaryValueSetOperationsTests
    {
        private static MultiDictionary<string, int> NewDict()
        {
            return new MultiDictionary<string, int>();
        }

        [Fact]
        public void UnionWith_AddsOnlyAbsentValues()
        {
            var dict = NewDict();
            dict.AddRange("a", new[] { 1, 2 });

            dict.UnionWith("a", new[] { 2, 3 });

            dict["a"].OrderBy(x => x).ShouldBe(new[] { 1, 2, 3 });
            dict.TotalValueCount.ShouldBe(3);
        }

        [Fact]
        public void UnionWith_CreatesKey_WhenAbsent()
        {
            var dict = NewDict();

            dict.UnionWith("k", new[] { 1, 1, 2 });

            dict.ContainsKey("k").ShouldBeTrue();
            dict["k"].OrderBy(x => x).ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public void UnionWith_DoesNotDuplicateInputValues()
        {
            var dict = NewDict();
            dict.Add("a", 1);

            dict.UnionWith("a", new[] { 1, 1, 2, 2 });

            dict["a"].OrderBy(x => x).ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public void UnionWith_NullValues_ThrowsArgumentNullException()
        {
            var dict = NewDict();
            Should.Throw<ArgumentNullException>(() => dict.UnionWith("k", null));
        }

        [Fact]
        public void IntersectionWith_KeepsCommonValues()
        {
            var dict = NewDict();
            dict.AddRange("a", new[] { 1, 2, 3 });

            dict.IntersectionWith("a", new[] { 2, 3, 4 });

            dict["a"].OrderBy(x => x).ShouldBe(new[] { 2, 3 });
            dict.ContainsKey("a").ShouldBeTrue();
        }

        [Fact]
        public void IntersectionWith_EmptyResult_RemovesKey()
        {
            var dict = NewDict();
            dict.AddRange("a", new[] { 1, 2 });

            dict.IntersectionWith("a", new[] { 3 });

            dict.ContainsKey("a").ShouldBeFalse();
        }

        [Fact]
        public void IntersectionWith_MissingKey_NoOp()
        {
            var dict = NewDict();

            dict.IntersectionWith("missing", new[] { 1 });

            dict.ContainsKey("missing").ShouldBeFalse();
        }

        [Fact]
        public void ExceptWith_RemovesAllOccurrences()
        {
            var dict = NewDict();
            dict.Add("a", 1);
            dict.Add("a", 1);
            dict.Add("a", 2);

            dict.ExceptWith("a", new[] { 1 });

            dict["a"].ShouldBe(new[] { 2 });
            dict.TotalValueCount.ShouldBe(1);
        }

        [Fact]
        public void ExceptWith_EmptyResult_RemovesKey()
        {
            var dict = NewDict();
            dict.Add("a", 1);

            dict.ExceptWith("a", new[] { 1, 9 });

            dict.ContainsKey("a").ShouldBeFalse();
        }

        [Fact]
        public void ExceptWith_MissingKey_NoOp()
        {
            var dict = NewDict();

            dict.ExceptWith("missing", new[] { 1 });

            dict.ContainsKey("missing").ShouldBeFalse();
        }

        [Fact]
        public void ExceptWith_RemovesNullValue()
        {
            var dict = new MultiDictionary<string, string>();
            dict.Add("k", null);
            dict.Add("k", "v");

            dict.ExceptWith("k", new[] { (string)null });

            dict["k"].ShouldBe(new[] { "v" });
        }

        [Fact]
        public void UnionWith_WithDuplicatingInner_KeepsExistingMultiplicities()
        {
            var dict = new MultiDictionary<string, int>(true);
            dict.Add("a", 1);
            dict.Add("a", 1);
            dict.Add("a", 2);

            dict.UnionWith("a", new[] { 2, 3 });

            dict["a"].Count.ShouldBe(4);
            dict.TotalValueCount.ShouldBe(4);
        }
    }
}
