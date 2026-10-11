# DotNetCore.Collections — Samples

> **中文** | [English](README.md)

本目录收录 `DotNetCore.Collections` 家族每个**能力（feature）**的独立可运行示例。
**一个 feature 一个控制台项目**：每个 `Sample.<Focus>/` 自带 `Program.cs`（中英双语注释）、
`README.md`（英文，讲清本 sample 的侧重点）、`README.zh-CN.md`（中文）与 `.csproj`。

- 覆盖范围：`DotNetCore.Collections`（7.1.0 新增的 LINQ 值类型引擎）、
  `DotNetCore.Collections.Multi`、`DotNetCore.Collections.Paginable`，
  以及逐包 ORM 适配器面——**截止到 7.1.0 版本（含）**的全部 feature（F01–F58 + 6.7–7.1 新增）。
- 目标框架：绝大多数为 `net8.0`；`Sample.ValueEnumerable` / `Sample.MultiTfmSurface` 为多目标 `net8.0;net10.0`。
- 每个 sample 直接 `ProjectReference` 仓库 `src/` 下的对应工程，因此**始终针对当前工作树构建**。

## 如何运行

```bash
# 任选一个（工作目录 = 仓库根）：
dotnet run --project sample/Sample.MultiSet
dotnet run --project sample/Sample.KeysetPagination
dotnet run --project sample/Sample.ValueEnumerable -f net8.0   # 多目标需指定 TFM
```

> 这些 sample **有意不加入 `DotNetCore.Collections.sln`**，以免影响仓库的发布门禁基线
> （解决方案级的「0 错 / 警告集」读数）。它们各自独立、可直接 `dotnet run`，也可按需自行加入解决方案。
> 既有的 5 个 sample（`Sample` / `Sample.Ef` / `Sample.EfCore` / `Sample.Engine` / `Sample.Multi`）**保持原样**，见文末。

---

## 一、`DotNetCore.Collections.Multi` — 多重集合 / 多重映射 / 多键 / 袋

> 一包三正交「multi」类型：`MultiList<T>` 乘**元素**、`MultiDictionary<TKey,TValue>` 乘**键下的值**、
> `MultiKeyDictionary` 乘**键分量**。选型问「什么允许重复」，而不是按名字相似度。

### 基础多重集合与多重映射

| Sample | Feature | 侧重点 / 演示的能力 |
| :--- | :--- | :--- |
| [MultiSet](Sample.MultiSet) | F01 | `MultiList<T>` 无序 bag：副本计数语义、`Count`=副本数（非不同元素数）、并/交/差按副本数运算、副本展开枚举 |
| [MultiMap](Sample.MultiMap) | F02 | `MultiDictionary<TKey,TValue>` 多重映射：`Count`=键数、`Values` 是**扁平**序列（非按键分组）、缺键返回空集合 |
| [StructuralEquality](Sample.StructuralEquality) | F03 | 多重集合**结构相等**：副本数参与 `Equals`/`GetHashCode`，与枚举顺序无关，可安全进 `HashSet` |
| [MultiMapBatchDelete](Sample.MultiMapBatchDelete) | F04 | `RemoveRange`（每个不同值删**一次**出现）与 `ExceptWith`（删**全部**出现）的对照 |
| [ExplicitSerialization](Sample.ExplicitSerialization) | F10（+6.7.1） | 显式序列化模型：`ToSerializableModel`/`FromModel` 往返，模型是快照、比较器不进模型；6.7.1 扩展至全家桶 |
| [MultiMapDualCache](Sample.MultiMapDualCache) | F11 | 双缓存：`TotalValueCount` / `ContainsValue` 的 O(1) 成本模型，对使用方透明 |
| [SetOpsZeroAlloc](Sample.SetOpsZeroAlloc) | F12 | 集合运算**零分配**：参数是同比较器 `MultiList<T>` 时就地读、暂存区复用；数组参数仍会物化 |
| [MultiDictionaryExtensions](Sample.MultiDictionaryExtensions) | 6.8.0 | 从既有源构建多重映射：`IndexBy` / `UniqueIndexBy` / `AsMultiDictionary` |

### 有序集合与有序映射

