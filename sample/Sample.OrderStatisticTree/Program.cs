// Sample.OrderStatisticTree — 顺序统计树引擎（经 OrderedMultiList<T> 观察，6.2.0）
// Sample.OrderStatisticTree — order-statistic tree engine (observed through OrderedMultiList<T>, 6.2.0)
// 运行：dotnet run --project sample/Sample.OrderStatisticTree
// Run: dotnet run --project sample/Sample.OrderStatisticTree
//
// 演示重点：
// Highlights:
//   1. 引擎 OrderStatisticTree<TKey,TPayload> 是 internal，不对外暴露；能力经上层类型可见。
//   1. The engine OrderStatisticTree<TKey,TPayload> is internal and not exposed; its capabilities are visible through the higher-level type.
//   2. 名次读是**单次根到叶下降**（O(log n)），不必物化整条展开序列。
//   2. A rank read is a **single root-to-leaf descent** (O(log n)); there is no need to materialize the whole expanded sequence.
//   3. 名次 / 分位针对「副本展开序列」，不是不同元素的下标。
//   3. Ranks / quantiles target the "copy-expanded sequence", not distinct-element indices.
//   4. 节点缓存子树副本总数，使第 k 个副本与某键的名次都可直接读出。
//   4. Nodes cache subtree copy totals, so both the k-th copy and the rank of a key can be read out directly.
using System;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 名次读：单次下降（O(log n)）
// 1. Rank reads: a single descent (O(log n))
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 名次读 ===");
Console.WriteLine("=== 1. Rank reads ===");

// 内部叶槽存 (1,count=1) (3,count=2) (5,count=1)；节点缓存子树副本总数。
// Internal leaf slots hold (1,count=1) (3,count=2) (5,count=1); nodes cache subtree copy totals.
var shelf = new OrderedMultiList<int> { 1, 3, 3, 5 };
Console.WriteLine($"展开序列 / Expanded sequence = {string.Join(",", shelf)}");     // 1,3,3,5（升序、副本展开）/ 1,3,3,5 (ascending, copies expanded)

// GetByRank：取第 k 个副本；名次针对展开序列（副本计名次）。
// GetByRank: takes the k-th copy; ranks target the expanded sequence (copies count toward rank).
Console.WriteLine($"GetByRank(0) = {shelf.GetByRank(0)}");     // 1
Console.WriteLine($"GetByRank(2) = {shelf.GetByRank(2)}");     // 3（3 的第二份副本）/ 3 (the second copy of 3)
// GetRank：某元素**首份**副本的名次；不存在返回 -1。
// GetRank: the rank of an element's **first** copy; returns -1 when absent.
Console.WriteLine($"GetRank(3)   = {shelf.GetRank(3)}");       // 1
Console.WriteLine($"GetRank(9)   = {shelf.GetRank(9)}");       // -1
// 索引器 this[int] 就是 GetByRank；IndexOf 就是 GetRank（两个名字可互换）。
// The indexer this[int] is GetByRank; IndexOf is GetRank (the two names are interchangeable).
Console.WriteLine($"shelf[2]     = {shelf[2]}");               // 3
Console.WriteLine($"IndexOf(5)   = {shelf.IndexOf(5)}");       // 3

// ---------------------------------------------------------------------------
// 2. 中位与分位：名次读的常用包装
// 2. Median and quantile: common wrappers around rank reads
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 中位与分位 ===");
Console.WriteLine("=== 2. Median and quantile ===");

// GetMedian：取中位名次；偶数副本时取**下中位**（不插值）。
// GetMedian: takes the median rank; for an even copy count it takes the **lower median** (no interpolation).
Console.WriteLine($"GetMedian() = {shelf.GetMedian()}");       // 3（展开序列第 (4-1)/2 = 1 位）/ 3 (position (4-1)/2 = 1 of the expanded sequence)

var dist = new OrderedMultiList<string>();
dist.Add("a", 100);   // 100 份 a / 100 copies of a
dist.Add("z", 1);     // 1 份 z / 1 copy of z
// GetQuantile 用最近秩定义：ceil(q * TotalCount) - 1，永不插值。
// GetQuantile uses the nearest-rank definition: ceil(q * TotalCount) - 1, never interpolating.
Console.WriteLine($"GetQuantile(0.99) = {dist.GetQuantile(0.99)}"); // a（第 100 位）/ a (position 100)
Console.WriteLine($"GetQuantile(1)    = {dist.GetQuantile(1)}");    // z（最后一位）/ z (the last position)

// ---------------------------------------------------------------------------
// 3. 区间 + 不同元素视图 + 名次陷阱
// 3. Range + distinct-element view + rank pitfalls
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 区间与不同元素视图 ===");
Console.WriteLine("=== 3. Range and distinct-element view ===");

Console.WriteLine($"GetRange(1,3)  = {string.Join(",", shelf.GetRange(1, 3))}"); // 1,3,3：闭区间、副本展开 / 1,3,3: closed range, copies expanded
// DistinctItems 只列不同元素（忽略副本数），升序。
// DistinctItems lists only distinct elements (ignoring copy counts), ascending.
Console.WriteLine($"DistinctItems  = {string.Join(",", shelf.DistinctItems())}"); // 1,3,5
// EntrySet 是「元素 + 副本数」的紧凑直方图，项数 == DistinctCount。
// EntrySet is a compact histogram of "element + copy count"; its item count == DistinctCount.
foreach (var (item, count) in shelf.EntrySet())
{
    Console.WriteLine($"  EntrySet: {item} x {count}");
}

// ⚠️ 陷阱：GetByRank(0) 返回的不是「第一个不同元素」，而是**最小元素的第一份副本**。
// ⚠️ Pitfall: GetByRank(0) does not return "the first distinct element" but the **first copy of the smallest element**.
// 要不同元素请用 DistinctItems / EntrySet（名次读针对副本展开序列）。
// For distinct elements use DistinctItems / EntrySet (rank reads target the copy-expanded sequence).

Console.WriteLine();
Console.WriteLine("Sample.OrderStatisticTree completed.");
