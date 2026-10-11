// Sample.StrictFragmentCheck — 严格片段校验选项（F33）
// 运行：dotnet run --project sample/Sample.StrictFragmentCheck
//
// 演示重点：
//   1. PageCreationOptions.Lenient（默认，6.1 行为）容忍「比元数据短」的片段；CurrentPageSize 仍报元数据值。
//   2. PageCreationOptions.Strict 把短片段视为 bug，抛 ArgumentException（点名 fragment）。
//   3. 长片段（超页容量 / 超期望）在两种模式下都抛 —— 那是数据损坏信号，不是容忍问题。
//   4. 选项是「每次调用」的参数（不是 PaginableSettings 的一部分），CreatePage / ToPage / CreateSinglePageSet 都吃同一选项。
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 默认 Lenient：短片段容忍
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 默认 Lenient：短片段容忍 ===");

// 元数据说第 3 页应有 2 条（total=12, size=5, skip=10 -> expected=2），实际只有 1 条。
List<Order> shortFragment = new List<Order>
{
    new Order { Id = 11, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), Name = "order-11", Amount = 11 },
};

// 无 options 的重载一律等价 Lenient：升级 6.1 代码不必改。
IPage<Order> lenient = Paginable.CreatePage(shortFragment, pageNumber: 3, pageSize: 5, totalMemberCount: 12);
Console.WriteLine($"CurrentPageSize = {lenient.CurrentPageSize}"); // 2（元数据值，不是实际条数）
Console.WriteLine($"实际枚举条数     = {lenient.Count()}");           // 1
// ⚠️ 陷阱：不要用 CurrentPageSize 推断实际行数——短片段下它仍报元数据值。

// ---------------------------------------------------------------------------
// 2. Strict：短片段抛错
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. Strict：短片段抛错 ===");

try
{
    // Strict 把短片段视为 bug（total 陈旧 / 切错查询 / 静默少行），抛 ArgumentException 点名 fragment。
    Paginable.CreatePage(shortFragment, pageNumber: 3, pageSize: 5, totalMemberCount: 12, PageCreationOptions.Strict);
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Strict 拒绝短片段：{ex.ParamName}"); // fragment
}

// ❌ 错误写法：以为 Strict 会「修复」少行——它只拒绝建页，修复责任在上游。

// ---------------------------------------------------------------------------
// 3. 扩展方法与单页集合同样支持 options
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. ToPage / CreateSinglePageSet 支持 options ===");

// 第 3 页恰好 2 条（与元数据吻合），Strict 下合法。
List<Order> items = new List<Order>
{
    new Order { Id = 11, Name = "order-11", Amount = 11 },
    new Order { Id = 12, Name = "order-12", Amount = 12 },
};

IPage<Order> a = items.ToPage(pageNumber: 3, pageSize: 5, totalMemberCount: 12, PageCreationOptions.Lenient);
Console.WriteLine($"ToPage Lenient CurrentPageSize = {a.CurrentPageSize}"); // 2

PaginableSinglePage<Order> b = Paginable.CreateSinglePageSet(
    items, new PageFragmentInfo(3, 5, 12), PageCreationOptions.Strict);
Console.WriteLine($"CreateSinglePageSet Strict PageCount = {b.PageCount}"); // 1（片段与元数据吻合，Strict 通过）

// ---------------------------------------------------------------------------
// 4. 长片段在两种模式下都抛
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 长片段两种模式都抛 ===");

// 第 1 页容量 5，片段却有 6 条：超过页容量，是数据损坏信号。
List<Order> longFragment = Enumerable.Range(1, 6)
    .Select(i => new Order { Id = i, Name = $"order-{i:00}", Amount = i })
    .ToList();

foreach (var options in new[] { PageCreationOptions.Lenient, PageCreationOptions.Strict })
{
    try
    {
        Paginable.CreatePage(longFragment, pageNumber: 1, pageSize: 5, totalMemberCount: 12, options);
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine($"{(options.IsStrict ? "Strict " : "Lenient")} 长片段被拒：{ex.ParamName}"); // fragment
    }
}

Console.WriteLine();
Console.WriteLine("Sample.StrictFragmentCheck completed.");

// 每个 sample 自带的小型域类型，不跨 sample 共享。
internal sealed class Order
{
    public int Id;
    public DateTimeOffset CreatedAt;
    public string Name = string.Empty;
    public decimal Amount;
}
