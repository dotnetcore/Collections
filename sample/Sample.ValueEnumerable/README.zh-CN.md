# Sample.ValueEnumerable

> **中文** | [English](README.md)

> **LINQ 值类型引擎** — `ToValueEnumerable()` 四入口 + 五算子 + 与 `System.Linq` 的命名空间治理（对应 7.1.0 CHANGELOG，`DotNetCore.Collections`，7.1.0）。

## 本示例演示什么

7.1.0 新增的 `DotNetCore.Collections` 是一个建立在**值类型枚举器**上的 LINQ 引擎：枚举器是普通 `struct`
（不是 `ref struct`），因此可作为类型参数、可跨 `await`，且热路径零分配。这个 sample 演示三个侧面：
（1）`ToValueEnumerable()` 的**四个入口**（`T[]` / `List<T>` / `IReadOnlyList<T>` / `IEnumerable<T>`，最具体的重载胜出）；
（2）五个算子 `Index` / `ForEach` / `TagFirstLast` / `Pairwise` / `Scan`（含带种子重载）；
（3）与 `System.Linq` 共存的**命名空间治理**——同文件 `using` 两个命名空间不产生 CS0121，引擎的 `Index`
绑到包装器，裸 `T[]` 仍绑到 BCL（net9+），既有 `Enumerable` 代码不受影响（F7-07）。工程多目标 `net8.0;net10.0`。

## 什么时候用

- 热路径想避免 `System.Linq` 的枚举器装箱与迭代器分配。
- 需要把枚举器作为类型参数（例如塞进泛型结构体），或跨 `await` 使用——`ref struct` 枚举器做不到。
- 需要 `TagFirstLast` / `Pairwise` / `Scan` 这类 `Enumerable` 没有的算子。
- 不该用：需要 `Enumerable` 全集算子 → 引擎只提供第一批算子（7.1.0 为 5 个）；两表面可自由组合。

## 如何运行

```bash
dotnet run --project sample/Sample.ValueEnumerable            # net8.0
dotnet run --project sample/Sample.ValueEnumerable -f net10.0 # net10.0
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 四个入口：最具体重载胜出 | `T[].ToValueEnumerable()` / `List<T>.ToValueEnumerable()` / `IReadOnlyList<T>.ToValueEnumerable()` / `IEnumerable<T>.ToValueEnumerable()`；`ValueEnumerable.From<T>()` |
| 2 | 位置配对 | `Index()` → `(int Index, T Item)` |
| 3 | 立即执行遍历 | `ForEach(Action<T>)` / `ForEach(Action<T,int>)` |
| 4 | 首末标记 | `TagFirstLast(Func<T,bool,bool,TResult>)` |
| 5 | 相邻配对 | `Pairwise(Func<T,T,TResult>)`（n → n−1） |
| 6 | 运行聚合 | `Scan(Func<T,T,T>)` / `Scan(TResult, Func<TResult,T,TResult>)` |
| 7 | 命名空间治理 | `using System.Linq;` + `using DotNetCore.Collections;` 共存 |

## 关键 API

- `CollectionExtensions.ToValueEnumerable<T>(...)` — 四个重载，按源形状分派。
- `ValueEnumerable.From<T>(...)` — 与扩展方法等价的静态入口。
- 包装器值类型：`ArrayValueEnumerable<T>` / `ListValueEnumerable<T>` / `ReadOnlyListValueEnumerable<T>` / `EnumerableValueEnumerable<T>`，
  实现 `IValueEnumerable<out T, out TEnumerator>`（= `IEnumerable<T>` + 值类型 `GetEnumerator()`）。
- 算子：`Index` / `ForEach` / `TagFirstLast` / `Pairwise` / `Scan`（无种子 + 带种子）。

## 注意事项 / 陷阱

- **`Index` 名称与 `Enumerable.Index` 相同（net9+）**：包装器上绑引擎，裸 `T[]` 仍绑 BCL——两处都是编译期事实。
- **引擎不接收裸 `T[]`**：要算子先 `ToValueEnumerable()`；没有从 `T[]` 到包装器的隐式转换。
- **`ForEach` 只声明在包装器上**：裸 `List<T>.ForEach` 仍是 BCL 的实例方法。
- **`Pairwise` 单元素源产出空序列**（n−1=0）；`Scan` 带种子比源长 1，无种子与源等长。
- **引擎只加命名空间、不接管**：`using DotNetCore.Collections;` 不会改变既有 `Enumerable` 代码的绑定。
