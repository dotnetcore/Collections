# Sample.SinglePageSet

> **单页集合包装** — 把「一页已装配数据 + 全源元数据」包成 `IPaginable<T>`，`PageCount` 恒为 1（对应 feature F34，`DotNetCore.Collections.Paginable`，6.2.0）。

## 本示例演示什么

当签名需要 `IPaginable<T>`（或 `IEnumerable<IPage<T>>`），而手头只有一页已装配数据时，用 `Paginable.CreateSinglePageSet` 把它包成单页集合，避免编造其余页或只为满足形状重新拉全源。关键是它的**分层语义**：集合层 `PageCount` 恒为 1（「我给你几页」），页层却保留全源编号（`CurrentPageNumber` / `TotalPageCount` 不动）。本示例展示接口面消费、枚举只 yield 一页、越界取页、以及带 `PageFragmentInfo` / `PageCreationOptions` 的用法。

## 什么时候用

- 服务层返回 `IPaginable<T>`，仓储层只拿到一页 DTO + total。
- gRPC / HTTP 适配层把 `items + totalCount` 转成内部统一分页形状。
- 测试：固定一页夹具喂给依赖 `IPaginable<T>` 的代码。
- 只要一页 `IPage<T>` → 直接 `CreatePage`，不必包集合；真正多页懒加载 → `ToPaginable`。

## 如何运行

```bash
dotnet run --project sample/Sample.SinglePageSet
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 包装并按接口消费（分层计数） | `Paginable.CreateSinglePageSet` / `PageCount` / `MemberCount` |
| 2 | 枚举只 yield 一页 | `GetEnumerator()` |
| 3 | 取不存在的页 → 越界 | `GetPage(2)` → `ArgumentOutOfRangeException` |
| 4 | 带元数据对象与选项 | `PageFragmentInfo` / `PageCreationOptions.Strict` |

## 关键 API

- `Paginable.CreateSinglePageSet<T>(fragment, pageNumber, pageSize, totalMemberCount)` — 三元组元数据包装。
- `Paginable.CreateSinglePageSet<T>(fragment, PageFragmentInfo[, PageCreationOptions])` — 元数据对象 / 选项包装。
- `PaginableSinglePage<T>.PageCount`（恒 1）/ `MemberCount`（全源总数）/ `GetPage(1)` / `GetPage(n≠1)` 越界。
- `IPaginable<T>.GetPage` / `IPaginable.PageSize` / `IPaginable.MemberCount`。

## 注意事项 / 陷阱

- **`PageCount` 不是内页 `TotalPageCount`**：前者集合层恒 1，后者是全源页数——有意分叉。
- **`MemberCount` 是全源总数**，不是 `items.Count`。
- **构造函数是 `internal`**：只能经 `Paginable.CreateSinglePageSet` 创建。
- **`GetPage(n≠1)` 抛 `ArgumentOutOfRangeException`**：集合物理上只有一页。
- **`foreach` 只执行一次**：不要以为会有多页；与 `CreateEmptyPage`（无成员的一页）语义不同。
