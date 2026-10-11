# Sample.SpanBag

> **English** | [中文](README.zh-CN.md)

> **Stack-based temporary bag** — `SpanBag<T>`: a fixed-capacity counting bag with zero heap allocation on the stack (feature F22, `DotNetCore.Collections.Multi`, 6.4.0).

## What this sample demonstrates

`SpanBag<T> where T : unmanaged` is a `ref struct`: its storage is **borrowed** from a `Span<T>` / `Span<int>` supplied by the
caller (usually `stackalloc`), for short-lived frequency counting within a single method — zero heap allocation, no `Dispose`
needed. The sample shows three facets: finding the first duplicate value within a method, fixed capacity and full-capacity
behavior (a new element's `Add` returns `false`), and enumeration / removal / `Clear` reuse. The lifetime contract is enforced
by the compiler: a `ref struct` cannot be boxed, cannot be a field, cannot be captured by a lambda, and cannot cross an `await`.

## When to use it

- In-method duplicate detection / frequency counting where a hot-path small histogram must have zero heap allocation.
- A short-lived count whose "memory disappears with the stack frame", with no manual release needed.
- Do not use for: needing to hold across methods / across `await` (use `PackedBag<T>` / `MultiList<T>`); reference types (the constraint is `unmanaged`); net451 / netstandard2.0 (the type does not exist).

## How to run

```bash
dotnet run --project sample/Sample.SpanBag
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | In-method zero-heap counting | `new SpanBag<T>(Span<T>, Span<int>)` / `CountOf` / `Add(T)` |
| 2 | Fixed capacity and full-capacity behavior | `Capacity` / `Remaining` / `Add` returning `bool` / `DistinctCount` / `TotalCount` |
| 3 | Enumerated histogram | `GetEnumerator()` (a `ref struct` enumerator) / `Contains` |
| 4 | Removal and `Clear` reuse | `Remove(T)` / `RemoveAllCopies(T)` / `Clear()` / `IsEmpty` |
| 5 | Lifetime boundary | `ref struct` compile-time constraints (counter-example comment) |

## Key APIs

- `SpanBag<T>` — `where T : unmanaged`, a `ref struct`; construct with `new SpanBag<T>(Span<T> values, Span<int> counts)`, the two spans must be the same length.
- `SpanBag<T>.Add(T)` — returns `bool`: a new element at full capacity returns `false` (a size condition, not an exception); an existing element always succeeds.
- `SpanBag<T>.CountOf` / `Contains` / `Remove(T)` / `RemoveAllCopies(T)` / `Clear()`.
- `SpanBag<T>.Capacity` / `Remaining` / `DistinctCount` / `TotalCount` / `IsEmpty`.
- `SpanBag<T>.GetEnumerator()` — returns a `ref struct` enumerator that likewise cannot escape the stack frame.

## Notes and pitfalls

- **`ref struct` lifetime**: cannot be boxed / used as a field / captured by a lambda / cross an `await` / `yield` — all compile errors, by design.
- **Fixed capacity, no growth**: capacity = `min(values.Length, counts.Length)` is fixed at construction; when full, a new element's `Add` returns `false`, so **always check the return value** or the new element is silently dropped.
- **The two spans must be the same length**: otherwise the constructor throws `ArgumentException`.
- **Only netstandard2.1 / net6.0+**: `net451`..`net48` / `netstandard2.0` do not expose this type (use `PackedBag<T>`).
- **No `Dispose` needed**: memory was never requested from the allocator; `Clear` only resets the counts and totals.
