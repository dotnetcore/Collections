using System;
using System.Collections.Generic;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.NetFxTests
{
    /// <summary>
    /// F6-27: the positional list contract has to hold on the .NET Framework generation too, and
    /// that is not implied by the net8.0 suite passing. The interface set itself is the reason the
    /// case is worth its own test here: <c>IReadOnlyList&lt;T&gt;</c> and <c>IList&lt;T&gt;</c> do
    /// exist from .NET 4.5 onwards, unlike the <c>IReadOnlyCollection&lt;T&gt;</c> gap F6-24 hit on
    /// <c>HashSet&lt;T&gt;</c> / <c>SortedSet&lt;T&gt;</c> - so a framework consumer sees the same
    /// surface - but the rank reads these members are built on descend an internal tree, and the
    /// whole point of running this here is that the framework's CLR actually executes that descent.
    /// net462 is the lowest .NET Framework target the current xunit runner stack runs.
    /// </summary>
    public class PositionalListNetFxTests
    {
        [Fact]
        public void OrderedMultiList_ImplementsBothListInterfacesAtRuntime()
        {
            var list = new OrderedMultiList<int>();
            list.AddRange(new[] { 2, 2, 5 });

            (list is IReadOnlyList<int>).ShouldBeTrue();
            (list is IList<int>).ShouldBeTrue();
            ((IReadOnlyList<int>)list).Count.ShouldBe(3);
            ((IList<int>)list).IsReadOnly.ShouldBeFalse();
        }

        [Fact]
        public void OrderedMultiList_PositionalReadsAddressTheExpandedSequence()
        {
            var list = new OrderedMultiList<string>();
            list.Add("mug");
            list.Add("bean", 2);

            list[0].ShouldBe("bean");
            list[2].ShouldBe("mug");
            list.IndexOf("mug").ShouldBe(2);
            list.IndexOf("cup").ShouldBe(-1);
            ((IReadOnlyList<string>)list)[1].ShouldBe("bean");
        }

        [Fact]
        public void OrderedMultiList_InsertAndRemoveAtWorkAtRuntime()
        {
            var list = new OrderedMultiList<int>();
            list.Add(1, 2);
            list.Add(3);

            list.Insert(1, 1);
            list.ToList().ShouldBe(new[] { 1, 1, 1, 3 });

            list.RemoveAt(0);
            list.ToList().ShouldBe(new[] { 1, 1, 3 });

            Should.Throw<ArgumentOutOfRangeException>(() => list.RemoveAt(3));
            Should.Throw<ArgumentOutOfRangeException>(() => list.Insert(0, 3));
        }

        [Fact]
        public void OrderedMultiList_IndexerSetterIsRefusedAtRuntime()
        {
            var list = new OrderedMultiList<int>();
            list.AddRange(new[] { 1, 2 });

            Should.Throw<NotSupportedException>(() => ((IList<int>)list)[0] = 9);
            list.ToList().ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public void ReadOnlyView_CastsToIReadOnlyListAtRuntime()
        {
            var list = new OrderedMultiList<int>();
            list.AddRange(new[] { 3, 1 });
            var view = list.AsReadOnly();

            var positional = (IReadOnlyList<int>)view;

            positional.Count.ShouldBe(2);
            positional[0].ShouldBe(1);
            positional[1].ShouldBe(3);
            (view is IList<int>).ShouldBeFalse();
        }

        [Fact]
        public void MultiList_HasNoPositionalContractAtRuntime()
        {
            var bag = new MultiList<int>();
            bag.AddRange(new[] { 3, 1 });

            (bag is IReadOnlyList<int>).ShouldBeFalse();
            (bag is IList<int>).ShouldBeFalse();
        }
    }
}
