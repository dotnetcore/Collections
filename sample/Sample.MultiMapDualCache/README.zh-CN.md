# Sample.MultiMapDualCache

> **中文** | [English](README.md)

> **多重映射双缓存** — 值总数缓存 + 值反向索引（`DotNetCore.Collections.Multi`，6.2.0）。

## 本示例演示什么

`MultiDictionary<TKey,TValue>` 内部维护两个缓存，把两类高频读从扫描变成 O(1)：
`_totalValueCount` 缓存全部值总数（`TotalValueCount` 读 O(1)），`_valueIndex`（值 → 键集）反向索引
让 `ContainsValue` 不再扫全表。这个 sample 展示三个侧面：O(1) 读的外显、`null` 值专用桶、
以及缓存如何随公开突变 API 同步（含 `Clone` 走 `Add` 重建）。

## 什么时候用

- 频繁读值总数：`TotalValueCount` 为 O(1)，观测与断言不再付 O(键) 扫描。
- 值存在性是热点：`ContainsValue` 为 O(1)，可进热路径。
- 活反查视图就绪：`AsReverse` 直接读反向索引（见 6.3.0）。
- 不该用：只写不读总数 → 缓存是几乎可忽略的纯开销。

## 如何运行

```bash
dotnet run --project sample/Sample.MultiMapDualCache
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | O(1) 读值总数与值存在性 | `TotalValueCount` / `ContainsValue` / `ValueCount` |
| 2 | `null` 值专用桶 | `Add(key, null)` / `ContainsValue(null)` |
| 3 | 缓存随公开突变 API 同步 | `AddRange` / `Remove` / `Clone` |

## 关键 API

- `MultiDictionary<TKey,TValue>.TotalValueCount` — 全部值数，O(1) 缓存读。
- `MultiDictionary<TKey,TValue>.ContainsValue(TValue)` — 值存在性，走反向索引 O(1)；`null` 走专用桶。
- `MultiDictionary<TKey,TValue>.Clone()` — 经公开 `Add` 重建，自带一致的双缓存。

## 注意事项 / 陷阱

- **缓存对使用方透明**：不要依赖「读总数会扫描」的旧行为；成本模型已变。
- **值相等固定为 `EqualityComparer<TValue>.Default`**：反向索引与内层集合一致，不接受自定义值比较器。
- **只用公开突变 API**：经自定义 `innerFactory` 拿到可变内层再改会破坏缓存一致性。
- **写路径多两处簿记**：`Add` 加 delta、`RemoveOccurrence` 减 1、`RemoveKeyCore` 减整桶——纯写入负载略有成本。
