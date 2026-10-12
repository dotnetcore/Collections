// Sample.EnumerablePageMaterialize — EnumerablePage 单次物化（6.0.0）
// Sample.EnumerablePageMaterialize — EnumerablePage single materialization (6.0.0)
// 运行：dotnet run --project sample/Sample.EnumerablePageMaterialize
// Run: dotnet run --project sample/Sample.EnumerablePageMaterialize
//
// 演示重点：
// Highlights:
//   1. 6.0.0 修复：非 IList 的 IEnumerable 深页从逐成员 ElementAt(k+i)（近似 O(skip²)）改为单次 Skip/Take 物化。
//   1. 6.0.0 fix: for a non-IList IEnumerable, a deep page changed from per-member ElementAt(k+i) (approximately O(skip²)) to a single Skip/Take materialization.
//   2. GetPage 侧：ICollection<T> 走 O(1) Count + Skip/Take；惰性非 ICollection 单次遍历同时取页与总长。
//   2. GetPage side: an ICollection<T> goes through O(1) Count + Skip/Take; a lazy non-ICollection does a single pass fetching both the page and the total length.
//   3. 物化意味着「页是快照」：建成后源变化不影响页。
//   3. Materialization means "the page is a snapshot": after it is built, changes to the source do not affect the page.
//   4. sourceIsFull 分支：true 会按页号 Skip + Take（全源）；false 只 Take（片段），与 6.1.0 配合防二次切片。
//   4. sourceIsFull branch: true slices by page number with Skip + Take (full source); false only Takes (fragment), cooperating with 6.1.0 to prevent double slicing.
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

// ---------------------------------------------------------------------------
// 1. 惰性序列深页：单次 Skip/Take 物化
// 1. Lazy-sequence deep page: single Skip/Take materialization
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 惰性序列深页 ===");
Console.WriteLine("=== 1. Lazy-sequence deep page ===");

// 惰性 yield 序列（非 IList）：深页不再对每个成员 ElementAt，而是一次物化当前页。
// A lazy yield sequence (not IList): a deep page no longer calls ElementAt per member but materializes the current page once.
IEnumerable<Order> LazyOrders()
{
    for (int i = 1; i <= 100_000; i++)
    {
        yield return new Order { Id = i, CreatedAt = baseTime.AddSeconds(i), Name = $"order-{i}", Amount = i };
    }
}

IPage<Order> deep = LazyOrders().GetPage(2000, 20); // skip = (2000-1)*20 = 39980
Console.WriteLine($"CurrentPageSize  = {deep.CurrentPageSize}");   // 20
Console.WriteLine($"TotalMemberCount = {deep.TotalMemberCount}"); // 100000
Console.WriteLine($"首成员 Id / First member Id        = {deep[0].Value.Id}");      // 39981
Console.WriteLine($"首成员 ItemNumber / First member ItemNumber= {deep[0].ItemNumber}");    // 39981（全局编号）
                                                                                        // 39981 (global numbering)

// ---------------------------------------------------------------------------
// 2. List<T> 快路径（ICollection）
// 2. List<T> fast path (ICollection)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. List<T> 快路径 ===");
Console.WriteLine("=== 2. List<T> fast path ===");

// ICollection<T> 源走 O(1) Count + Skip/Take，不做单次遍历统计。
// An ICollection<T> source goes through O(1) Count + Skip/Take and does not do a single-pass count.
var list = Enumerable.Range(1, 100)
    .Select(i => new Order { Id = i, Name = $"order-{i:00}", Amount = i })
    .ToList();
IPage<Order> fromList = list.GetPage(3, 10); // skip = 20
Console.WriteLine($"TotalMemberCount = {fromList.TotalMemberCount}"); // 100
Console.WriteLine($"首成员 Id / First member Id        = {fromList[0].Value.Id}");      // 21

// ---------------------------------------------------------------------------
// 3. 物化快照：页建成后源变化不影响页
// 3. Materialized snapshot: after the page is built, changes to the source do not affect the page
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 物化快照 ===");
Console.WriteLine("=== 3. Materialized snapshot ===");

var mutable = new List<Order>
{
    new Order { Id = 1 }, new Order { Id = 2 }, new Order { Id = 3 },
};
IPage<Order> snapshot = mutable.GetPage(1, 3);
Console.WriteLine($"取页后 CurrentPageSize / After fetching CurrentPageSize   = {snapshot.CurrentPageSize}");   // 3
Console.WriteLine($"取页后 TotalMemberCount / After fetching TotalMemberCount  = {snapshot.TotalMemberCount}"); // 3

mutable.Add(new Order { Id = 4 }); // 源事后被改
                                   // the source is changed afterwards
Console.WriteLine($"源新增后 CurrentPageSize 仍 / After adding to source, CurrentPageSize still = {snapshot.CurrentPageSize}");   // 3（物化，不跟着变）
                                                                                                                                // 3 (materialized, does not follow)
Console.WriteLine($"源新增后 TotalMemberCount 仍 / After adding to source, TotalMemberCount still = {snapshot.TotalMemberCount}"); // 3
// ❌ 错误写法：拿到页后改源集合期望页跟着变；✅ 要新数据重新取页。
// ❌ Wrong usage: mutating the source collection after obtaining the page and expecting the page to follow; ✅ for new data, fetch the page again.

// ---------------------------------------------------------------------------
// 4. sourceIsFull 分支：true 全源 Skip，false 片段只 Take
// 4. sourceIsFull branch: true slices the full source with Skip, false takes the fragment only
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. sourceIsFull 分支 ===");
Console.WriteLine("=== 4. sourceIsFull branch ===");

var seq = Enumerable.Range(1, 10)
    .Select(i => new Order { Id = i, Name = $"order-{i:00}", Amount = i })
    .ToList();

// sourceIsFull: true（默认）—— 入参是全源：按页号 Skip + Take。
// sourceIsFull: true (default) — the input is the full source: Skip + Take by page number.
IPage<Order> fullPage = new EnumerablePage<Order>(seq, currentPageNumber: 2, pageSize: 3, totalMemberCount: 10, sourceIsFull: true);
Console.WriteLine($"sourceIsFull=true  第 2 页 Id / page 2 Id: {string.Join(",", fullPage.ToOriginalItems().Select(o => o.Id))}"); // 4,5,6

// sourceIsFull: false —— 入参已是片段（CreatePage/GetPage 切好的页内容）：只 Take，不再偏移。
// sourceIsFull: false — the input is already a fragment (the page content sliced by CreatePage/GetPage): only Take, no further offset.
IPage<Order> fragmentPage = new EnumerablePage<Order>(seq, currentPageNumber: 2, pageSize: 3, totalMemberCount: 10, sourceIsFull: false);
Console.WriteLine($"sourceIsFull=false 第 2 页 Id / page 2 Id: {string.Join(",", fragmentPage.ToOriginalItems().Select(o => o.Id))}"); // 1,2,3

Console.WriteLine();
Console.WriteLine("Sample.EnumerablePageMaterialize completed.");

// 每个 sample 自带的小型域类型，不跨 sample 共享。
// Each sample carries its own small domain type, not shared across samples.
internal sealed class Order
{
    public int Id;
    public DateTimeOffset CreatedAt;
    public string Name = string.Empty;
    public decimal Amount;
}
