// Sample.FreeSqlAdapter — FreeSql 适配器入口面（F52）
// Sample.FreeSqlAdapter — FreeSql adapter entry surface (F52)
// 运行：dotnet run --project sample/Sample.FreeSqlAdapter
// Run: dotnet run --project sample/Sample.FreeSqlAdapter
//
// 演示重点：
// Highlights:
//   1. FreeSql 适配器公开三件套：SolidPageExtensions + PaginableFreeSqlQuery<T> + FreeSqlPage<T>；
//   1. The FreeSql adapter exposes a trio: SolidPageExtensions + PaginableFreeSqlQuery<T> + FreeSqlPage<T>;
//   2. 源类型是 FreeSql.ISelect<T>（参数名 select），并有独有开关 includeNestedMembers；
//   2. the source type is FreeSql.ISelect<T> (parameter name select), with a unique switch includeNestedMembers;
//   3. 与 FreeSql.DbContext 包的分工：DbContext 包**只有** SolidPageExtensions（委托）；
//   3. division of labor with the FreeSql.DbContext package: the DbContext package has **only** SolidPageExtensions (delegated);
//   4. 为什么这是「包支撑说明型」：GetPage 会经 FreeSqlHelper 真正打库，
//   4. why this is "package-backed explanatory": GetPage would genuinely hit the database via FreeSqlHelper,
//      控制台示例不便连库，故引用**真实适配器程序集**并用反射打印真实入口签名，绝不编造。
//      and a console example cannot conveniently connect to a database, so it references the **real adapter assembly** and prints the real entry signatures via reflection, never fabricating them.
using System;
using System.Linq;
using System.Reflection;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 从**真实**适配器程序集读出入口类型（不是字符串字面量，是运行期事实）
// 1. Read the entry types from the **real** adapter assembly (not string literals, but runtime facts)
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. FreeSql 适配器程序集与入口类型 ===");
Console.WriteLine("=== 1. FreeSql adapter assembly and entry types ===");

// PaginableFreeSqlQuery<T> 是该包独有类型，用它锚定真实程序集（避免与其它适配器同名类型混淆）。
// PaginableFreeSqlQuery<T> is a type unique to this package; use it to anchor the real assembly (avoiding confusion with same-named types in other adapters).
var assembly = typeof(PaginableFreeSqlQuery<>).Assembly;
Console.WriteLine($"程序集 / Assembly：{assembly.GetName().Name}  v{assembly.GetName().Version}");

// 打印该程序集导出的、与本适配器入口相关的公开类型（排序保证输出确定性）。
// Print the public types exported by this assembly that relate to this adapter's entry points (sorting ensures deterministic output).
var entryTypes = assembly.GetExportedTypes()
    .Where(t => t.Name is "PaginableFreeSqlQuery`1" or "FreeSqlPage`1" or "SolidPageExtensions")
    .OrderBy(t => t.Name, StringComparer.Ordinal);

foreach (var type in entryTypes)
{
    Console.WriteLine($"  公开类型 / Public type：{type.FullName}");
}

// ---------------------------------------------------------------------------
// 2. 反射打印 SolidPageExtensions 的真实公开方法签名
// 2. Reflect and print the real public method signatures of SolidPageExtensions
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. SolidPageExtensions 的真实公开动词 ===");
Console.WriteLine("=== 2. The real public verbs of SolidPageExtensions ===");

