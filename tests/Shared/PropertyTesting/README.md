# Property-testing kit

Shared source for the property-based tests of the `Multi` collections. The same files are linked
into two test projects, so `net8.0` and `net462` run identical properties against identical code:

- `tests/DotNetCore.Collections.Multi.Tests` (`net8.0`)
- `tests/DotNetCore.Collections.Multi.NetFxTests` (`net462`)

There is no project and no package here on purpose. A shared project would need its own TFM
negotiation and a package reference would put a test-only dependency into the dependency graph the
shipped packages resolve; a `Compile` glob does neither.

## Why the sources are C# 7.3

A `net462` project compiles at the default language version for a .NET Framework target, which is
C# 7.3. Every file here therefore avoids C# 8 and later syntax (switch expressions, `using`
declarations, nullable reference annotations) and avoids named tuples, so `System.ValueTuple` never
enters the graph. This is also why the adapters read distinct items through `DistinctItems()` plus
`CountOf()` instead of `EntrySet()`, which returns a tuple.

## Layout

| Path | Contents |
|---|---|
| `DeterministicRandom.cs` | Seeded random source; the seed is echoed in every failure report |
| `StatefulProperty.cs` | Contract for a stateful machine: produce an operation, apply it, check an invariant, reset |
| `PropertyRunner.cs` | Drives the machine; on failure shrinks the sequence to a minimal reproducer |
| `PropertyResult.cs` | Outcome plus the diagnostic report (seed, minimal steps, violation) |
| `MultisetModel.cs`, `MultimapModel.cs` | Naive reference implementations used as the oracle |
| `IBagUnderTest.cs`, `IMultimapUnderTest.cs` | Adapter contracts; take an instance and return the instance to use afterwards |
| `BagModelEquivalenceProperty.cs`, `MultimapModelEquivalenceProperty.cs` | The properties themselves |
| `Adapters/` | Adapters for the concrete collections |
| `Tests/` | The xunit suites that run the properties |

## Adding a collection to the suite

1. Implement `IBagUnderTest<TBag,T>` (or `IMultimapUnderTest<TMap,TKey,TValue>`). For a mutable
   type, mutate and return the same instance; for a persistent type, return the new instance. The
   property never needs to know which it is talking to.
2. Add a `[Fact]` that builds the property with a finite domain and calls
   `PropertyRunner.RunSeeds(property, seed, runCount, stepCount)`.
3. Assert `result.Success.ShouldBeTrue(result.ToDiagnosticString())`. The diagnostic string carries
   everything needed to reproduce a failure, so it should always be the assertion message.

Keep the domain finite and exhaustive: the invariant checks every element of the domain on every
step, which is what makes it strict without having to guess which elements exist.

## Reproducing a failure

A failure prints its seed and a minimised operation sequence. Re-run the same property with that
seed and the same step count; the run is fully deterministic, so the same sequence appears again.
If a later change makes the seed stop failing, the printed sequence is still the input to debug
against - replay it by hand through the adapter.

## What this kit is not for

It verifies semantics, not speed. Allocation and throughput assertions belong to
BenchmarkDotNet, which this repository already uses for its cross-version comparison work.
