using System;
using System.Collections;
using System.Collections.Generic;

// CS8714: T is deliberately unconstrained - null is a valid element for reference types (it lives
// in the ring slots and the projection buffers), and ToDictionary() rejects a null element before
// it can reach the Dictionary. The "notnull" key constraint of the annotated
// Dictionary<TKey, TValue> (net5.0+ reference assemblies) is therefore a false positive here.
#pragma warning disable CS8714

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Represents a bounded bag: a multiset whose total number of copies is capped at a fixed
    /// capacity, keeping the newest copies and evicting the oldest once the cap is reached.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A bounded bag is the sliding-window member of the bag family. Where
    /// <see cref="MultiList{T}"/> grows without limit and <see cref="PackedBag{T}"/> is a
    /// value-type histogram, this type holds at most <see cref="Capacity"/> copies in total and,
    /// when a new copy would exceed that cap, silently drops the <em>oldest</em> copy to make room
    /// - a first-in-first-out eviction policy, so the bag always describes the most recent
    /// <see cref="Capacity"/> additions. Adding therefore never fails: <see cref="Add(T)"/> is a
    /// <c>void</c> method, because there is no overflow to report.
    /// </para>
    /// <para>
    /// <b>Copy semantics.</b> As in the rest of the family, duplicates are counted: enumeration is
    /// copy-expanded (one iteration per copy, oldest first), <see cref="CountOf(T)"/> reports
    /// multiplicities, and the inherited <c>Count</c> is the number of copies - the same value as
    /// <see cref="TotalCount"/> - not the number of distinct elements. Use
    /// <see cref="DistinctCount"/> for the latter.
    /// </para>
    /// <para>
    /// <b>Complexity.</b> Storage is a single ring buffer of <see cref="Capacity"/> slots holding
    /// one element each, so <see cref="Add(T)"/> and eviction are O(1). Because the slots carry the
    /// elements themselves and no hash table is kept, the lookup and projection members
    /// (<see cref="Contains(T)"/>, <see cref="CountOf(T)"/>, <see cref="DistinctCount"/>,
    /// <see cref="DistinctItems"/>, <see cref="EntrySet"/>, <see cref="ToDictionary"/>) are linear
    /// in the number of copies held - which is bounded by <see cref="Capacity"/>, the whole point
    /// of this type. It is aimed at small, bounded windows, not at large collections.
    /// </para>
    /// <para>
    /// <b>Why this type does not implement <see cref="IMultiSet{T}"/>.</b> That contract states
    /// that the order in which <c>AddRange</c> supplies its elements does not affect the resulting
    /// multiset. For a bounded bag the order <em>does</em> affect the result - the earliest
    /// elements are the first to be evicted - so implementing the interface would contradict its
    /// documented guarantee. Like <see cref="PackedBag{T}"/>, this type is a standalone bag rather
    /// than an <see cref="IMultiSet{T}"/> implementation.
    /// </para>
    /// <para>
    /// <c>null</c> is a valid element for reference types, as in <see cref="MultiList{T}"/>;
    /// <see cref="ToDictionary"/> is the one member that can not represent it and throws.
    /// </para>
    /// <para>
    /// This class is not thread-safe. Wrap it with external synchronization for concurrent use.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var recent = new BoundedBag&lt;string&gt;(3);
    /// recent.Add("a");
    /// recent.Add("b");
    /// recent.Add("c");
    /// recent.Add("d");            // "a" is evicted
    ///
    /// recent.TotalCount;          // 3
    /// recent.Contains("a");       // false
    /// recent.CountOf("d");        // 1
    /// foreach (var item in recent) { }   // b, c, d
    /// </code>
    /// </example>
    public class BoundedBag<T> : IEnumerable<T>, IReadOnlyCollection<T>
    {
        private readonly T[] _slots;
        private readonly IEqualityComparer<T> _comparer;
        private int _head;
        private int _count;

        /// <summary>
        /// Initializes an empty <see cref="BoundedBag{T}"/> with the specified capacity, using the
        /// default equality comparer for the element type.
        /// </summary>
        /// <param name="capacity">The maximum number of copies the bag may hold. Must be positive.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is not positive.</exception>
        public BoundedBag(int capacity) : this(capacity, null)
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="BoundedBag{T}"/> with the specified capacity and element
        /// equality comparer.
        /// </summary>
        /// <param name="capacity">The maximum number of copies the bag may hold. Must be positive.</param>
        /// <param name="comparer">
        /// The comparer used to decide whether two elements are the same, or <c>null</c> to use
        /// <see cref="EqualityComparer{T}.Default"/>.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is not positive.</exception>
        public BoundedBag(int capacity, IEqualityComparer<T>? comparer)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be positive.");
            }

            _slots = new T[capacity];
            _comparer = comparer ?? EqualityComparer<T>.Default;
        }

        /// <summary>
        /// Gets the maximum number of copies the bag may hold. Adding a copy while the bag holds
        /// this many evicts the oldest copy.
        /// </summary>
        public int Capacity => _slots.Length;

        /// <summary>
        /// Gets the total number of copies currently held by the bag (never greater than
        /// <see cref="Capacity"/>).
        /// </summary>
        public int TotalCount => _count;

        /// <summary>
        /// Gets the total number of copies currently held by the bag (alias of
        /// <see cref="TotalCount"/>, implementing <see cref="IReadOnlyCollection{T}.Count"/>).
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Gets the number of distinct elements currently held by the bag, regardless of how many
        /// copies of each are present.
        /// </summary>
        /// <remarks>
        /// Runs in O(<see cref="TotalCount"/>): the slots are scanned and grouped, because no hash
        /// table is kept. <see cref="TotalCount"/> is bounded by <see cref="Capacity"/>.
        /// </remarks>
        public int DistinctCount
        {
            get
            {
                var seen = new HashSet<T>(_comparer);
                for (var i = 0; i < _count; i++)
                {
                    seen.Add(_slots[Physical(i)]);
                }

                return seen.Count;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the bag holds no copies at all.
        /// </summary>
        public bool IsEmpty => _count == 0;

        /// <summary>
        /// Gets a value indicating whether the bag holds exactly <see cref="Capacity"/> copies, so
        /// that the next addition will evict the oldest copy.
        /// </summary>
        public bool IsFull => _count == _slots.Length;

        /// <summary>
        /// Gets the comparer used to decide whether two elements are the same.
        /// </summary>
        public IEqualityComparer<T> Comparer => _comparer;

        /// <summary>Gets a value indicating whether the bag is read-only. Always <c>false</c>.</summary>
        public bool IsReadOnly => false;

        // ------------------------------------------------------------------
        // Add
        // ------------------------------------------------------------------

        /// <summary>
        /// Adds a single copy of the element. When the bag is already full, the oldest copy is
        /// evicted to make room.
        /// </summary>
        /// <example>
        /// <code>
        /// bag.Add("a");
        /// </code>
        /// </example>
        public void Add(T item)
        {
            Add(item, 1);
        }

        /// <summary>
        /// Adds the specified number of copies of the element, evicting the oldest copies as needed
        /// to stay within <see cref="Capacity"/>.
        /// </summary>
        /// <param name="item">The element to add. <c>null</c> is a valid element for reference types.</param>
        /// <param name="times">The number of copies to add. Must be positive.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="times"/> is zero or negative.</exception>
        /// <remarks>
        /// When <paramref name="times"/> is at least <see cref="Capacity"/>, nothing that was in the
        /// bag can survive: the bag ends up holding exactly <see cref="Capacity"/> copies of
        /// <paramref name="item"/>.
        /// </remarks>
        /// <example>
        /// <code>
        /// var bag = new BoundedBag&lt;string&gt;(2);
        /// bag.Add("a", 3);   // the bag holds two copies of "a"
        /// </code>
        /// </example>
        public void Add(T item, int times)
        {
            if (times <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(times), times, "The number of copies must be positive.");
            }

            if (times >= _slots.Length)
            {
                // The window is no larger than the request, so nothing older can survive: fill the
                // whole ring with the element instead of looping `times` times (which may be huge).
                for (var i = 0; i < _slots.Length; i++)
                {
                    _slots[i] = item;
                }

                _head = 0;
                _count = _slots.Length;
                return;
            }

            for (var i = 0; i < times; i++)
            {
                AddCopy(item);
            }
        }

        /// <summary>
        /// Adds one copy of each element of the specified collection, in order, evicting the oldest
        /// copies as needed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is <c>null</c>.</exception>
        /// <remarks>
        /// The order of <paramref name="items"/> matters here, unlike for the unbounded bags: if the
        /// sequence exceeds <see cref="Capacity"/>, the earliest elements are the ones evicted.
        /// </remarks>
        /// <example>
        /// <code>
        /// bag.AddRange(new[] { "a", "b", "c" });
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
        /// <remarks>Runs in O(<see cref="TotalCount"/>) - the slots are scanned.</remarks>
        /// <example>
        /// <code>
        /// bool has = bag.Contains("a");
        /// </code>
        /// </example>
        public bool Contains(T item)
        {
            for (var i = 0; i < _count; i++)
            {
                if (_comparer.Equals(_slots[Physical(i)], item))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the number of copies of the element (zero when absent).
        /// </summary>
        /// <remarks>Runs in O(<see cref="TotalCount"/>) - the slots are scanned.</remarks>
        /// <example>
        /// <code>
        /// int copies = bag.CountOf("a");
        /// </code>
        /// </example>
        public int CountOf(T item)
        {
            var count = 0;
            for (var i = 0; i < _count; i++)
            {
                if (_comparer.Equals(_slots[Physical(i)], item))
                {
                    count++;
                }
            }

            return count;
        }

        // ------------------------------------------------------------------
        // Remove
        // ------------------------------------------------------------------

        /// <summary>
        /// Removes a single copy of the element - the oldest copy, preserving the first-in-first-out
        /// order of the copies that remain - and returns the number of copies remaining afterwards.
        /// Returns zero when the element is absent (no copy was removed).
        /// </summary>
        /// <example>
        /// <code>
        /// int remaining = bag.Remove("a");
        /// </code>
        /// </example>
        public int Remove(T item)
        {
            return Remove(item, 1);
        }

        /// <summary>
        /// Removes up to the specified number of copies of the element, oldest first, and returns
        /// the number of copies remaining afterwards. Returns zero when the element is absent.
        /// Removing more copies than are stored removes everything the bag holds of the element.
        /// </summary>
        /// <param name="item">The element to remove.</param>
        /// <param name="times">The maximum number of copies to remove. Must be positive.</param>
        /// <returns>The number of copies remaining after the removal, or zero when the element was absent.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="times"/> is zero or negative.</exception>
        /// <example>
        /// <code>
        /// int remaining = bag.Remove("a", 2);
        /// </code>
        /// </example>
        public int Remove(T item, int times)
        {
            if (times <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(times), times, "The number of copies must be positive.");
            }

            var removed = 0;
            var i = 0;
            while (i < _count && removed < times)
            {
                if (_comparer.Equals(_slots[Physical(i)], item))
                {
                    RemoveSlotAt(i);
                    removed++;
                }
                else
                {
                    i++;
                }
            }

            return CountOf(item);
        }

        /// <summary>
        /// Removes every copy of the element. Returns <c>true</c> when at least one copy was
        /// removed.
        /// </summary>
        /// <example>
        /// <code>
        /// bool removed = bag.RemoveAllCopies("a");
        /// </code>
        /// </example>
        public bool RemoveAllCopies(T item)
        {
            var removed = false;
            var i = 0;
            while (i < _count)
            {
                if (_comparer.Equals(_slots[Physical(i)], item))
                {
                    RemoveSlotAt(i);
                    removed = true;
                }
                else
                {
                    i++;
                }
            }

            return removed;
        }

        /// <summary>
        /// Removes all elements and copies.
        /// </summary>
        /// <example>
        /// <code>
        /// bag.Clear();
        /// </code>
        /// </example>
        public void Clear()
        {
            // The slots are wiped (not just the count reset): a T with reference-type fields would
            // otherwise keep its dead elements reachable until the slots are overwritten.
            Array.Clear(_slots, 0, _slots.Length);
            _head = 0;
            _count = 0;
        }

        // ------------------------------------------------------------------
        // Projection and copying
        // ------------------------------------------------------------------

        /// <summary>
        /// Enumerates each distinct element exactly once, in the order in which it first appears in
        /// the bag (oldest first).
        /// </summary>
        /// <example>
        /// <code>
        /// foreach (var item in bag.DistinctItems()) { }
        /// </code>
        /// </example>
        public IEnumerable<T> DistinctItems()
        {
            var seen = new List<T>(_count);
            for (var i = 0; i < _count; i++)
            {
                var value = _slots[Physical(i)];
                if (IndexOf(seen, value) < 0)
                {
                    seen.Add(value);
                    yield return value;
                }
            }
        }

        /// <summary>
        /// Enumerates every distinct element together with its copy count, in the order in which the
        /// element first appears in the bag (oldest first). This is the histogram view of the bag.
        /// </summary>
        /// <example>
        /// <code>
        /// foreach (var (item, count) in bag.EntrySet()) { }
        /// </code>
        /// </example>
        public IEnumerable<(T Item, int Count)> EntrySet()
        {
            var items = new List<T>(_count);
            var counts = new List<int>(_count);
            for (var i = 0; i < _count; i++)
            {
                var value = _slots[Physical(i)];
                var index = IndexOf(items, value);
                if (index < 0)
                {
                    items.Add(value);
                    counts.Add(1);
                }
                else
                {
                    counts[index]++;
                }
            }

            for (var i = 0; i < items.Count; i++)
            {
                yield return (items[i], counts[i]);
            }
        }

        /// <summary>
        /// Returns a list containing every copy of every element (duplicates expanded), oldest
        /// first.
        /// </summary>
        /// <example>
        /// <code>
        /// List&lt;string&gt; copy = bag.ToList();
        /// </code>
        /// </example>
        public List<T> ToList()
        {
            var list = new List<T>(_count);
            for (var i = 0; i < _count; i++)
            {
                list.Add(_slots[Physical(i)]);
            }

            return list;
        }

        /// <summary>
        /// Returns an array containing every copy of every element (duplicates expanded), oldest
        /// first.
        /// </summary>
        /// <example>
        /// <code>
        /// string[] copy = bag.ToArray();
        /// </code>
        /// </example>
        public T[] ToArray()
        {
            var array = new T[_count];
            for (var i = 0; i < _count; i++)
            {
                array[i] = _slots[Physical(i)];
            }

            return array;
        }

        /// <summary>
        /// Exports the bag as a snapshot dictionary from element to copy count. The returned
        /// dictionary is independent of the bag.
        /// </summary>
        /// <exception cref="InvalidOperationException">The bag contains a <c>null</c> element, which can not be represented as a dictionary key.</exception>
        /// <example>
        /// <code>
        /// IReadOnlyDictionary&lt;string, int&gt; histogram = bag.ToDictionary();
        /// </code>
        /// </example>
        public IReadOnlyDictionary<T, int> ToDictionary()
        {
            var dictionary = new Dictionary<T, int>(_comparer);
            for (var i = 0; i < _count; i++)
            {
                var value = _slots[Physical(i)];
                if (value == null)
                {
                    throw new InvalidOperationException(
                        "The bag contains null elements, which can not be represented as dictionary keys.");
                }

                dictionary.TryGetValue(value, out var count);
                dictionary[value] = count + 1;
            }

            return dictionary;
        }

        /// <summary>
        /// Creates an independent copy of the bag: element references are shared, the slots and the
        /// copy counts are independent.
        /// </summary>
        /// <example>
        /// <code>
        /// BoundedBag&lt;string&gt; copy = bag.Clone();
        /// </code>
        /// </example>
        public BoundedBag<T> Clone()
        {
            var clone = new BoundedBag<T>(_slots.Length, _comparer);
            Array.Copy(_slots, clone._slots, _slots.Length);
            clone._head = _head;
            clone._count = _count;
            return clone;
        }

        // ------------------------------------------------------------------
        // Enumeration
        // ------------------------------------------------------------------

        /// <summary>
        /// Enumerates the bag copy-expanded - one entry per copy, duplicates repeated - oldest
        /// first. For the compact per-element view use <see cref="EntrySet()"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// foreach (var item in bag) { /* one iteration per copy */ }
        /// </code>
        /// </example>
        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < _count; i++)
            {
                yield return _slots[Physical(i)];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns the contents in copy-expanded form, comma separated, e.g. <c>a,a,b</c>.
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
        // Internals
        // ------------------------------------------------------------------

        /// <summary>
        /// Maps a logical slot index (0 = oldest) to its physical position in the ring.
        /// </summary>
        private int Physical(int logicalIndex)
        {
            return (_head + logicalIndex) % _slots.Length;
        }

        /// <summary>
        /// Appends one copy, evicting the oldest copy first when the ring is full.
        /// </summary>
        private void AddCopy(T item)
        {
            if (_count == _slots.Length)
            {
                _head = (_head + 1) % _slots.Length;
                _count--;
            }

            _slots[Physical(_count)] = item;
            _count++;
        }

        /// <summary>
        /// Removes the slot at <paramref name="logicalIndex"/> by shifting the newer copies over it,
        /// preserving the oldest-first order of the copies that remain.
        /// </summary>
        private void RemoveSlotAt(int logicalIndex)
        {
            for (var i = logicalIndex; i < _count - 1; i++)
            {
                _slots[Physical(i)] = _slots[Physical(i + 1)];
            }

            _count--;
            _slots[Physical(_count)] = default!;
        }

        /// <summary>
        /// Finds the index of the first element equal to <paramref name="value"/> in the projection
        /// buffer, or <c>-1</c> when absent. A linear scan over a bounded buffer.
        /// </summary>
        private int IndexOf(List<T> items, T value)
        {
            for (var i = 0; i < items.Count; i++)
            {
                if (_comparer.Equals(items[i], value))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
