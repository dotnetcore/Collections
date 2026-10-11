// Sample.MultiSet — 多重集合 MultiList<T>（F01）
// Sample.MultiSet — Multiset MultiList<T> (F01)
// 运行：dotnet run --project sample/Sample.MultiSet
// Run: dotnet run --project sample/Sample.MultiSet
//
// 演示重点：
// Highlights:
//   1. MultiList<T> 是「无序多重集合（bag）」：允许重复元素，并为每个不同元素跟踪**副本数**。
//   1. MultiList<T> is an "unordered multiset (bag)": it allows duplicate elements and tracks a **copy count** for each distinct element.
//   2. Count 是**副本数**（不是不同元素数）——本包最高危的口径，DistinctCount 才是不同元素数。
//   2. Count is the **copy count** (not the number of distinct elements) — this package's highest-risk semantic; DistinctCount is the number of distinct elements.
//   3. 集合运算按**多重集语义**：并取 max、交取 min、差减对方、对称差取绝对差。
//   3. Set operations follow **multiset semantics**: union takes max, intersection takes min, difference subtracts the other side, symmetric difference takes the absolute difference.
//   4. 枚举 / ToArray 会**副本展开**；EntrySet 给出「元素 + 副本数」的紧凑直方图。
//   4. Enumeration / ToArray perform **copy expansion**; EntrySet gives a compact histogram of "element + copy count".
using System;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 最小计数：加两份副本、读副本数与总数
// 1. Minimal counting: add two copies, read the copy count and the total
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 最小计数 ===");
Console.WriteLine("=== 1. Minimal counting ===");

var bag = new MultiList<string>();
bag.Add("apple");          // 固定加 1 份
                           // Add exactly 1 copy
bag.Add("apple");          // 再加 1 份 => apple 共 2 份
                           // Add 1 more copy => apple has 2 copies total
bag.Add("banana", 3);      // Add(item, times) 一次加多份 => banana 共 3 份
                           // Add(item, times) adds multiple copies at once => banana has 3 copies total

Console.WriteLine($"CountOf(apple) = {bag.CountOf("apple")}");   // 2：该元素的副本数
                                                                 // 2: the copy count of that element
Console.WriteLine($"TotalCount     = {bag.TotalCount}");         // 5：全部副本数
                                                                 // 5: total copies
Console.WriteLine($"DistinctCount  = {bag.DistinctCount}");      // 2：不同元素数
                                                                 // 2: number of distinct elements
// Count 在 MultiList<T> 上是**显式接口实现**（ICollection<T> / IReadOnlyCollection<T>），
// Count is an **explicit interface implementation** on MultiList<T> (ICollection<T> / IReadOnlyCollection<T>),
// 因此在具体类型上要经接口读到；它等于 TotalCount，即**副本数**。
// so on the concrete type you must read it through the interface; it equals TotalCount, i.e. the **copy count**.
Console.WriteLine($"IReadOnlyCollection.Count = {((IReadOnlyCollection<string>)bag).Count}"); // 5：副本数
                                                                                              // 5: copy count
Console.WriteLine($"Contains(apple)= {bag.Contains("apple")}");  // True：至少一份即 Contains
                                                                 // True: Contains holds as long as there is at least one copy

// ⚠️ 陷阱：Count 常被误当成「不同元素数」。这里 Count 是 5，DistinctCount 才是 2。
// ⚠️ Pitfall: Count is often mistaken for the "number of distinct elements". Here Count is 5, and only DistinctCount is 2.

// ---------------------------------------------------------------------------
// 2. 批量添加、副本展开枚举与导出
// 2. Batch adding, copy-expanded enumeration and export
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 批量添加 / 展开枚举 / 导出 ===");
Console.WriteLine("=== 2. Batch add / expanded enumeration / export ===");

var letters = new MultiList<string>();
letters.AddRange(new[] { "a", "a", "b" });  // 源里每个出现各计一份 => a:2, b:1
                                            // each occurrence in the source counts as one copy => a:2, b:1

// 枚举会按副本展开：a 出现两次、b 一次（顺序不保证——哈希表枚举顺序无契约）
// Enumeration expands by copy: a appears twice, b once (order not guaranteed — hash-table enumeration order has no contract)
var expanded = string.Join(",", letters);
Console.WriteLine($"枚举（副本展开） / Enumerated (copies expanded) = {expanded}");                 // a,a,b 或 a,b,a 等
                                                                                                   // e.g. a,a,b or a,b,a

