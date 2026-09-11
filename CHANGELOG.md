# Changelog

All notable changes to the `DotNetCore.Collections` packages are documented here.
Versions follow [Semantic Versioning](https://semver.org/); every package in this
repository ships the same version (see `build/version.props`).

## [6.2.0] - Unreleased

Unreleased. The date is filled in when the release is tagged; entries land here as the work
completes.

### Added

- `OrderedMultiList<T>` - an ordered multiset (sorted bag), the ordered counterpart of
  `MultiList<T>` and the equivalent of PowerCollections' `OrderedBag<T>`. It shares
  `MultiList<T>`'s copy-counting semantics - one distinct element with N copies, duplicates
  consecutive and expanded on enumeration, the same `UnionWith` / `IntersectionWith` /
  `ExceptWith` / `SymmetricExceptWith` and subset / superset judgments - and adds an order:
  enumeration is ascending, and lookup, insertion and removal are O(log n) **worst case**.
  Elements are compared with an injectable `IComparer<T>`, deliberately not an
  `IEqualityComparer<T>`: an equality comparer supplies hash codes but no ordering, whereas a
  red-black tree has to know which of two elements comes first, and it is the comparison
  result `0` that decides two elements are the same element (the stored element is the first
  one added). `null` is supported - under `Comparer<T>.Default` it sorts first, while a custom
  comparer decides for itself where `null` belongs and may reject it.
- `OrderedMultiList<T>` ordered access: `GetFirst()` / `GetLast()` (each O(log n), throwing
  `InvalidOperationException` on an empty multiset), `Reverse()` for descending enumeration,
  and `GetRange(from, to)` plus a `GetRange(from, to, inclusiveFrom, inclusiveTo)` overload for
  range queries. Subtrees outside the bounds are skipped, so a query costs O(log n + k) for k
  copies reported rather than a full traversal. As with `MultiList<T>`, the type implements
  `ICollection<T>` and `IReadOnlyCollection<T>` (copy-expanded `Count`), and its set operations
  treat their argument as a **multiset**, so multiplicities count.
- F6-01's O(log n) claim is proved rather than timed. The storage engine is a self-implemented
  left-leaning red-black tree, and the test suite asserts the red-black invariants - black
  root, no red node with a red child, equal black height, plus the left-leaning rule - together
  with the height bound `height <= 2 * log2(n + 1)`. It re-checks them after every single
  removal of a 300-key drain, and across 5,000 randomized operations compared with a
  `SortedDictionary` model; 10,000 sequentially ascending inserts stay under 30 levels where an
  unbalanced tree would be 10,000 deep. Timing baselines are deliberately avoided - they are
  noise on CI.
- Two deliberate omissions and one carry-over, so that neither is mistaken for an oversight.
  `OrderedMultiList<T>` does **not** override `Equals` / `GetHashCode`: the package's rule is
  that `MultiList<T>` is the only type carrying structural equality, and adding equality later
  is a non-breaking change while removing it would not be. It has no `ToDictionary()` either,
  because its `IComparer<T>` orders elements but supplies no hash codes for a dictionary to use,
  so `EntrySet()` is the export path. And `Add(item, times)` / `Remove(item, times)` keep
  `MultiList<T>`'s legacy "a non-positive `times` is coerced to one copy" behaviour verbatim,
  which means M6-05 (`times <= 0` throws) has to cover this type as well when it lands.
- `DotNetCore.Collections.Multi` now declares `InternalsVisibleTo` for its test assembly. The
  red-black invariants live on internal members and asserting them is how F6-01 justifies its
  complexity, so the suite has to reach them. Nothing becomes public: the members stay internal.
- `OrderedMultiDictionary<TKey, TValue>` - the ordered counterpart of `MultiDictionary<TKey, TValue>`
  (F6-02): a multimap whose keys are kept in ascending order by an `IComparer<TKey>` (a
  `SortedDictionary` axis) and whose per-key values are kept in ascending order by an
  `IComparer<TValue>` (an `OrderedMultiList<TValue>` in the default duplicating configuration, a
  `SortedSet<TValue>` when duplicate values are disallowed). Adding, looking up and removing a
  single pair costs O(log n) worst case on both axes. The per-key value-set operations
  (`UnionWith` / `IntersectionWith` / `ExceptWith` / `SymmetricExceptWith` / `RemoveRange`) keep
  `MultiDictionary<TKey, TValue>`'s semantics verbatim: the argument is a **set** of values, a
  value stored N times survives `RemoveRange` with N-1 copies, `ExceptWith` drops every
  occurrence, and a key whose value collection empties is removed automatically. The type also
  implements `IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>>` and ships `AsLookup()` /
  `AsReadOnly()` / `Clone()` / `EntrySet()`, `ValueCount(key)` / `TotalValueCount`,
  `ContainsValue`, and `ToString()` walking keys ascending. Identity on both axes is decided by
  the respective comparers (`null` keys are rejected with `ArgumentNullException`; `null` values
  follow the value comparer, sorting first under the default one), and the `notnull` key
  constraint of the annotated `SortedDictionary` is suppressed file-locally, exactly as in
  `MultiDictionary`, so `TKey` stays nullable-friendly.
- `PageCreationOptions` and strict fragment checking (F6-11). 6.1 tolerated a fragment shorter
  than its metadata says - a concurrent delete upstream must not make the page unbuildable - and
  that stays the default: `Paginable.CreatePage(fragment, info)`, the four-argument
  `CreatePage` and `fragment.ToPage(...)` behave exactly as before. When a short fragment is
  more likely a bug than a race - a stale `totalMemberCount`, a fragment sliced by the wrong
  query - pass `PageCreationOptions.Strict` through the new overloads
  `CreatePage(fragment, info, options)` / `CreatePage(fragment, pageNumber, pageSize,
  totalMemberCount, options)` / `fragment.ToPage(..., options)`: the same situation throws
  `ArgumentException` naming the fragment. Only the short-fragment behaviour differs - the
  over-long checks throw in both modes - and a `null` options argument is rejected like any
  other.
- `Paginable.CreateSinglePageSet` and `PaginableSinglePage<T>` (P6-03) - the set shape of the same
  fragment API, for callers whose signature wants an `IPaginable<T>` (or an
  `IEnumerable<IPage<T>>`) while the data in hand is one already-assembled page. The fragment is
  wrapped as-is, so `GetPage(1)` is exactly the page `CreatePage` would have built, with identical
  metadata and members, and the same metadata and `PageCreationOptions` arguments apply.
  `PageCount` is **always one**, which is deliberately not the wrapped page's own
  `TotalPageCount`: a set must be able to serve every page it claims and this one holds a single
  page with no source to slice the others from. The set layer therefore answers "how many pages am
  I handing you" while the page keeps the source-wide numbering (a fragment of page 3 of 12 still
  reports page 3 of 12), and `MemberCount` reports the source-wide total, as
  `PaginableSetBase<T>` does. `GetPage` accepts only one and reports anything else as
  `ArgumentOutOfRangeException`. The factories return the concrete `PaginableSinglePage<T>`, not
  the bare interface, because `IPaginable` exposes only `PageSize` and `MemberCount` - without the
  concrete type the `PageCount` guarantee would be unreadable.

