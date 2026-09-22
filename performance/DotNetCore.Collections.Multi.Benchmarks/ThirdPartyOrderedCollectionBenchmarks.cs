using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using DotNetCore.Collections.Multi;
using Kaos.Collections;

namespace DotNetCore.Collections.Multi.Benchmarks
{
    // F6-26: the head-to-head sweep against the reference ordered-collection library, on the same
    // machine, in the same process, with the same BenchmarkDotNet settings as every other file in
    // this project ([ShortRunJob] + [MemoryDiagnoser], net8.0). The point is to replace the
    // complexity argument in the comparison report with numbers, in both directions - the sweep is
    // written to be able to report a loss.
    //
    // THREE AXES, AND WHAT PAIRS WITH WHAT
    //
    //  * bag        - OrderedMultiList<int>  <->  RankedBag<int>
    //                 The one clean like-for-like pair. Both are sorted multisets: Count is the
    //                 total number of occurrences, the enumerator expands duplicates, ElementAt /
    //                 IndexOf / ElementsBetween answer by occurrence (verified against
    //                 GetByRank / GetRank / GetRange before any timing was read, including the
    //                 two-sided inclusivity of the range). Add(T, count) and Remove(T) - which
    //                 drops every occurrence - line up as well.
    //
    //  * set        - OrderedMultiList<int> with exactly one copy per key  <->  RankedSet<int>
    //                 NOT a like-for-like pair, and the reason is itself a finding: Collections has
    //                 no ranked set type, so the nearest equivalent is the ordered multiset holding
    //                 one copy of each key. By content the two agree, and every operation measured
    //                 here is equivalent, but the Collections arm still carries the copy-count
    //                 machinery the set does not have. Read this axis as "what a caller reaching
    //                 for a ranked set gets today", not as a type-for-type comparison.
    //
    //  * map        - OrderedMultiDictionary<int, int>  <->  RankedMap<int, int>
    //                 Structurally equivalent: one key to many ordered values, enumerating key/value
    //                 pairs. One trap to keep in mind when reading the numbers: the two libraries
    //                 count differently. Collections' Count and KeyCount are the number of KEYS and
    //                 TotalValueCount is the number of values; the reference's Count is the number
    //                 of VALUES and Keys.GetCount is per-key. The lookup arms are therefore written
    //                 against ContainsKey + per-key count on both sides rather than against Count.
    //
    // WHAT IS DELIBERATELY NOT MEASURED (the non-comparable items, stated rather than hidden)
    //
    //  * Positional removal on the reference's map - RemoveAt(index) / RemoveRange(index, count) /
    //    ElementsBetweenIndexes - has no Collections counterpart: OrderedMultiDictionary was left
    //    untouched by the engine work, so it exposes no positional surface at all. Its rank reads
    //    are a separate piece of work, not a missing measurement here.
    //  * Range-by-index on the bag and set axes is in the same position, and so is the reference's
    //    live GetViewBetween-style sub-view.
    //  * Set algebra is not measured. Both sides have it and it is a bulk operation whose cost is
    //    dominated by the input enumeration, so it says little about the engines.
    //  * The reference package carries net35 / net40 / net45 / netstandard1.0 assets; this net8.0
    //    harness resolves the netstandard1.0 one. Numbers are therefore for the reference built for
    //    .NET Standard 1.0, not for a framework-specific build.
    //
    // FAIRNESS
    //
    // Both arms see the same key sequence, the same sizes, the same default comparer
    // (Comparer<int>.Default) and the same process. Mutating benchmarks reset through
    // [IterationSetup] so each iteration starts from the same state; read-only ones fill once in
    // [GlobalSetup]. Each arm returns an accumulator so nothing can be optimised away.

    // ------------------------------------------------------------------ bag axis

    [ShortRunJob]
    [MemoryDiagnoser]
    public class HeadToHead_Bag_Add
    {
        private const int Ops = 1024;

        [Params(64, 4096)]
        public int Distinct;

        private OrderedMultiList<int> _list;
        private RankedBag<int> _reference;

