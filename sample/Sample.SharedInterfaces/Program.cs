// Sample.SharedInterfaces — 多集合共享接口（6.6.0）
// Sample.SharedInterfaces — shared multi-collection interfaces (6.6.0)
// 运行：dotnet run --project sample/Sample.SharedInterfaces
// Run: dotnet run --project sample/Sample.SharedInterfaces
//
// 演示重点：
// Highlights:
//   1. IMultiSet<T> / IMultiDictionary<TKey,TValue> 是多重集合 / 多重映射的**共享读写抽象**：
//   1. IMultiSet<T> / IMultiDictionary<TKey,TValue> are the **shared read/write abstraction** for multi-sets / multi-maps:
//      只编码每个实现都能兑现的语义，使代码可针对接口编写，便于 mock、替换与互换实现。
//      they encode only the semantics every implementation can honor, so code can be written against the interface, easing mocking, replacing, and swapping implementations.
//   2. Count 双语义（本包最高危口径）：IMultiSet.Count = **副本数**（恒等于 TotalCount）；
//   2. The dual Count semantics (this package's highest-risk semantics): IMultiSet.Count = the **copy count** (always equals TotalCount);
//      IMultiDictionary.Count = **键数**（恒等于 KeyCount）。
//      IMultiDictionary.Count = the **key count** (always equals KeyCount).
//   3. IMultiDictionary.Values 用 new 隐藏继承的按组 Values，给出**扁平**序列（每个值一次）。
//   3. IMultiDictionary.Values uses new to hide the inherited grouped Values, giving a **flat** sequence (each value once).
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// 面向接口的算法：同一函数可收 MultiList 或 OrderedMultiList。
// An interface-oriented algorithm: the same function can take a MultiList or an OrderedMultiList.
// 顺序对无序集合无契约，这里排序后打印以获得确定性输出。
// The order has no contract for an unordered collection, so we sort before printing to get deterministic output.
static void Dump(IMultiSet<string> bag)
{
    // bag.Count 是**副本数**（契约级：恒等于 TotalCount）——一个元素 3 份就报 3。
    // bag.Count is the **copy count** (contract-level: always equals TotalCount) — 3 copies of one element reports 3.
    Console.WriteLine($"  Count(副本数) / copy count = {bag.Count}, TotalCount = {bag.TotalCount}, DistinctCount = {bag.DistinctCount}");
    foreach (var (item, count) in bag.EntrySet().OrderBy(e => e.Item, StringComparer.Ordinal))
    {
        Console.WriteLine($"    EntrySet: {item} x {count}");
    }
}

// 面向接口的映射算法。
// An interface-oriented map algorithm.
static void DumpMap(IMultiDictionary<string, int> map)
{
    // 映射 Count / KeyCount = **键数**；TotalValueCount = 值数——与 IMultiSet.Count 语义不同。
    // For a map, Count / KeyCount = the **key count**; TotalValueCount = the value count — different from IMultiSet.Count's semantics.
    Console.WriteLine($"  Count/KeyCount = {map.Count}, KeyCount = {map.KeyCount}, TotalValueCount = {map.TotalValueCount}");
    foreach (var key in map.Keys.OrderBy(k => k, StringComparer.Ordinal))
    {
        Console.WriteLine($"    {key}: [{string.Join(", ", map[key])}]");
    }

    // map.Values 是**扁平**序列（new 隐藏继承的按组 Values），长度 = TotalValueCount。
    // map.Values is a **flat** sequence (new hides the inherited grouped Values); its length = TotalValueCount.
    var flat = map.Values.OrderBy(v => v).ToList();
    Console.WriteLine($"    Values(扁平) / Values (flat) = [{string.Join(", ", flat)}]");
}

// ---------------------------------------------------------------------------
// 1. 面向 IMultiSet 的算法：同一函数收两种实现
// 1. An IMultiSet-oriented algorithm: the same function takes two implementations
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 面向 IMultiSet 的算法 ===");
Console.WriteLine("=== 1. An IMultiSet-oriented algorithm ===");

Console.WriteLine("MultiList<string> {a,a,b}:");
Dump(new MultiList<string> { "a", "a", "b" });          // Count=3, Distinct=2
Console.WriteLine("OrderedMultiList<string> {a,a,b}:");
Dump(new OrderedMultiList<string> { "a", "a", "b" });   // Count=3, Distinct=2

