using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Multi;

namespace Sample.Multi
{
    internal static class Program
    {
        private static void Main()
        {
            MultiListDemo();
            Console.WriteLine();
            MultiDictionaryDemo();
        }

        private static void MultiListDemo()
        {
            Console.WriteLine("=== MultiList<T> (multiset / bag) ===");

            var bag = new MultiList<string>();
            bag.AddRange(new[] { "apple", "apple", "banana" });
            bag.Add("cherry", 3);

            Console.WriteLine($"bag               = {bag}");
            Console.WriteLine($"TotalCount        = {bag.TotalCount}   (copies: 2 apple + 1 banana + 3 cherry)");
            Console.WriteLine($"DistinctCount     = {bag.DistinctCount}");
            Console.WriteLine($"CountOf(apple)    = {bag.CountOf("apple")}");

            bag.Remove("apple", 1);
            Console.WriteLine($"after Remove(apple, 1): CountOf(apple) = {bag.CountOf("apple")}, TotalCount = {bag.TotalCount}");

            // Multiset set operations.
            var other = new MultiList<string> { "apple", "apple", "apple", "durian" };
            bag.UnionWith(other);
            Console.WriteLine($"UnionWith(other)  = {bag}");

            bag.IntersectionWith(new[] { "apple", "cherry" });
            Console.WriteLine($"IntersectionWith  = {bag}");

            bag.ExceptWith(new[] { "apple" });
            Console.WriteLine($"ExceptWith(apple) = {bag}");

            var a = new MultiList<int> { 1, 1, 2 };
            var b = new MultiList<int> { 1, 2, 2 };
            Console.WriteLine($"[1,1,2] symmetric-diff [1,2,2] = {SymmetricDiff(a, b)}");
            Console.WriteLine($"[1,1,2] is subset of [1,1,2,3]: {a.IsSubsetOf(new[] { 1, 1, 2, 3 })}");

            // Dictionary-style export: element -> copy count.
            var counts = new MultiList<string> { "x", "x", "y" }.ToDictionary();
            Console.WriteLine($"ToDictionary      = {string.Join(";", counts.Select(p => p.Key + ":" + p.Value))}");
        }

        private static string SymmetricDiff(MultiList<int> a, MultiList<int> b)
        {
            var result = a.Clone();
            result.SymmetricExceptWith(b);
            return result.ToString();
        }

        private static void MultiDictionaryDemo()
        {
            Console.WriteLine("=== MultiDictionary<TKey,TValue> (multimap) ===");

            var map = new MultiDictionary<string, int>();
            map.Add("orders", 1001);
            map.Add("orders", 1002);
            map.Add("orders", 1003);
            map.Add("customers", 7);

            Console.WriteLine($"orders            = {string.Join(", ", map["orders"])}");
            Console.WriteLine($"KeyCount          = {map.KeyCount}");
            Console.WriteLine($"TotalValueCount   = {map.TotalValueCount}");

            map.Remove("orders", 1002);
            Console.WriteLine($"after Remove(orders, 1002): orders = {string.Join(", ", map["orders"])}");

            // Per-key value set operations.
            map.UnionWith("orders", new[] { 1004, 1004 });
            Console.WriteLine($"UnionWith(orders) = {string.Join(", ", map["orders"])} (duplicates in input added once)");

            map.ExceptWith("orders", new[] { 1001, 1003 });
            Console.WriteLine($"ExceptWith(orders)= {string.Join(", ", map["orders"])}");

            // Duplicate values per key can be disallowed at construction.
            var setMap = new MultiDictionary<string, int>(allowDuplicateValues: false);
            setMap.Add("tags", 1);
            setMap.Add("tags", 1);
            Console.WriteLine($"no-dup map tags   = {string.Join(", ", setMap["tags"])}");

            // LINQ-friendly ILookup view (missing key yields an empty grouping).
            var lookup = setMap.AsLookup();
            Console.WriteLine($"lookup[tags]      = {string.Join(", ", lookup["tags"])}");
            Console.WriteLine($"lookup[missing]   = empty: {!lookup["missing"].Any()}");

            // Flat (key, value) enumeration.
            Console.WriteLine("flat pairs        = " + string.Join("; ",
                map.Select((KeyValuePair<string, int> p) => p.Key + "->" + p.Value)));
        }
    }
}
