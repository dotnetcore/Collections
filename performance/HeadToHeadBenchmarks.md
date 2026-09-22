# Head-to-head ordered-collection benchmarks

`ThirdPartyOrderedCollectionBenchmarks.cs` puts the ordered types of this repository side by side
with a reference implementation of the same shapes, in one process, so the comparison in the
internal working notes has numbers instead of complexity arguments.

This file is the **desensitized summary**: it reports this repository's own absolute numbers and the
ratios, and it does not name the library it is measured against. The named report, the equivalence
checks and the per-scenario analysis live in the internal working notes, which are not version
controlled.

```
dotnet run -c Release --project performance/DotNetCore.Collections.Multi.Benchmarks -- --filter "*HeadToHead*"
```

## What is measured, and what pairs with what

| Axis | Collections arm | Reference arm | Like-for-like? |
| --- | --- | --- | --- |
| bag | `OrderedMultiList<T>` | a sorted multiset | **yes** — both count occurrences, expand duplicates on enumeration, and answer position and rank by occurrence. Range bounds are two-sided inclusive on both sides, verified before any timing was read. |
| set | `OrderedMultiList<T>` holding one copy per element | a sorted set | **no** — this repository has no ranked set type, so the nearest equivalent is the ordered multiset. By content the two agree and every operation measured is equivalent, but the Collections arm still carries the copy-count machinery. |
| map | `OrderedMultiDictionary<TKey, TValue>` | an ordered multimap | **yes** structurally (one key to many ordered values), with one counting difference to keep in mind: here `Count` / `KeyCount` are keys and `TotalValueCount` is values, while the reference counts values in `Count`. The lookup arms therefore use `ContainsKey` plus the per-key count on both sides. |

**Not measured, stated rather than hidden:** positional operations on the map axis (there is no
positional surface on `OrderedMultiDictionary` at all); range-by-index on the bag and set axes; live
sub-views bound to a key range; and set algebra, whose cost is dominated by the input enumeration
rather than by either engine.

## Environment and fairness

```
BenchmarkDotNet v0.14.0, Windows 11 (10.0.28120.3002)
.NET SDK 10.0.400   .NET 8.0.30, X64 RyuJIT AVX2   GC = Concurrent Server
ShortRun (LaunchCount=1, WarmupCount=3, IterationCount=3) + MemoryDiagnoser
```

Both arms run in the same process, on the same runtime, with the same GC mode, the same key
sequence, the same sizes and the same default comparer. Mutating benchmarks reset through
`[IterationSetup]`; read-only ones fill once in `[GlobalSetup]`. Every arm returns an accumulator so
nothing can be optimised away. The reference package carries `net35` / `net40` / `net45` /
`netstandard1.0` assets and this `net8.0` harness resolves the `netstandard1.0` one, so the numbers
describe a .NET Standard 1.0 build of the reference rather than a framework-specific one.

Two caveats on reading the tables. There is no byte-identical control arm to subtract machine drift
from, so **ratios close to 1.0 are reported as "level", not as a win**. And BenchmarkDotNet could
not identify the processor on this machine, so the absolute figures need the machine spec alongside
them before they can be compared across machines.

## Bag axis — `OrderedMultiList<T>`, three copies per key

| Scenario | Size | Mean | Allocated | vs reference |
| --- | --- | --- | --- | --- |
| `Add` | 64 keys | 283.9 ns | 1 B | **2.03x faster** |
| `Add` | 4096 keys | 486.9 ns | 14 B | **1.38x faster** |
| `Contains` + `CountOf` | 64 keys | 42.51 ns | 0 B | **2.36x faster** |
| `Contains` + `CountOf` | 4096 keys | 121.04 ns | 0 B | **2.79x faster** |
| iteration (12,288 copies) | 4096 keys | 92.98 µs | 112 B | **2.73x slower** |
| iteration (196,608 copies) | 65,536 keys | 1,514.12 µs | 112 B | **3.08x slower** |
| 512-key-wide range | 65,536 keys | 13.134 µs | 152 B | **2.63x slower** |
| `GetRank` | 4096 keys | 59.11 ns | 0 B | **1.50x faster** |
| `GetRank` | 65,536 keys | 78.66 ns | 0 B | **1.80x faster** |
| `GetByRank` | 4096 keys | 28.56 ns | 0 B | **3.38x faster** |
| `GetByRank` | 65,536 keys | 29.85 ns | 0 B | **3.99x faster** |
| `RemoveAllCopies` (1024 calls) | 4096 keys | 1.214 µs | 0 B | **1.44x faster** |
| `RemoveAt` until empty (12,288 removals) | 4096 keys | 7.614 ms | 64 B | **2.41x faster** |