| Sample | Feature | 侧重点 / 演示的能力 |
| :--- | :--- | :--- |
| [OrderedMultiSet](Sample.OrderedMultiSet) | F05 | `OrderedMultiList<T>` 有序 bag：升序枚举、比较器同时定义**顺序与同一性**、O(log n) 增删 |
| [OrderedAccessRange](Sample.OrderedAccessRange) | F06 | 极值（`GetFirst`/`GetLast`）与区间（`GetRange`）查询，边界语义随注入的 `IComparer<T>` |
| [OrderStatisticTree](Sample.OrderStatisticTree) | F07 | 顺序统计 B+ 树引擎（internal）：经 `OrderedMultiList<T>` 观察名次读 O(log n) 与不变式 |
| [OrderedMultiMap](Sample.OrderedMultiMap) | F08 | `OrderedMultiDictionary<TKey,TValue>`：键升序 × 键下值升序，名次读针对展开序列 |
| [SecondaryReverseIndex](Sample.SecondaryReverseIndex) | F09 | `TwoKeyDictionary` 第二轴反向索引：`GetBySecondKey`/`CountOfSecondKey` 不扫全表 |

### 多键 / 双射 / 反向索引

| Sample | Feature | 侧重点 / 演示的能力 |
| :--- | :--- | :--- |
| [TwoKeyDictionary](Sample.TwoKeyDictionary) | F29（+6.7.1/6.8） | 两键复合字典：二维索引器 + 双轴切片；6.7.1 `Values`/`ContainsValue`/`AsReverse`，6.8 `EntrySet()` |
| [ThreeKeyDictionary](Sample.ThreeKeyDictionary) | F15（+6.7.1/6.8） | 三键复合字典：三元组索引器；**只有第一轴是前缀**，第二/三轴靠扫描 |
| [BiDictionary](Sample.BiDictionary) | F16 | 严格**双射**字典：右侧冲突不静默覆盖、`Add` 抛 / `TryAdd` 返 false、双向 O(1) |
| [ReverseMultiDictionary](Sample.ReverseMultiDictionary) | F17（+6.7.1） | `ReverseMultiDictionary<V,K>` 倒排**快照**（构造即拷出）；6.7.1 `AsReverse` 全家桶 |
| [LiveReverseView](Sample.LiveReverseView) | F18 | `MultiDictionary.AsReverse()` **活**反查视图：零拷贝，源 `Add` 后视图立即可见 |
| [MultiKeyMultiDictionary](Sample.MultiKeyMultiDictionary) | F20（+6.8） | 复合键多重映射：同质分量键 + 一键多值 + 前缀计数；6.8 `EntrySet()` |

### 变体与视图

| Sample | Feature | 侧重点 / 演示的能力 |
| :--- | :--- | :--- |
| [ImmutableVariants](Sample.ImmutableVariants) | F13 | `ImmutableMultiList<T>` / `ImmutableMultiDictionary`：突变返回新实例，Builder 批量写 |
| [ConcurrentVariants](Sample.ConcurrentVariants) | F14（+6.7.1） | 线程安全变体：`ConcurrentMultiList` 单锁线性化、`ConcurrentMultiDictionary` 分片；6.7.1 只读投影取自 `Snapshot()` |
| [ReadOnlyCollectionView](Sample.ReadOnlyCollectionView) | F19 | 跨 TFM 只读值视图：net451 上 `HashSet` 裸转 `IReadOnlyCollection` 会抛，包装后不抛，且是**活视图** |

### 袋家族（多重集合的特化）

| Sample | Feature | 侧重点 / 演示的能力 |
| :--- | :--- | :--- |
| [PackedBag](Sample.PackedBag) | F21 | `PackedBag<T>` 密集数组直方图：`where T : struct`、无装箱、小值域（≤16）更快 |
| [SpanBag](Sample.SpanBag) | F22 | `SpanBag<T>` 栈上零堆袋：借用 `stackalloc` 存储、`ref struct` 生命周期由编译器强制 |
| [FrequencyPriorityBag](Sample.FrequencyPriorityBag) | F23 | `FrequencyPriorityBag<T>`：`PeekMost`/`PopMost` 连弹得降频序列，Top-K 的天然形状 |
| [BoundedBag](Sample.BoundedBag) | 6.9.0 | 定容滑动窗口袋：超容**静默淘汰最旧**副本，`Add` 返回 `void`（无溢出可报） |
| [BagStatistics](Sample.BagStatistics) | 6.9.0 | `IMultiSet<T>` 上的 `Mode()` / `Median()` / `Entropy()`：频次分布摘要，经 `EntrySet()` 读 |

