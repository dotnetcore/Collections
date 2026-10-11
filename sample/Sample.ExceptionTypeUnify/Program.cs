// Sample.ExceptionTypeUnify — 越界参数统一为 ArgumentOutOfRangeException（F40）
// 运行：dotnet run --project sample/Sample.ExceptionTypeUnify
//
// 演示重点：
//   1. 6.2.0 起全部分页入口的越界参数一律抛 ArgumentOutOfRangeException，并携带 ParamName。
//   2. 只改类型、不改拒绝条件：pageNumber < 1、pageSize < 1、pageNumber 越过末页仍拒。
//   3. 空源仍返回单个空页（不抛）——空不是越界。
//   4. 该异常派生自 ArgumentException，因此更宽的 catch (ArgumentException) 也能兜住。
using System;
using System.Collections.Generic;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// 自带小型域模型 + 内存数据源（各 sample 互不共享）。
var orders = new List<Order>
{
    new(1, "A"), new(2, "B"), new(3, "C"), new(4, "D"), new(5, "E"),
};

// ---------------------------------------------------------------------------
// 1. pageNumber < 1
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. pageNumber < 1 ===");
try
{
    orders.GetPage(0, 2);   // 全源切页入口，pageNumber 从 1 起
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"异常类型  = {ex.GetType().Name}");
    Console.WriteLine($"ParamName = {ex.ParamName}");   // pageNumber
}

// ❌ 错误写法：6.2+ 不再抛 IndexOutOfRangeException，下面这个 catch 是死代码：
// try { orders.GetPage(0, 2); }
// catch (IndexOutOfRangeException) { /* 永远进不来，错误会冒泡成未处理异常 */ }
// ✅ 正确写法：catch (ArgumentOutOfRangeException)，或更宽的 catch (ArgumentException)。

// ---------------------------------------------------------------------------
// 2. pageSize < 1
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. pageSize < 1 ===");
try
{
    orders.GetPage(1, 0);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"ParamName = {ex.ParamName}");   // pageSize
}

// ---------------------------------------------------------------------------
// 3. pageNumber 越过末页
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. pageNumber 越过末页 ===");
try
{
    orders.GetPage(4, 2);   // 5 条 / 每页 2 => 只有 3 页，第 4 页越界
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"ParamName = {ex.ParamName}");   // pageNumber
}

// ---------------------------------------------------------------------------
// 4. 空源不抛：返回单个空页
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 空源 → 单个空页，不抛 ===");
var emptyPage = new List<Order>().GetPage(1, 2);
Console.WriteLine($"TotalMemberCount = {emptyPage.TotalMemberCount}");   // 0
Console.WriteLine($"CurrentPageSize  = {emptyPage.CurrentPageSize}");    // 0

// ---------------------------------------------------------------------------
// 5. 片段入口同样统一
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 5. 片段入口 CreatePage 也抛同一类型 ===");
try
{
    Paginable.CreatePage(new[] { orders[0] }, pageNumber: 0, pageSize: 2, totalMemberCount: 5);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"ParamName = {ex.ParamName}");   // pageNumber
}

// ---------------------------------------------------------------------------
// 6. 类型层级：可用更宽的 ArgumentException 兜底
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 6. 派生自 ArgumentException ===");
try
{
    orders.GetPage(1, -1);
}
catch (ArgumentException ex)   // 宽 catch 覆盖 ArgumentOutOfRangeException
{
    Console.WriteLine($"宽 catch 命中 = {ex.GetType().Name}");
    Console.WriteLine($"is ArgumentOutOfRangeException = {ex is ArgumentOutOfRangeException}");
}

Console.WriteLine();
Console.WriteLine("Sample.ExceptionTypeUnify completed.");

/// <summary>示例域模型：订单。</summary>
internal sealed record Order(int Id, string Name);
