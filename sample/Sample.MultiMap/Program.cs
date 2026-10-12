// Sample.MultiMap — 多重映射 MultiDictionary<TKey,TValue>（6.0.0）
// Sample.MultiMap — Multimap MultiDictionary<TKey,TValue> (6.0.0)
// 运行：dotnet run --project sample/Sample.MultiMap
// Run: dotnet run --project sample/Sample.MultiMap
//
// 演示重点：
// Highlights:
//   1. 一键多值：Add / 索引器 / ValueCount；缺键索引器返回空集合，永不 null、永不抛。
//   1. One key, many values: Add / indexer / ValueCount; the missing-key indexer returns an empty collection, never null, never throws.
//   2. Count 与 KeyCount 都是**键数**，值总数是 TotalValueCount——本包高频口径陷阱。
//   2. Count and KeyCount are both the **number of keys**; the total number of values is TotalValueCount — a high-frequency semantic trap in this package.
//   3. 批量添加与按键集合运算：AddRange / UnionWith / RemoveRange / ExceptWith。
//   3. Batch adding and per-key set operations: AddRange / UnionWith / RemoveRange / ExceptWith.
//   4. Values 是**扁平值序列**（用 new 隐藏了继承来的「每键一个集合」序列），按组请用 ToDictionary()。
//   4. Values is a **flat value sequence** (it uses new to hide the inherited "one collection per key" sequence); for grouping use ToDictionary().
using System;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 一键多值：加值、读计数、缺键友好
// 1. One key, many values: add values, read counts, missing-key friendliness
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 添加与查询 ===");
Console.WriteLine("=== 1. Add and query ===");

var map = new MultiDictionary<string, int>();   // 默认内层是 List，同一键下可存重复值
                                                // the inner collection is a List by default, so the same key can hold duplicate values
map.Add("orders", 1001);
map.Add("orders", 1002);
map.Add("customers", 1001);

// Count 与 KeyCount 都是**键数**（这里是 2），不是值总数。
// Count and KeyCount are both the **number of keys** (here 2), not the total number of values.
Console.WriteLine($"Count           = {map.Count}");            // 2：键数
                                                                 // 2: number of keys
Console.WriteLine($"KeyCount        = {map.KeyCount}");         // 2：与 Count 同义
                                                                 // 2: synonymous with Count
Console.WriteLine($"TotalValueCount = {map.TotalValueCount}");  // 3：全部值数
                                                                 // 3: total number of values
Console.WriteLine($"ValueCount(orders) = {map.ValueCount("orders")}"); // 2
Console.WriteLine($"ValueCount(nope)   = {map.ValueCount("nope")}");   // 0：缺键返回 0
                                                                        // 0: a missing key returns 0

// 索引器对缺键返回**空集合**（静态共享的 EmptyValues），与 Dictionary 的 KeyNotFoundException 不同。
// The indexer returns an **empty collection** for a missing key (the statically shared EmptyValues), unlike Dictionary's KeyNotFoundException.
var missing = map["nope"];
Console.WriteLine($"map[\"nope\"].Count = {missing.Count}");          // 0
Console.WriteLine($"ContainsKey(nope)  = {map.ContainsKey("nope")}"); // False：想区分「缺键」要用 ContainsKey / TryGetValue
                                                                      // False: to distinguish a "missing key" use ContainsKey / TryGetValue

// null 键被拒绝（与 Dictionary 一致），null 值允许。
// A null key is rejected (consistent with Dictionary); a null value is allowed.
try
{
    map.Add(null!, 1);
}
catch (ArgumentNullException ex)
{
    Console.WriteLine($"Add(null, 1) 被拒 / Add(null, 1) rejected: {ex.ParamName}");   // key
}

// ---------------------------------------------------------------------------
// 2. 批量添加与按键集合运算
// 2. Batch adding and per-key set operations
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 批量添加与按键集合运算 ===");
Console.WriteLine("=== 2. Batch adding and per-key set operations ===");

var orders = new MultiDictionary<string, int>();
orders.AddRange("orders", new[] { 1001, 1002, 1003 });   // 一次把一批值挂到同一键下
                                                          // attach a batch of values to the same key in one call
