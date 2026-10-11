# Sample.ReadOnlyCollectionView

> **中文** | [English](README.md)

> **跨 TFM 只读值视图包装** — `MultiDictionary` 的只读值视图（F19，`DotNetCore.Collections.Multi`，6.4.0）。

## 本示例演示什么

包内有一个 `internal sealed` 的 `ReadOnlyCollectionView<T>`，把 `ICollection<T>` 暴露为 `IReadOnlyCollection<T>`。它解决一个具体的跨 TFM 危险：把内层集合（如 `HashSet<T>`）以**裸转型**暴露为 `IReadOnlyCollection<T>`，而框架的 `HashSet<T>` 在 net451 / net461 上既不静态声明也不在运行时实现该接口，裸转型恰好在这些受支持目标上抛 `InvalidCastException`。本示例**只演示用户可见面**（索引器 / `AsReadOnly`），说明修复后的行为：全 TFM 可读、活视图、零拷贝。

## 什么时候用

- 使用方无直接场景；本篇帮助理解「为何 net451 上只读值视图曾经 / 不再抛」。
- 功能使用见 F02（`MultiDictionary`）/ F18（`AsReverse`）。
- 不该用：在示例 / 文档里直接引用 `ReadOnlyCollectionView`（它是 `internal`，编译不过）。

## 如何运行

```bash
dotnet run --project sample/Sample.ReadOnlyCollectionView
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 只读值视图（内层 `HashSet`） | `new MultiDictionary<TKey,TValue>(allowDuplicateValues: false)` / `this[key]` |
| 2 | 活视图（不是快照） | `this[key]` 的 `Count` / 枚举随源变化 |
| 3 | `AsReadOnly` / `ToDictionary` 内层视图 | `AsReadOnly()` / `ToDictionary()` |
| 4 | 跨 TFM 说明与陷阱 | 只用 `IReadOnlyCollection<T>`，不引用内部类型 |

## 关键 API

- `MultiDictionary<TKey,TValue>(bool allowDuplicateValues)` — `false` 时内层为 `HashSet<TValue>`。
- `MultiDictionary<TKey,TValue>.this[TKey]` — 返回 `IReadOnlyCollection<TValue>`，经包装暴露（活视图）。
- `MultiDictionary<TKey,TValue>.AsReadOnly()` — 整张 map 的活只读视图。
- `MultiDictionary<TKey,TValue>.ToDictionary()` — 外层快照，内层值集合仍是活视图。

## 注意事项 / 陷阱

- **内部类型**：`ReadOnlyCollectionView<T>` 是 `internal`，不要写进示例 / 公开文档当 API。
- **活视图非快照**：改 map 后视图内容跟着变；要冻结用 `ToSerializableModel`（F10）。
- **问题域是 net451 / net461**：高 TFM 上 `HashSet` 已实现该接口，但包内统一包装以避免 `#if` 分叉。
- **`ToDictionary` 的外层是快照、内层是活视图**：两者语义不同，勿混。
