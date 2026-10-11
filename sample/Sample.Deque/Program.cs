// Sample.Deque — 双端队列（F28）
// 运行：dotnet run --project sample/Sample.Deque
//
// 演示重点：
//   1. Deque<T> 用单数组环状缓冲实现双端队列：两端 O(1) 摊还增删、任意位置 O(1) 读。
//      位置**从头到尾**计：下标 0 是首元素（GetFirst），下标 Count-1 是末元素（GetLast）。
//   2. 完整实现 IList<T> / IReadOnlyList<T>：顺序由调用方控制，索引器 setter 与 Insert 都有意义
//      （与有序家族的收窄不同）。
//   3. 搜索是 O(n)（Contains / IndexOf / Remove(T)）；容量随 List 从 4 倍增、自身不收缩。
using System;
using System.Collections.Generic;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 两端操作与位置读
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 两端操作与位置读 ===");

var line = new Deque<string> { "b", "c" };   // 集合初始化走 Add（等价 AddLast）
line.AddFirst("a");                          // 头插：a, b, c
line.AddLast("d");                           // 尾插：a, b, c, d
Console.WriteLine($"Count = {line.Count}");  // 4
Console.WriteLine($"RemoveFirst() = {line.RemoveFirst()}");   // a
Console.WriteLine($"RemoveLast()  = {line.RemoveLast()}");    // d
Console.WriteLine($"this[0]   = {line[0]}");                  // b：位置从头计，O(1) 环读
Console.WriteLine($"GetFirst() = {line.GetFirst()}");         // b
Console.WriteLine($"GetLast()  = {line.GetLast()}");          // c
Console.WriteLine($"IndexOf(c) = {line.IndexOf("c")}");       // 1：从头扫，O(n)

// ---------------------------------------------------------------------------
// 2. 滑动窗口：最近 N 条
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 滑动窗口 ===");

// 预定 Capacity = 3，避免中途增长分配（容量不足仍会 4 倍增）。
var window = new Deque<int>(3);
foreach (var x in new[] { 1, 2, 3, 4, 5 })
{
    window.AddLast(x);          // 入尾，O(1)
    if (window.Count > 3)
    {
        window.RemoveFirst();   // 超窗弹头，O(1)
    }
}

Console.WriteLine($"window = [{string.Join(", ", window)}]");   // 3, 4, 5
Console.WriteLine($"window.Capacity = {window.Capacity}");     // ≥3（不自动缩）

// ---------------------------------------------------------------------------
// 3. 完整列表接口：索引器 setter / Insert / RemoveAt
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 完整列表接口 ===");

var d = new Deque<int>();
d.AddLast(1);
d.AddLast(2);
d.AddLast(3);                 // 1, 2, 3

// 索引器可赋值：**覆盖**该位置，不移位（与 OrderedMultiList 的 setter 抛不同）。
d[1] = 20;                    // 1, 20, 3
Console.WriteLine($"赋值后 = [{string.Join(", ", d)}]");       // 1, 20, 3

// Insert 任意位置都可用（顺序由调用方控制）。
d.Insert(1, 15);              // 1, 15, 20, 3
Console.WriteLine($"Insert 后 = [{string.Join(", ", d)}]");    // 1, 15, 20, 3

// RemoveAt 按下标删（移较短一侧）。
d.RemoveAt(0);                // 15, 20, 3
Console.WriteLine($"RemoveAt 后 = [{string.Join(", ", d)}]");  // 15, 20, 3

Console.WriteLine($"IndexOf(20) = {d.IndexOf(20)}");           // 1
Console.WriteLine($"Contains(3) = {d.Contains(3)}");           // True
Console.WriteLine($"IsReadOnly = {d.IsReadOnly}");             // False

// 容量管理：可直接设 Capacity；TrimExcess 回收到当前元素数。
Console.WriteLine($"Capacity(设前) = {d.Capacity}");
d.Capacity = 16;
Console.WriteLine($"Capacity(设为16) = {d.Capacity}");         // 16
d.TrimExcess();
Console.WriteLine($"TrimExcess 后 Capacity = {d.Capacity}");   // 3 == Count

// ---------------------------------------------------------------------------
// 4. 空时行为与成本提醒
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 空时行为与成本提醒 ===");

var empty = new Deque<int>();
Console.WriteLine($"IsEmpty = {empty.IsEmpty}");               // True
// 空时端点操作抛 InvalidOperationException；Try 形不抛。
Console.WriteLine($"TryRemoveFirst = {empty.TryRemoveFirst(out _)}");   // False
try
{
    empty.RemoveFirst();
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"空 RemoveFirst 抛：{ex.Message}");
}

// ⚠️ Contains / IndexOf / Remove(T) 是 O(n)：环缓冲无索引。要成员测试用 HashSet / MultiList。
// ❌ 错误写法：在大 deque 上频繁 Contains —— O(n) 扫描拖慢；要 O(1) 成员测试请换结构。
// 结构相等未重写：比内容请用 ToArray() 逐项比较。
Console.WriteLine("Contains / IndexOf / Remove(T) 是 O(n)；容量不自动缩，需 TrimExcess。");

Console.WriteLine();
Console.WriteLine("Sample.Deque completed.");
