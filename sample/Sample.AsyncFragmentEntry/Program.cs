// Sample.AsyncFragmentEntry — 异步片段入口（F35）
// 运行：dotnet run --project sample/Sample.AsyncFragmentEntry
//
// 演示重点：
//   1. Paginable.CreatePageAsync 是片段建页（F32）的 async 形状对偶，四个重载与 CreatePage 一一对应。
//   2. 它「同步完成」：片段已在内存，没有可 await 的 I/O；返回的 Task 已经完成（Task.FromResult）。
//   3. 参数校验从「调用本身」同步抛出，不是 faulted task；cancellationToken 仅为签名对称，不被观察。
//   4. 与真异步（EF Core / FreeSql / SqlSugar 的 GetPageAsync）同一签名，异步链里混片段路径不必改签名。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

// 上游已返回第 3 页的片段 + 全源总数 12。
List<Order> fragment = Enumerable.Range(11, 2)
    .Select(i => new Order { Id = i, CreatedAt = baseTime.AddMinutes(-i), Name = $"order-{i:00}", Amount = i })
    .ToList();

// ---------------------------------------------------------------------------
// 1. 与真异步同一签名：把片段路径藏进 Task<IPage<T>> 服务方法
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 与真异步同一签名 ===");

// FragmentPageSource 内部即 Task.FromResult(CreatePage(...))：返回类型与 EF Core 实现同形，调用方无感。
IOrderPageSource source = new FragmentPageSource(fragment, total: 12);
IPage<Order> page = await source.GetPageAsync(pageNumber: 3, pageSize: 5, cancellationToken: CancellationToken.None);
Console.WriteLine($"CurrentPageNumber = {page.CurrentPageNumber}"); // 3
Console.WriteLine($"CurrentPageSize   = {page.CurrentPageSize}");   // 2
Console.WriteLine($"首个 ItemNumber    = {page[0].ItemNumber}");     // 11

// ---------------------------------------------------------------------------
// 2. 严格校验重载（含 options 变体）
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 严格校验重载 ===");

// 重载集合 = CreatePage + CancellationToken，含三元组 / PageFragmentInfo / options 变体。
Task<IPage<Order>> strictTask = Paginable.CreatePageAsync(
    fragment, pageNumber: 3, pageSize: 5, totalMemberCount: 12, options: PageCreationOptions.Strict);

// Task.FromResult 的两个后果之一：返回的 Task 已经完成。
Console.WriteLine($"返回的 Task 是否已完成 = {strictTask.IsCompleted}"); // True

IPage<Order> strictPage = await strictTask;
Console.WriteLine($"Strict 页 CurrentPageSize = {strictPage.CurrentPageSize}"); // 2（片段与元数据吻合）

// ---------------------------------------------------------------------------
// 3. 误解取消语义：已取消令牌不阻止成功
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 取消语义 ===");

// ❌ 错误写法：期待已取消令牌让 CreatePageAsync 失败——它不观察令牌，仍成功返回。
using var cts = new CancellationTokenSource();
cts.Cancel();
IPage<Order> stillOk = await Paginable.CreatePageAsync(fragment, 3, 5, 12, cts.Token);
Console.WriteLine($"已取消令牌下仍成功：CurrentPageSize = {stillOk.CurrentPageSize}"); // 2
// ✅ 真需要取消用提供方异步 API（EF Core GetPageAsync 等，见 F31 / F44）。

// ---------------------------------------------------------------------------
// 4. 参数校验从调用本身同步抛出
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 校验同步抛出 ===");

try
{
    // pageNumber=9 越过末页：异常在调用处同步抛出，不要写 catch (AggregateException) 去包。
    await Paginable.CreatePageAsync(fragment, pageNumber: 9, pageSize: 5, totalMemberCount: 12);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"越界元数据被拒：{ex.ParamName}"); // pageNumber
}

Console.WriteLine();
Console.WriteLine("Sample.AsyncFragmentEntry completed.");

// 每个 sample 自带的小型域类型，不跨 sample 共享。
internal sealed class Order
{
    public int Id;
    public DateTimeOffset CreatedAt;
    public string Name = string.Empty;
    public decimal Amount;
}

// 与 EF Core / 真异步实现同形的服务契约：返回类型都是 Task<IPage<T>>。
internal interface IOrderPageSource
{
    Task<IPage<Order>> GetPageAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);
}

// 片段实现：内部就是 CreatePageAsync（同步完成），令牌被忽略。
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
