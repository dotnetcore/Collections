# Sample.FragmentPage

> **中文** | [English](README.md)

> **片段建页** — 从「已切好的一页 + 调用方自带元数据」装配 `IPage<T>`，片段绝不再被切片（`DotNetCore.Collections.Paginable`，6.1.0）。

## 本示例演示什么

当数据来自手写 Dapper/ADO.NET（`OFFSET`/`FETCH`）结果、缓存页，或上游 HTTP/gRPC 返回的 `items` + `totalCount` 时，用 `Paginable.CreatePage` / `FragmentPageExtensions.ToPage` 直接装配页面。它把片段**当作该页的精确内容**，绝不再 `Skip/Take`——因此刻意与 `GetPage`（对全源切片）不同名。本示例展示三元组元数据建页、`PageFragmentInfo` 往返、与全源分页的对比，以及两个高频陷阱。

## 什么时候用

- Dapper 已按 `offset/fetch` 取回 + 独立 `COUNT`。
- HTTP / gRPC 转发上游的 `items` + `totalCount`。
- 缓存的整页 JSON 反序列化后重建。
- 不该用：只有全源 List → 用 `GetPage`（**不要**先手动 `Skip/Take` 再 `ToPage`）。

## 如何运行

```bash
dotnet run --project sample/Sample.FragmentPage
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 三元组元数据建页与扩展方法糖 | `Paginable.CreatePage` / `FragmentPageExtensions.ToPage` |
| 2 | 元数据对象与往返 | `PageFragmentInfo` / `FromMetadata` / `ToMetadata` |
| 3 | 同一数据两种入口（全源 vs 片段） | `GetPage` vs `ToPage` |
| 4 | 长片段抛错、`total` 传片段长度的后果 | `ArgumentException` / `TotalPageCount` |

## 关键 API

- `Paginable.CreatePage<T>(fragment, pageNumber, pageSize, totalMemberCount)` — 三元组元数据建页。
- `Paginable.CreatePage<T>(fragment, PageFragmentInfo)` — 用已构造的元数据建页。
- `FragmentPageExtensions.ToPage<T>(fragment, pageNumber, pageSize, totalMemberCount)` — 扩展方法糖。
- `PageFragmentInfo.PageNumber` / `PageSize` / `TotalMemberCount` / `FromMetadata` / `ToMetadata`。

## 注意事项 / 陷阱

- **`totalMemberCount` 是全源总数，不是片段长度**：传 `fragment.Count` 会让 `TotalPageCount` 恒为 1。
- **片段比页容量长 → `ArgumentException`**；片段比元数据短 → 默认容忍（并发删除可接受，见 6.2.0）。
- **物化**：`CreatePage` 会把片段拷进 `List<T>`，防调用方事后改集合。
- **不要对预切片片段调用 `GetPage`**：会静默二次 skip；用 `ToPage` / `CreatePage`。
- **越界元数据抛 `ArgumentOutOfRangeException`**（6.2.0 统一口径）。
