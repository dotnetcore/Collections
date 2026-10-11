# Sample.CollectionChanged

> **中文** | [English](README.md)

> **自建变更通知 CollectionChanged** — `MultiList<T>` / `MultiDictionary<TKey,TValue>` 的变更事件（6.9.0，`DotNetCore.Collections.Multi`，6.9.0）。

## 本示例演示什么

两个核心类型各自暴露一个 `CollectionChanged` 事件：每次**真正改变**集合的改动（加、删、清空）都会触发一次，
事件参数 `CollectionChangedEventArgs<TKey>` 说明改的是哪个元素/键、移动了多少份、以及改动后的计数。
本示例展示三个侧面：`MultiList<T>` 的加/删/清空通知、处理器读回集合看到的是**新状态**、
`MultiDictionary<TKey,TValue>` 以「键」为单位报告值数量变化。核心记忆点——**它不是框架的 `INotifyCollectionChanged`**。

## 什么时候用

- 需要在集合变化时触发副作用（刷新界面、写审计日志、级联更新缓存）。
- 需要「改动后」而非「改动前」的一致状态——事件在集合到达新状态后才触发。
- 不该用：需要位置型、面向索引的变更通知（多重集没有位置）→ 用有序集合并配合框架通知；
  只是读取数据、无副作用 → 无需订阅（不订阅时事件**零开销**）。

## 如何运行

```bash
dotnet run --project sample/Sample.CollectionChanged
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | MultiList 的加 / 删 / 清空通知 | `CollectionChanged` / `CollectionChangeType.Add` / `Remove` / `Reset` |
| 2 | 事件后读回集合看到新状态 | 处理器内 `TotalCount` 已含本次改动 |
| 3 | MultiDictionary 以键为单位的通知 | `Add(TKey,TValue)` / `Remove(TKey)` / `Remove(TKey,TValue)` / `Clear()` |
| 4 | 与框架通知的区别 | 事件参数是自建的 `CollectionChangedEventArgs<TKey>` |

## 关键 API

- `MultiList<T>.CollectionChanged` / `MultiDictionary<TKey,TValue>.CollectionChanged` — `EventHandler<CollectionChangedEventArgs<TKey>>`。
- `CollectionChangedEventArgs<TKey>.ChangeType` — `CollectionChangeType`（`Add` / `Remove` / `Reset`）。
- `CollectionChangedEventArgs<TKey>.Key` — 元素（bag）或键（multimap）；`Reset` 时无意义。
- `CollectionChangedEventArgs<TKey>.Count` — 本次移动的份数（bag）或值数（multimap）；`Reset` 恒为 0。
- `CollectionChangedEventArgs<TKey>.NewCount` — 改动后的计数（0 表示该条目已消失）；`Reset` 恒为 0。
- `CollectionChangedEventArgs<TKey>.ToString()` — 一行摘要，如 `Add apple x3 -> 3`。

## 注意事项 / 陷阱

- **不是 `INotifyCollectionChanged`**：框架通知是位置导向的，而多重集没有位置、一次调用可移动多份副本，
  位置型动作无法表达；本通知是自建类型，无 `System.ObjectModel` 依赖。
- **只有真正改变集合的改动才触发**：例如空袋再 `Clear()`、`Add` 被去重集合静默拒绝，都不会触发事件。
- **事件在新状态之后触发**：处理器里读回集合能看到刚被告知的改动。
- **不订阅时零开销**：内部只在有处理器时才构造参数对象。
- **非线程安全**：并发修改仍需外部同步（并发类型见 F14）。
