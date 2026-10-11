// Sample.OrderStatisticTree — 顺序统计树引擎（经 OrderedMultiList<T> 观察，F07）
// 运行：dotnet run --project sample/Sample.OrderStatisticTree
//
// 演示重点：
//   1. 引擎 OrderStatisticTree<TKey,TPayload> 是 internal，不对外暴露；能力经上层类型可见。
//   2. 名次读是**单次根到叶下降**（O(log n)），不必物化整条展开序列。
//   3. 名次 / 分位针对「副本展开序列」，不是不同元素的下标。
//   4. 节点缓存子树副本总数，使第 k 个副本与某键的名次都可直接读出。
using System;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 名次读：单次下降（O(log n)）
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 名次读 ===");

// 内部叶槽存 (1,count=1) (3,count=2) (5,count=1)；节点缓存子树副本总数。
var shelf = new OrderedMultiList<int> { 1, 3, 3, 5 };
Console.WriteLine($"展开序列 = {string.Join(",", shelf)}");     // 1,3,3,5（升序、副本展开）

// GetByRank：取第 k 个副本；名次针对展开序列（副本计名次）。
Console.WriteLine($"GetByRank(0) = {shelf.GetByRank(0)}");     // 1
Console.WriteLine($"GetByRank(2) = {shelf.GetByRank(2)}");     // 3（3 的第二份副本）
// GetRank：某元素**首份**副本的名次；不存在返回 -1。
Console.WriteLine($"GetRank(3)   = {shelf.GetRank(3)}");       // 1
Console.WriteLine($"GetRank(9)   = {shelf.GetRank(9)}");       // -1
// 索引器 this[int] 就是 GetByRank；IndexOf 就是 GetRank（两个名字可互换）。
Console.WriteLine($"shelf[2]     = {shelf[2]}");               // 3
Console.WriteLine($"IndexOf(5)   = {shelf.IndexOf(5)}");       // 3

// ---------------------------------------------------------------------------
// 2. 中位与分位：名次读的常用包装
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 中位与分位 ===");

// GetMedian：取中位名次；偶数副本时取**下中位**（不插值）。
Console.WriteLine($"GetMedian() = {shelf.GetMedian()}");       // 3（展开序列第 (4-1)/2 = 1 位）

var dist = new OrderedMultiList<string>();
dist.Add("a", 100);   // 100 份 a
dist.Add("z", 1);     // 1 份 z
// GetQuantile 用最近秩定义：ceil(q * TotalCount) - 1，永不插值。
Console.WriteLine($"GetQuantile(0.99) = {dist.GetQuantile(0.99)}"); // a（第 100 位）
Console.WriteLine($"GetQuantile(1)    = {dist.GetQuantile(1)}");    // z（最后一位）

// ---------------------------------------------------------------------------
// 3. 区间 + 不同元素视图 + 名次陷阱
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 区间与不同元素视图 ===");

Console.WriteLine($"GetRange(1,3)  = {string.Join(",", shelf.GetRange(1, 3))}"); // 1,3,3：闭区间、副本展开
// DistinctItems 只列不同元素（忽略副本数），升序。
Console.WriteLine($"DistinctItems  = {string.Join(",", shelf.DistinctItems())}"); // 1,3,5
// EntrySet 是「元素 + 副本数」的紧凑直方图，项数 == DistinctCount。
foreach (var (item, count) in shelf.EntrySet())
{
    Console.WriteLine($"  EntrySet: {item} x {count}");
}

// ⚠️ 陷阱：GetByRank(0) 返回的不是「第一个不同元素」，而是**最小元素的第一份副本**。
// 要不同元素请用 DistinctItems / EntrySet（名次读针对副本展开序列）。

Console.WriteLine();
Console.WriteLine("Sample.OrderStatisticTree completed.");
