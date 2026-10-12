// Sample.ConcurrentVariants — 线程安全多重集合与映射（6.2.0，含 6.7.1 只读投影）
// Sample.ConcurrentVariants — thread-safe multi-set and multi-map (6.2.0, with 6.7.1 read-only projections)
// 运行：dotnet run --project sample/Sample.ConcurrentVariants
// Run: dotnet run --project sample/Sample.ConcurrentVariants
//
// 演示重点：
// Highlights:
//   1. ConcurrentMultiList<T> 每操作在单锁后串行，线性化：多线程 Add 后计数**确定**。
//   1. ConcurrentMultiList<T> serializes each operation behind a single lock and linearizes: counts after multi-threaded Add are **deterministic**.
//   2. ConcurrentMultiDictionary<TKey,TValue> 按键哈希分片（默认 8 片），不同键的写可并行；
//   2. ConcurrentMultiDictionary<TKey,TValue> shards by key hash (8 shards by default); writes to different keys run in parallel;
//      跨片读（Count / TotalValueCount）按片索引顺序锁一遍取一致快照。
//      cross-shard reads (Count / TotalValueCount) lock each shard in index order to take a consistent snapshot.
//   3. 6.7.1 起 ConcurrentMultiList<T> 补齐只读投影：EntrySet() / DistinctItems() /
//   3. Since 6.7.1, ConcurrentMultiList<T> adds read-only projections: EntrySet() / DistinctItems() /
//      ToList() / ToArray() / ToDictionary()，每个都取自 Snapshot()，与并发写脱钩。
//      ToList() / ToArray() / ToDictionary(), each taken from Snapshot() and detached from concurrent writes.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. 并发计数：单锁串行，计数确定
// 1. Concurrent counting: serialized behind a single lock, deterministic counts
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. 并发计数（单锁串行）===");
Console.WriteLine("=== 1. Concurrent counting (serialized behind a single lock) ===");

var bag = new ConcurrentMultiList<string>();
// Parallel.For 会跨多个线程并发 Add；单锁保证 1000 次都线性化，结果**确定**为 1000。
// Parallel.For Adds concurrently across multiple threads; the single lock guarantees all 1000 linearize, so the result is **deterministically** 1000.
Parallel.For(0, 1000, _ => bag.Add("hit"));

Console.WriteLine($"CountOf(hit)  = {bag.CountOf("hit")}");   // 1000：每次 Add 都记到
                                                             // 1000: every Add is recorded
Console.WriteLine($"TotalCount    = {bag.TotalCount}");      // 1000：副本数
                                                             // 1000: number of copies
Console.WriteLine($"DistinctCount = {bag.DistinctCount}");   // 1：只有一个不同元素
                                                             // 1: only one distinct element

// 两个键交替并发：各 250 次，仍是确定的。
// Two keys alternate concurrently: 250 each, still deterministic.
var parity = new ConcurrentMultiList<string>();
Parallel.For(0, 500, i => parity.Add(i % 2 == 0 ? "even" : "odd"));
Console.WriteLine($"even = {parity.CountOf("even")}, odd = {parity.CountOf("odd")}"); // 250 / 250

// ⚠️ 集合是单锁设计：并发正确 ≠ 高吞吐。读多写少请改用 ImmutableMultiList<T>（见 6.2.0）。
// ⚠️ The set uses a single lock: concurrency correctness ≠ high throughput. For read-heavy write-light, switch to ImmutableMultiList<T> (see 6.2.0).
// ❌ 错误写法：对集合并发变体期望读并行 —— 读吞吐上不去，因为所有操作都过同一把锁。
// ❌ Wrong usage: expecting read parallelism from the set's concurrent variant — read throughput won't rise, because all operations go through the same lock.

// ---------------------------------------------------------------------------
// 2. 分片映射：不同键并行，跨片读一致快照
// 2. Sharded map: different keys in parallel, cross-shard reads take a consistent snapshot
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 分片映射 ===");
Console.WriteLine("=== 2. Sharded map ===");

var map = new ConcurrentMultiDictionary<string, int>();
// 默认 8 片（shardCount 向上取 2 的幂）；不同键按哈希路由到不同片，写不争同一把锁。
// 8 shards by default (shardCount is rounded up to a power of two); different keys route to different shards by hash, so writes don't contend for the same lock.
Console.WriteLine($"ShardCount = {map.ShardCount}");          // 8

Parallel.For(0, 8, i => map.Add("k" + i, i));                 // 8 个不同键并行写
                                                             // 8 different keys written in parallel
Console.WriteLine($"KeyCount        = {map.KeyCount}");        // 8：键数
                                                               // 8: number of keys
