# Sample.BoundedBag

> **中文** | [English](README.md)

> **定容滑动窗口袋 BoundedBag&lt;T&gt;** — 副本总数被封顶的 bag：超容即静默淘汰最旧副本（6.9.0，`DotNetCore.Collections.Multi`，6.9.0）。

## 本示例演示什么

`BoundedBag<T>` 是 bag 家族里的「滑动窗口」成员。它最多持有固定的 `Capacity` 份副本，一旦新副本会超出上限，
就静默丢掉**最旧**的那一份（先进先出），所以袋里始终是最新的若干次添加。本示例展示四个侧面：容量与淘汰、
`Add` 的 void 语义与顺序敏感、投影/枚举、以及删除与参数校验。核心记忆点——**添加永不失败**。

## 什么时候用

- 需要「最近 N 次」滑动窗口（最近日志、最近访问、最近 N 个样本）。
- 容量很小、且允许最旧数据被自动挤出的场景。
- 不该用：需要无上限增长 → `MultiList<T>`（F01）；需要按副本数做多重集运算 / 实现 `IMultiSet<T>` → `MultiList<T>`；
  小值域直方图 → `PackedBag<T>`（F21）。

## 如何运行

```bash
dotnet run --project sample/Sample.BoundedBag
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 容量上限与静默 FIFO 淘汰 | `Capacity` / `TotalCount` / `IsFull` / `Add(T)` / `Contains(T)` |
| 2 | 一次加多份、顺序敏感与极端 times | `Add(T, times)` / `AddRange(IEnumerable<T>)` |
| 3 | 投影与枚举（最旧→最新） | `DistinctItems()` / `EntrySet()` / `ToDictionary()` / `ToArray()` / `ToString()` |
| 4 | 删除返回剩余份数与参数校验 | `Remove(T, times)` / `RemoveAllCopies(T)` / `Add(T, 0)` 抛 `ArgumentOutOfRangeException` |

## 关键 API

- `new BoundedBag<T>(capacity)` / `new BoundedBag<T>(capacity, comparer)` — 容量必须为正，否则 `ArgumentOutOfRangeException`。
- `Add(T)` / `Add(T, times)` — 返回 **`void`**，永不失败；`times >= Capacity` 时窗口只剩 `Capacity` 份该元素。
- `AddRange(IEnumerable<T>)` — 逐份添加，**顺序敏感**：超出容量的最早元素最先被淘汰。
- `Capacity` / `TotalCount` / `Count`（此处是**公开属性**）/ `DistinctCount` / `IsEmpty` / `IsFull`。
- `Contains(T)` / `CountOf(T)` / `DistinctItems()` / `EntrySet()` / `ToDictionary()` / `ToArray()` / `ToList()` — 线性扫描有界窗口。
- `Remove(T, times)` 返回**剩余副本数**（元素缺失时为 0）；`RemoveAllCopies(T)` 返回 `bool`。
- 枚举按「最旧→最新」展开副本；`Clone()` 生成独立副本。

## 注意事项 / 陷阱

- **`Add` 不返回任何东西**：没有溢出可报告，想感知淘汰只能自己比对前后内容。
- **顺序敏感**：与 `MultiList<T>` 不同，`AddRange` 的先后决定谁先被淘汰，因此它**不实现 `IMultiSet<T>`**。
- **无参数构造不存在**：容量是必填项，且必须为正。
- **`null` 是合法元素**，但 `ToDictionary()` 无法用 `null` 作键，会抛 `InvalidOperationException`。
- **非线程安全**：并发写会损坏环形缓冲，需外部同步。
