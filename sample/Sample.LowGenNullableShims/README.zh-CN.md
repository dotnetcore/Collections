# Sample.LowGenNullableShims

> **中文** | [English](README.md)

> **低代际 TFM 的可空属性垫片** — net451 等目标没有可空注解属性类型，由内部 polyfill 补上，使全 TFM 单源、注解完整（全包低 TFM 资产，6.3.0）。

## 本示例演示什么

低代际 TFM（`net451` / `net461` / `net47` / `net48` / `netstandard2.0`）的参考程序集没有 `NullableAttribute` / `NullableContextAttribute`，直接写 `T?` 会丢注解元数据。6.3.0 引入**内部垫片**（`build/NullabilityAttributes.cs`），仅在这些目标编入，`netstandard2.1` 与 `net6.0+` 用运行时自带类型。本示例从**使用方视角**证明：垫片无感——可空参数照常可用，公开 API 的空性注解元数据仍可被反射读到，且公开类型面里没有任何垫片类型泄漏。

## 什么时候用

- 库要同时支持 `net451` 与 `net8`：垫片让同一份带 `T?` 的源码在全 TFM 编译通过。
- 消费方在 `net462` + NRT 工程：也能正确读到空性标注。
- 不该用：把垫片当运行时判空（垫片不是检查）。

## 如何运行

```bash
dotnet run --project sample/Sample.LowGenNullableShims
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 使用方视角：`int?` 可空参数照常可用 | `SolidPageExtensions.ToPaginable<T>(..., int? limitedMemberCount)` |
| 2 | 空性注解元数据可被反射读到 | `NullabilityInfoContext` / `KeysetPage<T>.LastMember` / `PaginableSettingsManager.UpdateSettings` |
| 3 | 垫片无公开类型（internal，不泄漏） | `Assembly.GetExportedTypes()` |

## 关键 API

- `build/NullabilityAttributes.cs` — 内部垫片：`NullableAttribute` / `NullableContextAttribute` / `NullablePublicOnlyAttribute`（**internal，非公开 API**）。
- `SolidPageExtensions.ToPaginable<T>(this IEnumerable<T>, int pageSize, int? limitedMemberCount = null)` — 使用方直接受益的可空契约示例。
- `System.Reflection.NullabilityInfoContext` — 读取编译期写入的空性注解元数据（.NET 6+）。

## 注意事项 / 陷阱

- **垫片是内部实现**：不要写进公开 API 文档当类型（找不到 / internal）。
- **与 6.3.0 配套**：6.3.0 是「公开 API 补 NRT 标注」，6.3.0 是「低 TFM 让这份标注有属性类型可编」。
- **不替代 null 检查**：垫片只是让注解元数据可编可读。
- 本 sample 的 net8 资产**不编入**垫片，故第 3 步查不到定义，属预期（运行时自带同名类型）。
