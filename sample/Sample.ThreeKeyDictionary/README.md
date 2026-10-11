# Sample.ThreeKeyDictionary

> **English** | [中文](README.zh-CN.md)

> **Three-key composite dictionary ThreeKeyDictionary&lt;K1,K2,K3,V&gt;** — a composite-key dictionary whose key is three heterogeneous components (F15, `DotNetCore.Collections.Multi`, 6.2.0; enhanced in 6.7.1 / 6.8.0).

## What this sample demonstrates

`ThreeKeyDictionary<K1,K2,K3,V>` is a thin ergonomic wrapper over `MultiKeyDictionary` (arity 3): it provides a triple indexer `this[k1, k2, k3]` and first-axis prefix queries. The trie indexes by `(K1,K2,K3)`, so **only the first axis is a prefix** (second / third-axis slicing falls back to a scan, O(n)). The sample also shows the `Values` / `ContainsValue` / `AsReverse` added in 6.7.1 and the `EntrySet()` added in 6.8.0.

## When to use it

- Three-dimensional configuration / matrix coordinates whose key components are of **heterogeneous** types.
- You need to **slice by the first dimension** (`GetByFirstKey`, a prefix descent).
- Do not use it for: hot queries on the second / third axis (change key order or use `MultiKeyDictionary`); one key with multiple values (use `MultiKeyMultiDictionary`); homogeneous keys with boxing concerns (use `MultiKeyDictionary` directly).

## How to run

```bash
dotnet run --project sample/Sample.ThreeKeyDictionary
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Three-key read/write and first-axis slicing | `this[k1,k2,k3]` / `GetByFirstKey` / `CountOfFirstKey` / `ContainsFirstKey` |
| 2 | Second / third-axis slicing (scan) | `GetBySecondKey` / `CountOfSecondKey` / `CountOfThirdKey` / `ContainsSecondKey` |
| 3 | 6.7.1 enhancements | `Values` / `ContainsValue` / `AsReverse` |
| 4 | 6.8 EntrySet | `EntrySet()` |

## Key APIs

- `ThreeKeyDictionary<K1,K2,K3,V>.this[K1,K2,K3]` — triple indexer; reading a missing key throws `KeyNotFoundException`, writing overwrites.
- `GetByFirstKey(K1)` / `CountOfFirstKey(K1)` / `ContainsFirstKey(K1)` — first-axis prefix queries (subtree descent).
- `GetBySecondKey` / `GetByThirdKey` / `CountOfSecondKey` / `CountOfThirdKey` — second / third-axis slicing (**scan, O(n)**).
- `Values` — 6.7.1: a flat sequence of values (dropping key components); `ContainsValue(V)` — 6.7.1: full scan, O(n).
- `AsReverse()` — 6.7.1: an axis-reversed copy `ThreeKeyDictionary<K3,K2,K1,V>`; the former third axis becomes the first.
- `EntrySet()` — 6.8: a per-entry projection of `(K1,K2,K3,V)` (`Entries` is obsolete).

## Notes and pitfalls

- **Only the first axis is a prefix**: second / third-axis slicing is O(n); do not treat it as an index on hot paths.
- **Component boxing**: heterogeneous components are boxed into an `object` trie; for homogeneous-key hot spots use `MultiKeyDictionary`.
- **`ContainsValue` is a full scan** (O(n)): this type has no value-side index.
- **`Entries` is obsolete** (since 6.8): it still compiles but raises a CS0618 warning; use `EntrySet()` instead.
