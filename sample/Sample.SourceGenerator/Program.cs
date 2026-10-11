// Sample.SourceGenerator — SolidPageExtensions 源生成器：属性声明与生成物形状（F56）
// 运行：dotnet run --project sample/Sample.SourceGenerator
//
// 演示重点：
//   1. 6.5.0 起，**六个** ORM 适配器包的分页扩展成员由 Roslyn 增量生成器写出，替代逐包手写；
//   2. 包内 SolidPageExtensions（partial）挂 [SolidPageExtensionsFor(...)] 声明配置，
//      生成器据此写出 ToPaginable / ToPaginableAsync / GetPage / GetPageAsync 与类型级 XML；
//   3. 生成器是 **Analyzer**（OutputItemType=Analyzer、ReferenceOutputAssembly=false），
//      **不随消费者包发布**，消费者只感知「扩展方法仍在」，签名与手写前一致；
//   4. 说明型：不引用生成器工程、不要求真跑生成器，仅讲解属性声明与生成物形状。
using System;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// 说明型 sample 只引用分页核心包，不引用 SourceGenerators（那是 Analyzer，不是运行库）。
Console.WriteLine($"核心契约程序集：{typeof(IPage).Assembly.GetName().Name}");
Console.WriteLine();

// ---------------------------------------------------------------------------
// 1. 属性声明驱动生成（库内 partial 的声明形态）
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. [SolidPageExtensionsFor] 属性声明形态 ===");

// 生成器扫描带此特性的 partial 类；特性由生成器的 post-init 源注入（internal）。
// 构造函数 7 个位置参数：ormName、sourceTypeFullName、sourceParamName、
//                         pageTypeName、paginableTypeName、factoryTypeName、helperTypeName。
Console.WriteLine(@"[SolidPageExtensionsFor(
    ormName:             ""Chloe"",
    sourceTypeFullName:  ""Chloe.IQuery<T>"",
    sourceParamName:     ""query"",
    pageTypeName:        ""ChloePage"",
    paginableTypeName:   ""PaginableChloeQuery"",
    factoryTypeName:     ""PaginableChloeCollFactory"",
    helperTypeName:      ""ChloeHelper"",
    HasAdditionalQueryFunc = true)]
public static partial class SolidPageExtensions { }");

// ---------------------------------------------------------------------------
// 2. 关键属性开关（节选，逐字来自 F56）
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 关键属性开关 ===");

var switches = new (string Name, string Meaning)[]
{
    ("GenericConstraint",             "class / Entity（Dos 的 where T : Entity）"),
    ("HasAdditionalQueryFunc",        "Chloe / Dos 的查询变换 additionalQueryFunc"),
    ("HasIncludeNestedMembers",       "FreeSql 的 includeNestedMembers"),
    ("HasAsync + Async*Expression",   "异步计数 / 取页表达式（指向各包 Helper）"),
    ("SourceTypeIsNonGeneric",        "SqlKata：源类型非泛型 Query，方法带显式 <T>"),
    ("ForwardAsyncCancellationToken", "默认 true（6.5 取消令牌）"),
};

Console.WriteLine($"{"属性",-32}含义");
Console.WriteLine(new string('-', 96));
foreach (var s in switches)
{
    Console.WriteLine($"{s.Name,-32}{s.Meaning}");
}

// ---------------------------------------------------------------------------
// 3. 生成物形状：标准成员 + 手写特例
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 生成物形状 ===");

// 生成器写出（每个适配器一致，除开关差异）：
//   T ToPaginable<T>(...)、Task<...> ToPaginableAsync<T>(...)、
//   IPage<T> GetPage<T>(...)、Task<IPage<T>> GetPageAsync<T>(...)  + 类型级 XML 文档。
Console.WriteLine("生成成员：ToPaginable / ToPaginableAsync / GetPage / GetPageAsync + 类型级 XML。");
Console.WriteLine("手写特例留在 partial 内：NHibernate 的 ISession 重载、SqlKata 的 GetPageAsync 等。");
Console.WriteLine("未迁移（刻意）：EF6 / EF Core / FreeSql.DbContext——它们委托 queryable 扩展，本来不是重复形状。");

// ---------------------------------------------------------------------------
// 4. net451 双轨（不引用生成器，保留手写成员）
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. net451 双轨 ===");
Console.WriteLine("FreeSql / SqlKata / SqlSugar 在 net451 **不引用生成器**，保留 #if NET451 手写成员；");
Console.WriteLine("改生成器属性时要同步手写面，否则只测一个 TFM 会漏（F57）。");

// ---------------------------------------------------------------------------
// 5. 消费方可见性与不变量
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 5. 消费方可见性 ===");
Console.WriteLine("消费者看不到生成器类型；不要引用 SourceGenerators 包当运行库。");
Console.WriteLine("无消费方 API 变化 ⇒ 无 Breaking；消重效果：Chloe 约 95 行手写 → 约 19 行声明。");

// 断言：本 sample 引用的核心契约类型确实存在（说明型也不能是空跑）。
if (typeof(IPage).IsInterface && typeof(IPage<>).IsInterface)
{
    Console.WriteLine("核心契约 IPage / IPage<T> 就位——生成器产出的页类型实现同一契约。");
}

Console.WriteLine();
Console.WriteLine("Sample.SourceGenerator completed.");
