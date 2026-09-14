# Collections

[![Member project of .NET Core Community](https://img.shields.io/badge/member%20project%20of-NCC-9e20c9.svg)](https://github.com/dotnetcore)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/dotnetcore/CAP/master/LICENSE.txt)
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fdotnetcore%2FCollections.svg?type=shield)](https://app.fossa.io/projects/git%2Bgithub.com%2Fdotnetcore%2FCollections?ref=badge_shield)

NCC Collections is a set of collection-based extensions and tools, shipped as two independent
modules.

| Module | Package | What it does |
| --- | --- | --- |
| **Paginable** | `DotNetCore.Collections.Paginable` + 9 ORM integrations | Turns any `IEnumerable<T>` / `IQueryable<T>` into paged results, with keyset (cursor) paging and async support. |
| **Multi** | `DotNetCore.Collections.Multi` | Multiset, multimap, composite-key and thread-safe/immutable collection types. |

See [CHANGELOG.md](CHANGELOG.md) for the full per-release history. Highlights of the current
**6.3.0** release: `BiDictionary<TLeft, TRight>` (a strict one-to-one bijective map),
`ReverseMultiDictionary<V, K>` plus the `MultiDictionary.AsReverse()` live view, and nullable
reference annotations enabled across all eleven packages.

## Contents

- [NuGet Packages](#nuget-packages)
- [Paginable](#paginable)
- [Multi](#multi)
- [Building and testing](#building-and-testing)
- [Releasing](#releasing)
- [License](#license)

## NuGet Packages

| Package Name | Version | Downloads |
| --- | --- | --- |
| [DotNetCore.Collections.Paginable](https://www.nuget.org/packages/DotNetCore.Collections.Paginable/) | ![](https://img.shields.io/nuget/v/DotNetCore.Collections.Paginable.svg) | ![](https://img.shields.io/nuget/dt/DotNetCore.Collections.Paginable.svg) |
| [DotNetCore.Collections.Multi](https://www.nuget.org/packages/DotNetCore.Collections.Multi/) | ![](https://img.shields.io/nuget/v/DotNetCore.Collections.Multi.svg) | ![](https://img.shields.io/nuget/dt/DotNetCore.Collections.Multi.svg) |
| [DotNetCore.Collections.Paginable.Chloe](https://www.nuget.org/packages/DotNetCore.Collections.Paginable.Chloe/) | ![](https://img.shields.io/nuget/v/DotNetCore.Collections.Paginable.Chloe.svg) | ![](https://img.shields.io/nuget/dt/DotNetCore.Collections.Paginable.Chloe.svg) |
| [DotNetCore.Collections.Paginable.DosOrm](https://www.nuget.org/packages/DotNetCore.Collections.Paginable.DosORM/) | ![](https://img.shields.io/nuget/v/DotNetCore.Collections.Paginable.DosORM.svg) | ![](https://img.shields.io/nuget/dt/DotNetCore.Collections.Paginable.DosORM.svg) |
| [DotNetCore.Collections.Paginable.EntityFramework](https://www.nuget.org/packages/DotNetCore.Collections.Paginable.EntityFramework/) | ![](https://img.shields.io/nuget/v/DotNetCore.Collections.Paginable.EntityFramework.svg) | ![](https://img.shields.io/nuget/dt/DotNetCore.Collections.Paginable.EntityFramework.svg) |
| [DotNetCore.Collections.Paginable.EntityFrameworkCore](https://www.nuget.org/packages/DotNetCore.Collections.Paginable.EntityFrameworkCore/) | ![](https://img.shields.io/nuget/v/DotNetCore.Collections.Paginable.EntityFrameworkCore.svg) | ![](https://img.shields.io/nuget/dt/DotNetCore.Collections.Paginable.EntityFrameworkCore.svg) |
| [DotNetCore.Collections.Paginable.FreeSql](https://www.nuget.org/packages/DotNetCore.Collections.Paginable.FreeSql/) | ![](https://img.shields.io/nuget/v/DotNetCore.Collections.Paginable.FreeSql.svg) | ![](https://img.shields.io/nuget/dt/DotNetCore.Collections.Paginable.FreeSql.svg) |
| [DotNetCore.Collections.Paginable.FreeSql.DbContext](https://www.nuget.org/packages/DotNetCore.Collections.Paginable.FreeSql.DbContext/) | ![](https://img.shields.io/nuget/v/DotNetCore.Collections.Paginable.FreeSql.DbContext.svg) | ![](https://img.shields.io/nuget/dt/DotNetCore.Collections.Paginable.FreeSql.DbContext.svg) |
| [DotNetCore.Collections.Paginable.NHibernate](https://www.nuget.org/packages/DotNetCore.Collections.Paginable.NHibernate/) | ![](https://img.shields.io/nuget/v/DotNetCore.Collections.Paginable.NHibernate.svg) | ![](https://img.shields.io/nuget/dt/DotNetCore.Collections.Paginable.NHibernate.svg) |
| [DotNetCore.Collections.Paginable.SqlKata](https://www.nuget.org/packages/DotNetCore.Collections.Paginable.SqlKata/) | ![](https://img.shields.io/nuget/v/DotNetCore.Collections.Paginable.SqlKata.svg) | ![](https://img.shields.io/nuget/dt/DotNetCore.Collections.Paginable.SqlKata.svg) |
| [DotNetCore.Collections.Paginable.SqlSugar](https://www.nuget.org/packages/DotNetCore.Collections.Paginable.SqlSugar/) | ![](https://img.shields.io/nuget/v/DotNetCore.Collections.Paginable.SqlSugar.svg) | ![](https://img.shields.io/nuget/dt/DotNetCore.Collections.Paginable.SqlSugar.svg) |

## Paginable

Paging over `IEnumerable<T>` and `IQueryable<T>`. The core library covers in-memory and queryable
sources; the nine ORM integration packages add a provider-specific `GetPage` / `GetPageAsync`
extension, so paging stays a single call on the source you already have.

### Installation

```
Install-Package DotNetCore.Collections.Paginable
```

For a specific ORM, install the matching integration package — see
[ORM integrations](#orm-integrations).

### Quick start

Materialise a page from any sequence:

```c#
IEnumerable<ExampleModel> list = GetList();

// Page 15, 50 items per page.
var paginableList = list.ToPaginable(50);
var page = paginableList.GetPage(15);

for (var i = 0; i < page.CurrentPageSize; i++)
{
    var itemNumber = page[i].ItemNumber;
    var itemValue  = page[i].Value;
}
```

Or page the source directly, with no intermediate `IPaginable<T>`:

```c#
var page = GetList().GetPage(15, 50);
```

### Paging an `IQueryable<T>`

`IQueryable<T>` sources (EF Core `Where`, NHibernate `Query<T>`, …) page the same way. The query is
translated to SQL, so only the requested slice is fetched:

```c#
IQueryable<ExampleModel> queryable = GetQueryable();

var page = queryable.GetPage(15, 50);
var totalMemberCount = page.TotalMemberCount;
```

### Keyset (seek) pagination

Offset pagination degrades on deep pages because the database still scans the skipped rows. Keyset
(a.k.a. seek / cursor) pagination replaces `OFFSET n` with a `WHERE key > @lastKey` predicate, so
every page costs the same and the `COUNT(*)` round trip is avoided — the recommended mode for
infinite-scroll and cursor-style APIs.

```c#
IQueryable<ExampleModel> queryable = GetQueryable();

// First page: no anchor key yet.
var first = queryable.GetFirstPageByKeyset(x => x.Id, pageSize: 50);

// Subsequent pages: pass the ordering key of the last row of the previous page.
var lastId = first.LastMember.Id;
var next   = queryable.GetPageByKeyset(x => x.Id, lastId, pageSize: 50);

foreach (var item in next.Members) { /* ... */ }
var hasMore = next.HasNext;   // resolved without COUNT(*)
```

`GetFirstPageByKeyset` / `GetPageByKeyset` also have `IEnumerable<T>` overloads for in-memory
sources and an optional `descending` switch for reverse ordering. Use keyset pagination when you do
**not** need `TotalPageCount` / `TotalMemberCount`; use the offset APIs above when you do.

### Paging an existing fragment

When you already hold one page of data — a hand-written `OFFSET` / `FETCH` query, a cached page, or
an upstream API that answers with `items` plus `totalCount` — wrap it with `Paginable.CreatePage` /
`fragment.ToPage`. The fragment is taken to be the exact content of the named page and is **never
re-sliced**:

```c#
var items = connection.Query<Order>(sql, new { offset = 10, fetch = 5 }); // 5 rows
var total = connection.ExecuteScalar<int>(countSql);                      // 12

IPage<Order> page = Paginable.CreatePage(items, pageNumber: 3, pageSize: 5, totalMemberCount: total);

page.TotalPageCount;   // 3
page.CurrentPageSize;  // 2   (a short last page)
page.HasNext;          // false
page[0].ItemNumber;    // 11  (global row number, as full-source paging would give)
page.GetMetadata();    // a serializable PageMetadata snapshot
```

`GetPage(source, …)` and `CreatePage(fragment, …)` read alike but do opposite things: `GetPage`
takes the **whole** source and slices it (`Skip` + `Take`); `CreatePage` takes **one already-sliced
page** and only attaches metadata. Use `Paginable.CreateSinglePageSet` when the caller needs an
`IPaginable<T>` wrapping that single fragment.

Validation is eager: a `null` fragment throws `ArgumentNullException`; `pageNumber < 1`,
`pageSize < 1`, a negative `totalMemberCount`, or a page past the last page throw
`ArgumentOutOfRangeException`; a fragment longer than `pageSize` (or than the metadata expects)
throws `ArgumentException`. A fragment *shorter* than the metadata expects is tolerated by default
(an upstream row may have been deleted between the count and the fetch) and `CurrentPageSize` keeps
reporting the metadata value. Opt into strict checking — where a short fragment is more likely a bug
than a race — with `PageCreationOptions.Strict`:

```c#
var page1 = Paginable.CreatePage(items, info);                              // lenient (default)
var page2 = Paginable.CreatePage(items, info, PageCreationOptions.Strict);  // short fragment throws
```

### Asynchronous paging

The core library exposes `ToPaginableAsync` / `GetPageAsync` for in-memory and `IQueryable<T>`
sources. The EF Core, FreeSql and SqlSugar integrations provide true end-to-end async (`CountAsync`
+ `ToListAsync`, no synchronous database calls), with `CancellationToken` support:

```c#
using(var context = new ExampleDbContext())
{
    var page = await context.ExampleModels
        .GetPageAsync(pageNumber: 1, pageSize: 50, cancellationToken: ct);
    var totalMemberCount = page.TotalMemberCount;
}
```

`Paginable.CreatePageAsync` mirrors the fragment API but **completes synchronously** — the fragment
is already in memory, so validation throws from the call itself (catch it with `try`, not `await`)
and the `CancellationToken` is accepted for signature symmetry only.

### Configuration

`PaginableSettingsManager` holds a process-wide, validated settings snapshot. Configure it once at
startup and treat it as read-only afterwards:

```c#
PaginableSettingsManager.Settings = new PaginableSettings
{
    DefaultPageSize = 50,        // must be >= 1
    MaxMemberItems  = 10_000_000 // must be >= 1
};
```

### ORM integrations

Every integration adds a `GetPage` (and, where the provider supports it, `GetPageAsync`) extension
on the source type you already use.

| Provider | Package | Extension on |
| --- | --- | --- |
| Chloe | `…Paginable.Chloe` | `db.Query<ExampleModel>()` |
| Dos.ORM | `…Paginable.DosOrm` | `_session.From<ExampleModel>()` |
| FreeSql | `…Paginable.FreeSql` | `_freeSql.Select<ExampleModel>()` |
| SqlSugar | `…Paginable.SqlSugar` | `_sqlSugar.Query<ExampleModel>()` |
| NHibernate | `…Paginable.NHibernate` | `session.QueryOver<ExampleModel>()` |
| EF6 | `…Paginable.EntityFramework` | `context.ExampleModels.Where(…)` |
| EF Core | `…Paginable.EntityFrameworkCore` | `context.ExampleModels` |
| SqlKata + Dapper | `…Paginable.SqlKata` | `db.Query("ExampleModels")` |

A representative call (each of the above ends the same way):

```c#
var page = source.GetPage(1, 9);           // where `source` is the receiver from the table
var totalPageCount = page.TotalPageCount;
```

FreeSql and EF Core additionally expose the `DbSet` / `DbContext` directly:

```c#
// FreeSql — page a DbSet:
var ctx = _freeSql.CreateDbContext();
var page = ctx.Set<ExampleModel>().GetPage(1, 9);

// EF Core — page a DbSet:
using(var context = new ExampleDbContext())
{
    var page = context.ExampleModels.GetPage(1, 9);
}
```

### Examples

- [Paginable with EF Core](https://github.com/dotnetcore/Collections/blob/dev/sample/Sample.EfCore/Program.cs)
- [Paginable with EF6](https://github.com/dotnetcore/Collections/blob/dev/sample/Sample.Ef/Program.cs)

## Multi

`DotNetCore.Collections.Multi` ships in its own package and is independent of the paging
extensions. Every type carries the `Multi` prefix, but the families multiply **different things**
and are orthogonal to each other.

### Installation

```
Install-Package DotNetCore.Collections.Multi
```

### Choosing a type

Read each name as "what is multiplied" — `MultiList` multiplies elements, `MultiDictionary`
multiplies values, `MultiKeyDictionary` multiplies keys. Pick by asking *what is allowed to
repeat*, never by name similarity.

| Type | Multiplies | Shape |
| --- | --- | --- |
| `MultiList<T>` | elements | 1 element &#8594; N copies |
| `OrderedMultiList<T>` | elements, ordered | 1 element &#8594; N copies, sorted |
| `PackedBag<T>` | elements, packed histogram | 1 value-type element &#8594; N copies, dense struct array |
| `MultiDictionary<TKey, TValue>` | values | 1 key &#8594; N values |
| `OrderedMultiDictionary<TKey, TValue>` | values, ordered | 1 key &#8594; N values, sorted |
| `MultiKeyDictionary<TKey, TValue>` | key components | N components &#8594; 1 value |
| `TwoKeyDictionary<K1, K2, V>` | key components | 2 components &#8594; 1 value |
| `ThreeKeyDictionary<K1, K2, K3, V>` | key components | 3 components &#8594; 1 value |
| `MultiKeyMultiDictionary<TKey, TValue>` | key components and values | N components &#8594; N values |
| `BiDictionary<TLeft, TRight>` | nothing (bijective) | 1 left &#8596; 1 right |
| `ReverseMultiDictionary<V, K>` | values, inverted | 1 value &#8594; N keys |
| `ImmutableMultiList<T>` / `ImmutableMultiDictionary<TKey, TValue>` | frozen | write-once |
| `ConcurrentMultiDictionary<TKey, TValue>` | values, thread-safe | 1 key &#8594; N values, sharded |
| `ConcurrentMultiList<T>` | elements, thread-safe | 1 element &#8594; N copies, single lock |

The quick decision list:

- elements repeat &#8594; `MultiList<T>`;
- elements repeat, in sorted order &#8594; `OrderedMultiList<T>`;
- elements repeat over a small dense value-type domain (enums, small ints) and a histogram is the
  whole workload &#8594; `PackedBag<T>`;
- values repeat under one key &#8594; `MultiDictionary<TKey, TValue>`;
- values repeat under one key, both axes sorted &#8594; `OrderedMultiDictionary<TKey, TValue>`;
- key components combine, one value per complete key &#8594; `MultiKeyDictionary<TKey, TValue>` (or
  `TwoKeyDictionary<K1, K2, V>` / `ThreeKeyDictionary<K1, K2, K3, V>` for two or three differently
  typed components);
- key components combine, many values per complete key, queried by prefix &#8594;
  `MultiKeyMultiDictionary<TKey, TValue>`;
- each left maps to exactly one right and each right back to exactly one left, both directions O(1)
  &#8594; `BiDictionary<TLeft, TRight>`;
- many keys share one value and the question is "which keys hold *this* value?" &#8594; invert the map:
  `MultiDictionary<TKey, TValue>.AsReverse()` for a live read-only view, or `ReverseMultiDictionary<V, K>`
  for a self-contained snapshot.

The following sections describe each family.

### Multisets

**`MultiList<T>`** — a multiset (bag): duplicates matter and are counted. It implements the usual
set operations (`UnionWith` / `IntersectionWith` / `ExceptWith` / `SymmetricExceptWith`), subset and
superset judgments, `Overlaps` / `IsDisjointFrom`, multiset structural equality (`Equals` /
`GetHashCode`, via `IEquatable<MultiList<T>>`), copy-expanded enumeration, and an injectable
`IEqualityComparer<T>`.

**`OrderedMultiList<T>`** — the same bag semantics, plus an order. It is backed by a red-black tree
instead of a hash table, so add / lookup / remove cost O(log n) **worst case** while enumeration is
ascending. It adds `GetFirst()` / `GetLast()`, `Reverse()`, and `GetRange(from, to)`. It takes an
`IComparer<T>` rather than an `IEqualityComparer<T>`, because ordering needs a comparison — and that
comparison is also what decides which elements are the same element.

**`PackedBag<T>`** — the packed counting histogram for value-type elements (`int`, enums, small
structs; the `struct` constraint excludes `null` and `Nullable<T>` by design). One contiguous array
of `(value, count)` struct entries instead of a hash table — no boxing in storage, one cache line
per element. Measured against `MultiList<int>` (BenchmarkDotNet, net8.0): building a 32-value
histogram allocates ~2.7x less and runs ~1.4x faster, enumerating it is ~1.5x faster with ~1.5x
less garbage, and at 4 distinct values point operations are 1.7-2.1x faster — the O(1) dictionary
lookup catches up around 16-32 distinct values, so the recommendation is roughly **up to ~16
distinct values** and `MultiList<T>` beyond. Bag semantics mirror `MultiList` where they meet
(counting, copy-expanded enumeration, removal returns the remaining copies); set operations,
equality and comparer injection are deliberately not carried over — this type is a histogram, not a
general bag.

### Multimaps

**`MultiDictionary<TKey, TValue>`** — one key genuinely owns several values. It implements
`IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>>` and offers `AsLookup()` (an `ILookup`
view), the per-key value-set operations `UnionWith` / `IntersectionWith` / `ExceptWith` /
`SymmetricExceptWith`, the batch pair `AddRange` / `RemoveRange`, per-key counting via
`ValueCount(key)` (alongside `TotalValueCount`), and a configurable inner-collection factory.
`ContainsValue(value)` and `TotalValueCount` are answered in O(1) from two caches kept in step on
the write path — neither ever scans the inner collections.

**`OrderedMultiDictionary<TKey, TValue>`** — the ordered counterpart. The same per-key value-set
operations, the same "no value-less key" invariant, and the same `IReadOnlyDictionary` /
`AsLookup()` / `RemoveRange` shape — but keys enumerate ascending under an `IComparer<TKey>` and
each key's values enumerate ascending under an `IComparer<TValue>`, with single-pair add / lookup /
removal costing O(log n) worst case on both axes.

### Composite keys

**`MultiKeyDictionary<TKey, TValue>`** — the key is **composite** and you query it by a *partial*
prefix: a trie over `(region, country, city)`-style keys of any arity.

**`TwoKeyDictionary<K1, K2, V>`** — the same idea with exactly two components **of different
types**, with a typed indexer instead of a `TKey[]`. Its second axis is queried through a maintained
reverse index (`K2` &#8594; set of `K1`), so `GetBySecondKey` / `CountOfSecondKey` /
`ContainsSecondKey` / `RemoveBySecondKey` visit only the requested slice.

**`ThreeKeyDictionary<K1, K2, K3, V>`** — the same idea with exactly three differently typed
components. The first axis is a prefix, so its slice (`GetByFirstKey` / `CountOfFirstKey` /
`RemoveByFirstKey`) is a trie walk; the second and third axes are deliberately **not** backed by an
index and scan O(n). Use `MultiKeyDictionary<TKey, TValue>` when an axis other than the first must be
queried hard, with the key order putting that axis first.

**`MultiKeyMultiDictionary<TKey, TValue>`** — the composite key of a trie *and* several values per
complete key: the combination of `MultiKeyDictionary<TKey, TValue>` (N components &#8594; 1 value)
and `MultiDictionary<TKey, TValue>` (1 key &#8594; N values). The trie's prefix projection carries
over (`GetByPrefix` / `CountOfPrefix` / `GetSuffixes` / `GetBranches` / `RemovePrefix`), and the
value side mirrors `MultiDictionary`: a configurable inner collection factory
(`allowDuplicateValues`), `AddRange` / `RemoveRange`, the per-key value set operations, the O(1)
cached `TotalValueCount`, and the "no value-less key" invariant — a key disappears from the trie
together with its last value. The composite key here is a trie key of homogeneous components of any
arity, addressable by prefix; `TwoKeyDictionary<K1, K2, V>` / `ThreeKeyDictionary<K1, K2, K3, V>`
remain the strongly typed facades for two or three *differently typed* components that are always
addressed in full.

### One-to-one and inverted

**`BiDictionary<TLeft, TRight>`** — a strict one-to-one map: every left value maps to exactly one
right value and no right value is shared by two lefts, with both directions answered in O(1) from
two indexes kept in step on every write path. Conflicts are **strict** — `Add` throws
`ArgumentException` when the right value is already bound, `TryAdd` reports instead, and there is
deliberately no silently-overwriting setter (break the old binding with an explicit `Remove` /
`RemoveRight` first). `null` is accepted on both sides through dedicated buckets; `AsReverse()`
returns a live read-only view of the right-to-left direction.

**`ReverseMultiDictionary<V, K>`** — the inverse of `MultiDictionary<TKey, TValue>`: a **snapshot**
mapping every stored value to the set of keys that hold it, so "which keys store this value?" is
answered in O(1). The constructor copies the bindings out of the source map into a self-contained
instance (safe to serialize), and the instance stays mutable on its own with the "no value-less key"
invariant mirrored. Member names mirror `MultiDictionary` with the axes swapped: `Count` /
`ValueCount` count distinct values, `TotalKeyCount` counts bindings, `KeyCount(value)` counts one
value's keys, `ContainsValue(value)` is the O(1) presence check and `ContainsKey(key)` scans. For a
read-only view that keeps answering from the live map, use `MultiDictionary<TKey, TValue>.AsReverse()`.

### Immutable and thread-safe

**`ImmutableMultiList<T>`** / **`ImmutableMultiDictionary<TKey, TValue>`** — the immutable
counterparts of the two core types. An instance never changes, so any number of threads may read it
without locks. Mutations return a new instance (or the receiver itself when nothing would change);
bulk mutation goes through `ToBuilder()`, whose builder shares the source's state until its first
write (copy-on-write). Both round-trip through serializable models.

**`ConcurrentMultiDictionary<TKey, TValue>`** — the thread-safe counterpart of
`MultiDictionary<TKey, TValue>`. Keys are routed to shards, each an independent `MultiDictionary`
behind its own lock, so writes on different keys proceed in parallel. Whole-map reads (`Count`,
`TotalValueCount`, `ContainsValue`, enumeration, `Snapshot()`) take a consistent snapshot by locking
every shard once, in index order.

**`ConcurrentMultiList<T>`** — the thread-safe counterpart of `MultiList<T>`. Every operation is
serialized behind one lock — linearizable and trivially safe. It is deliberately **not** sharded: a
bag has one global state its operations compare against. For read-mostly workloads, prefer
`ImmutableMultiList<T>` plus a builder.

### Conventions

- **Set vs. multiset arguments.** The per-key operations of `MultiDictionary<TKey, TValue>` treat
  their argument as a **set** (a repeated value does not count twice, matching `ISet<T>`), whereas
  `MultiList<T>` treats its argument as a **multiset** (multiplicities count). That convention also
  fixes the batch delete: `RemoveRange(key, values)` removes **one occurrence per distinct argument
  value**, so a value stored N times keeps N-1 copies — use `ExceptWith(key, values)` when *every*
  occurrence must go.
- **Equality goes through a comparer, never hash codes alone**, so hash collisions between distinct
  elements/keys can not corrupt a collection. The hash-shaped types match with
  `IEqualityComparer<T>`; `OrderedMultiList<T>` matches with its `IComparer<T>`, where "compares
  equal" *is* "is the same element".
- **`null` handling follows each type's shape.** `MultiList<T>` and `OrderedMultiList<T>` support
  `null` elements (`null` sorts first under the default comparer); `PackedBag<T>` is value-type-only
  (`struct` constraint), so `null` is not applicable to it; `MultiDictionary<TKey, TValue>`
  rejects `null` keys but allows `null` values; the trie types (`MultiKeyDictionary<TKey, TValue>`,
  `TwoKeyDictionary<K1, K2, V>`, `ThreeKeyDictionary<K1, K2, K3, V>` and
  `MultiKeyMultiDictionary<TKey, TValue>`) support `null` key components, and
  `MultiKeyMultiDictionary<TKey, TValue>` also allows `null` values;
  `BiDictionary<TLeft, TRight>` accepts `null` on both sides; `ReverseMultiDictionary<V, K>` mirrors
  its source map inverted.
- **None of these types is thread-safe** — use the `Concurrent*` or `Immutable*` counterparts for
  that guarantee.

### Usage

```c#
// MultiList<T>: a bag counting occurrences
var bag = new MultiList<string> { "apple", "apple", "banana" };
bag.CountOf("apple");      // 2
bag.TotalCount;            // 3
bag.UnionWith(new[] { "apple", "cherry" });
bag.IsSupersetOf(new[] { "banana" }); // true
bag.Equals(new MultiList<string> { "banana", "apple", "apple" }); // true (bag equality, any order)
bag.ToSerializableModel(); // plain snapshot: Items + Counts (see "Save and restore")

// OrderedMultiList<T>: the same bag, kept sorted (red-black tree, O(log n) worst case)
var shelf = new OrderedMultiList<string> { "mug", "bean", "bean" };
foreach (var item in shelf) { /* "bean", "bean", "mug" */ }
shelf.GetFirst();                      // "bean"
shelf.GetLast();                       // "mug"
shelf.GetRange("a", "n");              // "bean", "bean" (both bounds included)
shelf.Reverse();                       // "mug", "bean", "bean"
shelf.EntrySet();                      // sorted (element, copies) pairs

// PackedBag<T>: the packed value-type histogram (no hash table, no boxing in storage)
var levels = new PackedBag<LogLevel>();
levels.Add(LogLevel.Info, 42);
levels.Add(LogLevel.Warn, 7);
levels.Add(LogLevel.Error, 1);
levels.CountOf(LogLevel.Warn);         // 7
levels.TotalCount;                     // 50
foreach (var (level, count) in levels.EntrySet()) { /* the whole histogram in one pass */ }
levels.Remove(LogLevel.Info, 10);      // returns copies remaining; an emptied entry is dropped

// MultiDictionary<K, V>: one key, many values
var map = new MultiDictionary<string, int>();
map.Add("orders", 1001);
map.Add("orders", 1002);
foreach (var order in map["orders"]) { /* 1001, 1002 */ }
var lookup = map.AsLookup();          // LINQ-friendly ILookup view
map.ValueCount("orders");             // 2 (0 for a missing key, never throws)
map.AddRange("orders", new[] { 1003, 1004 });
map.RemoveRange("orders", new[] { 1002, 1003 }); // batch delete, set semantics
map.ContainsValue(1001);              // true — O(1) via a backwards value index, not a scan
map.TotalValueCount;                  // 2 — all values across all keys, cached
map.ToSerializableModel();            // plain snapshot: Keys + Values (see "Save and restore")

// OrderedMultiDictionary<K, V>: the ordered multimap (keys and values both sorted)
var index = new OrderedMultiDictionary<string, int>();
index.Add("orders", 1002);
index.Add("orders", 1001);
foreach (var order in index["orders"]) { /* 1001, 1002 — values ascending */ }
foreach (var key in index.Keys) { /* keys ascending */ }
index.ExceptWith("orders", new[] { 1001 }); // drops every occurrence; an emptied key is removed automatically

// MultiKeyDictionary<K, V>: many key components, one value (a trie)
var tree = new MultiKeyDictionary<string, int>();
tree.Add(new[] { "eu", "de", "berlin" }, 1);
tree.Add(new[] { "eu", "de", "munich" }, 2);
tree.Add(new[] { "eu", "fr", "paris" }, 3);

tree[new[] { "eu", "de", "berlin" }];             // 1        (exact key lookup)
tree.CountOfPrefix(new[] { "eu", "de" });         // 2        (prefix projection)
foreach (var e in tree.GetByPrefix(new[] { "eu" }, relative: true))
{
    // e.Key is the *suffix*: ["de","berlin"], ["de","munich"], ["fr","paris"]
}
tree.RemovePrefix(new[] { "eu", "de" });          // drops the whole subtree at once

// TwoKeyDictionary<K1, K2, V>: the same idea for two differently typed components
var rates = new TwoKeyDictionary<int, string, decimal>();
rates[1, "USD"] = 1.00m;
rates[1, "EUR"] = 0.92m;
rates.CountOfFirstKey(1);             // 2  (a prefix walk over the trie)
rates.GetBySecondKey("USD");          // (1, 1.00m) — served from the second-axis reverse index

// MultiKeyMultiDictionary<K, V>: a composite key with many values per complete key
var assignments = new MultiKeyMultiDictionary<string, int>();
assignments.Add(new[] { "eu", "de", "berlin" }, 1001);
assignments.Add(new[] { "eu", "de", "berlin" }, 1002);  // another value under the same key
assignments.Add(new[] { "eu", "de", "munich" }, 1003);
assignments[new[] { "eu", "de", "berlin" }];    // [1001, 1002]
assignments.CountOfPrefix(new[] { "eu", "de" }); // 2 complete keys under the prefix
assignments.RemovePrefix(new[] { "eu", "de" });  // cascade delete; returns 3 values removed

// ImmutableMultiList<T> / ImmutableMultiDictionary<K, V>: freeze, never mutate
var frozen = new ImmutableMultiList<string>(new[] { "a", "b" });
var grown = frozen.Add("c");          // returns a new instance; `frozen` is untouched
var builder = frozen.ToBuilder();     // shares state until the first write (copy-on-write)
builder.Add("d");
var frozen2 = builder.ToImmutable();  // a fresh instance; `frozen` is still exactly 2 elements
ReferenceEquals(frozen.ToBuilder().ToImmutable(), frozen); // true — untouched builder, same instance

// ConcurrentMultiDictionary<K, V>: same per-key semantics, sharded locks
var concurrent = new ConcurrentMultiDictionary<int, string>();
concurrent.Add(1, "a");               // locks only the shard that owns key 1
concurrent.TryGetValue(1, out var values);
var snapshot = concurrent.Snapshot(); // consistent whole-map view; enumeration is snapshot-based too

// ThreeKeyDictionary<K1, K2, K3, V>: the same idea for three differently typed components
var seats = new ThreeKeyDictionary<string, string, int, bool>();
seats["2026-09-11", "7A", 14] = true;   // date, aircraft, row → occupied
seats.GetByFirstKey("2026-09-11");      // the whole day's slice — a prefix walk

// BiDictionary<L, R>: a strict one-to-one map, both directions O(1)
var users = new BiDictionary<int, string>();
users.Add(1, "alice");
users.Add(2, "bob");
users[1];                    // "alice"
users.GetLeft("bob");        // 2
users.AsReverse()["bob"];    // 1 — live right-to-left view
users.TryAdd(3, "bob");      // false — "bob" is already bound to 2, reported instead of thrown
// users.Add(3, "bob");      // throws ArgumentException — break the old binding explicitly:
users.Remove(2);             // frees "bob"
users.TryAdd(3, "bob");      // true

// ReverseMultiDictionary<V, K>: the inverted snapshot — which keys store this value?
var map = new MultiDictionary<string, int>();
map.Add("orders", 1001);
map.Add("customers", 1001);
map.Add("customers", 1002);

var inverted = new ReverseMultiDictionary<int, string>(map);
inverted[1001];                  // ["orders", "customers"] — O(1)
inverted.KeyCount(1001);         // 2
inverted.ContainsKey("orders");  // true — some value stores the key "orders"
inverted.TotalKeyCount;          // 3 distinct (value, key) bindings

map.Add("invoices", 1001);       // the snapshot does not follow the map ...
inverted[1001];                  // still ["orders", "customers"]
map.AsReverse()[1001];           // ... but the live view does: ["orders", "customers", "invoices"]
```

### Save and restore

`MultiList<T>` and `MultiDictionary<TKey, TValue>` have an **explicit** serialization entry point:
`ToSerializableModel()` hands back a plain snapshot and `FromModel()` rebuilds the collection from
one. The models — `MultiListModel<T>` (`Items` + `Counts`, parallel lists) and
`MultiDictionaryModel<TKey, TValue>` (`Keys` + `Values`, parallel lists) — are ordinary mutable
classes with public settable properties and no serializer dependencies, so JSON
(`System.Text.Json` included), XML or a database row is the caller's choice:

```c#
var model = bag.ToSerializableModel();
string json = JsonSerializer.Serialize(model);

var typed = JsonSerializer.Deserialize<MultiListModel<string>>(json);
var restored = MultiList<string>.FromModel(typed, comparer);   // pass the comparer back
```

The model carries **data only** — a comparer, and a multimap's inner-collection strategy, are
configuration rather than data, so they are supplied to `FromModel()`. Unlike `ToDictionary()`
(which throws on a `null` element) it represents `null` like any other element, and unlike the live
views it is a snapshot that does not move under a serializer's feet.

### Examples

- [Sample.Multi](https://github.com/dotnetcore/Collections/blob/dev/sample/Sample.Multi/Program.cs)

## Building and testing

```bash
dotnet build DotNetCore.Collections.sln -c Release

dotnet test tests/DotNetCore.Collections.Paginable.Tests -c Release
dotnet test tests/DotNetCore.Collections.Multi.Tests      -c Release
```

The unit tests run offline. The integration tests in `tests/DotNetCore.Collections.Paginable.DbTests`
need a SQL Server instance and read their connection string from the
`PAGINABLE_DBTESTS_CONNECTION_STRING` environment variable; on CI they run against a SQL Server 2022
service container.

Two GitHub Actions workflows gate the `dev` and `master` branches:

- `paginable-tests.yml` — builds all 11 TFMs, verifies packing (including `.snupkg`), then runs the unit tests and the SQL Server integration tests.
- `multi-tests.yml` — builds, packs and tests `DotNetCore.Collections.Multi`.

### Publishing

Publishing is automated by the GitHub Actions `Release` workflow
(`.github/workflows/release.yml`); no local tooling is involved. It runs on a version-tag push —
`6.3.0` or `v6.3.0` — and can also be started manually through `workflow_dispatch`:

1. **Pack** — all 11 projects are packed in Release configuration into `nuget_pub` on a
   `windows-latest` runner, with the full git history fetched so SourceLink can attach sources to
   the deterministic build.
2. **Authenticate** — the workflow uses NuGet **trusted publishing (OIDC)** instead of a stored API
   key. The `id-token: write` permission lets GitHub mint a short-lived OIDC token, which the
   `NuGet/login@v1` step exchanges for a temporary API key (one key per token, valid for roughly an
   hour, which is why the login step runs immediately before the push).
3. **Push** — every `.nupkg` and `.snupkg` is pushed to nuget.org with `--skip-duplicate`.

Because publishing relies on trusted publishing, the workflow needs an exactly matching policy on
nuget.org, configured outside the repository:

| Policy field | Value |
| --- | --- |
| Repository owner | `dotnetcore` |
| Repository | `Collections` |
| Workflow file | `release.yml` (file name only, no `.github/workflows/` prefix) |
| Environment | *(empty)* |
| Secret | `NUGET_USERNAME` — the nuget.org profile name, not the e-mail |

The policy owner must own all 11 `DotNetCore.Collections.*` packages. See
[Releasing](#releasing) for how the version being published is chosen.

## Releasing

Versions are driven by `build/version.props`, which is the single source of truth for every
package — bump the version there and all 11 packages follow. Tag the commit with the matching
version (`6.3.0` or `v6.3.0`) to trigger the automated publish; see [Publishing](#publishing) for
what the release workflow does and the one-time nuget.org policy it requires.

## License

Member project of [The NCC](https://github.com/dotnetcore), MIT

[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fdotnetcore%2FCollections.svg?type=large)](https://app.fossa.io/projects/git%2Bgithub.com%2Fdotnetcore%2FCollections?ref=badge_large)
