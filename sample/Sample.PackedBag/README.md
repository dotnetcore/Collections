# Sample.PackedBag

> **打包值类型袋** — `PackedBag<T>`：密集数组直方图袋，无装箱存储（对应 feature F21，`DotNetCore.Collections.Multi`，6.4.0）。

## 本示例演示什么

`PackedBag<T> where T : struct` 把值类型元素的计数直方图存为连续的 `(value, count)` 结构体数组：
存储路径无装箱、无每项对象开销，一条缓存线同时带值和计数。示例展示三个侧面：枚举直方图的计数与
`EntrySet`、删除与「末份删除即弃项」、以及批量 / 导出 / 克隆。它刻意**不带**集合运算 / 结构相等 /
自定义比较器——这是直方图，不是通用多重集合。

## 什么时候用

- 枚举域 / 小整数域（约 ≤16 个不同值）的直方图：稠密布局比 `MultiList<T>` 的字典更快、更省分配。
- 热点 `Add` / `CountOf`：无哈希、无追桶。
- 不该用：大 / 稀疏值域（用 `MultiList<T>`）；需要集合运算（用 `MultiList<T>`）；引用类型元素。
- 与 `SpanBag<T>` 的区别：后者还要 `unmanaged` 且存储来自栈上 `stackalloc`（见 F22）。

## 如何运行

```bash
dotnet run --project sample/Sample.PackedBag
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 枚举直方图计数与投影 | `Add(T)` / `Add(T,int)` / `CountOf` / `TotalCount` / `DistinctCount` / `EntrySet()` |
| 2 | 删除与末份弃项 | `Remove(T)` / `Remove(T,int)` / `RemoveAllCopies(T)` / `Contains` |
| 3 | 批量、导出与克隆 | `AddRange` / `ToDictionary()` / `DistinctItems()` / `Clone()` |
| 4 | 值类型边界 | `where T : struct`（引用类型编译期拒绝） |

## 关键 API

- `PackedBag<T>` — `where T : struct`；`Add` / `CountOf` / `TotalCount` / `DistinctCount` 语义镜像 `MultiList<T>`。
- `PackedBag<T>.Remove(T)` / `Remove(T,int)` — 删一份 / 删多份，返回**剩余副本数**（不是 `bool`）。
- `PackedBag<T>.RemoveAllCopies(T)` — 删光该元素全部副本，返回是否真的删掉了（`bool`）。
- `PackedBag<T>.EntrySet()` — `(元素, 副本数)` 紧凑直方图，项数等于 `DistinctCount`。
- `PackedBag<T>.ToDictionary()` / `DistinctItems()` / `Clone()` / `TrimExcess()` / `Capacity`。

## 注意事项 / 陷阱

- **`T : struct`**：无 `null`、无引用类型；约束也排除 `Nullable<T>` 本身，比较固定 `EqualityComparer<T>.Default`。
- **约 ≤16 个不同值才划算**：`IndexOf` 线性扫不同元素，~32 以上用 `MultiList<T>` 的字典 O(1) 反超。
- **末份删除即弃项**：删到零份时条目被整体丢弃，数组保持 packed、无零计数空洞。
- **非线程安全**：并发写会损坏内部条目数组，需外部同步。
