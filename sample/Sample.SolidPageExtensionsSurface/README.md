# Sample.SolidPageExtensionsSurface

> **English** | [中文](README.zh-CN.md)

> **The `SolidPageExtensions` entry surface** — the extension-method entry points of offset pagination: `ToPaginable` / `GetPage` / `*Async` (feature F45, core package + 9 adapters, each with a same-named type).

## What this sample demonstrates

`SolidPageExtensions` is the **extension-method entry surface** of offset pagination. On the core package this sample walks through: `ToPaginable` (turn a full source into a pageable set) and `GetPage` (slice one page directly from a full source) for both `IEnumerable` and `IQueryable` sources; an empty source returning an empty page and out-of-range throwing `ArgumentOutOfRangeException`; the verb distinction (`GetPage` for a full source vs `ToPage` for a fragment); and this repo's **number-one pitfall** — the type name `SolidPageExtensions` is identical across 10 packages, so when two packages are referenced at once, writing the bare type name causes `CS0104`.

## When to use it

- An in-memory / post-Dapper `IEnumerable`, or an `IQueryable` source, that needs one line to become a pageable set or to fetch a page.
- Wanting to cap via `limitedMemberCount` (F38).
- When mixing two ORMs you must disambiguate (see U27) — writing only the extension-method syntax is usually fine.

## How to run

```bash
dotnet run --project sample/Sample.SolidPageExtensionsSurface
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | `ToPaginable` builds a set (IEnumerable) | `SolidPageExtensions.ToPaginable<T>(this IEnumerable<T>, int pageSize, int? = null)` |
| 2 | `GetPage` slices one page from a full source | `SolidPageExtensions.GetPage<T>(this IEnumerable<T>, int, int)` |
| 3 | IQueryable uses the same verbs | `SolidPageExtensions.ToPaginable<T>(this IQueryable<T>, ...)` / `GetPage<T>(this IQueryable<T>, ...)` |
| 4 | Empty source does not throw / out-of-range throws | `GetPage` + `ArgumentOutOfRangeException` |
| 5 | Same-named type pitfall (CS0104) | `typeof(DotNetCore.Collections.Paginable.SolidPageExtensions)` |
| 6 | `GetPage` (full source) vs `ToPage` (fragment) | `FragmentPageExtensions.ToPage<T>(...)` |

## Key APIs

- `SolidPageExtensions.ToPaginable<T>(this IEnumerable<T>, int? limitedMemberCount = null)` / `ToPaginable<T>(this IEnumerable<T>, int pageSize, int? limitedMemberCount = null)` — returns `PaginableEnumerable<T>` (with `PageCount` / `MemberCount` / `PageSize`).
- `SolidPageExtensions.GetPage<T>(this IEnumerable<T>, int pageNumber, int pageSize)` — returns `IPage<T>`.
- `SolidPageExtensions.GetPage<T>(this IQueryable<T>, int pageNumber, int pageSize)` / `ToPaginable<T>(this IQueryable<T>, ...)` — the `IQueryable` versions.
- `FragmentPageExtensions.ToPage<T>(this IEnumerable<T>, int pageNumber, int pageSize, int totalMemberCount)` — the fragment entry, **not** `GetPage`.

## Notes and pitfalls

- **Docs and samples mentioning `SolidPageExtensions` must carry the package / assembly qualifier** (a hard rule of the whole series); there are 10 same-named ones, and referencing two packages and writing the bare type name gives `CS0104`.
- **Verb meanings**: `ToPaginable` = build a set; `GetPage` = slice one page from a **full source**; a fragment uses `ToPage`.
- **Empty source**: `GetPage(1)` returns an empty page and does not throw; **out-of-range**: `ArgumentOutOfRangeException` (F40).
- **Async semantics are per package** (F31 / F44): the core in-memory Async does not observe the token; this sample only demonstrates the synchronous surface.
