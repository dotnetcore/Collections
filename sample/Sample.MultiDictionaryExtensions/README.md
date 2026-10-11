# Sample.MultiDictionaryExtensions

> **从既有源构建多重映射** — `IndexBy` / `UniqueIndexBy` / `AsMultiDictionary`（`DotNetCore.Collections.Multi`，6.8.0）。

## 本示例演示什么

6.8.0 为多重映射补上了「从序列 / 从字典」的入口：`IndexBy(keySelector)` 把序列分组为 `MultiDictionary`
（保留重复，list 语义）；`UniqueIndexBy(keySelector)` 把序列索引为 `BiDictionary`（键必须唯一，重复即抛）；
`AsMultiDictionary()` 把 `IReadOnlyDictionary` 适配成 `MultiDictionary`（每键恰好一个值）。
这个 sample 演示三个入口各自的语义，以及它们共同的约定——**返回值都是独立副本，不是活视图**。

## 什么时候用

- 从内存中的一组对象建立一对多索引：`IndexBy`，例如按首字母 / 国家 / 类型分组。
- 建立严格的一对一查找表：`UniqueIndexBy`，把「键必须唯一」变成入口处的校验。
- 把既有 `Dictionary<TKey,TValue>` 升级为可多值的 multimap：`AsMultiDictionary`。
- 不该用：需要结果随源实时变化 → 三个入口都是副本，请自行维护刷新。

## 如何运行

```bash
dotnet run --project sample/Sample.MultiDictionaryExtensions
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 序列按键分组（保留重复） | `IndexBy(TSource, TKey)` / 带比较器重载 |
| 2 | 序列唯一索引（键重复即抛） | `UniqueIndexBy` / `AsReverse()` |
| 3 | 字典适配为 multimap（独立副本） | `AsMultiDictionary()` |

## 关键 API

- `IEnumerable<TSource>.IndexBy(keySelector)` / `IndexBy(keySelector, keyComparer)` — 返回 `MultiDictionary<TKey, TSource>`，保留重复（list 语义）。
- `IEnumerable<TSource>.UniqueIndexBy(keySelector)` / `UniqueIndexBy(keySelector, keyComparer)` — 返回 `BiDictionary<TKey, TSource>`；键重复抛 `ArgumentException`。
- `IReadOnlyDictionary<TKey,TValue>.AsMultiDictionary()` — 返回 `MultiDictionary<TKey, TValue>`，每键恰好一个值。

## 注意事项 / 陷阱

- **都是独立副本，不是活视图**：改动结果不影响源；源的变化也不会反映到结果，需最新数据要重新调用。
- **`IndexBy` 保留重复**：同一键下重复元素照单全收（list 语义），与 set 语义的索引不同。
- **`UniqueIndexBy` 因双射约束更严格**：键重复抛；两个元素本身相等也会抛（`BiDictionary` 严格一对一）。
- **`AsMultiDictionary` 的比较器带过来有条件**：源是 `Dictionary<TKey,TValue>` 时沿用其 `Comparer`，其它实现用 `EqualityComparer<TKey>.Default`（`IReadOnlyDictionary` 不暴露比较器）。
