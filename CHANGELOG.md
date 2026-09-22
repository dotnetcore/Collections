# Changelog

All notable changes to the `DotNetCore.Collections` packages are documented here.
Versions follow [Semantic Versioning](https://semver.org/); every package in this
repository ships the same version (see `build/version.props`).

## [Unreleased]

### Added

- `Type.ToReadableString()` (F6-30) turns a `Type` into one readable line, for diagnostics, log
  messages and test failure output. Array ranks are kept, generic arguments are rendered
  recursively and namespaces are dropped, so
  `typeof(Dictionary<DateTimeOffset, IReadOnlyDictionary<string, int>>[,])` reads as
  `Dictionary<DateTimeOffset,IReadOnlyDictionary<String,Int32>>[,]` — the shape of the type rather
  than its assembly-qualified identity. Rank specifiers follow C# source order (outermost first), so
  the two jagged shapes stay distinguishable; nested types are joined with `.` instead of the
  reflection `+` separator; and an overload taking `TypeNameFormat.CSharp` gives the C# spelling of
  the same type, with primitive aliases as keywords and `Nullable<T>` as `T?`. A third overload caps
  how many levels of array / generic nesting are rendered and collapses the remainder to `...`.
  That cap is not decoration: a type built through reflection can nest arbitrarily deep, and an
  unbounded walk would overflow the stack inside the very diagnostic meant to explain the failure.
  The output is stable enough to assert on. No existing signature changed, so there is no
  `### Breaking` entry for this.

- `IMultiSet<T>`, `IMultiDictionary<TKey, TValue>` and `IBiMap<TLeft, TRight>` (F6-32) give the
  multiset, the multimap and the bidirectional map a shared contract, so code can be written against
  the behaviour instead of a concrete class and the implementation can be mocked, replaced or swapped.
  Each interface extends the read-only BCL abstraction that already describes it rather than restating
  it: `IMultiSet<T>` is an `IReadOnlyCollection<T>`, `IMultiDictionary<TKey, TValue>` an
  `IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>>` and `IBiMap<TLeft, TRight>` an
  `IReadOnlyDictionary<TLeft, TRight>`. `MultiList<T>` and `OrderedMultiList<T>` implement
  `IMultiSet<T>`; `MultiDictionary<TKey, TValue>` and `OrderedMultiDictionary<TKey, TValue>` implement
  `IMultiDictionary<TKey, TValue>`; `BiDictionary<TLeft, TRight>` implements `IBiMap<TLeft, TRight>`.
  The member sets are deliberately minimal — only what every implementation can honour. Set algebra,
  the comparer properties and the view/export helpers (`AsReadOnly`, `AsReverse`, `ToList`, `Clone`)
  stay on the concrete types: they are not uniformly available across the family (the concurrent and
  immutable variants have no algebra, and an immutable variant returns a new instance from what looks
  like a mutating call), and exposing them would have leaked which implementation sits behind the
  interface.
  Two members are where a naive abstraction would have gone wrong, and both are spelled out in the XML
  documentation of the interface. `Count` is the number of *copies* on `IMultiSet<T>` and the number of
  *keys* on `IMultiDictionary<TKey, TValue>` — the inherited member keeps exactly the meaning the
  concrete types already gave it, so re-typing a variable from a concrete type to the interface does
  not silently change what it counts. And `IMultiDictionary<TKey, TValue>.Values` deliberately hides
  the inherited per-key `Values` to stay the flattened sequence, matching the concrete types, with the
  grouped view left reachable through the inherited dictionary members.
  No existing behaviour or signature changed — every implementing member already existed with the same
  shape, so no overload resolution moved and no `### Breaking` entry is needed. The member sets are
  pinned by tests, and each interface is additionally exercised by a hand-written implementation that
  shares no code with these packages: the abstraction is only worth having if a third party can
  satisfy it.

- `OrderedMultiDictionary<TKey, TValue>` gained the positional read surface (F6-36) that its list
  counterpart received in 6.5.0 (F6-25). `GetByRank(rank)` returns the `(key, value)` pair holding the
  copy at a rank of the *expanded* sequence — the order the map itself enumerates in, keys ascending
  and each key's values ascending, counting copies rather than distinct values — `GetRank(key, value)`
  returns the rank of a value's first copy under the key, or `-1` when the key or the value is absent,
  `TotalValueCount` is the length of that sequence in O(1), and `GetMedian()` / `GetQuantile(q)` answer
  the two statistical reads on top of them with the same definitions the list uses (`GetMedian` takes
  the lower middle copy; `GetQuantile` uses the nearest-rank rule and rejects `NaN` and anything outside
  `[0, 1]`). All of them are **O(log n) worst case, on both duplicate policies**, and that is what
  forced the map's per-key buckets to become one uniform type: the deduplicating policy
  (`allowDuplicateValues: false`) held a `SortedSet<TValue>`, which has no notion of rank at all, so the
  two policies could not both have answered `GetByRank` before. Nothing observable changed besides
  resource use — the ordering, the duplicate policy, the views and every existing member keep the
  semantics they had, which the pre-existing suite confirms by passing with no test edit at all.

### Changed

- `OrderedMultiDictionary<TKey, TValue>` is now backed by the same order-statistic B+ tree as
  `OrderedMultiList<T>` (`OrderStatisticTree<TKey, TPayload>`), closing the gap 6.5.0 left open when it
  recorded that this type "never had a self-implemented engine, being built on the BCL's sorted
  dictionary over a per-key collection". Each key's slot now carries its bucket *and* how many copies
  hang under it, which is what makes the rank reads above a single descent, and enumeration a single
  sweep of the tree's leaf chain through value-type enumerators instead of one iterator per key. A
  payload channel added to the engine is what lets one tree serve both types: `OrderedMultiList<T>`
  instantiates it with an empty payload struct and pays nothing, while the map stores its bucket object
  in the same slot as the key and its count. The engine's depth bound was tightened from 40 to 12 in the
  same pass — 9 levels already cover more distinct keys than `int` can hold — cutting its per-instance
  scratch arrays from 480 to 144 bytes, and that is where the write path's allocation win below comes
  from: the buckets are the same type they were, but no longer carry 336 bytes of over-sized scratch
  space each. The compensation is not uniform, and the other direction is recorded too: keeping the
  cached copy totals in step makes writes slower, `Add` going 492.5 → 862.6 ns at 64 keys and
  1,097.1 → 1,547.9 ns at 4,096 keys in the benchmark's own terms — the same cost 6.5.0 recorded for the
  list's `Add` when it gained this engine. The reference arm moved by at most 8% between the two runs,
  so the direction is not machine drift. Enumeration, meanwhile, is about three times faster: 410.90 →
  138.43 µs over 4,096 keys and 9,180.87 → 5,872.90 µs over 65,536.

### Fixed

- `DotNetCore.Collections.Paginable.SqlSugar`: `ToPaginableAsync` and the short `GetPageAsync`
  overload accepted a `CancellationToken` and then dropped it — it never reached the factory, so
  cancelling had no effect on either call (F6-38). The token is now threaded through the factory and
  through the page fetch, and it is honoured at the boundary this library owns: an already-cancelled
  token stops the call before the first database round-trip instead of performing it. The token could
  not simply be handed on because SqlSugar 5.1.3 exposes `CountAsync()`,
  `CountAsync(Expression<Func<T, bool>>)` and `ToPageListAsync(int, int, RefAsync<int>)`, and no
  `CancellationToken` overload on any of them; mid-query cancellation therefore stays the provider's
  to provide, and `SqlSugarHelper` is the single place that changes when it does. Callers who passed
  a token and relied on it being ignored now observe `OperationCanceledException` — the behaviour the
  parameter always promised, so there is no `### Breaking` entry for this.

- `OrderedMultiDictionary<TKey, TValue>` no longer allocates per key when it is enumerated (F6-36).
  Enumerating 65,536 keys allocated 7,340,454 bytes — a flat ≈112 bytes per key — and 459,104 bytes at
  4,096 keys, the same per-key figure at both sizes, which is what identified it as a fixed cost per key
  rather than growth. The cause was in the enumeration itself: the map walked a
  `SortedDictionary<TKey, ICollection<TValue>>`, so `pair.Value`'s static type was the *interface*
  `ICollection<TValue>`, and every key therefore built an `OrderedMultiList<TValue>.GetEnumerator()` — a
  `yield` iterator — which in turn walked the likewise-`yield` tree iterator: two iterator objects per
  key. Measured with `BenchmarkDotNet`'s `MemoryDiagnoser` on the same benchmark, before and after, the
  same figures are now 96 bytes at 4,096 keys and 99 bytes at 65,536 — a constant that does not grow
  with the key count. The write path allocates less as well, `Add` going 93 → 46 bytes at 64 keys and
  656 → 423 bytes at 4,096 keys.

## [6.5.0] - 2026-09-22

### Added

- `OrderedMultiList<T>` gained its positional read surface (F6-25): `GetByRank(rank)` returns the
  element sitting at a rank of the *expanded* sorted sequence — counting copies, not distinct
  elements, so the valid range is `[0, TotalCount-1]` and anything outside throws
  `ArgumentOutOfRangeException` — `GetRank(item)` returns the rank of an element's first copy or
  `-1` when it is absent, and `GetMedian()` / `GetQuantile(q)` answer the two statistical reads on
  top of them (`GetMedian` takes the lower of the two middle copies; `GetQuantile` uses the
  nearest-rank rule and rejects `NaN` and anything outside `[0, 1]`). All four are **O(log n) worst
  case**, which the engine swap recorded below is what makes possible: each of the type's nodes
  caches how many copies hang in its sub-tree, so a rank read is one descent down that sum instead
  of a scan. Measured against the only thing the type could do before — give up and materialise the
  expanded sequence, then index or search that copy — with BenchmarkDotNet + MemoryDiagnoser
  (net8.0, `Distinct` keys at three copies apiece, 64 reads per operation): over 4096 distinct keys
  `GetByRank` is 31.7 ns against 1,943 ns, `GetRank` 78.9 ns against 2,453 ns, and at 65,536
  distinct keys the ratios widen to 688x and 1,020x, with 0 B of garbage against 12 kB per read.
  No existing signature changed, so there is no `### Breaking` entry for this.

- `OrderedMultiList<T>` now implements `IReadOnlyList<T>` and `IList<T>` (F6-27), so it can be handed
  to anything expecting a list — data binding, position-based LINQ, third-party libraries — without
  copying it into a `List<T>` first. The index domain is the *expanded* sequence, matching what the
  type already did: `Count` counts copies, so `shelf[i]` is `GetByRank(i)` and `IndexOf(item)` is
  `GetRank(item)`, both O(log n) and both addressing the i-th copy rather than the i-th distinct
  element. `RemoveAt(i)` drops exactly one copy. The two members that would let a caller *place* an
  element are narrowed rather than free, because a comparer decides where an element belongs:
  `Insert(index, item)` accepts only a slot inside the run of equal elements — every accepted index
  yields the same multiset, so the parameter is a consistency check and not a placement — and throws
  `ArgumentOutOfRangeException` for any other, since a sorted sequence has no such position; and
  assigning through the indexer throws `NotSupportedException`, since writing over a position would
  break the order the whole type is built on. `IsReadOnly` stays `false` and `Add` / `Remove` /
  `RemoveAllCopies` are unchanged. `AsReadOnly()` keeps its declared `IReadOnlyCollection<T>` return
  type — widening it would be a binary-breaking change for existing consumers — but the view it
  returns now also answers `IReadOnlyList<T>`, with the same positional contract and no mutating
  member at all, which is why there is no `IsReadOnly` / `IsFixedSize` to define for it. The
  unordered `MultiList<T>` deliberately does not get the list interfaces: its enumeration order is an
  implementation detail of the hash table, so an index would name a different element from one call
  to the next, and that is now stated on the type. No member was removed, renamed or re-signed, so
  there is no `### Breaking` entry: the one consumer-visible hazard of implementing an interface — a
  call that used to resolve against an `IEnumerable<T>` arm now also seeing an `IList<T>` arm — is an
  overload-set ambiguity in the caller, not a change in what this type does.

- Documentation: [`docs/positioning.md`](docs/positioning.md) states where the library fits — the
  capability axes it was built around (multisets and multimaps, composite keys, bijection, inverted
  lookup, order *and* position, concurrency and immutability as explicit types, paging as a library,
  packed and stack-only specializations, `net451` … `net10.0` from one code path), the six things it
  deliberately does not provide and what to use instead, and how to choose between the hash path and
  the tree path. It also records the limits in both directions rather than only the favourable ones.

- Documentation: [`docs/migrating.md`](docs/migrating.md) is the guide for a codebase moving its
  sorted, positional and multiset work onto `DotNetCore.Collections.Multi` from one of the classic
  ordered-collection libraries — type and member mapping by shape, the twelve semantic differences
  that change behaviour rather than spelling, a migration checklist, and the operations that have no
  counterpart here.

### Changed

- `OrderedMultiList<T>` is now backed by an *order-statistic B+ tree* (`OrderStatisticTree<TKey>`)
  instead of the internal left-leaning red-black tree, which is deleted. This is an engine swap, not
  a semantic one: counted duplicates, the first-added key winning a comparer collision, `null`
  handled entirely by the comparer, injected comparers, and shallow-copy semantics (`Clone`) all
  behave exactly as before, verified by the pre-existing ordered-multiset suite running unchanged
  against the new engine - the only test edits were assertions about the old tree's internal
  *shape*, its height bound and the layout of a single-key tree. The swap is what the rank reads
  above need, and it pays off independently on the paths that were already there:
  ascending iteration over 12,288 copies went 129.9 → 75.6 µs and over 196,608 copies 2,662 → 1,284
  µs with per-operation allocations 440 → 112 B, a 512-wide `GetRange` inside 65,536 distinct keys
  went 176 → 11.3 µs and 92 kB → 152 B, `RemoveAllCopies` went 3.35 → 1.28 µs per key. The
  compensation is not uniform and the other direction is recorded too: a wide node trades the
  red-black tree's single-key hop for a binary search inside a page, so a *small, single-key* probe
  got slower (`CountOf` over 64 distinct 12.5 → 30.8 ns, `Add` 390 → 472 ns). `OrderedMultiDictionary`
  is deliberately untouched — it never had a self-implemented engine, being built on the BCL's sorted
  dictionary over a per-key collection, so giving it the same treatment is a separate piece of work.
  Alongside: the XML example for `GetRange` was corrected, since `GetRange("a", "n")` over
  `mug, bean, bean` returns `"bean", "bean", "mug"` — `"mug"` sorts below `"n"`.
- The paging extensions of six ORM integration packages (`Chloe`, `DosOrm`, `FreeSql`, `SqlSugar`,
  `NHibernate`, `SqlKata`) are now emitted at build time by a Roslyn source generator
  (`src/DotNetCore.Collections.Paginable.SourceGenerators`) instead of being handwritten once per
  provider. Each package declares what its paging surface looks like — source type and parameter
  name, page / paginable-collection / factory / helper type names, and which optional shapes it
  carries (`additionalQueryFunc`, `includeNestedMembers`, async members, the `class` constraint) —
  with a single `[SolidPageExtensionsFor]` attribute on the `SolidPageExtensions` partial class, and
  the generator writes out the `ToPaginable` / `ToPaginableAsync` / `GetPage` / `GetPageAsync`
  members from that declaration. Provider-specific members that do not fit the shape stay
  handwritten inside the same partial (NHibernate's `ISession` overloads, SqlKata's `GetPageAsync`).
  Chloe's extension class is now a 19-line declaration where it was 95 lines of handwriting,
  Dos.ORM's 21 where it was 99. The three packages that also target `net451` (FreeSql, SqlKata,
  SqlSugar) reference the generator for every other target and keep their handwritten members
  behind `#if NET451`; the legacy target keeps the same members, and carries the class-level XML
  summary in the handwritten partial itself so that it too compiles warning-free (see `### Fixed`
  below).
  **No consumer-visible change, and therefore no `### Breaking` entry for this**: the generated
  members are the same members with the same signatures - including parameter names, which are
  source-affecting for named-argument callers - and the same XML documentation, so the shipped
  IntelliSense is unchanged. Verified by a regression suite that runs the generator over each of the
  six packages and compares every emitted member (signature, body, documentation) against the
  pre-change source, held as test resources. Packaging is untouched as well: the generator is an
  analyzer, so no package gains a dependency and none of them ships the generator assembly. The
  EF6, EF Core and FreeSql.DbContext integrations are deliberately not migrated - they delegate to
  the queryable extensions rather than to a provider-specific paging factory, so their bodies were
  never the duplicated shape this replaces.

