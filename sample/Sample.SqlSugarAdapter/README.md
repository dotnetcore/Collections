# Sample.SqlSugarAdapter

> **English** | [中文](README.zh-CN.md)

> **SqlSugar adapter entry surface and cancellation-token boundary** — `SolidPageExtensions` + `PaginableSqlSugarQuery<T>` + `SqlSugarPage<T>` (feature 2.x, `DotNetCore.Collections.Paginable.SqlSugar`, 2.x).

## What this sample demonstrates

The SqlSugar adapter wires the pagination verbs into `SqlSugar.ISugarQueryable<T>`. This sample is a **package-backed explanatory** example: it references the **real SqlSugar adapter package**, uses reflection to read the entry types and the public verb signatures of `SolidPageExtensions` from the real assembly, and focuses on clarifying the cancellation-token boundary of **6.6.0** — an already-cancelled token throws `OperationCanceledException` **before the first database round trip**, while mid-flight cancellation still depends on the SqlSugar version (its `CountAsync` / `ToPageListAsync` have no token overloads). The sample does not connect to a database; at the end it uses the core `Paginable.CreatePage` to build a real page, proving the page object reuses the core contract `IPage<T>`.

## When to use it

- Confirm the entry type names and verbs of a SqlSugar application.
- Web scenarios that need request cancellation to take effect on the first round trip (6.6+).
- Should not use: when you need a mid-flight cancellation guarantee → it depends on the SqlSugar version / provider; EF Core stack → see `Sample.EfCoreAdapter`.

## How to run

```bash
dotnet run --project sample/Sample.SqlSugarAdapter
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Real adapter assembly and entry types | `PaginableSqlSugarQuery<T>` / `SqlSugarPage<T>` / `SolidPageExtensions` (reflection) |
| 2 | Real public verb signatures | `ToPaginable` / `GetPage` / `*Async` (reflection) |
| 3 | Cancellation-token boundary (since 6.6.0) | `GetPageAsync(1, 20, ct)` throws `OperationCanceledException` before the first round trip |
| 4 | Shared core contract | `Paginable.CreatePage` → `IPage<T>` / `IPageMember<T>` |
| 5 | TFM and why it is not really executed | no ns2.0; `SqlSugarHelper` hits the database |

## Key APIs

- `DotNetCore.Collections.Paginable.SolidPageExtensions` (**SqlSugar package**) — receiver `SqlSugar.ISugarQueryable<T>` (parameter name `query`).
- `PaginableSqlSugarQuery<T>` (paginable collection), `SqlSugarPage<T>` (page).
- Verbs: `ToPaginable` / `ToPaginableAsync` / `GetPage` / `GetPageAsync`.
- Core contracts: `IPage<T>` / `IPageMember<T>` (`Value` / `Offset` / `ItemNumber`).

## Notes and pitfalls

- **Upgrading 6.5→6.6 may newly throw `OperationCanceledException`** (the 6.6.0 fix, not a regression).
- **Mid-flight cancellation is not promised**: SqlSugar 5.1.3's `CountAsync` / `ToPageListAsync` have no token overloads.
- **No netstandard2.0**; net4* uses `sqlSugar`, others use `sqlSugarCore`.
- **Same-named `SolidPageExtensions`** (6.2.0); out of range throws `ArgumentOutOfRangeException`.
