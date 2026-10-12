# Sample.PositionalRead

> **English** | [中文](README.zh-CN.md)

> **Rank and quantile reads** — `GetByRank` / `GetRank` / `GetMedian` / `GetQuantile` / `TotalValueCount` (`DotNetCore.Collections.Multi`, 6.5.0 lists + 6.6.0 maps).

## What this sample demonstrates

The ordered Multi family provides rank and quantile reads: fetch an element by its position in the **copy-expanded** sequence
/ reverse-look-up a rank, and give medians and quantiles on the map. The sample shows three facets: list ranks (`this[i]` /
`IndexOf` are the rank read), map ranks and quantiles, and the key distinction that "order statistics ≠ numeric interpolation".
All operations are O(log n): a single root-to-leaf descent, without materializing the expanded sequence.

## When to use it

- Median / quartiles → `GetMedian` / `GetQuantile`.
- Ranking / percentile reverse lookup → `GetRank`.
- The k-th best element → `GetByRank`.
- Do not use for: needing numeric-interpolation quantiles (e.g. P95 statistics — materialize and compute yourself); unordered collections (`MultiList<T>` has no such surface).

## How to run

```bash
dotnet run --project sample/Sample.PositionalRead
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | List ranks (copy-expanded) | `OrderedMultiList<T>.GetByRank` / `GetRank` / `this[int]` / `IndexOf` / `GetMedian` / `GetQuantile` |
| 2 | Map ranks and quantiles | `OrderedMultiDictionary<TKey,TValue>.GetByRank` / `GetRank` / `GetMedian` / `GetQuantile` / `TotalValueCount` |
| 3 | Order statistics vs numeric interpolation | `GetQuantile` returns a real element; out of range throws `ArgumentOutOfRangeException` |

## Key APIs

- `OrderedMultiList<T>.GetByRank(int)` — the rank-th element of the expanded sequence; `this[int]` is its alias.
- `OrderedMultiList<T>.GetRank(T)` — the rank of the element's **first copy**; returns `-1` if missing; `IndexOf` is the alias.
- `OrderedMultiList<T>.GetMedian()` / `GetQuantile(double)` — order median / order quantile (nearest-rank).
- `OrderedMultiDictionary<TKey,TValue>.GetByRank(int)` — the rank-th `(key, value)` pair of the expanded sequence.
- `OrderedMultiDictionary<TKey,TValue>.GetRank(TKey,TValue)` — the rank of that pair; returns `-1` if missing.
- `OrderedMultiDictionary<TKey,TValue>.GetMedian()` / `GetQuantile(double)` / `TotalValueCount` — median / quantile / value count (O(1)).

## Notes and pitfalls

- **A rank = a position in the copy-expanded sequence**, not a distinct-element index: `GetByRank(2)` of `{1,3,3,5}` is the second `3`.
- **`GetQuantile` is an order quantile, not interpolation**: it returns a really stored element and never interpolates between two values.
- **A map rank targets the global `(key, value)` pair sequence**: the expanded sequence = keys ascending × values ascending per key; rank 0 is the smallest value of the smallest key.
- **`TotalValueCount` is O(1) on the map** (cached at the tree root); the list side counterpart is `TotalCount`.
- **Out of range throws**: `GetByRank` out of range throws `ArgumentOutOfRangeException`; reading the median / quantile of an empty collection throws `InvalidOperationException`.
