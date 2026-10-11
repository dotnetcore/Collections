# Sample.Snapshot

> **独立快照 Snapshot()** — `MultiList<T>` / `MultiDictionary<TKey,TValue>` 的脱离副本（6.9.0，`DotNetCore.Collections.Multi`，6.9.0）。

## 本示例演示什么

`Snapshot()` 返回一个**独立副本**，专门服务于「边改边枚举」：直接枚举活实例不是修改安全的，而快照已脱离，
枚举时不会观察到之后的改动。本示例展示三个侧面：枚举快照的同时继续修改原集合、
`Snapshot` 与 `Clone` 等价（浅拷贝）、以及 `MultiDictionary` 快照对外层字典与内层值集合都独立。

## 什么时候用

- 需要遍历集合，同时在遍历过程中继续修改它（安全的前提是先取快照）。
- 需要一个「此刻为止」的稳定视图，后续改动不应影响它。
- 不该用：需要连副本本身也不可变 → `ImmutableMultiList<T>` / `ImmutableMultiDictionary<TKey,TValue>`（F13）；
  只是想读一遍活集合且不改 → 直接枚举即可。

## 如何运行

```bash
dotnet run --project sample/Sample.Snapshot
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 边改边枚举：枚举快照同时改原集合 | `MultiList<T>.Snapshot()` |
| 2 | Snapshot 与 Clone 等价（浅拷贝） | `Snapshot()` / `Clone()` |
| 3 | MultiDictionary 快照的独立层次 | `MultiDictionary<TKey,TValue>.Snapshot()` |

## 关键 API

- `MultiList<T>.Snapshot()` — 返回 `MultiList<T>`，等价于 `Clone()`。
- `MultiDictionary<TKey,TValue>.Snapshot()` — 返回 `MultiDictionary<TKey,TValue>`，等价于 `Clone()`。
- 语义与 `Clone()` 一致：**浅拷贝**——元素引用共享，副本计数 / 内层值集合独立。

## 注意事项 / 陷阱

- **浅拷贝**：元素引用是共享的（改元素本身两边都看得到），独立的是计数与内层结构。
- **快照是脱离的**：快照构建后，对原集合的改动不会再反映到快照里。
- **不是并发类型**：`Snapshot()` 解决的是「枚举期间修改」的稳定性，不解决多线程竞态；
  并发场景请用 `ConcurrentMultiList<T>` / `ConcurrentMultiDictionary<TKey,TValue>`（F14）。
- **命名一致**：并发类型的 `Snapshot()` 也返回非并发类型（由 `Clone()` 构建），本示例让核心类型与家族读法统一。
