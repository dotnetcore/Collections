// Sample.OrderedMultiMap — 有序多重映射 OrderedMultiDictionary<TKey,TValue>（F08）
// Sample.OrderedMultiMap — ordered multimap OrderedMultiDictionary<TKey,TValue> (F08)
// 运行：dotnet run --project sample/Sample.OrderedMultiMap
// Run: dotnet run --project sample/Sample.OrderedMultiMap
//
// 演示重点：
// Highlights:
//   1. 双轴有序：键升序、每个键下的值也升序；枚举顺序 = 键升序 × 键下值升序。
//   1. Two-axis order: keys ascending and the values under each key also ascending; enumeration order = keys ascending × values within a key ascending.
//   2. 名次读针对「键升序 × 键下值升序」的展开序列，单次树下降 O(log n)。
//   2. Rank reads target the "keys ascending × values within a key ascending" expanded sequence, a single tree descent, O(log n).
//   3. Count 是**键数**，值总数是 TotalValueCount（口径易混）。
//   3. Count is the **key count**; the value total is TotalValueCount (the conventions are easy to confuse).
//   4. 枚举是叶链单次扫描（6.6.0 起），每键零堆对象。
//   4. Enumeration is a single leaf-chain scan (since 6.6.0), with zero heap objects per key.
using System;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 双轴有序与枚举
// 1. Two-axis order and enumeration
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 双轴有序 ===");
Console.WriteLine("=== 1. Two-axis order ===");

var index = new OrderedMultiDictionary<string, int>();
index.Add("beta", 2);
index.Add("alpha", 30);
index.Add("alpha", 10);

// 枚举顺序是「键升序 × 键下值升序」，不是插入序。
// The enumeration order is "keys ascending × values within a key ascending", not insertion order.
foreach (var pair in index)
{
    Console.Write($"({pair.Key},{pair.Value}) ");
}
Console.WriteLine(); // (alpha,10) (alpha,30) (beta,2)

// ---------------------------------------------------------------------------
// 2. 计数口径：Count 是键数
// 2. Counting convention: Count is the key count
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 计数口径 ===");
Console.WriteLine("=== 2. Counting convention ===");

Console.WriteLine($"Count（键数） / Count (key count)     = {index.Count}");            // 2
Console.WriteLine($"TotalValueCount   = {index.TotalValueCount}");  // 3：值总数 / 3: the value total
Console.WriteLine($"ValueCount(alpha) = {index.ValueCount("alpha")}"); // 2
Console.WriteLine($"Keys   = {string.Join(",", index.Keys)}");      // alpha,beta
// Values 是扁平序列：键升序，键下值升序（跨键不按全局值序交错）。
// Values is a flat sequence: keys ascending, values within a key ascending (not interleaved by a global value order across keys).
Console.WriteLine($"Values = {string.Join(",", index.Values)}");    // 10,30,2

// ---------------------------------------------------------------------------
// 3. 名次读：针对展开序列
// 3. Rank reads: targeting the expanded sequence
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 名次读 ===");
Console.WriteLine("=== 3. Rank reads ===");

// 展开序列 = (alpha,10) (alpha,30) (beta,2)，rank 0 是最小键的最小值。
// The expanded sequence = (alpha,10) (alpha,30) (beta,2); rank 0 is the smallest value of the smallest key.
var r0 = index.GetByRank(0);
Console.WriteLine($"GetByRank(0)      = ({r0.Key},{r0.Value})"); // (alpha,10)
var med = index.GetMedian();
Console.WriteLine($"GetMedian()       = ({med.Key},{med.Value})"); // (alpha,30)
Console.WriteLine($"GetRank(alpha,30) = {index.GetRank("alpha", 30)}"); // 1
Console.WriteLine($"GetRank(alpha,99) = {index.GetRank("alpha", 99)}"); // -1

// ---------------------------------------------------------------------------
// 4. 值存在性查询与分组导出
// 4. Value-existence queries and grouped export
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 值查询与 EntrySet ===");
Console.WriteLine("=== 4. Value queries and EntrySet ===");

Console.WriteLine($"Contains(alpha,10) = {index.Contains("alpha", 10)}"); // True：某键下是否有该值 / True: whether the value is under that key
Console.WriteLine($"ContainsValue(30)  = {index.ContainsValue(30)}");  // True：任意键下是否有该值（全扫描）/ True: whether the value is under any key (full scan)
// EntrySet 给出「键 + 该键下全部值」的分组投影。
// EntrySet gives a grouped projection of "key + all its values".
foreach (var (key, values) in index.EntrySet())
{
    Console.WriteLine($"  {key} -> {string.Join(",", values)}");
}

Console.WriteLine();
Console.WriteLine("Sample.OrderedMultiMap completed.");
