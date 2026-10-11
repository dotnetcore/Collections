# Sample.BagStatistics

> **bag 统计 Mode / Median / Entropy** — `IMultiSet<T>` 上的三个频次分布摘要扩展（6.9.0，`DotNetCore.Collections.Multi`，6.9.0）。

## 本示例演示什么

三个扩展方法把任意 `IMultiSet<T>` 当作频次分布来概括：`Mode()` 给出**所有**并列最高副本数的元素（多重集可能多峰）、
`Median(comparer)` 给出**上中位副本**、`Entropy()` 给出副本分布的香农熵（比特）。它们都通过 `EntrySet()`
（不同元素 + 副本数）读取，**不展开副本**，所以对任何 bag 实现都成立，也不需要泛型数学支持。

## 什么时候用

- 快速概括一批频次数据的「众数 / 中位 / 离散度」。
- 需要「多峰」众数而不是单一赢家（普通众数实现常常只给一个）。
- 需要中位数但又不想展开全部副本（内存 O(d) 而非 O(总份数)）。
- 不该用：需要更细的分位 / 名次读 → `PositionalRead`（F24，经有序集）；只是想要直方图 → `EntrySet()`。

## 如何运行

```bash
dotnet run --project sample/Sample.BagStatistics
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 众数（并列全部返回） | `IMultiSet<T>.Mode()` |
| 2 | 上中位副本（偶数取较大者） | `IMultiSet<T>.Median(IComparer<T>)` |
| 3 | 副本分布香农熵（比特） | `IMultiSet<T>.Entropy()` |

## 关键 API

- `IMultiSet<T>.Mode()` — 返回 `IReadOnlyList<T>`，含**全部**并列最高副本数的元素；空袋返回空列表。
- `IMultiSet<T>.Median(IComparer<T> comparer)` — 返回上中位副本：奇数总量取正中，偶数总量取两个中位中较大者；
  结果必是袋中元素，不插值；空袋抛 `InvalidOperationException`。
- `IMultiSet<T>.Entropy()` — 返回 `double`，`-Σ p·log2(p)`；单元素袋为 0，d 个元素均分为 `log2(d)`，空袋为 0。
- 三者都基于 `EntrySet()`；`Median` 成本 O(d log d) 时间、O(d) 空间（d 为不同元素数）。

## 注意事项 / 陷阱

- **`Median` 需要 `IComparer<T>`**：没有无参重载；因为多重集不一定是数值类型，绝不取平均。
- **`Mode` 返回顺序**是袋枚举不同元素的顺序（哈希顺序，无契约），不要依赖插入序。
- **上中位**：偶数总量返回的是较大的那个中位元素，不是两个中位的平均。
- **空袋**：`Mode` 返回空列表、`Entropy` 返回 0，但 `Median` **抛** `InvalidOperationException`。
- **非数值元素也能用**：计数是普通 `int`，不依赖泛型数学，`net451` 同样可用。
