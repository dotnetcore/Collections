// Sample.Engine.Polyfills — 7.2.0 的十九个 polyfill 算子一次走完
// Sample.Engine.Polyfills — all nineteen polyfill operators of 7.2.0 in one run
// 运行 / Run: dotnet run --project sample/Sample.Engine.Polyfills
//
// 演示重点 / Highlights:
//   1. P0 层 13 个算子：BCL 在 .NET 6 ~ .NET 10 之间陆续补上的那些，低 TFM 上由引擎补齐。
//   1. The thirteen P0 operators: the ones the BCL added between .NET 6 and .NET 10, filled in by the engine on lower TFMs.
//   2. P1 层 6 个算子组：BCL 任何 TFM 都没有的引擎自有算子。
//   2. The six P1 operator groups: engine-only additions that exist in no TFM of the BCL.
//   3. 与 BCL 逐项对拍：net8.0 上 BCL 已有的 9 个算子，引擎结果必须逐字相同。
//   3. A per-operator comparison with the BCL: for the nine operators the BCL ships on net8.0, the engine's answer must match exactly.
//
// 本文件同时 `using System.Linq;` 与 `using DotNetCore.Collections;`——这是 R7-05 的场景：
// 两个面并存时不得产生 CS0121，且引擎的 `using` 不得改变既有 BCL 代码的绑定。
// This file imports both `System.Linq` and `DotNetCore.Collections` on purpose — the R7-05 scenario:
// the two surfaces must coexist without CS0121, and the engine's `using` must not change what existing BCL code binds to.
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var failures = 0;

// 统一的断言出口：不一致就记一次失败，最后用退出码表达。
// One assertion exit: record a failure on disagreement and express the verdict through the exit code.
int Check(string what, string actual, string expected)
{
    var ok = string.Equals(actual, expected, StringComparison.Ordinal);
    Console.WriteLine(
        (ok ? "  PASS  " : "  FAIL  ")
        + what.PadRight(44, '.')
        + " got [" + actual + "]"
        + (ok ? string.Empty : " expected [" + expected + "]"));
    return ok ? 0 : 1;
}

// ---------------------------------------------------------------------------
// 1. P0 键控集合：DistinctBy / ExceptBy / IntersectBy / UnionBy
// 1. P0 keyed sets: DistinctBy / ExceptBy / IntersectBy / UnionBy
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. P0 键控集合（DistinctBy / ExceptBy / IntersectBy / UnionBy）===");
Console.WriteLine("=== 1. P0 keyed sets (DistinctBy / ExceptBy / IntersectBy / UnionBy) ===");

var visits = new[]
{
    (Name: "Ada", City: "London"),
    (Name: "Alan", City: "London"),
    (Name: "Grace", City: "New York"),
    (Name: "Edsger", City: "Rotterdam"),
    (Name: "Barbara", City: "New York"),
};

// DistinctBy 的键是「投影后的键」，不是元素本身：同一城市只保留**首次出现**的那一位。
// DistinctBy keys on the projection, not on the element: each city keeps only its **first** occurrence.
var firstPerCity = visits.ToValueEnumerable().DistinctBy(v => v.City);
Console.WriteLine($"每城首位 / first per city = {string.Join(", ", firstPerCity.Select(v => v.Name))}");

// ExceptBy / IntersectBy 的第二个参数是**键集合**，不是元素集合——这是最容易写错的地方。
// ExceptBy / IntersectBy take a **key set** as the second argument, not an element set — the easiest thing to get wrong.
//
// ⚠️ 更反直觉的是：两者都**按键去重**，而不是「保留所有符合条件的元素」。
// ExceptBy 把排除键与「已产出的键」放进同一个集合，Add 成功才产出；
// IntersectBy 用 Remove 消费键，所以一个键最多匹配一次。
// 于是 New York 有两个人（Grace / Barbara），只有第一个 Grace 能出来。
// ⚠️ Even less intuitive: both **deduplicate by key** rather than "keep every matching element".
// ExceptBy shares one set between the excluded keys and the keys already produced, yielding only on a
// successful Add; IntersectBy consumes keys with Remove, so one key matches at most once.
// New York therefore has two people (Grace / Barbara) and only the first, Grace, comes out.
var dropped = visits.ToValueEnumerable().ExceptBy(new[] { "London" }, v => v.City);
Console.WriteLine($"排除伦敦 / excluding London = {string.Join(", ", dropped.Select(v => v.Name))}");

