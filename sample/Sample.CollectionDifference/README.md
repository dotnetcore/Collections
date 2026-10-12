# Sample.CollectionDifference

> **English** | [中文](README.zh-CN.md)

> **Four-way difference CollectionDifference&lt;TKey,TValue&gt;** — slices two bags / two dictionaries into four disjoint partitions (`DotNetCore.Collections.Multi`, 6.9.0).

## What this sample demonstrates

`left.Difference(right)` answers "what changed between these two collections" in one go, slicing the answer into four **mutually exclusive partitions that cover all keys**:
`OnlyInLeft` (left only), `OnlyInRight` (right only), `InCommon` (present on both sides with equal values), and `Differing` (present on both sides but with different values,
mapped to a `(Left, Right)` pair); `AreEqual` is the shortcut for "nothing changed". Two multisets are compared by **copy count** (which elements were added /
removed / had their multiplicity changed), two `IReadOnlyDictionary`s are compared by **mapped value** (which keys were added / removed / had their value changed), with an identical partition shape.

## When to use it

- Reconciliation / diffing: compare two snapshots or two statistics results to quickly locate added, missing, and changed entries.
- Needing "change details" rather than just "are they equal" — `AreEqual` is just a degenerate use of it.
- Not for: needing a **live view** (continuously reflecting later changes) — this library only offers read-only views / snapshots / in-place forms, and `Difference` is a **snapshot**;
  just wanting to know whether two multisets are equal copy by copy → use `AreEqual` directly (or set operations, 6.2.0).

## How to run

```bash
dotnet run --project sample/Sample.CollectionDifference
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Four-way partition of two bags by copy count | `IMultiSet<T>.Difference(IMultiSet<T>)` → `CollectionDifference<T,int>` |
| 2 | Four-way partition of two dictionaries by mapped value | `IReadOnlyDictionary<K,V>.Difference(...)` → `CollectionDifference<K,V>` |
| 3 | Low-level factory, comparers and argument validation | `CollectionDifference<TKey,TValue>.Of` / key comparer / `null` and duplicate keys throw `ArgumentException` |

## Key APIs

- `CollectionDifference<TKey, TValue>.OnlyInLeft` / `OnlyInRight` / `InCommon` — `IReadOnlyDictionary<TKey,TValue>`.
- `CollectionDifference<TKey, TValue>.Differing` — `IReadOnlyDictionary<TKey,(TValue Left, TValue Right)>`.
- `CollectionDifference<TKey, TValue>.AreEqual` — `true` when all three "difference" partitions are empty.
- `IMultiSet<T>.Difference(IMultiSet<T>, IEqualityComparer<T>? = null)` — compares by copy count, with a value type of `int`.
- `IReadOnlyDictionary<K,V>.Difference(..., keyComparer = null, valueComparer = null)` — compares by value; a multimap uses "the set of values per key" as its value.
- `CollectionDifference<TKey,TValue>.Of(left, right, keyComparer = null, valueComparer = null)` — the low-level factory for arbitrary key/value sequences.
- `ToString()` — a one-line summary `left-only n, right-only n, in-common n, differing n`.

## Notes and pitfalls

- **The result is a snapshot**: changing the source collections after construction does not update `Difference`; this library offers no write-through view.
- **`null` keys cannot be represented** (all four partitions are dictionaries) and throw `ArgumentException` (`ParamName` identifies the left/right side), the same policy as `ToDictionary()`.
- **Keys must be unique within one side**: a duplicate key throws `ArgumentException`, because the partitions are keyed by key.
- **Comparers are supplied by the caller**: `IMultiSet<T>` does not expose its internal comparer, so the bag overload of `Difference` requires you to pass one explicitly to treat elements as "the same".
- **Immutable, thread-safe**: once built, a `CollectionDifference` can be shared across threads.
