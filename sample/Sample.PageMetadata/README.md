# Sample.PageMetadata

> **English** | [中文](README.zh-CN.md)

> **Page metadata and validation** — the metadata snapshot on a page, the caller-input metadata, and the derivation source shared by both (feature F36, `DotNetCore.Collections.Paginable`, 6.0–6.1).

## What this sample demonstrates

Page metadata answers "which page is this, how many pages are there, is there a previous/next". The two public types have clearly separated roles: `PageMetadata` is the **snapshot** on a page (serializable, assertable, unaffected by changes to the source), and `PageFragmentInfo` is the metadata used as caller **input** (validated on construction). The two convert through `FromMetadata` / `ToMetadata`, and share the same `PageMetaInfo.Calculate` derivation source as the page instance, so navigation fields never drift. This sample shows reading the snapshot, the `ToString` summary, round-trip consistency, and the key distinction of an empty page's `TotalPageCount=1` / `RealPageCount=0`.

## When to use it

- An API returns `{ items, page: {...} }` → `page.GetMetadata()`.
- Asserting that a rebuilt page matches the original → `FromMetadata` / `ToMetadata`.
- Logging the paging state → `metadata.ToString()`.
- When you need a total-count / total-page control.

## How to run

```bash
dotnet run --project sample/Sample.PageMetadata
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Read the page metadata snapshot | `IPage.GetMetadata()` → `PageMetadata` |
| 2 | Multi-line summary | `PageMetadata.ToString()` |
| 3 | Fragment metadata round-trip | `PageFragmentInfo.FromMetadata` / `ToMetadata` |
| 4 | Empty-page double counting | `TotalPageCount` / `RealPageCount` |

## Key APIs

- `PageMetadata.TotalPageCount` / `RealPageCount` / `TotalMemberCount` / `PageSize` / `CurrentPageSize` / `CurrentPageNumber` / `HasPrevious` / `HasNext`.
- `PageFragmentInfo.FromMetadata(PageMetadata)` / `ToMetadata()`.
- `Paginable.CreateEmptyPage<T>()` — an empty page (one page with no members).

## Notes and pitfalls

- **`PageMetadata` is a snapshot**, unaffected by changes to the source.
- **Empty source has `TotalPageCount == 1`, `RealPageCount == 0`**: to decide "there are no pages", use `TotalMemberCount == 0` or `RealPageCount == 0`.
- **`PageFragmentInfo` is validated on construction**: out-of-range metadata throws `ArgumentOutOfRangeException` (F40).
- **An empty page's `FromMetadata` may throw** (no usable pageSize).
- **`CurrentPageSize` is the metadata value under a Lenient short fragment** (F33).
