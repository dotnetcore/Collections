# Sample.FrequencyPriorityBag

> **English** | [中文](README.zh-CN.md)

> **Frequency-priority bag** — `FrequencyPriorityBag<T>`: the highest-frequency element is available in O(log n) (feature F23, `DotNetCore.Collections.Multi`, 6.4.0).

## What this sample demonstrates

The frequency-priority bag turns "who appears most often" into a first-class query: `PeekMost` looks at the highest-frequency
element without removing it, `PopMost` removes one copy and returns it, and popping repeatedly yields a descending-frequency
sequence — exactly the natural shape of a Top-K query. The sample shows three facets: Top-K repeated popping, the tie-breaking
policy, and enumeration / `null` / empty-bag behavior. It trades "each update is O(1), only touching the frequency index and
marking the heap dirty" for "the heap is rebuilt only when a priority query happens", so it pays off most when updates are
frequent and queries come in clusters.

## When to use it

- Hot-word / Top-K high-frequency queries: repeatedly pop with `PopMost`.
- Frequent updates with clustered priority queries: a lazily materialized heap.
- Do not use for: querying the most frequent only once (sorting a `MultiList<T>` once is cheaper); needing ordered enumeration (use `OrderedMultiList<T>`).

## How to run

```bash
dotnet run --project sample/Sample.FrequencyPriorityBag
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Top-K repeated popping | `AddRange` / `PeekMost` / `PopMost` / `TotalCount` / `DistinctCount` |
| 2 | Tie-breaking policy (FIFO-flavored) | when `PopMost` sees equal frequency, the one that reached its current count earlier pops first |
| 3 | Enumeration expands copies, unordered | enumeration / `TotalCount` |
| 4 | `null` as a first-class element | `Add(null)` / `CountOf(null)` / `PeekMost` |
| 5 | Empty bag and Try forms | `TryPeekMost` / `TryPopMost` / `PeekMost` throws `InvalidOperationException` |

## Key APIs

- `FrequencyPriorityBag<T>.PeekMost()` — returns the highest-frequency element without removing it; an empty bag throws `InvalidOperationException`.
- `FrequencyPriorityBag<T>.PopMost()` — removes one copy and returns the highest-frequency element; popping repeatedly yields a descending-frequency sequence (Top-K).
- `FrequencyPriorityBag<T>.TryPeekMost(out T)` / `TryPopMost(out T)` — the non-throwing versions; an empty bag returns `false`.
- `FrequencyPriorityBag<T>.Add(T)` / `Add(T,int)` / `AddRange` / `CountOf` / `Remove(T)` / `RemoveAllCopies(T)` / `EntrySet()`.
- `FrequencyPriorityBag<T>.TotalCount` / `DistinctCount` / `IsEmpty`.

## Notes and pitfalls

- **Enumeration is unordered**: enumeration is only copy expansion, neither by frequency nor by insertion order; for frequency order use `PopMost`.
- **Ties are FIFO-flavored**: on equal counts the element that reached its current count earlier pops first (a count change stamps an increasing token, and the heap takes the smaller token), not "most recently changed first".
- **Not worth it for query-once**: the first priority query rebuilds the heap in O(n); querying the most frequent only once is worse than sorting a `MultiList<T>` once.
- **Empty-bag `PeekMost` / `PopMost` throw**: use `TryPeekMost` / `TryPopMost`, or check `IsEmpty` first.
- **Not thread-safe**: concurrent writes corrupt the frequency index and the heap; external synchronization is required.
