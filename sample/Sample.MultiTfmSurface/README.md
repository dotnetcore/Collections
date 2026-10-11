# Sample.MultiTfmSurface

> **English** | [中文](README.zh-CN.md)

> **Multi-TFM surface and per-package subset differences** — an 11-TFM matrix, low-generation differences and per-package differences (feature F57, `DotNetCore.Collections.Multi` and others, expanded to 11 TFMs in 6.0.0).

## What this sample demonstrates

The core and some adapters target `net451;net461;net47;net48;netstandard2.0;netstandard2.1;net6.0–net10.0` (**11** of them), but **each package's TFM subset differs** — this is why the "target frameworks" column is mandatory in the API manual. This sample is a **multi-target project** (`net8.0;net10.0`): the same source compiles and runs on both branches, and the program prints the current runtime, the conditional-compilation branch it hit, a genuinely runnable piece of Multi API (proving consistent behavior across TFMs), as well as the 11-TFM matrix and per-package subset differences (EF Core is net6+ only, Chloe/Dos/NHibernate have no net451, SqlSugar has no ns2.0, etc.).

## When to use it

- Verify TFMs before selecting a package: for example, an old net48 system is fine with the core, but the EF Core adapter is unavailable.
- When writing docs / API references, annotate the target frameworks per type.
- Library maintainers ensuring low generations compile and are testable (the `Multi.NetFxTests` net462 sidecar).
- Should not use: treating "11 TFMs for all packages" as a default assumption.

## How to run

```bash
dotnet run --project sample/Sample.MultiTfmSurface            # net8.0
dotnet run --project sample/Sample.MultiTfmSurface -f net10.0 # net10.0
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Current runtime and compile branch | `RuntimeInformation.FrameworkDescription` / `Environment.Version` / `#if NET10_0_OR_GREATER` |
| 2 | Multi API with consistent behavior across TFMs | `MultiList<T>.Add` / `TotalCount` / `DistinctCount` / `CountOf` |
| 3 | 11-TFM matrix | lists the 11 targets as strings |
| 4 | Per-package TFM subset differences | EF Core is net6+ only; Chloe/Dos/NHibernate have no net451; SqlSugar has no ns2.0 |
| 5 | Type-level TFM differences | `SpanBag` exists only on targets where `Span<T>` is in the box |

## Key APIs

- `RuntimeInformation.FrameworkDescription` / `Environment.Version` — runtime target-framework information.
- `#if NET10_0_OR_GREATER` / `#if NET8_0_OR_GREATER` — conditional compilation branches.
- `MultiList<T>` (`Add` / `TotalCount` / `DistinctCount` / `CountOf`) — an example API whose behavior is consistent across both TFMs.

## Notes and pitfalls

- **Do not assume 11 TFMs for all packages**: EF Core is net6+ only; Chloe / Dos / NHibernate have no net451; SqlSugar has no netstandard2.0.
- **net451 lacks interfaces**, causing "the same code does not compile on a low generation"; nullability relies on internal shims (F43).
- **Types such as `SpanBag` exist only on targets where `Span<T>` is in the box** — do not assume all packages share the same matrix.
- **Locked dependencies** `packages.lock.json` (deterministic builds since 6.0.0).
