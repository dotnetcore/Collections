// Sample.ReadOnlyCollectionView — 跨 TFM 只读值视图包装（6.4.0）
// Sample.ReadOnlyCollectionView — cross-TFM read-only value-view wrapper (6.4.0)
// 运行：dotnet run --project sample/Sample.ReadOnlyCollectionView
// Run: dotnet run --project sample/Sample.ReadOnlyCollectionView
//
// 演示重点：
// Highlights:
//   1. 用户可见面是 MultiDictionary 的索引器 / AsReadOnly，而非 internal 的包装类型。
//   1. The user-visible surface is MultiDictionary's indexer / AsReadOnly, not the internal wrapper type.
//   2. allowDuplicateValues:false 时内层是 HashSet；只读视图经包装暴露，全 TFM 不抛。
//   2. With allowDuplicateValues:false the inner collection is a HashSet; the read-only view is exposed through the wrapper, so it does not throw on any TFM.
//   3. 视图是**活**的：Count / 枚举反映后续变化，不是快照。
//   3. The view is **live**: Count / enumeration reflect later changes, it is not a snapshot.
//   4. net451 / net461 上 HashSet 不实现 IReadOnlyCollection，裸转型会 InvalidCastException。
//   4. On net451 / net461 HashSet does not implement IReadOnlyCollection, so a raw cast throws InvalidCastException.
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 只读值视图（内层 HashSet）
// 1. Read-only value view (inner HashSet)
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 只读值视图（内层 HashSet）===");
Console.WriteLine("=== 1. Read-only value view (inner HashSet) ===");

// allowDuplicateValues:false => 内层工厂改用 HashSet，重复值被折叠。
// allowDuplicateValues:false => the inner factory switches to HashSet, and duplicate values are collapsed.
var map = new MultiDictionary<string, int>(allowDuplicateValues: false);
map.Add("k", 1);
map.Add("k", 1);   // 去重：忽略重复 / deduplicated: the duplicate is ignored
map.Add("k", 2);

// map["k"] 的静态类型是 IReadOnlyCollection<int>；内层 HashSet 经包装暴露，不裸转型。
// The static type of map["k"] is IReadOnlyCollection<int>; the inner HashSet is exposed through the wrapper, with no raw cast.
IReadOnlyCollection<int> view = map["k"];
Console.WriteLine($"view.Count = {view.Count}");                       // 2
Console.WriteLine($"view = {string.Join(",", view.OrderBy(x => x))}"); // 1,2

// ---------------------------------------------------------------------------
// 2. 视图是活的（不是快照）
// 2. The view is live (not a snapshot)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 活视图 ===");
Console.WriteLine("=== 2. Live view ===");

map.Add("k", 3);
Console.WriteLine($"Add 后 view.Count / view.Count after Add = {view.Count}");  // 3：活视图反映变化 / 3: the live view reflects the change
map.Remove("k", 1);
Console.WriteLine($"Remove 后 view.Count / view.Count after Remove = {view.Count}"); // 2

// ---------------------------------------------------------------------------
// 3. AsReadOnly 与 ToDictionary 的内层视图
// 3. Inner views of AsReadOnly and ToDictionary
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. AsReadOnly / ToDictionary ===");

// AsReadOnly：整张 map 的活只读视图；内层值集合同样经包装。
// AsReadOnly: a live read-only view of the whole map; the inner value sets are wrapped the same way.
var ro = map.AsReadOnly();
Console.WriteLine($"AsReadOnly()[k].Count = {ro["k"].Count}"); // 2
// ToDictionary：外层字典是快照，内层值集合仍是活视图（活）。
// ToDictionary: the outer dictionary is a snapshot, while the inner value sets are still live views (live).
var snapshot = map.ToDictionary();
Console.WriteLine($"ToDictionary()[k].Count = {snapshot["k"].Count}"); // 2
map.Add("k", 9);
Console.WriteLine($"Add 后 ToDictionary()[k].Count / ToDictionary()[k].Count after Add = {snapshot["k"].Count}"); // 3：内层是活视图 / 3: the inner layer is a live view

// ---------------------------------------------------------------------------
// 4. 跨 TFM 说明与陷阱
// 4. Cross-TFM notes and pitfalls
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 跨 TFM 说明 ===");
Console.WriteLine("=== 4. Cross-TFM notes ===");

// 问题域是 net451 / net461：框架的 HashSet<T> 既不静态声明也不在运行时实现
// The problem domain is net451 / net461: the framework's HashSet<T> neither statically declares nor implements at runtime
// IReadOnlyCollection<T>，裸转型恰好在这些受支持目标上抛 InvalidCastException。
// IReadOnlyCollection<T>, so a raw cast throws InvalidCastException exactly on those supported targets.
// 包内统一经 internal 包装（ReadOnlyCollectionView<T>）暴露，使用方透明、全 TFM 安全。
// The package exposes it uniformly through an internal wrapper (ReadOnlyCollectionView<T>), transparent to consumers and safe on all TFMs.
//
// ❌ 错误写法：new ReadOnlyCollectionView<int>(...) —— 该类型是 internal，编译不过。
// ❌ Wrong approach: new ReadOnlyCollectionView<int>(...) —— the type is internal and will not compile.
// ✅ 正确写法：只用 map["k"] / map.AsReadOnly() / IReadOnlyCollection<T>。
// ✅ Right approach: use only map["k"] / map.AsReadOnly() / IReadOnlyCollection<T>.
// ⚠️ 只读视图是**活**视图，不是快照；要冻结请用 ToSerializableModel（6.2.0）。
// ⚠️ The read-only view is a **live** view, not a snapshot; to freeze it, use ToSerializableModel (6.2.0).

Console.WriteLine("只读值视图经包装暴露：全 TFM 可读 Count 与枚举（net451/net461 不再抛）");
Console.WriteLine("The read-only value view is exposed through the wrapper: Count and enumeration are readable on all TFMs (no longer throwing on net451/net461)");

Console.WriteLine();
Console.WriteLine("Sample.ReadOnlyCollectionView completed.");
