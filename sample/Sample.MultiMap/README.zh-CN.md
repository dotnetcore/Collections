# Sample.MultiMap

> **中文** | [English](README.md)

> **多重映射 MultiDictionary&lt;TKey,TValue&gt;** — 一个键关联多个值（multimap）（`DotNetCore.Collections.Multi`，6.0.0）。

## 本示例演示什么

`MultiDictionary<TKey,TValue>` 把「一键多值」做成一等语义，单次增删查一对 `(key, value)` 为 O(1)。
这个 sample 展示四个侧面：加值与计数读取、批量添加与按键集合运算、以及最容易被误解的 `Values` 语义。
重点纠正两个高频误解——**`Count` 是键数不是值总数**、**`Values` 是扁平序列不是按键分组**。

## 什么时候用

- 标签 / 分组 / 一对多索引：一键多值即一等语义，不必再包 `Dictionary<TKey, List<TValue>>`。
- 缺键查询频繁：索引器缺键返回空集合，调用方少写 try/catch。
- 需要 O(1) 的值存在性：`ContainsValue` 走反向索引（见 6.2.0）。
- 不该用：需要键有序 → `OrderedMultiDictionary<TKey,TValue>`（6.2.0）；严格一对一 → `BiDictionary<TLeft,TRight>`（6.3.0）；
  复合键 → `MultiKeyDictionary` / `TwoKeyDictionary`（6.4.0 / 6.2.0）。

## 如何运行

```bash
dotnet run --project sample/Sample.MultiMap
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 加值、读计数、缺键友好 | `Add` / `Count` / `KeyCount` / `TotalValueCount` / `ValueCount` / 索引器 / `ContainsKey` |
| 2 | 批量添加与按键集合运算 | `AddRange` / `UnionWith` / `RemoveRange` / `ExceptWith` / `Contains` |
| 3 | `Values` 扁平序列 vs 按键分组 | `Values` / `ToDictionary()` |

## 关键 API

- `MultiDictionary<TKey,TValue>.Add(TKey, TValue)` — 加一对；`null` 键抛 `ArgumentNullException`，`null` 值允许。
- `Count` / `KeyCount` — 都是**键数**；`TotalValueCount` — 全部值数；`ValueCount(key)` — 该键的值数（缺键 0）。
- `this[TKey]` — 该键的值集合；缺键返回空集合（永不 `null`、永不抛）。
- `AddRange(key, values)` — 批量加值；`RemoveRange(key, values)` — 对参数去重后每个不同值删一次出现（返回是否删到）。
- `UnionWith` / `IntersectionWith` / `ExceptWith` / `SymmetricExceptWith` — 对某个键的值集合做 set 语义运算。
- `Values` — **扁平**的全部值序列；`ToDictionary()` — 「键 -> 值集合」快照。

## 注意事项 / 陷阱

- **`Count` 是键数，不是值总数**：加了 3 个值、2 个键时 `Count == 2`，值总数用 `TotalValueCount`。
- **缺键索引器返回空集合而非抛**：`map[key].Count` 为 0 不代表键存在，需区分时用 `ContainsKey` / `TryGetValue`。
- **`Values` 是扁平序列**（`new` 隐藏了 `IReadOnlyDictionary.Values`）：`foreach` 得到的是单个值，按组请用 `ToDictionary()` / `AsLookup()`。
- **无值之键不存在**：最后一个值被删后键自动消失，`KeyCount` 与 `TotalValueCount` 不会出现「空键」分歧。
- **非线程安全**：并发请用 `ConcurrentMultiDictionary<TKey,TValue>`（6.2.0）。
