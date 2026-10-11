# DotNetCore.Collections — Samples

> **English** | [中文](README.zh-CN.md)

This directory collects an independent, runnable sample for every **feature** in the
`DotNetCore.Collections` family.
**One feature, one console project**: each `Sample.<Focus>/` ships its own `Program.cs`
(richly commented), a `README.md` (explaining what the sample focuses on), and a `.csproj`.

- Coverage: `DotNetCore.Collections` (the LINQ value-type engine added in 7.1.0),
  `DotNetCore.Collections.Multi`, `DotNetCore.Collections.Paginable`,
  plus the per-package ORM adapter surface — **every feature up to and including 7.1.0**
  (F01–F58 plus the 6.7–7.1 additions).
- Target framework: mostly `net8.0`; `Sample.ValueEnumerable` / `Sample.MultiTfmSurface`
  are multi-targeted as `net8.0;net10.0`.
- Every sample `ProjectReference`s the matching project under the repository `src/`,
  so it **always builds against the current working tree**.

## How to run

```bash
# Pick any one (working directory = repository root):
dotnet run --project sample/Sample.MultiSet
dotnet run --project sample/Sample.KeysetPagination
dotnet run --project sample/Sample.ValueEnumerable -f net8.0   # multi-target needs an explicit TFM
```

> These samples are **intentionally kept out of `DotNetCore.Collections.sln`** so they do not
> disturb the repository's release-gate baseline (the solution-level "0 errors / warning set"
> reading). Each one is standalone and can be run directly with `dotnet run`, or added to the
> solution on demand.
> The five pre-existing samples (`Sample` / `Sample.Ef` / `Sample.EfCore` / `Sample.Engine` /
> `Sample.Multi`) are **left untouched** — see the end of this document.

---

## 1. `DotNetCore.Collections.Multi` — multisets / multimaps / multi-keys / bags

> One package, three orthogonal "multi" types: `MultiList<T>` multiplies **elements**,
> `MultiDictionary<TKey,TValue>` multiplies the **values under a key**, and
> `MultiKeyDictionary` multiplies the **key components**. Choose by asking "what is allowed to
> repeat", not by name similarity.

### Basic multisets and multimaps

| Sample | Feature | Focus / demonstrated capability |
| :--- | :--- | :--- |
| [MultiSet](Sample.MultiSet) | F01 | `MultiList<T>` unordered bag: copy-count semantics, `Count` = number of copies (not distinct elements), union/intersect/difference computed on copy counts, copy-expanded enumeration |
| [MultiMap](Sample.MultiMap) | F02 | `MultiDictionary<TKey,TValue>` multimap: `Count` = key count, `Values` is a **flattened** sequence (not grouped by key), a missing key returns an empty collection |
| [StructuralEquality](Sample.StructuralEquality) | F03 | Multiset **structural equality**: copy counts participate in `Equals`/`GetHashCode`, independent of enumeration order, safe to put in a `HashSet` |
| [MultiMapBatchDelete](Sample.MultiMapBatchDelete) | F04 | Contrast between `RemoveRange` (removes **one** occurrence per distinct value) and `ExceptWith` (removes **all** occurrences) |
| [ExplicitSerialization](Sample.ExplicitSerialization) | F10 (+6.7.1) | Explicit serialization model: `ToSerializableModel`/`FromModel` round-trip, the model is a snapshot, comparers are not serialized; extended to the whole family in 6.7.1 |
| [MultiMapDualCache](Sample.MultiMapDualCache) | F11 | Dual caching: the O(1) cost model behind `TotalValueCount` / `ContainsValue`, transparent to callers |
| [SetOpsZeroAlloc](Sample.SetOpsZeroAlloc) | F12 | **Zero-allocation** set operations: reads in place and reuses scratch space when the argument is a same-comparer `MultiList<T>`; array arguments still materialize |
| [MultiDictionaryExtensions](Sample.MultiDictionaryExtensions) | 6.8.0 | Build a multimap from an existing source: `IndexBy` / `UniqueIndexBy` / `AsMultiDictionary` |

### Ordered sets and ordered maps

