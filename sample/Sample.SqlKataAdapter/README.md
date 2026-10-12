# Sample.SqlKataAdapter

> **English** | [中文](README.zh-CN.md)

> **SqlKata adapter entry surface: non-generic Query and explicit type arguments** — `SolidPageExtensions` + `PaginableSqlKataQuery<T>` + `SqlKataPage<T>` (`DotNetCore.Collections.Paginable.SqlKata`, 3.2.0).

## What this sample demonstrates

What is special about the SqlKata adapter is that its **source type is non-generic** (`SqlKata.Query`), so the element type cannot be inferred, and `ToPaginable<T>` / `GetPage<T>` **must spell out `<T>` explicitly** (the generator's `SourceTypeIsNonGeneric = true`). This sample is a **package-backed explanatory** example: it references the **real SqlKata adapter package**, uses reflection to read the entry types and the public verbs (including generic parameters) of `SolidPageExtensions` from the real assembly, and clarifies the explicit `<T>` and the dependency drift by TFM (net451→2.2.0 / net8-10→4.0.1 / others→3.2.3). It does not connect to a database; at the end it uses the core `Paginable.CreatePage` to build a real page, proving the page object reuses the core contract `IPage<T>`.

## When to use it

- `SqlKata.Query` + an executor for data access, when you want to wire the pagination verbs into SqlKata.
- Sharing the same verbs across TFMs (note that the SqlKata major version changes with the TFM, so behavior differences must be tested per TFM).
- Should not use: when you need generic source inference → write your own `ToPaginable<T>`; EF Core stack → see `Sample.EfCoreAdapter`.

## How to run

```bash
dotnet run --project sample/Sample.SqlKataAdapter
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Real adapter assembly and entry types | `PaginableSqlKataQuery<T>` / `SqlKataPage<T>` / `SolidPageExtensions` (reflection) |
| 2 | Real public verbs (including generic parameters) | `ToPaginable<T>` / `GetPage<T>` / `GetPageAsync<T>` (reflection) |
| 3 | Non-generic source → explicit `<T>` | `query.ToPaginable<Product>(20)` |
| 4 | Shared core contract | `Paginable.CreatePage` → `IPage<T>` / `IPageMember<T>` |
| 5 | TFM drift and why it is not really executed | SqlKata 2/3/4 lines; `SqlKataHelper` hits the database |

## Key APIs

- `DotNetCore.Collections.Paginable.SolidPageExtensions` (**SqlKata package**) — receiver `SqlKata.Query` (non-generic, parameter name `query`).
- `ToPaginable<T>` / `GetPage<T>` / `GetPageAsync<T>` — **explicit** element type.
- `PaginableSqlKataQuery<T>` (paginable collection), `SqlKataPage<T>` (page).
- Core contracts: `IPage<T>` / `IPageMember<T>` (`Value` / `Offset` / `ItemNumber`).

## Notes and pitfalls

- **Explicit generic argument**: `query.ToPaginable<Product>(20)`, you cannot write `query.ToPaginable(20)`.
- **The SqlKata major version changes with the TFM**: net451→2.2.0; net8/9/10→4.0.1; others→3.2.3.
- **Transitive dependencies are pinned** (6.2.0) as a security fix, not an API change.
- **Same-named `SolidPageExtensions`** (6.2.0); net451 has no generator and relies on hand-written members kept in sync.
