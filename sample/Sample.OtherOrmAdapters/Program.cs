// Sample.OtherOrmAdapters — Chloe / DosOrm / NHibernate 三包入口名对照（F55）
// Sample.OtherOrmAdapters — Chloe / DosOrm / NHibernate three-package entry-name cross-reference (F55)
// 运行：dotnet run --project sample/Sample.OtherOrmAdapters
// Run: dotnet run --project sample/Sample.OtherOrmAdapters
//
// 演示重点：
// Highlights:
//   1. 三包入口类型名**逐包不同**，不可跨包类推：
//   1. The three packages' entry type names **differ from package to package** and must not be extrapolated across packages:
//      Chloe → PaginableChloeQuery<T> / ChloePage<T>
//      DosOrm → PaginableDosQuery<T> / DosPage<T>
//      NHibernate → PaginableNhCoreQuery<T> / NhCorePage<T>（**不是** NHibernatePage<T>）；
//      NHibernate → PaginableNhCoreQuery<T> / NhCorePage<T> (**not** NHibernatePage<T>);
//   2. 独有重载：Chloe / Dos 有 additionalQueryFunc；NHibernate 有 ISession 便捷重载；
//   2. unique overloads: Chloe / Dos have additionalQueryFunc; NHibernate has an ISession convenience overload;
//   3. DosOrm 有 `where T : Entity` 约束——不能把任意 DTO 直接 ToPaginable；
//   3. DosOrm has a `where T : Entity` constraint — you cannot ToPaginable an arbitrary DTO directly;
//   4. 说明型：只引用分页核心包，用字符串打印对照，不引入任何 ORM 包。
//   4. explanatory: references only the pagination core package, prints the cross-reference as strings, and pulls in no ORM package.
using System;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// 核心契约（三包共享）：IPage / IPage<T> / IPaginable / IPaginable<T>。
// Core contracts (shared by the three packages): IPage / IPage<T> / IPaginable / IPaginable<T>.
Console.WriteLine($"核心契约程序集 / Core contract assembly：{typeof(IPage).Assembly.GetName().Name}");
Console.WriteLine();

// ---------------------------------------------------------------------------
// 1. 三包入口名对照表（逐字来自 F55）
// 1. Three-package entry-name cross-reference table (verbatim from F55)
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 三包入口名对照表 ===");
Console.WriteLine("=== 1. Three-package entry-name cross-reference table ===");

var rows = new (string Package, string Query, string Page, string Source, string Unique)[]
{
    ("…Chloe",      "PaginableChloeQuery<T>",  "ChloePage<T>",   "Chloe.IQuery<T>",         "additionalQueryFunc"),
    ("…DosOrm",     "PaginableDosQuery<T>",   "DosPage<T>",     "Dos.ORM.FromSection<T>",  "additionalQueryFunc；where T : Entity / additionalQueryFunc; where T : Entity"),
    ("…NHibernate", "PaginableNhCoreQuery<T>","NhCorePage<T>",  "IQueryOver<T> / ISession","ISession 便捷重载 / ISession convenience overload"),
};

Console.WriteLine($"{"包 / Package",-16}{"分页集合类型 / Paginable collection type",-26}{"页类型 / Page type",-18}{"源类型 / Source type",-26}独有 / Unique");
Console.WriteLine(new string('-', 120));
foreach (var r in rows)
{
    Console.WriteLine($"{r.Package,-16}{r.Query,-26}{r.Page,-18}{r.Source,-26}{r.Unique}");
}

// ---------------------------------------------------------------------------
// 2. 独有重载与约束
// 2. Unique overloads and constraints
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 独有重载与约束 ===");
Console.WriteLine("=== 2. Unique overloads and constraints ===");

