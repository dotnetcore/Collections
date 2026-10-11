# Sample.ThreeKeyDictionary

> **三键复合字典 ThreeKeyDictionary&lt;K1,K2,K3,V&gt;** — 键为三个异构分量的复合键字典（F15，`DotNetCore.Collections.Multi`，6.2.0；6.7.1 / 6.8.0 增强）。

## 本示例演示什么

`ThreeKeyDictionary<K1,K2,K3,V>` 是 `MultiKeyDictionary`（arity 3）的薄人体工学包装：提供三元组索引器 `this[k1, k2, k3]` 与第一轴前缀查询。trie 按 `(K1,K2,K3)` 索引，因此**只有第一轴是前缀**（第二、三轴切片靠扫描，O(n)）。示例还展示 6.7.1 新增的 `Values` / `ContainsValue` / `AsReverse`，以及 6.8.0 新增的 `EntrySet()`。

## 什么时候用

- 三维配置 / 矩阵坐标，键为**异构**分量类型。
- 需要**按第一维切片**（`GetByFirstKey`，前缀下降）。
- 不该用：第二 / 三轴热点查询（换键序或用 `MultiKeyDictionary`）；一键多值（用 `MultiKeyMultiDictionary`）；同质键且在意装箱（直接用 `MultiKeyDictionary`）。

## 如何运行

```bash
dotnet run --project sample/Sample.ThreeKeyDictionary
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 三键读写与第一轴切片 | `this[k1,k2,k3]` / `GetByFirstKey` / `CountOfFirstKey` / `ContainsFirstKey` |
| 2 | 第二 / 三轴切片（扫描） | `GetBySecondKey` / `CountOfSecondKey` / `CountOfThirdKey` / `ContainsSecondKey` |
| 3 | 6.7.1 增强 | `Values` / `ContainsValue` / `AsReverse` |
| 4 | 6.8 EntrySet | `EntrySet()` |

## 关键 API

- `ThreeKeyDictionary<K1,K2,K3,V>.this[K1,K2,K3]` — 三元组索引器；读缺键抛 `KeyNotFoundException`，写覆盖。
- `GetByFirstKey(K1)` / `CountOfFirstKey(K1)` / `ContainsFirstKey(K1)` — 第一轴前缀查询（子树下降）。
- `GetBySecondKey` / `GetByThirdKey` / `CountOfSecondKey` / `CountOfThirdKey` — 第二 / 三轴切片（**扫描 O(n)**）。
- `Values` — 6.7.1：值扁平序列（丢弃键分量）；`ContainsValue(V)` — 6.7.1：全扫描 O(n)。
- `AsReverse()` — 6.7.1：轴反转拷贝 `ThreeKeyDictionary<K3,K2,K1,V>`；原第三轴成为第一轴。
- `EntrySet()` — 6.8：`(K1,K2,K3,V)` 逐条投影（`Entries` 已过时）。

## 注意事项 / 陷阱

- **只有第一轴是前缀**：第二 / 三轴切片 O(n)，热路径别当索引查。
- **分量装箱**：异构分量装箱进 `object` trie；同质键热点用 `MultiKeyDictionary`。
- **`ContainsValue` 是全扫描**（O(n)）：本类型无值侧索引。
- **`Entries` 已过时**（6.8 起）：仍可编译但产生 CS0618 警告，改用 `EntrySet()`。
