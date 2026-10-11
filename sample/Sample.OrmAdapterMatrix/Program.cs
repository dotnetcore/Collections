// Sample.OrmAdapterMatrix — 9 个 ORM 适配器的入口名对照矩阵（F50）
// 运行：dotnet run --project sample/Sample.OrmAdapterMatrix
//
// 演示重点：
//   1. 九个适配器包把**同一套分页动词**（ToPaginable / GetPage / *Async）接到不同 ORM；
//   2. 但**入口类型名逐包不同**，不可跨包类推（本仓最高风险的 API 幻觉点）；
//   3. EF6 / EF Core / FreeSql.DbContext **只有** SolidPageExtensions，没有独立 Query/Page 对；
//   4. 本 sample 为**说明型**：只引用分页核心包，用字符串打印对照表，不引入任何 ORM 包。
using System;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// 引用核心包只为「锚定」本示例确实在 DotNetCore.Collections.Paginable 语境下讨论适配器；
// 下面打印的适配器入口名全部是文档 F50 / A11 里逐字记录的公开类型名，不是运行期推断。
// 核心契约（所有适配器共享）：IPage / IPage<T> / IPaginable / IPaginable<T>。
Console.WriteLine($"核心契约程序集：{typeof(IPage).Assembly.GetName().Name}");
Console.WriteLine();

// ---------------------------------------------------------------------------
// 1. 九包入口名对照矩阵（逐字来自 F50「10,000 英尺矩阵」）
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 九包入口名对照矩阵 ===");

// 每行：包短名 | 查询集合类型（分页集合） | 页类型 | 是否只有 SolidPageExtensions
// 「—」表示该包**没有**独立集合/页类型，只有 SolidPageExtensions 扩展方法。
var rows = new (string Package, string Query, string Page, string Note)[]
{
    ("核心 …Paginable",        "PaginableEnumerable<T> / PaginableQueryable<T>", "EnumerablePage<T> / QueryablePage<T>", "含 Paginable / 键集 / 片段"),
    ("…EntityFramework",       "—（委托核心 PaginableQueryable）",                "—",                                  "仅 SolidPageExtensions"),
    ("…EntityFrameworkCore",   "—（委托核心）",                                   "—",                                  "仅 SolidPageExtensions + 真异步；另有 KeysetPageAsyncExtensions"),
    ("…FreeSql",               "PaginableFreeSqlQuery<T>",                       "FreeSqlPage<T>",                     "独有 includeNestedMembers"),
    ("…FreeSql.DbContext",     "—",                                              "—",                                  "仅 SolidPageExtensions（委托）"),
    ("…SqlSugar",              "PaginableSqlSugarQuery<T>",                      "SqlSugarPage<T>",                    "无 netstandard2.0"),
    ("…SqlKata",               "PaginableSqlKataQuery<T>",                       "SqlKataPage<T>",                     "源类型非泛型 Query，ToPaginable<T> 显式 T"),
    ("…Chloe",                 "PaginableChloeQuery<T>",                         "ChloePage<T>",                       "独有 additionalQueryFunc"),
    ("…DosOrm",                "PaginableDosQuery<T>",                           "DosPage<T>",                         "additionalQueryFunc；where T : Entity"),
    ("…NHibernate",            "PaginableNhCoreQuery<T>",                        "NhCorePage<T>",                      "ISession 便捷重载；页类型名是 NhCorePage"),
};

Console.WriteLine($"{"包",-24}{"分页集合类型",-40}{"页类型",-40}备注");
Console.WriteLine(new string('-', 140));
foreach (var r in rows)
{
    Console.WriteLine($"{r.Package,-24}{r.Query,-40}{r.Page,-40}{r.Note}");
}

// ---------------------------------------------------------------------------
// 2. 统一动词：所有适配器共用的入口方法名
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 统一动词（各包同名，源类型不同）===");

// 动词名统一，但**接收者类型**逐包不同：
//   FreeSql  : ISelect<T>            SqlSugar: ISugarQueryable<T>
//   Chloe    : IQuery<T>             DosOrm  : FromSection<T>
//   NHibernate: IQueryOver<T>/ISession  SqlKata: Query（非泛型，需显式 T）
//   EF Core  : DbSet<T>/IQueryable<T>
Console.WriteLine("ToPaginable(...)   建分页集合，可多次 GetPage(n)");
Console.WriteLine("GetPage(n[, size]) 从**全源**切一页");
Console.WriteLine("*Async             EF Core 真异步；核心内存形态只提供形状（不观察令牌）");
Console.WriteLine("ToPage / Paginable.CreatePage  片段建页——**不在适配器源上使用**（那是 F32/F58 的路径）");

// ---------------------------------------------------------------------------
// 3. 命名规律与例外（最容易被「类推」坑到的三处）
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 命名规律与例外 ===");

Console.WriteLine("多数：PaginableXxxQuery<T> + XxxPage<T>（FreeSql / SqlSugar / SqlKata / Chloe / Dos）");
Console.WriteLine("例外 1：NHibernate 页类型是 NhCorePage<T>，**不是** NHibernatePage<T>；集合是 PaginableNhCoreQuery<T>");
Console.WriteLine("例外 2：EF6 / EF Core / FreeSql.DbContext **只有** SolidPageExtensions，没有独立 Query/Page 对");
Console.WriteLine("例外 3：SqlKata 源类型是非泛型 Query，必须写 query.ToPaginable<Product>(20)（T 不能推断）");

// ❌ 错误写法：按命名规律硬造类型——下面这些类型在 9 个包中**都不存在**。
//    var x = new PaginableEfCoreQuery<Product>(...);   // 不存在
//    var y = new NHibernatePage<Product>(...);         // 真名是 NhCorePage<T>
//    query.ToPaginable(20);                            // SqlKata 缺显式 <T>，编不过

// ---------------------------------------------------------------------------
// 4. 同名消歧提醒
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 同名 SolidPageExtensions 消歧 ===");
// 10 个包各有一个 public static class SolidPageExtensions（核心 1 + 适配器 9），
// 同项目同时 using 两个适配器命名空间会 CS0104。规避：全限定静态类名或 using 别名。
Console.WriteLine("10 处同名 SolidPageExtensions（核心 1 + 适配器 9）；混用两包须全限定名或别名消歧（U27）。");

Console.WriteLine();
Console.WriteLine("Sample.OrmAdapterMatrix completed.");
