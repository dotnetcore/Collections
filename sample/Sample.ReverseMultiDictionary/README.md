# Sample.ReverseMultiDictionary

> **English** | [中文](README.zh-CN.md)

> **Reverse multidictionary ReverseMultiDictionary&lt;V,K&gt;** — an inverted snapshot of `MultiDictionary` (F17, `DotNetCore.Collections.Multi`, 6.3.0; enhanced in 6.7.1).

## What this sample demonstrates

`ReverseMultiDictionary<V,K>` copies out "value → the set of keys holding it" into a **self-contained** instance, answering "which keys store this value?" in O(1). The constructor copies the bindings out of the source map without retaining a reference chain, so the snapshot stays unchanged after the source later changes / is discarded and can be safely serialized. The sample shows the snapshot semantics, the axis-swapped member naming, and how it compares with the `AsReverse` family (live view / ordered copy / independent copy) shipped in 6.7.1.

## When to use it

- Offline inverted-index analysis needing a **self-contained**, serializable inverted index.
- You need the instance to remain **independently mutable** after construction.
- Do not use it for: following the source → use `MultiDictionary.AsReverse()` (a live view, see F18).

## How to run

```bash
dotnet run --project sample/Sample.ReverseMultiDictionary
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Inverted snapshot (copy, does not follow the source) | `new ReverseMultiDictionary<V,K>(source)` / `this[value]` |
| 2 | Axis-swapped naming and missing-value semantics | `ValueCount` / `TotalKeyCount` / `KeyCount` / `ContainsValue` / `ContainsKey` |
| 3 | Independent mutability and invariants | `Add` / `Remove(value, key)` |
| 4 | `AsReverse` family comparison | `MultiDictionary.AsReverse` / `OrderedMultiDictionary.AsReverse` / `BiDictionary.AsReverse` / `TwoKeyDictionary.AsReverse` |

## Key APIs

- `ReverseMultiDictionary<V,K>(MultiDictionary<K,V> source)` — a self-contained snapshot copying the bindings out of the source map.
- `this[V value]` — the set of keys holding that value; a missing value returns an empty collection without throwing.
- `Count` / `ValueCount` — the number of distinct **values** (mirrors `MultiDictionary.KeyCount`).
- `TotalKeyCount` — the total number of key occurrences (mirrors `TotalValueCount`); `KeyCount(V)` — the key count under that value.
- `ContainsValue(V)` — O(1) existence; `ContainsKey(K)` — scans the keys (O(entries)).
- `Add` / `Remove(V)` / `Remove(V,K)` / `Clear` — the snapshot is independently mutable; the "no empty inner set" invariant is mirrored.
- `ToSerializableModel()` / `FromModel` — the serialization model (`MultiDictionaryModel<V,K>`).

## Notes and pitfalls

- **It is a snapshot, not a live view**: source changes are not reflected automatically; for linkage use `MultiDictionary.AsReverse()`.
- **Duplicate values collapse**: storing the same value multiple times under one key lists the key only once in the inverted index.
- **A `null` value becomes a `null` entry** (a dedicated bucket), enumerated last.
- **The single name `AsReverse` has several forms**: `MultiDictionary` / `BiDictionary` return a **live view**, `OrderedMultiDictionary` returns an **ordered copy**, and `TwoKey` / `ThreeKey` return an **independent copy**; read the return type before assuming liveness.
