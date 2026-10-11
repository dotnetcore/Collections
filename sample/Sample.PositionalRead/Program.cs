// Sample.PositionalRead — 名次与分位读（F24）
// 运行：dotnet run --project sample/Sample.PositionalRead
//
// 演示重点：
//   1. 名次针对**副本展开**的有序序列，不是不同元素下标：{1,3,3,5} 的 rank 2 是第二个 3。
//   2. GetByRank / GetRank（列表）与 GetByRank / GetRank / GetMedian / GetQuantile（映射）全部 O(log n)：
//      单次根到叶下降，不物化展开序列。
//   3. 映射展开序列 = 键升序 × 每键值升序；GetMedian / GetQuantile 是**次序统计**，不做数值插值。
using System;
using System.Collections.Generic;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// 便于打印 KeyValuePair 的小工具（本包没有专门的重载，手动格式化）。
static string Pair(KeyValuePair<string, int> p) => $"({p.Key}, {p.Value})";

// ---------------------------------------------------------------------------
// 1. 列表名次：展开序列下标即名次
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 列表名次 ===");

var shelf = new OrderedMultiList<int> { 1, 3, 3, 5 };   // 展开序列：1, 3, 3, 5
Console.WriteLine($"TotalCount    = {shelf.TotalCount}");     // 4：副本数
Console.WriteLine($"DistinctCount = {shelf.DistinctCount}");  // 3：不同元素数

// GetByRank(rank) 取展开序列第 rank 个；rank 2 是第二个 3，不是「不同元素下标 2」。
Console.WriteLine($"GetByRank(0) = {shelf.GetByRank(0)}");    // 1
Console.WriteLine($"GetByRank(2) = {shelf.GetByRank(2)}");    // 3

// GetRank(item) 反查名次：返回**首份副本**的位置。
Console.WriteLine($"GetRank(3) = {shelf.GetRank(3)}");        // 1：第一个 3 的位置
// 缺失元素返回 -1（与实现一致，不抛）。
Console.WriteLine($"GetRank(99) = {shelf.GetRank(99)}");      // -1

// this[i] 等价 GetByRank(i)；IndexOf 等价 GetRank（列表侧别名）。
Console.WriteLine($"this[2] = {shelf[2]}");                   // 3
Console.WriteLine($"IndexOf(3) = {shelf.IndexOf(3)}");        // 1

// 中位 / 分位也是次序统计：4 份副本取第 (4-1)/2 = 1 个 => 3。
Console.WriteLine($"GetMedian() = {shelf.GetMedian()}");      // 3
Console.WriteLine($"GetQuantile(0.5) = {shelf.GetQuantile(0.5)}"); // 3（nearest-rank）

// ---------------------------------------------------------------------------
// 2. 映射名次与分位
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 映射名次与分位 ===");

var index = new OrderedMultiDictionary<string, int>();
index.Add("beta", 2);
index.Add("alpha", 30);
index.Add("alpha", 10);
// 展开序列 = 键升序 × 每键值升序：(alpha,10) (alpha,30) (beta,2)
Console.WriteLine($"TotalValueCount = {index.TotalValueCount}");   // 3：值数（O(1)，树根缓存）
Console.WriteLine($"KeyCount        = {index.KeyCount}");          // 2：键数

// rank 0 是最小键的最小值。
Console.WriteLine($"GetByRank(0) = {Pair(index.GetByRank(0))}");   // (alpha, 10)

// 中位是次序中位（真实元素），不是数值平均。
Console.WriteLine($"GetMedian() = {Pair(index.GetMedian())}");     // (alpha, 30)

// 反查某个 (key, value) 对的名次；缺失返回 -1。
Console.WriteLine($"GetRank(alpha, 30) = {index.GetRank("alpha", 30)}");  // 1
Console.WriteLine($"GetRank(alpha, 99) = {index.GetRank("alpha", 99)}");  // -1

// 次序分位：q ∈ [0,1]，0.0 得最小对，0.99 得最大对；从不插值。
Console.WriteLine($"GetQuantile(0.0)  = {Pair(index.GetQuantile(0.0))}");   // (alpha, 10)
Console.WriteLine($"GetQuantile(0.99) = {Pair(index.GetQuantile(0.99))}");  // (beta, 2)

// ---------------------------------------------------------------------------
// 3. 次序统计，不是数值插值
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 次序统计 vs 数值插值 ===");

// ⚠️ GetQuantile 返回**真实存储的**元素，不会在两个值之间插值。
//    若需要 P95 之类的数值插值分位，请自行物化后计算——本 API 只给次序统计。
Console.WriteLine($"GetQuantile(0.5) 与 GetMedian 一致 = {Pair(index.GetQuantile(0.5))}"); // (alpha, 30)

// 越界 rank 抛 ArgumentOutOfRangeException；空集合读中位 / 分位抛 InvalidOperationException。
try
{
    index.GetByRank(3);   // TotalValueCount = 3，合法 rank 是 0..2
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"GetByRank(3) 越界抛：{ex.ParamName}");     // rank
}

// 名次针对全局 (键, 值) 对序列，不是单键内的位置。
// ❌ 错误写法：把 rank 当不同元素下标 —— 同值多份会占连续名次（见第 1 节 rank 1/2 都是 3）。

Console.WriteLine();
Console.WriteLine("Sample.PositionalRead completed.");
