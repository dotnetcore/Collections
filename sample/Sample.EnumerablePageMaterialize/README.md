# Sample.EnumerablePageMaterialize

> **English** | [中文](README.zh-CN.md)

> **EnumerablePage materialization** — a single `Skip/Take` materialization, so a deep page no longer explodes with page number (feature F39, `DotNetCore.Collections.Paginable`, 6.0.0).

## What this sample demonstrates

`EnumerablePage<T>` is the page implementation for in-memory / general sequences. 6.0.0 fixed the materialization strategy: for a non-`IList` `IEnumerable`, instead of per-member `ElementAt(k+i)` (approximately `O(pageSize × skip)`, nearly quadratic) it now does a **single** `Skip(skip).Take(s).ToList()`; a fragment (`sourceIsFull: false`) only `Take(s)` with no further offset. On the `GetPage` side there are two more tiers: an `ICollection<T>` goes through O(1) `Count` + `Skip/Take`, and a lazy non-`ICollection` does a single pass that fetches the page and the total length at once. This sample shows a lazy deep page, the `List<T>` fast path, materialized-snapshot semantics, and the two `sourceIsFull` branches.

## When to use it

- Deep paging that does not explode with page number.
- A fragment page that is no longer offset twice (paired with F32's `sourceIsFull: false`).
- A lazy sequence that is enumerated only once.
- `IQueryable` (EF etc.) is left to the provider's translation, or to an adapter page type.

## How to run

```bash
dotnet run --project sample/Sample.EnumerablePageMaterialize
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Lazy-sequence deep page, single materialization | `SolidPageExtensions.GetPage` (`IEnumerable<T>`) |
| 2 | `List<T>` fast path (`ICollection`) | `GetPage(pageNumber, pageSize)` |
| 3 | Materialized snapshot: source changes, page does not | `CurrentPageSize` / `TotalMemberCount` |
| 4 | `sourceIsFull` branches | `EnumerablePage<T>(..., sourceIsFull: true/false)` |

## Key APIs

- `EnumerablePage<T>(enumerable, currentPageNumber, pageSize, totalMemberCount, sourceIsFull = true)` — public constructor.
- `SolidPageExtensions.GetPage<T>(enumerable, pageNumber, pageSize)` — full-source page fetch (`ICollection` / lazy, two tiers).
- `IPage<T>.CurrentPageSize` / `TotalMemberCount` / `ToOriginalItems()` / `this[int].ItemNumber`.

## Notes and pitfalls

- **`sourceIsFull` semantics**: `true` means the input is the full source (slice by page number with `Skip`); `false` means a fragment (only `Take`).
- **Materialization means later changes to the source do not affect the page**: for new data, fetch the page again.
- **An empty source is legal**: `EmptyPage<T>` / `CurrentPageSize == 0`.
- **6.0.0 also fixed the bug where `CurrentPageSize` returned 0 for an evenly-dividing last page** (see F36).
