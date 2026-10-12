# Sample.EfCoreAdapter

> **English** | [中文](README.zh-CN.md)

> **EF Core adapter: true async pagination** — `SolidPageExtensions` (EntityFrameworkCore) + keyset async entry point (`DotNetCore.Collections.Paginable.EntityFrameworkCore`, 6.0.0–6.5.0).

## What this sample demonstrates

The EF Core adapter wires the pagination verbs into `IQueryable<T>` / `DbSet<T>`. Its biggest difference from the core package is **true async**: `GetPageAsync` / `ToPaginableAsync` genuinely `await` I/O via the provider's native `CountAsync` / `ToListAsync`, rather than the core package's "shape async" over `IEnumerable` (`Task.FromResult`, completes synchronously, does not observe the token). This sample builds an in-memory `DbContext` with the EF Core **InMemory** provider, runs a full end-to-end async pagination, and demonstrates the direct `DbSet<T>` entry point, the last-page boundary, the out-of-range exception, and same-name extension class disambiguation. Page results reuse the core contract `IPage<T>`.

## When to use it

- The standard pagination path for ASP.NET Core + EF Core, when you need true async / cancellation token forwarding (6.0.0).
- When you need keyset async → there is also `KeysetPageAsyncExtensions` (6.0.0), unique to EF Core.
- Should not use: for a synchronous console utility → the core package's `IEnumerable` entry point suffices (all releases).

## How to run

```bash
dotnet run --project sample/Sample.EfCoreAdapter
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 0 | In-memory `DbContext` (InMemory provider) | `DbContextOptionsBuilder.UseInMemoryDatabase` / `DbSet<T>` |
| 1 | True async page fetch | `IQueryable<T>.GetPageAsync(pageNumber, pageSize)` |
| 2 | True async build a collection then fetch pages repeatedly | `IQueryable<T>.ToPaginableAsync()` / `PaginableSetBase<T>.GetPage` |
| 3 | Direct `DbSet<T>` entry point (synchronous) | `DbSet<T>.GetPage(pageNumber, pageSize)` |
| 4 | True async vs core shape async | explanation + `KeysetPageAsyncExtensions` |
| 5 | Unified out-of-range exception | `GetPageAsync(99, 10)` throws `ArgumentOutOfRangeException` |

## Key APIs

- `DotNetCore.Collections.Paginable.SolidPageExtensions` (**EF Core package**) — `ToPaginable` / `ToPaginableAsync` / `GetPage` / `GetPageAsync`, with a receiver of `IQueryable<T>` or `DbSet<T>`.
- `IQueryable<T>.GetPageAsync(int pageNumber, int pageSize, CancellationToken)` — true async, returns `IPage<T>`.
- `KeysetPageAsyncExtensions` — keyset async, unique to EF Core.
- Return type: the core contract `IPage<T>`; elements are `IPageMember<T>` (`Value` / `Offset` / `ItemNumber`, where `ItemNumber` is the **global** number).

## Notes and pitfalls

- **`SolidPageExtensions` is same-named across packages**: `using` both the core and EF namespaces causes CS0104; disambiguate by calling the extension method, with a fully-qualified name, or with an alias.
- **`ToPaginableAsync(pageSize: 10)` causes CS0121**: the core package `(IEnumerable<T>, int, …)` and the EF package `(IQueryable<T>, int?, …)` each win on one dimension, so the compiler reports ambiguity; only the no-argument call `ToPaginableAsync()` uniquely resolves to the EF package.
- **True async ≠ core shape async**: the core's `*Async` over `IEnumerable` completes synchronously and does not observe the token; do not conflate the two.
- **Keyset async lives in another extension class** `KeysetPageAsyncExtensions`, not on `SolidPageExtensions`.
- **TFM is net6+ only**: the EF Core package has no net451 (6.0.0).
- **Out of range throws `ArgumentOutOfRangeException`**; an empty source returns an empty page (6.2.0).
