// Sample.Deque — 双端队列（F28）
// Sample.Deque — double-ended queue (F28)
// 运行：dotnet run --project sample/Sample.Deque
// Run: dotnet run --project sample/Sample.Deque
//
// 演示重点：
// Highlights:
//   1. Deque<T> 用单数组环状缓冲实现双端队列：两端 O(1) 摊还增删、任意位置 O(1) 读。
//   1. Deque<T> implements a double-ended queue with a single-array ring buffer: O(1) amortized insertion/removal at both ends, O(1) read at any position.
//      位置**从头到尾**计：下标 0 是首元素（GetFirst），下标 Count-1 是末元素（GetLast）。
//      Positions are counted **from head to tail**: index 0 is the first element (GetFirst), index Count-1 the last (GetLast).
//   2. 完整实现 IList<T> / IReadOnlyList<T>：顺序由调用方控制，索引器 setter 与 Insert 都有意义
//   2. Fully implements IList<T> / IReadOnlyList<T>: the order is controlled by the caller, so both the indexer setter and Insert are meaningful
//      （与有序家族的收窄不同）。
//      (unlike the ordered family's narrowing).
//   3. 搜索是 O(n)（Contains / IndexOf / Remove(T)）；容量随 List 从 4 倍增、自身不收缩。
//   3. Search is O(n) (Contains / IndexOf / Remove(T)); capacity doubles from 4 like List and does not shrink on its own.
using System;
using System.Collections.Generic;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 两端操作与位置读
// 1. Operations at both ends and positional reads
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 两端操作与位置读 ===");
Console.WriteLine("=== 1. Operations at both ends and positional reads ===");

var line = new Deque<string> { "b", "c" };   // 集合初始化走 Add（等价 AddLast）
                                             // collection initialization goes through Add (equivalent to AddLast)
line.AddFirst("a");                          // 头插：a, b, c
                                             // head insert: a, b, c
line.AddLast("d");                           // 尾插：a, b, c, d
                                             // tail insert: a, b, c, d
Console.WriteLine($"Count = {line.Count}");  // 4
Console.WriteLine($"RemoveFirst() = {line.RemoveFirst()}");   // a
Console.WriteLine($"RemoveLast()  = {line.RemoveLast()}");    // d
Console.WriteLine($"this[0]   = {line[0]}");                  // b：位置从头计，O(1) 环读
                                                              // b: positions counted from the head, an O(1) ring read
Console.WriteLine($"GetFirst() = {line.GetFirst()}");         // b
Console.WriteLine($"GetLast()  = {line.GetLast()}");          // c
Console.WriteLine($"IndexOf(c) = {line.IndexOf("c")}");       // 1：从头扫，O(n)
                                                              // 1: scans from the head, O(n)

// ---------------------------------------------------------------------------
// 2. 滑动窗口：最近 N 条
// 2. Sliding window: the most recent N items
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 滑动窗口 ===");
Console.WriteLine("=== 2. Sliding window ===");

// 预定 Capacity = 3，避免中途增长分配（容量不足仍会 4 倍增）。
// Reserve Capacity = 3 to avoid growth allocation midway (if capacity is insufficient it still doubles by 4×).
var window = new Deque<int>(3);
foreach (var x in new[] { 1, 2, 3, 4, 5 })
{
    window.AddLast(x);          // 入尾，O(1)
                                // enqueue at the tail, O(1)
    if (window.Count > 3)
    {
        window.RemoveFirst();   // 超窗弹头，O(1)
                                // over the window, dequeue at the head, O(1)
    }
}

Console.WriteLine($"window = [{string.Join(", ", window)}]");   // 3, 4, 5
Console.WriteLine($"window.Capacity = {window.Capacity}");     // ≥3（不自动缩）
                                                               // ≥3 (does not shrink automatically)

