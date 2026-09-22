# Migrating to Collections

This guide is for a codebase that already uses one of the classic ordered-collection libraries and
wants to move its sorted, positional and multiset work onto `DotNetCore.Collections.Multi`.

## 1. How this guide is written

It is written against **shapes, not against a named library**. This repository's public
documentation states what this project does; the head-to-head analysis that named the alternatives
lives in the internal working notes and does not ship. So the first table maps a source *shape* to a
Collections type, and the member tables name the operations by what they do rather than by whose
spelling they use. If your source type keeps unique elements in order, it is "the ordered set" row;
if it counts copies in order, it is "the ordered bag" row.

One thing to establish before anything else: **there is no `OrderedSet<T>` here, and there is no
sorted 1-key-to-1-value map.** The ordered multiset covers both jobs — see §2.

## 2. Type mapping

| Your source type's shape | Collections type | Notes |
| --- | --- | --- |
| ordered **set** — unique elements, sorted, positional | `OrderedMultiList<T>` holding one copy of each element | By content it is a sorted set, and `GetByRank` / `GetRank` / `this[i]` / `IndexOf` give it the positional reads. The copy-count machinery is still underneath, so `DistinctCount` is the number you used to call `Count`. |
| ordered **bag** — repeated elements, sorted, positional | `OrderedMultiList<T>` | The clean like-for-like move. |
| ordered **1 key → 1 value** map | the BCL `SortedDictionary<TKey, TValue>` | Deliberately not reimplemented. Use `OrderedMultiDictionary<TKey, TValue>` only if you actually want many values per key. |
| ordered **1 key → N values** multimap | `OrderedMultiDictionary<TKey, TValue>` | Both axes ordered; enumerates expanded key/value pairs. |
| unordered bag | `MultiList<T>` | O(1) point access; no positional surface by design. |
| unordered multimap | `MultiDictionary<TKey, TValue>` | |
| composite key, one value per complete key | `MultiKeyDictionary<TKey, TValue>` (or `TwoKeyDictionary<K1, K2, V>` / `ThreeKeyDictionary<K1, K2, K3, V>`) | A trie over key components; prefix queries come free. |
| inverted lookup — "which keys hold this value?" | `ReverseMultiDictionary<V, K>`, or `MultiDictionary.AsReverse()` for a live view | |
| bijection | `BiDictionary<TLeft, TRight>` | |
| frozen / thread-safe variants | `ImmutableMultiList<T>`, `ImmutableMultiDictionary<TKey, TValue>`, `ConcurrentMultiList<T>`, `ConcurrentMultiDictionary<TKey, TValue>` | Explicit types rather than a locking convention. |

## 3. Member mapping — ordered bag / set

The table reads "what you call today" → "what to call here". Where the Collections name differs
from the natural one, the reason is in §4.

| Operation | Collections | Notes |
| --- | --- | --- |
| add one element | `Add(item)` | |
| add *n* copies | `Add(item, count)` | `count` must be positive; `0` throws instead of being a no-op. |
| membership | `Contains(item)` | |
| copies of one element | `CountOf(item)` | Returns `0` when absent. |
| number of distinct elements | `DistinctCount` (property) | |
| total copies | `TotalCount` (property) | This is the number the source library probably calls `Count`. |
| element at a position | `GetByRank(index)` or `this[index]` | Both are the same read. |
| position of an element | `GetRank(item)` or `IndexOf(item)` | Position of its **first copy**; `-1` when absent. |
| remove **one** copy | `Remove(item)` | Returns the copies **remaining**, not a `bool`. |
| remove up to *n* copies | `Remove(item, count)` | Same return shape; `count` must be positive. |
| remove **all** copies | `RemoveAllCopies(item)` | Returns `bool`. This is the one that matches "remove the element". |
| remove the copy at a position | `RemoveAt(index)` | |
| add a copy at a position | `Insert(index, item)` | Narrowed contract — see §4. |
| range between two values | `GetRange(from, to)` | Both bounds inclusive by default. |
| range between two values, exclusive bounds | `GetRange(from, to, inclusiveFrom, inclusiveTo)` | |
| range between two **positions** | no counterpart | Loop `GetByRank`, or enumerate and skip. |
| smallest / largest | `GetFirst()` / `GetLast()` | Throw `InvalidOperationException` when empty, where a `Min` / `Max` property might return `default`. |
| descending enumeration | `Reverse()` | |
| (element, copies) pairs | `EntrySet()` | |
| clear | `Clear()` | |
| the comparer | `Comparer` (property) | |
| set algebra | `UnionWith`, `IntersectionWith`, `ExceptWith`, `SymmetricExceptWith`, `IsSubsetOf`, `IsSupersetOf`, `IsProperSubsetOf`, `IsProperSupersetOf`, `Overlaps`, `IsDisjointFrom` | All multiplicity-aware. |
| nearest-neighbour probes ("give me the next element above *x*") | no counterpart | Read the rank of *x*, then `GetByRank` at it, ±1. |
| a live view bound to a key range | no counterpart | `GetRange` returns a snapshot enumeration; `AsReadOnly()` returns a live view of the **whole** collection. |

