// Sample.EfCoreAdapter — EF Core 适配器：真异步分页 + 键集异步入口（F51）
// Sample.EfCoreAdapter — EF Core adapter: true async pagination + keyset async entry point (F51)
// 运行：dotnet run --project sample/Sample.EfCoreAdapter
// Run: dotnet run --project sample/Sample.EfCoreAdapter
//
// 演示重点：
// Highlights:
//   1. EF Core 适配器公开面**只有** SolidPageExtensions（真异步）+ 独有的 KeysetPageAsyncExtensions；
//   1. The EF Core adapter's public surface has **only** SolidPageExtensions (true async) + the unique KeysetPageAsyncExtensions;
//   2. 与核心包对照：核心的 *Async 只是「形状」（内存源同步完成、不观察令牌），
//   2. contrast with the core package: the core's *Async is merely a "shape" (the in-memory source completes synchronously and does not observe the token),
//      EF Core 的 *Async 走 provider 原生的 CountAsync / ToListAsync，是**真异步**；
//      while EF Core's *Async goes through the provider's native CountAsync / ToListAsync and is **true async**;
//   3. 用 InMemory 提供程序建一个内存 DbContext，端到端跑一次 GetPageAsync；
//   3. build an in-memory DbContext with the InMemory provider and run GetPageAsync end to end once;
//   4. 10 处同名 SolidPageExtensions 的消歧：本文件靠扩展方法解析 + 全限定名对照说明。
//   4. disambiguation of the 10 same-named SolidPageExtensions: this file relies on extension-method resolution + a fully-qualified-name comparison note.
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 0. 准备：InMemory 提供程序 + 内存 DbContext（不连任何真实数据库）
// 0. Setup: InMemory provider + in-memory DbContext (does not connect to any real database)
// ---------------------------------------------------------------------------
Console.WriteLine("=== 0. 准备内存 DbContext（EF Core InMemory）===");
Console.WriteLine("=== 0. Preparing an in-memory DbContext (EF Core InMemory) ===");

var options = new DbContextOptionsBuilder<OrderContext>()
    .UseInMemoryDatabase("orders-demo")   // 内存库，进程退出即消失；确定性：固定库名
                                          // In-memory database, gone when the process exits; deterministic: fixed database name
    .Options;

await using var db = new OrderContext(options);

// 造 25 条确定性数据：Amount = Id * 10，便于肉眼核对分页边界。
// Create 25 deterministic rows: Amount = Id * 10, making the pagination boundaries easy to check by eye.
for (var id = 1; id <= 25; id++)
{
    db.Orders.Add(new Order { Id = id, Name = $"订单{id:D2}", Amount = id * 10m });
}
await db.SaveChangesAsync();
Console.WriteLine($"已写入 / Written {await db.Orders.CountAsync()} 条订单 / orders.");

// 源类型是 IQueryable<Order>（DbSet<T> 也是 IQueryable<T>）。
// The source type is IQueryable<Order> (DbSet<T> is also IQueryable<T>).
IQueryable<Order> query = db.Orders.Where(o => o.Amount > 0);

// ---------------------------------------------------------------------------
// 1. 真异步分页：await GetPageAsync(...)
// 1. True async pagination: await GetPageAsync(...)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 1. 真异步 GetPageAsync（走 CountAsync + ToListAsync）===");
Console.WriteLine("=== 1. True async GetPageAsync (goes through CountAsync + ToListAsync) ===");

// 注意：这里解析到的是 EF Core 包的 GetPageAsync(this IQueryable<T>, ...)，
// Note: what resolves here is the EF Core package's GetPageAsync(this IQueryable<T>, ...),
// 而不是核心包的 GetPageAsync(this IEnumerable<T>, ...)——因为 IQueryable<T> 是更具体的接收者。
// not the core package's GetPageAsync(this IEnumerable<T>, ...) — because IQueryable<T> is the more specific receiver.
// 返回类型是核心契约 IPage<T>（EF Core 复用核心的 EnumerablePage<T>）。
// The return type is the core contract IPage<T> (EF Core reuses the core's EnumerablePage<T>).
IPage<Order> page2 = await query.GetPageAsync(pageNumber: 2, pageSize: 10);

Console.WriteLine($"CurrentPageNumber = {page2.CurrentPageNumber}");   // 2
Console.WriteLine($"PageSize          = {page2.PageSize}");            // 10
Console.WriteLine($"TotalMemberCount  = {page2.TotalMemberCount}");     // 25
Console.WriteLine($"TotalPageCount    = {page2.TotalPageCount}");       // 3
Console.WriteLine($"CurrentPageSize   = {page2.CurrentPageSize}");      // 10（第 2 页是满页）
                                                                        // 10 (page 2 is full)
Console.WriteLine($"HasPrevious/HasNext= {page2.HasPrevious}/{page2.HasNext}"); // True/True
Console.WriteLine("第 2 页内容 / Page 2 contents:");
foreach (var member in page2)
{
    // IPageMember<T>：Value 是本元素，ItemNumber 是**全局**编号（跨页连续）。
    // IPageMember<T>: Value is this element and ItemNumber is the **global** number (continuous across pages).
    Console.WriteLine($"  #{member.ItemNumber,2} {member.Value.Name}  Amount={member.Value.Amount}");
}

// 末页：25 条、每页 10 条 → 第 3 页只有 5 条，CurrentPageSize == 5。
// Last page: 25 rows, 10 per page → page 3 has only 5 rows, CurrentPageSize == 5.
IPage<Order> page3 = await query.GetPageAsync(pageNumber: 3, pageSize: 10);
Console.WriteLine($"末页(3) CurrentPageSize / Last page(3) CurrentPageSize = {page3.CurrentPageSize}, HasNext = {page3.HasNext}"); // 5, False

