// Sample.ValueEnumerable — 7.1.0 LINQ 值类型引擎：四入口 + 五算子 + 命名空间治理
// Sample.ValueEnumerable — the 7.1.0 LINQ value-type engine: four entry points + five operators + namespace governance
// 运行：dotnet run --project sample/Sample.ValueEnumerable            （net8.0）
// Run: dotnet run --project sample/Sample.ValueEnumerable            (net8.0)
//       dotnet run --project sample/Sample.ValueEnumerable -f net10.0 （net10.0）
//       dotnet run --project sample/Sample.ValueEnumerable -f net10.0 (net10.0)
//
// 演示重点：
// Highlights:
//   1. ToValueEnumerable() 四个入口，每个源形状一个重载（T[] / List<T> / IReadOnlyList<T> / IEnumerable<T>），
//   1. The four ToValueEnumerable() entry points, one overload per source shape (T[] / List<T> / IReadOnlyList<T> / IEnumerable<T>),
//      最具体的重载胜出；包装器是值类型结构体、惰性、不复制源；
//      where the most specific overload wins; the wrapper is a value-type struct, lazy, and does not copy the source;
//   2. 五个算子 Index / ForEach / TagFirstLast / Pairwise / Scan（含带种子重载）；
//   2. the five operators Index / ForEach / TagFirstLast / Pairwise / Scan (including the seeded overload);
//   3. 与 System.Linq 共存：同文件 using 两个命名空间，engine 的 Index 绑到包装器，
//   3. coexistence with System.Linq: using both namespaces in the same file, the engine's Index binds to the wrapper,
//      裸数组仍绑到 BCL（net9+），Enumerable 代码不受影响（7.1.0）。
//      while a bare array still binds to the BCL (net9+), leaving Enumerable code unaffected (since 7.1.0).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetCore.Collections;   // 引擎命名空间：与 System.Linq 同处一个编译单元
                                // the engine namespace: shares a single compilation unit with System.Linq

Console.OutputEncoding = Encoding.UTF8;

var source = new[] { 1, 2, 3, 4 };

// ---------------------------------------------------------------------------
// 1. 四个 ToValueEnumerable() 入口：最具体的重载胜出
// 1. The four ToValueEnumerable() entry points: the most specific overload wins
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 四个 ToValueEnumerable() 入口 ===");
Console.WriteLine("=== 1. The four ToValueEnumerable() entry points ===");

// 用**显式类型**写出包装器，证明命中的是哪个「源形状臂」（值类型结构体，非装箱）。
// Use **explicit types** for the wrappers to prove which "source-shape arm" was hit (a value-type struct, not boxed).
ArrayValueEnumerable<int> fromArray = source.ToValueEnumerable();                     // T[] 臂
                                                                                      // the T[] arm
ListValueEnumerable<int> fromList = new List<int> { 1, 2, 3 }.ToValueEnumerable();    // List<T> 臂
                                                                                      // the List<T> arm
IReadOnlyList<int> asReadOnly = new List<int> { 1, 2, 3 };                            // 静态类型 IReadOnlyList
                                                                                      // the static type is IReadOnlyList
ReadOnlyListValueEnumerable<int> fromReadOnly = asReadOnly.ToValueEnumerable();       // IReadOnlyList<T> 臂
                                                                                      // the IReadOnlyList<T> arm
IEnumerable<int> asEnumerable = Enumerable.Range(1, 3);
EnumerableValueEnumerable<int> fromEnumerable = asEnumerable.ToValueEnumerable();     // IEnumerable<T> 回退臂
                                                                                      // the IEnumerable<T> fallback arm

Console.WriteLine($"T[]            → {fromArray.GetType().Name}");
Console.WriteLine($"List<T>        → {fromList.GetType().Name}");
Console.WriteLine($"IReadOnlyList  → {fromReadOnly.GetType().Name}");
Console.WriteLine($"IEnumerable    → {fromEnumerable.GetType().Name}");

// 静态入口 ValueEnumerable.From<T>()：不想 import 扩展方法时的等价写法。
// The static entry point ValueEnumerable.From<T>(): an equivalent form when you do not want to import the extension method.
ArrayValueEnumerable<int> viaStatic = ValueEnumerable.From(source);
Console.WriteLine($"ValueEnumerable.From(T[]) → {viaStatic.GetType().Name}");

