# Sample.Engine.Polyfills

> **English** | [中文](README.zh-CN.md)

> **All nineteen polyfill operators in one run** — the engine fills the BCL's version gaps on low TFMs, and this sample proves the answers match the BCL verbatim (`DotNetCore.Collections`, 7.2.0).

## What this sample demonstrates

`7.2.0` added nineteen operators to the engine in two layers. The **thirteen P0 operators** are the ones the BCL added between .NET 6 and .NET 10 (`DistinctBy` / `ExceptBy` / `IntersectBy` / `UnionBy` / `MaxBy` / `MinBy` / `Chunk` / `Order` / `OrderDescending` / `CountBy` / `AggregateBy` / `LeftJoin` / `RightJoin`);
the **six P1 operator groups** are engine-only additions that no BCL version ships (`DistinctUntilChanged` / `Memoize` / `FallbackIfEmpty` / `AtLeast`+`Exactly`+`CountBetween` / `PartialSortBy` / `Choose`).

The sample walks all nineteen through nine sections and ends with a **per-operator comparison**: on `net8.0` the BCL already ships nine of them, so the engine's answer must match `System.Linq` verbatim — that is the contract the polyfill layer exists for, and a low-TFM consumer must not be able to tell the difference.
The program is assertion-based: any disagreement returns a non-zero exit code.

## When to use it

- You are on a low TFM (`net451` / `netstandard2.0`) and want `DistinctBy` / `Chunk` / `CountBy`, which only arrived in .NET 6+.
- You are writing a cross-TFM library and want one source file to compile and behave identically from `net451` to `net10.0`.
- You want to know which operators the BCL already has (so you do not rewrite them) and which ones are engine-only.
- Not for: running on `net10.0` only with no back-compat requirement — use the BCL directly and skip the engine.

## How to run

```bash
dotnet run --project sample/Sample.Engine.Polyfills
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | The keyed-set quartet: dedupe / exclude / intersect / merge by key | `DistinctBy` / `ExceptBy` / `IntersectBy` / `UnionBy` |
| 2 | Extremes return the element; each chunk is a fresh array | `MaxBy` / `MinBy` / `Chunk` |
| 3 | Keyless ordering, returning `IOrderedEnumerable<T>` so `ThenBy` still chains | `Order` / `OrderDescending` |
| 4 | Group-and-count in one step; shared seed vs seed factory | `CountBy` / `AggregateBy` |
| 5 | Outer joins keep one whole side; the unmatched side is `default` | `LeftJoin` / `RightJoin` |
| 6 | Remove only adjacent duplicates; cache the first enumeration | `DistinctUntilChanged` / `Memoize` |
| 7 | Empty-sequence fallback; all three count predicates short-circuit | `FallbackIfEmpty` / `AtLeast` / `Exactly` / `CountBetween` |
| 8 | Top-k in O(n log k); filter and project in one step | `PartialSortBy` / `Choose` |
| 9 | Per-operator comparison with the BCL; answers must match verbatim | the P0 operators above vs `System.Linq` |

## Key APIs

- `ToValueEnumerable()` — four entry points: `T[]` / `List<T>` / `IReadOnlyList<T>` / `IEnumerable<T>`, each returning its own value-type wrapper.
- `DistinctBy(keySelector[, comparer])` — keeps only the first element per key.
- `ExceptBy(keys, keySelector[, comparer])` — the second argument is a **key set**; the result is **deduplicated by key**.
- `IntersectBy(keys, keySelector[, comparer])` — same, and each key matches at most once (it is consumed with `Remove`).
- `UnionBy(second, keySelector[, comparer])` — the second argument is an **element sequence**; both sides dedupe by key, left side first.
- `MaxBy` / `MinBy(keySelector[, comparer])` — returns the element itself, not the key.
- `Chunk(size)` — every chunk is a freshly allocated array; the tail may be short and is not padded.
- `Order` / `OrderDescending([comparer])` — returns the BCL's `IOrderedEnumerable<T>`, so `ThenBy` still chains.
- `CountBy(keySelector[, comparer])` — yields `KeyValuePair<TKey,int>`.
- `AggregateBy(keySelector, seed, func[, comparer])` — every key shares one seed instance.
- `AggregateBy(keySelector, seedFactory, func[, comparer])` — each key gets its own accumulator.
- `LeftJoin` / `RightJoin(inner, outerKey, innerKey, resultSelector[, comparer])` — the unmatched side is `default`.
- `DistinctUntilChanged([comparer])` — drops only **adjacent** duplicates.
- `Memoize()` — the first enumeration is cached; the second never returns to the source.
- `FallbackIfEmpty(T)` / `FallbackIfEmpty(IEnumerable<T>)` — substitutes only when empty.
- `AtLeast(n)` / `Exactly(n)` / `CountBetween(min, max)` — short-circuiting count predicates with inclusive bounds.
- `PartialSortBy(count, keySelector[, descending])` — returns a materialised `List<T>`.
- `Choose(chooser)` — `chooser` returns `(bool Keep, TResult Value)`; a false Keep neither yields nor projects.

## Notes and pitfalls

- **`ExceptBy` / `IntersectBy` deduplicate by key**, they are not "keep every matching element". When one key appears twice the second element never comes out — counter-intuitive, but exactly what the BCL does (its `ExceptBy` uses `set.Add` and its `IntersectBy` uses `set.Remove`).
- **The shared seed of `AggregateBy` is a trap for reference types**: `AggregateBy(key, new List<T>(), ...)` makes every key share one `List`, so the results merge into a single bucket. Use the **seed factory** overload when each key needs its own accumulator.
- **On an empty sequence `MaxBy` / `MinBy` depend on the element type**: a reference type yields `null`, a value type throws `InvalidOperationException`. That is the BCL's own rule and the polyfill copies it.
- **`Chunk` does not pad the tail**: the third chunk of `{1,2,3,4,5}.Chunk(2)` is an array of length 1, not `[5,0]`.
- **`LeftJoin` / `RightJoin` are engine-implemented only on `net451` through `net9.0`**; on `net10.0` and above the engine forwards to the BCL so it does not compete with the platform.
