# Sample.SqlSugarAdapter

> **SqlSugar 适配器入口面与取消令牌边界** — `SolidPageExtensions` + `PaginableSqlSugarQuery<T>` + `SqlSugarPage<T>`（对应 feature F53，`DotNetCore.Collections.Paginable.SqlSugar`，2.x）。

## 本示例演示什么

SqlSugar 适配器把分页动词接到 `SqlSugar.ISugarQueryable<T>`。这个 sample 是**包支撑的说明型**示例：引用
**真实的 SqlSugar 适配器包**，用反射从真实程序集读出入口类型与 `SolidPageExtensions` 的公开动词签名，并
重点讲清 **6.6.0 F6-38** 的取消令牌边界——已取消令牌在**第一次数据库往返前**抛 `OperationCanceledException`，
而中段取消仍取决于 SqlSugar 版本（其 `CountAsync` / `ToPageListAsync` 无令牌重载）。示例不连数据库；
最后用核心 `Paginable.CreatePage` 造一个真实页，证明页对象复用核心契约 `IPage<T>`。

## 什么时候用

- 确认 SqlSugar 应用的入口类型名与动词。
- Web 场景需要请求取消在首轮生效（6.6+）。
- 不该用：需要中段取消保证 → 取决于 SqlSugar 版本 / 提供方；EF Core 栈 → 见 `Sample.EfCoreAdapter`。

## 如何运行

```bash
dotnet run --project sample/Sample.SqlSugarAdapter
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 真实适配器程序集与入口类型 | `PaginableSqlSugarQuery<T>` / `SqlSugarPage<T>` / `SolidPageExtensions`（反射） |
| 2 | 真实公开动词签名 | `ToPaginable` / `GetPage` / `*Async`（反射） |
| 3 | 取消令牌边界（F6-38） | `GetPageAsync(1, 20, ct)` 在首轮往返前抛 `OperationCanceledException` |
| 4 | 共享核心契约 | `Paginable.CreatePage` → `IPage<T>` / `IPageMember<T>` |
| 5 | TFM 与为什么不真正执行 | 无 ns2.0；`SqlSugarHelper` 打库 |

## 关键 API

- `DotNetCore.Collections.Paginable.SolidPageExtensions`（**SqlSugar 包**）— 接收者 `SqlSugar.ISugarQueryable<T>`（参数名 `query`）。
- `PaginableSqlSugarQuery<T>`（分页集合）、`SqlSugarPage<T>`（页）。
- 动词：`ToPaginable` / `ToPaginableAsync` / `GetPage` / `GetPageAsync`。
- 核心契约：`IPage<T>` / `IPageMember<T>`（`Value` / `Offset` / `ItemNumber`）。

## 注意事项 / 陷阱

- **升级 6.5→6.6 可能新抛 `OperationCanceledException`**（F6-38 修复，不是回归）。
- **中段取消不承诺**：SqlSugar 5.1.3 的 `CountAsync` / `ToPageListAsync` 无令牌重载。
- **无 netstandard2.0**；net4* 用 `sqlSugar`，其它用 `sqlSugarCore`。
- **同名 `SolidPageExtensions`**（U27）；越界抛 `ArgumentOutOfRangeException`。
