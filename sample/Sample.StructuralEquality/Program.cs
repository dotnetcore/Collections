// Sample.StructuralEquality — 多重集合结构相等（F03）
// 运行：dotnet run --project sample/Sample.StructuralEquality
//
// 演示重点：
//   1. 结构相等是 **bag equality**：不同元素相同**且每个元素副本数相同**，与插入顺序无关。
//   2. GetHashCode 与 Equals 一致，结构相等的 bag 会塌缩为 HashSet 里的一个条目。
//   3. == / != 未重载（仍是引用比较）；IsSubsetOf 不是相等性（它忽略另一侧多余副本）。
using System;
using System.Collections.Generic;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 副本数参与比较
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 副本数参与比较 ===");

var a = new MultiList<string> { "x", "x", "y" };
var b = new MultiList<string> { "y", "x", "x" };
var c = new MultiList<string> { "x", "y" };

// a 与 b 只是插入顺序不同，结构相等；a 与 c 差在 "x" 的副本数（2 vs 1），不等。
Console.WriteLine($"a.Equals(b) = {a.Equals(b)}");   // True
Console.WriteLine($"a.Equals(c) = {a.Equals(c)}");   // False：x 的副本数不同
// 结构相等 = 「相同的不同元素集合 + 每个元素副本数一致」；这是 bag equality，不是 set equality。

// ---------------------------------------------------------------------------
// 2. 进入 HashSet：相等 bag 塌缩为一条
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 进入 HashSet ===");

var set = new HashSet<MultiList<string>>
{
    new MultiList<string> { "a", "a", "b" },
    new MultiList<string> { "b", "a", "a" },   // 与上一条结构相等
    new MultiList<string> { "a", "b" },        // 副本数不同，是另一条
};
Console.WriteLine($"HashSet.Count = {set.Count}");   // 2
// 哈希与枚举顺序无关（元素贡献用 XOR 累积），且副本数混入元素贡献，{a,a,b} 与 {a,b} 不会碰撞。

// ---------------------------------------------------------------------------
// 3. == 仍是引用比较；子集判定不是相等性
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. == 未重载 / 子集判定不是相等性 ===");

var p = new MultiList<int> { 7, 7 };
var q = new MultiList<int> { 7, 7 };

Console.WriteLine($"p.Equals(q) = {p.Equals(q)}");   // True：内容结构相等
Console.WriteLine($"p == q      = {p == q}");        // False：== 未重载，仍是引用比较
// ❌ 错误写法：用 if (p == q) 判断内容相等——它比较的是两个实例的引用。
// ✅ 正确写法：用 p.Equals(q)（或测试里的 Assert.Equal）走结构相等。

var one = new MultiList<int> { 7 };
Console.WriteLine($"one.IsSubsetOf(p) = {one.IsSubsetOf(p)}");   // True：{7} ⊆ {7,7}
Console.WriteLine($"one.Equals(p)     = {one.Equals(p)}");       // False：副本数不同
// ⚠️ IsSubsetOf 忽略另一侧多余的副本，返回 True 不代表相等；相等请用 Equals（内部双向确认）。

Console.WriteLine();
Console.WriteLine("Sample.StructuralEquality completed.");