### Fixed

- The three ORM integration packages that still target `net451` (`FreeSql`, `SqlKata`, `SqlSugar`)
  emitted `CS1591` for their `SolidPageExtensions` class on that target alone. The class-level
  `<summary>` had moved into the source generator, and the generator is deliberately not referenced
  on `net451`, so on that target nothing documented the type while the project still compiles with
  XML documentation required. Each handwritten `#if NET451` partial now carries the same
  class-level summary the generator emits, so the shipped XML documentation for the type is
  identical on every target and the build is warning-free again. No API change, and the three
  packages that never targeted `net451` (`Chloe`, `DosOrm`, `NHibernate`) were unaffected.

## [6.4.0] - 2026-09-16

### Added

- `FrequencyPriorityBag<T>` - the frequency priority bag (F6-08): a bag that knows which element
  occurs most often, so "pop the most frequent element" and Top-K queries are first-class
  operations (`PeekMost` / `TryPeekMost` / `PopMost` / `TryPopMost`; repeated `PopMost` drains the
  bag in descending-frequency order). A `Dictionary` frequency index is the source of truth; the
  max-heap over its counts is materialized **lazily** - every update touches only the index (O(1))
  and marks the heap dirty, the next priority query rebuilds it in O(n) (bottom-up heapify), and
  while it stays clean each further peek is O(1) and each pop O(log n). **Tie policy** (documented
  on the type): among elements with the same count, the one that reached its current count
  *earliest* pops first - every count change stamps a fresh sequence number and the heap breaks
  ties on the earlier stamp, a deterministic FIFO-flavoured rule. Bag semantics mirror
  `MultiList<T>`: counted duplicates, `Add(item, times)` / `Remove(item)` (returns the number
  remaining), an element disappearing with its last copy, `null` as a first-class element in a
  dedicated bucket (with its own tie stamp), copy-expanded enumeration and `EntrySet()`. Measured
  against re-sorting a `MultiList<int>`'s entries (BenchmarkDotNet + MemoryDiagnoser, net8.0,
  512 adds over 32 distinct): for the workload the type exists for - repeated Top-1 queries
  against live data - ~3.2x faster with ~2.9x less garbage; build-once-query-once and
  churn-interleaved workloads land at parity, where sorting once is just as good - the boundary is
  documented rather than hidden. Not thread-safe.
