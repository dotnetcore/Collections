using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using Perfolizer.Horology;

namespace DotNetCore.Collections.Benchmarks
{
    /// <summary>
    /// The job the F7-15 operator matrix runs under: the same shape as <see cref="GateConfig"/> at a
    /// quarter of the measured time, because the matrix measures 200 arms rather than 60.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The matrix is not the Gate. The Gate asks "is the engine at least as fast as
    /// <c>System.Linq</c> on the same source", reads an answer per case, and therefore needs a
    /// long-enough measured block that drift averages out - that is what
    /// <see cref="GateConfig"/>'s 250 ms iteration is for. The matrix asks "how do the four source
    /// arms of one operator compare with each other, and how many bytes does each allocate", both of
    /// which are read per row and neither of which is compared against a reference arm, so a 100 ms
    /// iteration is enough and 200 arms stay inside the suite's wall-clock budget.
    /// </para>
    /// <para>
    /// <see cref="RunStrategy.Throughput"/> is explicit for the same reason as in
    /// <see cref="GateConfig"/>: an iteration-time target only means something under that strategy.
    /// </para>
    /// <para>
    /// The class is public with a public parameterless constructor because
    /// <see cref="ConfigAttribute"/> instantiates it by reflection.
    /// </para>
    /// </remarks>
    public sealed class MatrixConfig : ManualConfig
    {
        /// <summary>Builds the configuration.</summary>
        public MatrixConfig() {
            AddJob(Job.Default
                .WithStrategy(RunStrategy.Throughput)
                .WithWarmupCount(3)
                .WithIterationCount(5)
                .WithIterationTime(TimeInterval.FromMilliseconds(100)));
        }
    }
}
