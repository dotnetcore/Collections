using System;
using System.Collections.Generic;

// The count table is keyed by T, which is deliberately unconstrained because a bag of null elements
// has to be expressible. A null element never reaches the dictionary - it is kept in its own counter,
// because no comparer is required to hash null - so the constraint the compiler wants here holds at
// run time for every key the table actually sees. Suppressing the warning is therefore narrower than
// narrowing T, which would push the restriction onto every caller.
#pragma warning disable CS8714

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Accumulates changes to a multiset and builds one <see cref="PersistentBag{T}"/> out of them.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <remarks>
    /// <para>
    /// A persistent collection has no in-place mutation, so folding <see cref="PersistentBag{T}.Add(T)"/>
    /// over <c>N</c> elements copies one trie path per element - correct, but O(<c>N</c> log <c>N</c>)
    /// node writes for what is really a bulk construction. The builder pays a different price: one
    /// pass in (copy the counts of the bag it started from, if any), dictionary updates in between,
    /// and one pass out when <see cref="ToBag"/> walks the accumulated counts into a fresh trie. A
    /// batch of <c>M</c> changes on top of <c>N</c> existing copies therefore costs
    /// O(<c>N</c> + <c>M</c>) rather than O(<c>M</c> log <c>N</c>) node copies, and none of those
    /// changes allocates a node at all.
    /// </para>
    /// <para>
    /// The builder is mutable, not thread-safe, and keeps its own copy of the counts: changes made
    /// through it are invisible to any bag already handed out, and the bag it was created from is
    /// never touched. <see cref="ToBag"/> may be called more than once and does not invalidate the
    /// builder - each call snapshots the counts as they are at that moment.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var builder = new PersistentBagBuilder&lt;string&gt;();
    /// foreach (var word in words)
    /// {
    ///     builder.Add(word);
    /// }
    ///
    /// PersistentBag&lt;string&gt; bag = builder.ToBag();
    /// </code>
    /// </example>
    public sealed class PersistentBagBuilder<T>
    {
        private readonly IEqualityComparer<T> _comparer;
        private readonly Dictionary<T, int> _counts;

        private int _nullCopies;
        private int _distinctCount;
        private int _totalCount;

        /// <summary>
        /// Initializes a new, empty builder that uses the default element comparer.
        /// </summary>
        public PersistentBagBuilder()
            : this((IEqualityComparer<T>?)null)
        {
        }

        /// <summary>
        /// Initializes a new, empty builder.
        /// </summary>
        /// <param name="comparer">The element comparer, or <c>null</c> for the default one.</param>
        public PersistentBagBuilder(IEqualityComparer<T>? comparer)
        {
            _comparer = comparer ?? EqualityComparer<T>.Default;
            _counts = new Dictionary<T, int>(_comparer);
        }

        internal PersistentBagBuilder(PersistentBag<T> bag)
            : this(bag.Comparer)
        {
            if (bag.Root != null)
            {
                var entries = new List<PersistentEntry<T, T>>();
                bag.Root.CollectEntries(entries);

                for (var index = 0; index < entries.Count; index++)
                {
                    var entry = entries[index];
                    _counts[entry.Key] = entry.ValueCount;
                    _totalCount += entry.ValueCount;
                    _distinctCount++;
                }
            }

            if (bag.NullCopies > 0)
            {
                _nullCopies = bag.NullCopies;
                _totalCount += bag.NullCopies;
                _distinctCount++;
            }
        }

        /// <summary>
        /// Gets the comparer used to decide element equality.
        /// </summary>
        /// <value>The comparer this builder was created with.</value>
        public IEqualityComparer<T> Comparer => _comparer;

        /// <summary>
        /// Gets the total number of copies accumulated so far.
        /// </summary>
        /// <value>The sum of the copy counts of all distinct elements.</value>
        public int Count => _totalCount;

        /// <summary>
        /// Gets the total number of copies accumulated so far.
        /// </summary>
        /// <value>The sum of the copy counts of all distinct elements.</value>
        public int TotalCount => _totalCount;

        /// <summary>
        /// Gets the number of distinct elements accumulated so far. A <c>null</c> element counts as
        /// one distinct element.
        /// </summary>
        /// <value>The number of elements with at least one copy.</value>
        public int DistinctCount => _distinctCount;

        /// <summary>
        /// Adds one copy of an element.
        /// </summary>
        /// <param name="item">The element to add; <c>null</c> is an ordinary element.</param>
        public void Add(T item)
        {
            Add(item, 1);
        }

        /// <summary>
        /// Adds copies of an element.
        /// </summary>
        /// <param name="item">The element to add; <c>null</c> is an ordinary element.</param>
        /// <param name="times">The number of copies to add.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="times"/> is negative.</exception>
        public void Add(T item, int times)
        {
            if (times <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(times), times, "The number of copies must be positive.");
            }

            if (times == 0)
            {
                return;
            }

            if (ReferenceEquals(item, null))
            {
                if (_nullCopies == 0)
                {
                    _distinctCount++;
                }

                _nullCopies += times;
                _totalCount += times;
                return;
            }

            int current;
            if (_counts.TryGetValue(item, out current))
            {
                _counts[item] = current + times;
            }
            else
            {
                _counts.Add(item, times);
                _distinctCount++;
            }

            _totalCount += times;
        }

        /// <summary>
        /// Adds one copy of each element of a sequence.
        /// </summary>
        /// <param name="items">The elements to add.</param>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is <c>null</c>.</exception>
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
        /// Removes one copy of an element.
        /// </summary>
        /// <param name="item">The element to remove one copy of.</param>
        public void Remove(T item)
        {
            Remove(item, 1);
        }

        /// <summary>
        /// Removes up to the specified number of copies of an element.
        /// </summary>
        /// <param name="item">The element to remove copies of.</param>
        /// <param name="times">The maximum number of copies to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="times"/> is negative.</exception>
        public void Remove(T item, int times)
        {
            if (times <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(times), times, "The number of copies must be positive.");
            }

            var current = CountOf(item);

            if (times == 0 || current == 0)
            {
                return;
            }

            var removed = times < current ? times : current;
            var remaining = current - removed;

            if (ReferenceEquals(item, null))
            {
                _nullCopies = remaining;
            }
            else if (remaining == 0)
            {
                _counts.Remove(item);
            }
            else
            {
                _counts[item] = remaining;
            }

            if (remaining == 0)
            {
                _distinctCount--;
            }

            _totalCount -= removed;
        }

        /// <summary>
        /// Removes every copy of an element.
        /// </summary>
        /// <param name="item">The element to remove entirely.</param>
        public void RemoveAllCopies(T item)
        {
            var current = CountOf(item);

            if (current > 0)
            {
                Remove(item, current);
            }
        }

        /// <summary>
        /// Removes every element and copy.
        /// </summary>
        public void Clear()
        {
            _counts.Clear();
            _nullCopies = 0;
            _distinctCount = 0;
            _totalCount = 0;
        }

        /// <summary>
        /// Determines whether at least one copy of an element has been accumulated.
        /// </summary>
        /// <param name="item">The element to locate.</param>
        /// <returns><c>true</c> when at least one copy is present.</returns>
        public bool Contains(T item)
        {
            return CountOf(item) > 0;
        }

        /// <summary>
        /// Returns the number of copies of an element accumulated so far.
        /// </summary>
        /// <param name="item">The element to count.</param>
        /// <returns>The copy count, or zero when the element is absent.</returns>
        public int CountOf(T item)
        {
            if (ReferenceEquals(item, null))
            {
                return _nullCopies;
            }

            int current;
            return _counts.TryGetValue(item, out current) ? current : 0;
        }

        /// <summary>
        /// Builds a bag holding the accumulated copies.
        /// </summary>
        /// <returns>A new bag; the builder stays usable.</returns>
        public PersistentBag<T> ToBag()
        {
            var bag = new PersistentBag<T>(_comparer);

            foreach (var pair in _counts)
            {
                bag = bag.Add(pair.Key, pair.Value);
            }

            if (_nullCopies > 0)
            {
                bag = bag.Add(default!, _nullCopies);
            }

            return bag;
        }

        /// <summary>
        /// Returns a description of the builder's size.
        /// </summary>
        /// <returns>A string of the form <c>PersistentBagBuilder&lt;T&gt;(copies, distinct)</c>.</returns>
        public override string ToString()
        {
            return "PersistentBagBuilder<" + typeof(T).Name + ">(" + _totalCount + " copies, " + _distinctCount + " distinct)";
        }
    }
}

#pragma warning restore CS8714
