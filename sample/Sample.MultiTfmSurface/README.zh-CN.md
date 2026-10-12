# Sample.MultiTfmSurface

> **中文** | [English](README.md)

> **多 TFM 暴露面与逐包子集差异** — 11 TFM 矩阵、低代际差异与逐包差异（`DotNetCore.Collections.Multi` 等，6.0.0 扩到 11 TFM）。

## 本示例演示什么

核心与部分适配器目标 `net451;net461;net47;net48;netstandard2.0;netstandard2.1;net6.0–net10.0`（**11** 个），
但**各包 TFM 子集不同**——这是 API 手册「目标框架」列必填的原因。本 sample 是一个**多目标工程**
（`net8.0;net10.0`）：同一份源码在两个分支都编译、都运行，程序打印当前运行时、命中的条件编译分支、
一段真实跑通的 Multi API（证明跨 TFM 行为一致），以及 11 TFM 矩阵与逐包子集差异（EF Core 仅 net6+、
Chloe/Dos/NHibernate 无 net451、SqlSugar 无 ns2.0 等）。

## 什么时候用

- 选包前核对 TFM：例如 net48 旧系统核心 OK，但 EF Core 适配器不可用。
- 写文档 / API 参考时逐类型标注目标框架。
- 库维护者保证低代际可编译、可测（`Multi.NetFxTests` net462 侧车）。
- 不该用：把「全包 11 TFM」当成默认假设。

## 如何运行

```bash
dotnet run --project sample/Sample.MultiTfmSurface            # net8.0
dotnet run --project sample/Sample.MultiTfmSurface -f net10.0 # net10.0
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 当前运行时与编译分支 | `RuntimeInformation.FrameworkDescription` / `Environment.Version` / `#if NET10_0_OR_GREATER` |
| 2 | 跨 TFM 行为一致的 Multi API | `MultiList<T>.Add` / `TotalCount` / `DistinctCount` / `CountOf` |
| 3 | 11 TFM 矩阵 | 字符串列出 11 个目标 |
| 4 | 逐包 TFM 子集差异 | EF Core 仅 net6+；Chloe/Dos/NHibernate 无 net451；SqlSugar 无 ns2.0 |
| 5 | 类型级 TFM 差异 | `SpanBag` 仅 `Span<T>` 在框的目标 |

## 关键 API

- `RuntimeInformation.FrameworkDescription` / `Environment.Version` — 运行期目标框架信息。
- `#if NET10_0_OR_GREATER` / `#if NET8_0_OR_GREATER` — 条件编译分支。
- `MultiList<T>`（`Add` / `TotalCount` / `DistinctCount` / `CountOf`）— 两个 TFM 上行为一致的示例 API。

## 注意事项 / 陷阱

- **不要假设全包 11 TFM**：EF Core 仅 net6+；Chloe / Dos / NHibernate 无 net451；SqlSugar 无 netstandard2.0。
- **net451 缺接口**会导致「同一代码在低代际编不过」；可空靠 internal 垫片（6.3.0）。
- **`SpanBag` 等类型仅 `Span<T>` 在框的目标**存在——勿假设全包同矩阵。
- **锁定依赖** `packages.lock.json`（6.0.0 起确定性构建）。
