using System;
using System.Collections.Generic;

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// An immutable multiset whose updates share the untouched parts of the structure, so adding or
    /// removing a copy costs a handful of nodes rather than a copy of the whole collection.
    /// </summary>
    /// <typeparam name="T">The element type. The type parameter is deliberately unconstrained so that a bag of null elements is supported.</typeparam>
    /// <remarks>
    /// <para>
    /// <b>"Persistent" means structural sharing, not persistence to disk.</b> Every update returns a
    /// new bag and leaves the receiver usable, but the two share every subtree the update did not
    /// touch. That is the difference from <see cref="ImmutableMultiList{T}"/>, which is equally
    /// immutable but clones its whole snapshot on every write and therefore costs O(<c>N</c>) per
    /// update - O(<c>N</c><sup>2</sup>) to build a bag of <c>N</c> copies.
    /// </para>
    /// <para>
    /// Elements are hashed with the comparer supplied at construction and stored in a compressed
    /// hash-array mapped trie keyed by the element itself; one entry holds the multiplicity of that
    /// element. A lookup or a write therefore touches at most seven nodes - O(log<sub>32</sub>
    /// <c>N</c>) expected, against O(<c>N</c>) for the clone-on-write family - and a million copies
    /// of one element still cost one entry, not a million.
    /// </para>
    /// <para>
    /// <b>Copy semantics.</b> The bag is a multiset, so it speaks the same vocabulary as
    /// <see cref="MultiList{T}"/>: <see cref="CountOf"/> reports the number of copies of one element,
    /// <see cref="TotalCount"/> the number of copies of all of them, and <see cref="DistinctCount"/>
    /// the number of distinct elements. The inherited <see cref="Count"/> is pinned to the number of
    /// <em>copies</em>, exactly as <see cref="IMultiSet{T}"/> pins it. Enumerating yields every copy,
    /// including the duplicates.
    /// </para>
    /// <para>
    /// A stored <c>null</c> is an ordinary element and counts as one distinct element, matching
    /// <see cref="MultiList{T}"/>. It is kept outside the trie - no comparer is required to hash
    /// <c>null</c>, and most throw when asked to.
    /// </para>
    /// <para>
    /// This type deliberately does not implement <see cref="IMultiSet{T}"/>: that interface declares
    /// <c>void Add</c> and <c>int Remove</c>, which no immutable type can honour. The same choice was
    /// made for <see cref="PersistentMultiDictionary{TKey,TValue}"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var bag = new PersistentBag&lt;string&gt;().Add("apple", 3).Add("pear");
    /// var smaller = bag.Remove("apple");
    ///
    /// bag.CountOf("apple");    // 3 - the receiver is untouched
    /// smaller.CountOf("apple"); // 2
    /// </code>
    /// </example>
    public sealed class PersistentBag<T> : IReadOnlyCollection<T>
    {
        /// <summary>
        /// The payload kind this collection stores under a key: a multiplicity, because every copy
        /// stored under an element <em>is</em> that element.
        /// </summary>
        private static readonly PersistentPayloadFactory<T, T> Payloads = PersistentPayloadFactory<T, T>.Counters;

        private readonly PersistentNode<T, T>? _root;
        private readonly IEqualityComparer<T> _comparer;
        private readonly int _nullCopies;
        private readonly int _distinctCount;
        private readonly int _totalCount;

        /// <summary>
        /// Initializes a new, empty bag that uses the default element comparer.
        /// </summary>
        public PersistentBag()
            : this((IEqualityComparer<T>?)null)
        {
        }

        /// <summary>
        /// Initializes a new, empty bag.
        /// </summary>
        /// <param name="comparer">The element comparer, or <c>null</c> for the default one.</param>
        public PersistentBag(IEqualityComparer<T>? comparer)
            : this(comparer, 0, 0, 0)
        {
        }

        /// <summary>
        /// Initializes a new bag holding one copy of each element of a sequence.
        /// </summary>
        /// <param name="collection">The elements to copy into the bag.</param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        public PersistentBag(IEnumerable<T> collection)
            : this(collection, null)
        {
        }

        /// <summary>
        /// Initializes a new bag holding one copy of each element of a sequence.
        /// </summary>
        /// <param name="collection">The elements to copy into the bag.</param>
        /// <param name="comparer">The element comparer, or <c>null</c> for the default one.</param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        public PersistentBag(IEnumerable<T> collection, IEqualityComparer<T>? comparer)
            : this(comparer, 0, 0, 0)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            // Build through the bulk accumulator: folding Add over the sequence would copy one trie
            // path per element, which is O(n log n) node copies for a bag that is known in full.
            var builder = new PersistentBagBuilder<T>(_comparer);

            foreach (var item in collection)
            {
                builder.Add(item);
            }

            var built = builder.ToBag();

            _root = built._root;
            _nullCopies = built._nullCopies;
            _distinctCount = built._distinctCount;
            _totalCount = built._totalCount;
        }

        private PersistentBag(IEqualityComparer<T>? comparer, int nullCopies, int distinctCount, int totalCount)
            : this(null, comparer, nullCopies, distinctCount, totalCount)
        {
        }

        private PersistentBag(PersistentNode<T, T>? root, IEqualityComparer<T>? comparer, int nullCopies, int distinctCount, int totalCount)
        {
            _root = root;
            _comparer = comparer ?? EqualityComparer<T>.Default;
            _nullCopies = nullCopies;
            _distinctCount = distinctCount;
            _totalCount = totalCount;
        }

        /// <summary>
        /// Gets the comparer used to decide element equality.
        /// </summary>
        /// <value>The comparer supplied at construction, or <see cref="EqualityComparer{T}.Default"/>.</value>
        public IEqualityComparer<T> Comparer => _comparer;

        /// <summary>
        /// Gets the total number of copies, which is what a multiset reports through the inherited
        /// collection count.
        /// </summary>
        /// <value>The sum of the copy counts of all distinct elements.</value>
        public int Count => _totalCount;

        /// <summary>
        /// Gets the total number of copies.
        /// </summary>
        /// <value>The sum of the copy counts of all distinct elements.</value>
        public int TotalCount => _totalCount;

        /// <summary>
        /// Gets the number of distinct elements. A stored <c>null</c> element counts as one
        /// distinct element.
        /// </summary>
        /// <value>The number of elements with at least one copy.</value>
        public int DistinctCount => _distinctCount;

        /// <summary>
        /// Gets a value indicating whether the bag holds no copies at all.
        /// </summary>
        /// <value><c>true</c> when <see cref="TotalCount"/> is zero.</value>
        public bool IsEmpty => _totalCount == 0;

        /// <summary>
        /// Returns a new bag with one more copy of an element.
        /// </summary>
        /// <param name="item">The element to add; <c>null</c> is an ordinary element.</param>
        /// <returns>A new bag; never the receiver, because a copy is always added.</returns>
        public PersistentBag<T> Add(T item)
        {
            return Add(item, 1);
        }

        /// <summary>
        /// Returns a new bag with copies of an element added.
        /// </summary>
        /// <param name="item">The element to add; <c>null</c> is an ordinary element.</param>
        /// <param name="times">The number of copies to add.</param>
        /// <returns>A new bag.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="times"/> is zero or negative, matching
        /// <see cref="MultiList{T}.Add(T, int)"/>.
        /// </exception>
        /// <remarks>
        /// Adding <c>m</c> copies costs one write path, not <c>m</c> of them: the multiplicity is a
        /// counter, so the work does not grow with the number of copies already held.
        /// </remarks>
        public PersistentBag<T> Add(T item, int times)
        {
            if (times <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(times), times, "The number of copies must be positive.");
            }

            if (ReferenceEquals(item, null))
            {
                return new PersistentBag<T>(
                    _root,
                    _comparer,
                    _nullCopies + times,
                    _nullCopies == 0 ? _distinctCount + 1 : _distinctCount,
                    _totalCount + times);
            }

            var hash = HashOf(item);

            if (_root == null)
            {
                var first = new PersistentEntry<T, T>(hash, item, Payloads.Create(item, times));
                return new PersistentBag<T>(
                    new CollisionNode<T, T>(hash, new[] { first }),
                    _comparer,
                    _nullCopies,
                    _distinctCount + 1,
                    _totalCount + times);
            }

            var hadKey = _root.TryGetEntry(hash, item, _comparer, out _);

            bool changed;
            var root = _root.Add(0, hash, item, item, _comparer, true, Payloads, times, out changed);

            if (!changed)
            {
                return this;
            }

            return new PersistentBag<T>(
                root,
                _comparer,
                _nullCopies,
                hadKey ? _distinctCount : _distinctCount + 1,
                _totalCount + times);
        }

        /// <summary>
        /// Returns a new bag with one copy of each element of a sequence added.
        /// </summary>
        /// <param name="items">The elements to add, one copy each.</param>
        /// <returns>A new bag, or the receiver when the sequence is empty.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is <c>null</c>.</exception>
        /// <remarks>
        /// Duplicates inside <paramref name="items"/> are honoured and the order of the sequence does
        /// not affect the result, which is what the bulk properties assert.
        /// </remarks>
        public PersistentBag<T> AddRange(IEnumerable<T> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var result = this;

            foreach (var item in items)
            {
                result = result.Add(item);
            }

            return result;
        }

        /// <summary>
        /// Returns a new bag with one copy of an element removed.
        /// </summary>
        /// <param name="item">The element to remove one copy of.</param>
        /// <returns>A new bag, or the receiver when no copy was present.</returns>
        /// <remarks>
        /// An element disappears from <see cref="DistinctCount"/> as soon as its last copy goes,
        /// which is why a count of zero and an absent element can never disagree.
        /// </remarks>
        public PersistentBag<T> Remove(T item)
        {
            return Remove(item, 1);
        }

        /// <summary>
        /// Returns a new bag with up to the specified number of copies of an element removed.
        /// </summary>
        /// <param name="item">The element to remove copies of.</param>
        /// <param name="times">The maximum number of copies to remove.</param>
        /// <returns>A new bag, or the receiver when nothing was removed.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="times"/> is negative.</exception>
        /// <remarks>
        /// Asking for more copies than are held removes what exists and reports nothing, mirroring
        /// <see cref="MultiList{T}.Remove(T, int)"/>. Call <see cref="CountOf"/> on the result to
        /// learn how many copies are left - an immutable type has no return slot for it.
        /// </remarks>
        public PersistentBag<T> Remove(T item, int times)
        {
            if (times <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(times), times, "The number of copies must be positive.");
            }

            if (times == 0)
            {
                return this;
            }

            var current = CountOf(item);

            if (current == 0)
            {
                return this;
            }

            var removed = times < current ? times : current;

            if (ReferenceEquals(item, null))
            {
                return new PersistentBag<T>(
                    _root,
                    _comparer,
                    _nullCopies - removed,
                    removed == current ? _distinctCount - 1 : _distinctCount,
                    _totalCount - removed);
            }

            var hash = HashOf(item);

            int actuallyRemoved;
            var root = _root!.RemoveValue(0, hash, item, item, removed, _comparer, out actuallyRemoved);

            if (actuallyRemoved == 0)
            {
                return this;
            }

            return new PersistentBag<T>(
                root,
                _comparer,
                _nullCopies,
                actuallyRemoved == current ? _distinctCount - 1 : _distinctCount,
                _totalCount - actuallyRemoved);
        }

        /// <summary>
        /// Returns a new bag with every copy of an element removed.
        /// </summary>
        /// <param name="item">The element to remove entirely.</param>
        /// <returns>A new bag, or the receiver when the element was absent.</returns>
        public PersistentBag<T> RemoveAllCopies(T item)
        {
            var current = CountOf(item);
            return current == 0 ? this : Remove(item, current);
        }

        /// <summary>
        /// Returns a new, empty bag with the same comparer.
        /// </summary>
        /// <returns>An empty bag, or the receiver when it was already empty.</returns>
        public PersistentBag<T> Clear()
        {
            return _totalCount == 0 ? this : new PersistentBag<T>(_comparer, 0, 0, 0);
        }

        /// <summary>
        /// Determines whether the bag holds at least one copy of an element.
        /// </summary>
        /// <param name="item">The element to locate.</param>
        /// <returns><c>true</c> when at least one copy is present.</returns>
        public bool Contains(T item)
        {
            return CountOf(item) > 0;
        }

        /// <summary>
        /// Determines whether the bag holds at least one copy of every element of a sequence.
        /// </summary>
        /// <param name="items">The elements to locate.</param>
        /// <returns><c>true</c> when every element occurs at least once, and for an empty sequence.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is <c>null</c>.</exception>
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
        /// Returns the number of copies of an element.
        /// </summary>
        /// <param name="item">The element to count.</param>
        /// <returns>The copy count, or zero when the element is absent.</returns>
        public int CountOf(T item)
        {
            if (ReferenceEquals(item, null))
            {
                return _nullCopies;
            }

            if (_root == null)
            {
                return 0;
            }

            PersistentEntry<T, T> entry;
            return _root.TryGetEntry(HashOf(item), item, _comparer, out entry) ? entry.ValueCount : 0;
        }

        /// <summary>
        /// Enumerates each distinct element once.
        /// </summary>
        /// <returns>The distinct elements, in unspecified order.</returns>
        public IEnumerable<T> DistinctItems()
        {
            if (_nullCopies > 0)
            {
                yield return default!;
            }

            if (_root == null)
            {
                yield break;
            }

            foreach (var entry in CollectEntries())
            {
                yield return entry.Key;
            }
        }

        /// <summary>
        /// Returns a builder that applies many changes and builds one bag out of them.
        /// </summary>
        /// <returns>A builder holding a copy of this bag's copies.</returns>
        /// <remarks>
        /// Building a bag of <c>N</c> copies by folding <see cref="Add(T)"/> over them costs
        /// O(<c>N</c> log <c>N</c>) node copies. The builder pays one pass in and one pass out
        /// instead, and every change in between is a dictionary update, so a batch of <c>M</c>
        /// changes over <c>N</c> existing copies costs O(<c>N</c> + <c>M</c>).
        /// </remarks>
        public PersistentBagBuilder<T> ToBuilder()
        {
            return new PersistentBagBuilder<T>(this);
        }

        /// <summary>
        /// Returns an enumerator over every copy.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            for (var copy = 0; copy < _nullCopies; copy++)
            {
                yield return default!;
            }

            if (_root == null)
            {
                yield break;
            }

            foreach (var entry in CollectEntries())
            {
                for (var copy = 0; copy < entry.ValueCount; copy++)
                {
                    yield return entry.Key;
                }
            }
        }

        /// <summary>
        /// Returns an enumerator over every copy.
        /// </summary>
        /// <returns>The enumerator.</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns a description of the bag's size.
        /// </summary>
        /// <returns>A string of the form <c>PersistentBag&lt;T&gt;(copies, distinct)</c>.</returns>
        public override string ToString()
        {
            return "PersistentBag<" + typeof(T).Name + ">(" + _totalCount + " copies, " + _distinctCount + " distinct)";
        }

        /// <summary>Gets the trie root, for the builder's single-pass population.</summary>
        internal PersistentNode<T, T>? Root => _root;

        /// <summary>Gets the number of stored <c>null</c> copies, for the builder.</summary>
        internal int NullCopies => _nullCopies;

        private int HashOf(T item)
        {
            // Callers have already routed a null reference to the bag's own null counter, so the
            // comparer is never asked to hash one.
            return _comparer.GetHashCode(item!) & 0x7FFFFFFF;
        }

        private List<PersistentEntry<T, T>> CollectEntries()
        {
            var sink = new List<PersistentEntry<T, T>>(_distinctCount);
            _root!.CollectEntries(sink);
            return sink;
        }
    }
}
