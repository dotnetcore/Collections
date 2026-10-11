// Sample.BoundedBag — 定容滑动窗口袋 BoundedBag<T>（6.9.0）
// Sample.BoundedBag — fixed-capacity sliding-window bag BoundedBag<T> (6.9.0)
// 运行：dotnet run --project sample/Sample.BoundedBag
// Run: dotnet run --project sample/Sample.BoundedBag
//
// 演示重点：
// Highlights:
//   1. 容量上限 Capacity：一旦超容就静默淘汰**最旧**的一份副本（FIFO 滑动窗口）。
//   1. Capacity cap: once over capacity, the **oldest** copy is silently evicted (FIFO sliding window).
//   2. Add 返回 void —— 永不失败，没有溢出可报告；Add(item, times) 一次加多份。
//   2. Add returns void —— it never fails and has no overflow to report; Add(item, times) adds several copies at once.
//   3. 查询与投影（CountOf / EntrySet / DistinctItems / ToDictionary / 枚举）都是扫描这个有界窗口。
//   3. Queries and projections (CountOf / EntrySet / DistinctItems / ToDictionary / enumeration) all scan this bounded window.
//   4. 它是**独立类型**，刻意不实现 IMultiSet<T>：AddRange 的顺序会影响结果，与接口契约相悖。
//   4. It is a **standalone type** that deliberately does not implement IMultiSet<T>: the order of AddRange affects the result, which conflicts with the interface contract.
using System;
using System.Collections.Generic;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 容量与静默淘汰：窗口只保留最近的 Capacity 份
// 1. Capacity and silent eviction: the window keeps only the most recent Capacity copies
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 容量与静默淘汰（FIFO）===");
Console.WriteLine("=== 1. Capacity and silent eviction (FIFO) ===");

var recent = new BoundedBag<string>(3);   // 最多 3 份副本 / at most 3 copies
recent.Add("a");
recent.Add("b");
recent.Add("c");
Console.WriteLine($"填满后 TotalCount / TotalCount after filling = {recent.TotalCount}  IsFull={recent.IsFull}"); // 3  True

// 再加一份就超容：最旧的 "a" 被静默淘汰，add 不报错、不返回任何东西
// Adding one more exceeds capacity: the oldest "a" is silently evicted; add neither errors nor returns anything
recent.Add("d");
Console.WriteLine($"Add(\"d\") 后 枚举 / enumeration after Add(\"d\") = {recent}");                     // b,c,d（最旧→最新）/ b,c,d (oldest→newest)
Console.WriteLine($"Contains(\"a\")={recent.Contains("a")}  Contains(\"d\")={recent.Contains("d")}"); // False True

// ⚠️ 陷阱：Add 是 void。想判断「是否发生了淘汰」只能自己比较前后内容，没有返回值可用。
// ⚠️ Pitfall: Add is void. To tell whether eviction happened you must compare the contents before and after yourself; there is no return value to use.
// ❌ 错误写法：int n = recent.Add("e");   // Add 不返回值，编译失败（CS0815）
// ❌ Wrong approach: int n = recent.Add("e");   // Add returns no value, so this fails to compile (CS0815)

// ---------------------------------------------------------------------------
// 2. 一次加多份、顺序敏感与极端 times
// 2. Adding several copies at once, order sensitivity and extreme times
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. Add(item, times) / AddRange 顺序敏感 ===");
Console.WriteLine("=== 2. Add(item, times) / AddRange order sensitivity ===");

var bag = new BoundedBag<int>(4);
bag.Add(7, 2);                              // 两份 7 / two copies of 7
bag.AddRange(new[] { 1, 2, 3 });            // 依次 1、2、3 各一份 => 总量到 5，超容挤掉最旧的一份 7 / adds 1, 2, 3 in order, one each => total reaches 5, exceeding capacity and squeezing out the oldest 7
Console.WriteLine($"TotalCount={bag.TotalCount}  CountOf(7)={bag.CountOf(7)}"); // 4  1

// times >= Capacity：窗口装不下任何旧元素，结果就是 Capacity 份 item
// times >= Capacity: the window can hold no older element, so the result is Capacity copies of item
bag.Add(9, 10);
Console.WriteLine("Add(9, 10) 后 EntrySet（只剩 9） / EntrySet after Add(9, 10) (only 9 remains):");
foreach (var (item, count) in bag.EntrySet())
{
    Console.WriteLine($"  {item} x {count}");   // 9 x 4
}

