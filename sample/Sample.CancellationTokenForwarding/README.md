# Sample.CancellationTokenForwarding

> **取消令牌转发** — `CancellationToken` 的承诺逐包不同：下传 / 库边界 / 不观察三类语义（对应 feature F44，Paginable 核心 / 适配器，6.0.0 引入、6.5.0 六适配器、6.6.0 SqlSugar 修复）。

## 本示例演示什么

「Async」不等于「可取消」——取消令牌是否被观察**逐包不同**。本示例聚焦核心包能实测的一类：**内存路径不观察令牌**。把**已取消**的令牌传给 `Paginable.CreatePageAsync`、核心 `GetPageAsync`、`ToPaginableAsync`，调用仍成功完成（`RanToCompletion`），因为片段已在内存、无 I/O 可等。示例同时用注释讲清另外两类语义（EF Core / FreeSql 下传；SqlSugar 6.6 库边界尊重），并演示「想在内存里可取消就自己在循环里 `ThrowIfCancellationRequested`」。

## 什么时候用

- ASP.NET `HttpContext.RequestAborted` + EF Core / FreeSql：真取消，可中止分页 I/O。
- SqlSugar 6.6+：已取消令牌在首轮数据库往返前抛 `OperationCanceledException`。
- 纯内存 `CreatePageAsync`：**不取消**——不要指望它响应令牌。
- 需要可移植取消语义：自己在循环里观察令牌。

## 如何运行

```bash
dotnet run --project sample/Sample.CancellationTokenForwarding
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 片段入口不观察令牌（已取消仍成功） | `Paginable.CreatePageAsync<T>(...)` |
| 2 | 核心 `IEnumerable` Async 不观察令牌 | `SolidPageExtensions.GetPageAsync<T>` / `ToPaginableAsync<T>` |
| 3 | 三类语义对照（下传 / 库边界 / 不观察） | —（注释说明） |
| 4 | 可移植取消：循环内检查令牌 | `CancellationToken.ThrowIfCancellationRequested()` |

## 关键 API

- `Paginable.CreatePageAsync<T>(IEnumerable<T>, PageFragmentInfo, CancellationToken = default)` — 参数注明 `unused`；片段已在内存，任务同步完成，**不观察**令牌。
- `SolidPageExtensions.GetPageAsync<T>(this IEnumerable<T>, int, int, CancellationToken = default)` — 核心内存形状，**不观察**令牌。
- `SolidPageExtensions.ToPaginableAsync<T>(this IEnumerable<T>, int, int? = null, CancellationToken = default)` — 同上。
- `CancellationToken.ThrowIfCancellationRequested()` — 自己实现可取消语义的唯一正确方式（内存路径）。

## 注意事项 / 陷阱

- **同步 `GetPage` 没有令牌参数**：不要在同步面找取消。
- **内存 Async 只是签名对称**，不是「可取消 I/O」。
- **异常类型**：取消是 `OperationCanceledException`（含 `TaskCanceledException` 子类）；越界仍是 `ArgumentOutOfRangeException`（F40）——勿混 catch。
- **不要假设所有适配器中段可取消**：查 F44 表 + F50 矩阵，逐包不同；SqlSugar 6.5→6.6 后取消可能开始抛出，是修复不是回归。
