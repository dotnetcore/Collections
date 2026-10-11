# Sample.OtherOrmAdapters

> **English** | [中文](README.zh-CN.md)

> **Chloe / DosOrm / NHibernate three-package entry-name cross-reference** — type names differ per package, unique overloads and constraints (feature F55, `DotNetCore.Collections.Paginable.Chloe` / `.DosOrm` / `.NHibernate`, the 2.x line).

## What this sample demonstrates

The public surfaces of these three adapters are **not fully symmetric in naming**: Chloe is `PaginableChloeQuery<T>` / `ChloePage<T>`, DosOrm is `PaginableDosQuery<T>` / `DosPage<T>`, while NHibernate's page type is **`NhCorePage<T>`** (not `NHibernatePage<T>`). This sample uses a cross-reference table to print the entry names, source types, and unique overloads of the three packages, and clarifies two error-prone points: `additionalQueryFunc` (the query-transform hook of Chloe / Dos) and DosOrm's `where T : Entity` constraint. It is an **explanatory** example: it references only the pagination core package and pulls in no ORM package, so there is no restore/connect-to-database risk.

## When to use it

- Selection: confirm the package and entry type names of Chloe / Dos.ORM / NHibernate.
- When you need to "append sorting before fetching a page" → `additionalQueryFunc` (Chloe / Dos).
- NHibernate: when you want to build a collection directly from `ISession` → `session.ToPaginable<T>(...)`.
- Should not use: EF Core → see `Sample.EfCoreAdapter`.

## How to run

```bash
dotnet run --project sample/Sample.OtherOrmAdapters
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Three-package entry-name cross-reference table | `PaginableChloeQuery<T>` / `PaginableDosQuery<T>` / `PaginableNhCoreQuery<T>` (string cross-reference) |
| 2 | Unique overloads and constraints | `additionalQueryFunc`; `ISession` overload; `where T : Entity` |
| 3 | Naming rule and exception | NHibernate `NhCorePage<T>` (not `NHibernatePage<T>`) |
| 4 | Shared core contract | `Paginable.CreatePage` → `IPage<T>` / `IPageMember<T>` |
| 5 | Same-name disambiguation and TFM | 10 same-named `SolidPageExtensions`; the three packages have no net451 |

## Key APIs

- `DotNetCore.Collections.Paginable.SolidPageExtensions` (**each package**) — receivers `Chloe.IQuery<T>` / `Dos.ORM.FromSection<T>` / `IQueryOver<T>` respectively.
- Entry type names (**must not be extrapolated across packages**): `PaginableChloeQuery<T>` / `ChloePage<T>`, `PaginableDosQuery<T>` / `DosPage<T>`, `PaginableNhCoreQuery<T>` / `NhCorePage<T>`.
- `additionalQueryFunc`: `Func<IQuery<T>, IQuery<T>>` (`FromSection<T>` for Dos), appends a query transform before fetching a page.

## Notes and pitfalls

- **Type names differ per package**; do not write analogies such as `PaginableEfCoreQuery`.
- **`NhCorePage<T>` has a special name**: it is not `NHibernatePage<T>`.
- **Dos's `Entity` constraint**: you cannot `ToPaginable` an arbitrary DTO directly.
- **`additionalQueryFunc` may be null**; do not perform side effects inside it; the three packages' TFM **has no net451** (F57).
