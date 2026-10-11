// Sample.MultiMap — 多重映射 MultiDictionary<TKey,TValue>（F02）
// 运行：dotnet run --project sample/Sample.MultiMap
//
// 演示重点：
//   1. 一键多值：Add / 索引器 / ValueCount；缺键索引器返回空集合，永不 null、永不抛。
//   2. Count 与 KeyCount 都是**键数**，值总数是 TotalValueCount——本包高频口径陷阱。
//   3. 批量添加与按键集合运算：AddRange / UnionWith / RemoveRange / ExceptWith。
//   4. Values 是**扁平值序列**（用 new 隐藏了继承来的「每键一个集合」序列），按组请用 ToDictionary()。
using System;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 一键多值：加值、读计数、缺键友好
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 添加与查询 ===");

var map = new MultiDictionary<string, int>();   // 默认内层是 List，同一键下可存重复值
map.Add("orders", 1001);
map.Add("orders", 1002);
map.Add("customers", 1001);

// Count 与 KeyCount 都是**键数**（这里是 2），不是值总数。
Console.WriteLine($"Count           = {map.Count}");            // 2：键数
Console.WriteLine($"KeyCount        = {map.KeyCount}");         // 2：与 Count 同义
Console.WriteLine($"TotalValueCount = {map.TotalValueCount}");  // 3：全部值数
Console.WriteLine($"ValueCount(orders) = {map.ValueCount("orders")}"); // 2
Console.WriteLine($"ValueCount(nope)   = {map.ValueCount("nope")}");   // 0：缺键返回 0

// 索引器对缺键返回**空集合**（静态共享的 EmptyValues），与 Dictionary 的 KeyNotFoundException 不同。
var missing = map["nope"];
Console.WriteLine($"map[\"nope\"].Count = {missing.Count}");          // 0
Console.WriteLine($"ContainsKey(nope)  = {map.ContainsKey("nope")}"); // False：想区分「缺键」要用 ContainsKey / TryGetValue

// null 键被拒绝（与 Dictionary 一致），null 值允许。
try
{
    map.Add(null!, 1);
}
catch (ArgumentNullException ex)
{
    Console.WriteLine($"Add(null, 1) 被拒：{ex.ParamName}");   // key
}

// ---------------------------------------------------------------------------
// 2. 批量添加与按键集合运算
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 批量添加与按键集合运算 ===");

var orders = new MultiDictionary<string, int>();
orders.AddRange("orders", new[] { 1001, 1002, 1003 });   // 一次把一批值挂到同一键下
Console.WriteLine($"AddRange 后 ValueCount(orders) = {orders.ValueCount("orders")}");  // 3

// UnionWith：对某个键的值集合做 set 并——已存在的值不会再加一次。
orders.UnionWith("orders", new[] { 1003, 1004 });
Console.WriteLine($"UnionWith 后含 1004 = {orders.Contains("orders", 1004)}");  // True
Console.WriteLine($"UnionWith 后 ValueCount = {orders.ValueCount("orders")}"); // 4（1001,1002,1003,1004）

// RemoveRange：对参数**去重后**的每个不同值删掉一次出现。
bool removed = orders.RemoveRange("orders", new[] { 1002, 1003 });
Console.WriteLine($"RemoveRange 返回 = {removed}");                             // True：至少删掉一个
Console.WriteLine($"RemoveRange 后 ValueCount = {orders.ValueCount("orders")}"); // 2（1001,1004）

// ExceptWith：删掉匹配值的**全部**出现（与 RemoveRange 只删一次不同，详见 Sample.MultiMapBatchDelete）。
orders.ExceptWith("orders", new[] { 1001 });
Console.WriteLine($"ExceptWith 后含 1001 = {orders.Contains("orders", 1001)}"); // False

// 内层集合清空后键自动消失——映射从不持有「无值之键」。
orders.RemoveRange("orders", new[] { 1004 });
Console.WriteLine($"删空后 ContainsKey(orders) = {orders.ContainsKey("orders")}"); // False

// ---------------------------------------------------------------------------
// 3. Values 是扁平序列，不是按键分组
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. Values 是扁平序列 ===");

var flat = new MultiDictionary<string, int>();
flat.Add("a", 1);
flat.Add("a", 2);
flat.Add("b", 3);

// map.Values 在具体类型与 IMultiDictionary 上都是**扁平**的 IEnumerable<TValue>，长度等于 TotalValueCount。
// 它用 new 隐藏了继承自 IReadOnlyDictionary.Values 的「每键一个集合」序列——这是高风险混淆点。
var flatValues = flat.Values.OrderBy(v => v).ToArray();   // 排序仅为让输出确定
Console.WriteLine($"扁平 Values = [{string.Join(",", flatValues)}]（共 {flatValues.Length} 个）");  // 1,2,3

// 要按键分组：ToDictionary() 给出「键 -> 值集合」的快照。
foreach (var pair in flat.ToDictionary().OrderBy(p => p.Key, StringComparer.Ordinal))
{
    Console.WriteLine($"  ToDictionary: {pair.Key} -> [{string.Join(",", pair.Value.OrderBy(v => v))}]");
}

Console.WriteLine();
Console.WriteLine("Sample.MultiMap completed.");
