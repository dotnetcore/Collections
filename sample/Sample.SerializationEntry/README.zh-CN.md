# Sample.SerializationEntry

> **中文** | [English](README.md)

> **Paginable 侧无独立序列化 API（负结果）+ 正确重建路径** — `PageMetadata` + `Paginable.CreatePage`（对应 feature 6.2.0，`DotNetCore.Collections.Paginable`）。

## 本示例演示什么

DOC_CATALOG 曾把 6.2.0 标为「序列化入口（Paginable 侧若无则合并 6.2.0）」。核实结论是**负结果**：Paginable 核心
与 9 个适配器**都没有** `ToSerializableModel` / `FromModel` 一类显式序列化模型——那是 **Multi** 的 API
（`MultiListModel<T>` / `MultiDictionaryModel<TKey,TValue>`，见 6.2.0）。这个 sample 演示「查过、确认没有」的
负结果，并给出跨进程传一页的**正确路径**：投影 `items` + 三元组元数据（`CurrentPageNumber` / `PageSize` /
`TotalMemberCount`），对端用 `Paginable.CreatePage` 重建 `IPage<T>`，全局编号与整源分页完全一致。

## 什么时候用

- 需要把一页数据跨进程 / 跨服务传输 → 序列化 `items` + 三个整数，对端 `CreatePage` 重建。
- 需要序列化分页导航信息 → 序列化 `PageMetadata` 的公共属性。
- 需要 Multi 集合的显式序列化 → 去 6.2.0（`Sample.ExplicitSerialization`）。
- 不该用：直接序列化 `IPage` / `IPaginable` 对象——它们不是序列化契约。

## 如何运行

```bash
dotnet run --project sample/Sample.SerializationEntry
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 0 | 服务端整源分页 | `IEnumerable<T>.GetPage(pageNumber, pageSize)` |
| 1 | 负结果：无 `ToSerializableModel` | 说明（正确路径指向 6.2.0 / 片段 API） |
| 2 | `PageMetadata` 属性快照 | `IPage.GetMetadata()` |
| 3 | 投影 DTO → 重建 | `IPage<T>.ToOriginalItems()` / `Paginable.CreatePage(items, pageNumber, pageSize, totalMemberCount)` |
| 4 | 元数据往返 | `PageFragmentInfo.FromMetadata` / `ToMetadata` |
| 5 | 为什么不直接序列化页对象 | `IPage` / `IPaginable` 非序列化契约 |

## 关键 API

- `PageMetadata` — 普通 get-only 属性快照（`TotalPageCount` / `RealPageCount` / `TotalMemberCount` / `PageSize` /
  `CurrentPageNumber` / `CurrentPageSize` / `HasPrevious` / `HasNext`），`ToString` 可读，可被任意序列化器处理。
- `Paginable.CreatePage<T>(IEnumerable<T> fragment, int pageNumber, int pageSize, int totalMemberCount)` — 从已切片片段重建页。
- `PageFragmentInfo.FromMetadata(PageMetadata)` / `ToMetadata()` — 片段元数据往返。
- `IPage<T>.ToOriginalItems()` — 把页成员投影回原始元素序列。

## 注意事项 / 陷阱

- **不要调用不存在的** `Paginable.ToSerializableModel()`——那是 Multi 的（6.2.0）。
- **`IPage` / `IPaginable` 实现不是序列化契约**——投影成 DTO，勿直接序列化页对象。
- **`CreatePage` 不重新切片**：片段被当作该页的确切内容；喂给 `GetPage` 会二次跳过。
- **Multi 序列化模型不是 Paginable 的**；负结果篇防止 API 幻觉。
