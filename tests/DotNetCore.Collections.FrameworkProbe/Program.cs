using System;
using System.Collections.Generic;
using System.Reflection;
using DotNetCore.Collections;
using DotNetCore.Collections.Internal;

namespace DotNetCore.Collections.FrameworkProbe
{
    /// <summary>
    /// F7-02: verifies at runtime, on each .NET Framework generation the package targets, that the
    /// list arm reads <c>List&lt;T&gt;._items</c> from the offset it assumes. The check is anchored
    /// on reflection: the array reached through <c>Unsafe.As&lt;List&lt;T&gt;, ListLayout&lt;T&gt;&gt;</c>
    /// has to be the very same instance reflection returns for the private field, on every
    /// generation. A moved field would instead hand back a different array and the probe would exit
    /// non-zero.
    ///
    /// The four generations of the 4.x line cannot be reached by the xunit stack
    /// (Microsoft.NET.Test.Sdk needs net462 or newer), which is why this is a console app rather
    /// than a test project, and why the four frameworks are exercised one process at a time.
    ///
    /// Run it with:
    ///     dotnet build tests/DotNetCore.Collections.FrameworkProbe -c Release
    ///     bin/Release/net451/DotNetCore.Collections.FrameworkProbe.exe   (and net461 / net47 / net48)
    ///
    /// NOTE for maintainers: every fixture below is built with a loop instead of a collection or
    /// array initialiser, on purpose. C# array literals are materialised through the runtime's
    /// static-data blob, and the desktop CLR of the development machine (CLR 4.0.30319.42000)
    /// returns corrupted bytes for the first blob an assembly emits - the values read back from
    /// such an array disagree with the bytes in the compiled file, stably within one process and
    /// differently across processes. Comparing a walk against a literal would make this probe
    /// report failures that have nothing to do with the library, so the framework's own enumerator
    /// is used as the oracle instead. The library itself, and net8.0, are unaffected.
    /// </summary>
    internal static class Program
    {
        private const int ElementCount = 1000;

        private static int Main()
        {
            var failures = new List<string>();

            CheckListLayoutMatchesReflection(failures);
            CheckListLayoutFollowsAResize(failures);
            CheckListArmIgnoresSpareCapacity(failures);
            CheckListArmMirrorsInPlaceWrites(failures);
            CheckListArmAfterRemovals(failures);
            CheckListArmAfterGrowth(failures);
            CheckListArmOfReferenceTypeElements(failures);
            CheckArrayArm(failures);
            CheckReadOnlyListArm(failures);
            CheckFallbackArm(failures);
            CheckDispatchFollowsTheStaticType(failures);
            CheckEnumeratorContract(failures);

            foreach (var failure in failures)
            {
                Console.Error.WriteLine("FAIL " + failure);
            }

            Console.WriteLine((failures.Count == 0 ? "PASS " : "FAILED ") + Target + " (CLR " + Environment.Version + ", " + failures.Count + " failure(s))");

            return failures.Count == 0 ? 0 : 1;
        }

        /// <summary>
        /// The list arm only works because <c>_items</c> is the first field of <c>List&lt;T&gt;</c>,
        /// which is what <c>Unsafe.As&lt;List&lt;T&gt;, ListLayout&lt;T&gt;&gt;</c> relies on. Comparing
        /// the accessor against reflection turns that assumption into a per-generation check.
        /// </summary>
        private static void CheckListLayoutMatchesReflection(List<string> failures)
        {
            var source = new List<int>(64);
            for (var index = 0; index < 7; index++)
            {
                source.Add(1000 + index);
            }

            var backing = BackingField();
            if (backing == null)
            {
                failures.Add("list-layout: List<int> has no _items field to compare against");
                return;
            }

            if (!(backing.GetValue(source) is int[] reflected))
            {
                failures.Add("list-layout: List<int>._items is not an int[]");
                return;
            }

            var throughLayout = ListLayoutAccessor.GetItems(source);
            if (!ReferenceEquals(reflected, throughLayout))
            {
                failures.Add("list-layout: the layout accessor reached a different array than reflection does");
                return;
            }

            if (throughLayout.Length != source.Capacity)
            {
                failures.Add("list-layout: the reachable array has length " + throughLayout.Length + " but the list capacity is " + source.Capacity);
            }
        }