// IMultiSet 刻意**不**继承 ICollection<T>：集合契约要求 bool Remove(T)，
// IMultiSet deliberately does **not** inherit ICollection<T>: the collection contract requires bool Remove(T),
// 而多重集合删一份并报告剩余副本数（int Remove(T)）。这里用反射直接看契约。
// whereas a multi-set removes one copy and reports the remaining copy count (int Remove(T)). Here we inspect the contract directly via reflection.
bool setIsCollection = typeof(ICollection<string>).IsAssignableFrom(typeof(IMultiSet<string>));
Console.WriteLine($"IMultiSet<T> 继承 ICollection<T> 吗？ / Does IMultiSet<T> inherit ICollection<T>? {setIsCollection}");   // False

// ---------------------------------------------------------------------------
// 2. Count 与 Values 语义：映射侧
// 2. Count and Values semantics: the map side
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 映射的 Count 与 Values 语义 ===");
Console.WriteLine("=== 2. Map Count and Values semantics ===");

var map = new MultiDictionary<string, int> { { "a", 1 }, { "a", 2 }, { "b", 3 } };
DumpMap(map);   // Count/KeyCount = 2, TotalValueCount = 3, Values 扁平 / flat = [1, 2, 3]

// 键与值口径对照（同一对象，接口与具体类型一致）。
// Key and value semantics compared (the same object; the interface and the concrete type agree).
IMultiDictionary<string, int> viaInterface = map;
Console.WriteLine($"具体类型 Count / concrete-type Count = {map.Count}，经接口 Count / via-interface Count = {viaInterface.Count}");   // 都是 2 / both 2
Console.WriteLine($"具体类型 Values 是扁平的 / concrete-type Values is flat: {string.Join(", ", map.Values.OrderBy(v => v))}"); // 1, 2, 3

// 按组读用索引器 / Keys；缺键返回空集合。
// For grouped reads use the indexer / Keys; a missing key returns an empty collection.
Console.WriteLine($"map[\"a\"] = [{string.Join(", ", map["a"])}]");   // [1, 2]
Console.WriteLine($"map[\"zzz\"] 值数 / map[\"zzz\"] value count = {map.ValueCount("zzz")}");     // 0

// ---------------------------------------------------------------------------
// 3. 契约范围：刻意最小
// 3. Contract scope: deliberately minimal
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 契约范围（刻意最小）===");
Console.WriteLine("=== 3. Contract scope (deliberately minimal) ===");

// 集合运算 / 比较器**不在**接口内：并发 / 不可变 / PackedBag 并不都提供集合运算；
// Set operations / comparers are **not** in the interface: concurrent / immutable / PackedBag do not all provide set operations;
// 比较器也无序（IEqualityComparer）与有序（IComparer）各异。要集合运算请落回具体类型。
// and comparers differ between unordered (IEqualityComparer) and ordered (IComparer). For set operations, fall back to a concrete type.
var ml = new MultiList<int> { 1, 1, 2 };
ml.UnionWith(new[] { 2, 3 });   // 具体类型上才有；接口上没有 UnionWith
                                // only available on the concrete type; the interface has no UnionWith
Console.WriteLine($"具体类型集合运算后 TotalCount / TotalCount after a set operation on the concrete type = {ml.TotalCount}");   // 4（{1,1,2} ∪ {2,3} 按 max 取并 => 1:2,2:1,3:1）
                                                                                                                                 // 4 ({1,1,2} ∪ {2,3} by max-union => 1:2,2:1,3:1)

// 「无空键」不变式：键仅在至少一值时存在。删光某键的全部值后，键消失。
// The "no empty key" invariant: a key exists only while it has at least one value. After removing all values of a key, the key disappears.
var m2 = new MultiDictionary<string, int> { { "k", 1 } };
m2.Remove("k");
Console.WriteLine($"删空键后 ContainsKey(k) / ContainsKey(k) after emptying the key = {m2.ContainsKey("k")}");   // False

// ⚠️ 陷阱：把 IMultiSet.Count 当不同元素数（它是副本数）；期望 IMultiDictionary.Values 按键分组
// ⚠️ Pitfall: treating IMultiSet.Count as the number of distinct elements (it is the copy count); expecting IMultiDictionary.Values to be grouped by key
//    （它是 new 隐藏后的扁平序列，按组用索引器 / Keys）。
//    (it is the flat sequence after new hiding; use the indexer / Keys for grouped reads).
Console.WriteLine("IMultiSet.Count = 副本数；IMultiDictionary.Values = 扁平序列（按组用索引器）。 / IMultiSet.Count = the copy count; IMultiDictionary.Values = a flat sequence (use the indexer for grouped reads).");

Console.WriteLine();
Console.WriteLine("Sample.SharedInterfaces completed.");
