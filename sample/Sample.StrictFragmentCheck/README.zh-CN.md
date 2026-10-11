# Sample.StrictFragmentCheck

> **中文** | [English](README.md)

> **严格片段校验选项** — `PageCreationOptions.Lenient` / `Strict` 决定片段短于元数据时是容忍还是报错（对应 feature F33，`DotNetCore.Collections.Paginable`，6.2.0）。

## 本示例演示什么

`PageCreationOptions` 只回答一个问题：当片段**比元数据说的少**时，建页该容忍还是报错？默认 `Lenient`（6.1 行为）容忍——覆盖 `COUNT(*)` 与 fetch 之间上游行被并发删除的场景，且 `CurrentPageSize` 仍报元数据值。`Strict` 把同样现象视为 bug（陈旧 total、切错查询、静默少行），抛 `ArgumentException` 点名 `fragment`。长片段（超页容量 / 超期望）在两种模式下都抛。本示例逐一演示这两条分派，并覆盖 `ToPage` / `CreateSinglePageSet`。

## 什么时候用

- 用户列表、可能有并发删除 → `Lenient`（默认，无需改代码）。
- 导出对账、金额核对、集成测试夹具 → `Strict`（钉死上游 COUNT 与查询一致性）。
- 缓存页可能过期一两条 → `Lenient` 或先失效缓存。

## 如何运行

```bash
dotnet run --project sample/Sample.StrictFragmentCheck
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 默认 Lenient 容忍短片段 | `Paginable.CreatePage(..., totalMemberCount)` |
| 2 | Strict 拒绝短片段 | `Paginable.CreatePage(..., PageCreationOptions.Strict)` |
| 3 | 扩展方法 / 单页集合同样支持 | `ToPage` / `CreateSinglePageSet` + options |
| 4 | 长片段两种模式都抛 | `ArgumentException`（`fragment`） |

## 关键 API

- `PageCreationOptions.Lenient` / `PageCreationOptions.Strict` — 两个单例（无公共构造器，防第三态）。
- `PageCreationOptions.IsStrict` — 是否开启短片段校验。
- `Paginable.CreatePage<T>(fragment, pageNumber, pageSize, totalMemberCount, options)` — 带选项的片段建页。
- `FragmentPageExtensions.ToPage<T>(fragment, pageNumber, pageSize, totalMemberCount, options)`。

## 注意事项 / 陷阱

- **默认是 `Lenient`**：升级 6.1 代码不必改。
- **`Strict` 只加严「短片段」**：长片段本来就抛；异常是 `ArgumentException`（点名 `fragment`）。
- **短片段下 `CurrentPageSize` 仍报元数据值**：不要用它推断实际枚举条数。
- **`options` 无公共构造器**：只用 `Lenient` / `Strict` 两个单例；传 `null` 抛 `ArgumentNullException`。
- **选项是每次调用的参数**，不是 `PaginableSettings` 的一部分。
