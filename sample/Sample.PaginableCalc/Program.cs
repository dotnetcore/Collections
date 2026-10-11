// Sample.PaginableCalc — 分页计算工具（F47）
// 运行：dotnet run --project sample/Sample.PaginableCalc
//
// 演示重点：
//   1. PaginableCalc 位于 DotNetCore.Collections.Paginable.Internal 命名空间，但类型是 public。
//   2. GetRealMemberCount：有效上限且更小时取 min，否则取源计数（null = 不限）。
//   3. GetRealPageCount：ceil(members / pageSize)，末页不满也算一页。
//   4. 与 ToPaginable 的 limitedMemberCount 串起来看。
using System;
using System.Collections.Generic;
using DotNetCore.Collections.Paginable;
using DotNetCore.Collections.Paginable.Internal;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 成员数上限裁剪
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. GetRealMemberCount ===");
Console.WriteLine($"GetRealMemberCount(50, 120)  = {PaginableCalc.GetRealMemberCount(50, 120)}");     // 50（上限更小）
Console.WriteLine($"GetRealMemberCount(null, 120) = {PaginableCalc.GetRealMemberCount(null, 120)}"); // 120（不限）
Console.WriteLine($"GetRealMemberCount(200, 120)= {PaginableCalc.GetRealMemberCount(200, 120)}");  // 120（上限大于源计数）

// ---------------------------------------------------------------------------
// 2. 页数换算（向上取整）
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. GetRealPageCount ===");
Console.WriteLine($"GetRealPageCount(50, 5)   = {PaginableCalc.GetRealPageCount(50, 5)}");     // 10
Console.WriteLine($"GetRealPageCount(120, 50) = {PaginableCalc.GetRealPageCount(120, 50)}");   // 3（末页不满也算一页）
Console.WriteLine($"GetRealPageCount(0, 50)   = {PaginableCalc.GetRealPageCount(0, 50)}");     // 0
Console.WriteLine($"GetRealPageCount(1, 50)   = {PaginableCalc.GetRealPageCount(1, 50)}");     // 1
// ⚠️ GetRealPageCount 不校验 pageSize > 0 —— 由工厂校验配套，调用方保证。

// ---------------------------------------------------------------------------
// 3. 与 ToPaginable 的 limitedMemberCount 串联
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 与 ToPaginable(limitedMemberCount) 对照 ===");
var orders = new List<Order>();
for (var i = 1; i <= 8; i++) orders.Add(new(i, $"N{i}"));

var unlimited = orders.ToPaginable(pageSize: 3, limitedMemberCount: null);
Console.WriteLine($"null 上限：MemberCount={unlimited.MemberCount}, PageCount={unlimited.PageCount}");   // 8, 3
var limited = orders.ToPaginable(pageSize: 3, limitedMemberCount: 5);
Console.WriteLine($"上限 5  ：MemberCount={limited.MemberCount}, PageCount={limited.PageCount}, LimitedMemberCount={limited.LimitedMemberCount}"); // 5, 2, 5

// 用 PaginableCalc 复算，验证与集合报告一致。
int real = PaginableCalc.GetRealMemberCount(5, orders.Count);
int pages = PaginableCalc.GetRealPageCount(real, 3);
Console.WriteLine($"PaginableCalc 复算：real={real}, pages={pages}");   // 5, 2

Console.WriteLine();
Console.WriteLine("Sample.PaginableCalc completed.");

/// <summary>示例域模型：订单。</summary>
internal sealed record Order(int Id, string Name);
