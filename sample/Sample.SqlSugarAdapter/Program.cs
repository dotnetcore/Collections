// Sample.SqlSugarAdapter — SqlSugar 适配器入口面与取消令牌边界（F53）
// Sample.SqlSugarAdapter — SqlSugar adapter entry surface and cancellation-token boundary (F53)
// 运行：dotnet run --project sample/Sample.SqlSugarAdapter
// Run: dotnet run --project sample/Sample.SqlSugarAdapter
//
// 演示重点：
// Highlights:
//   1. SqlSugar 适配器公开三件套：SolidPageExtensions + PaginableSqlSugarQuery<T> + SqlSugarPage<T>；
//   1. The SqlSugar adapter exposes a trio: SolidPageExtensions + PaginableSqlSugarQuery<T> + SqlSugarPage<T>;
//   2. 源类型是 SqlSugar.ISugarQueryable<T>（参数名 query）；
//   2. the source type is SqlSugar.ISugarQueryable<T> (parameter name query);
//   3. F6-38（6.6.0）：取消令牌穿透工厂与取页，**已取消令牌在第一次数据库往返前**抛
//   3. F6-38 (6.6.0): the cancellation token threads through the factory and page fetch, and an **already-cancelled token throws before the first database round trip**
//      OperationCanceledException；中段取消仍取决于 SqlSugar 版本（其 API 无令牌重载）；
//      OperationCanceledException; mid-flight cancellation still depends on the SqlSugar version (its API has no token overloads);
//   4. 包支撑说明型：引用真实适配器程序集，反射打印真实入口签名，不连库。
//   4. package-backed explanatory: references the real adapter assembly, prints the real entry signatures via reflection, and does not connect to a database.
using System;
using System.Linq;
using System.Reflection;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 真实适配器程序集与入口类型
// 1. Real adapter assembly and entry types
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. SqlSugar 适配器程序集与入口类型 ===");
Console.WriteLine("=== 1. SqlSugar adapter assembly and entry types ===");

var assembly = typeof(PaginableSqlSugarQuery<>).Assembly;   // 该包独有类型，锚定真实程序集
                                                            // a type unique to this package; anchors the real assembly
Console.WriteLine($"程序集 / Assembly：{assembly.GetName().Name}  v{assembly.GetName().Version}");

foreach (var type in assembly.GetExportedTypes()
             .Where(t => t.Name is "PaginableSqlSugarQuery`1" or "SqlSugarPage`1" or "SolidPageExtensions")
             .OrderBy(t => t.Name, StringComparer.Ordinal))
{
    Console.WriteLine($"  公开类型 / Public type：{type.FullName}");
}

// ---------------------------------------------------------------------------
// 2. 反射打印 SolidPageExtensions 的真实公开动词
// 2. Reflect and print the real public verbs of SolidPageExtensions
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. SolidPageExtensions 的真实公开动词 ===");
Console.WriteLine("=== 2. The real public verbs of SolidPageExtensions ===");

var spe = assembly.GetType("DotNetCore.Collections.Paginable.SolidPageExtensions");
if (spe is not null)
{
    foreach (var m in spe.GetMethods(BindingFlags.Public | BindingFlags.Static)
                 .Where(m => !m.IsSpecialName)
                 .OrderBy(m => m.Name, StringComparer.Ordinal)
                 .ThenBy(m => m.GetParameters().Length))
    {
        var parameters = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name));
        Console.WriteLine($"  {m.Name}({parameters}) -> {m.ReturnType.Name}");
    }
}

// ---------------------------------------------------------------------------
// 3. 统一动词 + 取消令牌边界（F6-38）
// 3. Unified verbs + cancellation-token boundary (F6-38)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 统一动词 + 取消令牌边界 ===");
Console.WriteLine("=== 3. Unified verbs + cancellation-token boundary ===");