var kept = visits.ToValueEnumerable().IntersectBy(new[] { "New York", "Rotterdam" }, v => v.City);
Console.WriteLine($"仅保留两城 / only two cities = {string.Join(", ", kept.Select(v => v.Name))}");

// UnionBy 的第二个参数是**元素序列**（不是键），两侧都按键去重、先左后右。
// UnionBy takes an **element sequence** (not keys), deduplicating both sides by key, left side first.
var more = new[] { (Name: "Ada Lovelace", City: "London"), (Name: "Linus", City: "Helsinki") };
var union = visits.ToValueEnumerable().UnionBy(more, v => v.City);
Console.WriteLine($"并集 / union = {string.Join(", ", union.Select(v => v.Name))}");

failures += Check("DistinctBy keeps the first per key", string.Join(",", firstPerCity.Select(v => v.Name)), "Ada,Grace,Edsger");
failures += Check("ExceptBy dedupes by key as well", string.Join(",", dropped.Select(v => v.Name)), "Grace,Edsger");
failures += Check("IntersectBy consumes each key once", string.Join(",", kept.Select(v => v.Name)), "Grace,Edsger");
failures += Check("UnionBy dedupes both sides", string.Join(",", union.Select(v => v.Name)), "Ada,Grace,Edsger,Linus");

// ---------------------------------------------------------------------------
// 2. P0 极值与分批：MaxBy / MinBy / Chunk
// 2. P0 extremes and batching: MaxBy / MinBy / Chunk
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. P0 极值与分批（MaxBy / MinBy / Chunk）===");
Console.WriteLine("=== 2. P0 extremes and batching (MaxBy / MinBy / Chunk) ===");

var scores = new[]
{
    (Name: "Ada", Score: 91),
    (Name: "Alan", Score: 84),
    (Name: "Grace", Score: 97),
    (Name: "Edsger", Score: 78),
};

// MaxBy / MinBy 返回**元素**（不是键）；并列时取最先出现的那一个，与 BCL 一致。
// MaxBy / MinBy return the **element** (not the key); on ties the first one wins, matching the BCL.
var best = scores.ToValueEnumerable().MaxBy(s => s.Score);
var worst = scores.ToValueEnumerable().MinBy(s => s.Score);
Console.WriteLine($"最高分 / highest = {best.Name} ({best.Score})   最低分 / lowest = {worst.Name} ({worst.Score})");

// ⚠️ 空序列的答案取决于**元素类型**，这是 BCL 自己的口径，polyfill 必须一模一样：
// 元素类型可空（引用类型）⇒ 返回 default(null)；元素类型不可空（值类型）⇒ 抛 InvalidOperationException。
// ⚠️ The answer on an empty sequence depends on the **element type**, which is the BCL's own rule and
// the polyfill must match it exactly: a nullable element type (reference) yields default(null), while a
// non-nullable one (value type) throws InvalidOperationException.
var emptyRef = Array.Empty<string>().ToValueEnumerable().MaxBy(x => x.Length);
Console.WriteLine($"空 string 序列 / empty string sequence = {emptyRef ?? "<null>"}");

var threwOnEmpty = false;
try
{
    Array.Empty<int>().ToValueEnumerable().MaxBy(x => x);
}
catch (InvalidOperationException)
{
    threwOnEmpty = true;
}
Console.WriteLine($"空 int 序列抛异常 / empty int sequence throws = {threwOnEmpty}");

