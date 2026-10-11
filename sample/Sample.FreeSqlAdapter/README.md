# Sample.FreeSqlAdapter

> **FreeSql 适配器入口面** — `SolidPageExtensions` + `PaginableFreeSqlQuery<T>` + `FreeSqlPage<T>`（对应 feature F52，`DotNetCore.Collections.Paginable.FreeSql`，2.x）。

## 本示例演示什么

FreeSql 适配器把分页动词接到 `FreeSql.ISelect<T>`。这个 sample 是**包支撑的说明型**示例：它引用**真实的
FreeSql 适配器包**（本地缓存可还原、构建通过），用反射从真实程序集读出入口类型与 `SolidPageExtensions`
的公开动词签名，并打印统一动词对照与 FreeSql 独有的 `includeNestedMembers` 开关。它**不连数据库**——
`GetPage` 会经 `FreeSqlHelper` 真正打库，控制台示例不便连接真实数据源，故只演示入口面。示例最后用核心的
`Paginable.CreatePage` 造一个真实页，证明「无论哪个适配器，页对象都是核心契约 `IPage<T>`」。

## 什么时候用

- 确认 FreeSql 应用的入口类型名与动词（`ToPaginable` / `GetPage` / `*Async`）。
- 需要「是否包含嵌套成员」时用 `includeNestedMembers` 开关。
- DbContext 表面 → 用 `DotNetCore.Collections.Paginable.FreeSql.DbContext` 包（**仅** `SolidPageExtensions`）。
- 不该用：EF Core 栈 → 见 `Sample.EfCoreAdapter`。

## 如何运行

```bash
dotnet run --project sample/Sample.FreeSqlAdapter
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 真实适配器程序集与入口类型 | `PaginableFreeSqlQuery<T>` / `FreeSqlPage<T>` / `SolidPageExtensions`（反射） |
| 2 | 真实公开动词签名 | `SolidPageExtensions` 的 `ToPaginable` / `GetPage` / `*Async`（反射） |
| 3 | 统一动词 + 独有开关 | `includeNestedMembers`；`where T : class` |
| 4 | 共享核心契约 | `Paginable.CreatePage` → `IPage<T>` / `IPageMember<T>` |
| 5 | 为什么不真正执行 | `FreeSqlHelper.Count` 打库 |

## 关键 API

- `DotNetCore.Collections.Paginable.SolidPageExtensions`（**FreeSql 包**）— 接收者 `FreeSql.ISelect<T>`（参数名 `select`）。
- `PaginableFreeSqlQuery<T>`（分页集合，`where T : class`）、`FreeSqlPage<T>`（页）。
- FreeSql 独有：`includeNestedMembers`（是否包含嵌套成员，语义由 FreeSql 决定）。
- 核心契约：`IPage<T>` / `IPageMember<T>`（`Value` / `Offset` / `ItemNumber`）。

## 注意事项 / 陷阱

- **`SolidPageExtensions` 同名**：适配器与核心同在 `DotNetCore.Collections.Paginable` 命名空间，直接写类型名会 CS0104（U27）。
- **DbContext 包没有** `PaginableFreeSqlQuery` 的自有工厂；用具体类型时仍引用 FreeSql 包。
- **`includeNestedMembers` 语义由 FreeSql 决定**（导航加载），影响结果形状与成本。
- **net451 双轨**：net451 保留 `#if NET451` 手写成员（F56）；越界抛 `ArgumentOutOfRangeException`。
