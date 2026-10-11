// Sample.KeysetAnchorNullability — 键集分页锚点参数可空化（F42）
// 运行：dotnet run --project sample/Sample.KeysetAnchorNullability
//
// 演示重点：
//   1. 键选择器返回可空键（string?）时，锚点参数标注与之匹配，NRT 工程不再误报。
//   2. null 锚点 = 无锚点 = 从头的第一页语义；**不是**「键值等于 null 的位置」。
//   3. 首页用 GetFirstPageByKeyset / null 锚点；续页用上页 LastMember 的键。
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
// 1. 第一页（无锚点）
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 第一页（无锚点）===");
var first = orders.GetFirstPageByKeyset(o => o.Code, pageSize: 2);
Console.WriteLine($"成员 = {string.Join(",", first.Members.Select(o => o.Code))}");   // A,B
Console.WriteLine($"HasNext = {first.HasNext}, IsFirstPage = {first.IsFirstPage}");   // True, True

// ---------------------------------------------------------------------------
// 2. 续页：锚点来自上页末键，类型是 string?（键选择器返回 string?）
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 续页：锚点 = 上页末键 ===");

// LastMember 是 Order?（可空返回）；Code 是 string?，所以 anchor 是 string?。
// 这正是 F42 说的「锚点标注与键类型可空性一致」：可空键能直接当锚点用。
string? anchor = first.LastMember?.Code;
Console.WriteLine($"锚点 = {anchor ?? "<null>"}");   // B

if (anchor is not null)
{
    var second = orders.GetPageByKeyset(o => o.Code, anchor, pageSize: 2);
    Console.WriteLine($"成员 = {string.Join(",", second.Members.Select(o => o.Code))}");  // C,D
    Console.WriteLine($"HasNext = {second.HasNext}, IsFirstPage = {second.IsFirstPage}");  // True, False
}

// ---------------------------------------------------------------------------
// 3. null 锚点 = 无锚点（从头开始），不是「键等于 null」
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. null 锚点 = 无锚点 = 从头 ===");
var fromNullAnchor = orders.GetPageByKeyset(o => o.Code, null, pageSize: 2);
Console.WriteLine($"成员 = {string.Join(",", fromNullAnchor.Members.Select(o => o.Code))}");   // A,B（与第一页相同）
Console.WriteLine($"与 GetFirstPageByKeyset 同成员 = {fromNullAnchor.Members.Select(o => o.Id).SequenceEqual(first.Members.Select(o => o.Id))}");

// ⚠️ 语义提醒：null 表示「无锚点」。若排序键本身可存 null，要按 null 键续页需由查询层自定义；
//    本库把 null 一律当作「没有锚点」，而不是「键值等于 null 的位置」。
// ❌ 错误写法：把 null 锚点当成「跳到键为 null 的位置」——实际会从头开始。

// ---------------------------------------------------------------------------
// 4. 翻到底：HasNext 变 false
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 翻到底 ===");
var p = orders.GetFirstPageByKeyset(o => o.Code, pageSize: 2);
int pageNo = 1;
while (p.HasNext)
{
    var key = p.LastMember!.Code;   // LastMember 可能为 null，用 ! 断言非空（上一页非空）
    p = orders.GetPageByKeyset(o => o.Code, key, pageSize: 2);
    pageNo++;
}
Console.WriteLine($"共翻 {pageNo} 页，最后一页 = {string.Join(",", p.Members.Select(o => o.Code))}");  // 3 页, E

Console.WriteLine();
Console.WriteLine("Sample.KeysetAnchorNullability completed.");

/// <summary>示例域模型：订单（Code 为可空排序键）。</summary>
internal sealed record Order(int Id, string? Code);
