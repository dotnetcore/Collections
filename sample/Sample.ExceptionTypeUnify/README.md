# Sample.ExceptionTypeUnify

> **English** | [中文](README.zh-CN.md)

> **Unified out-of-range exception type** — every paging entry point throws `ArgumentOutOfRangeException` carrying a `ParamName` for out-of-range arguments (feature F40, `DotNetCore.Collections.Paginable`, 6.2.0 breaking change).

## What this sample demonstrates

6.2.0 unified the out-of-range reporting of **all paging entry points** (core `IEnumerable` / `IQueryable` / `Task<IQueryable>`, keyset, and the 9 ORM adapters) from `IndexOutOfRangeException` to `ArgumentOutOfRangeException`, across roughly 40 throw sites. This sample walks through an in-memory source: the exception type and `ParamName` thrown by each of the three rejection conditions (`pageNumber < 1`, `pageSize < 1`, past the last page), the empty source returning an empty page without throwing, and the `ArgumentException` fallback.

## When to use it

- You need a unified `catch` / assertion / logging path for paging out-of-range.
- You are upgrading from 6.1 to 6.2+ and want to globally search for `IndexOutOfRangeException` and replace it.
- You want to rely on `ParamName` pointing directly at the offending parameter for diagnostics.

## How to run

```bash
dotnet run --project sample/Sample.ExceptionTypeUnify
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | `pageNumber < 1` → `ArgumentOutOfRangeException(pageNumber)` | `IEnumerable<T>.GetPage(int, int)` |
| 2 | `pageSize < 1` → `ArgumentOutOfRangeException(pageSize)` | `IEnumerable<T>.GetPage(int, int)` |
| 3 | `pageNumber` past the last page | `IEnumerable<T>.GetPage(int, int)` |
| 4 | Empty source returns a single empty page without throwing | `IEnumerable<T>.GetPage(int, int)` |
| 5 | The fragment entry point is unified the same way | `Paginable.CreatePage<T>(...)` |
| 6 | Derives from `ArgumentException`, so a broader catch works | `ArgumentException` |

## Key APIs

- `SolidPageExtensions.GetPage<T>(this IEnumerable<T>, int pageNumber, int pageSize)` (core package) — throws `ArgumentOutOfRangeException` on out-of-range, with `ParamName` of `"pageNumber"` / `"pageSize"`.
- `Paginable.CreatePage<T>(IEnumerable<T>, int, int, int)` — the fragment entry point; out-of-range throws `ArgumentOutOfRangeException` the same way.
- `ArgumentOutOfRangeException` — derives from `ArgumentException`; `ParamName` is dependable (pinned by 15 tests).

## Notes and pitfalls

- **Breaking**: catch / assertion code that relies on the exact exception type must switch to `ArgumentOutOfRangeException`.
- **Only the type changed, not the conditions**: the rejection conditions are the same as before; **an empty source still returns a single empty page** and does not throw.
- **The old `IndexOutOfRangeException` is not an `ArgumentException`**: to stay compatible with the old 6.1 library during migration, you need a transitional double catch.
- **Do not confuse this with the other Multi-side breaking change** (`times <= 0` throws `ArgumentOutOfRangeException`, unrelated to this document).