- `SpanBag<T>` - the stack-only temporary bag (F6-07): a fixed-capacity counting bag over
  `unmanaged` elements backed by caller-provided `Span<T>` storage (typically `stackalloc`), for
  short-lived frequency counting inside one method with zero heap allocation. The bag is a
  `ref struct`, so the compiler itself enforces the lifetime contract - no fields, no boxing, no
  capture, no crossing `await` / `yield` boundaries - and there is deliberately no factory method:
  the caller allocates (a `stackalloc` inside a factory would die with the factory's frame), the
  caller owns the lifetime, and `Clear()` reuses the same stack memory for the next counting round
  without touching the allocator. Bag semantics mirror `MultiList<T>` where the two meet (counted
  duplicates, `CountOf`, `Remove` returns the number remaining, entries packed with an element
  disappearing at its last copy); adding a *new* element when full returns `false` - a sizing
  condition, not an exception - while an already-present element always fits. Pattern-based
  `foreach` yields the packed `(value, count)` entries. Measured (BenchmarkDotNet +
  MemoryDiagnoser, net8.0, 64 reads over 8 distinct values): the whole counting round runs ~1.9x
  faster than a `Dictionary<int, int>` and allocates **0 B** against its 352 B (and `PackedBag`'s
  224 B); against a `HashSet<int>` in a first-duplicate scan the set is slightly faster but pays
  392 B per call - the span bag's win is the garbage-free path. Available only where `Span<T>` is
  in-box (netstandard2.1, net6.0+): the legacy targets would require the external System.Memory
  package, and the package has stayed dependency-free on every TFM so far.
- `PackedBag<T>` - the packed value-type counting histogram (F6-06): a bag over `struct` elements
  (`int`, enums, small structs) stored as one contiguous array of `(value, count)` struct entries
  instead of a hash table - no boxing in storage, one cache line per element, zero steady-state
  allocation on add and lookup. Bag semantics mirror `MultiList<T>` where the two meet: duplicates
  are counted (`Add(item, times)`, `CountOf`, copy-expanded enumeration with an element's copies
  consecutive), `Remove(item)` takes one copy away and returns the number remaining, and an entry
  is dropped - keeping the array packed, no zero-count holes - once its last copy goes.
  `EntrySet()` / `DistinctItems()` give the compact histogram views, `TotalCount` / `DistinctCount`
  the cached counts, plus `Clone` / `ToDictionary` / `TrimExcess`. The measured trade-off
  (BenchmarkDotNet + MemoryDiagnoser, net8.0, against `MultiList<int>`): building a 32-value
  histogram allocates ~2.7x less (608 B vs 1616 B) and runs ~1.4x faster, whole-histogram
  enumeration is ~1.5x faster with ~1.5x less garbage, and at 4 distinct values point operations
  are 1.7-2.1x faster; the O(1) dictionary lookup catches up around 16-32 distinct values, so the
  type targets dense domains of roughly up to ~16 distinct values. The `struct` constraint is the
  documented boundary - `null` and `Nullable<T>` are excluded by design, set operations, equality
  and comparer injection are deliberately not carried over, and `MultiList<T>` remains the
  general-purpose bag.
- `MultiKeyMultiDictionary<TKey, TValue>` - the composite-key multimap (F6-05): a dictionary whose
  keys are sequences of components of one type (`TKey[]`) and each complete key maps to a
  *collection* of values — the combination of `MultiKeyDictionary<TKey, TValue>` (N components
  &#8594; 1 value, a trie) and `MultiDictionary<TKey, TValue>` (1 key &#8594; N values, a
  multimap). The trie's prefix projection carries over: `GetByPrefix` enumerates every complete key
  stored under a partial key together with its live value collection (full keys, or suffix-only
  with `relative: true`), `CountOfPrefix` counts those keys, `GetSuffixes` / `GetBranches` expose
  the trie's shape, and `RemovePrefix` cascade-deletes a whole subtree, returning the number of
  values destroyed. The value side mirrors `MultiDictionary`: a configurable inner collection
  factory (a duplicating `List<TValue>` by default, a deduplicating `HashSet<TValue>` under
  `allowDuplicateValues: false`), `AddRange` / `RemoveRange`, the per-key value set operations
  `UnionWith` / `IntersectionWith` / `ExceptWith` / `SymmetricExceptWith` (their argument is a
  *set*, exactly as in `MultiDictionary`), `ValueCount(key)` plus the O(1) cached
  `TotalValueCount`, and the "no value-less key" invariant — a key is pruned from the trie
  automatically once its last value is removed, so `Count` / `KeyCount` never report a key without
  values. `null` key components are supported through the trie's dedicated bucket (a `null` key
  *array* is rejected with `ArgumentNullException`), and `null` values are ordinary values. The
  composite key of this type is a trie key of homogeneous components of any arity, addressable by
  prefix — not the fixed-arity, per-axis-typed scheme of `TwoKeyDictionary<K1, K2, V>` /
  `ThreeKeyDictionary<K1, K2, K3, V>`, which remain the strongly typed facades for two or three
  differently typed components addressed in full. Inner value collections are exposed as
  `IReadOnlyCollection<TValue>` through an internal live wrapper rather than a bare cast, so the
  contract holds on every supported target — including net451 / net461, where the framework's
  `HashSet<T>` does not declare that interface (the hazard F6-24 fixes for `MultiDictionary`).