Console.WriteLine($"TotalValueCount = {map.TotalValueCount}"); // 8：值数（跨片读，锁一遍取一致快照）
                                                               // 8: number of values (cross-shard read, locks all shards once to take a consistent snapshot)
Console.WriteLine($"map[k3] = [{string.Join(", ", map["k3"])}]"); // [3]

// 同一键再加值：同键恒进同片。
// Add more values under the same key: the same key always goes to the same shard.
map.Add("k0", 100);
Console.WriteLine($"k0 值数 / k0 value count = {map.ValueCount("k0")}");         // 2

// ❌ 错误写法：在循环里反复读 TotalValueCount —— 每次都要锁全部片，吞吐骤降。
// ❌ Wrong usage: repeatedly reading TotalValueCount inside a loop — each read locks all shards, collapsing throughput.
//    需要总量请一次读完缓存到局部变量。
//    If you need the total, read it once and cache it in a local variable.

// ---------------------------------------------------------------------------
// 3. 6.7.1 只读投影：取自 Snapshot()，与后续写脱钩
// 3. 6.7.1 read-only projections: taken from Snapshot(), detached from later writes
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 只读投影（6.7.1）===");
Console.WriteLine("=== 3. Read-only projections (6.7.1) ===");

var proj = new ConcurrentMultiList<string>();
proj.Add("x");
proj.Add("x");
proj.Add("y");

// 五个投影各取一次快照，之后对 bag 的写不会影响它们。
// Each of the five projections takes a snapshot once; later writes to the bag do not affect them.
List<string> toList = proj.ToList();          // 展开副本
                                              // copies expanded
string[] toArray = proj.ToArray();            // 展开副本
                                              // copies expanded
IReadOnlyDictionary<string, int> toDict = proj.ToDictionary(); // 元素 -> 副本数
                                                               // element -> number of copies
var entrySet = proj.EntrySet().ToList();      // (元素, 副本数) 紧凑投影
                                              // compact (element, count) projection
var distinct = proj.DistinctItems().ToList(); // 不同元素
                                              // distinct elements

Console.WriteLine($"ToList().Count        = {toList.Count}");     // 3
Console.WriteLine($"ToArray().Length      = {toArray.Length}");   // 3
Console.WriteLine($"ToDictionary[x]       = {toDict["x"]}");      // 2
Console.WriteLine($"EntrySet 项数 / EntrySet item count = {entrySet.Count}");   // 2 == DistinctCount
Console.WriteLine($"DistinctItems 项数 / DistinctItems item count = {distinct.Count}");   // 2

proj.Add("x", 5);   // 之后再加 5 份 x
                    // then add 5 more copies of x
// 已取出的快照**不随之改变**——这正是「投影取自 Snapshot()」的含义。
// The already-taken snapshots **do not change** — this is exactly what "the projections are taken from Snapshot()" means.
Console.WriteLine($"写后 ToDictionary 快照 x / ToDictionary snapshot x after write = {toDict["x"]}");   // 仍是 2
                                                                                                        // still 2
Console.WriteLine($"写后 ToList 快照 Count / ToList snapshot Count after write = {toList.Count}");  // 仍是 3
                                                                                                    // still 3
Console.WriteLine($"写后实时 CountOf(x) / live CountOf(x) after write = {proj.CountOf("x")}"); // 7
// 想拿最新快照就再调一次投影。
// To get the latest snapshot, just call the projection again.
Console.WriteLine($"重取 ToDictionary[x] / re-taken ToDictionary[x] = {proj.ToDictionary()["x"]}"); // 7

// ---------------------------------------------------------------------------
// 4. 枚举是快照：免疫并发写
// 4. Enumeration is a snapshot: immune to concurrent writes
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 枚举快照 ===");
Console.WriteLine("=== 4. Enumeration snapshot ===");

var en = new ConcurrentMultiList<int>();
en.AddRange(new[] { 1, 2, 3 });
var enumerated = en.ToArray();          // 枚举/导出开始时拷贝一份快照
                                        // a snapshot is copied when enumeration/export begins
en.Add(4);                              // 并发写不撕裂已开始的枚举
                                        // concurrent writes do not tear an already-started enumeration
Console.WriteLine($"快照长度 / snapshot length = {enumerated.Length}");             // 3
Console.WriteLine($"实时 TotalCount / live TotalCount = {en.TotalCount}");          // 4
// 注意：EntrySet 的项数与枚举顺序对哈希表无契约，不要依赖顺序写断言。
// Note: for a hash table there is no contract on the EntrySet item count or the enumeration order; don't write assertions that depend on the order.

Console.WriteLine();
Console.WriteLine("Sample.ConcurrentVariants completed.");
