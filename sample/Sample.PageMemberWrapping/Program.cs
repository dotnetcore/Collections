// Sample.PageMemberWrapping — 页成员包装与全局编号（F46）
// 运行：dotnet run --project sample/Sample.PageMemberWrapping
//
// 演示重点：
//   1. 页元素被包装为 PageMember<T>（struct）：Value / Offset / ItemNumber。
//   2. ItemNumber 是**全局 1 基编号** = startIndex + offset + 1，跨页连续。
//   3. 双形态：值形态直接持有 T；状态形态持 IQueryEntryState<T>，Value 经 ElementAt 延迟读。
//   4. struct 拷贝语义、offset < 0 构造抛。
using System;
using System.Collections.Generic;
using DotNetCore.Collections.Paginable;
using DotNetCore.Collections.Paginable.Abstractions;
using DotNetCore.Collections.Paginable.Internal;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var orders = new List<Order>
{
    new(11, "K"), new(12, "L"), new(13, "M"),
};

// ---------------------------------------------------------------------------
// 1. 页内成员的全局编号（片段页）
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 全局编号 ItemNumber ===");
var page = Paginable.CreatePage(
    new[] { orders[0], orders[1] },
    pageNumber: 3, pageSize: 5, totalMemberCount: 12);   // 第 3 页第 1 条 → 全局第 11 条

Console.WriteLine($"page[0].ItemNumber = {page[0].ItemNumber}");   // 11
Console.WriteLine($"page[0].Offset     = {page[0].Offset}");       // 0
Console.WriteLine($"page[0].Value.Id   = {page[0].Value.Id}");     // 11
Console.WriteLine($"page[1].ItemNumber = {page[1].ItemNumber}");   // 12
Console.WriteLine($"page[1].Offset     = {page[1].Offset}");       // 1

// ⚠️ 陷阱：ItemNumber 是全局号，不是页内下标。页内下标用 Offset 或索引器。
// ❌ 错误写法：把 page[0].ItemNumber 当 1 用（第 3 页第 1 条其实是 11）。

// ---------------------------------------------------------------------------
// 2. 值形态：工厂直接构造 PageMember（startIndex 以 ref 传入）
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 值形态 PageMemberFactory.Create ===");
int startIndex = 10;   // 代表「前面已有 10 条」，故本条全局号 = 10 + 0 + 1 = 11
var valueMember = PageMemberFactory.Create(orders[0], offset: 0, ref startIndex);
Console.WriteLine($"Value.Id   = {valueMember.Value.Id}");       // 11
Console.WriteLine($"Offset     = {valueMember.Offset}");         // 0
Console.WriteLine($"ItemNumber = {valueMember.ItemNumber}");     // 11

// ---------------------------------------------------------------------------
// 3. 状态形态：持 IQueryEntryState<T>，Value 延迟读取
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 状态形态 PageMemberFactory.Create(state, ...) ===");
// 自定义 IQueryEntryState<T>（公开接口）作为「元素来源」；Value 会经 AllValues.ElementAt(offset) 取。
IQueryEntryState<Order> state = new ListState<Order>(orders);
int si2 = 4;
var stateMember = PageMemberFactory.Create(state, offset: 1, ref si2);
Console.WriteLine($"Value.Id   = {stateMember.Value.Id}");       // 12（orders[1]）
Console.WriteLine($"ItemNumber = {stateMember.ItemNumber}");     // 4 + 1 + 1 = 6

// ---------------------------------------------------------------------------
// 4. struct 语义 + offset 校验
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. struct 拷贝语义与 offset 校验 ===");
Console.WriteLine($"PageMember<Order> 是值类型 = {typeof(PageMember<Order>).IsValueType}");   // True
// PageMember 是 struct：拷贝的是包装，改包装不回写源集合（Value 亦为只读）。
try
{
    int si3 = 0;
    PageMemberFactory.Create(orders[0], offset: -1, ref si3);   // offset < 0 构造抛
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"offset=-1 → {ex.GetType().Name}, ParamName={ex.ParamName}");   // offset
}

Console.WriteLine();
Console.WriteLine("Sample.PageMemberWrapping completed.");

// 公开接口 IQueryEntryState<T> 的最小实现：把一组元素当作「一页的来源」。
internal sealed class ListState<T> : IQueryEntryState<T>
{
    private readonly IReadOnlyList<T> _items;
    public ListState(IReadOnlyList<T> items) => _items = items;
    public IEnumerable<T> AllValues => _items;
}

/// <summary>示例域模型：订单。</summary>
internal sealed record Order(int Id, string Name);
