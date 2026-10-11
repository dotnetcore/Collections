// Sample.NullableAnnotations — 公开 API 的可空引用类型（NRT）契约（F41）
// 运行：dotnet run --project sample/Sample.NullableAnnotations
//
// 演示重点：
//   1. NRT 是**编译期**契约：T? / string? 标注告诉调用方「这里可为 null」，不产生运行时检查。
//   2. 「传 null 走默认」的典型模式：可空参数为 null 时回退到默认实现（如默认比较器）。
//   3. 库中真实存在的可空契约：int? limitedMemberCount（null = 不限）与 KeysetPage<T>.LastMember（T?）。
//   4. 注解 ≠ 运行时检查：边界仍由库自己抛 ArgumentNullException。
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var orders = new List<Order>
{
    new(3, "C"), new(1, "A"), new(2, "B"),
};

// ---------------------------------------------------------------------------
// 1. 可空参数：传 null 走默认（NRT 契约的典型形态）
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 可空参数：null 回退默认比较器 ===");

// 参数标注为 IComparer<Order>? —— 调用方可以合法地传 null；
// 运行时 null 被替换为 Comparer<Order>.Default（不是异常）。
// 这与本包 Multi 侧 `IEqualityComparer<T>? comparer` → EqualityComparer<T>.Default 是同一约定（F41）。
var byDefault = SortBy(orders, null);
Console.WriteLine($"null  → 默认比较器：{string.Join(",", byDefault.Select(o => o.Id))}"); // 1,2,3

var byIdDesc = SortBy(orders, Comparer<Order>.Create((a, b) => b.Id.CompareTo(a.Id)));
Console.WriteLine($"自定义 → 倒序：    {string.Join(",", byIdDesc.Select(o => o.Id))}");   // 3,2,1

// ❌ 错误写法：以为 NRT 会在运行时替你挡 null（省掉判断会 NRE）。
// ✅ 正确：可空参数在实现内部显式回退 / 判空（SortBy 就是这么做的）。

// ---------------------------------------------------------------------------
// 2. 库中真实可空契约：int? limitedMemberCount
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. ToPaginable 的 int? limitedMemberCount ===");

// 参数类型是 int?（可空值类型）：null = 不限成员数。
var unlimited = orders.ToPaginable(pageSize: 2, limitedMemberCount: null);
Console.WriteLine($"null → 不限：PageCount={unlimited.PageCount}, MemberCount={unlimited.MemberCount}"); // 2,3

var limited = orders.ToPaginable(pageSize: 2, limitedMemberCount: 2);
Console.WriteLine($"2    → 封顶：PageCount={limited.PageCount}, MemberCount={limited.MemberCount}");     // 1,2

// ---------------------------------------------------------------------------
// 3. 可空返回：KeysetPage<T>.LastMember 是 T?
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 可空返回 LastMember（T?）===");

var first = orders.GetFirstPageByKeyset(o => o.Id, pageSize: 2);
Order? last = first.LastMember;          // 类型就是 Order?，编译器要求判空
Console.WriteLine($"LastMember is null = {last is null}");
if (last is not null)
{
    Console.WriteLine($"LastMember.Id      = {last.Id}");   // 2
}
// ❌ 错误写法：Order last2 = first.LastMember;   // CS8600：可能为 null 的赋值
// ✅ 正确写法：先判空再解引用（如上面）。

// ---------------------------------------------------------------------------
// 4. 注解 ≠ 运行时检查
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 注解不替代运行时检查 ===");
try
{
    // UpdateSettings 的参数是非空标注（PaginableSettings）；用 ! 强行压掉编译期警告后，
    // 运行时仍会被库自己抛 ArgumentNullException —— 证明 NRT 只是编译期契约。
    PaginableSettingsManager.UpdateSettings(null!);
}
catch (ArgumentNullException ex)
{
    Console.WriteLine($"运行时仍抛 = {ex.GetType().Name}（ParamName={ex.ParamName}）");
}

Console.WriteLine();
Console.WriteLine("Sample.NullableAnnotations completed.");

// 可空参数 + null 回退默认：演示 NRT 契约「可空参数 → 内部回退」的写法。
static IEnumerable<Order> SortBy(IEnumerable<Order> source, IComparer<Order>? comparer)
    => source.OrderBy(o => o, comparer ?? Comparer<Order>.Default);

/// <summary>示例域模型：订单（按 Id 可比较，便于演示「默认比较器」）。</summary>
internal sealed record Order(int Id, string Name) : IComparable<Order>
{
    public int CompareTo(Order? other) => other is null ? 1 : Id.CompareTo(other.Id);
}
