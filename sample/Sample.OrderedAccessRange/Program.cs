// Sample.OrderedAccessRange — 有序访问与区间查询（F06）
// Sample.OrderedAccessRange — ordered access and range queries (F06)
// 运行：dotnet run --project sample/Sample.OrderedAccessRange
// Run: dotnet run --project sample/Sample.OrderedAccessRange
//
// 演示重点：
// Highlights:
//   1. 极值：GetFirst / GetLast 是单次最左 / 最右下降，O(log n)。
//   1. Extremes: GetFirst / GetLast are a single leftmost / rightmost descent, O(log n).
//   2. 区间：GetRange(from, to) 两端**闭**；另一重载可独立控制两端开闭。
//   2. Ranges: GetRange(from, to) is **closed** at both ends; the other overload controls each endpoint's inclusivity independently.
//   3. 区间结果是**副本展开**序列（按副本数计长），沿叶链扫描不重新入分支。
//   3. A range result is a **copy-expanded** sequence (length counted in copies); it scans along the leaf chain without re-entering branches.
//   4. 空集合调极值抛 InvalidOperationException；缺键索引器返回空集合。
//   4. Calling an extreme on an empty collection throws InvalidOperationException; a missing-key indexer returns an empty collection.
//   5. OrderedMultiDictionary 的键升序、每键下值升序（不是全局值序）。
//   5. OrderedMultiDictionary has keys ascending and values within each key ascending (not a global value order).
using System;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 极值：即取即用
// 1. Extremes: take them immediately
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 极值 ===");
Console.WriteLine("=== 1. Extremes ===");

var shelf = new OrderedMultiList<string> { "mug", "bean", "bean", "tea" };
Console.WriteLine($"GetFirst() = {shelf.GetFirst()}"); // bean：最小元素 / bean: the smallest element
Console.WriteLine($"GetLast()  = {shelf.GetLast()}");  // tea：最大元素 / tea: the largest element
// 极值不是「排序后取首尾」，而是树上的单次最左 / 最右下降，O(log n)。
// An extreme is not "sort then take the ends" but a single leftmost / rightmost descent on the tree, O(log n).

// ---------------------------------------------------------------------------
// 2. 区间：闭区间与半开区间
// 2. Ranges: closed and half-open
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 区间查询 ===");
Console.WriteLine("=== 2. Range queries ===");

// GetRange(from, to)：两端**闭**，边界语义由注入的比较器决定（此处默认字符串比较）。
// GetRange(from, to): **closed** at both ends; the boundary semantics are defined by the injected comparer (default string comparison here).
Console.WriteLine($"GetRange(a,n)             = {string.Join(",", shelf.GetRange("a", "n"))}");             // bean,bean,mug
// 另一重载可独立控制两端开闭：inclusiveFrom=false 排除下界，inclusiveTo=true 包含上界。
// The other overload controls each endpoint's inclusivity independently: inclusiveFrom=false excludes the lower bound, inclusiveTo=true includes the upper bound.
Console.WriteLine($"GetRange(a,n,false,true)  = {string.Join(",", shelf.GetRange("a", "n", false, true))}");  // bean,bean,mug（"a" 本就不在集合内 / "a" was never in the collection)
Console.WriteLine($"GetRange(m,tea,true,false)= {string.Join(",", shelf.GetRange("m", "tea", true, false))}"); // mug
// 区间结果是副本展开：bean 有两份就产出两次；长度按副本数计。
// A range result is copy-expanded: bean with two copies is produced twice; its length is counted in copies.

// ---------------------------------------------------------------------------
// 3. 空集合：极值抛异常，区间返回空序列
// 3. Empty collection: extremes throw, ranges return an empty sequence
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 空集合行为 ===");
Console.WriteLine("=== 3. Empty-collection behavior ===");

var empty = new OrderedMultiList<string>();
try
{
    empty.GetFirst();
}
catch (InvalidOperationException)
{
    // ⚠️ 陷阱：空集合没有最小元素，GetFirst / GetLast 抛 InvalidOperationException。
    // ⚠️ Pitfall: an empty collection has no smallest element, so GetFirst / GetLast throw InvalidOperationException.
    // 正确做法：先判 TotalCount / DistinctCount 再调极值。
    // The right approach: check TotalCount / DistinctCount before calling an extreme.
    Console.WriteLine("空集合 GetFirst 抛 InvalidOperationException");
    Console.WriteLine("Empty-collection GetFirst throws InvalidOperationException");
}

Console.WriteLine($"空集合 GetRange 长度 / Empty-collection GetRange length = {empty.GetRange("a", "z").Count()}"); // 0：返回空序列，不抛 / 0: returns an empty sequence, does not throw

// ---------------------------------------------------------------------------
// 4. OrderedMultiDictionary：键升序、键下值升序
// 4. OrderedMultiDictionary: keys ascending, values within each key ascending
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 有序映射的键序 / 值序 ===");
Console.WriteLine("=== 4. Ordered map key order / value order ===");

var index = new OrderedMultiDictionary<string, int>();
index.Add("beta", 2);
index.Add("alpha", 30);
index.Add("alpha", 10);

// Keys 按键升序枚举（alpha < beta）。
// Keys enumerates in ascending key order (alpha < beta).
Console.WriteLine($"Keys = {string.Join(",", index.Keys)}"); // alpha,beta
// 每个键下的值升序（10 < 30）；不同键的值**不会**按全局值序交错。
// Values within each key are ascending (10 < 30); values across different keys are **not** interleaved by a global value order.
foreach (var key in index.Keys)
{
    Console.WriteLine($"  {key} -> {string.Join(",", index[key])}");
}
// alpha -> 10,30 ; beta -> 2
// 缺键索引器返回空集合，不抛。
// A missing-key indexer returns an empty collection, not throwing.
Console.WriteLine($"index[missing].Count = {index["missing"].Count}"); // 0

Console.WriteLine();
Console.WriteLine("Sample.OrderedAccessRange completed.");
