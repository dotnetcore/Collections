// Sample.MultiSet — 多重集合 MultiList<T>（F01）
// 运行：dotnet run --project sample/Sample.MultiSet
//
// 演示重点：
//   1. MultiList<T> 是「无序多重集合（bag）」：允许重复元素，并为每个不同元素跟踪**副本数**。
//   2. Count 是**副本数**（不是不同元素数）——本包最高危的口径，DistinctCount 才是不同元素数。
//   3. 集合运算按**多重集语义**：并取 max、交取 min、差减对方、对称差取绝对差。
//   4. 枚举 / ToArray 会**副本展开**；EntrySet 给出「元素 + 副本数」的紧凑直方图。
using System;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 最小计数：加两份副本、读副本数与总数
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 最小计数 ===");

var bag = new MultiList<string>();
bag.Add("apple");          // 固定加 1 份
bag.Add("apple");          // 再加 1 份 => apple 共 2 份
bag.Add("banana", 3);      // Add(item, times) 一次加多份 => banana 共 3 份

Console.WriteLine($"CountOf(apple) = {bag.CountOf("apple")}");   // 2：该元素的副本数
Console.WriteLine($"TotalCount     = {bag.TotalCount}");         // 5：全部副本数
Console.WriteLine($"DistinctCount  = {bag.DistinctCount}");      // 2：不同元素数
// Count 在 MultiList<T> 上是**显式接口实现**（ICollection<T> / IReadOnlyCollection<T>），
// 因此在具体类型上要经接口读到；它等于 TotalCount，即**副本数**。
Console.WriteLine($"IReadOnlyCollection.Count = {((IReadOnlyCollection<string>)bag).Count}"); // 5：副本数
Console.WriteLine($"Contains(apple)= {bag.Contains("apple")}");  // True：至少一份即 Contains

// ⚠️ 陷阱：Count 常被误当成「不同元素数」。这里 Count 是 5，DistinctCount 才是 2。

// ---------------------------------------------------------------------------
// 2. 批量添加、副本展开枚举与导出
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 批量添加 / 展开枚举 / 导出 ===");

var letters = new MultiList<string>();
letters.AddRange(new[] { "a", "a", "b" });  // 源里每个出现各计一份 => a:2, b:1

// 枚举会按副本展开：a 出现两次、b 一次（顺序不保证——哈希表枚举顺序无契约）
var expanded = string.Join(",", letters);
Console.WriteLine($"枚举（副本展开）= {expanded}");                 // a,a,b 或 a,b,a 等

// EntrySet() 是「不同元素 + 副本数」投影，项数 == DistinctCount
foreach (var (item, count) in letters.EntrySet())
{
    Console.WriteLine($"  EntrySet: {item} x {count}");
}

// ToArray / ToList 展开全部副本，长度 == TotalCount
string[] array = letters.ToArray();
Console.WriteLine($"ToArray().Length = {array.Length}");            // 3 == TotalCount

// ToDictionary 导出「元素 -> 副本数」快照（含 null 元素时会抛 InvalidOperationException）
var histogram = letters.ToDictionary();
Console.WriteLine($"ToDictionary[a] = {histogram["a"]}");           // 2

// ---------------------------------------------------------------------------
// 3. 多重集合运算：并 / 交 / 差 / 对称差
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 集合运算（按副本数语义）===");

var left = new MultiList<string> { "x", "x", "y" };
var right = new MultiList<string> { "x", "z" };

// 并集：每个元素副本数取两边**最大值**（不是相加！）
var union = new MultiList<string> { "x", "x", "y" };
union.UnionWith(new MultiList<string> { "x", "z" });
Console.WriteLine($"UnionWith        => x:{union.CountOf("x")} y:{union.CountOf("y")} z:{union.CountOf("z")}"); // x:2 y:1 z:1

// 交集：每个元素副本数取两边**最小值**，对方没有的元素被丢弃
var intersection = new MultiList<string> { "x", "x", "y" };
intersection.IntersectionWith(new MultiList<string> { "x", "z" });
Console.WriteLine($"IntersectionWith => x:{intersection.CountOf("x")} y:{intersection.CountOf("y")}");           // x:1 y:0

// 差集：减去对方副本数，不低于 0
var except = new MultiList<string> { "x", "x", "y" };
except.ExceptWith(new MultiList<string> { "x" });
Console.WriteLine($"ExceptWith       => x:{except.CountOf("x")} y:{except.CountOf("y")}");                     // x:1 y:1

// 对称差：取副本数**绝对差**
var sym = new MultiList<string> { "x", "x", "y" };
sym.SymmetricExceptWith(new MultiList<string> { "x", "z" });
Console.WriteLine($"SymmetricExcept  => x:{sym.CountOf("x")} y:{sym.CountOf("y")} z:{sym.CountOf("z")}");        // x:1 y:1 z:1

// ---------------------------------------------------------------------------
// 4. 删除与不变式
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 删除与不变式 ===");

var removable = new MultiList<int> { 7, 7, 7, 8 };
int remaining = removable.Remove(7, 2);   // Remove(item, times) 删 2 份，返回剩余副本数
Console.WriteLine($"Remove(7, 2) 后剩余 7 的份数 = {remaining}");   // 1
Console.WriteLine($"TotalCount = {removable.TotalCount}");          // 2 (7 一份 + 8 一份)

// ⚠️ times <= 0 会抛 ArgumentOutOfRangeException（6.2.0 起，不再静默改写为「一份」）
try
{
    removable.Add(9, 0);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"Add(item, 0) 被拒：{ex.ParamName}");        // times
}

Console.WriteLine();
Console.WriteLine("Sample.MultiSet completed.");