### 集合工具与序列化增强

| Sample | Feature | 侧重点 / 演示的能力 |
| :--- | :--- | :--- |
| [PositionalRead](Sample.PositionalRead) | F24 | 名次与分位读：`GetByRank`/`GetRank`/`GetMedian`/`GetQuantile`/`TotalValueCount`（列表 + 映射） |
| [ListFaces](Sample.ListFaces) | F25 | `OrderedMultiList<T>` 的 `IList<T>`/`IReadOnlyList<T>` 面：索引器 setter 与 `Insert` 被收窄 |
| [TypeReadableName](Sample.TypeReadableName) | F26 | `Type.ToReadableString()` / `TypeNameFormat`：去命名空间、保留形状、深度上限折叠 |
| [SharedInterfaces](Sample.SharedInterfaces) | F27 | `IMultiSet<T>` / `IMultiDictionary<TKey,TValue>` / `IBiMap`：面向行为的共享抽象与 `Count` 双语义 |
| [Deque](Sample.Deque) | F28 | `Deque<T>` 环状缓冲双端队列：两端 O(1) 摊还、位置 O(1) 读、滑动窗口 |
| [CollectionDifference](Sample.CollectionDifference) | 6.9.0 | 四路差异比较：`OnlyInLeft`/`OnlyInRight`/`InCommon`/`Differing` + `AreEqual`（bag 按副本数、字典按值） |
| [CollectionChanged](Sample.CollectionChanged) | 6.9.0 | 自建变更通知：`MultiList`/`MultiDictionary` 的 `CollectionChanged` 事件（`Add`/`Remove`/`Reset`） |
| [Snapshot](Sample.Snapshot) | 6.9.0 | `Snapshot()` 独立副本：为「边改边枚举」提供脱离快照，与 `Clone()` 同名异形 |

---

## 二、`DotNetCore.Collections.Paginable` — 分页

> 动词分工：`ToPaginable` 建集合、`GetPage` 从**全源**切一页、`ToPage`/`CreatePage` 装配**已切好的片段**。

### 核心分页

| Sample | Feature | 侧重点 / 演示的能力 |
| :--- | :--- | :--- |
| [KeysetPagination](Sample.KeysetPagination) | F30 | 键集（seek）分页：锚点续页闭环、降序、`BuildKeysetQuery` 的「多取 1 条由调用方裁掉」契约 |
| [AsyncPaging](Sample.AsyncPaging) | F31 | 内存源的 `*Async` 是**同步完成**的「异步形状」，与真 I/O 异步语义不同 |
| [FragmentPage](Sample.FragmentPage) | F32 | 片段建页：`CreatePage`/`ToPage` 对**已切好的**片段绝不再 `Skip/Take`；`PageFragmentInfo` 元数据往返 |
| [StrictFragmentCheck](Sample.StrictFragmentCheck) | F33 | `PageCreationOptions.Lenient`（容忍短片段）vs `Strict`（视为 bug 抛 `ArgumentException`） |
| [SinglePageSet](Sample.SinglePageSet) | F34 | 单页集合：集合层 `PageCount` 恒 1，页层保留全源编号（**有意分叉**） |
| [AsyncFragmentEntry](Sample.AsyncFragmentEntry) | F35 | `Paginable.CreatePageAsync` 是片段建页的 async 对偶：**同步完成**、令牌不观察 |
| [PageMetadata](Sample.PageMetadata) | F36 | 页元数据快照、调用方输入元数据、两者共用的派生源（`PageMetaInfo.Calculate`） |
| [PaginableSettings](Sample.PaginableSettings) | F37 | 全局配置两个旋钮（`DefaultPageSize` / `MaxMemberItems`）：赋值即校验、原子换快照 |
| [LimitedMembers](Sample.LimitedMembers) | F38 | 成员上限与**开区间**：全局 `MaxMemberItems` + 调用级 `limitedMemberCount` 封顶 |
| [EnumerablePageMaterialize](Sample.EnumerablePageMaterialize) | F39 | `EnumerablePage<T>` 单次 `Skip/Take` 物化：深页不再 O(skip²) |

