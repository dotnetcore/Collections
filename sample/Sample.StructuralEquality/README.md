# Sample.StructuralEquality

> **English** | [中文](README.zh-CN.md)

> **Multiset structural equality** — `MultiList<T>`'s `Equals` / `GetHashCode` (`DotNetCore.Collections.Multi`, 6.1.0).

## What this sample demonstrates

`MultiList<T>` implements `IEquatable<MultiList<T>>`, providing **multiset structural equality**: two bags are equal
if and only if both hold the same distinct elements and each element has the same copy count, independent of
enumeration order. This sample shows three facets: copy counts participating in comparison, equal bags collapsing
into a single entry in a `HashSet`, and two easy-to-get-wrong points — `==` is not overloaded, and `IsSubsetOf` is
not an equality check.

## When to use it

- Asserting bag contents directly in tests: `Assert.Equal(expected, actual)`, without hand-writing an `EntrySet()` loop.
- Caches / de-duplication keyed by a bag: `HashSet<MultiList<T>>`, `Dictionary<MultiList<T>, ...>`.
- When you need order-independent content comparison (equal bags with different insertion order hash the same).
- When not to use it: to distinguish two content-identical instances by reference → keep reference comparison (the default; do not switch to `Equals`).

## How to run

```bash
dotnet run --project sample/Sample.StructuralEquality
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Copy counts participate in comparison (bag equality) | `MultiList<T>.Equals(MultiList<T>)` |
| 2 | Equal bags collapse in a `HashSet` | `GetHashCode()` + `Equals` |
| 3 | `==` is not overloaded, subset test is not equality | `==` (reference comparison) / `IsSubsetOf` / `Equals` |

## Key APIs

- `MultiList<T>.Equals(MultiList<T>)` — structural equality: same distinct elements + same copy counts, order-independent.
- `MultiList<T>.GetHashCode()` — consistent with `Equals`, mixing in `TotalCount` / `DistinctCount` and each element's contribution (XOR).
- `MultiList<T>.IsSubsetOf(IEnumerable<T>)` — multiset subset test (it is **not** an equality check in this package).
- `==` / `!=` — **not overloaded**, still reference comparison.

## Notes and pitfalls

- **Only `MultiList<T>` has structural equality**: other Multi types (such as `MultiDictionary`) still use reference comparison.
- **`==` is not overloaded**: `a == b` is reference comparison; for content equality use `a.Equals(b)`.
- **`IsSubsetOf` is not equality**: it ignores extra copies on the other side, so `{7}.IsSubsetOf({7,7})` is `True` yet the two are not equal.
- **`GetHashCode` is order-independent**: equal bags hash the same, so they can safely go into a `HashSet` / `Dictionary`.
