// Sample.PageMetadata — 页面元数据与校验（6.0.0）
// Sample.PageMetadata — Page metadata and validation (6.0.0)
// 运行：dotnet run --project sample/Sample.PageMetadata
// Run: dotnet run --project sample/Sample.PageMetadata
//
// 演示重点：
// Highlights:
//   1. PageMetadata 是页上的元数据「快照」：可序列化、可断言、不随源变化。
//   1. PageMetadata is the metadata "snapshot" on a page: serializable, assertable, unaffected by changes to the source.
//   2. PageFragmentInfo 是调用方「输入」用的元数据；FromMetadata / ToMetadata 与页实例共用同一派生源。
//   2. PageFragmentInfo is the metadata used as caller "input"; FromMetadata / ToMetadata share the same derivation source as the page instance.
//   3. TotalPageCount（空源报 1）与 RealPageCount（空源报 0）用途不同，勿混。
//   3. TotalPageCount (reports 1 for an empty source) and RealPageCount (reports 0 for an empty source) serve different purposes; do not mix them up.
//   4. 空页 / 末页 / 整除末页的导航字段由 PageMetaInfo.Calculate 统一派生。
//   4. The navigation fields of an empty page / last page / evenly-dividing last page are uniformly derived by PageMetaInfo.Calculate.
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
// 1. Read the page metadata snapshot
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 读页元数据 ===");
Console.WriteLine("=== 1. Read page metadata ===");

IPage<Order> page = all.GetPage(3, 5); // 第 3 页（末页）：Id 11,12
                                       // Page 3 (last page): Id 11,12
PageMetadata meta = page.GetMetadata();

Console.WriteLine($"TotalPageCount   = {meta.TotalPageCount}");   // 3
Console.WriteLine($"RealPageCount    = {meta.RealPageCount}");    // 3
Console.WriteLine($"TotalMemberCount = {meta.TotalMemberCount}"); // 12
Console.WriteLine($"PageSize         = {meta.PageSize}");         // 5
Console.WriteLine($"CurrentPageNumber= {meta.CurrentPageNumber}");// 3
Console.WriteLine($"CurrentPageSize  = {meta.CurrentPageSize}");  // 2（末页）
                                                                   // 2 (last page)
Console.WriteLine($"HasPrevious      = {meta.HasPrevious}");      // True
Console.WriteLine($"HasNext          = {meta.HasNext}");          // False（末页）
                                                                   // False (last page)

// ---------------------------------------------------------------------------
// 2. ToString 多行摘要（诊断友好）
// 2. ToString multi-line summary (diagnostic-friendly)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. ToString 摘要 ===");
Console.WriteLine("=== 2. ToString summary ===");
Console.WriteLine(meta.ToString());

// ---------------------------------------------------------------------------
// 3. 片段元数据往返（共用同一派生源，导航不漂移）
// 3. Fragment metadata round-trip (sharing the same derivation source, no navigation drift)
// ---------------------------------------------------------------------------
Console.WriteLine("=== 3. PageFragmentInfo 往返 ===");
Console.WriteLine("=== 3. PageFragmentInfo round-trip ===");

// 从已有页反推 info，再序列化给另一端。
// Derive info back from an existing page, then serialize it to the other end.
PageFragmentInfo info = PageFragmentInfo.FromMetadata(meta);
Console.WriteLine($"FromMetadata PageNumber      = {info.PageNumber}");      // 3
Console.WriteLine($"FromMetadata PageSize        = {info.PageSize}");        // 5
Console.WriteLine($"FromMetadata TotalMemberCount= {info.TotalMemberCount}"); // 12

// info -> 页元数据快照，与页实例共用 PageMetaInfo.Calculate，两边不会漂移。
// info -> page metadata snapshot, sharing PageMetaInfo.Calculate with the page instance, so the two sides never drift.
PageMetadata again = info.ToMetadata();
Console.WriteLine($"往返后 TotalPageCount 一致 / round-trip TotalPageCount consistent = {again.TotalPageCount == meta.TotalPageCount}"); // True
Console.WriteLine($"往返后 CurrentPageSize 一致 / round-trip CurrentPageSize consistent = {again.CurrentPageSize == meta.CurrentPageSize}"); // True
Console.WriteLine($"往返后 HasNext 一致 / round-trip HasNext consistent         = {again.HasNext == meta.HasNext}"); // True

// ---------------------------------------------------------------------------
// 4. 空页：TotalPageCount=1 但 RealPageCount=0
// 4. Empty page: TotalPageCount=1 but RealPageCount=0
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 空页元数据 ===");
Console.WriteLine("=== 4. Empty page metadata ===");

IPage<Order> empty = Paginable.CreateEmptyPage<Order>();
PageMetadata emptyMeta = empty.GetMetadata();
Console.WriteLine($"空页 TotalPageCount / Empty page TotalPageCount   = {emptyMeta.TotalPageCount}");   // 1（「有一个空页」）
                                                                                                  // 1 ("there is one empty page")
Console.WriteLine($"空页 RealPageCount / Empty page RealPageCount    = {emptyMeta.RealPageCount}");    // 0（真实页数）
                                                                                                  // 0 (the real page count)
Console.WriteLine($"空页 TotalMemberCount / Empty page TotalMemberCount = {emptyMeta.TotalMemberCount}"); // 0
Console.WriteLine($"空页 CurrentPageSize / Empty page CurrentPageSize  = {emptyMeta.CurrentPageSize}");  // 0

// ❌ 错误写法：用 TotalPageCount == 0 判「没有页」——空源时它是 1。
// ❌ Wrong usage: using TotalPageCount == 0 to decide "there are no pages" — for an empty source it is 1.
// ✅ 空源判断用 TotalMemberCount == 0 或 RealPageCount == 0。
// ✅ For an empty source use TotalMemberCount == 0 or RealPageCount == 0.
Console.WriteLine($"空源判断（应为 True）/ empty-source check (should be True): TotalMemberCount==0 -> {emptyMeta.TotalMemberCount == 0}"); // True

Console.WriteLine();
Console.WriteLine("Sample.PageMetadata completed.");

// 每个 sample 自带的小型域类型，不跨 sample 共享。
// Each sample carries its own small domain type, not shared across samples.
internal sealed class Order
{
    public int Id;
    public DateTimeOffset CreatedAt;
    public string Name = string.Empty;
    public decimal Amount;
}
