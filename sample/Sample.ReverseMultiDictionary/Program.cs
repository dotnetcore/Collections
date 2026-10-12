// Sample.ReverseMultiDictionary — 反向多重字典（倒排快照）（6.3.0）
// Sample.ReverseMultiDictionary — reverse multidictionary (inverted snapshot) (6.3.0)
// 运行：dotnet run --project sample/Sample.ReverseMultiDictionary
// Run: dotnet run --project sample/Sample.ReverseMultiDictionary
//
// 演示重点：
// Highlights:
//   1. ReverseMultiDictionary<V,K> 是 MultiDictionary 的**倒排快照**：构造时拷出绑定。
//   1. ReverseMultiDictionary<V,K> is an **inverted snapshot** of MultiDictionary: it copies the bindings out at construction.
//   2. 源后续变化不影响快照；快照自身仍可独立增删。
//   2. Later source changes do not affect the snapshot; the snapshot itself remains independently mutable.
//   3. 成员名与正向 map 轴对调：ValueCount / TotalKeyCount / KeyCount(V)。
//   3. Member names swap axes relative to the forward map: ValueCount / TotalKeyCount / KeyCount(V).
//   4. 6.7.1 AsReverse 全家桶对照：活视图 / 有序拷贝 / 独立拷贝。
//   4. Comparison with the 6.7.1 AsReverse family: live view / ordered copy / independent copy.
using System;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 从源构造快照（拷贝，不跟源）
// 1. Build a snapshot from the source (a copy, does not follow the source)
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 倒排快照 ===");
Console.WriteLine("=== 1. Inverted snapshot ===");

var map = new MultiDictionary<string, int>();
map.Add("orders", 1001);
map.Add("customers", 1001);
map.Add("customers", 1002);

// 构造器把 (value, key) 绑定拷进自包含实例；之后源可随便改。
// The constructor copies the (value, key) bindings into a self-contained instance; afterwards the source may change freely.
var inverted = new ReverseMultiDictionary<int, string>(map);
Console.WriteLine($"inverted[1001] = {string.Join(",", inverted[1001].OrderBy(k => k))}"); // customers,orders
Console.WriteLine($"inverted[1001].Count = {inverted[1001].Count}"); // 2

// 源后续变化不影响快照。
// Later source changes do not affect the snapshot.
map.Add("invoices", 1001);
Console.WriteLine($"源加 invoices 后 inverted[1001].Count / inverted[1001].Count after the source adds invoices = {inverted[1001].Count}"); // 仍 2 / still 2

// ---------------------------------------------------------------------------
// 2. 轴对调命名与缺值语义
// 2. Axis-swapped naming and missing-value semantics
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 轴对调命名 ===");
Console.WriteLine("=== 2. Axis-swapped naming ===");

Console.WriteLine($"ValueCount     = {inverted.ValueCount}");     // 2：不同值数（1001/1002）/ 2: distinct value count (1001/1002)
Console.WriteLine($"TotalKeyCount  = {inverted.TotalKeyCount}");  // 3：键出现总数 / 3: total key occurrences
Console.WriteLine($"KeyCount(1001) = {inverted.KeyCount(1001)}"); // 2
Console.WriteLine($"ContainsValue(1002)   = {inverted.ContainsValue(1002)}"); // True：O(1) 存在性 / True: O(1) existence
Console.WriteLine($"ContainsKey(customers) = {inverted.ContainsKey("customers")}"); // True：扫描键 / True: scans the keys
// 缺值返回空集合而非抛。
// A missing value returns an empty collection rather than throwing.
Console.WriteLine($"inverted[9999].Count = {inverted[9999].Count}"); // 0

// ---------------------------------------------------------------------------
// 3. 快照自身独立可变 + 「无空内层」不变式
// 3. The snapshot is independently mutable + the "no empty inner set" invariant
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 独立可变与不变式 ===");
Console.WriteLine("=== 3. Independent mutability and invariants ===");

inverted.Add(1003, "ledger");
Console.WriteLine($"Add 后 ValueCount / ValueCount after Add = {inverted.ValueCount}"); // 3
inverted.Remove(1003, "ledger");   // 删掉该值最后一个键 / removes the last key of that value
Console.WriteLine($"Remove 后 ContainsValue(1003) / ContainsValue(1003) after Remove = {inverted.ContainsValue(1003)}"); // False：该值自动消失 / False: the value disappears automatically

// ---------------------------------------------------------------------------
// 4. 6.7.1 AsReverse 全家桶对照
// 4. Comparison with the 6.7.1 AsReverse family
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. AsReverse 全家桶（6.7.1）===");
Console.WriteLine("=== 4. AsReverse family (6.7.1) ===");

// MultiDictionary.AsReverse()：零拷贝**活**视图（随源联动），与快照互为两个入口。
// MultiDictionary.AsReverse(): a zero-copy **live** view (linked to the source), the two entry points complementing the snapshot.
var live = map.AsReverse();
Console.WriteLine($"map.AsReverse().ContainsKey(1001) = {live.ContainsKey(1001)}"); // True

// OrderedMultiDictionary.AsReverse()：**有序拷贝**（键身份是 IComparer，哈希反转会重定义键）。
// OrderedMultiDictionary.AsReverse(): an **ordered copy** (key identity is the IComparer; a hash reversal would redefine the keys).
var ordered = new OrderedMultiDictionary<string, int>();
ordered.Add("orders", 1001);
ordered.Add("customers", 1001);
var orderedRev = ordered.AsReverse();   // OrderedMultiDictionary<int, string>
Console.WriteLine($"ordered.AsReverse()[1001] = {string.Join(",", orderedRev[1001])}"); // customers,orders（升序）/ customers,orders (ascending)

// BiDictionary.AsReverse()：**活**只读视图。
// BiDictionary.AsReverse(): a **live** read-only view.
var bi = new BiDictionary<int, string>();
bi.Add(1, "alice");
Console.WriteLine($"bi.AsReverse()[alice] = {bi.AsReverse()["alice"]}"); // 1

// TwoKeyDictionary.AsReverse()：**独立拷贝**（轴对调，保留每轴投影面）。
// TwoKeyDictionary.AsReverse(): an **independent copy** (axes swapped, each axis's projection surface preserved).
var two = new TwoKeyDictionary<int, string, decimal>();
two[1, "USD"] = 1m;
Console.WriteLine($"two.AsReverse().GetByFirstKey(USD).Count = {two.AsReverse().GetByFirstKey("USD").Count()}"); // 1

Console.WriteLine();
Console.WriteLine("Sample.ReverseMultiDictionary completed.");
