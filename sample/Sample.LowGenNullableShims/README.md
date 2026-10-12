# Sample.LowGenNullableShims

> **English** | [中文](README.zh-CN.md)

> **Nullability attribute shims for low-generation TFMs** — targets such as net451 have no nullable annotation attribute types, so an internal polyfill supplies them, keeping a single source across all TFMs with complete annotations (low-TFM assets of all packages, 6.3.0).

## What this sample demonstrates

The reference assemblies of low-generation TFMs (`net451` / `net461` / `net47` / `net48` / `netstandard2.0`) have no `NullableAttribute` / `NullableContextAttribute`, so writing `T?` directly loses the annotation metadata. 6.3.0 introduces **internal shims** (`build/NullabilityAttributes.cs`), compiled in only for these targets, while `netstandard2.1` and `net6.0+` use the types built into the runtime. This sample proves, from a **consumer's point of view**, that the shims are transparent: nullable parameters keep working, the nullability annotation metadata of the public APIs is still readable via reflection, and no shim type leaks into the public type surface.

## When to use it

- A library must support both `net451` and `net8`: the shims let the same `T?`-bearing source compile across all TFMs.
- A consumer on a `net462` + NRT project: it can also read the nullability annotations correctly.
- Not for: treating the shim as a runtime null check (a shim is not a check).

## How to run

```bash
dotnet run --project sample/Sample.LowGenNullableShims
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Consumer's view: the `int?` nullable parameter works as usual | `SolidPageExtensions.ToPaginable<T>(..., int? limitedMemberCount)` |
| 2 | The nullability annotation metadata is readable via reflection | `NullabilityInfoContext` / `KeysetPage<T>.LastMember` / `PaginableSettingsManager.UpdateSettings` |
| 3 | The shim has no public types (internal, no leakage) | `Assembly.GetExportedTypes()` |

## Key APIs

- `build/NullabilityAttributes.cs` — the internal shims: `NullableAttribute` / `NullableContextAttribute` / `NullablePublicOnlyAttribute` (**internal, not public API**).
- `SolidPageExtensions.ToPaginable<T>(this IEnumerable<T>, int pageSize, int? limitedMemberCount = null)` — an example of a nullable contract the consumer benefits from directly.
- `System.Reflection.NullabilityInfoContext` — reads the nullability annotation metadata written at compile time (.NET 6+).

## Notes and pitfalls

- **The shims are an internal implementation detail**: do not write them into public API docs as types (they cannot be found / are internal).
- **Paired with 6.3.0**: 6.3.0 is "add NRT annotations to the public APIs", 6.3.0 is "make that annotation have an attribute type to compile against on low TFMs".
- **Does not replace null checks**: the shims only make the annotation metadata compilable and readable.
- This sample's net8 asset does **not** compile in the shims, so step 3 cannot find the definition, which is expected (the runtime ships types of the same name).
