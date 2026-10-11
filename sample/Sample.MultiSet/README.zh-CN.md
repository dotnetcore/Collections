# Sample.MultiSet

> **中文** | [English](README.md)

> **多重集合 MultiList&lt;T&gt;** — 无序 bag：允许重复元素，并为每个不同元素跟踪**副本数**（F01，`DotNetCore.Collections.Multi`，6.0.0）。

## 本示例演示什么

`MultiList<T>` 是本包的基础「多重集合」。它把「元素 → 副本数」做成一等语义，而不是让你拿 `List<T>`
手写计数。这个 sample 展示四个侧面：计数读写、批量添加与副本展开枚举/导出、按多重集语义的集合运算、
以及删除与参数校验。重点纠正一个高频误解——**`Count` 是副本数，不是不同元素数**。

## 什么时候用

- 频次统计 / 直方图（值域较大），需要 O(1) 的单副本增删与查询。
- 需要「展开后序列」（每个副本各出现一次）直接对接下游。
- 需要按**副本数**做并 / 交 / 差的多重集运算。
- 不该用：要有序输出 → `OrderedMultiList<T>`（F05）；小值域（≤16）直方图 → `PackedBag<T>`（F21）；
  Top-K 高频查询 → `FrequencyPriorityBag<T>`（F23）。

## 如何运行

```bash
dotnet run --project sample/Sample.MultiSet
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 加副本、读副本数与总数 | `Add(T)` / `Add(T, times)` / `CountOf` / `TotalCount` / `DistinctCount` |
| 2 | 批量添加、展开枚举与导出 | `AddRange` / `EntrySet()` / `ToArray()` / `ToList()` / `ToDictionary()` |
| 3 | 多重集并 / 交 / 差 / 对称差 | `UnionWith` / `IntersectionWith` / `ExceptWith` / `SymmetricExceptWith` |
| 4 | 删除与非正份数校验 | `Remove(T, times)` / `Add(T, 0)` 抛 `ArgumentOutOfRangeException` |

## 关键 API

- `MultiList<T>.Add(T item)` / `Add(T item, int times)` — 加 1 份 / 加多份；`times <= 0` 抛 `ArgumentOutOfRangeException`。
- `CountOf(T)` — 该元素的副本数；`TotalCount` — 全部副本数；`DistinctCount` — 不同元素数。
- `((IReadOnlyCollection<T>)bag).Count` — 等于 `TotalCount`（副本数）；`Count` 是显式接口实现，具体类型上要经接口读。
- `EntrySet()` — `(元素, 副本数)` 投影，项数等于 `DistinctCount`。
- `ToArray()` / `ToList()` — 展开全部副本；`ToDictionary()` — 导出「元素 → 副本数」快照。
- `UnionWith` 取副本数 **max**、`IntersectionWith` 取 **min**、`ExceptWith` 相减、`SymmetricExceptWith` 取绝对差。

## 注意事项 / 陷阱

- **`Count` 不是不同元素数**：`bag.Count == TotalCount`（副本数）；要不同元素数用 `DistinctCount`。
- **枚举顺序无契约**：哈希表枚举顺序是实现细节，不要依赖插入序或哈希序写断言。
- **`UnionWith` 不加副本数**：`{x,x} ∪ {x}` 得 `{x,x}`（取 max），不是 `{x,x,x}`。
- **`ToDictionary()` 遇 `null` 元素会抛** `InvalidOperationException`（`null` 不能作字典键）；需要携带 `null` 用 `ToSerializableModel`（F10）。
- **非线程安全**：并发写会损坏内部计数表，请用 `ConcurrentMultiList<T>`（F14）。
