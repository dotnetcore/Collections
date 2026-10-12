# Sample.TypeReadableName

> **中文** | [English](README.md)

> **可读类型名格式化** — `TypeExtensions.ToReadableString()` / `TypeNameFormat`（`DotNetCore.Collections.Multi`，6.6.0）。

## 本示例演示什么

`TypeExtensions.ToReadableString()` 把 `System.Type` 渲染成一行可读类型名，用于诊断、日志与测试失败输出：
保留数组秩、递归渲染泛型实参、丢弃命名空间——呈现**形状**而非程序集限定身份。示例展示三个侧面：
默认 CLR 口径、C# 口径（关键字别名 + 可空）、以及 `maxDepth` 深度折叠。输出稳定，同一 `Type` 恒同一字符串，可断言。

## 什么时候用

- 测试失败信息：一眼看清类型形状。
- 诊断日志：一行搞定，不再是 `FullName` 的程序集限定串。
- 断言类型名：输出稳定。
- 不该用：类型反序列化 / type id（用 `FullName` / `AssemblyQualifiedName`）。

## 如何运行

```bash
dotnet run --project sample/Sample.TypeReadableName
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 默认 CLR 口径 | `ToReadableString(this Type)` / `TypeNameFormat.Clr` |
| 2 | C# 口径（关键字 / 可空） | `ToReadableString(this Type, TypeNameFormat)` / `TypeNameFormat.CSharp` |
| 3 | 深度折叠 | `ToReadableString(this Type, TypeNameFormat, int)` / `DefaultMaxDepth` |
| 4 | 参数校验与边界 | `null` 抛 `ArgumentNullException`；`maxDepth < 1` 抛 `ArgumentOutOfRangeException` |

## 关键 API

- `TypeExtensions.ToReadableString(this Type)` — 默认 `TypeNameFormat.Clr`，`DefaultMaxDepth = 16`。
- `TypeExtensions.ToReadableString(this Type, TypeNameFormat)` — 指定口径。
- `TypeExtensions.ToReadableString(this Type, TypeNameFormat, int maxDepth)` — 指定口径 + 嵌套深度上限。
- `TypeNameFormat.Clr`（0）— CLR 拼写、去命名空间；`TypeNameFormat.CSharp`（1）— 基元关键字别名 + `Nullable<T>` → `T?`。
- `TypeExtensions.DefaultMaxDepth` — 默认深度上限常量（16）。

## 注意事项 / 陷阱

- **丢弃命名空间**：同名不同命名空间的类型字符串相同，不是唯一标识；标识用 `AssemblyQualifiedName`。
- **不是 type id**：勿用于反序列化 / 类型查找。
- **`type == null` 抛 `ArgumentNullException`**；**`maxDepth < 1` 抛 `ArgumentOutOfRangeException`**。
- **递归有界**：超过 `maxDepth` 的嵌套折叠为 `...`；反射可无限 `MakeGenericType`，有界遍历保证诊断路径不炸栈。
- **断言用本 API 的稳定输出**：断言 `FullName` 样式会失败。
