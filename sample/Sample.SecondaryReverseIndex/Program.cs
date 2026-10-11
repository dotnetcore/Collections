// Sample.SecondaryReverseIndex — 第二轴反向索引（F09）
// 运行：dotnet run --project sample/Sample.SecondaryReverseIndex
//
// 演示重点：
//   1. TwoKeyDictionary<K1,K2,V> 底层 trie 按 (K1,K2) 索引，只有第一轴是前缀。
//   2. 第二轴反向索引（K2 -> HashSet<K1>）把第二轴查询从全表扫描变成一次查找。
//   3. GetBySecondKey / CountOfSecondKey / ContainsSecondKey / RemoveBySecondKey 都走该索引。
//   4. Keys2 刻意仍走 trie，保留**首次出现**顺序；null 第二分量走专用桶。
using System;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 第二轴切片：一次查找，不扫全表
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 第二轴切片 ===");

var map = new TwoKeyDictionary<int, string, decimal>();
map[1, "USD"] = 1.00m;
map[1, "EUR"] = 0.92m;
map[2, "USD"] = 1.05m;

// GetBySecondKey 经反向索引定位相关 K1，再读 trie 条目——不扫全表。
Console.WriteLine($"GetBySecondKey(USD).Count = {map.GetBySecondKey("USD").Count()}"); // 2
Console.WriteLine($"CountOfSecondKey(USD)     = {map.CountOfSecondKey("USD")}");      // 2
Console.WriteLine($"ContainsSecondKey(EUR)    = {map.ContainsSecondKey("EUR")}");     // True
Console.WriteLine($"ContainsSecondKey(JPY)    = {map.ContainsSecondKey("JPY")}");     // False
// 切片内容用排序保证输出确定性（索引本身不承诺顺序）。
var usd = map.GetBySecondKey("USD").Select(e => e.Key1).OrderBy(k => k);
Console.WriteLine($"USD 相关 K1 = {string.Join(",", usd)}"); // 1,2

// ---------------------------------------------------------------------------
// 2. 按第二轴批量删除
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. RemoveBySecondKey ===");

var m2 = new TwoKeyDictionary<int, string, decimal>();
m2[1, "USD"] = 1m;
m2[2, "USD"] = 2m;
m2[3, "EUR"] = 3m;

// 经反向索引定位相关 K1 后逐条删，保持 trie 与 _secondIndex 同步。
int removed = m2.RemoveBySecondKey("USD");
Console.WriteLine($"RemoveBySecondKey(USD) 删除 {removed} 条"); // 2
Console.WriteLine($"剩余 = {m2}");                               // (3,EUR):3
// ⚠️ 不要手写循环 Remove 来批量删第二轴——会漏同步索引或删不干净，用 RemoveBySecondKey。

// ---------------------------------------------------------------------------
// 3. Keys2 顺序与 null 第二分量
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. Keys2 顺序与 null 第二分量 ===");

var m3 = new TwoKeyDictionary<int, string?, decimal>();
m3[1, "USD"] = 1m;
m3[2, "EUR"] = 2m;
m3[3, null]  = 3m;   // null 第二分量走专用桶（Dictionary 拒 null 键）

// Keys2 刻意仍走 trie：顺序是**首次出现**，不是排序。
Console.WriteLine($"Keys2 = {string.Join(",", m3.Keys2.Select(k => k ?? "<null>"))}");
Console.WriteLine($"ContainsSecondKey(null) = {m3.ContainsSecondKey(null)}"); // True
Console.WriteLine($"CountOfSecondKey(null)  = {m3.CountOfSecondKey(null)}");  // 1

Console.WriteLine();
Console.WriteLine("Sample.SecondaryReverseIndex completed.");