### 契约 / 平台 / 工具

| Sample | Feature | 侧重点 / 演示的能力 |
| :--- | :--- | :--- |
| [ExceptionTypeUnify](Sample.ExceptionTypeUnify) | F40 | 越界异常统一为 `ArgumentOutOfRangeException` 并带 `ParamName`（6.2.0 破坏性变更，含旧类型反例） |
| [NullableAnnotations](Sample.NullableAnnotations) | F41 | 公开 API 的 NRT 空性契约：`T?` / `string?` 是**编译期**承诺，不替代运行时 null 检查 |
| [KeysetAnchorNullability](Sample.KeysetAnchorNullability) | F42 | 键集锚点可空化：可空键直接作锚点，`null` 表示「无锚点」= 第一页 |
| [LowGenNullableShims](Sample.LowGenNullableShims) | F43 | 低代际 TFM 的可空属性垫片（internal）：net451 等也能读到注解，使用方无感 |
| [CancellationTokenForwarding](Sample.CancellationTokenForwarding) | F44 | 取消令牌的三类语义：**下传**（EF/FreeSql）/ **库边界**（SqlSugar 6.6）/ **不观察**（内存 Async） |
| [SolidPageExtensionsSurface](Sample.SolidPageExtensionsSurface) | F45 | 扩展方法入口面：`ToPaginable`/`GetPage`/`*Async`；**同名跨 10 个包**的 CS0104 消歧 |
| [PageMemberWrapping](Sample.PageMemberWrapping) | F46 | 页成员包装：`PageMember<T>`（struct）的 `Value`/`Offset`/`ItemNumber` 与跨页全局编号 |
| [PaginableCalc](Sample.PaginableCalc) | F47 | `PaginableCalc`：成员上限裁剪与页数换算的单一事实源（`Internal` 命名空间但 public） |
| [QueryEntryState](Sample.QueryEntryState) | F48 | `IQueryEntryState<T>` 协变契约：把「一页元素从哪来」抽象出来供延迟取值 |
| [EmptyAndSinglePage](Sample.EmptyAndSinglePage) | F49 | `EmptyPage<T>`（无结果不返回 null）与 `PaginableSinglePage<T>` 的分层语义 |

---

## 三、ORM 适配器 / 平台

> 9 个适配器**动词同构**（`ToPaginable` / `GetPage`），但**入口类型名逐包不同、不可跨包类推**；
> 且各包 `SolidPageExtensions` 同名不同程序集。凡提到该类型必须写全限定名或包名。

| Sample | Feature | 侧重点 / 演示的能力 |
| :--- | :--- | :--- |
| [OrmAdapterMatrix](Sample.OrmAdapterMatrix) | F50 | 9 个适配器的同构矩阵与入口名对照；说明哪些包没有独立的 `Query`/`Page` 对 |
| [EfCoreAdapter](Sample.EfCoreAdapter) | F51 | EF Core **真异步**（`CountAsync`/`ToListAsync`）+ 键集异步；用 InMemory 提供程序端到端跑一次 |
| [FreeSqlAdapter](Sample.FreeSqlAdapter) | F52 | FreeSql 入口面与独有 `includeNestedMembers`；`PaginableFreeSqlQuery<T>` / `FreeSqlPage<T>` |
| [SqlSugarAdapter](Sample.SqlSugarAdapter) | F53 | SqlSugar 入口面与 6.6.0 **取消令牌边界**（已取消令牌在打库前抛 `OperationCanceledException`） |
| [SqlKataAdapter](Sample.SqlKataAdapter) | F54 | SqlKata 源类型**非泛型** `Query`：必须显式写 `ToPaginable<Product>(...)` / `GetPage<Product>(...)` |
| [OtherOrmAdapters](Sample.OtherOrmAdapters) | F55 | Chloe / DosOrm / NHibernate 三包入口名对照与独有重载（`additionalQueryFunc`、`NhCorePage<T>`） |
| [SourceGenerator](Sample.SourceGenerator) | F56 | `[SolidPageExtensionsFor]` 属性声明与生成物形状（analyzer，不随消费者包发布） |
| [MultiTfmSurface](Sample.MultiTfmSurface) | F57 | 11 TFM 矩阵与**逐包子集差异**：EF Core 仅 net6+、Chloe/Dos/NHibernate 无 net451、SqlSugar 无 ns2.0 |

