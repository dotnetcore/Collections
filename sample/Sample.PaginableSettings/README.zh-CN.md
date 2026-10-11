# Sample.PaginableSettings

> **中文** | [English](README.md)

> **分页配置与管理器** — 全局分页配置只有两个旋钮，赋值即校验、原子换快照（对应 feature F37，`DotNetCore.Collections.Paginable`，6.0.0）。

## 本示例演示什么

`PaginableSettings` 只有 `DefaultPageSize`（默认 `50`）与 `MaxMemberItems`（默认 `10_000_000`）两个旋钮。赋值即校验——非法值在 setter 抛 `ArgumentOutOfRangeException`，因此任何拿到手的实例都是合法态。`PaginableSettingsManager` 用 `volatile` 快照 + 引用赋值实现**原子换入**，`Settings` 读当前快照。本示例展示默认值、配置、单参 `GetPage(n)` 读 `DefaultPageSize`、setter 校验、`UpdateSettings(null)` 拒绝，以及运行结束还原默认。

## 什么时候用

- 启动时设定全站默认页大小与最大结果规模。
- 限制超大 `COUNT` 攻击面 → 收紧 `MaxMemberItems`。
- 多租户 / 测试中替换快照。
- 每请求不同页大小 → 显式传 `pageSize`，别改全局。

## 如何运行

```bash
dotnet run --project sample/Sample.PaginableSettings
```

## 走查

| 步骤 | 概念 | 关键 API |
| :--- | :--- | :--- |
| 1 | 默认值与原子换入 | `PaginableSettingsManager.Settings` / `UpdateSettings` |
| 2 | 赋值即校验 | `PaginableSettings.DefaultPageSize` / `MaxMemberItems` setter |
| 3 | `null` 被拒 | `UpdateSettings(null)` → `ArgumentNullException` |
| 4 | 还原默认 | `UpdateSettings(new PaginableSettings())` |

## 关键 API

- `PaginableSettings.DefaultPageSize`（`int`，≥1，默认 50） / `MaxMemberItems`（`long`，≥1，默认 10_000_000）。
- `PaginableSettingsManager.Settings` — 当前快照（volatile 读）。
- `PaginableSettingsManager.UpdateSettings(PaginableSettings)` — 原子换入；`null` 抛 `ArgumentNullException`。
- `SolidPageExtensions.GetPage<T>(enumerable, pageNumber)` — 单参入口使用 `DefaultPageSize`。

## 注意事项 / 陷阱

- **属性 setter 校验**：`< 1` 即抛 `ArgumentOutOfRangeException`（`ParamName` 为 `value`）。
- **`MaxMemberItems` 比较是 `>` 才拒**：默认 `10_000_000` 恰好允许（开区间，见 F38）。
- **不是 `IOptions` / 非 DI**：静态管理器；测试要自行隔离（改回默认或串行）。
- **运行中频繁 `UpdateSettings`** 可与正在构造的页交错——建议启动时一次定稿。
- **Multi 族不读这些设置**。
