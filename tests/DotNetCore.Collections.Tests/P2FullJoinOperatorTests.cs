using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Tests
{
    /// <summary>
    /// F7-11 (first half): the full outer joins of the P2 layer - <c>FullJoin</c> and
    /// <c>FullGroupJoin</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// No target framework in the matrix ships either operator, so there is no platform method to
    /// compare against and the expectations are written out by hand. Where a comparison is possible
    /// it is made against the engine's own <c>LeftJoin</c>, which is the BCL-shaped join the full
    /// join has to agree with on the rows they share.
    /// </para>
    /// <para>
    /// The cases that get their own test are the ones a full join can quietly get wrong: the order
    /// of the two phases, a null key (which must never match, but must still be emitted), and the
    /// empty side (which must contribute nothing rather than dropping the other side's rows).
    /// </para>
    /// </remarks>
    public class P2FullJoinOperatorTests
    {
        private static readonly (int Id, string Name)[] People =
        {
            (1, "Ada"), (2, "Grace"), (3, "Linus"), (4, "Ken")
        };

        private static readonly (int Owner, string Pet)[] Pets =
        {
            (1, "cat"), (1, "dog"), (2, "owl"), (5, "fox")
        };

        // Both halves are guarded, because either side of a full join can arrive as default.
        private static string Project((int Id, string Name) person, (int Owner, string Pet) pet)
            => (person.Name ?? "NONE") + "-" + (pet.Pet ?? "NONE");

        private static string ProjectText((string Id, string Name) left, (string Owner, string Pet) right)
            => (left.Name ?? "NONE") + "-" + (right.Pet ?? "NONE");

        private static string[] FullJoinAllArms(
            (int Id, string Name)[] people, (int Owner, string Pet)[] pets)
        {
            var fromArray = people.ToValueEnumerable()
                .FullJoin(pets, person => person.Id, pet => pet.Owner, Project)
                .ToArray();

            var fromList = new List<(int Id, string Name)>(people).ToValueEnumerable()
                .FullJoin(pets, person => person.Id, pet => pet.Owner, Project)
                .ToArray();

            IReadOnlyList<(int Id, string Name)> readOnlyPeople = new List<(int Id, string Name)>(people);
            var fromReadOnly = readOnlyPeople.ToValueEnumerable()
                .FullJoin(pets, person => person.Id, pet => pet.Owner, Project)
                .ToArray();

            IEnumerable<(int Id, string Name)> enumerationPeople = new Queue<(int Id, string Name)>(people);
            var fromEnumerable = enumerationPeople.ToValueEnumerable()
                .FullJoin(pets, person => person.Id, pet => pet.Owner, Project)
                .ToArray();

            foreach (var other in new[] { fromList, fromReadOnly, fromEnumerable })
            {
                other.ShouldBe(fromArray);
            }

            return fromArray;
        }

        // ----- FullJoin -----

        [Fact]
        public void FullJoin_emits_outer_rows_first_then_the_unmatched_inner_rows()
        {
            var actual = FullJoinAllArms(People, Pets);

            // Ada matches twice, Grace once, Linus and Ken not at all, and the fox belongs to an
            // owner the outer side does not have - so it lands after every outer row.
            actual.ShouldBe(new[]
            {
                "Ada-cat",
                "Ada-dog",
                "Grace-owl",
                "Linus-NONE",
                "Ken-NONE",
                "NONE-fox",
            });
        }

        [Fact]
        public void FullJoin_agrees_with_LeftJoin_on_the_rows_LeftJoin_produces()
        {
            var leftJoin = People.ToValueEnumerable()
                .LeftJoin(Pets, person => person.Id, pet => pet.Owner, Project)
                .ToArray();

            var fullJoin = People.ToValueEnumerable()
                .FullJoin(Pets, person => person.Id, pet => pet.Owner, Project)
                .ToArray();

            // A full join is a left join plus whatever the left join had to drop, so the left join's
            // rows have to be a prefix - and in the same order.
            fullJoin.Take(leftJoin.Length).ShouldBe(leftJoin);
            fullJoin.Length.ShouldBe(leftJoin.Length + 1);
            fullJoin[fullJoin.Length - 1].ShouldBe("NONE-fox");
        }

        [Fact]
        public void FullJoin_keeps_the_outer_rows_when_the_inner_side_is_empty()
        {
            var empty = Array.Empty<(int Owner, string Pet)>();

            var actual = People.ToValueEnumerable()
                .FullJoin(empty, person => person.Id, pet => pet.Owner, Project)
                .ToArray();

            actual.ShouldBe(new[] { "Ada-NONE", "Grace-NONE", "Linus-NONE", "Ken-NONE" });
        }

        [Fact]
        public void FullJoin_keeps_the_inner_rows_when_the_outer_side_is_empty()
        {
            var empty = Array.Empty<(int Id, string Name)>();

            var actual = empty.ToValueEnumerable()
                .FullJoin(Pets, person => person.Id, pet => pet.Owner, Project)
                .ToArray();

            actual.ShouldBe(new[] { "NONE-cat", "NONE-dog", "NONE-owl", "NONE-fox" });
        }

        [Fact]
        public void FullJoin_yields_nothing_when_both_sides_are_empty()
        {
            var actual = Array.Empty<(int Id, string Name)>().ToValueEnumerable()
                .FullJoin(Array.Empty<(int Owner, string Pet)>(), person => person.Id, pet => pet.Owner, Project)
                .ToArray();

            actual.ShouldBeEmpty();
        }

        [Fact]
        public void FullJoin_never_matches_a_null_key_but_still_emits_it()
        {
            // A null key cannot match anything - the rule Lookup.CreateForJoin applies to the BCL's
            // own joins - so the null-keyed element on each side comes out unmatched.
            var outer = new (int Id, string Name)[] { (1, "Ada"), (2, null!) };
            var inner = new (int Owner, string Pet)[] { (9, null!), (1, "Ada") };

            var actual = outer.ToValueEnumerable()
                .FullJoin(inner, person => person.Name, pet => pet.Pet, Project)
                .ToArray();

            // The outer side streams first: Ada pairs with Ada, the null-keyed person finds nothing,
            // and the null-keyed pet - which never entered the index - is emitted in the tail.
            actual.ShouldBe(new[] { "Ada-Ada", "NONE-NONE", "NONE-NONE" });
        }

        [Fact]
        public void FullJoin_uses_the_supplied_comparer_for_both_sides()
        {
            var outer = new[] { (Id: "ALPHA", Name: "a"), (Id: "beta", Name: "b") };
            var inner = new[] { (Owner: "alpha", Pet: "x") };

            var insensitive = outer.ToValueEnumerable()
                .FullJoin(inner, o => o.Id, i => i.Owner, ProjectText, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            insensitive.ShouldBe(new[] { "a-x", "b-NONE" });

            var sensitive = outer.ToValueEnumerable()
                .FullJoin(inner, o => o.Id, i => i.Owner, ProjectText, StringComparer.Ordinal)
                .ToArray();

            sensitive.ShouldBe(new[] { "a-NONE", "b-NONE", "NONE-x" });
        }

        [Fact]
        public void FullJoin_does_not_touch_either_side_until_it_is_enumerated()
        {
            var pulls = 0;

            IEnumerable<(int Id, string Name)> Outer()
            {
                pulls++;
                yield return (1, "Ada");
            }

            IEnumerable<(int Owner, string Pet)> Inner()
            {
                pulls++;
                yield return (1, "cat");
            }

            var joined = Outer().ToValueEnumerable()
                .FullJoin(Inner(), person => person.Id, pet => pet.Owner, Project);

            // Constructing the join is not the same as running it: both sides are still untouched.
            pulls.ShouldBe(0);

            joined.ToArray().ShouldBe(new[] { "Ada-cat" });
            pulls.ShouldBe(2);
        }

        [Fact]
        public void FullJoin_rejects_null_arguments()
        {
            IEnumerable<(int Owner, string Pet)> noInner = null!;
            Func<(int Id, string Name), int> noOuterKey = null!;
            Func<(int Owner, string Pet), int> noInnerKey = null!;
            Func<(int Id, string Name), (int Owner, string Pet), string> noProjection = null!;

            Should.Throw<ArgumentNullException>(() =>
                People.ToValueEnumerable().FullJoin(noInner, person => person.Id, pet => pet.Owner, Project));

            Should.Throw<ArgumentNullException>(() =>
                People.ToValueEnumerable().FullJoin(Pets, noOuterKey, pet => pet.Owner, Project));

            Should.Throw<ArgumentNullException>(() =>
                People.ToValueEnumerable().FullJoin(Pets, person => person.Id, noInnerKey, Project));

            Should.Throw<ArgumentNullException>(() =>
                People.ToValueEnumerable().FullJoin(Pets, person => person.Id, pet => pet.Owner, noProjection));
        }

        // ----- FullGroupJoin -----

        [Fact]
        public void FullGroupJoin_emits_one_row_per_key_in_first_seen_order()
        {
            var first = new[] { "a", "b", "a" };
            var second = new[] { "b", "c" };

            var actual = first.ToValueEnumerable()
                .FullGroupJoin(second, x => x, x => x, (key, left, right) =>
                    key + ":" + string.Join("+", left) + "/" + string.Join("+", right))
                .ToArray();

            // "a" is introduced by the first side, "b" is shared, "c" is introduced by the second.
            actual.ShouldBe(new[]
            {
                "a:a+a/",
                "b:b/b",
                "c:/c",
            });
        }

        [Fact]
        public void FullGroupJoin_gives_the_missing_side_an_empty_group()
        {
            var first = new[] { 1, 2 };
            var second = new[] { 3, 4 };

            var actual = first.ToValueEnumerable()
                .FullGroupJoin(second, x => x, x => x, (key, left, right) => (key, left.Count(), right.Count()))
                .ToArray();

            actual.ShouldBe(new[] { (1, 1, 0), (2, 1, 0), (3, 0, 1), (4, 0, 1) });
        }

        [Fact]
        public void FullGroupJoin_returns_the_same_rows_on_every_arm()
        {
            var first = new[] { "a", "b", "a" };
            var second = new[] { "b", "c" };

            static string Describe(string key, IEnumerable<string> left, IEnumerable<string> right)
                => key + ":" + string.Join("+", left) + "/" + string.Join("+", right);

            var fromArray = first.ToValueEnumerable().FullGroupJoin(second, x => x, x => x, Describe).ToArray();
            var fromList = new List<string>(first).ToValueEnumerable().FullGroupJoin(second, x => x, x => x, Describe).ToArray();

            IReadOnlyList<string> readOnlyFirst = new List<string>(first);
            var fromReadOnly = readOnlyFirst.ToValueEnumerable().FullGroupJoin(second, x => x, x => x, Describe).ToArray();

            IEnumerable<string> enumerationFirst = new Queue<string>(first);
            var fromEnumerable = enumerationFirst.ToValueEnumerable().FullGroupJoin(second, x => x, x => x, Describe).ToArray();

            fromList.ShouldBe(fromArray);
            fromReadOnly.ShouldBe(fromArray);
            fromEnumerable.ShouldBe(fromArray);
        }

        [Fact]
        public void FullGroupJoin_groups_a_null_key_rather_than_dropping_it()
        {
            // A group join groups the way GroupBy does, and GroupBy gives a null key a group of its
            // own - the opposite of what FullJoin does with one.
            var first = new string[] { "a", null! };
            var second = new string[] { null!, "b" };

            var actual = first.ToValueEnumerable()
                .FullGroupJoin(second, x => x, x => x, (key, left, right) =>
                    (key ?? "NULL") + ":" + left.Count() + "/" + right.Count())
                .ToArray();

            actual.ShouldBe(new[] { "a:1/0", "NULL:1/1", "b:0/1" });
        }

        [Fact]
        public void FullGroupJoin_uses_the_supplied_comparer()
        {
            var first = new[] { "Alpha" };
            var second = new[] { "alpha", "beta" };

            var actual = first.ToValueEnumerable()
                .FullGroupJoin(second, x => x, x => x,
                    (key, left, right) => left.Count() + "/" + right.Count(),
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

            actual.ShouldBe(new[] { "1/1", "0/1" });
        }

        [Fact]
        public void FullGroupJoin_rejects_null_arguments()
        {
            IEnumerable<(int Owner, string Pet)> noSecond = null!;
            Func<(int Id, string Name), int> noFirstKey = null!;
            Func<(int Owner, string Pet), int> noSecondKey = null!;
            Func<int, IEnumerable<(int Id, string Name)>, IEnumerable<(int Owner, string Pet)>, string> noProjection = null!;

            Should.Throw<ArgumentNullException>(() =>
                People.ToValueEnumerable().FullGroupJoin(noSecond, person => person.Id, pet => pet.Owner, (key, left, right) => key.ToString()));

            Should.Throw<ArgumentNullException>(() =>
                People.ToValueEnumerable().FullGroupJoin(Pets, noFirstKey, pet => pet.Owner, (key, left, right) => key.ToString()));

            Should.Throw<ArgumentNullException>(() =>
                People.ToValueEnumerable().FullGroupJoin(Pets, person => person.Id, noSecondKey, (key, left, right) => key.ToString()));

            Should.Throw<ArgumentNullException>(() =>
                People.ToValueEnumerable().FullGroupJoin(Pets, person => person.Id, pet => pet.Owner, noProjection));
        }
    }
}
