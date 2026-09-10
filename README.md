# Collections

[![Member project of .NET Core Community](https://img.shields.io/badge/member%20project%20of-NCC-9e20c9.svg)](https://github.com/dotnetcore)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/dotnetcore/CAP/master/LICENSE.txt)
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fdotnetcore%2FCollections.svg?type=shield)](https://app.fossa.io/projects/git%2Bgithub.com%2Fdotnetcore%2FCollections?ref=badge_shield)

NCC Collections consists of a set of collection-based extensions and tools, such as paging extensions and multiset/multimap collections.

## What's new in 6.0

6.0 is a modernization release covering both shipped modules (`Paginable` and `Multi`).

| Area | 5.x | 6.0 |
| --- | --- | --- |
| Target frameworks (core) | `net451`; `net461`; `netstandard2.1`; `net5.0` | `net451`; `net461`; `net47`; `net48`; `netstandard2.0`; `netstandard2.1`; `net6.0` – `net10.0` (11 TFMs) |
| Pagination algorithms | offset only | offset **and** keyset / seek (`GetPageByKeyset`) |
| Async | 3 ORMs, synchronous SQL under the hood | true end-to-end async (`CountAsync` + `ToListAsync`) for EF Core / FreeSql / SqlSugar, with `CancellationToken` passthrough |
| Enumeration performance | `ElementAt` → O(skip²) on non-indexed sources | single `Skip`/`Take` materialization; lazy one-shot materialization in NHibernate |
| Correctness | `CurrentPageSize` wrong on exact-multiple last page; `MaxMemberItems` off-by-one; no argument validation | all fixed; `pageNumber >= 1` / `pageSize >= 1` enforced across core and every ORM integration |
| `Multi` module | `netstandard2.0` only, minimal API surface | `netstandard2.0` / `netstandard2.1` / `net6.0`; rewritten `MultiList<T>` plus a complete `MultiDictionary<TKey, TValue>` |
| Packaging | plain packages | deterministic build, SourceLink, `.snupkg` symbol packages, `packages.lock.json` |
| Quality gates | none | 2 GitHub Actions workflows; 76 Paginable + 233 Multi unit tests, plus SQL Server integration tests |

### Supported target frameworks

| Package | Target frameworks |
| --- | --- |
| `DotNetCore.Collections.Paginable` | `net451`, `net461`, `net47`, `net48`, `netstandard2.0`, `netstandard2.1`, `net6.0`, `net7.0`, `net8.0`, `net9.0`, `net10.0` |
| `DotNetCore.Collections.Paginable.Chloe` | `net461`, `net47`, `net48`, `netstandard2.0`, `net6.0`, `net7.0`, `net8.0`, `net9.0`, `net10.0` |
| `DotNetCore.Collections.Paginable.DosORM` | `netstandard2.1`, `net6.0`, `net7.0`, `net8.0`, `net9.0`, `net10.0` |
| `DotNetCore.Collections.Paginable.EntityFramework` | `net451`, `net461`, `net47`, `net48`, `netstandard2.1`, `net6.0`, `net7.0`, `net8.0`, `net9.0`, `net10.0` |
| `DotNetCore.Collections.Paginable.EntityFrameworkCore` | `net6.0`, `net7.0`, `net8.0`, `net9.0`, `net10.0` |
| `DotNetCore.Collections.Paginable.FreeSql` | `net451`, `net461`, `net47`, `net48`, `netstandard2.0`, `netstandard2.1`, `net6.0`, `net7.0`, `net8.0`, `net9.0`, `net10.0` |
| `DotNetCore.Collections.Paginable.FreeSql.DbContext` | `net451`, `net461`, `net47`, `net48`, `netstandard2.0`, `netstandard2.1`, `net6.0`, `net7.0`, `net8.0`, `net9.0`, `net10.0` |
| `DotNetCore.Collections.Paginable.NHibernate` | `net461`, `net47`, `net48`, `netstandard2.0`, `netstandard2.1`, `net6.0`, `net7.0`, `net8.0`, `net9.0`, `net10.0` |
| `DotNetCore.Collections.Paginable.SqlKata` | `net451`, `net461`, `net47`, `net48`, `netstandard2.0`, `netstandard2.1`, `net6.0`, `net7.0`, `net8.0`, `net9.0`, `net10.0` |
| `DotNetCore.Collections.Paginable.SqlSugar` | `net451`, `net461`, `net47`, `net48`, `netstandard2.1`, `net6.0`, `net7.0`, `net8.0`, `net9.0`, `net10.0` |
| `DotNetCore.Collections.Multi` | `netstandard2.0`, `netstandard2.1`, `net6.0` |

## Nuget Packages

| Package Name                                                                                                                                 | Version                                                                                      | Downloads                                                                                     |
| -------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------- |
| [DotNetCore.Collections.Paginable](https://www.nuget.org/packages/DotNetCore.Collections.Paginable/)                                         | ![](https://img.shields.io/nuget/v/DotNetCore.Collections.Paginable.svg)                     | ![](https://img.shields.io/nuget/dt/DotNetCore.Collections.Paginable.svg)                     |
| [DotNetCore.Collections.Multi](https://www.nuget.org/packages/DotNetCore.Collections.Multi/)                                                 | ![](https://img.shields.io/nuget/v/DotNetCore.Collections.Multi.svg)                         | ![](https://img.shields.io/nuget/dt/DotNetCore.Collections.Multi.svg)                         |
| [DotNetCore.Collections.Paginable.Chloe](https://www.nuget.org/packages/DotNetCore.Collections.Paginable.Chloe/)                             | ![](https://img.shields.io/nuget/v/DotNetCore.Collections.Paginable.Chloe.svg)               | ![](https://img.shields.io/nuget/dt/DotNetCore.Collections.Paginable.Chloe.svg)               |
| [DotNetCore.Collections.Paginable.DosOrm](https://www.nuget.org/packages/DotNetCore.Collections.Paginable.DosOrm/)                           | ![](https://img.shields.io/nuget/v/DotNetCore.Collections.Paginable.DosOrm.svg)              | ![](https://img.shields.io/nuget/dt/DotNetCore.Collections.Paginable.DosOrm.svg)              |
| [DotNetCore.Collections.Paginable.EntityFrameworkCore](https://www.nuget.org/packages/DotNetCore.Collections.Paginable.EntityFrameworkCore/) | ![](https://img.shields.io/nuget/v/DotNetCore.Collections.Paginable.EntityFrameworkCore.svg) | ![](https://img.shields.io/nuget/dt/DotNetCore.Collections.Paginable.EntityFrameworkCore.svg) |
| [DotNetCore.Collections.Paginable.FreeSql](https://www.nuget.org/packages/DotNetCore.Collections.Paginable.FreeSql/)                         | ![](https://img.shields.io/nuget/v/DotNetCore.Collections.Paginable.FreeSql.svg)             | ![](https://img.shields.io/nuget/dt/DotNetCore.Collections.Paginable.FreeSql.svg)             |
| [DotNetCore.Collections.Paginable.NHibernate](https://www.nuget.org/packages/DotNetCore.Collections.Paginable.NHibernate/)                   | ![](https://img.shields.io/nuget/v/DotNetCore.Collections.Paginable.NHibernate.svg)          | ![](https://img.shields.io/nuget/dt/DotNetCore.Collections.Paginable.NHibernate.svg)          |
| [DotNetCore.Collections.Paginable.SqlKata](https://www.nuget.org/packages/DotNetCore.Collections.Paginable.SqlKata/)                         | ![](https://img.shields.io/nuget/v/DotNetCore.Collections.Paginable.SqlKata.svg)             | ![](https://img.shields.io/nuget/dt/DotNetCore.Collections.Paginable.SqlKata.svg)             |
| [DotNetCore.Collections.Paginable.SqlSugar](https://www.nuget.org/packages/DotNetCore.Collections.Paginable.SqlSugar/)                       | ![](https://img.shields.io/nuget/v/DotNetCore.Collections.Paginable.SqlSugar.svg)            | ![](https://img.shields.io/nuget/dt/DotNetCore.Collections.Paginable.SqlSugar.svg)            |

## Usage

### Install the package

```
Install-Package DotNetCore.Collections.Paginable
```

### Write code

```c#
IEnumerable<ExampleModel> list = GetList();//...

//Get a collection of Page, each page has 50 PageMembers
var paginableList = list.ToPaginable(50);

//Get page 15th
var page = paginableList.GetPage(15);

for (var i = 0; i < page.CurrentPageSize; i++)
{
    var itemNumber = page[i].ItemNumber;
    var itemValue = page[i].Value;
}
```

Or use a more streamlined code:

```c#
IEnumerable<ExampleModel> list = GetList();//...

//Get page 15th, each page has 50 items.
var page = list.GetPage(15, 50);

for (var i = 0; i < page.CurrentPageSize; i++)
{
    var itemNumber = page[i].ItemNumber;
    var itemValue = page[i].Value;
}
```

### Work with IQueryable&lt;T&gt;

You can get `IQueryable<T>` from `Where` in EfCore or `Query<T>` in NHibernate, and then:

```c#
IQueryable<ExampleModel> queryable = GetQueryable();//...

var page = queryable.GetPage(15, 50);

var totalMemberCount = page.TotalMemberCount;

for(var i = 0; i < page.CurrentPageSize; i++)
{
    var itemNumber = page[i].ItemNumber;
    var itemValue = page[i].Value;
}
```

Just do it.

### Work with ORMs

#### For Chloe ORM

Install `DotNetCore.Collections.Paginable.Chloe` package:

```
Install-Package DotNetCore.Collections.Paginable.Chloe
```

then:

```c#
//... do some config for Chloe by EntityTypeBuilder<ExampleModel>

using(var db = new MsSqlContext(connectionString))
{
    var page = db.Query<ExampleModel>().GetPage(15, 50);

    var totalPageCount = page.TotalPageCount;
    var totalMemberCount = page.TotalMemberCount;
    var pageSize = page.PageSize;

    var currentPageNumber = page.CurrentPageNumber;
    var currentPageSize = page.CurrentPageSize;

    var hasNext = page.HasNext;
    var HasPrevious = page.HasPrevious;

    for(var i = 0; i < currentPageSize; i++)
    {
        var id = page[i].Value.Id;
    }
}
```

#### For Dos.ORM

Install `DotNetCore.Collections.Paginable.DosOrm` package:

```
Install-Package DotNetCore.Collections.Paginable.DosOrm
```

then:

```c#
var _session = new DbSession(DatabaseType.SqlServer, connectionString);

var page = _dosOrmSession.From<ExampleModel>().GetPage(1, 9);

var totalPageCount = page.TotalPageCount;
//...

.
.
.

class ExampleModel : Entity
{
    public ExampleModel() : base("ExampleModels") { }

    public virtual int Id { get; set; }

    public override Field[] GetPrimaryKeyFields() => new Field[] { new Field("Id"), };
}
```

#### For FreeSql

Install `DotNetCore.Collections.Paginable.FreeSql` package:

```
Install-Package DotNetCore.Collections.Paginable.FreeSql
```

then:

```c#
var _freeSql = new FreeSql.FreeSqlBuilder()
    .UseConnectionString(DataType.SqlServer, connectionString)
    .UseAutoSyncStructure(false)
    .Build();

//... do some config for FreeSql

var page = _freeSql.Select<ExampleModel>().GetPage(1, 9);

var totalPageCount = page.TotalPageCount;
//...
```

or call the extension method of DbSet directly:

```c#
var ctx = _freeSql.CreateDbContext();
var source = ctx.Set<ExampleModel>();

var page = source.GetPage(1, 9);

var totalPageCount = page.TotalPageCount;
//...
```

or

```c#
using(var ctx = new ExampleDbContext())
{
    var page = ctx.ExampleModels.GetPage(1, 9);

    var totalPageCount = page.TotalPageCount;
    //...
}

.
.
.

class ExampleDbContext: DbContext
{
    public DbSet<ExampleModel> ExampleModel {get; set;}

    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        builder.UseFreeSql(_freeSqlInstance);
    }
}
```

#### For SqlSugar

Install `DotNetCore.Collections.Paginable.SqlSugar` package:

```
Install-Package DotNetCore.Collections.Paginable.SqlSugar
```

then:

```c#
var sqlSugar = new SqlSugarClient(new ConnectionConfig{
    ConnectionString = connectionString,
    DbType = DbType.SqlServer,
    IsAutoCloseConnection = true
});

//... do some config for sqlSugar

var page = _sqlSugar.Query<ExampleModel>().GetPage(1, 9);

var totalPageCount = page.TotalPageCount;
//...
```

#### For NHibernate

Install `DotNetCore.Collections.Paginable.NHibernate` package:

```
Install-Package DotNetCore.Collections.Paginable.NHibernate
```

then:

```c#
//... do some config for NHibernate by FluentNHibernate.ClassMap<ExampleModel>

using(var session = GetAndOpenSession())
{
    var page = session.QueryOver<ExampleModel>().GetPage(1, 9);

    var totalPageCount = page.TotalPageCount;
    //...
}
```

#### For Microsoft.EntityFrameworkCore

```c#
//... do come config for EFCore

using(var context = new ExampleDbContext())
{
    var page = context.ExampleModels.Where(x => x.Id > 100).GetPage(1, 9);

    var totalPageCount = page.TotalPageCount;
    //...
}
```

or call the extension method of DbSet directly:

Install `DotNetCore.Collections.Paginable.EntityFrameworkCore` package first:

```
Install-Package DotNetCore.Collections.Paginable.EntityFrameworkCore
```

then:

```c#
using(var context = new ExampleDbContext())
{
    var page = context.ExampleModels.GetPage(1, 9);

    var totalPageCount = page.TotalPageCount;
    //...
}
//...
```

### Keyset (seek) pagination

Offset pagination degrades on deep pages because the database still scans the skipped rows.
Keyset (a.k.a. seek / cursor) pagination replaces `OFFSET n` with a `WHERE key > @lastKey`
predicate, so every page costs the same and the `COUNT(*)` round trip is avoided. It is the
recommended mode for infinite-scroll and cursor-style APIs.

```c#
IQueryable<ExampleModel> queryable = GetQueryable();//...

// First page: no anchor key yet.
var first = queryable.GetFirstPageByKeyset(x => x.Id, pageSize: 50);

// Subsequent pages: pass the ordering key of the last row of the previous page.
var lastId = first.LastMember.Id;
var next = queryable.GetPageByKeyset(x => x.Id, lastId, pageSize: 50);

foreach (var item in next.Members) { /* ... */ }

var hasMore = next.HasNext; // resolved without COUNT(*)
```

`GetFirstPageByKeyset` / `GetPageByKeyset` also have `IEnumerable<T>` overloads for in-memory
sources, and an optional `descending` switch for reverse ordering. Use keyset pagination when you
do **not** need `TotalPageCount` / `TotalMemberCount`; use the offset APIs above when you do.

### Asynchronous paging

The core library exposes `ToPaginableAsync` / `GetPageAsync` for in-memory and `IQueryable<T>`
sources, and the EF Core, FreeSql and SqlSugar integrations provide true end-to-end async
(`CountAsync` + `ToListAsync`, no synchronous database calls) with `CancellationToken` support.

```c#
using(var context = new ExampleDbContext())
{
    var page = await context.ExampleModels
        .GetPageAsync(pageNumber: 1, pageSize: 50, cancellationToken: ct);

    var totalMemberCount = page.TotalMemberCount;
}
```

### Configuration

`PaginableSettingsManager` holds a process-wide settings snapshot. Values are validated on
assignment, so any instance handed out by the library is always in a valid state — configure it
once at startup and treat it as read-only afterwards.

```c#
PaginableSettingsManager.Settings = new PaginableSettings
{
    DefaultPageSize = 50,          // must be >= 1
    MaxMemberItems = 10_000_000    // must be >= 1
};
```

#### For SqlKata with Dapper

Install `DotNetCore.Collections.Paginable.SqlKata` package:

```
Install-Package DotNetCore.Collections.Paginable.SqlKata
```

then:

```c#
using(var connection = new SqlConnection(connectionString))
{
    connection.Open();

    var compiler = new SqlServerCompiler();
    var db = new QueryFactory(connection, compiler);

    var page = db.Query("ExampleModels").GetPage<ExampleModel>(1, 9);

    var totalPageCount = page.TotalCount;
    //...
}
```

### Examples

- [DotNetCore.Collections.Paginable with EFCore](https://github.com/dotnetcore/Collections/blob/dev/sample/Sample.EfCore/Program.cs)
- [DotNetCore.Collections.Paginable with EF6](https://github.com/dotnetcore/Collections/blob/dev/sample/Sample.Ef/Program.cs)

## MultiSet &amp; MultiDictionary

`DotNetCore.Collections.Multi` provides two collection types that are independent of the paging extensions and ship in their own package:

- **`MultiList<T>`** — a multiset (bag): an unordered collection that allows duplicates and tracks the number of occurrences of each element. Supports multiset set operations (`UnionWith` / `IntersectionWith` / `ExceptWith` / `SymmetricExceptWith`, subset &amp; superset judgments, `Overlaps` / `IsDisjointFrom`), copy-expanded enumeration, `CountOf` / `TotalCount` / `DistinctCount`, and injectable `IEqualityComparer<T>`.
- **`MultiDictionary<TKey, TValue>`** — a multimap: a dictionary that associates multiple values with a single key. Implements `IReadOnlyDictionary<TKey, IReadOnlyCollection<TValue>>`, provides `AsLookup()` (an `ILookup` view), per-key value set operations, and a configurable inner-collection factory (`allowDuplicateValues` or a custom factory).

Both target `netstandard2.0`, `netstandard2.1` and `net6.0`. Element/key equality always goes through `IEqualityComparer` (never hash codes alone), `null` elements are supported in `MultiList<T>`, and neither type is thread-safe.

### Install the package

```
Install-Package DotNetCore.Collections.Multi
```

### Write code

```c#
// MultiList<T>: a bag counting occurrences
var bag = new MultiList<string> { "apple", "apple", "banana" };
bag.CountOf("apple");      // 2
bag.TotalCount;            // 3
bag.UnionWith(new[] { "apple", "cherry" });
bag.IsSupersetOf(new[] { "banana" }); // true

// MultiDictionary<K, V>: one key, many values
var map = new MultiDictionary<string, int>();
map.Add("orders", 1001);
map.Add("orders", 1002);
foreach (var order in map["orders"]) { /* 1001, 1002 */ }
var lookup = map.AsLookup();          // LINQ-friendly ILookup view
```

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

## Releasing

Versions are driven by `build/version.props`, which is the single source of truth for every
package — bump the version there and all 11 packages follow.

Publishing to nuget.org is automated by the GitHub Actions `Release` workflow
(`.github/workflows/release.yml`): pushing a tag such as `6.0.0` (or `v6.0.0`) packs all 11
projects and pushes every `.nupkg` / `.snupkg` with the key stored in the `NUGET_API_KEY`
repository secret.

For a local fallback, run `scripts\Publish.bat`, which packs the same 11 projects and pushes
them with a key taken from the `NUGET_API_KEY` environment variable (or from an interactive
prompt).

## License

Member project of [The NCC](https://github.com/dotnetcore), MIT

[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fdotnetcore%2FCollections.svg?type=large)](https://app.fossa.io/projects/git%2Bgithub.com%2Fdotnetcore%2FCollections?ref=badge_large)
