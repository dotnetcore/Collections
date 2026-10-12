# Sample.MultiKeyMultiDictionary

> **English** | [中文](README.zh-CN.md)

> **Composite-key multimap MultiKeyMultiDictionary&lt;TKey,TValue&gt;** — composite key + one-key-many-values (`DotNetCore.Collections.Multi`, 6.4.0; enhanced in 6.8.0).

## What this sample demonstrates

The key of `MultiKeyMultiDictionary<TKey,TValue>` is a sequence of homogeneous components (`TKey[]`), and each full key maps to a **value set**. It is the combination of `MultiKeyDictionary` (N components → 1 value) and `MultiDictionary` (1 key → N values). Storage is a trie over the key components; each full key holds a value set built by a configurable factory (a `List` by default, preserving duplicates). The sample shows the counting convention of composite-key multi-values, **prefix projection** (a capability inherited from the trie), and the `EntrySet()` added in 6.8.0.

## When to use it

- Hierarchical paths plus multiple tags (composite key + one-key-many-values).
- You need **prefix queries / prefix deletion** (`GetByPrefix` / `RemovePrefix`), which a tuple-key `Dictionary` cannot do.
- Do not use it for: fixed heterogeneous two / three keys → `TwoKey` / `ThreeKey`; single-key O(1) hot spots → `MultiDictionary`.

## How to run

```bash
dotnet run --project sample/Sample.MultiKeyMultiDictionary
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Composite-key multi-values (Count = full key count) | `Add` / `Count` / `TotalValueCount` / `this[key]` |
| 2 | Prefix projection | `CountOfPrefix` / `GetByPrefix` / `GetBranches` |
| 3 | Removing a whole subtree | `RemovePrefix` |
| 4 | 6.8 EntrySet | `EntrySet()` |

## Key APIs

- `MultiKeyMultiDictionary<TKey,TValue>.Add(TKey[], TValue)` — single add / remove = trie walk O(key length) + a set operation.
- `Count` / `KeyCount` — the **full key count**; `TotalValueCount` — the value total.
- `this[TKey[]]` — the value set under that full key; a missing key returns an empty collection without throwing.
- `GetByPrefix(TKey[])` / `CountOfPrefix(TKey[])` / `GetBranches(TKey[])` — prefix projection (subtree traversal).
- `RemovePrefix(TKey[])` — removes a whole subtree, returning the deleted **value** count.
- `EntrySet()` — 6.8: a flat `(TKey[] Key, TValue Value)` projection, one pair per value.

## Notes and pitfalls

- **`Count` is the full key count**, not the value count: use `TotalValueCount` for the value total.
- **A missing-key indexer returns an empty collection** without throwing.
- **A full key is O(key length), not O(1)**: it is a trie walk; for tuple-key O(1) use `MultiDictionary` with a tuple key (losing prefixes).
- **`RemovePrefix` returns the value count**, not the key count; use `CountOfPrefix` first if you need the key count.
