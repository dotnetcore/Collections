# Sample.ExplicitSerialization

> **English** | [中文](README.zh-CN.md)

> **Explicit serialization models** — `MultiListModel<T>` / `MultiDictionaryModel<TKey,TValue>` snapshots and rebuild (F10, `DotNetCore.Collections.Multi`, 6.2.0; extended to the whole family in 6.7.1).

## What this sample demonstrates

The Multi family provides **explicit serialization models**: `MultiListModel<T>` (two parallel lists of distinct
elements + copy counts) and `MultiDictionaryModel<TKey,TValue>` (a key list + a per-key value list). They are plain
mutable POCOs — public settable properties, no attributes, no interfaces, no base classes — that **do not force
consumers to use any serializer**. This sample shows: bag / multimap round-trips, models that can carry `null`
(which `ToDictionary` cannot), the strict validation of `FromModel`, and the 6.7.1 whole-family enhancements
(a `BiDictionary`-specific model, and `ReverseMultiDictionary` reusing `MultiDictionaryModel`).

## When to use it

- Persisting a bag / multimap: a stable-shaped POCO works for JSON / XML / database rows.
- Cross-process serialization: the library binds no serializer, so the format is your choice.
- Round-trips that must carry `null` elements: the models use parallel lists, treating `null` like any other element.
- When not to use it: if you only need an in-memory snapshot → `Clone` / `ToDictionary`; for automatic JSON integration → write your own converter.

## How to run

```bash
dotnet run --project sample/Sample.ExplicitSerialization
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | `MultiList` snapshot and rebuild | `ToSerializableModel()` / `MultiList<T>.FromModel` / `Items` / `Counts` |
| 2 | `MultiDictionary` snapshot and rebuild | `ToSerializableModel()` / `FromModel` / `Keys` / `Values` |
| 3 | Model carries `null` elements | `ToSerializableModel` / `ToDictionary` (contrast: throws) |
| 4 | Malformed model is rejected | `FromModel` throws `ArgumentException` |
| 5 | 6.7.1 `BiDictionary`-specific model | `BiDictionaryModel.Lefts` / `Rights` / `AsReverse()` |
| 6 | 6.7.1 `ReverseMultiDictionary` reuses the model | `ToSerializableModel()` / `FromModel` |

## Key APIs

- `MultiList<T>.ToSerializableModel()` / `MultiList<T>.FromModel(model, comparer = null)` — snapshot and rebuild of a bag.
- `MultiDictionary<TKey,TValue>.ToSerializableModel()` / `FromModel(model, comparer = null, allowDuplicateValues = true)` — snapshot and rebuild of a multimap.
- `MultiListModel<T>.Items` / `Counts` — two parallel lists; `MultiDictionaryModel<TKey,TValue>.Keys` / `Values` — keys and per-key value lists.
- `BiDictionaryModel<TLeft,TRight>.Lefts` / `Rights` — the bijection-specific model (6.7.1).
- `BiDictionary<TLeft,TRight>.ToSerializableModel()` / `FromModel` — bijection round-trip.
- `ReverseMultiDictionary<V,K>.ToSerializableModel()` / `FromModel(model, comparer = null)` — the index axis (values) plays the model's `Keys` (6.7.1).

## Notes and pitfalls

- **The comparer is not part of the model**: `FromModel` must be given it again (or use the default); after rebuilding, `Contains` behavior may change.
- **`FromModel` validates strictly**: a `null` list, mismatched list lengths, a copy count ≤ 0, or a `null` inner list all throw `ArgumentException`.
- **`ToDictionary` throws** `InvalidOperationException` when a `null` element is present (`null` cannot be a dictionary key); use the model when you need `null`.
- **The model is a snapshot, not a live view**: the inner lists of `MultiDictionaryModel.Values` are independent copies.
- **`ReverseMultiDictionary`'s stored keys form a set**: on rebuild the inner order is not preserved and duplicate keys collapse.
