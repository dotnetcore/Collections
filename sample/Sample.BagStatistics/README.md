# Sample.BagStatistics

> **English** | [中文](README.zh-CN.md)

> **Bag statistics Mode / Median / Entropy** — three frequency-distribution summary extensions on `IMultiSet<T>` (`DotNetCore.Collections.Multi`, 6.9.0).

## What this sample demonstrates

Three extension methods summarize any `IMultiSet<T>` as a frequency distribution: `Mode()` yields **all** elements tied for the highest copy count (a multiset can be multimodal),
`Median(comparer)` yields the **upper-median copy**, and `Entropy()` yields the Shannon entropy (in bits) of the copy distribution. They all read via `EntrySet()`
(distinct elements + copy counts), **without expanding copies**, so they hold for any bag implementation and need no generic-math support.

## When to use it

- Quickly summarizing the "mode / median / dispersion" of a batch of frequency data.
- Needing a "multimodal" mode rather than a single winner (ordinary mode implementations often return only one).
- Needing a median without expanding every copy (memory O(d) instead of O(total copies)).
- Not for: needing finer quantile / rank reads → `PositionalRead` (6.5.0, via an ordered set); just wanting a histogram → `EntrySet()`.

## How to run

```bash
dotnet run --project sample/Sample.BagStatistics
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Mode (all ties returned) | `IMultiSet<T>.Mode()` |
| 2 | Upper-median copy (the larger one for an even total) | `IMultiSet<T>.Median(IComparer<T>)` |
| 3 | Shannon entropy of the copy distribution (bits) | `IMultiSet<T>.Entropy()` |

## Key APIs

- `IMultiSet<T>.Mode()` — returns `IReadOnlyList<T>` containing **all** elements tied for the highest copy count; an empty bag returns an empty list.
- `IMultiSet<T>.Median(IComparer<T> comparer)` — returns the upper-median copy: for an odd total, the middle one; for an even total, the larger of the two middle ones;
  the result is always an element of the bag, never interpolated; an empty bag throws `InvalidOperationException`.
- `IMultiSet<T>.Entropy()` — returns a `double`, `-Σ p·log2(p)`; a single-element bag is 0, d equally sized elements give `log2(d)`, and an empty bag is 0.
- All three are based on `EntrySet()`; `Median` costs O(d log d) time and O(d) space (d being the number of distinct elements).

## Notes and pitfalls

- **`Median` requires an `IComparer<T>`**: there is no parameterless overload; because a multiset is not necessarily numeric, it never averages.
- **`Mode`'s return order** is the order in which the bag enumerates distinct elements (hash order, no contract); do not rely on insertion order.
- **Upper median**: for an even total it returns the larger of the two middle elements, not their average.
- **Empty bag**: `Mode` returns an empty list and `Entropy` returns 0, but `Median` **throws** `InvalidOperationException`.
- **Non-numeric elements work too**: the counts are plain `int` and do not rely on generic math, so `net451` works as well.