### Breaking

- The read-only value views of the multimaps are now served through an internal live wrapper
  instead of a bare cast to `IReadOnlyCollection<TValue>` (F6-24, root cure): the affected
  members are `MultiDictionary<TKey, TValue>`'s indexer / `TryGetValue` / `AsReadOnly` /
  `ToDictionary` / enumerations, its `AsReverse()` key collections, and the same surface of
  `OrderedMultiDictionary<TKey, TValue>`. **No signature changes anywhere** - every member still
  declares the same `IReadOnlyCollection<TValue>` / `IReadOnlyDictionary<...>` return type, and
  the views remain live. What changes is the concrete object behind the interface: it used to be
  the inner `List<TValue>` / `HashSet<TValue>` / `SortedSet<TValue>` itself on runtimes where
  those declare `IReadOnlyCollection<T>` (modern .NET), and is now always an internal wrapper.
  Code that cast the returned view back to the concrete inner collection type (e.g.
  `(HashSet<TValue>)map[key]`) - never a supported pattern - will now throw; code working against
  the declared interfaces is unaffected. The reason for the change: the .NET Framework
  generation the package supports does not declare the interface on `HashSet<T>` /
  `SortedSet<T>` - the net451/net461 reference assemblies reject a direct assignment
  (CS0266) and the 4.5.1/4.6.1-era runtimes throw `InvalidCastException` on the cast - so the
  bare cast was a runtime crash exactly on the deduplicating inner collections
  (`allowDuplicateValues: false`) of those low-generation consumers. The wrapper removes the
  dependency on the interface declaration entirely, on every target.