Point operations lead, rank reads lead by the widest margin and allocate nothing, and positional
removal is clean in allocation terms — the reference allocates about 208 B per positional removal
(2.56 MB across the run) against 64 B here. Iteration and range enumeration are the other way:
2.6–3.1x behind, which is the cache-locality dividend of a wide B+ node, and the engine work in this
release did not close that gap.

## Set axis — `OrderedMultiList<T>` with one copy per key

| Scenario | Size | Mean | vs reference |
| --- | --- | --- | --- |
| `Add` | 64 keys | 340.1 ns | level (1.06x) |
| `Add` | 4096 keys | 769.1 ns | **2.06x slower** |
| iteration | 4096 keys | 54.865 µs | **6.18x slower** |
| iteration | 65,536 keys | 876.770 µs | **6.04x slower** |
| `GetRank` | 4096 keys | 46.08 ns | level (1.09x) |
| `GetRank` | 65,536 keys | 63.76 ns | **1.58x faster** |
| `GetByRank` | 4096 keys | 28.16 ns | level (1.00x) |
| `GetByRank` | 65,536 keys | 37.93 ns | **3.14x faster** |

At small sizes the two are level on rank reads — one descent either way — and at 65,536 keys the
Collections arm is ahead. The copy-count machinery itself is not what costs anything here; the 4096
`Add` gap is a node-layout effect.

## Map axis — `OrderedMultiDictionary<TKey, TValue>`, two values per key

| Scenario | Size | Mean | Allocated | vs reference |
| --- | --- | --- | --- | --- |
| `Add` | 64 keys | 492.5 ns | 93 B | **1.27x faster** |
| `Add` | 4096 keys | 1,097.1 ns | 656 B | **1.37x slower** |
| `ContainsKey` + `ValueCount` | 64 keys | 54.42 ns | 0 B | **1.95x faster** |
| `ContainsKey` + `ValueCount` | 4096 keys | 152.68 ns | 0 B | **1.77x faster** |
| iteration (8,192 pairs) | 4096 keys | 375.20 µs | **459,104 B** | **17.3x slower** |
| iteration (131,072 pairs) | 65,536 keys | 7,494.35 µs | **7,340,449 B** | **20.2x slower** |

### The iteration allocation is a real defect, not just a slower path

`OrderedMultiDictionary<TKey, TValue>.GetEnumerator()` walks a
`SortedDictionary<TKey, ICollection<TValue>>` and `foreach`es each inner collection **through the
`ICollection<TValue>` interface**. The inner collection is an `OrderedMultiList<TValue>`, whose
`GetEnumerator()` is a `yield` iterator that in turn enumerates the tree's `yield` iterator — so
**each key's inner enumeration allocates two iterator objects**.

The measurement matches that exactly: 459,104 B over 4096 keys and 7,340,449 B over 65,536 keys are
both **≈112 B per key**, i.e. a fixed per-key allocation rather than a growing one. The timing agrees
— 7,494 µs over 65,536 keys is ≈114 ns per key, far above the cost of one tree descent.

This is not fixed here: F6-26 is a measurement item, and changing the enumerator is a structural
change to a type whose storage layer is also what a future positional/rank surface would have to
touch. It is logged as a follow-up to be handled together with that work rather than twice
separately.

## Reproducing

```
# every head-to-head scenario
dotnet run -c Release --project performance/DotNetCore.Collections.Multi.Benchmarks -- --filter "*HeadToHead*"

# one axis
dotnet run -c Release --project performance/DotNetCore.Collections.Multi.Benchmarks -- --filter "*HeadToHead_Bag*"
```

BenchmarkDotNet writes `csv` / `html` / `github.md` reports into `BenchmarkDotNet.Artifacts/`, which
is not version controlled.
