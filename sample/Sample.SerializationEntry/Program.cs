// Sample.SerializationEntry — Paginable 侧「无独立序列化 API」与正确重建路径（F58，负结果）
// 运行：dotnet run --project sample/Sample.SerializationEntry
//
// 演示重点：
//   1. **负结果**：Paginable 核心与 9 个适配器**都没有** `ToSerializableModel` / `FromModel`——
//      那是 Multi 的 API（MultiListModel<T> / MultiDictionaryModel<TKey,TValue>，见 F10）；
//   2. 正确路径：序列化 `items` + 三元组元数据（CurrentPageNumber / PageSize / TotalMemberCount），
//      对端用 `Paginable.CreatePage` 重建 IPage<T>，全局编号与整源分页一致；
//   3. `PageMetadata` 是普通属性快照（get-only），可被任意序列化器处理；
//      `PageFragmentInfo.FromMetadata` / `ToMetadata` 可往返；
//   4. IPage / IPaginable 实现**不是**序列化契约——要传就投影成 DTO。
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 0. 先造一个「整源」页（模拟服务器端分页结果）
// ---------------------------------------------------------------------------
Console.WriteLine("=== 0. 服务端整源分页（源 25 条，取第 2 页）===");

var source = Enumerable.Range(1, 25).Select(i => new Order(i, i * 10m)).ToArray();

// 从**整源**切第 2 页（每页 10 条）——这是 SolidPageExtensions 的核心入口（F45）。
IPage<Order> page = source.GetPage(pageNumber: 2, pageSize: 10);
Console.WriteLine($"CurrentPageNumber = {page.CurrentPageNumber}, PageSize = {page.PageSize}"); // 2, 10
Console.WriteLine($"TotalMemberCount = {page.TotalMemberCount}, CurrentPageSize = {page.CurrentPageSize}"); // 25, 10

// ---------------------------------------------------------------------------
// 1. 负结果：不存在 Paginable.ToSerializableModel
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 1. 负结果：Paginable 侧没有 ToSerializableModel ===");

// ❌ 错误写法：从旧教程抄到 Paginable 上——这些成员在 Paginable 核心与 9 适配器里**都不存在**。
//    var model = page.ToSerializableModel();          // 不存在（那是 Multi 的 API，F10）
//    var page2 = Paginable.FromModel(model);           // 不存在
// ✅ 正确写法：投影成 DTO（items + 三元组元数据），对端用 Paginable.CreatePage 重建。
Console.WriteLine("Paginable 侧无 ToSerializableModel / FromModel（对比 Multi 的 MultiListModel / MultiDictionaryModel）。");

// ---------------------------------------------------------------------------
// 2. 正确路径 A：PageMetadata 快照（可被任意序列化器处理）
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. PageMetadata 属性快照 ===");

// GetMetadata() 返回 PageMetadata：普通 get-only 属性快照，序列化器可直接处理。
PageMetadata meta = page.GetMetadata();
Console.WriteLine($"TotalPageCount={meta.TotalPageCount} RealPageCount={meta.RealPageCount} " +
                  $"TotalMemberCount={meta.TotalMemberCount} PageSize={meta.PageSize} " +
                  $"CurrentPageNumber={meta.CurrentPageNumber} CurrentPageSize={meta.CurrentPageSize} " +
                  $"HasPrevious={meta.HasPrevious} HasNext={meta.HasNext}");

// ---------------------------------------------------------------------------
// 3. 正确路径 B：投影 DTO → 「传输」→ 对端重建
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 投影 DTO → 重建 IPage<T> ===");

// 传输载荷：items + 三元组元数据。这里用匿名类型模拟 DTO；真实场景可换成可序列化 POCO。
var dto = new
{
    meta.CurrentPageNumber,   // 2
    meta.PageSize,            // 10
    meta.TotalMemberCount,    // 25
    Items = page.ToOriginalItems().ToArray(),   // 第 2 页的 10 条原始元素
};
Console.WriteLine($"DTO：页码={dto.CurrentPageNumber} 页大小={dto.PageSize} 总数={dto.TotalMemberCount} items={dto.Items.Length}");

// 对端重建：CreatePage 认为 fragment 就是该页的确切内容（**不再切片**）。
IPage<Order> rebuilt = Paginable.CreatePage(
    dto.Items, dto.CurrentPageNumber, dto.PageSize, dto.TotalMemberCount);

Console.WriteLine($"重建页 CurrentPageSize = {rebuilt.CurrentPageSize}");           // 10
Console.WriteLine($"重建页 TotalPageCount  = {rebuilt.TotalPageCount}");            // 3
Console.WriteLine($"重建页首元素全局编号   = {rebuilt.First().ItemNumber}");         // 11（= (2-1)*10+1）

// 一致性断言：重建页与原页的**全局编号**完全一致（证明 CreatePage 复刻了整源分页的编号语义）。
var originalNumbers = page.Select(m => m.ItemNumber).ToArray();
var rebuiltNumbers = rebuilt.Select(m => m.ItemNumber).ToArray();
Console.WriteLine($"原页编号   = [{string.Join(",", originalNumbers)}]");
Console.WriteLine($"重建页编号 = [{string.Join(",", rebuiltNumbers)}]");
Console.WriteLine($"编号一致   = {originalNumbers.SequenceEqual(rebuiltNumbers)}"); // True

// ---------------------------------------------------------------------------
// 4. PageFragmentInfo 的元数据往返
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. PageFragmentInfo.FromMetadata / ToMetadata 往返 ===");

PageFragmentInfo info = PageFragmentInfo.FromMetadata(meta);   // PageMetadata → PageFragmentInfo
Console.WriteLine($"info.PageNumber={info.PageNumber} PageSize={info.PageSize} TotalMemberCount={info.TotalMemberCount}");
PageMetadata roundTrip = info.ToMetadata();                    // 再回到 PageMetadata
Console.WriteLine($"往返一致 = {roundTrip.CurrentPageNumber == meta.CurrentPageNumber && roundTrip.TotalMemberCount == meta.TotalMemberCount}"); // True

// ---------------------------------------------------------------------------
// 5. 为什么不能直接序列化页对象
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 5. 为什么不直接序列化页对象 ===");
Console.WriteLine("IPage / IPaginable 实现不是为序列化设计的 POCO（含内部状态与惰性缓存）；");
Console.WriteLine("跨进程传一页 → 投影 items + 三个整数 → 对端 CreatePage；序列化导航用 PageMetadata 公共属性。");

// ❌ 错误写法：直接 JsonSerializer.Serialize(page)——页对象不是序列化契约。
// ✅ 正确写法：序列化 dto（items + 三个整数）。

Console.WriteLine();
Console.WriteLine("Sample.SerializationEntry completed.");

// ---------------------------------------------------------------------------
// 传输元素类型（顶层语句之后声明）
// ---------------------------------------------------------------------------

/// <summary>订单元素（演示用的最小 POCO）。</summary>
internal sealed record Order(int Id, decimal Amount);
