// Sample.SetOpsZeroAlloc — 集合运算零分配（F12）
// Sample.SetOpsZeroAlloc — Zero-allocation set operations (F12)
// 运行：dotnet run --project sample/Sample.SetOpsZeroAlloc
// Run: dotnet run --project sample/Sample.SetOpsZeroAlloc
//
// 演示重点：
// Highlights:
//   1. 参数是**同比较器的 MultiList<T>** 时，集合运算就地读参数计数表，不物化第二个 bag。
//   1. When the argument is a **same-comparer MultiList<T>**, set operations read the argument's count table in place and do not materialize a second bag.
//   2. 传数组 / LINQ 序列仍会 Snapshot 物化；比较器不同也会物化——语义不变，只是分配不同。
//   2. Passing an array / LINQ sequence still goes through Snapshot materialization; a different comparer also materializes — the semantics are unchanged, only the allocation differs.
//   3. 子集 / 超集判定同样就地读；UnionWith(self) 有显式恒等短路。
//   3. The subset / superset tests also read in place; UnionWith(self) has an explicit identity short-circuit.
// 说明：本示例演示**可观察语义**。零分配是参数侧的实现细节，无法在输出里稳定断言，
// Note: this sample demonstrates **observable semantics**. Zero-allocation is an implementation detail on the argument side and cannot be asserted stably from the output,
//       因此用注释标出「何时就地 / 何时物化」，而不是打印分配字节数。
//       so comments mark "when it reads in place / when it materializes", rather than printing allocation byte counts.
using System;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 同比较器 MultiList 参数：就地读
// 1. Same-comparer MultiList argument: read in place
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 同比较器 MultiList 参数（就地读）===");
Console.WriteLine("=== 1. Same-comparer MultiList argument (read in place) ===");

var a = new MultiList<string> { "x", "x", "y" };
var b = new MultiList<string> { "x", "z" };
// b 是同比较器的 MultiList => AsInPlaceArgument 命中，就地读 b 的计数表，不分配第二 bag。
// b is a same-comparer MultiList => AsInPlaceArgument hits, reading b's count table in place without allocating a second bag.
a.UnionWith(b);
Console.WriteLine($"UnionWith(MultiList) => x:{a.CountOf("x")} y:{a.CountOf("y")} z:{a.CountOf("z")}");  // x:2 y:1 z:1

// ---------------------------------------------------------------------------
// 2. 数组参数：结果相同，但会物化
// 2. Array argument: same result, but it materializes
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 数组参数（会物化，结果相同）===");
Console.WriteLine("=== 2. Array argument (materializes, same result) ===");

var c = new MultiList<string> { "x", "x", "y" };
// ❌ 错误写法：以为传任意序列都零分配——数组不是 MultiList，会走 Snapshot 物化参数。
// ❌ Wrong: assuming any sequence is zero-allocation — an array is not a MultiList, so it goes through Snapshot materialization of the argument.
c.UnionWith(new[] { "x", "z" });
// ✅ 正确写法：要零分配就传同比较器的 MultiList（见上一节）。
// ✅ Right: for zero-allocation, pass a same-comparer MultiList (see the previous section).
Console.WriteLine($"UnionWith(array)    => x:{c.CountOf("x")} y:{c.CountOf("y")} z:{c.CountOf("z")}");  // x:2 y:1 z:1
// 语义与上一节完全一致——只是参数侧多了一次物化与迭代器分配。
// The semantics are exactly the same as the previous section — only the argument side adds one materialization and iterator allocation.

// ---------------------------------------------------------------------------
// 3. 比较器不同：拒绝就地，按接收方比较器重物化
// 3. Different comparer: reject in-place, re-materialize by the receiver's comparer
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 比较器不同（按接收方比较器重物化）===");
Console.WriteLine("=== 3. Different comparer (re-materialize by the receiver's comparer) ===");

var caseInsensitive = new MultiList<string>(StringComparer.OrdinalIgnoreCase) { "X" };
var lower = new MultiList<string> { "x" };
// 两边比较器不同 => 拒绝就地视图，用接收方（忽略大小写）重解释参数，可观察结果不变。
// The comparers differ => reject the in-place view and re-interpret the argument with the receiver's (case-insensitive) comparer; the observable result is unchanged.
caseInsensitive.UnionWith(lower);
Console.WriteLine($"忽略大小写并集 / Case-insensitive union => DistinctCount:{caseInsensitive.DistinctCount} CountOf(X):{caseInsensitive.CountOf("X")}");  // 1, 1
// "x" 在忽略大小写语义下等于 "X"，因此并集仍是单个元素（1 份）。
// Under case-insensitive semantics "x" equals "X", so the union is still a single element (1 copy).

// ---------------------------------------------------------------------------
// 4. 子集 / 超集判定：直接走两表
// 4. Subset / superset tests: go straight to the two tables
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 子集 / 超集判定 ===");
Console.WriteLine("=== 4. Subset / superset tests ===");

var big = new MultiList<int> { 1, 1, 2 };
var small = new MultiList<int> { 1, 2 };
Console.WriteLine($"small.IsSubsetOf(big)         = {small.IsSubsetOf(big)}");          // True
Console.WriteLine($"big.IsSupersetOf(small)       = {big.IsSupersetOf(small)}");        // True
Console.WriteLine($"big.IsProperSupersetOf(small) = {big.IsProperSupersetOf(small)}");  // True

// ---------------------------------------------------------------------------
// 5. UnionWith(self) 恒等短路
// 5. UnionWith(self) identity short-circuit
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 5. UnionWith(self) 恒等短路 ===");
Console.WriteLine("=== 5. UnionWith(self) identity short-circuit ===");

var self = new MultiList<string> { "m", "m", "n" };
self.UnionWith(self);   // 显式短路：不会边遍历边加导致副本翻倍
                        // explicit short-circuit: it will not double copies by adding while iterating
Console.WriteLine($"UnionWith(self) => TotalCount:{self.TotalCount} m:{self.CountOf("m")} n:{self.CountOf("n")}");  // 3, 2, 1

Console.WriteLine();
Console.WriteLine("Sample.SetOpsZeroAlloc completed.");
