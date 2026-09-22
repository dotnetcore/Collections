# Where Collections fits

This note states what `DotNetCore.Collections` is for, what it is *not* for, and how it relates to
the older collection libraries a .NET codebase is likely to already carry. It is deliberately
written from this repository's own capability axes: the goal is to make the choice decidable, not to
argue that any one library wins.

## 1. What this library is

Two independent packages:

| Package | What it covers |
| --- | --- |
| `DotNetCore.Collections.Paginable` | paging over `IEnumerable<T>` and `IQueryable<T>`, plus provider-specific extensions for nine ORM stacks |
| `DotNetCore.Collections.Multi` | the "multi" families: multisets, multimaps, composite keys, bijection, inverted lookup, and the concurrent / immutable / packed / ordered specializations |

Both target the same broad framework matrix — `net451`, `net461`, `net47`, `net48`,
`netstandard2.0`, `netstandard2.1`, `net6.0` … `net10.0` — from one code path, with nullable
reference annotations and XML documentation on every public member.

The through-line is **the shapes a plain `Dictionary<TKey, TValue>` or `List<T>` cannot express**.
Almost everything in the Multi package answers one of a small set of questions:

- *may the element repeat?* → `MultiList<T>`, `PackedBag<T>`, `SpanBag<T>`, `FrequencyPriorityBag<T>`
- *may the value repeat under one key?* → `MultiDictionary<TKey, TValue>`
- *may the key be assembled from parts?* → `MultiKeyDictionary<TKey, TValue>`, `TwoKeyDictionary<K1, K2, V>`, `ThreeKeyDictionary<K1, K2, K3, V>`
- *does the mapping have to run both ways?* → `BiDictionary<TLeft, TRight>`, `ReverseMultiDictionary<V, K>`
- *does it have to be ordered, and does "the k-th element" have to be answerable?* → `OrderedMultiList<T>`, `OrderedMultiDictionary<TKey, TValue>`
- *does it have to survive concurrency or be frozen?* → `Concurrent*`, `Immutable*`

## 2. The capability axes

These are the dimensions on which this library's scope is set. Each one is a design decision that
was made and paid for, and each one is where a narrower library stops.

**Multisets and multimaps are first-class.** A multiset counts copies; a multimap holds many values
per key. In the BCL both are "a `Dictionary<TKey, int>` you maintain yourself" or "a
`Dictionary<TKey, List<TValue>>` you remember to clean up". Here they are types with the operations
you actually want: `CountOf`, `DistinctCount`, copy-expanded enumeration, `EntrySet`, set algebra
that respects multiplicity, and `Remove(item, count)` returning the copies that remain.

**Composite keys without tuple plumbing.** `MultiKeyDictionary` is a trie over key components, so
"N components → 1 value" costs one lookup rather than a string join or a nested dictionary, and
prefix queries come for free. `TwoKeyDictionary<K1, K2, V>` and `ThreeKeyDictionary<K1, K2, K3, V>`
are the strongly typed facades for the common arities.

**Both directions of a mapping.** `BiDictionary` keeps the forward and reverse maps consistent by
construction, so a bijection costs one insert and two O(1) lookups instead of a hand-maintained
second dictionary that drifts. `ReverseMultiDictionary` (and `MultiDictionary.AsReverse()`) answers
"which keys hold this value?" — an inverted index, which is a query shape the BCL does not model at
all.

**Order *and* position.** `OrderedMultiList<T>` is a sorted multiset whose engine caches the number
of copies below each node, so `GetByRank` / `GetRank` / `GetMedian` / `GetQuantile` are O(log n)
descents rather than scans, and the type implements `IReadOnlyList<T>` and `IList<T>` over the
expanded sequence. Ordering and positional access are treated as one feature, not two.

**Concurrency and immutability are separate types, not a lock you add.** `ConcurrentMultiDictionary`
shards; `ConcurrentMultiList` takes a single lock; `ImmutableMultiList` / `ImmutableMultiDictionary`
are write-once with a builder. The choice is visible in the type name, which is what makes it
reviewable.

**Paging is a library, not a helper method.** Offset paging (`GetPage(pageNumber, pageSize)`) and
keyset / seek paging (`GetPageByKeyset(keySelector, lastKey, pageSize)`) over `IEnumerable<T>` and
`IQueryable<T>`, with `IPage<T>` / `PageMetadata` / `TotalPageCount` / `TotalMemberCount` as a real
contract, plus fragment paging for a slice of an existing page. The nine ORM integration packages
make it a single call on the source you already have, and the shared per-provider extension class is
emitted at build time by a source generator — so the surface stays identical across providers
without nine copies of the same file.

**Specializations when the general type is the wrong shape.** `PackedBag<T>` is a dense `(value,
count)` struct array for a small value-type domain — no hash table, no boxing in storage. `SpanBag<T>`
is a `ref struct` over caller-provided `stackalloc` storage for counting inside one method with zero
heap allocation. `FrequencyPriorityBag<T>` answers "most frequent first" and "Top-K" directly.