        /// <summary>
        /// The accessor has to read the field on every call instead of caching the array, so a
        /// resize that swaps the backing store must be observed.
        /// </summary>
        private static void CheckListLayoutFollowsAResize(List<string> failures)
        {
            var source = new List<int>();
            source.Add(1);

            var before = ListLayoutAccessor.GetItems(source);
            var capacity = source.Capacity;
            for (var index = source.Count; index <= capacity; index++)
            {
                source.Add(index);
            }

            var after = ListLayoutAccessor.GetItems(source);
            if (ReferenceEquals(before, after))
            {
                failures.Add("list-layout: the backing array did not change after the list outgrew its capacity");
                return;
            }

            if (!(BackingField()?.GetValue(source) is int[] reflected) || !ReferenceEquals(reflected, after))
            {
                failures.Add("list-layout: the layout accessor went stale across a resize");
            }
        }

        /// <summary>A list with spare capacity must still walk only its live elements.</summary>
        private static void CheckListArmIgnoresSpareCapacity(List<string> failures)
        {
            var source = new List<int>(64);
            for (var index = 0; index < 3; index++)
            {
                source.Add(700 + index);
            }

            var backing = ListLayoutAccessor.GetItems(source);
            if (backing.Length <= source.Count)
            {
                failures.Add("list-arm-spare-capacity: the fixture has no spare capacity (length " + backing.Length + ", count " + source.Count + ")");
                return;
            }

            ExpectAgreesWithFramework(failures, "list-arm-spare-capacity", WalkListArm(source), source);
        }

        /// <summary>
        /// The array reached through the layout is the list's live storage, so writing an element
        /// in place after the wrapper was created has to be visible to the walk.
        /// </summary>
        private static void CheckListArmMirrorsInPlaceWrites(List<string> failures)
        {
            var source = new List<int>(64);
            for (var index = 0; index < 4; index++)
            {
                source.Add(10 + index);
            }

            var wrapper = source.ToValueEnumerable();
            source[2] = 999;

            var actual = new List<int>();
            foreach (var value in wrapper)
            {
                actual.Add(value);
            }

            ExpectAgreesWithFramework(failures, "list-arm-in-place-write", actual, source);

            if (actual.Count == 4 && actual[2] != 999)
            {
                failures.Add("list-arm-in-place-write: the walk did not observe the element written after the wrapper was created");
            }
        }

        private static void CheckListArmAfterRemovals(List<string> failures)
        {
            var source = new List<int>();
            for (var index = 0; index < 5; index++)
            {
                source.Add(index + 1);
            }

            source.RemoveAt(4);
            source.RemoveAt(0);

            ExpectAgreesWithFramework(failures, "list-arm-after-removals", WalkListArm(source), source);
        }

        private static void CheckListArmAfterGrowth(List<string> failures)
        {
            var source = new List<int>();
            for (var index = 0; index < ElementCount; index++)
            {
                source.Add(index);
            }

            ExpectAgreesWithFramework(failures, "list-arm-after-growth", WalkListArm(source), source);
        }

        private static void CheckListArmOfReferenceTypeElements(List<string> failures)
        {
            var source = new List<string>(16);
            source.Add("a");
            source.Add(null);
            source.Add("c");

            var actual = new List<string>();
            foreach (var value in source.ToValueEnumerable())
            {
                actual.Add(value);
            }

            if (actual.Count != 3 || actual[0] != "a" || actual[1] != null || actual[2] != "c")
            {
                failures.Add("list-arm-reference-elements: unexpected walk over a reference-type list");
            }
        }

        private static void CheckArrayArm(List<string> failures)
        {
            var source = new int[32];
            for (var index = 0; index < source.Length; index++)
            {
                source[index] = 500 + index;
            }

            var actual = new List<int>();
            foreach (var value in source.ToValueEnumerable())
            {
                actual.Add(value);
            }

            ExpectAgreesWithFramework(failures, "array-arm", actual, source);
        }

        private static void CheckReadOnlyListArm(List<string> failures)
        {
            var list = new List<int>();
            for (var index = 0; index < 6; index++)
            {
                list.Add(200 + index);
            }

            IReadOnlyList<int> source = list;

            var actual = new List<int>();
            foreach (var value in source.ToValueEnumerable())
            {
                actual.Add(value);
            }

            ExpectAgreesWithFramework(failures, "read-only-list-arm", actual, source);
        }

