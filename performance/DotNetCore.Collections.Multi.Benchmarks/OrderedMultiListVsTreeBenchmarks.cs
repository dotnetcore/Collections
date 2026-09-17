using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using DotNetCore.Collections.Multi;

namespace DotNetCore.Collections.Multi.Benchmarks
{
    /// <summary>
    /// F6-25 evidence for the ordered multiset engine. One class per scenario, one baseline per
    /// class, the same layout the F6-06 and F6-08 files use.
    ///
    /// Two comparisons run side by side on purpose:
    /// <list type="bullet">
    /// <item>the ordered multiset against <see cref="SortedDictionary{TKey,TValue}"/>, which is the
    /// shape the engine used to have - one heap object per distinct key, reached by a pointer written
    /// at an unrelated moment - so the sweep and range classes are where the cache-locality claim of
    /// the B+ engine is read;</item>
    /// <item>the rank classes against the only thing the pre-F6-25 type could do, which was to give up
    /// and materialise. Rank and select were not merely slow there, they were absent.</item>
    /// </list>
    /// </summary>
    [ShortRunJob]
    [MemoryDiagnoser]
    public class OrderedMultiListVsTree_Add
    {
        private const int Ops = 1024;

        [Params(64, 4096)]
        public int Distinct;

        private OrderedMultiList<int> _list;
        private SortedDictionary<int, int> _tree;

        [IterationSetup]
        public void Setup()
        {
            _list = new OrderedMultiList<int>();
            _tree = new SortedDictionary<int, int>();
            for (var i = 0; i < Distinct; i++)
            {
                _list.Add(i, 3);
                _tree[i] = 3;
            }
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = Ops)]
        public OrderedMultiList<int> OrderedMultiList()
        {
            for (var i = 0; i < Ops; i++)
            {
                _list.Add((i * 7 + Distinct) % (Distinct * 2));
            }

            return _list;
        }

