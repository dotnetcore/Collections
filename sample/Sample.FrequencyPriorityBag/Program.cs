// Sample.FrequencyPriorityBag — 频次优先袋（6.4.0）
// Sample.FrequencyPriorityBag — frequency-priority bag (6.4.0)
// 运行：dotnet run --project sample/Sample.FrequencyPriorityBag
// Run: dotnet run --project sample/Sample.FrequencyPriorityBag
//
// 演示重点：
// Highlights:
//   1. FrequencyPriorityBag<T> 知道哪个元素出现最多，并能在 O(log n) 交出来——Top-K 的天然形状。
//   1. FrequencyPriorityBag<T> knows which element appears most and can hand it over in O(log n) — the natural shape of Top-K.
//      PeekMost 不删，PopMost 删一份并返回；连弹即降频序列。
//      PeekMost does not remove; PopMost removes one copy and returns it; popping repeatedly yields a descending-frequency sequence.
//   2. 平局策略确定、FIFO 味：同计数时，**更早**达到当前计数的元素先弹出。
//   2. A deterministic, FIFO-flavored tie-break: on equal counts, the element that reached its current count **earlier** pops first.
//   3. 袋语义镜像 MultiList<T>：枚举副本展开但**无序**；null 为一等元素；空袋 PeekMost/PopMost 抛。
//   3. Bag semantics mirror MultiList<T>: enumeration expands copies but is **unordered**; null is a first-class element; an empty bag's PeekMost/PopMost throw.
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. Top-K：连弹 PopMost 得到降频序列
// 1. Top-K: repeated PopMost yields a descending-frequency sequence
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. Top-K ===");

var bag = new FrequencyPriorityBag<string>();
bag.AddRange(new[] { "a", "b", "b", "c", "b", "c" });   // b x3, c x2, a x1

Console.WriteLine($"TotalCount    = {bag.TotalCount}");      // 6：副本数
                                                             // 6: number of copies
Console.WriteLine($"DistinctCount = {bag.DistinctCount}");   // 3：不同元素数
                                                             // 3: number of distinct elements
Console.WriteLine($"PeekMost()    = {bag.PeekMost()}");      // b：最高频，且不删
                                                             // b: the most frequent, and not removed

// 连弹 3 次即 Top-3（降频序）。同频平局见第 2 节：更早达到当前计数者先。
// Popping 3 times gives the Top-3 (descending-frequency order). For equal-frequency ties see section 2: the one reaching its current count earlier goes first.
var topK = new List<string>();
for (var i = 0; i < 3; i++)
{
    topK.Add(bag.PopMost());
}

Console.WriteLine($"Top-3 = [{string.Join(", ", topK)}]");   // b, c, b

// ---------------------------------------------------------------------------
// 2. 平局：更早达到当前计数者先弹出
// 2. Tie-break: the one reaching its current count earlier pops first
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 平局策略（FIFO 味）===");
Console.WriteLine("=== 2. Tie-breaking policy (FIFO-flavored) ===");

var tie = new FrequencyPriorityBag<string>();
tie.Add("first");    // first 达到计数 1
                     // first reaches count 1
tie.Add("second");   // second 达到计数 1
                     // second reaches count 1
tie.Add("second");   // second 达到计数 2（较早）
                     // second reaches count 2 (earlier)
tie.Add("first");    // first 达到计数 2（较晚）
                     // first reaches count 2 (later)

// 两者当前计数都是 2；second 更早到达该计数，故先弹出。
// Both currently have count 2; second reached that count earlier, so it pops first.
Console.WriteLine($"第一次 PopMost / first PopMost = {tie.PopMost()}");   // second
Console.WriteLine($"第二次 PopMost / second PopMost = {tie.PopMost()}");   // first

// ---------------------------------------------------------------------------
// 3. 枚举副本展开但无序
// 3. Enumeration expands copies but is unordered
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 枚举（副本展开、无序）===");
Console.WriteLine("=== 3. Enumeration (copies expanded, unordered) ===");

var en = new FrequencyPriorityBag<string>();
en.AddRange(new[] { "x", "x", "y" });

Console.WriteLine($"TotalCount = {en.TotalCount}");       // 3
// 枚举是副本展开：x 出现两次、y 一次。顺序无契约，这里排序后打印以获得确定性输出。
// Enumeration expands copies: x appears twice, y once. The order has no contract, so we sort before printing to get deterministic output.
var expanded = en.OrderBy(v => v, StringComparer.Ordinal).ToList();
Console.WriteLine($"枚举（排序展示） / Enumerated (sorted for display) = [{string.Join(", ", expanded)}]");  // [x, x, y]
// ❌ 错误写法：依赖枚举顺序 —— 枚举只是副本展开，不按频次也不按插入序。
// ❌ Wrong usage: relying on the enumeration order — enumeration is only copy expansion, neither by frequency nor by insertion order.

// ---------------------------------------------------------------------------
// 4. null 为一等元素
// 4. null as a first-class element
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. null 支持 ===");
Console.WriteLine("=== 4. null support ===");

var withNull = new FrequencyPriorityBag<string?>();
withNull.Add(null);          // null 有专用桶 + 自己的平局戳
                             // null has a dedicated bucket + its own tie-break stamp
withNull.Add(null);
withNull.Add("z");

Console.WriteLine($"CountOf(null) = {withNull.CountOf(null)}");   // 2
Console.WriteLine($"DistinctCount = {withNull.DistinctCount}");   // 2：null 算一个不同元素
                                                                   // 2: null counts as one distinct element
// PeekMost 返回 default(string?) 即 null，这里显式标注以便阅读。
// PeekMost returns default(string?), i.e. null; it is annotated explicitly here for readability.
string? most = withNull.PeekMost();
Console.WriteLine($"PeekMost = {most ?? "<null>"}");              // <null>

// ---------------------------------------------------------------------------
// 5. 空袋与 Try 形
// 5. Empty bag and Try forms
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 5. 空袋与 Try 形 ===");
Console.WriteLine("=== 5. Empty bag and Try forms ===");

var empty = new FrequencyPriorityBag<int>();
Console.WriteLine($"TryPeekMost = {empty.TryPeekMost(out _)}");   // False：不抛
                                                                  // False: does not throw
Console.WriteLine($"TryPopMost  = {empty.TryPopMost(out _)}");    // False：不抛
                                                                  // False: does not throw

try
{
    empty.PeekMost();   // 空袋 PeekMost 抛 InvalidOperationException
                        // an empty bag's PeekMost throws InvalidOperationException
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"空袋 PeekMost 抛 / empty-bag PeekMost throws: {ex.Message}");
}

// ⚠️ query-once 场景不如排一次 MultiList；要有序枚举用 OrderedMultiList。
// ⚠️ For query-once scenarios, sorting a MultiList once beats this; for ordered enumeration use OrderedMultiList.
// 更新只动频次索引 O(1) 并标堆脏，下次优先查询才 O(n) 重建堆——写热读簇最划算。
// An update only touches the frequency index in O(1) and marks the heap dirty; the heap is rebuilt in O(n) only on the next priority query — most worthwhile for hot writes and clustered reads.

Console.WriteLine();
Console.WriteLine("Sample.FrequencyPriorityBag completed.");
