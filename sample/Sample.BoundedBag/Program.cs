// Sample.BoundedBag — 定容滑动窗口袋 BoundedBag<T>（6.9.0）
// 运行：dotnet run --project sample/Sample.BoundedBag
//
// 演示重点：
//   1. 容量上限 Capacity：一旦超容就静默淘汰**最旧**的一份副本（FIFO 滑动窗口）。
//   2. Add 返回 void —— 永不失败，没有溢出可报告；Add(item, times) 一次加多份。
//   3. 查询与投影（CountOf / EntrySet / DistinctItems / ToDictionary / 枚举）都是扫描这个有界窗口。
//   4. 它是**独立类型**，刻意不实现 IMultiSet<T>：AddRange 的顺序会影响结果，与接口契约相悖。
using System;
using System.Collections.Generic;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 容量与静默淘汰：窗口只保留最近的 Capacity 份
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 容量与静默淘汰（FIFO）===");

var recent = new BoundedBag<string>(3);   // 最多 3 份副本
recent.Add("a");
recent.Add("b");
recent.Add("c");
Console.WriteLine($"填满后 TotalCount={recent.TotalCount}  IsFull={recent.IsFull}"); // 3  True

// 再加一份就超容：最旧的 "a" 被静默淘汰，add 不报错、不返回任何东西
recent.Add("d");
Console.WriteLine($"Add(\"d\") 后 枚举 = {recent}");                     // b,c,d（最旧→最新）
Console.WriteLine($"Contains(\"a\")={recent.Contains("a")}  Contains(\"d\")={recent.Contains("d")}"); // False True

// ⚠️ 陷阱：Add 是 void。想判断「是否发生了淘汰」只能自己比较前后内容，没有返回值可用。
// ❌ 错误写法：int n = recent.Add("e");   // Add 不返回值，编译失败（CS0815）

// ---------------------------------------------------------------------------
// 2. 一次加多份、顺序敏感与极端 times
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. Add(item, times) / AddRange 顺序敏感 ===");

var bag = new BoundedBag<int>(4);
bag.Add(7, 2);                              // 两份 7
bag.AddRange(new[] { 1, 2, 3 });            // 依次 1、2、3 各一份 => 总量到 5，超容挤掉最旧的一份 7
Console.WriteLine($"TotalCount={bag.TotalCount}  CountOf(7)={bag.CountOf(7)}"); // 4  1

// times >= Capacity：窗口装不下任何旧元素，结果就是 Capacity 份 item
bag.Add(9, 10);
Console.WriteLine("Add(9, 10) 后 EntrySet（只剩 9）：");
foreach (var (item, count) in bag.EntrySet())
{
    Console.WriteLine($"  {item} x {count}");   // 9 x 4
}

// 顺序确实重要：这里 "x" 最先加，容量只有 2，于是它最先被挤出
var ordered = new BoundedBag<string>(2);
ordered.AddRange(new[] { "x", "y", "z" });
Console.WriteLine($"AddRange(x,y,z) 容量 2 => 枚举 = {ordered}");   // y,z

// ---------------------------------------------------------------------------
// 3. 投影与枚举：按「最旧→最新」展开副本
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 投影与枚举顺序（最旧→最新）===");

var window = new BoundedBag<string>(4);
window.AddRange(new[] { "w", "x", "x", "y" });
Console.WriteLine($"枚举（副本展开）= {string.Join(",", window)}");              // w,x,x,y
Console.WriteLine($"TotalCount={window.TotalCount}  DistinctCount={window.DistinctCount}"); // 4  3

// DistinctItems 按「首次出现」顺序给出不同元素；EntrySet 给出紧凑直方图
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
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 删除与参数校验 ===");

var removable = new BoundedBag<int>(5);
removable.AddRange(new[] { 1, 1, 1, 2, 3 });
int remaining = removable.Remove(1, 2);      // 删两份最旧的 1，返回剩余份数
Console.WriteLine($"Remove(1, 2) 后剩余 1 的份数 = {remaining}");        // 1
Console.WriteLine($"RemoveAllCopies(2) = {removable.RemoveAllCopies(2)}"); // True
Console.WriteLine($"删除后 TotalCount = {removable.TotalCount}");        // 2（1 一份 + 3 一份）

// times <= 0 会抛 ArgumentOutOfRangeException，而不是被静默当作「一份」
try
{
    removable.Add(0, 0);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"Add(x, 0) 被拒：{ex.ParamName}");   // times
}

// null 对引用类型是合法元素，可以进袋；但 ToDictionary 无法用 null 作字典键，会抛
var nullBag = new BoundedBag<string>(2);
nullBag.Add("z");
nullBag.Add(null!);                          // 允许：null 是一等元素
Console.WriteLine($"nullBag 含 null：Contains(null)={nullBag.Contains(null!)}"); // True
try
{
    nullBag.ToDictionary();
}
catch (InvalidOperationException)
{
    Console.WriteLine("ToDictionary 遇 null 元素抛 InvalidOperationException");
}

// ❌ 错误写法：var noCap = new BoundedBag<string>();   // 没有无参构造，容量必须为正
Console.WriteLine();
Console.WriteLine("Sample.BoundedBag completed.");
