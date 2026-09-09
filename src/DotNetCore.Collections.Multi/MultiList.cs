using System;
using System.Collections;
using System.Collections.Generic;

// CS8714: T is deliberately unconstrained because null elements are supported
// (tracked in a dedicated bucket); the "notnull" key constraint of the annotated
// Dictionary<TKey, TValue> (net5.0+ reference assemblies) is a false positive here.
#pragma warning disable CS8714

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Represents a multiset (bag): an unordered collection that allows duplicate elements
    /// and tracks the number of occurrences (copies) of each element.
    /// Adding, looking up and removing a single copy of an element runs in O(1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Element equality is determined by the <see cref="IEqualityComparer{T}"/> supplied at
    /// construction (default: <see cref="EqualityComparer{T}.Default"/>). Elements are never
    /// keyed by their hash code alone, so hash collisions between distinct (per
    /// <see cref="object.Equals(object)"/>) elements can not corrupt the multiset.
    /// </para>
    /// <para>
    /// This class is not thread-safe. Wrap it with external synchronization for concurrent use.
    /// </para>
    /// </remarks>
    public class MultiList<T> : IEnumerable<T>, ICollection<T>, IReadOnlyCollection<T>
    {
        private readonly Dictionary<T, int> _counts;
        private readonly IEqualityComparer<T> _comparer;

        // Dedicated bucket for null elements (Dictionary<T,> rejects null keys).
        // Only reachable when T is a reference (or nullable) type.
        private int _nullCount;

        /// <summary>
        /// Initializes an empty <see cref="MultiList{T}"/>.
        /// </summary>
        public MultiList() : this(0, null)
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="MultiList{T}"/> with the specified initial capacity
        /// for distinct elements.
        /// </summary>
        public MultiList(int capacity) : this(capacity, null)
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="MultiList{T}"/> with the specified comparer.
        /// </summary>
        public MultiList(IEqualityComparer<T>? comparer) : this(0, comparer)
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="MultiList{T}"/> with the specified initial capacity
        /// and comparer.
        /// </summary>
        public MultiList(int capacity, IEqualityComparer<T>? comparer)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity can not be less than zero.");
            }

            _comparer = comparer ?? EqualityComparer<T>.Default;
            _counts = new Dictionary<T, int>(capacity, _comparer);
        }

        /// <summary>
        /// Initializes a <see cref="MultiList{T}"/> containing one copy of each element of the
        /// specified collection (duplicates in the source are accumulated as multiple copies).
        /// </summary>
        public MultiList(IEnumerable<T> collection) : this(collection, null)
        {
        }

        /// <summary>
        /// Initializes a <see cref="MultiList{T}"/> containing one copy of each element of the
        /// specified collection, using the specified comparer.
        /// </summary>
        public MultiList(IEnumerable<T> collection, IEqualityComparer<T>? comparer) : this(0, comparer)
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
        /// Gets the comparer used to determine element equality.
        /// </summary>
        public IEqualityComparer<T> Comparer => _comparer;

        /// <summary>
        /// Gets the total number of copies across all elements.
        /// </summary>
        public int TotalCount { get; private set; }

        /// <summary>
        /// Gets the number of distinct elements. A stored <c>null</c> element counts as one
        /// distinct element.
        /// </summary>
        public int DistinctCount => _counts.Count + (_nullCount > 0 ? 1 : 0);

        /// <summary>Gets a value indicating whether the multiset is read-only. Always <c>false</c>.</summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Adds a single copy of the element.
        /// </summary>
        public void Add(T item)
        {
            Add(item, 1);
        }

        /// <summary>
        /// Adds the specified number of copies of the element. A non-positive
        /// <paramref name="times"/> is coerced to one copy (legacy behaviour).
        /// </summary>
        public void Add(T item, int times)
        {
            if (times <= 0)
            {
                times = 1;
            }

            TotalCount += times;
            if (item == null)
            {
                _nullCount += times;
                return;
            }

            _counts[item] = (_counts.TryGetValue(item, out var current) ? current : 0) + times;
        }

        /// <summary>
        /// Adds one copy of each element of the specified collection.
        /// </summary>
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
        public bool Contains(T item)
        {
            return item == null ? _nullCount > 0 : _counts.ContainsKey(item);
        }

        /// <summary>
        /// Determines whether the multiset contains at least one copy of every element of the
        /// specified collection.
        /// </summary>
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
        public int CountOf(T item)
        {
            if (item == null)
            {
                return _nullCount;
            }

            return _counts.TryGetValue(item, out var current) ? current : 0;
        }

        /// <summary>
        /// Removes a single copy of the element and returns the number of copies remaining
        /// afterwards. Returns zero when the element is absent (no copy was removed).
        /// </summary>
        public int Remove(T item)
        {
            return Remove(item, 1);
        }

        /// <summary>
        /// Removes up to the specified number of copies of the element and returns the number of
        /// copies remaining afterwards. A non-positive <paramref name="times"/> is coerced to one
        /// copy (legacy behaviour). Returns zero when the element is absent. The element is
        /// dropped from the multiset once its copy count reaches zero.
        /// </summary>
        public int Remove(T item, int times)
        {
            if (times <= 0)
            {
                times = 1;
            }

            var current = CountOf(item);
            if (current == 0)
            {
                return 0;
            }

            var removed = times > current ? current : times;
            var remaining = current - removed;
            if (item == null)
            {
                _nullCount = remaining;
            }
            else if (remaining == 0)
            {
                _counts.Remove(item);
            }
            else
            {
                _counts[item] = remaining;
            }

            TotalCount -= removed;
            return remaining;
        }

        /// <summary>
        /// Removes every copy of the element. Returns <c>true</c> when at least one copy was
        /// removed, <c>false</c> when the element was absent.
        /// </summary>
        public bool RemoveAllCopies(T item)
        {
            var current = CountOf(item);
            if (current == 0)
            {
                return false;
            }

            if (item == null)
            {
                _nullCount = 0;
            }
            else
            {
                _counts.Remove(item);
            }

            TotalCount -= current;
            return true;
        }

        /// <summary>
        /// Removes all elements and copies.
        /// </summary>
        public void Clear()
        {
            _counts.Clear();
            _nullCount = 0;
            TotalCount = 0;
        }

        /// <summary>
        /// Returns a list containing every copy of every element (duplicates expanded).
        /// </summary>
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
        /// Returns an array containing every copy of every element (duplicates expanded).
        /// </summary>
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
        /// Enumerates the distinct elements only, ignoring copy counts.
        /// </summary>
        public IEnumerable<T> DistinctItems()
        {
            if (_nullCount > 0)
            {
                yield return default!;
            }

            foreach (var key in _counts.Keys)
            {
                yield return key;
            }
        }

        /// <summary>
        /// Enumerates each distinct element together with its copy count.
        /// </summary>
        public IEnumerable<(T Item, int Count)> EntrySet()
        {
            if (_nullCount > 0)
            {
                yield return (default!, _nullCount);
            }

            foreach (var pair in _counts)
            {
                yield return (pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Creates a shallow copy: element references are shared, copy counts are independent.
        /// </summary>
        public MultiList<T> Clone()
        {
            return new MultiList<T>(this, _comparer);
        }

        /// <summary>
        /// Exports the multiset as a snapshot dictionary of element to copy count. The returned
        /// dictionary is independent of the multiset. Throws
        /// <see cref="InvalidOperationException"/> when the multiset contains a <c>null</c>
        /// element, which can not be represented as a dictionary key.
        /// </summary>
        public IReadOnlyDictionary<T, int> ToDictionary()
        {
            if (_nullCount > 0)
            {
                throw new InvalidOperationException(
                    "The multiset contains null elements, which can not be represented as dictionary keys.");
            }

            return new Dictionary<T, int>(_counts, _comparer);
        }

        /// <summary>
        /// Returns a live read-only view of the multiset: enumeration and
        /// <see cref="IReadOnlyCollection{T}.Count"/> reflect subsequent changes to the owning
        /// multiset. Mutating members are not exposed.
        /// </summary>
        public IReadOnlyCollection<T> AsReadOnly()
        {
            return new ReadOnlyView(this);
        }

        /// <summary>
        /// Copies every copy of every element (duplicates expanded) to the target array.
        /// </summary>
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
        // Multiset operations
        // ------------------------------------------------------------------

        private MultiList<T> Snapshot(IEnumerable<T> other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            return new MultiList<T>(other, _comparer);
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
        /// collections. Occurrences in <paramref name="other"/> are counted element-wise (a
        /// <see cref="MultiList{T}"/> argument contributes its full multiplicities).
        /// </summary>
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
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return IsSubsetOfBag(Snapshot(other));
        }

        /// <summary>
        /// Determines whether the multiset is a superset of the specified collection, using
        /// multiset semantics.
        /// </summary>
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return IsSupersetOfBag(Snapshot(other));
        }

        /// <summary>
        /// Determines whether the multiset is a proper subset of the specified collection
        /// (a subset that is not equal as a multiset).
        /// </summary>
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            var otherBag = Snapshot(other);
            return IsSubsetOfBag(otherBag) && TotalCount != otherBag.TotalCount;
        }

        /// <summary>
        /// Determines whether the multiset is a proper superset of the specified collection
        /// (a superset that is not equal as a multiset).
        /// </summary>
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            var otherBag = Snapshot(other);
            return IsSupersetOfBag(otherBag) && TotalCount != otherBag.TotalCount;
        }

        /// <summary>
        /// Determines whether the multiset and the specified collection share at least one
        /// element.
        /// </summary>
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
        public bool IsDisjointFrom(IEnumerable<T> other)
        {
            return !Overlaps(other);
        }

        private bool IsSubsetOfBag(MultiList<T> otherBag)
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

        private bool IsSupersetOfBag(MultiList<T> otherBag)
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
        /// Enumerates every copy of every element (duplicates expanded).
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < _nullCount; i++)
            {
                yield return default!;
            }

            foreach (var pair in _counts)
            {
                for (var i = 0; i < pair.Value; i++)
                {
                    yield return pair.Key;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private sealed class ReadOnlyView : IReadOnlyCollection<T>
        {
            private readonly MultiList<T> _owner;

            public ReadOnlyView(MultiList<T> owner)
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
        /// Returns the expanded form (every copy, duplicates included), comma separated.
        /// </summary>
        public override string ToString()
        {
            return string.Join(",", this);
        }
    }
}
