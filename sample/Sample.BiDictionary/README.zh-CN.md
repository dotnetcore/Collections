# Sample.BiDictionary

> **中文** | [English](README.md)

> **严格双射字典 BiDictionary&lt;TLeft,TRight&gt;** — 一对一映射，双向 O(1)（`DotNetCore.Collections.Multi`，6.3.0）。

## 本示例演示什么

`BiDictionary<TLeft,TRight>` 是**严格一对一（双射）字典**：每个 `TLeft` 恰绑一个 `TRight`，且没有 `TRight` 被两个 `TLeft` 共享，双向都是 O(1) 查找。它实现 `IReadOnlyDictionary<TLeft,TRight>`（正向）与 `IBiMap`（含反向成员）。示例展示双向查找、严格冲突处理（`Add` 抛 / `TryAdd` 返 false / 无覆盖 setter）、`AsReverse` 活视图，以及 null 左值在 `ToDictionary` 与序列化模型中的差异。

## 什么时候用

- ID ↔ 显示名、编码 ↔ 枚举等**双向查**场景。
- 需要双射约束**快速失败**，不希望静默丢绑定。
- 不该用：一对多 → `MultiDictionary`；复合键 → `MultiKeyDictionary`。

## 如何运行

```bash
dotnet run --project sample/Sample.BiDictionary
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 双向查找 | `this[left]` / `GetLeft(right)` / `Count` |
| 2 | 严格冲突处理 | `Add`（冲突抛）/ `TryAdd`（返 false）/ `Remove` / `RemoveRight` |
| 3 | `AsReverse` 活视图 | `AsReverse()` |
| 4 | null 左值与导出 | `ToDictionary` / `ToSerializableModel` / `FromModel` |

## 关键 API

- `BiDictionary<TLeft,TRight>.this[TLeft]` — 正向查右值；缺键抛 `KeyNotFoundException`。
- `GetLeft(TRight)` / `TryGetLeft(TRight, out TLeft)` / `ContainsRight(TRight)` — 反向查。
- `Add(TLeft, TRight)` — 左值已有、或右值已绑别的左值时抛 `ArgumentException`（**无静默覆盖**）。
- `TryAdd(TLeft, TRight)` — 冲突返回 `false`，供判定而非捕获。
- `Remove(TLeft)` / `RemoveRight(TRight)` — 显式解绑，之后才能重绑。
- `AsReverse()` — 反向（右 → 左）的**活**只读视图。
- `ToDictionary()` — 快照；**省略** null 左值条目。`ToSerializableModel()` / `FromModel` — 并行列表模型，**保留** null 左值。

## 注意事项 / 陷阱

- **无覆盖 setter**：破坏既有绑定必须先显式 `Remove` / `RemoveRight`。
- **`Add` 冲突抛 `ArgumentException`**：偏好判定用 `TryAdd`。
- **`ToDictionary` 省略 null 左值条目**（null 不能作字典键）；要保留用 `ToSerializableModel`。
- **`FromModel` 拒绝非双射**：左值重复或右值冲突都会抛 `ArgumentException`。
