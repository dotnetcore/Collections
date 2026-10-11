# Sample.SpanBag

> **中文** | [English](README.md)

> **栈上临时袋** — `SpanBag<T>`：栈上零堆分配的定容计数袋（对应 feature F22，`DotNetCore.Collections.Multi`，6.4.0）。

## 本示例演示什么

`SpanBag<T> where T : unmanaged` 是 `ref struct`：存储由调用方提供的 `Span<T>` / `Span<int>`（通常
`stackalloc`）**借用**，用于单方法内短命频次统计——零堆分配、无需 `Dispose`。示例展示三个侧面：
方法内找第一个重复值、定容与满容行为（新元素 `Add` 返回 `false`）、以及枚举 / 删除 / `Clear` 复用。
生命周期契约由编译器强制：`ref struct` 不可装箱、不可作字段、不可被 lambda 捕获、不可跨 `await`。

## 什么时候用

- 方法内重复检测 / 频次统计，要求零堆分配的热路径小直方图。
- 需要「内存随栈帧消失」的短命计数，不必手动释放。
- 不该用：需跨方法 / 跨 `await` 持有（用 `PackedBag<T>` / `MultiList<T>`）；引用类型（约束 `unmanaged`）；net451 / netstandard2.0（类型不存在）。

## 如何运行

```bash
dotnet run --project sample/Sample.SpanBag
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 方法内零堆计数 | `new SpanBag<T>(Span<T>, Span<int>)` / `CountOf` / `Add(T)` |
| 2 | 定容与满容行为 | `Capacity` / `Remaining` / `Add` 返回 `bool` / `DistinctCount` / `TotalCount` |
| 3 | 枚举直方图 | `GetEnumerator()`（`ref struct` 枚举器）/ `Contains` |
| 4 | 删除与 `Clear` 复用 | `Remove(T)` / `RemoveAllCopies(T)` / `Clear()` / `IsEmpty` |
| 5 | 生命周期边界 | `ref struct` 编译期约束（反例注释） |

## 关键 API

- `SpanBag<T>` — `where T : unmanaged`，`ref struct`；构造 `new SpanBag<T>(Span<T> values, Span<int> counts)`，两 span 必须等长。
- `SpanBag<T>.Add(T)` — 返回 `bool`：新元素在满容时返回 `false`（尺寸条件，非异常），已有元素恒成功。
- `SpanBag<T>.CountOf` / `Contains` / `Remove(T)` / `RemoveAllCopies(T)` / `Clear()`。
- `SpanBag<T>.Capacity` / `Remaining` / `DistinctCount` / `TotalCount` / `IsEmpty`。
- `SpanBag<T>.GetEnumerator()` — 返回 `ref struct` 枚举器，同样不能逃出栈帧。

## 注意事项 / 陷阱

- **`ref struct` 生命周期**：不可装箱 / 作字段 / 被 lambda 捕获 / 跨 `await` / `yield`——全为编译错误，by design。
- **定容，不增长**：容量 = `min(values.Length, counts.Length)` 构造时定死；满时新元素 `Add` 返回 `false`，**务必检查返回值**，否则新元素被静默丢弃。
- **两 span 必须等长**：否则构造抛 `ArgumentException`。
- **仅 netstandard2.1 / net6.0+**：`net451`..`net48` / `netstandard2.0` 不暴露本类型（用 `PackedBag<T>`）。
- **无需 `Dispose`**：内存从未向分配器要；`Clear` 只重置计数与总数。
