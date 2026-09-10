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
            TaxonomyAtAGlance();
            Console.WriteLine();
            MultiListBasics();
            Console.WriteLine();
            MultiListAdvanced();
            Console.WriteLine();
            MultiDictionaryBasics();
            Console.WriteLine();
            MultiDictionaryAdvanced();
            Console.WriteLine();
            MultiKeyDictionaryDemo();
            Console.WriteLine();
            TwoKeyDictionaryDemo();
        }

        // The three core types multiply three different things; the fourth is the arity-2
        // facade over the trie. Keeping the distinction visible is the point of this demo.
        private static void TaxonomyAtAGlance()
        {
            Console.WriteLine("=== The \"multi\" types: what each one multiplies ===");
            Console.WriteLine("MultiList<T>                    : 1 element        -> N copies   (multiset / bag)");
            Console.WriteLine("MultiDictionary<TKey,TValue>    : 1 key            -> N values   (multimap)");
            Console.WriteLine("MultiKeyDictionary<TKey,TValue> : N key components -> 1 value    (composite key / trie)");
            Console.WriteLine("TwoKeyDictionary<K1,K2,V>       : 2 key components -> 1 value    (arity-2 facade over the trie)");
        }

        private static void MultiListBasics()
        {
            Console.WriteLine("=== MultiList<T> (multiset / bag): basics ===");

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

        private static void MultiListAdvanced()
        {
            Console.WriteLine("=== MultiList<T> (multiset / bag): advanced ===");

            var bag = new MultiList<string> { "a", "a", "b", "c", "c", "c" };

            // Distinct items vs. (item, count) entries.
            Console.WriteLine($"DistinctItems     = {string.Join(", ", bag.DistinctItems())}");
            Console.WriteLine($"EntrySet          = {string.Join("; ", bag.EntrySet().Select(e => e.Item + "x" + e.Count))}");

            // Subset / superset / disjointness with multiplicities.
            Console.WriteLine($"IsSupersetOf [a,b]         = {bag.IsSupersetOf(new[] { "a", "b" })}");
            Console.WriteLine($"IsProperSupersetOf [a,a,b] = {bag.IsProperSupersetOf(new[] { "a", "a", "b" })}");
            Console.WriteLine($"IsDisjointFrom [z]         = {bag.IsDisjointFrom(new[] { "z" })}");

            // Remove every copy at once, then clone for an independent snapshot.
            bag.RemoveAllCopies("c");
            Console.WriteLine($"RemoveAllCopies(c) = {bag} (TotalCount = {bag.TotalCount})");

            var snapshot = bag.Clone();
            bag.Clear();
            Console.WriteLine($"after Clear: bag = <empty>, snapshot = {snapshot} (independent)");
        }

        private static string SymmetricDiff(MultiList<int> a, MultiList<int> b)
        {
            var result = a.Clone();
            result.SymmetricExceptWith(b);
            return result.ToString();
        }

        private static void MultiDictionaryBasics()
        {
            Console.WriteLine("=== MultiDictionary<TKey,TValue> (multimap): basics ===");

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

        private static void MultiDictionaryAdvanced()
        {
            Console.WriteLine("=== MultiDictionary<TKey,TValue> (multimap): advanced ===");

            var map = new MultiDictionary<string, int>();
            map.Add("a", 1);
            map.Add("a", 2);
            map.Add("b", 3);

            // Value lookups.
            Console.WriteLine($"Contains(a, 2)    = {map.Contains("a", 2)}");
            Console.WriteLine($"ContainsValue(3)  = {map.ContainsValue(3)}");

            // TryGetValue with a live read-only view.
            map.TryGetValue("a", out var view);
            map.Add("a", 9);
            Console.WriteLine($"TryGetValue(a) is a live view: Count = {view.Count} after adding 9");

            // ToString and AsReadOnly.
            Console.WriteLine($"ToString          = {map}");
            var readOnly = map.AsReadOnly();
            map.Remove("b");
            Console.WriteLine($"AsReadOnly after Remove(b): Count = {readOnly.Count} (live)");

            // Clone independence.
            var clone = map.Clone();
            clone.Add("clone-only", 42);
            Console.WriteLine($"clone keys        = {string.Join(", ", clone.Keys.OrderBy(k => k))}");
            Console.WriteLine($"original keys     = {string.Join(", ", map.Keys.OrderBy(k => k))}");
        }

        private static void MultiKeyDictionaryDemo()
        {
            Console.WriteLine("=== MultiKeyDictionary<TKey,TValue> (composite key / trie) ===");

            var tree = new MultiKeyDictionary<string, int>();
            tree.Add(new[] { "eu", "de", "berlin" }, 1);
            tree.Add(new[] { "eu", "de", "munich" }, 2);
            tree.Add(new[] { "eu", "fr", "paris" }, 3);

            Console.WriteLine($"Count                   = {tree.Count}   (whole entries, not nodes)");
            Console.WriteLine($"NodeCount               = {tree.NodeCount}");
            Console.WriteLine($"[eu,de,berlin]          = {tree[new[] { "eu", "de", "berlin" }]}");

            // Prefix projection: what a plain Dictionary<TKey[], TValue> lookup can not do.
            Console.WriteLine($"CountOfPrefix([eu])     = {tree.CountOfPrefix(new[] { "eu" })}");
            Console.WriteLine($"CountOfPrefix([eu,de])  = {tree.CountOfPrefix(new[] { "eu", "de" })}");
            Console.WriteLine("GetByPrefix([eu]) full  = " + string.Join("; ",
                tree.GetByPrefix(new[] { "eu" }).Select(e => string.Join("/", e.Key) + "=" + e.Value)));
            Console.WriteLine("GetByPrefix([eu]) rel.  = " + string.Join("; ",
                tree.GetByPrefix(new[] { "eu" }, relative: true).Select(e => string.Join("/", e.Key) + "=" + e.Value)));
            Console.WriteLine($"GetBranches([eu])       = {string.Join(", ", tree.GetBranches(new[] { "eu" }))}");

            // A whole subtree goes away in one call.
            var removed = tree.RemovePrefix(new[] { "eu", "de" });
            Console.WriteLine($"RemovePrefix([eu,de])   = {removed} entries removed -> Count = {tree.Count}");
            Console.WriteLine($"remaining keys          = {string.Join("; ", tree.Keys.Select(k => string.Join("/", k)))}");
        }

        private static void TwoKeyDictionaryDemo()
        {
            Console.WriteLine("=== TwoKeyDictionary<K1,K2,V> (two differently typed components) ===");

            var rates = new TwoKeyDictionary<int, string, decimal>();
            rates[1, "USD"] = 1.00m;
            rates[1, "EUR"] = 0.92m;
            rates[2, "USD"] = 1.05m;

            Console.WriteLine($"Count                   = {rates.Count}");
            Console.WriteLine($"[1, USD]                = {rates[1, "USD"]}");

            // The first axis is a trie prefix, so this is a subtree projection.
            Console.WriteLine($"CountOfFirstKey(1)      = {rates.CountOfFirstKey(1)}");
            Console.WriteLine("GetByFirstKey(1)        = " + string.Join("; ",
                rates.GetByFirstKey(1).Select(e => e.Key2 + "=" + e.Value)));

            // The second axis is not a prefix of the trie key, so it is an O(n) full scan.
            Console.WriteLine($"CountOfSecondKey(USD)   = {rates.CountOfSecondKey("USD")}");
            Console.WriteLine("GetBySecondKey(USD)     = " + string.Join("; ",
                rates.GetBySecondKey("USD").Select(e => e.Key1 + "=" + e.Value)));

            // A per-axis comparer decides matching, not storage: the component keeps the
            // casing it was first stored with, while lookups fold case.
            var ci = new TwoKeyDictionary<string, string, int>(
                StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase);
            ci["EU", "DE"] = 1;
            Console.WriteLine($"ci[eu, de]              = {ci["eu", "de"]}   (stored as {string.Join("/", ci.Keys1)}/{string.Join("/", ci.Keys2)})");
        }
    }
}
