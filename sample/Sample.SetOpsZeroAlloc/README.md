# Sample.SetOpsZeroAlloc

> **English** | [中文](README.zh-CN.md)

> **Zero-allocation set operations** — in-place reads plus a reused scratch area when the argument is already a same-comparer `MultiList<T>` (F12, `DotNetCore.Collections.Multi`, 6.2.0).

## What this sample demonstrates

`MultiList<T>`'s set operations (`UnionWith` / `IntersectionWith` / `ExceptWith` / `SymmetricExceptWith` and the subset /
superset tests), when the argument is **already a same-comparer `MultiList<T>`**, read the argument's count table in
place instead of materializing a second multiset; the target state is written into a scratch list reused across calls.
This sample demonstrates the **observable semantics** of four call shapes, marking with comments "when it reads in
place, when it materializes". Zero-allocation is an implementation detail on the argument side and cannot be asserted
stably from the output, so this sample does not print allocation byte counts.

## When to use it

- High-frequency bag set operations: skip allocation when the argument is already a bag, so the hot path no longer juggles on the heap.
- Chained `a.Op(b)` where `b` is already a bag: read in place, saving the argument materialization and iterator allocation.
- When not to use it: one-off low-frequency operations → the difference is negligible; either form will do.

## How to run

```bash
dotnet run --project sample/Sample.SetOpsZeroAlloc
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Same-comparer `MultiList` argument read in place | `UnionWith(IEnumerable<T>)` |
| 2 | Array argument materializes (same result) | `UnionWith` (contrast form) |
| 3 | Different comparer re-materializes by the receiver | `UnionWith` (comparer difference) |
| 4 | Subset / superset tests go straight to the two tables | `IsSubsetOf` / `IsSupersetOf` / `IsProperSupersetOf` |
| 5 | `UnionWith(self)` identity short-circuit | `UnionWith(self)` |

## Key APIs

- `MultiList<T>.UnionWith(IEnumerable<T>)` / `IntersectionWith` / `ExceptWith` / `SymmetricExceptWith` — multiset operations.
- `MultiList<T>.IsSubsetOf(IEnumerable<T>)` / `IsSupersetOf` / `IsProperSubsetOf` / `IsProperSupersetOf` — subset / superset tests.
- `MultiList<T>.CountOf(T)` / `TotalCount` / `DistinctCount` — used to observe the operation results.

## Notes and pitfalls

- **Zero-allocation only when the argument is a same-comparer `MultiList<T>`**: passing an array / LINQ sequence goes through `Snapshot` materialization and still allocates.
- **A different comparer materializes**: the semantics require re-interpreting the argument with the receiver's comparer, so the result is unchanged but the cost returns.
- **The scratch area is cleared after the call**: it does not hold element references; not thread-safe.
- **`UnionWith(self)` is the identity**: an explicit short-circuit avoids doubling copies by adding while iterating.
- **Zero-allocation is only an implementation detail**: do not write it as a public contract; the observable semantics match the old behavior.