// Chunk 的每一块都是**新数组**；最后一块可以不满，但不补默认值。
// Every chunk is a **fresh array**; the last one may be short and is not padded with defaults.
// 显式写出类型 = 编译期断言：这里绑定的是引擎的重载，而不是 BCL 的 Enumerable.Chunk。
// Spelling the type out is a compile-time assertion: this binds to the engine's overload, not to Enumerable.Chunk.
ChunkValueEnumerable<ArrayValueEnumerable<int>, ArrayValueEnumerator<int>, int> batches =
    new[] { 1, 2, 3, 4, 5 }.ToValueEnumerable().Chunk(2);

var rendered = new List<string>();
foreach (var batch in batches)
{
    rendered.Add("[" + string.Join(",", batch) + "]");
}
Console.WriteLine($"分批 / chunks of 2 = {string.Join(" ", rendered)}");

failures += Check("MaxBy returns the element", best.Name, "Grace");
failures += Check("MinBy returns the element", worst.Name, "Edsger");
failures += Check("Chunk leaves the tail short", string.Join(" ", rendered), "[1,2] [3,4] [5]");

// ---------------------------------------------------------------------------
// 3. P0 无键排序：Order / OrderDescending（可继续 ThenBy）
// 3. P0 keyless ordering: Order / OrderDescending (still chainable with ThenBy)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. P0 无键排序（Order / OrderDescending）===");
Console.WriteLine("=== 3. P0 keyless ordering (Order / OrderDescending) ===");

var numbers = new[] { 5, 1, 4, 1, 5, 9, 2, 6 };

// Order / OrderDescending 是「不给键」的排序；返回的是 BCL 的 IOrderedEnumerable<T>，
// 所以后面可以直接接 ThenBy / ThenByDescending，与 OrderBy 的链式体验完全一样。
// Order / OrderDescending sort without a key; they return the BCL's IOrderedEnumerable<T>,
// so ThenBy / ThenByDescending chain on exactly as they do after OrderBy.
var ascending = numbers.ToValueEnumerable().Order().ToArray();
var descending = numbers.ToValueEnumerable().OrderDescending().ToArray();
Console.WriteLine($"升序 / ascending = {string.Join(",", ascending)}");
Console.WriteLine($"降序 / descending = {string.Join(",", descending)}");

var staff = new[]
{
    (Dept: "eng", Name: "Ada"),
    (Dept: "ops", Name: "Zoe"),
    (Dept: "eng", Name: "Alan"),
};
var byDeptThenName = staff.ToValueEnumerable()
    .OrderBy(s => s.Dept)          // System.Linq：按部门
    .ThenBy(s => s.Name)           // System.Linq：同部门再按姓名
    .Select(s => s.Dept + "/" + s.Name)
    .ToArray();
Console.WriteLine($"两级排序 / two-level sort = {string.Join(" ", byDeptThenName)}");

failures += Check("Order sorts ascending", string.Join(",", ascending), "1,1,2,4,5,5,6,9");
failures += Check("OrderDescending sorts descending", string.Join(",", descending), "9,6,5,5,4,2,1,1");

// ---------------------------------------------------------------------------
// 4. P0 键控聚合：CountBy / AggregateBy（共享 seed 与 seed 工厂）
// 4. P0 keyed aggregation: CountBy / AggregateBy (shared seed vs seed factory)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. P0 键控聚合（CountBy / AggregateBy）===");
Console.WriteLine("=== 4. P0 keyed aggregation (CountBy / AggregateBy) ===");

// CountBy 一步完成「分组 + 计数」，返回 KeyValuePair<TKey,int> 序列；不必先 GroupBy 再 Select。
// CountBy does "group + count" in one step, yielding KeyValuePair<TKey,int>; no GroupBy-then-Select needed.
var perCity = visits.ToValueEnumerable().CountBy(v => v.City)
    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
    .Select(pair => pair.Key + "=" + pair.Value);