| Sample | Feature | Focus / demonstrated capability |
| :--- | :--- | :--- |
| [OrderedMultiSet](Sample.OrderedMultiSet) | F05 | `OrderedMultiList<T>` ordered bag: ascending enumeration, the comparer defines both **order and identity**, O(log n) add/remove |
| [OrderedAccessRange](Sample.OrderedAccessRange) | F06 | Extremes (`GetFirst`/`GetLast`) and range (`GetRange`) queries, with boundary semantics following the injected `IComparer<T>` |
| [OrderStatisticTree](Sample.OrderStatisticTree) | F07 | The order-statistic B+ tree engine (internal): observe O(log n) rank reads and the invariants through `OrderedMultiList<T>` |
| [OrderedMultiMap](Sample.OrderedMultiMap) | F08 | `OrderedMultiDictionary<TKey,TValue>`: keys ascending × values under a key ascending, with rank reads against the flattened sequence |
| [SecondaryReverseIndex](Sample.SecondaryReverseIndex) | F09 | `TwoKeyDictionary` second-axis reverse index: `GetBySecondKey`/`CountOfSecondKey` without scanning the whole table |

### Multi-keys / bijection / reverse indexes

| Sample | Feature | Focus / demonstrated capability |
| :--- | :--- | :--- |
| [TwoKeyDictionary](Sample.TwoKeyDictionary) | F29 (+6.7.1/6.8) | Two-key composite dictionary: two-dimensional indexer plus dual-axis slicing; 6.7.1 `Values`/`ContainsValue`/`AsReverse`, 6.8 `EntrySet()` |
| [ThreeKeyDictionary](Sample.ThreeKeyDictionary) | F15 (+6.7.1/6.8) | Three-key composite dictionary: triple indexer; **only the first axis is a prefix**, the second/third axes fall back to scanning |
| [BiDictionary](Sample.BiDictionary) | F16 | Strict **bijective** dictionary: a right-side conflict never silently overwrites, `Add` throws / `TryAdd` returns false, O(1) both ways |
| [ReverseMultiDictionary](Sample.ReverseMultiDictionary) | F17 (+6.7.1) | `ReverseMultiDictionary<V,K>` inverted **snapshot** (copied out at construction); 6.7.1 `AsReverse` family |
| [LiveReverseView](Sample.LiveReverseView) | F18 | `MultiDictionary.AsReverse()` as a **live** reverse lookup view: zero-copy, the view is visible immediately after the source `Add` |
| [MultiKeyMultiDictionary](Sample.MultiKeyMultiDictionary) | F20 (+6.8) | Composite-key multimap: homogeneous component key + one-key-many-values + prefix counting; 6.8 `EntrySet()` |

### Variants and views

| Sample | Feature | Focus / demonstrated capability |
| :--- | :--- | :--- |
| [ImmutableVariants](Sample.ImmutableVariants) | F13 | `ImmutableMultiList<T>` / `ImmutableMultiDictionary`: mutations return new instances, the Builder does batch writes |
| [ConcurrentVariants](Sample.ConcurrentVariants) | F14 (+6.7.1) | Thread-safe variants: `ConcurrentMultiList` with single-lock linearization, `ConcurrentMultiDictionary` sharded; 6.7.1 read-only projections come from `Snapshot()` |
| [ReadOnlyCollectionView](Sample.ReadOnlyCollectionView) | F19 | Cross-TFM read-only value view: on net451 a bare `HashSet`→`IReadOnlyCollection` cast throws, the wrapper does not, and it is a **live view** |

### The bag family (specialized multisets)

| Sample | Feature | Focus / demonstrated capability |
| :--- | :--- | :--- |
| [PackedBag](Sample.PackedBag) | F21 | `PackedBag<T>` dense-array histogram: `where T : struct`, no boxing, faster for a small value domain (≤16) |
| [SpanBag](Sample.SpanBag) | F22 | `SpanBag<T>` zero-heap bag on the stack: borrows `stackalloc` storage, `ref struct` lifetime enforced by the compiler |
| [FrequencyPriorityBag](Sample.FrequencyPriorityBag) | F23 | `FrequencyPriorityBag<T>`: repeated `PeekMost`/`PopMost` yields a descending-frequency sequence, the natural shape for Top-K |
| [BoundedBag](Sample.BoundedBag) | 6.9.0 | Fixed-capacity sliding-window bag: over capacity it **silently evicts the oldest** copy, `Add` returns `void` (nothing to report on overflow) |
| [BagStatistics](Sample.BagStatistics) | 6.9.0 | `Mode()` / `Median()` / `Entropy()` on `IMultiSet<T>`: frequency-distribution summaries, read via `EntrySet()` |

