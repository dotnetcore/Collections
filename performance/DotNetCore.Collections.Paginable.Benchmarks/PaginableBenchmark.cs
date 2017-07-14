using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Columns;
using DotNetCore.Collections.Paginable;
using ABPaginableCollections = PaginableCollections;

namespace DotNetCore.Collections.Paginable.Benchmarks
{
    [MaxColumn, MinColumn]
    public class PaginableBenchmark
    {
        private readonly IEnumerable<int> _list;
        public PaginableBenchmark()
        {
            _list = Enumerable.Range(0, 10000000);
        }
        [Benchmark]
        public int DotNetCoreCollectionEnumerable()
        {
            var paginable = _list.GetPage(15, 50);
            return paginable.TotalPageCount;
        }

        [Benchmark]
        public int ABPaginableCollectionEnumerable()
        {
            var paginable = ABPaginableCollections.EnumerableExtensions.ToPaginable(_list, 15, 50);
            return paginable.Count;
        }

        [Benchmark]
        public int DotNetCoreCollectionQueryable()
        {
            var paginable = _list.AsQueryable().GetPage(15, 50);
            return paginable.TotalPageCount;
        }

        [Benchmark]
        public int ABPaginableCollectionQueryable()
        {
            var paginable = ABPaginableCollections.PaginableExtensions.ToPaginable(_list.AsQueryable(), 15, 50);
            return paginable.Count;
        }
    }
}