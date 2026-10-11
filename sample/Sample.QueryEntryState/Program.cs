// Sample.QueryEntryState — 查询入口状态 IQueryEntryState<T>（F48）
// 运行：dotnet run --project sample/Sample.QueryEntryState
//
// 演示重点：
//   1. 接口只有一员 IEnumerable<T> AllValues，位于 Abstractions，且协变（out T）。
//   2. 实现类 QueryEntryState<T> 是 internal；使用方通常经 PageMember.Value 间接用。
//   3. AllValues 可能是延迟序列 —— 多次枚举可能多次查询。
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable;
using DotNetCore.Collections.Paginable.Abstractions;
using DotNetCore.Collections.Paginable.Internal;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var orders = new List<Order>
{
    new(1, "A"), new(2, "B"), new(3, "C"),
};

// ---------------------------------------------------------------------------
// 1. 接口形状：只有 AllValues
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 接口形状 ===");
IQueryEntryState<Order> state = new ListState<Order>(orders);
Console.WriteLine($"AllValues 项数 = {state.AllValues.Count()}");   // 3
Console.Write("枚举 AllValues = ");
foreach (var order in state.AllValues)
{
    Console.Write($"{order.Id} ");
}
Console.WriteLine();

// ---------------------------------------------------------------------------
// 2. 协变：IQueryEntryState<Derived> 可作 IQueryEntryState<Base>
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 协变 out T ===");
IQueryEntryState<object> asObject = state;   // Order -> object（协变）
var firstObj = asObject.AllValues.First();
Console.WriteLine($"经协变接口读到的元素类型 = {firstObj.GetType().Name}");   // Order

// ---------------------------------------------------------------------------
// 3. 供 PageMember 状态形态延迟取值
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 作为 PageMember 状态形态的来源 ===");
int startIndex = 0;
var member = PageMemberFactory.Create(state, offset: 2, ref startIndex);
Console.WriteLine($"member.Value.Id    = {member.Value.Id}");     // 3（AllValues.ElementAt(2)）
Console.WriteLine($"member.ItemNumber  = {member.ItemNumber}");   // 3

// ---------------------------------------------------------------------------
// 4. AllValues 可能是延迟序列：多次枚举 = 多次查询
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 延迟序列：多次枚举 ===");
var counting = new CountingState<Order>(orders);
_ = counting.AllValues.ElementAt(1);   // 第 1 次取 AllValues（枚举 1）
_ = counting.AllValues.ElementAt(2);   // 第 2 次取 AllValues（枚举 2）
Console.WriteLine($"AllValues 被读取次数 = {counting.ReadCount}");   // 2
// ⚠️ 对非列表源，ElementAt 会重枚举；热路径应一次物化（F39）。
// ❌ 错误写法：在循环里反复读 PageMember.Value（状态形态），可能每读一次就重查一次。

Console.WriteLine();
Console.WriteLine("Sample.QueryEntryState completed.");

// 最小实现：把一组元素当作查询结果。
internal sealed class ListState<T> : IQueryEntryState<T>
{
    private readonly IReadOnlyList<T> _items;
    public ListState(IReadOnlyList<T> items) => _items = items;
    public IEnumerable<T> AllValues => _items;
}

// 统计 AllValues 被读取次数的实现，用于演示「延迟序列可能多次枚举」。
internal sealed class CountingState<T> : IQueryEntryState<T>
{
    private readonly IEnumerable<T> _source;
    public CountingState(IEnumerable<T> source) => _source = source;
    public int ReadCount { get; private set; }
    public IEnumerable<T> AllValues
    {
        get
        {
            ReadCount++;
            return _source;
        }
    }
}

/// <summary>示例域模型：订单。</summary>
internal sealed record Order(int Id, string Name);
