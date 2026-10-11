# Sample.AsyncFragmentEntry

> **异步片段入口** — `Paginable.CreatePageAsync` 是片段建页的 async 形状对偶，**同步完成**（对应 feature F35，`DotNetCore.Collections.Paginable`，6.2.0）。

## 本示例演示什么

`Paginable.CreatePageAsync` 的四个重载与 `CreatePage` 一一对应（三元组 / `PageFragmentInfo`，均可带 `PageCreationOptions`），再加一个 `CancellationToken`。它**同步完成**：片段已在内存，没有可 await 的 I/O，用 `Task.FromResult` 包装，返回的 Task 已经完成；参数校验从调用本身同步抛出（不是 faulted task），`cancellationToken` 仅为签名对称、不被观察。本示例把它藏进一个 `Task<IPage<T>>` 服务方法，展示与真异步同形、Strict 校验、取消语义误解、同步抛错四个侧面。

## 什么时候用

- `async Task<IPage<T>>` 服务方法内有时走数据库、有时走缓存页 → 两边都 `await`。
- 与 `GetPageAsync` 统一的 `await` 调用风格，减少 if/else 分叉。
- 只是同步片段建页 → 直接 `CreatePage`，更诚实。
- 期望取消中断建页 → 无 I/O；改用提供方异步 API。

## 如何运行

```bash
dotnet run --project sample/Sample.AsyncFragmentEntry
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 与真异步同一签名 | `Paginable.CreatePageAsync(fragment, pageNumber, pageSize, totalMemberCount, ct)` |
| 2 | 严格校验重载 + Task 已完成 | `CreatePageAsync(..., options)` / `Task.IsCompleted` |
| 3 | 已取消令牌不阻止成功 | `CreatePageAsync(..., CancellationToken)` |
| 4 | 校验从调用本身同步抛出 | `ArgumentOutOfRangeException` |

## 关键 API

- `Paginable.CreatePageAsync<T>(fragment, pageNumber, pageSize, totalMemberCount, cancellationToken = default)`。
- `Paginable.CreatePageAsync<T>(fragment, pageNumber, pageSize, totalMemberCount, options, cancellationToken = default)`。
- `Paginable.CreatePageAsync<T>(fragment, PageFragmentInfo[, PageCreationOptions], cancellationToken = default)`。
- `IPage<T>.CurrentPageNumber` / `CurrentPageSize` / `this[int].ItemNumber`。

## 注意事项 / 陷阱

- **同步抛错**：不要写 `catch (AggregateException)` 去包校验失败；`ArgumentException` / `ArgumentOutOfRangeException` 从调用本身抛出。
- **令牌不观察**：`Async` 后缀是形状，不是取消语义。
- **无 `CreateSinglePageSetAsync`**：单页集合用同步工厂（F34）。
- **重载集合 = `CreatePage` + `CancellationToken`**：含 options 变体，行为与同步 `CreatePage` 完全一致。
