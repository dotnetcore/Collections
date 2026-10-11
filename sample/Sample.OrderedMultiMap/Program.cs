// Sample.OrderedMultiMap — 有序多重映射 OrderedMultiDictionary<TKey,TValue>（F08）
// 运行：dotnet run --project sample/Sample.OrderedMultiMap
//
// 演示重点：
//   1. 双轴有序：键升序、每个键下的值也升序；枚举顺序 = 键升序 × 键下值升序。
//   2. 名次读针对「键升序 × 键下值升序」的展开序列，单次树下降 O(log n)。
//   3. Count 是**键数**，值总数是 TotalValueCount（口径易混）。
//   4. 枚举是叶链单次扫描（6.6.0 起），每键零堆对象。
using System;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 双轴有序与枚举
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 双轴有序 ===");

var index = new OrderedMultiDictionary<string, int>();
index.Add("beta", 2);
index.Add("alpha", 30);
index.Add("alpha", 10);

// 枚举顺序是「键升序 × 键下值升序」，不是插入序。
foreach (var pair in index)
{
    Console.Write($"({pair.Key},{pair.Value}) ");
}
Console.WriteLine(); // (alpha,10) (alpha,30) (beta,2)

// ---------------------------------------------------------------------------
// 2. 计数口径：Count 是键数
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 计数口径 ===");

Console.WriteLine($"Count（键数）     = {index.Count}");            // 2
Console.WriteLine($"TotalValueCount   = {index.TotalValueCount}");  // 3：值总数
Console.WriteLine($"ValueCount(alpha) = {index.ValueCount("alpha")}"); // 2
Console.WriteLine($"Keys   = {string.Join(",", index.Keys)}");      // alpha,beta
// Values 是扁平序列：键升序，键下值升序（跨键不按全局值序交错）。
Console.WriteLine($"Values = {string.Join(",", index.Values)}");    // 10,30,2

// ---------------------------------------------------------------------------
// 3. 名次读：针对展开序列
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 名次读 ===");

// 展开序列 = (alpha,10) (alpha,30) (beta,2)，rank 0 是最小键的最小值。
var r0 = index.GetByRank(0);
Console.WriteLine($"GetByRank(0)      = ({r0.Key},{r0.Value})"); // (alpha,10)
var med = index.GetMedian();
Console.WriteLine($"GetMedian()       = ({med.Key},{med.Value})"); // (alpha,30)
Console.WriteLine($"GetRank(alpha,30) = {index.GetRank("alpha", 30)}"); // 1
Console.WriteLine($"GetRank(alpha,99) = {index.GetRank("alpha", 99)}"); // -1

// ---------------------------------------------------------------------------
// 4. 值存在性查询与分组导出
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 值查询与 EntrySet ===");

Console.WriteLine($"Contains(alpha,10) = {index.Contains("alpha", 10)}"); // True：某键下是否有该值
Console.WriteLine($"ContainsValue(30)  = {index.ContainsValue(30)}");  // True：任意键下是否有该值（全扫描）
// EntrySet 给出「键 + 该键下全部值」的分组投影。
foreach (var (key, values) in index.EntrySet())
{
    Console.WriteLine($"  {key} -> {string.Join(",", values)}");
}

Console.WriteLine();
Console.WriteLine("Sample.OrderedMultiMap completed.");
