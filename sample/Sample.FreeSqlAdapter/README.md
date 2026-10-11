# Sample.FreeSqlAdapter

> **English** | [中文](README.zh-CN.md)

> **FreeSql adapter entry surface** — `SolidPageExtensions` + `PaginableFreeSqlQuery<T>` + `FreeSqlPage<T>` (feature F52, `DotNetCore.Collections.Paginable.FreeSql`, 2.x).

## What this sample demonstrates

The FreeSql adapter wires the pagination verbs into `FreeSql.ISelect<T>`. This sample is a **package-backed explanatory** example: it references the **real FreeSql adapter package** (restorable from the local cache, builds successfully), uses reflection to read the entry types and the public verb signatures of `SolidPageExtensions` from the real assembly, and prints a unified-verb cross-reference together with FreeSql's unique `includeNestedMembers` switch. It **does not connect to a database** — `GetPage` would genuinely hit the database via `FreeSqlHelper`, which is inconvenient for a console example to connect to a real data source, so it only demonstrates the entry surface. At the end it uses the core `Paginable.CreatePage` to build a real page, proving that "no matter which adapter, the page object is the core contract `IPage<T>`".

## When to use it

- Confirm the entry type names and verbs of a FreeSql application (`ToPaginable` / `GetPage` / `*Async`).
- Use the `includeNestedMembers` switch when you need "whether to include nested members".
- DbContext surface → use the `DotNetCore.Collections.Paginable.FreeSql.DbContext` package (**only** `SolidPageExtensions`).
- Should not use: EF Core stack → see `Sample.EfCoreAdapter`.

## How to run

```bash
dotnet run --project sample/Sample.FreeSqlAdapter
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Real adapter assembly and entry types | `PaginableFreeSqlQuery<T>` / `FreeSqlPage<T>` / `SolidPageExtensions` (reflection) |
| 2 | Real public verb signatures | `SolidPageExtensions`'s `ToPaginable` / `GetPage` / `*Async` (reflection) |
| 3 | Unified verbs + unique switch | `includeNestedMembers`; `where T : class` |
| 4 | Shared core contract | `Paginable.CreatePage` → `IPage<T>` / `IPageMember<T>` |
| 5 | Why it is not really executed | `FreeSqlHelper.Count` hits the database |

## Key APIs

- `DotNetCore.Collections.Paginable.SolidPageExtensions` (**FreeSql package**) — receiver `FreeSql.ISelect<T>` (parameter name `select`).
- `PaginableFreeSqlQuery<T>` (paginable collection, `where T : class`), `FreeSqlPage<T>` (page).
- FreeSql-unique: `includeNestedMembers` (whether to include nested members; the semantics are decided by FreeSql).
- Core contracts: `IPage<T>` / `IPageMember<T>` (`Value` / `Offset` / `ItemNumber`).

## Notes and pitfalls

- **`SolidPageExtensions` is same-named**: the adapter and the core both live in the `DotNetCore.Collections.Paginable` namespace, so writing the type name directly causes CS0104 (U27).
- **The DbContext package has no** own factory for `PaginableFreeSqlQuery`; when using the concrete type, still reference the FreeSql package.
- **The semantics of `includeNestedMembers` are decided by FreeSql** (navigation loading), affecting the result shape and cost.
- **net451 dual-track**: net451 keeps the hand-written `#if NET451` members (F56); out of range throws `ArgumentOutOfRangeException`.
