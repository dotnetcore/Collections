# Sample.ExceptionTypeUnify

> **中文** | [English](README.md)

> **越界异常类型统一** — 全部分页入口的越界参数一律抛 `ArgumentOutOfRangeException` 并携带 `ParamName`（对应 feature F40，`DotNetCore.Collections.Paginable`，6.2.0 破坏性变更）。

## 本示例演示什么

6.2.0 把**所有分页入口**（核心 `IEnumerable` / `IQueryable` / `Task<IQueryable>`、键集、9 个 ORM 适配器）的越界报告从 `IndexOutOfRangeException` 统一为 `ArgumentOutOfRangeException`，约 40 处 throw 点。本示例在内存源上走查：三类拒绝条件（`pageNumber < 1`、`pageSize < 1`、越过末页）各自抛出的异常类型与 `ParamName`，空源返回空页不抛，以及 `ArgumentException` 兜底。

## 什么时候用

- 需要统一 `catch` / 断言 / 日志处理分页越界。
- 6.1 升级到 6.2+ 时，全局搜 `IndexOutOfRangeException` 并替换。
- 想依赖 `ParamName` 直指错误参数做诊断。

## 如何运行

```bash
dotnet run --project sample/Sample.ExceptionTypeUnify
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | `pageNumber < 1` → `ArgumentOutOfRangeException(pageNumber)` | `IEnumerable<T>.GetPage(int, int)` |
| 2 | `pageSize < 1` → `ArgumentOutOfRangeException(pageSize)` | `IEnumerable<T>.GetPage(int, int)` |
| 3 | `pageNumber` 越过末页 | `IEnumerable<T>.GetPage(int, int)` |
| 4 | 空源返回单个空页，不抛 | `IEnumerable<T>.GetPage(int, int)` |
| 5 | 片段入口同样统一 | `Paginable.CreatePage<T>(...)` |
| 6 | 派生自 `ArgumentException`，可宽 catch | `ArgumentException` |

## 关键 API

- `SolidPageExtensions.GetPage<T>(this IEnumerable<T>, int pageNumber, int pageSize)`（核心包）— 越界抛 `ArgumentOutOfRangeException`，`ParamName` 为 `"pageNumber"` / `"pageSize"`。
- `Paginable.CreatePage<T>(IEnumerable<T>, int, int, int)` — 片段入口，越界同样抛 `ArgumentOutOfRangeException`。
- `ArgumentOutOfRangeException` — 派生自 `ArgumentException`；`ParamName` 可依赖（15 个测试已钉死）。

## 注意事项 / 陷阱

- **破坏性**：依赖精确异常类型的 catch / 断言需改为 `ArgumentOutOfRangeException`。
- **只改类型不改条件**：拒绝条件与旧版一致；**空源仍返回单个空页**，不抛。
- **旧 `IndexOutOfRangeException` 不是 `ArgumentException`**：迁移期若要兼容 6.1 旧库，需双 catch 过渡。
- **勿与 Multi 侧另一条破坏性混淆**（`times <= 0` 抛 `ArgumentOutOfRangeException`，与本文无关）。
