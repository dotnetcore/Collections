// Sample.MultiMapBatchDelete — 多重映射批量删除 RemoveRange（F04）
// 运行：dotnet run --project sample/Sample.MultiMapBatchDelete
//
// 演示重点：
//   1. RemoveRange(key, values) 是 Remove(key, value) 的批量形：对参数**去重后**的每个不同值删一次出现。
//   2. 返回 bool 表示「是否至少删掉一个值」；缺键 / 全不匹配是返回 false 的空操作，不抛。
//   3. 与 ExceptWith 对照：RemoveRange 每个不同值删一次，ExceptWith 删匹配值的全部出现。
using System;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 批量删值
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 批量删值 ===");

var map = new MultiDictionary<string, int>();
map.AddRange("orders", new[] { 1001, 1002, 1003 });

bool removed = map.RemoveRange("orders", new[] { 1002, 1003 });
Console.WriteLine($"RemoveRange 返回 = {removed}");                    // True：至少删掉一个
Console.WriteLine($"ValueCount(orders) = {map.ValueCount("orders")}"); // 1（只剩 1001）

bool noOp = map.RemoveRange("orders", new[] { 9999 });
Console.WriteLine($"全不匹配返回 = {noOp}");                           // False
Console.WriteLine($"ValueCount(orders) = {map.ValueCount("orders")}"); // 1：空操作，键保留

bool missing = map.RemoveRange("nope", new[] { 1 });
Console.WriteLine($"缺键返回 = {missing}");                            // False：缺键是空操作，不抛

// ---------------------------------------------------------------------------
// 2. 参数是集合语义：重复值不计两次
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 参数是集合语义 ===");

var dup = new MultiDictionary<string, int>(allowDuplicateValues: true);
dup.AddRange("m", new[] { 5, 5, 5 });     // 内层允许重复 => m 有 3 份 5

// 参数里写了三个 5，但去重后只有一个不同值 => 只删 1 份。
dup.RemoveRange("m", new[] { 5, 5, 5 });
Console.WriteLine($"RemoveRange(三个5) 后 ValueCount = {dup.ValueCount("m")}");   // 2
// ⚠️ 与 AddRange 的多重语义刻意不同：AddRange 按份加，RemoveRange 按**不同值**各删一次出现。

// ---------------------------------------------------------------------------
// 3. 与 ExceptWith 对照
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 与 ExceptWith 对照 ===");

var cmp = new MultiDictionary<string, int>(allowDuplicateValues: true);
cmp.Add("k", 7);
cmp.Add("k", 7);
cmp.Add("k", 8);
Console.WriteLine($"初始 k = [{string.Join(",", cmp["k"].OrderBy(x => x))}]");   // 7,7,8

cmp.RemoveRange("k", new[] { 7 });   // 每个不同值删一次出现 => 7 还剩 1 份
Console.WriteLine($"RemoveRange 后 k = [{string.Join(",", cmp["k"].OrderBy(x => x))}]");  // 7,8

cmp.ExceptWith("k", new[] { 7 });    // 删 7 的**全部**出现
Console.WriteLine($"ExceptWith 后 k = [{string.Join(",", cmp["k"])}]");   // 8
// 记忆：RemoveRange = 每个不同值删一次出现；ExceptWith = 删匹配值的全部出现。

// ❌ 错误写法：以为 RemoveRange 会删掉同值的全部副本——允许重复时它只删一份；要删全部用 ExceptWith。

Console.WriteLine();
Console.WriteLine("Sample.MultiMapBatchDelete completed.");
