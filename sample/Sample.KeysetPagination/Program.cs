// Sample.KeysetPagination — 键集分页（seek / keyset pagination）续页闭环（F30）
// Sample.KeysetPagination — keyset (seek) pagination continuation loop (F30)
// 运行：dotnet run --project sample/Sample.KeysetPagination
// Run: dotnet run --project sample/Sample.KeysetPagination
//
// 演示重点：
// Highlights:
//   1. KeysetPageExtensions 的 GetFirstPageByKeyset / GetPageByKeyset：用「上一页最后一键」当锚点续页。
//   1. KeysetPageExtensions.GetFirstPageByKeyset / GetPageByKeyset: use the "last key of the previous page" as the anchor to fetch the next page.
//   2. KeysetPage<T> 刻意不携带总数 / 总页数——只有 Members / HasNext / IsFirstPage / LastMember。
//   2. KeysetPage<T> deliberately carries no total count / total page count — only Members / HasNext / IsFirstPage / LastMember.
//   3. IQueryable 表达式树路径 + PaginableKeyset.BuildKeysetQuery：只组查询、不物化，保留 pageSize+1 探测行。
//   3. IQueryable expression-tree path + PaginableKeyset.BuildKeysetQuery: composes the query only, does not materialize, keeping the pageSize+1 probe row.
//   4. 边界：空源返回合法空页不抛；pageSize < 1 抛 ArgumentOutOfRangeException。
//   4. Boundaries: an empty source returns a valid empty page without throwing; pageSize < 1 throws ArgumentOutOfRangeException.
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// 固定时间基准，保证输出确定性（不要用 DateTime.Now 之类的时间源）。
// Fixed time base to keep the output deterministic (do not use time sources like DateTime.Now).
var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
var orders = Enumerable.Range(1, 12)
    .Select(i => new Order { Id = i, CreatedAt = baseTime.AddMinutes(-i), Name = $"order-{i:00}", Amount = i })
    .ToList();

IQueryable<Order> query = orders.AsQueryable();

// ---------------------------------------------------------------------------
// 1. 最小键集闭环：第一页 → 读 LastMember → 续页 → HasNext 结束
// 1. Minimal keyset loop: first page → read LastMember → next page → end on HasNext
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 第一页 → 续页 → HasNext ===");
Console.WriteLine("=== 1. First page → next page → HasNext ===");

// GetFirstPageByKeyset 不接受锚点；IQueryable 源走表达式树键选择器（x => x.Id）。
// GetFirstPageByKeyset takes no anchor; the IQueryable source goes through the expression-tree key selector (x => x.Id).
KeysetPage<Order> first = query.GetFirstPageByKeyset(x => x.Id, pageSize: 5);
Console.WriteLine($"第一页 Id / First page Id: {string.Join(",", first.Members.Select(o => o.Id))}"); // 1,2,3,4,5
Console.WriteLine($"CurrentPageSize = {first.CurrentPageSize}"); // 5
Console.WriteLine($"HasNext         = {first.HasNext}");          // True（内部多取 1 条探测到还有）
                                                                 // True (internally fetched 1 extra row to probe for more)
Console.WriteLine($"IsFirstPage     = {first.IsFirstPage}");      // True

// 锚点 = 本页最后成员的排序键；后续一律用 GetPageByKeyset。
// The anchor = the sort key of this page's last member; from then on always use GetPageByKeyset.
int lastId = first.LastMember!.Id; // 5
KeysetPage<Order> second = query.GetPageByKeyset(x => x.Id, lastKey: lastId, pageSize: 5);
Console.WriteLine($"第二页 Id / Second page Id: {string.Join(",", second.Members.Select(o => o.Id))}"); // 6,7,8,9,10
Console.WriteLine($"第二页 IsFirstPage / Second page IsFirstPage = {second.IsFirstPage}"); // False（应用了锚点）
                                                                                          // False (the anchor was applied)

KeysetPage<Order> third = query.GetPageByKeyset(x => x.Id, lastKey: second.LastMember!.Id, pageSize: 5);
Console.WriteLine($"第三页 Id / Third page Id: {string.Join(",", third.Members.Select(o => o.Id))}"); // 11,12
Console.WriteLine($"第三页 HasNext / Third page HasNext = {third.HasNext}");          // False（到底了）
                                                                                      // False (reached the end)

// ---------------------------------------------------------------------------
// 2. 内存委托重载 + 降序
// 2. In-memory delegate overload + descending
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 内存委托重载 + 降序 ===");
Console.WriteLine("=== 2. In-memory delegate overload + descending ===");