- Nothing else. The four new types of this cycle (`MultiKeyMultiDictionary<TKey, TValue>`,
  `PackedBag<T>`, `SpanBag<T>`, `FrequencyPriorityBag<T>`) are additive.

## [6.3.0] - 2026-09-14

### Added

- `BiDictionary<TLeft, TRight>` - a strict one-to-one (bijective) map (F6-03): every left value
  maps to exactly one right value and no right value is shared by two lefts, with both
  directions answered in O(1) by two indexes kept in step on every write path — so a removed
  or rebound entry frees its partner immediately on the other side. Conflict handling is
  **strict** (R3-01 decision): `Add` throws `ArgumentException` when the left value already has
  a binding or when the right value is already bound to a different left value, `TryAdd`
  reports the same conditions without throwing, and there is deliberately no
  silently-overwriting setter — overwriting a right value would silently unbind the left value
  it used to belong to, an entry the caller never mentioned, so breaking an existing binding is
  always an explicit `Remove(left)` or `RemoveRight(right)` first. The type implements
  `IReadOnlyDictionary<TLeft, TRight>` for the forward direction and exposes the reverse
  direction through `TryGetLeft` / `GetLeft(right)` / `ContainsRight` plus `AsReverse()`, a live
  read-only `IReadOnlyDictionary<TRight, TLeft>` view served from the same indexes. `null` is
  accepted on both sides through dedicated buckets (the binding `(null, null)` is expressible
  and occupies one entry); `ToDictionary()` exports an independent snapshot that omits a
  `null`-left binding, because a `Dictionary<TLeft, TRight>` can not key on `null` — the remark
  says so explicitly.