        [IterationSetup]
        public void Setup()
        {
            _list = new OrderedMultiList<int>();
            _reference = new RankedBag<int>();
            for (var i = 0; i < Distinct; i++)
            {
                _list.Add(i, 3);
                _reference.Add(i, 3);
            }
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = Ops)]
        public OrderedMultiList<int> Collections()
        {
            for (var i = 0; i < Ops; i++)
            {
                _list.Add((i * 7 + Distinct) % (Distinct * 2));
            }

            return _list;
        }

        [Benchmark(OperationsPerInvoke = Ops)]
        public RankedBag<int> Reference()
        {
            for (var i = 0; i < Ops; i++)
            {
                _reference.Add((i * 7 + Distinct) % (Distinct * 2));
            }

            return _reference;
        }
    }

    [ShortRunJob]
    [MemoryDiagnoser]
    public class HeadToHead_Bag_Contains
    {
        private const int Ops = 1024;

        [Params(64, 4096)]
        public int Distinct;

        private OrderedMultiList<int> _list;
        private RankedBag<int> _reference;

        [GlobalSetup]
        public void Setup()
        {
            _list = new OrderedMultiList<int>();
            _reference = new RankedBag<int>();
            for (var i = 0; i < Distinct; i++)
            {
                _list.Add(i, 3);
                _reference.Add(i, 3);
            }
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = Ops)]
        public long Collections()
        {
            var total = 0L;
            for (var i = 0; i < Ops; i++)
            {
                var key = i % Distinct;
                if (_list.Contains(key))
                {
                    total += _list.CountOf(key);
                }
            }

            return total;
        }

        [Benchmark(OperationsPerInvoke = Ops)]
        public long Reference()
        {
            var total = 0L;
            for (var i = 0; i < Ops; i++)
            {
                var key = i % Distinct;
                if (_reference.Contains(key))
                {
                    total += _reference.GetCount(key);
                }
            }

            return total;
        }
    }

    [ShortRunJob]
    [MemoryDiagnoser]
    public class HeadToHead_Bag_Iterate
    {
        [Params(4096, 65536)]
        public int Distinct;

        private OrderedMultiList<int> _list;
        private RankedBag<int> _reference;

        [GlobalSetup]
        public void Setup()
        {
            _list = new OrderedMultiList<int>();
            _reference = new RankedBag<int>();
            for (var i = 0; i < Distinct; i++)
            {
                _list.Add(i, 3);
                _reference.Add(i, 3);
            }
        }

        [Benchmark(Baseline = true)]
        public long Collections()
        {
            var total = 0L;
            foreach (var item in _list)
            {
                total += item;
            }

            return total;
        }

        [Benchmark]
        public long Reference()
        {
            var total = 0L;
            foreach (var item in _reference)
            {
                total += item;
            }

            return total;
        }
    }

    /// <summary>
    /// Positional reads are measured as a batch of 64, the same shape the F6-25 file uses, so the
    /// per-operation cost is the cost of the descent rather than of the benchmark loop.
    /// </summary>
    [ShortRunJob]
    [MemoryDiagnoser]
    public class HeadToHead_Bag_SelectByRank
    {
        private const int Batch = 64;

        [Params(4096, 65536)]
        public int Distinct;

        private OrderedMultiList<int> _list;
        private RankedBag<int> _reference;
        private int _total;

        [GlobalSetup]
        public void Setup()
        {
            _list = new OrderedMultiList<int>();
            _reference = new RankedBag<int>();
            for (var i = 0; i < Distinct; i++)
            {
                _list.Add(i, 3);
                _reference.Add(i, 3);
            }

            _total = _list.TotalCount;
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = Batch)]
        public long Collections()
        {
            var total = 0L;
            for (var i = 0; i < Batch; i++)
            {
                total += _list.GetByRank((i * 977) % _total);
            }

            return total;
        }

        [Benchmark(OperationsPerInvoke = Batch)]
        public long Reference()
        {
            var total = 0L;
            for (var i = 0; i < Batch; i++)
            {
                total += _reference.ElementAt((i * 977) % _total);
            }

            return total;
        }
    }

    [ShortRunJob]
    [MemoryDiagnoser]
    public class HeadToHead_Bag_RankOf
    {
        private const int Batch = 64;

        [Params(4096, 65536)]
        public int Distinct;

        private OrderedMultiList<int> _list;
        private RankedBag<int> _reference;

        [GlobalSetup]
        public void Setup()
        {
            _list = new OrderedMultiList<int>();
            _reference = new RankedBag<int>();
            for (var i = 0; i < Distinct; i++)
            {
                _list.Add(i, 3);
                _reference.Add(i, 3);
            }
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = Batch)]
        public long Collections()
        {
            var total = 0L;
            for (var i = 0; i < Batch; i++)
            {
                total += _list.GetRank((i * 977) % Distinct);
            }

            return total;
        }

        [Benchmark(OperationsPerInvoke = Batch)]
        public long Reference()
        {
            var total = 0L;
            for (var i = 0; i < Batch; i++)
            {
                total += _reference.IndexOf((i * 977) % Distinct);
            }

            return total;
        }
    }

    [ShortRunJob]
    [MemoryDiagnoser]
    public class HeadToHead_Bag_Range
    {
        private const int Distinct = 65536;

        private OrderedMultiList<int> _list;
        private RankedBag<int> _reference;

        [GlobalSetup]
        public void Setup()
        {
            _list = new OrderedMultiList<int>();
            _reference = new RankedBag<int>();
            for (var i = 0; i < Distinct; i++)
            {
                _list.Add(i, 3);
                _reference.Add(i, 3);
            }
        }

        [Benchmark(Baseline = true)]
        public long Collections()
        {
            var total = 0L;
            foreach (var item in _list.GetRange(Distinct / 3, Distinct / 3 + 512))
            {
                total += item;
            }

            return total;
        }

        [Benchmark]
        public long Reference()
        {
            var total = 0L;
            foreach (var item in _reference.ElementsBetween(Distinct / 3, Distinct / 3 + 512))
            {
                total += item;
            }

            return total;
        }
    }

    [ShortRunJob]
    [MemoryDiagnoser]
    public class HeadToHead_Bag_RemoveAllCopies
    {
        private const int Distinct = 4096;
        private const int Ops = 1024;

        private OrderedMultiList<int> _list;
        private RankedBag<int> _reference;

        [IterationSetup]
        public void Setup()
        {
            _list = new OrderedMultiList<int>();
            _reference = new RankedBag<int>();
            for (var i = 0; i < Distinct; i++)
            {
                _list.Add(i, 3);
                _reference.Add(i, 3);
            }
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = Ops)]
        public OrderedMultiList<int> Collections()
        {
            for (var i = 0; i < Ops; i++)
            {
                _list.RemoveAllCopies((i * 31 + Distinct) % Distinct);
            }

            return _list;
        }

        [Benchmark(OperationsPerInvoke = Ops)]
        public RankedBag<int> Reference()
        {
            for (var i = 0; i < Ops; i++)
            {
                _reference.Remove((i * 31 + Distinct) % Distinct);
            }

            return _reference;
        }
    }

    /// <summary>
    /// The positional removal that F6-27 added to the Collections arm. Both arms remove the copy at
    /// the middle rank and stop when the multiset empties, so the two loops perform identical work.
    /// </summary>
    [ShortRunJob]
    [MemoryDiagnoser]
    public class HeadToHead_Bag_RemoveAt
    {
        private const int Distinct = 4096;

        private OrderedMultiList<int> _list;
        private RankedBag<int> _reference;

        [IterationSetup]
        public void Setup()
        {
            _list = new OrderedMultiList<int>();
            _reference = new RankedBag<int>();
            for (var i = 0; i < Distinct; i++)
            {
                _list.Add(i, 3);
                _reference.Add(i, 3);
            }
        }

        [Benchmark(Baseline = true)]
        public int Collections()
        {
            var removed = 0;
            while (_list.TotalCount > 0)
            {
                _list.RemoveAt(_list.TotalCount / 2);
                removed++;
            }

            return removed;
        }

        [Benchmark]
        public int Reference()
        {
            var removed = 0;
            while (_reference.Count > 0)
            {
                _reference.RemoveAt(_reference.Count / 2);
                removed++;
            }

            return removed;
        }
    }

    // ------------------------------------------------------------------ set axis

    [ShortRunJob]
    [MemoryDiagnoser]
    public class HeadToHead_Set_Add
    {
        private const int Ops = 1024;

        [Params(64, 4096)]
        public int Distinct;

        private OrderedMultiList<int> _list;
        private RankedSet<int> _reference;

        [IterationSetup]
        public void Setup()
        {
            _list = new OrderedMultiList<int>();
            _reference = new RankedSet<int>();
            for (var i = 0; i < Distinct; i++)
            {
                _list.Add(i);
                _reference.Add(i);
            }
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = Ops)]
        public OrderedMultiList<int> Collections()
        {
            for (var i = 0; i < Ops; i++)
            {
                _list.Add((i * 7 + Distinct) % (Distinct * 2));
            }

            return _list;
        }

        [Benchmark(OperationsPerInvoke = Ops)]
        public RankedSet<int> Reference()
        {
            for (var i = 0; i < Ops; i++)
            {
                _reference.Add((i * 7 + Distinct) % (Distinct * 2));
            }

            return _reference;
        }
    }

    [ShortRunJob]
    [MemoryDiagnoser]
    public class HeadToHead_Set_Iterate
    {
        [Params(4096, 65536)]
        public int Distinct;

        private OrderedMultiList<int> _list;
        private RankedSet<int> _reference;

        [GlobalSetup]
        public void Setup()
        {
            _list = new OrderedMultiList<int>();
            _reference = new RankedSet<int>();
            for (var i = 0; i < Distinct; i++)
            {
                _list.Add(i);
                _reference.Add(i);
            }
        }

        [Benchmark(Baseline = true)]
        public long Collections()
        {
            var total = 0L;
            foreach (var item in _list)
            {
                total += item;
            }

            return total;
        }

        [Benchmark]
        public long Reference()
        {
            var total = 0L;
            foreach (var item in _reference)
            {
                total += item;
            }

            return total;
        }
    }

    [ShortRunJob]
    [MemoryDiagnoser]
    public class HeadToHead_Set_SelectByRank
    {
        private const int Batch = 64;

        [Params(4096, 65536)]
        public int Distinct;

        private OrderedMultiList<int> _list;
        private RankedSet<int> _reference;

        [GlobalSetup]
        public void Setup()
        {
            _list = new OrderedMultiList<int>();
            _reference = new RankedSet<int>();
            for (var i = 0; i < Distinct; i++)
            {
                _list.Add(i);
                _reference.Add(i);
            }
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = Batch)]
        public long Collections()
        {
            var total = 0L;
            for (var i = 0; i < Batch; i++)
            {
                total += _list.GetByRank((i * 977) % Distinct);
            }

            return total;
        }

        [Benchmark(OperationsPerInvoke = Batch)]
        public long Reference()
        {
            var total = 0L;
            for (var i = 0; i < Batch; i++)
            {
                total += _reference.ElementAt((i * 977) % Distinct);
            }

            return total;
        }
    }

    [ShortRunJob]
    [MemoryDiagnoser]
    public class HeadToHead_Set_RankOf
    {
        private const int Batch = 64;

        [Params(4096, 65536)]
        public int Distinct;

        private OrderedMultiList<int> _list;
        private RankedSet<int> _reference;

        [GlobalSetup]
        public void Setup()
        {
            _list = new OrderedMultiList<int>();
            _reference = new RankedSet<int>();
            for (var i = 0; i < Distinct; i++)
            {
                _list.Add(i);
                _reference.Add(i);
            }
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = Batch)]
        public long Collections()
        {
            var total = 0L;
            for (var i = 0; i < Batch; i++)
            {
                total += _list.GetRank((i * 977) % Distinct);
            }

            return total;
        }

        [Benchmark(OperationsPerInvoke = Batch)]
        public long Reference()
        {
            var total = 0L;
            for (var i = 0; i < Batch; i++)
            {
                total += _reference.IndexOf((i * 977) % Distinct);
            }

            return total;
        }
    }

    // ------------------------------------------------------------------ map axis

    [ShortRunJob]
    [MemoryDiagnoser]
    public class HeadToHead_Map_Add
    {
        private const int Ops = 1024;

        [Params(64, 4096)]
        public int Distinct;

        private OrderedMultiDictionary<int, int> _map;
        private RankedMap<int, int> _reference;

        [IterationSetup]
        public void Setup()
        {
            _map = new OrderedMultiDictionary<int, int>();
            _reference = new RankedMap<int, int>();
            for (var i = 0; i < Distinct; i++)
            {
                _map.Add(i, i);
                _map.Add(i, -i);
                _reference.Add(i, i);
                _reference.Add(i, -i);
            }
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = Ops)]
        public OrderedMultiDictionary<int, int> Collections()
        {
            for (var i = 0; i < Ops; i++)
            {
                var key = (i * 7 + Distinct) % (Distinct * 2);
                _map.Add(key, key);
            }

            return _map;
        }

        [Benchmark(OperationsPerInvoke = Ops)]
        public RankedMap<int, int> Reference()
        {
            for (var i = 0; i < Ops; i++)
            {
                var key = (i * 7 + Distinct) % (Distinct * 2);
                _reference.Add(key, key);
            }

            return _reference;
        }
    }

    /// <summary>
    /// ContainsKey followed by the per-key value count, on both sides. The reference's own Count is
    /// the number of values while Collections' Count is the number of keys, so neither is used here
    /// - the per-key count is the operation that means the same thing on both.
    /// </summary>
    [ShortRunJob]
    [MemoryDiagnoser]
    public class HeadToHead_Map_Lookup
    {
        private const int Ops = 1024;

        [Params(64, 4096)]
        public int Distinct;

        private OrderedMultiDictionary<int, int> _map;
        private RankedMap<int, int> _reference;

        [GlobalSetup]
        public void Setup()
        {
            _map = new OrderedMultiDictionary<int, int>();
            _reference = new RankedMap<int, int>();
            for (var i = 0; i < Distinct; i++)
            {
                _map.Add(i, i);
                _map.Add(i, -i);
                _reference.Add(i, i);
                _reference.Add(i, -i);
            }
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = Ops)]
        public long Collections()
        {
            var total = 0L;
            for (var i = 0; i < Ops; i++)
            {
                var key = i % Distinct;
                if (_map.ContainsKey(key))
                {
                    total += _map.ValueCount(key);
                }
            }

            return total;
        }

        [Benchmark(OperationsPerInvoke = Ops)]
        public long Reference()
        {
            var total = 0L;
            for (var i = 0; i < Ops; i++)
            {
                var key = i % Distinct;
                if (_reference.ContainsKey(key))
                {
                    total += _reference.Keys.GetCount(key);
                }
            }

            return total;
        }
    }

    [ShortRunJob]
    [MemoryDiagnoser]
    public class HeadToHead_Map_Iterate
    {
        [Params(4096, 65536)]
        public int Distinct;

        private OrderedMultiDictionary<int, int> _map;
        private RankedMap<int, int> _reference;

        [GlobalSetup]
        public void Setup()
        {
            _map = new OrderedMultiDictionary<int, int>();
            _reference = new RankedMap<int, int>();
            for (var i = 0; i < Distinct; i++)
            {
                _map.Add(i, i);
                _map.Add(i, -i);
                _reference.Add(i, i);
                _reference.Add(i, -i);
            }
        }

        [Benchmark(Baseline = true)]
        public long Collections()
        {
            var total = 0L;
            foreach (var pair in _map)
            {
                total += pair.Value;
            }

            return total;
        }

        [Benchmark]
        public long Reference()
        {
            var total = 0L;
            foreach (var pair in _reference)
            {
                total += pair.Value;
            }

            return total;
        }
    }
}
