# Sample.QueryEntryState

> **English** | [中文](README.zh-CN.md)

> **The query entry state `IQueryEntryState<T>`** — abstracting "where a page's elements come from" into a covariant interface, used by `PageMember`'s state form for lazy value reads (`DotNetCore.Collections.Paginable`, 6.0.0).

## What this sample demonstrates

`IQueryEntryState<out T>` lives in `DotNetCore.Collections.Paginable.Abstractions` and has **only one member**, `IEnumerable<T> AllValues`. It abstracts "where a page's elements come from" for `PageMember`'s state form (6.0.0) to read lazily — page initialization does not have to materialize all elements into a `List`, and `Value` is only read via `ElementAt(_offset)` when accessed. This sample shows: the interface shape (only `AllValues`), covariance (an `IQueryEntryState<Derived>` can be used as an `IQueryEntryState<Base>`), its use as the source for `PageMember`'s state form, and that `AllValues` may be a lazy sequence (multiple enumerations may mean multiple queries).

## When to use it

- Lazy reads for a query page: the state form `PageMember` avoids allocating early for large pages.
- Implementing a custom page: implement this interface for `PageMemberFactory` to use.
- Not for: replacing `IEnumerable` as a collection — it is only an entry state.

## How to run

```bash
dotnet run --project sample/Sample.QueryEntryState
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Interface shape: only `AllValues` | `IQueryEntryState<T>.AllValues` |
| 2 | Covariant `out T` | `IQueryEntryState<Order>` → `IQueryEntryState<object>` |
| 3 | Used as the source for `PageMember`'s state form | `PageMemberFactory.Create<T>(IQueryEntryState<T>, int, ref int)` |
| 4 | Lazy sequence: multiple enumerations | a custom implementation counting `AllValues` reads |

## Key APIs

- `IQueryEntryState<out T>` (`DotNetCore.Collections.Paginable.Abstractions`) — only `IEnumerable<T> AllValues { get; }`.
- `PageMemberFactory.Create<T>(IQueryEntryState<T> state, int offset, ref int startIndex)` — state-form construction, whose `Value` is read via `AllValues.ElementAt(_offset)`.

## Notes and pitfalls

- **`AllValues` may be a lazy sequence**: multiple enumerations may mean multiple queries (depending on the source).
- **The implementation class `QueryEntryState<T>` is internal**: consumers usually use it indirectly through `PageMember.Value`; the sample uses a custom `IQueryEntryState<T>` implementation to demonstrate.
- **Not a collection type**: do not use it as an `IList`; `ElementAt` is O(n) for non-list sources.
- **Covariant interface**: `out T` makes the type compatible more widely.
- Hot paths should materialize once (6.0.0) to avoid repeatedly reading the state form's `Value`.
