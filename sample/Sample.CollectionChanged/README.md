# Sample.CollectionChanged

> **English** | [中文](README.zh-CN.md)

> **A home-grown change notification, CollectionChanged** — the change events of `MultiList<T>` / `MultiDictionary<TKey,TValue>` (`DotNetCore.Collections.Multi`, 6.9.0).

## What this sample demonstrates

The two core types each expose a `CollectionChanged` event: every change that **actually mutates** the collection (add, remove, clear) fires it once,
and the event arguments `CollectionChangedEventArgs<TKey>` describe which element/key changed, how many copies moved, and the count after the change.
This sample shows three facets: add/remove/clear notifications for `MultiList<T>`, a handler reading the collection back sees the **new state**,
and `MultiDictionary<TKey,TValue>` reports value-count changes in units of a "key". The key takeaway — **it is not the framework's `INotifyCollectionChanged`**.

## When to use it

- Needing to trigger side effects when a collection changes (refresh the UI, write an audit log, cascade-update a cache).
- Needing the consistent state "after the change" rather than "before" — the event fires after the collection reaches its new state.
- Not for: needing positional, index-oriented change notifications (a multiset has no positions) → use an ordered collection together with the framework notification;
  only reading data with no side effects → no need to subscribe (with no subscriber the event is **zero-cost**).

## How to run

```bash
dotnet run --project sample/Sample.CollectionChanged
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Add / remove / clear notifications for MultiList | `CollectionChanged` / `CollectionChangeType.Add` / `Remove` / `Reset` |
| 2 | Reading the collection back after the event sees the new state | inside the handler, `TotalCount` already includes this change |
| 3 | Per-key notifications for MultiDictionary | `Add(TKey,TValue)` / `Remove(TKey)` / `Remove(TKey,TValue)` / `Clear()` |
| 4 | How it differs from the framework notification | the event args are the home-grown `CollectionChangedEventArgs<TKey>` |

## Key APIs

- `MultiList<T>.CollectionChanged` / `MultiDictionary<TKey,TValue>.CollectionChanged` — `EventHandler<CollectionChangedEventArgs<TKey>>`.
- `CollectionChangedEventArgs<TKey>.ChangeType` — `CollectionChangeType` (`Add` / `Remove` / `Reset`).
- `CollectionChangedEventArgs<TKey>.Key` — the element (bag) or key (multimap); meaningless for `Reset`.
- `CollectionChangedEventArgs<TKey>.Count` — the number of copies moved this time (bag) or values (multimap); always 0 for `Reset`.
- `CollectionChangedEventArgs<TKey>.NewCount` — the count after the change (0 means the entry is gone); always 0 for `Reset`.
- `CollectionChangedEventArgs<TKey>.ToString()` — a one-line summary, e.g. `Add apple x3 -> 3`.

## Notes and pitfalls

- **It is not `INotifyCollectionChanged`**: the framework notification is position-oriented, but a multiset has no positions and a single call may move several copies,
  so a positional action cannot express it; this notification is a home-grown type with no `System.ObjectModel` dependency.
- **Only changes that actually mutate the collection fire**: e.g. `Clear()` on an already-empty bag, or an `Add` silently rejected by a de-duplicating collection, fire no event.
- **The event fires after the new state**: inside the handler, reading the collection back shows the change just announced.
- **Zero-cost when unsubscribed**: the arguments object is only constructed internally when a handler exists.
- **Not thread-safe**: concurrent modification still needs external synchronization (for concurrent types see 6.2.0).
