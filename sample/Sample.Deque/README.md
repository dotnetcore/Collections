# Sample.Deque

> **双端队列** — `Deque<T>`：环状缓冲双端队列，两端 O(1) 摊还（对应 feature F28，`DotNetCore.Collections.Multi`，6.6.0）。

## 本示例演示什么

BCL 没有 deque：`Queue` / `Stack` 单端，`LinkedList` 双端但每元素一堆节点。`Deque<T>` 用**单数组环状缓冲**
补位：头尾是同一分配里的两个移动下标，推 / 弹任一端是下标移动 + 一个数组槽，只有增长才分配。示例展示
三个侧面：两端操作与位置读、滑动窗口、以及完整列表接口（索引器 setter / `Insert` / `RemoveAt`）。
位置**从头到尾**计：下标 0 是首元素，`Count-1` 是末元素。

## 什么时候用

- 滑动窗口 / 最近 N 条：两端推弹，O(1)。
- 工作窃取双端队列：两端 O(1) 增删。
- 撤销 / 重做双栈：两端 + 位置读。
- 不该用：要排序（用 `OrderedMultiList<T>`）；热点 `Contains`（用 `HashSet` / `MultiList<T>`）。

## 如何运行

```bash
dotnet run --project sample/Sample.Deque
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 两端操作与位置读 | `AddFirst` / `AddLast` / `RemoveFirst` / `RemoveLast` / `this[int]` / `GetFirst` / `GetLast` / `IndexOf` |
| 2 | 滑动窗口 | `new Deque<T>(int capacity)` / `AddLast` / `RemoveFirst` / `Count` / `Capacity` |
| 3 | 完整列表接口 | 索引器 setter / `Insert` / `RemoveAt` / `Contains` / `TrimExcess` / `IsReadOnly` |
| 4 | 空时行为与成本提醒 | `IsEmpty` / `TryRemoveFirst` / 空端点操作抛 `InvalidOperationException` |

## 关键 API

- `Deque<T>` — 实现 `IList<T>` / `IReadOnlyList<T>`；`AddFirst` / `AddLast` / `RemoveFirst` / `RemoveLast` 均 O(1) 摊还。
- `Deque<T>.this[int]` — O(1) 环读，可赋值（**覆盖**，不移位）。
- `Deque<T>.Insert(int,T)` / `RemoveAt(int)` — O(min(index, Count-1-index))。
- `Deque<T>.GetFirst()` / `GetLast()` / `TryRemoveFirst` / `TryRemoveLast` / `TryGetFirst` / `TryGetLast`。
- `Deque<T>.Capacity` / `TrimExcess()` / `AsReadOnly()` / `Clone()` / `ToArray()`。

## 注意事项 / 陷阱

- **`Contains` / `IndexOf` / `Remove(T)` 是 O(n)**：环缓冲无索引；要成员测试用 `HashSet` / `MultiList<T>`。
- **容量不自动缩**：排空后保留缓冲；要回收用 `TrimExcess` 或直接设 `Capacity`。
- **构造容量不是硬上限**：超了仍会 4 倍增；要硬定容请自己判 `Count`。
- **空时端点操作抛** `InvalidOperationException`：先判 `IsEmpty` / `Count`，或用 `TryRemoveFirst` / `TryRemoveLast`。
- **结构相等未重写**：比内容用 `ToArray()`。