// 顺序确实重要：这里 "x" 最先加，容量只有 2，于是它最先被挤出
// Order really matters: here "x" is added first, and the capacity is only 2, so it is squeezed out first
var ordered = new BoundedBag<string>(2);
ordered.AddRange(new[] { "x", "y", "z" });
Console.WriteLine($"AddRange(x,y,z) 容量 2 => 枚举 / AddRange(x,y,z) with capacity 2 => enumeration = {ordered}");   // y,z

// ---------------------------------------------------------------------------
// 3. 投影与枚举：按「最旧→最新」展开副本
// 3. Projection and enumeration: copies are expanded "oldest→newest"
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 投影与枚举顺序（最旧→最新）===");
Console.WriteLine("=== 3. Projection and enumeration order (oldest→newest) ===");

var window = new BoundedBag<string>(4);
window.AddRange(new[] { "w", "x", "x", "y" });
Console.WriteLine($"枚举（副本展开） / Enumerated (copies expanded) = {string.Join(",", window)}");              // w,x,x,y
Console.WriteLine($"TotalCount={window.TotalCount}  DistinctCount={window.DistinctCount}"); // 4  3

// DistinctItems 按「首次出现」顺序给出不同元素；EntrySet 给出紧凑直方图
// DistinctItems yields the distinct elements in "first occurrence" order; EntrySet yields a compact histogram
Console.Write("DistinctItems = ");
foreach (var item in window.DistinctItems())
{
    Console.Write(item + " ");
}
Console.WriteLine();

foreach (var (item, count) in window.EntrySet())
{
    Console.WriteLine($"  EntrySet: {item} x {count}");
}

var histogram = window.ToDictionary();
Console.WriteLine($"ToDictionary[\"x\"] = {histogram["x"]}");   // 2
Console.WriteLine($"ToArray().Length = {window.ToArray().Length}"); // 4 == TotalCount

// ---------------------------------------------------------------------------
// 4. 删除（返回剩余份数）与参数校验
// 4. Removal (returns remaining copies) and argument validation
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 删除与参数校验 ===");
Console.WriteLine("=== 4. Removal and argument validation ===");

var removable = new BoundedBag<int>(5);
removable.AddRange(new[] { 1, 1, 1, 2, 3 });
int remaining = removable.Remove(1, 2);      // 删两份最旧的 1，返回剩余份数 / removes the two oldest 1s, returning the remaining copy count
Console.WriteLine($"Remove(1, 2) 后剩余 1 的份数 / copies of 1 remaining after Remove(1, 2) = {remaining}");        // 1
Console.WriteLine($"RemoveAllCopies(2) = {removable.RemoveAllCopies(2)}"); // True
Console.WriteLine($"删除后 TotalCount / TotalCount after removal = {removable.TotalCount}");        // 2（1 一份 + 3 一份）/ 2 (one 1 plus one 3)

// times <= 0 会抛 ArgumentOutOfRangeException，而不是被静默当作「一份」
// times <= 0 throws ArgumentOutOfRangeException rather than being silently treated as "one copy"
try
{
    removable.Add(0, 0);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"Add(x, 0) 被拒 / Add(x, 0) rejected：{ex.ParamName}");   // times
}

// null 对引用类型是合法元素，可以进袋；但 ToDictionary 无法用 null 作字典键，会抛
// null is a valid element for reference types and can enter the bag; but ToDictionary cannot use null as a dictionary key and will throw
var nullBag = new BoundedBag<string>(2);
nullBag.Add("z");
nullBag.Add(null!);                          // 允许：null 是一等元素 / allowed: null is a first-class element
Console.WriteLine($"nullBag 含 null / nullBag contains null：Contains(null)={nullBag.Contains(null!)}"); // True
try
{
    nullBag.ToDictionary();
}
catch (InvalidOperationException)
{
    Console.WriteLine("ToDictionary 遇 null 元素抛 InvalidOperationException / ToDictionary throws InvalidOperationException on a null element");
}

// ❌ 错误写法：var noCap = new BoundedBag<string>();   // 没有无参构造，容量必须为正
// ❌ Wrong approach: var noCap = new BoundedBag<string>();   // there is no parameterless constructor; the capacity must be positive
Console.WriteLine();
Console.WriteLine("Sample.BoundedBag completed.");