// 三件套：SolidPageExtensions（ISugarQueryable<T> 入口）、PaginableSqlSugarQuery<T>、SqlSugarPage<T>。
// The trio: SolidPageExtensions (ISugarQueryable<T> entry point), PaginableSqlSugarQuery<T>, SqlSugarPage<T>.
// 动词同构：ToPaginable / GetPage / *Async；越界抛 ArgumentOutOfRangeException（F40）。
// Isomorphic verbs: ToPaginable / GetPage / *Async; out of range throws ArgumentOutOfRangeException (F40).
Console.WriteLine("query.ToPaginable<T>(20)                         // 建集合 / build collection");
Console.WriteLine("query.GetPage<T>(1, 20)                          // 从全源切一页 / slice one page from the full source");
Console.WriteLine("await query.GetPageAsync<T>(1, 20, ct)           // 6.6+：ct 已取消则不打库 / 6.6+: if ct is already cancelled, no database hit");

// ✅ 6.6+ 语义：已取消令牌在**第一次数据库往返前**抛 OperationCanceledException。
// ✅ 6.6+ semantics: an already-cancelled token throws OperationCanceledException **before the first database round trip**.
//    var ct = new CancellationToken(canceled: true);
//    await Assert.ThrowsAsync<OperationCanceledException>(() => query.GetPageAsync(1, 20, ct));
// ❌ 错误写法（旧假设，6.6+ 会失败）：
// ❌ Wrong approach (an old assumption that fails on 6.6+):
//    Assert.DoesNotThrow(() => query.GetPageAsync(1, 20, cancelledToken)); // 6.6+ 会抛 / throws on 6.6+
Console.WriteLine("注意：SqlSugar 5.1.3 的 CountAsync / ToPageListAsync **无令牌重载**—— / Note: SqlSugar 5.1.3's CountAsync / ToPageListAsync have **no token overloads** —");
Console.WriteLine("      中段取消仍属提供方；SqlSugarHelper 是唯一适配点。 / mid-flight cancellation is still up to the provider; SqlSugarHelper is the only adaptation point.");

// ---------------------------------------------------------------------------
// 4. 共享核心契约（本地可运行，不连库）
// 4. Shared core contract (runs locally, no database)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 共享核心契约（本地可运行，不连库）===");
Console.WriteLine("=== 4. Shared core contract (runs locally, no database) ===");

// 适配器只负责从 ORM 源取数；页对象复用核心契约 IPage<T> / IPageMember<T>。
// The adapter is only responsible for fetching data from the ORM source; the page object reuses the core contract IPage<T> / IPageMember<T>.
IPage<string> page = Paginable.CreatePage(
    new[] { "row-1", "row-2", "row-3" }, pageNumber: 1, pageSize: 10, totalMemberCount: 3);
Console.WriteLine($"IPage<string> 类型 / Type：{page.GetType().Name}");
Console.WriteLine($"CurrentPageSize = {page.CurrentPageSize}, TotalPageCount = {page.TotalPageCount}"); // 3, 1
foreach (var member in page)
{
    Console.WriteLine($"  #{member.ItemNumber} {member.Value}");
}

// ---------------------------------------------------------------------------
// 5. TFM 与为什么不真正执行
// 5. TFM and why it is not really executed
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 5. TFM 与为什么不真正执行 ===");
Console.WriteLine("=== 5. TFM and why it is not really executed ===");
Console.WriteLine("SqlSugar 包无 netstandard2.0；net4* 用 sqlSugar，其它用 sqlSugarCore（F57）。 / The SqlSugar package has no netstandard2.0; net4* uses sqlSugar and the rest use sqlSugarCore (F57).");
Console.WriteLine("GetPage 经 SqlSugarHelper.Count / FetchPageAsync 打库，控制台示例不连库，故只演示入口面。 / GetPage hits the database via SqlSugarHelper.Count / FetchPageAsync; the console example does not connect to a database, so it only demonstrates the entry surface.");

Console.WriteLine();
Console.WriteLine("Sample.SqlSugarAdapter completed.");
