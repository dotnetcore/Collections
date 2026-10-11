# Sample.SharedInterfaces

> **中文** | [English](README.md)

> **共享接口** — `IMultiSet<T>` / `IMultiDictionary<TKey,TValue>`：多重集合 / 多重映射的共享读写抽象（对应 feature F27，`DotNetCore.Collections.Multi`，6.6.0）。

## 本示例演示什么

`IMultiSet<T>` 与 `IMultiDictionary<TKey,TValue>` 只编码每个实现都能兑现的语义，使代码可针对接口编写，
便于 mock、替换与互换实现。示例展示三个侧面：面向 `IMultiSet` 的算法（同一函数收 `MultiList` 与
`OrderedMultiList`）、映射的 `Count` 与 `Values` 语义、以及刻意最小的契约范围。重点是纠正两处高危口径：
`IMultiSet.Count` 是**副本数**、`IMultiDictionary.Values` 是**扁平**序列。

## 什么时候用

- 服务接收多重集合 / 映射：依赖抽象，便于替换实现。
- 单测 mock：接口可替身。
- 跨实现复用算法：仅用契约内的最小语义。
- 不该用：需要集合运算 / 比较器——那些不在契约内，落回具体类型。

## 如何运行

```bash
dotnet run --project sample/Sample.SharedInterfaces
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 面向 `IMultiSet` 的算法 | `IMultiSet<T>.Count` / `TotalCount` / `DistinctCount` / `EntrySet()` |
| 2 | 映射 `Count` 与 `Values` | `IMultiDictionary<TKey,TValue>.Count` / `KeyCount` / `TotalValueCount` / 索引器 / `Values` / `ValueCount` |
| 3 | 契约范围（最小） | 集合运算落具体类型；「无空键」不变式 |

## 关键 API

- `IMultiSet<T>`（继承 `IReadOnlyCollection<T>`）— `Count` = **副本数**（恒等于 `TotalCount`）；`DistinctCount` 才是不同元素数。
- `IMultiSet<T>.EntrySet()` / `Contains` / `ContainsAll` / `CountOf` / `Remove` / `RemoveAllCopies` / `DistinctItems`。
- `IMultiDictionary<TKey,TValue>`（继承 `IReadOnlyDictionary<TKey,IReadOnlyCollection<TValue>>`）— `Count` = **键数**（恒等于 `KeyCount`）。
- `IMultiDictionary<TKey,TValue>.Values` — 用 `new` 隐藏继承的按组 `Values`，给出**扁平**序列（长度 = `TotalValueCount`）。
- `IMultiDictionary<TKey,TValue>.KeyCount` / `TotalValueCount` / `ValueCount(TKey)` / `AsLookup()`。

## 注意事项 / 陷阱

- **`IMultiSet.Count` = 副本数**，不是 `DistinctCount`：3 份 1 元素时 `Count == 3`。
- **`IMultiDictionary.Count` = 键数**，不是值数；`TotalValueCount` 才是值数。
- **`Values` 是 `new` 扁平序列**：不是按组集合序列；按组读用索引器 `map[key]` / `Keys`。
- **集合运算 / 比较器不在接口上**：并发 / 不可变 / `PackedBag` 并不都提供集合运算。
- **`IMultiSet` 不继承 `ICollection<T>`**：`int Remove(T)` 删一份并报告剩余副本数，不是 `bool Remove`。
- **接口故意不双继承枚举形**：避免 `foreach` 对接口二义（`CS0411`）。
