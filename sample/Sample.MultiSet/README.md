# Sample.MultiSet

> **English** | [中文](README.zh-CN.md)

> **Multiset MultiList&lt;T&gt;** — an unordered bag: it allows duplicate elements and tracks a **copy count** for each distinct element (`DotNetCore.Collections.Multi`, 6.0.0).

## What this sample demonstrates

`MultiList<T>` is this package's foundational "multiset". It makes "element → copy count" a first-class
semantic, instead of leaving you to hand-roll counting with a `List<T>`. This sample shows four facets:
reading/writing counts, batch adding with copy-expanded enumeration/export, set operations under multiset
semantics, and removal with argument validation. It corrects one high-frequency misunderstanding —
**`Count` is the copy count, not the number of distinct elements**.

## When to use it

- Frequency counting / histograms (large value domain) that need O(1) single-copy add/remove/query.
- When you need an "expanded sequence" (each copy appears once) to feed downstream directly.
- When you need union / intersection / difference multiset operations by **copy count**.
- When not to use it: ordered output → `OrderedMultiList<T>` (6.2.0); small-domain (≤16) histograms → `PackedBag<T>` (6.4.0);
  Top-K frequent queries → `FrequencyPriorityBag<T>` (6.4.0).

## How to run

```bash
dotnet run --project sample/Sample.MultiSet
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Add copies, read copy count and total | `Add(T)` / `Add(T, times)` / `CountOf` / `TotalCount` / `DistinctCount` |
| 2 | Batch add, expanded enumeration and export | `AddRange` / `EntrySet()` / `ToArray()` / `ToList()` / `ToDictionary()` |
| 3 | Multiset union / intersection / difference / symmetric difference | `UnionWith` / `IntersectionWith` / `ExceptWith` / `SymmetricExceptWith` |
| 4 | Removal and non-positive copy count validation | `Remove(T, times)` / `Add(T, 0)` throws `ArgumentOutOfRangeException` |

## Key APIs

- `MultiList<T>.Add(T item)` / `Add(T item, int times)` — add 1 copy / add multiple copies; `times <= 0` throws `ArgumentOutOfRangeException`.
- `CountOf(T)` — the copy count of that element; `TotalCount` — total copies; `DistinctCount` — number of distinct elements.
- `((IReadOnlyCollection<T>)bag).Count` — equals `TotalCount` (copy count); `Count` is an explicit interface implementation, so on the concrete type you must read it through the interface.
- `EntrySet()` — a `(element, copy count)` projection; its item count equals `DistinctCount`.
- `ToArray()` / `ToList()` — expand all copies; `ToDictionary()` — export an "element → copy count" snapshot.
- `UnionWith` takes the **max** copy count, `IntersectionWith` takes the **min**, `ExceptWith` subtracts, `SymmetricExceptWith` takes the absolute difference.

## Notes and pitfalls

- **`Count` is not the number of distinct elements**: `bag.Count == TotalCount` (copy count); for the number of distinct elements use `DistinctCount`.
- **Enumeration order has no contract**: hash-table enumeration order is an implementation detail; do not write assertions that depend on insertion or hash order.
- **`UnionWith` does not add copy counts**: `{x,x} ∪ {x}` yields `{x,x}` (max), not `{x,x,x}`.
- **`ToDictionary()` throws** `InvalidOperationException` on a `null` element (`null` cannot be a dictionary key); to carry `null`, use `ToSerializableModel` (6.2.0).
- **Not thread-safe**: concurrent writes will corrupt the internal count table; use `ConcurrentMultiList<T>` (6.2.0).
