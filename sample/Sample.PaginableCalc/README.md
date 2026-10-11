# Sample.PaginableCalc

> **English** | [中文](README.zh-CN.md)

> **The pagination calculation utility `PaginableCalc`** — the single source of truth for member-count capping and page-count conversion (feature F47, `DotNetCore.Collections.Paginable`, 6.0.0).

## What this sample demonstrates

`PaginableCalc` is the **single source of truth** for pagination calculations, shared by the core factory and all ORM adapters' coll-factories to avoid manual drift. It lives in the `DotNetCore.Collections.Paginable.Internal` namespace, but the type is a `public static class` that adapters and tests can call directly. This sample walks through two members: `GetRealMemberCount` (take the min when there is an effective cap and it is smaller; `null` means unlimited) and `GetRealPageCount` (`ceil(members / pageSize)`, counting a partially-filled last page as a page), and cross-checks them against the result of `ToPaginable(limitedMemberCount: ...)`.

## When to use it

- Developing a custom executor / adapter: call the same algorithm directly to compute the member count and page count.
- Wanting to stay consistent with the library's internal convention (avoiding inconsistent last-page semantics across ORMs).
- Not for: replacing business paging logic — it only covers member-count / page-count conversion.

## How to run

```bash
dotnet run --project sample/Sample.PaginableCalc
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Member-count capping (`null` = unlimited) | `PaginableCalc.GetRealMemberCount(int?, int)` |
| 2 | Page-count conversion (round up) | `PaginableCalc.GetRealPageCount(int, int)` |
| 3 | Cross-check with `ToPaginable`'s `limitedMemberCount` | `SolidPageExtensions.ToPaginable<T>(..., int? limitedMemberCount)` |

## Key APIs

- `PaginableCalc.GetRealMemberCount(int? limitedMemberCount, int count)` — takes the cap when it is effective and smaller than the source count, otherwise the source count.
- `PaginableCalc.GetRealPageCount(int realMemberCount, int pageSize)` — `ceil(members / pageSize)`, counting a partially-filled last page as a page.
- `PaginableSetBase<T>.LimitedMemberCount` / `.MemberCount` / `.PageCount` — consistent with `PaginableCalc`'s recomputed results.

## Notes and pitfalls

- **In the `Internal` namespace but public**: callable, but treat it as an implementation utility, not a business API.
- **`GetRealPageCount` does not validate `pageSize > 0`**: the factory validates alongside it; the caller must guarantee it.
- **Paired with F38 `MaxMemberItems`**: a result set exceeding the cap is rejected or trimmed, and trimming goes through `GetRealMemberCount`.
- Each adapter computing the page count by hand leads to inconsistent last-page semantics — go through `PaginableCalc` uniformly.
