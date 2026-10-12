// Sample.ListFaces — 列表接口面（6.5.0）
// Sample.ListFaces — list interface surface (6.5.0)
// 运行：dotnet run --project sample/Sample.ListFaces
// Run: dotnet run --project sample/Sample.ListFaces
//
// 演示重点：
// Highlights:
//   1. OrderedMultiList<T> 完整实现 IReadOnlyList<T>：位置访问针对**副本展开**序列，
//   1. OrderedMultiList<T> fully implements IReadOnlyList<T>: positional access targets the **copy-expanded** sequence,
//      this[int] 是 GetByRank 的另一名字，均 O(log n)。
//      this[int] is another name for GetByRank, both O(log n).
//   2. IList<T> 也实现，但比较器决定元素归属，「放置」类成员被**收窄**而非放任：
//   2. IList<T> is also implemented, but the comparer decides element membership, so "placement" members are **narrowed** rather than left loose:
//      Insert 只接受相等元素段内的槽位；索引器 setter 抛 NotSupportedException。
//      Insert accepts only slots within a run of equal elements; the indexer setter throws NotSupportedException.
//   3. 删除面完整：RemoveAt / Remove / RemoveAllCopies 正常；IsReadOnly 恒 false。
//   3. The removal surface is complete: RemoveAt / Remove / RemoveAllCopies work; IsReadOnly is always false.
using System;
using System.Collections.Generic;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 当作 IReadOnlyList 使用
// 1. Use it as an IReadOnlyList
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 当作 IReadOnlyList 使用 ===");
Console.WriteLine("=== 1. Use it as an IReadOnlyList ===");

var shelf = new OrderedMultiList<int> { 1, 3, 3, 5 };   // 展开序列：1, 3, 3, 5
                                                        // expanded sequence: 1, 3, 3, 5
IReadOnlyList<int> readOnly = shelf;

// Count 是**副本数**（本例 4），不是 DistinctCount（3）。
// Count is the **copy count** (4 here), not DistinctCount (3).
Console.WriteLine($"readOnly.Count = {readOnly.Count}");     // 4
// 索引器即名次读：第 2 个位置（0 基）是第二个 3。
// The indexer is the rank read: the 2nd position (0-based) is the second 3.
Console.WriteLine($"readOnly[2]    = {readOnly[2]}");        // 3
// IndexOf 是 IList<T> 成员（IReadOnlyList 没有），在具体类型上直接用；等价 GetRank。
// IndexOf is an IList<T> member (IReadOnlyList doesn't have it); use it directly on the concrete type; it is equivalent to GetRank.
Console.WriteLine($"shelf.IndexOf(3) = {shelf.IndexOf(3)}"); // 1：首份副本名次
                                                             // 1: the rank of the first copy
Console.WriteLine($"shelf[2] = {shelf[2]}");                 // 3：this[int] 即 GetByRank
                                                             // 3: this[int] is GetByRank
Console.WriteLine($"shelf.IsReadOnly = {shelf.IsReadOnly}"); // False

// 可把 OrderedMultiList 直接交给收 IReadOnlyList 的下游（序列化器、绑定、LINQ）。
// OrderedMultiList can be handed directly to a downstream that takes IReadOnlyList (serializers, binding, LINQ).
IReadOnlyList<int> downstream = shelf;
Console.WriteLine($"交给下游后枚举 / enumerated after handing to downstream: {string.Join(",", downstream)}"); // 1,3,3,5

// ---------------------------------------------------------------------------
// 2. IList 的「放置面」被收窄
// 2. The "placement surface" of IList is narrowed
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. IList 放置面收窄 ===");
Console.WriteLine("=== 2. IList placement surface narrowed ===");

var shelf2 = new OrderedMultiList<int> { 1, 3, 3, 5 };
IList<int> mutable = shelf2;

// Insert 只在**相等元素段内**的槽位被接受：位置 1、2 都夹在 3 的段里。
// Insert accepts only slots **within a run of equal elements**: positions 1 and 2 both sit inside the run of 3s.
shelf2.Insert(1, 3);
Console.WriteLine($"Insert(1, 3) 后序列 / sequence after Insert(1, 3) = {string.Join(",", shelf2)}"); // 1,3,3,3,5

