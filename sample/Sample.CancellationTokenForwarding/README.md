# Sample.CancellationTokenForwarding

> **English** | [中文](README.zh-CN.md)

> **Cancellation token forwarding** — the promise of `CancellationToken` differs per package: three semantics — forward / library boundary / unobserved (feature F44, Paginable core / adapters, introduced in 6.0.0, six adapters in 6.5.0, SqlSugar fix in 6.6.0).

## What this sample demonstrates

"Async" does not mean "cancellable" — whether a cancellation token is observed **differs per package**. This sample focuses on the one category that is measurable in the core package: **the in-memory path does not observe the token**. Passing an **already-cancelled** token to `Paginable.CreatePageAsync`, the core `GetPageAsync`, and `ToPaginableAsync` still completes successfully (`RanToCompletion`), because the fragment is already in memory and there is no I/O to await. The sample also uses comments to explain the other two categories (EF Core / FreeSql forward; SqlSugar 6.6 respects at the library boundary) and demonstrates "if you want in-memory cancellation, call `ThrowIfCancellationRequested` yourself in the loop".

## When to use it

- ASP.NET `HttpContext.RequestAborted` + EF Core / FreeSql: real cancellation, able to abort paging I/O.
- SqlSugar 6.6+: an already-cancelled token throws `OperationCanceledException` before the first database round trip.
- Pure in-memory `CreatePageAsync`: **does not cancel** — do not expect it to respond to the token.
- Needing portable cancellation semantics: observe the token yourself in the loop.

## How to run

```bash
dotnet run --project sample/Sample.CancellationTokenForwarding
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | The fragment entry does not observe the token (an already-cancelled token still succeeds) | `Paginable.CreatePageAsync<T>(...)` |
| 2 | The core `IEnumerable` Async does not observe the token | `SolidPageExtensions.GetPageAsync<T>` / `ToPaginableAsync<T>` |
| 3 | Contrast of the three semantics (forward / library boundary / unobserved) | — (described in comments) |
| 4 | Portable cancellation: check the token inside the loop | `CancellationToken.ThrowIfCancellationRequested()` |

## Key APIs

- `Paginable.CreatePageAsync<T>(IEnumerable<T>, PageFragmentInfo, CancellationToken = default)` — the parameter is documented as `unused`; the fragment is already in memory, the task completes synchronously, and it does **not observe** the token.
- `SolidPageExtensions.GetPageAsync<T>(this IEnumerable<T>, int, int, CancellationToken = default)` — the core in-memory shape, **does not observe** the token.
- `SolidPageExtensions.ToPaginableAsync<T>(this IEnumerable<T>, int, int? = null, CancellationToken = default)` — same as above.
- `CancellationToken.ThrowIfCancellationRequested()` — the only correct way to implement cancellation semantics yourself (on the in-memory path).

## Notes and pitfalls

- **The synchronous `GetPage` has no token parameter**: do not look for cancellation on the synchronous surface.
- **In-memory Async is only signature symmetry**, not "cancellable I/O".
- **Exception types**: cancellation is `OperationCanceledException` (including the `TaskCanceledException` subclass); out-of-range is still `ArgumentOutOfRangeException` (F40) — do not mix the catches.
- **Do not assume every adapter is cancellable mid-stream**: check the F44 table + the F50 matrix; it differs per package; after SqlSugar 6.5->6.6 cancellation may start throwing, which is a fix, not a regression.
