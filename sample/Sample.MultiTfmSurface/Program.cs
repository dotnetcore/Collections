// Sample.MultiTfmSurface — 多 TFM 暴露面与逐包子集差异（6.0.0）
// Sample.MultiTfmSurface — multi-TFM surface and per-package subset differences (6.0.0)
// 运行：dotnet run --project sample/Sample.MultiTfmSurface            （默认 net8.0）
// Run: dotnet run --project sample/Sample.MultiTfmSurface            (default net8.0)
//       dotnet run --project sample/Sample.MultiTfmSurface -f net10.0
//       dotnet run --project sample/Sample.MultiTfmSurface -f net10.0
//
// 演示重点：
// Highlights:
//   1. 同一份源码在 net8.0 / net10.0 两个分支都能编译、都能运行（本工程多目标）；
//   1. The same source compiles and runs on both the net8.0 / net10.0 branches (this project is multi-target);
//   2. 核心与部分适配器目标 11 个 TFM，但**各包 TFM 子集不同**——写能力时必须标 TFM；
//   2. the core and some adapters target 11 TFMs, but **each package's TFM subset differs** — you must annotate the TFM when documenting a capability;
//   3. 低代际（尤其 net451）缺 IReadOnly* 暴露面、可空靠垫片、生成器不引用；
//   3. low generations (especially net451) lack the IReadOnly* surface, rely on shims for nullability, and do not reference the generator;
//   4. Multi 类型如 SpanBag 只在「Span<T> 在框」的目标存在——不要假设全包同矩阵。
//   4. Multi types such as SpanBag exist only on targets where "Span<T> is in the box" — do not assume all packages share the same matrix.
using System;
using System.Runtime.InteropServices;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 当前运行时与编译分支
// 1. Current runtime and compile branch
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 当前运行时与编译分支 ===");
Console.WriteLine("=== 1. Current runtime and compile branch ===");

// FrameworkDescription 形如 ".NET 8.0.30" / ".NET 10.0.11"——取决于运行的是哪个 TFM。
// FrameworkDescription looks like ".NET 8.0.30" / ".NET 10.0.11" — depending on which TFM is running.
Console.WriteLine($"FrameworkDescription = {RuntimeInformation.FrameworkDescription}");
Console.WriteLine($"Environment.Version   = {Environment.Version}");

// 条件编译分支：证明同一文件确实按 TFM 分别编译过。
// Conditional compilation branches: proving the same file really was compiled separately per TFM.
#if NET10_0_OR_GREATER
Console.WriteLine("编译分支：#if NET10_0_OR_GREATER 命中（net10.0 分支） / Compile branch: #if NET10_0_OR_GREATER hit (the net10.0 branch)");
#elif NET8_0_OR_GREATER
Console.WriteLine("编译分支：#if NET8_0_OR_GREATER 命中（net8.0 分支） / Compile branch: #if NET8_0_OR_GREATER hit (the net8.0 branch)");
#else
Console.WriteLine("编译分支：更早的目标框架 / Compile branch: an earlier target framework");
#endif

// ---------------------------------------------------------------------------
// 2. 真实跑一段 Multi API（两个 TFM 行为一致）
// 2. Really run a piece of Multi API (behavior is consistent across both TFMs)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 两个 TFM 上行为一致的 Multi API ===");
Console.WriteLine("=== 2. Multi API with consistent behavior across both TFMs ===");

// MultiList<T> 在两个分支都可编译、可运行；结果确定性，便于跨 TFM 对比输出。
// MultiList<T> compiles and runs on both branches; the result is deterministic, making cross-TFM output comparison easy.
var bag = new MultiList<string>();
bag.Add("a", 2);          // a 两份
                          // two copies of a
bag.Add("b");             // b 一份
                          // one copy of b
Console.WriteLine($"TotalCount = {bag.TotalCount}");       // 3
Console.WriteLine($"DistinctCount = {bag.DistinctCount}"); // 2
Console.WriteLine($"CountOf(a) = {bag.CountOf("a")}");     // 2