### Collection utilities and serialization enhancements

| Sample | Feature | Focus / demonstrated capability |
| :--- | :--- | :--- |
| [PositionalRead](Sample.PositionalRead) | F24 | Rank and quantile reads: `GetByRank`/`GetRank`/`GetMedian`/`GetQuantile`/`TotalValueCount` (lists + maps) |
| [ListFaces](Sample.ListFaces) | F25 | The `IList<T>`/`IReadOnlyList<T>` faces of `OrderedMultiList<T>`: the indexer setter and `Insert` are narrowed |
| [TypeReadableName](Sample.TypeReadableName) | F26 | `Type.ToReadableString()` / `TypeNameFormat`: drop the namespace, keep the shape, fold past a depth limit |
| [SharedInterfaces](Sample.SharedInterfaces) | F27 | `IMultiSet<T>` / `IMultiDictionary<TKey,TValue>` / `IBiMap`: behavior-oriented shared abstractions and the dual meaning of `Count` |
| [Deque](Sample.Deque) | F28 | `Deque<T>` ring-buffer double-ended queue: O(1) amortized at both ends, O(1) positional read, sliding windows |
| [CollectionDifference](Sample.CollectionDifference) | 6.9.0 | Four-way difference comparison: `OnlyInLeft`/`OnlyInRight`/`InCommon`/`Differing` + `AreEqual` (bags by copy count, dictionaries by value) |
| [CollectionChanged](Sample.CollectionChanged) | 6.9.0 | Home-grown change notifications: the `CollectionChanged` event on `MultiList`/`MultiDictionary` (`Add`/`Remove`/`Reset`) |
| [Snapshot](Sample.Snapshot) | 6.9.0 | `Snapshot()` as an independent copy: a detached snapshot for "enumerate while mutating", a namesake of a different shape from `Clone()` |

---

## 2. `DotNetCore.Collections.Paginable` — pagination

> Division of verbs: `ToPaginable` builds a collection, `GetPage` slices one page from the
> **full source**, and `ToPage`/`CreatePage` assemble an **already-sliced fragment**.

### Core pagination

| Sample | Feature | Focus / demonstrated capability |
| :--- | :--- | :--- |
| [KeysetPagination](Sample.KeysetPagination) | F30 | Keyset (seek) pagination: anchor-driven continuation loop, descending order, and the "fetch one extra row for the caller to trim" contract of `BuildKeysetQuery` |
| [AsyncPaging](Sample.AsyncPaging) | F31 | The `*Async` methods on in-memory sources are a **synchronously completed** "async shape", semantically different from true I/O async |
| [FragmentPage](Sample.FragmentPage) | F32 | Building a page from a fragment: `CreatePage`/`ToPage` never re-apply `Skip/Take` to an **already-sliced** fragment; `PageFragmentInfo` metadata round-trip |
| [StrictFragmentCheck](Sample.StrictFragmentCheck) | F33 | `PageCreationOptions.Lenient` (tolerates a short fragment) vs `Strict` (treats it as a bug and throws `ArgumentException`) |
| [SinglePageSet](Sample.SinglePageSet) | F34 | Single-page set: the collection layer's `PageCount` is always 1, while the page layer keeps full-source numbering (**an intentional fork**) |
| [AsyncFragmentEntry](Sample.AsyncFragmentEntry) | F35 | `Paginable.CreatePageAsync` is the async dual of fragment page-building: **synchronously completed**, the token is not observed |
| [PageMetadata](Sample.PageMetadata) | F36 | Page metadata snapshot, caller-supplied input metadata, and the derivation source shared by both (`PageMetaInfo.Calculate`) |
| [PaginableSettings](Sample.PaginableSettings) | F37 | The two global configuration knobs (`DefaultPageSize` / `MaxMemberItems`): validated on assignment, swapped atomically as a snapshot |
| [LimitedMembers](Sample.LimitedMembers) | F38 | Member caps and the **open interval**: global `MaxMemberItems` plus a call-level `limitedMemberCount` ceiling |
| [EnumerablePageMaterialize](Sample.EnumerablePageMaterialize) | F39 | Single `Skip/Take` materialization for `EnumerablePage<T>`: deep pages are no longer O(skip²) |

