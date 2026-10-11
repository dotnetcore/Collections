using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections;
using DotNetCore.Collections.Internal;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Tests
{
    /// <summary>
    /// F7-06 (fifth batch): the outer-join operators of the P0 polyfill layer - <c>LeftJoin</c> and
    /// <c>RightJoin</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These two are BCL operators from .NET 10 onwards, and they exist on the lower targets only,
    /// so this suite runs on net8.0, where neither the platform's method nor a higher-target
    /// override of ours is reachable. Every assertion therefore compares the engine against a
    /// hand-written join built out of <c>ToLookup</c>, which is the same hash-lookup shape the
    /// operator claims to implement.
    /// </para>
    /// <para>
    /// The oracle reproduces the one detail that is easy to get wrong: the BCL's lookup refuses to
    /// store an element whose key is <see langword="null"/>, so a null-keyed element can never be
    /// matched, on either side of the join.
    /// </para>
    /// </remarks>
    public class PolyfillOuterJoinOperatorTests
    {
        private static readonly (int Id, string Name)[] People =
        {
            (1, "Ada"), (2, "Grace"), (3, "Linus"), (4, "Ken")
        };

        private static readonly (int Owner, string Pet)[] Pets =
        {
            (1, "cat"), (1, "dog"), (2, "owl"), (5, "fox")
        };

        // Both halves are guarded: LeftJoin leaves the pet at default, RightJoin leaves the person
        // at default, and the projection is shared by the two so it has to survive either.
        private static string Project((int Id, string Name) person, (int Owner, string Pet) pet)
            => (person.Name ?? "NONE") + "-" + (pet.Pet ?? "NONE");

        /// <summary>
        /// The left-join oracle: outer order is the primary order, an outer element that matches
        /// several inner elements produces one result per match in inner order, and an outer
        /// element with no match produces one result against <see langword="default"/>.
        /// </summary>
        private static List<TResult> LeftJoinOracle<TOuter, TInner, TKey, TResult>(
            IEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey> comparer = null)
        {
            // The lookup drops null-keyed elements, so the oracle has to drop them too.
            var lookup = inner
                .Where(element => !(innerKeySelector(element) is null))
                .ToLookup(innerKeySelector, comparer);

            var results = new List<TResult>();
            foreach (var outerElement in outer)
            {
                var key = outerKeySelector(outerElement);
                if (!(key is null) && lookup.Contains(key))
                {
                    foreach (var innerElement in lookup[key])
                    {
                        results.Add(resultSelector(outerElement, innerElement));
                    }
                }
                else
                {
                    results.Add(resultSelector(outerElement, default));
                }
            }

            return results;
        }

        /// <summary>
        /// The right-join oracle: inner order is the primary order, an inner element that matches
        /// several outer elements produces one result per match in outer order, and an inner
        /// element with no match produces one result against <see langword="default"/>.
        /// </summary>
        private static List<TResult> RightJoinOracle<TOuter, TInner, TKey, TResult>(
            IEnumerable<TOuter> outer, IEnumerable<TInner> inner,
            Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector,
            Func<TOuter, TInner, TResult> resultSelector, IEqualityComparer<TKey> comparer = null)
        {
            var lookup = outer
                .Where(element => !(outerKeySelector(element) is null))
                .ToLookup(outerKeySelector, comparer);

            var results = new List<TResult>();
            foreach (var innerElement in inner)
            {
                var key = innerKeySelector(innerElement);
                if (!(key is null) && lookup.Contains(key))
                {
                    foreach (var outerElement in lookup[key])
                    {
                        results.Add(resultSelector(outerElement, innerElement));
                    }
                }
                else
                {
                    results.Add(resultSelector(default, innerElement));
                }
            }

            return results;
        }

        /// <summary>A comparer that puts every key in the same bucket, so the lookup's collision
        /// handling is what is being exercised.</summary>
        private sealed class CollidingComparer : IEqualityComparer<int>
        {
            public bool Equals(int x, int y) => x == y;

            public int GetHashCode(int obj) => 0;
        }

        // ----- LeftJoin -----

        [Fact]
        public void LeftJoin_keeps_the_outer_elements_that_have_no_match_on_every_arm()
        {
            IEnumerable<(int Owner, string Pet)> inner = Pets;

            var expected = LeftJoinOracle(People, inner, person => person.Id, pet => pet.Owner, Project);

            IEnumerable<string> fromArray = People.ToValueEnumerable()
                .LeftJoin(inner, person => person.Id, pet => pet.Owner, Project);
            IEnumerable<string> fromList = new List<(int Id, string Name)>(People).ToValueEnumerable()
                .LeftJoin(inner, person => person.Id, pet => pet.Owner, Project);

            IReadOnlyList<(int Id, string Name)> readOnlySource = new List<(int Id, string Name)>(People);
            IEnumerable<string> fromReadOnly = readOnlySource.ToValueEnumerable()
                .LeftJoin(inner, person => person.Id, pet => pet.Owner, Project);

            IEnumerable<(int Id, string Name)> enumerationSource = new Queue<(int Id, string Name)>(People);
            IEnumerable<string> fromEnumerable = enumerationSource.ToValueEnumerable()
                .LeftJoin(inner, person => person.Id, pet => pet.Owner, Project);

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);

            // Ada matches twice, Grace once, and Linus and Ken not at all - the unmatched two are
            // projected against default rather than dropped.
            expected.ShouldBe(new[] { "Ada-cat", "Ada-dog", "Grace-owl", "Linus-NONE", "Ken-NONE" });
        }

        [Fact]
        public void LeftJoin_produces_one_result_per_inner_match_in_inner_order()
        {
            var outer = new[] { 1 };
            var inner = new[] { (Key: 1, Tag: "first"), (Key: 1, Tag: "second"), (Key: 1, Tag: "third") };

            var expected = LeftJoinOracle(outer, inner, value => value, item => item.Key, (value, item) => item.Tag);
            var actual = outer.ToValueEnumerable()
                .LeftJoin(inner, value => value, item => item.Key, (value, item) => item.Tag)
                .ToList();

            actual.ShouldBe(expected);
            actual.ShouldBe(new[] { "first", "second", "third" });
        }

        [Fact]
        public void LeftJoin_treats_a_null_key_as_unmatched()
        {
            var outer = new[] { "a", "b", null };
            var inner = new[] { "a", "b", null };

            var actual = outer.ToValueEnumerable()
                .LeftJoin(inner, value => value, value => value, (left, right) => (left ?? "none") + "/" + (right ?? "none"))
                .ToList();

            // The null-keyed outer element has no match. The null-keyed inner element is invisible
            // for the same reason: the lookup refuses to store it, exactly as the BCL's does.
            actual.ShouldBe(new[] { "a/a", "b/b", "none/none" });
        }

        [Fact]
        public void LeftJoin_honours_a_custom_comparer()
        {
            var outer = new[] { "a", "B" };
            var inner = new[] { "A", "b", "c" };

            var expected = LeftJoinOracle(outer, inner, value => value, value => value, (left, right) => left + "/" + right, StringComparer.OrdinalIgnoreCase);
            var actual = outer.ToValueEnumerable()
                .LeftJoin(inner, value => value, value => value, (left, right) => left + "/" + right, StringComparer.OrdinalIgnoreCase)
                .ToList();

            actual.ShouldBe(expected);
            actual.ShouldBe(new[] { "a/A", "B/b" });
        }

        [Fact]
        public void LeftJoin_is_deferred_and_builds_the_lookup_on_the_first_pull()
        {
            var outerProbe = new CountingEnumerable(4);
            var innerProbe = new CountingEnumerable(4);

            var joined = outerProbe.ToValueEnumerable()
                .LeftJoin<int, int, int, string>((IEnumerable<int>)innerProbe, value => value % 2, value => value % 2, (outer, inner) => outer + ":" + inner);

            // Nothing is read when the pipeline is built, on either side.
            outerProbe.EnumeratorRequests.ShouldBe(0);
            innerProbe.EnumeratorRequests.ShouldBe(0);

            // Keys 0 and 1 each hold two elements, and the outer side has four elements, so the
            // answer is four outer elements times two matches.
            joined.Count().ShouldBe(8);

            // The inner side is folded in one pass; the outer side is streamed in one pass.
            outerProbe.EnumeratorRequests.ShouldBe(1);
            innerProbe.EnumeratorRequests.ShouldBe(1);
        }

        [Fact]
        public void LeftJoin_never_touches_the_inner_sequence_when_the_outer_is_empty()
        {
            var innerProbe = new CountingEnumerable(4);

            var joined = new int[0].ToValueEnumerable()
                .LeftJoin<int, int, int, int>((IEnumerable<int>)innerProbe, value => value, value => value, (outer, inner) => outer + inner);

            joined.ShouldBeEmpty();

            // The lookup is built only once an outer element exists, so an empty outer sequence
            // never pays for folding the inner one.
            innerProbe.EnumeratorRequests.ShouldBe(0);
        }

        [Fact]
        public void LeftJoin_survives_a_reset()
        {
            IEnumerable<(int Owner, string Pet)> inner = Pets;

            var enumerator = People.ToValueEnumerable()
                .LeftJoin(inner, person => person.Id, pet => pet.Owner, Project)
                .GetEnumerator();

            enumerator.MoveNext().ShouldBeTrue();
            var first = enumerator.Current;
            first.ShouldBe("Ada-cat");

            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe("Ada-dog");

            enumerator.Reset();

            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(first);
        }

        [Fact]
        public void LeftJoin_looks_up_correctly_when_every_key_collides()
        {
            var outer = Enumerable.Range(0, 20).ToArray();
            var inner = Enumerable.Range(10, 20).ToArray();
            var comparer = new CollidingComparer();

            var expected = LeftJoinOracle(outer, inner, value => value, value => value, (left, right) => (left * 100) + right, comparer);
            var actual = outer.ToValueEnumerable()
                .LeftJoin(inner, value => value, value => value, (left, right) => (left * 100) + right, comparer)
                .ToList();

            actual.ShouldBe(expected);

            // The first ten outer values match nothing and are projected against default.
            actual[0].ShouldBe(0);
            actual[9].ShouldBe(900);
            actual[10].ShouldBe(1010);
        }

        // ----- RightJoin -----

        [Fact]
        public void RightJoin_keeps_the_inner_elements_that_have_no_match_on_every_arm()
        {
            IEnumerable<(int Owner, string Pet)> inner = Pets;

            var expected = RightJoinOracle(People, inner, person => person.Id, pet => pet.Owner, Project);

            IEnumerable<string> fromArray = People.ToValueEnumerable()
                .RightJoin(inner, person => person.Id, pet => pet.Owner, Project);
            IEnumerable<string> fromList = new List<(int Id, string Name)>(People).ToValueEnumerable()
                .RightJoin(inner, person => person.Id, pet => pet.Owner, Project);

            IReadOnlyList<(int Id, string Name)> readOnlySource = new List<(int Id, string Name)>(People);
            IEnumerable<string> fromReadOnly = readOnlySource.ToValueEnumerable()
                .RightJoin(inner, person => person.Id, pet => pet.Owner, Project);

            IEnumerable<(int Id, string Name)> enumerationSource = new Queue<(int Id, string Name)>(People);
            IEnumerable<string> fromEnumerable = enumerationSource.ToValueEnumerable()
                .RightJoin(inner, person => person.Id, pet => pet.Owner, Project);

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);

            // The fox belongs to nobody, so it survives; Linus and Ken own nothing, so they do not.
            expected.ShouldBe(new[] { "Ada-cat", "Ada-dog", "Grace-owl", "NONE-fox" });
        }

        [Fact]
        public void RightJoin_produces_one_result_per_outer_match_in_outer_order()
        {
            var outer = new[] { (Key: 1, Tag: "first"), (Key: 1, Tag: "second"), (Key: 1, Tag: "third") };
            var inner = new[] { 1 };

            var expected = RightJoinOracle(outer, inner, item => item.Key, value => value, (item, value) => item.Tag);
            var actual = outer.ToValueEnumerable()
                .RightJoin(inner, item => item.Key, value => value, (item, value) => item.Tag)
                .ToList();

            actual.ShouldBe(expected);
            actual.ShouldBe(new[] { "first", "second", "third" });
        }

        [Fact]
        public void RightJoin_treats_a_null_key_as_unmatched()
        {
            var outer = new[] { "a", "b", null };
            var inner = new[] { "a", "b", null };

            var actual = outer.ToValueEnumerable()
                .RightJoin(inner, value => value, value => value, (left, right) => (left ?? "none") + "/" + (right ?? "none"))
                .ToList();

            actual.ShouldBe(new[] { "a/a", "b/b", "none/none" });
        }

        [Fact]
        public void RightJoin_honours_a_custom_comparer()
        {
            var outer = new[] { "A", "b", "c" };
            var inner = new[] { "a", "B" };

            var expected = RightJoinOracle(outer, inner, value => value, value => value, (left, right) => left + "/" + right, StringComparer.OrdinalIgnoreCase);
            var actual = outer.ToValueEnumerable()
                .RightJoin(inner, value => value, value => value, (left, right) => left + "/" + right, StringComparer.OrdinalIgnoreCase)
                .ToList();

            actual.ShouldBe(expected);
            actual.ShouldBe(new[] { "A/a", "b/B" });
        }

        [Fact]
        public void RightJoin_is_deferred_and_never_touches_the_outer_sequence_when_the_inner_is_empty()
        {
            var outerProbe = new CountingEnumerable(4);

            var joined = outerProbe.ToValueEnumerable()
                .RightJoin<int, int, int, int>(new int[0], value => value, value => value, (outer, inner) => outer + inner);

            outerProbe.EnumeratorRequests.ShouldBe(0);

            joined.ShouldBeEmpty();

            // The lookup is built only once an inner element exists, so an empty inner sequence
            // never pays for folding the outer one.
            outerProbe.EnumeratorRequests.ShouldBe(0);
        }

        [Fact]
        public void RightJoin_survives_a_reset()
        {
            IEnumerable<(int Owner, string Pet)> inner = Pets;

            var enumerator = People.ToValueEnumerable()
                .RightJoin(inner, person => person.Id, pet => pet.Owner, Project)
                .GetEnumerator();

            enumerator.MoveNext().ShouldBeTrue();
            var first = enumerator.Current;
            first.ShouldBe("Ada-cat");

            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe("Ada-dog");

            enumerator.Reset();

            enumerator.MoveNext().ShouldBeTrue();
            enumerator.Current.ShouldBe(first);
        }

        // ----- cross-cutting contracts -----

        [Fact]
        public void Null_arguments_are_rejected_by_every_operator()
        {
            var outer = new[] { 1, 2, 3 }.ToValueEnumerable();
            var inner = new[] { 1, 2, 3 };

            Should.Throw<ArgumentNullException>(() => outer.LeftJoin<int, int, int, int>(null, value => value, value => value, (left, right) => left + right));
            Should.Throw<ArgumentNullException>(() => outer.LeftJoin<int, int, int, int>(inner, null, value => value, (left, right) => left + right));
            Should.Throw<ArgumentNullException>(() => outer.LeftJoin<int, int, int, int>(inner, value => value, null, (left, right) => left + right));
            Should.Throw<ArgumentNullException>(() => outer.LeftJoin<int, int, int, int>(inner, value => value, value => value, null));

            Should.Throw<ArgumentNullException>(() => outer.RightJoin<int, int, int, int>(null, value => value, value => value, (left, right) => left + right));
            Should.Throw<ArgumentNullException>(() => outer.RightJoin<int, int, int, int>(inner, null, value => value, (left, right) => left + right));
            Should.Throw<ArgumentNullException>(() => outer.RightJoin<int, int, int, int>(inner, value => value, null, (left, right) => left + right));
            Should.Throw<ArgumentNullException>(() => outer.RightJoin<int, int, int, int>(inner, value => value, value => value, null));
        }

        [Fact]
        public void Null_arguments_are_rejected_on_the_arbitrary_sequence_arm_too()
        {
            IEnumerable<int> raw = new Queue<int>(new[] { 1, 2, 3 });
            var outer = raw.ToValueEnumerable();
            var inner = new[] { 1, 2, 3 };

            Should.Throw<ArgumentNullException>(() => outer.LeftJoin<int, int, int, int>(null, value => value, value => value, (left, right) => left + right));
            Should.Throw<ArgumentNullException>(() => outer.LeftJoin<int, int, int, int>(inner, null, value => value, (left, right) => left + right));
            Should.Throw<ArgumentNullException>(() => outer.LeftJoin<int, int, int, int>(inner, value => value, null, (left, right) => left + right));
            Should.Throw<ArgumentNullException>(() => outer.LeftJoin<int, int, int, int>(inner, value => value, value => value, null));

            Should.Throw<ArgumentNullException>(() => outer.RightJoin<int, int, int, int>(null, value => value, value => value, (left, right) => left + right));
            Should.Throw<ArgumentNullException>(() => outer.RightJoin<int, int, int, int>(inner, null, value => value, (left, right) => left + right));
            Should.Throw<ArgumentNullException>(() => outer.RightJoin<int, int, int, int>(inner, value => value, null, (left, right) => left + right));
            Should.Throw<ArgumentNullException>(() => outer.RightJoin<int, int, int, int>(inner, value => value, value => value, null));
        }

        [Fact]
        public void The_layer_declines_the_three_hooks()
        {
            var outer = new[] { 1, 2, 3 }.ToValueEnumerable();
            var inner = new[] { 1, 2, 3 };

            IValueEnumerableHooks<string> left = outer.LeftJoin(inner, value => value, value => value, (l, r) => l + ":" + r);
            IValueEnumerableHooks<string> right = outer.RightJoin(inner, value => value, value => value, (l, r) => l + ":" + r);

            left.TryGetNonEnumeratedCount(out _).ShouldBeFalse();
            right.TryGetNonEnumeratedCount(out _).ShouldBeFalse();

#if NETCOREAPP3_0_OR_GREATER
            left.TryGetSpan(out _).ShouldBeFalse();
            right.TryGetSpan(out _).ShouldBeFalse();
#endif
        }

        [Fact]
        public void The_layer_enumerators_are_plain_value_types()
        {
            typeof(LeftJoinValueEnumerator<ArrayValueEnumerator<int>, int, int, int, int>).IsValueType.ShouldBeTrue();
            typeof(LeftJoinValueEnumerator<ArrayValueEnumerator<int>, int, int, int, int>).IsByRefLike.ShouldBeFalse();

            typeof(RightJoinValueEnumerator<ArrayValueEnumerable<int>, ArrayValueEnumerator<int>, int, int, int, int>).IsValueType.ShouldBeTrue();
            typeof(RightJoinValueEnumerator<ArrayValueEnumerable<int>, ArrayValueEnumerator<int>, int, int, int, int>).IsByRefLike.ShouldBeFalse();
        }
    }
}
