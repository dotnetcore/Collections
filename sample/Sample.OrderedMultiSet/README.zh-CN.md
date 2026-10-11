# Sample.OrderedMultiSet

> **中文** | [English](README.md)

> **有序多重集合 OrderedMultiList&lt;T&gt;** — 有序 bag：允许重复、跟踪副本数、枚举升序（F05，`DotNetCore.Collections.Multi`，6.2.0）。

## 本示例演示什么

`OrderedMultiList<T>` 是 `MultiList<T>` 的有序对应物：副本计数语义逐条一致（一个不同元素带 N 份副本、枚举副本展开），但额外提供一个**全序**——枚举升序，查找 / 插入 / 删除最坏 O(log n)。这个 sample 展示四个侧面：有序计数与升序枚举、批量副本与删除、比较器如何同时定义**顺序与同一性**、以及 `IList` 写入为何受限、结构相等为何未重写。

## 什么时候用

- 既要计数又要**有序输出**（有序频次、榜单、字典序报表）。
- 需要 `GetFirst` / `GetLast` 这类 O(log n) 极值，或后续的名次 / 区间读（见 F06 / F07 / F24）。
- 不该用：纯无序计数热点 → `MultiList<T>`（O(1) 更快）；小值域直方图 → `PackedBag<T>`。

## 如何运行

```bash
dotnet run --project sample/Sample.OrderedMultiSet
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 有序计数与升序枚举 | `Add(T)` / `CountOf` / `TotalCount` / `DistinctCount` / `GetFirst` / `GetLast` |
| 2 | 批量副本与删除 | `Add(T, times)` / `Remove(T, times)`（返回剩余副本数）/ `RemoveAllCopies` |
| 3 | 比较器定义顺序与同一性 | `new OrderedMultiList<T>(IComparer<T>)` / `StringComparer.OrdinalIgnoreCase` |
| 4 | 结构相等与 `IList` 写入限制 | `Equals`（未重写）/ `IsSubsetOf` / `Insert` / 索引器赋值抛 `NotSupportedException` |

## 关键 API

- `OrderedMultiList<T>` — 有序多重集合；构造器接受 `IComparer<T>`（顺序与同一性由它决定）。
- `Add(T)` / `Add(T, int times)` — 加 1 份 / 加多份；`times <= 0` 抛 `ArgumentOutOfRangeException`。
- `Remove(T)` / `Remove(T, int times)` — 返回**剩余**副本数；`RemoveAllCopies(T)` 返回 `bool`。
- `CountOf` / `TotalCount` / `DistinctCount` — 单元素副本数 / 全部副本数 / 不同元素数。
- `GetFirst` / `GetLast` — O(log n) 极值；空集合抛 `InvalidOperationException`。
- `Insert(int, T)` — 只接受相等元素段内的位置；索引器 `this[int]` 赋值抛 `NotSupportedException`。
- `IsSubsetOf` / `IsSupersetOf` — 多重集语义的内容比较（`Equals` 未重写）。

## 注意事项 / 陷阱

- **`Count` 是副本数**：`((IReadOnlyCollection<T>)shelf).Count == TotalCount`；不同元素数用 `DistinctCount`。
- **比较器不是相等比较器**：本类型要 `IComparer<T>`（全序）；比较为 `0` 即同一元素，存下的是先加入的那个。
- **结构相等未重写**：两个内容相同的 `OrderedMultiList` 仍是不同对象；请用 `IsSubsetOf` + `IsSupersetOf` 或比 `EntrySet`。
- **`IList` 写入受限**：`Insert` 只接受相等元素段内的位置（其它位置抛 `ArgumentOutOfRangeException`），索引器赋值抛 `NotSupportedException`。