## 4. Semantic differences to plan for

These are the ones that change behaviour rather than just spelling. Read them before the first
compile of a migrated file.

**1. `Count` means copies, not distinct elements.** `TotalCount` is the number of copies;
`DistinctCount` is the number of distinct elements; `ICollection<T>.Count` (reachable through the
interface) is also the number of copies. Every index and every rank is measured over the expanded
sequence, so `shelf[3]` is the fourth *copy*, not the fourth distinct element. This is the single
most common migration error: a loop that used to iterate distinct elements now iterates copies.

**2. `Remove` returns the copies remaining, not a `bool`.** The old `bool Remove(T)` shape is
available as `RemoveAllCopies(item)` (drops every copy, returns whether anything went) or as
`ICollection<T>.Remove(item)` (drops one copy, returns whether anything went). Pick deliberately —
`if (list.Remove(x))` silently becomes a compile error, but `list.Remove(x)` used as a statement
changes meaning from "drop all" to "drop one".

**3. The comparer decides identity, not just order.** `OrderedMultiList<T>` takes an
`IComparer<T>`; two elements that compare `0` are **the same element**, they share one node and one
copy count, and the one stored is the one added first. If your source library separated "ordering"
from "equality", that separation does not exist here — and a comparer that is not consistent with
your old equality will merge elements you expected to stay apart. `MultiList<T>` is the opposite:
it takes an `IEqualityComparer<T>` and has no order.

**4. `null` is the comparer's business.** With `Comparer<T>.Default`, `null` sorts below everything
else and is an ordinary smallest element. A custom comparer decides for itself where `null` belongs
and may reject it by throwing — the type never inspects elements itself. `PackedBag<T>` cannot hold
`null` at all, because its `struct` constraint excludes it by design.

**5. Only `MultiList<T>` has content-based equality.** It overrides `Equals` / `GetHashCode` with
multiset equality (same elements, same copy counts, any order). `OrderedMultiList<T>` deliberately
does **not**: two ordered multisets with identical content are still distinct objects. Use
`IsSubsetOf` / `IsSupersetOf` or compare `EntrySet` sequences.

**6. Range bounds are two-sided inclusive by default.** `GetRange("a", "n")` returns `"a"` and
`"n"` as well as everything between them. Note that "between" is decided by the *comparer*, so a
string range is ordinal/culture-dependent exactly as your comparer says.

**7. `Insert(index, item)` is a consistency check, not a placement.** Because a comparer decides
where an element belongs, the index must name a slot inside the run of equal elements; every
accepted index produces the same multiset. An index outside that run throws
`ArgumentOutOfRangeException` rather than being silently ignored. If you do not care where the copy
lands, call `Add(item)`.

**8. `IList<T>`'s indexer setter throws `NotSupportedException`.** Writing an element over a
position would break the order the whole type is built on. `this[i]` is a read; use `RemoveAt` +
`Add` / `Insert` when you actually want to change the content.

**9. `Clone()` is a shallow copy.** Element references are shared, copy counts are independent, and
the comparer is carried over.