// 适配器的 SolidPageExtensions 与核心包同名（同在命名空间 DotNetCore.Collections.Paginable），
// The adapter's SolidPageExtensions is same-named as the core package's (both in the namespace DotNetCore.Collections.Paginable),
// 所以这里按「全名」从**适配器程序集**取，而不是写 SolidPageExtensions（那会与核心包 CS0104）。
// so here we fetch it by "full name" from the **adapter assembly** instead of writing SolidPageExtensions (which would cause CS0104 against the core package).
var spe = assembly.GetType("DotNetCore.Collections.Paginable.SolidPageExtensions");
if (spe is null)
{
    Console.WriteLine("  （未找到 SolidPageExtensions——不应发生） / (SolidPageExtensions not found — this should not happen)");
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
// 3. Unified verbs and FreeSql's unique includeNestedMembers
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 统一动词 + FreeSql 独有开关 ===");
Console.WriteLine("=== 3. Unified verbs + FreeSql unique switch ===");

// 统一动词：ToPaginable / ToPaginableAsync / GetPage / GetPageAsync；接收者 FreeSql.ISelect<T>。
// Unified verbs: ToPaginable / ToPaginableAsync / GetPage / GetPageAsync; receiver FreeSql.ISelect<T>.
// 独有：includeNestedMembers —— 是否包含嵌套成员（导航加载），影响结果形状与成本。
// Unique: includeNestedMembers — whether to include nested members (navigation loading), affecting the result shape and cost.
// 约束：where T : class。同步计数有 .AsInt32() 填充（生成器 SyncCountCastExpression）。
// Constraint: where T : class. Synchronous counting is filled in by .AsInt32() (the generator's SyncCountCastExpression).
Console.WriteLine("select.ToPaginable<T>(20, limitedMemberCount: null, includeNestedMembers: true)  // 建集合，含嵌套成员 / build collection, includes nested members");
Console.WriteLine("select.GetPage<T>(1, 20, includeNestedMembers: false)                          // 从全源切一页 / slice one page from the full source");
Console.WriteLine("await select.GetPageAsync<T>(1, 20, includeNestedMembers: false, cancellationToken: ct)");

// ✅ 正确写法（真实签名，参数名来自生成器属性）：
// ✅ Correct approach (real signatures, parameter names come from the generator's attributes):
//    PaginableFreeSqlQuery<Product> set =
//        select.ToPaginable(20, limitedMemberCount: null, includeNestedMembers: true);
//    IPage<Product> page = select.GetPage(1, 20, includeNestedMembers: false);
// ❌ 错误写法：在只引用 FreeSql.DbContext 包时直接 new PaginableFreeSqlQuery<Product>(...)——
// ❌ Wrong approach: with only the FreeSql.DbContext package referenced, directly doing new PaginableFreeSqlQuery<Product>(...) —
//    DbContext 包**只有** SolidPageExtensions（委托），没有自有工厂；要用具体类型请引用 FreeSql 包。
//    the DbContext package has **only** SolidPageExtensions (delegated) and no factory of its own; to use the concrete type, reference the FreeSql package.

// ---------------------------------------------------------------------------
// 4. 共享的核心契约：无论哪个适配器，页结果都是 IPage<T>
// 4. The shared core contract: no matter which adapter, the page result is IPage<T>
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 共享核心契约（本地可运行，不连库）===");
Console.WriteLine("=== 4. Shared core contract (runs locally, no database) ===");

// 适配器只负责「从 ORM 源取数」，页对象本身复用核心契约 IPage<T> / IPageMember<T>。
// The adapter is only responsible for "fetching data from the ORM source"; the page object itself reuses the core contract IPage<T> / IPageMember<T>.
// 这里用核心的片段重建入口造一个真实页，证明契约形状与适配器无关。
// Here we build a real page with the core's fragment-rebuild entry point, proving the contract shape is independent of the adapter.
IPage<string> page = Paginable.CreatePage(
    new[] { "sku-001", "sku-002" }, pageNumber: 1, pageSize: 5, totalMemberCount: 2);
Console.WriteLine($"IPage<string> 类型 / Type：{page.GetType().Name}");
Console.WriteLine($"CurrentPageSize = {page.CurrentPageSize}, TotalPageCount = {page.TotalPageCount}"); // 2, 1
foreach (var member in page)
{
    Console.WriteLine($"  #{member.ItemNumber} {member.Value}");
}

// ---------------------------------------------------------------------------
// 5. 为什么本 sample 不真正执行分页
// 5. Why this sample does not really execute pagination
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 5. 为什么不真正执行 ===");
Console.WriteLine("=== 5. Why it is not really executed ===");
Console.WriteLine("FreeSql 的 GetPage 内部经 FreeSqlHelper.Count → select.Count() 打库； / FreeSql's GetPage internally hits the database via FreeSqlHelper.Count → select.Count();");
Console.WriteLine("控制台示例若连库会依赖真实数据源，违背「可复现、确定性」目标，故只演示入口面。 / a console example connecting to a database would depend on a real data source, violating the \"reproducible, deterministic\" goal, so it only demonstrates the entry surface.");

Console.WriteLine();
Console.WriteLine("Sample.FreeSqlAdapter completed.");
