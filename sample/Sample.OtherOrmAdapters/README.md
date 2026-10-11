# Sample.OtherOrmAdapters

> **Chloe / DosOrm / NHibernate 三包入口名对照** — 类型名逐包不同、独有重载与约束（对应 feature F55，`DotNetCore.Collections.Paginable.Chloe` / `.DosOrm` / `.NHibernate`，2.x 线）。

## 本示例演示什么

这三个适配器的公开面**命名不完全对称**：Chloe 是 `PaginableChloeQuery<T>` / `ChloePage<T>`，DosOrm 是
`PaginableDosQuery<T>` / `DosPage<T>`，而 NHibernate 的页类型是 **`NhCorePage<T>`**（不是 `NHibernatePage<T>`）。
这个 sample 用一张对照表打印三包的入口名、源类型与独有重载，并讲清两处易错点：`additionalQueryFunc`
（Chloe / Dos 的查询变换钩子）与 DosOrm 的 `where T : Entity` 约束。它是**说明型**示例：只引用分页核心包，
不引入任何 ORM 包，因此没有还原 / 连库风险。

## 什么时候用

- 选型：确认 Chloe / Dos.ORM / NHibernate 各自的包与入口类型名。
- 需要「取页前追加排序」→ `additionalQueryFunc`（Chloe / Dos）。
- NHibernate 想直接从 `ISession` 建集合 → `session.ToPaginable<T>(...)`。
- 不该用：EF Core → 见 `Sample.EfCoreAdapter`。

## 如何运行

```bash
dotnet run --project sample/Sample.OtherOrmAdapters
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 三包入口名对照表 | `PaginableChloeQuery<T>` / `PaginableDosQuery<T>` / `PaginableNhCoreQuery<T>`（字符串对照） |
| 2 | 独有重载与约束 | `additionalQueryFunc`；`ISession` 重载；`where T : Entity` |
| 3 | 命名规律与例外 | NHibernate `NhCorePage<T>`（非 `NHibernatePage<T>`） |
| 4 | 共享核心契约 | `Paginable.CreatePage` → `IPage<T>` / `IPageMember<T>` |
| 5 | 同名消歧与 TFM | 10 处同名 `SolidPageExtensions`；三包无 net451 |

## 关键 API

- `DotNetCore.Collections.Paginable.SolidPageExtensions`（**各包**）— 接收者分别 `Chloe.IQuery<T>` / `Dos.ORM.FromSection<T>` / `IQueryOver<T>`。
- 入口类型名（**不可跨包类推**）：`PaginableChloeQuery<T>` / `ChloePage<T>`、`PaginableDosQuery<T>` / `DosPage<T>`、
  `PaginableNhCoreQuery<T>` / `NhCorePage<T>`。
- `additionalQueryFunc`：`Func<IQuery<T>, IQuery<T>>`（Dos 为 `FromSection<T>`），取页前追加查询变换。

## 注意事项 / 陷阱

- **类型名逐包不同**，不可写 `PaginableEfCoreQuery` 之类类比。
- **`NhCorePage<T>` 特殊命名**：不是 `NHibernatePage<T>`。
- **Dos 的 `Entity` 约束**：不能把任意 DTO 直接 `ToPaginable`。
- **`additionalQueryFunc` 可空**，不要在其中做副作用；三包 TFM **无 net451**（F57）。
