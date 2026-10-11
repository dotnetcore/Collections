// Sample.MultiTfmSurface — 多 TFM 暴露面与逐包子集差异（F57）
// 运行：dotnet run --project sample/Sample.MultiTfmSurface            （默认 net8.0）
//       dotnet run --project sample/Sample.MultiTfmSurface -f net10.0
//
// 演示重点：
//   1. 同一份源码在 net8.0 / net10.0 两个分支都能编译、都能运行（本工程多目标）；
//   2. 核心与部分适配器目标 11 个 TFM，但**各包 TFM 子集不同**——写能力时必须标 TFM；
//   3. 低代际（尤其 net451）缺 IReadOnly* 暴露面、可空靠垫片、生成器不引用；
//   4. Multi 类型如 SpanBag 只在「Span<T> 在框」的目标存在——不要假设全包同矩阵。
using System;
using System.Runtime.InteropServices;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 当前运行时与编译分支
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 当前运行时与编译分支 ===");

// FrameworkDescription 形如 ".NET 8.0.30" / ".NET 10.0.11"——取决于运行的是哪个 TFM。
Console.WriteLine($"FrameworkDescription = {RuntimeInformation.FrameworkDescription}");
Console.WriteLine($"Environment.Version   = {Environment.Version}");

// 条件编译分支：证明同一文件确实按 TFM 分别编译过。
#if NET10_0_OR_GREATER
Console.WriteLine("编译分支：#if NET10_0_OR_GREATER 命中（net10.0 分支）");
#elif NET8_0_OR_GREATER
Console.WriteLine("编译分支：#if NET8_0_OR_GREATER 命中（net8.0 分支）");
#else
Console.WriteLine("编译分支：更早的目标框架");
#endif

// ---------------------------------------------------------------------------
// 2. 真实跑一段 Multi API（两个 TFM 行为一致）
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 两个 TFM 上行为一致的 Multi API ===");

// MultiList<T> 在两个分支都可编译、可运行；结果确定性，便于跨 TFM 对比输出。
var bag = new MultiList<string>();
bag.Add("a", 2);          // a 两份
bag.Add("b");             // b 一份
Console.WriteLine($"TotalCount = {bag.TotalCount}");       // 3
Console.WriteLine($"DistinctCount = {bag.DistinctCount}"); // 2
Console.WriteLine($"CountOf(a) = {bag.CountOf("a")}");     // 2

// ---------------------------------------------------------------------------
// 3. 11 TFM 矩阵（核心与部分适配器）
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 11 TFM 矩阵 ===");

var tfms = new[]
{
    "net451", "net461", "net47", "net48",
    "netstandard2.0", "netstandard2.1",
    "net6.0", "net7.0", "net8.0", "net9.0", "net10.0",
};
Console.WriteLine("核心 / Multi / 部分适配器目标：" + string.Join("; ", tfms));
Console.WriteLine("各包 TFM **子集不同**——API 手册「目标框架」列必填（F57）。");

// ---------------------------------------------------------------------------
// 4. 逐包 TFM 子集差异（逐字来自 F57）
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 逐包 TFM 子集差异 ===");

var diffs = new (string Package, string Diff)[]
{
    ("EF Core 包",              "仅 net6+（无 net451）"),
    ("Chloe / Dos / NHibernate","无 net451"),
    ("SqlSugar",                "无 netstandard2.0"),
    ("FreeSql / SqlKata / SqlSugar", "net451 保留 #if NET451 手写（不引用生成器）"),
    ("低代际（尤其 net451）",    "缺 IReadOnly* 暴露面；可空靠 internal 垫片（F43）"),
    ("Multi.NetFxTests",        "net462 侧车工程，验证低代际暴露面"),
};

Console.WriteLine($"{"包 / 目标",-32}差异");
Console.WriteLine(new string('-', 96));
foreach (var d in diffs)
{
    Console.WriteLine($"{d.Package,-32}{d.Diff}");
}

// ---------------------------------------------------------------------------
// 5. SpanBag 等类型只存在于「Span<T> 在框」的目标
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 5. 类型级 TFM 差异 ===");
Console.WriteLine("SpanBag 等 Multi 类型仅 `Span<T>` 在框的目标——对照 Multi 文档，勿假设全包同矩阵。");
Console.WriteLine("在 netstandard2.0 测试工程里直接用 net6+ only 的 API / 适配器会编不过。");

// ❌ 错误写法：假设全包 11 TFM。
//    // 在 net48 上引用 EF Core 适配器 → 该包无 net451/net48 资产，还原/编译失败。
// ✅ 正确写法：选包前核对矩阵，写能力时标 TFM。

Console.WriteLine();
Console.WriteLine("Sample.MultiTfmSurface completed.");
