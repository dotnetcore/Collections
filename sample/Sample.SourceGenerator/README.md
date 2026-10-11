# Sample.SourceGenerator

> **English** | [中文](README.zh-CN.md)

> **SolidPageExtensions source generator** — the `[SolidPageExtensionsFor]` attribute declaration and the shape of the generated output (feature F56, `DotNetCore.Collections.Paginable.SourceGenerators`, 6.5.0).

## What this sample demonstrates

Since 6.5.0, the pagination extension members of the **six** ORM adapter packages (Chloe, DosOrm, FreeSql, SqlSugar, NHibernate, SqlKata) are emitted by a Roslyn incremental generator, replacing a hand-written pass per package. This sample explains the generator's **attribute declaration form** (`[SolidPageExtensionsFor]`'s 7 positional parameters + key switches), the **shape of the generated output** (`ToPaginable` / `ToPaginableAsync` / `GetPage` / `GetPageAsync` plus type-level XML), as well as the **net451 dual-track** and **consumer visibility**. It is an **explanatory** example: it does not reference the generator project and does not require actually running the generator (the generator is an Analyzer and is not shipped with the consumer package); it references only the pagination core package.

## When to use it

- When you are a **library maintainer**: when adding a new ORM adapter, you only write the attribute declaration + the Helper / Factory.
- When investigating "why the same signature is identical across six packages": a single source of truth prevents drift.
- Should not use: as a **consumer** — you only perceive "the extension methods are still there", you do not see the generator types, and you should not reference the SourceGenerators package.

## How to run

```bash
dotnet run --project sample/Sample.SourceGenerator
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Attribute declaration form | `[SolidPageExtensionsFor(ormName, sourceTypeFullName, sourceParamName, pageTypeName, paginableTypeName, factoryTypeName, helperTypeName)]` |
| 2 | Key attribute switches | `GenericConstraint` / `HasAdditionalQueryFunc` / `HasIncludeNestedMembers` / `HasAsync` / `SourceTypeIsNonGeneric` / `ForwardAsyncCancellationToken` |
| 3 | Shape of the generated output | generates `ToPaginable` / `ToPaginableAsync` / `GetPage` / `GetPageAsync`; hand-written special cases stay in partial |
| 4 | net451 dual-track | FreeSql / SqlKata / SqlSugar keep the `#if NET451` hand-written members on net451 |
| 5 | Consumer visibility | no consumer API change ⇒ no Breaking |

## Key APIs

- `DotNetCore.Collections.Paginable.SolidPageExtensionsForAttribute` — an **internal** attribute injected by the generator's post-init, attached to the partial class.
- `DotNetCore.Collections.Paginable.SourceGenerators.SolidPageExtensionsGenerator` — an `IIncrementalGenerator`.
- The generator project is wired into the adapter packages with `OutputItemType=Analyzer`, `ReferenceOutputAssembly=false`, and **adds no package dependency**.

## Notes and pitfalls

- **Consumers cannot see the generator types**; do not reference the SourceGenerators package as a runtime library.
- **net451 dual-track**: when changing generator attributes, sync the `#if NET451` hand-written surface, otherwise testing only one TFM will miss it.
- **Hand-written special cases still exist** (NHibernate's `ISession` overload, SqlKata's `GetPageAsync`); do not assume it is 100% generated.
- **EF6 / EF Core / FreeSql.DbContext are deliberately not migrated** — they delegate the queryable extensions and were never a duplicated shape to begin with.
