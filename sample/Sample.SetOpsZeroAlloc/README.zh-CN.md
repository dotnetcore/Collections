# Sample.SetOpsZeroAlloc

> **中文** | [English](README.md)

> **集合运算零分配** — 参数已是同比较器 `MultiList<T>` 时就地读 + 暂存区复用（F12，`DotNetCore.Collections.Multi`，6.2.0）。

## 本示例演示什么

`MultiList<T>` 的集合运算（`UnionWith` / `IntersectionWith` / `ExceptWith` / `SymmetricExceptWith` 与子集 / 超集判定）
在参数**已是同比较器的 `MultiList<T>`** 时，就地读参数的计数表，不再物化第二个多重集合；目标状态写入跨调用复用的
暂存列表。这个 sample 演示四种调用形态的**可观察语义**，并用注释标出「何时就地、何时物化」。
零分配是参数侧的实现细节，无法在输出里稳定断言，故本示例不打印分配字节数。

## 什么时候用

- 高频 bag 集合运算：参数已是 bag 时去分配，热路径不再堆上拍皮球。
- 链式 `a.Op(b)` 且 `b` 已是 bag：就地读，省下参数物化与迭代器分配。
- 不该用：一次性低频运算 → 差异可忽略，用哪种写法都行。

## 如何运行

```bash
dotnet run --project sample/Sample.SetOpsZeroAlloc
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 同比较器 `MultiList` 参数就地读 | `UnionWith(IEnumerable<T>)` |
| 2 | 数组参数会物化（结果相同） | `UnionWith`（对照写法） |
| 3 | 比较器不同按接收方重物化 | `UnionWith`（比较器差异） |
| 4 | 子集 / 超集判定直接走两表 | `IsSubsetOf` / `IsSupersetOf` / `IsProperSupersetOf` |
| 5 | `UnionWith(self)` 恒等短路 | `UnionWith(self)` |

## 关键 API

- `MultiList<T>.UnionWith(IEnumerable<T>)` / `IntersectionWith` / `ExceptWith` / `SymmetricExceptWith` — 多重集运算。
- `MultiList<T>.IsSubsetOf(IEnumerable<T>)` / `IsSupersetOf` / `IsProperSubsetOf` / `IsProperSupersetOf` — 子集 / 超集判定。
- `MultiList<T>.CountOf(T)` / `TotalCount` / `DistinctCount` — 用于观察运算结果。

## 注意事项 / 陷阱

- **零分配仅当参数是同比较器 `MultiList<T>`**：传数组 / LINQ 序列会走 `Snapshot` 物化，仍有分配。
- **比较器不同会物化**：语义要求用接收方比较器重解释参数，结果不变、成本回来。
- **暂存区调用后清空**：不持有元素引用；非线程安全。
- **`UnionWith(self)` 是恒等**：显式短路，避免边遍历边加导致副本翻倍。
- **零分配只是实现细节**：不要把它写成对外契约，可观察语义与旧行为一致。
