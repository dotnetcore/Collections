// Sample.ReadOnlyCollectionView — 跨 TFM 只读值视图包装（F19）
// 运行：dotnet run --project sample/Sample.ReadOnlyCollectionView
//
// 演示重点：
//   1. 用户可见面是 MultiDictionary 的索引器 / AsReadOnly，而非 internal 的包装类型。
//   2. allowDuplicateValues:false 时内层是 HashSet；只读视图经包装暴露，全 TFM 不抛。
//   3. 视图是**活**的：Count / 枚举反映后续变化，不是快照。
//   4. net451 / net461 上 HashSet 不实现 IReadOnlyCollection，裸转型会 InvalidCastException。
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 只读值视图（内层 HashSet）
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 只读值视图（内层 HashSet）===");

// allowDuplicateValues:false => 内层工厂改用 HashSet，重复值被折叠。
var map = new MultiDictionary<string, int>(allowDuplicateValues: false);
map.Add("k", 1);
map.Add("k", 1);   // 去重：忽略重复
map.Add("k", 2);

// map["k"] 的静态类型是 IReadOnlyCollection<int>；内层 HashSet 经包装暴露，不裸转型。
IReadOnlyCollection<int> view = map["k"];
Console.WriteLine($"view.Count = {view.Count}");                       // 2
Console.WriteLine($"view = {string.Join(",", view.OrderBy(x => x))}"); // 1,2

// ---------------------------------------------------------------------------
// 2. 视图是活的（不是快照）
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 活视图 ===");

map.Add("k", 3);
Console.WriteLine($"Add 后 view.Count = {view.Count}");  // 3：活视图反映变化
map.Remove("k", 1);
Console.WriteLine($"Remove 后 view.Count = {view.Count}"); // 2

// ---------------------------------------------------------------------------
// 3. AsReadOnly 与 ToDictionary 的内层视图
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. AsReadOnly / ToDictionary ===");

// AsReadOnly：整张 map 的活只读视图；内层值集合同样经包装。
var ro = map.AsReadOnly();
Console.WriteLine($"AsReadOnly()[k].Count = {ro["k"].Count}"); // 2
// ToDictionary：外层字典是快照，内层值集合仍是活视图（活）。
var snapshot = map.ToDictionary();
Console.WriteLine($"ToDictionary()[k].Count = {snapshot["k"].Count}"); // 2
map.Add("k", 9);
Console.WriteLine($"Add 后 ToDictionary()[k].Count = {snapshot["k"].Count}"); // 3：内层是活视图

// ---------------------------------------------------------------------------
// 4. 跨 TFM 说明与陷阱
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 跨 TFM 说明 ===");

// 问题域是 net451 / net461：框架的 HashSet<T> 既不静态声明也不在运行时实现
// IReadOnlyCollection<T>，裸转型恰好在这些受支持目标上抛 InvalidCastException。
// 包内统一经 internal 包装（ReadOnlyCollectionView<T>）暴露，使用方透明、全 TFM 安全。
//
// ❌ 错误写法：new ReadOnlyCollectionView<int>(...) —— 该类型是 internal，编译不过。
// ✅ 正确写法：只用 map["k"] / map.AsReadOnly() / IReadOnlyCollection<T>。
// ⚠️ 只读视图是**活**视图，不是快照；要冻结请用 ToSerializableModel（F10）。

Console.WriteLine("只读值视图经包装暴露：全 TFM 可读 Count 与枚举（net451/net461 不再抛）");

Console.WriteLine();
Console.WriteLine("Sample.ReadOnlyCollectionView completed.");
