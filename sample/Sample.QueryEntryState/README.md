# Sample.QueryEntryState

> **查询入口状态 `IQueryEntryState<T>`** — 把「一页元素从哪来」抽象成一个协变接口，供 `PageMember` 状态形态延迟取值（对应 feature F48，`DotNetCore.Collections.Paginable`，6.0.0）。

## 本示例演示什么

`IQueryEntryState<out T>` 位于 `DotNetCore.Collections.Paginable.Abstractions`，**只有一员** `IEnumerable<T> AllValues`。它把「一页的元素从哪来」抽象出来，供 `PageMember` 的状态形态（F46）延迟取值——页初始化时不必把元素全部物化成 `List`，`Value` 被读时才 `ElementAt(_offset)`。本示例展示：接口形状（只有 `AllValues`）、协变（`IQueryEntryState<Derived>` 可作 `IQueryEntryState<Base>`）、作为 `PageMember` 状态形态的来源，以及 `AllValues` 可能是延迟序列（多次枚举可能多次查询）。

## 什么时候用

- 查询页延迟读：状态形态 `PageMember`，大页不提前分配。
- 自定义页实现：实现此接口供 `PageMemberFactory` 用。
- 不该用：替代 `IEnumerable` 当集合——它只是入口状态。

## 如何运行

```bash
dotnet run --project sample/Sample.QueryEntryState
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 接口形状：只有 `AllValues` | `IQueryEntryState<T>.AllValues` |
| 2 | 协变 `out T` | `IQueryEntryState<Order>` → `IQueryEntryState<object>` |
| 3 | 作为 `PageMember` 状态形态来源 | `PageMemberFactory.Create<T>(IQueryEntryState<T>, int, ref int)` |
| 4 | 延迟序列：多次枚举 | 自定义实现统计 `AllValues` 读取次数 |

## 关键 API

- `IQueryEntryState<out T>`（`DotNetCore.Collections.Paginable.Abstractions`）— 只有 `IEnumerable<T> AllValues { get; }`。
- `PageMemberFactory.Create<T>(IQueryEntryState<T> state, int offset, ref int startIndex)` — 状态形态构造，`Value` 经 `AllValues.ElementAt(_offset)` 读取。

## 注意事项 / 陷阱

- **`AllValues` 可能是延迟序列**：多次枚举可能多次查询（取决于源）。
- **实现类 `QueryEntryState<T>` 是 internal**：使用方通常经 `PageMember.Value` 间接使用；示例用自定义 `IQueryEntryState<T>` 实现来演示。
- **不是集合类型**：不要当 `IList` 用；`ElementAt` 对非列表源是 O(n)。
- **协变接口**：`out T` 使类型兼容更宽。
- 热路径应一次物化（F39），避免反复读状态形态的 `Value`。
