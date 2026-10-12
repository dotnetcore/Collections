// Sample.CancellationTokenForwarding — 取消令牌的转发 / 边界 / 不观察（6.0.0）
// Sample.CancellationTokenForwarding — cancellation token forwarding / boundary / unobserved (6.0.0)
// 运行：dotnet run --project sample/Sample.CancellationTokenForwarding
// Run: dotnet run --project sample/Sample.CancellationTokenForwarding
//
// 演示重点：
// Highlights:
//   1. 核心内存路径（CreatePageAsync / 核心 IEnumerable Async）**不观察**令牌，仅签名对称。
//   1. The core in-memory path (CreatePageAsync / core IEnumerable Async) does **not observe** the token; it is only signature symmetry.
//   2. 三类语义：下传（EF Core / FreeSql 真异步）、库边界（SqlSugar 6.6）、不观察（片段 / 内存）。
//   2. Three categories of semantics: forward (EF Core / FreeSql, real async), library boundary (SqlSugar 6.6), unobserved (fragment / in-memory).
//   3. 已取消的令牌传给 CreatePageAsync，任务仍成功完成（RanToCompletion）。
//   3. Passing an already-cancelled token to CreatePageAsync still completes the task successfully (RanToCompletion).
//   4. 想在内存里获得可取消语义，需自己在循环里 ThrowIfCancellationRequested。
//   4. To get cancellation semantics in memory, you must call ThrowIfCancellationRequested yourself in the loop.
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
// An "already-cancelled" token: if some path observed it, the call should throw OperationCanceledException.
using var cts = new CancellationTokenSource();
cts.Cancel();

// ---------------------------------------------------------------------------
// 1. 片段入口 CreatePageAsync 不观察令牌
// 1. The fragment entry CreatePageAsync does not observe the token
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. CreatePageAsync 不观察令牌 ===");
Console.WriteLine("=== 1. CreatePageAsync does not observe the token ===");
var info = new PageFragmentInfo(pageNumber: 1, pageSize: 2, totalMemberCount: 3);
var page = await Paginable.CreatePageAsync(new[] { orders[0], orders[1] }, info, cts.Token);
Console.WriteLine($"令牌已取消 / Token cancelled = {cts.IsCancellationRequested}");                     // True
Console.WriteLine($"仍成功取页 / Still fetched the page successfully = {page.CurrentPageSize} 条 / items（PageSize={page.PageSize}）"); // 2

// ❌ 错误写法：对内存 CreatePageAsync 期待取消 —— 它永不观察令牌，会一直成功：
// ❌ Wrong approach: expecting cancellation from the in-memory CreatePageAsync — it never observes the token and always succeeds:
// try { await Paginable.CreatePageAsync(frag, info, cts.Token); }
// catch (OperationCanceledException) { /* 永远不会进这里 */ }
// catch (OperationCanceledException) { /* this is never entered */ }

// ---------------------------------------------------------------------------
// 2. 核心 IEnumerable 异步形状同样不观察令牌
// 2. The core IEnumerable async shape likewise does not observe the token
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 核心 IEnumerable Async 不观察令牌 ===");
Console.WriteLine("=== 2. The core IEnumerable Async does not observe the token ===");
var asyncPage = await orders.GetPageAsync(1, 2, cts.Token);
Console.WriteLine($"GetPageAsync(已取消令牌 / cancelled token) → {asyncPage.CurrentPageSize} 条 / items");        // 2

var asyncSet = await orders.ToPaginableAsync(2, cancellationToken: cts.Token);
Console.WriteLine($"ToPaginableAsync(已取消令牌 / cancelled token) → PageCount={asyncSet.PageCount}");   // 2

// 断言任务确实成功完成（不是取消，也不是故障）。
// Assert that the task really completed successfully (not cancelled, not faulted).
var t = Paginable.CreatePageAsync(new[] { orders[0] }, info, cts.Token);
Console.WriteLine($"任务状态 / Task status = {t.Status}");   // RanToCompletion

// ---------------------------------------------------------------------------
// 3. 三类语义对照（注释说明；核心包内只做「不观察」这一类）
// 3. Contrast of the three semantics (described in comments; the core package only covers the "unobserved" category)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 三类语义对照 ===");
Console.WriteLine("=== 3. Contrast of the three semantics ===");
Console.WriteLine("下传   ：EF Core / FreeSql 的 GetPageAsync 把令牌交给 CountAsync/ToListAsync（真异步，可中止 I/O） / Forward: EF Core / FreeSql's GetPageAsync hands the token to CountAsync/ToListAsync (real async, can abort I/O)");
Console.WriteLine("库边界 ：SqlSugar 6.6 起在自有边界尊重已取消令牌（首轮往返前抛 OperationCanceledException） / Library boundary: since SqlSugar 6.6 an already-cancelled token is respected at its own boundary (throws OperationCanceledException before the first round trip)");
Console.WriteLine("不观察 ：片段 / 内存 Async —— 无 I/O，仅签名对称（本示例演示的就是这一类） / Unobserved: fragment / in-memory Async — no I/O, only signature symmetry (this is the category this sample demonstrates)");
Console.WriteLine("同步 GetPage 没有令牌参数；越界是 ArgumentOutOfRangeException（6.2.0），与取消异常无关。 / The synchronous GetPage has no token parameter; out-of-range is ArgumentOutOfRangeException (6.2.0), unrelated to cancellation exceptions.");

// ⚠️ 异常类型提醒：取消 = OperationCanceledException（含 TaskCanceledException 子类）；
// ⚠️ Exception type reminder: cancellation = OperationCanceledException (including the TaskCanceledException subclass);
//    ❌ 错误写法：catch (Exception) 吞掉取消，导致请求变慢 / 挂死。
//    ❌ Wrong approach: catch (Exception) swallowing the cancellation, making requests slow / hang.
//    ✅ 正确：让 OperationCanceledException 传播给主机。
//    ✅ Correct: let OperationCanceledException propagate to the host.

// ---------------------------------------------------------------------------
// 4. 想在内存里可取消：自己在循环里检查令牌
// 4. Wanting in-memory cancellation: check the token yourself in the loop
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 可移植取消：循环内 ThrowIfCancellationRequested ===");
Console.WriteLine("=== 4. Portable cancellation: ThrowIfCancellationRequested inside the loop ===");
using var cts2 = new CancellationTokenSource();
cts2.Cancel();
try
{
    ScanAll(orders, cts2.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("ScanAll 在循环内观察到令牌 → 抛 OperationCanceledException（这是你自己加的可取消语义） / ScanAll observes the token inside the loop -> throws OperationCanceledException (this is the cancellation semantics you added yourself)");
}

Console.WriteLine();
Console.WriteLine("Sample.CancellationTokenForwarding completed.");

// 内存路径想获得取消语义，必须自己观察令牌——库不会替你做。
// To get cancellation semantics on the in-memory path you must observe the token yourself — the library will not do it for you.
static void ScanAll(IEnumerable<Order> source, CancellationToken cancellationToken)
{
    foreach (var order in source)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = order.Id;
    }
}

/// <summary>示例域模型：订单。</summary>
/// <summary>Sample domain model: an order.</summary>
internal sealed record Order(int Id, string Name);
