# Sample.AsyncPaging

> **中文** | [English](README.md)

> **端到端异步分页（内存形态）** — 内存源的 `*Async` 是同步完成的「异步形状」，与真 I/O 异步语义不同（`DotNetCore.Collections.Paginable`，6.0.0 / 6.2.0）。

## 本示例演示什么

Paginable 的异步面分两档：真端到端异步（EF Core / FreeSql / SqlSugar 的 `GetPageAsync` / `ToPaginableAsync`，走提供方原生 `CountAsync` + `ToListAsync`）与「异步形状」（内存 `IEnumerable` 的 `*Async` 与 `Paginable.CreatePageAsync`，由 `Task.FromResult` 包装、**同步完成**）。本示例只演示内存形态：`await` 内存列表的分页调用、`Paginable.CreatePageAsync` 重建片段页、以及「取消令牌不被观察」「校验同步抛出」两个关键语义。全程只用内存源，不引入第三方包。

## 什么时候用

- 异步调用链里混有「片段路径」时，用同一 `Task<IPage<T>>` 签名，不必改签名。
- 与 `GetPageAsync` 统一的 `await` 调用风格，减少 if/else 分叉。
- 只是同步片段建页：直接用 `CreatePage` 更诚实。
- 需要真取消 / 线程让渡：用提供方异步 API（EF Core / FreeSql / SqlSugar）。

## 如何运行

```bash
dotnet run --project sample/Sample.AsyncPaging
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 内存源异步形状（同步完成） | `DotNetCore.Collections.Paginable.SolidPageExtensions.ToPaginableAsync` / `GetPageAsync` |
| 2 | 片段异步入口重建页 | `Paginable.CreatePageAsync(fragment, pageNumber, pageSize, totalMemberCount)` |
| 3 | 已取消令牌不阻止成功 | `CreatePageAsync(..., CancellationToken)` |
| 4 | 校验从调用本身同步抛出 | `ArgumentOutOfRangeException` |

## 关键 API

- `Paginable.CreatePageAsync<T>(fragment, pageNumber, pageSize, totalMemberCount, cancellationToken = default)` — 片段建页 async 形状，**同步完成**。
- `SolidPageExtensions.ToPaginableAsync<T>(enumerable, pageSize, limitedMemberCount = null, cancellationToken = default)` — 内存集合异步形状。
- `SolidPageExtensions.GetPageAsync<T>(enumerable, pageNumber, pageSize, cancellationToken = default)` — 内存取页异步形状。
- `PaginableEnumerable<T>.PageSize` / `MemberCount` / `PageCount` — 集合层导航。

## 注意事项 / 陷阱

- **看包与源类型判断真异步，别只看方法名有没有 `Async`**：内存 `IEnumerable` 的 `*Async` 同步完成。
- **片段 `CreatePageAsync` 不观察 `CancellationToken`**：无 I/O 可取消，令牌仅为签名对称。
- **校验同步抛**：非法参数在 `await` 之前就抛 `ArgumentException` / `ArgumentOutOfRangeException`，不要用 `catch (AggregateException)` 去接。
- **`IPage<T>` 枚举仍是同步的**：库不提供 `IAsyncEnumerable`；页对象 `IPage<T> : IEnumerable<IPageMember<T>>`。
- **真 I/O 异步**要用提供方扩展（EF Core `SolidPageExtensions.GetPageAsync` 等），见 6.0.0。
