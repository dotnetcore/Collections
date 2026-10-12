# Sample.ConcurrentVariants

> **English** | [中文](README.zh-CN.md)

> **Concurrent variants** — `ConcurrentMultiList<T>` / `ConcurrentMultiDictionary<TKey,TValue>`: thread-safe multi-set and multi-map (`DotNetCore.Collections.Multi`, 6.2.0; read-only projections since 6.7.1).

## What this sample demonstrates

The concurrent variants let multiple threads count and group safely without an outer lock. The set `ConcurrentMultiList<T>`
serializes each operation behind a single lock, guaranteeing linearization and deterministic counts; the map
`ConcurrentMultiDictionary<TKey,TValue>` shards by key hash (8 shards by default), so writes to different keys run in
parallel and cross-shard reads take a consistent snapshot. The sample shows three facets: deterministic counts after
multi-threaded `Add`, the key-count / value-count semantics of the sharded map, and the read-only projections added in
6.7.1 (`EntrySet()` / `DistinctItems()` / `ToList()` / `ToArray()` / `ToDictionary()`).

## When to use it

- Multi-threaded frequency counting / histograms → `ConcurrentMultiList<T>`.
- Multi-threaded one-to-many indexing → `ConcurrentMultiDictionary<TKey,TValue>`.
- Do not use for: very read-heavy, very write-light workloads → use `ImmutableMultiList<T>` (6.2.0, lock-free reads).
- Do not use for: single-threaded hot spots → the mutable `MultiList<T>` is faster.

## How to run

```bash
dotnet run --project sample/Sample.ConcurrentVariants
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Multi-threaded `Add`, deterministic counts | `ConcurrentMultiList<T>.Add` / `CountOf` / `TotalCount` / `DistinctCount` |
| 2 | Sharded map and cross-shard reads | `ConcurrentMultiDictionary<TKey,TValue>.Add` / `ShardCount` / `KeyCount` / `TotalValueCount` / indexer |
| 3 | 6.7.1 read-only projections (detached snapshots) | `EntrySet()` / `DistinctItems()` / `ToList()` / `ToArray()` / `ToDictionary()` |
| 4 | Enumeration snapshot | after exporting via `ToArray()`, later writes don't affect the taken snapshot |

## Key APIs

- `ConcurrentMultiList<T>.Add(T)` / `Add(T,int)` — serialized behind a single lock, linearized; `CountOf` / `TotalCount` / `DistinctCount` match the mutable version's semantics.
- `ConcurrentMultiList<T>.Snapshot()` — returns an independent `MultiList<T>`; all projections below are taken from it.
- `ConcurrentMultiList<T>.EntrySet()` / `DistinctItems()` / `ToList()` / `ToArray()` / `ToDictionary()` — available since 6.7.1; the results are detached from the bag and unaffected by concurrent writes.
- `ConcurrentMultiDictionary<TKey,TValue>` — 8 shards by default (`shardCount` is rounded up to a power of two); `Count` / `KeyCount` = key count, `TotalValueCount` = value count.
- `ConcurrentMultiDictionary<TKey,TValue>.ShardCount` — the actual shard count.

## Notes and pitfalls

- **The set uses a single lock, no read parallelism**: concurrency correctness ≠ high throughput; for read-heavy write-light use `ImmutableMultiList<T>`.
- **Cross-shard reads on the map are expensive**: `Count` / `TotalValueCount` / `ContainsValue` / enumeration lock each shard in index order to take a consistent snapshot; don't read them repeatedly inside a loop.
- **Enumeration is a snapshot copy**: enumeration pays a one-time copy cost but is immune to concurrent writes and never tears.
- **`shardCount` is rounded up to a power of two**: required by the routing mask; `ShardCount` reports the actual value.
- **`ToDictionary()` throws** `InvalidOperationException` on a `null` element (inherited from `MultiList<T>`); `EntrySet()` is the `null`-safe alternative.
