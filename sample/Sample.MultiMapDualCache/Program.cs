// Sample.MultiMapDualCache — 多重映射双缓存（F11）
// Sample.MultiMapDualCache — Multimap dual cache (F11)
// 运行：dotnet run --project sample/Sample.MultiMapDualCache
// Run: dotnet run --project sample/Sample.MultiMapDualCache
//
// 演示重点：
// Highlights:
//   1. TotalValueCount 是 O(1) 缓存读、ContainsValue 走值反向索引 O(1)——这是双缓存的外显。
//   1. TotalValueCount is an O(1) cached read and ContainsValue goes through the value reverse index in O(1) — this is the dual cache made visible.
//   2. null 值走专用桶（_nullValueKeys），ContainsValue(null) 同样 O(1)。
//   2. null values go to a dedicated bucket (_nullValueKeys), and ContainsValue(null) is likewise O(1).
//   3. 缓存由**公开突变 API** 维护；Clone 经公开 Add 重建，避免第二套簿记漂移。
//   3. The caches are maintained by the **public mutation APIs**; Clone rebuilds through the public Add, avoiding a second set of bookkeeping that could drift.
using System;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. O(1) 读：值总数与值存在性
// 1. O(1) reads: total value count and value existence
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. O(1) 读 ===");
Console.WriteLine("=== 1. O(1) reads ===");

var map = new MultiDictionary<string, int>();
map.Add("orders", 1001);
map.Add("customers", 1001);
map.Add("orders", 1002);

// TotalValueCount 由 _totalValueCount 缓存直接读出，不再每次扫描内层集合（旧行为成本 O(键数)）。
// TotalValueCount is read straight from the _totalValueCount cache, no longer scanning the inner collections each time (the old behavior cost O(keys)).
Console.WriteLine($"TotalValueCount = {map.TotalValueCount}");          // 3
// ContainsValue 走 _valueIndex（值 -> 键集）反向索引，不扫全表（旧行为成本 O(键 × 值)）。
// ContainsValue goes through the _valueIndex (value -> key set) reverse index instead of scanning the whole table (the old behavior cost O(keys × values)).
Console.WriteLine($"ContainsValue(1001) = {map.ContainsValue(1001)}");  // True
Console.WriteLine($"ContainsValue(9999) = {map.ContainsValue(9999)}");  // False
// 同一个值被多个键存储时，反向索引里仍只有一条索引项。
// Even when the same value is stored under multiple keys, the reverse index still holds only one index entry.
Console.WriteLine($"ValueCount(orders) = {map.ValueCount("orders")}");  // 2

// ---------------------------------------------------------------------------
// 2. null 值专用桶
// 2. Dedicated bucket for null values
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. null 值专用桶 ===");
Console.WriteLine("=== 2. Dedicated bucket for null values ===");

var withNull = new MultiDictionary<string, string?>();
withNull.Add("x", null);
withNull.Add("y", "v");

// Dictionary 不能以 null 作键，故 null 值单独放 _nullValueKeys；ContainsValue(null) 仍是 O(1)。
// A Dictionary cannot use null as a key, so null values are kept separately in _nullValueKeys; ContainsValue(null) is still O(1).
Console.WriteLine($"TotalValueCount = {withNull.TotalValueCount}");          // 2（含 null 一份）
                                                                             // 2 (including one null)
Console.WriteLine($"ContainsValue(null) = {withNull.ContainsValue(null)}");  // True
Console.WriteLine($"ValueCount(x) = {withNull.ValueCount("x")}");            // 1

// ---------------------------------------------------------------------------
// 3. 缓存随公开突变 API 同步
// 3. The caches stay in sync with the public mutation APIs
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 缓存随公开突变 API 同步 ===");
Console.WriteLine("=== 3. Caches stay in sync with public mutation APIs ===");

var m = new MultiDictionary<string, int>();
m.AddRange("k", new[] { 1, 2, 3 });
Console.WriteLine($"加 3 份后 TotalValueCount / TotalValueCount after adding 3 copies = {m.TotalValueCount}");   // 3
m.Remove("k", 2);                                                        // 删一份出现
                                                                         // remove one occurrence
Console.WriteLine($"删 1 份后 TotalValueCount / TotalValueCount after removing 1 copy = {m.TotalValueCount}");   // 2
m.Remove("k");                                                           // 删整个键
                                                                         // remove the whole key
Console.WriteLine($"删键后 TotalValueCount / TotalValueCount after removing the key = {m.TotalValueCount}");     // 0

// Clone 经公开 Add 重建，因此克隆体自带一致的双缓存（不会出现「只复制一半簿记」的漂移）。
// Clone rebuilds through the public Add, so the clone carries a consistent dual cache (no drift from "copying only half the bookkeeping").
var src = new MultiDictionary<string, int>();
src.AddRange("g", new[] { 7, 8 });
var clone = src.Clone();
Console.WriteLine($"clone.TotalValueCount = {clone.TotalValueCount}");    // 2
Console.WriteLine($"clone.ContainsValue(8) = {clone.ContainsValue(8)}");  // True

// ❌ 错误写法：经自定义 innerFactory 拿到可变内层后直接改它——缓存不会同步，TotalValueCount / ContainsValue 会失真。
// ❌ Wrong: obtaining a mutable inner collection through a custom innerFactory and modifying it directly — the caches will not sync, and TotalValueCount / ContainsValue become inaccurate.
// ✅ 正确写法：只用 Add / Remove / RemoveRange / 集合运算等公开突变 API。
// ✅ Right: use only the public mutation APIs such as Add / Remove / RemoveRange / set operations.
// 另注：值相等固定为 EqualityComparer<TValue>.Default，不接受自定义值比较器（反向索引与内层集合一致）。
// Also note: value equality is fixed to EqualityComparer<TValue>.Default and does not accept a custom value comparer (the reverse index is consistent with the inner collections).

Console.WriteLine();
Console.WriteLine("Sample.MultiMapDualCache completed.");
