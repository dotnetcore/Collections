// Sample.LiveReverseView — 多重映射活反查视图 MultiDictionary.AsReverse()（F18）
// 运行：dotnet run --project sample/Sample.LiveReverseView
//
// 演示重点：
//   1. AsReverse() 返回**活的只读倒排视图**：值 -> 持有它的键集合。
//   2. 视图直接读源已维护的反向索引，构建零成本、每读 O(1)。
//   3. 源一变视图即变；不暴露突变成员。
//   4. 与 ReverseMultiDictionary 快照互为两半，任意时刻一致（见 F17）。
using System;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 活视图：源一变视图即变
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 活视图随源变化 ===");

var map = new MultiDictionary<string, int>();
map.Add("orders", 1001);
map.Add("customers", 1001);

// 视图无拷贝：读的是源已维护的反向索引（_valueIndex）。
var inverted = map.AsReverse();
Console.WriteLine($"inverted[1001] = {string.Join(",", inverted[1001].OrderBy(k => k))}"); // customers,orders
Console.WriteLine($"inverted[1001].Count = {inverted[1001].Count}"); // 2

map.Add("invoices", 1001);   // 源 Add 后视图立即可见
Console.WriteLine($"加 invoices 后 inverted[1001].Count = {inverted[1001].Count}"); // 3

// ---------------------------------------------------------------------------
// 2. 缺值语义与折叠多重性
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 缺值与折叠多重性 ===");

// 缺值返回空集合，不抛。
Console.WriteLine($"inverted[9999].Count = {inverted[9999].Count}"); // 0

// 倒排折叠多重性：同键存同值多次，只列一次键。
map.Add("orders", 1001);   // orders 再存一份 1001
int ordersListed = inverted[1001].Where(k => k == "orders").Count();
Console.WriteLine($"orders 重复后仍只列一次 = {ordersListed}"); // 1

// ---------------------------------------------------------------------------
// 3. 只读安全：不暴露突变成员
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 只读安全 ===");

// 视图的静态类型是 IReadOnlyDictionary，没有 Add / Remove 等突变成员。
Console.WriteLine($"视图类型 = {inverted.GetType().Name}（对外只见 IReadOnlyDictionary）");
Console.WriteLine($"视图键数 = {inverted.Count}"); // 不同值数

// ---------------------------------------------------------------------------
// 4. null 值：作为最后枚举的 null 条目浮现
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. null 值条目 ===");

var withNull = new MultiDictionary<string, int?>();
withNull.Add("k", null);
var nullView = withNull.AsReverse();
Console.WriteLine($"nullView.ContainsKey(null) = {nullView.ContainsKey(null)}"); // True
Console.WriteLine($"nullView[null].Count       = {nullView[null].Count}");       // 1

Console.WriteLine();
Console.WriteLine("Sample.LiveReverseView completed.");
