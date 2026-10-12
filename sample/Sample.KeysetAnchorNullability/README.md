# Sample.KeysetAnchorNullability

> **English** | [中文](README.zh-CN.md)

> **Nullable keyset pagination anchor parameter** — the `lastKey` anchor is consistent with the nullability of the key type: a nullable key can be used directly as an anchor, and `null` means "no anchor" (`DotNetCore.Collections.Paginable` / `…EntityFrameworkCore`, 6.3.0).

## What this sample demonstrates

The `lastKey` anchor parameter of keyset (seek) pagination became nullable in 6.3.0. Previously, NRT-enabled projects raised false positives on legal calls where "the key type is itself nullable" or "the first page passes null as no anchor". This sample walks through a nullable sort key (`string?`): the first page (no anchor), the continuation page (anchor = the key of the previous page's `LastMember`), and the most easily misunderstood point — **a `null` anchor means "no anchor" (start from the beginning), not "the position where the key value equals null"**.

## When to use it

- The sort key is itself nullable (such as `string?`, a nullable foreign key) and you need to use it directly as an anchor.
- Unifying "first page / continuation page" call sites: the first page passes `null` or uses `GetFirstPageByKeyset`, the continuation page passes the previous page's last key.
- Not for: using `null` to locate some real key.

## How to run

```bash
dotnet run --project sample/Sample.KeysetAnchorNullability
```

## Walkthrough

| Step | Concept | Key API |
| :--- | :--- | :--- |
| 1 | First page (no anchor) | `KeysetPageExtensions.GetFirstPageByKeyset<T, TKey>(...)` |
| 2 | Continuation page: anchor = previous page's last key (`string?`) | `KeysetPage<T>.LastMember` / `GetPageByKeyset<T, TKey>(...)` |
| 3 | `null` anchor = no anchor = from the beginning | `GetPageByKeyset<T, TKey>(..., TKey lastKey, ...)` |
| 4 | Reaching the end, `HasNext=false` | `KeysetPage<T>.HasNext` |

## Key APIs

- `KeysetPageExtensions.GetFirstPageByKeyset<T, TKey>(this IEnumerable<T>, Func<T, TKey>, int pageSize, bool descending = false)` — the first page, no anchor.
- `KeysetPageExtensions.GetPageByKeyset<T, TKey>(this IEnumerable<T>, Func<T, TKey>, TKey lastKey, int pageSize, bool descending = false)` — the continuation page, with an anchor of `TKey`; when the key type is nullable you can pass `null` directly.
- `KeysetPage<T>.LastMember` — returns `T?`; an empty page returns `default`, so a null check is needed before feeding it to the next page.

## Notes and pitfalls

- **A `null` anchor is "no anchor", not "a key value of null".** If the sort key itself can store `null`, the semantics are defined by the query provider.
- **Non-breaking upgrade**, shipped together with 6.3.0; the runtime "anchored / not anchored" branch is unchanged.
- **A keyset page carries no total page count** (no `COUNT(*)`, no `OFFSET`), unlike offset pagination `IPage<T>` (see 6.0.0).
- An `IQueryable<T>` source has an `Expression<Func<T, TKey>>` overload; this sample uses the in-memory `IEnumerable<T>` `Func` overload.