// IEnumerable 重载用 Func<T,TKey> + Comparer<TKey>.Default，不走表达式树（供内存源使用）。
// The IEnumerable overload uses Func<T,TKey> + Comparer<TKey>.Default and does not go through the expression tree (for in-memory sources).
KeysetPage<Order> newest = orders.GetFirstPageByKeyset(x => x.CreatedAt, pageSize: 3, descending: true);
Console.WriteLine($"按 CreatedAt 降序首页 Id / First page Id ordered by CreatedAt desc: {string.Join(",", newest.Members.Select(o => o.Id))}"); // 1,2,3

// 降序续页的边界谓词是 key < lastKey。
// The boundary predicate for descending continuation is key < lastKey.
KeysetPage<Order> nextNewest = orders.GetPageByKeyset(
    x => x.CreatedAt, lastKey: newest.LastMember!.CreatedAt, pageSize: 3, descending: true);
Console.WriteLine($"降序续页 Id / Descending continuation Id: {string.Join(",", nextNewest.Members.Select(o => o.Id))}"); // 4,5,6

// ---------------------------------------------------------------------------
// 3. PaginableKeyset.BuildKeysetQuery：只组查询，不执行
// 3. PaginableKeyset.BuildKeysetQuery: compose the query only, do not execute
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. BuildKeysetQuery 只组查询 ===");
Console.WriteLine("=== 3. BuildKeysetQuery composes the query only ===");

// BuildKeysetQuery 不物化，且保留 pageSize + 1 条探测行：契约是「多取 1 条，调用方裁掉并推断 HasNext」。
// BuildKeysetQuery does not materialize and keeps pageSize + 1 probe rows: the contract is "fetch 1 extra row; the caller trims it and infers HasNext".
DateTimeOffset anchor = newest.LastMember!.CreatedAt;
IQueryable<Order> continuation = PaginableKeyset.BuildKeysetQuery(
    query, x => x.CreatedAt, lastKey: anchor, pageSize: 3, descending: true);
var raw = continuation.ToList();
Console.WriteLine($"BuildKeysetQuery 结果条数 / result row count = {raw.Count}"); // 4 = pageSize(3) + 1 探测行
                                                                               // 4 = pageSize(3) + 1 probe row
Console.WriteLine($"裁掉探测行后的 Id / Ids after trimming the probe row: {string.Join(",", raw.Take(3).Select(o => o.Id))}"); // 4,5,6

// ---------------------------------------------------------------------------
// 4. 边界：空源 / pageSize 校验
// 4. Boundaries: empty source / pageSize validation
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 边界：空源 / pageSize 校验 ===");
Console.WriteLine("=== 4. Boundaries: empty source / pageSize validation ===");

// 空源：GetFirstPageByKeyset 返回 Members 为空、HasNext==false、IsFirstPage==true 的合法结果，不抛。
// Empty source: GetFirstPageByKeyset returns a valid result with empty Members, HasNext==false and IsFirstPage==true, and does not throw.
var empty = Array.Empty<Order>();
KeysetPage<Order> emptyPage = empty.GetFirstPageByKeyset(x => x.Id, pageSize: 5);
Console.WriteLine($"空源 CurrentPageSize  / empty source CurrentPageSize  = {emptyPage.CurrentPageSize}"); // 0
Console.WriteLine($"空源 HasNext          / empty source HasNext          = {emptyPage.HasNext}");          // False
Console.WriteLine($"空源 IsFirstPage      / empty source IsFirstPage      = {emptyPage.IsFirstPage}");      // True
Console.WriteLine($"空源 LastMember 为 null / empty source LastMember is null = {emptyPage.LastMember is null}"); // True（解引用前先看 CurrentPageSize）
                                                                                                            // True (check CurrentPageSize before dereferencing)

// pageSize 必须 >= 1，否则 ArgumentOutOfRangeException（6.2.0 统一口径）。
// pageSize must be >= 1, otherwise ArgumentOutOfRangeException (unified in 6.2.0).
try
{
    orders.GetFirstPageByKeyset(x => x.Id, pageSize: 0);
}
catch (ArgumentOutOfRangeException ex)
{
    Console.WriteLine($"pageSize=0 被拒 / pageSize=0 rejected: {ex.ParamName}"); // pageSize
}

Console.WriteLine();
Console.WriteLine("Sample.KeysetPagination completed.");

// 每个 sample 自带的小型域类型，不跨 sample 共享。
// Each sample carries its own small domain type, not shared across samples.
internal sealed class Order
{
    public int Id;
    public DateTimeOffset CreatedAt;
    public string Name = string.Empty;
    public decimal Amount;
}