        [Benchmark(OperationsPerInvoke = Ops)]
        public SortedDictionary<int, int> SortedDictionary()
        {
            for (var i = 0; i < Ops; i++)
            {
                var key = (i * 7 + Distinct) % (Distinct * 2);
                _tree.TryGetValue(key, out var count);
                _tree[key] = count + 1;
            }

            return _tree;
        }
    }

    [ShortRunJob]
    [MemoryDiagnoser]
    public class OrderedMultiListVsTree_CountOf
    {
        private const int Ops = 1024;

        [Params(64, 4096)]
        public int Distinct;

        private OrderedMultiList<int> _list;
        private SortedDictionary<int, int> _tree;

        [GlobalSetup]
        public void Setup()
        {
            _list = new OrderedMultiList<int>();
            _tree = new SortedDictionary<int, int>();
            for (var i = 0; i < Distinct; i++)
            {
                _list.Add(i, 3);
                _tree[i] = 3;
            }
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = Ops)]
        public int OrderedMultiList()
        {
            var total = 0;
            for (var i = 0; i < Ops; i++)
            {
                total += _list.CountOf(i % Distinct);
            }

            return total;
        }

        [Benchmark(OperationsPerInvoke = Ops)]
        public int SortedDictionary()
        {
            var total = 0;
            for (var i = 0; i < Ops; i++)
            {
                _tree.TryGetValue(i % Distinct, out var count);
                total += count;
            }

            return total;
        }
    }

    [ShortRunJob]
    [MemoryDiagnoser]
    public class OrderedMultiListVsTree_Iteration
    {
        [Params(64, 4096, 65536)]
        public int Distinct;

        private OrderedMultiList<int> _list;
        private SortedDictionary<int, int> _tree;

        [GlobalSetup]
        public void Setup()
        {
            _list = new OrderedMultiList<int>();
            _tree = new SortedDictionary<int, int>();
            for (var i = 0; i < Distinct; i++)
            {
                _list.Add(i, 3);
                _tree[i] = 3;
            }
        }

        [Benchmark(Baseline = true)]
        public int OrderedMultiList()
        {
            var total = 0;
            foreach (var item in _list)
            {
                total += item;
            }

            return total;
        }

        [Benchmark]
        public int SortedDictionary()
        {
            var total = 0;
            foreach (var entry in _tree)
            {
                for (var copy = 0; copy < entry.Value; copy++)
                {
                    total += entry.Key;
                }
            }

            return total;
        }
    }

    [ShortRunJob]
    [MemoryDiagnoser]
    public class OrderedMultiListVsTree_Range
    {
        private const int Distinct = 65536;

        private OrderedMultiList<int> _list;
        private SortedDictionary<int, int> _tree;

        [GlobalSetup]
        public void Setup()
        {
            _list = new OrderedMultiList<int>();
            _tree = new SortedDictionary<int, int>();
            for (var i = 0; i < Distinct; i++)
            {
                _list.Add(i, 3);
                _tree[i] = 3;
            }
        }

        [Benchmark(Baseline = true)]
        public int OrderedMultiList()
        {
            var total = 0;
            foreach (var item in _list.GetRange(Distinct / 3, Distinct / 3 + 512))
            {
                total += item;
            }

            return total;
        }

        [Benchmark]
        public int SortedDictionary()
        {
            var low = Distinct / 3;
            var high = low + 512;
            var total = 0;
            foreach (var entry in _tree)
            {
                if (entry.Key < low || entry.Key > high)
                {
                    continue;
                }

                for (var copy = 0; copy < entry.Value; copy++)
                {
                    total += entry.Key;
                }
            }

            return total;
        }
    }

    [ShortRunJob]
    [MemoryDiagnoser]
    public class OrderedMultiListVsTree_Remove
    {
        private const int Distinct = 4096;
        private const int Ops = 1024;

        private OrderedMultiList<int> _list;
        private SortedDictionary<int, int> _tree;

        [IterationSetup]
        public void Setup()
        {
            _list = new OrderedMultiList<int>();
            _tree = new SortedDictionary<int, int>();
            for (var i = 0; i < Distinct; i++)
            {
                _list.Add(i, 3);
                _tree[i] = 3;
            }
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = Ops)]
        public OrderedMultiList<int> OrderedMultiList()
        {
            for (var i = 0; i < Ops; i++)
            {
                _list.RemoveAllCopies((i * 31 + Distinct) % Distinct);
            }

            return _list;
        }

        [Benchmark(OperationsPerInvoke = Ops)]
        public SortedDictionary<int, int> SortedDictionary()
        {
            for (var i = 0; i < Ops; i++)
            {
                _tree.Remove((i * 31 + Distinct) % Distinct);
            }

            return _tree;
        }
    }

    /// <summary>
    /// The positional reads are measured as a batch, because the alternative the type offered
    /// before was to materialise the whole expanded sequence first and read from that copy. A
    /// single read would hide the build cost entirely, so each operation answers 64 of them and
    /// the materialising arms pay for the build once per batch, which is their best case.
    /// </summary>
    [ShortRunJob]
    [MemoryDiagnoser]
    public class OrderedMultiListVsTree_Rank
    {
        private const int Batch = 64;

        [Params(64, 4096, 65536)]
        public int Distinct;

        private OrderedMultiList<int> _list;
        private int _total;

        [GlobalSetup]
        public void Setup()
        {
            _list = new OrderedMultiList<int>();
            for (var i = 0; i < Distinct; i++)
            {
                _list.Add(i, 3);
            }

            _total = _list.TotalCount;
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = Batch)]
        public int GetByRank()
        {
            var total = 0;
            for (var i = 0; i < Batch; i++)
            {
                total += _list.GetByRank((i * 977) % _total);
            }

            return total;
        }

        [Benchmark(OperationsPerInvoke = Batch)]
        public int MaterialisedSelect()
        {
            var expanded = _list.ToList();
            var total = 0;
            for (var i = 0; i < Batch; i++)
            {
                total += expanded[(i * 977) % expanded.Count];
            }

            return total;
        }

        [Benchmark(OperationsPerInvoke = Batch)]
        public int GetRank()
        {
            var total = 0;
            for (var i = 0; i < Batch; i++)
            {
                total += _list.GetRank((i * 977) % Distinct);
            }

            return total;
        }

        [Benchmark(OperationsPerInvoke = Batch)]
        public int MaterialisedRankOf()
        {
            var expanded = _list.ToList();
            var total = 0;
            for (var i = 0; i < Batch; i++)
            {
                total += expanded.IndexOf((i * 977) % Distinct);
            }

            return total;
        }

        [Benchmark(OperationsPerInvoke = Batch)]
        public int GetMedianAndQuantile()
        {
            var total = 0;
            for (var i = 0; i < Batch; i++)
            {
                total += _list.GetMedian();
                total += _list.GetQuantile((i % 100) / 100d);
            }

            return total;
        }

        [Benchmark(OperationsPerInvoke = Batch)]
        public int MaterialisedMedianAndQuantile()
        {
            var expanded = _list.ToList();
            var total = 0;
            for (var i = 0; i < Batch; i++)
            {
                total += expanded[(expanded.Count - 1) / 2];
                total += expanded[CeilQuantile(i % 100, expanded.Count)];
            }

            return total;
        }

        private static int CeilQuantile(int percent, int count)
        {
            var rank = (int)Math.Ceiling(percent / 100d * count) - 1;
            return rank < 0 ? 0 : rank;
        }
    }
}
