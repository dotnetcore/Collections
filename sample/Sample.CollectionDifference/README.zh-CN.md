# Sample.CollectionDifference

> **中文** | [English](README.md)

> **四路差异比较 CollectionDifference&lt;TKey,TValue&gt;** — 把两个 bag / 两个字典切成四个不相交分区（6.9.0，`DotNetCore.Collections.Multi`，6.9.0）。

## 本示例演示什么

`left.Difference(right)` 一次性回答「这两个集合之间变了什么」，并把答案切成四个**互斥且覆盖全部键**的分区：
`OnlyInLeft`（只在左边）、`OnlyInRight`（只在右边）、`InCommon`（两边都有且值相等）、`Differing`（两边都有但值不同，
映射为 `(Left, Right)` 一对）；`AreEqual` 是「什么都没变」的快捷判断。两个多重集按**副本数**比较（哪些元素被加 /
删 / 改了重数），两个 `IReadOnlyDictionary` 按**映射值**比较（哪些键被加 / 删 / 改了值），分区形状完全一致。

## 什么时候用

- 对账 / 差分：比对两份快照、两次统计结果，快速定位新增、消失、变化的条目。
- 需要「变化明细」而不是只想知道「是否相等」——`AreEqual` 只是它的一种退化用法。
- 不该用：需要**活视图**（持续反映后续改动）——本库只提供只读视图 / 快照 / 就地三种形态，`Difference` 是**快照**；
  只想知道两个多重集是否逐份相等 → 直接用 `AreEqual`（或集合运算，F12）。

## 如何运行

```bash
dotnet run --project sample/Sample.CollectionDifference
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 两个 bag 按副本数四路分区 | `IMultiSet<T>.Difference(IMultiSet<T>)` → `CollectionDifference<T,int>` |
| 2 | 两个字典按映射值四路分区 | `IReadOnlyDictionary<K,V>.Difference(...)` → `CollectionDifference<K,V>` |
| 3 | 底层工厂、比较器与参数校验 | `CollectionDifference<TKey,TValue>.Of` / 键比较器 / `null` 与重复键抛 `ArgumentException` |

## 关键 API

- `CollectionDifference<TKey, TValue>.OnlyInLeft` / `OnlyInRight` / `InCommon` — `IReadOnlyDictionary<TKey,TValue>`。
- `CollectionDifference<TKey, TValue>.Differing` — `IReadOnlyDictionary<TKey,(TValue Left, TValue Right)>`。
- `CollectionDifference<TKey, TValue>.AreEqual` — 三个「差异」分区皆空时为 `true`。
- `IMultiSet<T>.Difference(IMultiSet<T>, IEqualityComparer<T>? = null)` — 按副本数比较，值类型为 `int`。
- `IReadOnlyDictionary<K,V>.Difference(..., keyComparer = null, valueComparer = null)` — 按值比较；多重映射以「每键的值集合」为值。
- `CollectionDifference<TKey,TValue>.Of(left, right, keyComparer = null, valueComparer = null)` — 任意键值序列的底层工厂。
- `ToString()` — 一行摘要 `left-only n, right-only n, in-common n, differing n`。

## 注意事项 / 陷阱

- **结果是快照**：构建后再改源集合，`Difference` 不会跟着变；本库不提供可写穿视图。
- **`null` 键无法表示**（四个分区都是字典），会抛 `ArgumentException`（`ParamName` 指明左右侧），与 `ToDictionary()` 同策。
- **同一侧键必须唯一**：重复键抛 `ArgumentException`，因为分区是按键索引的。
- **比较器由调用者提供**：`IMultiSet<T>` 不暴露其内部比较器，`Difference` 的 bag 重载需要你显式传入才算「同一元素」。
- **不可变、线程安全**：`CollectionDifference` 一旦构建即可跨线程共享。
