// Sample.CollectionDifference — 四路差异比较 CollectionDifference<TKey,TValue>（6.9.0）
// Sample.CollectionDifference — four-way difference CollectionDifference<TKey,TValue> (6.9.0)
// 运行：dotnet run --project sample/Sample.CollectionDifference
// Run: dotnet run --project sample/Sample.CollectionDifference
//
// 演示重点：
// Highlights:
//   1. left.Difference(right) 把两侧一次性切成四个不相交分区：
//   1. left.Difference(right) slices both sides into four disjoint partitions in one go:
//      OnlyInLeft / OnlyInRight / InCommon / Differing，并用 AreEqual 做「什么都没变」快捷判断。
//      OnlyInLeft / OnlyInRight / InCommon / Differing, with AreEqual as the shortcut for "nothing changed".
//   2. 两个**多重集**按副本数比较；两个 IReadOnlyDictionary 按映射值比较（同一套四分区形状）。
//   2. Two **multisets** are compared by copy count; two IReadOnlyDictionary are compared by mapped value (the same four-partition shape).
//   3. CollectionDifference.Of 是底层工厂；两个重载都接受比较器，键身份与值相等由调用者定义。
//   3. CollectionDifference.Of is the low-level factory; both overloads accept comparers, so key identity and value equality are defined by the caller.
//   4. 结果是**快照**（不可变），不会观察之后对源集合的改动。
//   4. The result is a **snapshot** (immutable) and does not observe later changes to the source collections.
using System;
using System.Collections.Generic;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 两个 bag：按副本数四路分区
// 1. Two bags: four-way partition by copy count
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 两个 bag：按副本数比较 ===");
Console.WriteLine("=== 1. Two bags: comparison by copy count ===");

var left = new MultiList<string>();
left.Add("a", 3);   // a 共 3 份 / 3 copies of a in total
left.Add("b");      // b 共 1 份 / 1 copy of b in total

var right = new MultiList<string>();
right.Add("a", 2);  // a 共 2 份 / 2 copies of a in total
right.Add("c");     // c 共 1 份 / 1 copy of c in total

// Difference 挂在 IMultiSet<T> 上，值类型固定为 int（副本数）
// Difference hangs off IMultiSet<T>; the value type is fixed to int (the copy count)
CollectionDifference<string, int> diff = left.Difference(right);

Console.WriteLine($"摘要 / Summary：{diff}");                                   // left-only 1, right-only 1, in-common 0, differing 1
Console.WriteLine($"OnlyInLeft[\"b\"]  = {diff.OnlyInLeft["b"]}");    // 1：只在左边，值=左边副本数 / 1: left only, value = the left copy count
Console.WriteLine($"OnlyInRight[\"c\"] = {diff.OnlyInRight["c"]}");   // 1：只在右边，值=右边副本数 / 1: right only, value = the right copy count
var (aLeft, aRight) = diff.Differing["a"];                            // 两边都有但副本数不同 / present on both sides but with different copy counts
Console.WriteLine($"Differing[\"a\"]   = (Left={aLeft}, Right={aRight})"); // (3, 2)
Console.WriteLine($"AreEqual = {diff.AreEqual}");                     // False

// 结果是快照：算完之后再改源集合，不会影响已经拿到的 diff
// The result is a snapshot: changing the source collections afterwards does not affect the diff already obtained
left.Add("a");   // 现在 a 有 4 份 / a now has 4 copies
Console.WriteLine($"改左集合后 diff.Differing[\"a\"].Left 仍为 / diff.Differing[\"a\"].Left after changing the left collection is still {diff.Differing["a"].Left}"); // 3（快照未变）/ 3 (snapshot unchanged)

// 两边完全一致 => AreEqual 为 true
// When both sides are exactly identical => AreEqual is true
var sameA = new MultiList<string> { "k", "k" };
var sameB = new MultiList<string> { "k", "k" };
Console.WriteLine($"内容相同的两个 bag AreEqual / AreEqual for two identical bags = {sameA.Difference(sameB).AreEqual}"); // True

