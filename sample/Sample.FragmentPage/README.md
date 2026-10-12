# Sample.FragmentPage

> **English** | [中文](README.zh-CN.md)

> **Fragment page building** — assemble an `IPage<T>` from "an already-sliced page + caller-supplied metadata", and the fragment is never sliced again (`DotNetCore.Collections.Paginable`, 6.1.0).

## What this sample demonstrates

When data comes from a hand-written Dapper/ADO.NET (`OFFSET`/`FETCH`) result, a cached page, or an upstream HTTP/gRPC `items` + `totalCount`, use `Paginable.CreatePage` / `FragmentPageExtensions.ToPage` to assemble the page directly. It treats the fragment as **the exact content of that page** and never `Skip/Take` again — hence deliberately a different name from `GetPage` (which slices a full source). This sample shows triple-metadata page building, a `PageFragmentInfo` round-trip, a comparison with full-source paging, and two high-frequency pitfalls.

## When to use it

- Dapper has already fetched by `offset/fetch` plus a separate `COUNT`.
- HTTP / gRPC forwards an upstream `items` + `totalCount`.
- A cached full-page JSON is deserialized and rebuilt.
- Do not use for: only having a full-source List → use `GetPage` (**do not** manually `Skip/Take` and then `ToPage`).

## How to run

```bash
dotnet run --project sample/Sample.FragmentPage
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | Triple-metadata page building and extension-method sugar | `Paginable.CreatePage` / `FragmentPageExtensions.ToPage` |
| 2 | The metadata object and its round-trip | `PageFragmentInfo` / `FromMetadata` / `ToMetadata` |
| 3 | Two entries on the same data (full source vs fragment) | `GetPage` vs `ToPage` |
| 4 | A long fragment throws, consequences of passing the fragment length as `total` | `ArgumentException` / `TotalPageCount` |

## Key APIs

- `Paginable.CreatePage<T>(fragment, pageNumber, pageSize, totalMemberCount)` — triple-metadata page building.
- `Paginable.CreatePage<T>(fragment, PageFragmentInfo)` — page building with pre-built metadata.
- `FragmentPageExtensions.ToPage<T>(fragment, pageNumber, pageSize, totalMemberCount)` — extension-method sugar.
- `PageFragmentInfo.PageNumber` / `PageSize` / `TotalMemberCount` / `FromMetadata` / `ToMetadata`.

## Notes and pitfalls

- **`totalMemberCount` is the full-source total, not the fragment length**: passing `fragment.Count` makes `TotalPageCount` always 1.
- **A fragment longer than page capacity → `ArgumentException`**; a fragment shorter than the metadata → tolerated by default (concurrent deletes are acceptable, see 6.2.0).
- **Materialization**: `CreatePage` copies the fragment into a `List<T>`, preventing the caller from mutating the collection afterwards.
- **Do not call `GetPage` on a pre-sliced fragment**: it silently skips again; use `ToPage` / `CreatePage`.
- **Out-of-range metadata throws `ArgumentOutOfRangeException`** (unified in 6.2.0).
