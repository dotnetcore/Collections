// Sample.CancellationTokenForwarding — 取消令牌的转发 / 边界 / 不观察（F44）
// 运行：dotnet run --project sample/Sample.CancellationTokenForwarding
//
// 演示重点：
//   1. 核心内存路径（CreatePageAsync / 核心 IEnumerable Async）**不观察**令牌，仅签名对称。
//   2. 三类语义：下传（EF Core / FreeSql 真异步）、库边界（SqlSugar 6.6）、不观察（片段 / 内存）。
//   3. 已取消的令牌传给 CreatePageAsync，任务仍成功完成（RanToCompletion）。
//   4. 想在内存里获得可取消语义，需自己在循环里 ThrowIfCancellationRequested。
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var orders = new List<Order>
{
    new(1, "A"), new(2, "B"), new(3, "C"),
};

// 一个「已取消」的令牌：如果某路径观察它，调用应抛 OperationCanceledException。
using var cts = new CancellationTokenSource();
cts.Cancel();

// ---------------------------------------------------------------------------
// 1. 片段入口 CreatePageAsync 不观察令牌
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. CreatePageAsync 不观察令牌 ===");
var info = new PageFragmentInfo(pageNumber: 1, pageSize: 2, totalMemberCount: 3);
var page = await Paginable.CreatePageAsync(new[] { orders[0], orders[1] }, info, cts.Token);
Console.WriteLine($"令牌已取消 = {cts.IsCancellationRequested}");                     // True
Console.WriteLine($"仍成功取页 = {page.CurrentPageSize} 条（PageSize={page.PageSize}）"); // 2

// ❌ 错误写法：对内存 CreatePageAsync 期待取消 —— 它永不观察令牌，会一直成功：
// try { await Paginable.CreatePageAsync(frag, info, cts.Token); }
// catch (OperationCanceledException) { /* 永远不会进这里 */ }

// ---------------------------------------------------------------------------
// 2. 核心 IEnumerable 异步形状同样不观察令牌
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 核心 IEnumerable Async 不观察令牌 ===");
var asyncPage = await orders.GetPageAsync(1, 2, cts.Token);
Console.WriteLine($"GetPageAsync(已取消令牌) → {asyncPage.CurrentPageSize} 条");        // 2

var asyncSet = await orders.ToPaginableAsync(2, cancellationToken: cts.Token);
Console.WriteLine($"ToPaginableAsync(已取消令牌) → PageCount={asyncSet.PageCount}");   // 2

// 断言任务确实成功完成（不是取消，也不是故障）。
var t = Paginable.CreatePageAsync(new[] { orders[0] }, info, cts.Token);
Console.WriteLine($"任务状态 = {t.Status}");   // RanToCompletion

// ---------------------------------------------------------------------------
// 3. 三类语义对照（注释说明；核心包内只做「不观察」这一类）
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 三类语义对照 ===");
Console.WriteLine("下传   ：EF Core / FreeSql 的 GetPageAsync 把令牌交给 CountAsync/ToListAsync（真异步，可中止 I/O）");
Console.WriteLine("库边界 ：SqlSugar 6.6 起在自有边界尊重已取消令牌（首轮往返前抛 OperationCanceledException）");
Console.WriteLine("不观察 ：片段 / 内存 Async —— 无 I/O，仅签名对称（本示例演示的就是这一类）");
Console.WriteLine("同步 GetPage 没有令牌参数；越界是 ArgumentOutOfRangeException（F40），与取消异常无关。");

// ⚠️ 异常类型提醒：取消 = OperationCanceledException（含 TaskCanceledException 子类）；
//    ❌ 错误写法：catch (Exception) 吞掉取消，导致请求变慢 / 挂死。
//    ✅ 正确：让 OperationCanceledException 传播给主机。

// ---------------------------------------------------------------------------
// 4. 想在内存里可取消：自己在循环里检查令牌
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 可移植取消：循环内 ThrowIfCancellationRequested ===");
using var cts2 = new CancellationTokenSource();
cts2.Cancel();
try
{
    ScanAll(orders, cts2.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("ScanAll 在循环内观察到令牌 → 抛 OperationCanceledException（这是你自己加的可取消语义）");
}

Console.WriteLine();
Console.WriteLine("Sample.CancellationTokenForwarding completed.");

// 内存路径想获得取消语义，必须自己观察令牌——库不会替你做。
static void ScanAll(IEnumerable<Order> source, CancellationToken cancellationToken)
{
    foreach (var order in source)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = order.Id;
    }
}

/// <summary>示例域模型：订单。</summary>
internal sealed record Order(int Id, string Name);