// ---------------------------------------------------------------------------
// 2. 真异步建集合 + 多次取页
// 2. True async build a collection + fetch pages repeatedly
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. ToPaginableAsync：建集合后多次 GetPage ===");
Console.WriteLine("=== 2. ToPaginableAsync: build a collection then GetPage repeatedly ===");

// ToPaginableAsync 会先 await CountAsync 取总数，再惰性建页。
// ToPaginableAsync first awaits CountAsync to get the total, then lazily builds pages.
// ⚠️ 陷阱：写 query.ToPaginableAsync(pageSize: 10) 会 CS0121——核心包签名是
// ⚠️ Pitfall: writing query.ToPaginableAsync(pageSize: 10) causes CS0121 — the core package's signature is
//    (IEnumerable<T>, int pageSize, ...)，EF 包是 (IQueryable<T>, int? pageSize, ...)；
//    (IEnumerable<T>, int pageSize, ...), while the EF package's is (IQueryable<T>, int? pageSize, ...);
//    接收者上 EF 更具体、实参上核心更精确，编译器判二义。**无实参**调用则唯一解析到 EF 包。
//    EF is more specific on the receiver and the core is more precise on the argument, so the compiler reports ambiguity. A **no-argument** call uniquely resolves to the EF package.
// ❌ var p = await query.ToPaginableAsync(pageSize: 10);   // CS0121
var paginable = await query.ToPaginableAsync();
Console.WriteLine($"Paginable MemberCount = {paginable.MemberCount}");  // 25
Console.WriteLine($"Paginable PageSize    = {paginable.PageSize}");     // 默认页大小 / default page size

IPage<Order> first = paginable.GetPage(1);
Console.WriteLine($"第 1 页首元素全局编号 / Global number of the first element on page 1 = {first.First().ItemNumber}"); // 1

// ---------------------------------------------------------------------------
// 3. DbSet<T> 直接入口 + 同步形态
// 3. Direct DbSet<T> entry point + synchronous form
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. DbSet<T> 直接入口（同步 GetPage）===");
Console.WriteLine("=== 3. Direct DbSet<T> entry point (synchronous GetPage) ===");

// DbSet<T> 重载内部 AsQueryable() 后委托给核心/EF 的 IQueryable 入口。
// The DbSet<T> overload calls AsQueryable() internally and then delegates to the core/EF IQueryable entry point.
IPage<Order> dsPage = db.Orders.GetPage(1, 5);
Console.WriteLine($"db.Orders.GetPage(1, 5) → TotalPageCount={dsPage.TotalPageCount}, CurrentPageSize={dsPage.CurrentPageSize}"); // 5, 5

// ---------------------------------------------------------------------------
// 4. 真异步 vs 核心「形状异步」的差别（说明）
// 4. The difference between true async and the core's "shape async" (explanatory)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 真异步 vs 核心形状异步 ===");
Console.WriteLine("=== 4. True async vs core shape async ===");
Console.WriteLine("核心包 list.GetPageAsync(...) 对 IEnumerable 只是 Task.FromResult（同步完成、不观察令牌）； / The core package's list.GetPageAsync(...) over IEnumerable is only Task.FromResult (completes synchronously, does not observe the token);");
Console.WriteLine("EF Core 包 query.GetPageAsync(...) 真正 await CountAsync/ToListAsync（令牌下传，F44）。 / The EF Core package's query.GetPageAsync(...) genuinely awaits CountAsync/ToListAsync (the token is forwarded, F44).");
Console.WriteLine("另有 KeysetPageAsyncExtensions（键集异步），是 EF Core 独有、其它适配器未必有（F31）。 / There is also KeysetPageAsyncExtensions (keyset async), unique to EF Core and not necessarily present in other adapters (F31).");

// ❌ 错误写法：同时 using 核心与 EF 的 SolidPageExtensions 后直接写类型名——会 CS0104。
// ❌ Wrong approach: after using both the core and EF SolidPageExtensions, writing the type name directly causes CS0104.
//    using DotNetCore.Collections.Paginable;                 // 核心 SolidPageExtensions / core SolidPageExtensions
//    using static ...EntityFrameworkCore.SolidPageExtensions; // 又一处同名 / another same-named one
//    SolidPageExtensions.GetPageAsync(query, 1, 10);          // 编译器不知指向哪个 / the compiler does not know which one is meant
// ✅ 正确写法：调用扩展方法（按接收者类型解析），或对静态类写全限定名 / using 别名。
// ✅ Correct approach: call the extension method (resolved by receiver type), or write a fully-qualified name / using alias for the static class.

// ---------------------------------------------------------------------------
// 5. 越界抛 ArgumentOutOfRangeException（F40 统一）
// 5. Out of range throws ArgumentOutOfRangeException (unified in F40)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 5. 越界异常统一 ===");
Console.WriteLine("=== 5. Unified out-of-range exception ===");
try
{
    await query.GetPageAsync(pageNumber: 99, pageSize: 10);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"越界页码被拒 / Out-of-range page number rejected：{ex.ParamName}"); // pageNumber
}

Console.WriteLine();
Console.WriteLine("Sample.EfCoreAdapter completed.");

// ---------------------------------------------------------------------------
// 模型与上下文（顶层语句之后声明）
// Model and context (declared after the top-level statements)
// ---------------------------------------------------------------------------

/// <summary>订单实体。</summary>
/// <summary>Order entity.</summary>
internal sealed class Order
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

/// <summary>最小 DbContext：一个 Orders 集合。</summary>
/// <summary>Minimal DbContext: a single Orders collection.</summary>
internal sealed class OrderContext : DbContext
{
    public OrderContext(DbContextOptions<OrderContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
}