// 其它位置抛 ArgumentOutOfRangeException（此处 9 排在位置 0 的 1 之后，无处安放）。
// Other positions throw ArgumentOutOfRangeException (here 9 would follow the 1 at position 0 and has nowhere to fit).
try
{
    shelf2.Insert(0, 9);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"Insert(0, 9) 抛 / Insert(0, 9) throws: {ex.ParamName}");     // index
}

// ❌ 错误写法：按下标覆盖 —— 索引器 setter 抛 NotSupportedException（写入会破坏全序）。
// ❌ Wrong usage: overwriting by index — the indexer setter throws NotSupportedException (a write would break the total order).
//    shelf2[0] = 9;               // 编译期能过，运行期抛 NotSupportedException
//                                 // compiles, but throws NotSupportedException at runtime
try
{
    mutable[0] = 9;
}
catch (NotSupportedException)
{
    Console.WriteLine("索引器赋值抛：NotSupportedException（写入会破坏全序） / indexer assignment throws NotSupportedException (a write would break the total order)");
}

// 删除面完整：RemoveAt 是名次删，与全序兼容。
// The removal surface is complete: RemoveAt is a rank removal, compatible with the total order.
shelf2.RemoveAt(1);              // 删掉展开序列第 1 个（一个 3）
                                 // removes the 1st item of the expanded sequence (one 3)
Console.WriteLine($"RemoveAt(1) 后序列 / sequence after RemoveAt(1) = {string.Join(",", shelf2)}"); // 1,3,3,5

// 值删也正常：Remove 删一份，返回剩余副本数。
// Value removal also works: Remove removes one copy and returns the remaining copy count.
int left = shelf2.Remove(3);
Console.WriteLine($"Remove(3) 后剩余 3 的份数 / copies of 3 remaining after Remove(3) = {left}");       // 1
// RemoveAllCopies 删光某元素全部副本。
// RemoveAllCopies removes all copies of an element.
Console.WriteLine($"RemoveAllCopies(3) = {shelf2.RemoveAllCopies(3)}"); // True
Console.WriteLine($"最终序列 / final sequence = {string.Join(",", shelf2)}");   // 1,5

// ---------------------------------------------------------------------------
// 3. 与 MultiList / Deque 对照
// 3. Comparison with MultiList / Deque
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 与 MultiList / Deque 对照 ===");
Console.WriteLine("=== 3. Comparison with MultiList / Deque ===");

// MultiList<T> 无序，位置无契约，故**刻意不实现** IList / IReadOnlyList。
// MultiList<T> is unordered with no positional contract, so it **deliberately does not implement** IList / IReadOnlyList.
var bag = new MultiList<int> { 1, 3, 3, 5 };
Console.WriteLine($"MultiList 是 IReadOnlyList 吗？ / Is MultiList an IReadOnlyList? {bag is IReadOnlyList<int>}");   // False
// OrderedMultiList 有序展开序列满足稳定位置语义，故实现。
// OrderedMultiList's ordered expanded sequence satisfies stable positional semantics, so it implements them.
Console.WriteLine($"OrderedMultiList 是 IReadOnlyList 吗？ / Is OrderedMultiList an IReadOnlyList? {shelf is IReadOnlyList<int>}"); // True
// Deque<T> 顺序由调用方控制，索引器 setter 与 Insert 都有意义（见 6.6.0）。
// Deque<T>'s order is controlled by the caller, so both the indexer setter and Insert are meaningful (see 6.6.0).
Console.WriteLine("Deque<T> 完整实现列表接口，setter / Insert 均可用（对照见 Sample.Deque）。 / Deque<T> fully implements the list interfaces; both setter / Insert are usable (see Sample.Deque for comparison).");

// ❌ 错误写法：把 OrderedMultiList 当 List<T> 随机插 —— Insert 非任意位置，只在相等段内。
// ❌ Wrong usage: treating OrderedMultiList as List<T> for random insertion — Insert is not at an arbitrary position, only within a run of equal elements.

Console.WriteLine();
Console.WriteLine("Sample.ListFaces completed.");
