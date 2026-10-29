using System;
using System.Collections.Generic;

namespace DotNetCore.Collections.Testing.PropertyTesting
{
    /// <summary>
    /// An independent, deliberately naive reference implementation of multiset semantics, used as
    /// the oracle the real collections are compared against.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <remarks>
    /// <para>
    /// The whole point of this type is to be obviously correct rather than fast: copy counts live in
    /// a <see cref="Dictionary{TKey,TValue}"/> and every member is a direct transcription of the
    /// contract documented on <c>IMultiSet&lt;T&gt;</c>. It shares no code with the production
    /// types, so a defect that both implementations would have to share cannot hide here.
    /// </para>
    /// <para>
    /// Contract points that are easy to get wrong and are therefore mirrored exactly:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// <see cref="Remove(T)"/> and <see cref="Remove(T, int)"/> return the number of copies
    /// <em>remaining</em>, not the number removed, and return zero both when the element is absent
    /// and when its last copy has just been removed.
    /// </description></item>
    /// <item><description>
    /// An element disappears from the model when its copy count reaches zero, so
    /// <see cref="DistinctCount"/> never counts zero-copy entries.
    /// </description></item>
    /// <item><description>
    /// <see cref="Add(T, int)"/> and <see cref="Remove(T, int)"/> reject a non-positive count with
    /// <see cref="ArgumentOutOfRangeException"/>, matching the documented contract.
    /// </description></item>
    /// </list>
    /// </remarks>
    public sealed class MultisetModel<T>
    {
        private readonly Dictionary<T, int> _counts;
        private int _nullCount;

        /// <summary>
        /// Initializes a new, empty model.
        /// </summary>
        /// <param name="comparer">
        /// The element comparer, or <c>null</c> to use <see cref="EqualityComparer{T}.Default"/>.
        /// Passing the same comparer to the real collection is what makes comparer-injection
        /// properties meaningful.
        /// </param>
        public MultisetModel(IEqualityComparer<T> comparer)
        {
            _counts = new Dictionary<T, int>(comparer);
        }

        /// <summary>
        /// Gets the total number of copies held, equivalent to <c>IMultiSet&lt;T&gt;.TotalCount</c>.
        /// </summary>
        public int TotalCount { get; private set; }

        /// <summary>
        /// Gets the number of distinct elements held, equivalent to <c>IMultiSet&lt;T&gt;.DistinctCount</c>.
        /// </summary>
        /// <remarks>
        /// <c>null</c> is tracked outside the dictionary, because <see cref="Dictionary{TKey,TValue}"/>
        /// rejects a <c>null</c> key while the multiset contract explicitly allows a
        /// <c>null</c> element for reference types.
        /// </remarks>
        public int DistinctCount => _counts.Count + (_nullCount > 0 ? 1 : 0);

        /// <summary>
        /// Adds one copy of the element.
        /// </summary>
        /// <param name="item">The element to add.</param>
        public void Add(T item)
        {
            Add(item, 1);
        }

        /// <summary>
        /// Adds the specified number of copies of the element.
        /// </summary>
        /// <param name="item">The element to add.</param>
        /// <param name="times">The number of copies to add. Must be positive.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="times"/> is not positive.</exception>
        public void Add(T item, int times)
        {
            if (times <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(times), times, "The number of copies must be positive.");
            }

            if (ReferenceEquals(item, null))
            {
                _nullCount += times;
                TotalCount += times;
                return;
            }

            int current;
            _counts.TryGetValue(item, out current);
            _counts[item] = current + times;
            TotalCount += times;
        }

        /// <summary>
        /// Adds one copy of each element of the specified sequence.
        /// </summary>
        /// <param name="items">The elements to add. Each occurrence contributes one copy.</param>
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
        /// Returns the number of copies of the element.
        /// </summary>
        /// <param name="item">The element to count.</param>
        /// <returns>The copy count, or zero when the element is absent.</returns>
        public int CountOf(T item)
        {
            if (ReferenceEquals(item, null))
            {
                return _nullCount;
            }

            int current;
            return _counts.TryGetValue(item, out current) ? current : 0;
        }

        /// <summary>
        /// Determines whether the model holds at least one copy of the element.
        /// </summary>
        /// <param name="item">The element to locate.</param>
        /// <returns><c>true</c> when at least one copy is present.</returns>
        public bool Contains(T item)
        {
            if (ReferenceEquals(item, null))
            {
                return _nullCount > 0;
            }

            return _counts.ContainsKey(item);
        }

        /// <summary>
        /// Removes a single copy of the element.
        /// </summary>
        /// <param name="item">The element to remove.</param>
        /// <returns>The number of copies remaining, or zero when the element was absent.</returns>
        public int Remove(T item)
        {
            return Remove(item, 1);
        }

        /// <summary>
        /// Removes up to the specified number of copies of the element.
        /// </summary>
        /// <param name="item">The element to remove.</param>
        /// <param name="times">The maximum number of copies to remove. Must be positive.</param>
        /// <returns>The number of copies remaining, or zero when the element was absent.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="times"/> is not positive.</exception>
        public int Remove(T item, int times)
        {
            if (times <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(times), times, "The number of copies must be positive.");
            }

            if (ReferenceEquals(item, null))
            {
                var removedNulls = _nullCount < times ? _nullCount : times;
                _nullCount -= removedNulls;
                TotalCount -= removedNulls;
                return _nullCount;
            }

            int current;
            if (!_counts.TryGetValue(item, out current))
            {
                return 0;
            }

            var removed = current < times ? current : times;
            var remaining = current - removed;
            TotalCount -= removed;

            if (remaining == 0)
            {
                _counts.Remove(item);
            }
            else
            {
                _counts[item] = remaining;
            }

            return remaining;
        }

        /// <summary>
        /// Removes every copy of the element.
        /// </summary>
        /// <param name="item">The element to remove.</param>
        /// <returns><c>true</c> when at least one copy was removed.</returns>
        public bool RemoveAllCopies(T item)
        {
            if (ReferenceEquals(item, null))
            {
                if (_nullCount == 0)
                {
                    return false;
                }

                TotalCount -= _nullCount;
                _nullCount = 0;
                return true;
            }

            int current;
            if (!_counts.TryGetValue(item, out current))
            {
                return false;
            }

            _counts.Remove(item);
            TotalCount -= current;
            return true;
        }

        /// <summary>
        /// Removes every element and copy.
        /// </summary>
        public void Clear()
        {
            _counts.Clear();
            _nullCount = 0;
            TotalCount = 0;
        }

        /// <summary>
        /// Enumerates each distinct element exactly once.
        /// </summary>
        /// <returns>The distinct elements, in unspecified order.</returns>
        public IEnumerable<T> DistinctItems()
        {
            if (_nullCount > 0)
            {
                yield return default(T);
            }

            foreach (var key in _counts.Keys)
            {
                yield return key;
            }
        }

        /// <summary>
        /// Enumerates each distinct element with its copy count.
        /// </summary>
        /// <returns>The entries, in unspecified order.</returns>
        public IEnumerable<KeyValuePair<T, int>> Entries()
        {
            if (_nullCount > 0)
            {
                yield return new KeyValuePair<T, int>(default(T), _nullCount);
            }

            foreach (var entry in _counts)
            {
                yield return entry;
            }
        }
    }
}
