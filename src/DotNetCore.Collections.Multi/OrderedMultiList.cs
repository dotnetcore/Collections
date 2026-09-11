using System;
using System.Collections;
using System.Collections.Generic;

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Represents an ordered multiset (sorted bag): a collection that allows duplicate elements,
    /// tracks the number of occurrences (copies) of each element, and always enumerates its
    /// elements in ascending order. Adding, looking up and removing a single copy of an element
    /// runs in O(log n) worst case.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the ordered counterpart of <see cref="MultiList{T}"/> and the equivalent of
    /// PowerCollections' <c>OrderedBag&lt;T&gt;</c>. The two types share their copy-counting
    /// semantics - one distinct element with N copies, duplicates expanded on enumeration, the
    /// same multiset set operations - and differ in the storage underneath and in what that
    /// storage buys: <see cref="MultiList{T}"/> keeps a hash table and answers in O(1) but has no
    /// defined order, while this type keeps a red-black tree and answers in O(log n) while
    /// enumerating in sorted order.
    /// </para>
    /// <para>
    /// <b>Element ordering and element equality are the same decision here, and it is made by an
    /// <see cref="IComparer{T}"/>.</b> That is deliberately different from
    /// <see cref="MultiList{T}"/>, which takes an <see cref="IEqualityComparer{T}"/>: an equality
    /// comparer supplies hash codes but no ordering, so it can not define a sorted sequence, and
    /// the comparison result <c>0</c> is what this type uses to decide that two elements are the
    /// same element. Two elements that compare equal share one node and one copy count, and the
    /// element actually stored is the one that was added first - the same rule a dictionary
    /// keyed by a comparer follows.
    /// </para>
    /// <para>
    /// <b>Null elements are supported</b>, because <see cref="System.Collections.Generic.Comparer{T}.Default"/> orders
    /// <c>null</c> below every other reference, so a <c>null</c> element sorts first and is
    /// enumerated first. Handling <c>null</c> is the comparer's responsibility: with the default
    /// comparer it is an ordinary smallest element, while a custom comparer decides for itself
    /// where <c>null</c> belongs and may reject it by throwing. This type never inspects elements
    /// itself, so whatever the comparer does with <c>null</c> is what happens.
    /// </para>
    /// <para>
    /// This type is one of the "multi" family of this package. Read the name as "what is
    /// multiplied": <see cref="MultiList{T}"/> multiplies <em>elements</em> (1 element &#8594; N
    /// copies), <see cref="MultiDictionary{TKey,TValue}"/> multiplies <em>values</em> per key
    /// (1 key &#8594; N values) and <see cref="MultiKeyDictionary{TKey,TValue}"/> multiplies
    /// <em>key components</em> (N components &#8594; 1 value).
    /// </para>
    /// <para>
    /// Structural equality is <b>not</b> overridden: two ordered multisets with the same content
    /// are still distinct objects under <see cref="object.Equals(object)"/>, matching every other
    /// type of this package except <see cref="MultiList{T}"/>. Use
    /// <see cref="IsSubsetOf(IEnumerable{T})"/> / <see cref="IsSupersetOf(IEnumerable{T})"/> to
    /// compare contents, or compare <see cref="EntrySet"/> sequences directly.
    /// </para>
    /// <para>
    /// This class is not thread-safe. Wrap it with external synchronization for concurrent use.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var shelf = new OrderedMultiList&lt;string&gt; { "mug", "bean", "bean" };
    ///
    /// foreach (var item in shelf) { /* "bean", "bean", "mug" */ }
    ///
    /// shelf.GetFirst();                       // "bean"
    /// shelf.GetRange("a", "n");               // "bean", "bean"
    /// shelf.CountOf("bean");                  // 2
    /// </code>
    /// </example>
    public class OrderedMultiList<T> : IEnumerable<T>, ICollection<T>, IReadOnlyCollection<T>
    {
        private readonly IComparer<T> _comparer;
        private readonly RedBlackTree<T> _tree;

        /// <summary>
        /// Initializes an empty <see cref="OrderedMultiList{T}"/> ordered by
        /// <see cref="System.Collections.Generic.Comparer{T}.Default"/>.
        /// </summary>
        public OrderedMultiList() : this((IComparer<T>?)null)
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="OrderedMultiList{T}"/> ordered by the specified
        /// comparer.
        /// </summary>
        /// <param name="comparer">the comparer that defines element order and element identity, or <c>null</c> for <see cref="System.Collections.Generic.Comparer{T}.Default"/>.</param>
        public OrderedMultiList(IComparer<T>? comparer)
        {
            _comparer = comparer ?? Comparer<T>.Default;
            _tree = new RedBlackTree<T>(_comparer);
        }

        /// <summary>
        /// Initializes an <see cref="OrderedMultiList{T}"/> containing one copy of each element
        /// of the specified collection (duplicates in the source are accumulated as multiple
        /// copies), ordered by <see cref="System.Collections.Generic.Comparer{T}.Default"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        public OrderedMultiList(IEnumerable<T> collection) : this(collection, null)
        {
        }

        /// <summary>
        /// Initializes an <see cref="OrderedMultiList{T}"/> containing one copy of each element
        /// of the specified collection, ordered by the specified comparer.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        public OrderedMultiList(IEnumerable<T> collection, IComparer<T>? comparer) : this(comparer)
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
        /// Gets the comparer that defines the order of the elements, and with it which elements
        /// count as the same element.
        /// </summary>
        public IComparer<T> Comparer => _comparer;

        /// <summary>
        /// Gets the total number of copies across all elements.
        /// </summary>
        public int TotalCount { get; private set; }

        /// <summary>
        /// Gets the number of distinct elements. A stored <c>null</c> element counts as one
        /// distinct element.
        /// </summary>
        public int DistinctCount => _tree.Count;

        /// <summary>Gets a value indicating whether the multiset is read-only. Always <c>false</c>.</summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Adds a single copy of the element.
        /// </summary>
        /// <example>
        /// <code>
        /// shelf.Add("mug");
        /// </code>
        /// </example>
        public void Add(T item)
        {
            Add(item, 1);
        }

        /// <summary>
        /// Adds the specified number of copies of the element. Non-positive
        /// <paramref name="times"/> is rejected, matching <see cref="MultiList{T}.Add(T, int)"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="times"/> is less than
        /// or equal to zero.</exception>
        /// <example>
        /// <code>
        /// shelf.Add("mug", 3);
        /// // shelf.CountOf("mug") == 3
        /// </code>
        /// </example>
        public void Add(T item, int times)
        {
            if (times <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(times), times, "The number of copies must be positive.");
            }

            _tree.AddCount(item, times);
            TotalCount += times;
        }

        /// <summary>
        /// Adds one copy of each element of the specified collection.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// shelf.AddRange(new[] { "a", "a", "b" });
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

        /// <summary>
        /// Determines whether the multiset contains at least one copy of the element.
        /// </summary>
        /// <example>
        /// <code>
        /// bool has = shelf.Contains("mug");
        /// </code>
        /// </example>
        public bool Contains(T item)
        {
            return _tree.TryGetCount(item, out _);
        }

        /// <summary>
        /// Determines whether the multiset contains at least one copy of every element of the
        /// specified collection.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bool has = shelf.ContainsAll(new[] { "a", "b" });
        /// </code>
        /// </example>
        public bool ContainsAll(IEnumerable<T> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            foreach (var item in items)
            {
                if (!Contains(item))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the number of copies of the element (zero when absent).
        /// </summary>
        /// <example>
        /// <code>
        /// int copies = shelf.CountOf("mug");
        /// </code>
        /// </example>
        public int CountOf(T item)
        {
            return _tree.TryGetCount(item, out var count) ? count : 0;
        }

        /// <summary>
        /// Removes a single copy of the element and returns the number of copies remaining
        /// afterwards. Returns zero when the element is absent (no copy was removed).
        /// </summary>
        /// <example>
        /// <code>
        /// int remaining = shelf.Remove("mug");
        /// </code>
        /// </example>
        public int Remove(T item)
        {
            return Remove(item, 1);
        }

        /// <summary>
        /// Removes up to the specified number of copies of the element and returns the number of
        /// copies remaining afterwards. Non-positive <paramref name="times"/> is rejected,
        /// matching <see cref="MultiList{T}.Remove(T, int)"/>. Returns zero when the element is
        /// absent. The element is dropped from the multiset once its copy count reaches zero.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="times"/> is less than
        /// or equal to zero.</exception>
        /// <example>
        /// <code>
        /// shelf.Remove("mug", 2);
        /// // removes at most 2 copies
        /// </code>
        /// </example>
        public int Remove(T item, int times)
        {
            if (times <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(times), times, "The number of copies must be positive.");
            }

            var current = CountOf(item);
            if (current == 0)
            {
                return 0;
            }

            var removed = times > current ? current : times;
            var remaining = current - removed;
            if (remaining == 0)
            {
                _tree.TryRemoveKey(item);
            }
            else
            {
                _tree.TrySetCount(item, remaining);
            }

            TotalCount -= removed;
            return remaining;
        }

        /// <summary>
        /// Removes every copy of the element. Returns <c>true</c> when at least one copy was
        /// removed, <c>false</c> when the element was absent.
        /// </summary>
        /// <example>
        /// <code>
        /// bool any = shelf.RemoveAllCopies("mug");
        /// </code>
        /// </example>
        public bool RemoveAllCopies(T item)
        {
            var current = CountOf(item);
            if (current == 0)
            {
                return false;
            }

            _tree.TryRemoveKey(item);
            TotalCount -= current;
            return true;
        }

        /// <summary>
        /// Removes all elements and copies.
        /// </summary>
        /// <example>
        /// <code>
        /// shelf.Clear();
        /// // shelf.TotalCount == 0
        /// </code>
        /// </example>
        public void Clear()
        {
            _tree.Clear();
            TotalCount = 0;
        }

        /// <summary>
        /// Gets the smallest element, in O(log n).
        /// </summary>
        /// <returns>the smallest element held by the multiset.</returns>
        /// <exception cref="InvalidOperationException">The multiset is empty.</exception>
        /// <example>
        /// <code>
        /// string first = shelf.GetFirst();
        /// </code>
        /// </example>
        public T GetFirst()
        {
            if (!_tree.TryGetFirst(out var entry))
            {
                throw new InvalidOperationException("The multiset is empty, so it has no smallest element.");
            }

            return entry.Key;
        }

        /// <summary>
        /// Gets the largest element, in O(log n).
        /// </summary>
        /// <returns>the largest element held by the multiset.</returns>
        /// <exception cref="InvalidOperationException">The multiset is empty.</exception>
        /// <example>
        /// <code>
        /// string last = shelf.GetLast();
        /// </code>
        /// </example>
        public T GetLast()
        {
            if (!_tree.TryGetLast(out var entry))
            {
                throw new InvalidOperationException("The multiset is empty, so it has no largest element.");
            }

            return entry.Key;
        }

        /// <summary>
        /// Enumerates every copy of every element in <b>descending</b> order.
        /// </summary>
        /// <example>
        /// <code>
        /// foreach (var item in shelf.Reverse())
        /// {
        ///     // largest first
        /// }
        /// </code>
        /// </example>
        public IEnumerable<T> Reverse()
        {
            foreach (var entry in _tree.Descending())
            {
                for (var i = 0; i < entry.Value; i++)
                {
                    yield return entry.Key;
                }
            }
        }

        /// <summary>
        /// Enumerates every copy of every element that lies between the two bounds, in ascending
        /// order. Both bounds are inclusive.
        /// </summary>
        /// <param name="from">the lower bound, included in the result.</param>
        /// <param name="to">the upper bound, included in the result.</param>
        /// <example>
        /// <code>
        /// foreach (var item in shelf.GetRange("a", "n"))
        /// {
        ///     // ascending, "a" and "n" included
        /// }
        /// </code>
        /// </example>
        public IEnumerable<T> GetRange(T from, T to)
        {
            return GetRange(from, to, true, true);
        }

        /// <summary>
        /// Enumerates every copy of every element that lies between the two bounds, in ascending
        /// order, with independent control over whether each bound is included. An empty sequence
        /// is returned when <paramref name="from"/> sorts above <paramref name="to"/>. Subtrees
        /// outside the bounds are skipped, so the cost is O(log n + k) for k reported copies.
        /// </summary>
        /// <param name="from">the lower bound.</param>
        /// <param name="to">the upper bound.</param>
        /// <param name="inclusiveFrom">whether the lower bound is included.</param>
        /// <param name="inclusiveTo">whether the upper bound is included.</param>
        /// <example>
        /// <code>
        /// foreach (var item in shelf.GetRange("a", "n", false, true))
        /// {
        ///     // ascending, "a" excluded, "n" included
        /// }
        /// </code>
        /// </example>
        public IEnumerable<T> GetRange(T from, T to, bool inclusiveFrom, bool inclusiveTo)
        {
            if (_comparer.Compare(from, to) > 0)
            {
                yield break;
            }

            foreach (var entry in _tree.Range(from, inclusiveFrom, to, inclusiveTo))
            {
                for (var i = 0; i < entry.Value; i++)
                {
                    yield return entry.Key;
                }
            }
        }

        /// <summary>
        /// Returns a list containing every copy of every element (duplicates expanded), in
        /// ascending order.
        /// </summary>
        /// <example>
        /// <code>
        /// List&lt;string&gt; copy = shelf.ToList();
        /// // sorted, duplicate copies preserved
        /// </code>
        /// </example>
        public List<T> ToList()
        {
            var list = new List<T>(TotalCount);
            foreach (var item in this)
            {
                list.Add(item);
            }

            return list;
        }

        /// <summary>
        /// Returns an array containing every copy of every element (duplicates expanded), in
        /// ascending order.
        /// </summary>
        /// <example>
        /// <code>
        /// string[] copy = shelf.ToArray();
        /// </code>
        /// </example>
        public T[] ToArray()
        {
            var array = new T[TotalCount];
            var index = 0;
            foreach (var item in this)
            {
                array[index++] = item;
            }

            return array;
        }

        /// <summary>
        /// Enumerates the distinct elements only, ignoring copy counts, in ascending order.
        /// </summary>
        /// <example>
        /// <code>
        /// foreach (var item in shelf.DistinctItems())
        /// {
        ///     // each element once, sorted
        /// }
        /// </code>
        /// </example>
        public IEnumerable<T> DistinctItems()
        {
            foreach (var entry in _tree.Ascending())
            {
                yield return entry.Key;
            }
        }

        /// <summary>
        /// Enumerates each distinct element together with its copy count, in ascending order.
        /// This is the export path of the type: unlike <see cref="MultiList{T}"/>, the ordered
        /// multiset has no <c>ToDictionary</c>, because its <see cref="IComparer{T}"/> orders
        /// elements but supplies no hash codes for the dictionary to use.
        /// </summary>
        /// <example>
        /// <code>
        /// foreach (var (item, count) in shelf.EntrySet())
        /// {
        ///     // sorted distinct elements with their copy counts
        /// }
        /// </code>
        /// </example>
        public IEnumerable<(T Item, int Count)> EntrySet()
        {
            foreach (var entry in _tree.Ascending())
            {
                yield return (entry.Key, entry.Value);
            }
        }

        /// <summary>
        /// Creates a shallow copy: element references are shared, copy counts are independent.
        /// The copy keeps the same comparer.
        /// </summary>
        /// <example>
        /// <code>
        /// OrderedMultiList&lt;string&gt; copy = shelf.Clone();
        /// </code>
        /// </example>
        public OrderedMultiList<T> Clone()
        {
            return new OrderedMultiList<T>(this, _comparer);
        }

        /// <summary>
        /// Returns a live read-only view of the multiset: enumeration and
        /// <see cref="IReadOnlyCollection{T}.Count"/> reflect subsequent changes to the owning
        /// multiset, and enumeration stays sorted. Mutating members are not exposed.
        /// </summary>
        /// <example>
        /// <code>
        /// IReadOnlyCollection&lt;string&gt; view = shelf.AsReadOnly();
        /// </code>
        /// </example>
        public IReadOnlyCollection<T> AsReadOnly()
        {
            return new ReadOnlyView(this);
        }

        /// <summary>
        /// Copies every copy of every element (duplicates expanded) to the target array, in
        /// ascending order.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is out of its allowed range.</exception>
        /// <example>
        /// <code>
        /// shelf.CopyTo(array, 0);
        /// </code>
        /// </example>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Array index can not be less than zero.");
            }

            if (arrayIndex + TotalCount > array.Length)
            {
                throw new ArgumentException("The number of elements is greater than the available space from arrayIndex to the end of the target array.");
            }

            foreach (var item in this)
            {
                array[arrayIndex++] = item;
            }
        }

        // ------------------------------------------------------------------
        // Multiset operations, shared verbatim with MultiList<T>
        // ------------------------------------------------------------------

        private OrderedMultiList<T> Snapshot(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            return new OrderedMultiList<T>(other, _comparer);
        }

        private void SetCountExact(T item, int count)
        {
            var current = CountOf(item);
            if (count == current)
            {
                return;
            }

            if (count == 0)
            {
                RemoveAllCopies(item);
            }
            else if (count > current)
            {
                Add(item, count - current);
            }
            else
            {
                Remove(item, current - count);
            }
        }

        private void ApplyUpdates(List<(T Item, int Count)> updates)
        {
            foreach (var update in updates)
            {
                SetCountExact(update.Item, update.Count);
            }
        }

        /// <summary>
        /// Modifies the multiset to the union with the specified collection, using multiset
        /// semantics: each element ends up with the <b>maximum</b> of its copy counts in both
        /// collections. Occurrences in <paramref name="other"/> are counted element-wise (an
        /// <see cref="OrderedMultiList{T}"/> argument contributes its full multiplicities).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// shelf.UnionWith(new[] { "a", "c" });
        /// // adds one copy of each missing element
        /// </code>
        /// </example>
        public void UnionWith(IEnumerable<T> other)
        {
            var otherBag = Snapshot(other);
            foreach (var entry in otherBag.EntrySet())
            {
                var current = CountOf(entry.Item);
                var target = entry.Count > current ? entry.Count : current;
                if (target > current)
                {
                    Add(entry.Item, target - current);
                }
            }
        }

        /// <summary>
        /// Modifies the multiset to the intersection with the specified collection, using
        /// multiset semantics: each element keeps the <b>minimum</b> of its copy counts in both
        /// collections. Elements absent from <paramref name="other"/> are dropped.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// shelf.IntersectionWith(new[] { "a" });
        /// // keeps min(copies(here), copies(other))
        /// </code>
        /// </example>
        public void IntersectionWith(IEnumerable<T> other)
        {
            var otherBag = Snapshot(other);
            var updates = new List<(T Item, int Count)>();
            foreach (var entry in EntrySet())
            {
                var otherCount = otherBag.CountOf(entry.Item);
                updates.Add((entry.Item, otherCount < entry.Count ? otherCount : entry.Count));
            }

            ApplyUpdates(updates);
        }

        /// <summary>
        /// Removes from the multiset all copies contained in the specified collection, using
        /// multiset semantics: each element loses up to the number of copies present in
        /// <paramref name="other"/> (never below zero).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// shelf.ExceptWith(new[] { "a" });
        /// // removes every copy of the elements
        /// </code>
        /// </example>
        public void ExceptWith(IEnumerable<T> other)
        {
            var otherBag = Snapshot(other);
            var updates = new List<(T Item, int Count)>();
            foreach (var entry in EntrySet())
            {
                var target = entry.Count - otherBag.CountOf(entry.Item);
                updates.Add((entry.Item, target > 0 ? target : 0));
            }

            ApplyUpdates(updates);
        }

        /// <summary>
        /// Modifies the multiset to the symmetric difference with the specified collection:
        /// each element ends up with the <b>absolute difference</b> of its copy counts in both
        /// collections.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// shelf.SymmetricExceptWith(new[] { "a", "d" });
        /// </code>
        /// </example>
        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            var otherBag = Snapshot(other);
            var updates = new List<(T Item, int Count)>();
            foreach (var entry in EntrySet())
            {
                updates.Add((entry.Item, Math.Abs(entry.Count - otherBag.CountOf(entry.Item))));
            }

            foreach (var otherEntry in otherBag.EntrySet())
            {
                if (CountOf(otherEntry.Item) == 0)
                {
                    updates.Add((otherEntry.Item, otherEntry.Count));
                }
            }

            ApplyUpdates(updates);
        }

        /// <summary>
        /// Determines whether the multiset is a subset of the specified collection, using
        /// multiset semantics: the copy count of every element in this multiset must be less
        /// than or equal to its copy count in <paramref name="other"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bool ok = shelf.IsSubsetOf(other);
        /// </code>
        /// </example>
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return IsSubsetOfBag(Snapshot(other));
        }

        /// <summary>
        /// Determines whether the multiset is a superset of the specified collection, using
        /// multiset semantics.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bool ok = shelf.IsSupersetOf(other);
        /// </code>
        /// </example>
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return IsSupersetOfBag(Snapshot(other));
        }

        /// <summary>
        /// Determines whether the multiset is a proper subset of the specified collection
        /// (a subset that is not equal as a multiset).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bool ok = shelf.IsProperSubsetOf(other);
        /// </code>
        /// </example>
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            var otherBag = Snapshot(other);
            return IsSubsetOfBag(otherBag) && TotalCount != otherBag.TotalCount;
        }

        /// <summary>
        /// Determines whether the multiset is a proper superset of the specified collection
        /// (a superset that is not equal as a multiset).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bool ok = shelf.IsProperSupersetOf(other);
        /// </code>
        /// </example>
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            var otherBag = Snapshot(other);
            return IsSupersetOfBag(otherBag) && TotalCount != otherBag.TotalCount;
        }

        /// <summary>
        /// Determines whether the multiset and the specified collection share at least one
        /// element.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bool ok = shelf.Overlaps(other);
        /// </code>
        /// </example>
        public bool Overlaps(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            foreach (var item in other)
            {
                if (Contains(item))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether the multiset and the specified collection share no elements.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bool ok = shelf.IsDisjointFrom(other);
        /// </code>
        /// </example>
        public bool IsDisjointFrom(IEnumerable<T> other)
        {
            return !Overlaps(other);
        }

        private bool IsSubsetOfBag(OrderedMultiList<T> otherBag)
        {
            foreach (var entry in EntrySet())
            {
                if (otherBag.CountOf(entry.Item) < entry.Count)
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsSupersetOfBag(OrderedMultiList<T> otherBag)
        {
            foreach (var entry in otherBag.EntrySet())
            {
                if (CountOf(entry.Item) < entry.Count)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Enumerates every copy of every element (duplicates expanded), in ascending order.
        /// </summary>
        /// <example>
        /// <code>
        /// foreach (var item in shelf)
        /// {
        ///     // sorted, copies expanded
        /// }
        /// </code>
        /// </example>
        public IEnumerator<T> GetEnumerator()
        {
            foreach (var entry in _tree.Ascending())
            {
                for (var i = 0; i < entry.Value; i++)
                {
                    yield return entry.Key;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private sealed class ReadOnlyView : IReadOnlyCollection<T>
        {
            private readonly OrderedMultiList<T> _owner;

            public ReadOnlyView(OrderedMultiList<T> owner)
            {
                _owner = owner;
            }

            public int Count => _owner.TotalCount;

            public IEnumerator<T> GetEnumerator()
            {
                return _owner.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _owner.GetEnumerator();
            }
        }

        int ICollection<T>.Count => TotalCount;

        int IReadOnlyCollection<T>.Count => TotalCount;

        bool ICollection<T>.Remove(T item)
        {
            var before = TotalCount;
            Remove(item);
            return TotalCount < before;
        }

        /// <summary>
        /// Returns the expanded form (every copy, duplicates included), comma separated, in
        /// ascending order.
        /// </summary>
        /// <example>
        /// <code>
        /// string text = shelf.ToString();
        /// </code>
        /// </example>
        public override string ToString()
        {
            return string.Join(",", this);
        }

        // ------------------------------------------------------------------
        // Diagnostics for the test suite
        // ------------------------------------------------------------------

        /// <summary>
        /// Gets the height of the underlying red-black tree. Exposed for invariant assertions,
        /// which is where the O(log n) guarantee is checked; it is O(n) to compute.
        /// </summary>
        internal int TreeHeight => _tree.Height;

        /// <summary>
        /// Re-checks the red-black invariants of the underlying tree. Exposed for the test
        /// suite, which asserts them instead of relying on timing measurements.
        /// </summary>
        internal bool ValidateTree(out string? error)
        {
            return _tree.Validate(out error);
        }
    }
}
