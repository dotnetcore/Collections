# Sample.TypeReadableName

> **English** | [中文](README.zh-CN.md)

> **Readable type-name formatting** — `TypeExtensions.ToReadableString()` / `TypeNameFormat` (feature F26, `DotNetCore.Collections.Multi`, 6.6.0).

## What this sample demonstrates

`TypeExtensions.ToReadableString()` renders a `System.Type` as a one-line readable type name for diagnostics, logging, and test-failure
output: it preserves array rank, recursively renders generic arguments, and drops namespaces — presenting the **shape** rather than
the assembly-qualified identity. The sample shows three facets: the default CLR flavor, the C# flavor (keyword aliases + nullable),
and `maxDepth` depth folding. The output is stable: the same `Type` always yields the same string, so it can be asserted.

## When to use it

- Test failure messages: see the type's shape at a glance.
- Diagnostic logging: done in one line, no longer an assembly-qualified `FullName` string.
- Asserting a type name: the output is stable.
- Do not use for: type deserialization / type ids (use `FullName` / `AssemblyQualifiedName`).

## How to run

```bash
dotnet run --project sample/Sample.TypeReadableName
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Default CLR flavor | `ToReadableString(this Type)` / `TypeNameFormat.Clr` |
| 2 | C# flavor (keywords / nullable) | `ToReadableString(this Type, TypeNameFormat)` / `TypeNameFormat.CSharp` |
| 3 | Depth folding | `ToReadableString(this Type, TypeNameFormat, int)` / `DefaultMaxDepth` |
| 4 | Argument validation and boundaries | `null` throws `ArgumentNullException`; `maxDepth < 1` throws `ArgumentOutOfRangeException` |

## Key APIs

- `TypeExtensions.ToReadableString(this Type)` — defaults to `TypeNameFormat.Clr`, with `DefaultMaxDepth = 16`.
- `TypeExtensions.ToReadableString(this Type, TypeNameFormat)` — specify the flavor.
- `TypeExtensions.ToReadableString(this Type, TypeNameFormat, int maxDepth)` — specify the flavor + the nesting-depth limit.
- `TypeNameFormat.Clr` (0) — CLR spelling, namespaces removed; `TypeNameFormat.CSharp` (1) — primitive keyword aliases + `Nullable<T>` → `T?`.
- `TypeExtensions.DefaultMaxDepth` — the default depth-limit constant (16).

## Notes and pitfalls

- **Namespaces are dropped**: types with the same name in different namespaces produce the same string; this is not a unique identity — for identity use `AssemblyQualifiedName`.
- **Not a type id**: don't use it for deserialization / type lookup.
- **`type == null` throws `ArgumentNullException`**; **`maxDepth < 1` throws `ArgumentOutOfRangeException`**.
- **Recursion is bounded**: nesting beyond `maxDepth` folds to `...`; reflection can `MakeGenericType` indefinitely, and bounded traversal keeps the diagnostic path from blowing the stack.
- **Assert using this API's stable output**: asserting a `FullName` style will fail.
