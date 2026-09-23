using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using DotNetCore.Collections.Multi;

namespace DotNetCore.Collections.Multi.Benchmarks
{
    /// <summary>
    /// F6-31 evidence (R6-02): <see cref="Deque{T}"/> against the only two-ended collection the
    /// base class library ships, <see cref="LinkedList{T}"/>, and against the two one-ended
    /// baselines, <see cref="Queue{T}"/> and <see cref="List{T}"/>. Every claim the type makes in
    /// its documentation is measured here: that adding and removing at <em>either</em> end is O(1)
    /// amortized, that reading a position is O(1), and that the ring is one array rather than one
    /// allocation per element.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rotation benchmarks are the O(1) evidence. Each runs <c>Count</c> add-then-remove pairs
    /// over a collection that already holds <c>Count</c> elements, so the collection never grows
    /// and nothing but the two end operations is on the clock. If those operations were not O(1),
    /// the mean time would rise faster than the number of operations as <c>Count</c> goes from 64
    /// to 65,536 - a 1,024x increase in work.
    /// </para>
    /// <para>
    /// <see cref="Queue{T}"/> is the one-ended baseline: it answers whether a deque pays anything
    /// for having a second end. It is deliberately constructed with room for the whole rotation,
    /// because <c>Queue&lt;T&gt;</c> grows its buffer when its tail runs into the end of it and
    /// that growth is a property of <c>Queue&lt;T&gt;</c>, not of the operation being measured. The
    /// deque is sized for its starting contents only - its ring is what is under test.
    /// </para>
    /// <para>
    /// The build benchmarks include the growth path, so their allocation figure is the honest
    /// total: one buffer (plus the discarded smaller ones) for the deque, one node per element for
    /// <see cref="LinkedList{T}"/>.
    /// </para>
    /// </remarks>
    [ShortRunJob]
    [MemoryDiagnoser]
    public class Deque_RingBuffer
    {
        [Params(64, 4096, 65536)]
        public int Count;

        private Deque<int> _deque;
        private LinkedList<int> _linked;
        private Queue<int> _queue;
        private List<int> _list;

        [GlobalSetup]
        public void Setup()
        {
            _deque = new Deque<int>(Count);
            _linked = new LinkedList<int>();
            _list = new List<int>(Count);
            _queue = new Queue<int>(Count * 2);

            for (var i = 0; i < Count; i++)
            {
                _deque.AddLast(i);
                _linked.AddLast(i);
                _list.Add(i);
                _queue.Enqueue(i);
            }
        }

        // ------------------------------------------------------------------
        // End operations: add at one end, remove at the other
        // ------------------------------------------------------------------

        [Benchmark(Baseline = true)]
        public long Deque_Rotate()
        {
            var total = 0L;
            for (var i = 0; i < Count; i++)
            {
                _deque.AddLast(i);
                total += _deque.RemoveFirst();
            }

            return total;
        }

        [Benchmark]
        public long LinkedList_Rotate()
        {
            var total = 0L;
            for (var i = 0; i < Count; i++)
            {
                _linked.AddLast(i);
                total += _linked.First.Value;
                _linked.RemoveFirst();
            }

            return total;
        }

        [Benchmark]
        public long Queue_Rotate()
        {
            var total = 0L;
            for (var i = 0; i < Count; i++)
            {
                _queue.Enqueue(i);
                total += _queue.Dequeue();
            }

            return total;
        }

        [Benchmark]
        public long Deque_ReverseRotate()
        {
            // The same work from the other end: add at the front, remove from the back. A ring
            // buffer has no reason to be asymmetric, and this is where an off-by-one in the
            // decrementing head would show up as a cost rather than as a wrong answer.
            var total = 0L;
            for (var i = 0; i < Count; i++)
            {
                _deque.AddFirst(i);
                total += _deque.RemoveLast();
            }

            return total;
        }

        // ------------------------------------------------------------------
        // Build: the growth path
        // ------------------------------------------------------------------

        [Benchmark]
        public Deque<int> Deque_BuildByAddLast()
        {
            var deque = new Deque<int>();
            for (var i = 0; i < Count; i++)
            {
                deque.AddLast(i);
            }

            return deque;
        }

        [Benchmark]
        public Deque<int> Deque_BuildByAddFirst()
        {
            var deque = new Deque<int>();
            for (var i = 0; i < Count; i++)
            {
                deque.AddFirst(i);
            }

            return deque;
        }

        [Benchmark]
        public LinkedList<int> LinkedList_BuildByAddLast()
        {
            var linked = new LinkedList<int>();
            for (var i = 0; i < Count; i++)
            {
                linked.AddLast(i);
            }

            return linked;
        }

        [Benchmark]
        public List<int> List_BuildByAdd()
        {
            var list = new List<int>();
            for (var i = 0; i < Count; i++)
            {
                list.Add(i);
            }

            return list;
        }

        // ------------------------------------------------------------------
        // Positional reads and enumeration
        // ------------------------------------------------------------------

        [Benchmark]
        public long Deque_IndexRead()
        {
            var total = 0L;
            for (var i = 0; i < _deque.Count; i++)
            {
                total += _deque[i];
            }

            return total;
        }

        [Benchmark]
        public long List_IndexRead()
        {
            var total = 0L;
            for (var i = 0; i < _list.Count; i++)
            {
                total += _list[i];
            }

            return total;
        }

        [Benchmark]
        public long Deque_Enumerate()
        {
            var total = 0L;
            foreach (var item in _deque)
            {
                total += item;
            }

            return total;
        }
    }
}
