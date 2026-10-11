// Sample.FragmentPage — 从已切好的片段构造页面（F32）
// 运行：dotnet run --project sample/Sample.FragmentPage
//
// 演示重点：
//   1. Paginable.CreatePage / FragmentPageExtensions.ToPage：把「已切好的片段 + 元数据」装配成 IPage<T>，
//      片段绝不再被切片（故刻意与 GetPage 不同名）。
//   2. PageFragmentInfo：调用方输入的元数据，构造即校验；FromMetadata / ToMetadata 与页实例共用同一派生源。
//   3. 与全源分页对比：同一数据两种入口应得到一致的全局编号与导航。
//   4. 陷阱：totalMemberCount 必须是全源 COUNT；长片段两种模式都抛。
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
var all = Enumerable.Range(1, 12)
    .Select(i => new Order { Id = i, CreatedAt = baseTime.AddMinutes(-i), Name = $"order-{i:00}", Amount = i })
    .ToList();

// ---------------------------------------------------------------------------
// 1. 三元组元数据建页
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 三元组元数据建页 ===");

// 上游已返回第 3 页、每页 5、全源共 12 条中的 2 条（例如手写 Dapper OFFSET/FETCH 的结果）。
List<Order> items = all.Skip(10).Take(2).ToList();

// 三元组元数据：pageNumber / pageSize / totalMemberCount（全源总数，不是片段长度）。
IPage<Order> page = Paginable.CreatePage(items, pageNumber: 3, pageSize: 5, totalMemberCount: 12);
Console.WriteLine($"TotalPageCount  = {page.TotalPageCount}");  // 3
Console.WriteLine($"CurrentPageSize = {page.CurrentPageSize}"); // 2
Console.WriteLine($"HasNext         = {page.HasNext}");          // False
Console.WriteLine($"首个 ItemNumber  = {page[0].ItemNumber}");   // 11（与全源分页的全局编号一致）

// 等价扩展方法：ToPage（不是 GetPage —— 预切片片段喂 GetPage 会静默二次 skip）。
IPage<Order> page2 = items.ToPage(pageNumber: 3, pageSize: 5, totalMemberCount: 12);
Console.WriteLine($"ToPage 首个 ItemNumber = {page2[0].ItemNumber}"); // 11

// ---------------------------------------------------------------------------
// 2. PageFragmentInfo 与元数据往返
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. PageFragmentInfo 元数据往返 ===");

// 元数据可单独到达（缓存 JSON / gRPC），构造即校验：坏元数据到不了建页阶段。
var info = new PageFragmentInfo(pageNumber: 3, pageSize: 5, totalMemberCount: 12);
IPage<Order> page3 = Paginable.CreatePage(items, info);
Console.WriteLine($"CreatePage(items, info) 首个 ItemNumber = {page3[0].ItemNumber}"); // 11

// 从已有页反推 info（再序列化给另一端）。
PageFragmentInfo same = PageFragmentInfo.FromMetadata(page3.GetMetadata());
Console.WriteLine($"FromMetadata 往返 PageNumber = {same.PageNumber}"); // 3

// info -> 页元数据快照，与页实例共用 PageMetaInfo.Calculate，导航字段不漂移。
PageMetadata snapshot = info.ToMetadata();
Console.WriteLine($"ToMetadata TotalPageCount = {snapshot.TotalPageCount}"); // 3

// ---------------------------------------------------------------------------
// 3. 与全源分页对比（同一数据两种入口）
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 与全源分页对比 ===");

// 左边：GetPage 对全源再切；右边：ToPage 对预切片段不再切。
IPage<Order> fromFull = all.GetPage(3, 5);
var fragment = all.Skip(10).Take(2).ToList();
IPage<Order> fromFragment = fragment.ToPage(3, 5, 12);

Console.WriteLine($"全源   : TotalPageCount={fromFull.TotalPageCount}, CurrentPageSize={fromFull.CurrentPageSize}, 首 ItemNumber={fromFull[0].ItemNumber}");         // 3,2,11
Console.WriteLine($"片段   : TotalPageCount={fromFragment.TotalPageCount}, CurrentPageSize={fromFragment.CurrentPageSize}, 首 ItemNumber={fromFragment[0].ItemNumber}"); // 3,2,11

// ---------------------------------------------------------------------------
// 4. 陷阱：长片段抛错 / total 传片段长度
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 陷阱 ===");

// 长片段（超页容量）在两种模式下都抛 ArgumentException —— 那是数据损坏信号。
var tooLong = all.Take(6).ToList(); // 6 > pageSize(5)
try
{
    Paginable.CreatePage(tooLong, pageNumber: 1, pageSize: 5, totalMemberCount: 12);
}
catch (ArgumentException ex)
{
    Console.WriteLine($"长片段被拒：{ex.ParamName}"); // fragment
}

// ❌ 错误写法：fragment.GetPage(3, 5) 会把已是第 3 页的内容再 skip 一次，得到空页或错位。
// ✅ 正确写法：fragment.ToPage(3, 5, 12) 或 Paginable.CreatePage(...)。

// ❌ 错误写法：totalMemberCount 传 fragment.Count（片段长度），TotalPageCount 会被算成 1。
IPage<Order> wrongTotal = items.ToPage(pageNumber: 1, pageSize: 5, totalMemberCount: items.Count);
Console.WriteLine($"total 传片段长度 => TotalPageCount = {wrongTotal.TotalPageCount}"); // 1（应为 3）

Console.WriteLine();
Console.WriteLine("Sample.FragmentPage completed.");

// 每个 sample 自带的小型域类型，不跨 sample 共享。
internal sealed class Order
{
    public int Id;
    public DateTimeOffset CreatedAt;
    public string Name = string.Empty;
    public decimal Amount;
}