**10. Enumeration order of the unordered types is unspecified.** `MultiList<T>` and
`MultiDictionary<TKey, TValue>` enumerate in an implementation-defined order. Code that depended on
the source library's insertion order needs `OrderedMultiList<T>` instead, or an explicit sort.

**11. Constructors accumulate, they do not deduplicate.**
`new OrderedMultiList<T>(collection)` takes one copy of every element of the source, so duplicates
in the source become multiple copies. `new MultiList<T>(collection)` behaves the same way.

**12. There are no live sub-views.** `GetRange` returns an enumeration computed from the current
state, and `AsReadOnly()` returns a live view of the whole collection. Nothing tracks a key range.

## 5. Member mapping — ordered multimap

| Operation | Collections | Notes |
| --- | --- | --- |
| add a value under a key | `Add(key, value)` | |
| add many values under a key | `AddRange(key, values)` | |
| membership of a key | `ContainsKey(key)` | |
| membership of a pair | `Contains(key, value)` | |
| values under a key | `this[key]` | Returns an **empty collection** for an absent key rather than throwing — check `ContainsKey` when the difference matters. |
| values under a key, non-throwing read | `TryGetValue(key, out values)` | |
| count of keys | `Count` / `KeyCount` | |
| count of values across all keys | `TotalValueCount` | If your source library's `Count` counted values, this is the replacement. |
| count of values under one key | `ValueCount(key)` | |
| remove a key and all its values | `Remove(key)` | Returns `bool`. |
| remove **one occurrence** of a value | `Remove(key, value)` | Not "remove *n*" — it removes one. |
| remove many values under a key | `RemoveRange(key, values)` | |
| enumerate | `GetEnumerator()` | Expanded key/value pairs, ordered by key then value. |
| (key, values) pairs | `EntrySet()` | One entry per key. |
| key collection | `Keys` | Ascending, lazy. |
| as an `ILookup<TKey, TValue>` | `AsLookup()` | |
| value at a position / position of a key | no counterpart | `OrderedMultiDictionary` exposes no positional surface. |

## 6. Migration checklist

- [ ] Decide the **shape** first (§2). In particular, decide whether you need a sorted *set* or a
      sorted *map*, and accept the substitution given in the table.
- [ ] Pick the **comparer** explicitly and check it against the source library's equality: elements
      that compare `0` are merged.
- [ ] Audit every `Count` read: does the code mean copies (`TotalCount`) or distinct elements
      (`DistinctCount`)?
- [ ] Audit every `Remove` call: one copy (`Remove`), *n* copies (`Remove(item, count)`), or every
      copy (`RemoveAllCopies`)?
- [ ] Audit every `bool` return: `Remove` / `Remove(item, count)` now return `int`.
- [ ] Check range bounds for inclusivity; both ends are included by default.
- [ ] Check `null` handling against the comparer you chose.
- [ ] Check `Equals` / `GetHashCode` usage: only `MultiList<T>` has content equality.
- [ ] Check any live sub-view or positional range; both have no counterpart.
- [ ] Re-run your own tests with the copy-expanded enumeration in mind — assertions written against
      a distinct-element enumeration will need `DistinctItems()`.

## 7. What has no counterpart

Stated plainly, so a migration plan can account for it:

| Missing | Workaround |
| --- | --- |
| sorted set type | `OrderedMultiList<T>` with one copy per element; read `DistinctCount` |
| sorted 1:1 map | BCL `SortedDictionary<TKey, TValue>` |
| positional ranges (remove-by-index-range, enumerate-by-index-range) | loops over `RemoveAt` / `GetByRank` |
| positional surface on the ordered **multimap** | none — `OrderedMultiDictionary` has no `ElementAt` / `IndexOfKey` |
| live sub-views bound to a key range | `GetRange` snapshot, or `AsReadOnly()` over the whole collection |
| nearest-neighbour probes | `GetRank` + `GetByRank` ± 1 |
| a LINQ provider | paging composes with `IQueryable<T>`; queries are not translated |
| serializers | `ToSerializableModel()` / `FromModel()` give a plain snapshot for your own serializer |
