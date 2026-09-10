using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.Tests
{
    // M6-03 / L-02: MultiList<T> structural (multiset) equality.
    // Equality is "same distinct elements, same copy counts, any order" - not set equality
    // and not sequence equality - and GetHashCode has to agree with it.
    public class MultiListEqualityTests
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

        /// <summary>
        /// An ordinal-ignore-case comparer that is a different instance from
        /// <see cref="StringComparer.OrdinalIgnoreCase"/> yet behaves the same, used to prove that
        /// equality does not compare comparer identity.
        /// </summary>
        private sealed class OwnIgnoreCaseComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase);

            public int GetHashCode(string obj) => obj == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(obj);
        }

        // ------------------------------------------------------------ order independence

        [Fact]
        public void Equals_IgnoresEnumerationOrder()
        {
            Bag("a", "a", "b").Equals(Bag("b", "a", "a")).ShouldBeTrue();
            Bag("a", "b", "c").Equals(Bag("c", "b", "a")).ShouldBeTrue();
        }

        [Fact]
        public void Equals_EmptyMultisetsAreEqual()
        {
            new MultiList<string>().Equals(new MultiList<string>()).ShouldBeTrue();
        }

        [Fact]
        public void Equals_SameInstanceIsEqual()
        {
            var bag = Bag("a", "b");

            bag.Equals(bag).ShouldBeTrue();
        }

        // ------------------------------------------------------------ copy counts matter

        [Fact]
        public void Equals_DifferentCopyCountsAreNotEqual()
        {
            Bag("a", "a", "b").Equals(Bag("a", "b")).ShouldBeFalse();
            Bag("a").Equals(Bag("a", "a")).ShouldBeFalse();
        }

        [Fact]
        public void Equals_DifferentElementsAreNotEqual()
        {
            Bag("a", "b").Equals(Bag("a", "c")).ShouldBeFalse();
        }

        [Fact]
        public void Equals_SubsetIsNotEqual()
        {
            // The laxer question (IsSubsetOf) says yes, bag equality has to say no.
            var small = Bag("a");
            var large = Bag("a", "b");

            small.IsSubsetOf(large).ShouldBeTrue();
            small.Equals(large).ShouldBeFalse();
            large.Equals(small).ShouldBeFalse();
        }

        // ------------------------------------------------------------ null semantics

        [Fact]
        public void Equals_NullElementsCountLikeAnyOtherElement()
        {
            Bag(null, null, "a").Equals(Bag("a", null, null)).ShouldBeTrue();
            Bag(null, null, "a").Equals(Bag(null, "a")).ShouldBeFalse();
        }

        [Fact]
        public void Equals_NullOnlyMultisets()
        {
            // (string) null, not null: a bare null argument would bind to the params array itself.
            Bag((string) null).Equals(Bag((string) null)).ShouldBeTrue();
            Bag((string) null).Equals(Bag(null, null)).ShouldBeFalse();
            Bag((string) null).Equals(new MultiList<string>()).ShouldBeFalse();
            new MultiList<string>().Equals(Bag((string) null)).ShouldBeFalse();
        }

        // ------------------------------------------------------------ null argument / object overload

        [Fact]
        public void Equals_NullIsNeverEqual()
        {
            Bag("a").Equals((MultiList<string>) null).ShouldBeFalse();
            Bag("a").Equals((object) null).ShouldBeFalse();
        }

        [Fact]
        public void Equals_ObjectOverloadAgreesWithTheTypedOne()
        {
            Bag("a", "a", "b").Equals((object) Bag("b", "a", "a")).ShouldBeTrue();
            Bag("a", "b").Equals((object) Bag("a", "c")).ShouldBeFalse();
        }

        [Fact]
        public void Equals_OtherTypesAreNeverEqual()
        {
            Bag("a").Equals("a").ShouldBeFalse();
            Bag("a").Equals(new MultiList<int> { 1 }).ShouldBeFalse();
        }

        // ------------------------------------------------------------ comparer semantics

        [Fact]
        public void Equals_UsesTheComparerToMatchElements()
        {
            // The comparer decides matching, not normalization: "A" is stored as "A", but an
            // ordinal-ignore-case multiset considers "A" and "a" the same element.
            var upper = new MultiList<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            var lower = new MultiList<string>(StringComparer.OrdinalIgnoreCase) { "a" };

            upper.DistinctItems().Single().ShouldBe("A");
            lower.DistinctItems().Single().ShouldBe("a");

            upper.Equals(lower).ShouldBeTrue();
            lower.Equals(upper).ShouldBeTrue();
        }

        [Fact]
        public void Equals_WithDifferentComparers_BothSidesHaveToAgree()
        {
            var ignoreCase = new MultiList<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            var ordinal = new MultiList<string>(StringComparer.Ordinal) { "a" };

            // ignoreCase sees "a" as its own "A" element; ordinal does not see "A" at all.
            // Asking only one side would make the two directions disagree, so both must be false.
            ignoreCase.Equals(ordinal).ShouldBeFalse();
            ordinal.Equals(ignoreCase).ShouldBeFalse();
        }

        [Fact]
        public void Equals_DoesNotCompareComparerIdentity()
        {
            // Two distinct comparer instances with the same semantics: still equal.
            var left = new MultiList<string>(new OwnIgnoreCaseComparer()) { "a" };
            var right = new MultiList<string>(new OwnIgnoreCaseComparer()) { "A" };

            left.Comparer.ShouldNotBeSameAs(right.Comparer);
            left.Equals(right).ShouldBeTrue();
            right.Equals(left).ShouldBeTrue();
        }

        // ------------------------------------------------------------ contract properties

        [Fact]
        public void Equals_IsReflexiveSymmetricAndTransitive()
        {
            var a = Bag("a", "a", "b");
            var b = Bag("b", "a", "a");
            var c = Bag("a", "b", "a");
            var d = Bag("a", "b");

            a.Equals(a).ShouldBeTrue();

            a.Equals(b).ShouldBeTrue();
            b.Equals(a).ShouldBeTrue();

            a.Equals(b).ShouldBeTrue();
            b.Equals(c).ShouldBeTrue();
            a.Equals(c).ShouldBeTrue();

            d.Equals(a).ShouldBeFalse();
            a.Equals(d).ShouldBeFalse();
        }

        [Fact]
        public void Equals_CloneIsEqualAndStaysIndependent()
        {
            var bag = Bag("a", "a", "b");
            var clone = bag.Clone();

            clone.Equals(bag).ShouldBeTrue();
            bag.Equals(clone).ShouldBeTrue();

            clone.Add("c");
            clone.Equals(bag).ShouldBeFalse();
            bag.Equals(clone).ShouldBeFalse();
        }

        [Fact]
        public void Equals_MutationBreaksEquality()
        {
            var a = Bag("a", "b");
            var b = Bag("a", "b");

            a.Equals(b).ShouldBeTrue();

            b.Add("b");
            a.Equals(b).ShouldBeFalse();
            b.Equals(a).ShouldBeFalse();

            b.Remove("b");
            a.Equals(b).ShouldBeTrue();
        }

        [Fact]
        public void Equals_WorksForValueTypeElements()
        {
            var a = new MultiList<int> { 1, 1, 2 };
            var b = new MultiList<int> { 2, 1, 1 };

            a.Equals(b).ShouldBeTrue();
            a.Equals(new MultiList<int> { 1, 2 }).ShouldBeFalse();
        }

        // ------------------------------------------------------------ hash code contract

        [Fact]
        public void GetHashCode_IsStableAcrossOrderAndInstances()
        {
            Bag("a", "a", "b").GetHashCode().ShouldBe(Bag("b", "a", "a").GetHashCode());
            Bag(null, "a").GetHashCode().ShouldBe(Bag("a", null).GetHashCode());
            new MultiList<string>().GetHashCode().ShouldBe(new MultiList<string>().GetHashCode());
        }

        [Fact]
        public void GetHashCode_DoesNotDependOnEnumerationOrder()
        {
            var order = Enumerable.Range(0, 40).Select(i => (i % 7).ToString()).ToArray();
            var shuffled = order.Reverse().ToArray();

            Bag(order).Equals(Bag(shuffled)).ShouldBeTrue();
            Bag(order).GetHashCode().ShouldBe(Bag(shuffled).GetHashCode());
        }

        [Fact]
        public void GetHashCode_ReflectsCopyCountsAndElements()
        {
            // Hash codes are never contractually required to differ, but they must not be
            // degenerate either: the copy counts and the element identities both contribute.
            Bag("a", "a").GetHashCode().ShouldNotBe(Bag("a").GetHashCode());
            Bag("a").GetHashCode().ShouldNotBe(Bag("b").GetHashCode());
            Bag("a", "b").GetHashCode().ShouldNotBe(Bag("a", "c").GetHashCode());
        }

        [Fact]
        public void GetHashCode_AgreesWithEqualsInsideAHashSet()
        {
            // The practical contract: structurally equal multisets must collapse into one entry.
            var set = new HashSet<MultiList<string>>
            {
                Bag("a", "a", "b"),
                Bag("b", "a", "a"),
                Bag("a", "b", "a"),
                Bag("a", "b")
            };

            set.Count.ShouldBe(2);
            set.Contains(Bag("a", "a", "b")).ShouldBeTrue();
            set.Contains(Bag("a", "b")).ShouldBeTrue();
            set.Contains(Bag("a", "a", "a", "b")).ShouldBeFalse();
        }

        [Fact]
        public void GetHashCode_AgreesWithEqualsForNullElements()
        {
            var set = new HashSet<MultiList<string>>
            {
                Bag(null, null, "a"),
                Bag("a", null, null)
            };

            set.Count.ShouldBe(1);
            set.Contains(Bag(null, "a", null)).ShouldBeTrue();
        }

        [Fact]
        public void GetHashCode_IsConsistentAcrossRepeatedCalls()
        {
            var bag = Bag("a", "b", "a", null);

            var first = bag.GetHashCode();
            bag.GetHashCode().ShouldBe(first);
            bag.Add("c");
            bag.Remove("c");
            bag.GetHashCode().ShouldBe(first);
        }
    }
}
