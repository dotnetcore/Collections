# Sample.KeysetPagination

> **中文** | [English](README.md)

> **键集分页（keyset / seek pagination）** — 用排序键锚点续页，不做 `OFFSET` 扫描也不做 `COUNT(*)` 回环（对应 feature F30，`DotNetCore.Collections.Paginable`，6.0.0）。

## 本示例演示什么

`KeysetPageExtensions` 提供 `GetFirstPageByKeyset` / `GetPageByKeyset`：第一页不带锚点，之后每次把「上一页最后一键」喂回去，形成「第一页 → 下一页 → … → `HasNext == false`」的闭环。结果类型是具体的 `KeysetPage<T>`（**不是** `IPage<T>`），刻意不携带总页数 / 总成员数。本示例展示内存委托重载、`IQueryable` 表达式树路径、`PaginableKeyset.BuildKeysetQuery` 的「只组查询」契约，以及空源与非法 `pageSize` 的边界。

## 什么时候用

- 无限滚动 / 消息流 / 活动流：只向前翻，不需要总页数。
- 深分页导出、扫全表分批：成本与页深无关。
- 游标分页 REST API：锚点即 cursor。
- 不该用：需要「跳到第 N 页」或展示总条数 → 偏移分页 `GetPage` / `ToPaginable`。

## 如何运行

```bash
dotnet run --project sample/Sample.KeysetPagination
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 第一页 → 读 `LastMember` → 续页 → `HasNext` 结束 | `GetFirstPageByKeyset` / `GetPageByKeyset` / `LastMember` |
| 2 | 内存委托重载与降序续页 | `IEnumerable<T>` 重载 + `descending: true` |
| 3 | 只组查询、保留 `pageSize + 1` 探测行 | `PaginableKeyset.BuildKeysetQuery` |
| 4 | 空源合法空页 / `pageSize` 校验 | `KeysetPage<T>.CurrentPageSize` / `ArgumentOutOfRangeException` |

## 关键 API

- `KeysetPageExtensions.GetFirstPageByKeyset<T,TKey>(keySelector, pageSize, descending = false)` — 第一页，无锚点。
- `KeysetPageExtensions.GetPageByKeyset<T,TKey>(keySelector, lastKey, pageSize, descending = false)` — 锚点之后的下一页。
- `PaginableKeyset.BuildKeysetQuery(source, keySelector, lastKey, pageSize, descending)` — 只组查询（`Where + OrderBy + Take(pageSize + 1)`），不物化。
- `KeysetPage<T>.Members` / `PageSize` / `CurrentPageSize` / `HasNext` / `IsFirstPage` / `LastMember`（`T?`）。

## 注意事项 / 陷阱

- **无总数、无页号、无 `IPage` 导航**：`KeysetPage<T>` 刻意不是 `IPage<T>`；页码控件请用偏移分页。
- **`LastMember` 可能为 `default`**（空页为 `null`）：解引用前先判 `CurrentPageSize > 0`。
- **排序键必须稳定且可比较**：`int` / `long` / `DateTime` / `decimal` / `DateTimeOffset` 等支持 `<` / `>` 的类型；键重复时需决胜键。
- **`BuildKeysetQuery` 保留 `pageSize + 1` 条**：调用方需自行裁掉探测行；用 `GetFirstPageByKeyset` / `GetPageByKeyset` 则已裁好。
- **越界 / 非法参数抛 `ArgumentOutOfRangeException`**（6.2.0 统一），不是 `IndexOutOfRangeException`。
