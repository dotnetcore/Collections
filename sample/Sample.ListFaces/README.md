# Sample.ListFaces

> **English** | [中文](README.zh-CN.md)

> **List interface surface** — `OrderedMultiList<T>`'s `IList<T>` / `IReadOnlyList<T>` implementations and narrowed writes (feature F25, `DotNetCore.Collections.Multi`, 6.5.0).

## What this sample demonstrates

`OrderedMultiList<T>` fully implements `IReadOnlyList<T>` on top of copy counting and a total order: positional access targets the
**copy-expanded** sequence, `this[int]` is another name for `GetByRank`, and `IndexOf(T)` is `GetRank(T)`, both O(log n). `IList<T>`
is also implemented, but the comparer decides element membership, so "placement" members are **narrowed**: `Insert` accepts only
slots within a run of equal elements, and the indexer setter throws `NotSupportedException`. The sample shows three facets: using it
as an `IReadOnlyList`, the narrowed `IList` writes, and a comparison with `MultiList` / `Deque`.

## When to use it

- A downstream that needs `IReadOnlyList<T>` (serializers, binding, LINQ): pass `OrderedMultiList` directly.
- Ordered + positional reads: use ranks and indices together.
- Do not use for: inserting at an arbitrary index (use `List<T>` / `Deque<T>`); overwriting by index (use `List<T>` / `Deque<T>`).

## How to run

```bash
dotnet run --project sample/Sample.ListFaces
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Use it as an `IReadOnlyList` | `IReadOnlyList<T>.Count` / indexer / `IndexOf` / `this[int]` / `IsReadOnly` |
| 2 | Narrowed `IList` placement surface | `Insert(int,T)` / indexer setter throws `NotSupportedException` / `RemoveAt` / `Remove` / `RemoveAllCopies` |
| 3 | Compare with `MultiList` / `Deque` | `is IReadOnlyList<T>` test |

## Key APIs

- `OrderedMultiList<T>` — implements `IReadOnlyList<T>` and `IList<T>`; `Count` = the **copy count** (read through the interface).
- `OrderedMultiList<T>.this[int]` — equivalent to `GetByRank` (a position in the expanded sequence, O(log n)).
- `OrderedMultiList<T>.IndexOf(T)` — equivalent to `GetRank` (the rank of the first copy); returns `-1` if missing.
- `OrderedMultiList<T>.Insert(int,T)` — accepts only slots **within a run of equal elements**; other positions throw `ArgumentOutOfRangeException`.
- `OrderedMultiList<T>.RemoveAt(int)` / `Remove(T)` / `RemoveAllCopies(T)` — the removal surface is complete.

## Notes and pitfalls

- **The indexer setter throws `NotSupportedException`**: an ordered sequence forbids writing by index (a write would break the total order).
- **`Insert` only within a run of equal elements**: not an arbitrary position; for arbitrary insertion use `List<T>` / `Deque<T>`.
- **`Count` = the copy count** (4 in this example), not `DistinctCount` (3).
- **`IReadOnlyList<T>` has no `IndexOf`**: `IndexOf` is an `IList<T>` member; call it via the concrete type or a cast to `IList<T>`.
- **`MultiList<T>` deliberately does not implement the list interfaces**: it is unordered with no positional contract; for positional reads use `OrderedMultiList<T>`.
