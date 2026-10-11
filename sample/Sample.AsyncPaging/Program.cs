// Sample.AsyncPaging — 端到端异步分页（内存形态）（F31）
// Sample.AsyncPaging — End-to-end async paging (in-memory form) (F31)
// 运行：dotnet run --project sample/Sample.AsyncPaging
// Run: dotnet run --project sample/Sample.AsyncPaging
//
// 演示重点：
// Highlights:
//   1. 内存源的 *Async 是「异步形状」：由 Task.FromResult 包装，同步完成，无线程让渡、无 I/O。
//   1. The *Async on an in-memory source is an "async shape": wrapped by Task.FromResult, completes synchronously, no thread yield, no I/O.
//   2. 扩展方法来自 DotNetCore.Collections.Paginable.SolidPageExtensions
//   2. The extension methods come from DotNetCore.Collections.Paginable.SolidPageExtensions
//      （该类型名在 10 个包中同名，故写全限定名以避免歧义）。
//      (that type name is identical across 10 packages, so the fully qualified name is written to avoid ambiguity).
//   3. Paginable.CreatePageAsync 是片段建页（F32/F35）的 async 对偶：校验同步抛出、CancellationToken 不被观察。
//   3. Paginable.CreatePageAsync is the async dual of fragment page building (F32/F35): validation throws synchronously and the CancellationToken is not observed.
//   4. 真 I/O 异步要用提供方扩展（EF Core / FreeSql / SqlSugar）；本示例只用内存源，不引入任何第三方包。
//   4. True I/O async requires provider extensions (EF Core / FreeSql / SqlSugar); this sample uses an in-memory source only and pulls in no third-party package.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
var orders = Enumerable.Range(1, 12)
    .Select(i => new Order { Id = i, CreatedAt = baseTime.AddMinutes(-i), Name = $"order-{i:00}", Amount = i })
    .ToList();

// ---------------------------------------------------------------------------
// 1. 内存源异步形状：ToPaginableAsync / GetPageAsync（同步完成）
// 1. In-memory source async shape: ToPaginableAsync / GetPageAsync (completes synchronously)
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 内存源异步形状 ===");
Console.WriteLine("=== 1. In-memory source async shape ===");

// DotNetCore.Collections.Paginable.SolidPageExtensions.ToPaginableAsync：
// DotNetCore.Collections.Paginable.SolidPageExtensions.ToPaginableAsync:
// 返回的 Task 已经完成（Task.FromResult），await 只是语法统一，不会释放线程去等 I/O。
// The returned Task is already completed (Task.FromResult); await is only for syntactic uniformity and does not release the thread to wait for I/O.
PaginableEnumerable<Order> set = await orders.ToPaginableAsync(pageSize: 5);
Console.WriteLine($"set.PageSize   = {set.PageSize}");    // 5
Console.WriteLine($"set.MemberCount = {set.MemberCount}"); // 12
Console.WriteLine($"set.PageCount   = {set.PageCount}");   // 3

// 页对象本身仍是同步可枚举的 IPage<T>：异步只发生在「取数」这一步，不在枚举成员。
// The page object itself is still the synchronously enumerable IPage<T>: the async only happens at the "fetch" step, not when enumerating members.
IPage<Order> page = await orders.GetPageAsync(pageNumber: 3, pageSize: 5);
Console.WriteLine($"第 3 页 TotalPageCount / Page 3 TotalPageCount  = {page.TotalPageCount}");  // 3
Console.WriteLine($"第 3 页 CurrentPageSize / Page 3 CurrentPageSize = {page.CurrentPageSize}"); // 2（末页）
                                                                                                // 2 (last page)
Console.WriteLine($"第 3 页首成员 ItemNumber / Page 3 first member ItemNumber = {page[0].ItemNumber}");   // 11（全局编号）
                                                                                                        // 11 (global numbering)

// ---------------------------------------------------------------------------
// 2. 片段异步入口 Paginable.CreatePageAsync
// 2. Fragment async entry Paginable.CreatePageAsync
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 片段异步入口 CreatePageAsync ===");
Console.WriteLine("=== 2. Fragment async entry CreatePageAsync ===");

// 上游已返回第 3 页的片段 + 全源总数 12（缓存页 / gRPC items+totalCount 的典型形态）。
// The upstream has already returned page 3's fragment + the full-source total 12 (the typical shape of a cached page / gRPC items+totalCount).
List<Order> fragment = orders.Skip(10).Take(2).ToList();

// CreatePageAsync 同步完成：参数校验从「调用本身」抛出，而不是 faulted task。
// CreatePageAsync completes synchronously: argument validation throws from "the call itself", not as a faulted task.
IPage<Order> rebuilt = await Paginable.CreatePageAsync(fragment, pageNumber: 3, pageSize: 5, totalMemberCount: 12);
Console.WriteLine($"重建页 CurrentPageNumber / Rebuilt page CurrentPageNumber = {rebuilt.CurrentPageNumber}"); // 3
Console.WriteLine($"重建页 CurrentPageSize / Rebuilt page CurrentPageSize   = {rebuilt.CurrentPageSize}");   // 2
Console.WriteLine($"重建页首成员 ItemNumber / Rebuilt page first member ItemNumber   = {rebuilt[0].ItemNumber}");     // 11

// ---------------------------------------------------------------------------
// 3. 取消令牌不被观察（片段路径无 I/O 可取消）
// 3. Cancellation token is not observed (the fragment path has no I/O to cancel)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 取消令牌不被观察 ===");
Console.WriteLine("=== 3. Cancellation token is not observed ===");

// ❌ 错误写法：以为取消令牌能中断 CreatePageAsync —— 它不观察令牌，仍成功返回。
// ❌ Wrong usage: expecting a cancelled token to interrupt CreatePageAsync — it does not observe the token and still returns successfully.
//    （源码注释与 XML 都写明：片段已在内存，没有可 await 的 I/O。）
//    (both the source comments and XML state: the fragment is already in memory, there is no I/O to await.)
using var cts = new CancellationTokenSource();
cts.Cancel();
IPage<Order> stillOk = await Paginable.CreatePageAsync(fragment, 3, 5, 12, cts.Token);
Console.WriteLine($"已取消令牌下仍成功 / still succeeds with a cancelled token: CurrentPageSize = {stillOk.CurrentPageSize}"); // 2

// ---------------------------------------------------------------------------
// 4. 校验同步抛出（不是 AggregateException / faulted task 形态）
// 4. Validation throws synchronously (not an AggregateException / faulted-task shape)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 校验同步抛出 ===");
Console.WriteLine("=== 4. Validation throws synchronously ===");

try
{
    // pageNumber=9 越过末页（total=12, size=5 只有 3 页）：ArgumentOutOfRangeException 在调用处直接抛。
    // pageNumber=9 goes past the last page (total=12, size=5 yields only 3 pages): ArgumentOutOfRangeException is thrown directly at the call site.
    await Paginable.CreatePageAsync(fragment, pageNumber: 9, pageSize: 5, totalMemberCount: 12);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"越界元数据被拒 / out-of-range metadata rejected: {ex.ParamName}"); // pageNumber
}

Console.WriteLine();
Console.WriteLine("Sample.AsyncPaging completed.");

// 每个 sample 自带的小型域类型，不跨 sample 共享。
// Each sample carries its own small domain type, not shared across samples.
internal sealed class Order
{
    public int Id;
    public DateTimeOffset CreatedAt;
    public string Name = string.Empty;
    public decimal Amount;
}
