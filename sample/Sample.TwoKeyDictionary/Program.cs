// Sample.TwoKeyDictionary — 两键复合字典 TwoKeyDictionary<K1,K2,V>（F29）
// 运行：dotnet run --project sample/Sample.TwoKeyDictionary
//
// 演示重点：
//   1. 两键（异构分量）二维字典：二维索引器 map[k1,k2]，一键一值。
//   2. 双轴投影：GetByFirstKey（前缀下降）/ GetBySecondKey（第二轴反向索引，O(1) 起）。
//   3. RemoveBySecondKey 一次清掉该第二键的全部条目。
//   4. 6.7.1 新增：Values / ContainsValue / AsReverse；6.8 新增：EntrySet()。
using System;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 二维读写与双轴投影
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 二维读写与双轴投影 ===");

var map = new TwoKeyDictionary<int, string, decimal>();
map[1, "USD"] = 1.00m;
map[1, "EUR"] = 0.92m;
map[2, "USD"] = 1.05m;

Console.WriteLine($"map[1,USD] = {map[1, "USD"]}");                        // 1.00
Console.WriteLine($"Count      = {map.Count}");                            // 3
// 第一轴走 trie 前缀下降；第二轴走 K2 -> HashSet<K1> 反向索引（见 F09）——两轴都是 O(1) 起。
Console.WriteLine($"GetByFirstKey(1).Count  = {map.GetByFirstKey(1).Count()}");     // 2
Console.WriteLine($"GetBySecondKey(USD).Count = {map.GetBySecondKey("USD").Count()}"); // 2
Console.WriteLine($"CountOfSecondKey(USD)   = {map.CountOfSecondKey("USD")}");      // 2
Console.WriteLine($"ContainsSecondKey(EUR)  = {map.ContainsSecondKey("EUR")}");     // True

// ---------------------------------------------------------------------------
// 2. RemoveBySecondKey：批量删第二轴
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. RemoveBySecondKey ===");

var m2 = new TwoKeyDictionary<int, string, decimal>();
m2[1, "USD"] = 1m;
m2[2, "USD"] = 2m;
m2[3, "EUR"] = 3m;

// 经反向索引定位相关 K1 后逐条删，保持 trie 与索引同步。
Console.WriteLine($"RemoveBySecondKey(USD) 删除 {m2.RemoveBySecondKey("USD")} 条"); // 2
Console.WriteLine($"剩余 = {m2}"); // (3,EUR):3

// ---------------------------------------------------------------------------
// 3. 6.7.1：Values / ContainsValue / AsReverse
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 6.7.1 增强 ===");

// Values：每个 (k1,k2) 一个条目，扁平化（丢弃键分量），恰好 Count 个。
Console.WriteLine($"Values = {string.Join(",", map.Values)}");            // 1.00,0.92,1.05（trie 结构序）
// ContainsValue：任意键组合下是否有该值，全扫描 O(n)（本类型无值侧索引）。
Console.WriteLine($"ContainsValue(0.92) = {map.ContainsValue(0.92m)}");   // True
// AsReverse：轴对调的独立拷贝 TwoKeyDictionary<K2,K1,V>；原第二轴成为第一轴（前缀可查）。
var rev = map.AsReverse();
Console.WriteLine($"AsReverse().GetByFirstKey(USD).Count = {rev.GetByFirstKey("USD").Count()}"); // 2

// ---------------------------------------------------------------------------
// 4. 6.8：EntrySet()
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 6.8 EntrySet ===");

foreach (var (k1, k2, v) in map.EntrySet())
{
    Console.WriteLine($"  ({k1},{k2}) = {v}");
}
// ❌ 6.8 起 Entries 属性已过时（仍可编译，但产生 CS0618 弃用警告）；新代码用 EntrySet()。

Console.WriteLine();
Console.WriteLine("Sample.TwoKeyDictionary completed.");