Console.WriteLine($"每城人数 / people per city = {string.Join(" ", perCity)}");

// AggregateBy 的**共享 seed** 形态：每个键的累加器都从同一个 seed 开始。
// 值类型（int）安全；**引用类型（List）会共享同一个实例**，见下面的陷阱。
// The **shared seed** form of AggregateBy: every key's accumulator starts from the same seed.
// Safe for value types (int); **a reference type (List) shares one instance** — see the pitfall below.
var totalPerCity = visits.ToValueEnumerable()
    .AggregateBy(v => v.City, 0, (sum, v) => sum + 1)
    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
    .Select(pair => pair.Key + "=" + pair.Value);
Console.WriteLine($"共享 seed 累加 / shared-seed fold = {string.Join(" ", totalPerCity)}");

// ⚠️ 陷阱：seed 是 List<int> 时，所有键共用同一个 List ⇒ 结果被串成一锅。
// ⚠️ Pitfall: with a List<int> seed every key shares one List, so the results merge into one bucket.
var sharedSeed = visits.ToValueEnumerable()
    .AggregateBy(v => v.City, new List<string>(), (bucket, v) => { bucket.Add(v.Name); return bucket; })
    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
    .ToArray();
Console.WriteLine($"共享 List seed / shared List seed = {string.Join(" ", sharedSeed.Select(p => p.Key + "=" + p.Value.Count))}");

// 正确写法：用 **seed 工厂**，每个键各拿一个新的累加器。
// Correct form: use the **seed factory**, so each key gets its own accumulator.
var ownSeed = visits.ToValueEnumerable()
    .AggregateBy(v => v.City, _ => new List<string>(), (bucket, v) => { bucket.Add(v.Name); return bucket; })
    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
    .ToArray();
Console.WriteLine($"seed 工厂 / seed factory = {string.Join(" ", ownSeed.Select(p => p.Key + "=" + string.Join("+", p.Value)))}");

failures += Check("CountBy counts per key", string.Join(" ", perCity), "London=2 New York=2 Rotterdam=1");
failures += Check("seed factory isolates keys", string.Join(" ", ownSeed.Select(p => p.Key + "=" + string.Join("+", p.Value))), "London=Ada+Alan New York=Grace+Barbara Rotterdam=Edsger");

// ---------------------------------------------------------------------------
// 5. P0 外连接：LeftJoin / RightJoin（.NET 10 才进 BCL，net8.0 上由引擎提供）
// 5. P0 outer joins: LeftJoin / RightJoin (BCL only from .NET 10; the engine provides them on net8.0)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 5. P0 外连接（LeftJoin / RightJoin）===");
Console.WriteLine("=== 5. P0 outer joins (LeftJoin / RightJoin) ===");

var customers = new[]
{
    (Id: 10, Name: "Ada"),
    (Id: 20, Name: "Grace"),
    (Id: 30, Name: "Linus"),
};
var orders = new[]
{
    (Id: 1, CustomerId: 10, Total: 99),
    (Id: 2, CustomerId: 20, Total: 150),
    (Id: 3, CustomerId: 99, Total: 20),   // 没有对应客户 / no matching customer
};

// LeftJoin 保留**左表全部**行；未匹配时右表为 default ⇒ 引用类型就是 null，必须容忍。
// LeftJoin keeps **every left row**; an unmatched right side is default ⇒ null for reference types, so tolerate it.
var leftRows = orders.ToValueEnumerable().LeftJoin(
    customers,
    order => order.CustomerId,
    customer => customer.Id,
    (order, customer) => order.Id + "->" + (customer.Name ?? "<none>"));
Console.WriteLine($"左连接 / left join = {string.Join(" ", leftRows)}");

