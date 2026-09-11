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
    /// This class is one of the three orthogonal "multi" types of this package:
    /// <see cref="MultiList{T}"/> multiplies <em>elements</em> (1 element &#8594; N copies, this
    /// type), <see cref="MultiDictionary{TKey,TValue}"/> multiplies <em>values</em> per key
    /// (1 key &#8594; N values) and <see cref="MultiKeyDictionary{TKey,TValue}"/> multiplies
    /// <em>key components</em> (N components &#8594; 1 value). Pick the type by asking what is
    /// allowed to repeat, never by name similarity.
    /// </para>
    /// <para>
    /// Structural equality follows multiset semantics: two multisets are equal when every
    /// distinct element is present in both with the same number of copies, regardless of
    /// enumeration order. See <see cref="Equals(MultiList{T})"/>.
    /// </para>
    /// <para>
    /// This class is not thread-safe. Wrap it with external synchronization for concurrent use.
    /// </para>
    /// </remarks>
    public class MultiList<T> : IEnumerable<T>, ICollection<T>, IReadOnlyCollection<T>, IEquatable<MultiList<T>>
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
        /// // bag.CountOf("apple") == 3
        /// </code>
        /// </example>
        public void Add(T item, int times)
        {
            if (times <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(times), times, "The number of copies must be positive.");
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
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bag.AddRange(new[] { "a", "a", "b" });
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
        /// bool has = bag.Contains("apple");
        /// </code>
        /// </example>
        public bool Contains(T item)
        {
            return item == null ? _nullCount > 0 : _counts.ContainsKey(item);
        }

        /// <summary>
        /// Determines whether the multiset contains at least one copy of every element of the
        /// specified collection.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bool has = bag.ContainsAll(new[] { "a", "b" });
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
        /// int copies = bag.CountOf("apple");
        /// </code>
        /// </example>
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
        /// <example>
        /// <code>
        /// int removed = bag.Remove("apple");
        /// // removes a single copy
        /// </code>
        /// </example>
        public int Remove(T item)
        {
            return Remove(item, 1);
        }

        /// <summary>
        /// Removes up to the specified number of copies of the element and returns the number of
        /// copies remaining afterwards. Returns zero when the element is absent. The element is
        /// dropped from the multiset once its copy count reaches zero.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="times"/> is less than
        /// or equal to zero.</exception>
        /// <example>
        /// <code>
        /// bag.Remove("apple", 2);
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
        /// <example>
        /// <code>
        /// bool any = bag.RemoveAllCopies("apple");
        /// </code>
        /// </example>
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
        /// <example>
        /// <code>
        /// bag.Clear();
        /// // bag.TotalCount == 0
        /// </code>
        /// </example>
        public void Clear()
        {
            _counts.Clear();
            _nullCount = 0;
            TotalCount = 0;
        }

        /// <summary>
        /// Returns a list containing every copy of every element (duplicates expanded).
        /// </summary>
        /// <example>
        /// <code>
        /// List&lt;string&gt; copy = bag.ToList();
        /// // duplicate copies preserved
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
        /// Returns an array containing every copy of every element (duplicates expanded).
        /// </summary>
        /// <example>
        /// <code>
        /// string[] copy = bag.ToArray();
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
        /// Enumerates the distinct elements only, ignoring copy counts.
        /// </summary>
        /// <example>
        /// <code>
        /// foreach (var item in bag.DistinctItems())
        /// {
        ///     // each element once
        /// }
        /// </code>
        /// </example>
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
        /// <example>
        /// <code>
        /// MultiList&lt;string&gt; copy = bag.Clone();
        /// // deep copy: independent counts
        /// </code>
        /// </example>
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
        /// <exception cref="InvalidOperationException">The operation is not valid for the current state of the collection.</exception>
        /// <example>
        /// <code>
        /// IReadOnlyDictionary&lt;string, int&gt; counts = bag.ToDictionary();
        /// </code>
        /// </example>
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
        /// Exports the multiset as a plain serializable model: the distinct elements and their copy
        /// counts, in two parallel lists.
        /// </summary>
        /// <remarks>
        /// The model is a snapshot - mutating the multiset afterwards does not change it - and,
        /// unlike <see cref="ToDictionary()"/>, it can carry a <c>null</c> element. See
        /// <see cref="MultiListModel{T}"/> for the shape and for why this library takes no dependency
        /// on any serializer. Use <see cref="FromModel"/> to rebuild the multiset.
        /// </remarks>
        /// <example>
        /// <code>
        /// MultiListModel&lt;string&gt; model = bag.ToSerializableModel();
        /// string json = System.Text.Json.JsonSerializer.Serialize(model);
        /// </code>
        /// </example>
        public MultiListModel<T> ToSerializableModel()
        {
            var items = new List<T>(DistinctCount);
            var counts = new List<int>(DistinctCount);
            foreach (var entry in EntrySet())
            {
                items.Add(entry.Item);
                counts.Add(entry.Count);
            }

            return new MultiListModel<T> { Items = items, Counts = counts };
        }

        /// <summary>
        /// Rebuilds a multiset from a serializable model.
        /// </summary>
        /// <param name="model">the model to read: either one produced by <see cref="ToSerializableModel"/> or one built by hand.</param>
        /// <param name="comparer">the element comparer of the rebuilt multiset; <c>null</c> selects <see cref="EqualityComparer{T}.Default"/>. The comparer is configuration rather than data, so it is not part of the model and has to be supplied here.</param>
        /// <returns>a new multiset holding the model's elements with the model's copy counts.</returns>
        /// <remarks>
        /// Elements that are equal under <paramref name="comparer"/> are merged into a single entry
        /// whose count is their sum - the multiset reading of a model that lists the same element
        /// twice. A model produced by <see cref="ToSerializableModel"/> never contains such a pair,
        /// so the merge only affects hand-built models.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="model"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="model"/> is malformed: either list is
        /// <c>null</c>, the two lists have different lengths, or a copy count is not positive.</exception>
        /// <example>
        /// <code>
        /// var model = new MultiListModel&lt;string&gt;
        /// {
        ///     Items = new List&lt;string&gt; { "apple", "banana" },
        ///     Counts = new List&lt;int&gt; { 2, 1 },
        /// };
        ///
        /// MultiList&lt;string&gt; bag = MultiList&lt;string&gt;.FromModel(model);
        /// </code>
        /// </example>
        public static MultiList<T> FromModel(MultiListModel<T> model, IEqualityComparer<T>? comparer = null)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (model.Items == null)
            {
                throw new ArgumentException("The model is malformed: its Items list is null.", nameof(model));
            }

            if (model.Counts == null)
            {
                throw new ArgumentException("The model is malformed: its Counts list is null.", nameof(model));
            }

            if (model.Items.Count != model.Counts.Count)
            {
                throw new ArgumentException(
                    "The model is malformed: Items and Counts have different lengths.", nameof(model));
            }

            var result = new MultiList<T>(model.Items.Count, comparer);
            for (var i = 0; i < model.Items.Count; i++)
            {
                var count = model.Counts[i];
                if (count <= 0)
                {
                    throw new ArgumentException(
                        "The model is malformed: every copy count must be positive.", nameof(model));
                }

                result.Add(model.Items[i], count);
            }

            return result;
        }

        /// <summary>
        /// Returns a live read-only view of the multiset: enumeration and
        /// <see cref="IReadOnlyCollection{T}.Count"/> reflect subsequent changes to the owning
        /// multiset. Mutating members are not exposed.
        /// </summary>
        /// <example>
        /// <code>
        /// IReadOnlyCollection&lt;string&gt; view = bag.AsReadOnly();
        /// </code>
        /// </example>
        public IReadOnlyCollection<T> AsReadOnly()
        {
            return new ReadOnlyView(this);
        }

        /// <summary>
        /// Copies every copy of every element (duplicates expanded) to the target array.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is out of its allowed range.</exception>
        /// <example>
        /// <code>
        /// bag.CopyTo(array, 0);
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

        /// <summary>
        /// Views the argument of a multiset operation in place rather than materialising it as a
        /// <see cref="MultiList{T}"/> (L-07). Returns the argument when it already <em>is</em> a
        /// multiset whose element comparer is equivalent to this one - reading it directly can not
        /// change the result, because re-materialising would only re-hash each element under the
        /// same notion of equality - and <c>null</c> when the caller has to fall back to
        /// <see cref="Snapshot(IEnumerable{T})"/>.
        /// </summary>
        /// <remarks>
        /// This is where chained multiset operations stop paying for a copy of their argument: the
        /// common <c>a.UnionWith(b)</c> with <c>b</c> already a multiset used to allocate a whole
        /// second multiset (and, through <see cref="EntrySet"/>, an iterator per enumeration) just
        /// to read it back. With the view in hand every operation below can walk the argument's
        /// count table directly, which is a struct-enumerator walk and therefore allocates nothing.
        /// When the comparers differ the view is refused and the argument is re-materialised under
        /// this instance's comparer, exactly as before, so the observable semantics are unchanged.
        /// </remarks>
        private MultiList<T>? AsInPlaceArgument(IEnumerable<T> other)
        {
            return other is MultiList<T> bag && _counts.Comparer.Equals(bag._counts.Comparer) ? bag : null;
        }

        /// <summary>
        /// The scratch buffer the mutating multiset operations stage their target state in. It is
        /// reused across calls (and cleared afterwards, so it holds no element references between
        /// calls) because the alternative - a fresh list per call - is a per-call allocation on a
        /// path that is supposed to be allocation-free. Sized from the receiver on first use so a
        /// single operation does not pay the growth ladder. This class is not thread-safe, so one
        /// buffer is enough.
        /// </summary>
        private List<(T Item, int Count)>? _scratch;

        private List<(T Item, int Count)> Scratch()
        {
            return _scratch ?? (_scratch = new List<(T Item, int Count)>(_counts.Count));
        }

        /// <summary>
        /// The copy count of a key that is already known to live in a count table and therefore can
        /// not be <c>null</c>. Bypasses <see cref="CountOf"/>'s null test, which is both redundant
        /// for such a key and a box for value element types.
        /// </summary>
        private static int CountInTable(Dictionary<T, int> counts, T key)
        {
            return counts.TryGetValue(key, out var count) ? count : 0;
        }

        /// <summary>
        /// Applies the multiset union to the receiver, reading the argument in place when it is a
        /// multiset (see <see cref="AsInPlaceArgument"/>).
        /// </summary>
        private void UnionWithBag(MultiList<T> otherBag)
        {
            // Union with self is the identity (max(c, c) == c). Guarded explicitly because the
            // loop below adds to the receiver while walking the argument, and here they are the
            // same count table.
            if (ReferenceEquals(otherBag, this))
            {
                return;
            }

            foreach (var pair in otherBag._counts)
            {
                var current = CountInTable(_counts, pair.Key);
                if (pair.Value > current)
                {
                    Add(pair.Key, pair.Value - current);
                }
            }

            if (otherBag._nullCount > _nullCount)
            {
                Add(default!, otherBag._nullCount - _nullCount);
            }
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
        /// <remarks>
        /// Allocates nothing when <paramref name="other"/> is a <see cref="MultiList{T}"/> with an
        /// equivalent comparer: the argument is read in place instead of being copied.
        /// </remarks>
        /// <example>
        /// <code>
        /// bag.UnionWith(new[] { "a", "c" });
        /// // adds one copy of each missing element
        /// </code>
        /// </example>
        public void UnionWith(IEnumerable<T> other)
        {
            var inPlace = AsInPlaceArgument(other);
            if (inPlace != null)
            {
                UnionWithBag(inPlace);
                return;
            }

            UnionWithBag(Snapshot(other));
        }

        /// <summary>
        /// Modifies the multiset to the intersection with the specified collection, using
        /// multiset semantics: each element keeps the <b>minimum</b> of its copy counts in both
        /// collections. Elements absent from <paramref name="other"/> are dropped.
        /// </summary>
        /// <remarks>
        /// Allocates nothing when <paramref name="other"/> is a <see cref="MultiList{T}"/> with an
        /// equivalent comparer: the argument is read in place instead of being copied, and the
        /// target state is staged in a reused scratch buffer.
        /// </remarks>
        /// <example>
        /// <code>
        /// bag.IntersectionWith(new[] { "a" });
        /// // keeps min(copies(here), copies(other))
        /// </code>
        /// </example>
        public void IntersectionWith(IEnumerable<T> other)
        {
            var otherBag = AsInPlaceArgument(other) ?? Snapshot(other);
            var scratch = Scratch();
            try
            {
                foreach (var pair in _counts)
                {
                    var otherCount = CountInTable(otherBag._counts, pair.Key);
                    scratch.Add((pair.Key, otherCount < pair.Value ? otherCount : pair.Value));
                }

                ApplyUpdates(scratch);

                // Intersection keeps the smaller count, the null bucket included. Guarded because
                // for a value element type the bucket is always empty and `default!` would name the
                // element default(T) - 0 in a MultiList<int>, say - instead of "no element".
                if (_nullCount > 0 || otherBag._nullCount > 0)
                {
                    SetCountExact(default!, Math.Min(_nullCount, otherBag._nullCount));
                }
            }
            finally
            {
                scratch.Clear();
            }
        }

        /// <summary>
        /// Removes from the multiset all copies contained in the specified collection, using
        /// multiset semantics: each element loses up to the number of copies present in
        /// <paramref name="other"/> (never below zero).
        /// </summary>
        /// <remarks>
        /// Allocates nothing when <paramref name="other"/> is a <see cref="MultiList{T}"/> with an
        /// equivalent comparer: the argument is read in place instead of being copied, and the
        /// target state is staged in a reused scratch buffer.
        /// </remarks>
        /// <example>
        /// <code>
        /// bag.ExceptWith(new[] { "a" });
        /// // removes every copy of the elements
        /// </code>
        /// </example>
        public void ExceptWith(IEnumerable<T> other)
        {
            var otherBag = AsInPlaceArgument(other) ?? Snapshot(other);
            var scratch = Scratch();
            try
            {
                foreach (var pair in _counts)
                {
                    var target = pair.Value - CountInTable(otherBag._counts, pair.Key);
                    scratch.Add((pair.Key, target > 0 ? target : 0));
                }

                ApplyUpdates(scratch);

                // Difference drops up to the argument's null count. Guarded on the bucket being
                // non-empty: see the note in IntersectionWith.
                if (_nullCount > 0)
                {
                    var nullTarget = _nullCount - otherBag._nullCount;
                    SetCountExact(default!, nullTarget > 0 ? nullTarget : 0);
                }
            }
            finally
            {
                scratch.Clear();
            }
        }

        /// <summary>
        /// Modifies the multiset to the symmetric difference with the specified collection:
        /// each element ends up with the <b>absolute difference</b> of its copy counts in both
        /// collections.
        /// </summary>
        /// <remarks>
        /// Allocates nothing when <paramref name="other"/> is a <see cref="MultiList{T}"/> with an
        /// equivalent comparer: the argument is read in place instead of being copied, and the
        /// target state is staged in a reused scratch buffer.
        /// </remarks>
        /// <example>
        /// <code>
        /// bag.SymmetricExceptWith(new[] { "a", "d" });
        /// </code>
        /// </example>
        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            var otherBag = AsInPlaceArgument(other) ?? Snapshot(other);
            var scratch = Scratch();
            try
            {
                foreach (var pair in _counts)
                {
                    scratch.Add((pair.Key, Math.Abs(pair.Value - CountInTable(otherBag._counts, pair.Key))));
                }

                // Second pass: elements only the argument holds. Read against the receiver's
                // *current* counts, before any target is applied.
                foreach (var pair in otherBag._counts)
                {
                    if (CountInTable(_counts, pair.Key) == 0)
                    {
                        scratch.Add((pair.Key, pair.Value));
                    }
                }

                ApplyUpdates(scratch);

                // The symmetric difference of the null buckets is their absolute difference.
                // Guarded for value element types: see the note in IntersectionWith.
                if (_nullCount > 0 || otherBag._nullCount > 0)
                {
                    SetCountExact(default!, Math.Abs(_nullCount - otherBag._nullCount));
                }
            }
            finally
            {
                scratch.Clear();
            }
        }

        /// <summary>
        /// Determines whether the multiset is a subset of the specified collection, using
        /// multiset semantics: the copy count of every element in this multiset must be less
        /// than or equal to its copy count in <paramref name="other"/>.
        /// </summary>
        /// <remarks>
        /// Allocates nothing when <paramref name="other"/> is a <see cref="MultiList{T}"/> with an
        /// equivalent comparer: the argument is read in place instead of being copied.
        /// </remarks>
        /// <example>
        /// <code>
        /// bool ok = bag.IsSubsetOf(other);
        /// </code>
        /// </example>
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return IsSubsetOfBag(AsInPlaceArgument(other) ?? Snapshot(other));
        }

        /// <summary>
        /// Determines whether the multiset is a superset of the specified collection, using
        /// multiset semantics.
        /// </summary>
        /// <remarks>
        /// Allocates nothing when <paramref name="other"/> is a <see cref="MultiList{T}"/> with an
        /// equivalent comparer: the argument is read in place instead of being copied.
        /// </remarks>
        /// <example>
        /// <code>
        /// bool ok = bag.IsSupersetOf(other);
        /// </code>
        /// </example>
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return IsSupersetOfBag(AsInPlaceArgument(other) ?? Snapshot(other));
        }

        /// <summary>
        /// Determines whether the multiset is a proper subset of the specified collection
        /// (a subset that is not equal as a multiset).
        /// </summary>
        /// <remarks>
        /// Allocates nothing when <paramref name="other"/> is a <see cref="MultiList{T}"/> with an
        /// equivalent comparer: the argument is read in place instead of being copied.
        /// </remarks>
        /// <example>
        /// <code>
        /// bool ok = bag.IsProperSubsetOf(other);
        /// </code>
        /// </example>
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            var otherBag = AsInPlaceArgument(other) ?? Snapshot(other);
            return IsSubsetOfBag(otherBag) && TotalCount != otherBag.TotalCount;
        }

        /// <summary>
        /// Determines whether the multiset is a proper superset of the specified collection
        /// (a superset that is not equal as a multiset).
        /// </summary>
        /// <remarks>
        /// Allocates nothing when <paramref name="other"/> is a <see cref="MultiList{T}"/> with an
        /// equivalent comparer: the argument is read in place instead of being copied.
        /// </remarks>
        /// <example>
        /// <code>
        /// bool ok = bag.IsProperSupersetOf(other);
        /// </code>
        /// </example>
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            var otherBag = AsInPlaceArgument(other) ?? Snapshot(other);
            return IsSupersetOfBag(otherBag) && TotalCount != otherBag.TotalCount;
        }

        /// <summary>
        /// Determines whether the multiset and the specified collection share at least one
        /// element.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="other"/> is <c>null</c>.</exception>
        /// <example>
        /// <code>
        /// bool ok = bag.Overlaps(other);
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
        /// <example>
        /// <code>
        /// bool ok = bag.IsDisjointFrom(other);
        /// </code>
        /// </example>
        public bool IsDisjointFrom(IEnumerable<T> other)
        {
            return !Overlaps(other);
        }

        /// <summary>
        /// Determines whether this multiset is structurally equal to another: both must hold the
        /// same distinct elements, each with the same number of copies. Enumeration order is
        /// irrelevant, so <c>a, a, b</c> equals <c>b, a, a</c>, while <c>a, a, b</c> does not
        /// equal <c>a, b</c>.
        /// </summary>
        /// <param name="other">the multiset to compare with, or <c>null</c>.</param>
        /// <returns><c>true</c> when both multisets hold the same elements with the same copy counts.</returns>
        /// <remarks>
        /// <para>
        /// Copy counts take part in the comparison: this is <b>multiset (bag) equality</b>, not set
        /// equality. <see cref="IsSubsetOf(IEnumerable{T})"/> answers the laxer question that
        /// ignores the extra copies of one side.
        /// </para>
        /// <para>
        /// Each side has to confirm the other, and each confirms it with <em>its own</em> comparer -
        /// the same rule the subset and superset judgments follow. When both multisets share a
        /// comparer (the usual case) that is simply the classic multiset comparison. When the
        /// comparers differ, both must agree; asking only the receiver's comparer would make
        /// <c>a.Equals(b)</c> and <c>b.Equals(a)</c> disagree, and <see cref="object.Equals(object)"/>
        /// has to stay symmetric.
        /// </para>
        /// <para>
        /// A <c>null</c> element compares like any other element, through the comparer as usual.
        /// The comparer is expected to be consistent with itself, exactly as everywhere else in
        /// this type.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var a = new MultiList&lt;string&gt; { "x", "x", "y" };
        /// var b = new MultiList&lt;string&gt; { "y", "x", "x" };
        /// var c = new MultiList&lt;string&gt; { "x", "y" };
        ///
        /// a.Equals(b);   // true  - same elements, same copy counts
        /// a.Equals(c);   // false - "x" has two copies in a, one in c
        /// </code>
        /// </example>
        public bool Equals(MultiList<T>? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is null)
            {
                return false;
            }

            // Both directions are needed for symmetry when the two multisets use different
            // comparers; with equal comparers the first check already implies the second.
            return IsSubsetOfBag(other) && IsSupersetOfBag(other);
        }

        /// <summary>
        /// Determines whether this multiset is structurally equal to another object; see
        /// <see cref="Equals(MultiList{T})"/>. The result is <c>false</c> for anything that is not
        /// a <see cref="MultiList{T}"/> with a compatible element type.
        /// </summary>
        /// <param name="obj">the object to compare with.</param>
        /// <returns><c>true</c> when <paramref name="obj"/> is a structurally equal multiset.</returns>
        /// <example>
        /// <code>
        /// bool same = bag.Equals((object) otherBag);
        /// </code>
        /// </example>
        public override bool Equals(object? obj)
        {
            return Equals(obj as MultiList<T>);
        }

        /// <summary>
        /// Returns a hash code consistent with <see cref="Equals(MultiList{T})"/>: it depends only
        /// on which elements the multiset holds and how many copies of each, never on enumeration
        /// order. Two structurally equal multisets therefore always share a hash code.
        /// </summary>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// int hash = bag.GetHashCode();
        /// </code>
        /// </example>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 31) + TotalCount;
                hash = (hash * 31) + DistinctCount;

                // XOR keeps the per-element part independent of enumeration order, which is what
                // equal multisets with different insertion orders need. The copy count goes into
                // each element's contribution, so { x, x } and { x } do not collide.
                var elementsHash = 0;
                foreach (var entry in EntrySet())
                {
                    var elementHash = entry.Item == null ? 0 : _comparer.GetHashCode(entry.Item!);
                    elementsHash ^= (elementHash * 397) ^ entry.Count;
                }

                return (hash * 31) + elementsHash;
            }
        }

        /// <remarks>
        /// Walks both count tables directly rather than through <see cref="EntrySet"/>, whose
        /// iterator would allocate on every call. The <c>null</c> bucket is compared separately
        /// because it lives outside <c>_counts</c>.
        /// </remarks>
        private bool IsSubsetOfBag(MultiList<T> otherBag)
        {
            foreach (var pair in _counts)
            {
                if (CountInTable(otherBag._counts, pair.Key) < pair.Value)
                {
                    return false;
                }
            }

            return _nullCount == 0 || otherBag._nullCount >= _nullCount;
        }

        private bool IsSupersetOfBag(MultiList<T> otherBag)
        {
            foreach (var pair in otherBag._counts)
            {
                if (CountInTable(_counts, pair.Key) < pair.Value)
                {
                    return false;
                }
            }

            return otherBag._nullCount == 0 || _nullCount >= otherBag._nullCount;
        }

        /// <summary>
        /// Enumerates every copy of every element (duplicates expanded).
        /// </summary>
        /// <example>
        /// <code>
        /// foreach (var item in bag)
        /// {
        ///     // copies expanded
        /// }
        /// </code>
        /// </example>
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
        /// <example>
        /// <code>
        /// string text = bag.ToString();
        /// </code>
        /// </example>
        public override string ToString()
        {
            return string.Join(",", this);
        }
    }
}
