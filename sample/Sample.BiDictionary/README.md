# Sample.BiDictionary

> **English** | [中文](README.zh-CN.md)

> **Strict bijective dictionary BiDictionary&lt;TLeft,TRight&gt;** — one-to-one mapping, O(1) both ways (`DotNetCore.Collections.Multi`, 6.3.0).

## What this sample demonstrates

`BiDictionary<TLeft,TRight>` is a **strictly one-to-one (bijective) dictionary**: each `TLeft` is bound to exactly one `TRight`, and no `TRight` is shared by two `TLeft`s, with O(1) lookup both ways. It implements `IReadOnlyDictionary<TLeft,TRight>` (forward) and `IBiMap` (including reverse members). The sample shows bidirectional lookup, strict conflict handling (`Add` throws / `TryAdd` returns false / no overwriting setter), the `AsReverse` live view, and how a null left value differs between `ToDictionary` and the serialization model.

## When to use it

- **Bidirectional lookup** scenarios such as ID ↔ display name, code ↔ enum.
- You need the bijective constraint to **fail fast** rather than silently drop a binding.
- Do not use it for: one-to-many → `MultiDictionary`; composite keys → `MultiKeyDictionary`.

## How to run

```bash
dotnet run --project sample/Sample.BiDictionary
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Bidirectional lookup | `this[left]` / `GetLeft(right)` / `Count` |
| 2 | Strict conflict handling | `Add` (throws on conflict) / `TryAdd` (returns false) / `Remove` / `RemoveRight` |
| 3 | `AsReverse` live view | `AsReverse()` |
| 4 | Null left values and export | `ToDictionary` / `ToSerializableModel` / `FromModel` |

## Key APIs

- `BiDictionary<TLeft,TRight>.this[TLeft]` — forward lookup of the right value; a missing key throws `KeyNotFoundException`.
- `GetLeft(TRight)` / `TryGetLeft(TRight, out TLeft)` / `ContainsRight(TRight)` — reverse lookups.
- `Add(TLeft, TRight)` — throws `ArgumentException` when the left value already exists or the right value is bound to another left value (**no silent overwrite**).
- `TryAdd(TLeft, TRight)` — returns `false` on conflict, for branching instead of catching.
- `Remove(TLeft)` / `RemoveRight(TRight)` — explicit unbinding, after which rebinding is possible.
- `AsReverse()` — a **live** read-only reverse (right → left) view.
- `ToDictionary()` — a snapshot; **omits** null-left entries. `ToSerializableModel()` / `FromModel` — a parallel-list model that **preserves** null-left entries.

## Notes and pitfalls

- **No overwriting setter**: breaking an existing binding requires an explicit `Remove` / `RemoveRight`.
- **`Add` throws `ArgumentException` on conflict**: prefer `TryAdd` when you want to branch.
- **`ToDictionary` omits null-left entries** (null cannot be a dictionary key); use `ToSerializableModel` to preserve them.
- **`FromModel` rejects non-bijections**: duplicate left values or conflicting right values both throw `ArgumentException`.
