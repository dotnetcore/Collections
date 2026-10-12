# Sample.OrderStatisticTree

> **English** | [中文](README.zh-CN.md)

> **Order-statistic tree engine** — `OrderStatisticTree&lt;TKey,TPayload&gt;` (internal), observed through `OrderedMultiList&lt;T&gt;` (`DotNetCore.Collections.Multi`, 6.2.0 / 6.5.0).

## What this sample demonstrates

`OrderStatisticTree<TKey,TPayload>` is the storage engine of the ordered Multi family: it maps each distinct key to a copy count and caches subtree copy totals, making "the k-th copy" and "the rank of a key" a **single root-to-leaf descent**. The engine is `internal` and not exposed, so this sample demonstrates **only through the observable behavior of `OrderedMultiList<T>`**: rank reads (`GetByRank` / `GetRank`), median / quantile (`GetMedian` / `GetQuantile`), ranges and the distinct-element view.

## When to use it

- You need an O(log n) **rank read** (the k-th copy / the rank of an element) without materializing the whole sequence.
- You need order statistics such as median / quantile (see 6.5.0).
- This article also targets readers who need to understand the performance model of ordered Multi; ordinary consumers can just use 6.2.0 / 6.5.0.
- Do not use it for: referencing the engine type directly — it is `internal` and will not compile.

## How to run

```bash
dotnet run --project sample/Sample.OrderStatisticTree
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Rank reads (O(log n)) | `GetByRank` / `GetRank` / `this[int]` / `IndexOf` |
| 2 | Median and quantile | `GetMedian` / `GetQuantile` |
| 3 | Ranges and distinct-element view | `GetRange` / `DistinctItems` / `EntrySet` |

## Key APIs

- `OrderedMultiList<T>.GetByRank(int)` — the k-th **copy** (rank targets the expanded sequence); out of range throws `ArgumentOutOfRangeException`.
- `OrderedMultiList<T>.GetRank(T)` — the rank of an element's first copy; returns `-1` when absent.
- `OrderedMultiList<T>.this[int]` / `IndexOf(T)` — equivalent to `GetByRank` / `GetRank` respectively.
- `OrderedMultiList<T>.GetMedian()` — the median element; for an even copy count it takes the **lower median**, and an empty collection throws `InvalidOperationException`.
- `OrderedMultiList<T>.GetQuantile(double)` — nearest-rank definition (`ceil(q × TotalCount) - 1`), never interpolating.
- `OrderedMultiList<T>.DistinctItems()` / `EntrySet()` — the distinct-element / "element + copy count" views.

## Notes and pitfalls

- **The engine is `internal`**: docs / samples must not reference `OrderStatisticTree` as a public API (it will not compile).
- **Ranks target the copy-expanded sequence**, not distinct-element indices: `GetByRank(0)` is the **first copy** of the smallest element.
- **It was a red-black tree before 6.5.0**: be mindful of the engine difference when citing older docs (the external semantics are unchanged).
- **`GetQuantile` does not interpolate**: it always returns an element actually held, never the average between two elements.
