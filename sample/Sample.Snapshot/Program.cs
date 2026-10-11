// Sample.Snapshot — 独立快照 Snapshot()（6.9.0）
// Sample.Snapshot — an independent snapshot, Snapshot() (6.9.0)
// 运行：dotnet run --project sample/Sample.Snapshot
// Run: dotnet run --project sample/Sample.Snapshot
//
// 演示重点：
// Highlights:
//   1. MultiList<T>.Snapshot() / MultiDictionary<TKey,TValue>.Snapshot() 返回**独立副本**，
//   1. MultiList<T>.Snapshot() / MultiDictionary<TKey,TValue>.Snapshot() return an **independent copy**,
//      专门用于「边改边枚举」：直接枚举活实例不安全，快照已脱离，不会看到之后的改动。
//      specifically for "mutate while enumerating": enumerating a live instance directly is unsafe, whereas the snapshot is detached and won't see later changes.
//   2. Snapshot 与 Clone 是同一操作，名字统一到家族其它类型（并发类型也这样叫）。
//   2. Snapshot and Clone are the same operation, with the name unified across the rest of the family (the concurrent types use it too).
//   3. 拷贝是**浅拷贝**：元素引用共享，但副本计数 / 内层值集合是独立的。
//   3. The copy is a **shallow copy**: element references are shared, but copy counts / inner value collections are independent.
//   4. 若连副本本身也必须不可变，应改用 ImmutableMultiList<T> / ImmutableMultiDictionary<TKey,TValue>。
//   4. If the copy itself must also be immutable, use ImmutableMultiList<T> / ImmutableMultiDictionary<TKey,TValue> instead.
using System;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 边改边枚举：枚举快照，同时继续改原集合
// 1. Mutate while enumerating: enumerate the snapshot while continuing to modify the original
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. MultiList<T>.Snapshot()：边改边枚举 ===");
Console.WriteLine("=== 1. MultiList<T>.Snapshot(): mutate while enumerating ===");

var live = new MultiList<string>();
live.Add("a", 2);   // a 两份 / two copies of a
live.Add("b");      // b 一份 / one copy of b
MultiList<string> snapshot = live.Snapshot();

// 枚举快照（3 份副本 => 循环 3 次），循环体内修改原集合——安全，因为快照已脱离
// Enumerating the snapshot (3 copies => 3 iterations); modifying the original inside the loop is safe because the snapshot is detached
foreach (var item in snapshot)
{
    live.Add("c");   // 每次迭代给原集合加一份 c / add one copy of c to the original on each iteration
}

Console.WriteLine($"原集合 TotalCount / Original TotalCount = {live.TotalCount}");                 // 3 + 3 = 6
Console.WriteLine($"快照  TotalCount / Snapshot TotalCount = {snapshot.TotalCount}");              // 3（不受影响）/ 3 (unaffected)
Console.WriteLine($"快照 CountOf(\"c\") = {snapshot.CountOf("c")}");         // 0（看不到之后新增的 c）/ 0 (cannot see the c added later)

// ---------------------------------------------------------------------------
// 2. Snapshot 与 Clone 等价：浅拷贝，计数独立
// 2. Snapshot is equivalent to Clone: a shallow copy with independent counts
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. Snapshot 与 Clone 等价（浅拷贝）===");
Console.WriteLine("=== 2. Snapshot is equivalent to Clone (shallow copy) ===");

var source = new MultiList<int>();
source.Add(1, 2);          // 1 两份 / two copies of 1
var copy = source.Snapshot();
source.Add(1);             // 只改原集合 / only the original is changed

Console.WriteLine($"原 CountOf(1) / Original CountOf(1) = {source.CountOf(1)}   副本 CountOf(1) / Copy CountOf(1) = {copy.CountOf(1)}"); // 3  2

// ---------------------------------------------------------------------------
// 3. MultiDictionary<TKey,TValue>.Snapshot()
// 3. MultiDictionary<TKey,TValue>.Snapshot()
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. MultiDictionary<TKey,TValue>.Snapshot() ===");

var map = new MultiDictionary<string, int>();
map.Add("k", 1);
map.Add("k", 2);
MultiDictionary<string, int> mapSnapshot = map.Snapshot();

map.Add("k", 3);   // 只改原 map / only the original map is changed

Console.WriteLine($"原 map[\"k\"].Count / Original map[\"k\"].Count = {map["k"].Count}   快照 mapSnapshot[\"k\"].Count / Snapshot mapSnapshot[\"k\"].Count = {mapSnapshot["k"].Count}"); // 3  2
// 外层字典与每个内层值集合都是独立副本：改原 map 不会影响快照
// The outer dictionary and every inner value collection are independent copies: changing the original map does not affect the snapshot

// ❌ 错误写法：一边 foreach (var x in live) 一边 live.Add(...);
// ❌ Wrong approach: doing foreach (var x in live) while live.Add(...);
//    直接枚举活实例不是修改安全的；要边改边枚举请先 Snapshot()。
//    Enumerating a live instance directly is not modification-safe; to mutate while enumerating, call Snapshot() first.
Console.WriteLine();
Console.WriteLine("Sample.Snapshot completed.");
