# Sample.PageMemberWrapping

> **English** | [中文](README.zh-CN.md)

> **Page member wrapping and global numbering** — each element of a page is wrapped as a `PageMember<T>` (struct): `Value` / `Offset` / `ItemNumber` (`DotNetCore.Collections.Paginable`, 6.0.0–6.1.0).

## What this sample demonstrates

What `IPage<T>` enumerates is not a bare element but a `PageMember<T>` wrapper carrying three semantics: `Value` (the element value), `Offset` (0-based within the page), and `ItemNumber` (the **global 1-based number** = `startIndex + offset + 1`). This sample shows: the `ItemNumber` of page 3's first item (page size 5) is 11, not 1; the factory `PageMemberFactory`'s two constructions — the **value form** and the **state form**; the state form holding an `IQueryEntryState<T>` whose `Value` is read lazily via `AllValues.ElementAt(offset)`; and the struct copy semantics plus the `offset < 0` construction throw.

## When to use it

- A table's "index" column: read `ItemNumber` directly, continuous across pages.
- Lazy reads for large pages: the state form avoids materializing early (paired with 6.0.0).
- Not for: treating it as a mutable list element — it is a struct wrapper, and changing `Value` does not write back to the source.

## How to run

```bash
dotnet run --project sample/Sample.PageMemberWrapping
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Global number / in-page offset | `IPage<T>` indexer → `IPageMember<T>.ItemNumber` / `.Offset` / `.Value` |
| 2 | Value-form factory construction | `PageMemberFactory.Create<T>(T, int, ref int)` |
| 3 | State form (lazy value read) | `PageMemberFactory.Create<T>(IQueryEntryState<T>, int, ref int)` |
| 4 | Struct semantics / `offset < 0` throw | `PageMember<T>` / `ArgumentOutOfRangeException` |

## Key APIs

- `PageMember<T> : IPageMember<T>` — a `struct`; `Value` / `Offset` / `ItemNumber`.
- `IPageMember<out T>` — the read-only interface: `Value` / `Offset` / `ItemNumber`.
- `PageMemberFactory.Create<T>(T memberValue, int offset, ref int startIndex)` — the value form.
- `PageMemberFactory.Create<T>(IQueryEntryState<T> state, int offset, ref int startIndex)` — the state form, whose `Value` is read via `AllValues.ElementAt(_offset)`.
- `PageMemberFactory` lives in the `DotNetCore.Collections.Paginable.Internal` namespace, but the type is `public`.

## Notes and pitfalls

- **`ItemNumber` is a 1-based global number**, not the in-page index; within a page use `Offset` or the indexer.
- **The state form's `Value` may re-query every time** (O(n) for non-list sources); avoid repeated reads on hot paths.
- **Struct: copy semantics** — changing the wrapper does not write back to the source collection.
- **`offset < 0` construction throws** `ArgumentOutOfRangeException` (`ParamName = "offset"`).
