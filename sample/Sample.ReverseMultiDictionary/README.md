# Sample.ReverseMultiDictionary

> **反向多重字典 ReverseMultiDictionary&lt;V,K&gt;** — `MultiDictionary` 的倒排快照（F17，`DotNetCore.Collections.Multi`，6.3.0；6.7.1 增强）。

## 本示例演示什么

`ReverseMultiDictionary<V,K>` 把「值 → 持有它的键集合」拷出来，成为**自包含**实例：「哪些键存了这个值？」O(1) 回答。构造器从源 map 拷出绑定、不保留引用链，因此快照在源后续变化 / 被丢弃后保持不变，可安全序列化。示例展示快照语义、轴对调的成员命名，以及它与 6.7.1 落地的 `AsReverse` 全家桶（活视图 / 有序拷贝 / 独立拷贝）的对照。

## 什么时候用

- 离线倒排分析、需要**自包含**可序列化的倒排索引。
- 需要实例构造后仍可**独立增删**。
- 不该用：要跟源联动 → 用 `MultiDictionary.AsReverse()`（活视图，见 F18）。

## 如何运行

```bash
dotnet run --project sample/Sample.ReverseMultiDictionary
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 倒排快照（拷贝，不跟源） | `new ReverseMultiDictionary<V,K>(source)` / `this[value]` |
| 2 | 轴对调命名与缺值语义 | `ValueCount` / `TotalKeyCount` / `KeyCount` / `ContainsValue` / `ContainsKey` |
| 3 | 独立可变与不变式 | `Add` / `Remove(value, key)` |
| 4 | `AsReverse` 全家桶对照 | `MultiDictionary.AsReverse` / `OrderedMultiDictionary.AsReverse` / `BiDictionary.AsReverse` / `TwoKeyDictionary.AsReverse` |

## 关键 API

- `ReverseMultiDictionary<V,K>(MultiDictionary<K,V> source)` — 从源 map 拷出绑定的自包含快照。
- `this[V value]` — 持有该值的键集合；缺值返回空集合，不抛。
- `Count` / `ValueCount` — 不同**值**数（镜像 `MultiDictionary.KeyCount`）。
- `TotalKeyCount` — 键出现总数（镜像 `TotalValueCount`）；`KeyCount(V)` — 该值下的键数。
- `ContainsValue(V)` — O(1) 存在性；`ContainsKey(K)` — 扫描键（O(entries)）。
- `Add` / `Remove(V)` / `Remove(V,K)` / `Clear` — 快照自身可独立变；「无空内层」不变式镜像。
- `ToSerializableModel()` / `FromModel` — 序列化模型（`MultiDictionaryModel<V,K>`）。

## 注意事项 / 陷阱

- **是快照不是活视图**：源变化不自动反映；要联动用 `MultiDictionary.AsReverse()`。
- **重复值折叠**：同键存同值多次，倒排只列一次键。
- **`null` 值成 `null` 条目**（专用桶），枚举最后。
- **`AsReverse` 一名的多种形态**：`MultiDictionary` / `BiDictionary` 返回**活视图**，`OrderedMultiDictionary` 返回**有序拷贝**，`TwoKey` / `ThreeKey` 返回**独立拷贝**；读返回类型再假定活性。
