using System;
using System.Collections;
using System.Collections.Generic;

namespace DotNetCore.Collections
{
    /// <summary>
    /// <c>OrderedMerge(first, second)</c>: merges two sequences that are <em>already</em> ordered
    /// into one ordered sequence, without buffering either side.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the streaming half of the merge pair: it pulls one element from each side, hands over
    /// the smaller one and pulls its replacement, so the walk is O(n) and constant-memory. When the
    /// two keys are equal it yields the element from <c>first</c> first, which is what makes the
    /// merge stable with respect to the two inputs.
    /// </para>
    /// <para>
    /// The ordering is a precondition, not something the operator checks. Verifying it would mean
    /// looking at every element twice; a caller whose inputs are not ordered wants
    /// <c>SortedMerge</c> instead.
    /// </para>
    /// <para>
    /// The comparison is held as a delegate rather than as an <see cref="IComparer{T}"/> because the
    /// keyed form has to project both operands before comparing, and a delegate lets that projection
    /// be composed once at construction instead of being an interface implementation whose
    /// nullability cannot be expressed on the lower language version the engine compiles with.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of the elements.</typeparam>
    internal sealed class OrderedMergeEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _first;
        private readonly IEnumerable<T> _second;
        private readonly Func<T, T, int> _compare;

        internal OrderedMergeEnumerable(IEnumerable<T> first, IEnumerable<T> second, Func<T, T, int> compare)
        {
            _first = first;
            _second = second;
            _compare = compare;
        }

        public IEnumerator<T> GetEnumerator() => new OrderedMergeEnumerator<T>(_first, _second, _compare);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Performs the two-way merge. Each side is read one element ahead, so the comparison always
    /// has both candidates in hand and no element is buffered beyond the two.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    internal sealed class OrderedMergeEnumerator<T> : IEnumerator<T>
    {
        private readonly IEnumerator<T> _first;
        private readonly IEnumerator<T> _second;
        private readonly Func<T, T, int> _compare;
        private bool _firstHas;
        private bool _secondHas;
        private bool _started;
        private bool _finished;
        private T _firstCurrent;
        private T _secondCurrent;
        private T _current;

        internal OrderedMergeEnumerator(IEnumerable<T> first, IEnumerable<T> second, Func<T, T, int> compare)
        {
            _first = first.GetEnumerator();
            _second = second.GetEnumerator();
            _compare = compare;
            _firstHas = false;
            _secondHas = false;
            _started = false;
            _finished = false;
            _firstCurrent = default!;
            _secondCurrent = default!;
            _current = default!;
        }

        public T Current => _current;

        object IEnumerator.Current => _current!;

        public bool MoveNext()
        {
            if (_finished)
            {
                return false;
            }

            if (!_started)
            {
                Prime();
            }

            if (_firstHas && _secondHas)
            {
                if (_compare(_firstCurrent, _secondCurrent) <= 0)
                {
                    _current = _firstCurrent;
                    AdvanceFirst();
                }
                else
                {
                    _current = _secondCurrent;
                    AdvanceSecond();
                }

                return true;
            }

            if (_firstHas)
            {
                _current = _firstCurrent;
                AdvanceFirst();
                return true;
            }

            if (_secondHas)
            {
                _current = _secondCurrent;
                AdvanceSecond();
                return true;
            }

            _finished = true;
            return false;
        }

        private void Prime()
        {
            _firstHas = _first.MoveNext();
            _secondHas = _second.MoveNext();
            _started = true;

            if (_firstHas)
            {
                _firstCurrent = _first.Current;
            }

            if (_secondHas)
            {
                _secondCurrent = _second.Current;
            }
        }

        private void AdvanceFirst()
        {
            _firstHas = _first.MoveNext();
            if (_firstHas)
            {
                _firstCurrent = _first.Current;
            }
        }

        private void AdvanceSecond()
        {
            _secondHas = _second.MoveNext();
            if (_secondHas)
            {
                _secondCurrent = _second.Current;
            }
        }

        public void Reset() => throw new NotSupportedException("A merged sequence cannot be reset.");

        public void Dispose()
        {
            _first.Dispose();
            _second.Dispose();
            _finished = true;
        }
    }

    /// <summary>
    /// <c>SortedMerge(first, second)</c>: the forgiving half of the merge pair. It does not assume
    /// either side is ordered; it stably sorts each side and then merges them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The cost is O(n log n) plus a buffer of both inputs, against <c>OrderedMerge</c>'s O(n) and
    /// constant memory. That trade is the whole point of having both: one is for streams that are
    /// already ordered, the other for collections that are merely available.
    /// </para>
    /// <para>
    /// The sort is a bottom-up merge sort rather than <see cref="Array.Sort{T}(T[],IComparer{T})"/>
    /// because <see cref="Array.Sort{T}(T[],IComparer{T})"/> is not stable, and stability here is
    /// observable: two equal elements coming from the same side would otherwise be free to swap.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of the elements.</typeparam>
    internal sealed class SortedMergeEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _first;
        private readonly IEnumerable<T> _second;
        private readonly Func<T, T, int> _compare;

        internal SortedMergeEnumerable(IEnumerable<T> first, IEnumerable<T> second, Func<T, T, int> compare)
        {
            _first = first;
            _second = second;
            _compare = compare;
        }

        public IEnumerator<T> GetEnumerator()
        {
            var left = SortedCopy(_first, _compare);
            var right = SortedCopy(_second, _compare);
            return new OrderedMergeEnumerator<T>(left, right, _compare);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private static T[] SortedCopy(IEnumerable<T> source, Func<T, T, int> compare)
        {
            var items = new List<T>(source).ToArray();
            if (items.Length > 1)
            {
                var scratch = new T[items.Length];
                var width = 1;
                while (width < items.Length)
                {
                    for (var start = 0; start < items.Length; start += width * 2)
                    {
                        var middle = Math.Min(start + width, items.Length);
                        var end = Math.Min(start + (width * 2), items.Length);
                        MergeRun(items, scratch, start, middle, end, compare);
                    }

                    Array.Copy(scratch, items, items.Length);
                    width *= 2;
                }
            }

            return items;
        }

        private static void MergeRun(T[] items, T[] scratch, int start, int middle, int end, Func<T, T, int> compare)
        {
            var left = start;
            var right = middle;
            var target = start;

            // '<=' is what keeps the left run ahead among equal keys, and therefore what keeps the
            // sort stable.
            while (left < middle && right < end)
            {
                if (compare(items[left], items[right]) <= 0)
                {
                    scratch[target++] = items[left++];
                }
                else
                {
                    scratch[target++] = items[right++];
                }
            }

            while (left < middle)
            {
                scratch[target++] = items[left++];
            }

            while (right < end)
            {
                scratch[target++] = items[right++];
            }
        }
    }
}
