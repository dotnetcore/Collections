// Sample.PaginableSettings — 分页配置与管理器（6.0.0）
// Sample.PaginableSettings — Paging settings and manager (6.0.0)
// 运行：dotnet run --project sample/Sample.PaginableSettings
// Run: dotnet run --project sample/Sample.PaginableSettings
//
// 演示重点：
// Highlights:
//   1. 全局分页配置只有两个旋钮：DefaultPageSize（默认 50）与 MaxMemberItems（默认 10_000_000）。
//   1. The global paging configuration has only two knobs: DefaultPageSize (default 50) and MaxMemberItems (default 10_000_000).
//   2. 赋值即校验：非法值在 setter 抛 ArgumentOutOfRangeException，因此拿到手的实例永远是合法态。
//   2. Validation on assignment: an invalid value throws ArgumentOutOfRangeException in the setter, so any instance you obtain is always in a valid state.
//   3. PaginableSettingsManager.UpdateSettings 原子换入新快照（volatile 写）；null 抛 ArgumentNullException。
//   3. PaginableSettingsManager.UpdateSettings swaps in a new snapshot atomically (volatile write); null throws ArgumentNullException.
//   4. 单参 GetPage(n) 使用 DefaultPageSize；配置一次后应视为只读（本示例结束时还原默认）。
//   4. The single-argument GetPage(n) uses DefaultPageSize; after configuring once it should be treated as read-only (this sample restores the defaults at the end).
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

// ---------------------------------------------------------------------------
// 1. 默认值与配置（启动时一次定稿）
// 1. Default values and configuration (finalized once at startup)
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 默认值与配置 ===");
Console.WriteLine("=== 1. Default values and configuration ===");

// 静态构造创建默认实例：DefaultPageSize=50、MaxMemberItems=10_000_000。
// The static constructor creates the default instance: DefaultPageSize=50, MaxMemberItems=10_000_000.
Console.WriteLine($"默认 DefaultPageSize / Default DefaultPageSize = {PaginableSettingsManager.Settings.DefaultPageSize}"); // 50
Console.WriteLine($"默认 MaxMemberItems / Default MaxMemberItems   = {PaginableSettingsManager.Settings.MaxMemberItems}");   // 10000000

// 构造第二个快照并原子换入。
// Build a second snapshot and swap it in atomically.
PaginableSettingsManager.UpdateSettings(new PaginableSettings
{
    DefaultPageSize = 20,
    MaxMemberItems = 1_000_000,
});
Console.WriteLine($"更新后 DefaultPageSize / After update DefaultPageSize = {PaginableSettingsManager.Settings.DefaultPageSize}"); // 20
Console.WriteLine($"更新后 MaxMemberItems / After update MaxMemberItems   = {PaginableSettingsManager.Settings.MaxMemberItems}");   // 1000000

// 单参 GetPage(n) 使用 DefaultPageSize（未显式传 pageSize 的入口都读它）。
// The single-argument GetPage(n) uses DefaultPageSize (every entry that does not pass pageSize explicitly reads it).
var all = Enumerable.Range(1, 100)
    .Select(i => new Order { Id = i, CreatedAt = baseTime.AddMinutes(-i), Name = $"order-{i:00}", Amount = i })
    .ToList();
IPage<Order> page = all.GetPage(2); // 单参：走 DefaultPageSize=20
                                    // single argument: goes through DefaultPageSize=20
Console.WriteLine($"单参 GetPage(2) 的 PageSize / single-arg GetPage(2) PageSize   = {page.PageSize}");        // 20
Console.WriteLine($"单参 GetPage(2) 首 ItemNumber / single-arg GetPage(2) first ItemNumber = {page[0].ItemNumber}");  // 21

// ---------------------------------------------------------------------------
// 2. 赋值即校验
// 2. Validation on assignment
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 赋值即校验 ===");
Console.WriteLine("=== 2. Validation on assignment ===");

try
{
    _ = new PaginableSettings { DefaultPageSize = 0 }; // setter 直接抛
                                                       // the setter throws directly
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"DefaultPageSize=0 被拒 / DefaultPageSize=0 rejected: {ex.ParamName}"); // value
}

try
{
    _ = new PaginableSettings { MaxMemberItems = 0 };
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"MaxMemberItems=0 被拒 / MaxMemberItems=0 rejected: {ex.ParamName}"); // value
}

// ---------------------------------------------------------------------------
// 3. UpdateSettings(null) 抛 ArgumentNullException
// 3. UpdateSettings(null) throws ArgumentNullException
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. UpdateSettings(null) ===");

try
{
    PaginableSettingsManager.UpdateSettings(null!);
}
catch (ArgumentNullException ex)
{
    Console.WriteLine($"UpdateSettings(null) 被拒 / UpdateSettings(null) rejected: {ex.ParamName}"); // settings
}

// ---------------------------------------------------------------------------
// 4. 还原默认（全局静态状态，避免污染后续）
// 4. Restore defaults (global static state, to avoid polluting later runs)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 还原默认 ===");
Console.WriteLine("=== 4. Restore defaults ===");

// ❌ 错误写法：每个请求都 UpdateSettings（并发读交错、全局可变状态）。
// ❌ Wrong usage: calling UpdateSettings on every request (interleaved concurrent reads, global mutable state).
// ✅ 配置一次；请求差异用显式 pageSize 参数。
// ✅ Configure once; express per-request differences with an explicit pageSize argument.
PaginableSettingsManager.UpdateSettings(new PaginableSettings());
Console.WriteLine($"还原后 DefaultPageSize / After restore DefaultPageSize = {PaginableSettingsManager.Settings.DefaultPageSize}"); // 50
Console.WriteLine($"还原后 MaxMemberItems / After restore MaxMemberItems   = {PaginableSettingsManager.Settings.MaxMemberItems}");   // 10000000

Console.WriteLine();
Console.WriteLine("Sample.PaginableSettings completed.");

// 每个 sample 自带的小型域类型，不跨 sample 共享。
// Each sample carries its own small domain type, not shared across samples.
internal sealed class Order
{
    public int Id;
    public DateTimeOffset CreatedAt;
    public string Name = string.Empty;
    public decimal Amount;
}
