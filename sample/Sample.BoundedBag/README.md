# Sample.BoundedBag

> **English** | [中文](README.zh-CN.md)

> **Fixed-capacity sliding-window bag BoundedBag&lt;T&gt;** — a bag whose total copy count is capped: exceeding capacity silently evicts the oldest copies (6.9.0, `DotNetCore.Collections.Multi`, 6.9.0).

## What this sample demonstrates

`BoundedBag<T>` is the "sliding window" member of the bag family. It holds at most a fixed `Capacity` number of copies, and as soon as a new copy would exceed the limit,
it silently drops the **oldest** one (first-in-first-out), so the bag always holds the most recent additions. This sample shows four facets: capacity and eviction,
the void semantics and order sensitivity of `Add`, projection/enumeration, and removal with argument validation. The key takeaway — **adding never fails**.

## When to use it

- Needing a "last N" sliding window (recent logs, recent visits, the last N samples).
- Scenarios with a small capacity where automatically pushing out the oldest data is acceptable.
- Not for: needing unbounded growth → `MultiList<T>` (F01); needing multiset operations by copy count / implementing `IMultiSet<T>` → `MultiList<T>`;
  a small-domain histogram → `PackedBag<T>` (F21).

## How to run

```bash
dotnet run --project sample/Sample.BoundedBag
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Capacity cap and silent FIFO eviction | `Capacity` / `TotalCount` / `IsFull` / `Add(T)` / `Contains(T)` |
| 2 | Adding multiple copies at once, order sensitivity and extreme times | `Add(T, times)` / `AddRange(IEnumerable<T>)` |
| 3 | Projection and enumeration (oldest→newest) | `DistinctItems()` / `EntrySet()` / `ToDictionary()` / `ToArray()` / `ToString()` |
| 4 | Removal returns remaining copies and argument validation | `Remove(T, times)` / `RemoveAllCopies(T)` / `Add(T, 0)` throws `ArgumentOutOfRangeException` |

## Key APIs

- `new BoundedBag<T>(capacity)` / `new BoundedBag<T>(capacity, comparer)` — the capacity must be positive, otherwise `ArgumentOutOfRangeException`.
- `Add(T)` / `Add(T, times)` — returns **`void`**, never fails; when `times >= Capacity` the window holds only `Capacity` copies of that element.
- `AddRange(IEnumerable<T>)` — adds copy by copy, **order sensitive**: the earliest elements beyond capacity are evicted first.
- `Capacity` / `TotalCount` / `Count` (a **public property** here) / `DistinctCount` / `IsEmpty` / `IsFull`.
- `Contains(T)` / `CountOf(T)` / `DistinctItems()` / `EntrySet()` / `ToDictionary()` / `ToArray()` / `ToList()` — linear scan over the bounded window.
- `Remove(T, times)` returns the **remaining copy count** (0 when the element is absent); `RemoveAllCopies(T)` returns `bool`.
- Enumeration expands copies in "oldest→newest" order; `Clone()` produces an independent copy.

## Notes and pitfalls

- **`Add` returns nothing**: there is no overflow to report; to detect eviction you must compare the contents before and after yourself.
- **Order sensitive**: unlike `MultiList<T>`, the order of `AddRange` decides who is evicted first, which is why it does **not** implement `IMultiSet<T>`.
- **No parameterless constructor**: capacity is required and must be positive.
- **`null` is a valid element**, but `ToDictionary()` cannot use `null` as a key and throws `InvalidOperationException`.
- **Not thread-safe**: concurrent writes corrupt the ring buffer; external synchronization is required.
