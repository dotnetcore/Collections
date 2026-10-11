// Sample.ListFaces — 列表接口面（F25）
// 运行：dotnet run --project sample/Sample.ListFaces
//
// 演示重点：
//   1. OrderedMultiList<T> 完整实现 IReadOnlyList<T>：位置访问针对**副本展开**序列，
//      this[int] 是 GetByRank 的另一名字，均 O(log n)。
//   2. IList<T> 也实现，但比较器决定元素归属，「放置」类成员被**收窄**而非放任：
//      Insert 只接受相等元素段内的槽位；索引器 setter 抛 NotSupportedException。
//   3. 删除面完整：RemoveAt / Remove / RemoveAllCopies 正常；IsReadOnly 恒 false。
using System;
using System.Collections.Generic;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 当作 IReadOnlyList 使用
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 当作 IReadOnlyList 使用 ===");

var shelf = new OrderedMultiList<int> { 1, 3, 3, 5 };   // 展开序列：1, 3, 3, 5
IReadOnlyList<int> readOnly = shelf;

// Count 是**副本数**（本例 4），不是 DistinctCount（3）。
Console.WriteLine($"readOnly.Count = {readOnly.Count}");     // 4
// 索引器即名次读：第 2 个位置（0 基）是第二个 3。
Console.WriteLine($"readOnly[2]    = {readOnly[2]}");        // 3
// IndexOf 是 IList<T> 成员（IReadOnlyList 没有），在具体类型上直接用；等价 GetRank。
Console.WriteLine($"shelf.IndexOf(3) = {shelf.IndexOf(3)}"); // 1：首份副本名次
Console.WriteLine($"shelf[2] = {shelf[2]}");                 // 3：this[int] 即 GetByRank
Console.WriteLine($"shelf.IsReadOnly = {shelf.IsReadOnly}"); // False

// 可把 OrderedMultiList 直接交给收 IReadOnlyList 的下游（序列化器、绑定、LINQ）。
IReadOnlyList<int> downstream = shelf;
Console.WriteLine($"交给下游后枚举：{string.Join(",", downstream)}"); // 1,3,3,5

// ---------------------------------------------------------------------------
// 2. IList 的「放置面」被收窄
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. IList 放置面收窄 ===");

var shelf2 = new OrderedMultiList<int> { 1, 3, 3, 5 };
IList<int> mutable = shelf2;

// Insert 只在**相等元素段内**的槽位被接受：位置 1、2 都夹在 3 的段里。
shelf2.Insert(1, 3);
Console.WriteLine($"Insert(1, 3) 后序列 = {string.Join(",", shelf2)}"); // 1,3,3,3,5

// 其它位置抛 ArgumentOutOfRangeException（此处 9 排在位置 0 的 1 之后，无处安放）。
try
{
    shelf2.Insert(0, 9);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"Insert(0, 9) 抛：{ex.ParamName}");     // index
}

// ❌ 错误写法：按下标覆盖 —— 索引器 setter 抛 NotSupportedException（写入会破坏全序）。
//    shelf2[0] = 9;               // 编译期能过，运行期抛 NotSupportedException
try
{
    mutable[0] = 9;
}
catch (NotSupportedException)
{
    Console.WriteLine("索引器赋值抛：NotSupportedException（写入会破坏全序）");
}

// 删除面完整：RemoveAt 是名次删，与全序兼容。
shelf2.RemoveAt(1);              // 删掉展开序列第 1 个（一个 3）
Console.WriteLine($"RemoveAt(1) 后序列 = {string.Join(",", shelf2)}"); // 1,3,3,5

// 值删也正常：Remove 删一份，返回剩余副本数。
int left = shelf2.Remove(3);
Console.WriteLine($"Remove(3) 后剩余 3 的份数 = {left}");       // 1
// RemoveAllCopies 删光某元素全部副本。
Console.WriteLine($"RemoveAllCopies(3) = {shelf2.RemoveAllCopies(3)}"); // True
Console.WriteLine($"最终序列 = {string.Join(",", shelf2)}");   // 1,5

// ---------------------------------------------------------------------------
// 3. 与 MultiList / Deque 对照
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 与 MultiList / Deque 对照 ===");

// MultiList<T> 无序，位置无契约，故**刻意不实现** IList / IReadOnlyList。
var bag = new MultiList<int> { 1, 3, 3, 5 };
Console.WriteLine($"MultiList 是 IReadOnlyList 吗？{bag is IReadOnlyList<int>}");   // False
// OrderedMultiList 有序展开序列满足稳定位置语义，故实现。
Console.WriteLine($"OrderedMultiList 是 IReadOnlyList 吗？{shelf is IReadOnlyList<int>}"); // True
// Deque<T> 顺序由调用方控制，索引器 setter 与 Insert 都有意义（见 F28）。
Console.WriteLine("Deque<T> 完整实现列表接口，setter / Insert 均可用（对照见 Sample.Deque）。");

// ❌ 错误写法：把 OrderedMultiList 当 List<T> 随机插 —— Insert 非任意位置，只在相等段内。

Console.WriteLine();
Console.WriteLine("Sample.ListFaces completed.");
