# Sample.MultiDictionaryExtensions

> **English** | [中文](README.zh-CN.md)

> **Building multimaps from existing sources** — `IndexBy` / `UniqueIndexBy` / `AsMultiDictionary` (`DotNetCore.Collections.Multi`, 6.8.0).

## What this sample demonstrates

6.8.0 added "from a sequence / from a dictionary" entry points to multimaps: `IndexBy(keySelector)` groups a sequence into a `MultiDictionary`
(keeping duplicates, list semantics); `UniqueIndexBy(keySelector)` indexes a sequence into a `BiDictionary` (keys must be unique, duplicates throw);
`AsMultiDictionary()` adapts an `IReadOnlyDictionary` into a `MultiDictionary` (exactly one value per key).
This sample demonstrates the semantics of each of the three entry points, and the convention they share — **the returned value is always an independent copy, not a live view**.

## When to use it

- Building a one-to-many index from a set of in-memory objects: `IndexBy`, e.g. grouping by first letter / country / type.
- Building a strict one-to-one lookup table: `UniqueIndexBy`, turning "keys must be unique" into a check at the entry point.
- Upgrading an existing `Dictionary<TKey,TValue>` into a multi-valued multimap: `AsMultiDictionary`.
- Not for: needing the result to track the source live → all three entry points are copies; refresh them yourself.

## How to run

```bash
dotnet run --project sample/Sample.MultiDictionaryExtensions
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Group a sequence by key (duplicates kept) | `IndexBy(TSource, TKey)` / comparer overload |
| 2 | Unique index of a sequence (throws on duplicate key) | `UniqueIndexBy` / `AsReverse()` |
| 3 | Adapt a dictionary into a multimap (independent copy) | `AsMultiDictionary()` |

## Key APIs

- `IEnumerable<TSource>.IndexBy(keySelector)` / `IndexBy(keySelector, keyComparer)` — returns `MultiDictionary<TKey, TSource>`, keeping duplicates (list semantics).
- `IEnumerable<TSource>.UniqueIndexBy(keySelector)` / `UniqueIndexBy(keySelector, keyComparer)` — returns `BiDictionary<TKey, TSource>`; a duplicate key throws `ArgumentException`.
- `IReadOnlyDictionary<TKey,TValue>.AsMultiDictionary()` — returns `MultiDictionary<TKey, TValue>`, with exactly one value per key.

## Notes and pitfalls

- **All are independent copies, not live views**: changes to the result don't affect the source, and source changes aren't reflected in the result; call again for the latest data.
- **`IndexBy` keeps duplicates**: repeated elements under the same key are all kept (list semantics), unlike set-semantics indexing.
- **`UniqueIndexBy` is stricter due to the bijection constraint**: a duplicate key throws; two equal elements also throw (`BiDictionary` is strictly one-to-one).
- **`AsMultiDictionary`'s comparer is carried over only conditionally**: when the source is a `Dictionary<TKey,TValue>` its `Comparer` is reused; other implementations use `EqualityComparer<TKey>.Default` (`IReadOnlyDictionary` does not expose a comparer).
