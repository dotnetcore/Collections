using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using DotNetCore.Collections.Multi;

namespace DotNetCore.Collections.Multi.Benchmarks
{
    /// <summary>
    /// F6-08 evidence: <see cref="FrequencyPriorityBag{T}"/> (heap + frequency index, maintained
    /// incrementally) against the natural baseline - counting into a
    /// <see cref="MultiList{T}"/> and sorting its entry set on every query.
    /// </summary>
    [ShortRunJob]
    [MemoryDiagnoser]
    public class FrequencyPriorityBagVsSort_TopK
    {
        private const int K = 8;

        private int[] _data;

        [GlobalSetup]
        public void Setup()
        {
            _data = new int[512];
            for (var i = 0; i < _data.Length; i++)
            {
                _data[i] = (i * 13 + 5) % 32;
            }
        }

        [Benchmark(Baseline = true)]
        public List<int> FrequencyPriorityBag()
        {
            var bag = new FrequencyPriorityBag<int>();
            foreach (var item in _data)
            {
                bag.Add(item);
            }

            var topK = new List<int>(K);
            for (var i = 0; i < K; i++)
            {
                topK.Add(bag.PopMost());
            }

            return topK;
        }

        [Benchmark]
        public List<int> MultiListSort()
        {
            var bag = new MultiList<int>();
            foreach (var item in _data)
            {
                bag.Add(item);
            }

            return bag.EntrySet()
                .OrderByDescending(entry => entry.Count)
                .Take(K)
                .Select(entry => entry.Item)
                .ToList();
        }
    }

    /// <summary>
    /// F6-08 evidence for the workload the type exists for - the priority question asked
    /// <em>repeatedly</em> against live data. The incremental heap pays for itself only here:
    /// on a build-once-query-once workload (see the scenarios above) sorting a
    /// <see cref="MultiList{T}"/> once is cheaper, and that boundary is documented.
    /// </summary>
    [ShortRunJob]
    [MemoryDiagnoser]
    public class FrequencyPriorityBagVsSort_RepeatedTop1
    {
        private const int Queries = 64;

        private int[] _data;

        [GlobalSetup]
        public void Setup()
        {
            _data = new int[512];
            for (var i = 0; i < _data.Length; i++)
            {
                _data[i] = (i * 13 + 5) % 32;
            }
        }

        [Benchmark(Baseline = true)]
        public int FrequencyPriorityBag()
        {
            var bag = new FrequencyPriorityBag<int>();
            foreach (var item in _data)
            {
                bag.Add(item);
            }

            var checksum = 0;
            for (var q = 0; q < Queries; q++)
            {
                bag.TryPeekMost(out var most);
                checksum += most;
            }

            return checksum;
        }

        [Benchmark]
        public int MultiListSort()
        {
            var bag = new MultiList<int>();
            foreach (var item in _data)
            {
                bag.Add(item);
            }

            var checksum = 0;
            for (var q = 0; q < Queries; q++)
            {
                checksum += bag.EntrySet().OrderByDescending(entry => entry.Count).First().Item;
            }

            return checksum;
        }
    }

    /// <summary>
    /// F6-08 evidence for updates interleaved with queries: the heap absorbs each change
    /// incrementally, so every interleaved Top-1 query stays cheap; the baseline re-sorts on
    /// every query.
    /// </summary>
    [ShortRunJob]
    [MemoryDiagnoser]
    public class FrequencyPriorityBagVsSort_InterleavedChurn
    {
        private int[] _data;

        [GlobalSetup]
        public void Setup()
        {
            _data = new int[256];
            for (var i = 0; i < _data.Length; i++)
            {
                _data[i] = (i * 7 + 1) % 32;
            }
        }

        [Benchmark(Baseline = true)]
        public int FrequencyPriorityBag()
        {
            var bag = new FrequencyPriorityBag<int>();
            foreach (var item in _data)
            {
                bag.Add(item);
            }

            var checksum = 0;
            for (var round = 0; round < 16; round++)
            {
                for (var c = 0; c < 8; c++)
                {
                    var value = (round * 8 + c) % 32;
                    bag.Add(value);
                    bag.Remove(value);
                }

                bag.TryPeekMost(out var most);
                checksum += most;
            }

            return checksum;
        }

        [Benchmark]
        public int MultiListSort()
        {
            var bag = new MultiList<int>();
            foreach (var item in _data)
            {
                bag.Add(item);
            }

            var checksum = 0;
            for (var round = 0; round < 16; round++)
            {
                for (var c = 0; c < 8; c++)
                {
                    var value = (round * 8 + c) % 32;
                    bag.Add(value);
                    bag.Remove(value);
                }

                checksum += bag.EntrySet().OrderByDescending(entry => entry.Count).First().Item;
            }

            return checksum;
        }
    }
}
