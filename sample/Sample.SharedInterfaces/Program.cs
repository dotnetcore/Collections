// Sample.SharedInterfaces — 多集合共享接口（F27）
// 运行：dotnet run --project sample/Sample.SharedInterfaces
//
// 演示重点：
//   1. IMultiSet<T> / IMultiDictionary<TKey,TValue> 是多重集合 / 多重映射的**共享读写抽象**：
//      只编码每个实现都能兑现的语义，使代码可针对接口编写，便于 mock、替换与互换实现。
//   2. Count 双语义（本包最高危口径）：IMultiSet.Count = **副本数**（恒等于 TotalCount）；
//      IMultiDictionary.Count = **键数**（恒等于 KeyCount）。
//   3. IMultiDictionary.Values 用 new 隐藏继承的按组 Values，给出**扁平**序列（每个值一次）。
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// 面向接口的算法：同一函数可收 MultiList 或 OrderedMultiList。
// 顺序对无序集合无契约，这里排序后打印以获得确定性输出。
static void Dump(IMultiSet<string> bag)
{
    // bag.Count 是**副本数**（契约级：恒等于 TotalCount）——一个元素 3 份就报 3。
    Console.WriteLine($"  Count(副本数) = {bag.Count}, TotalCount = {bag.TotalCount}, DistinctCount = {bag.DistinctCount}");
    foreach (var (item, count) in bag.EntrySet().OrderBy(e => e.Item, StringComparer.Ordinal))
    {
        Console.WriteLine($"    EntrySet: {item} x {count}");
    }
}

// 面向接口的映射算法。
static void DumpMap(IMultiDictionary<string, int> map)
{
    // 映射 Count / KeyCount = **键数**；TotalValueCount = 值数——与 IMultiSet.Count 语义不同。
    Console.WriteLine($"  Count/KeyCount = {map.Count}, KeyCount = {map.KeyCount}, TotalValueCount = {map.TotalValueCount}");
    foreach (var key in map.Keys.OrderBy(k => k, StringComparer.Ordinal))
    {
        Console.WriteLine($"    {key}: [{string.Join(", ", map[key])}]");
    }

    // map.Values 是**扁平**序列（new 隐藏继承的按组 Values），长度 = TotalValueCount。
    var flat = map.Values.OrderBy(v => v).ToList();
    Console.WriteLine($"    Values(扁平) = [{string.Join(", ", flat)}]");
}

// ---------------------------------------------------------------------------
// 1. 面向 IMultiSet 的算法：同一函数收两种实现
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 面向 IMultiSet 的算法 ===");

Console.WriteLine("MultiList<string> {a,a,b}:");
Dump(new MultiList<string> { "a", "a", "b" });          // Count=3, Distinct=2
Console.WriteLine("OrderedMultiList<string> {a,a,b}:");
Dump(new OrderedMultiList<string> { "a", "a", "b" });   // Count=3, Distinct=2

// IMultiSet 刻意**不**继承 ICollection<T>：集合契约要求 bool Remove(T)，
// 而多重集合删一份并报告剩余副本数（int Remove(T)）。这里用反射直接看契约。
bool setIsCollection = typeof(ICollection<string>).IsAssignableFrom(typeof(IMultiSet<string>));
Console.WriteLine($"IMultiSet<T> 继承 ICollection<T> 吗？{setIsCollection}");   // False

// ---------------------------------------------------------------------------
// 2. Count 与 Values 语义：映射侧
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 映射的 Count 与 Values 语义 ===");

var map = new MultiDictionary<string, int> { { "a", 1 }, { "a", 2 }, { "b", 3 } };
DumpMap(map);   // Count/KeyCount = 2, TotalValueCount = 3, Values 扁平 = [1, 2, 3]

// 键与值口径对照（同一对象，接口与具体类型一致）。
IMultiDictionary<string, int> viaInterface = map;
Console.WriteLine($"具体类型 Count = {map.Count}，经接口 Count = {viaInterface.Count}");   // 都是 2
Console.WriteLine($"具体类型 Values 是扁平的：{string.Join(", ", map.Values.OrderBy(v => v))}"); // 1, 2, 3

// 按组读用索引器 / Keys；缺键返回空集合。
Console.WriteLine($"map[\"a\"] = [{string.Join(", ", map["a"])}]");   // [1, 2]
Console.WriteLine($"map[\"zzz\"] 值数 = {map.ValueCount("zzz")}");     // 0

// ---------------------------------------------------------------------------
// 3. 契约范围：刻意最小
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 契约范围（刻意最小）===");

// 集合运算 / 比较器**不在**接口内：并发 / 不可变 / PackedBag 并不都提供集合运算；
// 比较器也无序（IEqualityComparer）与有序（IComparer）各异。要集合运算请落回具体类型。
var ml = new MultiList<int> { 1, 1, 2 };
ml.UnionWith(new[] { 2, 3 });   // 具体类型上才有；接口上没有 UnionWith
Console.WriteLine($"具体类型集合运算后 TotalCount = {ml.TotalCount}");   // 4（{1,1,2} ∪ {2,3} 按 max 取并 => 1:2,2:1,3:1）

// 「无空键」不变式：键仅在至少一值时存在。删光某键的全部值后，键消失。
var m2 = new MultiDictionary<string, int> { { "k", 1 } };
m2.Remove("k");
Console.WriteLine($"删空键后 ContainsKey(k) = {m2.ContainsKey("k")}");   // False

// ⚠️ 陷阱：把 IMultiSet.Count 当不同元素数（它是副本数）；期望 IMultiDictionary.Values 按键分组
//    （它是 new 隐藏后的扁平序列，按组用索引器 / Keys）。
Console.WriteLine("IMultiSet.Count = 副本数；IMultiDictionary.Values = 扁平序列（按组用索引器）。");

Console.WriteLine();
Console.WriteLine("Sample.SharedInterfaces completed.");
