// Sample.EnumerablePageMaterialize — EnumerablePage 单次物化（F39）
// 运行：dotnet run --project sample/Sample.EnumerablePageMaterialize
//
// 演示重点：
//   1. 6.0.0 修复：非 IList 的 IEnumerable 深页从逐成员 ElementAt(k+i)（近似 O(skip²)）改为单次 Skip/Take 物化。
//   2. GetPage 侧：ICollection<T> 走 O(1) Count + Skip/Take；惰性非 ICollection 单次遍历同时取页与总长。
//   3. 物化意味着「页是快照」：建成后源变化不影响页。
//   4. sourceIsFull 分支：true 会按页号 Skip + Take（全源）；false 只 Take（片段），与 F32 配合防二次切片。
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

// ---------------------------------------------------------------------------
// 1. 惰性序列深页：单次 Skip/Take 物化
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 惰性序列深页 ===");

// 惰性 yield 序列（非 IList）：深页不再对每个成员 ElementAt，而是一次物化当前页。
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
Console.WriteLine($"首成员 Id        = {deep[0].Value.Id}");      // 39981
Console.WriteLine($"首成员 ItemNumber= {deep[0].ItemNumber}");    // 39981（全局编号）

// ---------------------------------------------------------------------------
// 2. List<T> 快路径（ICollection）
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. List<T> 快路径 ===");

// ICollection<T> 源走 O(1) Count + Skip/Take，不做单次遍历统计。
var list = Enumerable.Range(1, 100)
    .Select(i => new Order { Id = i, Name = $"order-{i:00}", Amount = i })
    .ToList();
IPage<Order> fromList = list.GetPage(3, 10); // skip = 20
Console.WriteLine($"TotalMemberCount = {fromList.TotalMemberCount}"); // 100
Console.WriteLine($"首成员 Id        = {fromList[0].Value.Id}");      // 21

// ---------------------------------------------------------------------------
// 3. 物化快照：页建成后源变化不影响页
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 物化快照 ===");

var mutable = new List<Order>
{
    new Order { Id = 1 }, new Order { Id = 2 }, new Order { Id = 3 },
};
IPage<Order> snapshot = mutable.GetPage(1, 3);
Console.WriteLine($"取页后 CurrentPageSize   = {snapshot.CurrentPageSize}");   // 3
Console.WriteLine($"取页后 TotalMemberCount  = {snapshot.TotalMemberCount}"); // 3

mutable.Add(new Order { Id = 4 }); // 源事后被改
Console.WriteLine($"源新增后 CurrentPageSize  仍 = {snapshot.CurrentPageSize}");   // 3（物化，不跟着变）
Console.WriteLine($"源新增后 TotalMemberCount 仍 = {snapshot.TotalMemberCount}"); // 3
// ❌ 错误写法：拿到页后改源集合期望页跟着变；✅ 要新数据重新取页。

// ---------------------------------------------------------------------------
// 4. sourceIsFull 分支：true 全源 Skip，false 片段只 Take
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. sourceIsFull 分支 ===");

var seq = Enumerable.Range(1, 10)
    .Select(i => new Order { Id = i, Name = $"order-{i:00}", Amount = i })
    .ToList();

// sourceIsFull: true（默认）—— 入参是全源：按页号 Skip + Take。
IPage<Order> fullPage = new EnumerablePage<Order>(seq, currentPageNumber: 2, pageSize: 3, totalMemberCount: 10, sourceIsFull: true);
Console.WriteLine($"sourceIsFull=true  第 2 页 Id: {string.Join(",", fullPage.ToOriginalItems().Select(o => o.Id))}"); // 4,5,6

// sourceIsFull: false —— 入参已是片段（CreatePage/GetPage 切好的页内容）：只 Take，不再偏移。
IPage<Order> fragmentPage = new EnumerablePage<Order>(seq, currentPageNumber: 2, pageSize: 3, totalMemberCount: 10, sourceIsFull: false);
Console.WriteLine($"sourceIsFull=false 第 2 页 Id: {string.Join(",", fragmentPage.ToOriginalItems().Select(o => o.Id))}"); // 1,2,3

Console.WriteLine();
Console.WriteLine("Sample.EnumerablePageMaterialize completed.");

// 每个 sample 自带的小型域类型，不跨 sample 共享。
internal sealed class Order
{
    public int Id;
    public DateTimeOffset CreatedAt;
    public string Name = string.Empty;
    public decimal Amount;
}
