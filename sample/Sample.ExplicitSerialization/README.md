# Sample.ExplicitSerialization

> **显式序列化模型** — `MultiListModel<T>` / `MultiDictionaryModel<TKey,TValue>` 快照与重建（F10，`DotNetCore.Collections.Multi`，6.2.0；6.7.1 扩展至全家桶）。

## 本示例演示什么

Multi 家族提供**显式序列化模型**：`MultiListModel<T>`（不同元素 + 副本数两条平行列表）与
`MultiDictionaryModel<TKey,TValue>`（键列表 + 每键值列表）。它们是普通可变 POCO——公开可设属性、无特性、
无接口、无基类——**不强迫消费方使用任何序列化器**。这个 sample 展示：bag / multimap 的往返、
模型可携带 `null`（`ToDictionary` 做不到）、`FromModel` 的严格校验，以及 6.7.1 的全家桶增强
（`BiDictionary` 专用模型、`ReverseMultiDictionary` 复用 `MultiDictionaryModel`）。

## 什么时候用

- 持久化 bag / multimap：形状稳定的 POCO，JSON / XML / 数据库行都可。
- 跨进程序列化：库不绑任何序列化器，格式自选。
- 需要携带 `null` 元素的往返：模型用平行列表，`null` 与普通元素一视同仁。
- 不该用：只要内存快照 → `Clone` / `ToDictionary`；要自动 JSON 集成 → 自写转换器。

## 如何运行

```bash
dotnet run --project sample/Sample.ExplicitSerialization
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | `MultiList` 快照与重建 | `ToSerializableModel()` / `MultiList<T>.FromModel` / `Items` / `Counts` |
| 2 | `MultiDictionary` 快照与重建 | `ToSerializableModel()` / `FromModel` / `Keys` / `Values` |
| 3 | 模型携带 `null` 元素 | `ToSerializableModel` / `ToDictionary`（对照抛异常） |
| 4 | 畸形模型被拒绝 | `FromModel` 抛 `ArgumentException` |
| 5 | 6.7.1 `BiDictionary` 专用模型 | `BiDictionaryModel.Lefts` / `Rights` / `AsReverse()` |
| 6 | 6.7.1 `ReverseMultiDictionary` 复用模型 | `ToSerializableModel()` / `FromModel` |

## 关键 API

- `MultiList<T>.ToSerializableModel()` / `MultiList<T>.FromModel(model, comparer = null)` — bag 的快照与重建。
- `MultiDictionary<TKey,TValue>.ToSerializableModel()` / `FromModel(model, comparer = null, allowDuplicateValues = true)` — multimap 的快照与重建。
- `MultiListModel<T>.Items` / `Counts` — 两条平行列表；`MultiDictionaryModel<TKey,TValue>.Keys` / `Values` — 键与每键值列表。
- `BiDictionaryModel<TLeft,TRight>.Lefts` / `Rights` — 双射专用模型（6.7.1）。
- `BiDictionary<TLeft,TRight>.ToSerializableModel()` / `FromModel` — 双射往返。
- `ReverseMultiDictionary<V,K>.ToSerializableModel()` / `FromModel(model, comparer = null)` — 索引轴（值）扮演模型的 `Keys`（6.7.1）。

## 注意事项 / 陷阱

- **比较器不进模型**：`FromModel` 必须再传（或用默认）；重建后 `Contains` 行为可能变化。
- **`FromModel` 校验严格**：列表 `null`、两列表长度不一致、副本数 ≤ 0、内层列表 `null` 都抛 `ArgumentException`。
- **`ToDictionary` 含 `null` 元素时抛** `InvalidOperationException`（`null` 不能作字典键）；需要 `null` 用模型。
- **模型是快照不是活视图**：`MultiDictionaryModel.Values` 的内层是独立拷贝。
- **`ReverseMultiDictionary` 的存储键是 set**：重建时内层顺序不保留、重复键会塌缩。
