// Sample.OrderedMultiSet — 有序多重集合 OrderedMultiList<T>（F05）
// 运行：dotnet run --project sample/Sample.OrderedMultiSet
//
// 演示重点：
//   1. OrderedMultiList<T> 是「有序 bag」：允许重复、跟踪副本数，枚举**升序**。
//   2. 计数口径与 MultiList<T> 逐条一致：Count 是副本数，DistinctCount 才是不同元素数。
//   3. 顺序与同一性由 IComparer<T> 决定：比较为 0 即同一元素，存下的是**先加入**的那个。
//   4. IList 写入受限：Insert 只接受相等元素段内的位置；索引器赋值抛 NotSupportedException。
//   5. 结构相等未重写：内容相同仍是不同对象（仅 MultiList<T> 例外）。
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 有序计数与升序枚举
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 有序计数与升序枚举 ===");

// 集合初始化器按 Add(T) 逐个加入；枚举时按比较器升序、副本展开，与插入序无关。
var shelf = new OrderedMultiList<string> { "mug", "bean", "bean" };
Console.WriteLine($"枚举（升序副本展开）= {string.Join(",", shelf)}"); // bean,bean,mug
Console.WriteLine($"GetFirst()     = {shelf.GetFirst()}");             // bean：最小元素，O(log n)
Console.WriteLine($"GetLast()      = {shelf.GetLast()}");              // mug：最大元素，O(log n)
Console.WriteLine($"CountOf(bean)  = {shelf.CountOf("bean")}");        // 2：该元素的副本数
Console.WriteLine($"TotalCount     = {shelf.TotalCount}");             // 3：全部副本数
Console.WriteLine($"DistinctCount  = {shelf.DistinctCount}");          // 2：不同元素数
// Count 在 OrderedMultiList<T> 上是**显式接口实现**，语义 = 副本数（不是不同元素数）。
Console.WriteLine($"IReadOnlyCollection.Count = {((IReadOnlyCollection<string>)shelf).Count}"); // 3

// ---------------------------------------------------------------------------
// 2. 批量副本与删除
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 批量副本与删除 ===");

var box = new OrderedMultiList<string>();
box.Add("mug", 3);                       // Add(item, times) 一次加 3 份
int remaining = box.Remove("mug", 2);    // Remove 返回**剩余**副本数（不是删除数）
Console.WriteLine($"Remove(mug,2) 后剩余 = {remaining}");   // 1
Console.WriteLine($"Remove(mug)   后剩余 = {box.Remove("mug")}"); // 0：删掉最后一份
Console.WriteLine($"RemoveAllCopies 返回 = {box.RemoveAllCopies("mug")}"); // False：已不存在

// ⚠️ 陷阱：非正 times 抛 ArgumentOutOfRangeException（不再静默改写为「一份」）。
try
{
    box.Add("x", 0);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"Add(x,0) 被拒：{ex.ParamName}");    // times
}

// ---------------------------------------------------------------------------
// 3. 比较器定义顺序与同一性
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 比较器定义顺序与同一性 ===");

// 传入的必须是 IComparer<T>（提供全序），不是 IEqualityComparer<T>（只有哈希、无顺序）。
var ci = new OrderedMultiList<string>(StringComparer.OrdinalIgnoreCase);
ci.Add("Apple");
ci.Add("apple");   // 与 "Apple" 比较为 0 => 判定同一元素，副本数 +1，存下的仍是先加入的 "Apple"
Console.WriteLine($"DistinctCount   = {ci.DistinctCount}");   // 1
Console.WriteLine($"CountOf(APPLE)  = {ci.CountOf("APPLE")}"); // 2：忽略大小写比较
Console.WriteLine($"存下的元素       = {ci.GetFirst()}");       // Apple（先加入者胜出）

// ---------------------------------------------------------------------------
// 4. 结构相等未重写 / Insert 受限 / 索引器赋值被拒
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 结构相等与 IList 写入限制 ===");

var a = new OrderedMultiList<int> { 1, 2 };
var b = new OrderedMultiList<int> { 1, 2 };
// 结构相等未重写：内容相同仍是不同对象。要比较内容请用多重集语义的 IsSubsetOf/IsSupersetOf。
Console.WriteLine($"a.Equals(b)   = {a.Equals(b)}");      // False
Console.WriteLine($"a.IsSubsetOf(b)= {a.IsSubsetOf(b)}"); // True

var seq = new OrderedMultiList<string> { "bean", "bean", "mug" };
seq.Insert(0, "bean");   // 合法：位置 0 落在 "bean" 的相等段内（位置只是校验，不改变结果）
Console.WriteLine($"Insert(0,bean) 后 = {string.Join(",", seq)}"); // bean,bean,bean,mug
try
{
    seq.Insert(2, "cup");   // 非法：cup 排在 mug 之前，无法落在位置 2
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"Insert(2,cup) 被拒：{ex.ParamName}"); // index
}

// ❌ 错误写法：seq[0] = "x";  —— 索引器赋值会破坏有序性，运行时抛 NotSupportedException。
// 这里用注释表示，不写会编译失败的代码；要写入请用 Add / Insert / RemoveAt / Remove。
var asList = (IList<string>)seq;
try
{
    asList[0] = "x";
}
catch (NotSupportedException)
{
    Console.WriteLine("索引器赋值被拒：NotSupportedException");
}

Console.WriteLine();
Console.WriteLine("Sample.OrderedMultiSet completed.");
