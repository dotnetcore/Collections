// Sample.TypeReadableName — 可读类型名格式化（6.6.0）
// Sample.TypeReadableName — readable type-name formatting (6.6.0)
// 运行：dotnet run --project sample/Sample.TypeReadableName
// Run: dotnet run --project sample/Sample.TypeReadableName
//
// 演示重点：
// Highlights:
//   1. TypeExtensions.ToReadableString() 把 System.Type 渲染成一行**可读**类型名：保留数组秩、
//   1. TypeExtensions.ToReadableString() renders a System.Type as a one-line **readable** type name: it preserves array rank,
//      递归渲染泛型实参、丢弃命名空间——呈现**形状**而非程序集限定身份。
//      recursively renders generic arguments, and drops namespaces — presenting the **shape** rather than the assembly-qualified identity.
//   2. 两种口径：TypeNameFormat.Clr（默认，CLR 拼写、去命名空间）与 TypeNameFormat.CSharp
//   2. Two flavors: TypeNameFormat.Clr (default; CLR spelling, namespaces removed) and TypeNameFormat.CSharp
//      （基元关键字别名 + Nullable<T> → T?）。
//      (primitive keyword aliases + Nullable<T> → T?).
//   3. 递归有界：超过 maxDepth 的数组 / 泛型嵌套折叠为 ...，诊断路径不炸栈；输出稳定可断言。
//   3. Recursion is bounded: array / generic nesting beyond maxDepth folds to ...; the diagnostic path never blows the stack; the output is stable and assertable.
using System;
using System.Collections.Generic;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 默认 CLR 口径：去命名空间，保留形状
// 1. Default CLR flavor: drop namespaces, keep the shape
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 默认 CLR 口径 ===");
Console.WriteLine("=== 1. Default CLR flavor ===");

// 多维数组 + 嵌套泛型：外层数组秩在前（C# 源码顺序），泛型实参递归渲染。
// A multidimensional array + nested generics: the outer array rank comes first (C# source order), and generic arguments are rendered recursively.
string clr = typeof(Dictionary<DateTimeOffset, IReadOnlyDictionary<string, int>>[,])
    .ToReadableString();
Console.WriteLine(clr);   // Dictionary<DateTimeOffset,IReadOnlyDictionary<String,Int32>>[,]

// 输出稳定：同一 Type 恒同一字符串，可断言。
// Stable output: the same Type always yields the same string, so it can be asserted.
string again = typeof(Dictionary<DateTimeOffset, IReadOnlyDictionary<string, int>>[,])
    .ToReadableString();
Console.WriteLine($"稳定输出？ / Stable output? {clr == again}");   // True

// 简单类型也去掉命名空间：System.String -> String。
// Simple types also drop the namespace: System.String -> String.
Console.WriteLine(typeof(string).ToReadableString());   // String
// 嵌套类型用 . 而非 +（反射的 + 被替换掉），泛型实参表挂在末尾。
// Nested types use . instead of + (reflection's + is replaced), and the generic argument list hangs at the end.
Console.WriteLine(typeof(Dictionary<string, int>.Enumerator).ToReadableString()); // Dictionary.Enumerator<String,Int32>

// ---------------------------------------------------------------------------
// 2. C# 口径：关键字别名 + 可空
// 2. C# flavor: keyword aliases + nullable
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. C# 口径 ===");
Console.WriteLine("=== 2. C# flavor ===");

// TypeNameFormat.CSharp 用关键字别名：string / int?（Nullable<T> -> T?）。
// TypeNameFormat.CSharp uses keyword aliases: string / int? (Nullable<T> -> T?).
string csharp = typeof(Dictionary<string, int?>).ToReadableString(TypeNameFormat.CSharp);
Console.WriteLine(csharp);   // Dictionary<string,int?>

// 同一 Type 双口径对照：形状一致，只差基元 / 可空拼写。
// The same Type under both flavors: the same shape, differing only in the primitive / nullable spelling.
string clrSame = typeof(Dictionary<string, int?>).ToReadableString(TypeNameFormat.Clr);
Console.WriteLine($"Clr    = {clrSame}");     // Dictionary<String,Nullable<Int32>>
Console.WriteLine($"CSharp = {csharp}");      // Dictionary<string,int?>

// ---------------------------------------------------------------------------
// 3. 深度上限：更深嵌套折叠为 ...
// 3. Depth limit: deeper nesting folds to ...
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 深度上限 ===");
Console.WriteLine("=== 3. Depth limit ===");

// maxDepth 默认 16；这里限制为 2，第三层嵌套被折叠。
// maxDepth defaults to 16; limited to 2 here, so the third level of nesting is folded.
string shallow = typeof(List<List<List<int>>>)
    .ToReadableString(TypeNameFormat.Clr, maxDepth: 2);
Console.WriteLine(shallow);   // List<List<...>>

// 不限制时完整展开。
// Without a limit it expands fully.
Console.WriteLine(typeof(List<List<List<int>>>).ToReadableString()); // List<List<List<Int32>>>
// 有界递归是刻意的：反射可无限 MakeGenericType，诊断路径不能炸栈。
// Bounded recursion is deliberate: reflection can MakeGenericType indefinitely, and a diagnostic path must not blow the stack.
Console.WriteLine($"DefaultMaxDepth = {TypeExtensions.DefaultMaxDepth}"); // 16

// ---------------------------------------------------------------------------
// 4. 参数校验与用途边界
// 4. Argument validation and usage boundaries
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 参数校验与边界 ===");
Console.WriteLine("=== 4. Argument validation and boundaries ===");

try
{
    Type? nullType = null;
    nullType!.ToReadableString();   // type == null 抛 ArgumentNullException
                                    // type == null throws ArgumentNullException
}
catch (ArgumentNullException ex)
{
    Console.WriteLine($"null 类型抛 / null type throws: {ex.ParamName}");   // type
}

try
{
    typeof(int).ToReadableString(TypeNameFormat.Clr, maxDepth: 0);   // maxDepth < 1 抛
                                                                    // maxDepth < 1 throws
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"maxDepth < 1 抛 / maxDepth < 1 throws: {ex.ParamName}");   // maxDepth
}

// ⚠️ 陷阱：本 API **丢弃命名空间**，同名不同命名空间的类型字符串会相同——它不是 type id。
// ⚠️ Pitfall: this API **drops namespaces**, so types with the same name in different namespaces produce the same string — it is not a type id.
//    要唯一标识请用 Type.FullName / AssemblyQualifiedName。
//    For a unique identity use Type.FullName / AssemblyQualifiedName.
Console.WriteLine("ToReadableString 只呈现形状，不是唯一类型标识（勿用于反序列化）。 / ToReadableString only presents the shape, not a unique type identity (don't use it for deserialization).");

Console.WriteLine();
Console.WriteLine("Sample.TypeReadableName completed.");
