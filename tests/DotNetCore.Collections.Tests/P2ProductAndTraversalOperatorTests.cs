using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Tests
{
    /// <summary>
    /// F7-11 (second half): the product, traversal, tied-extreme and randomization operators of the
    /// P2 layer - <c>Cartesian</c>, <c>TraverseBreadthFirst</c> / <c>TraverseDepthFirst</c>,
    /// <c>Maxima</c> / <c>Minima</c> and <c>Shuffle</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// None of these has a platform counterpart to diff against, so each expectation is written out
    /// by hand. Two of them do have a shape worth pinning precisely because a mistake there would
    /// still look plausible: the order of a cartesian product (first source slowest-varying) and the
    /// order of a depth-first walk (a node before its children, siblings left to right).
    /// </para>
    /// <para>
    /// The traversals are the only operators in the engine that are not extension methods, so they
    /// are called through <see cref="CollectionExtensions"/> by name here rather than fluently.
    /// </para>
    /// </remarks>
    public class P2ProductAndTraversalOperatorTests
    {
        private sealed class Node
        {
            internal Node(string name, params Node[] children)
            {
                Name = name;
                Children = children;
            }

            internal string Name { get; }

            internal Node[] Children { get; }
        }

        //        A
        //      /   \
        //     B     C
        //    / \     \
        //   D   E     F
        private static Node Tree()
            => new Node("A",
                new Node("B",
                    new Node("D"),
                    new Node("E")),
                new Node("C",
                    new Node("F")));

        // ----- Cartesian -----

        [Fact]
        public void Cartesian_lists_the_first_source_slowest_varying()
        {
            var actual = new[] { 1, 2 }.ToValueEnumerable()
                .Cartesian(new[] { "a", "b" }, (number, letter) => number + letter)
                .ToArray();

            actual.ShouldBe(new[] { "1a", "1b", "2a", "2b" });
        }

        [Fact]
        public void Cartesian_over_three_sources_lists_them_outermost_first()
        {
            var actual = new[] { 1, 2 }.ToValueEnumerable()
                .Cartesian(new[] { "a", "b" }, new[] { true, false },
                    (number, letter, flag) => number + letter + (flag ? "+" : "-"))
                .ToArray();

            actual.ShouldBe(new[] { "1a+", "1a-", "1b+", "1b-", "2a+", "2a-", "2b+", "2b-" });
        }

        [Fact]
        public void Cartesian_is_empty_when_any_factor_is_empty()
        {
            var empty = Array.Empty<int>();

            new[] { 1, 2 }.ToValueEnumerable()
                .Cartesian(empty, (number, other) => number + other)
                .ShouldBeEmpty();

            empty.ToValueEnumerable()
                .Cartesian(new[] { 1, 2 }, (number, other) => number + other)
                .ShouldBeEmpty();

            new[] { 1, 2 }.ToValueEnumerable()
                .Cartesian(new[] { 1 }, empty, (a, b, c) => a + b + c)
                .ShouldBeEmpty();
        }

        [Fact]
        public void Cartesian_returns_the_same_product_on_every_arm()
        {
            var expected = new[] { "1a", "1b", "2a", "2b" };

            new[] { 1, 2 }.ToValueEnumerable().Cartesian(new[] { "a", "b" }, (n, s) => n + s)
                .ToArray().ShouldBe(expected);

            new List<int> { 1, 2 }.ToValueEnumerable().Cartesian(new[] { "a", "b" }, (n, s) => n + s)
                .ToArray().ShouldBe(expected);

            IReadOnlyList<int> readOnly = new List<int> { 1, 2 };
            readOnly.ToValueEnumerable().Cartesian(new[] { "a", "b" }, (n, s) => n + s)
                .ToArray().ShouldBe(expected);

            IEnumerable<int> enumeration = new Queue<int>(new[] { 1, 2 });
            enumeration.ToValueEnumerable().Cartesian(new[] { "a", "b" }, (n, s) => n + s)
                .ToArray().ShouldBe(expected);
        }

        [Fact]
        public void Cartesian_chains_to_a_higher_arity()
        {
            // Four sources are two chained products; the outer one varies slowest overall.
            var actual = new[] { 1, 2 }.ToValueEnumerable()
                .Cartesian(new[] { "a" }, (number, letter) => number + letter)
                .Cartesian(new[] { true, false }, (pair, flag) => pair + (flag ? "+" : "-"))
                .ToArray();

            actual.ShouldBe(new[] { "1a+", "1a-", "2a+", "2a-" });
        }

        [Fact]
        public void Cartesian_rejects_null_arguments()
        {
            IEnumerable<string> noSecond = null!;
            IEnumerable<string> noThird = null!;
            Func<int, string, string> noPair = null!;
            Func<int, string, string, string> noTriple = null!;

            Should.Throw<ArgumentNullException>(() =>
                new[] { 1 }.ToValueEnumerable().Cartesian(noSecond, (n, s) => n + s));

            Should.Throw<ArgumentNullException>(() =>
                new[] { 1 }.ToValueEnumerable().Cartesian(new[] { "a" }, noPair));

            Should.Throw<ArgumentNullException>(() =>
                new[] { 1 }.ToValueEnumerable().Cartesian(new[] { "a" }, noThird, (a, b, c) => a + b + c));

            Should.Throw<ArgumentNullException>(() =>
                new[] { 1 }.ToValueEnumerable().Cartesian(new[] { "a" }, new[] { "b" }, noTriple));
        }

        // ----- TraverseBreadthFirst -----

        [Fact]
        public void TraverseBreadthFirst_visits_level_by_level()
        {
            var actual = CollectionExtensions
                .TraverseBreadthFirst(Tree(), node => node.Children)
                .Select(node => node.Name)
                .ToArray();

            actual.ShouldBe(new[] { "A", "B", "C", "D", "E", "F" });
        }

        [Fact]
        public void TraverseBreadthFirst_projects_each_node()
        {
            var actual = CollectionExtensions
                .TraverseBreadthFirst(Tree(), node => node.Children, node => node.Name.ToLowerInvariant())
                .ToArray();

            actual.ShouldBe(new[] { "a", "b", "c", "d", "e", "f" });
        }

        [Fact]
        public void TraverseBreadthFirst_of_a_leaf_yields_only_the_root()
        {
            var leaf = new Node("only");

            CollectionExtensions.TraverseBreadthFirst(leaf, node => node.Children)
                .Select(node => node.Name)
                .ToArray()
                .ShouldBe(new[] { "only" });
        }

        [Fact]
        public void TraverseBreadthFirst_asks_for_children_lazily()
        {
            var asked = new List<string>();

            var walk = CollectionExtensions.TraverseBreadthFirst(Tree(), node =>
            {
                asked.Add(node.Name);
                return node.Children;
            });

            // Building the walk touches nothing.
            asked.ShouldBeEmpty();

            // The root is yielded before its children are asked for, so pulling one node asks for
            // nothing at all.
            walk.Take(1).Select(node => node.Name).ShouldBe(new[] { "A" });
            asked.ShouldBeEmpty();

            // The second node is a child of the root, so producing it had to ask the root for its
            // children - and for nothing else.
            walk.Take(2).Select(node => node.Name).ShouldBe(new[] { "A", "B" });
            asked.ShouldBe(new[] { "A" });
        }

        // ----- TraverseDepthFirst -----

        [Fact]
        public void TraverseDepthFirst_visits_a_node_before_its_children()
        {
            var actual = CollectionExtensions
                .TraverseDepthFirst(Tree(), node => node.Children)
                .Select(node => node.Name)
                .ToArray();

            actual.ShouldBe(new[] { "A", "B", "D", "E", "C", "F" });
        }

        [Fact]
        public void TraverseDepthFirst_projects_each_node()
        {
            var actual = CollectionExtensions
                .TraverseDepthFirst(Tree(), node => node.Children, node => node.Name)
                .ToArray();

            actual.ShouldBe(new[] { "A", "B", "D", "E", "C", "F" });
        }

        [Fact]
        public void TraverseDepthFirst_of_a_leaf_yields_only_the_root()
        {
            var leaf = new Node("only");

            CollectionExtensions.TraverseDepthFirst(leaf, node => node.Children)
                .Select(node => node.Name)
                .ToArray()
                .ShouldBe(new[] { "only" });
        }

        [Fact]
        public void Traverse_rejects_null_arguments()
        {
            Func<Node, IEnumerable<Node>> noChildren = null!;
            Func<Node, string> noProjection = null!;
            Node noRoot = null!;

            Should.Throw<ArgumentNullException>(() =>
                CollectionExtensions.TraverseBreadthFirst(noRoot, node => node.Children));

            Should.Throw<ArgumentNullException>(() =>
                CollectionExtensions.TraverseBreadthFirst(Tree(), noChildren));

            Should.Throw<ArgumentNullException>(() =>
                CollectionExtensions.TraverseBreadthFirst(Tree(), node => node.Children, noProjection));

            Should.Throw<ArgumentNullException>(() =>
                CollectionExtensions.TraverseDepthFirst(noRoot, node => node.Children));

            Should.Throw<ArgumentNullException>(() =>
                CollectionExtensions.TraverseDepthFirst(Tree(), noChildren));

            Should.Throw<ArgumentNullException>(() =>
                CollectionExtensions.TraverseDepthFirst(Tree(), node => node.Children, noProjection));
        }

        // ----- Maxima / Minima -----

        private static readonly (string Name, int Score)[] Scores =
        {
            ("Ada", 10), ("Grace", 30), ("Linus", 20), ("Ken", 30), ("Barbara", 10)
        };

        [Fact]
        public void Maxima_returns_every_tied_element_in_source_order()
        {
            var actual = Scores.ToValueEnumerable()
                .Maxima(entry => entry.Score)
                .Select(entry => entry.Name)
                .ToArray();

            actual.ShouldBe(new[] { "Grace", "Ken" });
        }

        [Fact]
        public void Minima_returns_every_tied_element_in_source_order()
        {
            var actual = Scores.ToValueEnumerable()
                .Minima(entry => entry.Score)
                .Select(entry => entry.Name)
                .ToArray();

            actual.ShouldBe(new[] { "Ada", "Barbara" });
        }

        [Fact]
        public void Maxima_and_Minima_agree_with_the_BCL_when_there_is_a_single_winner()
        {
            var distinct = new[] { (Name: "Ada", Score: 10), (Name: "Grace", Score: 30), (Name: "Linus", Score: 20) };

            distinct.ToValueEnumerable().Maxima(entry => entry.Score).Single().Name
                .ShouldBe(distinct.MaxBy(entry => entry.Score).Name);

            distinct.ToValueEnumerable().Minima(entry => entry.Score).Single().Name
                .ShouldBe(distinct.MinBy(entry => entry.Score).Name);
        }

        [Fact]
        public void Maxima_honours_the_comparer()
        {
            // Reversing the ordering turns the maxima into the minima, which is the cheapest way to
            // prove the comparer is actually consulted rather than the default one being used.
            var actual = Scores.ToValueEnumerable()
                .Maxima(entry => entry.Score, Comparer<int>.Create((left, right) => right.CompareTo(left)))
                .Select(entry => entry.Name)
                .ToArray();

            actual.ShouldBe(new[] { "Ada", "Barbara" });
        }

        [Fact]
        public void Maxima_and_Minima_are_empty_for_an_empty_source()
        {
            // Unlike MaxBy / MinBy, which throw: "no element is maximal" is a truthful answer here.
            Array.Empty<int>().ToValueEnumerable().Maxima(value => value).ShouldBeEmpty();
            Array.Empty<int>().ToValueEnumerable().Minima(value => value).ShouldBeEmpty();
        }

        [Fact]
        public void Maxima_returns_every_element_when_they_all_tie()
        {
            var actual = new[] { 7, 7, 7 }.ToValueEnumerable().Maxima(value => value);

            actual.ShouldBe(new[] { 7, 7, 7 });
        }

        [Fact]
        public void Maxima_and_Minima_return_the_same_elements_on_every_arm()
        {
            var expected = new[] { "Grace", "Ken" };

            Scores.ToValueEnumerable().Maxima(entry => entry.Score).Select(entry => entry.Name)
                .ToArray().ShouldBe(expected);

            new List<(string Name, int Score)>(Scores).ToValueEnumerable().Maxima(entry => entry.Score).Select(entry => entry.Name)
                .ToArray().ShouldBe(expected);

            IReadOnlyList<(string Name, int Score)> readOnly = new List<(string Name, int Score)>(Scores);
            readOnly.ToValueEnumerable().Maxima(entry => entry.Score).Select(entry => entry.Name)
                .ToArray().ShouldBe(expected);

            IEnumerable<(string Name, int Score)> enumeration = new Queue<(string Name, int Score)>(Scores);
            enumeration.ToValueEnumerable().Maxima(entry => entry.Score).Select(entry => entry.Name)
                .ToArray().ShouldBe(expected);
        }

        [Fact]
        public void Maxima_and_Minima_reject_a_null_key_selector()
        {
            Func<int, int> noKey = null!;

            Should.Throw<ArgumentNullException>(() => new[] { 1 }.ToValueEnumerable().Maxima(noKey));
            Should.Throw<ArgumentNullException>(() => new[] { 1 }.ToValueEnumerable().Minima(noKey));
        }

        // ----- Shuffle -----

        [Fact]
        public void Shuffle_keeps_every_element_exactly_once()
        {
            var source = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            var shuffled = source.ToValueEnumerable().Shuffle(new Random(1234));

            shuffled.Count.ShouldBe(source.Length);
            shuffled.OrderBy(value => value).ShouldBe(source);
        }

        [Fact]
        public void Shuffle_with_the_same_seed_repeats_the_same_permutation()
        {
            var source = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            source.ToValueEnumerable().Shuffle(new Random(1234))
                .ShouldBe(source.ToValueEnumerable().Shuffle(new Random(1234)));
        }

        [Fact]
        public void Shuffle_does_not_always_return_the_source_order()
        {
            var source = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            var permutations = Enumerable.Range(0, 20)
                .Select(seed => string.Join(",", source.ToValueEnumerable().Shuffle(new Random(seed))))
                .Distinct()
                .Count();

            permutations.ShouldBeGreaterThan(1);
        }

        [Fact]
        public void Shuffle_returns_the_same_permutation_on_every_arm()
        {
            var source = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var expected = source.ToValueEnumerable().Shuffle(new Random(99));

            new List<int>(source).ToValueEnumerable().Shuffle(new Random(99)).ShouldBe(expected);

            IReadOnlyList<int> readOnly = new List<int>(source);
            readOnly.ToValueEnumerable().Shuffle(new Random(99)).ShouldBe(expected);

            IEnumerable<int> enumeration = new Queue<int>(source);
            enumeration.ToValueEnumerable().Shuffle(new Random(99)).ShouldBe(expected);
        }

        [Fact]
        public void Shuffle_of_an_empty_source_is_empty()
        {
            Array.Empty<int>().ToValueEnumerable().Shuffle().ShouldBeEmpty();
        }
    }
}
