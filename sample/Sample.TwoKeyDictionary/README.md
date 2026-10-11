# Sample.TwoKeyDictionary

> **两键复合字典 TwoKeyDictionary&lt;K1,K2,V&gt;** — 二维字典 + 双轴投影（F29，`DotNetCore.Collections.Multi`，6.2.0；6.7.1 / 6.8.0 增强）。

## 本示例演示什么

`TwoKeyDictionary<K1,K2,V>` 的键为两个**异构**分量，映射到单个 `V`。它是 `MultiKeyDictionary`（arity 2）的薄人体工学包装：二维索引器 `this[k1, k2]` + 每轴投影 `GetByFirstKey` / `GetBySecondKey`。两分量可不同类型，故分量装箱进 `object` 交给同质 trie。示例展示双轴投影、第二轴反向索引（F09）、按第二轴批量删除，以及 6.7.1 新增的 `Values` / `ContainsValue` / `AsReverse` 与 6.8.0 新增的 `EntrySet()`。

## 什么时候用

- 二维查找表（汇率表、矩阵坐标、(用户, 角色) 等）。
- 需要**按行 / 按列投影**（`GetByFirstKey` / `GetBySecondKey`）。
- 不该用：一键多值 → `MultiDictionary` / `MultiKeyMultiDictionary`；同质键零装箱热点 → `MultiKeyDictionary`；三键 → `ThreeKeyDictionary`。

## 如何运行

```bash
dotnet run --project sample/Sample.TwoKeyDictionary
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 二维读写与双轴投影 | `this[k1,k2]` / `GetByFirstKey` / `GetBySecondKey` / `CountOfSecondKey` / `ContainsSecondKey` |
| 2 | 按第二轴批量删除 | `RemoveBySecondKey` |
| 3 | 6.7.1 增强 | `Values` / `ContainsValue` / `AsReverse` |
| 4 | 6.8 EntrySet | `EntrySet()` |

## 关键 API

- `TwoKeyDictionary<K1,K2,V>.this[K1,K2]` — 二维索引器；读缺键抛 `KeyNotFoundException`，写覆盖。
- `GetByFirstKey(K1)` — 第一轴前缀切片（trie 下降）。
- `GetBySecondKey(K2)` / `CountOfSecondKey(K2)` / `ContainsSecondKey(K2)` — 第二轴切片（反向索引，O(1) 起）。
- `RemoveBySecondKey(K2)` — 删掉该第二键的全部条目，返回删除数。
- `Values` — 6.7.1：值扁平序列；`ContainsValue(V)` — 6.7.1：全扫描 O(n)。
- `AsReverse()` — 6.7.1：轴对调拷贝 `TwoKeyDictionary<K2,K1,V>`；原第二轴成为第一轴。
- `EntrySet()` — 6.8：`(K1,K2,V)` 逐条投影（`Entries` 已过时）。

## 注意事项 / 陷阱

- **分量装箱**：异构分量进 `object` trie；同质键热点用 `MultiKeyDictionary`。
- **`Keys2` 不排序**：走 trie 首次出现顺序，不是字典序。
- **一键一值**：赋值覆盖语义；不是多重映射。
- **`Entries` 已过时**（6.8 起）：仍可编译但产生 CS0618 警告，改用 `EntrySet()`。
