// Sample.PackedBag — 打包值类型袋（6.4.0）
// Sample.PackedBag — packed value-type bag (6.4.0)
// 运行：dotnet run --project sample/Sample.PackedBag
// Run: dotnet run --project sample/Sample.PackedBag
//
// 演示重点：
// Highlights:
//   1. PackedBag<T> where T : struct 是**密集数组直方图**：存为连续 (value, count) 结构体数组，
//   1. PackedBag<T> where T : struct is a **dense-array histogram**: stored as a contiguous (value, count) struct array,
//      存储路径无装箱、无每项对象开销，适合约 ≤16 个不同值的枚举 / 小整数直方图。
//      the storage path is box-free with no per-item object overhead, suited to enum / small-integer histograms with roughly ≤16 distinct values.
//   2. 袋语义镜像 MultiList<T>：Add 加副本、Remove 去一份并返回剩余副本数、枚举副本展开；
//   2. Bag semantics mirror MultiList<T>: Add adds a copy, Remove removes one copy and returns the remaining copy count, enumeration expands copies;
//      末份删除即弃项（尾移保持 packed，无零计数空洞）。
//      deleting the last copy drops the entry (a tail move keeps the array packed, with no zero-count holes).
//   3. 刻意**不带**集合运算 / 结构相等 / 自定义比较器——它是直方图，不是通用多重集合。
//   3. Deliberately **omits** set operations / structural equality / custom comparers — it is a histogram, not a general multi-set.
using System;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 枚举直方图：T : struct，无装箱存储
// 1. Enum histogram: T : struct, box-free storage
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 枚举直方图 ===");
Console.WriteLine("=== 1. Enum histogram ===");

// DayOfWeek 是值类型（枚举），满足 where T : struct；元素内联在连续数组里。
// DayOfWeek is a value type (an enum) and satisfies where T : struct; elements are inlined in a contiguous array.
var bag = new PackedBag<DayOfWeek>();
bag.Add(DayOfWeek.Monday, 3);   // Add(item, times) 一次加 3 份
                                // Add(item, times) adds 3 copies at once
bag.Add(DayOfWeek.Friday);      // Add(item) 加 1 份
                                // Add(item) adds 1 copy

Console.WriteLine($"CountOf(Monday) = {bag.CountOf(DayOfWeek.Monday)}");   // 3
Console.WriteLine($"TotalCount      = {bag.TotalCount}");                 // 4：副本数
                                                                          // 4: number of copies
Console.WriteLine($"DistinctCount   = {bag.DistinctCount}");              // 2：不同元素数
                                                                          // 2: number of distinct elements
Console.WriteLine($"IsEmpty         = {bag.IsEmpty}");                    // False
Console.WriteLine($"Capacity        = {bag.Capacity}");                   // 内部条目数组容量（≥2）
                                                                          // capacity of the internal entry array (≥2)

// EntrySet() 是「元素 + 副本数」的紧凑直方图；项数 == DistinctCount。
// EntrySet() is a compact "element + copy count" histogram; the item count == DistinctCount.
foreach (var (day, count) in bag.EntrySet())
{
    Console.WriteLine($"  EntrySet: {day} x {count}");
}

// Contains 语义与 MultiList 一致：至少一份即 True。
// Contains semantics match MultiList: True as long as there is at least one copy.
Console.WriteLine($"Contains(Monday) = {bag.Contains(DayOfWeek.Monday)}"); // True

// ---------------------------------------------------------------------------
// 2. 删除与末份弃项
// 2. Deletion and dropping the last copy
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 删除与末份弃项 ===");
Console.WriteLine("=== 2. Deletion and dropping the last copy ===");

// Remove(item, times) 删最多 times 份，返回**剩余副本数**（不是 bool）。
// Remove(item, times) removes at most times copies and returns the **remaining copy count** (not a bool).
int remaining = bag.Remove(DayOfWeek.Monday, 2);
Console.WriteLine($"Remove(Monday, 2) 后剩余 / remaining after Remove(Monday, 2) = {remaining}");     // 1
Console.WriteLine($"TotalCount = {bag.TotalCount}");             // 2（Monday 1 份 + Friday 1 份）
                                                                 // 2 (1 copy of Monday + 1 copy of Friday)

// Remove(item) 等价删一份。
// Remove(item) is equivalent to removing one copy.
Console.WriteLine($"Remove(Monday) 后剩余 / remaining after Remove(Monday) = {bag.Remove(DayOfWeek.Monday)}"); // 0：末份删除即弃项
                                                                                                              // 0: deleting the last copy drops the entry
