# Sample.PageMemberWrapping

> **中文** | [English](README.md)

> **页成员包装与全局编号** — 页的每个元素被包装为 `PageMember<T>`（struct）：`Value` / `Offset` / `ItemNumber`（对应 feature F46，`DotNetCore.Collections.Paginable`，6.0.0–6.1.0）。

## 本示例演示什么

`IPage<T>` 枚举出来的不是裸元素，而是 `PageMember<T>` 包装，携带三个语义：`Value`（元素值）、`Offset`（页内 0 基）、`ItemNumber`（**全局 1 基编号** = `startIndex + offset + 1`）。本示例展示：第 3 页第 1 条（页大小 5）的 `ItemNumber` 是 11 而非 1；工厂 `PageMemberFactory` 的**值形态**与**状态形态**两种构造；状态形态持 `IQueryEntryState<T>`，`Value` 经 `AllValues.ElementAt(offset)` 延迟读；以及 struct 拷贝语义与 `offset < 0` 构造抛。

## 什么时候用

- 表格「序号」列：直接读 `ItemNumber`，跨页连续。
- 大页延迟读：状态形态避免提前物化（与 F39 / F48 配套）。
- 不该用：当可变列表元素——struct 包装，改 `Value` 不回写源。

## 如何运行

```bash
dotnet run --project sample/Sample.PageMemberWrapping
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 全局编号 / 页内偏移 | `IPage<T>` 索引器 → `IPageMember<T>.ItemNumber` / `.Offset` / `.Value` |
| 2 | 值形态工厂构造 | `PageMemberFactory.Create<T>(T, int, ref int)` |
| 3 | 状态形态（延迟取值） | `PageMemberFactory.Create<T>(IQueryEntryState<T>, int, ref int)` |
| 4 | struct 语义 / `offset < 0` 抛 | `PageMember<T>` / `ArgumentOutOfRangeException` |

## 关键 API

- `PageMember<T> : IPageMember<T>` — `struct`；`Value` / `Offset` / `ItemNumber`。
- `IPageMember<out T>` — 只读接口：`Value` / `Offset` / `ItemNumber`。
- `PageMemberFactory.Create<T>(T memberValue, int offset, ref int startIndex)` — 值形态。
- `PageMemberFactory.Create<T>(IQueryEntryState<T> state, int offset, ref int startIndex)` — 状态形态，`Value` 经 `AllValues.ElementAt(_offset)` 读取。
- `PageMemberFactory` 位于 `DotNetCore.Collections.Paginable.Internal` 命名空间，但类型为 `public`。

## 注意事项 / 陷阱

- **`ItemNumber` 是 1 基全局号**，不是页内下标；页内用 `Offset` 或索引器。
- **状态形态的 `Value` 每次可能重查**（对非列表源是 O(n)）；热路径避免反复读。
- **struct：拷贝语义**——改包装不回写源集合。
- **`offset < 0` 构造抛** `ArgumentOutOfRangeException`（`ParamName = "offset"`）。
