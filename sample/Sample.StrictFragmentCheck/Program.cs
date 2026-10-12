// Sample.StrictFragmentCheck — 严格片段校验选项（6.2.0）
// Sample.StrictFragmentCheck — Strict fragment-check option (6.2.0)
// 运行：dotnet run --project sample/Sample.StrictFragmentCheck
// Run: dotnet run --project sample/Sample.StrictFragmentCheck
//
// 演示重点：
// Highlights:
//   1. PageCreationOptions.Lenient（默认，6.1 行为）容忍「比元数据短」的片段；CurrentPageSize 仍报元数据值。
//   1. PageCreationOptions.Lenient (the default, the 6.1 behavior) tolerates a fragment "shorter than the metadata"; CurrentPageSize still reports the metadata value.
//   2. PageCreationOptions.Strict 把短片段视为 bug，抛 ArgumentException（点名 fragment）。
//   2. PageCreationOptions.Strict treats a short fragment as a bug and throws ArgumentException (naming fragment).
//   3. 长片段（超页容量 / 超期望）在两种模式下都抛 —— 那是数据损坏信号，不是容忍问题。
//   3. A long fragment (over page capacity / over the expected count) throws in both modes — that is a data-corruption signal, not a tolerance question.
//   4. 选项是「每次调用」的参数（不是 PaginableSettings 的一部分），CreatePage / ToPage / CreateSinglePageSet 都吃同一选项。
//   4. The option is a "per-call" argument (not part of PaginableSettings); CreatePage / ToPage / CreateSinglePageSet all take the same option.
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 默认 Lenient：短片段容忍
// 1. Default Lenient: short fragments tolerated
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 默认 Lenient：短片段容忍 ===");
Console.WriteLine("=== 1. Default Lenient: short fragments tolerated ===");

// 元数据说第 3 页应有 2 条（total=12, size=5, skip=10 -> expected=2），实际只有 1 条。
// The metadata says page 3 should have 2 rows (total=12, size=5, skip=10 -> expected=2), but there is actually only 1.
List<Order> shortFragment = new List<Order>
{
    new Order { Id = 11, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), Name = "order-11", Amount = 11 },
};

// 无 options 的重载一律等价 Lenient：升级 6.1 代码不必改。
// The overload without options is always equivalent to Lenient: upgrading 6.1 code requires no change.
IPage<Order> lenient = Paginable.CreatePage(shortFragment, pageNumber: 3, pageSize: 5, totalMemberCount: 12);
Console.WriteLine($"CurrentPageSize = {lenient.CurrentPageSize}"); // 2（元数据值，不是实际条数）
                                                                   // 2 (the metadata value, not the actual row count)
Console.WriteLine($"实际枚举条数 / actual enumerated count     = {lenient.Count()}");           // 1
// ⚠️ 陷阱：不要用 CurrentPageSize 推断实际行数——短片段下它仍报元数据值。
// ⚠️ Pitfall: do not infer the actual row count from CurrentPageSize — under a short fragment it still reports the metadata value.

// ---------------------------------------------------------------------------
// 2. Strict：短片段抛错
// 2. Strict: short fragments throw
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. Strict：短片段抛错 ===");
Console.WriteLine("=== 2. Strict: short fragments throw ===");

try
{
    // Strict 把短片段视为 bug（total 陈旧 / 切错查询 / 静默少行），抛 ArgumentException 点名 fragment。
    // Strict treats a short fragment as a bug (stale total / wrong query sliced / silently missing rows), throwing ArgumentException naming fragment.
    Paginable.CreatePage(shortFragment, pageNumber: 3, pageSize: 5, totalMemberCount: 12, PageCreationOptions.Strict);
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Strict 拒绝短片段 / Strict rejects the short fragment: {ex.ParamName}"); // fragment
}

// ❌ 错误写法：以为 Strict 会「修复」少行——它只拒绝建页，修复责任在上游。
// ❌ Wrong usage: expecting Strict to "fix" missing rows — it only rejects page building; fixing is the upstream's responsibility.

// ---------------------------------------------------------------------------
// 3. 扩展方法与单页集合同样支持 options
// 3. Extension methods and single-page sets also support options
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. ToPage / CreateSinglePageSet 支持 options ===");
Console.WriteLine("=== 3. ToPage / CreateSinglePageSet support options ===");

// 第 3 页恰好 2 条（与元数据吻合），Strict 下合法。
// Page 3 has exactly 2 rows (matching the metadata), legal under Strict.
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
                                                                          // 1 (the fragment matches the metadata; Strict passes)

// ---------------------------------------------------------------------------
// 4. 长片段在两种模式下都抛
// 4. A long fragment throws in both modes
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 长片段两种模式都抛 ===");
Console.WriteLine("=== 4. A long fragment throws in both modes ===");

// 第 1 页容量 5，片段却有 6 条：超过页容量，是数据损坏信号。
// Page 1 has capacity 5 but the fragment has 6 rows: over page capacity, a data-corruption signal.
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
        Console.WriteLine($"{(options.IsStrict ? "Strict " : "Lenient")} 长片段被拒 / long fragment rejected: {ex.ParamName}"); // fragment
    }
}

Console.WriteLine();
Console.WriteLine("Sample.StrictFragmentCheck completed.");

// 每个 sample 自带的小型域类型，不跨 sample 共享。
// Each sample carries its own small domain type, not shared across samples.
internal sealed class Order
{
    public int Id;
    public DateTimeOffset CreatedAt;
    public string Name = string.Empty;
    public decimal Amount;
}
