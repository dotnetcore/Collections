# Sample.PackedBag

> **English** | [中文](README.zh-CN.md)

> **Packed value-type bag** — `PackedBag<T>`: a dense-array histogram bag with box-free storage (feature F21, `DotNetCore.Collections.Multi`, 6.4.0).

## What this sample demonstrates

`PackedBag<T> where T : struct` stores a value-type element's count histogram as a contiguous `(value, count)` struct array:
the storage path is box-free with no per-item object overhead, and a single cache line carries both value and count. The
sample shows three facets: the counts and `EntrySet` of an enumerated histogram, deletion and "deleting the last copy drops
the entry", and bulk / export / clone. It deliberately **omits** set operations / structural equality / custom comparers —
it is a histogram, not a general multi-set.

## When to use it

- Histograms over an enum / small-integer domain (roughly ≤16 distinct values): a dense layout is faster and allocates less than `MultiList<T>`'s dictionary.
- Hot `Add` / `CountOf`: no hashing, no bucket chasing.
- Do not use for: a large / sparse value domain (use `MultiList<T>`); needing set operations (use `MultiList<T>`); reference-type elements.
- Difference from `SpanBag<T>`: the latter additionally requires `unmanaged` and its storage comes from a stack `stackalloc` (see F22).

## How to run

```bash
dotnet run --project sample/Sample.PackedBag
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Enumerated histogram counts and projection | `Add(T)` / `Add(T,int)` / `CountOf` / `TotalCount` / `DistinctCount` / `EntrySet()` |
| 2 | Deletion and dropping the last copy | `Remove(T)` / `Remove(T,int)` / `RemoveAllCopies(T)` / `Contains` |
| 3 | Bulk, export and clone | `AddRange` / `ToDictionary()` / `DistinctItems()` / `Clone()` |
| 4 | Value-type boundary | `where T : struct` (reference types rejected at compile time) |

## Key APIs

- `PackedBag<T>` — `where T : struct`; `Add` / `CountOf` / `TotalCount` / `DistinctCount` mirror `MultiList<T>`'s semantics.
- `PackedBag<T>.Remove(T)` / `Remove(T,int)` — removes one / multiple copies, returning the **remaining copy count** (not a `bool`).
- `PackedBag<T>.RemoveAllCopies(T)` — removes all copies of the element, returning whether anything was actually removed (`bool`).
- `PackedBag<T>.EntrySet()` — a compact `(element, copy count)` histogram; the item count equals `DistinctCount`.
- `PackedBag<T>.ToDictionary()` / `DistinctItems()` / `Clone()` / `TrimExcess()` / `Capacity`.

## Notes and pitfalls

- **`T : struct`**: no `null`, no reference types; the constraint also excludes `Nullable<T>` itself, and comparison is fixed to `EqualityComparer<T>.Default`.
- **Worth it only for roughly ≤16 distinct values**: `IndexOf` linearly scans the distinct elements; beyond ~32 the dictionary O(1) of `MultiList<T>` overtakes it.
- **Deleting the last copy drops the entry**: when the count reaches zero the entry is discarded entirely, keeping the array packed with no zero-count holes.
- **Not thread-safe**: concurrent writes corrupt the internal entry array; external synchronization is required.
