# Sample.SecondaryReverseIndex

> **English** | [中文](README.zh-CN.md)

> **Secondary-axis reverse index** — second-axis queries on `TwoKeyDictionary&lt;K1,K2,V&gt;` (`DotNetCore.Collections.Multi`, 6.2.0).

## What this sample demonstrates

The underlying trie of `TwoKeyDictionary<K1,K2,V>` indexes by `(K1, K2)`, so only the **first axis** is a prefix and resolvable by a subtree descent; the second axis is not a prefix and cannot be handled by walking the tree. This feature maintains a **secondary-axis reverse index** (`K2 → HashSet<K1>`) alongside the trie, turning `GetBySecondKey` / `CountOfSecondKey` / `ContainsSecondKey` / `RemoveBySecondKey` from a full-table scan into a single lookup. The index is kept in sync on every write path.

## When to use it

- **Slicing a 2-D table by column** (taking a slice by the second key) without scanning the whole table.
- **Batch deletion by the second key** (`RemoveBySecondKey` clears all entries for that second key at once).
- Do not use it for: lookups by the full key only (no need for this index); second / third-axis slicing of a three-key map (change key order or use `MultiKeyDictionary`).

## How to run

```bash
dotnet run --project sample/Sample.SecondaryReverseIndex
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Second-axis slice (single lookup) | `GetBySecondKey` / `CountOfSecondKey` / `ContainsSecondKey` |
| 2 | Batch deletion by the second axis | `RemoveBySecondKey` |
| 3 | `Keys2` order and the null second component | `Keys2` / `ContainsSecondKey(null)` |

## Key APIs

- `TwoKeyDictionary<K1,K2,V>.GetBySecondKey(K2)` — a second-axis slice, located via the reverse index, O(1) to start.
- `TwoKeyDictionary<K1,K2,V>.CountOfSecondKey(K2)` — the number of entries under that second key.
- `TwoKeyDictionary<K1,K2,V>.ContainsSecondKey(K2)` — whether that second key exists (O(1)).
- `TwoKeyDictionary<K1,K2,V>.RemoveBySecondKey(K2)` — removes all entries for that second key, returning the deleted count.
- `TwoKeyDictionary<K1,K2,V>.Keys2` — the second-axis projection; it deliberately walks the trie, preserving **first-appearance** order.

## Notes and pitfalls

- **The index is an internal implementation**, not a public type; do not write it into a sample expecting it to compile.
- **`Keys2` is not sorted**: the order is first-appearance, not lexicographic; sort it yourself if you need order.
- **Write paths do one extra dictionary operation**: a workload of pure full-key lookups might prefer `MultiKeyDictionary`.
- **A `null` second component goes to a dedicated bucket** (`Dictionary` rejects null keys), consistent with the null-bucket policy of `MultiList` / `MultiKeyDictionary`.
