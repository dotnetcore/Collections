// Sample.ExplicitSerialization — 显式序列化模型（6.2.0，含 6.7.1 全家桶）
// Sample.ExplicitSerialization — Explicit serialization models (6.2.0, including the 6.7.1 whole-family additions)
// 运行：dotnet run --project sample/Sample.ExplicitSerialization
// Run: dotnet run --project sample/Sample.ExplicitSerialization
//
// 演示重点：
// Highlights:
//   1. MultiListModel<T> / MultiDictionaryModel<TKey,TValue>：普通可变 POCO 快照，不绑任何序列化器。
//   1. MultiListModel<T> / MultiDictionaryModel<TKey,TValue>: plain mutable POCO snapshots that bind no serializer.
//   2. ToSerializableModel() 快照 / FromModel() 重建；比较器与内层策略是**配置**，不进模型。
//   2. ToSerializableModel() takes a snapshot / FromModel() rebuilds; the comparer and inner-collection policy are **configuration**, not part of the model.
//   3. 模型能携带 null 元素 / null 值（ToDictionary 因 null 不能作键而抛）。
//   3. Models can carry null elements / null values (ToDictionary throws because null cannot be a key).
//   4. 6.7.1：BiDictionaryModel（Lefts + Rights 专用模型）、ReverseMultiDictionary 复用 MultiDictionaryModel。
//   4. 6.7.1: BiDictionaryModel (a dedicated Lefts + Rights model), and ReverseMultiDictionary reusing MultiDictionaryModel.
using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. MultiList 往返
// 1. MultiList round-trip
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. MultiList 往返 ===");
Console.WriteLine("=== 1. MultiList round-trip ===");

var bag = new MultiList<string> { "apple", "apple", "banana" };
MultiListModel<string> bagModel = bag.ToSerializableModel();
// 模型是**快照**：Items 与 Counts 两条平行列表，Items[i] 有 Counts[i] 份。
// The model is a **snapshot**: Items and Counts are two parallel lists, where Items[i] has Counts[i] copies.
Console.WriteLine($"Items  = [{string.Join(",", bagModel.Items)}]");   // apple,banana
Console.WriteLine($"Counts = [{string.Join(",", bagModel.Counts)}]");  // 2,1

MultiList<string> restored = MultiList<string>.FromModel(bagModel);
Console.WriteLine($"restored.CountOf(apple) = {restored.CountOf("apple")}");  // 2
// FromModel 校验严格：两列表等长、副本数为正，否则抛 ArgumentException。
// FromModel validates strictly: the two lists must be the same length and the copy counts positive, otherwise it throws ArgumentException.

// ---------------------------------------------------------------------------
// 2. MultiDictionary 往返
// 2. MultiDictionary round-trip
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. MultiDictionary 往返 ===");
Console.WriteLine("=== 2. MultiDictionary round-trip ===");

var map = new MultiDictionary<string, int>();
map.Add("orders", 1001);
map.Add("orders", 1002);

MultiDictionaryModel<string, int> mapModel = map.ToSerializableModel();
Console.WriteLine($"Keys   = [{string.Join(",", mapModel.Keys)}]");        // orders
Console.WriteLine($"Values = [[{string.Join(",", mapModel.Values[0])}]]"); // 1001,1002

var restoredMap = MultiDictionary<string, int>.FromModel(mapModel);
Console.WriteLine($"restoredMap.ValueCount(orders) = {restoredMap.ValueCount("orders")}");  // 2
// 内层是独立拷贝（不是活视图）；allowDuplicateValues 与比较器是重建时的配置项，默认 allowDuplicateValues: true。
// The inner collections are independent copies (not live views); allowDuplicateValues and the comparer are configuration options at rebuild time, defaulting to allowDuplicateValues: true.

// ---------------------------------------------------------------------------
// 3. 模型能携带 null（ToDictionary 做不到）
// 3. Models can carry null (which ToDictionary cannot)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. 模型能携带 null ===");
Console.WriteLine("=== 3. Models can carry null ===");

var withNull = new MultiList<string?> { "a", null, null };
try
{
    _ = withNull.ToDictionary();   // null 不能作字典键
                                   // null cannot be a dictionary key
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"ToDictionary 抛 / ToDictionary threw: {ex.GetType().Name}");
}

