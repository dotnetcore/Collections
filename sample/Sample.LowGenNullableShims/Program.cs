// Sample.LowGenNullableShims — 低代际 TFM 的可空属性垫片（F43）
// Sample.LowGenNullableShims — nullability attribute shims for low-generation TFMs (F43)
// 运行：dotnet run --project sample/Sample.LowGenNullableShims
// Run: dotnet run --project sample/Sample.LowGenNullableShims
//
// 演示重点：
// Highlights:
//   1. 垫片是**内部实现细节**：net451/net461/net47/net48/netstandard2.0 编译进程序集的
//   1. The shims are an **internal implementation detail**: the polyfills compiled into the assembly for net451/net461/net47/net48/netstandard2.0
//      NullableAttribute / NullableContextAttribute / NullablePublicOnlyAttribute polyfill，
//      (NullableAttribute / NullableContextAttribute / NullablePublicOnlyAttribute),
//      netstandard2.1 与 net6.0+ 用运行时自带的同名类型（本 sample 的 net8 资产即后者）。
//      while netstandard2.1 and net6.0+ use the same-named types built into the runtime (this sample's net8 asset is the latter).
//   2. 使用方无感：无论哪个 TFM，公开 API 的空性标注都能被编译器 / 反射读到。
//   2. Transparent to the consumer: no matter which TFM, the nullability annotations of the public APIs can be read by the compiler / reflection.
//   3. 不要引用垫片类型名——它不是公开 API（内部类型、无公开暴露）。
//   3. Do not reference the shim type names — they are not public API (internal types, no public exposure).
using System;
using System.Collections.Generic;
using System.Reflection;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var orders = new List<Order> { new(1, "A"), new(2, "B") };

// ---------------------------------------------------------------------------
// 1. 使用方视角：可空契约直接可用（无需知道垫片存在）
// 1. Consumer's view: the nullable contract works directly (no need to know the shims exist)
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 使用方视角：可空参数照常可用 ===");
Console.WriteLine("=== 1. Consumer's view: the nullable parameter works as usual ===");
var set = orders.ToPaginable(pageSize: 2, limitedMemberCount: null);   // int? 可空参数，null = 不限 / an int? nullable parameter, null = unlimited
Console.WriteLine($"ToPaginable(limitedMemberCount: null) → PageCount={set.PageCount}, MemberCount={set.MemberCount}");

// ---------------------------------------------------------------------------
// 2. 注解元数据可被读取（垫片保证低 TFM 也输出这份元数据）
// 2. The annotation metadata is readable (the shims ensure low TFMs also emit this metadata)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 公开 API 的空性注解元数据可被反射读到 ===");
Console.WriteLine("=== 2. The nullability annotation metadata of the public APIs is readable via reflection ===");

// 编译器把 T? / 非空 标注写成 NullableAttribute / NullableContextAttribute 元数据；
// The compiler writes the T? / non-null annotations as NullableAttribute / NullableContextAttribute metadata;
// 在低 TFM 上，正是垫片让这些属性类型有定义可编，元数据才得以输出。
// on low TFMs it is exactly the shims that give these attribute types a definition to compile against, so the metadata can be emitted.
var lastMemberProp = typeof(KeysetPage<Order>).GetProperty(nameof(KeysetPage<Order>.LastMember))!;
var readState = new NullabilityInfoContext().Create(lastMemberProp).ReadState;
Console.WriteLine($"KeysetPage<Order>.LastMember.ReadState = {readState}");   // Nullable

var updateMethod = typeof(PaginableSettingsManager).GetMethod(nameof(PaginableSettingsManager.UpdateSettings))!;
var writeState = new NullabilityInfoContext().Create(updateMethod.GetParameters()[0]).WriteState;
Console.WriteLine($"UpdateSettings(settings).WriteState     = {writeState}"); // NotNull

// ---------------------------------------------------------------------------
// 3. 垫片是 internal，不属于公开 API 面
// 3. The shims are internal and are not part of the public API surface
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 垫片无公开类型 ===");
Console.WriteLine("=== 3. The shims have no public types ===");

var asm = typeof(PaginableSettingsManager).Assembly;
// net8 资产不编入垫片（运行时自带），故此处查不到该类型定义；低 TFM 才会编入 internal polyfill。
// The net8 asset does not compile in the shims (the runtime provides them), so the type definition cannot be found here; only low TFMs compile in the internal polyfill.
var shimType = asm.GetType("System.Runtime.CompilerServices.NullableContextAttribute");
Console.WriteLine($"程序集 {asm.GetName().Name} 定义 NullableContextAttribute / Assembly {asm.GetName().Name} defines NullableContextAttribute = {shimType is not null} (net8 资产应为 False / net8 asset should be False: 运行时自带 / provided by the runtime)");

// 断言：公开导出类型里没有以垫片命名的类型（垫片只应是 internal）。
// Assertion: none of the publicly exported types is named after a shim (the shims should only be internal).
var leaked = Array.FindAll(
    asm.GetExportedTypes(),
    t => t.Name is "NullableAttribute" or "NullableContextAttribute" or "NullablePublicOnlyAttribute");
Console.WriteLine($"公开类型中泄漏的垫片类型数 / Leaked shim types among public types = {leaked.Length}");   // 0

// ❌ 错误写法：把垫片类型名当 API 写进业务代码 / 文档（internal，找不到或不可用）。
// ❌ Wrong approach: writing shim type names into business code / docs as if they were API (internal, not found or not usable).
// ✅ 正确写法：只描述公开 API 的空性，垫片交给库自己处理。
// ✅ Correct approach: describe only the nullability of the public APIs and leave the shims to the library itself.

Console.WriteLine();
Console.WriteLine("Sample.LowGenNullableShims completed.");

/// <summary>示例域模型：订单。</summary>
/// <summary>Sample domain model: an order.</summary>
internal sealed record Order(int Id, string Name);
