using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using DotNetCore.Collections.Multi;

namespace DotNetCore.Collections.Multi.Benchmarks
{
    /// <summary>
    /// F7 evidence: <see cref="SpanBag{T}"/> (stack-only, borrowed stackalloc storage) against
    /// the heap-allocated alternatives on the type's motivating workload - a short-lived counting
    /// round inside one method. The MemoryDiagnoser column is the point: the whole scenario is
    /// garbage-free for the span bag, while every heap alternative allocates its structures
    /// <em>per call</em>.
    /// </summary>
    [ShortRunJob]
    [MemoryDiagnoser]
    public class SpanBagVsHeap_ShortLivedCounting
    {
        private const int Distinct = 8;

        private int[] _data;

        [GlobalSetup]
        public void Setup()
        {
            // 64 reads over an 8-distinct domain - a dense, method-local counting round.
            _data = new int[64];
            for (var i = 0; i < _data.Length; i++)
            {
                _data[i] = (i * 7 + 3) % Distinct;
            }
        }

        [Benchmark(Baseline = true)]
        public int SpanBag()
        {
            Span<int> values = stackalloc int[Distinct];
            Span<int> counts = stackalloc int[Distinct];
            var bag = new SpanBag<int>(values, counts);

            foreach (var item in _data)
            {
                bag.Add(item);
            }

            var total = 0;
            foreach (var entry in bag)
            {
                total += entry.Value * entry.Count;
            }

            return total;
        }

        [Benchmark]
        public int Dictionary()
        {
            var counts = new Dictionary<int, int>(Distinct);

            foreach (var item in _data)
            {
                counts[item] = counts.TryGetValue(item, out var current) ? current + 1 : 1;
            }

            var total = 0;
            foreach (var pair in counts)
            {
                total += pair.Key * pair.Value;
            }

            return total;
        }

        [Benchmark]
        public int PackedBag()
        {
            var bag = new PackedBag<int>();

            foreach (var item in _data)
            {
                bag.Add(item);
            }

            var total = 0;
            foreach (var (item, count) in bag.EntrySet())
            {
                total += item * count;
            }

            return total;
        }
    }

    /// <summary>
    /// F6-07 evidence for the first-duplicate scan, the canonical stack-bag use: find the first
    /// value that appears twice in a method-local sequence.
    /// </summary>
    [ShortRunJob]
    [MemoryDiagnoser]
    public class SpanBagVsHashSet_FirstDuplicate
    {
        private int[] _data;

        [GlobalSetup]
        public void Setup()
        {
            // 16 distinct values, the duplicate appears about halfway through.
            _data = new int[32];
            for (var i = 0; i < _data.Length; i++)
            {
                _data[i] = i < 16 ? i : i - 8;
            }
        }

        [Benchmark(Baseline = true)]
        public int SpanBag()
        {
            Span<int> values = stackalloc int[16];
            Span<int> counts = stackalloc int[16];
            var seen = new SpanBag<int>(values, counts);

            foreach (var item in _data)
            {
                if (seen.CountOf(item) > 0)
                {
                    return item;
                }

                seen.Add(item);
            }

            return -1;
        }

        [Benchmark]
        public int HashSet()
        {
            var seen = new HashSet<int>(16);

            foreach (var item in _data)
            {
                if (!seen.Add(item))
                {
                    return item;
                }
            }

            return -1;
        }
    }
}
