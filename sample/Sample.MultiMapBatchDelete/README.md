# Sample.MultiMapBatchDelete

> **English** | [中文](README.zh-CN.md)

> **Multimap batch delete** — `MultiDictionary<TKey,TValue>.RemoveRange` removes values by key in bulk (`DotNetCore.Collections.Multi`, 6.1.0).

## What this sample demonstrates

`RemoveRange(TKey, IEnumerable<TValue>)` is the bulk form of `Remove(key, value)`: for **each distinct value** in the
argument collection it removes one occurrence under the key. This sample shows three facets: bulk deletion and its
return-value semantics, the set (de-duplicating) semantics of the argument, and a contrast with `ExceptWith`. The key
point is to distinguish them — `RemoveRange` removes only **one occurrence** per distinct value, whereas `ExceptWith`
removes **all occurrences** of matching values.

## When to use it

- Removing a batch of values by tag / set: one call removes several values, without hand-writing a `Remove` loop.
- Set-difference-style cleanup: in combination with `ExceptWith`.
- When not to use it: to remove all occurrences of a value → `ExceptWith(key, values)`; to remove an entire key → `Remove(key)`.

## How to run

```bash
dotnet run --project sample/Sample.MultiMapBatchDelete
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Bulk delete values, `bool` return, missing-key no-op | `RemoveRange(TKey, IEnumerable<TValue>)` |
| 2 | Set (de-duplicating) semantics of the argument | `RemoveRange` (argument de-duplicated first) |
| 3 | Contrast with removing all occurrences | `RemoveRange` vs `ExceptWith` |

## Key APIs

- `MultiDictionary<TKey,TValue>.RemoveRange(TKey, IEnumerable<TValue>)` — after de-duplicating the argument, remove one occurrence of each distinct value; returns whether at least one was removed.
- `MultiDictionary<TKey,TValue>.ExceptWith(TKey, IEnumerable<TValue>)` — remove **all** occurrences of matching values.
- `MultiDictionary<TKey,TValue>.ValueCount(TKey)` — the current value count of that key, used to verify the deletion.

## Notes and pitfalls

- **The argument has set semantics**: duplicate values in the argument are not counted twice; it is first de-duplicated via `DistinctValuesOf`, then removed one by one.
- **With duplicate values allowed, only 1 copy is removed**: when `allowDuplicateValues: true`, N copies of the same value lose only 1 copy; to remove them all use `ExceptWith`.
- **Named `RemoveRange` rather than overloading `Remove`**: this avoids the `CS0121` ambiguity of `map.Remove(key, null)` (removing a stored `null` value).
- **A missing key / empty argument / no matches are all no-ops that return `false`**, and they do not throw.
- **The no-empty-key invariant is preserved**: once the last value under a key is removed, the key disappears automatically.
