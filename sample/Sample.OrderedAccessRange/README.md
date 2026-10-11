# Sample.OrderedAccessRange

> **有序访问与区间查询** — `OrderedMultiList` 的极值、区间与有序访问（F06，`DotNetCore.Collections.Multi`，6.2.0）。

## 本示例演示什么

在全序之上，`OrderedMultiList<T>` 提供一组有序访问与区间查询：`GetFirst` / `GetLast` 取极值（O(log n)），`GetRange` 按元素区间切出升序的副本展开序列，且可独立控制两端开闭。这些能力都依赖底层顺序统计树的有序叶链，区间扫描不重新进入分支。示例同时展示 `OrderedMultiDictionary` 的**键升序、键下值升序**这一双轴有序语义。

## 什么时候用

- 排行榜的 Top / Bottom，或任何「即取极值」的场景。
- 分数段 / 字母段等**区间过滤**，不想做全量 `Where` + `OrderBy`。
- 有序快照输出，直接对接报表 / 榜单。
- 不该用：无序只需计数 → `MultiList`；需要降序枚举 → 反转结果或换比较器。

## 如何运行

```bash
dotnet run --project sample/Sample.OrderedAccessRange
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 极值即取即用 | `GetFirst()` / `GetLast()` |
| 2 | 闭区间与半开区间 | `GetRange(from, to)` / `GetRange(from, to, inclusiveFrom, inclusiveTo)` |
| 3 | 空集合行为 | `GetFirst` 抛 `InvalidOperationException`；`GetRange` 返回空序列 |
| 4 | 有序映射键序 / 值序 | `OrderedMultiDictionary.Keys` / 索引器 `this[key]` |

## 关键 API

- `OrderedMultiList<T>.GetFirst()` / `GetLast()` — 最小 / 最大元素，O(log n)；空集合抛 `InvalidOperationException`。
- `OrderedMultiList<T>.GetRange(T from, T to)` — 两端**闭**的区间，返回副本展开的升序序列。
- `OrderedMultiList<T>.GetRange(T from, T to, bool inclusiveFrom, bool inclusiveTo)` — 独立控制两端开闭；`from` 排在 `to` 之后时返回空序列。
- `OrderedMultiDictionary<TKey,TValue>.Keys` — 按键升序枚举。
- `OrderedMultiDictionary<TKey,TValue>.this[TKey]` — 该键下值升序的活序列；缺键返回空集合。

## 注意事项 / 陷阱

- **区间边界由比较器定义**：换比较器即换边界语义，本类型不解释元素含义。
- **极值在空集合上抛** `InvalidOperationException`：先判 `TotalCount` / `DistinctCount`。
- **区间结果是副本展开**：长度按副本数计（`bean` 两份就产出两次）。
- **有序映射的值序是「每键内」升序**，不是全局值排序；跨键的值按键交错。
