// Sample.PageMetadata — 页面元数据与校验（F36）
// 运行：dotnet run --project sample/Sample.PageMetadata
//
// 演示重点：
//   1. PageMetadata 是页上的元数据「快照」：可序列化、可断言、不随源变化。
//   2. PageFragmentInfo 是调用方「输入」用的元数据；FromMetadata / ToMetadata 与页实例共用同一派生源。
//   3. TotalPageCount（空源报 1）与 RealPageCount（空源报 0）用途不同，勿混。
//   4. 空页 / 末页 / 整除末页的导航字段由 PageMetaInfo.Calculate 统一派生。
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
// 1. 读页元数据快照
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 读页元数据 ===");

IPage<Order> page = all.GetPage(3, 5); // 第 3 页（末页）：Id 11,12
PageMetadata meta = page.GetMetadata();

Console.WriteLine($"TotalPageCount   = {meta.TotalPageCount}");   // 3
Console.WriteLine($"RealPageCount    = {meta.RealPageCount}");    // 3
Console.WriteLine($"TotalMemberCount = {meta.TotalMemberCount}"); // 12
Console.WriteLine($"PageSize         = {meta.PageSize}");         // 5
Console.WriteLine($"CurrentPageNumber= {meta.CurrentPageNumber}");// 3
Console.WriteLine($"CurrentPageSize  = {meta.CurrentPageSize}");  // 2（末页）
Console.WriteLine($"HasPrevious      = {meta.HasPrevious}");      // True
Console.WriteLine($"HasNext          = {meta.HasNext}");          // False（末页）

// ---------------------------------------------------------------------------
// 2. ToString 多行摘要（诊断友好）
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. ToString 摘要 ===");
Console.WriteLine(meta.ToString());

// ---------------------------------------------------------------------------
// 3. 片段元数据往返（共用同一派生源，导航不漂移）
// ---------------------------------------------------------------------------
Console.WriteLine("=== 3. PageFragmentInfo 往返 ===");

// 从已有页反推 info，再序列化给另一端。
PageFragmentInfo info = PageFragmentInfo.FromMetadata(meta);
Console.WriteLine($"FromMetadata PageNumber      = {info.PageNumber}");      // 3
Console.WriteLine($"FromMetadata PageSize        = {info.PageSize}");        // 5
Console.WriteLine($"FromMetadata TotalMemberCount= {info.TotalMemberCount}"); // 12

// info -> 页元数据快照，与页实例共用 PageMetaInfo.Calculate，两边不会漂移。
PageMetadata again = info.ToMetadata();
Console.WriteLine($"往返后 TotalPageCount 一致 = {again.TotalPageCount == meta.TotalPageCount}"); // True
Console.WriteLine($"往返后 CurrentPageSize 一致 = {again.CurrentPageSize == meta.CurrentPageSize}"); // True
Console.WriteLine($"往返后 HasNext 一致         = {again.HasNext == meta.HasNext}"); // True

// ---------------------------------------------------------------------------
// 4. 空页：TotalPageCount=1 但 RealPageCount=0
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 空页元数据 ===");

IPage<Order> empty = Paginable.CreateEmptyPage<Order>();
PageMetadata emptyMeta = empty.GetMetadata();
Console.WriteLine($"空页 TotalPageCount   = {emptyMeta.TotalPageCount}");   // 1（「有一个空页」）
Console.WriteLine($"空页 RealPageCount    = {emptyMeta.RealPageCount}");    // 0（真实页数）
Console.WriteLine($"空页 TotalMemberCount = {emptyMeta.TotalMemberCount}"); // 0
Console.WriteLine($"空页 CurrentPageSize  = {emptyMeta.CurrentPageSize}");  // 0

// ❌ 错误写法：用 TotalPageCount == 0 判「没有页」——空源时它是 1。
// ✅ 空源判断用 TotalMemberCount == 0 或 RealPageCount == 0。
Console.WriteLine($"空源判断（应为 True）: TotalMemberCount==0 -> {emptyMeta.TotalMemberCount == 0}"); // True

Console.WriteLine();
Console.WriteLine("Sample.PageMetadata completed.");

// 每个 sample 自带的小型域类型，不跨 sample 共享。
internal sealed class Order
{
    public int Id;
    public DateTimeOffset CreatedAt;
    public string Name = string.Empty;
    public decimal Amount;
}
