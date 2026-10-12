# Sample.AsyncPaging

> **English** | [中文](README.zh-CN.md)

> **End-to-end async paging (in-memory form)** — the `*Async` on an in-memory source is a synchronously completing "async shape", semantically different from true I/O async (`DotNetCore.Collections.Paginable`, 6.0.0 / 6.2.0).

## What this sample demonstrates

Paginable's async surface has two tiers: true end-to-end async (EF Core / FreeSql / SqlSugar `GetPageAsync` / `ToPaginableAsync`, going through the provider's native `CountAsync` + `ToListAsync`) and the "async shape" (the `*Async` on an in-memory `IEnumerable` and `Paginable.CreatePageAsync`, wrapped by `Task.FromResult` and **completing synchronously**). This sample demonstrates the in-memory form only: `await`-ing a paging call on an in-memory list, rebuilding a fragment page with `Paginable.CreatePageAsync`, and the two key semantics "the cancellation token is not observed" and "validation throws synchronously". It uses an in-memory source throughout and pulls in no third-party package.

## When to use it

- When an async call chain mixes in a "fragment path", use the same `Task<IPage<T>>` signature without changing signatures.
- A unified `await` call style with `GetPageAsync`, reducing if/else branches.
- For purely synchronous fragment page building: use `CreatePage` directly, which is more honest.
- When true cancellation / thread yield is needed: use the provider async APIs (EF Core / FreeSql / SqlSugar).

## How to run

```bash
dotnet run --project sample/Sample.AsyncPaging
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | In-memory source async shape (completes synchronously) | `DotNetCore.Collections.Paginable.SolidPageExtensions.ToPaginableAsync` / `GetPageAsync` |
| 2 | Fragment async entry rebuilds a page | `Paginable.CreatePageAsync(fragment, pageNumber, pageSize, totalMemberCount)` |
| 3 | A cancelled token does not prevent success | `CreatePageAsync(..., CancellationToken)` |
| 4 | Validation throws synchronously from the call itself | `ArgumentOutOfRangeException` |

## Key APIs

- `Paginable.CreatePageAsync<T>(fragment, pageNumber, pageSize, totalMemberCount, cancellationToken = default)` — fragment page building async shape, **completes synchronously**.
- `SolidPageExtensions.ToPaginableAsync<T>(enumerable, pageSize, limitedMemberCount = null, cancellationToken = default)` — in-memory collection async shape.
- `SolidPageExtensions.GetPageAsync<T>(enumerable, pageNumber, pageSize, cancellationToken = default)` — in-memory page fetch async shape.
- `PaginableEnumerable<T>.PageSize` / `MemberCount` / `PageCount` — collection-level navigation.

## Notes and pitfalls

- **Judge true async by the package and source type, not just whether the method name has `Async`**: the `*Async` on an in-memory `IEnumerable` completes synchronously.
- **The fragment `CreatePageAsync` does not observe `CancellationToken`**: there is no I/O to cancel; the token is only for signature symmetry.
- **Validation throws synchronously**: invalid arguments throw `ArgumentException` / `ArgumentOutOfRangeException` before the `await`, so do not use `catch (AggregateException)` to catch them.
- **`IPage<T>` enumeration is still synchronous**: the library provides no `IAsyncEnumerable`; the page object is `IPage<T> : IEnumerable<IPageMember<T>>`.
- **True I/O async** requires provider extensions (e.g. EF Core `SolidPageExtensions.GetPageAsync`), see 6.0.0.
