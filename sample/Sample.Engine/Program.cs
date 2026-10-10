using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections;

namespace Sample.Engine
{
    /// <summary>
    /// F7-07: the dual-<c>using</c> proof. This file imports the BCL's LINQ surface and the engine's
    /// surface into one compilation unit, which is the exact situation R7-05 is about: the compiler
    /// must resolve every call without CS0121, and importing the engine must not change what a
    /// caller's existing <see cref="Enumerable"/> code binds to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The interesting case is <c>Index</c>, because it is the only name the two surfaces currently
    /// share: .NET 9 added <c>Enumerable.Index&lt;TSource&gt;(this IEnumerable&lt;TSource&gt;)</c> and the
    /// engine ships its own <c>Index</c> over the same element shape. Two facts keep that safe, and
    /// both are compile-time facts, so they are asserted by writing the type out rather than by
    /// checking a value at run time:
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// <c>array.ToValueEnumerable().Index()</c> binds to the engine. Both candidates are applicable
    /// on .NET 9+, but the engine's receiver is the exact static type (an identity conversion) while
    /// the BCL's needs an interface conversion, and the better conversion wins.
    /// </description></item>
    /// <item><description>
    /// <c>array.Index()</c> still binds to the BCL. The engine declares no conversion from
    /// <c>T[]</c> to its wrapper, so its overload is not even a candidate for a bare array.
    /// </description></item>
    /// </list>
    /// <para>
    /// Exits non-zero on the first disagreement so that CI fails rather than prints.
    /// </para>
    /// </remarks>
    internal static class Program
    {
        private static int Main()
        {
            Console.WriteLine("Sample.Engine -- `using System.Linq;` plus `using DotNetCore.Collections;` in one file");
            Console.WriteLine();

            var failures = 0;

            failures += CheckIndexBindsToTheEngine();
            failures += CheckTheBclSurfaceIsUntouched();
            failures += CheckTagFirstLast();
            failures += CheckPairwise();
            failures += CheckScan();
            failures += CheckSeededScan();
            failures += CheckForEach();
            failures += CheckTheTwoSurfacesCompose();

            Console.WriteLine();

            if (failures == 0)
            {
                Console.WriteLine("PASS: every call resolved unambiguously and agreed with its oracle.");
                return 0;
            }

            Console.WriteLine("FAIL: " + failures + " check(s) disagreed.");
            return 1;
        }

        /// <summary>
        /// The colliding call, plus the compile-time assertion of which overload won.
        /// </summary>
        /// <remarks>
        /// The declared type of <c>indexed</c> is the engine's wrapper. Had the BCL's arm been chosen
        /// on net9.0+ this statement would not compile, because <c>Enumerable.Index</c> returns
        /// <c>IEnumerable&lt;(int Index, TSource Item)&gt;</c> and no wrapper.
        /// </remarks>
        private static int CheckIndexBindsToTheEngine()
        {
            var source = new[] { 10, 20, 30 };

            IndexValueEnumerable<ArrayValueEnumerable<int>, ArrayValueEnumerator<int>, int> indexed =
                source.ToValueEnumerable().Index();

            var actual = new List<string>();
            foreach (var pair in indexed)
            {
                actual.Add(pair.Index + ":" + pair.Item);
            }

            // Oracle: the BCL's own indexed projection, which exists on every target.
            var oracle = new List<string>();
            foreach (var pair in source.Select((item, index) => (index, item)))
            {
                oracle.Add(pair.index + ":" + pair.item);
            }

            return Check(
                "engine Index binds on a value enumerable",
                string.Join(",", actual),
                string.Join(",", oracle));
        }

        private static int CheckTheBclSurfaceIsUntouched()
        {
            var source = new[] { 10, 20, 30 };

#if NET9_0_OR_GREATER
            // No conversion from int[] to the engine wrapper exists, so the engine is not a
            // candidate here and the call can only reach System.Linq. That is the guarantee a
            // consumer needs: adding the engine `using` leaves their array code alone.
            IEnumerable<(int Index, int Item)> viaBcl = source.Index();

            var actual = new List<string>();
            foreach (var pair in viaBcl)
            {
                actual.Add(pair.Index + ":" + pair.Item);
            }

            return Check(
                "bare T[] still binds to System.Linq.Enumerable.Index",
                string.Join(",", actual),
                "0:10,1:20,2:30");
#else
            Console.WriteLine("  n/a   bare T[] binds to System.Linq.Enumerable.Index -- no BCL Index before .NET 9");
            return 0;
#endif
        }

        private static int CheckTagFirstLast()
        {
            var source = new[] { 1, 2, 3, 4 };

            var actual = new List<string>();
            foreach (var text in source.ToValueEnumerable().TagFirstLast(
                (value, isFirst, isLast) => (isFirst ? "<" : string.Empty) + value + (isLast ? ">" : string.Empty)))
            {
                actual.Add(text);
            }

            return Check("TagFirstLast", string.Join(",", actual), "<1,2,3,4>");
        }

        private static int CheckPairwise()
        {
            var source = new[] { 1, 2, 3, 4 };

            var actual = new List<string>();
            foreach (var text in source.ToValueEnumerable().Pairwise((left, right) => left + "-" + right))
            {
                actual.Add(text);
            }

            return Check("Pairwise", string.Join(",", actual), "1-2,2-3,3-4");
        }

        private static int CheckScan()
        {
            var source = new[] { 1, 2, 3, 4 };

            var actual = new List<int>();
            foreach (var value in source.ToValueEnumerable().Scan((aggregate, value) => aggregate + value))
            {
                actual.Add(value);
            }

            return Check("Scan (seedless)", string.Join(",", actual), "1,3,6,10");
        }

        private static int CheckSeededScan()
        {
            var source = new[] { 1, 2, 3, 4 };

            var actual = new List<int>();
            foreach (var value in source.ToValueEnumerable().Scan(100, (aggregate, value) => aggregate + value))
            {
                actual.Add(value);
            }

            return Check("Scan (seeded)", string.Join(",", actual), "100,101,103,106,110");
        }

        private static int CheckForEach()
        {
            var source = new[] { 1, 2, 3, 4 };

            var sum = 0;
            source.ToValueEnumerable().ForEach(value => sum += value);

            var weighted = 0;
            source.ToValueEnumerable().ForEach((value, index) => weighted += value * index);

            return Check("ForEach (plain and indexed)", sum + "/" + weighted, "10/20");
        }

        /// <summary>
        /// Both surfaces interleaved inside one expression: the import of one must not shadow the
        /// other, and an engine result must be usable by the BCL straight away.
        /// </summary>
        private static int CheckTheTwoSurfacesCompose()
        {
            var actual = Enumerable.Range(1, 6)                      // System.Linq
                .Where(value => value % 2 == 0)                      // System.Linq -> 2, 4, 6
                .ToArray()                                           // System.Linq
                .ToValueEnumerable()                                 // engine entry point
                .Scan((aggregate, value) => aggregate + value)       // engine -> 2, 6, 12
                .ToArray();                                          // System.Linq again

            return Check("System.Linq and the engine compose", string.Join(",", actual), "2,6,12");
        }

        private static int Check(string what, string actual, string expected)
        {
            var ok = string.Equals(actual, expected, StringComparison.Ordinal);

            Console.WriteLine(
                (ok ? "  PASS  " : "  FAIL  ")
                + what.PadRight(46, '.')
                + " got [" + actual + "]"
                + (ok ? string.Empty : " expected [" + expected + "]"));

            return ok ? 0 : 1;
        }
    }
}