- `ReverseMultiDictionary<V, K>` - the inverse of `MultiDictionary<TKey, TValue>` (F6-04): a
  snapshot mapping every stored value to the set of keys that hold it, so "which keys store this
  value?" is answered in O(1) (with `MultiDictionary.AsReverse()` as the live counterpart, see
  below). The constructors copy the bindings out of the source map into a fully self-contained
  instance — no reference chain, so the snapshot stays as it was built while the map changes or
  is dropped, and it is safe to serialize (R3-02 decision: snapshot type + live view, no
  bidirectional synchronization). The instance is also a standalone mutable collection of its
  own, and the "no value-less key" invariant of `MultiDictionary` is mirrored — a value
  disappears automatically once its last key is removed. Per value the keys form a set, so a key
  that stores one value several times is listed once (multiplicities collapse); value equality
  on the indexed axis is `EqualityComparer<V>.Default` (the notion the map's own backwards index
  uses) and the stored keys compare with the source map's key comparer by default. The member
  names mirror `MultiDictionary` with the axes swapped: `Count` / `ValueCount` count distinct
  values, `TotalKeyCount` counts distinct bindings, `KeyCount(value)` counts one value's keys,
  `ContainsValue(value)` is the O(1) presence check while `ContainsKey(key)` scans the stored
  keys, and a missing value yields an empty collection rather than throwing. A `null` value of
  the source surfaces as a `null` entry of the snapshot through a dedicated bucket, while a
  `null` key is rejected with `ArgumentNullException` — the exact mirror of the source map,
  which allows `null` values and rejects `null` keys.
- `MultiDictionary<TKey, TValue>.AsReverse()` - the live half of the inversion entry points: a
  read-only `IReadOnlyDictionary<TValue, IReadOnlyCollection<TKey>>` view served directly from
  the backwards index the map already maintains for `ContainsValue` (plus the `null`-value
  bucket), so building it costs nothing, every read runs in O(1), and every subsequent mutation
  of the map is visible immediately. The same collapse, comparer and `null`-entry semantics as
  `ReverseMultiDictionary<V, K>`; a value nobody stores yields an empty collection rather than
  an exception, matching the map's own indexer. The view exposes internal state without copying
  and is not thread-safe.

### Changed

- **All eleven shipped packages now compile with nullable reference annotations enabled and
  ship a fully annotated public API (F6-23)**: `DotNetCore.Collections.Multi`,
  `DotNetCore.Collections.Paginable` and the nine ORM integration packages build warning-free
  under `<Nullable>enable</Nullable>` across their whole target matrix — zero CS86xx
  diagnostics, none silenced with `NoWarn` — so consumers who enable nullable in their own
  project get accurate null-state analysis of these signatures instead of oblivious ones.
  The full audit confirmed `DotNetCore.Collections.Multi` had been annotated since its
  rebuild; the annotation work landed in the Paginable module and the integration packages.
- One annotation is visible to consumers: `KeysetPage<T>.LastMember` is now declared
  `T?`. It has always returned `default` — i.e. `null` for reference element types — when
  the page is empty; the annotation now says so, so nullable-enabled callers are prompted to
  check `CurrentPageSize` before dereferencing the anchor. No runtime behaviour changed
  anywhere; this entry is annotation-only.
- Low-generation targets (`net451` / `net461` / `net47` / `net48` / `netstandard2.0`) have no
  in-box nullable attribute types; a single shared **internal** polyfill of the compiler's
  nullability attributes (`NullableAttribute` / `NullableContextAttribute` /
  `NullablePublicOnlyAttribute`, `build/NullabilityAttributes.cs`) is compiled into every
  assembly for exactly those targets, mirroring what Roslyn embeds by itself when no
  definition is reachable. `netstandard2.1` and `net6.0+` define the types in-box and do not
  take the copy. No target framework was raised and no external package was introduced.
- Dead private parameterless constructors on the paging collections (never callable, kept
  only as ReSharper noise) were removed; they were the only path on which a paging
  collection could exist with an unassigned internal state.

### Breaking

