// Sample.CollectionChanged — 自建变更通知 CollectionChanged（6.9.0）
// 运行：dotnet run --project sample/Sample.CollectionChanged
//
// 演示重点：
//   1. MultiList<T> / MultiDictionary<TKey,TValue> 各自暴露 CollectionChanged 事件，
//      每次**真正改变**集合的改动都会触发一次。
//   2. 事件参数 CollectionChangedEventArgs<TKey> 携带：ChangeType（Add/Remove/Reset）、
//      Key（元素或键）、Count（移动的份数/值数）、NewCount（改动后的计数）。
//   3. 事件在集合已到达**新的稳定状态之后**触发：处理器里读回集合能看到刚被告知的改动。
//   4. 它**不是**框架的 INotifyCollectionChanged——多重集没有位置，一次调用可能移动多份副本。
using System;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. MultiList<T>：加 / 删 / 清空 各触发一次事件
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. MultiList<T> 的 CollectionChanged ===");

var bag = new MultiList<string>();
bag.CollectionChanged += (sender, e) => Console.WriteLine($"  事件：{e}");

bag.Add("apple", 3);      // Add apple x3 -> 3
bag.Remove("apple");      // Remove apple x1 -> 2（删一份，还剩 2）
bag.Remove("apple", 5);   // Remove apple x2 -> 0（要删 5 份但只有 2，删到底）
bag.Add("pear");          // Add pear x1 -> 1（放回一个元素，好让后面的 Clear 真正改变集合）
bag.Clear();              // Reset（整袋清空，不指名任何元素）

// 集合已空，再 Clear 一次不改变任何东西，因此**不触发**事件
bag.Clear();
Console.WriteLine("  （空袋再次 Clear 无事件——只有真正改变集合的改动才通知）");

// ---------------------------------------------------------------------------
// 2. 处理器里读回集合：看到的已是新状态
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. 事件后读取集合：已是新状态 ===");

var live = new MultiList<string>();
live.CollectionChanged += (sender, e) =>
{
    // 事件在集合到达新状态后触发，所以这里的 TotalCount 已经包含本次改动
    var self = (MultiList<string>)sender!;
    Console.WriteLine($"  {e.ChangeType,-6} -> TotalCount={self.TotalCount}");
};

live.Add("k", 2);   // Add    -> TotalCount=2
live.Remove("k");   // Remove -> TotalCount=1

// ---------------------------------------------------------------------------
// 3. MultiDictionary<TKey,TValue>：以「键」为单位报告值数量的变化
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. MultiDictionary<TKey,TValue> 的 CollectionChanged ===");

var map = new MultiDictionary<string, int>();
map.CollectionChanged += (sender, e) => Console.WriteLine($"  事件：{e}");

map.Add("orders", 1001);          // Add orders x1 -> 1（新键带一个值）
map.Add("orders", 1002);          // Add orders x1 -> 2
map.Remove("orders", 1001);       // Remove orders x1 -> 1（删一个值，键还在）
map.Remove("orders");             // Remove orders x1 -> 0（键被整体丢弃，NewCount=0）
map.Clear();                      // Reset

// ---------------------------------------------------------------------------
// 4. 与框架通知的区别
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 它不是 INotifyCollectionChanged ===");
Console.WriteLine("  多重集没有位置，且一次调用可能移动多份副本，位置型的框架通知无法表达。");
// ❌ 错误写法：bag.CollectionChanged += (s, e) => { NotifyCollectionChangedEventArgs x = e; };
//    e 是自建的 CollectionChangedEventArgs<TKey>，与框架 INotifyCollectionChanged 无关，类型不兼容。

Console.WriteLine();
Console.WriteLine("Sample.CollectionChanged completed.");
