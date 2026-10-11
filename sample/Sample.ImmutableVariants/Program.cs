// Sample.ImmutableVariants — 不可变多重集合与映射（F13）
// 运行：dotnet run --project sample/Sample.ImmutableVariants
//
// 演示重点：
//   1. ImmutableMultiList<T> / ImmutableMultiDictionary<TKey,TValue> 是「真不可变」：
//      看似突变的 Add / Remove 都返回**新实例**，接收方（源）不变。
//   2. 批量突变走 ToBuilder()：ToBuilder 不拷贝内部状态；builder 无写时 ToImmutable()
//      返回**源实例本身**（可用 ReferenceEquals 断言结构共享），首次写才 copy-on-write。
//   3. 实例永不变，因此任意线程可无锁并发读——不是「加了锁」，而是「不需要锁」。
using System;
using System.Collections.Generic;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 不可变多重集合：突变返回新实例，源不变
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 不可变多重集合（突变返回新实例）===");

// 注意：本包没有 ImmutableMultiList<T>.Empty 静态成员，用公开无参构造器起步。
var seed = new ImmutableMultiList<string>();
var one = seed.Add("apple");           // 返回新实例，seed 仍为空
var two = one.Add("apple");            // 再返回新实例，one 仍只有一份

Console.WriteLine($"seed.CountOf(apple) = {seed.CountOf("apple")}");   // 0：源从未改变
Console.WriteLine($"one.CountOf(apple)  = {one.CountOf("apple")}");    // 1
Console.WriteLine($"two.CountOf(apple)  = {two.CountOf("apple")}");    // 2
Console.WriteLine($"two.TotalCount      = {two.TotalCount}");          // 2：副本数
Console.WriteLine($"two.DistinctCount   = {two.DistinctCount}");       // 1：不同元素数
// 三个变量指向三个不同实例（旧版本实例都还在，可回滚 / 可共享）。
Console.WriteLine($"seed 与 two 是同一实例？{ReferenceEquals(seed, two)}"); // False

// ❌ 错误写法：忽略返回值 —— 以为「加成功了」，其实源没变。
//    seed.Add("apple");            // 丢弃了新实例，seed 仍为空
//    Console.WriteLine(seed.CountOf("apple"));  // 仍然是 0

// ---------------------------------------------------------------------------
// 2. Builder 结构共享：ToBuilder 不拷贝，无写 ToImmutable 返回源实例
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. Builder 与结构共享 ===");

var frozen = new ImmutableMultiList<string>();
var seeded = frozen.Add("a");          // seeded 含 a 一份

var builder = seeded.ToBuilder();      // ToBuilder 不拷贝：builder 读同一份内部状态
// 尚未写：ToImmutable 返回**源实例本身**（冻结只是身份判断，这就是结构共享可观测之处）。
ImmutableMultiList<string> untouched = builder.ToImmutable();
Console.WriteLine($"无写时 ToImmutable 返回源实例？{ReferenceEquals(seeded, untouched)}"); // True

builder.Add("b", 2);                   // 首次写触发 copy-on-write
builder.Add("a");                      // a 再加一份
ImmutableMultiList<string> built = builder.ToImmutable();
Console.WriteLine($"built: a x {built.CountOf("a")}, b x {built.CountOf("b")}");  // a x 2, b x 2
Console.WriteLine($"built.TotalCount = {built.TotalCount}");                     // 4
// 源 seeded 完全不受 builder 写入影响：builder 首次写时已拷贝出自己的可变副本。
Console.WriteLine($"seeded: a x {seeded.CountOf("a")}, b x {seeded.CountOf("b")}"); // a x 1, b x 0

// ❌ 错误写法：链式大量 Add 不用 Builder —— 每步整表 COW，分配爆炸。
//    var acc = new ImmutableMultiList<int>();
//    for (var i = 0; i < 1000; i++) acc = acc.Add(i);   // 1000 次拷贝
//    // 正确：var b = acc.ToBuilder(); for (...) b.Add(i); acc = b.ToImmutable();

// ---------------------------------------------------------------------------
// 3. 不可变多重映射：每次 Add 返回新映射
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 不可变多重映射 ===");

var map0 = new ImmutableMultiDictionary<string, int>();
var map1 = map0.Add("orders", 1001);          // map0 仍为空
var map2 = map1.Add("orders", 1002);          // 同一键加第二个值
var map3 = map2.Add("invoices", 9001);

Console.WriteLine($"map0.KeyCount = {map0.KeyCount}");                 // 0
Console.WriteLine($"map3.KeyCount = {map3.KeyCount}");                 // 2：键数
Console.WriteLine($"map3.TotalValueCount = {map3.TotalValueCount}");   // 3：值数
Console.WriteLine($"map3[orders] = [{string.Join(", ", map3["orders"])}]"); // [1001, 1002]
// 注意：ImmutableMultiDictionary 的 Count 等于 KeyCount（键数），不是值数。
Console.WriteLine($"map3.Count = {map3.Count}");                       // 2
// 旧版本实例仍可读：map1 只有一条 orders。
Console.WriteLine($"map1[orders] = [{string.Join(", ", map1["orders"])}]"); // [1001]

// 不可变映射也能导出只读快照（每次调用返回新对象）。
IReadOnlyDictionary<string, IReadOnlyCollection<int>> snapshot = map3.ToDictionary();
Console.WriteLine($"ToDictionary 键数 = {snapshot.Count}");            // 2

// 实例不可变 => 可安全交给别的线程无锁并发读（这里只演示引用可共享，不引入竞态）。
Console.WriteLine($"map3 与 map2 是同一实例？{ReferenceEquals(map3, map2)}"); // False

Console.WriteLine();
Console.WriteLine("Sample.ImmutableVariants completed.");