// RightJoin 反过来：保留**右表全部**行（这里客户 30 没有订单），未匹配时左表为 default。
// RightJoin is the mirror image: it keeps **every right row** (customer 30 has no order) and the left side is default when unmatched.
var rightRows = orders.ToValueEnumerable().RightJoin(
    customers,
    order => order.CustomerId,
    customer => customer.Id,
    (order, customer) => customer.Name + "<-" + (order.Id == 0 ? "<none>" : order.Id.ToString()));
Console.WriteLine($"右连接 / right join = {string.Join(" ", rightRows)}");

failures += Check("LeftJoin keeps unmatched left rows", string.Join(" ", leftRows), "1->Ada 2->Grace 3-><none>");
failures += Check("RightJoin keeps unmatched right rows", string.Join(" ", rightRows), "Ada<-1 Grace<-2 Linus<-<none>");

// ---------------------------------------------------------------------------
// 6. P1 连续去重与缓存：DistinctUntilChanged / Memoize
// 6. P1 consecutive dedupe and caching: DistinctUntilChanged / Memoize
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 6. P1 连续去重与缓存（DistinctUntilChanged / Memoize）===");
Console.WriteLine("=== 6. P1 consecutive dedupe and caching (DistinctUntilChanged / Memoize) ===");

// DistinctUntilChanged 只看**相邻**重复：1 在末尾再次出现时仍然保留（与 Distinct 语义不同）。
// DistinctUntilChanged only removes **adjacent** duplicates: the trailing 1 survives (unlike Distinct).
var signal = new[] { 1, 1, 2, 2, 2, 3, 1 };
var compressed = signal.ToValueEnumerable().DistinctUntilChanged().ToArray();
Console.WriteLine($"相邻去重 / adjacent dedupe = {string.Join(",", compressed)}");

// Memoize 把「首次枚举」的结果缓存下来：第二次枚举不再回到源。
// Memoize caches the first enumeration: a second pass never goes back to the source.
var probes = 0;
IEnumerable<int> CountingSource()
{
    probes++;
    foreach (var value in new[] { 10, 20, 30 })
    {
        yield return value;
    }
}

var memoized = CountingSource().ToValueEnumerable().Memoize();
var firstPass = memoized.ToArray();
var secondPass = memoized.ToArray();
Console.WriteLine($"两次枚举结果 / two passes = {string.Join(",", firstPass)} | {string.Join(",", secondPass)}");
Console.WriteLine($"源被打开次数 / source opened = {probes}");

failures += Check("DistinctUntilChanged keeps non-adjacent repeats", string.Join(",", compressed), "1,2,3,1");
failures += Check("Memoize opens the source once", probes.ToString(), "1");

// ---------------------------------------------------------------------------
// 7. P1 空回退与计数谓词：FallbackIfEmpty / AtLeast / Exactly / CountBetween
// 7. P1 empty fallback and count predicates: FallbackIfEmpty / AtLeast / Exactly / CountBetween
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 7. P1 空回退与计数谓词（FallbackIfEmpty / AtLeast / Exactly / CountBetween）===");
Console.WriteLine("=== 7. P1 empty fallback and count predicates (FallbackIfEmpty / AtLeast / Exactly / CountBetween) ===");

// FallbackIfEmpty 只在**空**时替换；非空时原样透传。单值与序列两种重载。
// FallbackIfEmpty substitutes only when **empty**; otherwise it passes the source through. Single-value and sequence overloads.
var nothing = Array.Empty<int>().ToValueEnumerable();
Console.WriteLine($"空源回退单值 / empty fallback value = {string.Join(",", nothing.FallbackIfEmpty(42))}");
Console.WriteLine($"空源回退序列 / empty fallback sequence = {string.Join(",", nothing.FallbackIfEmpty(new[] { 7, 8 }))}");
Console.WriteLine($"非空源不变 / non-empty unchanged = {string.Join(",", new[] { 1, 2 }.ToValueEnumerable().FallbackIfEmpty(42))}");

