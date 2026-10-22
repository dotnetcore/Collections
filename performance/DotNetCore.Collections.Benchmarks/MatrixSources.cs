using System;
using System.Collections;
using System.Collections.Generic;

namespace DotNetCore.Collections.Benchmarks
{
    /// <summary>
    /// A plain sequence that is neither an array nor a list, so the engine selects its
    /// <c>IEnumerable&lt;T&gt;</c> arm when it is wrapped.
    /// </summary>
    /// <remarks>
    /// The type deliberately implements only <see cref="IEnumerable{T}"/>. It exposes no
    /// <see cref="ICollection{T}"/> and no <see cref="IReadOnlyCollection{T}"/>, which is what makes
    /// the engine's count hook decline on this arm instead of answering from a count property - the
    /// distinction the F7-15 report has to state rather than hide.
    /// </remarks>
    internal sealed class SequenceSource : IEnumerable<int>
    {
        private readonly int _length;

        /// <summary>Creates a sequence of <paramref name="length"/> elements: <c>0..length-1</c>.</summary>
        /// <param name="length">The element count.</param>
        internal SequenceSource(int length) {
            _length = length;
        }

        /// <summary>Yields <c>0..length-1</c>.</summary>
        /// <returns>The sequence's enumerator.</returns>
        public IEnumerator<int> GetEnumerator() {
            for (var i = 0; i < _length; i++) {
                yield return i;
            }
        }

        /// <summary>Yields <c>0..length-1</c>.</summary>
        /// <returns>The sequence's enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// The inputs and delegates of the F7-15 operator matrix: every operator name the engine
    /// exposes over four element sources, at one element count.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The matrix runs at a tenth of the Gate's element count (<see cref="Length"/> instead of
    /// <see cref="BaselineSources.Length"/>). The matrix answers two questions - how each source arm
    /// of an operator compares with the other three, and how many bytes the operator allocates - and
    /// both are ratios or per-operation counts that do not need the Gate's 100 000 elements. Keeping
    /// the walk short is what makes 200 measured methods finish inside the suite's wall-clock budget.
    /// </para>
    /// <para>
    /// Every source is built by an explicit loop for the reason <see cref="BaselineSources"/> gives:
    /// this machine's .NET Framework 4.8 CLR hands back corrupted bytes for a module's first static
    /// data blob, so an array literal would give the Framework leg different data from the modern
    /// leg.
    /// </para>
    /// <para>
    /// The delegates are static methods rather than lambdas so the compiler caches one delegate
    /// instance each; a lambda inside a benchmark body would allocate per invocation and that
    /// allocation would land in the column the report reads.
    /// </para>
    /// </remarks>
    internal static class MatrixSources
    {
        /// <summary>The element count of every primary source in the matrix.</summary>
        internal const int Length = 10000;

        /// <summary>The bucket count of the coarse key selectors; 1024 groups over 10 000 elements.</summary>
        internal const int Buckets = 1024;

        /// <summary>The bucket count of the fine key selector used by the grouping operators.</summary>
        internal const int SmallBuckets = 32;

        /// <summary>The element count of the second sequence used by the binary operators.</summary>
        internal const int SecondLength = 1024;

        /// <summary>The element count of the short sequence the Cartesian arm multiplies by.</summary>
        internal const int SmallLength = 32;

        /// <summary>The size of the window / chunk / partial-sort arms.</summary>
        internal const int WindowSize = 16;

        /// <summary>Written by <see cref="Observe"/>; makes the side-effecting operators
        /// impossible to elide.</summary>
        internal static int Sink;

        /// <summary>Builds <c>0..Length-1</c> as an array.</summary>
        /// <returns>The array arm's source.</returns>
        internal static int[] CreateArray() {
            var values = new int[Length];
            for (var i = 0; i < values.Length; i++) {
                values[i] = i;
            }

            return values;
        }

        /// <summary>Builds <c>0..Length-1</c> as a list.</summary>
        /// <returns>The list arm's source.</returns>
        internal static List<int> CreateList() {
            var values = new List<int>(Length);
            for (var i = 0; i < Length; i++) {
                values.Add(i);
            }

            return values;
        }

        /// <summary>Builds <c>0..Length-1</c> as a read-only list.</summary>
        /// <param name="list">The list to wrap; it stays the backing store, so the arm measures an
        /// indexer reached through <see cref="IReadOnlyList{T}"/>.</param>
        /// <returns>The read-only-list arm's source.</returns>
        internal static IReadOnlyList<int> CreateReadOnlyList(List<int> list) => new ReadOnlyListAdapter(list);

        /// <summary>Builds <c>0..SecondLength-1</c>: the second sequence of the binary operators, and
        /// the key sequence of the set operators.</summary>
        /// <returns>The second source.</returns>
        internal static int[] CreateSecond() {
            var values = new int[SecondLength];
            for (var i = 0; i < values.Length; i++) {
                values[i] = i;
            }

            return values;
        }

        /// <summary>Builds <c>0..SmallLength-1</c>: the multiplier of the Cartesian arm.</summary>
        /// <returns>The short second source.</returns>
        internal static int[] CreateSmall() {
            var values = new int[SmallLength];
            for (var i = 0; i < values.Length; i++) {
                values[i] = i;
            }

            return values;
        }

