// Sample.MultiDictionaryExtensions — 从既有源构建多重映射（6.8.0）
// Sample.MultiDictionaryExtensions — building multimaps from existing sources (6.8.0)
// 运行：dotnet run --project sample/Sample.MultiDictionaryExtensions
// Run: dotnet run --project sample/Sample.MultiDictionaryExtensions
//
// 演示重点：
// Highlights:
//   1. IndexBy(keySelector)：把序列按键分组为 MultiDictionary，保留重复（list 语义），组内保持源顺序。
//   1. IndexBy(keySelector): groups a sequence by key into a MultiDictionary, keeping duplicates (list semantics) and preserving source order within each group.
//   2. UniqueIndexBy(keySelector)：索引为 BiDictionary，键重复（或元素重复）抛 ArgumentException。
//   2. UniqueIndexBy(keySelector): indexes into a BiDictionary; a duplicate key (or duplicate element) throws ArgumentException.
//   3. AsMultiDictionary()：把 IReadOnlyDictionary 适配成 MultiDictionary，每键恰好一个值。
//   3. AsMultiDictionary(): adapts an IReadOnlyDictionary into a MultiDictionary, with exactly one value per key.
// 三个入口都返回**独立副本**（非活视图）：改动结果不影响源，源的变化也不反映到结果。
// All three entry points return **independent copies** (not live views): changes to the result don't affect the source, and source changes aren't reflected in the result.
using System;
using System.Collections.Generic;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. IndexBy：序列 -> MultiDictionary（保留重复）
// 1. IndexBy: sequence -> MultiDictionary (duplicates kept)
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. IndexBy：序列 -> MultiDictionary ===");
Console.WriteLine("=== 1. IndexBy: sequence -> MultiDictionary ===");

string[] words = { "ant", "ape", "bee" };
MultiDictionary<char, string> byFirst = words.IndexBy(w => w[0]);
Console.WriteLine($"byFirst['a'].Count = {byFirst['a'].Count}");   // 2：ant 与 ape / 2: ant and ape
Console.WriteLine($"byFirst['b'].Count = {byFirst['b'].Count}");   // 1：bee / 1: bee
// 重复元素被保留（list 语义，不是 set 语义），组内保持源顺序。
// Duplicate elements are kept (list semantics, not set semantics), and source order is preserved within each group.

// 带比较器的重载：键比较忽略大小写，使 "Red" 与 "red" 归到同一组。
// The comparer overload: key comparison ignores case, so "Red" and "red" land in the same group.
string[] tags = { "Red", "red", "Blue" };
var byTag = tags.IndexBy(t => t, StringComparer.OrdinalIgnoreCase);
Console.WriteLine($"忽略大小写分组后 KeyCount / KeyCount after case-insensitive grouping = {byTag.KeyCount}");   // 2
Console.WriteLine($"byTag[\"RED\"].Count = {byTag["RED"].Count}");    // 2

// ---------------------------------------------------------------------------
// 2. UniqueIndexBy：序列 -> BiDictionary（键必须唯一）
// 2. UniqueIndexBy: sequence -> BiDictionary (keys must be unique)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. UniqueIndexBy：序列 -> BiDictionary ===");
Console.WriteLine("=== 2. UniqueIndexBy: sequence -> BiDictionary ===");

string[] users = { "alice", "bob" };
BiDictionary<string, string> byName = users.UniqueIndexBy(u => u);
Console.WriteLine($"byName[\"alice\"] = {byName["alice"]}");               // alice
Console.WriteLine($"AsReverse()[\"bob\"] = {byName.AsReverse()["bob"]}");  // bob
// BiDictionary 是严格一对一：键重复、或两个元素本身相等，都会抛。
// BiDictionary is strictly one-to-one: a duplicate key, or two equal elements, both throw.

try
{
    _ = new[] { "a", "a" }.UniqueIndexBy(x => x);   // 键 "a" 重复 / key "a" is duplicated
}
catch (ArgumentException ex)
{
    Console.WriteLine($"UniqueIndexBy 键重复抛 / UniqueIndexBy throws on duplicate key: {ex.GetType().Name}");   // ArgumentException
}

// ---------------------------------------------------------------------------
// 3. AsMultiDictionary：字典 -> MultiDictionary（独立副本）
// 3. AsMultiDictionary: dictionary -> MultiDictionary (independent copy)
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. AsMultiDictionary：字典 -> MultiDictionary ===");
Console.WriteLine("=== 3. AsMultiDictionary: dictionary -> MultiDictionary ===");

var source = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
{
    ["widget"] = 7,
};
MultiDictionary<string, int> map = source.AsMultiDictionary();
Console.WriteLine($"map[\"widget\"].Count = {map["widget"].Count}");    // 1：每键恰好一个值 / 1: exactly one value per key

// 键比较器随源 Dictionary 一起带过来：用不同大小写也能命中。
// The key comparer is carried over from the source Dictionary: a different casing still hits.
Console.WriteLine($"map[\"WIDGET\"].Count = {map["WIDGET"].Count}");  // 1

// 结果是独立副本：往 map 里再加值不会影响源字典。
// The result is an independent copy: adding more values to map does not affect the source dictionary.
map.Add("widget", 8);
Console.WriteLine($"map 加值后 ValueCount / ValueCount after adding to map = {map.ValueCount("widget")}");  // 2
Console.WriteLine($"source[\"widget\"] = {source["widget"]}");            // 7：源未变 / 7: source unchanged

// ❌ 错误写法：把 AsMultiDictionary() 当活视图——它是快照副本，之后对源字典的修改不会反映到 map。
// ❌ Wrong approach: treating AsMultiDictionary() as a live view — it is a snapshot copy, and later changes to the source dictionary are not reflected in map.
// ✅ 正确写法：需要最新数据时重新调用 AsMultiDictionary()。
// ✅ Correct approach: call AsMultiDictionary() again when you need the latest data.

Console.WriteLine();
Console.WriteLine("Sample.MultiDictionaryExtensions completed.");
