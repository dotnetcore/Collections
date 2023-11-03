# Collections

[![Member project of .NET Core Community](https://img.shields.io/badge/member%20project%20of-NCC-9e20c9.svg)](https://github.com/dotnetcore)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/dotnetcore/CAP/master/LICENSE.txt)
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fdotnetcore%2FCollections.svg?type=shield)](https://app.fossa.io/projects/git%2Bgithub.com%2Fdotnetcore%2FCollections?ref=badge_shield)

NCC Collections consists of a set of collection-based extensions and tools, such as paging extensions.

## Nuget Packages

| Package Name                                                                                                                                 | Version                                                                                      | Downloads                                                                                     |
| -------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------- |
| [DotNetCore.Collections.Paginable](https://www.nuget.org/packages/DotNetCore.Collections.Paginable/)                                         | ![](https://img.shields.io/nuget/v/DotNetCore.Collections.Paginable.svg)                     | ![](https://img.shields.io/nuget/dt/DotNetCore.Collections.Paginable.svg)                     |
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
ar page = list.GetPage(15, 50);

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
var sqlSugar = new SqlSugatClient(new ConnectionConfig{
    ConnectionString = connectionString,
    DbType = DbTypee.SqlServer,
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
    var pagee = context.ExampleModels.GetPage(1, 9);

    var totalPageCount = page.TotalPageCount;
    //...
}
//...
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

## License

Member project of [The NCC](https://github.com/dotnetcore), MIT

[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fdotnetcore%2FCollections.svg?type=large)](https://app.fossa.io/projects/git%2Bgithub.com%2Fdotnetcore%2FCollections?ref=badge_large)
