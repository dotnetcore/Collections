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
            Console.WriteLine();
            PackedBagDemo();
            Console.WriteLine();
            SpanBagDemo();
            Console.WriteLine();
            FrequencyPriorityBagDemo();
            Console.WriteLine();
            MultiKeyMultiDictionaryDemo();
        }

        // The three core types multiply three different things; the fourth is the arity-2
        // facade over the trie; the rest are the specialised bags added in 6.4. Keeping the
        // distinction visible is the point of this demo.
        private static void TaxonomyAtAGlance()
        {
            Console.WriteLine("=== The \"multi\" types: what each one multiplies ===");
            Console.WriteLine("MultiList<T>                    : 1 element        -> N copies   (multiset / bag)");
            Console.WriteLine("MultiDictionary<TKey,TValue>    : 1 key            -> N values   (multimap)");
            Console.WriteLine("MultiKeyDictionary<TKey,TValue> : N key components -> 1 value    (composite key / trie)");
            Console.WriteLine("TwoKeyDictionary<K1,K2,V>       : 2 key components -> 1 value    (arity-2 facade over the trie)");
            Console.WriteLine("MultiKeyMultiDictionary<K,V>    : N key components -> N values   (trie + multimap, 6.4)");
            Console.WriteLine("PackedBag<T>                    : 1 struct element -> N copies   (packed histogram, 6.4)");
            Console.WriteLine("SpanBag<T>                      : 1 element        -> N copies   (stack-only, netstandard2.1 / net6.0+, 6.4)");
            Console.WriteLine("FrequencyPriorityBag<T>         : 1 element        -> N copies   (most-frequent-first, 6.4)");
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

            // Symmetric difference: every distinct value of the argument toggles -- it cancels
            // one stored copy, or is added when none is stored. The argument is a set, exactly
            // as in UnionWith / IntersectionWith / ExceptWith above.
            var diff = new MultiDictionary<string, int>();
            diff.AddRange("a", new[] { 1, 1, 2 });
            diff.SymmetricExceptWith("a", new[] { 1, 2, 2, 3 });
            Console.WriteLine($"SymmetricExceptWith= {string.Join(", ", diff["a"])}  ([1,1,2] toggled by {{1,2,3}})");

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

        // ---------------------------------------------------------------------------------
        // 6.4 additions
        // ---------------------------------------------------------------------------------

        // The value type of the PackedBag demo: a struct element, the shape the type is for.
        private enum Severity
        {
            Debug,
            Info,
            Warning,
            Error
        }

        private static void PackedBagDemo()
        {
            Console.WriteLine("=== PackedBag<T> (packed value-type counting histogram) ===");

            var histogram = new PackedBag<Severity>();
            histogram.Add(Severity.Info, 5);
            histogram.Add(Severity.Warning, 2);
            histogram.Add(Severity.Error);
            histogram.AddRange(new[] { Severity.Info, Severity.Debug });

            Console.WriteLine($"TotalCount        = {histogram.TotalCount}   (5 info + 2 warning + 1 error + 1 info + 1 debug)");
            Console.WriteLine($"DistinctCount     = {histogram.DistinctCount}");
            Console.WriteLine($"CountOf(Info)     = {histogram.CountOf(Severity.Info)}");
            Console.WriteLine($"Capacity          = {histogram.Capacity}   (one contiguous (value, count) array, no hash table)");

            var remaining = histogram.Remove(Severity.Warning);
            Console.WriteLine($"Remove(Warning)   = {remaining} remaining copies, TotalCount = {histogram.TotalCount}");
            histogram.RemoveAllCopies(Severity.Error);
            Console.WriteLine($"RemoveAllCopies(Error): DistinctCount = {histogram.DistinctCount}   (zero-count entries are dropped, array stays packed)");

            Console.WriteLine("EntrySet          = " + string.Join("; ",
                histogram.EntrySet().Select(e => e.Item + "x" + e.Count)));
            Console.WriteLine("DistinctItems     = " + string.Join(", ", histogram.DistinctItems()));
            Console.WriteLine("ToDictionary      = " + string.Join("; ",
                histogram.ToDictionary().Select(p => p.Key + ":" + p.Value)));
            Console.WriteLine("boundary          = struct-only (no null / Nullable<T>), dense domains up to ~16 distinct values");
        }

        private static void SpanBagDemo()
        {
            Console.WriteLine("=== SpanBag<T> (stack-only temporary bag, netstandard2.1 / net6.0+) ===");

            // The caller owns the storage: a stackalloc'd pair of spans that dies with this frame.
            // SpanBag<T> is a ref struct, so it cannot escape the method either - the compiler
            // enforces the lifetime contract, and no factory method exists for that very reason.
            Span<int> values = stackalloc int[16];
            Span<int> counts = stackalloc int[16];
            var window = new SpanBag<int>(values, counts);

            foreach (var reading in new[] { 7, 3, 7, 7, 9, 3, 7 })
            {
                window.Add(reading);
            }

            Console.WriteLine($"TotalCount        = {window.TotalCount}   (counting inside one method, zero heap allocation)");
            Console.WriteLine($"DistinctCount     = {window.DistinctCount}");
            Console.WriteLine($"CountOf(7)        = {window.CountOf(7)}");
            Console.WriteLine($"Remove(3)         = {window.Remove(3)} copy left");

            var entries = new List<string>();
            foreach (var (value, count) in window)
            {
                entries.Add(value + "x" + count);
            }
            Console.WriteLine("entries           = " + string.Join("; ", entries));

            // Fixed capacity: a new element when full is a sizing condition (false), not an
            // exception; an already-present element always fits.
            Span<int> tinyValues = stackalloc int[2];
            Span<int> tinyCounts = stackalloc int[2];
            var tiny = new SpanBag<int>(tinyValues, tinyCounts);
            Console.WriteLine($"capacity-2 bag    = Add(1) {tiny.Add(1)}, Add(2) {tiny.Add(2)}, Add(3) {tiny.Add(3)} (full, rejected), Add(1) {tiny.Add(1)} (already present, fits)");

            window.Clear();
            Console.WriteLine($"after Clear()     = TotalCount {window.TotalCount}, Capacity {window.Capacity} (same stack memory reused, no allocation)");
        }

        private static void FrequencyPriorityBagDemo()
        {
            Console.WriteLine("=== FrequencyPriorityBag<T> (most-frequent-first) ===");

            var alerts = new FrequencyPriorityBag<string>();
            alerts.AddRange(new[] { "cpu", "disk", "cpu", "cpu", "disk", "net" });

            Console.WriteLine($"TotalCount        = {alerts.TotalCount}");
            Console.WriteLine($"DistinctCount     = {alerts.DistinctCount}");
            Console.WriteLine($"PeekMost()        = {alerts.PeekMost()}   (O(1) while the heap is clean; rebuilt lazily when dirty)");

            // Each PopMost takes one copy away (MultiList multiplicity semantics), so repeating
            // it yields the Top-K in descending-frequency order. Ties go to whichever element
            // reached its current count first - the documented FIFO-flavoured policy.
            var top3 = new List<string>();
            for (var i = 0; i < 3; i++)
            {
                top3.Add(alerts.PopMost());
            }
            Console.WriteLine("Top-3 by PopMost  = " + string.Join(" > ", top3));

            Console.WriteLine($"after 3 pops      = TotalCount {alerts.TotalCount} (one copy per pop, not the whole element)");
            Console.WriteLine("remaining entries = " + string.Join("; ",
                alerts.EntrySet().Select(e => e.Item + "x" + e.Count)));

            alerts.Clear();
            Console.WriteLine($"after Clear()     : TryPopMost = {alerts.TryPopMost(out _)}   (empty bag reports false, PopMost would throw)");
        }

        private static void MultiKeyMultiDictionaryDemo()
        {
            Console.WriteLine("=== MultiKeyMultiDictionary<TKey,TValue> (composite key + many values per key) ===");

            var assignments = new MultiKeyMultiDictionary<string, int>();
            assignments.AddRange(new[] { "eu", "de" }, new[] { 101, 102 });
            assignments.Add(new[] { "eu", "de" }, 103);
            assignments.Add(new[] { "eu", "fr" }, 201);
            assignments.Add(new[] { "us", "ca" }, 301);

            Console.WriteLine($"Count             = {assignments.Count}   (complete keys, not trie nodes)");
            Console.WriteLine($"NodeCount         = {assignments.NodeCount}");
            Console.WriteLine($"TotalValueCount   = {assignments.TotalValueCount}");
            Console.WriteLine($"[eu,de] values    = {string.Join(", ", assignments[new[] { "eu", "de" }])}");
            Console.WriteLine($"ValueCount([eu,de])= {assignments.ValueCount(new[] { "eu", "de" })}");

            // The trie half's prefix projection carries over, values included.
            Console.WriteLine($"CountOfPrefix([eu]) = {assignments.CountOfPrefix(new[] { "eu" })}");
            Console.WriteLine("GetByPrefix([eu])   = " + string.Join("; ",
                assignments.GetByPrefix(new[] { "eu" })
                    .Select(e => string.Join("/", e.Key) + "=[" + string.Join(",", e.Values) + "]")));
            Console.WriteLine("GetByPrefix rel.    = " + string.Join("; ",
                assignments.GetByPrefix(new[] { "eu" }, relative: true)
                    .Select(e => string.Join("/", e.Key) + "=[" + string.Join(",", e.Values) + "]")));

            // Per-key value set operations mirror MultiDictionary: the argument is a *set*.
            assignments.ExceptWith(new[] { "eu", "de" }, new[] { 102 });
            Console.WriteLine($"ExceptWith([eu,de], 102) = {string.Join(", ", assignments[new[] { "eu", "de" }])}");

            // "No value-less key": a key is pruned from the trie once its last value goes.
            assignments.Remove(new[] { "eu", "fr" }, 201);
            Console.WriteLine($"ContainsKey([eu,fr]) after its last value is removed = {assignments.ContainsKey(new[] { "eu", "fr" })}");

            var destroyed = assignments.RemovePrefix(new[] { "eu" });
            Console.WriteLine($"RemovePrefix([eu])  = {destroyed} values destroyed -> Count = {assignments.Count}");
            Console.WriteLine("remaining keys      = " + string.Join("; ",
                assignments.Keys.Select(k => string.Join("/", k))));
        }
    }
}
