// Sample.MultiKeyMultiDictionary — 复合键多重映射 MultiKeyMultiDictionary<TKey,TValue>（F20）
// 运行：dotnet run --project sample/Sample.MultiKeyMultiDictionary
//
// 演示重点：
//   1. 键是同质分量序列（TKey[]），每个完整键映射到**值集合**（MultiKey + Multi 的组合）。
//   2. Count 是**完整键数**，TotalValueCount 才是值总数（口径易混）。
//   3. 前缀投影：GetByPrefix / CountOfPrefix / GetBranches / RemovePrefix（元组键字典做不到）。
//   4. 6.8 新增：EntrySet()（扁平，一值一对）。
using System;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 复合键多值：Count 是完整键数
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 复合键多值 ===");

var map = new MultiKeyMultiDictionary<string, int>();
map.Add(new[] { "eu", "de", "berlin" }, 1);
map.Add(new[] { "eu", "de", "berlin" }, 2);
map.Add(new[] { "eu", "de", "munich" }, 3);

Console.WriteLine($"Count（完整键数） = {map.Count}");           // 2：berlin / munich
Console.WriteLine($"TotalValueCount   = {map.TotalValueCount}"); // 3：值总数
// 缺键索引器返回空集合；berlin 持有 1、2。
Console.WriteLine($"berlin -> {string.Join(",", map[new[] { "eu", "de", "berlin" }].OrderBy(v => v))}"); // 1,2

// ---------------------------------------------------------------------------
// 2. 前缀投影：子树走位
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 前缀投影 ===");

// CountOfPrefix 返回「该前缀下的完整键数」。
Console.WriteLine($"CountOfPrefix(eu,de) = {map.CountOfPrefix(new[] { "eu", "de" })}"); // 2
// GetByPrefix 枚举部分键下所有完整键及其值。
foreach (var (key, values) in map.GetByPrefix(new[] { "eu" }))
{
    Console.WriteLine($"  {string.Join(".", key)} -> {string.Join(",", values.OrderBy(v => v))}");
}
// eu.de.berlin -> 1,2 ; eu.de.munich -> 3
// GetBranches 列下一层分量。
Console.WriteLine($"GetBranches(eu) = {string.Join(",", map.GetBranches(new[] { "eu" }))}"); // de

// ---------------------------------------------------------------------------
// 3. RemovePrefix：删整棵子树
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. RemovePrefix ===");

// RemovePrefix 返回删除的**值**数（不是键数）；可先 CountOfPrefix 探知受影响键数。
int removedValues = map.RemovePrefix(new[] { "eu", "de" });
Console.WriteLine($"RemovePrefix(eu,de) 删除值数 = {removedValues}"); // 3
Console.WriteLine($"Remove 后 Count = {map.Count}");                  // 0

// ---------------------------------------------------------------------------
// 4. 6.8：EntrySet()（扁平，一值一对）
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 6.8 EntrySet ===");

var m2 = new MultiKeyMultiDictionary<string, int>();
m2.Add(new[] { "eu", "fr", "paris" }, 10);
m2.Add(new[] { "eu", "fr", "paris" }, 20);
// EntrySet 每个存储值产出一对（一个键持多值时贡献多对）。
foreach (var (key, value) in m2.EntrySet())
{
    Console.WriteLine($"  {string.Join(".", key)} = {value}");
}

Console.WriteLine();
Console.WriteLine("Sample.MultiKeyMultiDictionary completed.");
