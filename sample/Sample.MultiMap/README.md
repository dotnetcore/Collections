# Sample.MultiMap

> **English** | [中文](README.zh-CN.md)

> **Multimap MultiDictionary&lt;TKey,TValue&gt;** — one key associated with multiple values (multimap) (`DotNetCore.Collections.Multi`, 6.0.0).

## What this sample demonstrates

`MultiDictionary<TKey,TValue>` makes "one key, many values" a first-class semantic, with a single add/remove/query
of a `(key, value)` pair being O(1). This sample shows four facets: adding values and reading counts, batch adding
with per-key set operations, and the most easily misunderstood `Values` semantics. It corrects two high-frequency
misunderstandings — **`Count` is the number of keys, not the total number of values**, and **`Values` is a flat
sequence, not grouped by key**.

## When to use it

- Tags / grouping / one-to-many indexes: one-key-many-values is first-class, so you no longer need to wrap a `Dictionary<TKey, List<TValue>>`.
- Frequent missing-key queries: the indexer returns an empty collection for a missing key, so callers write less try/catch.
- When you need O(1) value existence: `ContainsValue` uses a reverse index (see 6.2.0).
- When not to use it: ordered keys → `OrderedMultiDictionary<TKey,TValue>` (6.2.0); strict one-to-one → `BiDictionary<TLeft,TRight>` (6.3.0);
  composite keys → `MultiKeyDictionary` / `TwoKeyDictionary` (6.4.0 / 6.2.0).

## How to run

```bash
dotnet run --project sample/Sample.MultiMap
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Add values, read counts, missing-key friendliness | `Add` / `Count` / `KeyCount` / `TotalValueCount` / `ValueCount` / indexer / `ContainsKey` |
| 2 | Batch adding and per-key set operations | `AddRange` / `UnionWith` / `RemoveRange` / `ExceptWith` / `Contains` |
| 3 | `Values` flat sequence vs grouped by key | `Values` / `ToDictionary()` |

## Key APIs

- `MultiDictionary<TKey,TValue>.Add(TKey, TValue)` — add a pair; a `null` key throws `ArgumentNullException`, a `null` value is allowed.
- `Count` / `KeyCount` — both are the **number of keys**; `TotalValueCount` — total number of values; `ValueCount(key)` — value count of that key (0 for a missing key).
- `this[TKey]` — the value collection of that key; a missing key returns an empty collection (never `null`, never throws).
- `AddRange(key, values)` — batch add values; `RemoveRange(key, values)` — after de-duplicating the argument, remove one occurrence of each distinct value (returns whether anything was removed).
- `UnionWith` / `IntersectionWith` / `ExceptWith` / `SymmetricExceptWith` — set-semantics operations on the value collection of a key.
- `Values` — the **flat** sequence of all values; `ToDictionary()` — a "key -> value collection" snapshot.

## Notes and pitfalls

- **`Count` is the number of keys, not the total number of values**: with 3 values and 2 keys, `Count == 2`; use `TotalValueCount` for the total.
- **A missing-key indexer returns an empty collection rather than throwing**: `map[key].Count` being 0 does not mean the key exists; use `ContainsKey` / `TryGetValue` when you need to distinguish.
- **`Values` is a flat sequence** (`new` hides `IReadOnlyDictionary.Values`): `foreach` yields a single value; for grouping use `ToDictionary()` / `AsLookup()`.
- **A key without values does not exist**: once the last value is removed the key disappears automatically, so `KeyCount` and `TotalValueCount` never disagree about "empty keys".
- **Not thread-safe**: for concurrency use `ConcurrentMultiDictionary<TKey,TValue>` (6.2.0).
