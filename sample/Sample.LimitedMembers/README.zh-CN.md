# Sample.LimitedMembers

> **中文** | [English](README.md)

> **成员上限与开区间** — 全局硬上限 `MaxMemberItems` 与调用级封顶 `limitedMemberCount`（对应 feature F38，`DotNetCore.Collections.Paginable`，6.0.0）。

## 本示例演示什么

Paginable 有两层「限制有多少成员进入分页视野」的机制：全局硬上限 `MaxMemberItems`（默认 `10_000_000`，**恰好等于上限允许**——6.0.0 把闭区间改成开区间）与每次调用可选的 `limitedMemberCount`（封顶展示）。封顶计算集中在 `PaginableCalc`（在 `Internal` 命名空间但 `public static`，可单测）。本示例展示 `PaginableCalc` 的 `min` 语义与向上取整、`ToPaginable(..., limitedMemberCount)` 截断分页视野、limited 高于真实 COUNT 时的回退，以及 `MaxMemberItems` 开区间边界。

## 什么时候用

- 后台列表限制最多 N 条可翻 → `ToPaginable(..., limitedMemberCount: 5000)`。
- 全局防护非法巨量 total → 收紧 `MaxMemberItems`。
- 数据量本身很小 → 不传 limited。

## 如何运行

```bash
dotnet run --project sample/Sample.LimitedMembers
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 封顶计算单点 | `PaginableCalc.GetRealMemberCount` / `GetRealPageCount` |
| 2 | limited 截断分页视野 | `ToPaginable(pageSize, limitedMemberCount)` / `MemberCount` / `PageCount` |
| 3 | limited 高于真实 COUNT | `MemberCount` / `PageCount`（取真实值） |
| 4 | 全局硬上限开区间 | `PageFragmentInfo` 构造 / `LimitedMembersTypes` |

## 关键 API

- `PaginableCalc.GetRealMemberCount(int? limitedMemberCount, int count)` — `min` 语义，`null` 不限制。
- `PaginableCalc.GetRealPageCount(int realMemberCount, int pageSize)` — 向上取整。
- `SolidPageExtensions.ToPaginable<T>(enumerable, pageSize, limitedMemberCount)` — 调用级封顶。
- `LimitedMembersTypes.Unlimited`（0） / `Customize`（1）。

## 注意事项 / 陷阱

- **开区间**：`MaxMemberItems` **本身允许**；`> MaxMemberItems` 才抛 `ArgumentOutOfRangeException`。
- **limited 是「展示封顶」不是过滤谓词**：它截断的是分页视野，不是 `WHERE`；`GetPage(3)` 仍是封顶后集合的第 3 页。
- **`limitedMemberCount <= 0` 视为无效**（`IsValid` 只判非 `null`，工厂路径常用 `> 0` 判断）。
- **`PaginableCalc` 在 `DotNetCore.Collections.Paginable.Internal`**，但是 `public static`，可单测。
