# Sample.SqlKataAdapter

> **中文** | [English](README.md)

> **SqlKata 适配器入口面：非泛型 Query 与显式类型参数** — `SolidPageExtensions` + `PaginableSqlKataQuery<T>` + `SqlKataPage<T>`（`DotNetCore.Collections.Paginable.SqlKata`，3.2.0）。

## 本示例演示什么

SqlKata 适配器的特殊点是**源类型非泛型**（`SqlKata.Query`），因此元素类型无从推断，`ToPaginable<T>` /
`GetPage<T>` **必须显式写 `<T>`**（生成器 `SourceTypeIsNonGeneric = true`）。这个 sample 是**包支撑的
说明型**示例：引用**真实的 SqlKata 适配器包**，用反射从真实程序集读出入口类型与 `SolidPageExtensions` 的
公开动词（含泛型参数），并讲清显式 `<T>`、依赖随 TFM 漂移（net451→2.2.0 / net8-10→4.0.1 / 其余→3.2.3）。
它不连数据库；最后用核心 `Paginable.CreatePage` 造一个真实页，证明页对象复用核心契约 `IPage<T>`。

## 什么时候用

- `SqlKata.Query` + 执行器取数，想把分页动词接到 SqlKata。
- 跨 TFM 共用同一动词（注意 SqlKata 主版本随 TFM 变，行为差异要按 TFM 测）。
- 不该用：需要泛型源推断 → 自己写 `ToPaginable<T>`；EF Core 栈 → 见 `Sample.EfCoreAdapter`。

## 如何运行

```bash
dotnet run --project sample/Sample.SqlKataAdapter
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 真实适配器程序集与入口类型 | `PaginableSqlKataQuery<T>` / `SqlKataPage<T>` / `SolidPageExtensions`（反射） |
| 2 | 真实公开动词（含泛型参数） | `ToPaginable<T>` / `GetPage<T>` / `GetPageAsync<T>`（反射） |
| 3 | 非泛型源 → 显式 `<T>` | `query.ToPaginable<Product>(20)` |
| 4 | 共享核心契约 | `Paginable.CreatePage` → `IPage<T>` / `IPageMember<T>` |
| 5 | TFM 漂移与为什么不真正执行 | SqlKata 2/3/4 线；`SqlKataHelper` 打库 |

## 关键 API

- `DotNetCore.Collections.Paginable.SolidPageExtensions`（**SqlKata 包**）— 接收者 `SqlKata.Query`（非泛型，参数名 `query`）。
- `ToPaginable<T>` / `GetPage<T>` / `GetPageAsync<T>` — **显式**元素类型。
- `PaginableSqlKataQuery<T>`（分页集合）、`SqlKataPage<T>`（页）。
- 核心契约：`IPage<T>` / `IPageMember<T>`（`Value` / `Offset` / `ItemNumber`）。

## 注意事项 / 陷阱

- **显式泛型参数**：`query.ToPaginable<Product>(20)`，不能写 `query.ToPaginable(20)`。
- **SqlKata 主版本随 TFM 变**：net451→2.2.0；net8/9/10→4.0.1；其余→3.2.3。
- **传递依赖钉死**（6.2.0）是安全修复，不是 API 变化。
- **同名 `SolidPageExtensions`**（6.2.0）；net451 无生成器，靠手写成员对齐。
