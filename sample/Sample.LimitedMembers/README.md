# Sample.LimitedMembers

> **English** | [中文](README.zh-CN.md)

> **Member caps and open interval** — the global hard cap `MaxMemberItems` and the call-level cap `limitedMemberCount` (`DotNetCore.Collections.Paginable`, 6.0.0).

## What this sample demonstrates

Paginable has two mechanisms for "limiting how many members enter the paging view": the global hard cap `MaxMemberItems` (default `10_000_000`, where **exactly equal to the cap is allowed** — 6.0.0 changed the closed interval to an open one) and the optional per-call `limitedMemberCount` (capping the displayed count). Cap calculation is centralized in `PaginableCalc` (in the `Internal` namespace but `public static`, unit-testable). This sample shows `PaginableCalc`'s `min` semantics and rounding up, `ToPaginable(..., limitedMemberCount)` truncating the paging view, the fallback when limited is above the real COUNT, and the `MaxMemberItems` open-interval boundary.

## When to use it

- A back-office list limits flipping to at most N rows → `ToPaginable(..., limitedMemberCount: 5000)`.
- Global protection against an illegal huge total → tighten `MaxMemberItems`.
- The data volume is inherently small → do not pass limited.

## How to run

```bash
dotnet run --project sample/Sample.LimitedMembers
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | The single point of cap calculation | `PaginableCalc.GetRealMemberCount` / `GetRealPageCount` |
| 2 | limited truncates the paging view | `ToPaginable(pageSize, limitedMemberCount)` / `MemberCount` / `PageCount` |
| 3 | limited above the real COUNT | `MemberCount` / `PageCount` (takes the real values) |
| 4 | Global hard cap open interval | `PageFragmentInfo` constructor / `LimitedMembersTypes` |

## Key APIs

- `PaginableCalc.GetRealMemberCount(int? limitedMemberCount, int count)` — `min` semantics, `null` means no limit.
- `PaginableCalc.GetRealPageCount(int realMemberCount, int pageSize)` — rounds up.
- `SolidPageExtensions.ToPaginable<T>(enumerable, pageSize, limitedMemberCount)` — call-level capping.
- `LimitedMembersTypes.Unlimited` (0) / `Customize` (1).

## Notes and pitfalls

- **Open interval**: `MaxMemberItems` **itself is allowed**; only `> MaxMemberItems` throws `ArgumentOutOfRangeException`.
- **limited is a "display cap", not a filter predicate**: it truncates the paging view, not a `WHERE`; `GetPage(3)` is still the 3rd page of the capped collection.
- **`limitedMemberCount <= 0` is treated as invalid** (`IsValid` only checks non-`null`; the factory path usually checks `> 0`).
- **`PaginableCalc` is in `DotNetCore.Collections.Paginable.Internal`**, but is `public static` and unit-testable.
