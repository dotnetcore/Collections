﻿using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Columns;
using ABPaginableCollections = PaginableCollections;

namespace DotNetCore.Collections.Paginable.Benchmarks {
    [MaxColumn, MinColumn]
    public class PaginableBenchmark {
        private readonly IEnumerable<int> _list;

        public PaginableBenchmark() {
            _list = Enumerable.Range(0, 10000000);
        }

        [Benchmark]
        public int DotNetCoreCollectionEnumerable() {
            var paginable = _list.GetPage(15, 50);
            return paginable.TotalPageCount;
        }

        [Benchmark]
        public int PaginableCollectionEnumerable() {
            var paginable = ABPaginableCollections.EnumerableExtensions.ToPaginable(_list, 15, 50);
            return paginable.Count;
        }

        [Benchmark]
        public int DotNetCoreCollectionQueryable() {
            var paginable = _list.AsQueryable().GetPage(15, 50);
            return paginable.TotalPageCount;
        }

        [Benchmark]
        public int PaginableCollectionQueryable() {
            var paginable = ABPaginableCollections.PaginableExtensions.ToPaginable(_list.AsQueryable(), 15, 50);
            return paginable.Count;
        }

        //[Benchmark]
        public int DotNetCoreCollectionEnumerable_ToPaginable() {
            var paginable = _list.ToPaginable(50);
            var page = paginable.GetPage(15);
            return page.TotalPageCount;
        }

        //[Benchmark]
        public int DotNetCoreCollectionQueryable_ToPaginable() {
            var paginable = _list.AsQueryable().ToPaginable(50);
            var page = paginable.GetPage(15);
            return page.TotalPageCount;
        }
    }
}