MultiListModel<string?> nullModel = withNull.ToSerializableModel();
Console.WriteLine($"nullModel.Items  = [{string.Join(",", nullModel.Items.Select(x => x ?? "<null>"))}]");  // null 与普通元素一起入表（顺序无契约）
                                                                                                          // null goes into the table alongside ordinary elements (order has no contract)
Console.WriteLine($"nullModel.Counts = [{string.Join(",", nullModel.Counts)}]");                          // 合计 2 份 null、1 份 "a"
                                                                                                          // 2 copies of null and 1 copy of "a" in total

var nullRestored = MultiList<string?>.FromModel(nullModel);
Console.WriteLine($"nullRestored.CountOf(null) = {nullRestored.CountOf(null)}");   // 2

// ---------------------------------------------------------------------------
// 4. 畸形模型被 FromModel 拒绝
// 4. A malformed model is rejected by FromModel
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 4. 畸形模型被拒绝 ===");
Console.WriteLine("=== 4. A malformed model is rejected ===");

var bad = new MultiListModel<string>
{
    Items = new List<string> { "x", "y" },
    Counts = new List<int> { 1 },          // 与 Items 长度不一致
                                           // length does not match Items
};
try
{
    _ = MultiList<string>.FromModel(bad);
}
catch (ArgumentException ex)
{
    Console.WriteLine($"FromModel 抛 / FromModel threw: {ex.GetType().Name}");   // ArgumentException
}

// ---------------------------------------------------------------------------
// 5. 6.7.1 全家桶：BiDictionary 专用模型
// 5. 6.7.1 whole family: the BiDictionary-specific model
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 5. 6.7.1：BiDictionary 专用模型 ===");
Console.WriteLine("=== 5. 6.7.1: the BiDictionary-specific model ===");

var bi = new BiDictionary<int, string>();
bi.Add(1, "alice");
bi.Add(2, "bob");

BiDictionaryModel<int, string> biModel = bi.ToSerializableModel();
Console.WriteLine($"Lefts  = [{string.Join(",", biModel.Lefts)}]");    // 1,2
Console.WriteLine($"Rights = [{string.Join(",", biModel.Rights)}]");   // alice,bob
// 双射是一对一，专用模型用两条平行列表（而非多值内层列表），避免误表达多重性。
// A bijection is one-to-one, so the dedicated model uses two parallel lists (rather than multi-value inner lists), avoiding a misleading expression of multiplicity.

var biRestored = BiDictionary<int, string>.FromModel(biModel);
Console.WriteLine($"biRestored[1] = {biRestored[1]}");                        // alice
Console.WriteLine($"AsReverse()[\"alice\"] = {biRestored.AsReverse()["alice"]}");  // 1

// ---------------------------------------------------------------------------
// 6. 6.7.1：ReverseMultiDictionary 复用 MultiDictionaryModel
// 6. 6.7.1: ReverseMultiDictionary reuses MultiDictionaryModel
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 6. 6.7.1：ReverseMultiDictionary 复用模型 ===");
Console.WriteLine("=== 6. 6.7.1: ReverseMultiDictionary reuses the model ===");

var rev = new ReverseMultiDictionary<int, string>();
rev.Add(1001, "orders");      // Add(V value, K key)
rev.Add(1002, "orders");
rev.Add(1001, "customers");

// 反向字典的**索引轴（值）**扮演模型的 Keys，存储的键扮演 Values。
// The reverse dictionary's **index axis (values)** plays the model's Keys, and the stored keys play Values.
MultiDictionaryModel<int, string> revModel = rev.ToSerializableModel();
Console.WriteLine($"revModel.Keys = [{string.Join(",", revModel.Keys.OrderBy(x => x))}]");   // 1001,1002

var revRestored = ReverseMultiDictionary<int, string>.FromModel(revModel);
Console.WriteLine($"revRestored[1001].Count = {revRestored[1001].Count}");   // 2（orders, customers）
                                                                             // 2 (orders, customers)
// ⚠️ 存储的键是 set：内层顺序不保留，重复键会塌缩。
// ⚠️ The stored keys form a set: the inner order is not preserved and duplicate keys collapse.

Console.WriteLine();
Console.WriteLine("Sample.ExplicitSerialization completed.");
