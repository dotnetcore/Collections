# Sample.PaginableCalc

> **中文** | [English](README.md)

> **分页计算工具 `PaginableCalc`** — 成员数上限裁剪与页数换算的单一事实源（`DotNetCore.Collections.Paginable`，6.0.0）。

## 本示例演示什么

`PaginableCalc` 是分页计算的**单一事实源**，由核心工厂与所有 ORM 适配器 coll-factory 共享，避免各处手算漂移。它位于 `DotNetCore.Collections.Paginable.Internal` 命名空间，但类型是 `public static class`，可被适配器与测试直接调用。本示例走查两个成员：`GetRealMemberCount`（有效上限且更小时取 min，`null` 表示不限）与 `GetRealPageCount`（`ceil(members / pageSize)`，末页不满也算一页），并与 `ToPaginable(limitedMemberCount: ...)` 的结果互相印证。

## 什么时候用

- 自定义执行器 / 适配器开发：直接调用同一套算法算成员数与页数。
- 想与库内部口径保持一致（避免不同 ORM 末页语义不一致）。
- 不该用：替代业务分页逻辑——它只覆盖成员数 / 页数换算。

## 如何运行

```bash
dotnet run --project sample/Sample.PaginableCalc
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 成员数上限裁剪（`null` = 不限） | `PaginableCalc.GetRealMemberCount(int?, int)` |
| 2 | 页数换算（向上取整） | `PaginableCalc.GetRealPageCount(int, int)` |
| 3 | 与 `ToPaginable` 的 `limitedMemberCount` 对照 | `SolidPageExtensions.ToPaginable<T>(..., int? limitedMemberCount)` |

## 关键 API

- `PaginableCalc.GetRealMemberCount(int? limitedMemberCount, int count)` — 有效上限且小于源计数时取上限，否则取源计数。
- `PaginableCalc.GetRealPageCount(int realMemberCount, int pageSize)` — `ceil(members / pageSize)`，末页不满也算一页。
- `PaginableSetBase<T>.LimitedMemberCount` / `.MemberCount` / `.PageCount` — 与 `PaginableCalc` 复算结果一致。

## 注意事项 / 陷阱

- **在 `Internal` 命名空间但是 public**：可调用，但视为实现工具，勿当业务 API。
- **`GetRealPageCount` 不校验 `pageSize > 0`**：由工厂校验配套，调用方保证。
- **与 6.0.0 `MaxMemberItems` 配套**：超过上限的结果集被拒绝或裁剪，裁剪走 `GetRealMemberCount`。
- 各适配器手算页数会导致末页语义不一致——统一走 `PaginableCalc`。
