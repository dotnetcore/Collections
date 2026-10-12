// Sample.SpanBag — 栈上临时袋（6.4.0）
// Sample.SpanBag — stack-based temporary bag (6.4.0)
// 运行：dotnet run --project sample/Sample.SpanBag
// Run: dotnet run --project sample/Sample.SpanBag
//
// 演示重点：
// Highlights:
//   1. SpanBag<T> where T : unmanaged 是 ref struct：存储由调用方 stackalloc 的 Span<T> / Span<int>
//   1. SpanBag<T> where T : unmanaged is a ref struct: its storage is borrowed from a Span<T> / Span<int> stackalloc'ed by the caller
//      借用，用于单方法内短命频次统计——零堆分配，无需 Dispose。
//      for short-lived frequency counting within a single method — zero heap allocation, no Dispose needed.
//   2. 定容：容量 = min(values.Length, counts.Length)，构造时定死；满时**新**元素 Add 返回 false
//   2. Fixed capacity: capacity = min(values.Length, counts.Length), fixed at construction; when full, a **new** element's Add returns false
//      （尺寸条件，非异常），已有元素只加计数、恒成功——务必检查返回值。
//      (a size condition, not an exception); an existing element only increments its count and always succeeds — always check the return value.
//   3. ref struct 的生命周期由编译器强制：不可装箱 / 不可作字段 / 不可被 lambda 捕获 / 不可跨 await。
//   3. A ref struct's lifetime is enforced by the compiler: cannot be boxed / a field / captured by a lambda / cross an await.
using System;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 方法内零堆计数：找第一个重复值
// 1. In-method zero-heap counting: find the first duplicate value
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 找第一个重复值（零堆）===");
Console.WriteLine("=== 1. Find the first duplicate value (zero heap) ===");

// 局部函数：两段 stackalloc 存储借给 SpanBag；袋只原地读写，不向堆要内存。
// Local function: two stackalloc segments are lent to the SpanBag; the bag only reads and writes in place, never requesting heap memory.
static int FirstDuplicate(ReadOnlySpan<int> source)
{
    Span<int> values = stackalloc int[32];
    Span<int> counts = stackalloc int[32];
    var seen = new SpanBag<int>(values, counts);

    foreach (var item in source)
    {
        if (seen.CountOf(item) > 0)
        {
            return item;   // 已经见过 => 它就是第一个重复值
                           // already seen => it is the first duplicate value
        }

        if (!seen.Add(item))
        {
            // 容量不足只对新元素发生；这是尺寸条件，不是异常。
            // Insufficient capacity only happens for new elements; this is a size condition, not an exception.
            throw new InvalidOperationException("Not enough stack capacity.");
        }
    }

    return -1;   // 无重复
                 // no duplicates
}

Console.WriteLine($"FirstDuplicate(10,20,30,20) = {FirstDuplicate(new[] { 10, 20, 30, 20 })}"); // 20
Console.WriteLine($"FirstDuplicate(1,2,3)       = {FirstDuplicate(new[] { 1, 2, 3 })}");       // -1

// ---------------------------------------------------------------------------
// 2. 定容与满容行为
// 2. Fixed capacity and full-capacity behavior
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 定容与满容 ===");
Console.WriteLine("=== 2. Fixed capacity and full capacity ===");

Span<int> smallValues = stackalloc int[2];
Span<int> smallCounts = stackalloc int[2];
var small = new SpanBag<int>(smallValues, smallCounts);

Console.WriteLine($"Capacity   = {small.Capacity}");       // 2：min(2, 2) 构造时定死
                                                           // 2: min(2, 2) fixed at construction
Console.WriteLine($"Remaining  = {small.Remaining}");      // 2：还能容纳 2 个不同元素
                                                           // 2: can still hold 2 distinct elements

Console.WriteLine($"Add(10) = {small.Add(10)}");           // True：新元素
                                                           // True: a new element
Console.WriteLine($"Add(20) = {small.Add(20)}");           // True：新元素，袋已满
                                                           // True: a new element; the bag is now full
Console.WriteLine($"Add(30) = {small.Add(30)}");           // False：满容 + 新元素 => 返回 false（非异常）
                                                           // False: full capacity + a new element => returns false (not an exception)
Console.WriteLine($"Add(10) = {small.Add(10)}");           // True：已有元素只加计数，恒成功
                                                           // True: an existing element only increments its count and always succeeds
Console.WriteLine($"CountOf(10) = {small.CountOf(10)}");   // 2：10 已被加两次
                                                           // 2: 10 has been added twice
