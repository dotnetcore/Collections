// Sample.SerializationEntry — Paginable 侧「无独立序列化 API」与正确重建路径（6.2.0，负结果）
// Sample.SerializationEntry — "no standalone serialization API" on the Paginable side and the correct rebuild path (6.2.0, negative result)
// 运行：dotnet run --project sample/Sample.SerializationEntry
// Run: dotnet run --project sample/Sample.SerializationEntry
//
// 演示重点：
// Highlights:
//   1. **负结果**：Paginable 核心与 9 个适配器**都没有** `ToSerializableModel` / `FromModel`——
//   1. **negative result**: the Paginable core and the 9 adapters **all lack** `ToSerializableModel` / `FromModel` —
//      那是 Multi 的 API（MultiListModel<T> / MultiDictionaryModel<TKey,TValue>，见 6.2.0）；
//      that is Multi's API (MultiListModel<T> / MultiDictionaryModel<TKey,TValue>, see 6.2.0);
//   2. 正确路径：序列化 `items` + 三元组元数据（CurrentPageNumber / PageSize / TotalMemberCount），
//   2. the correct path: serialize `items` + the triple metadata (CurrentPageNumber / PageSize / TotalMemberCount),
//      对端用 `Paginable.CreatePage` 重建 IPage<T>，全局编号与整源分页一致；
//      and have the other side rebuild IPage<T> with `Paginable.CreatePage`, where the global numbering matches whole-source pagination;
//   3. `PageMetadata` 是普通属性快照（get-only），可被任意序列化器处理；
//   3. `PageMetadata` is a plain property snapshot (get-only), processable by any serializer;
//      `PageFragmentInfo.FromMetadata` / `ToMetadata` 可往返；
//      `PageFragmentInfo.FromMetadata` / `ToMetadata` round-trips;
//   4. IPage / IPaginable 实现**不是**序列化契约——要传就投影成 DTO。
//   4. IPage / IPaginable implementations are **not** serialization contracts — to transfer, project into a DTO.
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 0. 先造一个「整源」页（模拟服务器端分页结果）
// 0. First build a "whole-source" page (simulating a server-side pagination result)
// ---------------------------------------------------------------------------
Console.WriteLine("=== 0. 服务端整源分页（源 25 条，取第 2 页）===");
Console.WriteLine("=== 0. Server-side whole-source pagination (25 rows in the source, take page 2) ===");

var source = Enumerable.Range(1, 25).Select(i => new Order(i, i * 10m)).ToArray();

// 从**整源**切第 2 页（每页 10 条）——这是 SolidPageExtensions 的核心入口（all releases）。
// Slice page 2 from the **whole source** (10 rows per page) — this is the core entry point of SolidPageExtensions (all releases).
IPage<Order> page = source.GetPage(pageNumber: 2, pageSize: 10);
Console.WriteLine($"CurrentPageNumber = {page.CurrentPageNumber}, PageSize = {page.PageSize}"); // 2, 10
Console.WriteLine($"TotalMemberCount = {page.TotalMemberCount}, CurrentPageSize = {page.CurrentPageSize}"); // 25, 10

// ---------------------------------------------------------------------------
// 1. 负结果：不存在 Paginable.ToSerializableModel
// 1. Negative result: Paginable.ToSerializableModel does not exist
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 1. 负结果：Paginable 侧没有 ToSerializableModel ===");
Console.WriteLine("=== 1. Negative result: there is no ToSerializableModel on the Paginable side ===");

// ❌ 错误写法：从旧教程抄到 Paginable 上——这些成员在 Paginable 核心与 9 适配器里**都不存在**。
// ❌ Wrong approach: copying from old tutorials onto Paginable — these members **do not exist** in the Paginable core or the 9 adapters.
//    var model = page.ToSerializableModel();          // 不存在（那是 Multi 的 API，6.2.0）/ does not exist (that is Multi's API, 6.2.0)
//    var page2 = Paginable.FromModel(model);           // 不存在 / does not exist
// ✅ 正确写法：投影成 DTO（items + 三元组元数据），对端用 Paginable.CreatePage 重建。
// ✅ Correct approach: project into a DTO (items + the triple metadata) and rebuild with Paginable.CreatePage on the other side.
Console.WriteLine("Paginable 侧无 ToSerializableModel / FromModel（对比 Multi 的 MultiListModel / MultiDictionaryModel）。 / There is no ToSerializableModel / FromModel on the Paginable side (contrast with Multi's MultiListModel / MultiDictionaryModel).");

// ---------------------------------------------------------------------------
// 2. 正确路径 A：PageMetadata 快照（可被任意序列化器处理）
// 2. Correct path A: the PageMetadata snapshot (processable by any serializer)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. PageMetadata 属性快照 ===");
Console.WriteLine("=== 2. PageMetadata property snapshot ===");

// GetMetadata() 返回 PageMetadata：普通 get-only 属性快照，序列化器可直接处理。
// GetMetadata() returns a PageMetadata: a plain get-only property snapshot that a serializer can process directly.
PageMetadata meta = page.GetMetadata();
Console.WriteLine($"TotalPageCount={meta.TotalPageCount} RealPageCount={meta.RealPageCount} " +
                  $"TotalMemberCount={meta.TotalMemberCount} PageSize={meta.PageSize} " +
                  $"CurrentPageNumber={meta.CurrentPageNumber} CurrentPageSize={meta.CurrentPageSize} " +
                  $"HasPrevious={meta.HasPrevious} HasNext={meta.HasNext}");

