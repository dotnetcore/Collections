# Sample.ImmutableVariants

> **English** | [中文](README.zh-CN.md)

> **Immutable variants** — `ImmutableMultiList<T>` / `ImmutableMultiDictionary<TKey,TValue>`: mutations return a new instance, the instance never changes (`DotNetCore.Collections.Multi`, 6.2.0).

## What this sample demonstrates

Immutable multi-set / multi-map turn "a seemingly mutating operation returns a new instance" into a first-class semantic:
`Add` / `Remove` do not modify the receiver but hand back a new instance, while the old instance stays usable. Because an
instance never changes, any thread can read concurrently **lock-free** — this is not "a lock added" but "no lock needed".
The sample shows three facets: a mutation returns a new instance and the source is unchanged, the Builder's structural
sharing (a no-write `ToImmutable` returns the source instance itself), and the key-count / value-count semantics of the
immutable map.

## When to use it

- Many readers share one snapshot, read-heavy write-light, needing natural thread safety.
- Configuration / snapshot freezing: after construction you don't want any code to change it.
- Version rollback: old instances are retained and can be rolled back.
- Do not use for: hot mutable counting (use a mutable `MultiList<T>` + synchronization); a single-point change on a huge structure (the first write copies the whole table, a costly COW).

## How to run

```bash
dotnet run --project sample/Sample.ImmutableVariants
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Mutation returns a new instance, source unchanged | `ImmutableMultiList<T>.Add(T)` / `CountOf` / `TotalCount` / `DistinctCount` |
| 2 | Builder structural sharing and copy-on-write | `ToBuilder()` / `Builder.Add` / `Builder.ToImmutable()` |
| 3 | Immutable multi-map | `ImmutableMultiDictionary<TKey,TValue>.Add` / `KeyCount` / `TotalValueCount` / indexer / `ToDictionary()` |

## Key APIs

- `ImmutableMultiList<T>` — truly immutable multi-set; `Add(T)` / `Add(T,int)` / `Remove(T,int)` / `RemoveAllCopies(T)` all return a **new instance**.
- `ImmutableMultiList<T>.ToBuilder()` — returns a `Builder`, **without copying** the internal state; when the builder has no write, `ToImmutable()` returns the source instance itself (assertable via `ReferenceEquals`).
- `ImmutableMultiList<T>.Count` — the number of copies (equals `TotalCount`); `DistinctCount` is the number of distinct elements.
- `ImmutableMultiDictionary<TKey,TValue>` — truly immutable multi-map; `Add` / `AddRange` / `Remove` return a new instance.
- `ImmutableMultiDictionary<TKey,TValue>.Count` — the **key count** (equals `KeyCount`); `TotalValueCount` is the value count.

## Notes and pitfalls

- **A mutation returns a new instance — don't ignore the return value**: `seed.Add(...)` does not change `seed`; use `seed = seed.Add(...)` or a Builder.
- **This package has no `ImmutableMultiList<T>.Empty`**: start with the public parameterless constructor `new ImmutableMultiList<T>()`.
- **A no-write Builder's `ToImmutable` may be the source instance**: reference equality is deliberately preserved structural sharing; don't assume it is always a new object.
- **The first write copies the whole table (COW)**: many chained `Add` calls without a Builder cause heavy allocation; for bulk use `ToBuilder`.
