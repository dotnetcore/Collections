# Sample.LiveReverseView

> **English** | [中文](README.zh-CN.md)

> **Live reverse view of a multimap** — `MultiDictionary&lt;TKey,TValue&gt;.AsReverse()` (`DotNetCore.Collections.Multi`, 6.3.0).

## What this sample demonstrates

`MultiDictionary<TKey,TValue>.AsReverse()` returns a **live read-only inverted view** (`IReadOnlyDictionary<TValue, IReadOnlyCollection<TKey>>`): value → the set of keys holding it. Lookups and enumeration reflect later changes to the source map, and no mutating members are exposed. The view reads the reverse index the source already maintains (`_valueIndex` + the null-value bucket) directly, so it is zero-cost to build and O(1) per read. It is the **live half** of the two inverted-index entry points, consistent with the `ReverseMultiDictionary` snapshot (6.3.0) at any moment.

## When to use it

- **Real-time reverse lookup** inside a UI / service: the view changes as soon as the source does, with no rebuild.
- You need to expose the inverted relation zero-copy and read-only safely.
- Do not use it for: export / persistence (use a `ReverseMultiDictionary` snapshot); lock-free reads across threads (external synchronization is needed).

## How to run

```bash
dotnet run --project sample/Sample.LiveReverseView
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Live view changes with the source | `AsReverse()` / `this[value]` |
| 2 | Missing values and collapsed multiplicity | `inverted[value]` (empty collection for a missing value) / the same key repeated is listed once |
| 3 | Read-only safety | static type `IReadOnlyDictionary<...>` / `Count` |
| 4 | Null-value entry | `ContainsKey(null)` / `this[null]` |

## Key APIs

- `MultiDictionary<TKey,TValue>.AsReverse()` — a live read-only inverted view; returns `IReadOnlyDictionary<TValue, IReadOnlyCollection<TKey>>`.
- `inverted[value]` — the set of keys holding that value; a missing value returns an empty collection without throwing.
- `inverted.Count` — the distinct value count; enumeration reflects later source changes.
- A stored `null` value surfaces as a `null` entry **enumerated last**.

## Notes and pitfalls

- **It is a live view that depends on the source surviving**: do not serialize it, and do not hold it long-term after the source is discarded.
- **Not thread-safe**: the view exposes the source's internals without synchronization; multi-threaded reads need external locking or an immutable structure.
- **Collapsed multiplicity**: storing the same value multiple times under one key lists the key only once.
- **Empty collection for a missing value**: consistent with the `MultiDictionary` indexer, it does not throw.
