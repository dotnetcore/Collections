using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Multi.NetFxTests
{
    /// <summary>
    /// F6-31: the <see cref="Deque{T}"/> face has to hold on the .NET Framework generation too,
    /// and a net8.0-only run does not show that. The reason it is worth a case here is the
    /// interface surface, which is exactly where this package has been bitten before: F6-24 was an
    /// <see cref="InvalidCastException"/> from a bare cast to <c>IReadOnlyCollection&lt;T&gt;</c>
    /// that only the framework CLR threw, and F6-04 before it the same shape of gap.
    /// <c>IReadOnlyList&lt;T&gt;</c> and <c>IList&lt;T&gt;</c> do exist from .NET 4.5 onwards, so the
    /// deque's two list faces resolve here - but only a run on this target proves the framework's
    /// CLR actually dispatches them, and only a run here executes the ring on it. net462 is the
    /// lowest .NET Framework target the current xunit runner stack runs.
    /// </summary>
    public class DequeNetFxTests
    {
        [Fact]
        public void Deque_ExposesBothListInterfacesAtRuntime()
        {
            var deque = new Deque<int> { 2, 3, 4 };

            (deque is IReadOnlyList<int>).ShouldBeTrue();
            (deque is IList<int>).ShouldBeTrue();
            (deque is ICollection<int>).ShouldBeTrue();
            (deque is IReadOnlyCollection<int>).ShouldBeTrue();
        }

        [Fact]
        public void ReadOnlyListFace_AddressesTheSequenceFromTheHead()
        {
            IReadOnlyList<string> view = new Deque<string> { "a", "b", "c" };

            view.Count.ShouldBe(3);
            view[0].ShouldBe("a");
            view[2].ShouldBe("c");
        }

        [Fact]
        public void ListFace_MutatesThroughTheSamePositions()
        {
            IList<int> list = new Deque<int> { 1, 2, 3 };

            list.IsReadOnly.ShouldBeFalse();
            list[0] = 9;
            list.IndexOf(3).ShouldBe(2);
            list.Insert(0, 0);
            list.ToArray().ShouldBe(new[] { 0, 9, 2, 3 });

            list.RemoveAt(0);
            list.Remove(2).ShouldBeTrue();
            list.Contains(3).ShouldBeTrue();
        }

        [Fact]
        public void AsReadOnly_IsLiveAndReadableThroughBothFaces()
        {
            var deque = new Deque<string> { "a", "b" };
            var view = deque.AsReadOnly();

            view.Count.ShouldBe(2);
            view[0].ShouldBe("a");

            deque.AddLast("c");

            view.Count.ShouldBe(3);
            view[2].ShouldBe("c");

            // The narrower face the rest of the package hands out is still reachable from the
            // wider one this type declares.
            ((IReadOnlyCollection<string>)view).Count.ShouldBe(3);
        }

        [Fact]
        public void Ring_WrapsAndGrowsOnTheFrameworkRuntime()
        {
            var deque = new Deque<int>(4);
            deque.AddLast(1);
            deque.AddLast(2);
            deque.AddLast(3);
            deque.AddLast(4);
            deque.RemoveFirst();
            deque.RemoveFirst();
            deque.AddLast(5);
            deque.AddLast(6);

            deque.Capacity.ShouldBe(4);
            deque.ToArray().ShouldBe(new[] { 3, 4, 5, 6 });

            deque.AddLast(7);

            deque.Capacity.ShouldBe(8);
            deque.ToArray().ShouldBe(new[] { 3, 4, 5, 6, 7 });
        }

        [Fact]
        public void NonGenericEnumeration_WalksTheSameSequence()
        {
            var deque = new Deque<int>(4) { 1, 2, 3, 4, 5 };
            var seen = new List<int>();

            foreach (var item in (IEnumerable)deque)
            {
                seen.Add((int)item);
            }

            seen.ShouldBe(new[] { 1, 2, 3, 4, 5 });
        }

        [Fact]
        public void NullElements_AreStoredLikeAnyOtherElement()
        {
            var deque = new Deque<string>();

            deque.AddFirst(null);
            deque.AddLast("b");
            deque.AddLast(null);

            deque.Count.ShouldBe(3);
            deque.GetFirst().ShouldBeNull();
            deque.IndexOf(null).ShouldBe(0);
            deque.Contains(null).ShouldBeTrue();
            deque.ToArray().ShouldBe(new[] { null, "b", null });
        }

        [Fact]
        public void EndOperations_ThrowOnEmptyAndAnswerOnNonEmpty()
        {
            var deque = new Deque<int> { 1, 2 };

            deque.GetFirst().ShouldBe(1);
            deque.GetLast().ShouldBe(2);
            deque.TryGetFirst(out var first).ShouldBeTrue();
            first.ShouldBe(1);

            deque.Clear();

            Should.Throw<System.InvalidOperationException>(() => deque.GetFirst());
            Should.Throw<System.InvalidOperationException>(() => deque.RemoveLast());
            deque.TryGetLast(out var last).ShouldBeFalse();
            last.ShouldBe(default(int));
        }
    }
}
