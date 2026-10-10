using BenchmarkDotNet.Running;

namespace DotNetCore.Collections.Benchmarks
{
    internal static class Program
    {
        private static void Main(string[] args) {
            // Run the whole suite:   <exe> --filter *
            // One family only:       <exe> --filter *WhereSelect*
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
