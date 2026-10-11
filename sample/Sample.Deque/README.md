# Sample.Deque

> **English** | [中文](README.zh-CN.md)

> **Double-ended queue** — `Deque<T>`: a ring-buffer double-ended queue with O(1) amortized at both ends (feature F28, `DotNetCore.Collections.Multi`, 6.6.0).

## What this sample demonstrates

The BCL has no deque: `Queue` / `Stack` are single-ended, and `LinkedList` is double-ended but has a heap of nodes per element. `Deque<T>`
fills the gap with a **single-array ring buffer**: the head and tail are two moving indices within the same allocation, so pushing / popping
either end is an index move plus one array slot, and only growth allocates. The sample shows three facets: operations at both ends and
positional reads, a sliding window, and the complete list interface (indexer setter / `Insert` / `RemoveAt`). Positions are counted **from
head to tail**: index 0 is the first element, `Count-1` the last.

## When to use it

- Sliding windows / the most recent N items: push and pop at both ends, O(1).
- Work-stealing deques: O(1) insertion and removal at both ends.
- Undo / redo double stacks: both ends + positional reads.
- Do not use for: needing ordering (use `OrderedMultiList<T>`); a hot `Contains` (use `HashSet` / `MultiList<T>`).

## How to run

```bash
dotnet run --project sample/Sample.Deque
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Operations at both ends and positional reads | `AddFirst` / `AddLast` / `RemoveFirst` / `RemoveLast` / `this[int]` / `GetFirst` / `GetLast` / `IndexOf` |
| 2 | Sliding window | `new Deque<T>(int capacity)` / `AddLast` / `RemoveFirst` / `Count` / `Capacity` |
| 3 | Complete list interface | indexer setter / `Insert` / `RemoveAt` / `Contains` / `TrimExcess` / `IsReadOnly` |
| 4 | Empty behavior and a cost reminder | `IsEmpty` / `TryRemoveFirst` / endpoint operations on an empty deque throw `InvalidOperationException` |

## Key APIs

- `Deque<T>` — implements `IList<T>` / `IReadOnlyList<T>`; `AddFirst` / `AddLast` / `RemoveFirst` / `RemoveLast` are all O(1) amortized.
- `Deque<T>.this[int]` — an O(1) ring read; assignable (**overwrite**, no shifting).
- `Deque<T>.Insert(int,T)` / `RemoveAt(int)` — O(min(index, Count-1-index)).
- `Deque<T>.GetFirst()` / `GetLast()` / `TryRemoveFirst` / `TryRemoveLast` / `TryGetFirst` / `TryGetLast`.
- `Deque<T>.Capacity` / `TrimExcess()` / `AsReadOnly()` / `Clone()` / `ToArray()`.

## Notes and pitfalls

- **`Contains` / `IndexOf` / `Remove(T)` are O(n)**: a ring buffer has no index; for membership testing use `HashSet` / `MultiList<T>`.
- **Capacity does not shrink automatically**: the buffer is retained after draining; to reclaim it use `TrimExcess` or set `Capacity` directly.
- **The constructor capacity is not a hard limit**: exceeding it still doubles by 4×; for a hard cap, check `Count` yourself.
- **Endpoint operations on an empty deque throw** `InvalidOperationException`: check `IsEmpty` / `Count` first, or use `TryRemoveFirst` / `TryRemoveLast`.
- **Structural equality is not overridden**: to compare contents use `ToArray()`.
