# Sample.MultiKeyMultiDictionary

> **中文** | [English](README.md)

> **复合键多重映射 MultiKeyMultiDictionary&lt;TKey,TValue&gt;** — 复合键 + 一键多值（F20，`DotNetCore.Collections.Multi`，6.4.0；6.8.0 增强）。

## 本示例演示什么

`MultiKeyMultiDictionary<TKey,TValue>` 的键是同质分量序列（`TKey[]`），每个完整键映射到**值集合**。它是 `MultiKeyDictionary`（N 分量 → 1 值）与 `MultiDictionary`（1 键 → N 值）的组合。存储是键分量上的 trie；每个完整键持可配置工厂建的值集合（默认 `List` 保留重复）。示例展示复合键多值的计数口径、**前缀投影**（trie 继承的能力），以及 6.8.0 新增的 `EntrySet()`。

## 什么时候用

- 层次路径 + 多标签（复合键 + 一键多值）。
- 需要**前缀查询 / 前缀删除**（`GetByPrefix` / `RemovePrefix`），这是元组键 `Dictionary` 做不到的。
- 不该用：固定异构二 / 三键 → `TwoKey` / `ThreeKey`；单键热点 O(1) → `MultiDictionary`。

## 如何运行

```bash
dotnet run --project sample/Sample.MultiKeyMultiDictionary
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 复合键多值（Count = 完整键数） | `Add` / `Count` / `TotalValueCount` / `this[key]` |
| 2 | 前缀投影 | `CountOfPrefix` / `GetByPrefix` / `GetBranches` |
| 3 | 删整棵子树 | `RemovePrefix` |
| 4 | 6.8 EntrySet | `EntrySet()` |

## 关键 API

- `MultiKeyMultiDictionary<TKey,TValue>.Add(TKey[], TValue)` — 单对增删 = trie 走位 O(键长) + 集合操作。
- `Count` / `KeyCount` — **完整键数**；`TotalValueCount` — 值总数。
- `this[TKey[]]` — 该完整键下的值集合；缺键返回空集合，不抛。
- `GetByPrefix(TKey[])` / `CountOfPrefix(TKey[])` / `GetBranches(TKey[])` — 前缀投影（子树遍历）。
- `RemovePrefix(TKey[])` — 删整棵子树，返回删除的**值**数。
- `EntrySet()` — 6.8：`(TKey[] Key, TValue Value)` 扁平投影，一值一对。

## 注意事项 / 陷阱

- **`Count` 是完整键数**，不是值数：值总数用 `TotalValueCount`。
- **缺键索引器返回空集合**，不抛。
- **完整键 O(键长) 而非 O(1)**：trie 走位；要元组键 O(1) 用 `MultiDictionary` + 元组键（失去前缀）。
- **`RemovePrefix` 返回的是值数**，不是键数；需要键数先 `CountOfPrefix`。
