using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Tests
{
    /// <summary>
    /// F7-06 (third batch): the keyless ordering operators of the P0 polyfill layer - <c>Order</c>
    /// and <c>OrderDescending</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both are BCL operators from .NET 7 onwards and both answer with
    /// <see cref="IOrderedEnumerable{TElement}"/>. The suite runs on net8.0, where the platform's
    /// own implementation is available, so every engine answer is compared with the
    /// <c>System.Linq</c> expression it claims to be equivalent to.
    /// </para>
    /// <para>
    /// The reason the return type is the interface rather than an engine struct is that a low-TFM
    /// caller has to be able to chain the BCL's <c>ThenBy</c> onto it, so the composition tests are
    /// the load-bearing ones here.
    /// </para>
    /// </remarks>
    public class PolyfillOrderingOperatorTests
    {
        private static readonly int[] Numbers = { 3, 1, 4, 1, 5, 9, 2, 6 };

        private static readonly Item[] Items =
        {
            new Item(2, "b"),
            new Item(1, "c"),
            new Item(2, "a"),
            new Item(1, "d"),
        };

        [Fact]
        public void Order_sorts_ascending_on_every_arm()
        {
            var expected = Numbers.Order().ToList();

            var fromArray = Numbers.ToValueEnumerable().Order().ToList();
            var fromList = new List<int>(Numbers).ToValueEnumerable().Order().ToList();

            IReadOnlyList<int> readOnlySource = new List<int>(Numbers);
            var fromReadOnly = readOnlySource.ToValueEnumerable().Order().ToList();

            IEnumerable<int> enumerationSource = new Queue<int>(Numbers);
            var fromEnumerable = enumerationSource.ToValueEnumerable().Order().ToList();

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);

            expected.ShouldBe(new[] { 1, 1, 2, 3, 4, 5, 6, 9 });
        }

        [Fact]
        public void OrderDescending_sorts_descending_on_every_arm()
        {
            var expected = Numbers.OrderDescending().ToList();

            var fromArray = Numbers.ToValueEnumerable().OrderDescending().ToList();
            var fromList = new List<int>(Numbers).ToValueEnumerable().OrderDescending().ToList();

            IReadOnlyList<int> readOnlySource = new List<int>(Numbers);
            var fromReadOnly = readOnlySource.ToValueEnumerable().OrderDescending().ToList();

            IEnumerable<int> enumerationSource = new Queue<int>(Numbers);
            var fromEnumerable = enumerationSource.ToValueEnumerable().OrderDescending().ToList();

            fromArray.ShouldBe(expected);
            fromList.ShouldBe(expected);
            fromReadOnly.ShouldBe(expected);
            fromEnumerable.ShouldBe(expected);

            expected.ShouldBe(new[] { 9, 6, 5, 4, 3, 2, 1, 1 });
        }

        [Fact]
        public void Ordering_answers_with_an_IOrderedEnumerable_so_ThenBy_can_chain()
        {
            // The shape assertion: on a low target this is what makes the BCL's ThenBy - present
            // since .NET 3.5, so on every target the package ships - usable on an engine ordering.
            (Numbers.ToValueEnumerable().Order() is IOrderedEnumerable<int>).ShouldBeTrue();
            (Numbers.ToValueEnumerable().OrderDescending() is IOrderedEnumerable<int>).ShouldBeTrue();
        }

        [Fact]
        public void Order_is_stable()
        {
            // The comparer ties on Key, so the only thing that can decide the order of the ties is
            // the engine's stability. The oracle is the BCL's OrderBy, which is documented stable.
            var comparer = Comparer<Item>.Create((left, right) => left.Key.CompareTo(right.Key));

            var expected = Items.Order(comparer).Select(item => item.Name).ToList();
            var actual = Items.ToValueEnumerable().Order(comparer).Select(item => item.Name).ToList();

            actual.ShouldBe(expected);
            actual.ShouldBe(new[] { "c", "d", "b", "a" });
        }

        [Fact]
        public void Order_composes_with_the_BCLs_ThenBy()
        {
            var expected = Items.Order(Comparer<Item>.Create((left, right) => left.Key.CompareTo(right.Key)))
                .ThenBy(item => item.Name)
                .Select(item => item.Name)
                .ToList();

            var actual = Items.ToValueEnumerable()
                .Order(Comparer<Item>.Create((left, right) => left.Key.CompareTo(right.Key)))
                .ThenBy(item => item.Name)
                .Select(item => item.Name)
                .ToList();

            actual.ShouldBe(expected);
            actual.ShouldBe(new[] { "c", "d", "a", "b" });
        }

        [Fact]
        public void OrderDescending_composes_with_the_BCLs_ThenBy()
        {
            var comparer = Comparer<Item>.Create((left, right) => left.Key.CompareTo(right.Key));

            var expected = Items.OrderDescending(comparer)
                .ThenBy(item => item.Name)
                .Select(item => item.Name)
                .ToList();

            var actual = Items.ToValueEnumerable()
                .OrderDescending(comparer)
                .ThenBy(item => item.Name)
                .Select(item => item.Name)
                .ToList();

            actual.ShouldBe(expected);
            actual.ShouldBe(new[] { "a", "b", "c", "d" });
        }

        [Fact]
        public void Ordering_walks_the_source_again_on_every_enumeration()
        {
            // A cached sort would answer the second question with the first answer's data. The BCL
            // recomputes, so the engine has to as well, or a caller that mutates the list between
            // two walks would see two different libraries.
            var list = new List<int> { 3, 1, 2 };

            var engineOrdered = list.ToValueEnumerable().Order();
            var bclOrdered = list.Order();

            engineOrdered.ToList().ShouldBe(bclOrdered.ToList());

            list.Add(0);

            engineOrdered.ToList().ShouldBe(new[] { 0, 1, 2, 3 });
            engineOrdered.ToList().ShouldBe(bclOrdered.ToList());
        }

        [Fact]
        public void Ordering_yields_nothing_for_an_empty_source()
        {
            new int[0].ToValueEnumerable().Order().ShouldBeEmpty();
            new int[0].ToValueEnumerable().OrderDescending().ShouldBeEmpty();
        }

        private sealed class Item
        {
            internal Item(int key, string name)
            {
                Key = key;
                Name = name;
            }

            internal int Key { get; }

            internal string Name { get; }
        }
    }
}
