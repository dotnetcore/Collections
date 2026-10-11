// Sample.BagStatistics — bag 统计 Mode / Median / Entropy（6.9.0）
// Sample.BagStatistics — bag statistics Mode / Median / Entropy (6.9.0)
// 运行：dotnet run --project sample/Sample.BagStatistics
// Run: dotnet run --project sample/Sample.BagStatistics
//
// 演示重点：
// Highlights:
//   1. 三个扩展方法都定义在 IMultiSet<T> 上，通过 EntrySet()（不同元素 + 副本数）读取，
//   1. All three extension methods are defined on IMultiSet<T> and read via EntrySet() (distinct elements + copy counts),
//      不展开副本，因此对任何 bag 实现都成立。
//      without expanding copies, so they hold for any bag implementation.
//   2. Mode() 返回**所有**并列最高副本数的元素（多重集可能多峰）；空袋返回空列表。
//   2. Mode() returns **all** elements tied for the highest copy count (a multiset can be multimodal); an empty bag returns an empty list.
//   3. Median(comparer) 返回**上中位副本**：奇数总量取正中，偶数总量取两个中位中较大的一个；
//   3. Median(comparer) returns the **upper-median copy**: for an odd total, the middle one; for an even total, the larger of the two middle ones;
//      永远是袋里的元素，绝不插值；空袋抛 InvalidOperationException。
//      it is always an element of the bag and never interpolated; an empty bag throws InvalidOperationException.
//   4. Entropy() 返回副本分布的香农熵（比特）：单元素为 0，d 个元素均分为 log2(d)，空袋为 0。
//   4. Entropy() returns the Shannon entropy (in bits) of the copy distribution: 0 for a single element, log2(d) for d equally sized elements, and 0 for an empty bag.
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. Mode()：并列最高频的全部元素
// 1. Mode(): all elements tied for the highest frequency
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. Mode()：最高副本数的所有并列元素 ===");
Console.WriteLine("=== 1. Mode(): all elements tied for the highest copy count ===");

var bag = new MultiList<string>();
bag.Add("a", 3);
bag.Add("b", 3);   // a、b 并列最高（各 3 份）/ a and b are tied for the highest (3 copies each)
bag.Add("c", 1);
bag.Add("d", 2);

IReadOnlyList<string> modes = bag.Mode();
// 返回顺序 = 袋枚举不同元素的顺序（哈希顺序，无契约）；这里排序后打印以固定输出
// The return order = the order in which the bag enumerates distinct elements (hash order, no contract); here it is sorted before printing to fix the output
Console.WriteLine($"Mode = [{string.Join(", ", modes.OrderBy(x => x))}]");   // a, b

// 空袋没有任何元素 => 空列表（不是 null，也不抛）
// An empty bag has no elements => an empty list (not null, and it does not throw)
var empty = new MultiList<int>();
Console.WriteLine($"空袋 Mode().Count / Empty bag Mode().Count = {empty.Mode().Count}");              // 0

// ---------------------------------------------------------------------------
// 2. Median()：上中位副本
// 2. Median(): the upper-median copy
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. Median(comparer)：上中位副本 ===");
Console.WriteLine("=== 2. Median(comparer): the upper-median copy ===");

// 副本升序铺开为 1,1,2,3（总量 4，偶数）：两个中位是 1 和 2，取较大的 => 2
// Copies in ascending order are 1,1,2,3 (total 4, even): the two middle ones are 1 and 2, so take the larger => 2
var even = new MultiList<int>();
even.Add(1, 2);
even.Add(2);
even.Add(3);
Console.WriteLine($"even 的 Median / Median of even = {even.Median(Comparer<int>.Default)}");  // 2

// 副本铺开为 5,5,5（总量 3，奇数）：正中 => 5
// Copies expand to 5,5,5 (total 3, odd): the middle => 5
var odd = new MultiList<int>();
odd.Add(5, 3);
Console.WriteLine($"odd 的 Median / Median of odd = {odd.Median(Comparer<int>.Default)}");    // 5

// 空袋没有中位，抛 InvalidOperationException
// An empty bag has no median and throws InvalidOperationException
try
{
    empty.Median(Comparer<int>.Default);
}
catch (InvalidOperationException)
{
    Console.WriteLine("空袋 Median 抛 InvalidOperationException / Empty-bag Median throws InvalidOperationException");
}

// ---------------------------------------------------------------------------
// 3. Entropy()：副本分布的香农熵（比特）
// 3. Entropy(): the Shannon entropy (in bits) of the copy distribution
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. Entropy()：副本分布的香农熵 ===");
Console.WriteLine("=== 3. Entropy(): Shannon entropy of the copy distribution ===");

// 只有一种元素：无论多少份，熵都是 0（分布完全确定）
// Only one kind of element: no matter how many copies, the entropy is 0 (the distribution is fully determined)
var single = new MultiList<string>();
single.Add("only", 5);
Console.WriteLine($"单元素袋 Entropy / Single-element bag Entropy = {single.Entropy()}");                  // 0

// 两个元素各 2 份：均匀分布，熵 = log2(2) = 1
// Two elements with 2 copies each: a uniform distribution, entropy = log2(2) = 1
var uniform = new MultiList<string>();
uniform.Add("x", 2);
uniform.Add("y", 2);
Console.WriteLine($"两元素均分 Entropy / Two-element uniform Entropy = {uniform.Entropy()}");               // 1

// 3:1 倾斜分布：熵 = -(0.75·log2 0.75 + 0.25·log2 0.25) ≈ 0.8113
// A skewed 3:1 distribution: entropy = -(0.75·log2 0.75 + 0.25·log2 0.25) ≈ 0.8113
var skewed = new MultiList<string>();
skewed.Add("x", 3);
skewed.Add("y");
Console.WriteLine($"3:1 分布 Entropy / 3:1 distribution Entropy = {skewed.Entropy():F4}");               // 0.8113

// 空袋约定为 0
// An empty bag is defined as 0
Console.WriteLine($"空袋 Entropy / Empty bag Entropy = {empty.Entropy()}");                       // 0

// ❌ 错误写法：bag.Median()   // Median 需要 IComparer<T>；无参重载不存在
// ❌ Wrong approach: bag.Median()   // Median requires an IComparer<T>; there is no parameterless overload
Console.WriteLine();
Console.WriteLine("Sample.BagStatistics completed.");
