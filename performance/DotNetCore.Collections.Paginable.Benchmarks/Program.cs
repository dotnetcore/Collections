using BenchmarkDotNet.Running;

namespace DotNetCore.Collections.Paginable.Benchmarks {
    internal static class Program {
        private static void Main(string[] args) {
            // Run all benchmarks:  dotnet run -c Release -- --filter *
            // Direct-path only:    dotnet run -c Release -- --filter *GetPage_List*
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
