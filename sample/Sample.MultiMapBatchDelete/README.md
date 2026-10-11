# Sample.MultiMapBatchDelete

> **多重映射批量删除** — `MultiDictionary<TKey,TValue>.RemoveRange` 按键批量删值（F04，`DotNetCore.Collections.Multi`，6.1.0）。

## 本示例演示什么

`RemoveRange(TKey, IEnumerable<TValue>)` 是 `Remove(key, value)` 的批量形：对参数集合里**每个不同值**删掉键下一次出现。
这个 sample 展示三个侧面：批量删值与返回值语义、参数的集合（去重）语义、以及与 `ExceptWith` 的对照。
关键在于分清——`RemoveRange` 每个不同值只删**一次出现**，`ExceptWith` 才删掉匹配值的**全部出现**。

## 什么时候用

- 按标签 / 集合批量摘除一批值：一次调用删多个值，不必手写 `Remove` 循环。
- 差集式清理：与 `ExceptWith` 配合。
- 不该用：要删某值的全部出现 → `ExceptWith(key, values)`；要删整个键 → `Remove(key)`。

## 如何运行

```bash
dotnet run --project sample/Sample.MultiMapBatchDelete
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 批量删值、返回 `bool`、缺键空操作 | `RemoveRange(TKey, IEnumerable<TValue>)` |
| 2 | 参数的集合（去重）语义 | `RemoveRange`（参数先去重） |
| 3 | 与删全部出现对照 | `RemoveRange` vs `ExceptWith` |

## 关键 API

- `MultiDictionary<TKey,TValue>.RemoveRange(TKey, IEnumerable<TValue>)` — 对参数去重后每个不同值删一次出现；返回是否至少删掉一个。
- `MultiDictionary<TKey,TValue>.ExceptWith(TKey, IEnumerable<TValue>)` — 删掉匹配值的**全部**出现。
- `MultiDictionary<TKey,TValue>.ValueCount(TKey)` — 该键当前的值数，用于验证删除效果。

## 注意事项 / 陷阱

- **参数是集合语义**：参数里重复值不计两次，先 `DistinctValuesOf` 去重再逐个删。
- **允许重复值时只删 1 份**：`allowDuplicateValues: true` 时同值 N 份只删 1 份；要删全部用 `ExceptWith`。
- **命名为 `RemoveRange` 而非重载 `Remove`**：避免 `map.Remove(key, null)`（删存储的 `null` 值）产生 `CS0121` 歧义。
- **缺键 / 空参数 / 全不匹配都是返回 `false` 的空操作**，且不抛异常。
- **保持无空键不变式**：键下最后一个值被删后，键自动消失。