// EntrySet() 是「不同元素 + 副本数」投影，项数 == DistinctCount
// EntrySet() is a "distinct element + copy count" projection; its item count == DistinctCount
foreach (var (item, count) in letters.EntrySet())
{
    Console.WriteLine($"  EntrySet: {item} x {count}");
}

// ToArray / ToList 展开全部副本，长度 == TotalCount
// ToArray / ToList expand all copies; length == TotalCount
string[] array = letters.ToArray();
Console.WriteLine($"ToArray().Length = {array.Length}");            // 3 == TotalCount

// ToDictionary 导出「元素 -> 副本数」快照（含 null 元素时会抛 InvalidOperationException）
// ToDictionary exports an "element -> copy count" snapshot (throws InvalidOperationException when a null element is present)
var histogram = letters.ToDictionary();
Console.WriteLine($"ToDictionary[a] = {histogram["a"]}");           // 2

// ---------------------------------------------------------------------------
// 3. 多重集合运算：并 / 交 / 差 / 对称差
// 3. Multiset operations: union / intersection / difference / symmetric difference
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 集合运算（按副本数语义）===");
Console.WriteLine("=== 3. Set operations (by copy-count semantics) ===");

var left = new MultiList<string> { "x", "x", "y" };
var right = new MultiList<string> { "x", "z" };

// 并集：每个元素副本数取两边**最大值**（不是相加！）
// Union: each element's copy count takes the **max** of both sides (not a sum!)
var union = new MultiList<string> { "x", "x", "y" };
union.UnionWith(new MultiList<string> { "x", "z" });
Console.WriteLine($"UnionWith        => x:{union.CountOf("x")} y:{union.CountOf("y")} z:{union.CountOf("z")}"); // x:2 y:1 z:1

// 交集：每个元素副本数取两边**最小值**，对方没有的元素被丢弃
// Intersection: each element's copy count takes the **min** of both sides; elements absent on the other side are dropped
var intersection = new MultiList<string> { "x", "x", "y" };
intersection.IntersectionWith(new MultiList<string> { "x", "z" });
Console.WriteLine($"IntersectionWith => x:{intersection.CountOf("x")} y:{intersection.CountOf("y")}");           // x:1 y:0

// 差集：减去对方副本数，不低于 0
// Difference: subtract the other side's copy count, never below 0
var except = new MultiList<string> { "x", "x", "y" };
except.ExceptWith(new MultiList<string> { "x" });
Console.WriteLine($"ExceptWith       => x:{except.CountOf("x")} y:{except.CountOf("y")}");                     // x:1 y:1

// 对称差：取副本数**绝对差**
// Symmetric difference: take the **absolute difference** of copy counts
var sym = new MultiList<string> { "x", "x", "y" };
sym.SymmetricExceptWith(new MultiList<string> { "x", "z" });
Console.WriteLine($"SymmetricExcept  => x:{sym.CountOf("x")} y:{sym.CountOf("y")} z:{sym.CountOf("z")}");        // x:1 y:1 z:1

// ---------------------------------------------------------------------------
// 4. 删除与不变式
// 4. Removal and invariants
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 删除与不变式 ===");
Console.WriteLine("=== 4. Removal and invariants ===");

var removable = new MultiList<int> { 7, 7, 7, 8 };
int remaining = removable.Remove(7, 2);   // Remove(item, times) 删 2 份，返回剩余副本数
                                          // Remove(item, times) removes 2 copies and returns the remaining copy count
Console.WriteLine($"Remove(7, 2) 后剩余 7 的份数 / Remaining copies of 7 after Remove(7, 2) = {remaining}");   // 1
Console.WriteLine($"TotalCount = {removable.TotalCount}");          // 2 (7 一份 + 8 一份)
                                                                     // 2 (one copy of 7 + one copy of 8)

// ⚠️ times <= 0 会抛 ArgumentOutOfRangeException（6.2.0 起，不再静默改写为「一份」）
// ⚠️ times <= 0 throws ArgumentOutOfRangeException (since 6.2.0, no longer silently rewritten to "one copy")
try
{
    removable.Add(9, 0);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"Add(item, 0) 被拒 / Add(item, 0) rejected: {ex.ParamName}");        // times
}

Console.WriteLine();
Console.WriteLine("Sample.MultiSet completed.");
