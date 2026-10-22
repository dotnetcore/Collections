# Sample.Engine.Polyfills

> **中文** | [English](README.md)

> **十九个 polyfill 算子一次走完** — 引擎在低 TFM 上补齐 BCL 的版本差异，并验证与 BCL 逐字一致（`DotNetCore.Collections`，7.2.0）。

## 本示例演示什么

`7.2.0` 给引擎加了十九个算子，分两层：**P0 层 13 个**是 BCL 在 .NET 6 ~ .NET 10 之间陆续补上的（`DistinctBy` / `ExceptBy` / `IntersectBy` / `UnionBy` / `MaxBy` / `MinBy` / `Chunk` / `Order` / `OrderDescending` / `CountBy` / `AggregateBy` / `LeftJoin` / `RightJoin`），
**P1 层 6 个算子组**是 BCL 任何版本都没有的引擎自有算子（`DistinctUntilChanged` / `Memoize` / `FallbackIfEmpty` / `AtLeast`+`Exactly`+`CountBetween` / `PartialSortBy` / `Choose`）。

本示例分九段把十九个算子全部跑一遍，最后一段做**逐项对拍**：在 `net8.0` 上 BCL 已自带其中 9 个算子，引擎的答案必须与 `System.Linq` 的答案逐字相同——这是 polyfill 层存在的意义，低 TFM 的用户不应该能察觉差别。
程序是断言式的：任何一项不一致就返回非零退出码。

## 什么时候用

- 你在 `net451` / `netstandard2.0` 这类低 TFM 上，想要 `DistinctBy` / `Chunk` / `CountBy` 这些 .NET 6+ 才有的算子。
- 你在写跨 TFM 的库，希望同一份源码在 `net451` 到 `net10.0` 上编译通过、行为一致。
- 你想知道哪些算子 BCL 已经有了（不该自己再写一遍），哪些是引擎独有的。
- 不该用：只在 `net10.0` 上运行、且不打算向下兼容 ⇒ 直接用 BCL 即可，无需引入引擎。

## 如何运行

```bash
dotnet run --project sample/Sample.Engine.Polyfills
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 键控集合四件套：按键去重 / 排除 / 相交 / 合并 | `DistinctBy` / `ExceptBy` / `IntersectBy` / `UnionBy` |
| 2 | 极值返回元素本身；分批每块是新数组 | `MaxBy` / `MinBy` / `Chunk` |
| 3 | 无键排序，返回 `IOrderedEnumerable<T>` 可继续 `ThenBy` | `Order` / `OrderDescending` |
| 4 | 一步完成分组计数；共享 seed 与 seed 工厂的区别 | `CountBy` / `AggregateBy` |
| 5 | 外连接保留整侧，未匹配侧为 `default` | `LeftJoin` / `RightJoin` |
| 6 | 只去相邻重复；缓存首次枚举结果 | `DistinctUntilChanged` / `Memoize` |
| 7 | 空序列回退；三个计数谓词都短路 | `FallbackIfEmpty` / `AtLeast` / `Exactly` / `CountBetween` |
| 8 | O(n log k) 取前 k 名；过滤与投影合成一步 | `PartialSortBy` / `Choose` |
| 9 | 与 BCL 逐项对拍，答案必须逐字相同 | 上述 P0 算子 vs `System.Linq` |

## 关键 API

- `ToValueEnumerable()` — 四个入口：`T[]` / `List<T>` / `IReadOnlyList<T>` / `IEnumerable<T>`，各自返回对应的值类型包装。
- `DistinctBy(keySelector[, comparer])` — 每个键只保留首次出现的元素。
- `ExceptBy(keys, keySelector[, comparer])` — 第二个参数是**键集合**；结果**按键去重**。
- `IntersectBy(keys, keySelector[, comparer])` — 同上，且每个键最多匹配一次（用 `Remove` 消费）。
- `UnionBy(second, keySelector[, comparer])` — 第二个参数是**元素序列**，两侧按键去重、先左后右。
- `MaxBy` / `MinBy(keySelector[, comparer])` — 返回元素本身，不是键。
- `Chunk(size)` — 每块是新建的数组，尾块可以不满且不补默认值。
- `Order` / `OrderDescending([comparer])` — 返回 BCL 的 `IOrderedEnumerable<T>`，可继续 `ThenBy`。
- `CountBy(keySelector[, comparer])` — 返回 `KeyValuePair<TKey,int>` 序列。
- `AggregateBy(keySelector, seed, func[, comparer])` — 所有键共享同一个 seed 实例。
- `AggregateBy(keySelector, seedFactory, func[, comparer])` — 每个键各拿一个新累加器。
- `LeftJoin` / `RightJoin(inner, outerKey, innerKey, resultSelector[, comparer])` — 未匹配侧为 `default`。
- `DistinctUntilChanged([comparer])` — 只去掉**相邻**重复。
- `Memoize()` — 首次枚举结果被缓存，第二次不再回到源。
- `FallbackIfEmpty(T)` / `FallbackIfEmpty(IEnumerable<T>)` — 仅在空时替换。
- `AtLeast(n)` / `Exactly(n)` / `CountBetween(min, max)` — 短路计数谓词，两端闭区间。
- `PartialSortBy(count, keySelector[, descending])` — 返回已物化的 `List<T>`。
- `Choose(chooser)` — `chooser` 返回 `(bool Keep, TResult Value)`，Keep 为 false 时既不产出也不投影。

## 注意事项 / 陷阱

- **`ExceptBy` / `IntersectBy` 会按键去重**，不是「保留所有符合条件的元素」。同一个键出现两次时，第二个元素不会出来——这与直觉相反，但与 BCL 完全一致（源码里 `ExceptBy` 用 `set.Add`、`IntersectBy` 用 `set.Remove`）。
- **`AggregateBy` 的共享 seed 对引用类型是陷阱**：`AggregateBy(key, new List<T>(), ...)` 会让所有键共用一个 `List`，结果被串成一锅。需要独立累加器时用 **seed 工厂**重载。
- **`MaxBy` / `MinBy` 在空序列上的答案取决于元素类型**：引用类型返回 `null`，值类型抛 `InvalidOperationException`。这是 BCL 自己的口径，polyfill 必须照抄。
- **`Chunk` 的尾块不补齐**：`{1,2,3,4,5}.Chunk(2)` 的第三块是长度 1 的数组，不是 `[5,0]`。
- **`LeftJoin` / `RightJoin` 只在 `net451` ~ `net9.0` 上由引擎实现**；`net10.0` 及以上引擎直接转发 BCL，避免与平台竞争。