// ---------------------------------------------------------------------------
// 3. 11 TFM 矩阵（核心与部分适配器）
// 3. The 11-TFM matrix (the core and some adapters)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 11 TFM 矩阵 ===");
Console.WriteLine("=== 3. The 11-TFM matrix ===");

var tfms = new[]
{
    "net451", "net461", "net47", "net48",
    "netstandard2.0", "netstandard2.1",
    "net6.0", "net7.0", "net8.0", "net9.0", "net10.0",
};
Console.WriteLine("核心 / Multi / 部分适配器目标 / Core / Multi / some adapters target：" + string.Join("; ", tfms));
Console.WriteLine("各包 TFM **子集不同**——API 手册「目标框架」列必填（6.0.0）。 / Each package's TFM **subset differs** — the \"target frameworks\" column is mandatory in the API manual (6.0.0).");

// ---------------------------------------------------------------------------
// 4. 逐包 TFM 子集差异（逐字来自 6.0.0）
// 4. Per-package TFM subset differences (verbatim from 6.0.0)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 逐包 TFM 子集差异 ===");
Console.WriteLine("=== 4. Per-package TFM subset differences ===");

var diffs = new (string Package, string Diff)[]
{
    ("EF Core 包 / EF Core package",              "仅 net6+（无 net451）/ net6+ only (no net451)"),
    ("Chloe / Dos / NHibernate",                  "无 net451 / no net451"),
    ("SqlSugar",                                  "无 netstandard2.0 / no netstandard2.0"),
    ("FreeSql / SqlKata / SqlSugar",              "net451 保留 #if NET451 手写（不引用生成器）/ net451 keeps the #if NET451 hand-written members (does not reference the generator)"),
    ("低代际（尤其 net451）/ low generations (especially net451)", "缺 IReadOnly* 暴露面；可空靠 internal 垫片（6.3.0）/ lacks the IReadOnly* surface; nullability relies on internal shims (6.3.0)"),
    ("Multi.NetFxTests",                          "net462 侧车工程，验证低代际暴露面 / a net462 sidecar project that validates the low-generation surface"),
};

Console.WriteLine($"{"包 / 目标 / Package / Target",-32}差异 / Difference");
Console.WriteLine(new string('-', 96));
foreach (var d in diffs)
{
    Console.WriteLine($"{d.Package,-32}{d.Diff}");
}

// ---------------------------------------------------------------------------
// 5. SpanBag 等类型只存在于「Span<T> 在框」的目标
// 5. Types such as SpanBag exist only on targets where "Span<T> is in the box"
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 5. 类型级 TFM 差异 ===");
Console.WriteLine("=== 5. Type-level TFM differences ===");
Console.WriteLine("SpanBag 等 Multi 类型仅 `Span<T>` 在框的目标——对照 Multi 文档，勿假设全包同矩阵。 / Multi types such as SpanBag exist only on targets where `Span<T>` is in the box — consult the Multi docs and do not assume all packages share the same matrix.");
Console.WriteLine("在 netstandard2.0 测试工程里直接用 net6+ only 的 API / 适配器会编不过。 / Using net6+ only APIs / adapters directly in a netstandard2.0 test project will not compile.");

// ❌ 错误写法：假设全包 11 TFM。
// ❌ Wrong approach: assuming all packages have 11 TFMs.
//    // 在 net48 上引用 EF Core 适配器 → 该包无 net451/net48 资产，还原/编译失败。
//    // referencing the EF Core adapter on net48 → the package has no net451/net48 assets, so restore/compile fails.
// ✅ 正确写法：选包前核对矩阵，写能力时标 TFM。
// ✅ Correct approach: verify the matrix before selecting a package, and annotate the TFM when documenting a capability.

Console.WriteLine();
Console.WriteLine("Sample.MultiTfmSurface completed.");
