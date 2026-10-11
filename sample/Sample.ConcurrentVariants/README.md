# Sample.ConcurrentVariants

> **并发变体** — `ConcurrentMultiList<T>` / `ConcurrentMultiDictionary<TKey,TValue>`：线程安全多重集合与映射（对应 feature F14，`DotNetCore.Collections.Multi`，6.2.0；只读投影为 6.7.1）。

## 本示例演示什么

并发变体让多线程无需外层加锁即可安全计数与分组。集合 `ConcurrentMultiList<T>` 每操作在单锁后串行，
保证线性化、计数确定；映射 `ConcurrentMultiDictionary<TKey,TValue>` 按键哈希分片（默认 8 片），
不同键的写可并行，跨片读取一致快照。示例展示三个侧面：多线程 `Add` 后计数确定、分片映射的键数 / 值数口径、
以及 6.7.1 新增的只读投影（`EntrySet()` / `DistinctItems()` / `ToList()` / `ToArray()` / `ToDictionary()`）。

## 什么时候用

- 多线程频次统计 / 直方图 → `ConcurrentMultiList<T>`。
- 多线程一对多索引 → `ConcurrentMultiDictionary<TKey,TValue>`。
- 不该用：读极多写极少 → 用 `ImmutableMultiList<T>`（F13，无锁读）。
- 不该用：单线程热点 → 可变版 `MultiList<T>` 更快。

## 如何运行

```bash
dotnet run --project sample/Sample.ConcurrentVariants
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 多线程 `Add`，计数确定 | `ConcurrentMultiList<T>.Add` / `CountOf` / `TotalCount` / `DistinctCount` |
| 2 | 分片映射与跨片读 | `ConcurrentMultiDictionary<TKey,TValue>.Add` / `ShardCount` / `KeyCount` / `TotalValueCount` / 索引器 |
| 3 | 6.7.1 只读投影（脱钩快照） | `EntrySet()` / `DistinctItems()` / `ToList()` / `ToArray()` / `ToDictionary()` |
| 4 | 枚举快照 | `ToArray()` 导出后写不影响已取快照 |

## 关键 API

- `ConcurrentMultiList<T>.Add(T)` / `Add(T,int)` — 单锁串行，线性化；`CountOf` / `TotalCount` / `DistinctCount` 与可变版语义一致。
- `ConcurrentMultiList<T>.Snapshot()` — 返回独立的 `MultiList<T>`；下列投影均取自它。
- `ConcurrentMultiList<T>.EntrySet()` / `DistinctItems()` / `ToList()` / `ToArray()` / `ToDictionary()` — 6.7.1 起提供；结果与 bag 脱钩，不受并发写影响。
- `ConcurrentMultiDictionary<TKey,TValue>` — 默认 8 片（`shardCount` 向上取 2 的幂）；`Count` / `KeyCount` = 键数，`TotalValueCount` = 值数。
- `ConcurrentMultiDictionary<TKey,TValue>.ShardCount` — 实际分片数。

## 注意事项 / 陷阱

- **集合单锁，无读并行**：并发正确 ≠ 高吞吐；读多写少用 `ImmutableMultiList<T>`。
- **映射跨片读贵**：`Count` / `TotalValueCount` / `ContainsValue` / 枚举按片索引顺序锁一遍取一致快照，别在循环里反复读。
- **枚举是快照拷贝**：枚举开始时付一次拷贝成本，但免疫并发写、不会撕裂。
- **`shardCount` 向上取 2 的幂**：路由掩码要求；`ShardCount` 报的是实际值。
- **`ToDictionary()` 遇 `null` 元素抛** `InvalidOperationException`（继承自 `MultiList<T>`）；`EntrySet()` 是 `null` 安全替代。
