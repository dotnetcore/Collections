# Sample.SinglePageSet

> **English** | [中文](README.zh-CN.md)

> **Single-page set wrapper** — wrap "one already-assembled page + full-source metadata" into an `IPaginable<T>`, with `PageCount` always 1 (`DotNetCore.Collections.Paginable`, 6.2.0).

## What this sample demonstrates

When a signature needs `IPaginable<T>` (or `IEnumerable<IPage<T>>`) but you only have one already-assembled page, use `Paginable.CreateSinglePageSet` to wrap it into a single-page set, avoiding fabricating the remaining pages or re-fetching the whole source just to satisfy the shape. The key is its **layered semantics**: the collection level's `PageCount` is always 1 ("how many pages do I give you"), while the page level keeps the full-source numbering (`CurrentPageNumber` / `TotalPageCount` unchanged). This sample shows consuming through the interface, enumeration yielding only one page, out-of-range page fetch, and usage with `PageFragmentInfo` / `PageCreationOptions`.

## When to use it

- A service layer returns `IPaginable<T>`, but the repository layer only has one page of DTOs + total.
- A gRPC / HTTP adapter converts `items + totalCount` into the internal unified paging shape.
- Testing: feeding a fixed one-page fixture to code that depends on `IPaginable<T>`.
- Only needing one `IPage<T>` → use `CreatePage` directly, no need to wrap a set; real multi-page lazy loading → `ToPaginable`.

## How to run

```bash
dotnet run --project sample/Sample.SinglePageSet
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Wrap and consume through the interface (layered counts) | `Paginable.CreateSinglePageSet` / `PageCount` / `MemberCount` |
| 2 | Enumeration yields only one page | `GetEnumerator()` |
| 3 | Fetching a non-existent page → out of range | `GetPage(2)` → `ArgumentOutOfRangeException` |
| 4 | With a metadata object and options | `PageFragmentInfo` / `PageCreationOptions.Strict` |

## Key APIs

- `Paginable.CreateSinglePageSet<T>(fragment, pageNumber, pageSize, totalMemberCount)` — triple-metadata wrapping.
- `Paginable.CreateSinglePageSet<T>(fragment, PageFragmentInfo[, PageCreationOptions])` — metadata-object / options wrapping.
- `PaginableSinglePage<T>.PageCount` (always 1) / `MemberCount` (full-source total) / `GetPage(1)` / `GetPage(n≠1)` out of range.
- `IPaginable<T>.GetPage` / `IPaginable.PageSize` / `IPaginable.MemberCount`.

## Notes and pitfalls

- **`PageCount` is not the inner page's `TotalPageCount`**: the former is always 1 at the collection level, the latter is the full-source page count — deliberately divergent.
- **`MemberCount` is the full-source total**, not `items.Count`.
- **The constructor is `internal`**: it can only be created via `Paginable.CreateSinglePageSet`.
- **`GetPage(n≠1)` throws `ArgumentOutOfRangeException`**: the collection physically has only one page.
- **`foreach` runs only once**: do not assume multiple pages; semantically different from `CreateEmptyPage` (one page with no members).
