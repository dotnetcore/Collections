# Head-to-head multimap benchmarks

`ThirdPartyMultiMapBenchmarks.cs` puts this repository's unordered multimap next to a reference
implementation of the same call shape, in one process, on one runtime, so the comparison in the
internal working notes has numbers instead of arguments.

This file is the **desensitized summary**: it reports this repository's own absolute numbers and the
ratios, and it does not name the library it is measured against. The named report, the equivalence
probes and the per-scenario analysis live in the internal working notes, which are not version
controlled.

**This axis is a loss, and it is recorded as one.** On the two operations the two sides share, this
repository's type is about 2–3x slower, and at the small size it allocates about 7x more per call.
The gap is not marginal and it is not explained away below.

```
# the project has to be built for the Framework target first - the reference ships a net461 asset only
dotnet build performance/DotNetCore.Collections.Multi.Benchmarks -c Release -f net461
./performance/DotNetCore.Collections.Multi.Benchmarks/bin/Release/net461/DotNetCore.Collections.Multi.Benchmarks.exe --filter "*HeadToHead_MultiMap*"
```

## What is measured, and what pairs with what

The reference's published surface is three public types, of which exactly one overlaps with this
repository: an unordered multimap. There is therefore one axis here, and the two sides do not agree
on it:

| Axis | Collections arm | Reference arm | Like-for-like? |
| --- | --- | --- | --- |
| add | `MultiDictionary<TKey, TValue>.Add(key, value)` | `Add(key, value)` | **no** — multiplicity differs. This repository adds one occurrence per call; the reference adds one on a key's *first* `Add` and **two** on every later one. The same call sequence therefore leaves the two arms holding different amounts of data, so the ratio is a ratio between different amounts of work. |
| remove | `MultiDictionary<TKey, TValue>.Remove(key, value)`, returns `bool` | `Remove(key, value)`, returns `void` | **no** — the absent-pair contract differs. This repository returns `false` when the pair is not there; the reference **throws `NullReferenceException`**. The arm is written so that neither side is ever asked to remove a pair twice. |
| read | `ContainsKey` / `ValueCount` / `this[key]` | single-value and per-key reads only | **no common surface at all** — the reference has no key count, no per-key count and no enumerator, so nothing on the read side pairs up. |

## Environment and fairness

```
BenchmarkDotNet v0.14.0, Windows 11 (10.0.28120.3002)
12th Gen Intel Core i7-1260P, 1 CPU, 16 logical and 12 physical cores
  [Host]   : .NET Framework 4.8.1 (4.8.9222.0), X64 RyuJIT VectorSize=256
  ShortRun : .NET Framework 4.8.1 (4.8.9222.0), X64 RyuJIT VectorSize=256
ShortRun (LaunchCount=1, WarmupCount=3, IterationCount=3) + MemoryDiagnoser
```

Both arms run in the same process, on the same runtime, with the same GC mode, the same key
sequence, the same sizes and the same default comparer. The mutating benchmarks reset through
`[IterationSetup]`, and every arm either returns an accumulator or performs an observable removal,
so nothing can be optimised away.

Three things to hold in mind while reading the tables:

- **These numbers are not comparable with `HeadToHeadBenchmarks.md`.** That file measures on
  `net8.0`; this one can only run on the .NET Framework, because the reference assembly it loads
  exists for `net461` alone. Different runtime, different JIT, different GC — the two files'
  absolute figures must not be put in the same column.
- **Iteration times here are 20–90 µs**, well below BenchmarkDotNet's recommended 100 ms floor, so
  the per-operation figures are indicative rather than precise. The direction and the rough size of
  the gap are what the run supports.
- There is no byte-identical control arm to subtract machine drift from, so **a ratio close to 1.0
  would be reported as "level", not as a win**. Nothing on this axis is close to 1.0.

## Add — `Add(key, value)`, existing-key path

| Method | Size | Mean | Allocated | vs reference |
| --- | --- | --- | --- | --- |
| Collections | 64 keys | 74.51 ns | 277 B | **3.1x slower** |
| Reference | 64 keys | 23.99 ns | 40 B | — |
| Collections | 4096 keys | 59.67 ns | 0 B | **2.8x slower** |
| Reference | 4096 keys | 21.32 ns | 0 B | — |

## Remove — `Remove(key, value)`

