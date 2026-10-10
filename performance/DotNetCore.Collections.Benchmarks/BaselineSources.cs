using System.Collections.Generic;

namespace DotNetCore.Collections.Benchmarks
{
    /// <summary>
    /// The shared inputs of the F7-04 baseline suite, plus the two delegates every arm uses.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every source is built by an explicit loop rather than from an array literal, on purpose. The
    /// .NET Framework 4.8 CLR on this machine hands back corrupted bytes for the first static data
    /// blob a module emits - the mechanism C# array literals are lowered to
    /// (<c>RuntimeHelpers.InitializeArray</c>) - so a literal would give the Framework leg a
    /// sequence whose values differ from the source text while the modern leg gets the right ones.
    /// Building the payload at run time keeps both legs measuring the same data.
    /// </para>
    /// <para>
    /// The two delegates are static methods rather than lambdas so the compiler caches one
    /// delegate instance each: a lambda in the benchmark body would allocate per call and the
    /// allocation would show up in the very column the suite is trying to read.
    /// </para>
    /// </remarks>
    internal static class BaselineSources
    {
        /// <summary>The element count of every source in the suite.</summary>
        internal const int Length = 100000;

        /// <summary>Builds the <see cref="System.Array"/> arm's source: 0..Length-1.</summary>
        internal static int[] CreateArray() {
            var values = new int[Length];
            for (var i = 0; i < values.Length; i++) {
                values[i] = i;
            }

            return values;
        }

        /// <summary>Builds the <see cref="List{T}"/> arm's source: 0..Length-1.</summary>
        internal static List<int> CreateList() {
            var values = new List<int>(Length);
            for (var i = 0; i < Length; i++) {
                values.Add(i);
            }

            return values;
        }

        /// <summary>Keeps half the elements. A bit test rather than <c>% 2</c>: an integer division
        /// costs ~20-40 cycles and would swamp the enumeration overhead the suite measures.</summary>
        internal static bool IsEven(int value) => (value & 1) == 0;

        /// <summary>Never matches on 0..Length-1, so an <c>Any</c> arm has to walk the whole
        /// sequence instead of returning on the first element.</summary>
        internal static bool IsNegative(int value) => value < 0;

        /// <summary>The selector of the projection and fusion arms.</summary>
        internal static int Increment(int value) => value + 1;
    }
}
