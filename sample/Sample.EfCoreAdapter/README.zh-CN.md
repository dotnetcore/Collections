# Sample.EfCoreAdapter

> **中文** | [English](README.md)

> **EF Core 适配器：真异步分页** — `SolidPageExtensions`（EntityFrameworkCore）+ 键集异步入口（对应 feature F51，`DotNetCore.Collections.Paginable.EntityFrameworkCore`，6.0.0–6.5.0）。

## 本示例演示什么

EF Core 适配器把分页动词接到 `IQueryable<T>` / `DbSet<T>`。它和核心包最大的区别是**真异步**：
`GetPageAsync` / `ToPaginableAsync` 经 provider 原生的 `CountAsync` / `ToListAsync` 真正 `await` I/O，
而不是核心包对 `IEnumerable` 的「形状异步」（`Task.FromResult`，同步完成、不观察令牌）。本 sample 用
EF Core **InMemory** 提供程序建一个内存 `DbContext`，端到端跑一次异步分页，并展示 `DbSet<T>` 直接入口、
末页边界、越界异常与同名扩展类消歧。页结果复用核心契约 `IPage<T>`。

## 什么时候用

- ASP.NET Core + EF Core 的标准分页路径，且需要真异步 / 取消令牌下传（F44）。
- 需要键集异步 → 另有 `KeysetPageAsyncExtensions`（F31），是 EF Core 独有。
- 不该用：同步控制台小工具 → 核心包 `IEnumerable` 入口即可（F45）。

## 如何运行

```bash
dotnet run --project sample/Sample.EfCoreAdapter
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 0 | 内存 `DbContext`（InMemory 提供程序） | `DbContextOptionsBuilder.UseInMemoryDatabase` / `DbSet<T>` |
| 1 | 真异步取页 | `IQueryable<T>.GetPageAsync(pageNumber, pageSize)` |
| 2 | 真异步建集合后多次取页 | `IQueryable<T>.ToPaginableAsync()` / `PaginableSetBase<T>.GetPage` |
| 3 | `DbSet<T>` 直接入口（同步） | `DbSet<T>.GetPage(pageNumber, pageSize)` |
| 4 | 真异步 vs 核心形状异步 | 说明 + `KeysetPageAsyncExtensions` |
| 5 | 越界异常统一 | `GetPageAsync(99, 10)` 抛 `ArgumentOutOfRangeException` |

## 关键 API

- `DotNetCore.Collections.Paginable.SolidPageExtensions`（**EF Core 包**）— `ToPaginable` / `ToPaginableAsync` /
  `GetPage` / `GetPageAsync`，接收者 `IQueryable<T>` 或 `DbSet<T>`。
- `IQueryable<T>.GetPageAsync(int pageNumber, int pageSize, CancellationToken)` — 真异步，返回 `IPage<T>`。
- `KeysetPageAsyncExtensions` — 键集异步，EF Core 独有。
- 返回类型：核心契约 `IPage<T>`；元素是 `IPageMember<T>`（`Value` / `Offset` / `ItemNumber`，`ItemNumber` 为**全局**编号）。

## 注意事项 / 陷阱

- **`SolidPageExtensions` 同名跨包**：同时 `using` 核心与 EF 命名空间会 CS0104；用扩展方法调用、全限定名或别名消歧。
- **`ToPaginableAsync(pageSize: 10)` 会 CS0121**：核心包 `(IEnumerable<T>, int, …)` 与 EF 包 `(IQueryable<T>, int?, …)` 各胜一项，编译器判二义；无实参调用 `ToPaginableAsync()` 才唯一解析到 EF 包。
- **真异步 ≠ 核心形状异步**：核心对 `IEnumerable` 的 `*Async` 同步完成且不观察令牌；不要混为一谈。
- **键集异步在另一个扩展类** `KeysetPageAsyncExtensions`，不在 `SolidPageExtensions` 上。
- **TFM 仅 net6+**：EF Core 包无 net451（F57）。
- **越界抛 `ArgumentOutOfRangeException`**；空源返回空页（F40）。
