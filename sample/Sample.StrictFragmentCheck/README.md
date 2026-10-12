# Sample.StrictFragmentCheck

> **English** | [中文](README.zh-CN.md)

> **Strict fragment-check option** — `PageCreationOptions.Lenient` / `Strict` decides whether a fragment shorter than the metadata is tolerated or rejected (`DotNetCore.Collections.Paginable`, 6.2.0).

## What this sample demonstrates

`PageCreationOptions` answers just one question: when a fragment is **shorter than the metadata says**, should page building tolerate it or throw? The default `Lenient` (the 6.1 behavior) tolerates it — covering the scenario where upstream rows are concurrently deleted between `COUNT(*)` and the fetch — and `CurrentPageSize` still reports the metadata value. `Strict` treats the same phenomenon as a bug (stale total, wrong query sliced, silently missing rows) and throws `ArgumentException` naming `fragment`. A long fragment (over page capacity / over the expected count) throws in both modes. This sample demonstrates both branches one by one, and covers `ToPage` / `CreateSinglePageSet`.

## When to use it

- User lists with possible concurrent deletes → `Lenient` (the default, no code change needed).
- Export reconciliation, amount checks, integration-test fixtures → `Strict` (pinning upstream COUNT and query consistency).
- A cached page that may be one or two rows stale → `Lenient` or invalidate the cache first.

## How to run

```bash
dotnet run --project sample/Sample.StrictFragmentCheck
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Default Lenient tolerates short fragments | `Paginable.CreatePage(..., totalMemberCount)` |
| 2 | Strict rejects short fragments | `Paginable.CreatePage(..., PageCreationOptions.Strict)` |
| 3 | Extension methods / single-page sets also support it | `ToPage` / `CreateSinglePageSet` + options |
| 4 | A long fragment throws in both modes | `ArgumentException` (`fragment`) |

## Key APIs

- `PageCreationOptions.Lenient` / `PageCreationOptions.Strict` — two singletons (no public constructor, preventing a third state).
- `PageCreationOptions.IsStrict` — whether short-fragment checking is enabled.
- `Paginable.CreatePage<T>(fragment, pageNumber, pageSize, totalMemberCount, options)` — fragment page building with options.
- `FragmentPageExtensions.ToPage<T>(fragment, pageNumber, pageSize, totalMemberCount, options)`.

## Notes and pitfalls

- **The default is `Lenient`**: upgrading 6.1 code requires no change.
- **`Strict` only tightens "short fragments"**: long fragments already throw; the exception is `ArgumentException` (naming `fragment`).
- **Under a short fragment, `CurrentPageSize` still reports the metadata value**: do not use it to infer the actual enumerated count.
- **`options` has no public constructor**: use only the two singletons `Lenient` / `Strict`; passing `null` throws `ArgumentNullException`.
- **The option is a per-call argument**, not part of `PaginableSettings`.
