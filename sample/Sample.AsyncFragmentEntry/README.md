# Sample.AsyncFragmentEntry

> **English** | [中文](README.zh-CN.md)

> **Async fragment entry** — `Paginable.CreatePageAsync` is the async-shape dual of fragment page building, **completing synchronously** (feature F35, `DotNetCore.Collections.Paginable`, 6.2.0).

## What this sample demonstrates

`Paginable.CreatePageAsync`'s four overloads correspond one-to-one with `CreatePage` (triple / `PageFragmentInfo`, each optionally with `PageCreationOptions`), plus one `CancellationToken`. It **completes synchronously**: the fragment is already in memory, there is no I/O to await, it is wrapped by `Task.FromResult` and the returned Task is already completed; argument validation throws synchronously from the call itself (not as a faulted task), and `cancellationToken` is only for signature symmetry and is not observed. This sample hides it inside a `Task<IPage<T>>` service method, demonstrating four facets: same shape as true async, Strict validation, the cancellation-semantics misunderstanding, and synchronous throwing.

## When to use it

- Inside an `async Task<IPage<T>>` service method that sometimes hits the database and sometimes a cached page → both sides `await`.
- A unified `await` call style with `GetPageAsync`, reducing if/else branches.
- For purely synchronous fragment page building → use `CreatePage` directly, which is more honest.
- Expecting cancellation to interrupt page building → there is no I/O; switch to a provider async API.

## How to run

```bash
dotnet run --project sample/Sample.AsyncFragmentEntry
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Same signature as true async | `Paginable.CreatePageAsync(fragment, pageNumber, pageSize, totalMemberCount, ct)` |
| 2 | Strict validation overload + Task already completed | `CreatePageAsync(..., options)` / `Task.IsCompleted` |
| 3 | A cancelled token does not prevent success | `CreatePageAsync(..., CancellationToken)` |
| 4 | Validation throws synchronously from the call itself | `ArgumentOutOfRangeException` |

## Key APIs

- `Paginable.CreatePageAsync<T>(fragment, pageNumber, pageSize, totalMemberCount, cancellationToken = default)`.
- `Paginable.CreatePageAsync<T>(fragment, pageNumber, pageSize, totalMemberCount, options, cancellationToken = default)`.
- `Paginable.CreatePageAsync<T>(fragment, PageFragmentInfo[, PageCreationOptions], cancellationToken = default)`.
- `IPage<T>.CurrentPageNumber` / `CurrentPageSize` / `this[int].ItemNumber`.

## Notes and pitfalls

- **Throws synchronously**: do not use `catch (AggregateException)` to wrap a validation failure; `ArgumentException` / `ArgumentOutOfRangeException` are thrown from the call itself.
- **The token is not observed**: the `Async` suffix is a shape, not a cancellation semantic.
- **There is no `CreateSinglePageSetAsync`**: use the synchronous factory for single-page sets (F34).
- **The overload set = `CreatePage` + `CancellationToken`**: including the options variant, behavior is identical to the synchronous `CreatePage`.