// 三个计数谓词都**短路**：AtLeast(3) 读到第 3 个就停，不会把整条序列走完。
// All three count predicates **short-circuit**: AtLeast(3) stops at the third element instead of draining the sequence.
// 注意 CountBetween 的两端都是**闭区间**，判的是元素总数（这里 signal 有 7 个元素）。
// Note that both ends of CountBetween are **inclusive**, and it judges the total element count (signal holds 7).
Console.WriteLine($"AtLeast(3)={signal.ToValueEnumerable().AtLeast(3)}  Exactly(7)={signal.ToValueEnumerable().Exactly(7)}  CountBetween(5,8)={signal.ToValueEnumerable().CountBetween(5, 8)}");
Console.WriteLine($"AtLeast(100)={signal.ToValueEnumerable().AtLeast(100)}  Exactly(6)={signal.ToValueEnumerable().Exactly(6)}  CountBetween(2,5)={signal.ToValueEnumerable().CountBetween(2, 5)}");

failures += Check("FallbackIfEmpty substitutes only when empty", string.Join(",", nothing.FallbackIfEmpty(42)), "42");
failures += Check("AtLeast / Exactly / CountBetween agree", signal.ToValueEnumerable().AtLeast(3) + "/" + signal.ToValueEnumerable().Exactly(7) + "/" + signal.ToValueEnumerable().CountBetween(5, 8), "True/True/True");

// ---------------------------------------------------------------------------
// 8. P1 Top-N 与过滤投影：PartialSortBy / Choose
// 8. P1 top-N and filtered projection: PartialSortBy / Choose
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 8. P1 Top-N 与过滤投影（PartialSortBy / Choose）===");
Console.WriteLine("=== 8. P1 top-N and filtered projection (PartialSortBy / Choose) ===");

// PartialSortBy 是 O(n log k) 的「只要前 k 名」：不必整表排序，返回的是 List<T>（已物化）。
// PartialSortBy is an O(n log k) "top k only": no full sort, and it returns a materialised List<T>.
var topTwo = scores.ToValueEnumerable().PartialSortBy(2, s => s.Score, descending: true);
Console.WriteLine($"前二名 / top two = {string.Join(", ", topTwo.Select(s => s.Name))}");

// Choose = 过滤 + 投影合成一步：chooser 返回 (Keep, Value)，Keep 为 false 的元素被丢弃且不产生值。
// Choose = filter and projection in one step: the chooser returns (Keep, Value); a false Keep drops the element and yields nothing.
var honours = scores.ToValueEnumerable()
    .Choose(s => (s.Score >= 90, s.Name.ToUpperInvariant()));
Console.WriteLine($"90 分以上 / 90 and above = {string.Join(", ", honours)}");

failures += Check("PartialSortBy returns the top k", string.Join(",", topTwo.Select(s => s.Name)), "Grace,Ada");
failures += Check("Choose filters and projects at once", string.Join(",", honours), "ADA,GRACE");

// ---------------------------------------------------------------------------
// 9. 与 BCL 逐项对拍：同一表达式，两个实现必须给出同一个答案
// 9. Per-operator comparison with the BCL: one expression, two implementations, one answer
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 9. 与 BCL 逐项对拍 ===");
Console.WriteLine("=== 9. Per-operator comparison with the BCL ===");

// 这一节是「polyfill 不是另一个东西」的证明：net8.0 上 BCL 已自带这些算子，
// 引擎的答案必须与 System.Linq 的答案**逐字相同**，否则低 TFM 的用户能察觉到差别。
// This section is the proof that the polyfill is not "something else": on net8.0 the BCL already ships
// these operators, so the engine's answer must match System.Linq **exactly**; otherwise a low-TFM consumer could tell them apart.
var probe = new[] { 5, 1, 4, 1, 5, 9, 2, 6 };

failures += Check("DistinctBy vs BCL",
    string.Join(",", probe.ToValueEnumerable().DistinctBy(x => x % 3)),
    string.Join(",", probe.DistinctBy(x => x % 3)));
