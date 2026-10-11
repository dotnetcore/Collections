# Sample.StructuralEquality

> **多重集合结构相等** — `MultiList<T>` 的 `Equals` / `GetHashCode`（F03，`DotNetCore.Collections.Multi`，6.1.0）。

## 本示例演示什么

`MultiList<T>` 实现 `IEquatable<MultiList<T>>`，提供**多重集合结构相等**：两个 bag 相等，当且仅当双方持有
相同的不同元素、且每个元素副本数相同，与枚举顺序无关。这个 sample 展示三个侧面：副本数参与比较、
相等的 bag 在 `HashSet` 里塌缩为一条、以及两个易错点——`==` 未重载、`IsSubsetOf` 不是相等性。

## 什么时候用

- 测试里直接断言 bag 内容：`Assert.Equal(expected, actual)`，不必手写 `EntrySet()` 循环。
- 以 bag 为键的缓存 / 去重：`HashSet<MultiList<T>>`、`Dictionary<MultiList<T>, ...>`。
- 需要顺序无关的内容比较（插入顺序不同的相等 bag 哈希相同）。
- 不该用：要按引用区分内容相同的两份 → 保留引用比较（默认即是，别改用 `Equals`）。

## 如何运行

```bash
dotnet run --project sample/Sample.StructuralEquality
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 副本数参与比较（bag equality） | `MultiList<T>.Equals(MultiList<T>)` |
| 2 | 相等 bag 在 `HashSet` 塌缩 | `GetHashCode()` + `Equals` |
| 3 | `==` 未重载、子集判定不是相等 | `==`（引用比较）/ `IsSubsetOf` / `Equals` |

## 关键 API

- `MultiList<T>.Equals(MultiList<T>)` — 结构相等：相同不同元素 + 相同副本数，与顺序无关。
- `MultiList<T>.GetHashCode()` — 与 `Equals` 一致，混入 `TotalCount` / `DistinctCount` 与各元素贡献（XOR）。
- `MultiList<T>.IsSubsetOf(IEnumerable<T>)` — 多重集子集判定（本包内**不是**相等性）。
- `==` / `!=` — **未重载**，仍是引用比较。

## 注意事项 / 陷阱

- **仅 `MultiList<T>` 有结构相等**：其它 Multi 类型（如 `MultiDictionary`）仍是引用比较。
- **`==` 未重载**：`a == b` 是引用比较，内容相等要用 `a.Equals(b)`。
- **`IsSubsetOf` 不是相等性**：它忽略另一侧多余副本，`{7}.IsSubsetOf({7,7})` 为 `True` 但两者不等。
- **`GetHashCode` 顺序无关**：相等 bag 哈希一致，可安全进入 `HashSet` / `Dictionary`。