- None. This release is additive and annotation-only: the three new public types
  (`BiDictionary<TLeft,TRight>`, `ReverseMultiDictionary<V,K>`, and
  `MultiDictionary<TKey,TValue>.AsReverse()`) extend the surface without changing any
  existing signature, and the nullable rollout (F6-23) is annotation-only —
  `KeysetPage<T>.LastMember` is now declared `T?` but its IL signature and runtime
  behaviour are unchanged.

## [6.2.0] - 2026-09-12

Release covering both shipped modules (`Paginable` and `Multi`); every package ships version
`6.2.0.0` (see `build/version.props`).

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
- `TwoKeyDictionary<K1, K2, V>` now answers its second axis from a maintained reverse index
  (F6-21). The underlying trie is keyed by `(K1, K2)` in that order, so the second component is
  not a prefix and a subtree walk can not reach it; `GetBySecondKey`, `CountOfSecondKey`,
  `ContainsSecondKey` and `RemoveBySecondKey` therefore used to scan the whole map. They now read
  a `K2 -> set of K1` index kept in step on every write path - `Add` (both overloads), `TryAdd`,
  `Remove` (both overloads), the `RemoveByFirstKey` cascade, `RemoveBySecondKey` and `Clear` - so
  only the requested slice is visited: O(1) for `CountOfSecondKey` and `ContainsSecondKey`, and
  O(s) trie lookups for a slice of s entries for the other two. The index uses the same injected
  comparers as the map (the second one keys the index, the first one the per-second-key set), and
  a `null` second component gets a dedicated bucket because `Dictionary<TKey, TValue>` rejects a
  `null` key. The cost is one extra dictionary operation per write and one set entry per stored
  pair, plus one temporary list per `RemoveByFirstKey` cascade. `Keys2` deliberately still walks
  the trie, preserving its first-encounter enumeration order. The `GetBySecondKey` XML docs no
  longer describe the method as O(n).
- Explicit serialization entry points for `MultiList<T>` and `MultiDictionary<TKey, TValue>`
  (M6-06): `ToSerializableModel()` and the static `FromModel()`, backed by two new plain models.
  `MultiListModel<T>` carries `Items` (the distinct elements) plus `Counts` (one multiplicity per
  item); `MultiDictionaryModel<TKey, TValue>` carries `Keys` plus `Values` (one value list per key).
  Both are ordinary mutable POCOs - public settable properties, no attributes, no interface
  implementations, no base type - so serializing them needs no particular serializer and the
  library takes a dependency on none; `System.Text.Json` is one option among many rather than a
  requirement, and the tests assert that the shipped assembly references no serializer at all.
  Three points are deliberate. The models carry **data only**: comparers and the multimap's
  inner-collection strategy are configuration rather than data, so they are not serialized and
  `FromModel` takes them as arguments (the rebuilt collection is therefore only as faithful as the
  comparer passed back in). The models handle what `ToDictionary()` can not: a `null` element is an
  ordinary entry in `Items`, whereas `ToDictionary()` has to throw because a `null` can not be a
  dictionary key (a `null` key still can not be rebuilt, since the multimap rejects those). And a
  model is a snapshot while `AsReadOnly()` and `ToDictionary()`'s inner collections are live views,
  which is the property a serializer needs. `FromModel` validates the model rather than trusting it
  (null model, null list, mismatched lengths, non-positive copy count, null inner value list), names
  the offending argument, and merges elements a comparer calls equal in the multiset reading.
- Two caches under `MultiDictionary<TKey, TValue>` (M6-07), both landing L-05 and L-06 and both
  invisible from the outside: **no signature changed and no behaviour moved**, so the full suite
  passes unchanged and the pair is a pure performance change. `TotalValueCount` is now a stored
  count maintained by every mutation rather than a walk over all inner collections (L-06), and
  `ContainsValue` is answered by a backwards index - value &#8594; keys - maintained on the write
  path rather than by scanning every key's values (L-05). Both caches are updated by *every* path
  that can move them, which is the whole risk of the change: `Add` (including the branch where a
  custom inner factory hands back a non-empty collection), `AddRange`, both `Remove` overloads,
  `RemoveRange`, `IntersectionWith`, `ExceptWith`, `SymmetricExceptWith`, `Clear` and `Clone`
  (rebuilt through the public `Add` so the caches are maintained rather than copied). Three details
  are deliberate. A `null` value is an ordinary value and gets a dedicated bucket, because a
  `Dictionary<TValue, …>` can not key on `null`; value equality is
  `EqualityComparer<TValue>.Default`, the same notion the per-key collections use. Key membership
  in the index uses the map's own `IEqualityComparer<TKey>`, so removing `"a"` clears the entry
  indexed under `"A"` when the comparer says they are the same key. And a value index entry is
  dropped as soon as its last key is gone, so `ContainsValue` never answers from a value nobody
  stores. The write-path cost the acceptance criteria asked to measure is pinned the same way the
  gain is: a value type counts its own equality and hash operations, and the suite asserts that
  maintaining the index during an `Add` costs a constant few, whether 1 or 4,096 values are already
  stored, while a `ContainsValue` on 4,096 values costs the same as on 64. Allocation of the read
  paths is asserted to be zero via `GC.GetAllocatedBytesForCurrentThread()`. As usual the claim is
  proved by counting, not by a stopwatch. A 3,000-step randomized differential re-checks both
  caches after every single step against a naive recomputation.