failures += Check("ExceptBy vs BCL",
    string.Join(",", probe.ToValueEnumerable().ExceptBy(new[] { 1, 9 }, x => x)),
    string.Join(",", probe.ExceptBy(new[] { 1, 9 }, x => x)));
failures += Check("IntersectBy vs BCL",
    string.Join(",", probe.ToValueEnumerable().IntersectBy(new[] { 1, 4, 6 }, x => x)),
    string.Join(",", probe.IntersectBy(new[] { 1, 4, 6 }, x => x)));
failures += Check("UnionBy vs BCL",
    string.Join(",", probe.ToValueEnumerable().UnionBy(new[] { 7, 5 }, x => x % 4)),
    string.Join(",", probe.UnionBy(new[] { 7, 5 }, x => x % 4)));
failures += Check("MaxBy vs BCL",
    probe.ToValueEnumerable().MaxBy(x => x % 4).ToString(),
    probe.MaxBy(x => x % 4).ToString());
failures += Check("MinBy vs BCL",
    probe.ToValueEnumerable().MinBy(x => x % 4).ToString(),
    probe.MinBy(x => x % 4).ToString());
failures += Check("Chunk vs BCL",
    string.Join("|", probe.ToValueEnumerable().Chunk(3).Select(c => string.Join(",", c))),
    string.Join("|", probe.Chunk(3).Select(c => string.Join(",", c))));
failures += Check("Order vs BCL",
    string.Join(",", probe.ToValueEnumerable().Order()),
    string.Join(",", probe.Order()));
failures += Check("OrderDescending vs BCL",
    string.Join(",", probe.ToValueEnumerable().OrderDescending()),
    string.Join(",", probe.OrderDescending()));

// CountBy / AggregateBy 是 .NET 9 才进 BCL 的，net8.0 上没有对手可比。
// CountBy / AggregateBy only reached the BCL in .NET 9, so net8.0 has no counterpart to compare against.
#if NET9_0_OR_GREATER
failures += Check("CountBy vs BCL",
    string.Join(",", probe.ToValueEnumerable().CountBy(x => x % 3).OrderBy(p => p.Key).Select(p => p.Key + "=" + p.Value)),
    string.Join(",", probe.CountBy(x => x % 3).OrderBy(p => p.Key).Select(p => p.Key + "=" + p.Value)));
failures += Check("AggregateBy vs BCL",
    string.Join(",", probe.ToValueEnumerable().AggregateBy(x => x % 3, 0, (sum, x) => sum + x).OrderBy(p => p.Key).Select(p => p.Key + "=" + p.Value)),
    string.Join(",", probe.AggregateBy(x => x % 3, 0, (sum, x) => sum + x).OrderBy(p => p.Key).Select(p => p.Key + "=" + p.Value)));
#else
Console.WriteLine("  n/a   CountBy / AggregateBy -- no BCL counterpart before .NET 9");
#endif

// LeftJoin / RightJoin 是 .NET 10 才进 BCL 的，且引擎在 net10.0 上直接转发 BCL，故此处不设对拍。
// LeftJoin / RightJoin only reached the BCL in .NET 10, and on net10.0 the engine forwards to the BCL, so no comparison here.
Console.WriteLine("  n/a   LeftJoin / RightJoin -- BCL counterpart only from .NET 10");

Console.WriteLine();

if (failures == 0)
{
    Console.WriteLine("PASS: all nineteen operators agreed with their oracle.");
    Console.WriteLine("PASS：十九个算子与各自的对照实现完全一致。");
    Console.WriteLine();
    Console.WriteLine("Sample.Engine.Polyfills completed.");
    return 0;
}

Console.WriteLine("FAIL: " + failures + " check(s) disagreed.");
Console.WriteLine("FAIL：" + failures + " 项校验不一致。");
return 1;