// 包装器也实现 IEnumerable<T>，可直接交给任何期望序列的 API（会走装箱枚举器）。
// The wrapper also implements IEnumerable<T>, so it can be handed directly to any API expecting a sequence (which will go through a boxed enumerator).
IEnumerable<int> asSeq = fromArray;
Console.WriteLine($"包装器可直接当 IEnumerable<int>：Sum={asSeq.Sum()} / The wrapper can be used directly as IEnumerable<int>: Sum={asSeq.Sum()}");   // 10

// ---------------------------------------------------------------------------
// 2. 算子一：Index —— 元素与零基位置配对为 (int Index, T Item)
// 2. Operator 1: Index — pairs an element with its zero-based position as (int Index, T Item)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. Index：位置配对 ===");
Console.WriteLine("=== 2. Index: positional pairing ===");

// ⚠️ 关键：在包装器上调用 Index() 绑到**引擎**（接收者是精确静态类型 = identity 转换）。
// ⚠️ Key: calling Index() on the wrapper binds to the **engine** (the receiver is the exact static type = identity conversion).
var indexed = source.ToValueEnumerable().Index();
var indexText = new StringBuilder();
foreach (var pair in indexed)
{
    // 命名元组元素：pair.Index / pair.Item。
    // Named tuple elements: pair.Index / pair.Item.
    indexText.Append(pair.Index).Append(':').Append(pair.Item).Append(' ');
}
Console.WriteLine($"engine Index → {indexText.ToString().Trim()}");   // 0:1 1:2 2:3 3:4

// ---------------------------------------------------------------------------
// 3. 算子二：ForEach —— 立即执行，另有带位置重载
// 3. Operator 2: ForEach — executes immediately, with an additional positional overload
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. ForEach：立即执行 ===");
Console.WriteLine("=== 3. ForEach: immediate execution ===");

var sum = 0;
source.ToValueEnumerable().ForEach(v => sum += v);            // 无位置重载
                                                              // the no-position overload
var weighted = 0;
source.ToValueEnumerable().ForEach((v, i) => weighted += v * i); // 带位置重载
                                                                 // the positional overload
Console.WriteLine($"ForEach(sum)={sum}  ForEach(indexed weighted)={weighted}");   // 10, 20

// 注意：ForEach 只声明在**包装器**上；裸 List<T> 的 ForEach 仍是 BCL 的 List<T>.ForEach。
// Note: ForEach is declared only on the **wrapper**; a bare List<T>'s ForEach is still the BCL's List<T>.ForEach.

// ---------------------------------------------------------------------------
// 4. 算子三：TagFirstLast —— 首 / 末标记
// 4. Operator 3: TagFirstLast — first / last tagging
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. TagFirstLast：首末标记 ===");
Console.WriteLine("=== 4. TagFirstLast: first/last tagging ===");

var tagged = new List<string>();
foreach (var text in source.ToValueEnumerable().TagFirstLast(
             (value, isFirst, isLast) => (isFirst ? "<" : "") + value + (isLast ? ">" : "")))
{
    tagged.Add(text);
}
Console.WriteLine($"TagFirstLast → {string.Join(",", tagged)}");   // <1,2,3,4>

// 单元素源：同一个元素既是首又是末。
// A single-element source: the same element is both the first and the last.
var single = new[] { 7 }.ToValueEnumerable().TagFirstLast((v, f, l) => $"{v}(first={f},last={l})");
Console.WriteLine($"单元素 / Single element → {string.Join(",", single)}");          // 7(first=True,last=True)

// ---------------------------------------------------------------------------
// 5. 算子四：Pairwise —— 前一个与当前，n 个元素产出 n-1 个结果
// 5. Operator 4: Pairwise — previous and current; n elements produce n-1 results
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 5. Pairwise：相邻配对 ===");
Console.WriteLine("=== 5. Pairwise: adjacent pairing ===");

var pairs = new List<string>();
foreach (var text in source.ToValueEnumerable().Pairwise((left, right) => left + "-" + right))
{
    pairs.Add(text);
}
Console.WriteLine($"Pairwise → {string.Join(",", pairs)}");          // 1-2,2-3,3-4

// 单元素源产出**空**序列（n-1 = 0）。
// A single-element source produces an **empty** sequence (n-1 = 0).
Console.WriteLine($"单元素 Pairwise 结果数 / Single-element Pairwise result count = {new[] { 9 }.ToValueEnumerable().Pairwise((a, b) => a + b).Count()}"); // 0

