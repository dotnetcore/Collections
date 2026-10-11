// Sample.PackedBag — 打包值类型袋（F21）
// 运行：dotnet run --project sample/Sample.PackedBag
//
// 演示重点：
//   1. PackedBag<T> where T : struct 是**密集数组直方图**：存为连续 (value, count) 结构体数组，
//      存储路径无装箱、无每项对象开销，适合约 ≤16 个不同值的枚举 / 小整数直方图。
//   2. 袋语义镜像 MultiList<T>：Add 加副本、Remove 去一份并返回剩余副本数、枚举副本展开；
//      末份删除即弃项（尾移保持 packed，无零计数空洞）。
//   3. 刻意**不带**集合运算 / 结构相等 / 自定义比较器——它是直方图，不是通用多重集合。
using System;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 枚举直方图：T : struct，无装箱存储
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 枚举直方图 ===");

// DayOfWeek 是值类型（枚举），满足 where T : struct；元素内联在连续数组里。
var bag = new PackedBag<DayOfWeek>();
bag.Add(DayOfWeek.Monday, 3);   // Add(item, times) 一次加 3 份
bag.Add(DayOfWeek.Friday);      // Add(item) 加 1 份

Console.WriteLine($"CountOf(Monday) = {bag.CountOf(DayOfWeek.Monday)}");   // 3
Console.WriteLine($"TotalCount      = {bag.TotalCount}");                 // 4：副本数
Console.WriteLine($"DistinctCount   = {bag.DistinctCount}");              // 2：不同元素数
Console.WriteLine($"IsEmpty         = {bag.IsEmpty}");                    // False
Console.WriteLine($"Capacity        = {bag.Capacity}");                   // 内部条目数组容量（≥2）

// EntrySet() 是「元素 + 副本数」的紧凑直方图；项数 == DistinctCount。
foreach (var (day, count) in bag.EntrySet())
{
    Console.WriteLine($"  EntrySet: {day} x {count}");
}

// Contains 语义与 MultiList 一致：至少一份即 True。
Console.WriteLine($"Contains(Monday) = {bag.Contains(DayOfWeek.Monday)}"); // True

// ---------------------------------------------------------------------------
// 2. 删除与末份弃项
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 删除与末份弃项 ===");

// Remove(item, times) 删最多 times 份，返回**剩余副本数**（不是 bool）。
int remaining = bag.Remove(DayOfWeek.Monday, 2);
Console.WriteLine($"Remove(Monday, 2) 后剩余 = {remaining}");     // 1
Console.WriteLine($"TotalCount = {bag.TotalCount}");             // 2（Monday 1 份 + Friday 1 份）

// Remove(item) 等价删一份。
Console.WriteLine($"Remove(Monday) 后剩余 = {bag.Remove(DayOfWeek.Monday)}"); // 0：末份删除即弃项
Console.WriteLine($"DistinctCount = {bag.DistinctCount}");        // 1：Monday 已被整体丢弃
Console.WriteLine($"Contains(Monday) = {bag.Contains(DayOfWeek.Monday)}"); // False

// 删除不存在的元素返回 0（不抛）。
Console.WriteLine($"Remove(Sunday) 后剩余 = {bag.Remove(DayOfWeek.Sunday)}"); // 0
// RemoveAllCopies 删光某元素全部副本，返回是否真的删掉了。
Console.WriteLine($"RemoveAllCopies(Friday) = {bag.RemoveAllCopies(DayOfWeek.Friday)}"); // True
Console.WriteLine($"IsEmpty = {bag.IsEmpty}");                    // True

// ⚠️ times <= 0 会抛 ArgumentOutOfRangeException（与 MultiList 一致，不静默改写为「一份」）。
try
{
    bag.Add(DayOfWeek.Monday, 0);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"Add(item, 0) 被拒：{ex.ParamName}");        // times
}

// ---------------------------------------------------------------------------
// 3. 批量、导出与克隆
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 批量 / 导出 / 克隆 ===");

var ints = new PackedBag<int>();
ints.AddRange(new[] { 7, 7, 8, 8, 8, 9 });   // 7 x2, 8 x3, 9 x1

Console.WriteLine($"TotalCount    = {ints.TotalCount}");          // 6
Console.WriteLine($"DistinctCount = {ints.DistinctCount}");       // 3

// ToDictionary 导出「元素 -> 副本数」快照；T 是 struct，不可能为 null，故不会抛。
var histogram = ints.ToDictionary();
Console.WriteLine($"ToDictionary[8] = {histogram[8]}");           // 3

// DistinctItems 只列不同元素；顺序对直方图无契约，这里排序后打印以获得确定性输出。
var distinct = new System.Collections.Generic.List<int>(ints.DistinctItems());
distinct.Sort();
Console.WriteLine($"DistinctItems = [{string.Join(", ", distinct)}]"); // [7, 8, 9]

// Clone 独立副本：改克隆不影响源。
var clone = ints.Clone();
clone.Add(7);
Console.WriteLine($"clone.CountOf(7) = {clone.CountOf(7)}, ints.CountOf(7) = {ints.CountOf(7)}"); // 3 / 2

// ---------------------------------------------------------------------------
// 4. 边界：只接受值类型
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 边界 ===");

// ❌ 错误写法：PackedBag<string> —— string 是引用类型，where T : struct 在编译期拒绝。
//    var refBag = new PackedBag<string>();   // 无法编译
//    ✅ 正确写法：引用类型计数用 MultiList<string>；小值域堆上袋用 PackedBag<int> / 枚举。
// 约束同时排除 Nullable<T> 本身；比较固定 EqualityComparer<T>.Default。
// ⚠️ 选型分界：约 ≤16 个不同值用 PackedBag；~32 以上 MultiList<T> 的字典 O(1) 反超。
Console.WriteLine("PackedBag<T> 要求 T : struct；引用类型请用 MultiList<T>。");

Console.WriteLine();
Console.WriteLine("Sample.PackedBag completed.");
