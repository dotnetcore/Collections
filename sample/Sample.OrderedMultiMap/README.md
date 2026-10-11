# Sample.OrderedMultiMap

> **有序多重映射 OrderedMultiDictionary&lt;TKey,TValue&gt;** — 键升序、键下值也升序的多重映射（F08，`DotNetCore.Collections.Multi`，6.2.0）。

## 本示例演示什么

`OrderedMultiDictionary<TKey,TValue>` 是 `MultiDictionary` 的有序对应物：键保持升序，每个键下的值也保持升序，单次增删查在两轴上最坏 O(log n)。它共享按键值集合语义（参数为 set、值可重复、内层清空即删键）。示例展示三个侧面：双轴有序枚举、`Count` 是键数而非值数的计数口径、以及针对「键升序 × 键下值升序」展开序列的名次读。

## 什么时候用

- 有序索引 / 字典序报表，需要可预测的键序与值序。
- 全局值名次 / 分位（`GetByRank` / `GetMedian` / `GetQuantile`）。
- 不该用：O(1) 热点键值查找 → `MultiDictionary`。

## 如何运行

```bash
dotnet run --project sample/Sample.OrderedMultiMap
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 双轴有序枚举 | `Add(TKey, TValue)` / `GetEnumerator` |
| 2 | 计数口径（Count = 键数） | `Count` / `TotalValueCount` / `ValueCount(key)` / `Keys` / `Values` |
| 3 | 名次读（针对展开序列） | `GetByRank` / `GetRank` / `GetMedian` |
| 4 | 值查询与分组导出 | `Contains(key,value)` / `ContainsValue` / `EntrySet` |

## 关键 API

- `OrderedMultiDictionary<TKey,TValue>.Add(TKey, TValue)` — 加一对；两轴各一次下降，O(log n)。
- `Count` / `KeyCount` — **键数**；`TotalValueCount` / `ValueCount(key)` — **值数**。
- `Keys` — 键升序；`Values` — 扁平序列（键升序 × 键下值升序）。
- `this[TKey]` — 该键下值升序的活序列；缺键返回空集合，不抛。
- `GetByRank(int)` / `GetRank(TKey, TValue)` / `GetMedian()` / `GetQuantile(double)` — 针对展开序列的名次读。
- `Contains(TKey, TValue)` — 某键下是否有该值；`ContainsValue(TValue)` — 任意键下是否有该值（全扫描 O(n)）。
- `EntrySet()` — 「键 + 该键下全部值」的分组投影。

## 注意事项 / 陷阱

- **`Count` 是键数**，不是值数：值总数用 `TotalValueCount`。
- **`Values` 是扁平序列**：跨键按键交错，不是全局值排序。
- **`GetRank` 不是不同元素下标**：同值多份时名次指展开后的位置。
- **缺键索引器返回空集合**，不抛；`Count` 与 `ContainsKey` 才能区分「缺键」与「空值」。