---

## 四、`DotNetCore.Collections` — 7.1.0 新增 LINQ 值类型引擎

| Sample | Feature | 侧重点 / 演示的能力 |
| :--- | :--- | :--- |
| [ValueEnumerable](Sample.ValueEnumerable) | 7.1.0 | `ToValueEnumerable()` 四入口（`T[]`/`List<T>`/`IReadOnlyList<T>`/`IEnumerable<T>`）+ 五算子（`Index`/`ForEach`/`TagFirstLast`/`Pairwise`/`Scan`）+ 与 `System.Linq` 共存的命名空间治理 |
| [SerializationEntry](Sample.SerializationEntry) | F58 | **负结果**：Paginable 侧**没有** `ToSerializableModel`；正确路径 = `PageMetadata` + `Paginable.CreatePage` 重建 |

---

## 五、按主题速查

| 主题 | 相关 sample |
| :--- | :--- |
| **副本计数语义（最高危口径）** | MultiSet、MultiMap、SharedInterfaces、OrderedMultiSet |
| **有序 / 名次 / 区间** | OrderedMultiSet、OrderedAccessRange、OrderStatisticTree、OrderedMultiMap、PositionalRead、ListFaces |
| **多键 / 双射 / 反向索引** | TwoKeyDictionary、ThreeKeyDictionary、BiDictionary、ReverseMultiDictionary、LiveReverseView、MultiKeyMultiDictionary、SecondaryReverseIndex |
| **变体（不可变 / 并发）** | ImmutableVariants、ConcurrentVariants、ReadOnlyCollectionView |
| **袋家族** | PackedBag、SpanBag、FrequencyPriorityBag、BoundedBag、BagStatistics |
| **集合运算与差异** | SetOpsZeroAlloc、CollectionDifference、StructuralEquality |
| **序列化 / 快照 / 通知** | ExplicitSerialization、Snapshot、CollectionChanged、SerializationEntry |
| **分页核心** | KeysetPagination、AsyncPaging、FragmentPage、SinglePageSet、EnumerablePageMaterialize、PageMetadata |
| **分页契约 / 配置** | PaginableSettings、LimitedMembers、PaginableCalc、ExceptionTypeUnify、NullableAnnotations、KeysetAnchorNullability、QueryEntryState、EmptyAndSinglePage |
| **适配器 / 平台** | OrmAdapterMatrix、EfCoreAdapter、FreeSqlAdapter、SqlSugarAdapter、SqlKataAdapter、OtherOrmAdapters、SourceGenerator、MultiTfmSurface、SolidPageExtensionsSurface |
| **引擎（7.1.0）** | ValueEnumerable |

---

## 六、既有示例（未改动，保留原样）

| Sample | 说明 |
| :--- | :--- |
| [Sample](Sample) | 早期 Paginable 基础示例（`net8.0`，引用 `PaginableCollections` 3.3.0） |
| [Sample.Ef](Sample.Ef) | EntityFramework（EF6）适配器示例 |
| [Sample.EfCore](Sample.EfCore) | EF Core 适配器示例 |
| [Sample.Multi](Sample.Multi) | Multi 包基础示例 |
| [Sample.Engine](Sample.Engine) | 7.1.0 引擎「双 `using`」命名空间治理验证（工作项 F7-07），多目标 `net8.0;net10.0` |

---

## 七、约定

- **一个 feature 一个项目**：`Sample.<Focus>/` 只聚焦一篇 feature 文档或一条 CHANGELOG 条目。
- **注释讲语义**：例如「`Count` 是副本数不是不同元素数」「`Values` 是扁平序列」，而不是复述代码。
- **输出确定性**：不依赖哈希枚举顺序；涉及无序集合处先 `OrderBy` 再打印。
- **不编造 API**：示例只使用 `reports/features/` 与 `CHANGELOG.md` 里确有的公开 API；
  与文档措辞冲突处以 `src/` 源码为准（部分 feature 文档的示例存在少量幻觉，已在对应 sample 中按源码修正）。
- **错误写法以注释呈现**（`// ❌ 错误写法：…`），不写入会编译失败的代码。