// ---------------------------------------------------------------------------
// 2. 两个字典：按映射值四路分区
// 2. Two dictionaries: four-way partition by mapped value
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 两个字典：按映射值比较 ===");
Console.WriteLine("=== 2. Two dictionaries: comparison by mapped value ===");

var d1 = new Dictionary<string, int> { ["x"] = 1, ["y"] = 2 };
var d2 = new Dictionary<string, int> { ["x"] = 1, ["y"] = 9, ["z"] = 3 };

// Dictionary<TKey,TValue> 满足 IReadOnlyDictionary<TKey,TValue> 重载
// Dictionary<TKey,TValue> satisfies the IReadOnlyDictionary<TKey,TValue> overload
var dd = d1.Difference(d2);
Console.WriteLine($"摘要 / Summary：{dd}");                                    // left-only 0, right-only 1, in-common 1, differing 1
Console.WriteLine($"InCommon[\"x\"]  = {dd.InCommon["x"]}");         // 1：两边键值都相同 / 1: same key and value on both sides
Console.WriteLine($"OnlyInRight[\"z\"] = {dd.OnlyInRight["z"]}");    // 3
var (yLeft, yRight) = dd.Differing["y"];
Console.WriteLine($"Differing[\"y\"]  = (Left={yLeft}, Right={yRight})"); // (2, 9)

// 相同内容 => AreEqual
// Identical content => AreEqual
var d3 = new Dictionary<string, int> { ["x"] = 1, ["y"] = 2 };
Console.WriteLine($"d1 与 d3 AreEqual / AreEqual of d1 and d3 = {d1.Difference(d3).AreEqual}"); // True

// ---------------------------------------------------------------------------
// 3. Of 工厂、比较器与参数校验
// 3. The Of factory, comparers and argument validation
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. CollectionDifference.Of / 比较器 / 参数校验 ===");
Console.WriteLine("=== 3. CollectionDifference.Of / comparers / argument validation ===");

var pairsLeft = new[] { new KeyValuePair<string, int>("A", 1) };
var pairsRight = new[] { new KeyValuePair<string, int>("a", 1) };

// 默认比较器区分大小写：A 与 a 是两个不同的键
// The default comparer is case-sensitive: A and a are two different keys
var caseSensitive = CollectionDifference<string, int>.Of(pairsLeft, pairsRight);
Console.WriteLine($"默认比较 / Default comparison：{caseSensitive}");                       // left-only 1, right-only 1, in-common 0, differing 0

// 传入忽略大小写的键比较器：A 与 a 视为同一键，且值相等 => 落入 InCommon
// Passing a case-insensitive key comparer: A and a are treated as the same key, and the values are equal => they fall into InCommon
var caseInsensitive = CollectionDifference<string, int>.Of(
    pairsLeft, pairsRight, StringComparer.OrdinalIgnoreCase);
Console.WriteLine($"忽略大小写 / Case-insensitive：{caseInsensitive}  AreEqual={caseInsensitive.AreEqual}"); // in-common 1 ... True

// 键为 null 无法放进分区字典 => 直接抛 ArgumentException（ParamName 指出是哪一侧）
// A null key cannot be placed into the partition dictionaries => it throws ArgumentException directly (ParamName identifies which side)
try
{
    CollectionDifference<string, int>.Of(
        new[] { new KeyValuePair<string, int>(null!, 1) },
        Array.Empty<KeyValuePair<string, int>>());
}
catch (ArgumentException ex)
{
    Console.WriteLine($"null 键被拒 / null key rejected：ParamName={ex.ParamName}");   // left
}

// 同一侧键重复同样被拒（分区要求键唯一）
// A duplicate key on one side is likewise rejected (partitions require unique keys)
try
{
    CollectionDifference<string, int>.Of(
        new[] { new KeyValuePair<string, int>("dup", 1), new KeyValuePair<string, int>("dup", 2) },
        Array.Empty<KeyValuePair<string, int>>());
}
catch (ArgumentException ex)
{
    Console.WriteLine($"重复键被拒 / duplicate key rejected：ParamName={ex.ParamName}");    // left
}

Console.WriteLine();
Console.WriteLine("Sample.CollectionDifference completed.");