Console.WriteLine($"DistinctCount = {small.DistinctCount}"); // 2
Console.WriteLine($"TotalCount = {small.TotalCount}");     // 3：10 两份 + 20 一份
                                                           // 3: two copies of 10 + one copy of 20
Console.WriteLine($"Remaining = {small.Remaining}");       // 0

// ⚠️ 陷阱：满容后忘记检查 Add 返回值 —— 新元素会被静默丢弃。
// ⚠️ Pitfall: forgetting to check Add's return value after the bag is full — the new element is silently dropped.
// ❌ 错误写法：small.Add(30);   // 丢弃返回值，30 根本没进去
// ❌ Wrong usage: small.Add(30);   // the return value is discarded; 30 never gets in

// ---------------------------------------------------------------------------
// 3. 枚举直方图（枚举器也是 ref struct）
// 3. Enumerated histogram (the enumerator is also a ref struct)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 枚举直方图 ===");
Console.WriteLine("=== 3. Enumerated histogram ===");

// GetEnumerator 返回 ref struct 枚举器，它同样不能逃出当前栈帧。
// GetEnumerator returns a ref struct enumerator that likewise cannot escape the current stack frame.
foreach (var entry in small)
{
    Console.WriteLine($"  {entry.Value} x {entry.Count}");
}
Console.WriteLine($"Contains(20) = {small.Contains(20)}");   // True

// ---------------------------------------------------------------------------
// 4. 删除与 Clear 复用
// 4. Removal and Clear reuse
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 删除与 Clear ===");
Console.WriteLine("=== 4. Removal and Clear ===");

// Remove(item) 删一份，返回剩余副本数；末份删除即弃项。
// Remove(item) removes one copy and returns the remaining copy count; deleting the last copy drops the entry.
Console.WriteLine($"Remove(10) 后剩余 / remaining after Remove(10) = {small.Remove(10)}");      // 1
Console.WriteLine($"RemoveAllCopies(20) = {small.RemoveAllCopies(20)}"); // True
Console.WriteLine($"DistinctCount = {small.DistinctCount}");       // 1

// Clear 只重置计数与总数（unmanaged 无需擦除），可复用同一段栈存储。
// Clear only resets the counts and totals (unmanaged values need no wiping), so the same stack storage can be reused.
small.Clear();
Console.WriteLine($"Clear 后 IsEmpty / IsEmpty after Clear = {small.IsEmpty}");           // True
Console.WriteLine($"Clear 后 Remaining / Remaining after Clear = {small.Remaining}");      // 2：容量回到初始
                                                                                           // 2: capacity back to the initial value
Console.WriteLine($"Clear 后 Add(10) / Add(10) after Clear = {small.Add(10)}");          // True：可继续用
                                                                                          // True: usable again

// ---------------------------------------------------------------------------
// 5. 生命周期边界（编译期强制）
// 5. Lifetime boundary (enforced at compile time)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 5. 生命周期边界 ===");
Console.WriteLine("=== 5. Lifetime boundary ===");

// ❌ 错误写法：ref struct 不能跨 await / 作字段 / 被 lambda 捕获——以下都编译不过。
// ❌ Wrong usage: a ref struct cannot cross an await / be a field / be captured by a lambda — none of the following compiles.
//    async Task Bad()
//    {
//        Span<int> v = stackalloc int[8];
//        Span<int> c = stackalloc int[8];
//        var bag = new SpanBag<int>(v, c);
//        await Task.Yield();
//        bag.Add(1);                    // 编译错误：ref struct 跨 await
//                                       // compile error: ref struct crossing an await
//    }
//    ✅ 正确写法：方法内同步计数完就返回；需要跨 await / 跨方法请用 PackedBag<T>（6.4.0）或 MultiList<T>。
//    ✅ Correct: finish counting synchronously inside the method and return; if you need to cross an await / a method, use PackedBag<T> (6.4.0) or MultiList<T>.
// 另注：两段 span 必须等长，否则构造抛 ArgumentException；仅 netstandard2.1 / net6.0+ 暴露本类型。
// Also note: the two spans must be the same length, otherwise the constructor throws ArgumentException; only netstandard2.1 / net6.0+ exposes this type.
Console.WriteLine("SpanBag<T> 是 ref struct：不可装箱 / 字段 / 捕获 / 跨 await，容量构造时定死。 / SpanBag<T> is a ref struct: cannot be boxed / a field / captured / cross an await, and its capacity is fixed at construction.");

Console.WriteLine();
Console.WriteLine("Sample.SpanBag completed.");