### Contracts / platforms / utilities

| Sample | Feature | Focus / demonstrated capability |
| :--- | :--- | :--- |
| [ExceptionTypeUnify](Sample.ExceptionTypeUnify) | F40 | Out-of-range exceptions unified as `ArgumentOutOfRangeException` carrying `ParamName` (a 6.2.0 breaking change, with counterexamples for the old types) |
| [NullableAnnotations](Sample.NullableAnnotations) | F41 | The NRT nullability contract of public APIs: `T?` / `string?` is a **compile-time** promise, not a replacement for runtime null checks |
| [KeysetAnchorNullability](Sample.KeysetAnchorNullability) | F42 | Nullable keyset anchors: a nullable key can serve directly as an anchor, where `null` means "no anchor" = the first page |
| [LowGenNullableShims](Sample.LowGenNullableShims) | F43 | Nullable-attribute shims for low-generation TFMs (internal): net451 and friends can read the annotations too, invisibly to callers |
| [CancellationTokenForwarding](Sample.CancellationTokenForwarding) | F44 | The three semantics of cancellation tokens: **forwarded** (EF/FreeSql) / **library boundary** (SqlSugar 6.6) / **not observed** (in-memory async) |
| [SolidPageExtensionsSurface](Sample.SolidPageExtensionsSurface) | F45 | The extension-method entry surface: `ToPaginable`/`GetPage`/`*Async`; CS0104 disambiguation for the **same name across 10 packages** |
| [PageMemberWrapping](Sample.PageMemberWrapping) | F46 | Page member wrapping: `PageMember<T>` (a struct) with `Value`/`Offset`/`ItemNumber` and cross-page global numbering |
| [PaginableCalc](Sample.PaginableCalc) | F47 | `PaginableCalc`: the single source of truth for member-cap trimming and page-count conversion (in the `Internal` namespace but public) |
| [QueryEntryState](Sample.QueryEntryState) | F48 | The covariant `IQueryEntryState<T>` contract: abstracting "where a page's elements come from" for deferred evaluation |
| [EmptyAndSinglePage](Sample.EmptyAndSinglePage) | F49 | The layered semantics of `EmptyPage<T>` (no results never returns null) and `PaginableSinglePage<T>` |

---

## 3. ORM adapters / platforms

> The 9 adapters are **verb-isomorphic** (`ToPaginable` / `GetPage`), but the **entry type names
> differ per package and cannot be inferred across packages**; moreover, each package's
> `SolidPageExtensions` is a different assembly with the same name. Whenever that type is
> mentioned, use the fully qualified name or the package name.

| Sample | Feature | Focus / demonstrated capability |
| :--- | :--- | :--- |
| [OrmAdapterMatrix](Sample.OrmAdapterMatrix) | F50 | The isomorphic matrix of the 9 adapters and the entry-name cross-reference; notes which packages lack a standalone `Query`/`Page` pair |
| [EfCoreAdapter](Sample.EfCoreAdapter) | F51 | EF Core **true async** (`CountAsync`/`ToListAsync`) plus async keyset; run end-to-end once with the InMemory provider |
| [FreeSqlAdapter](Sample.FreeSqlAdapter) | F52 | The FreeSql entry surface and its unique `includeNestedMembers`; `PaginableFreeSqlQuery<T>` / `FreeSqlPage<T>` |
| [SqlSugarAdapter](Sample.SqlSugarAdapter) | F53 | The SqlSugar entry surface and the 6.6.0 **cancellation-token boundary** (an already-canceled token throws `OperationCanceledException` before hitting the database) |
| [SqlKataAdapter](Sample.SqlKataAdapter) | F54 | SqlKata's source type is a **non-generic** `Query`: you must write `ToPaginable<Product>(...)` / `GetPage<Product>(...)` explicitly |
| [OtherOrmAdapters](Sample.OtherOrmAdapters) | F55 | Entry-name cross-reference for the Chloe / DosOrm / NHibernate packages and their unique overloads (`additionalQueryFunc`, `NhCorePage<T>`) |
| [SourceGenerator](Sample.SourceGenerator) | F56 | The `[SolidPageExtensionsFor]` attribute declaration and the shape of what it generates (an analyzer, not shipped with the consumer package) |
| [MultiTfmSurface](Sample.MultiTfmSurface) | F57 | The 11-TFM matrix and the **per-package subset differences**: EF Core is net6+ only, Chloe/Dos/NHibernate have no net451, SqlSugar has no ns2.0 |

