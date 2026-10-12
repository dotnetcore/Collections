# Sample.OrderedAccessRange

> **English** | [中文](README.zh-CN.md)

> **Ordered access and range queries** — extremes, ranges and ordered access on `OrderedMultiList` (`DotNetCore.Collections.Multi`, 6.2.0).

## What this sample demonstrates

On top of its total order, `OrderedMultiList<T>` provides a set of ordered access and range queries: `GetFirst` / `GetLast` take the extremes (O(log n)), and `GetRange` slices out an ascending copy-expanded sequence by element range, with each endpoint's inclusivity controlled independently. All of these rely on the ordered leaf chain of the underlying order-statistic tree, so a range scan does not re-enter branches. The sample also shows the two-axis ordered semantics of `OrderedMultiDictionary`: **keys ascending, values within each key ascending**.

## When to use it

- Top / Bottom of a leaderboard, or any "grab the extreme immediately" scenario.
- **Range filtering** such as score bands / letter bands, when you do not want a full `Where` + `OrderBy`.
- Ordered snapshot output that feeds directly into reports / leaderboards.
- Do not use it for: unordered counting only → `MultiList`; descending enumeration → reverse the result or swap the comparer.

## How to run

```bash
dotnet run --project sample/Sample.OrderedAccessRange
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Extremes taken immediately | `GetFirst()` / `GetLast()` |
| 2 | Closed and half-open ranges | `GetRange(from, to)` / `GetRange(from, to, inclusiveFrom, inclusiveTo)` |
| 3 | Empty-collection behavior | `GetFirst` throws `InvalidOperationException`; `GetRange` returns an empty sequence |
| 4 | Ordered map key order / value order | `OrderedMultiDictionary.Keys` / indexer `this[key]` |

## Key APIs

- `OrderedMultiList<T>.GetFirst()` / `GetLast()` — smallest / largest element, O(log n); an empty collection throws `InvalidOperationException`.
- `OrderedMultiList<T>.GetRange(T from, T to)` — a **closed** range at both ends, returning an ascending copy-expanded sequence.
- `OrderedMultiList<T>.GetRange(T from, T to, bool inclusiveFrom, bool inclusiveTo)` — controls each endpoint's inclusivity independently; returns an empty sequence when `from` sorts after `to`.
- `OrderedMultiDictionary<TKey,TValue>.Keys` — enumerates keys in ascending order.
- `OrderedMultiDictionary<TKey,TValue>.this[TKey]` — a live sequence of the values under that key in ascending order; a missing key returns an empty collection.

## Notes and pitfalls

- **Range boundaries are defined by the comparer**: swapping the comparer swaps the boundary semantics; this type does not interpret element meaning.
- **Extremes throw on an empty collection** (`InvalidOperationException`): check `TotalCount` / `DistinctCount` first.
- **A range result is copy-expanded**: its length is counted in copies (`bean` with two copies is produced twice).
- **An ordered map's value order is "within each key" ascending**, not a global value sort; values across keys are interleaved by key.
