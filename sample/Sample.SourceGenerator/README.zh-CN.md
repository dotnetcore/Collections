# Sample.SourceGenerator

> **中文** | [English](README.md)

> **SolidPageExtensions 源生成器** — `[SolidPageExtensionsFor]` 属性声明与生成物形状（对应 feature F56，`DotNetCore.Collections.Paginable.SourceGenerators`，6.5.0）。

## 本示例演示什么

6.5.0 起，**六个** ORM 适配器包（Chloe、DosOrm、FreeSql、SqlSugar、NHibernate、SqlKata）的分页扩展成员
由 Roslyn 增量生成器写出，替代逐包手写一遍。这个 sample 讲解生成器的**属性声明形态**（`[SolidPageExtensionsFor]`
的 7 个位置参数 + 关键开关）、**生成物形状**（`ToPaginable` / `ToPaginableAsync` / `GetPage` / `GetPageAsync`
与类型级 XML）、以及 **net451 双轨**与**消费方可见性**。它是**说明型**示例：不引用生成器工程、不要求真跑生成器
（生成器是 Analyzer，不随消费者包发布），只引用分页核心包。

## 什么时候用

- 你是**库维护者**：新增一个 ORM 适配器时，只写属性声明 + Helper / Factory。
- 排查「同一份签名为何六个包一致」：单一事实源防漂移。
- 不该用：**消费者**——你只感知「扩展方法仍在」，看不到生成器类型，也不应引用 SourceGenerators 包。

## 如何运行

```bash
dotnet run --project sample/Sample.SourceGenerator
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 属性声明形态 | `[SolidPageExtensionsFor(ormName, sourceTypeFullName, sourceParamName, pageTypeName, paginableTypeName, factoryTypeName, helperTypeName)]` |
| 2 | 关键属性开关 | `GenericConstraint` / `HasAdditionalQueryFunc` / `HasIncludeNestedMembers` / `HasAsync` / `SourceTypeIsNonGeneric` / `ForwardAsyncCancellationToken` |
| 3 | 生成物形状 | 生成 `ToPaginable` / `ToPaginableAsync` / `GetPage` / `GetPageAsync`；手写特例留在 partial |
| 4 | net451 双轨 | FreeSql / SqlKata / SqlSugar 在 net451 保留 `#if NET451` 手写 |
| 5 | 消费方可见性 | 无消费方 API 变化 ⇒ 无 Breaking |

## 关键 API

- `DotNetCore.Collections.Paginable.SolidPageExtensionsForAttribute` — 生成器 post-init 注入的 **internal** 特性，挂 partial 类。
- `DotNetCore.Collections.Paginable.SourceGenerators.SolidPageExtensionsGenerator` — `IIncrementalGenerator`。
- 生成器工程以 `OutputItemType=Analyzer`、`ReferenceOutputAssembly=false` 接入适配器包，**不增加包依赖**。

## 注意事项 / 陷阱

- **消费者看不到生成器类型**；不要引用 SourceGenerators 包当运行库。
- **net451 双轨**：改生成器属性时要同步 `#if NET451` 手写面，否则只测一个 TFM 会漏。
- **手写特例仍存在**（NHibernate `ISession` 重载、SqlKata `GetPageAsync`），不要假设 100% 生成。
- **EF6 / EF Core / FreeSql.DbContext 刻意未迁移**——它们委托 queryable 扩展，本来不是重复形状。
