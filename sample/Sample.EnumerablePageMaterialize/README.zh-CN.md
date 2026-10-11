# Sample.EnumerablePageMaterialize

> **中文** | [English](README.md)

> **EnumerablePage 物化** — 单次 `Skip/Take` 物化，深页不再随页号爆炸（对应 feature F39，`DotNetCore.Collections.Paginable`，6.0.0）。

## 本示例演示什么

`EnumerablePage<T>` 是内存 / 通用序列的页实现。6.0.0 修复了物化策略：非 `IList` 的 `IEnumerable` 从逐成员 `ElementAt(k+i)`（近似 `O(pageSize × skip)`，接近平方级）改为**单次** `Skip(skip).Take(s).ToList()`；片段（`sourceIsFull: false`）只 `Take(s)` 不再偏移。`GetPage` 侧另有两档：`ICollection<T>` 走 O(1) `Count` + `Skip/Take`，惰性非 `ICollection` 单次遍历同时取页与总长。本示例展示惰性深页、`List<T>` 快路径、物化快照语义、以及 `sourceIsFull` 两分支。

## 什么时候用

- 深翻页不随页号爆炸。
- 片段页不再被二次偏移（与 F32 的 `sourceIsFull: false` 配合）。
- 惰性序列只遍历一次。
- `IQueryable`（EF 等）交给提供方翻译，或适配器页类型。

## 如何运行

```bash
dotnet run --project sample/Sample.EnumerablePageMaterialize
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 惰性序列深页单次物化 | `SolidPageExtensions.GetPage`（`IEnumerable<T>`） |
| 2 | `List<T>` 快路径（`ICollection`） | `GetPage(pageNumber, pageSize)` |
| 3 | 物化快照：源变页不变 | `CurrentPageSize` / `TotalMemberCount` |
| 4 | `sourceIsFull` 分支 | `EnumerablePage<T>(..., sourceIsFull: true/false)` |

## 关键 API

- `EnumerablePage<T>(enumerable, currentPageNumber, pageSize, totalMemberCount, sourceIsFull = true)` — 公开构造。
- `SolidPageExtensions.GetPage<T>(enumerable, pageNumber, pageSize)` — 全源取页（`ICollection` / 惰性两档）。
- `IPage<T>.CurrentPageSize` / `TotalMemberCount` / `ToOriginalItems()` / `this[int].ItemNumber`。

## 注意事项 / 陷阱

- **`sourceIsFull` 语义**：`true` 表示入参是全源（按页号 `Skip`）；`false` 表示片段（只 `Take`）。
- **物化意味着源之后变化不影响页**：要新数据请重新取页。
- **空源合法**：`EmptyPage<T>` / `CurrentPageSize == 0`。
- **6.0.0 同时修了 `CurrentPageSize` 整除末页归 0 的缺陷**（见 F36）。
