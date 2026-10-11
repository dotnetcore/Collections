// Sample.SqlKataAdapter — SqlKata 适配器入口面：非泛型 Query 与显式类型参数（F54）
// 运行：dotnet run --project sample/Sample.SqlKataAdapter
//
// 演示重点：
//   1. SqlKata 适配器公开三件套：SolidPageExtensions + PaginableSqlKataQuery<T> + SqlKataPage<T>；
//   2. **源类型非泛型**（SqlKata.Query）——T 不能从 Query 推断，
//      ToPaginable<T> / GetPage<T> **必须显式写元素类型**（生成器 SourceTypeIsNonGeneric = true）；
//   3. 依赖随 TFM 漂移（net451→SqlKata 2.2.0；net8/9/10→4.0.1；其余→3.2.3）；
//   4. 包支撑说明型：引用真实适配器程序集，反射打印真实入口签名，不连库。
using System;
using System.Linq;
using System.Reflection;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 真实适配器程序集与入口类型
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. SqlKata 适配器程序集与入口类型 ===");

var assembly = typeof(PaginableSqlKataQuery<>).Assembly;   // 该包独有类型，锚定真实程序集
Console.WriteLine($"程序集：{assembly.GetName().Name}  v{assembly.GetName().Version}");

foreach (var type in assembly.GetExportedTypes()
             .Where(t => t.Name is "PaginableSqlKataQuery`1" or "SqlKataPage`1" or "SolidPageExtensions")
             .OrderBy(t => t.Name, StringComparer.Ordinal))
{
    Console.WriteLine($"  公开类型：{type.FullName}");
}

// ---------------------------------------------------------------------------
// 2. 反射打印 SolidPageExtensions 的真实公开动词（注意泛型参数 T）
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. SolidPageExtensions 的真实公开动词（含显式 T）===");

var spe = assembly.GetType("DotNetCore.Collections.Paginable.SolidPageExtensions");
if (spe is not null)
{
    foreach (var m in spe.GetMethods(BindingFlags.Public | BindingFlags.Static)
                 .Where(m => !m.IsSpecialName)
                 .OrderBy(m => m.Name, StringComparer.Ordinal)
                 .ThenBy(m => m.GetParameters().Length))
    {
        var generic = m.IsGenericMethodDefinition
            ? "<" + string.Join(",", m.GetGenericArguments().Select(a => a.Name)) + ">"
            : string.Empty;
        var parameters = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name));
        Console.WriteLine($"  {m.Name}{generic}({parameters}) -> {m.ReturnType.Name}");
    }
}

// ---------------------------------------------------------------------------
// 3. 关键点：非泛型源 → 必须显式写 <T>
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 非泛型 Query → 显式类型参数 ===");

// SqlKata.Query 不是泛型，元素类型无从推断，故方法签名带 <T>：
//   ToPaginable<T>(this Query query, int? limitedMemberCount = null)
//   ToPaginable<T>(this Query query, int pageSize, int? limitedMemberCount = null)
//   GetPage<T>(this Query query, int pageNumber[, int pageSize])
//   GetPageAsync<T>(this Query query, int pageNumber[, int pageSize])
Console.WriteLine("new Query(\"Products\").ToPaginable<Product>(20)          // ✅ 显式 <Product>");
Console.WriteLine("new Query(\"Products\").GetPage<Product>(1, 20)           // ✅ 显式 <Product>");

// ❌ 错误写法：期待类型推断——非泛型 Query 没有元素类型可推，编不过。
//    query.ToPaginable(20);   // CS0411：无法从用法推断类型实参

// ---------------------------------------------------------------------------
// 4. 共享核心契约（本地可运行，不连库）
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 共享核心契约（本地可运行，不连库）===");

// 适配器只负责从 ORM 源取数；页对象复用核心契约 IPage<T> / IPageMember<T>。
IPage<string> page = Paginable.CreatePage(
    new[] { "p-01", "p-02" }, pageNumber: 1, pageSize: 2, totalMemberCount: 4);
Console.WriteLine($"IPage<string> 类型：{page.GetType().Name}");
Console.WriteLine($"CurrentPageSize = {page.CurrentPageSize}, TotalPageCount = {page.TotalPageCount}"); // 2, 2
foreach (var member in page)
{
    Console.WriteLine($"  #{member.ItemNumber} {member.Value}");
}

// ---------------------------------------------------------------------------
// 5. TFM 漂移与为什么不真正执行
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 5. TFM 漂移与为什么不真正执行 ===");
Console.WriteLine("SqlKata 主版本随 TFM 变：net451→2.2.0；net8/9/10→4.0.1；其余→3.2.3——行为差异按 TFM 测（F57）。");
Console.WriteLine("GetPage 经 SqlKataHelper.Count → query.Clone().Count<int>() 打库，控制台示例不连库，故只演示入口面。");

Console.WriteLine();
Console.WriteLine("Sample.SqlKataAdapter completed.");
