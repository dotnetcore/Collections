// Sample.AsyncFragmentEntry — 异步片段入口（F35）
// Sample.AsyncFragmentEntry — Async fragment entry (F35)
// 运行：dotnet run --project sample/Sample.AsyncFragmentEntry
// Run: dotnet run --project sample/Sample.AsyncFragmentEntry
//
// 演示重点：
// Highlights:
//   1. Paginable.CreatePageAsync 是片段建页（F32）的 async 形状对偶，四个重载与 CreatePage 一一对应。
//   1. Paginable.CreatePageAsync is the async-shape dual of fragment page building (F32); its four overloads correspond one-to-one with CreatePage.
//   2. 它「同步完成」：片段已在内存，没有可 await 的 I/O；返回的 Task 已经完成（Task.FromResult）。
//   2. It "completes synchronously": the fragment is already in memory, there is no I/O to await; the returned Task is already completed (Task.FromResult).
//   3. 参数校验从「调用本身」同步抛出，不是 faulted task；cancellationToken 仅为签名对称，不被观察。
//   3. Argument validation throws synchronously from "the call itself", not as a faulted task; cancellationToken is only for signature symmetry and is not observed.
//   4. 与真异步（EF Core / FreeSql / SqlSugar 的 GetPageAsync）同一签名，异步链里混片段路径不必改签名。
//   4. Same signature as true async (EF Core / FreeSql / SqlSugar GetPageAsync); mixing a fragment path into an async chain requires no signature change.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

// 上游已返回第 3 页的片段 + 全源总数 12。
// The upstream has already returned page 3's fragment + the full-source total 12.
List<Order> fragment = Enumerable.Range(11, 2)
    .Select(i => new Order { Id = i, CreatedAt = baseTime.AddMinutes(-i), Name = $"order-{i:00}", Amount = i })
    .ToList();

// ---------------------------------------------------------------------------
// 1. 与真异步同一签名：把片段路径藏进 Task<IPage<T>> 服务方法
// 1. Same signature as true async: hide the fragment path inside a Task<IPage<T>> service method
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 与真异步同一签名 ===");
Console.WriteLine("=== 1. Same signature as true async ===");

// FragmentPageSource 内部即 Task.FromResult(CreatePage(...))：返回类型与 EF Core 实现同形，调用方无感。
// FragmentPageSource is internally Task.FromResult(CreatePage(...)): the return type is shaped like the EF Core implementation, transparent to the caller.
IOrderPageSource source = new FragmentPageSource(fragment, total: 12);
IPage<Order> page = await source.GetPageAsync(pageNumber: 3, pageSize: 5, cancellationToken: CancellationToken.None);
Console.WriteLine($"CurrentPageNumber = {page.CurrentPageNumber}"); // 3
Console.WriteLine($"CurrentPageSize   = {page.CurrentPageSize}");   // 2
Console.WriteLine($"首个 ItemNumber / first ItemNumber    = {page[0].ItemNumber}");     // 11

// ---------------------------------------------------------------------------
// 2. 严格校验重载（含 options 变体）
// 2. Strict validation overload (including the options variant)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 严格校验重载 ===");
Console.WriteLine("=== 2. Strict validation overload ===");

// 重载集合 = CreatePage + CancellationToken，含三元组 / PageFragmentInfo / options 变体。
// The overload set = CreatePage + CancellationToken, including the triple / PageFragmentInfo / options variants.
Task<IPage<Order>> strictTask = Paginable.CreatePageAsync(
    fragment, pageNumber: 3, pageSize: 5, totalMemberCount: 12, options: PageCreationOptions.Strict);

// Task.FromResult 的两个后果之一：返回的 Task 已经完成。
// One of the two consequences of Task.FromResult: the returned Task is already completed.
Console.WriteLine($"返回的 Task 是否已完成 / Is the returned Task already completed = {strictTask.IsCompleted}"); // True

IPage<Order> strictPage = await strictTask;
Console.WriteLine($"Strict 页 CurrentPageSize = {strictPage.CurrentPageSize}"); // 2（片段与元数据吻合）
                                                                                // 2 (the fragment matches the metadata)

// ---------------------------------------------------------------------------
// 3. 误解取消语义：已取消令牌不阻止成功
// 3. Misunderstanding cancellation: a cancelled token does not prevent success
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 取消语义 ===");
Console.WriteLine("=== 3. Cancellation semantics ===");

// ❌ 错误写法：期待已取消令牌让 CreatePageAsync 失败——它不观察令牌，仍成功返回。
// ❌ Wrong usage: expecting a cancelled token to make CreatePageAsync fail — it does not observe the token and still returns successfully.
using var cts = new CancellationTokenSource();
cts.Cancel();
IPage<Order> stillOk = await Paginable.CreatePageAsync(fragment, 3, 5, 12, cts.Token);
Console.WriteLine($"已取消令牌下仍成功 / still succeeds with a cancelled token: CurrentPageSize = {stillOk.CurrentPageSize}"); // 2
// ✅ 真需要取消用提供方异步 API（EF Core GetPageAsync 等，见 F31 / F44）。
// ✅ For real cancellation use the provider async APIs (e.g. EF Core GetPageAsync, see F31 / F44).

// ---------------------------------------------------------------------------
// 4. 参数校验从调用本身同步抛出
// 4. Argument validation throws synchronously from the call itself
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 校验同步抛出 ===");
Console.WriteLine("=== 4. Validation throws synchronously ===");

try
{
    // pageNumber=9 越过末页：异常在调用处同步抛出，不要写 catch (AggregateException) 去包。
    // pageNumber=9 goes past the last page: the exception is thrown synchronously at the call site; do not wrap it with catch (AggregateException).
    await Paginable.CreatePageAsync(fragment, pageNumber: 9, pageSize: 5, totalMemberCount: 12);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"越界元数据被拒 / out-of-range metadata rejected: {ex.ParamName}"); // pageNumber
}

Console.WriteLine();
Console.WriteLine("Sample.AsyncFragmentEntry completed.");

// 每个 sample 自带的小型域类型，不跨 sample 共享。
// Each sample carries its own small domain type, not shared across samples.
internal sealed class Order
{
    public int Id;
    public DateTimeOffset CreatedAt;
    public string Name = string.Empty;
    public decimal Amount;
}

// 与 EF Core / 真异步实现同形的服务契约：返回类型都是 Task<IPage<T>>。
// The service contract shaped like the EF Core / true-async implementation: both return Task<IPage<T>>.
internal interface IOrderPageSource
{
    Task<IPage<Order>> GetPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
}

// 片段实现：内部就是 CreatePageAsync（同步完成），令牌被忽略。
// The fragment implementation: internally it is CreatePageAsync (completing synchronously); the token is ignored.
internal sealed class FragmentPageSource : IOrderPageSource
{
    private readonly List<Order> _fragment;
    private readonly int _total;

    public FragmentPageSource(List<Order> fragment, int total)
    {
        _fragment = fragment;
        _total = total;
    }

    public Task<IPage<Order>> GetPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
        => Paginable.CreatePageAsync(_fragment, pageNumber, pageSize, _total, cancellationToken);
}
