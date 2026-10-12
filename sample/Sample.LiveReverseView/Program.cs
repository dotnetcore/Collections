// Sample.LiveReverseView — 多重映射活反查视图 MultiDictionary.AsReverse()（6.3.0）
// Sample.LiveReverseView — live reverse view of a multimap, MultiDictionary.AsReverse() (6.3.0)
// 运行：dotnet run --project sample/Sample.LiveReverseView
// Run: dotnet run --project sample/Sample.LiveReverseView
//
// 演示重点：
// Highlights:
//   1. AsReverse() 返回**活的只读倒排视图**：值 -> 持有它的键集合。
//   1. AsReverse() returns a **live read-only inverted view**: value -> the set of keys holding it.
//   2. 视图直接读源已维护的反向索引，构建零成本、每读 O(1)。
//   2. The view reads the reverse index the source already maintains directly: zero-cost to build, O(1) per read.
//   3. 源一变视图即变；不暴露突变成员。
//   3. The view changes as soon as the source does; no mutating members are exposed.
//   4. 与 ReverseMultiDictionary 快照互为两半，任意时刻一致（见 6.3.0）。
//   4. It is one half of the two inverted entry points alongside the ReverseMultiDictionary snapshot, consistent at any moment (see 6.3.0).
using System;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 活视图：源一变视图即变
// 1. Live view: it changes as soon as the source does
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 活视图随源变化 ===");
Console.WriteLine("=== 1. Live view changes with the source ===");

var map = new MultiDictionary<string, int>();
map.Add("orders", 1001);
map.Add("customers", 1001);

// 视图无拷贝：读的是源已维护的反向索引（_valueIndex）。
// The view makes no copy: it reads the reverse index the source already maintains (_valueIndex).
var inverted = map.AsReverse();
Console.WriteLine($"inverted[1001] = {string.Join(",", inverted[1001].OrderBy(k => k))}"); // customers,orders
Console.WriteLine($"inverted[1001].Count = {inverted[1001].Count}"); // 2

map.Add("invoices", 1001);   // 源 Add 后视图立即可见 / the view is immediately visible after a source Add
Console.WriteLine($"加 invoices 后 inverted[1001].Count / inverted[1001].Count after adding invoices = {inverted[1001].Count}"); // 3

// ---------------------------------------------------------------------------
// 2. 缺值语义与折叠多重性
// 2. Missing-value semantics and collapsed multiplicity
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 缺值与折叠多重性 ===");
Console.WriteLine("=== 2. Missing values and collapsed multiplicity ===");

// 缺值返回空集合，不抛。
// A missing value returns an empty collection without throwing.
Console.WriteLine($"inverted[9999].Count = {inverted[9999].Count}"); // 0

// 倒排折叠多重性：同键存同值多次，只列一次键。
// Inverted collapsed multiplicity: storing the same value multiple times under one key lists the key only once.
map.Add("orders", 1001);   // orders 再存一份 1001 / orders stores another copy of 1001
int ordersListed = inverted[1001].Where(k => k == "orders").Count();
Console.WriteLine($"orders 重复后仍只列一次 / orders still listed once after a duplicate = {ordersListed}"); // 1

// ---------------------------------------------------------------------------
// 3. 只读安全：不暴露突变成员
// 3. Read-only safety: no mutating members exposed
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 只读安全 ===");
Console.WriteLine("=== 3. Read-only safety ===");

// 视图的静态类型是 IReadOnlyDictionary，没有 Add / Remove 等突变成员。
// The view's static type is IReadOnlyDictionary, with no mutating members such as Add / Remove.
Console.WriteLine($"视图类型 / View type = {inverted.GetType().Name}（对外只见 IReadOnlyDictionary / externally only IReadOnlyDictionary is visible）");
Console.WriteLine($"视图键数 / View key count = {inverted.Count}"); // 不同值数 / distinct value count

// ---------------------------------------------------------------------------
// 4. null 值：作为最后枚举的 null 条目浮现
// 4. Null value: surfaces as a null entry enumerated last
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. null 值条目 ===");
Console.WriteLine("=== 4. Null-value entry ===");

var withNull = new MultiDictionary<string, int?>();
withNull.Add("k", null);
var nullView = withNull.AsReverse();
Console.WriteLine($"nullView.ContainsKey(null) = {nullView.ContainsKey(null)}"); // True
Console.WriteLine($"nullView[null].Count       = {nullView[null].Count}");       // 1

Console.WriteLine();
Console.WriteLine("Sample.LiveReverseView completed.");
