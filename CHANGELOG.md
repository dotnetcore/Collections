# Changelog

All notable changes to the `DotNetCore.Collections` packages are documented here.
Versions follow [Semantic Versioning](https://semver.org/); every package in this
repository ships the same version (see `build/version.props`).

## [6.1.0] - 2026-09-XX

Unreleased. The date is filled in when the release is tagged; entries land here as the work
completes, so the same section also carries the 6.1 `Multi` changes.

### Added

- Paging objects can now be created directly from a materialized fragment plus paging metadata:
  `Paginable.CreatePage(fragment, pageNumber, pageSize, totalMemberCount)`, the
  `PageFragmentInfo` overload, the `PageFragmentInfo.FromMetadata` / `ToMetadata` round trip, and
  the `IEnumerable<T>.ToPage(...)` sugar. The fragment is **never re-sliced** and member item
  numbers match full-source pagination exactly, so a page built from a Dapper / hand-written SQL
  result, a cached page or an upstream API response (`items` + `totalCount`) is indistinguishable
  from one sliced out of the full source. The metadata is validated eagerly: `null` fragment,
  non-positive page numbers, an out-of-range page, a negative or oversized total count, and a
  fragment larger than the page it claims to be are all rejected at construction time. A fragment
  shorter than the metadata expects is tolerated and `CurrentPageSize` keeps reporting the
  metadata value. Closes [#8](https://github.com/dotnetcore/Collections/issues/8).
- `MultiList<T>` now implements `IEquatable<MultiList<T>>`: multiset **structural equality**,
  where two multisets are equal when both hold the same distinct elements with the same number
  of copies in any order, plus a `GetHashCode` that agrees with it (structurally equal
  multisets collapse into a single `HashSet<MultiList<T>>` entry). Element matching goes
  through the comparer of each side, exactly like the existing subset and superset judgments,
  and copy counts take part, so this is bag equality rather than set equality. `Equals(object)`
  is overridden to match; `==` / `!=` are deliberately left as reference comparisons, and no
  other `Multi` type gains equality.
- `MultiDictionary<TKey, TValue>` can now delete several values under a key in one call, and
  report how many it holds: `RemoveRange(key, values)` removes **one occurrence per distinct
  argument value** — the batch form of `Remove(key, value)`, so a value stored N times keeps
  N-1 copies (`ExceptWith(key, values)` stays the way to drop every occurrence) — and
  `ValueCount(key)` returns the number of values stored under a key, or `0` for a missing key
  instead of throwing. Both follow the conventions the type already had: the argument is a
  **set**, `null` elements are ordinary values, matching goes through the inner collection's own
  comparer, and the key is recycled as soon as its last value is removed. The batch form is a
  separate name rather than an overload `Remove(key, IEnumerable<TValue>)` because that overload
  is a source-breaking change: `map.Remove(key, null)` would become ambiguous (CS0121), since
  `null` converts to both `TValue` and `IEnumerable<TValue>`; `RemoveRange` also mirrors the
  existing `AddRange(key, values)`.

## [6.0.0] - 2026-09-10

Modernization release covering both shipped modules (`Paginable` and `Multi`).

### Added

- Keyset (seek) pagination: `GetFirstPageByKeyset` / `GetPageByKeyset` over
  `IQueryable<T>` and `IEnumerable<T>`, with an optional `descending` switch.
  Each page is a single `WHERE key > @lastKey ORDER BY key LIMIT @size` query —
  no `OFFSET` scan and no `COUNT(*)` round trip.
- True end-to-end asynchronous paging for EF Core, FreeSql and SqlSugar
  (`CountAsync` + `ToListAsync`, no synchronous database calls), with
  `CancellationToken` passthrough throughout the core and ORM integrations.
- `Multi` module rewritten: `MultiList<T>` (multiset / bag) plus a complete
  `MultiDictionary<TKey, TValue>` (multimap) with `AsLookup()`, per-key value
  set operations and a configurable inner-collection factory.
- XML documentation with `<example>` and `<exception>` sections on the public
  API surface.
- Symbol packages (`.snupkg`) and SourceLink on every package.
- Automated nuget.org publishing through GitHub Actions (`Release` workflow).

### Changed

- Target frameworks expanded to 11 TFMs (`net451`, `net461`, `net47`, `net48`,
  `netstandard2.0`, `netstandard2.1`, `net6.0` – `net10.0`), now including the
  `Multi` package.
- `Multi` targets `netstandard2.0`, `netstandard2.1` and `net6.0` upwards
  (previously `netstandard2.0` only).
- Paginable EF Core integration: `net8.0` consumes the EF Core 8.0.x assets and
  `net9.0` the 9.0.x assets (both previously resolved to 9.0.x).
- `PaginableSettings` values are validated on assignment; `PaginableSettingsManager`
  swaps an immutable snapshot atomically.
- Deterministic builds with locked dependencies (`packages.lock.json`).

### Fixed

- `EnumerablePage`: single `Skip`/`Take` materialization for non-`IList` sources,
  removing the O(skip²) cost of per-member `ElementAt` calls.
- `CurrentPageSize` integer-division defect on exact-multiple last pages.
- `MaxMemberItems` boundary is now an open interval (exactly 10,000,000 rows allowed).
- Argument validation (`pageNumber >= 1`, `pageSize >= 1`) enforced across the
  core and every ORM integration.
- NHibernate `AllValues` and core `QueryEntryState` now materialize lazily, once.

### Removed

- `PublishToMyget.bat`: the MyGet feed is no longer part of the release flow;
  local publishing is done with `scripts/Publish.bat`, CI publishing with the
  `Release` workflow.

## [5.0.0] - 2023-11-03

- Target frameworks `net451`, `net461`, `netstandard2.1`, `net5.0`.
- `Multi` module introduced (`MultiList`, `MultiDictionary`, `netstandard2.0`).

## [3.2.0] - 2020-05-16

- Added `DotNetCore.Collections.Paginable.SqlKata` integration.
- Split the FreeSql integration into `FreeSql` and `FreeSql.DbContext`.

## [2.1.4] - 2019-05-30

- Maintenance release of the 2.x line (Chloe, Dos.ORM, EF6, EF Core, FreeSql,
  NHibernate and SqlSugar integrations).

## [2.0.1] - 2019-02-16

- First stable 2.x release of the pagination extensions.

## [1.0.0-beta1] - 2017-08-03

- Initial preview of the pagination extensions.

[6.0.0]: https://github.com/dotnetcore/Collections/releases/tag/6.0.0
[5.0.0]: https://github.com/dotnetcore/Collections/releases/tag/5.0.0
[3.2.0]: https://github.com/dotnetcore/Collections/releases/tag/3.2.0
[2.1.4]: https://github.com/dotnetcore/Collections/releases/tag/2.1.4
[2.0.1]: https://github.com/dotnetcore/Collections/releases/tag/2.0.1
[1.0.0-beta1]: https://github.com/dotnetcore/Collections/releases/tag/1.0.0-beta1
