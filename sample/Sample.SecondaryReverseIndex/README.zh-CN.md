# Sample.SecondaryReverseIndex

> **中文** | [English](README.md)

> **第二轴反向索引** — `TwoKeyDictionary&lt;K1,K2,V&gt;` 的第二轴查询（F09，`DotNetCore.Collections.Multi`，6.2.0）。

## 本示例演示什么

`TwoKeyDictionary<K1,K2,V>` 的底层 trie 按 `(K1, K2)` 顺序索引，因此只有**第一轴**是前缀、可用子树下降解析；第二轴不是前缀，无法靠树走位解决。本特性在 trie 旁维护一张**第二轴反向索引**（`K2 → HashSet<K1>`），把 `GetBySecondKey` / `CountOfSecondKey` / `ContainsSecondKey` / `RemoveBySecondKey` 从全表扫描变成一次查找。索引在每条写路径上同步维护。

## 什么时候用

- 二维表**按列切片**（按第二键取切片），不想扫全表。
- 按第二键**批量删除**（`RemoveBySecondKey` 一次清掉该第二键的全部条目）。
- 不该用：只按完整键查（无需此索引）；需要三键的第二 / 三轴切片（换键序或 `MultiKeyDictionary`）。

## 如何运行

```bash
dotnet run --project sample/Sample.SecondaryReverseIndex
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 第二轴切片（一次查找） | `GetBySecondKey` / `CountOfSecondKey` / `ContainsSecondKey` |
| 2 | 按第二轴批量删除 | `RemoveBySecondKey` |
| 3 | `Keys2` 顺序与 null 第二分量 | `Keys2` / `ContainsSecondKey(null)` |

## 关键 API

- `TwoKeyDictionary<K1,K2,V>.GetBySecondKey(K2)` — 第二轴切片，经反向索引定位，O(1) 起。
- `TwoKeyDictionary<K1,K2,V>.CountOfSecondKey(K2)` — 该第二键下的条目数。
- `TwoKeyDictionary<K1,K2,V>.ContainsSecondKey(K2)` — 是否存在该第二键（O(1)）。
- `TwoKeyDictionary<K1,K2,V>.RemoveBySecondKey(K2)` — 删掉该第二键的全部条目，返回删除数。
- `TwoKeyDictionary<K1,K2,V>.Keys2` — 第二轴投影；刻意走 trie，保留**首次出现**顺序。

## 注意事项 / 陷阱

- **索引是内部实现**，不是公开类型；不要写进示例期望编译。
- **`Keys2` 不排序**：顺序是首次出现，不是字典序；需要有序请自行排序。
- **写路径多一次字典操作**：纯完整键查的负载可考虑 `MultiKeyDictionary`。
- **`null` 第二分量走专用桶**（`Dictionary` 拒 null 键），与 `MultiList` / `MultiKeyDictionary` 的 null 桶策略一致。