// ---------------------------------------------------------------------------
// 3. 完整列表接口：索引器 setter / Insert / RemoveAt
// 3. Complete list interface: indexer setter / Insert / RemoveAt
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 完整列表接口 ===");
Console.WriteLine("=== 3. Complete list interface ===");

var d = new Deque<int>();
d.AddLast(1);
d.AddLast(2);
d.AddLast(3);                 // 1, 2, 3

// 索引器可赋值：**覆盖**该位置，不移位（与 OrderedMultiList 的 setter 抛不同）。
// The indexer is assignable: it **overwrites** that position without shifting (unlike OrderedMultiList's setter, which throws).
d[1] = 20;                    // 1, 20, 3
Console.WriteLine($"赋值后 / after assignment = [{string.Join(", ", d)}]");       // 1, 20, 3

// Insert 任意位置都可用（顺序由调用方控制）。
// Insert works at any position (the order is controlled by the caller).
d.Insert(1, 15);              // 1, 15, 20, 3
Console.WriteLine($"Insert 后 / after Insert = [{string.Join(", ", d)}]");    // 1, 15, 20, 3

// RemoveAt 按下标删（移较短一侧）。
// RemoveAt removes by index (shifting the shorter side).
d.RemoveAt(0);                // 15, 20, 3
Console.WriteLine($"RemoveAt 后 / after RemoveAt = [{string.Join(", ", d)}]");  // 15, 20, 3

Console.WriteLine($"IndexOf(20) = {d.IndexOf(20)}");           // 1
Console.WriteLine($"Contains(3) = {d.Contains(3)}");           // True
Console.WriteLine($"IsReadOnly = {d.IsReadOnly}");             // False

// 容量管理：可直接设 Capacity；TrimExcess 回收到当前元素数。
// Capacity management: set Capacity directly; TrimExcess reclaims down to the current element count.
Console.WriteLine($"Capacity(设前) / Capacity (before setting) = {d.Capacity}");
d.Capacity = 16;
Console.WriteLine($"Capacity(设为16) / Capacity (set to 16) = {d.Capacity}");         // 16
d.TrimExcess();
Console.WriteLine($"TrimExcess 后 Capacity / Capacity after TrimExcess = {d.Capacity}");   // 3 == Count

// ---------------------------------------------------------------------------
// 4. 空时行为与成本提醒
// 4. Empty behavior and a cost reminder
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 空时行为与成本提醒 ===");
Console.WriteLine("=== 4. Empty behavior and a cost reminder ===");

var empty = new Deque<int>();
Console.WriteLine($"IsEmpty = {empty.IsEmpty}");               // True
// 空时端点操作抛 InvalidOperationException；Try 形不抛。
// Endpoint operations on an empty deque throw InvalidOperationException; the Try forms do not.
Console.WriteLine($"TryRemoveFirst = {empty.TryRemoveFirst(out _)}");   // False
try
{
    empty.RemoveFirst();
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"空 RemoveFirst 抛 / empty RemoveFirst throws: {ex.Message}");
}

// ⚠️ Contains / IndexOf / Remove(T) 是 O(n)：环缓冲无索引。要成员测试用 HashSet / MultiList。
// ⚠️ Contains / IndexOf / Remove(T) are O(n): a ring buffer has no index. For membership testing use HashSet / MultiList.
// ❌ 错误写法：在大 deque 上频繁 Contains —— O(n) 扫描拖慢；要 O(1) 成员测试请换结构。
// ❌ Wrong usage: calling Contains frequently on a large deque — the O(n) scan slows things down; for O(1) membership testing switch structures.
// 结构相等未重写：比内容请用 ToArray() 逐项比较。
// Structural equality is not overridden: to compare contents, compare item by item via ToArray().
Console.WriteLine("Contains / IndexOf / Remove(T) 是 O(n)；容量不自动缩，需 TrimExcess。 / Contains / IndexOf / Remove(T) are O(n); capacity does not shrink automatically, use TrimExcess.");

Console.WriteLine();
Console.WriteLine("Sample.Deque completed.");