**Framework breadth from one code path.** `net451` through `net10.0`. This is a maintenance cost
this repository accepts on purpose: it means a library targeting an older framework can take the
same package without a second build.

## 3. What it deliberately does not do

Being explicit about the gaps is what makes the rest of this note trustworthy.

| Not provided | What to use instead |
| --- | --- |
| A sorted **set** type (unique elements, ordered, positional) | `OrderedMultiList<T>` holding one copy of each element. By content it is a sorted set, and `GetByRank` / `GetRank` give it the positional reads; the copy-count machinery is still there underneath. |
| A sorted **1 key → 1 value** map | the BCL `SortedDictionary<TKey, TValue>`, or `OrderedMultiDictionary<TKey, TValue>` when the multimap shape is what you want. |
| Positional **ranges** — remove-by-index-range, enumerate-by-index-range | repeated `RemoveAt(index)` / `GetByRank(index)`. `OrderedMultiDictionary` has no positional surface at all. |
| Live **sub-views** (a view bound to a key range that tracks the parent) | enumerate `GetRange(from, to)` / `AsReadOnly()`. |
| A **LINQ provider** of its own | the library's paging composes with `IQueryable<T>`; it does not translate queries. |
| **Serializers** beyond an explicit model | `ToSerializableModel()` / `FromModel()` give a plain snapshot to hand to whatever serializer you already use. |

## 4. Choosing between the hash path and the tree path

The same question has two answers in this library, and the choice is about the workload, not about
which type is "better".

| | Hash path (`MultiList<T>`, `MultiDictionary<TKey, TValue>`, `Concurrent*`, `Immutable*`) | Tree path (`OrderedMultiList<T>`, `OrderedMultiDictionary<TKey, TValue>`) |
| --- | --- | --- |
| single-element add / lookup / remove | O(1) average | O(log n) worst case |
| enumeration order | unspecified — an implementation detail | sorted, duplicates expanded |
| positional reads (`GetByRank`, `GetRank`, median, quantile) | not available | O(log n) |
| range enumeration | filter while enumerating | O(log n + k) |
| identity decided by | `IEqualityComparer<T>` | `IComparer<T>` (a comparison of `0` means the same element) |
| memory shape | hash buckets | wide nodes, cached sub-tree totals |

The rule of thumb: **if you never need an order, do not pay for one.** The tree path is the right
choice exactly when you need sorted enumeration, a range, or a position — and the hash path is the
right choice when you need O(1) point access over a large key space.

## 5. Honest limits

- **The ordered path trades a small probe for a wide node.** Measured with BenchmarkDotNet, the
  order-statistic engine made ascending iteration, wide ranges, bulk removal and every positional
  read substantially cheaper, and made *small, single-key* probes slower — a wide node replaces the
  old single-key hop with a binary search inside a page. Both directions are recorded in the
  changelog rather than only the favourable one.
- **Iteration on the ordered types is not the fastest available.** A head-to-head sweep against a
  reference implementation of the same shapes ([numbers](HeadToHeadBenchmarks.md)) puts point
  operations, rank reads and positional removal ahead, and ascending iteration and range enumeration
  behind by roughly 2.6–3.1x — the cache-locality dividend of a wide node is real and this engine
  does not collect all of it. `OrderedMultiDictionary<TKey, TValue>` is the worst case on that axis,
  and the sweep found a concrete reason: enumerating it allocates about 112 B per key, because each
  key's inner collection is walked through an interface-typed `foreach` over a `yield` iterator. That
  is logged as a follow-up rather than quietly left out of the table.
- **More types means a selection step.** Twelve-plus collection types with precise names is a
  deliberate trade: each one is unambiguous once found, but there is a "which one" question that a
  single general-purpose type would not ask. The `Choosing a type` section of the README is the
  answer to it.
- **Multiset semantics are not `List<T>` semantics.** `Count` counts copies, `Remove` returns the
  copies remaining rather than a `bool`, and equality is content-based for `MultiList<T>` only. See
  [`migrating.md`](migrating.md) for the full list of differences to expect.
- **Ordered types do not override `Equals`.** Two `OrderedMultiList<T>` with the same content are
  still distinct objects; use `IsSubsetOf` / `IsSupersetOf` or compare `EntrySet` sequences.

## 6. Summary

The classic sorted-collection libraries answer one question very well: *keep this set or dictionary
in order, and let me index into it.* That is a good question, and this library answers it too — with
the ordered multiset and the order-statistic engine behind it.

The reason to reach for `DotNetCore.Collections` is everything the sorted-collection libraries do
not model: multisets and multimaps, composite keys as a trie, bijection, inverted lookup,
concurrency and immutability as explicit types, packed and stack-only specializations, paging as a
library with ORM integrations, and a framework matrix that reaches back to `net451`. The two sets
of concerns barely overlap, and a codebase can reasonably hold both.
