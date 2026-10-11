# Sample.ImmutableVariants

> **中文** | [English](README.md)

> **不可变变体** — `ImmutableMultiList<T>` / `ImmutableMultiDictionary<TKey,TValue>`：突变返回新实例，实例永不变（对应 feature F13，`DotNetCore.Collections.Multi`，6.2.0）。

## 本示例演示什么

不可变多重集合 / 多重映射把「看似突变的操作返回新实例」做成一等语义：`Add` / `Remove` 不修改接收方，
而是交回一个新实例，旧实例继续可用。因为实例永不变，任意线程可**无锁并发读**——这不是「加了锁」，
而是「不需要锁」。示例展示三个侧面：突变返回新实例且源不变、Builder 的结构共享（无写时 `ToImmutable`
返回源实例本身）、以及不可变映射的键数 / 值数口径。

## 什么时候用

- 多读者共享同一份快照，读多写少，需要天然线程安全。
- 配置 / 快照冻结：构造后不想再被任何代码改动。
- 需要版本回溯：旧实例仍保留，可回滚。
- 不该用：热点可变计数（用可变 `MultiList<T>` + 同步）；超大结构单点改（首次写整表 COW 成本高）。

## 如何运行

```bash
dotnet run --project sample/Sample.ImmutableVariants
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 突变返回新实例，源不变 | `ImmutableMultiList<T>.Add(T)` / `CountOf` / `TotalCount` / `DistinctCount` |
| 2 | Builder 结构共享与 copy-on-write | `ToBuilder()` / `Builder.Add` / `Builder.ToImmutable()` |
| 3 | 不可变多重映射 | `ImmutableMultiDictionary<TKey,TValue>.Add` / `KeyCount` / `TotalValueCount` / 索引器 / `ToDictionary()` |

## 关键 API

- `ImmutableMultiList<T>` — 真不可变多重集合；`Add(T)` / `Add(T,int)` / `Remove(T,int)` / `RemoveAllCopies(T)` 均返回**新实例**。
- `ImmutableMultiList<T>.ToBuilder()` — 返回 `Builder`，**不拷贝**内部状态；builder 无写时 `ToImmutable()` 返回源实例本身（`ReferenceEquals` 可断言）。
- `ImmutableMultiList<T>.Count` — 副本数（等于 `TotalCount`）；`DistinctCount` 才是不同元素数。
- `ImmutableMultiDictionary<TKey,TValue>` — 真不可变多重映射；`Add` / `AddRange` / `Remove` 返回新实例。
- `ImmutableMultiDictionary<TKey,TValue>.Count` — **键数**（等于 `KeyCount`），`TotalValueCount` 才是值数。

## 注意事项 / 陷阱

- **突变返回新实例，别忽略返回值**：`seed.Add(...)` 不改 `seed`，要 `seed = seed.Add(...)` 或用 Builder。
- **本包没有 `ImmutableMultiList<T>.Empty`**：用公开无参构造器 `new ImmutableMultiList<T>()` 起步。
- **无写 Builder 的 `ToImmutable` 可能是源实例**：引用相等是刻意保留的结构共享，不要假设它总是新对象。
- **首次写整表 COW**：链式大量 `Add` 不用 Builder 会造成大量分配；批量请 `ToBuilder`。
