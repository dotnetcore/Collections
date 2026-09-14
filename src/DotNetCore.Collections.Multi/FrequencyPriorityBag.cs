using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// CS8714: T is deliberately unconstrained (null elements are supported through a dedicated
// bucket, but the type parameter itself must stay nullable-friendly); the "notnull" key
// constraint of the annotated Dictionary<TKey, TValue> (net5.0+ reference assemblies) is a false
// positive here - a stored null never enters the Dictionary, it lives in its own bucket fields.
#pragma warning disable CS8714

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Represents a frequency priority bag: a bag that knows which element occurs most often and
    /// can hand it out in O(log n), making Top-K queries a natural fit (F6-08).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The bag combines two structures kept in step: a <see cref="Dictionary{TKey,TValue}"/>
    /// frequency index (element &#8594; its count and a stamp of when that count last changed) as
    /// the source of truth, and a binary max-heap of count snapshots ordered by count. Popping
    /// the most frequent element walks the heap top down, discarding snapshots that no longer
    /// match the index (the standard lazy-deletion scheme), so a single
    /// <see cref="PopMost"/> runs in O(log n) amortized. The heap is compacted back to the live
    /// element set when stale snapshots outnumber the live entries, which bounds its memory to
    /// O(distinct elements).
    /// </para>
    /// <para>
    /// <b>Tie policy</b>: among elements with the same count, the element that reached its
    /// current count <em>earliest</em> pops first - every count change (add or remove) stamps a
    /// fresh monotonically increasing sequence number, and the heap breaks ties on the smaller
    /// stamp. The order is therefore deterministic and FIFO-flavoured: at equal frequency, the
    /// longest-waiting element wins. This is a documented choice; a most-recently-changed tie
    /// break would invert the comparison of the stamps.
    /// </para>
    /// <para>
    /// Bag semantics mirror <see cref="MultiList{T}"/> where the two meet: duplicates are counted
    /// (<see cref="Add(T)"/>, <see cref="CountOf"/>, copy-expanded enumeration),
    /// <see cref="Remove(T)"/> takes one copy away and returns the number remaining, an element
    /// disappears automatically once its last copy is removed, and <c>null</c> is a first-class
    /// element stored in a dedicated bucket (with its own tie stamp) - exactly as in
    /// <see cref="MultiList{T}"/>, while <see cref="Dictionary{TKey,TValue}"/> itself rejects
    /// <c>null</c> keys. Unlike the sorted <see cref="OrderedMultiList{T}"/>, the enumeration of
    /// this type carries no order guarantee beyond its copy expansion: the structures are tuned
    /// for answering "what is most frequent", not for ordered traversal.
    /// </para>
    /// <para>
    /// This class is not thread-safe. Wrap it with external synchronization for concurrent use.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var bag = new FrequencyPriorityBag&lt;string&gt;();
    /// bag.AddRange(new[] { "a", "b", "b", "c", "b", "c" });
    ///
    /// bag.PeekMost();        // "b" (3 copies) - O(1) peek, O(log n) amortized when stale
    /// bag.PopMost();         // "b", now 2 copies
    /// bag.PopMost();         // "c" or "b" - both at 2, the earliest-stamped one wins
    /// </code>
    /// </example>
    public class FrequencyPriorityBag<T> : IEnumerable<T>
    {
        /// <summary>
        /// The frequency index: per element its current count and the stamp of the moment that
        /// count last changed. This is the source of truth; the heap only holds snapshots that
        /// are validated against it.
        /// </summary>
        private readonly Dictionary<T, Entry> _index;

        /// <summary>
        /// The <c>null</c>-element bucket of the frequency index, held outside
        /// <see cref="_index"/> because <see cref="Dictionary{TKey,TValue}"/> rejects a
        /// <c>null</c> key. Mirrors <see cref="MultiList{T}"/>'s null bucket; no allocation is
        /// paid until a <c>null</c> element is actually stored.
        /// </summary>
        private int _nullCount;

        /// <summary>The null bucket's tie stamp, meaningful only while <see cref="_nullCount"/> is positive.</summary>
        private long _nullSeq;

        /// <summary>
        /// The lazy binary max-heap of count snapshots. A snapshot is <em>live</em> while an
        /// element's index entry still matches its count and stamp; anything else is stale and is
        /// discarded when it surfaces at the top.
        /// </summary>
        private List<HeapEntry> _heap;

        /// <summary>Monotonic tie-break stamp source, bumped on every count change.</summary>
        private long _nextSeq;

        /// <summary>Total copies across all elements, cached.</summary>
        private int _totalCount;

        /// <summary>One frequency-index record: the count and when it last changed.</summary>
        private struct Entry
        {
            public int Count;
            public long Seq;
        }

        /// <summary>
        /// One heap snapshot. <see cref="IsNull"/> separates a stored <c>null</c> element from a
        /// non-null element whose value happens to equal <c>default(T)</c> - both look alike in
        /// the snapshot's <see cref="Value"/> slot, but they live in different places in the
        /// index.
        /// </summary>
        private struct HeapEntry
        {
            public T Value;
            public bool IsNull;
            public int Count;
            public long Seq;
        }

        /// <summary>
        /// Initializes an empty <see cref="FrequencyPriorityBag{T}"/>.
        /// </summary>
        public FrequencyPriorityBag()
        {
            _index = new Dictionary<T, Entry>();
            _heap = new List<HeapEntry>();
        }

        /// <summary>
        /// Initializes a <see cref="FrequencyPriorityBag{T}"/> containing one copy of each
        /// element of the specified collection.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        public FrequencyPriorityBag(IEnumerable<T> collection) : this()
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            foreach (var item in collection)
            {
                Add(item);
            }
        }

        /// <summary>
        /// Gets the total number of copies across all elements.
        /// </summary>
        public int TotalCount => _totalCount;

        /// <summary>
        /// Gets the number of distinct elements. A stored <c>null</c> counts as one distinct
        /// element.
        /// </summary>
        public int DistinctCount => _index.Count + (_nullCount > 0 ? 1 : 0);

        /// <summary>
        /// Gets a value indicating whether the bag holds no copies at all.
        /// </summary>
        public bool IsEmpty => _totalCount == 0;

        // ------------------------------------------------------------------
        // Add
        // ------------------------------------------------------------------

        /// <summary>
        /// Adds a single copy of the element, raising it to its new count in the priority
        /// structure.
        /// </summary>
        /// <example>
        /// <code>
        /// bag.Add("apple");
        /// </code>
        /// </example>
        public void Add(T item)
        {
            Add(item, 1);
        }

        /// <summary>
        /// Adds the specified number of copies of the element.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="times"/> is less than
        /// or equal to zero.</exception>
        /// <example>
        /// <code>
        /// bag.Add("apple", 3);
        /// </code>
        /// </example>
        public void Add(T item, int times)
        {
            if (times <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(times), times, "The number of copies must be positive.");
            }

            if (item == null)
            {
                _nullCount += times;
                PushSnapshot(default!, true, _nullCount, _nullSeq = ++_nextSeq);
            }
            else
            {
                var count = _index.TryGetValue(item, out var entry) ? entry.Count : 0;
                _index[item] = new Entry { Count = count + times, Seq = ++_nextSeq };
                PushSnapshot(item, false, count + times, _nextSeq);
            }

            _totalCount += times;
        }

        /// <summary>
        /// Adds one copy of each element of the specified collection.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bag.AddRange(new[] { "a", "b", "b" });
        /// </code>
        /// </example>
        public void AddRange(IEnumerable<T> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            foreach (var item in items)
            {
                Add(item);
            }
        }

        // ------------------------------------------------------------------
        // Lookup
        // ------------------------------------------------------------------

        /// <summary>
        /// Determines whether the bag contains at least one copy of the element.
        /// </summary>
        /// <example>
        /// <code>
        /// bool has = bag.Contains("apple");
        /// </code>
        /// </example>
        public bool Contains(T item)
        {
            return item == null ? _nullCount > 0 : _index.ContainsKey(item);
        }

        /// <summary>
        /// Gets the number of copies of the element (zero when absent).
        /// </summary>
        /// <example>
        /// <code>
        /// int copies = bag.CountOf("apple");
        /// </code>
        /// </example>
        public int CountOf(T item)
        {
            if (item == null)
            {
                return _nullCount;
            }

            return _index.TryGetValue(item, out var entry) ? entry.Count : 0;
        }

        // ------------------------------------------------------------------
        // Priority access
        // ------------------------------------------------------------------

        /// <summary>
        /// Returns the most frequent element without removing it. Among elements with the same
        /// count, the one that reached its current count earliest wins (the documented tie
        /// policy). Throws <see cref="InvalidOperationException"/> when the bag is empty.
        /// </summary>
        /// <exception cref="InvalidOperationException">The bag is empty.</exception>
        /// <example>
        /// <code>
        /// string mostFrequent = bag.PeekMost();
        /// </code>
        /// </example>
        public T PeekMost()
        {
            if (!TryPeekMost(out var item))
            {
                throw new InvalidOperationException("The bag is empty.");
            }

            return item;
        }

        /// <summary>
        /// Returns the most frequent element without removing it. Among elements with the same
        /// count, the one that reached its current count earliest wins. Returns <c>false</c> when
        /// the bag is empty.
        /// </summary>
        public bool TryPeekMost(out T item)
        {
            while (_heap.Count > 0)
            {
                var top = _heap[0];
                if (IsLive(top))
                {
                    item = top.IsNull ? default! : top.Value;
                    return true;
                }

                // Stale snapshot: swap it to the end and drop it, then look at the new top.
                RemoveTop();
            }

            item = default!;
            return false;
        }

        /// <summary>
        /// Removes and returns the most frequent element - one copy of it, mirroring
        /// <see cref="MultiList{T}.Remove(T)"/>'s multiplicity semantics. Among elements with the
        /// same count, the one that reached its current count earliest is popped first. Throws
        /// <see cref="InvalidOperationException"/> when the bag is empty. Repeated calls drain
        /// the bag in descending-frequency order, which is exactly the Top-K query.
        /// </summary>
        /// <exception cref="InvalidOperationException">The bag is empty.</exception>
        /// <example>
        /// <code>
        /// var topK = new List&lt;string&gt;();
        /// for (var i = 0; i &lt; 3; i++)
        /// {
        ///     topK.Add(bag.PopMost()); // the k most frequent elements, most frequent first
        /// }
        /// </code>
        /// </example>
        public T PopMost()
        {
            if (!TryPopMost(out var item))
            {
                throw new InvalidOperationException("The bag is empty.");
            }

            return item;
        }

        /// <summary>
        /// Removes and returns the most frequent element - one copy of it. Among elements with
        /// the same count, the one that reached its current count earliest is popped first.
        /// Returns <c>false</c> when the bag is empty.
        /// </summary>
        public bool TryPopMost(out T item)
        {
            while (_heap.Count > 0)
            {
                var top = _heap[0];
                if (IsLive(top))
                {
                    item = top.IsNull ? default! : top.Value;

                    // Decrease the count in the index and re-snapshot it at the new count; a
                    // count reaching zero removes the element entirely (MultiList invariant).
                    if (top.IsNull)
                    {
                        _nullCount--;
                        if (_nullCount > 0)
                        {
                            PushSnapshot(default!, true, _nullCount, _nullSeq = ++_nextSeq);
                        }
                    }
                    else
                    {
                        var entry = _index[top.Value];
                        if (entry.Count == 1)
                        {
                            _index.Remove(top.Value);
                        }
                        else
                        {
                            _index[top.Value] = new Entry { Count = entry.Count - 1, Seq = ++_nextSeq };
                            PushSnapshot(top.Value, false, entry.Count - 1, _nextSeq);
                        }
                    }

                    _totalCount--;
                    RemoveTop();
                    return true;
                }

                RemoveTop();
            }

            item = default!;
            return false;
        }

        // ------------------------------------------------------------------
        // Remove
        // ------------------------------------------------------------------

        /// <summary>
        /// Removes a single copy of the element and returns the number of copies remaining
        /// afterwards. Returns zero when the element is absent (no copy was removed). The element
        /// disappears from the priority structure once its last copy is removed.
        /// </summary>
        /// <example>
        /// <code>
        /// int remaining = bag.Remove("apple"); // removes a single copy
        /// </code>
        /// </example>
        public int Remove(T item)
        {
            if (item == null)
            {
                if (_nullCount == 0)
                {
                    return 0;
                }

                _nullCount--;
                _totalCount--;
                return _nullCount;
            }

            if (!_index.TryGetValue(item, out var entry))
            {
                return 0;
            }

            _totalCount--;
            if (entry.Count == 1)
            {
                _index.Remove(item);
                return 0;
            }

            _index[item] = new Entry { Count = entry.Count - 1, Seq = ++_nextSeq };
            PushSnapshot(item, false, entry.Count - 1, _nextSeq);
            return entry.Count - 1;
        }

        /// <summary>
        /// Removes every copy of the element. Returns <c>true</c> when at least one copy was
        /// removed.
        /// </summary>
        /// <example>
        /// <code>
        /// bool removed = bag.RemoveAllCopies("apple");
        /// </code>
        /// </example>
        public bool RemoveAllCopies(T item)
        {
            if (item == null)
            {
                if (_nullCount == 0)
                {
                    return false;
                }

                _totalCount -= _nullCount;
                _nullCount = 0;
                return true;
            }

            if (!_index.TryGetValue(item, out var entry))
            {
                return false;
            }

            _totalCount -= entry.Count;
            _index.Remove(item);
            return true;
        }

        /// <summary>
        /// Removes all elements and counts.
        /// </summary>
        /// <example>
        /// <code>
        /// bag.Clear();
        /// </code>
        /// </example>
        public void Clear()
        {
            _index.Clear();
            _heap.Clear();
            _nullCount = 0;
            _nextSeq = 0;
            _totalCount = 0;
        }

        // ------------------------------------------------------------------
        // Projection and copying
        // ------------------------------------------------------------------

        /// <summary>
        /// Enumerates every distinct element together with its copy count. The order is not
        /// specified: the type is tuned for "what is most frequent", not for ordered traversal.
        /// </summary>
        /// <example>
        /// <code>
        /// foreach (var (item, count) in bag.EntrySet()) { }
        /// </code>
        /// </example>
        public IEnumerable<(T Item, int Count)> EntrySet()
        {
            foreach (var pair in _index)
            {
                yield return (pair.Key, pair.Value.Count);
            }

            if (_nullCount > 0)
            {
                yield return (default!, _nullCount);
            }
        }

        /// <summary>
        /// Creates a deep copy of the bag: elements are shared, counts and priority structure are
        /// independent.
        /// </summary>
        /// <example>
        /// <code>
        /// FrequencyPriorityBag&lt;string&gt; copy = bag.Clone();
        /// </code>
        /// </example>
        public FrequencyPriorityBag<T> Clone()
        {
            var clone = new FrequencyPriorityBag<T>();
            foreach (var pair in _index)
            {
                clone.Add(pair.Key, pair.Value.Count);
            }

            if (_nullCount > 0)
            {
                // default(T) is the null element here: the null bucket only ever fills when T is
                // a reference type, and Add routes the null element back into the bucket.
                clone.Add(default!, _nullCount);
            }

            return clone;
        }

        // ------------------------------------------------------------------
        // Enumeration
        // ------------------------------------------------------------------

        /// <summary>
        /// Enumerates the bag copy-expanded - one entry per copy - with no order guarantee. For
        /// the compact per-element view use <see cref="EntrySet()"/>; for frequency-ordered
        /// draining use <see cref="PopMost"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// foreach (var item in bag) { /* one iteration per copy */ }
        /// </code>
        /// </example>
        public IEnumerator<T> GetEnumerator()
        {
            foreach (var pair in _index)
            {
                for (var c = 0; c < pair.Value.Count; c++)
                {
                    yield return pair.Key;
                }
            }

            for (var c = 0; c < _nullCount; c++)
            {
                yield return default!;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns the contents in copy-expanded form, comma separated.
        /// </summary>
        /// <example>
        /// <code>
        /// string text = bag.ToString();
        /// </code>
        /// </example>
        public override string ToString()
        {
            return string.Join(",", this);
        }

        // ------------------------------------------------------------------
        // Internals: lazy heap (F6-08)
        // ------------------------------------------------------------------

        /// <summary>
        /// Determines whether a heap snapshot still matches the index - same count, same stamp,
        /// element still present. Anything else is a stale leftover of an overwritten count
        /// change or a removed element.
        /// </summary>
        private bool IsLive(HeapEntry snapshot)
        {
            if (snapshot.IsNull)
            {
                return _nullCount == snapshot.Count && _nullSeq == snapshot.Seq;
            }

            return _index.TryGetValue(snapshot.Value, out var entry)
                && entry.Count == snapshot.Count
                && entry.Seq == snapshot.Seq;
        }

        /// <summary>Records a count snapshot into the heap.</summary>
        private void PushSnapshot(T value, bool isNull, int count, long seq)
        {
            _heap.Add(new HeapEntry { Value = value, IsNull = isNull, Count = count, Seq = seq });
            SiftUp(_heap.Count - 1);

            // Compaction: stale snapshots are otherwise only reclaimed on pops; rebuilding once
            // they dominate the heap bounds its memory to O(distinct elements) under churn.
            if (_heap.Count > 2 * (DistinctCount + 16))
            {
                RebuildHeap();
            }
        }

        /// <summary>
        /// Discards the heap top (validated stale or consumed) and restores the heap invariant.
        /// </summary>
        private void RemoveTop()
        {
            var last = _heap.Count - 1;
            _heap[0] = _heap[last];
            _heap.RemoveAt(last);
            if (_heap.Count > 1)
            {
                SiftDown(0);
            }
        }

        /// <summary>
        /// Rebuilds the heap from live index entries only - O(distinct) - discarding every stale
        /// snapshot at once.
        /// </summary>
        private void RebuildHeap()
        {
            _heap.Clear();
            foreach (var pair in _index)
            {
                _heap.Add(new HeapEntry { Value = pair.Key, IsNull = false, Count = pair.Value.Count, Seq = pair.Value.Seq });
            }

            if (_nullCount > 0)
            {
                _heap.Add(new HeapEntry { Value = default!, IsNull = true, Count = _nullCount, Seq = _nullSeq });
            }

            for (var i = _heap.Count / 2 - 1; i >= 0; i--)
            {
                SiftDown(i);
            }
        }

        /// <summary>
        /// Heap ordering: higher count first; on equal counts the <em>smaller</em> stamp first
        /// (the element that reached this count earliest - the documented tie policy).
        /// </summary>
        private bool Before(HeapEntry candidate, HeapEntry against)
        {
            if (candidate.Count != against.Count)
            {
                return candidate.Count > against.Count;
            }

            return candidate.Seq < against.Seq;
        }

        private void SiftUp(int index)
        {
            var node = _heap[index];
            while (index > 0)
            {
                var parent = (index - 1) / 2;
                if (!Before(node, _heap[parent]))
                {
                    break;
                }

                _heap[index] = _heap[parent];
                index = parent;
            }

            _heap[index] = node;
        }

        private void SiftDown(int index)
        {
            var node = _heap[index];
            while (true)
            {
                var child = index * 2 + 1;
                if (child >= _heap.Count)
                {
                    break;
                }

                if (child + 1 < _heap.Count && Before(_heap[child + 1], _heap[child]))
                {
                    child++;
                }

                if (!Before(_heap[child], node))
                {
                    break;
                }

                _heap[index] = _heap[child];
                index = child;
            }

            _heap[index] = node;
        }
    }
}