---

## 4. `DotNetCore.Collections` — the LINQ value-type engine added in 7.1.0

| Sample | Feature | Focus / demonstrated capability |
| :--- | :--- | :--- |
| [ValueEnumerable](Sample.ValueEnumerable) | 7.1.0 | The four `ToValueEnumerable()` entries (`T[]`/`List<T>`/`IReadOnlyList<T>`/`IEnumerable<T>`) plus five operators (`Index`/`ForEach`/`TagFirstLast`/`Pairwise`/`Scan`), and namespace governance for coexisting with `System.Linq` |
| [SerializationEntry](Sample.SerializationEntry) | F58 | A **negative result**: there is **no** `ToSerializableModel` on the Paginable side; the correct path is `PageMetadata` + `Paginable.CreatePage` to rebuild |

---

## 5. Quick lookup by theme

| Theme | Related samples |
| :--- | :--- |
| **Copy-count semantics (the highest-risk contract)** | MultiSet, MultiMap, SharedInterfaces, OrderedMultiSet |
| **Ordering / rank / range** | OrderedMultiSet, OrderedAccessRange, OrderStatisticTree, OrderedMultiMap, PositionalRead, ListFaces |
| **Multi-key / bijection / reverse index** | TwoKeyDictionary, ThreeKeyDictionary, BiDictionary, ReverseMultiDictionary, LiveReverseView, MultiKeyMultiDictionary, SecondaryReverseIndex |
| **Variants (immutable / concurrent)** | ImmutableVariants, ConcurrentVariants, ReadOnlyCollectionView |
| **The bag family** | PackedBag, SpanBag, FrequencyPriorityBag, BoundedBag, BagStatistics |
| **Set operations and differences** | SetOpsZeroAlloc, CollectionDifference, StructuralEquality |
| **Serialization / snapshot / notifications** | ExplicitSerialization, Snapshot, CollectionChanged, SerializationEntry |
| **Pagination core** | KeysetPagination, AsyncPaging, FragmentPage, SinglePageSet, EnumerablePageMaterialize, PageMetadata |
| **Pagination contracts / configuration** | PaginableSettings, LimitedMembers, PaginableCalc, ExceptionTypeUnify, NullableAnnotations, KeysetAnchorNullability, QueryEntryState, EmptyAndSinglePage |
| **Adapters / platforms** | OrmAdapterMatrix, EfCoreAdapter, FreeSqlAdapter, SqlSugarAdapter, SqlKataAdapter, OtherOrmAdapters, SourceGenerator, MultiTfmSurface, SolidPageExtensionsSurface |
| **Engine (7.1.0)** | ValueEnumerable |

---

## 6. Existing samples (untouched, kept as-is)

| Sample | Description |
| :--- | :--- |
| [Sample](Sample) | Early Paginable basics sample (`net8.0`, references `PaginableCollections` 3.3.0) |
| [Sample.Ef](Sample.Ef) | EntityFramework (EF6) adapter sample |
| [Sample.EfCore](Sample.EfCore) | EF Core adapter sample |
| [Sample.Multi](Sample.Multi) | Multi package basics sample |
| [Sample.Engine](Sample.Engine) | 7.1.0 engine "dual `using`" namespace-governance verification (work item F7-07), multi-targeted `net8.0;net10.0` |

---

## 7. Conventions

- **One feature, one project**: `Sample.<Focus>/` focuses on a single feature document or a
  single CHANGELOG entry.
- **Comments explain semantics**: e.g. "`Count` is the number of copies, not the number of
  distinct elements" or "`Values` is a flattened sequence" — not a restatement of the code.
- **Deterministic output**: never rely on hash enumeration order; where an unordered collection
  is involved, `OrderBy` first, then print.
- **No invented APIs**: samples use only public APIs that genuinely exist in
  `reports/features/` and `CHANGELOG.md`; where the wording of a document conflicts, `src/`
  source wins (a few feature documents had small hallucinations in their examples, corrected
  against source in the corresponding sample).
- **Wrong usage is shown in comments** (`// ❌ Wrong: …`), never as code that fails to compile.
