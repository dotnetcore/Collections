# Sample.NullableAnnotations

> **中文** | [English](README.md)

> **公开 API 可空引用类型标注（NRT）** — 编译期空性契约：`T?` / `string?` 告诉调用方「这里可为 null」（对应 feature F41，全包，6.3.0）。

## 本示例演示什么

6.3.0 起公开 API 携带 NRT 标注，覆盖 Multi、Paginable 与各适配器包。这不是行为变更，而是**编译期契约**。本示例用可运行代码讲清四件事：可空参数为 `null` 时回退到默认实现（默认比较器）；库中真实存在的可空契约（`int? limitedMemberCount`、`KeysetPage<T>.LastMember` 的 `T?`）；以及最易误解的一点——**注解不产生运行时检查**，边界仍由库自己抛 `ArgumentNullException`。

## 什么时候用

- 新代码开启 `#nullable enable`，希望得到准确空性警告（消 CS86xx）。
- 旧代码关闭 NRT：升级**无感**，无需改代码。
- 设计「可空参数 → 回退默认」的 API 时，用 `T?` 表达合法传 `null`。
- 不该用：把标注当运行时检查（NRT 不产生 `null` 检查）。

## 如何运行

```bash
dotnet run --project sample/Sample.NullableAnnotations
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 可空参数 `IComparer<Order>?` 传 `null` 回退默认比较器 | 本地 `SortBy(..., IComparer<Order>?)` |
| 2 | 库中真实可空契约 `int?`（`null` = 不限） | `SolidPageExtensions.ToPaginable<T>(..., int? limitedMemberCount)` |
| 3 | 可空返回 `T?` 需判空 | `KeysetPage<T>.LastMember` |
| 4 | 注解 ≠ 运行时检查，边界仍抛 | `PaginableSettingsManager.UpdateSettings(...)` |

## 关键 API

- `SolidPageExtensions.ToPaginable<T>(this IEnumerable<T>, int pageSize, int? limitedMemberCount = null)` — `int?` 可空值类型参数，`null` 表示不限成员数。
- `KeysetPage<T>.LastMember` — 返回 `T?`；页为空时返回 `default`（引用类型为 `null`）。
- `PaginableSettingsManager.UpdateSettings(PaginableSettings)` — 参数为非空标注，运行时仍显式抛 `ArgumentNullException`。

## 注意事项 / 陷阱

- **标注非运行时检查**：不会替你抛 `ArgumentNullException`；边界仍要自己防 NRE。
- **升级无需改代码**：仅需知晓新标注，按新标注消警告即可。
- **低代际 TFM**：注解元数据靠内部垫片提供（见 F43）。
- **本示例只引用核心包**；文档示例中的 `MultiList<T>` 可空比较器属 Multi 侧，约定一致（`IEqualityComparer<T>?` → `EqualityComparer<T>.Default`），此处以本地 `SortBy` 演示同一模式。