        private static void CheckFallbackArm(List<string> failures)
        {
            var queue = new Queue<int>();
            for (var index = 0; index < 5; index++)
            {
                queue.Enqueue(300 + index);
            }

            IEnumerable<int> source = queue;

            var actual = new List<int>();
            foreach (var value in source.ToValueEnumerable())
            {
                actual.Add(value);
            }

            ExpectAgreesWithFramework(failures, "fallback-arm", actual, source);
        }

        /// <summary>
        /// The dispatch follows the static type of the source, so an array seen through
        /// <see cref="IReadOnlyList{T}"/> takes the read-only-list arm rather than the array arm.
        /// </summary>
        private static void CheckDispatchFollowsTheStaticType(List<string> failures)
        {
            var array = new int[4];
            for (var index = 0; index < array.Length; index++)
            {
                array[index] = 22 + index;
            }

            ReadOnlyListValueEnumerable<int> throughInterface = ((IReadOnlyList<int>)array).ToValueEnumerable();

            if (throughInterface.GetEnumerator().GetType() != typeof(ReadOnlyListValueEnumerator<int>))
            {
                failures.Add("dispatch-static-type: an array seen through IReadOnlyList<int> did not take the read-only-list arm");
                return;
            }

            var actual = new List<int>();
            foreach (var value in throughInterface)
            {
                actual.Add(value);
            }

            ExpectAgreesWithFramework(failures, "dispatch-static-type", actual, array);
        }

        private static void CheckEnumeratorContract(List<string> failures)
        {
            var source = new List<int>(8);
            for (var index = 0; index < 3; index++)
            {
                source.Add(40 + index);
            }

            var enumerator = source.ToValueEnumerable().GetEnumerator();

            if (!ThrowsBeforeStart(enumerator))
            {
                failures.Add("contract-current-before-start: Current did not throw");
            }

            enumerator.MoveNext();
            if (enumerator.Current != 40)
            {
                failures.Add("contract-first-element: expected 40 but was " + enumerator.Current);
            }

            enumerator.Reset();
            enumerator.MoveNext();
            if (enumerator.Current != 40)
            {
                failures.Add("contract-reset: the walk did not restart at the first element");
            }

            while (enumerator.MoveNext())
            {
            }

            if (enumerator.MoveNext())
            {
                failures.Add("contract-end: MoveNext kept reporting true past the end of the list");
            }

            if (!ThrowsBeforeStart(enumerator))
            {
                failures.Add("contract-current-after-end: Current did not throw");
            }

            // Dispose owns no resources, so it has to be safe to call more than once.
            enumerator.Dispose();
            enumerator.Dispose();
        }

        /// <summary>Reads <c>Current</c> on a copy, so the probe never has to hand the enumerator out.</summary>
        private static bool ThrowsBeforeStart(ListValueEnumerator<int> enumerator)
        {
            try
            {
                var value = enumerator.Current;
                return false;
            }
            catch (InvalidOperationException)
            {
                return true;
            }
        }

        private static List<int> WalkListArm(List<int> source)
        {
            var actual = new List<int>();
            foreach (var value in source.ToValueEnumerable())
            {
                actual.Add(value);
            }

            return actual;
        }

        /// <summary>
        /// Compares a walk against the framework's own enumerator over the same instance. The
        /// framework enumerator is the oracle, so no expected sequence has to be spelled out.
        /// </summary>
        private static void ExpectAgreesWithFramework(List<string> failures, string name, List<int> actual, IEnumerable<int> source)
        {
            var reference = new List<int>();
            foreach (var value in source)
            {
                reference.Add(value);
            }

            if (actual.Count != reference.Count)
            {
                failures.Add(name + ": the arm produced " + actual.Count + " elements but the framework enumerator produced " + reference.Count);
                return;
            }

            for (var index = 0; index < reference.Count; index++)
            {
                if (actual[index] != reference[index])
                {
                    failures.Add(name + ": element " + index + " was " + actual[index] + " but the framework enumerator reports " + reference[index]);
                    return;
                }
            }
        }

        private static FieldInfo BackingField() =>
            typeof(List<int>).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic);

#if NET48_OR_GREATER
        private const string Target = "net48";
#elif NET47_OR_GREATER
        private const string Target = "net47";
#elif NET461_OR_GREATER
        private const string Target = "net461";
#elif NET451_OR_GREATER
        private const string Target = "net451";
#else
        private const string Target = "unknown";
#endif
    }
}
