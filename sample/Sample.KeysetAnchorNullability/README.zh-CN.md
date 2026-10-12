# Sample.KeysetAnchorNullability

> **中文** | [English](README.md)

> **键集分页锚点参数可空化** — `lastKey` 锚点与键类型可空性一致：可空键可直接当锚点，`null` 表示「无锚点」（`DotNetCore.Collections.Paginable` / `…EntityFrameworkCore`，6.3.0）。

## 本示例演示什么

键集（seek）分页的 `lastKey` 锚点参数在 6.3.0 做了可空化。此前 NRT 工程在「键类型本身可空」或「首页无锚点传 null」的合法调用上会误报。本示例用可空排序键（`string?`）走查：第一页（无锚点）、续页（锚点 = 上页 `LastMember` 的键）、以及最容易误解的一点——**`null` 锚点等于「无锚点」（从头开始），不是「键值等于 null 的位置」**。

## 什么时候用

- 排序键本身可空（如 `string?`、可空外键），需要把它直接当锚点。
- 统一「首页 / 续页」调用点：首页传 `null` 或 `GetFirstPageByKeyset`，续页传上页末键。
- 不该用：把 `null` 当某个真实键去定位。

## 如何运行

```bash
dotnet run --project sample/Sample.KeysetAnchorNullability
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 第一页（无锚点） | `KeysetPageExtensions.GetFirstPageByKeyset<T, TKey>(...)` |
| 2 | 续页：锚点 = 上页末键（`string?`） | `KeysetPage<T>.LastMember` / `GetPageByKeyset<T, TKey>(...)` |
| 3 | `null` 锚点 = 无锚点 = 从头 | `GetPageByKeyset<T, TKey>(..., TKey lastKey, ...)` |
| 4 | 翻到底 `HasNext=false` | `KeysetPage<T>.HasNext` |

## 关键 API

- `KeysetPageExtensions.GetFirstPageByKeyset<T, TKey>(this IEnumerable<T>, Func<T, TKey>, int pageSize, bool descending = false)` — 首页，无锚点。
- `KeysetPageExtensions.GetPageByKeyset<T, TKey>(this IEnumerable<T>, Func<T, TKey>, TKey lastKey, int pageSize, bool descending = false)` — 续页，锚点为 `TKey`；键类型可空时可直接传 `null`。
- `KeysetPage<T>.LastMember` — 返回 `T?`；空页返回 `default`，喂回下页前需判空。

## 注意事项 / 陷阱

- **`null` 锚点是「无锚点」，不是「键值为 null」。** 若排序键本身可存 `null`，语义由查询提供方定义。
- **非破坏升级**，与 6.3.0 同批；运行时「有锚点 / 无锚点」分支不变。
- **键集页不带总页数**（无 `COUNT(*)`、无 `OFFSET`），与偏移分页 `IPage<T>` 不同（见 6.0.0）。
- `IQueryable<T>` 源有 `Expression<Func<T, TKey>>` 重载；本示例用内存 `IEnumerable<T>` 的 `Func` 重载。