- The set operations of `MultiList<T>` no longer copy their argument (M6-08, landing L-07). Every
  one of them - `UnionWith`, `IntersectionWith`, `ExceptWith`, `SymmetricExceptWith`, the four
  subset/superset judgments, and `Equals`/`GetHashCode`'s helpers - used to start by materialising
  `other` as a whole second `MultiList<T>`, so a chained `a.UnionWith(b)` allocated a full multiset
  just to read it back; on a 32-element argument that was 1,712-2,440 bytes per call. When the
  argument already **is** a `MultiList<T>` whose element comparer is equivalent to the receiver's,
  it is now read in place: the operation walks its count table directly, which is a struct-enumerator
  walk. The three mutating operations additionally stage their target state in a single buffer that
  is allocated once per instance and reused (cleared afterwards, so it retains no element
  references). Measured with `GC.GetAllocatedBytesForCurrentThread()`: every one of the eight
  operations allocates **0 bytes/call** in steady state, and five of them (the four judgments and
  `UnionWith`) allocate 0 bytes/call even on a receiver that has never run a set operation before -
  the remaining three pay ~310 bytes once, for that buffer, on a cold receiver only. Three points
  are deliberate. An argument of any other shape - an array, a LINQ sequence, or a `MultiList<T>`
  built with a different comparer - still goes through the original materialising path, because its
  multiplicities have to be counted under the receiver's comparer before the operation can define
  its result; that path is unchanged and still correct, and the tests assert the two argument shapes
  never disagree. The `null` bucket is handled separately and only touched when it can be non-empty,
  because for a value element type it is always empty and `default!` would otherwise name the
  element `default(T)` - `0` in a `MultiList<int>` - and wipe it. And the behaviour is otherwise
  bit-for-bit what it was: `IsSubsetOfBag`/`IsSupersetOfBag` were rewritten to walk the count tables
  instead of `EntrySet()`, whose iterator allocated on every call, with the subset/superset and
  `null` comparisons preserved. A 3,000-step randomized differential runs each operation twice,
  once with a multiset argument and once with an equivalent plain array, and requires the two
  outcomes to agree at every step.
- `ImmutableMultiList<T>`, `ImmutableMultiDictionary<TKey, TValue>`, `ConcurrentMultiDictionary<TKey, TValue>` and `ConcurrentMultiList<T>` (M6-09, closing L-09) — the thread-safe faces of the package. The two immutable types wrap a snapshot of the mutable type that is never touched after construction, so reading needs no locks or fences. Every mutation returns a new instance, or the receiver itself when nothing would change (an immutable instance may safely be shared, so "no change" needs no copy); bulk mutation is expected through `ToBuilder()`. The builder shares the source's state until its first write — a copy-on-write the caller can observe, because `ToImmutable()` on an untouched builder hands back the very source instance — and a freeze moves the working state into the frozen instance while the builder continues on a private copy, so writes after a freeze can never leak into it. The two concurrent types keep the mutable semantics under contention. `ConcurrentMultiDictionary<TKey, TValue>` routes keys to shards, each an independent `MultiDictionary<TKey, TValue>` behind its own lock, so writes on different keys proceed in parallel, while whole-map reads take a consistent snapshot by locking every shard once, in index order. `ConcurrentMultiList<T>` deliberately stays single-lock, because a bag has one global state its operations compare against, and sharding would trade that simple semantics for little gain. Enumeration on both concurrent types is over a snapshot, immune to concurrent writes. The stress tests run eight writers over a shared key domain while three readers hammer snapshots and whole-map reads mid-flight; the settled state is exactly predictable, because every writer removes only occurrences it added itself.
- `ThreeKeyDictionary<K1, K2, K3, V>` (M6-13) — the three-component counterpart of `TwoKeyDictionary<K1, K2, V>`, kept deliberately thin (R2-03): the same axis-tag scheme — every component stored wrapped in a tag recording the position it came from, so three same-typed axes can not collide and each axis's injected comparer is dispatched by position rather than runtime type — a typed three-axis indexer, and per-axis projections (`Keys1` / `Keys2` / `Keys3`). The first axis is a prefix and its slice (`GetByFirstKey` / `CountOfFirstKey` / `RemoveByFirstKey`) is a trie walk; the second and third axes are deliberately not backed by an index here, their slices scan at O(n), and the XML remarks say so — when a later axis must be queried hard, `MultiKeyDictionary<TKey, TValue>` with a key order that puts that axis first is the tool. No logic was sunk out of `TwoKeyDictionary` and nothing in it changed.
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
- `Paginable.CreatePageAsync` (P6-04) - the fragment entry points in an awaitable shape, so a caller
  that pages asynchronously does not have to special-case the path where the data is already in
  hand. It **completes synchronously**: a fragment is already in memory, so `Task.FromResult` wraps
  the same synchronous result and the task is finished before it is returned. Nothing here pretends
  to be I/O - use a provider-specific async extension when real I/O has to be awaited. Two
  consequences of `Task.FromResult` are documented on every overload because they invert what an
  `…Async` name usually promises: validation throws **synchronously** from the call itself rather
  than through a faulted task, and the `CancellationToken` parameter is accepted for signature
  symmetry but never observed. Both match `ToPaginableAsync` / `GetPageAsync` as they have been
  since 6.0. All four overloads mirror `CreatePage`, `PageCreationOptions` included.

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
