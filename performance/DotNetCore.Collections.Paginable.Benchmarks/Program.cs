using System;
using System.Linq;
using BenchmarkDotNet.Running;

namespace DotNetCore.Collections.Paginable.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<PaginableBenchmark>();
        }
    }
}