// ---------------------------------------------------------------------------
// 6. 算子五：Scan —— 无种子（与源同长）/ 带种子（比源长 1）
// 6. Operator 5: Scan — seedless (same length as the source) / seeded (one longer than the source)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 6. Scan：运行聚合 ===");
Console.WriteLine("=== 6. Scan: running aggregation ===");

// 无种子：先发第一个元素，再每个元素发一次累积；长度 == 源长度。
// Seedless: emit the first element, then emit an accumulation for each element; length == source length.
var seedless = new List<int>();
foreach (var v in source.ToValueEnumerable().Scan((agg, value) => agg + value))
{
    seedless.Add(v);
}
Console.WriteLine($"Scan(seedless) → {string.Join(",", seedless)}");  // 1,3,6,10

// 带种子：先发种子，再每个元素发一次；长度 == 源长度 + 1。聚合类型可与元素类型不同。
// Seeded: emit the seed, then emit once per element; length == source length + 1. The aggregate type may differ from the element type.
var seeded = new List<string>();
foreach (var v in source.ToValueEnumerable().Scan("Σ=", (agg, value) => agg + value + " "))
{
    seeded.Add(v);
}
Console.WriteLine($"Scan(seeded)   → {string.Join("|", seeded)}");    // Σ=|Σ=1 |Σ=1 2 |...

// ---------------------------------------------------------------------------
// 7. 命名空间治理：与 System.Linq 共存，互不遮蔽
// 7. Namespace governance: coexists with System.Linq, neither shadows the other
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 7. 命名空间治理（System.Linq + 引擎）===");
Console.WriteLine("=== 7. Namespace governance (System.Linq + the engine) ===");

#if NET9_0_OR_GREATER
// 裸 T[]：引擎没有从 T[] 到包装器的隐式转换，故引擎的 Index 不是候选——
// A bare T[]: the engine has no implicit conversion from T[] to the wrapper, so the engine's Index is not a candidate —
// 调用只能到达 System.Linq（net9+ 的 Enumerable.Index）。这保证「加 using 不改既有代码绑定」。
// the call can only reach System.Linq (net9+'s Enumerable.Index). This guarantees "adding a using does not change existing code's binding".
IEnumerable<(int Index, int Item)> viaBcl = source.Index();
var bclText = new StringBuilder();
foreach (var pair in viaBcl)
{
    bclText.Append(pair.Index).Append(':').Append(pair.Item).Append(' ');
}
Console.WriteLine($"BCL source.Index() → {bclText.ToString().Trim()}");   // 0:1 1:2 2:3 3:4
#else
Console.WriteLine("BCL 无 Enumerable.Index（net8.0 之前）——裸数组的 Index() 不存在，n/a / The BCL has no Enumerable.Index (before net8.0) — a bare array's Index() does not exist, n/a");
#endif

// 两个面在同一表达式里交错：System.Linq → 引擎 → System.Linq，互不遮蔽。
// The two surfaces interleave within a single expression: System.Linq → the engine → System.Linq, neither shadowing the other.
var composed = Enumerable.Range(1, 6)                    // System.Linq
    .Where(v => v % 2 == 0)                              // System.Linq → 2,4,6
    .ToArray()
    .ToValueEnumerable()                                 // 引擎入口
                                                         // the engine entry point
    .Scan((agg, v) => agg + v)                           // 引擎 → 2,6,12
                                                         // the engine → 2,6,12
    .ToArray();                                          // System.Linq
Console.WriteLine($"两表面组合 / Combined surfaces → {string.Join(",", composed)}");   // 2,6,12

// ❌ 错误写法：以为加 using DotNetCore.Collections 会改变既有 Enumerable 代码的绑定——
// ❌ Wrong approach: thinking that adding using DotNetCore.Collections changes the binding of existing Enumerable code —
//    不会；引擎的公开面以 DotNetCore.Collections 为根，不往 System.Linq 注入任何东西。
//    it does not; the engine's public surface is rooted at DotNetCore.Collections and injects nothing into System.Linq.
// ❌ 错误写法：裸数组直接 .Index() 期望走引擎——引擎不接收 T[]（先 ToValueEnumerable()）。
// ❌ Wrong approach: expecting a bare array's .Index() to go through the engine — the engine does not accept T[] (call ToValueEnumerable() first).

Console.WriteLine();
Console.WriteLine("Sample.ValueEnumerable completed.");