| Method | Size | Mean | Allocated | vs reference |
| --- | --- | --- | --- | --- |
| Collections | 1024 keys | 80.24 ns | 0 B | **2.0x slower** |
| Reference | 1024 keys | 40.53 ns | 0 B | — |
| Collections | 4096 keys | 79.10 ns | 0 B | **2.0x slower** |
| Reference | 4096 keys | 39.71 ns | 0 B | — |

## Reading the numbers

`vs reference` is mean over mean. BenchmarkDotNet's own `Ratio` column — the reference measured
against the `Collections` baseline — reads 0.32 / 0.39 / 0.51 / 0.50 for the same four rows, which
is the same finding expressed the other way round.

**The Add gap is understated rather than overstated.** The reference performs *two* insertions on
every call after the first for a key, and this repository performs one, so per insertion the
reference is roughly twice as far ahead again as the per-call ratio suggests. The arm compares the
cost of a call against a side doing more work per call — and the side doing more work still wins.

**The allocation column is the other half of the story.** At 64 keys the Collections arm allocates
277 B per call against 40 B, roughly 7x, while at 4096 keys both sides are effectively
allocation-free. The small-size allocation is consistent with inner storage that grows as repeated
adds land on the same key: at 4096 keys each key is touched once and nothing grows. It is a
per-key-count effect, not a constant overhead.

**Reproducibility.** The same code was run twice; only the benchmark class names differed between
the runs. The four mean-over-mean ratios were 2.73x / 2.12x / 1.99x / 2.05x on the first run
against 3.11x / 2.80x / 1.98x / 1.99x on the second, in table order. The two `Remove` rows
reproduce to within 0.01x; the `Add` rows move more, and `Add` at 4096 keys is the least stable of
the four cells — it is the one to re-measure before quoting.

## Not comparable, not measured

Stated rather than left blank, so that nothing above reads as more than it is.

| Item | Status | Why, or how to close it |
| --- | --- | --- |
| the read side (key lookup, per-key count, iteration) | **not comparable** | the reference has no such surface at all, so there is nothing to pair up. This is not a gap in the measurement |
| `Add` as a like-for-like operation | **not comparable** | the multiplicity difference set out above. The table gives a conservative per-*call* figure; per *insertion* the reference is about twice as far ahead again. A truly like-for-like arm would have to make this repository insert two copies on every second call, which changes the operation being measured, so it was not written |
| the reference's behaviour on an absent pair | **not measurable** | it throws, so there is no stable path to time. The arm removes only present pairs instead |
| the cause of the 64-key allocation gap | **not located in code** | the effect is consistent with inner storage that grows as repeated adds land on one key, but the reference's storage layer was not read to confirm it. Closing this means decompiling that layer and checking its growth policy |
| sizes above 4096 keys | **not measured** | the remove arm scales safely to any size at or above the loop length; the add arm has no contract limit at all |
| a second machine, and a longer job | **not done** | this is one machine under `ShortRun`, with iteration times of 20–90 µs against a recommended floor of 100 ms. The absolute figures need a longer job before they are quoted anywhere |

One sentence to carry along with any use of these numbers: **the four cells say what each side costs
for its own behaviour, not how two implementations of the same operation compare.**

## Reproducing

```
# build the Framework target (building both targets also regression-checks net8.0)
dotnet build performance/DotNetCore.Collections.Multi.Benchmarks -c Release

# start the net461 host directly - do NOT use `dotnet run --`, because the MSBuild
# serialisation switches would land in BenchmarkDotNet's own argument list
./performance/DotNetCore.Collections.Multi.Benchmarks/bin/Release/net461/DotNetCore.Collections.Multi.Benchmarks.exe --filter "*HeadToHead_MultiMap*"
```

Two environment problems will stop the run before it starts, and both are worth knowing about:

1. **Environment entries that differ only by case.** On .NET Framework,
   `ProcessStartInfo.EnvironmentVariables` is a case-insensitive dictionary, so BenchmarkDotNet
   fails to launch its child process — `System.ArgumentException: ... "HTTP_PROXY" ...
   "http_proxy"` — when the environment carries both spellings of one name. The usual offenders are
   the proxy variables. Keep one spelling of each and drop the other before starting the host.
2. **Missing Windows environment variables.** BenchmarkDotNet restores and compiles a project of its
   own, so running the executable without `SYSTEMROOT` / `WINDIR` / `PROGRAMFILES` / `APPDATA`
   present fails with `NuGet.targets(...): error : Value cannot be null. (Parameter 'path1')`.

BenchmarkDotNet writes `csv` / `html` / `github.md` reports into `BenchmarkDotNet.Artifacts/`, which
is not version controlled.