### Fixed

- `DotNetCore.Collections.Paginable.SqlKata` no longer carries vulnerable transitives in its
  dependency graph (E6-03). SqlKata's two usable lines are both frozen with advisories still open
  underneath them, and neither can be upgraded past the problem: `2.2.0` is the last release that
  targets `net451`, `3.2.3` is what the `netstandard2.x` / `net6.0` / `net7.0` group can take, and
  `4.x` requires `net8.0`. The three affected transitives are therefore pinned forward, one
  framework group at a time. On `net451`, `NETStandard.Library 1.6.1` (via SqlKata 2.2.0) used to
  supply `System.Net.Http 4.3.0` (CVE-2018-8292) and `System.Text.RegularExpressions 4.3.0`
  (CVE-2019-0820); they are now `4.3.4` and `4.3.1`. Because `net451` takes both of them from the
  framework — their NuGet assets are the `_._` placeholders — that pin costs no assembly and only
  settles the audit. On `netstandard2.0` / `2.1` / `net6.0` / `net7.0`, `Dapper 1.50.5` (via
  `SqlKata.Execution 3.2.3`) used to pin `System.Data.SqlClient 4.4.0` (CVE-2024-0056 /
  CVE-2022-41064); it is now `4.8.6`, the first release past both advisories. `net461` / `net47` /
  `net48` resolve the two `netstandard1.x` transitives from the framework and `net8.0` and above run
  on SqlKata 4.0.1, so those groups were already clean and are untouched. The audit is settled by
  fixing the graph rather than by `NoWarn`, and the raised floors are recorded in the shipped
  `packages.lock.json`.
