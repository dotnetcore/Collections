# Sample.Snapshot

> **English** | [中文](README.zh-CN.md)

> **An independent snapshot, Snapshot()** — a detached copy of `MultiList<T>` / `MultiDictionary<TKey,TValue>` (`DotNetCore.Collections.Multi`, 6.9.0).

## What this sample demonstrates

`Snapshot()` returns an **independent copy**, specifically serving "mutate while enumerating": enumerating a live instance directly is not modification-safe, whereas a snapshot is already detached,
so it won't observe later changes during enumeration. This sample shows three facets: enumerating a snapshot while continuing to modify the original collection,
`Snapshot` being equivalent to `Clone` (a shallow copy), and a `MultiDictionary` snapshot being independent for both the outer dictionary and the inner value collections.

## When to use it

- Needing to iterate a collection while continuing to modify it during the iteration (the safe prerequisite is to take a snapshot first).
- Needing a stable view "as of now" that later changes should not affect.
- Not for: needing the copy itself to be immutable → `ImmutableMultiList<T>` / `ImmutableMultiDictionary<TKey,TValue>` (6.2.0);
  just reading a live collection once without modifying it → enumerate it directly.

## How to run

```bash
dotnet run --project sample/Sample.Snapshot
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Mutate while enumerating: enumerate a snapshot while modifying the original | `MultiList<T>.Snapshot()` |
| 2 | Snapshot is equivalent to Clone (shallow copy) | `Snapshot()` / `Clone()` |
| 3 | The independent layers of a MultiDictionary snapshot | `MultiDictionary<TKey,TValue>.Snapshot()` |

## Key APIs

- `MultiList<T>.Snapshot()` — returns a `MultiList<T>`, equivalent to `Clone()`.
- `MultiDictionary<TKey,TValue>.Snapshot()` — returns a `MultiDictionary<TKey,TValue>`, equivalent to `Clone()`.
- The semantics match `Clone()`: a **shallow copy** — element references are shared, while copy counts / inner value collections are independent.

## Notes and pitfalls

- **Shallow copy**: element references are shared (changing an element itself is visible on both sides); what is independent is the counts and the inner structure.
- **A snapshot is detached**: after a snapshot is built, later changes to the original collection are not reflected in the snapshot.
- **Not a concurrent type**: `Snapshot()` solves the stability of "modifying during enumeration", not multi-threaded races;
  for concurrent scenarios use `ConcurrentMultiList<T>` / `ConcurrentMultiDictionary<TKey,TValue>` (6.2.0).
- **Consistent naming**: the concurrent types' `Snapshot()` also returns non-concurrent types (built via `Clone()`), and this sample keeps the reading consistent between the core type and the family.
