# Sample.SharedInterfaces

> **English** | [中文](README.zh-CN.md)

> **Shared interfaces** — `IMultiSet<T>` / `IMultiDictionary<TKey,TValue>`: the shared read/write abstraction for multi-sets / multi-maps (`DotNetCore.Collections.Multi`, 6.6.0).

## What this sample demonstrates

`IMultiSet<T>` and `IMultiDictionary<TKey,TValue>` encode only the semantics every implementation can honor, so code can be written
against the interface, making it easy to mock, replace, and swap implementations. The sample shows three facets: algorithms written
against `IMultiSet` (the same function taking `MultiList` and `OrderedMultiList`), the `Count` and `Values` semantics of the map, and
the deliberately minimal contract scope. The focus is correcting two high-risk semantics: `IMultiSet.Count` is the **copy count**, and
`IMultiDictionary.Values` is a **flat** sequence.

## When to use it

- A service accepting a multi-set / map: depend on the abstraction to ease replacing the implementation.
- Unit-test mocking: the interface can be substituted.
- Reusing an algorithm across implementations: use only the minimal semantics in the contract.
- Do not use for: needing set operations / comparers — those are not in the contract; fall back to a concrete type.

## How to run

```bash
dotnet run --project sample/Sample.SharedInterfaces
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | An algorithm written against `IMultiSet` | `IMultiSet<T>.Count` / `TotalCount` / `DistinctCount` / `EntrySet()` |
| 2 | Map `Count` and `Values` | `IMultiDictionary<TKey,TValue>.Count` / `KeyCount` / `TotalValueCount` / indexer / `Values` / `ValueCount` |
| 3 | Contract scope (minimal) | set operations fall back to a concrete type; the "no empty key" invariant |

## Key APIs

- `IMultiSet<T>` (inherits `IReadOnlyCollection<T>`) — `Count` = the **copy count** (always equals `TotalCount`); `DistinctCount` is the number of distinct elements.
- `IMultiSet<T>.EntrySet()` / `Contains` / `ContainsAll` / `CountOf` / `Remove` / `RemoveAllCopies` / `DistinctItems`.
- `IMultiDictionary<TKey,TValue>` (inherits `IReadOnlyDictionary<TKey,IReadOnlyCollection<TValue>>`) — `Count` = the **key count** (always equals `KeyCount`).
- `IMultiDictionary<TKey,TValue>.Values` — uses `new` to hide the inherited grouped `Values`, giving a **flat** sequence (length = `TotalValueCount`).
- `IMultiDictionary<TKey,TValue>.KeyCount` / `TotalValueCount` / `ValueCount(TKey)` / `AsLookup()`.

## Notes and pitfalls

- **`IMultiSet.Count` = the copy count**, not `DistinctCount`: with 3 copies of 1 element, `Count == 3`.
- **`IMultiDictionary.Count` = the key count**, not the value count; `TotalValueCount` is the value count.
- **`Values` is a `new` flat sequence**: not a sequence of grouped collections; for grouped reads use the indexer `map[key]` / `Keys`.
- **Set operations / comparers are not on the interface**: concurrent / immutable / `PackedBag` do not all provide set operations.
- **`IMultiSet` does not inherit `ICollection<T>`**: `int Remove(T)` removes one copy and reports the remaining copy count, not `bool Remove`.
- **The interface deliberately avoids double enumeration inheritance**: to avoid `foreach` ambiguity on the interface (`CS0411`).