Console.WriteLine($"DistinctCount = {bag.DistinctCount}");        // 1：Monday 已被整体丢弃
                                                                   // 1: Monday has been discarded entirely
Console.WriteLine($"Contains(Monday) = {bag.Contains(DayOfWeek.Monday)}"); // False

// 删除不存在的元素返回 0（不抛）。
// Removing a non-existent element returns 0 (no throw).
Console.WriteLine($"Remove(Sunday) 后剩余 / remaining after Remove(Sunday) = {bag.Remove(DayOfWeek.Sunday)}"); // 0
// RemoveAllCopies 删光某元素全部副本，返回是否真的删掉了。
// RemoveAllCopies removes all copies of an element and returns whether anything was actually removed.
Console.WriteLine($"RemoveAllCopies(Friday) = {bag.RemoveAllCopies(DayOfWeek.Friday)}"); // True
Console.WriteLine($"IsEmpty = {bag.IsEmpty}");                    // True

// ⚠️ times <= 0 会抛 ArgumentOutOfRangeException（与 MultiList 一致，不静默改写为「一份」）。
// ⚠️ times <= 0 throws ArgumentOutOfRangeException (consistent with MultiList, not silently rewritten to "one copy").
try
{
    bag.Add(DayOfWeek.Monday, 0);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"Add(item, 0) 被拒 / Add(item, 0) rejected: {ex.ParamName}");        // times
}

// ---------------------------------------------------------------------------
// 3. 批量、导出与克隆
// 3. Bulk, export and clone
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 批量 / 导出 / 克隆 ===");
Console.WriteLine("=== 3. Bulk / export / clone ===");

var ints = new PackedBag<int>();
ints.AddRange(new[] { 7, 7, 8, 8, 8, 9 });   // 7 x2, 8 x3, 9 x1

Console.WriteLine($"TotalCount    = {ints.TotalCount}");          // 6
Console.WriteLine($"DistinctCount = {ints.DistinctCount}");       // 3

// ToDictionary 导出「元素 -> 副本数」快照；T 是 struct，不可能为 null，故不会抛。
// ToDictionary exports an "element -> copy count" snapshot; T is a struct and can never be null, so it does not throw.
var histogram = ints.ToDictionary();
Console.WriteLine($"ToDictionary[8] = {histogram[8]}");           // 3

// DistinctItems 只列不同元素；顺序对直方图无契约，这里排序后打印以获得确定性输出。
// DistinctItems lists only the distinct elements; the order has no contract for a histogram, so we sort before printing to get deterministic output.
var distinct = new System.Collections.Generic.List<int>(ints.DistinctItems());
distinct.Sort();
Console.WriteLine($"DistinctItems = [{string.Join(", ", distinct)}]"); // [7, 8, 9]

// Clone 独立副本：改克隆不影响源。
// Clone is an independent copy: modifying the clone does not affect the source.
var clone = ints.Clone();
clone.Add(7);
Console.WriteLine($"clone.CountOf(7) = {clone.CountOf(7)}, ints.CountOf(7) = {ints.CountOf(7)}"); // 3 / 2

// ---------------------------------------------------------------------------
// 4. 边界：只接受值类型
// 4. Boundary: value types only
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 边界 ===");
Console.WriteLine("=== 4. Boundary ===");

// ❌ 错误写法：PackedBag<string> —— string 是引用类型，where T : struct 在编译期拒绝。
// ❌ Wrong usage: PackedBag<string> — string is a reference type, rejected at compile time by where T : struct.
//    var refBag = new PackedBag<string>();   // 无法编译
//                                            // does not compile
//    ✅ 正确写法：引用类型计数用 MultiList<string>；小值域堆上袋用 PackedBag<int> / 枚举。
//    ✅ Correct: use MultiList<string> to count reference types; use PackedBag<int> / an enum for a heap bag over a small value domain.
// 约束同时排除 Nullable<T> 本身；比较固定 EqualityComparer<T>.Default。
// The constraint also excludes Nullable<T> itself; comparison is fixed to EqualityComparer<T>.Default.
// ⚠️ 选型分界：约 ≤16 个不同值用 PackedBag；~32 以上 MultiList<T> 的字典 O(1) 反超。
// ⚠️ Selection boundary: use PackedBag for roughly ≤16 distinct values; beyond ~32 the dictionary O(1) of MultiList<T> overtakes it.
Console.WriteLine("PackedBag<T> 要求 T : struct；引用类型请用 MultiList<T>。 / PackedBag<T> requires T : struct; use MultiList<T> for reference types.");

Console.WriteLine();
Console.WriteLine("Sample.PackedBag completed.");
