// Sample.MultiKeyMultiDictionary — 复合键多重映射 MultiKeyMultiDictionary<TKey,TValue>（6.4.0）
// Sample.MultiKeyMultiDictionary — composite-key multimap MultiKeyMultiDictionary<TKey,TValue> (6.4.0)
// 运行：dotnet run --project sample/Sample.MultiKeyMultiDictionary
// Run: dotnet run --project sample/Sample.MultiKeyMultiDictionary
//
// 演示重点：
// Highlights:
//   1. 键是同质分量序列（TKey[]），每个完整键映射到**值集合**（MultiKey + Multi 的组合）。
//   1. The key is a sequence of homogeneous components (TKey[]), and each full key maps to a **value set** (the combination of MultiKey + Multi).
//   2. Count 是**完整键数**，TotalValueCount 才是值总数（口径易混）。
//   2. Count is the **full key count**; TotalValueCount is the value total (the conventions are easy to confuse).
//   3. 前缀投影：GetByPrefix / CountOfPrefix / GetBranches / RemovePrefix（元组键字典做不到）。
//   3. Prefix projection: GetByPrefix / CountOfPrefix / GetBranches / RemovePrefix (which a tuple-key dictionary cannot do).
//   4. 6.8 新增：EntrySet()（扁平，一值一对）。
//   4. Added in 6.8: EntrySet() (flat, one pair per value).
using System;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 复合键多值：Count 是完整键数
// 1. Composite-key multi-values: Count is the full key count
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 复合键多值 ===");
Console.WriteLine("=== 1. Composite-key multi-values ===");

var map = new MultiKeyMultiDictionary<string, int>();
map.Add(new[] { "eu", "de", "berlin" }, 1);
map.Add(new[] { "eu", "de", "berlin" }, 2);
map.Add(new[] { "eu", "de", "munich" }, 3);

Console.WriteLine($"Count（完整键数） / Count (full key count) = {map.Count}");           // 2：berlin / munich
Console.WriteLine($"TotalValueCount   = {map.TotalValueCount}"); // 3：值总数 / 3: the value total
// 缺键索引器返回空集合；berlin 持有 1、2。
// A missing-key indexer returns an empty collection; berlin holds 1 and 2.
Console.WriteLine($"berlin -> {string.Join(",", map[new[] { "eu", "de", "berlin" }].OrderBy(v => v))}"); // 1,2

// ---------------------------------------------------------------------------
// 2. 前缀投影：子树走位
// 2. Prefix projection: subtree walking
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 前缀投影 ===");
Console.WriteLine("=== 2. Prefix projection ===");

// CountOfPrefix 返回「该前缀下的完整键数」。
// CountOfPrefix returns "the full key count under that prefix".
Console.WriteLine($"CountOfPrefix(eu,de) = {map.CountOfPrefix(new[] { "eu", "de" })}"); // 2
// GetByPrefix 枚举部分键下所有完整键及其值。
// GetByPrefix enumerates all full keys under a partial key together with their values.
foreach (var (key, values) in map.GetByPrefix(new[] { "eu" }))
{
    Console.WriteLine($"  {string.Join(".", key)} -> {string.Join(",", values.OrderBy(v => v))}");
}
// eu.de.berlin -> 1,2 ; eu.de.munich -> 3
// GetBranches 列下一层分量。
// GetBranches lists the components of the next level.
Console.WriteLine($"GetBranches(eu) = {string.Join(",", map.GetBranches(new[] { "eu" }))}"); // de

// ---------------------------------------------------------------------------
// 3. RemovePrefix：删整棵子树
// 3. RemovePrefix: removes a whole subtree
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. RemovePrefix ===");

// RemovePrefix 返回删除的**值**数（不是键数）；可先 CountOfPrefix 探知受影响键数。
// RemovePrefix returns the deleted **value** count (not the key count); use CountOfPrefix first to learn the affected key count.
int removedValues = map.RemovePrefix(new[] { "eu", "de" });
Console.WriteLine($"RemovePrefix(eu,de) 删除值数 / deleted value count = {removedValues}"); // 3
Console.WriteLine($"Remove 后 Count / Count after Remove = {map.Count}");                  // 0

// ---------------------------------------------------------------------------
// 4. 6.8：EntrySet()（扁平，一值一对）
// 4. 6.8: EntrySet() (flat, one pair per value)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 6.8 EntrySet ===");

var m2 = new MultiKeyMultiDictionary<string, int>();
m2.Add(new[] { "eu", "fr", "paris" }, 10);
m2.Add(new[] { "eu", "fr", "paris" }, 20);
// EntrySet 每个存储值产出一对（一个键持多值时贡献多对）。
// EntrySet yields one pair per stored value (a key holding multiple values contributes multiple pairs).
foreach (var (key, value) in m2.EntrySet())
{
    Console.WriteLine($"  {string.Join(".", key)} = {value}");
}

Console.WriteLine();
Console.WriteLine("Sample.MultiKeyMultiDictionary completed.");
