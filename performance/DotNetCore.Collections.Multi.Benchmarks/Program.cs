using BenchmarkDotNet.Running;

namespace DotNetCore.Collections.Multi.Benchmarks {
    internal static class Program {
        private static void Main(string[] args) {
            // Run all benchmarks:  dotnet run -c Release -- --filter *
            // Direct-path only:    dotnet run -c Release -- --filter *PackedBag*
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
