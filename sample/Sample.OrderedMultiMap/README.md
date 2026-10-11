# Sample.OrderedMultiMap

> **English** | [中文](README.zh-CN.md)

> **Ordered multimap OrderedMultiDictionary&lt;TKey,TValue&gt;** — a multimap with keys ascending and values within each key ascending (F08, `DotNetCore.Collections.Multi`, 6.2.0).

## What this sample demonstrates

`OrderedMultiDictionary<TKey,TValue>` is the ordered counterpart of `MultiDictionary`: keys stay ascending and the values under each key stay ascending, with single add / remove / lookup on both axes worst-case O(log n). It shares the set-of-key-value semantics (parameter is a set, values may repeat, emptying the inner set deletes the key). The sample shows three facets: two-axis ordered enumeration, the counting convention where `Count` is the key count rather than the value count, and rank reads targeting the "keys ascending × values within a key ascending" expanded sequence.

## When to use it

- Ordered indexes / lexicographic reports that need a predictable key order and value order.
- Global value ranks / quantiles (`GetByRank` / `GetMedian` / `GetQuantile`).
- Do not use it for: O(1) hot key-value lookups → `MultiDictionary`.

## How to run

```bash
dotnet run --project sample/Sample.OrderedMultiMap
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Two-axis ordered enumeration | `Add(TKey, TValue)` / `GetEnumerator` |
| 2 | Counting convention (Count = key count) | `Count` / `TotalValueCount` / `ValueCount(key)` / `Keys` / `Values` |
| 3 | Rank reads (targeting the expanded sequence) | `GetByRank` / `GetRank` / `GetMedian` |
| 4 | Value queries and grouped export | `Contains(key,value)` / `ContainsValue` / `EntrySet` |

## Key APIs

- `OrderedMultiDictionary<TKey,TValue>.Add(TKey, TValue)` — adds a pair; one descent per axis, O(log n).
- `Count` / `KeyCount` — the **key count**; `TotalValueCount` / `ValueCount(key)` — the **value count**.
- `Keys` — keys ascending; `Values` — a flat sequence (keys ascending × values within a key ascending).
- `this[TKey]` — a live sequence of the values under that key in ascending order; a missing key returns an empty collection without throwing.
- `GetByRank(int)` / `GetRank(TKey, TValue)` / `GetMedian()` / `GetQuantile(double)` — rank reads targeting the expanded sequence.
- `Contains(TKey, TValue)` — whether the value is under that key; `ContainsValue(TValue)` — whether the value is under any key (full scan, O(n)).
- `EntrySet()` — a grouped projection of "key + all its values".

## Notes and pitfalls

- **`Count` is the key count**, not the value count: use `TotalValueCount` for the value total.
- **`Values` is a flat sequence**: values are interleaved by key, not sorted globally.
- **`GetRank` is not a distinct-element index**: when a value has multiple copies, the rank refers to the expanded position.
- **A missing-key indexer returns an empty collection** without throwing; only `Count` and `ContainsKey` can distinguish "missing key" from "empty values".
