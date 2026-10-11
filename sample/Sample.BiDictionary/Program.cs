// Sample.BiDictionary — 严格双射字典 BiDictionary<TLeft,TRight>（F16）
// Sample.BiDictionary — strict bijective dictionary BiDictionary<TLeft,TRight> (F16)
// 运行：dotnet run --project sample/Sample.BiDictionary
// Run: dotnet run --project sample/Sample.BiDictionary
//
// 演示重点：
// Highlights:
//   1. 严格一对一（双射）：每个左值恰绑一个右值，且没有右值被两个左值共享。
//   1. Strictly one-to-one (bijective): each left value is bound to exactly one right value, and no right value is shared by two left values.
//   2. 双向都是 O(1)：正向索引器 / GetLeft(right) 反向。
//   2. O(1) both ways: the forward indexer / GetLeft(right) for reverse.
//   3. 冲突处理严格：Add 冲突抛 ArgumentException；TryAdd 返回 false；**无静默覆盖 setter**。
//   3. Strict conflict handling: Add throws ArgumentException on conflict; TryAdd returns false; **no silent-overwrite setter**.
//   4. AsReverse 活只读视图；ToDictionary 省略 null 左值；ToSerializableModel 保留 null 左值。
//   4. AsReverse is a live read-only view; ToDictionary omits null left values; ToSerializableModel preserves them.
using System;
using System.Collections.Generic;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 双向查找
// 1. Bidirectional lookup
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 双向查找 ===");
Console.WriteLine("=== 1. Bidirectional lookup ===");

var users = new BiDictionary<int, string>();
users.Add(1, "alice");
users.Add(2, "bob");

Console.WriteLine($"users[1]           = {users[1]}");              // alice：正向，O(1) / alice: forward, O(1)
Console.WriteLine($"users.GetLeft(bob) = {users.GetLeft("bob")}");  // 2：反向，O(1) / 2: reverse, O(1)
Console.WriteLine($"Count              = {users.Count}");           // 2

// ---------------------------------------------------------------------------
// 2. 严格冲突：Add 抛 / TryAdd 返 false / 无覆盖 setter
// 2. Strict conflicts: Add throws / TryAdd returns false / no overwriting setter
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 严格冲突处理 ===");
Console.WriteLine("=== 2. Strict conflict handling ===");

// 右侧冲突不静默覆盖——覆盖会静默解绑它原本所属的左侧（调用方从未提及的条目）。
// A right-side conflict is not silently overwritten — overwriting would silently unbind the left value it originally belonged to (an entry the caller never mentioned).
Console.WriteLine($"TryAdd(3,bob) = {users.TryAdd(3, "bob")}"); // False：bob 已绑 2 / False: bob is already bound to 2
try
{
    users.Add(3, "bob");
}
catch (ArgumentException)
{
    Console.WriteLine("Add(3,bob) 抛 ArgumentException / Add(3,bob) throws ArgumentException");       // 右侧冲突 / right-side conflict
}
// ❌ 错误写法：users[3] = "bob"; —— 刻意没有覆盖 setter；破坏既有绑定必须先显式 Remove。
// ❌ Wrong approach: users[3] = "bob"; —— there is deliberately no overwriting setter; breaking an existing binding requires an explicit Remove first.
// 解绑后即可重绑：
// After unbinding, rebinding is possible:
users.Remove(2);
Console.WriteLine($"Remove(2) 后 TryAdd(3,bob) / after Remove(2), TryAdd(3,bob) = {users.TryAdd(3, "bob")}"); // True

// ---------------------------------------------------------------------------
// 3. AsReverse 活只读视图
// 3. AsReverse live read-only view
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. AsReverse 活视图 ===");
Console.WriteLine("=== 3. AsReverse live view ===");

IReadOnlyDictionary<string, int> byName = users.AsReverse();
Console.WriteLine($"AsReverse()[alice] = {byName["alice"]}"); // 1
users.Add(4, "carol");   // 源变化立即反映到活视图 / a source change is immediately reflected in the live view
Console.WriteLine($"AsReverse().ContainsKey(carol) = {byName.ContainsKey("carol")}"); // True

// ---------------------------------------------------------------------------
// 4. null 左值与导出
// 4. Null left values and export
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. null 左值与导出 ===");
Console.WriteLine("=== 4. Null left values and export ===");

// 用 string? 作为左值类型，才能合法地表达 null 左值绑定（TLeft 本身须可空）。
// Using string? as the left type is what makes a null left-value binding expressible (TLeft itself must be nullable).
var withNull = new BiDictionary<string?, int>();
withNull.Add("k", 1);
withNull.Add(null, 2);   // null 两侧都支持，(null, null) 绑定也可表达 / both sides support null, and a (null, null) binding is expressible too
Console.WriteLine($"Count = {withNull.Count}");                                  // 2
// ToDictionary 无法用 null 作字典键，会**省略** null 左值条目。
// ToDictionary cannot use null as a dictionary key, so it **omits** null-left entries.
Console.WriteLine($"ToDictionary().Count = {withNull.ToDictionary().Count}");    // 1
// ToSerializableModel 用并行 List 承载，保留 null 左值条目。
// ToSerializableModel carries them in parallel Lists, preserving null-left entries.
var model = withNull.ToSerializableModel();
Console.WriteLine($"ToSerializableModel().Lefts.Count = {model.Lefts.Count}");   // 2
Console.WriteLine($"FromModel 重建 Count / FromModel rebuilt Count = {BiDictionary<string?, int>.FromModel(model).Count}"); // 2

// 模型必须是双射：右值重复会被 FromModel 拒绝（与 Add 同一冲突）。
// The model must be bijective: duplicate right values are rejected by FromModel (the same conflict as Add).
var bad = new BiDictionaryModel<string, int>
{
    Lefts = new List<string> { "a", "b" },
    Rights = new List<int> { 1, 1 }
};
try
{
    BiDictionary<string, int>.FromModel(bad);
}
catch (ArgumentException)
{
    Console.WriteLine("FromModel 拒绝非双射 / FromModel rejects a non-bijection：ArgumentException");
}

Console.WriteLine();
Console.WriteLine("Sample.BiDictionary completed.");
