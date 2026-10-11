// Sample.FrequencyPriorityBag — 频次优先袋（F23）
// 运行：dotnet run --project sample/Sample.FrequencyPriorityBag
//
// 演示重点：
//   1. FrequencyPriorityBag<T> 知道哪个元素出现最多，并能在 O(log n) 交出来——Top-K 的天然形状。
//      PeekMost 不删，PopMost 删一份并返回；连弹即降频序列。
//   2. 平局策略确定、FIFO 味：同计数时，**更早**达到当前计数的元素先弹出。
//   3. 袋语义镜像 MultiList<T>：枚举副本展开但**无序**；null 为一等元素；空袋 PeekMost/PopMost 抛。
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. Top-K：连弹 PopMost 得到降频序列
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. Top-K ===");

var bag = new FrequencyPriorityBag<string>();
bag.AddRange(new[] { "a", "b", "b", "c", "b", "c" });   // b x3, c x2, a x1

Console.WriteLine($"TotalCount    = {bag.TotalCount}");      // 6：副本数
Console.WriteLine($"DistinctCount = {bag.DistinctCount}");   // 3：不同元素数
Console.WriteLine($"PeekMost()    = {bag.PeekMost()}");      // b：最高频，且不删

// 连弹 3 次即 Top-3（降频序）。同频平局见第 2 节：更早达到当前计数者先。
var topK = new List<string>();
for (var i = 0; i < 3; i++)
{
    topK.Add(bag.PopMost());
}

Console.WriteLine($"Top-3 = [{string.Join(", ", topK)}]");   // b, c, b

// ---------------------------------------------------------------------------
// 2. 平局：更早达到当前计数者先弹出
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 平局策略（FIFO 味）===");

var tie = new FrequencyPriorityBag<string>();
tie.Add("first");    // first 达到计数 1
tie.Add("second");   // second 达到计数 1
tie.Add("second");   // second 达到计数 2（较早）
tie.Add("first");    // first 达到计数 2（较晚）

// 两者当前计数都是 2；second 更早到达该计数，故先弹出。
Console.WriteLine($"第一次 PopMost = {tie.PopMost()}");   // second
Console.WriteLine($"第二次 PopMost = {tie.PopMost()}");   // first

// ---------------------------------------------------------------------------
// 3. 枚举副本展开但无序
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 枚举（副本展开、无序）===");

var en = new FrequencyPriorityBag<string>();
en.AddRange(new[] { "x", "x", "y" });

Console.WriteLine($"TotalCount = {en.TotalCount}");       // 3
// 枚举是副本展开：x 出现两次、y 一次。顺序无契约，这里排序后打印以获得确定性输出。
var expanded = en.OrderBy(v => v, StringComparer.Ordinal).ToList();
Console.WriteLine($"枚举（排序展示）= [{string.Join(", ", expanded)}]");  // [x, x, y]
// ❌ 错误写法：依赖枚举顺序 —— 枚举只是副本展开，不按频次也不按插入序。

// ---------------------------------------------------------------------------
// 4. null 为一等元素
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. null 支持 ===");

var withNull = new FrequencyPriorityBag<string?>();
withNull.Add(null);          // null 有专用桶 + 自己的平局戳
withNull.Add(null);
withNull.Add("z");

Console.WriteLine($"CountOf(null) = {withNull.CountOf(null)}");   // 2
Console.WriteLine($"DistinctCount = {withNull.DistinctCount}");   // 2：null 算一个不同元素
// PeekMost 返回 default(string?) 即 null，这里显式标注以便阅读。
string? most = withNull.PeekMost();
Console.WriteLine($"PeekMost = {most ?? "<null>"}");              // <null>

// ---------------------------------------------------------------------------
// 5. 空袋与 Try 形
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 5. 空袋与 Try 形 ===");

var empty = new FrequencyPriorityBag<int>();
Console.WriteLine($"TryPeekMost = {empty.TryPeekMost(out _)}");   // False：不抛
Console.WriteLine($"TryPopMost  = {empty.TryPopMost(out _)}");    // False：不抛

try
{
    empty.PeekMost();   // 空袋 PeekMost 抛 InvalidOperationException
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"空袋 PeekMost 抛：{ex.Message}");
}

// ⚠️ query-once 场景不如排一次 MultiList；要有序枚举用 OrderedMultiList。
// 更新只动频次索引 O(1) 并标堆脏，下次优先查询才 O(n) 重建堆——写热读簇最划算。

Console.WriteLine();
Console.WriteLine("Sample.FrequencyPriorityBag completed.");
