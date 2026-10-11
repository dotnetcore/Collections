// Sample.ThreeKeyDictionary — 三键复合字典 ThreeKeyDictionary<K1,K2,K3,V>（F15）
// 运行：dotnet run --project sample/Sample.ThreeKeyDictionary
//
// 演示重点：
//   1. 三键（异构分量）复合字典：三元组索引器 map[k1,k2,k3]，一键一值。
//   2. trie 按 (K1,K2,K3) 索引：只有第一轴是前缀，第二 / 三轴切片靠扫描（O(n)）。
//   3. 6.7.1 新增：Values（扁平）、ContainsValue（全扫描）、AsReverse（轴反转拷贝）。
//   4. 6.8 新增：EntrySet()（Entries 已过时）。
using System;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 三键读写与第一轴切片
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 三键读写与第一轴切片 ===");

var map = new ThreeKeyDictionary<int, string, string, decimal>();
map[1, "USD", "cash"] = 1.00m;
map[1, "EUR", "cash"] = 0.92m;
map[2, "USD", "wire"] = 1.05m;

// 三元组索引器是 MultiKeyDictionary 的人体工学包装；一键一值，不是多重映射。
Console.WriteLine($"map[1,USD,cash] = {map[1, "USD", "cash"]}"); // 1.00
Console.WriteLine($"Count           = {map.Count}");             // 3

// 第一轴前缀查询走 trie 子树下降，O(log n) 起。
Console.WriteLine($"CountOfFirstKey(1)     = {map.CountOfFirstKey(1)}");      // 2
Console.WriteLine($"ContainsFirstKey(1)    = {map.ContainsFirstKey(1)}");     // True
foreach (var (k2, k3, v) in map.GetByFirstKey(1))
{
    Console.WriteLine($"  ({k2},{k3}) = {v}");
}

// ---------------------------------------------------------------------------
// 2. 第二 / 三轴切片：靠扫描，O(n)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 第二 / 三轴切片（扫描）===");

// 第二、三轴不是前缀，切片靠全表扫描。
Console.WriteLine($"CountOfSecondKey(USD) = {map.CountOfSecondKey("USD")}"); // 2
Console.WriteLine($"CountOfThirdKey(cash) = {map.CountOfThirdKey("cash")}"); // 2
Console.WriteLine($"ContainsSecondKey(USD)= {map.ContainsSecondKey("USD")}"); // True
// ❌ 错误写法：把第二轴当 O(1) 索引查询 —— 本类型只有第一轴是前缀，第二 / 三轴是扫描。
// 需要第二轴热点索引请换 TwoKeyDictionary（F09），或把查询轴放到 MultiKeyDictionary 最前。

// ---------------------------------------------------------------------------
// 3. 6.7.1：Values / ContainsValue / AsReverse
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 6.7.1 增强 ===");

// Values：把每个条目的值扁平化（丢弃键分量），恰好 Count 个。
Console.WriteLine($"Values = {string.Join(",", map.Values)}");             // 1.00,0.92,1.05（trie 结构序，不承诺字典序）
// ContainsValue：任意键组合下是否有该值，全扫描 O(n)（本类型无值侧索引）。
Console.WriteLine($"ContainsValue(0.92) = {map.ContainsValue(0.92m)}");    // True
Console.WriteLine($"ContainsValue(9.99) = {map.ContainsValue(9.99m)}");    // False
// AsReverse：轴反转的独立拷贝 ThreeKeyDictionary<K3,K2,K1,V>；原第三轴成为第一轴（前缀可查）。
var rev = map.AsReverse();
Console.WriteLine($"AsReverse().GetByFirstKey(cash).Count = {rev.GetByFirstKey("cash").Count()}"); // 2

// ---------------------------------------------------------------------------
// 4. 6.8：EntrySet()
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 6.8 EntrySet ===");

foreach (var (k1, k2, k3, v) in map.EntrySet())
{
    Console.WriteLine($"  ({k1},{k2},{k3}) = {v}");
}
// ❌ 6.8 起 Entries 属性已过时（仍可编译，但产生 CS0618 弃用警告）；新代码用 EntrySet()。

Console.WriteLine();
Console.WriteLine("Sample.ThreeKeyDictionary completed.");
