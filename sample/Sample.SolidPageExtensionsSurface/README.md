# Sample.SolidPageExtensionsSurface

> **`SolidPageExtensions` 入口面** — 偏移分页的扩展方法入口：`ToPaginable` / `GetPage` / `*Async`（对应 feature F45，核心包 + 9 个适配器各有同名类型）。

## 本示例演示什么

`SolidPageExtensions` 是偏移分页的**扩展方法入口面**。本示例在核心包上走查：`ToPaginable`（把全源变成可翻页集合）与 `GetPage`（从全源直接切一页）对 `IEnumerable` 与 `IQueryable` 两种源的用法；空源返回空页、越界抛 `ArgumentOutOfRangeException`；动词区分（`GetPage` 全源 vs `ToPage` 片段）；以及本仓**头号陷阱**——类型名 `SolidPageExtensions` 在 10 个包同名，同时引用两个包时裸写类型名会 `CS0104`。

## 什么时候用

- 内存 / Dapper 后的 `IEnumerable`，或 `IQueryable` 源，需要一行变成可翻页集合或取页。
- 想通过 `limitedMemberCount` 封顶（F38）。
- 混用两个 ORM 时必须消歧（见 U27）——只写扩展方法语法通常无碍。

## 如何运行

```bash
dotnet run --project sample/Sample.SolidPageExtensionsSurface
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | `ToPaginable` 建集合（IEnumerable） | `SolidPageExtensions.ToPaginable<T>(this IEnumerable<T>, int pageSize, int? = null)` |
| 2 | `GetPage` 从全源切一页 | `SolidPageExtensions.GetPage<T>(this IEnumerable<T>, int, int)` |
| 3 | IQueryable 同一套动词 | `SolidPageExtensions.ToPaginable<T>(this IQueryable<T>, ...)` / `GetPage<T>(this IQueryable<T>, ...)` |
| 4 | 空源不抛 / 越界抛 | `GetPage` + `ArgumentOutOfRangeException` |
| 5 | 同名类型陷阱（CS0104） | `typeof(DotNetCore.Collections.Paginable.SolidPageExtensions)` |
| 6 | `GetPage`（全源）vs `ToPage`（片段） | `FragmentPageExtensions.ToPage<T>(...)` |

## 关键 API

- `SolidPageExtensions.ToPaginable<T>(this IEnumerable<T>, int? limitedMemberCount = null)` / `ToPaginable<T>(this IEnumerable<T>, int pageSize, int? limitedMemberCount = null)` — 返回 `PaginableEnumerable<T>`（有 `PageCount` / `MemberCount` / `PageSize`）。
- `SolidPageExtensions.GetPage<T>(this IEnumerable<T>, int pageNumber, int pageSize)` — 返回 `IPage<T>`。
- `SolidPageExtensions.GetPage<T>(this IQueryable<T>, int pageNumber, int pageSize)` / `ToPaginable<T>(this IQueryable<T>, ...)` — `IQueryable` 版本。
- `FragmentPageExtensions.ToPage<T>(this IEnumerable<T>, int pageNumber, int pageSize, int totalMemberCount)` — 片段入口，**不是** `GetPage`。

## 注意事项 / 陷阱

- **文档与示例提到 `SolidPageExtensions` 必须带包名 / 程序集限定**（全系列硬规则）；10 处同名，两包同引裸写类型名即 `CS0104`。
- **动词含义**：`ToPaginable` = 建集合；`GetPage` = 从**全源**切一页；片段用 `ToPage`。
- **空源**：`GetPage(1)` 返回空页，不抛；**越界**：`ArgumentOutOfRangeException`（F40）。
- **异步语义随包**（F31 / F44）：核心内存 Async 不观察令牌；本示例只演示同步面。
