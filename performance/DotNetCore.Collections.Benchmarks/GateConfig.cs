using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using Perfolizer.Horology;

[assembly: Config(typeof(DotNetCore.Collections.Benchmarks.GateConfig))]

namespace DotNetCore.Collections.Benchmarks
{
    /// <summary>
    /// The job every F7-04 benchmark runs under.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This replaces <c>Job.Default</c> on purpose, and the reason is measurement, not taste. The
    /// first pass of this suite ran on the default job; BenchmarkDotNet resolved it to roughly a
    /// hundred iterations of ~0.9 s each, so a single arm occupied its core for ~90 s and the two
    /// arms of a case sat ~90 s apart. On a shared machine the core's throughput drifts on that
    /// same timescale - across the first pass the <em>reference</em> arm of structurally identical
    /// iterators moved between 403 µs and 589 µs - and drift of that size lands straight in the
    /// ratio, which is the number the Gate is read from.
    /// </para>
    /// <para>
    /// The fix is to keep each arm's measured block short relative to the drift period. Pinning
    /// <see cref="JobExtensions.WithIterationTime"/> to 250 ms makes BenchmarkDotNet resolve a
    /// per-method invocation count that lands one iteration on the target (so a 458 µs filter op
    /// and a 2.4 ns count op each get the count they need - a single fixed invocation count would
    /// be three orders of magnitude wrong for one of them), which puts 30 iterations at ~7.5 s per
    /// arm. The two arms of a case then run within ~15 s of each other instead of ~180 s, and the
    /// drift that survives is measured directly by each class's control arm rather than assumed.
    /// </para>
    /// <para>
    /// <see cref="RunStrategy.Throughput"/> is set explicitly because an iteration-time target is
    /// only meaningful under that strategy.
    /// </para>
    /// <para>
    /// The class and its constructor are public because <see cref="ConfigAttribute"/> instantiates
    /// the config by reflection through its parameterless constructor.
    /// </para>
    /// </remarks>
    public sealed class GateConfig : ManualConfig
    {
        /// <summary>Builds the configuration.</summary>
        public GateConfig() {
            AddJob(Job.Default
                .WithStrategy(RunStrategy.Throughput)
                .WithWarmupCount(5)
                .WithIterationCount(30)
                .WithIterationTime(TimeInterval.FromMilliseconds(250)));
        }
    }
}