// ---------------------------------------------------------------------------
// 3. 正确路径 B：投影 DTO → 「传输」→ 对端重建
// 3. Correct path B: project DTO → "transfer" → rebuild on the other side
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 投影 DTO → 重建 IPage<T> ===");
Console.WriteLine("=== 3. Project DTO → rebuild IPage<T> ===");

// 传输载荷：items + 三元组元数据。这里用匿名类型模拟 DTO；真实场景可换成可序列化 POCO。
// Transfer payload: items + the triple metadata. Here an anonymous type simulates the DTO; in a real scenario it can be a serializable POCO.
var dto = new
{
    meta.CurrentPageNumber,   // 2
    meta.PageSize,            // 10
    meta.TotalMemberCount,    // 25
    Items = page.ToOriginalItems().ToArray(),   // 第 2 页的 10 条原始元素
                                                // the 10 original elements of page 2
};
Console.WriteLine($"DTO：页码={dto.CurrentPageNumber} 页大小={dto.PageSize} 总数={dto.TotalMemberCount} items={dto.Items.Length} / DTO: pageNumber={dto.CurrentPageNumber} pageSize={dto.PageSize} total={dto.TotalMemberCount} items={dto.Items.Length}");

// 对端重建：CreatePage 认为 fragment 就是该页的确切内容（**不再切片**）。
// Rebuild on the other side: CreatePage treats the fragment as the exact contents of that page (**no re-slicing**).
IPage<Order> rebuilt = Paginable.CreatePage(
    dto.Items, dto.CurrentPageNumber, dto.PageSize, dto.TotalMemberCount);

Console.WriteLine($"重建页 CurrentPageSize / Rebuilt page CurrentPageSize = {rebuilt.CurrentPageSize}");           // 10
Console.WriteLine($"重建页 TotalPageCount  / Rebuilt page TotalPageCount  = {rebuilt.TotalPageCount}");            // 3
Console.WriteLine($"重建页首元素全局编号   / Rebuilt page first element global number   = {rebuilt.First().ItemNumber}");         // 11（= (2-1)*10+1）
                                                                                                                                   // 11 (= (2-1)*10+1)

// 一致性断言：重建页与原页的**全局编号**完全一致（证明 CreatePage 复刻了整源分页的编号语义）。
// Consistency assertion: the **global numbering** of the rebuilt page and the original page is identical (proving CreatePage replicated the numbering semantics of whole-source pagination).
var originalNumbers = page.Select(m => m.ItemNumber).ToArray();
var rebuiltNumbers = rebuilt.Select(m => m.ItemNumber).ToArray();
Console.WriteLine($"原页编号   / Original page numbers   = [{string.Join(",", originalNumbers)}]");
Console.WriteLine($"重建页编号 / Rebuilt page numbers    = [{string.Join(",", rebuiltNumbers)}]");
Console.WriteLine($"编号一致   / Numbers identical        = {originalNumbers.SequenceEqual(rebuiltNumbers)}"); // True

// ---------------------------------------------------------------------------
// 4. PageFragmentInfo 的元数据往返
// 4. The metadata round trip of PageFragmentInfo
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. PageFragmentInfo.FromMetadata / ToMetadata 往返 ===");
Console.WriteLine("=== 4. PageFragmentInfo.FromMetadata / ToMetadata round trip ===");

PageFragmentInfo info = PageFragmentInfo.FromMetadata(meta);   // PageMetadata → PageFragmentInfo
Console.WriteLine($"info.PageNumber={info.PageNumber} PageSize={info.PageSize} TotalMemberCount={info.TotalMemberCount}");
PageMetadata roundTrip = info.ToMetadata();                    // 再回到 PageMetadata
                                                               // back to PageMetadata
Console.WriteLine($"往返一致 / Round trip consistent = {roundTrip.CurrentPageNumber == meta.CurrentPageNumber && roundTrip.TotalMemberCount == meta.TotalMemberCount}"); // True

// ---------------------------------------------------------------------------
// 5. 为什么不能直接序列化页对象
// 5. Why the page object cannot be serialized directly
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 5. 为什么不直接序列化页对象 ===");
Console.WriteLine("=== 5. Why not serialize the page object directly ===");
Console.WriteLine("IPage / IPaginable 实现不是为序列化设计的 POCO（含内部状态与惰性缓存）； / IPage / IPaginable implementations are not POCOs designed for serialization (they contain internal state and lazy caches);");
Console.WriteLine("跨进程传一页 → 投影 items + 三个整数 → 对端 CreatePage；序列化导航用 PageMetadata 公共属性。 / to transfer a page across processes → project items + three integers → CreatePage on the other side; to serialize navigation, use the public properties of PageMetadata.");

// ❌ 错误写法：直接 JsonSerializer.Serialize(page)——页对象不是序列化契约。
// ❌ Wrong approach: directly JsonSerializer.Serialize(page) — the page object is not a serialization contract.
// ✅ 正确写法：序列化 dto（items + 三个整数）。
// ✅ Correct approach: serialize dto (items + three integers).

Console.WriteLine();
Console.WriteLine("Sample.SerializationEntry completed.");

// ---------------------------------------------------------------------------
// 传输元素类型（顶层语句之后声明）
// Transfer element type (declared after the top-level statements)
// ---------------------------------------------------------------------------

/// <summary>订单元素（演示用的最小 POCO）。</summary>
/// <summary>Order element (a minimal POCO for the demo).</summary>
internal sealed record Order(int Id, decimal Amount);
