// Sample.MultiMapBatchDelete — 多重映射批量删除 RemoveRange（6.1.0）
// Sample.MultiMapBatchDelete — Multimap batch delete RemoveRange (6.1.0)
// 运行：dotnet run --project sample/Sample.MultiMapBatchDelete
// Run: dotnet run --project sample/Sample.MultiMapBatchDelete
//
// 演示重点：
// Highlights:
//   1. RemoveRange(key, values) 是 Remove(key, value) 的批量形：对参数**去重后**的每个不同值删一次出现。
//   1. RemoveRange(key, values) is the bulk form of Remove(key, value): after **de-duplicating** the argument, remove one occurrence of each distinct value.
//   2. 返回 bool 表示「是否至少删掉一个值」；缺键 / 全不匹配是返回 false 的空操作，不抛。
//   2. The bool return indicates "whether at least one value was removed"; a missing key / no matches are no-ops that return false and do not throw.
//   3. 与 ExceptWith 对照：RemoveRange 每个不同值删一次，ExceptWith 删匹配值的全部出现。
//   3. Contrast with ExceptWith: RemoveRange removes each distinct value once, while ExceptWith removes all occurrences of matching values.
using System;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 批量删值
// 1. Bulk delete values
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 批量删值 ===");
Console.WriteLine("=== 1. Bulk delete values ===");

var map = new MultiDictionary<string, int>();
map.AddRange("orders", new[] { 1001, 1002, 1003 });

bool removed = map.RemoveRange("orders", new[] { 1002, 1003 });
Console.WriteLine($"RemoveRange 返回 / RemoveRange returned = {removed}");                    // True：至少删掉一个
                                                                                             // True: at least one was removed
Console.WriteLine($"ValueCount(orders) = {map.ValueCount("orders")}"); // 1（只剩 1001）
                                                                       // 1 (only 1001 remains)

bool noOp = map.RemoveRange("orders", new[] { 9999 });
Console.WriteLine($"全不匹配返回 / No matches returned = {noOp}");                           // False
Console.WriteLine($"ValueCount(orders) = {map.ValueCount("orders")}"); // 1：空操作，键保留
                                                                       // 1: no-op, the key is kept

bool missing = map.RemoveRange("nope", new[] { 1 });
Console.WriteLine($"缺键返回 / Missing key returned = {missing}");                            // False：缺键是空操作，不抛
                                                                                              // False: a missing key is a no-op and does not throw

// ---------------------------------------------------------------------------
// 2. 参数是集合语义：重复值不计两次
// 2. The argument has set semantics: duplicate values are not counted twice
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 参数是集合语义 ===");
Console.WriteLine("=== 2. The argument has set semantics ===");

var dup = new MultiDictionary<string, int>(allowDuplicateValues: true);
dup.AddRange("m", new[] { 5, 5, 5 });     // 内层允许重复 => m 有 3 份 5
                                          // duplicates are allowed in the inner collection => m holds 3 copies of 5

// 参数里写了三个 5，但去重后只有一个不同值 => 只删 1 份。
// The argument lists three 5s, but after de-duplication there is only one distinct value => only 1 copy is removed.
dup.RemoveRange("m", new[] { 5, 5, 5 });
Console.WriteLine($"RemoveRange(三个5) 后 ValueCount / ValueCount after RemoveRange(three 5s) = {dup.ValueCount("m")}");   // 2
// ⚠️ 与 AddRange 的多重语义刻意不同：AddRange 按份加，RemoveRange 按**不同值**各删一次出现。
// ⚠️ Deliberately different from AddRange's multiset semantics: AddRange adds by copy, while RemoveRange removes one occurrence per **distinct value**.

// ---------------------------------------------------------------------------
// 3. 与 ExceptWith 对照
// 3. Contrast with ExceptWith
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 与 ExceptWith 对照 ===");
Console.WriteLine("=== 3. Contrast with ExceptWith ===");

var cmp = new MultiDictionary<string, int>(allowDuplicateValues: true);
cmp.Add("k", 7);
cmp.Add("k", 7);
cmp.Add("k", 8);
Console.WriteLine($"初始 k / Initial k = [{string.Join(",", cmp["k"].OrderBy(x => x))}]");   // 7,7,8

cmp.RemoveRange("k", new[] { 7 });   // 每个不同值删一次出现 => 7 还剩 1 份
                                     // remove one occurrence per distinct value => 1 copy of 7 remains
Console.WriteLine($"RemoveRange 后 k / k after RemoveRange = [{string.Join(",", cmp["k"].OrderBy(x => x))}]");  // 7,8

cmp.ExceptWith("k", new[] { 7 });    // 删 7 的**全部**出现
                                     // remove **all** occurrences of 7
Console.WriteLine($"ExceptWith 后 k / k after ExceptWith = [{string.Join(",", cmp["k"])}]");   // 8
// 记忆：RemoveRange = 每个不同值删一次出现；ExceptWith = 删匹配值的全部出现。
// Mnemonic: RemoveRange = remove one occurrence per distinct value; ExceptWith = remove all occurrences of matching values.

// ❌ 错误写法：以为 RemoveRange 会删掉同值的全部副本——允许重复时它只删一份；要删全部用 ExceptWith。
// ❌ Wrong: assuming RemoveRange removes all copies of a value — with duplicates allowed it removes only one; to remove all, use ExceptWith.

Console.WriteLine();
Console.WriteLine("Sample.MultiMapBatchDelete completed.");
