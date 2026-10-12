# Sample.PaginableSettings

> **English** | [中文](README.zh-CN.md)

> **Paging settings and manager** — the global paging configuration has only two knobs, validated on assignment and swapped in atomically (`DotNetCore.Collections.Paginable`, 6.0.0).

## What this sample demonstrates

`PaginableSettings` has only two knobs, `DefaultPageSize` (default `50`) and `MaxMemberItems` (default `10_000_000`). It is validated on assignment — an invalid value throws `ArgumentOutOfRangeException` in the setter, so any instance you obtain is always in a valid state. `PaginableSettingsManager` implements **atomic swap-in** with a `volatile` snapshot + reference assignment, and `Settings` reads the current snapshot. This sample shows the default values, configuration, the single-argument `GetPage(n)` reading `DefaultPageSize`, setter validation, rejection of `UpdateSettings(null)`, and restoring the defaults at the end.

## When to use it

- Setting the site-wide default page size and maximum result size at startup.
- Limiting the attack surface of huge `COUNT` → tightening `MaxMemberItems`.
- Swapping the snapshot in multi-tenant / test scenarios.
- Different page sizes per request → pass `pageSize` explicitly, do not change the global.

## How to run

```bash
dotnet run --project sample/Sample.PaginableSettings
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Default values and atomic swap-in | `PaginableSettingsManager.Settings` / `UpdateSettings` |
| 2 | Validation on assignment | `PaginableSettings.DefaultPageSize` / `MaxMemberItems` setter |
| 3 | `null` is rejected | `UpdateSettings(null)` → `ArgumentNullException` |
| 4 | Restore defaults | `UpdateSettings(new PaginableSettings())` |

## Key APIs

- `PaginableSettings.DefaultPageSize` (`int`, ≥1, default 50) / `MaxMemberItems` (`long`, ≥1, default 10_000_000).
- `PaginableSettingsManager.Settings` — the current snapshot (volatile read).
- `PaginableSettingsManager.UpdateSettings(PaginableSettings)` — atomic swap-in; `null` throws `ArgumentNullException`.
- `SolidPageExtensions.GetPage<T>(enumerable, pageNumber)` — the single-argument entry uses `DefaultPageSize`.

## Notes and pitfalls

- **The property setter validates**: `< 1` throws `ArgumentOutOfRangeException` (`ParamName` is `value`).
- **`MaxMemberItems` comparison rejects only when `>`**: the default `10_000_000` is exactly allowed (open interval, see 6.0.0).
- **Not `IOptions` / not DI**: it is a static manager; tests must isolate themselves (restore defaults or run serially).
- **Frequent `UpdateSettings` at runtime** can interleave with a page being constructed — finalize once at startup.
- **The Multi family does not read these settings**.
