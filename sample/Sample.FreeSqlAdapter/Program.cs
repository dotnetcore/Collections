// Sample.FreeSqlAdapter — FreeSql 适配器入口面（F52）
// 运行：dotnet run --project sample/Sample.FreeSqlAdapter
//
// 演示重点：
//   1. FreeSql 适配器公开三件套：SolidPageExtensions + PaginableFreeSqlQuery<T> + FreeSqlPage<T>；
//   2. 源类型是 FreeSql.ISelect<T>（参数名 select），并有独有开关 includeNestedMembers；
//   3. 与 FreeSql.DbContext 包的分工：DbContext 包**只有** SolidPageExtensions（委托）；
//   4. 为什么这是「包支撑说明型」：GetPage 会经 FreeSqlHelper 真正打库，
//      控制台示例不便连库，故引用**真实适配器程序集**并用反射打印真实入口签名，绝不编造。
using System;
using System.Linq;
using System.Reflection;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 从**真实**适配器程序集读出入口类型（不是字符串字面量，是运行期事实）
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. FreeSql 适配器程序集与入口类型 ===");

// PaginableFreeSqlQuery<T> 是该包独有类型，用它锚定真实程序集（避免与其它适配器同名类型混淆）。
var assembly = typeof(PaginableFreeSqlQuery<>).Assembly;
Console.WriteLine($"程序集：{assembly.GetName().Name}  v{assembly.GetName().Version}");

// 打印该程序集导出的、与本适配器入口相关的公开类型（排序保证输出确定性）。
var entryTypes = assembly.GetExportedTypes()
    .Where(t => t.Name is "PaginableFreeSqlQuery`1" or "FreeSqlPage`1" or "SolidPageExtensions")
    .OrderBy(t => t.Name, StringComparer.Ordinal);

foreach (var type in entryTypes)
{
    Console.WriteLine($"  公开类型：{type.FullName}");
}

// ---------------------------------------------------------------------------
// 2. 反射打印 SolidPageExtensions 的真实公开方法签名
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. SolidPageExtensions 的真实公开动词 ===");

// 适配器的 SolidPageExtensions 与核心包同名（同在命名空间 DotNetCore.Collections.Paginable），
// 所以这里按「全名」从**适配器程序集**取，而不是写 SolidPageExtensions（那会与核心包 CS0104）。
var spe = assembly.GetType("DotNetCore.Collections.Paginable.SolidPageExtensions");
if (spe is null)
{
    Console.WriteLine("  （未找到 SolidPageExtensions——不应发生）");
}
else
{
    var verbs = spe.GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Where(m => !m.IsSpecialName)
        .OrderBy(m => m.Name, StringComparer.Ordinal)
        .ThenBy(m => m.GetParameters().Length);

    foreach (var m in verbs)
    {
        var parameters = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name));
        Console.WriteLine($"  {m.Name}({parameters}) -> {m.ReturnType.Name}");
    }
}

// ---------------------------------------------------------------------------
// 3. 统一动词与 FreeSql 独有的 includeNestedMembers
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 统一动词 + FreeSql 独有开关 ===");

// 统一动词：ToPaginable / ToPaginableAsync / GetPage / GetPageAsync；接收者 FreeSql.ISelect<T>。
// 独有：includeNestedMembers —— 是否包含嵌套成员（导航加载），影响结果形状与成本。
// 约束：where T : class。同步计数有 .AsInt32() 填充（生成器 SyncCountCastExpression）。
Console.WriteLine("select.ToPaginable<T>(20, limitedMemberCount: null, includeNestedMembers: true)  // 建集合，含嵌套成员");
Console.WriteLine("select.GetPage<T>(1, 20, includeNestedMembers: false)                          // 从全源切一页");
Console.WriteLine("await select.GetPageAsync<T>(1, 20, includeNestedMembers: false, cancellationToken: ct)");

// ✅ 正确写法（真实签名，参数名来自生成器属性）：
//    PaginableFreeSqlQuery<Product> set =
//        select.ToPaginable(20, limitedMemberCount: null, includeNestedMembers: true);
//    IPage<Product> page = select.GetPage(1, 20, includeNestedMembers: false);
// ❌ 错误写法：在只引用 FreeSql.DbContext 包时直接 new PaginableFreeSqlQuery<Product>(...)——
//    DbContext 包**只有** SolidPageExtensions（委托），没有自有工厂；要用具体类型请引用 FreeSql 包。

// ---------------------------------------------------------------------------
// 4. 共享的核心契约：无论哪个适配器，页结果都是 IPage<T>
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 共享核心契约（本地可运行，不连库）===");

// 适配器只负责「从 ORM 源取数」，页对象本身复用核心契约 IPage<T> / IPageMember<T>。
// 这里用核心的片段重建入口造一个真实页，证明契约形状与适配器无关。
IPage<string> page = Paginable.CreatePage(
    new[] { "sku-001", "sku-002" }, pageNumber: 1, pageSize: 5, totalMemberCount: 2);
Console.WriteLine($"IPage<string> 类型：{page.GetType().Name}");
Console.WriteLine($"CurrentPageSize = {page.CurrentPageSize}, TotalPageCount = {page.TotalPageCount}"); // 2, 1
foreach (var member in page)
{
    Console.WriteLine($"  #{member.ItemNumber} {member.Value}");
}

// ---------------------------------------------------------------------------
// 5. 为什么本 sample 不真正执行分页
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 5. 为什么不真正执行 ===");
Console.WriteLine("FreeSql 的 GetPage 内部经 FreeSqlHelper.Count → select.Count() 打库；");
Console.WriteLine("控制台示例若连库会依赖真实数据源，违背「可复现、确定性」目标，故只演示入口面。");

Console.WriteLine();
Console.WriteLine("Sample.FreeSqlAdapter completed.");
