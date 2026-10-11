# Sample.ListFaces

> **列表接口面** — `OrderedMultiList<T>` 的 `IList<T>` / `IReadOnlyList<T>` 实现与写入收窄（对应 feature F25，`DotNetCore.Collections.Multi`，6.5.0）。

## 本示例演示什么

`OrderedMultiList<T>` 在副本计数与全序之上完整实现 `IReadOnlyList<T>`：位置访问针对**副本展开**序列，
`this[int]` 是 `GetByRank` 的另一名字，`IndexOf(T)` 是 `GetRank(T)`，均 O(log n)。`IList<T>` 也实现，
但比较器决定元素归属，因此「放置」类成员被**收窄**：`Insert` 只接受相等元素段内的槽位，索引器 setter 抛
`NotSupportedException`。示例展示三个侧面：作为 `IReadOnlyList` 使用、`IList` 写入收窄、与 `MultiList` / `Deque` 对照。

## 什么时候用

- 需要 `IReadOnlyList<T>` 的下游（序列化器、绑定、LINQ）：直接把 `OrderedMultiList` 传过去。
- 有序 + 位置读：名次与下标一起用。
- 不该用：按下标插入任意位置（用 `List<T>` / `Deque<T>`）；按下标覆盖（用 `List<T>` / `Deque<T>`）。

## 如何运行

```bash
dotnet run --project sample/Sample.ListFaces
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 当作 `IReadOnlyList` 使用 | `IReadOnlyList<T>.Count` / 索引器 / `IndexOf` / `this[int]` / `IsReadOnly` |
| 2 | `IList` 放置面收窄 | `Insert(int,T)` / 索引器 setter 抛 `NotSupportedException` / `RemoveAt` / `Remove` / `RemoveAllCopies` |
| 3 | 与 `MultiList` / `Deque` 对照 | `is IReadOnlyList<T>` 判定 |

## 关键 API

- `OrderedMultiList<T>` — 实现 `IReadOnlyList<T>` 与 `IList<T>`；`Count` = **副本数**（经接口读）。
- `OrderedMultiList<T>.this[int]` — 等价 `GetByRank`（展开序列位置，O(log n)）。
- `OrderedMultiList<T>.IndexOf(T)` — 等价 `GetRank`（首份副本名次）；缺失返回 `-1`。
- `OrderedMultiList<T>.Insert(int,T)` — 只接受**相等元素段内**的槽位，其它位置抛 `ArgumentOutOfRangeException`。
- `OrderedMultiList<T>.RemoveAt(int)` / `Remove(T)` / `RemoveAllCopies(T)` — 删除面完整。

## 注意事项 / 陷阱

- **索引器 setter 抛 `NotSupportedException`**：有序序列禁止按下标写入（写入会破坏全序）。
- **`Insert` 只在相等元素段内**：非任意位置；需要任意位置插用 `List<T>` / `Deque<T>`。
- **`Count` = 副本数**（本例 4），不是 `DistinctCount`（3）。
- **`IReadOnlyList<T>` 没有 `IndexOf`**：`IndexOf` 是 `IList<T>` 成员，用具体类型或转 `IList<T>` 调用。
- **`MultiList<T>` 刻意不实现列表接口**：无序、位置无契约；要位置读用 `OrderedMultiList<T>`。
