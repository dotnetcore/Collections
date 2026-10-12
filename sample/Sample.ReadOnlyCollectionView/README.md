# Sample.ReadOnlyCollectionView

> **English** | [中文](README.zh-CN.md)

> **Cross-TFM read-only value-view wrapper** — the read-only value view of `MultiDictionary` (`DotNetCore.Collections.Multi`, 6.4.0).

## What this sample demonstrates

The package has an `internal sealed` `ReadOnlyCollectionView<T>` that exposes an `ICollection<T>` as an `IReadOnlyCollection<T>`. It addresses a specific cross-TFM danger: exposing the inner collection (such as `HashSet<T>`) as an `IReadOnlyCollection<T>` via a **raw cast**, when the framework's `HashSet<T>` neither statically declares nor implements that interface at runtime on net451 / net461, so the raw cast throws `InvalidCastException` exactly on those supported targets. This sample demonstrates **only the user-visible surface** (the indexer / `AsReadOnly`) to show the fixed behavior: readable on all TFMs, a live view, zero-copy.

## When to use it

- Consumers have no direct scenario; this article helps explain "why the read-only value view used to throw / no longer throws on net451".
- For feature usage see 6.0.0 (`MultiDictionary`) / 6.3.0 (`AsReverse`).
- Do not use it for: referencing `ReadOnlyCollectionView` directly in a sample / doc (it is `internal` and will not compile).

## How to run

```bash
dotnet run --project sample/Sample.ReadOnlyCollectionView
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Read-only value view (inner `HashSet`) | `new MultiDictionary<TKey,TValue>(allowDuplicateValues: false)` / `this[key]` |
| 2 | Live view (not a snapshot) | `Count` / enumeration of `this[key]` change with the source |
| 3 | `AsReadOnly` / `ToDictionary` inner views | `AsReadOnly()` / `ToDictionary()` |
| 4 | Cross-TFM notes and pitfalls | Use only `IReadOnlyCollection<T>`, do not reference the internal type |

## Key APIs

- `MultiDictionary<TKey,TValue>(bool allowDuplicateValues)` — with `false` the inner collection is a `HashSet<TValue>`.
- `MultiDictionary<TKey,TValue>.this[TKey]` — returns `IReadOnlyCollection<TValue>`, exposed through the wrapper (a live view).
- `MultiDictionary<TKey,TValue>.AsReadOnly()` — a live read-only view of the whole map.
- `MultiDictionary<TKey,TValue>.ToDictionary()` — an outer snapshot whose inner value sets are still live views.

## Notes and pitfalls

- **Internal type**: `ReadOnlyCollectionView<T>` is `internal`; do not write it into samples / public docs as an API.
- **A live view, not a snapshot**: view contents change after the map changes; use `ToSerializableModel` (6.2.0) to freeze.
- **The problem domain is net451 / net461**: on higher TFMs `HashSet` already implements the interface, but the package wraps uniformly to avoid `#if` forks.
- **`ToDictionary`'s outer layer is a snapshot while its inner layers are live views**: the two semantics differ, do not mix them up.
