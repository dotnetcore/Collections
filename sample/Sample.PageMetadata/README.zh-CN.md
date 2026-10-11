# Sample.PageMetadata

> **中文** | [English](README.md)

> **页面元数据与校验** — 页上的元数据快照、调用方输入元数据、以及两者共用的派生源（对应 feature F36，`DotNetCore.Collections.Paginable`，6.0–6.1）。

## 本示例演示什么

页面元数据回答「这是第几页、共几页、前后有没有」。两个公开类型分工明确：`PageMetadata` 是页上的**快照**（可序列化、可断言、不随源变化），`PageFragmentInfo` 是调用方**输入**用的元数据（构造即校验）。二者通过 `FromMetadata` / `ToMetadata` 互转，且与页实例共用同一 `PageMetaInfo.Calculate` 派生源，导航字段不会漂移。本示例展示快照读取、`ToString` 摘要、往返一致性，以及空页 `TotalPageCount=1` / `RealPageCount=0` 的关键区别。

## 什么时候用

- API 返回 `{ items, page: {...} }` → `page.GetMetadata()`。
- 断言重建页与原页一致 → `FromMetadata` / `ToMetadata`。
- 日志打印分页状态 → `metadata.ToString()`。
- 需要携带总数 / 总页数控件时。

## 如何运行

```bash
dotnet run --project sample/Sample.PageMetadata
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 读页元数据快照 | `IPage.GetMetadata()` → `PageMetadata` |
| 2 | 多行摘要 | `PageMetadata.ToString()` |
| 3 | 片段元数据往返 | `PageFragmentInfo.FromMetadata` / `ToMetadata` |
| 4 | 空页双计数 | `TotalPageCount` / `RealPageCount` |

## 关键 API

- `PageMetadata.TotalPageCount` / `RealPageCount` / `TotalMemberCount` / `PageSize` / `CurrentPageSize` / `CurrentPageNumber` / `HasPrevious` / `HasNext`。
- `PageFragmentInfo.FromMetadata(PageMetadata)` / `ToMetadata()`。
- `Paginable.CreateEmptyPage<T>()` — 空页（无成员的一页）。

## 注意事项 / 陷阱

- **`PageMetadata` 是快照**，不随源变化。
- **空源 `TotalPageCount == 1`、`RealPageCount == 0`**：判「没有页」请用 `TotalMemberCount == 0` 或 `RealPageCount == 0`。
- **`PageFragmentInfo` 构造即校验**：越界元数据抛 `ArgumentOutOfRangeException`（F40）。
- **空页 `FromMetadata` 可能抛**（无可用 pageSize）。
- **`CurrentPageSize` 在 Lenient 短片段下是元数据值**（F33）。
