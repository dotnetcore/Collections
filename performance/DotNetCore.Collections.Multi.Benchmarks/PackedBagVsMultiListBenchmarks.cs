using BenchmarkDotNet.Attributes;
using DotNetCore.Collections.Multi;

namespace DotNetCore.Collections.Multi.Benchmarks
{
    /// <summary>
    /// F6-06 evidence: <see cref="PackedBag{T}"/> (dense struct-entry array, no hash table, no
    /// boxing in storage) against <see cref="MultiList{T}"/> (Dictionary-backed counts) on the
    /// type's intended workload - a small-cardinality counting histogram. One class per scenario,
    /// because BenchmarkDotNet allows a single baseline per class. The point-operation classes
    /// sweep the distinct-value count to map the crossover where the O(1) dictionary lookup
    /// starts beating the linear entry scan.
    /// </summary>
    [ShortRunJob]
    [MemoryDiagnoser]
    public class PackedBagVsMultiList_Add
    {
        private const int Ops = 1024;

        [Params(4, 16, 64)]
        public int Distinct;

        private PackedBag<int> _packed;
        private MultiList<int> _multi;

        [GlobalSetup]
        public void Setup()
        {
            _packed = new PackedBag<int>();
            _multi = new MultiList<int>();
            for (var i = 0; i < Distinct; i++)
            {
                _packed.Add(i, 5);
                _multi.Add(i, 5);
            }
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = Ops)]
        public PackedBag<int> PackedBag()
        {
            for (var i = 0; i < Ops; i++)
            {
                _packed.Add(i % Distinct);
            }

            return _packed;
        }

        [Benchmark(OperationsPerInvoke = Ops)]
        public MultiList<int> MultiList()
        {
            for (var i = 0; i < Ops; i++)
            {
                _multi.Add(i % Distinct);
            }

            return _multi;
        }
    }

    [ShortRunJob]
    [MemoryDiagnoser]
    public class PackedBagVsMultiList_CountOf
    {
        private const int Ops = 1024;

        [Params(4, 16, 64)]
        public int Distinct;

        private PackedBag<int> _packed;
        private MultiList<int> _multi;

        [GlobalSetup]
        public void Setup()
        {
            _packed = new PackedBag<int>();
            _multi = new MultiList<int>();
            for (var i = 0; i < Distinct; i++)
            {
                _packed.Add(i, 5);
                _multi.Add(i, 5);
            }
        }

        [Benchmark(Baseline = true, OperationsPerInvoke = Ops)]
        public int PackedBag()
        {
            var total = 0;
            for (var i = 0; i < Ops; i++)
            {
                total += _packed.CountOf(i % Distinct);
            }

            return total;
        }

        [Benchmark(OperationsPerInvoke = Ops)]
        public int MultiList()
        {
            var total = 0;
            for (var i = 0; i < Ops; i++)
            {
                total += _multi.CountOf(i % Distinct);
            }

            return total;
        }
    }

    [ShortRunJob]
    [MemoryDiagnoser]
    public class PackedBagVsMultiList_EntrySet
    {
        private const int Distinct = 32;

        private PackedBag<int> _packed;
        private MultiList<int> _multi;

        [GlobalSetup]
        public void Setup()
        {
            _packed = new PackedBag<int>();
            _multi = new MultiList<int>();
            for (var i = 0; i < Distinct; i++)
            {
                _packed.Add(i, 5);
                _multi.Add(i, 5);
            }
        }

        [Benchmark(Baseline = true)]
        public int PackedBag()
        {
            var total = 0;
            for (var round = 0; round < 1000; round++)
            {
                foreach (var (item, count) in _packed.EntrySet())
                {
                    total += item * count;
                }
            }

            return total;
        }

        [Benchmark]
        public int MultiList()
        {
            var total = 0;
            for (var round = 0; round < 1000; round++)
            {
                foreach (var (item, count) in _multi.EntrySet())
                {
                    total += item * count;
                }
            }

            return total;
        }
    }

    [ShortRunJob]
    [MemoryDiagnoser]
    public class PackedBagVsMultiList_Build
    {
        private const int Distinct = 32;

        [Benchmark(Baseline = true)]
        public PackedBag<int> PackedBag()
        {
            var bag = new PackedBag<int>();
            for (var i = 0; i < Distinct; i++)
            {
                bag.Add(i, 4);
            }

            return bag;
        }

        [Benchmark]
        public MultiList<int> MultiList()
        {
            var bag = new MultiList<int>();
            for (var i = 0; i < Distinct; i++)
            {
                bag.Add(i, 4);
            }

            return bag;
        }
    }
}