        /// <summary>Builds an ascending sequence disjoint from and after the primary source, for the
        /// merge operators that require both inputs ordered.</summary>
        /// <returns>The ordered second source.</returns>
        internal static int[] CreateSortedSecond() {
            var values = new int[Length];
            for (var i = 0; i < values.Length; i++) {
                values[i] = i + (Length / 2);
            }

            return values;
        }

        /// <summary>Keeps half the elements; a bit test rather than <c>% 2</c>.</summary>
        /// <param name="value">The element.</param>
        /// <returns><see langword="true"/> for even values.</returns>
        internal static bool IsEven(int value) => (value & 1) == 0;

        /// <summary>The predicate of the split arm: cuts at every value that is a multiple of 1024.</summary>
        /// <param name="value">The element.</param>
        /// <returns><see langword="true"/> at a separator.</returns>
        internal static bool IsSeparator(int value) => (value & (Buckets - 1)) == 0;

        /// <summary>Never matches; keeps a filter from short-circuiting.</summary>
        /// <param name="value">The element.</param>
        /// <returns>Always <see langword="false"/> for a non-negative source.</returns>
        internal static bool IsNegative(int value) => value < 0;

        /// <summary>The projection of the map arms.</summary>
        /// <param name="value">The element.</param>
        /// <returns>The element plus one.</returns>
        internal static int Increment(int value) => value + 1;

        /// <summary>The identity projection; the partial-sort key.</summary>
        /// <param name="value">The element.</param>
        /// <returns>The element.</returns>
        internal static int Identity(int value) => value;

        /// <summary>The binary selector of the pairwise / scan / zip / merge / join arms.</summary>
        /// <param name="left">The first element.</param>
        /// <param name="right">The second element.</param>
        /// <returns>The sum of the two.</returns>
        internal static int Add(int left, int right) => left + right;

        /// <summary>Returns the current element, so a lag / lead arm pays for the pairing and not for
        /// an arithmetic step.</summary>
        /// <param name="previous">The earlier element.</param>
        /// <param name="current">The current element.</param>
        /// <returns>The current element.</returns>
        internal static int PickSecond(int previous, int current) => current;

        /// <summary>The coarse key: 1024 buckets over the primary source.</summary>
        /// <param name="value">The element.</param>
        /// <returns><c>value &amp; 1023</c>.</returns>
        internal static int Bucket(int value) => value & (Buckets - 1);

        /// <summary>The fine key: 32 buckets.</summary>
        /// <param name="value">The element.</param>
        /// <returns><c>value &amp; 31</c>.</returns>
        internal static int SmallBucket(int value) => value & (SmallBuckets - 1);

        /// <summary>The grouping key: 625 runs of 16 consecutive elements.</summary>
        /// <param name="value">The element.</param>
        /// <returns><c>value &gt;&gt; 4</c>.</returns>
        internal static int Run(int value) => value >> 4;

        /// <summary>The chooser of the <c>Choose</c> arm: keeps the even elements, drops the rest.</summary>
        /// <param name="value">The element.</param>
        /// <returns>A keep flag and, when kept, the element.</returns>
        internal static (bool Keep, int Value) TryHalf(int value)
            => (value & 1) == 0 ? (true, value) : (false, 0);

        /// <summary>The result selector of the <c>TagFirstLast</c> arm.</summary>
        /// <param name="value">The element.</param>
        /// <param name="isFirst">Whether this is the first element.</param>
        /// <param name="isLast">Whether this is the last element.</param>
        /// <returns>The element, so the arm pays for the tagging and not for a projection.</returns>
        internal static int Tag(int value, bool isFirst, bool isLast) => value;

        /// <summary>The result selector of the <c>FullGroupJoin</c> arm: the key alone, so the inner
        /// groups are never materialised and the row measures the join's own pass.</summary>
        /// <param name="key">The group key.</param>
        /// <param name="left">The outer group.</param>
        /// <param name="right">The inner group.</param>
        /// <returns>The key.</returns>
        internal static int KeyOf(int key, IEnumerable<int> left, IEnumerable<int> right) => key;

        /// <summary>The side effect of the <c>Do</c> and <c>ForEach</c> arms.</summary>
        /// <param name="value">The element.</param>
        internal static void Observe(int value) {
            Sink = value;
        }
    }

    /// <summary>
    /// A read-only view over a list that exposes nothing but <see cref="IReadOnlyList{T}"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}"/> would do the job, but it
    /// also implements <see cref="IList{T}"/> and <see cref="IList"/>, so the engine's
    /// <c>IReadOnlyList&lt;T&gt;</c> arm would be reached through a type that carries more contracts
    /// than the arm needs. This adapter carries exactly one, which is what makes the arm's cost
    /// attributable to the indexer the arm actually calls.
    /// </remarks>
    internal sealed class ReadOnlyListAdapter : IReadOnlyList<int>
    {
        private readonly List<int> _list;

        /// <summary>Wraps a list.</summary>
        /// <param name="list">The backing store.</param>
        internal ReadOnlyListAdapter(List<int> list) {
            _list = list;
        }

        /// <summary>Gets the element count.</summary>
        public int Count => _list.Count;

        /// <summary>Gets the element at a position.</summary>
        /// <param name="index">The zero-based position.</param>
        /// <returns>The element.</returns>
        public int this[int index] => _list[index];

        /// <summary>Enumerates the backing list.</summary>
        /// <returns>The backing list's enumerator.</returns>
        public IEnumerator<int> GetEnumerator() => _list.GetEnumerator();

        /// <summary>Enumerates the backing list.</summary>
        /// <returns>The backing list's enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
