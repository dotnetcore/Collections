// Sample.EmptyAndSinglePage — 空页与单页集合（F49）
// 运行：dotnet run --project sample/Sample.EmptyAndSinglePage
//
// 演示重点：
//   1. EmptyPage<T>：查询无结果时返回它而非 null；固定 pageNumber=1、pageSize=0、totalMemberCount=0。
//   2. PaginableSinglePage<T>：把「已存在的一页」包成 IPaginable<T>；集合层 PageCount 恒为 1。
//   3. 元数据分层：集合层 PageCount=1，页层保留全局编号（12 页中的第 3 页仍是 3/12）。
//   4. GetPage(2) 抛 ArgumentOutOfRangeException。
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 空页：无结果不是 null
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. EmptyPage<T> ===");
IPage<Order> empty = new EmptyPage<Order>();
Console.WriteLine($"TotalMemberCount  = {empty.TotalMemberCount}");   // 0
Console.WriteLine($"CurrentPageNumber = {empty.CurrentPageNumber}"); // 1
Console.WriteLine($"PageSize          = {empty.PageSize}");          // 0
Console.WriteLine($"CurrentPageSize   = {empty.CurrentPageSize}");   // 0
Console.WriteLine($"TotalPageCount    = {empty.TotalPageCount}");    // 1
Console.WriteLine($"可安全枚举项数    = {empty.ToOriginalItems().Count()}");   // 0

// 工厂等价入口：
var empty2 = Paginable.CreateEmptyPage<Order>();
Console.WriteLine($"CreateEmptyPage → TotalMemberCount={empty2.TotalMemberCount}");   // 0
// ❌ 错误写法：查询无结果返回 null，让下游 NRE。✅ 统一返回 EmptyPage<T>。

// ---------------------------------------------------------------------------
// 2. 单页集合：PageCount 恒为 1
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. PaginableSinglePage<T>（第 3 页 / 共 12 条 / 每页 5）===");
var items = new List<Order> { new(11, "K"), new(12, "L") };   // 第 3 页实际 2 条（12 - skip(10)）
PaginableSinglePage<Order> set = Paginable.CreateSinglePageSet(
    items, pageNumber: 3, pageSize: 5, totalMemberCount: 12);

Console.WriteLine($"集合层 PageCount   = {set.PageCount}");     // 1（恒为 1）
Console.WriteLine($"集合层 MemberCount = {set.MemberCount}");   // 12（源总数，不是 items.Count）
Console.WriteLine($"集合层 PageSize    = {set.PageSize}");      // 5

var p1 = set.GetPage(1);
Console.WriteLine($"页层 CurrentPageNumber = {p1.CurrentPageNumber}");   // 3（保留全局编号）
Console.WriteLine($"页层 TotalPageCount    = {p1.TotalPageCount}");      // 3
Console.WriteLine($"枚举集合得到的页数     = {set.Count()}");            // 1

// 同时它也是 IPaginable<Order>：
IPaginable<Order> asInterface = set;
Console.WriteLine($"作为 IPaginable<Order> 取第 1 页 → 成员 = {string.Join(",", asInterface.GetPage(1).ToOriginalItems().Select(o => o.Id))}"); // 11,12

// ---------------------------------------------------------------------------
// 3. GetPage(2) 越界
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. GetPage(2) → ArgumentOutOfRangeException ===");
try
{
    set.GetPage(2);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"{ex.GetType().Name}, ParamName={ex.ParamName}");   // pageNumber
}

// ---------------------------------------------------------------------------
// 4. PageFragmentInfo 入口 + 陷阱
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. PageFragmentInfo 入口 ===");
var info = new PageFragmentInfo(pageNumber: 3, pageSize: 5, totalMemberCount: 12);
PaginableSinglePage<Order> set2 = Paginable.CreateSinglePageSet(items, info);
Console.WriteLine($"CreateSinglePageSet(info) → PageCount={set2.PageCount}, MemberCount={set2.MemberCount}");

// ⚠️ 陷阱：set.PageCount 恒为 1，不能拿它当源总页数；源总页数读 GetPage(1).TotalPageCount。
// ❌ 错误写法：用 set.PageCount 渲染「共 N 页」分页条（永远显示 1）。

Console.WriteLine();
Console.WriteLine("Sample.EmptyAndSinglePage completed.");

/// <summary>示例域模型：订单。</summary>
internal sealed record Order(int Id, string Name);
