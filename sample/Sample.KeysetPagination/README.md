# Sample.KeysetPagination

> **English** | [中文](README.zh-CN.md)

> **Keyset (seek) pagination** — continue paging with a sort-key anchor, with no `OFFSET` scan and no `COUNT(*)` round-trip (`DotNetCore.Collections.Paginable`, 6.0.0).

## What this sample demonstrates

`KeysetPageExtensions` provides `GetFirstPageByKeyset` / `GetPageByKeyset`: the first page carries no anchor, and each subsequent call feeds back the "last key of the previous page", forming a loop of "first page → next page → … → `HasNext == false`". The result type is the concrete `KeysetPage<T>` (**not** `IPage<T>`), deliberately carrying no total page count / total member count. This sample shows the in-memory delegate overload, the `IQueryable` expression-tree path, the "compose only" contract of `PaginableKeyset.BuildKeysetQuery`, and the boundaries of an empty source and an invalid `pageSize`.

## When to use it

- Infinite scroll / message feeds / activity streams: forward-only paging, no total page count needed.
- Deep-pagination exports, scanning a whole table in batches: the cost is independent of page depth.
- Cursor-based REST APIs: the anchor is the cursor.
- Do not use for: needing "jump to page N" or displaying a total count → offset paging `GetPage` / `ToPaginable`.

## How to run

```bash
dotnet run --project sample/Sample.KeysetPagination
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | First page → read `LastMember` → next page → end on `HasNext` | `GetFirstPageByKeyset` / `GetPageByKeyset` / `LastMember` |
| 2 | In-memory delegate overload and descending continuation | `IEnumerable<T>` overload + `descending: true` |
| 3 | Compose only, keeping the `pageSize + 1` probe row | `PaginableKeyset.BuildKeysetQuery` |
| 4 | Valid empty page for an empty source / `pageSize` validation | `KeysetPage<T>.CurrentPageSize` / `ArgumentOutOfRangeException` |

## Key APIs

- `KeysetPageExtensions.GetFirstPageByKeyset<T,TKey>(keySelector, pageSize, descending = false)` — first page, no anchor.
- `KeysetPageExtensions.GetPageByKeyset<T,TKey>(keySelector, lastKey, pageSize, descending = false)` — the next page after the anchor.
- `PaginableKeyset.BuildKeysetQuery(source, keySelector, lastKey, pageSize, descending)` — composes only (`Where + OrderBy + Take(pageSize + 1)`), does not materialize.
- `KeysetPage<T>.Members` / `PageSize` / `CurrentPageSize` / `HasNext` / `IsFirstPage` / `LastMember` (`T?`).

## Notes and pitfalls

- **No total, no page number, no `IPage` navigation**: `KeysetPage<T>` is deliberately not `IPage<T>`; for a page-number control use offset paging.
- **`LastMember` may be `default`** (`null` for an empty page): check `CurrentPageSize > 0` before dereferencing.
- **The sort key must be stable and comparable**: types supporting `<` / `>` such as `int` / `long` / `DateTime` / `decimal` / `DateTimeOffset`; add a tie-breaker when keys duplicate.
- **`BuildKeysetQuery` keeps `pageSize + 1` rows**: the caller must trim the probe row itself; `GetFirstPageByKeyset` / `GetPageByKeyset` already trim it.
- **Out-of-range / invalid arguments throw `ArgumentOutOfRangeException`** (unified in 6.2.0), not `IndexOutOfRangeException`.
