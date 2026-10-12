// Sample.StructuralEquality — 多重集合结构相等（6.1.0）
// Sample.StructuralEquality — Multiset structural equality (6.1.0)
// 运行：dotnet run --project sample/Sample.StructuralEquality
// Run: dotnet run --project sample/Sample.StructuralEquality
//
// 演示重点：
// Highlights:
//   1. 结构相等是 **bag equality**：不同元素相同**且每个元素副本数相同**，与插入顺序无关。
//   1. Structural equality is **bag equality**: same distinct elements **and** same copy count per element, independent of insertion order.
//   2. GetHashCode 与 Equals 一致，结构相等的 bag 会塌缩为 HashSet 里的一个条目。
//   2. GetHashCode is consistent with Equals, so structurally equal bags collapse into a single entry in a HashSet.
//   3. == / != 未重载（仍是引用比较）；IsSubsetOf 不是相等性（它忽略另一侧多余副本）。
//   3. == / != are not overloaded (still reference comparison); IsSubsetOf is not equality (it ignores extra copies on the other side).
using System;
using System.Collections.Generic;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 副本数参与比较
// 1. Copy counts participate in comparison
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 副本数参与比较 ===");
Console.WriteLine("=== 1. Copy counts participate in comparison ===");

var a = new MultiList<string> { "x", "x", "y" };
var b = new MultiList<string> { "y", "x", "x" };
var c = new MultiList<string> { "x", "y" };

// a 与 b 只是插入顺序不同，结构相等；a 与 c 差在 "x" 的副本数（2 vs 1），不等。
// a and b differ only in insertion order, so they are structurally equal; a and c differ in the copy count of "x" (2 vs 1), so they are not equal.
Console.WriteLine($"a.Equals(b) = {a.Equals(b)}");   // True
Console.WriteLine($"a.Equals(c) = {a.Equals(c)}");   // False：x 的副本数不同
                                                     // False: the copy count of x differs
// 结构相等 = 「相同的不同元素集合 + 每个元素副本数一致」；这是 bag equality，不是 set equality。
// Structural equality = "same set of distinct elements + same copy count per element"; this is bag equality, not set equality.

// ---------------------------------------------------------------------------
// 2. 进入 HashSet：相等 bag 塌缩为一条
// 2. Entering a HashSet: equal bags collapse into one entry
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 进入 HashSet ===");
Console.WriteLine("=== 2. Entering a HashSet ===");

var set = new HashSet<MultiList<string>>
{
    new MultiList<string> { "a", "a", "b" },
    new MultiList<string> { "b", "a", "a" },   // 与上一条结构相等
                                               // structurally equal to the previous entry
    new MultiList<string> { "a", "b" },        // 副本数不同，是另一条
                                               // different copy counts, so a separate entry
};
Console.WriteLine($"HashSet.Count = {set.Count}");   // 2
// 哈希与枚举顺序无关（元素贡献用 XOR 累积），且副本数混入元素贡献，{a,a,b} 与 {a,b} 不会碰撞。
// The hash is independent of enumeration order (element contributions are accumulated with XOR), and copy counts are mixed into the element contributions, so {a,a,b} and {a,b} do not collide.

// ---------------------------------------------------------------------------
// 3. == 仍是引用比较；子集判定不是相等性
// 3. == is still reference comparison; the subset test is not equality
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. == 未重载 / 子集判定不是相等性 ===");
Console.WriteLine("=== 3. == is not overloaded / the subset test is not equality ===");

var p = new MultiList<int> { 7, 7 };
var q = new MultiList<int> { 7, 7 };

Console.WriteLine($"p.Equals(q) = {p.Equals(q)}");   // True：内容结构相等
                                                     // True: contents are structurally equal
Console.WriteLine($"p == q      = {p == q}");        // False：== 未重载，仍是引用比较
                                                     // False: == is not overloaded, still reference comparison
// ❌ 错误写法：用 if (p == q) 判断内容相等——它比较的是两个实例的引用。
// ❌ Wrong: using if (p == q) to test content equality — it compares the references of the two instances.
// ✅ 正确写法：用 p.Equals(q)（或测试里的 Assert.Equal）走结构相等。
// ✅ Right: use p.Equals(q) (or Assert.Equal in tests) to go through structural equality.

var one = new MultiList<int> { 7 };
Console.WriteLine($"one.IsSubsetOf(p) = {one.IsSubsetOf(p)}");   // True：{7} ⊆ {7,7}
                                                                 // True: {7} ⊆ {7,7}
Console.WriteLine($"one.Equals(p)     = {one.Equals(p)}");       // False：副本数不同
                                                                 // False: copy counts differ
// ⚠️ IsSubsetOf 忽略另一侧多余的副本，返回 True 不代表相等；相等请用 Equals（内部双向确认）。
// ⚠️ IsSubsetOf ignores extra copies on the other side, so a True result does not mean equality; for equality use Equals (which confirms both directions internally).

Console.WriteLine();
Console.WriteLine("Sample.StructuralEquality completed.");
