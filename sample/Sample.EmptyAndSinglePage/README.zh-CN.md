# Sample.EmptyAndSinglePage

> **中文** | [English](README.md)

> **空页与单页集合** — `EmptyPage<T>`（无结果不返回 null）与 `PaginableSinglePage<T>`（一页包成集合，`PageCount` 恒为 1）（对应 feature F49，`DotNetCore.Collections.Paginable`，6.2.0）。

## 本示例演示什么

两个「边界页」类型。`EmptyPage<T>` 是空结果页：查询无结果时返回它而非 `null`，固定 `pageNumber: 1`、`pageSize: 0`、`totalMemberCount: 0`。`PaginableSinglePage<T>` 把**已经存在**的一页包成 `IPaginable<T>`，供「签名要集合、手头只有一页」的调用方使用（缓存页、gRPC 的 `items + totalCount`）。关键在**元数据分层**：集合层 `PageCount` 恒为 1、`MemberCount` 是源总数；页层保留全局编号（12 页中的第 3 页仍是 3/12）。

## 什么时候用

- 查询 0 行：统一返回 `EmptyPage<T>`，下游可安全枚举。
- 只有一页数据但 API 要 `IPaginable<T>`：用 `Paginable.CreateSinglePageSet`，不伪造其余页。
- 不该用：假装有多页（`PageCount` 刻意恒为 1）。

## 如何运行

```bash
dotnet run --project sample/Sample.EmptyAndSinglePage
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 空页：无结果不是 null | `new EmptyPage<T>()` / `Paginable.CreateEmptyPage<T>()` |
| 2 | 单页集合：集合层 `PageCount` 恒为 1 | `Paginable.CreateSinglePageSet<T>(...)` / `PaginableSinglePage<T>.PageCount` |
| 3 | `GetPage(2)` 越界 | `PaginableSinglePage<T>.GetPage(int)` → `ArgumentOutOfRangeException` |
| 4 | `PageFragmentInfo` 入口 + 分层陷阱 | `PageFragmentInfo` / `PaginableSinglePage<T>.MemberCount` |

## 关键 API

- `EmptyPage<T> : EnumerablePage<T>` — 构造为 `Enumerable.Empty<T>()`、`pageNumber: 1`、`pageSize: 0`、`totalMemberCount: 0`。
- `Paginable.CreateEmptyPage<T>()` — 空页工厂入口。
- `Paginable.CreateSinglePageSet<T>(IEnumerable<T> fragment, int pageNumber, int pageSize, int totalMemberCount)` — 单页集合工厂（返回具体类型 `PaginableSinglePage<T>`）。
- `Paginable.CreateSinglePageSet<T>(IEnumerable<T> fragment, PageFragmentInfo metadata)` — 用元数据对象的入口。
- `PaginableSinglePage<T>.PageCount` / `.MemberCount` / `.GetPage(int)` — 集合层与页层语义。

## 注意事项 / 陷阱

- **`PaginableSinglePage.PageCount` 恒为 1**：不是页的 `TotalPageCount`；源总页数读 `GetPage(1).TotalPageCount`。
- **`GetPage` 只接受 1**：集合物理上只有一页，`GetPage(2)` 抛 `ArgumentOutOfRangeException`。
- **`EmptyPage` 的 `PageSize` 是 0**；其 `TotalPageCount` 报告为 1。
- **单页集合不重切片**：传入的片段即页内容。
