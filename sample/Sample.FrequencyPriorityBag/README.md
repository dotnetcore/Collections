# Sample.FrequencyPriorityBag

> **频次优先袋** — `FrequencyPriorityBag<T>`：最高频元素 O(log n) 可取（对应 feature F23，`DotNetCore.Collections.Multi`，6.4.0）。

## 本示例演示什么

频次优先袋把「谁出现最多」做成一等查询：`PeekMost` 不删地看最高频元素，`PopMost` 删一份并返回，
连弹即得降频序列——这正是 Top-K 查询的天然形状。示例展示三个侧面：Top-K 连弹、平局策略、
以及枚举 / `null` / 空袋行为。它用「每次更新 O(1) 只动频次索引并标堆脏」换「每次优先查询才重建堆」，
因此更新多、查询成簇时最划算。

## 什么时候用

- 热词 / Top-K 高频查询：连弹 `PopMost`。
- 更新频繁、优先查询成簇：懒物化堆。
- 不该用：只查一次最频（排一次 `MultiList<T>` 更便宜）；要有序枚举（用 `OrderedMultiList<T>`）。

## 如何运行

```bash
dotnet run --project sample/Sample.FrequencyPriorityBag
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | Top-K 连弹 | `AddRange` / `PeekMost` / `PopMost` / `TotalCount` / `DistinctCount` |
| 2 | 平局策略（FIFO 味） | `PopMost` 同频时更早达到当前计数者先 |
| 3 | 枚举副本展开、无序 | 枚举 / `TotalCount` |
| 4 | `null` 为一等元素 | `Add(null)` / `CountOf(null)` / `PeekMost` |
| 5 | 空袋与 Try 形 | `TryPeekMost` / `TryPopMost` / `PeekMost` 抛 `InvalidOperationException` |

## 关键 API

- `FrequencyPriorityBag<T>.PeekMost()` — 返回最高频元素，不删；空袋抛 `InvalidOperationException`。
- `FrequencyPriorityBag<T>.PopMost()` — 删一份并返回最高频元素；连弹即降频序列（Top-K）。
- `FrequencyPriorityBag<T>.TryPeekMost(out T)` / `TryPopMost(out T)` — 不抛版本，空袋返回 `false`。
- `FrequencyPriorityBag<T>.Add(T)` / `Add(T,int)` / `AddRange` / `CountOf` / `Remove(T)` / `RemoveAllCopies(T)` / `EntrySet()`。
- `FrequencyPriorityBag<T>.TotalCount` / `DistinctCount` / `IsEmpty`。

## 注意事项 / 陷阱

- **枚举无序**：枚举只是副本展开，既不按频次也不按插入序；要频次序用 `PopMost`。
- **平局是 FIFO 味**：同计数时更早达到当前计数的元素先弹出（计数变更打递增戳，堆在戳上取小者），不是「最近变更优先」。
- **query-once 不划算**：首次优先查询要 O(n) 重建堆；只查一次最频不如排一次 `MultiList<T>`。
- **空袋 `PeekMost` / `PopMost` 抛**：用 `TryPeekMost` / `TryPopMost` 或先判 `IsEmpty`。
- **非线程安全**：并发写会损坏频次索引与堆，需外部同步。
