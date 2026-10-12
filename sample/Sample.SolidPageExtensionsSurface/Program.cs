// Sample.SolidPageExtensionsSurface — 偏移分页的扩展入口面（all releases）
// Sample.SolidPageExtensionsSurface — the extension entry surface of offset pagination (all releases)
// 运行：dotnet run --project sample/Sample.SolidPageExtensionsSurface
// Run: dotnet run --project sample/Sample.SolidPageExtensionsSurface
//
// 演示重点：
// Highlights:
//   1. 核心包的 ToPaginable / GetPage（IEnumerable + IQueryable）两种源。
//   1. The core package's ToPaginable / GetPage for two kinds of source (IEnumerable + IQueryable).
//   2. 动词区分：GetPage = 从**全源**切一页；片段用 ToPage。
//   2. Verb distinction: GetPage = slice one page from a **full source**; a fragment uses ToPage.
//   3. 空源返回空页、越界抛 ArgumentOutOfRangeException（6.2.0）。
//   3. An empty source returns an empty page; out-of-range throws ArgumentOutOfRangeException (6.2.0).
//   4. ⚠️ 头号陷阱：SolidPageExtensions 在 10 个包同名，两包同引裸写类型名会 CS0104。
//   4. ⚠️ The number-one pitfall: SolidPageExtensions has the same name in 10 packages, so referencing two packages and writing the bare type name causes CS0104.
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var orders = new List<Order>
{
    new(1, "A"), new(2, "B"), new(3, "C"), new(4, "D"), new(5, "E"),
};

// ---------------------------------------------------------------------------
// 1. ToPaginable：把全源变成可翻页集合（IEnumerable）
// 1. ToPaginable: turn a full source into a pageable set (IEnumerable)
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. ToPaginable(IEnumerable) ===");
var set = orders.ToPaginable(pageSize: 2);
Console.WriteLine($"PageSize={set.PageSize}, PageCount={set.PageCount}, MemberCount={set.MemberCount}"); // 2,3,5
Console.WriteLine($"GetPage(2) 成员 / members = {string.Join(",", set.GetPage(2).ToOriginalItems().Select(o => o.Id))}"); // 3,4

// ---------------------------------------------------------------------------
// 2. GetPage：从全源直接切一页
// 2. GetPage: slice one page directly from a full source
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. GetPage(IEnumerable) ===");
var page = orders.GetPage(3, 2);
Console.WriteLine($"第 3 页 / Page 3: CurrentPageNumber={page.CurrentPageNumber}, CurrentPageSize={page.CurrentPageSize}"); // 3,1
Console.WriteLine($"成员 / Members = {string.Join(",", page.ToOriginalItems().Select(o => o.Id))}");   // 5

// ---------------------------------------------------------------------------
// 3. IQueryable 源：同一套动词
// 3. IQueryable source: the same set of verbs
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. ToPaginable / GetPage(IQueryable) ===");
IQueryable<Order> query = orders.AsQueryable();
var qset = query.ToPaginable(pageSize: 2);
Console.WriteLine($"ToPaginable(Queryable) → PageCount={qset.PageCount}");   // 3
var qpage = query.GetPage(1, 2);
Console.WriteLine($"GetPage(Queryable) → 成员 / Members = {string.Join(",", qpage.ToOriginalItems().Select(o => o.Id))}"); // 1,2

// ---------------------------------------------------------------------------
// 4. 空源不抛、越界抛（6.2.0）
// 4. An empty source does not throw; out-of-range throws (6.2.0)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 空源 vs 越界 ===");
Console.WriteLine("=== 4. Empty source vs out-of-range ===");
var emptyPage = new List<Order>().GetPage(1, 2);
Console.WriteLine($"空源 / Empty source GetPage(1) → TotalMemberCount={emptyPage.TotalMemberCount}（不抛 / no throw）");   // 0
try
{
    orders.GetPage(0, 2);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"越界 / Out-of-range → {ex.GetType().Name}, ParamName={ex.ParamName}");   // pageNumber
}

// ---------------------------------------------------------------------------
// 5. 同名类型陷阱
// 5. The same-named type pitfall
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 5. SolidPageExtensions 同名陷阱 ===");
Console.WriteLine("=== 5. The SolidPageExtensions same-name pitfall ===");
// 扩展方法都声明在命名空间 DotNetCore.Collections.Paginable（核心包类型全限定名如下）。
// The extension methods are all declared in the namespace DotNetCore.Collections.Paginable (the core package type's fully qualified name follows).
Console.WriteLine($"核心包类型所在程序集 / Assembly of the core package type = {typeof(DotNetCore.Collections.Paginable.SolidPageExtensions).Assembly.GetName().Name}");
Console.WriteLine("9 个 ORM 适配器各有同名 public static class SolidPageExtensions（命名空间相同、程序集不同）。 / Each of the 9 ORM adapters has a same-named public static class SolidPageExtensions (same namespace, different assembly).");
Console.WriteLine("同时引用两个包时裸写 typeof(SolidPageExtensions) → CS0104；扩展方法语法 db.GetPage(...) 不碰类型名，通常无碍。 / Referencing two packages and writing the bare typeof(SolidPageExtensions) -> CS0104; the extension-method syntax db.GetPage(...) does not touch the type name and is usually fine.");
// ❌ 错误写法（同时引用核心 + EF Core 时二义）：
// ❌ Wrong approach (ambiguous when referencing both core + EF Core):
// var t = typeof(SolidPageExtensions);   // CS0104
// ✅ 正确写法：写全限定名 typeof(DotNetCore.Collections.Paginable.SolidPageExtensions)，
// ✅ Correct approach: write the fully qualified name typeof(DotNetCore.Collections.Paginable.SolidPageExtensions),
//             或 extern alias 区分程序集，或只用扩展方法语法不碰类型名。
//             or use extern alias to distinguish the assemblies, or use only the extension-method syntax without touching the type name.

// ---------------------------------------------------------------------------
// 6. 动词陷阱：GetPage vs ToPage
// 6. The verb pitfall: GetPage vs ToPage
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 6. GetPage（全源）vs ToPage（片段）===");
Console.WriteLine("=== 6. GetPage (full source) vs ToPage (fragment) ===");
var fragment = new List<Order> { orders[4] };   // 已经是「第 3 页」的内容（5 条 / 每页 2 → 第 3 页仅 1 条） / already the content of "page 3" (5 items / 2 per page -> page 3 has only 1 item)
var fragmentPage = fragment.ToPage(pageNumber: 3, pageSize: 2, totalMemberCount: 5);
Console.WriteLine($"ToPage → CurrentPageNumber={fragmentPage.CurrentPageNumber}, 首成员全局编号 / first member's global number={fragmentPage[0].ItemNumber}"); // 3, 5
// ❌ 错误写法：对已切好的片段调用 GetPage —— 会二次切片，得到错页。
// ❌ Wrong approach: calling GetPage on an already-sliced fragment — it slices again and yields the wrong page.

Console.WriteLine();
Console.WriteLine("Sample.SolidPageExtensionsSurface completed.");

/// <summary>示例域模型：订单。</summary>
/// <summary>Sample domain model: an order.</summary>
internal sealed record Order(int Id, string Name);
