# Sample.SerializationEntry

> **English** | [中文](README.zh-CN.md)

> **No standalone serialization API on the Paginable side (negative result) + the correct rebuild path** — `PageMetadata` + `Paginable.CreatePage` (feature 6.2.0, `DotNetCore.Collections.Paginable`).

## What this sample demonstrates

DOC_CATALOG once labeled 6.2.0 as "serialization entry (merge 6.2.0 if there is none on the Paginable side)". The verified conclusion is a **negative result**: the Paginable core and the 9 adapters **all lack** an explicit serialization model such as `ToSerializableModel` / `FromModel` — that is **Multi**'s API (`MultiListModel<T>` / `MultiDictionaryModel<TKey,TValue>`, see 6.2.0). This sample demonstrates the negative result of "we looked and confirmed there is none", and gives the **correct path** for sending a page across processes: project `items` + the triple metadata (`CurrentPageNumber` / `PageSize` / `TotalMemberCount`), and have the other side rebuild `IPage<T>` with `Paginable.CreatePage`, where the global numbering matches whole-source pagination exactly.

## When to use it

- When you need to transfer a page of data across processes / services → serialize `items` + three integers and rebuild with `CreatePage` on the other side.
- When you need to serialize pagination navigation info → serialize the public properties of `PageMetadata`.
- When you need explicit serialization of Multi collections → go to 6.2.0 (`Sample.ExplicitSerialization`).
- Should not use: serializing `IPage` / `IPaginable` objects directly — they are not a serialization contract.

## How to run

```bash
dotnet run --project sample/Sample.SerializationEntry
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 0 | Server-side whole-source pagination | `IEnumerable<T>.GetPage(pageNumber, pageSize)` |
| 1 | Negative result: no `ToSerializableModel` | explanation (the correct path points to 6.2.0 / the fragment API) |
| 2 | `PageMetadata` property snapshot | `IPage.GetMetadata()` |
| 3 | Project DTO → rebuild | `IPage<T>.ToOriginalItems()` / `Paginable.CreatePage(items, pageNumber, pageSize, totalMemberCount)` |
| 4 | Metadata round trip | `PageFragmentInfo.FromMetadata` / `ToMetadata` |
| 5 | Why not serialize the page object directly | `IPage` / `IPaginable` are not serialization contracts |

## Key APIs

- `PageMetadata` — a plain get-only property snapshot (`TotalPageCount` / `RealPageCount` / `TotalMemberCount` / `PageSize` / `CurrentPageNumber` / `CurrentPageSize` / `HasPrevious` / `HasNext`), with a readable `ToString`, processable by any serializer.
- `Paginable.CreatePage<T>(IEnumerable<T> fragment, int pageNumber, int pageSize, int totalMemberCount)` — rebuilds a page from an already-sliced fragment.
- `PageFragmentInfo.FromMetadata(PageMetadata)` / `ToMetadata()` — fragment metadata round trip.
- `IPage<T>.ToOriginalItems()` — projects the page members back to the original element sequence.

## Notes and pitfalls

- **Do not call the non-existent** `Paginable.ToSerializableModel()` — that belongs to Multi (6.2.0).
- **`IPage` / `IPaginable` implementations are not serialization contracts** — project into a DTO and do not serialize the page object directly.
- **`CreatePage` does not re-slice**: the fragment is treated as the exact contents of that page; feeding it to `GetPage` would skip a second time.
- **Multi's serialization model is not Paginable's**; this negative-result piece guards against API hallucination.
