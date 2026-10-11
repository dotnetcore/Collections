# Sample.LiveReverseView

> **多重映射活反查视图** — `MultiDictionary&lt;TKey,TValue&gt;.AsReverse()`（F18，`DotNetCore.Collections.Multi`，6.3.0）。

## 本示例演示什么

`MultiDictionary<TKey,TValue>.AsReverse()` 返回**活的只读倒排视图**（`IReadOnlyDictionary<TValue, IReadOnlyCollection<TKey>>`）：值 → 持有它的键集合。查找与枚举反映源 map 的后续变化，且不暴露突变成员。视图直接读源已维护的反向索引（`_valueIndex` + null 值桶），构建零成本、每读 O(1)。它是两个倒排入口的**活半边**，与 `ReverseMultiDictionary` 快照（F17）任意时刻一致。

## 什么时候用

- UI / 服务内的**实时反查**：源一变视图即变，无需重建。
- 需要零拷贝、只读安全地暴露倒排关系。
- 不该用：导出 / 持久化（用 `ReverseMultiDictionary` 快照）；跨线程无锁读（需外部同步）。

## 如何运行

```bash
dotnet run --project sample/Sample.LiveReverseView
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 活视图随源变化 | `AsReverse()` / `this[value]` |
| 2 | 缺值与折叠多重性 | `inverted[value]`（缺值空集合）/ 同键重复只列一次 |
| 3 | 只读安全 | 静态类型 `IReadOnlyDictionary<...>` / `Count` |
| 4 | null 值条目 | `ContainsKey(null)` / `this[null]` |

## 关键 API

- `MultiDictionary<TKey,TValue>.AsReverse()` — 活只读倒排视图；返回 `IReadOnlyDictionary<TValue, IReadOnlyCollection<TKey>>`。
- `inverted[value]` — 持有该值的键集合；缺值返回空集合，不抛。
- `inverted.Count` — 不同值数；枚举反映源后续变化。
- 存储的 `null` 值作为**最后枚举**的 `null` 条目浮现。

## 注意事项 / 陷阱

- **活视图，依赖源存活**：不要序列化它，也不要长期持有而源已弃。
- **非线程安全**：视图暴露源内部、无同步；多线程读需外部加锁或改用不可变结构。
- **折叠多重性**：同键存同值多次只列一次键。
- **缺值空集合**：与 `MultiDictionary` 索引器一致，不抛。
