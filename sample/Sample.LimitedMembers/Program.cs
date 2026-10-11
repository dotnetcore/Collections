// Sample.LimitedMembers — 成员上限与开区间（F38）
// 运行：dotnet run --project sample/Sample.LimitedMembers
//
// 演示重点：
//   1. 两层限制：全局硬上限 MaxMemberItems（默认 10_000_000，恰好等于上限允许，6.0.0 开区间修复）
//      与每次调用可选的 limitedMemberCount（封顶展示）。
//   2. 封顶计算集中在 PaginableCalc（在 Internal 命名空间，但 public static，可单测）。
//   3. limited 是「展示封顶」不是过滤谓词：截断的是分页视野（MemberCount / PageCount 上界），不是 WHERE。
//   4. LimitedMembersTypes 枚举：Unlimited = 0，Customize = 1。
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable;
using DotNetCore.Collections.Paginable.Internal;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

// ---------------------------------------------------------------------------
// 1. PaginableCalc：封顶计算的单点
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. PaginableCalc 封顶计算 ===");

// limited 有效且低于真实 COUNT -> 取 limited（min）。
Console.WriteLine($"GetRealMemberCount(50, 120)   = {PaginableCalc.GetRealMemberCount(50, 120)}");   // 50
// null 表示不限制 -> 保留真实 COUNT。
Console.WriteLine($"GetRealMemberCount(null, 120) = {PaginableCalc.GetRealMemberCount(null, 120)}"); // 120
// limited 高于真实 COUNT -> 取真实 COUNT（min）。
Console.WriteLine($"GetRealMemberCount(200, 120)  = {PaginableCalc.GetRealMemberCount(200, 120)}");  // 120
// 向上取整：末页短页仍算一页。
Console.WriteLine($"GetRealPageCount(50, 5)       = {PaginableCalc.GetRealPageCount(50, 5)}");       // 10
Console.WriteLine($"GetRealPageCount(120, 50)     = {PaginableCalc.GetRealPageCount(120, 50)}");     // 3

// ---------------------------------------------------------------------------
// 2. limited 分页集合：截断分页视野
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. limited 分页集合 ===");

var data = Enumerable.Range(1, 120)
    .Select(i => new Order { Id = i, CreatedAt = baseTime.AddMinutes(-i), Name = $"order-{i:00}", Amount = i })
    .ToList();

// 真实 120 条，但产品上「只展示前 50 条」：limitedMemberCount 封顶。
PaginableEnumerable<Order> set = data.ToPaginable(pageSize: 5, limitedMemberCount: 50);
Console.WriteLine($"set.MemberCount       = {set.MemberCount}");       // 50（封顶后的数）
Console.WriteLine($"set.PageCount         = {set.PageCount}");         // 10
Console.WriteLine($"set.LimitedMemberCount= {set.LimitedMemberCount}"); // 50

// limited 只影响 MemberCount / PageCount 上界；GetPage(3) 仍是封顶后集合的第 3 页。
IPage<Order> p3 = set.GetPage(3);
Console.WriteLine($"封顶后第 3 页首成员 Id = {p3[0].Value.Id}"); // 11

// ---------------------------------------------------------------------------
// 3. limited 高于真实 COUNT -> 取真实 COUNT
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. limited 高于真实 COUNT ===");

var small = Enumerable.Range(1, 10)
    .Select(i => new Order { Id = i, Name = $"order-{i:00}", Amount = i })
    .ToList();
PaginableEnumerable<Order> smallSet = small.ToPaginable(pageSize: 5, limitedMemberCount: 50);
Console.WriteLine($"MemberCount = {smallSet.MemberCount}"); // 10（min(50, 10)）
Console.WriteLine($"PageCount   = {smallSet.PageCount}");   // 2

// ---------------------------------------------------------------------------
// 4. 全局硬上限：开区间（恰好等于上限允许，超过才拒）
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. MaxMemberItems 开区间 ===");

// 默认 MaxMemberItems = 10_000_000；PageFragmentInfo 构造校验 totalMemberCount > 上限 才拒。
// 恰好等于上限：允许（6.0.0 把闭区间改成开区间）。
var atLimit = new PageFragmentInfo(pageNumber: 1, pageSize: 5, totalMemberCount: 10_000_000);
Console.WriteLine($"恰好 10_000_000 允许：PageNumber = {atLimit.PageNumber}"); // 1

// 超过上限一条：拒。
try
{
    _ = new PageFragmentInfo(pageNumber: 1, pageSize: 5, totalMemberCount: 10_000_001);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"10_000_001 被拒：{ex.ParamName}"); // totalMemberCount
}

// LimitedMembersTypes 枚举语义（Unlimited=0 表示不限制，Customize=1 表示调用级封顶）。
Console.WriteLine($"LimitedMembersTypes.Unlimited = {(int)LimitedMembersTypes.Unlimited}"); // 0
Console.WriteLine($"LimitedMembersTypes.Customize = {(int)LimitedMembersTypes.Customize}"); // 1

Console.WriteLine();
Console.WriteLine("Sample.LimitedMembers completed.");

// 每个 sample 自带的小型域类型，不跨 sample 共享。
internal sealed class Order
{
    public int Id;
    public DateTimeOffset CreatedAt;
    public string Name = string.Empty;
    public decimal Amount;
}
