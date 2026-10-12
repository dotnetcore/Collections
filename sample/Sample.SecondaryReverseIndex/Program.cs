// Sample.SecondaryReverseIndex — 第二轴反向索引（6.2.0）
// Sample.SecondaryReverseIndex — secondary-axis reverse index (6.2.0)
// 运行：dotnet run --project sample/Sample.SecondaryReverseIndex
// Run: dotnet run --project sample/Sample.SecondaryReverseIndex
//
// 演示重点：
// Highlights:
//   1. TwoKeyDictionary<K1,K2,V> 底层 trie 按 (K1,K2) 索引，只有第一轴是前缀。
//   1. The underlying trie of TwoKeyDictionary<K1,K2,V> indexes by (K1,K2), so only the first axis is a prefix.
//   2. 第二轴反向索引（K2 -> HashSet<K1>）把第二轴查询从全表扫描变成一次查找。
//   2. The secondary-axis reverse index (K2 -> HashSet<K1>) turns a second-axis query from a full-table scan into a single lookup.
//   3. GetBySecondKey / CountOfSecondKey / ContainsSecondKey / RemoveBySecondKey 都走该索引。
//   3. GetBySecondKey / CountOfSecondKey / ContainsSecondKey / RemoveBySecondKey all go through that index.
//   4. Keys2 刻意仍走 trie，保留**首次出现**顺序；null 第二分量走专用桶。
//   4. Keys2 deliberately still walks the trie, preserving **first-appearance** order; a null second component goes to a dedicated bucket.
using System;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 第二轴切片：一次查找，不扫全表
// 1. Second-axis slice: one lookup, no full-table scan
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 第二轴切片 ===");
Console.WriteLine("=== 1. Second-axis slice ===");

var map = new TwoKeyDictionary<int, string, decimal>();
map[1, "USD"] = 1.00m;
map[1, "EUR"] = 0.92m;
map[2, "USD"] = 1.05m;

// GetBySecondKey 经反向索引定位相关 K1，再读 trie 条目——不扫全表。
// GetBySecondKey locates the relevant K1 via the reverse index, then reads trie entries — no full-table scan.
Console.WriteLine($"GetBySecondKey(USD).Count = {map.GetBySecondKey("USD").Count()}"); // 2
Console.WriteLine($"CountOfSecondKey(USD)     = {map.CountOfSecondKey("USD")}");      // 2
Console.WriteLine($"ContainsSecondKey(EUR)    = {map.ContainsSecondKey("EUR")}");     // True
Console.WriteLine($"ContainsSecondKey(JPY)    = {map.ContainsSecondKey("JPY")}");     // False
// 切片内容用排序保证输出确定性（索引本身不承诺顺序）。
// The slice content is sorted to keep the output deterministic (the index itself does not promise an order).
var usd = map.GetBySecondKey("USD").Select(e => e.Key1).OrderBy(k => k);
Console.WriteLine($"USD 相关 K1 / K1 related to USD = {string.Join(",", usd)}"); // 1,2

// ---------------------------------------------------------------------------
// 2. 按第二轴批量删除
// 2. Batch deletion by the second axis
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. RemoveBySecondKey ===");

var m2 = new TwoKeyDictionary<int, string, decimal>();
m2[1, "USD"] = 1m;
m2[2, "USD"] = 2m;
m2[3, "EUR"] = 3m;

// 经反向索引定位相关 K1 后逐条删，保持 trie 与 _secondIndex 同步。
// After locating the relevant K1 via the reverse index, it deletes them one by one, keeping the trie and _secondIndex in sync.
int removed = m2.RemoveBySecondKey("USD");
Console.WriteLine($"RemoveBySecondKey(USD) 删除 / deleted {removed} 条 / entries"); // 2
Console.WriteLine($"剩余 / Remaining = {m2}");                               // (3,EUR):3
// ⚠️ 不要手写循环 Remove 来批量删第二轴——会漏同步索引或删不干净，用 RemoveBySecondKey。
// ⚠️ Do not hand-write a Remove loop to batch-delete by the second axis — it may miss index synchronization or leave residues; use RemoveBySecondKey.

// ---------------------------------------------------------------------------
// 3. Keys2 顺序与 null 第二分量
// 3. Keys2 order and the null second component
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. Keys2 顺序与 null 第二分量 ===");
Console.WriteLine("=== 3. Keys2 order and the null second component ===");

var m3 = new TwoKeyDictionary<int, string?, decimal>();
m3[1, "USD"] = 1m;
m3[2, "EUR"] = 2m;
m3[3, null]  = 3m;   // null 第二分量走专用桶（Dictionary 拒 null 键）/ the null second component goes to a dedicated bucket (Dictionary rejects null keys)

// Keys2 刻意仍走 trie：顺序是**首次出现**，不是排序。
// Keys2 deliberately still walks the trie: the order is **first-appearance**, not sorted.
Console.WriteLine($"Keys2 = {string.Join(",", m3.Keys2.Select(k => k ?? "<null>"))}");
Console.WriteLine($"ContainsSecondKey(null) = {m3.ContainsSecondKey(null)}"); // True
Console.WriteLine($"CountOfSecondKey(null)  = {m3.CountOfSecondKey(null)}");  // 1

Console.WriteLine();
Console.WriteLine("Sample.SecondaryReverseIndex completed.");
