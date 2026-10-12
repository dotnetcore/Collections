# Sample.NullableAnnotations

> **English** | [中文](README.zh-CN.md)

> **Public API nullable reference type annotations (NRT)** — the compile-time nullability contract: `T?` / `string?` tell the caller "this can be null" (all packages, 6.3.0).

## What this sample demonstrates

Since 6.3.0 the public APIs carry NRT annotations, covering Multi, Paginable, and the adapter packages. This is not a behavior change but a **compile-time contract**. This sample uses runnable code to explain four things clearly: a nullable parameter falling back to the default implementation when `null` (the default comparer); the nullable contracts that really exist in the library (`int? limitedMemberCount`, the `T?` of `KeysetPage<T>.LastMember`); and the most easily misunderstood point — **annotations do not produce runtime checks**, the boundaries still throw `ArgumentNullException` from the library itself.

## When to use it

- New code that enables `#nullable enable` and wants accurate nullability warnings (silencing CS86xx).
- Old code with NRT disabled: the upgrade is **transparent**, no code changes needed.
- Designing an API with "nullable parameter -> fall back to default" and using `T?` to express that passing `null` is legal.
- Not for: treating annotations as runtime checks (NRT does not produce `null` checks).

## How to run

```bash
dotnet run --project sample/Sample.NullableAnnotations
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Nullable parameter `IComparer<Order>?` passed `null` falls back to the default comparer | local `SortBy(..., IComparer<Order>?)` |
| 2 | A real nullable contract in the library, `int?` (`null` = unlimited) | `SolidPageExtensions.ToPaginable<T>(..., int? limitedMemberCount)` |
| 3 | A nullable return `T?` needs a null check | `KeysetPage<T>.LastMember` |
| 4 | Annotations != runtime checks; the boundary still throws | `PaginableSettingsManager.UpdateSettings(...)` |

## Key APIs

- `SolidPageExtensions.ToPaginable<T>(this IEnumerable<T>, int pageSize, int? limitedMemberCount = null)` — an `int?` nullable value-type parameter; `null` means unlimited members.
- `KeysetPage<T>.LastMember` — returns `T?`; returns `default` when the page is empty (a reference type yields `null`).
- `PaginableSettingsManager.UpdateSettings(PaginableSettings)` — the parameter is non-null annotated, yet at runtime it still explicitly throws `ArgumentNullException`.

## Notes and pitfalls

- **Annotations are not runtime checks**: they will not throw `ArgumentNullException` for you; at the boundary you still guard against NREs yourself.
- **Upgrading needs no code changes**: you only need to be aware of the new annotations and clear warnings accordingly.
- **Low-generation TFMs**: the annotation metadata is supplied by an internal shim (see 6.3.0).
- **This sample references only the core package**; the `MultiList<T>` nullable comparer in the doc example belongs to the Multi side, with the same convention (`IEqualityComparer<T>?` -> `EqualityComparer<T>.Default`); here a local `SortBy` demonstrates the same pattern.
