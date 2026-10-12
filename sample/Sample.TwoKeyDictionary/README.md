# Sample.TwoKeyDictionary

> **English** | [中文](README.zh-CN.md)

> **Two-key composite dictionary TwoKeyDictionary&lt;K1,K2,V&gt;** — a 2-D dictionary + dual-axis projection (`DotNetCore.Collections.Multi`, 6.2.0; enhanced in 6.7.1 / 6.8.0).

## What this sample demonstrates

The key of `TwoKeyDictionary<K1,K2,V>` is two **heterogeneous** components, mapping to a single `V`. It is a thin ergonomic wrapper over `MultiKeyDictionary` (arity 2): a 2-D indexer `this[k1, k2]` plus per-axis projections `GetByFirstKey` / `GetBySecondKey`. The two components may be of different types, so the components are boxed into `object` and handed to a homogeneous trie. The sample shows dual-axis projection, the secondary-axis reverse index (6.2.0), batch deletion by the second axis, the `Values` / `ContainsValue` / `AsReverse` added in 6.7.1, and the `EntrySet()` added in 6.8.0.

## When to use it

- 2-D lookup tables (FX rate tables, matrix coordinates, (user, role), etc.).
- You need to **project by row / by column** (`GetByFirstKey` / `GetBySecondKey`).
- Do not use it for: one-key-many-values → `MultiDictionary` / `MultiKeyMultiDictionary`; homogeneous keys with zero-boxing hot spots → `MultiKeyDictionary`; three keys → `ThreeKeyDictionary`.

## How to run

```bash
dotnet run --project sample/Sample.TwoKeyDictionary
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | 2-D read/write and dual-axis projection | `this[k1,k2]` / `GetByFirstKey` / `GetBySecondKey` / `CountOfSecondKey` / `ContainsSecondKey` |
| 2 | Batch deletion by the second axis | `RemoveBySecondKey` |
| 3 | 6.7.1 enhancements | `Values` / `ContainsValue` / `AsReverse` |
| 4 | 6.8 EntrySet | `EntrySet()` |

## Key APIs

- `TwoKeyDictionary<K1,K2,V>.this[K1,K2]` — 2-D indexer; reading a missing key throws `KeyNotFoundException`, writing overwrites.
- `GetByFirstKey(K1)` — first-axis prefix slice (trie descent).
- `GetBySecondKey(K2)` / `CountOfSecondKey(K2)` / `ContainsSecondKey(K2)` — second-axis slice (reverse index, O(1) to start).
- `RemoveBySecondKey(K2)` — removes all entries for that second key, returning the deleted count.
- `Values` — 6.7.1: a flat value sequence; `ContainsValue(V)` — 6.7.1: full scan, O(n).
- `AsReverse()` — 6.7.1: an axis-swapped copy `TwoKeyDictionary<K2,K1,V>`; the former second axis becomes the first.
- `EntrySet()` — 6.8: a per-entry projection of `(K1,K2,V)` (`Entries` is obsolete).

## Notes and pitfalls

- **Component boxing**: heterogeneous components go into an `object` trie; for homogeneous-key hot spots use `MultiKeyDictionary`.
- **`Keys2` is not sorted**: it follows the trie's first-appearance order, not lexicographic order.
- **One value per key**: assignment overwrites; it is not a multimap.
- **`Entries` is obsolete** (since 6.8): it still compiles but raises a CS0618 warning; use `EntrySet()` instead.
