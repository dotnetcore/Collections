// Sample.SinglePageSet — 单页集合包装（F34）
// 运行：dotnet run --project sample/Sample.SinglePageSet
//
// 演示重点：
//   1. Paginable.CreateSinglePageSet：当签名要 IPaginable<T>，而手头只有一页已装配数据时，
//      把它包成单页集合，避免编造其余页或重新拉全源。
//   2. 分层语义（有意分叉）：集合层 PageCount 恒为 1；页层保留全源编号 CurrentPageNumber / TotalPageCount。
//   3. MemberCount 是全源成员数（= 内页 TotalMemberCount），不是 items.Count。
//   4. GetPage(1) 返回包装的那一页；GetPage(n != 1) 抛 ArgumentOutOfRangeException；foreach 只 yield 一页。
using System;
using System.Collections.Generic;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// 一页片段（第 3 页内容：Id 11,12）+ 全源元数据（每页 5、共 12 条）。
List<Order> items = new List<Order>
{
    new Order { Id = 11, Name = "order-11", Amount = 11 },
    new Order { Id = 12, Name = "order-12", Amount = 12 },
};

// ---------------------------------------------------------------------------
// 1. 包装并按接口消费
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 包装并按接口消费 ===");

// 工厂返回具体类型 PaginableSinglePage<T>（而非裸接口），因为 IPaginable 没有 PageCount 可读。
PaginableSinglePage<Order> set = Paginable.CreateSinglePageSet(items, pageNumber: 3, pageSize: 5, totalMemberCount: 12);

// 它同时实现 IPaginable<T>；IPaginable 面只有 PageSize / MemberCount。
IPaginable<Order> asInterface = set;
Console.WriteLine($"集合层 PageCount   = {set.PageCount}");        // 1（集合层回答「我给你几页」）
Console.WriteLine($"集合层 MemberCount = {set.MemberCount}");    // 12（全源总数，不是 items.Count=2）
Console.WriteLine($"接口面 PageSize    = {asInterface.PageSize}");    // 5
Console.WriteLine($"接口面 MemberCount = {asInterface.MemberCount}"); // 12

// 页层保留全源编号：这是「12 条中的第 3 页」，不是「第 1 页」。
IPage<Order> inner = set.GetPage(1);
Console.WriteLine($"页层 CurrentPageNumber = {inner.CurrentPageNumber}"); // 3
Console.WriteLine($"页层 TotalPageCount    = {inner.TotalPageCount}");    // 3（12 / 5 向上取整）
Console.WriteLine($"页层首个 ItemNumber    = {inner[0].ItemNumber}");     // 11（全局编号）

// ---------------------------------------------------------------------------
// 2. 枚举只 yield 一页
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 枚举只 yield 一页 ===");

// 集合物理上只有一页：foreach 循环体只执行一次。
int loopCount = 0;
foreach (IPage<Order> p in set)
{
    loopCount++;
}
Console.WriteLine($"foreach 循环次数 = {loopCount}"); // 1

// ---------------------------------------------------------------------------
// 3. 取不存在的页 → 越界
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 取不存在的页 ===");

try
{
    // ❌ 错误写法：集合只有 1 页，取第 2 页是越界。
    set.GetPage(2);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"GetPage(2) 越界：{ex.ParamName}"); // pageNumber
}

// ---------------------------------------------------------------------------
// 4. 带 PageFragmentInfo / PageCreationOptions
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 带元数据对象与选项 ===");

// 可用 PageFragmentInfo 代替三元组；校验规则与 CreatePage 相同。
var info = new PageFragmentInfo(pageNumber: 3, pageSize: 5, totalMemberCount: 12);

// Strict 同样生效：本片段恰好 2 条与元数据吻合，通过。
PaginableSinglePage<Order> set2 = Paginable.CreateSinglePageSet(items, info, PageCreationOptions.Strict);
Console.WriteLine($"set2.PageCount   = {set2.PageCount}");   // 1
Console.WriteLine($"set2.MemberCount = {set2.MemberCount}"); // 12

// ⚠️ 陷阱：PageCount 恒为 1，别用它做页码 UI；UI 总页数读 GetPage(1).TotalPageCount。
Console.WriteLine($"UI 总页数应读 GetPage(1).TotalPageCount = {set2.GetPage(1).TotalPageCount}"); // 3

Console.WriteLine();
Console.WriteLine("Sample.SinglePageSet completed.");

// 每个 sample 自带的小型域类型，不跨 sample 共享。
internal sealed class Order
{
    public int Id;
    public DateTimeOffset CreatedAt;
    public string Name = string.Empty;
    public decimal Amount;
}
