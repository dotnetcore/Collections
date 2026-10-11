# Sample.OrmAdapterMatrix

> **English** | [中文](README.zh-CN.md)

> **ORM adapter isomorphic matrix** — a cross-reference of the public surfaces and entry-point names of the 9 ORM integration packages (feature F50, `DotNetCore.Collections.Paginable.*`, introduced package by package across 2.x–6.6).

## What this sample demonstrates

The nine adapter packages wire **the same set of pagination verbs** into different ORMs, but **the entry type names differ from package to package**. This sample prints a cross-reference table row by row — "package → paginable collection type → page type → note" — and separately spells out the three exceptions that are most easily tripped up by "analogy": NHibernate's `NhCorePage<T>`, the fact that EF/EF Core/FreeSql.DbContext only have `SolidPageExtensions`, and SqlKata's non-generic `Query`. It is an **explanatory** sample: it references only the pagination core package and pulls in no ORM package, so there is no restore/connect-to-database risk.

## When to use it

- Selection: confirm which NuGet package corresponds to your ORM and what the entry type is called.
- When writing docs/tests, verify entry names to avoid inventing types such as `PaginableEfCoreQuery<T>` that do not exist.
- Auditing: compare per-package differences in async, cancellation, and TFM (see F44 / F57).
- Should not use: when you want to actually run pagination once → see `Sample.EfCoreAdapter` (true async) or `Sample.SerializationEntry` (fragment rebuild).

## How to run

```bash
dotnet run --project sample/Sample.OrmAdapterMatrix
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Nine-package entry-name cross-reference matrix | Each package's `PaginableXxxQuery<T>` / `XxxPage<T>` (string cross-reference) |
| 2 | Unified verbs | `ToPaginable` / `GetPage` / `*Async` (receiver type differs per package) |
| 3 | Naming rule and three exceptions | NHibernate `NhCorePage<T>`, EF has no standalone Query/Page, SqlKata explicit `<T>` |
| 4 | Same-name disambiguation | 10 same-named `SolidPageExtensions`, need full qualification or alias |

## Key APIs

- Core contracts (shared by all adapters): `IPage`, `IPage<T>`, `IPaginable`, `IPaginable<T>`.
- Per-package entry type names (**must not be extrapolated across packages**): `PaginableFreeSqlQuery<T>` / `FreeSqlPage<T>`, `PaginableSqlSugarQuery<T>` / `SqlSugarPage<T>`, `PaginableSqlKataQuery<T>` / `SqlKataPage<T>`, `PaginableChloeQuery<T>` / `ChloePage<T>`, `PaginableDosQuery<T>` / `DosPage<T>`, `PaginableNhCoreQuery<T>` / `NhCorePage<T>`.
- EF / EF Core / FreeSql.DbContext: **only** `SolidPageExtensions`; EF Core additionally has `KeysetPageAsyncExtensions`.

## Notes and pitfalls

- **Entry names differ from package to package**; consult the matrix, do not fabricate types by following a naming rule.
- **`NhCorePage<T>`** is not `NHibernatePage<T>`; the paginable collection is `PaginableNhCoreQuery<T>`.
- **`SolidPageExtensions` is same-named in 10 places**: any mention must carry the package name / namespace; mixing two packages causes CS0104 (U27).
- **SqlKata's source type is non-generic**: `ToPaginable<T>` / `GetPage<T>` must spell out the element type explicitly.
