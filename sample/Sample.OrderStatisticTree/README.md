# Sample.OrderStatisticTree

> **顺序统计树引擎** — `OrderStatisticTree&lt;TKey,TPayload&gt;`（internal）经 `OrderedMultiList&lt;T&gt;` 观察（F07，`DotNetCore.Collections.Multi`，6.2.0 / 6.5.0）。

## 本示例演示什么

`OrderStatisticTree<TKey,TPayload>` 是有序 Multi 家族的存储引擎，把每个不同键映射到副本数并缓存子树副本总数，使「第 k 个副本」「某键的名次」成为**单次根到叶下降**。引擎是 `internal`，不对外暴露，因此本示例**只经 `OrderedMultiList<T>` 的可观察行为**演示：名次读（`GetByRank` / `GetRank`）、中位 / 分位（`GetMedian` / `GetQuantile`）、区间与不同元素视图。

## 什么时候用

- 需要 O(log n) 的**名次读**（第 k 个副本 / 某元素名次），而不想物化整条序列。
- 需要中位 / 分位这类顺序统计量（见 F24）。
- 本篇也面向需要理解有序 Multi 性能模型的读者；一般使用方直接用 F05 / F06 / F08 / F24 即可。
- 不该用：直接引用引擎类型 —— 它是 `internal`，编译不过。

## 如何运行

```bash
dotnet run --project sample/Sample.OrderStatisticTree
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 名次读（O(log n)） | `GetByRank` / `GetRank` / `this[int]` / `IndexOf` |
| 2 | 中位与分位 | `GetMedian` / `GetQuantile` |
| 3 | 区间与不同元素视图 | `GetRange` / `DistinctItems` / `EntrySet` |

## 关键 API

- `OrderedMultiList<T>.GetByRank(int)` — 第 k 个**副本**（名次针对展开序列）；越界抛 `ArgumentOutOfRangeException`。
- `OrderedMultiList<T>.GetRank(T)` — 元素首份副本的名次；不存在返回 `-1`。
- `OrderedMultiList<T>.this[int]` / `IndexOf(T)` — 分别等价于 `GetByRank` / `GetRank`。
- `OrderedMultiList<T>.GetMedian()` — 中位元素；偶数副本取**下中位**，空集合抛 `InvalidOperationException`。
- `OrderedMultiList<T>.GetQuantile(double)` — 最近秩定义（`ceil(q × TotalCount) - 1`），永不插值。
- `OrderedMultiList<T>.DistinctItems()` / `EntrySet()` — 不同元素 / 「元素 + 副本数」视图。

## 注意事项 / 陷阱

- **引擎是 `internal`**：文档 / 示例不要引用 `OrderStatisticTree` 作为公开 API（会编译不过）。
- **名次针对副本展开序列**，不是不同元素下标：`GetByRank(0)` 是最小元素的**第一份副本**。
- **6.5.0 前是红黑树**：引用旧文档时注意引擎差异（对外语义不变）。
- **`GetQuantile` 不插值**：返回的永远是一个实际持有的元素，不会在两个元素之间取平均。
