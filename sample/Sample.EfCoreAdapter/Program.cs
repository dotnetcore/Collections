// Sample.EfCoreAdapter — EF Core 适配器：真异步分页 + 键集异步入口（F51）
// 运行：dotnet run --project sample/Sample.EfCoreAdapter
//
// 演示重点：
//   1. EF Core 适配器公开面**只有** SolidPageExtensions（真异步）+ 独有的 KeysetPageAsyncExtensions；
//   2. 与核心包对照：核心的 *Async 只是「形状」（内存源同步完成、不观察令牌），
//      EF Core 的 *Async 走 provider 原生的 CountAsync / ToListAsync，是**真异步**；
//   3. 用 InMemory 提供程序建一个内存 DbContext，端到端跑一次 GetPageAsync；
//   4. 10 处同名 SolidPageExtensions 的消歧：本文件靠扩展方法解析 + 全限定名对照说明。
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 0. 准备：InMemory 提供程序 + 内存 DbContext（不连任何真实数据库）
// ---------------------------------------------------------------------------
Console.WriteLine("=== 0. 准备内存 DbContext（EF Core InMemory）===");

var options = new DbContextOptionsBuilder<OrderContext>()
    .UseInMemoryDatabase("orders-demo")   // 内存库，进程退出即消失；确定性：固定库名
    .Options;

await using var db = new OrderContext(options);

// 造 25 条确定性数据：Amount = Id * 10，便于肉眼核对分页边界。
for (var id = 1; id <= 25; id++)
{
    db.Orders.Add(new Order { Id = id, Name = $"订单{id:D2}", Amount = id * 10m });
}
await db.SaveChangesAsync();
Console.WriteLine($"已写入 {await db.Orders.CountAsync()} 条订单。");

// 源类型是 IQueryable<Order>（DbSet<T> 也是 IQueryable<T>）。
IQueryable<Order> query = db.Orders.Where(o => o.Amount > 0);

// ---------------------------------------------------------------------------
// 1. 真异步分页：await GetPageAsync(...)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 1. 真异步 GetPageAsync（走 CountAsync + ToListAsync）===");

// 注意：这里解析到的是 EF Core 包的 GetPageAsync(this IQueryable<T>, ...)，
// 而不是核心包的 GetPageAsync(this IEnumerable<T>, ...)——因为 IQueryable<T> 是更具体的接收者。
// 返回类型是核心契约 IPage<T>（EF Core 复用核心的 EnumerablePage<T>）。
IPage<Order> page2 = await query.GetPageAsync(pageNumber: 2, pageSize: 10);

Console.WriteLine($"CurrentPageNumber = {page2.CurrentPageNumber}");   // 2
Console.WriteLine($"PageSize          = {page2.PageSize}");            // 10
Console.WriteLine($"TotalMemberCount  = {page2.TotalMemberCount}");     // 25
Console.WriteLine($"TotalPageCount    = {page2.TotalPageCount}");       // 3
Console.WriteLine($"CurrentPageSize   = {page2.CurrentPageSize}");      // 10（第 2 页是满页）
Console.WriteLine($"HasPrevious/HasNext= {page2.HasPrevious}/{page2.HasNext}"); // True/True
Console.WriteLine("第 2 页内容：");
foreach (var member in page2)
{
    // IPageMember<T>：Value 是本元素，ItemNumber 是**全局**编号（跨页连续）。
    Console.WriteLine($"  #{member.ItemNumber,2} {member.Value.Name}  Amount={member.Value.Amount}");
}

// 末页：25 条、每页 10 条 → 第 3 页只有 5 条，CurrentPageSize == 5。
IPage<Order> page3 = await query.GetPageAsync(pageNumber: 3, pageSize: 10);
Console.WriteLine($"末页(3) CurrentPageSize = {page3.CurrentPageSize}, HasNext = {page3.HasNext}"); // 5, False

// ---------------------------------------------------------------------------
// 2. 真异步建集合 + 多次取页
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. ToPaginableAsync：建集合后多次 GetPage ===");

// ToPaginableAsync 会先 await CountAsync 取总数，再惰性建页。
// ⚠️ 陷阱：写 query.ToPaginableAsync(pageSize: 10) 会 CS0121——核心包签名是
//    (IEnumerable<T>, int pageSize, ...)，EF 包是 (IQueryable<T>, int? pageSize, ...)；
//    接收者上 EF 更具体、实参上核心更精确，编译器判二义。**无实参**调用则唯一解析到 EF 包。
// ❌ var p = await query.ToPaginableAsync(pageSize: 10);   // CS0121
var paginable = await query.ToPaginableAsync();
Console.WriteLine($"Paginable MemberCount = {paginable.MemberCount}");  // 25
Console.WriteLine($"Paginable PageSize    = {paginable.PageSize}");     // 默认页大小

IPage<Order> first = paginable.GetPage(1);
Console.WriteLine($"第 1 页首元素全局编号 = {first.First().ItemNumber}"); // 1

// ---------------------------------------------------------------------------
// 3. DbSet<T> 直接入口 + 同步形态
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. DbSet<T> 直接入口（同步 GetPage）===");

// DbSet<T> 重载内部 AsQueryable() 后委托给核心/EF 的 IQueryable 入口。
IPage<Order> dsPage = db.Orders.GetPage(1, 5);
Console.WriteLine($"db.Orders.GetPage(1, 5) → TotalPageCount={dsPage.TotalPageCount}, CurrentPageSize={dsPage.CurrentPageSize}"); // 5, 5

// ---------------------------------------------------------------------------
// 4. 真异步 vs 核心「形状异步」的差别（说明）
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 真异步 vs 核心形状异步 ===");
Console.WriteLine("核心包 list.GetPageAsync(...) 对 IEnumerable 只是 Task.FromResult（同步完成、不观察令牌）；");
Console.WriteLine("EF Core 包 query.GetPageAsync(...) 真正 await CountAsync/ToListAsync（令牌下传，F44）。");
Console.WriteLine("另有 KeysetPageAsyncExtensions（键集异步），是 EF Core 独有、其它适配器未必有（F31）。");

// ❌ 错误写法：同时 using 核心与 EF 的 SolidPageExtensions 后直接写类型名——会 CS0104。
//    using DotNetCore.Collections.Paginable;                 // 核心 SolidPageExtensions
//    using static ...EntityFrameworkCore.SolidPageExtensions; // 又一处同名
//    SolidPageExtensions.GetPageAsync(query, 1, 10);          // 编译器不知指向哪个
// ✅ 正确写法：调用扩展方法（按接收者类型解析），或对静态类写全限定名 / using 别名。

// ---------------------------------------------------------------------------
// 5. 越界抛 ArgumentOutOfRangeException（F40 统一）
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 5. 越界异常统一 ===");
try
{
    await query.GetPageAsync(pageNumber: 99, pageSize: 10);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"越界页码被拒：{ex.ParamName}"); // pageNumber
}

Console.WriteLine();
Console.WriteLine("Sample.EfCoreAdapter completed.");

// ---------------------------------------------------------------------------
// 模型与上下文（顶层语句之后声明）
// ---------------------------------------------------------------------------

/// <summary>订单实体。</summary>
internal sealed class Order
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

/// <summary>最小 DbContext：一个 Orders 集合。</summary>
internal sealed class OrderContext : DbContext
{
    public OrderContext(DbContextOptions<OrderContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
}
