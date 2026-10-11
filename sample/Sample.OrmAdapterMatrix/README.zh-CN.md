# Sample.OrmAdapterMatrix

> **中文** | [English](README.md)

> **ORM 适配器同构矩阵** — 9 个 ORM 集成包的公开面与入口名对照（对应 feature F50，`DotNetCore.Collections.Paginable.*`，2.x–6.6 逐包引入）。

## 本示例演示什么

九个适配器包把**同一套分页动词**接到不同 ORM，但**入口类型名逐包不同**。这个 sample 用一张对照表把
「包 → 分页集合类型 → 页类型 → 备注」逐行打印出来，并单独讲清三处最容易被「类推」坑到的例外：
NHibernate 的 `NhCorePage<T>`、EF/EF Core/FreeSql.DbContext 只有 `SolidPageExtensions`、SqlKata 的非泛型 `Query`。
它是**说明型**示例：只引用分页核心包，不引入任何 ORM 包，因此不会有还原 / 连接数据库的风险。

## 什么时候用

- 选型：确认你的 ORM 对应哪个 NuGet 包、入口类型叫什么。
- 写文档 / 测试时核对入口名，避免杜撰 `PaginableEfCoreQuery<T>` 之类不存在的类型。
- 审计：对照异步、取消、TFM 的逐包差异（见 F44 / F57）。
- 不该用：想直接跑一次分页 → 见 `Sample.EfCoreAdapter`（真异步）或 `Sample.SerializationEntry`（片段重建）。

## 如何运行

```bash
dotnet run --project sample/Sample.OrmAdapterMatrix
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 九包入口名对照矩阵 | 各包 `PaginableXxxQuery<T>` / `XxxPage<T>`（字符串对照） |
| 2 | 统一动词 | `ToPaginable` / `GetPage` / `*Async`（接收者类型逐包不同） |
| 3 | 命名规律与三处例外 | NHibernate `NhCorePage<T>`、EF 无独立 Query/Page、SqlKata 显式 `<T>` |
| 4 | 同名消歧 | 10 处同名 `SolidPageExtensions`，需全限定名或别名 |

## 关键 API

- 核心契约（所有适配器共享）：`IPage`、`IPage<T>`、`IPaginable`、`IPaginable<T>`。
- 逐包入口类型名（**不可跨包类推**）：`PaginableFreeSqlQuery<T>` / `FreeSqlPage<T>`、`PaginableSqlSugarQuery<T>` /
  `SqlSugarPage<T>`、`PaginableSqlKataQuery<T>` / `SqlKataPage<T>`、`PaginableChloeQuery<T>` / `ChloePage<T>`、
  `PaginableDosQuery<T>` / `DosPage<T>`、`PaginableNhCoreQuery<T>` / `NhCorePage<T>`。
- EF / EF Core / FreeSql.DbContext：**只有** `SolidPageExtensions`；EF Core 另有 `KeysetPageAsyncExtensions`。

## 注意事项 / 陷阱

- **入口名逐包不同**，查矩阵，不要按命名规律硬造类型。
- **`NhCorePage<T>`** 不是 `NHibernatePage<T>`；查询集合是 `PaginableNhCoreQuery<T>`。
- **`SolidPageExtensions` 10 处同名**：凡提及必带包名 / 命名空间，混用两包会 CS0104（U27）。
- **SqlKata 源类型非泛型**：`ToPaginable<T>` / `GetPage<T>` 必须显式写元素类型。
