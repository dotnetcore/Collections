# Sample.OrderedMultiSet

> **English** | [中文](README.zh-CN.md)

> **Ordered multiset OrderedMultiList&lt;T&gt;** — an ordered bag: duplicates allowed, copy counts tracked, enumeration in ascending order (`DotNetCore.Collections.Multi`, 6.2.0).

## What this sample demonstrates

`OrderedMultiList<T>` is the ordered counterpart of `MultiList<T>`: the copy-counting semantics are identical line for line (one distinct element carrying N copies, enumeration expands copies), but it additionally provides a **total order** — enumeration is ascending, and lookup / insert / delete are worst-case O(log n). This sample shows four facets: ordered counting and ascending enumeration, batch copies and deletion, how the comparer defines both **order and identity**, and why `IList` writes are restricted and structural equality is not overridden.

## When to use it

- You need both counting and **ordered output** (ordered frequencies, leaderboards, lexicographic reports).
- You need O(log n) extremes such as `GetFirst` / `GetLast`, or subsequent rank / range reads (see 6.2.0 / 6.5.0).
- Do not use it for: pure unordered counting hot spots → `MultiList<T>` (O(1), faster); small-domain histograms → `PackedBag<T>`.

## How to run

```bash
dotnet run --project sample/Sample.OrderedMultiSet
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Ordered counting and ascending enumeration | `Add(T)` / `CountOf` / `TotalCount` / `DistinctCount` / `GetFirst` / `GetLast` |
| 2 | Batch copies and deletion | `Add(T, times)` / `Remove(T, times)` (returns remaining copies) / `RemoveAllCopies` |
| 3 | Comparer defines order and identity | `new OrderedMultiList<T>(IComparer<T>)` / `StringComparer.OrdinalIgnoreCase` |
| 4 | Structural equality and `IList` write restrictions | `Equals` (not overridden) / `IsSubsetOf` / `Insert` / indexer assignment throws `NotSupportedException` |

## Key APIs

- `OrderedMultiList<T>` — ordered multiset; the constructor accepts an `IComparer<T>` (which defines order and identity).
- `Add(T)` / `Add(T, int times)` — add 1 copy / add multiple copies; `times <= 0` throws `ArgumentOutOfRangeException`.
- `Remove(T)` / `Remove(T, int times)` — returns the **remaining** copy count; `RemoveAllCopies(T)` returns `bool`.
- `CountOf` / `TotalCount` / `DistinctCount` — copies of one element / all copies / distinct element count.
- `GetFirst` / `GetLast` — O(log n) extremes; an empty collection throws `InvalidOperationException`.
- `Insert(int, T)` — only accepts a position inside a run of equal elements; indexer `this[int]` assignment throws `NotSupportedException`.
- `IsSubsetOf` / `IsSupersetOf` — multiset-semantics content comparison (`Equals` is not overridden).

## Notes and pitfalls

- **`Count` is the copy count**: `((IReadOnlyCollection<T>)shelf).Count == TotalCount`; use `DistinctCount` for the distinct element count.
- **The comparer is not an equality comparer**: this type wants an `IComparer<T>` (a total order); a comparison of `0` means the same element, and the one added first is retained.
- **Structural equality is not overridden**: two `OrderedMultiList`s with identical content are still different objects; use `IsSubsetOf` + `IsSupersetOf` or compare `EntrySet`.
- **`IList` writes are restricted**: `Insert` only accepts a position inside a run of equal elements (other positions throw `ArgumentOutOfRangeException`), and indexer assignment throws `NotSupportedException`.