Console.WriteLine($"AddRange 后 ValueCount(orders) / ValueCount(orders) after AddRange = {orders.ValueCount("orders")}");  // 3

// UnionWith：对某个键的值集合做 set 并——已存在的值不会再加一次。
// UnionWith: set-union on the value collection of a key — an already-present value is not added again.
orders.UnionWith("orders", new[] { 1003, 1004 });
Console.WriteLine($"UnionWith 后含 1004 / Contains 1004 after UnionWith = {orders.Contains("orders", 1004)}");  // True
Console.WriteLine($"UnionWith 后 ValueCount / ValueCount after UnionWith = {orders.ValueCount("orders")}"); // 4（1001,1002,1003,1004）
                                                                                                            // 4 (1001,1002,1003,1004)

// RemoveRange：对参数**去重后**的每个不同值删掉一次出现。
// RemoveRange: after **de-duplicating** the argument, remove one occurrence of each distinct value.
bool removed = orders.RemoveRange("orders", new[] { 1002, 1003 });
Console.WriteLine($"RemoveRange 返回 / RemoveRange returned = {removed}");                             // True：至少删掉一个
                                                                                                       // True: at least one was removed
Console.WriteLine($"RemoveRange 后 ValueCount / ValueCount after RemoveRange = {orders.ValueCount("orders")}"); // 2（1001,1004）
                                                                                                                  // 2 (1001,1004)

// ExceptWith：删掉匹配值的**全部**出现（与 RemoveRange 只删一次不同，详见 Sample.MultiMapBatchDelete）。
// ExceptWith: remove **all** occurrences of matching values (unlike RemoveRange which removes only one; see Sample.MultiMapBatchDelete for details).
orders.ExceptWith("orders", new[] { 1001 });
Console.WriteLine($"ExceptWith 后含 1001 / Contains 1001 after ExceptWith = {orders.Contains("orders", 1001)}"); // False

// 内层集合清空后键自动消失——映射从不持有「无值之键」。
// Once the inner collection is emptied the key disappears automatically — the map never holds a "key without values".
orders.RemoveRange("orders", new[] { 1004 });
Console.WriteLine($"删空后 ContainsKey(orders) / ContainsKey(orders) after emptying = {orders.ContainsKey("orders")}"); // False

// ---------------------------------------------------------------------------
// 3. Values 是扁平序列，不是按键分组
// 3. Values is a flat sequence, not grouped by key
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. Values 是扁平序列 ===");
Console.WriteLine("=== 3. Values is a flat sequence ===");

var flat = new MultiDictionary<string, int>();
flat.Add("a", 1);
flat.Add("a", 2);
flat.Add("b", 3);

// map.Values 在具体类型与 IMultiDictionary 上都是**扁平**的 IEnumerable<TValue>，长度等于 TotalValueCount。
// map.Values is a **flat** IEnumerable<TValue> on both the concrete type and IMultiDictionary, with length equal to TotalValueCount.
// 它用 new 隐藏了继承自 IReadOnlyDictionary.Values 的「每键一个集合」序列——这是高风险混淆点。
// It uses new to hide the "one collection per key" sequence inherited from IReadOnlyDictionary.Values — a high-risk confusion point.
var flatValues = flat.Values.OrderBy(v => v).ToArray();   // 排序仅为让输出确定
                                                          // sorting only to make the output deterministic
Console.WriteLine($"扁平 Values / Flat Values = [{string.Join(",", flatValues)}]（共 {flatValues.Length} 个 / {flatValues.Length} items）");  // 1,2,3

// 要按键分组：ToDictionary() 给出「键 -> 值集合」的快照。
// To group by key: ToDictionary() gives a "key -> value collection" snapshot.
foreach (var pair in flat.ToDictionary().OrderBy(p => p.Key, StringComparer.Ordinal))
{
    Console.WriteLine($"  ToDictionary: {pair.Key} -> [{string.Join(",", pair.Value.OrderBy(v => v))}]");
}

Console.WriteLine();
Console.WriteLine("Sample.MultiMap completed.");
