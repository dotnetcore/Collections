# Sample.PositionalRead

> **名次与分位读** — `GetByRank` / `GetRank` / `GetMedian` / `GetQuantile` / `TotalValueCount`（对应 feature F24，`DotNetCore.Collections.Multi`，6.5.0 列表 + 6.6.0 映射）。

## 本示例演示什么

有序 Multi 家族提供名次与分位读：按**副本展开**序列的位置取元素 / 反查名次，并在映射上给出中位数与分位。
示例展示三个侧面：列表名次（`this[i]` / `IndexOf` 即名次读）、映射的名次与分位、以及「次序统计 ≠ 数值插值」
这一关键口径。全部操作 O(log n)：单次根到叶下降，不物化展开序列。

## 什么时候用

- 中位 / 四分位 → `GetMedian` / `GetQuantile`。
- 排名 / 百分位反查 → `GetRank`。
- 第 k 优元素 → `GetByRank`。
- 不该用：需要数值插值分位（如 P95 统计，自行物化计算）；无序集合（`MultiList<T>` 无此面）。

## 如何运行

```bash
dotnet run --project sample/Sample.PositionalRead
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 列表名次（副本展开） | `OrderedMultiList<T>.GetByRank` / `GetRank` / `this[int]` / `IndexOf` / `GetMedian` / `GetQuantile` |
| 2 | 映射名次与分位 | `OrderedMultiDictionary<TKey,TValue>.GetByRank` / `GetRank` / `GetMedian` / `GetQuantile` / `TotalValueCount` |
| 3 | 次序统计 vs 数值插值 | `GetQuantile` 返回真实元素；越界抛 `ArgumentOutOfRangeException` |

## 关键 API

- `OrderedMultiList<T>.GetByRank(int)` — 展开序列第 rank 个元素；`this[int]` 是它的别名。
- `OrderedMultiList<T>.GetRank(T)` — 元素**首份副本**的名次；缺失返回 `-1`；`IndexOf` 是别名。
- `OrderedMultiList<T>.GetMedian()` / `GetQuantile(double)` — 次序中位 / 次序分位（nearest-rank）。
- `OrderedMultiDictionary<TKey,TValue>.GetByRank(int)` — 展开序列第 rank 个 `(key, value)` 对。
- `OrderedMultiDictionary<TKey,TValue>.GetRank(TKey,TValue)` — 该对名次；缺失返回 `-1`。
- `OrderedMultiDictionary<TKey,TValue>.GetMedian()` / `GetQuantile(double)` / `TotalValueCount` — 中位 / 分位 / 值数（O(1)）。

## 注意事项 / 陷阱

- **名次 = 副本展开序列位置**，不是不同元素下标：`{1,3,3,5}` 的 `GetByRank(2)` 是第二个 `3`。
- **`GetQuantile` 是次序分位，非插值**：返回真实存储的元素，绝不在两个值之间插值。
- **映射 rank 针对全局 `(键, 值)` 对序列**：展开序列 = 键升序 × 每键值升序；rank 0 是最小键的最小值。
- **`TotalValueCount` 在映射上 O(1)**（树根缓存）；列表侧对应 `TotalCount`。
- **越界抛异常**：`GetByRank` 越界抛 `ArgumentOutOfRangeException`；空集合读中位 / 分位抛 `InvalidOperationException`。