- The `<example>` sections of `PaginableCalc.GetRealMemberCount` / `GetRealPageCount` show
  correctly-arity samples (P6-02). Both were added by the same docs pass and the first called
  `GetRealMemberCount(0, 50, 120)` — three arguments for a two-argument method — so the shipped
  IntelliSense sample did not compile. The pair is now a worked example that mirrors the actual
  call sites (`GetRealMemberCount(limitedMemberCount, count)` then
  `GetRealPageCount(realMemberCount, size)`), and the "null means unlimited" contract is stated
  where a reader meets it.

### Breaking

- An out-of-range argument is now reported as `ArgumentOutOfRangeException` by **every** paging entry
  point, replacing the `IndexOutOfRangeException` that the `GetPage` family and the keyset
  (`GetFirstPageByKeyset` / `GetPageByKeyset`) family used to throw. The change covers the core
  `IEnumerable<T>` / `IQueryable<T>` / `Task<IQueryable<T>>` paths, both keyset extension classes
  including the EF Core async pair, and all nine ORM integration packages — 40 throw sites in 10
  files. It closes a split the 6.1 fragment API opened: `Paginable.CreatePage` / `fragment.ToPage`
  already answered an out-of-range argument with `ArgumentOutOfRangeException` and named the
  offending parameter, so the same mistake (`pageNumber: 0`) reported a different type depending on
  which entry point the caller happened to use. `ArgumentOutOfRangeException` derives from
  `ArgumentException`, so callers catching `ArgumentException` (or `Exception`) are unaffected; a
  caller that specifically caught or asserted on `IndexOutOfRangeException` must be updated.
  **Only the type changes** — the rejection conditions are untouched (an empty source still yields a
  single empty page), and the exceptions now carry `ParamName`. The XML `<exception>` docs declare
  the type on all 21 affected public members, and the 15 new tests in
  `GetPageExceptionTypeTest` pin the exact type and parameter name per path.

- `MultiList<T>.Add(item, times)` / `MultiList<T>.Remove(item, times)` and their
  `OrderedMultiList<T>` counterparts now throw `ArgumentOutOfRangeException` when `times` is
  less than or equal to zero, replacing the legacy behaviour that silently coerced a
  non-positive count to one copy. The coercion hid mistakes: `Add(item, 0)` reported success
  while inserting a copy nobody asked for, and `Remove(item, 0)` silently removed one. Zero
  copies is already expressible as a no-op by simply not calling, so any non-positive value is
  now treated as a programming error and rejected with the offending parameter named. The
  single-copy `Add(item)` / `Remove(item)` overloads and `AddRange(items)` are unchanged. The
  XML `<exception>` docs declare the behaviour on all four affected members, and the tests pin
  the exception type on both classes.

## [6.1.0] - 2026-09-10

Release covering both shipped modules (`Paginable` and `Multi`); every package ships version
`6.1.0.0` (see `build/version.props`).

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
