# Sample.ValueEnumerable

> **English** | [中文](README.zh-CN.md)

> **LINQ value-type engine** — the four `ToValueEnumerable()` entry points + five operators + namespace governance against `System.Linq` (7.1.0 CHANGELOG, `DotNetCore.Collections`, 7.1.0).

## What this sample demonstrates

The `DotNetCore.Collections` added in 7.1.0 is a LINQ engine built on **value-type enumerators**: the enumerator is an ordinary `struct` (not a `ref struct`), so it can be a type parameter, can cross `await`, and is allocation-free on hot paths. This sample demonstrates three facets: (1) the **four entry points** of `ToValueEnumerable()` (`T[]` / `List<T>` / `IReadOnlyList<T>` / `IEnumerable<T>`, where the most specific overload wins); (2) the five operators `Index` / `ForEach` / `TagFirstLast` / `Pairwise` / `Scan` (including the seeded overload); (3) the **namespace governance** that coexists with `System.Linq` — `using` both namespaces in the same file produces no CS0121, the engine's `Index` binds to the wrapper, a bare `T[]` still binds to the BCL (net9+), and existing `Enumerable` code is unaffected (since 7.1.0). The project is multi-target `net8.0;net10.0`.

## When to use it

- When a hot path wants to avoid `System.Linq`'s enumerator boxing and iterator allocation.
- When you need to use an enumerator as a type parameter (for example, putting it into a generic struct), or across `await` — a `ref struct` enumerator cannot do this.
- When you need operators such as `TagFirstLast` / `Pairwise` / `Scan` that `Enumerable` does not have.
- Should not use: when you need the full set of `Enumerable` operators → the engine provides only the first batch (5 in 7.1.0); the two surfaces can be freely combined.

## How to run

```bash
dotnet run --project sample/Sample.ValueEnumerable            # net8.0
dotnet run --project sample/Sample.ValueEnumerable -f net10.0 # net10.0
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Four entry points: the most specific overload wins | `T[].ToValueEnumerable()` / `List<T>.ToValueEnumerable()` / `IReadOnlyList<T>.ToValueEnumerable()` / `IEnumerable<T>.ToValueEnumerable()`; `ValueEnumerable.From<T>()` |
| 2 | Positional pairing | `Index()` → `(int Index, T Item)` |
| 3 | Immediate-execution traversal | `ForEach(Action<T>)` / `ForEach(Action<T,int>)` |
| 4 | First/last tagging | `TagFirstLast(Func<T,bool,bool,TResult>)` |
| 5 | Adjacent pairing | `Pairwise(Func<T,T,TResult>)` (n → n−1) |
| 6 | Running aggregation | `Scan(Func<T,T,T>)` / `Scan(TResult, Func<TResult,T,TResult>)` |
| 7 | Namespace governance | `using System.Linq;` + `using DotNetCore.Collections;` coexist |

## Key APIs

- `CollectionExtensions.ToValueEnumerable<T>(...)` — four overloads, dispatched by source shape.
- `ValueEnumerable.From<T>(...)` — a static entry point equivalent to the extension method.
- Wrapper value types: `ArrayValueEnumerable<T>` / `ListValueEnumerable<T>` / `ReadOnlyListValueEnumerable<T>` / `EnumerableValueEnumerable<T>`, implementing `IValueEnumerable<out T, out TEnumerator>` (= `IEnumerable<T>` + a value-type `GetEnumerator()`).
- Operators: `Index` / `ForEach` / `TagFirstLast` / `Pairwise` / `Scan` (seedless + seeded).

## Notes and pitfalls

- **`Index` shares its name with `Enumerable.Index` (net9+)**: on a wrapper it binds to the engine, while a bare `T[]` still binds to the BCL — both are compile-time facts.
- **The engine does not accept a bare `T[]`**: to use an operator, call `ToValueEnumerable()` first; there is no implicit conversion from `T[]` to the wrapper.
- **`ForEach` is declared only on the wrapper**: a bare `List<T>.ForEach` is still the BCL's instance method.
- **`Pairwise` produces an empty sequence for a single-element source** (n−1=0); a seeded `Scan` is one longer than the source, a seedless one is the same length.
- **The engine only adds a namespace, it does not take over**: `using DotNetCore.Collections;` does not change the binding of existing `Enumerable` code.
