using System.Collections;
using System.Collections.Generic;

namespace DotNetCore.Collections.Tests
{
    /// <summary>
    /// Walks each source arm through its concrete value-type enumerator. The helpers are typed on
    /// the wrapper and enumerator structs rather than on <see cref="IValueEnumerable{T,TEnumerator}"/>,
    /// so the compiler binds the struct enumerator and nothing gets boxed.
    /// </summary>
    internal static class Walk
    {
        internal static List<T> FromArray<T>(ArrayValueEnumerable<T> source)
        {
            var result = new List<T>();
            foreach (var value in source)
            {
                result.Add(value);
            }

            return result;
        }

        internal static List<T> FromList<T>(ListValueEnumerable<T> source)
        {
            var result = new List<T>();
            foreach (var value in source)
            {
                result.Add(value);
            }

            return result;
        }

        internal static List<T> FromReadOnlyList<T>(ReadOnlyListValueEnumerable<T> source)
        {
            var result = new List<T>();
            foreach (var value in source)
            {
                result.Add(value);
            }

            return result;
        }

        internal static List<T> FromEnumerable<T>(EnumerableValueEnumerable<T> source)
        {
            var result = new List<T>();
            foreach (var value in source)
            {
                result.Add(value);
            }

            return result;
        }

        /// <summary>Drains an array enumerator the test obtained directly from the wrapper.</summary>
        internal static List<T> ViaArrayEnumerator<T>(ArrayValueEnumerator<T> enumerator)
        {
            var result = new List<T>();
            while (enumerator.MoveNext())
            {
                result.Add(enumerator.Current);
            }

            return result;
        }

        /// <summary>Drains a list enumerator the test obtained directly from the wrapper.</summary>
        internal static List<T> ViaListEnumerator<T>(ListValueEnumerator<T> enumerator)
        {
            var result = new List<T>();
            while (enumerator.MoveNext())
            {
                result.Add(enumerator.Current);
            }

            return result;
        }

        /// <summary>Drains a read-only-list enumerator the test obtained directly from the wrapper.</summary>
        internal static List<T> ViaReadOnlyListEnumerator<T>(ReadOnlyListValueEnumerator<T> enumerator)
        {
            var result = new List<T>();
            while (enumerator.MoveNext())
            {
                result.Add(enumerator.Current);
            }

            return result;
        }

        /// <summary>Drains a fallback enumerator the test obtained directly from the wrapper.</summary>
        internal static List<T> ViaEnumerableEnumerator<T>(EnumerableValueEnumerator<T> enumerator)
        {
            var result = new List<T>();
            while (enumerator.MoveNext())
            {
                result.Add(enumerator.Current);
            }

            return result;
        }
    }

    /// <summary>
    /// An <see cref="IEnumerable{T}"/> that records how it was consumed, so the fallback arm's
    /// laziness and its disposal forwarding can be observed.
    /// </summary>
    internal sealed class ProbeEnumerable : IEnumerable<int>
    {
        internal int EnumeratorRequests { get; private set; }

        internal int DisposeCount { get; private set; }

        public IEnumerator<int> GetEnumerator()
        {
            EnumeratorRequests++;
            return new ProbeEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void RecordDispose() => DisposeCount++;

        private sealed class ProbeEnumerator : IEnumerator<int>
        {
            private readonly ProbeEnumerable _owner;
            private int _index = -1;

            internal ProbeEnumerator(ProbeEnumerable owner) => _owner = owner;

            public int Current => _index + 1;

            object IEnumerator.Current => Current;

            public bool MoveNext() => ++_index < 3;

            public void Reset() => _index = -1;

            public void Dispose() => _owner.RecordDispose();
        }
    }
}
