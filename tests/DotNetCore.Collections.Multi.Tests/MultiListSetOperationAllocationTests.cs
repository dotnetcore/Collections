using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace DotNetCore.Collections.Multi.Tests
{
    /// <summary>
    /// M6-08 / L-07: the set operations no longer materialise their argument as a
    /// <see cref="MultiList{T}"/> when it already is one, so a chained operation allocates nothing.
    /// </summary>
    /// <remarks>
    /// The optimisation is only acceptable if it is invisible from the outside, so this suite
    /// carries two kinds of tests. The allocation tests pin the *claim* with
    /// <c>GC.GetAllocatedBytesForCurrentThread()</c> rather than a stopwatch, and each one is
    /// measured after a warm-up so JIT work is not charged to the operation. The equivalence tests
    /// pin the *reason it is safe*: every operation is run twice - once with a multiset argument
    /// (which takes the in-place path) and once with an equivalent plain array (which takes the
    /// original materialising path) - and the two outcomes must agree. Any divergence between the
    /// two argument shapes is a bug in the fast path by definition.
    /// </remarks>
    public class MultiListSetOperationAllocationTests
    {
        private readonly ITestOutputHelper _output;

        public MultiListSetOperationAllocationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // ------------------------------------------------------------------
        // Allocation: the claim itself
        // ------------------------------------------------------------------

        /// <summary>
        /// The eight operations, each expressed against a multiset argument.
        /// </summary>
        public static IEnumerable<object[]> Operations()
        {
            yield return new object[] { "IsSubsetOf", (Action<MultiList<int>, MultiList<int>>)((b, a) => _ = b.IsSubsetOf(a)) };
            yield return new object[] { "IsSupersetOf", (Action<MultiList<int>, MultiList<int>>)((b, a) => _ = b.IsSupersetOf(a)) };
            yield return new object[] { "IsProperSubsetOf", (Action<MultiList<int>, MultiList<int>>)((b, a) => _ = b.IsProperSubsetOf(a)) };
            yield return new object[] { "IsProperSupersetOf", (Action<MultiList<int>, MultiList<int>>)((b, a) => _ = b.IsProperSupersetOf(a)) };
            yield return new object[] { "UnionWith", (Action<MultiList<int>, MultiList<int>>)((b, a) => b.UnionWith(a)) };
            yield return new object[] { "IntersectionWith", (Action<MultiList<int>, MultiList<int>>)((b, a) => b.IntersectionWith(a)) };
            yield return new object[] { "ExceptWith", (Action<MultiList<int>, MultiList<int>>)((b, a) => b.ExceptWith(a)) };
            yield return new object[] { "SymmetricExceptWith", (Action<MultiList<int>, MultiList<int>>)((b, a) => b.SymmetricExceptWith(a)) };
        }

        [Theory]
        [MemberData(nameof(Operations))]
        public void SetOperations_AllocateNothingInSteadyState(string name, Action<MultiList<int>, MultiList<int>> operation)
        {
            const int size = 32;

            var argument = Bag(0, size);
            var receiver = Bag(0, size);

            // Warm-up: let the JIT run the path once, and let any lazily-created buffer exist.
            operation(receiver, argument);

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 200; i++)
            {
                Reset(receiver, size);
                operation(receiver, argument);
            }

            var perCall = (GC.GetAllocatedBytesForCurrentThread() - before) / 200.0;
            _output.WriteLine($"{name}: {perCall:N1} bytes/call");
            perCall.ShouldBe(0.0);
        }

        [Theory]
        [InlineData("IsSubsetOf")]
        [InlineData("IsSupersetOf")]
        [InlineData("IsProperSubsetOf")]
        [InlineData("IsProperSupersetOf")]
        [InlineData("UnionWith")]
        public void ReadOnlyOperationsAndUnion_AllocateNothingEvenOnAFreshReceiver(string name)
        {
            const int size = 32;
            var argument = Bag(0, size);

            // A brand-new receiver per call: nothing at all can be reused, so this is the strictest
            // form of the claim. The three mutating operations are excluded because they need one
            // reusable staging buffer - pinned separately below.
            var receivers = new List<MultiList<int>>(200);
            for (var i = 0; i < 200; i++)
            {
                receivers.Add(Bag(0, size));
            }

            var operation = Operations().Single(row => (string)row[0] == name)[1]
                as Action<MultiList<int>, MultiList<int>>;
            operation.ShouldNotBeNull();

            operation(receivers[0], argument); // warm-up

            var before = GC.GetAllocatedBytesForCurrentThread();
            foreach (var receiver in receivers)
            {
                operation(receiver, argument);
            }

            var perCall = (GC.GetAllocatedBytesForCurrentThread() - before) / 200.0;
            _output.WriteLine($"{name}: {perCall:N1} bytes/call (fresh receiver)");
            perCall.ShouldBe(0.0);
        }

        [Theory]
        [InlineData("IntersectionWith")]
        [InlineData("ExceptWith")]
        [InlineData("SymmetricExceptWith")]
        public void MutatingOperations_PayForOneReusableBufferOnAFreshReceiverOnly(string name)
        {
            const int size = 32;
            var argument = Bag(0, size);

            var receivers = new List<MultiList<int>>(200);
            for (var i = 0; i < 200; i++)
            {
                receivers.Add(Bag(0, size));
            }

            var operation = Operations().Single(row => (string)row[0] == name)[1]
                as Action<MultiList<int>, MultiList<int>>;
            operation.ShouldNotBeNull();

            operation(receivers[0], argument); // warm-up

            var before = GC.GetAllocatedBytesForCurrentThread();
            foreach (var receiver in receivers)
            {
                operation(receiver, argument);
            }

            var perCall = (GC.GetAllocatedBytesForCurrentThread() - before) / 200.0;
            _output.WriteLine($"{name}: {perCall:N1} bytes/call (fresh receiver)");

            // Only the one-off staging buffer remains - a single list sized from the receiver - and
            // it is nothing like the whole second multiset (plus iterator) the snapshot used to
            // allocate. The steady-state assertion above is the real claim; this one only records
            // that a first use is cheap too.
            perCall.ShouldBeLessThan(1024.0);
        }

        // ------------------------------------------------------------------
        // Equivalence: the multiset-argument path must agree with the array path
        // ------------------------------------------------------------------

        public static IEnumerable<object[]> Scenarios()
        {
            // receiver, argument, seed values, duplicates, nulls, disjoint, superset, empty
            yield return new object[] { new[] { 1, 2, 3 }, new[] { 3, 4 }, "overlap" };
            yield return new object[] { new[] { 1, 1, 2 }, new[] { 1, 1, 1, 3 }, "duplicates" };
            yield return new object[] { new[] { 1, 2 }, new[] { 3, 4 }, "disjoint" };
            yield return new object[] { new[] { 1, 1, 1 }, new[] { 1 }, "receiver superset" };
            yield return new object[] { new[] { 1 }, new[] { 1, 1, 1 }, "argument superset" };
            yield return new object[] { new[] { 1, 2, 3 }, new int[0], "empty argument" };
            yield return new object[] { new int[0], new[] { 1, 2, 3 }, "empty receiver" };
            yield return new object[] { new int[0], new int[0], "both empty" };
            yield return new object[] { new[] { 1, 1, 2, 2, 2 }, new[] { 2, 2, 9 }, "mixed multiplicities" };
        }

        [Theory]
        [MemberData(nameof(Scenarios))]
        public void UnionWith_AgreesWithTheArrayArgument(int[] receiverValues, int[] argumentValues, string label)
        {
            Agrees(receiverValues, argumentValues, (b, a) => b.UnionWith(a), label);
        }

        [Theory]
        [MemberData(nameof(Scenarios))]
        public void IntersectionWith_AgreesWithTheArrayArgument(int[] receiverValues, int[] argumentValues, string label)
        {
            Agrees(receiverValues, argumentValues, (b, a) => b.IntersectionWith(a), label);
        }

        [Theory]
        [MemberData(nameof(Scenarios))]
        public void ExceptWith_AgreesWithTheArrayArgument(int[] receiverValues, int[] argumentValues, string label)
        {
            Agrees(receiverValues, argumentValues, (b, a) => b.ExceptWith(a), label);
        }

        [Theory]
        [MemberData(nameof(Scenarios))]
        public void SymmetricExceptWith_AgreesWithTheArrayArgument(int[] receiverValues, int[] argumentValues, string label)
        {
            Agrees(receiverValues, argumentValues, (b, a) => b.SymmetricExceptWith(a), label);
        }

        [Theory]
        [MemberData(nameof(Scenarios))]
        public void Predicates_AgreeWithTheArrayArgument(int[] receiverValues, int[] argumentValues, string label)
        {
            var arrayReceiver = Bag(receiverValues);
            var arrayArgument = argumentValues;
            var bagReceiver = Bag(receiverValues);
            var bagArgument = Bag(argumentValues);

            bagReceiver.IsSubsetOf(bagArgument).ShouldBe(arrayReceiver.IsSubsetOf(arrayArgument), label);
            bagReceiver.IsSupersetOf(bagArgument).ShouldBe(arrayReceiver.IsSupersetOf(arrayArgument), label);
            bagReceiver.IsProperSubsetOf(bagArgument).ShouldBe(arrayReceiver.IsProperSubsetOf(arrayArgument), label);
            bagReceiver.IsProperSupersetOf(bagArgument).ShouldBe(arrayReceiver.IsProperSupersetOf(arrayArgument), label);
            bagReceiver.Overlaps(bagArgument).ShouldBe(arrayReceiver.Overlaps(arrayArgument), label);
            bagReceiver.Equals(bagArgument).ShouldBe(arrayReceiver.Equals(Bag(argumentValues)), label);
        }

        [Fact]
        public void SetOperations_AgreeOnTheNullElement()
        {
            // Same argument shapes, but on strings where null is a real element. The bucket lives
            // outside the count table, so it has its own path in every operation.
            var scenarios = new (string[] Receiver, string[] Argument)[]
            {
                (new string[] { "a", null, null }, new string[] { null, "b" }),
                (new string[] { null, null }, new string[] { null }),
                (new string[] { "a" }, new string[] { null }),
                (new string[] { null }, new string[] { "a" }),
                (new string[] { null }, new string[] { null }),
                (new string[] { null, "a", "a" }, new string[] { "a", null, null }),
                (new string[0], new string[] { null }),
                (new string[] { null }, new string[0]),
            };

            foreach (var (receiver, argument) in scenarios)
            {
                Agrees(receiver, argument, (b, a) => b.UnionWith(a), "UnionWith");
                Agrees(receiver, argument, (b, a) => b.IntersectionWith(a), "IntersectionWith");
                Agrees(receiver, argument, (b, a) => b.ExceptWith(a), "ExceptWith");
                Agrees(receiver, argument, (b, a) => b.SymmetricExceptWith(a), "SymmetricExceptWith");
            }
        }

        // ------------------------------------------------------------------
        // Edge cases the fast path introduces
        // ------------------------------------------------------------------

        [Fact]
        public void UnionWithSelf_IsTheIdentity()
        {
            var bag = new MultiList<int> { 1, 1, 2 };

            bag.UnionWith(bag);

            bag.TotalCount.ShouldBe(3);
            bag.CountOf(1).ShouldBe(2);
            bag.CountOf(2).ShouldBe(1);
        }

        [Fact]
        public void IntersectionWithSelf_IsTheIdentity()
        {
            var bag = new MultiList<int> { 1, 1, 2 };

            bag.IntersectionWith(bag);

            bag.TotalCount.ShouldBe(3);
        }

        [Fact]
        public void ExceptWithSelf_EmptiesTheBag()
        {
            var bag = new MultiList<int> { 1, 1, 2 };

            bag.ExceptWith(bag);

            bag.TotalCount.ShouldBe(0);
            bag.DistinctCount.ShouldBe(0);
        }

        [Fact]
        public void SymmetricExceptWithSelf_EmptiesTheBag()
        {
            var bag = new MultiList<int> { 1, 1, 2 };

            bag.SymmetricExceptWith(bag);

            bag.TotalCount.ShouldBe(0);
        }

        [Fact]
        public void DefaultElementOfAValueType_IsNotMistakenForTheNullBucket()
        {
            // The null bucket is always empty for a value element type, so the fast path has to
            // leave element default(int) == 0 alone. Getting this wrong shows up as every copy of 0
            // vanishing the moment any set operation runs.
            var bag = new MultiList<int> { 0, 0, 1 };

            bag.IntersectionWith(new MultiList<int> { 0 });

            bag.CountOf(0).ShouldBe(1);
            bag.CountOf(1).ShouldBe(0);
            bag.TotalCount.ShouldBe(1);
        }

        [Fact]
        public void DefaultElementOfAValueType_SurvivesExceptWith()
        {
            var bag = new MultiList<int> { 0, 0, 1 };

            bag.ExceptWith(new MultiList<int> { 0 });

            bag.CountOf(0).ShouldBe(1);
            bag.CountOf(1).ShouldBe(1);
        }

        [Fact]
        public void DefaultElementOfAValueType_SurvivesSymmetricExceptWith()
        {
            var bag = new MultiList<int> { 0, 0, 1 };

            bag.SymmetricExceptWith(new MultiList<int> { 0 });

            bag.CountOf(0).ShouldBe(1);
            bag.CountOf(1).ShouldBe(1);
        }

        [Fact]
        public void ADifferingComparer_RefusesTheInPlacePathAndStillAgrees()
        {
            var ignoringCase = new MultiList<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            var caseSensitive = new MultiList<string>(StringComparer.Ordinal) { "a" };

            // The comparers differ, so the argument is re-materialised under the receiver's
            // comparer - exactly the behaviour before the optimisation. Comparing against the
            // array path proves the two agree.
            var viaBag = new MultiList<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            viaBag.IntersectionWith(caseSensitive);

            var viaArray = new MultiList<string>(StringComparer.OrdinalIgnoreCase) { "A" };
            viaArray.IntersectionWith(new[] { "a" });

            viaBag.CountOf("A").ShouldBe(viaArray.CountOf("A"));
            viaBag.TotalCount.ShouldBe(viaArray.TotalCount);
        }

        [Fact]
        public void ASameComparerInstance_TakesTheInPlacePath()
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            var receiver = new MultiList<string>(comparer) { "A", "b" };
            var argument = new MultiList<string>(comparer) { "a" };

            receiver.ExceptWith(argument);

            receiver.CountOf("A").ShouldBe(0);
            receiver.CountOf("b").ShouldBe(1);
        }

        [Fact]
        public void TheArgumentIsNotMutated()
        {
            var argument = new MultiList<int> { 1, 1, 2 };
            var receiver = new MultiList<int> { 1, 3 };

            receiver.UnionWith(argument);
            receiver.IntersectionWith(argument);
            receiver.ExceptWith(argument);
            receiver.SymmetricExceptWith(argument);

            argument.TotalCount.ShouldBe(3);
            argument.CountOf(1).ShouldBe(2);
            argument.CountOf(2).ShouldBe(1);
        }

        [Fact]
        public void TheArgumentIsNotMutated_WithNullElements()
        {
            var argument = new MultiList<string> { null, null, "a" };
            var receiver = new MultiList<string> { "b" };

            receiver.UnionWith(argument);
            receiver.IntersectionWith(argument);
            receiver.ExceptWith(argument);
            receiver.SymmetricExceptWith(argument);

            argument.TotalCount.ShouldBe(3);
            argument.CountOf(null).ShouldBe(2);
        }

        // ------------------------------------------------------------------
        // Differential: the two argument shapes must never diverge
        // ------------------------------------------------------------------

        [Fact]
        public void RandomOperations_NeverDivergeBetweenTheTwoArgumentShapes()
        {
            var random = new Random(20260911);

            for (var step = 0; step < 3000; step++)
            {
                var receiverValues = Enumerable.Range(0, random.Next(6))
                    .Select(_ => random.Next(4))
                    .ToList();
                var argumentValues = Enumerable.Range(0, random.Next(6))
                    .Select(_ => random.Next(4))
                    .ToList();
                var op = random.Next(8);

                var viaBag = Bag(receiverValues);
                var viaArray = Bag(receiverValues);
                var bagArgument = Bag(argumentValues);
                var arrayArgument = argumentValues.ToArray();

                switch (op)
                {
                    case 0:
                        viaBag.UnionWith(bagArgument);
                        viaArray.UnionWith(arrayArgument);
                        break;
                    case 1:
                        viaBag.IntersectionWith(bagArgument);
                        viaArray.IntersectionWith(arrayArgument);
                        break;
                    case 2:
                        viaBag.ExceptWith(bagArgument);
                        viaArray.ExceptWith(arrayArgument);
                        break;
                    case 3:
                        viaBag.SymmetricExceptWith(bagArgument);
                        viaArray.SymmetricExceptWith(arrayArgument);
                        break;
                    case 4:
                        viaBag.IsSubsetOf(bagArgument).ShouldBe(viaArray.IsSubsetOf(arrayArgument));
                        break;
                    case 5:
                        viaBag.IsSupersetOf(bagArgument).ShouldBe(viaArray.IsSupersetOf(arrayArgument));
                        break;
                    case 6:
                        viaBag.IsProperSubsetOf(bagArgument).ShouldBe(viaArray.IsProperSubsetOf(arrayArgument));
                        break;
                    default:
                        viaBag.IsProperSupersetOf(bagArgument).ShouldBe(viaArray.IsProperSupersetOf(arrayArgument));
                        break;
                }

                var description = $"step {step}, op {op}, "
                                  + $"receiver [{string.Join(",", receiverValues)}], "
                                  + $"argument [{string.Join(",", argumentValues)}]";

                viaBag.TotalCount.ShouldBe(viaArray.TotalCount, description);
                viaBag.DistinctCount.ShouldBe(viaArray.DistinctCount, description);
                foreach (var probe in new[] { 0, 1, 2, 3 })
                {
                    viaBag.CountOf(probe).ShouldBe(viaArray.CountOf(probe), description);
                }
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static void Agrees<T>(
            T[] receiverValues,
            T[] argumentValues,
            Action<MultiList<T>, IEnumerable<T>> operation,
            string label)
        {
            var viaBag = Bag(receiverValues);
            var viaArray = Bag(receiverValues);

            operation(viaBag, Bag(argumentValues));
            operation(viaArray, argumentValues);

            var description = $"{label}: receiver [{string.Join(",", receiverValues)}], "
                              + $"argument [{string.Join(",", argumentValues)}]";

            viaBag.TotalCount.ShouldBe(viaArray.TotalCount, description);
            viaBag.DistinctCount.ShouldBe(viaArray.DistinctCount, description);
            foreach (var probe in receiverValues.Concat(argumentValues).Distinct())
            {
                viaBag.CountOf(probe).ShouldBe(viaArray.CountOf(probe), $"{description}, element {probe}");
            }
        }

        private static MultiList<T> Bag<T>(IEnumerable<T> values)
        {
            var bag = new MultiList<T>();
            foreach (var value in values)
            {
                bag.Add(value);
            }

            return bag;
        }

        private static MultiList<int> Bag(int start, int count)
        {
            var bag = new MultiList<int>(count);
            for (var i = 0; i < count; i++)
            {
                bag.Add(start + i);
            }

            return bag;
        }

        private static void Reset(MultiList<int> bag, int size)
        {
            // Deliberately allocation-free: this runs inside the measured window, and a clearing
            // multiset keeps its count table's capacity so the re-adds do not allocate either.
            bag.Clear();
            for (var i = 0; i < size; i++)
            {
                bag.Add(i);
            }
        }
    }
}
