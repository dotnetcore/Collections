# Sample.EmptyAndSinglePage

> **English** | [中文](README.zh-CN.md)

> **Empty page and single-page set** — `EmptyPage<T>` (no results does not return null) and `PaginableSinglePage<T>` (one page wrapped as a set, `PageCount` always 1) (`DotNetCore.Collections.Paginable`, 6.2.0).

## What this sample demonstrates

Two "boundary page" types. `EmptyPage<T>` is the empty-result page: when a query returns no results, return it instead of `null`, with fixed `pageNumber: 1`, `pageSize: 0`, `totalMemberCount: 0`. `PaginableSinglePage<T>` wraps an **already existing** page as an `IPaginable<T>`, for callers whose "signature wants a set but they only have one page" (a cached page, gRPC's `items + totalCount`). The key point is **metadata layering**: at the set level `PageCount` is always 1 and `MemberCount` is the source total; at the page level the global numbering is preserved (page 3 of 12 is still 3/12).

## When to use it

- A query returns 0 rows: uniformly return `EmptyPage<T>` so downstream can enumerate safely.
- Only one page of data but the API wants an `IPaginable<T>`: use `Paginable.CreateSinglePageSet`, without faking the other pages.
- Not for: pretending there are multiple pages (`PageCount` is deliberately always 1).

## How to run

```bash
dotnet run --project sample/Sample.EmptyAndSinglePage
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Empty page: no results is not null | `new EmptyPage<T>()` / `Paginable.CreateEmptyPage<T>()` |
| 2 | Single-page set: set-level `PageCount` is always 1 | `Paginable.CreateSinglePageSet<T>(...)` / `PaginableSinglePage<T>.PageCount` |
| 3 | `GetPage(2)` is out of range | `PaginableSinglePage<T>.GetPage(int)` → `ArgumentOutOfRangeException` |
| 4 | The `PageFragmentInfo` entry + the layering pitfall | `PageFragmentInfo` / `PaginableSinglePage<T>.MemberCount` |

## Key APIs

- `EmptyPage<T> : EnumerablePage<T>` — constructed as `Enumerable.Empty<T>()`, `pageNumber: 1`, `pageSize: 0`, `totalMemberCount: 0`.
- `Paginable.CreateEmptyPage<T>()` — the empty-page factory entry.
- `Paginable.CreateSinglePageSet<T>(IEnumerable<T> fragment, int pageNumber, int pageSize, int totalMemberCount)` — the single-page set factory (returns the concrete type `PaginableSinglePage<T>`).
- `Paginable.CreateSinglePageSet<T>(IEnumerable<T> fragment, PageFragmentInfo metadata)` — the entry taking a metadata object.
- `PaginableSinglePage<T>.PageCount` / `.MemberCount` / `.GetPage(int)` — set-level and page-level semantics.

## Notes and pitfalls

- **`PaginableSinglePage.PageCount` is always 1**: it is not the page's `TotalPageCount`; read the source total page count via `GetPage(1).TotalPageCount`.
- **`GetPage` only accepts 1**: the set physically has one page, so `GetPage(2)` throws `ArgumentOutOfRangeException`.
- **`EmptyPage`'s `PageSize` is 0**; its `TotalPageCount` is reported as 1.
- **A single-page set does not re-slice**: the passed fragment is the page content.
