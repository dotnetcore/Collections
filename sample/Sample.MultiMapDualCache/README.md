# Sample.MultiMapDualCache

> **English** | [中文](README.zh-CN.md)

> **Multimap dual cache** — a total-value-count cache + a value reverse index (F11, `DotNetCore.Collections.Multi`, 6.2.0).

## What this sample demonstrates

`MultiDictionary<TKey,TValue>` maintains two caches internally, turning two kinds of high-frequency reads from scans
into O(1): `_totalValueCount` caches the total number of values (`TotalValueCount` reads in O(1)), and `_valueIndex`
(a value → key-set reverse index) lets `ContainsValue` stop scanning the whole table. This sample shows three facets:
the externally visible O(1) reads, the dedicated bucket for `null` values, and how the caches stay in sync with the
public mutation APIs (including `Clone` rebuilding via `Add`).

## When to use it

- Reading the total value count frequently: `TotalValueCount` is O(1), so observation and assertions no longer pay an O(keys) scan.
- Value existence is a hot path: `ContainsValue` is O(1) and can go on the hot path.
- A live reverse view is ready: `AsReverse` reads the reverse index directly (see F18).
- When not to use it: if you only write and never read the total → the cache is nearly negligible pure overhead.

## How to run

```bash
dotnet run --project sample/Sample.MultiMapDualCache
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | O(1) reads of the total value count and value existence | `TotalValueCount` / `ContainsValue` / `ValueCount` |
| 2 | Dedicated bucket for `null` values | `Add(key, null)` / `ContainsValue(null)` |
| 3 | Caches stay in sync with public mutation APIs | `AddRange` / `Remove` / `Clone` |

## Key APIs

- `MultiDictionary<TKey,TValue>.TotalValueCount` — the total number of values, an O(1) cached read.
- `MultiDictionary<TKey,TValue>.ContainsValue(TValue)` — value existence via the reverse index in O(1); `null` goes to the dedicated bucket.
- `MultiDictionary<TKey,TValue>.Clone()` — rebuilt through the public `Add`, carrying a consistent dual cache.

## Notes and pitfalls

- **The cache is transparent to consumers**: do not rely on the old "reading the total scans" behavior; the cost model has changed.
- **Value equality is fixed to `EqualityComparer<TValue>.Default`**: the reverse index is consistent with the inner collections, and a custom value comparer is not accepted.
- **Use only the public mutation APIs**: obtaining a mutable inner collection through a custom `innerFactory` and then modifying it breaks cache consistency.
- **The write path has two extra bookkeeping steps**: `Add` increments a delta, `RemoveOccurrence` decrements by 1, and `RemoveKeyCore` decrements a whole bucket — pure-write workloads pay a slight cost.