// additionalQueryFunc：Func<IQuery<T>, IQuery<T>>（Dos 为 FromSection<T>）——在取页前追加查询变换（排序等）。
// additionalQueryFunc: Func<IQuery<T>, IQuery<T>> (FromSection<T> for Dos) — appends a query transform (sorting, etc.) before fetching a page.
// 可空；不要在其中做副作用。
// It may be null; do not perform side effects inside it.
Console.WriteLine("Chloe: q.ToPaginable(20, limitedMemberCount: null,");
Console.WriteLine("                       additionalQueryFunc: query => query.OrderBy(x => x.Id));");
Console.WriteLine("Dos:   s.ToPaginable(20);  // FromSection<ProductEntity>，且 ProductEntity : Dos.ORM.Entity / and ProductEntity : Dos.ORM.Entity");
Console.WriteLine("NHib:  session.ToPaginable<Product>(20);  // ISession 便捷重载内部 QueryOver<T>() / the ISession convenience overload calls QueryOver<T>() internally");

// ❌ 错误写法：把普通 DTO 直接给 Dos 的 ToPaginable——DosOrm 有 where T : Entity 约束，编不过。
// ❌ Wrong approach: passing an ordinary DTO directly to Dos's ToPaginable — DosOrm has a where T : Entity constraint, so it will not compile.
//    var p = dtoSection.ToPaginable(20);   // DTO 不继承 Dos.ORM.Entity / the DTO does not inherit Dos.ORM.Entity

// ---------------------------------------------------------------------------
// 3. 命名规律与例外（NHibernate 页类型名）
// 3. Naming rule and exception (the NHibernate page type name)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 命名规律与例外 ===");
Console.WriteLine("=== 3. Naming rule and exception ===");
Console.WriteLine("多数：PaginableXxxQuery<T> + XxxPage<T>；但 NHibernate 页类型是 **NhCorePage<T>**，不是 NHibernatePage<T>。 / Most: PaginableXxxQuery<T> + XxxPage<T>; but NHibernate's page type is **NhCorePage<T>**, not NHibernatePage<T>.");
Console.WriteLine("三包都走源生成器（F56）；6.5 起异步 / 取消对齐（F44）。 / All three packages go through the source generator (F56); async / cancellation is aligned since 6.5 (F44).");

// ---------------------------------------------------------------------------
// 4. 共享核心契约（本地可运行，不连库）
// 4. Shared core contract (runs locally, no database)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 共享核心契约（本地可运行，不连库）===");
Console.WriteLine("=== 4. Shared core contract (runs locally, no database) ===");

// 三包页对象都复用核心契约；这里用核心片段入口造一个真实页，证明形状与 ORM 无关。
// The page objects of all three packages reuse the core contract; here we build a real page with the core fragment entry point, proving the shape is independent of the ORM.
IPage<string> page = Paginable.CreatePage(
    new[] { "chloe", "dos", "nhibernate" }, pageNumber: 1, pageSize: 5, totalMemberCount: 3);
Console.WriteLine($"IPage<string> 类型 / Type：{page.GetType().Name}");
Console.WriteLine($"CurrentPageSize = {page.CurrentPageSize}, TotalPageCount = {page.TotalPageCount}"); // 3, 1
foreach (var member in page)
{
    Console.WriteLine($"  #{member.ItemNumber} {member.Value}");
}

// ---------------------------------------------------------------------------
// 5. 同名消歧提醒
// 5. Same-name disambiguation reminder
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 5. 同名 SolidPageExtensions 消歧 ===");
Console.WriteLine("=== 5. Same-name SolidPageExtensions disambiguation ===");
Console.WriteLine("10 处同名 SolidPageExtensions（核心 1 + 适配器 9）；混用两包须全限定名或别名（U27）。 / 10 same-named SolidPageExtensions (1 core + 9 adapters); mixing two packages requires a fully-qualified name or alias (U27).");
Console.WriteLine("三包 TFM **无 net451**（Chloe 5.x 亦不再支持 net451）。 / The three packages' TFM has **no net451** (Chloe 5.x no longer supports net451 either).");

Console.WriteLine();
Console.WriteLine("Sample.OtherOrmAdapters completed.");
