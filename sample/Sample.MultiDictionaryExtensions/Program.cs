// Sample.MultiDictionaryExtensions — 从既有源构建多重映射（6.8.0）
// 运行：dotnet run --project sample/Sample.MultiDictionaryExtensions
//
// 演示重点：
//   1. IndexBy(keySelector)：把序列按键分组为 MultiDictionary，保留重复（list 语义），组内保持源顺序。
//   2. UniqueIndexBy(keySelector)：索引为 BiDictionary，键重复（或元素重复）抛 ArgumentException。
//   3. AsMultiDictionary()：把 IReadOnlyDictionary 适配成 MultiDictionary，每键恰好一个值。
// 三个入口都返回**独立副本**（非活视图）：改动结果不影响源，源的变化也不反映到结果。
using System;
using System.Collections.Generic;
using DotNetCore.Collections.Multi;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ---------------------------------------------------------------------------
// 1. IndexBy：序列 -> MultiDictionary（保留重复）
// ---------------------------------------------------------------------------
Console.WriteLine("=== 1. IndexBy：序列 -> MultiDictionary ===");

string[] words = { "ant", "ape", "bee" };
MultiDictionary<char, string> byFirst = words.IndexBy(w => w[0]);
Console.WriteLine($"byFirst['a'].Count = {byFirst['a'].Count}");   // 2：ant 与 ape
Console.WriteLine($"byFirst['b'].Count = {byFirst['b'].Count}");   // 1：bee
// 重复元素被保留（list 语义，不是 set 语义），组内保持源顺序。

// 带比较器的重载：键比较忽略大小写，使 "Red" 与 "red" 归到同一组。
string[] tags = { "Red", "red", "Blue" };
var byTag = tags.IndexBy(t => t, StringComparer.OrdinalIgnoreCase);
Console.WriteLine($"忽略大小写分组后 KeyCount = {byTag.KeyCount}");   // 2
Console.WriteLine($"byTag[\"RED\"].Count = {byTag["RED"].Count}");    // 2

// ---------------------------------------------------------------------------
// 2. UniqueIndexBy：序列 -> BiDictionary（键必须唯一）
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 2. UniqueIndexBy：序列 -> BiDictionary ===");

string[] users = { "alice", "bob" };
BiDictionary<string, string> byName = users.UniqueIndexBy(u => u);
Console.WriteLine($"byName[\"alice\"] = {byName["alice"]}");               // alice
Console.WriteLine($"AsReverse()[\"bob\"] = {byName.AsReverse()["bob"]}");  // bob
// BiDictionary 是严格一对一：键重复、或两个元素本身相等，都会抛。

try
{
    _ = new[] { "a", "a" }.UniqueIndexBy(x => x);   // 键 "a" 重复
}
catch (ArgumentException ex)
{
    Console.WriteLine($"UniqueIndexBy 键重复抛：{ex.GetType().Name}");   // ArgumentException
}

// ---------------------------------------------------------------------------
// 3. AsMultiDictionary：字典 -> MultiDictionary（独立副本）
// ---------------------------------------------------------------------------
Console.WriteLine();
Console.WriteLine("=== 3. AsMultiDictionary：字典 -> MultiDictionary ===");

var source = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
{
    ["widget"] = 7,
};
MultiDictionary<string, int> map = source.AsMultiDictionary();
Console.WriteLine($"map[\"widget\"].Count = {map["widget"].Count}");    // 1：每键恰好一个值

// 键比较器随源 Dictionary 一起带过来：用不同大小写也能命中。
Console.WriteLine($"map[\"WIDGET\"].Count = {map["WIDGET"].Count}");  // 1

// 结果是独立副本：往 map 里再加值不会影响源字典。
map.Add("widget", 8);
Console.WriteLine($"map 加值后 ValueCount = {map.ValueCount("widget")}");  // 2
Console.WriteLine($"source[\"widget\"] = {source["widget"]}");            // 7：源未变

// ❌ 错误写法：把 AsMultiDictionary() 当活视图——它是快照副本，之后对源字典的修改不会反映到 map。
// ✅ 正确写法：需要最新数据时重新调用 AsMultiDictionary()。

Console.WriteLine();
Console.WriteLine("Sample.MultiDictionaryExtensions completed.");
