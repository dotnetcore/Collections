using System;
using System.Collections.Generic;

// This type is compiled only where Span<T> is in-box (netstandard2.1, net6.0+). On the legacy
// targets (net451..net48, netstandard2.0) Span<T> would require the external System.Memory
// package, and adding a dependency to keep one hot-path type alive there was judged the worse
// trade (the package has stayed dependency-free on every TFM so far).

#if NETSTANDARD2_1 || NET6_0_OR_GREATER

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Represents a stack-only temporary bag: a fixed-capacity counting bag over unmanaged
    /// elements, backed by caller-provided <see cref="Span{T}"/> storage (typically
    /// <c>stackalloc</c>), for short-lived frequency counting inside one method with zero heap
    /// allocation (F6-07).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The bag is a <c>ref struct</c>, so the compiler itself enforces the lifetime contract: it
    /// can not be boxed, can not be a field of a class or a normal struct, can not be captured by
    /// a lambda, and can not cross an <c>await</c> or <c>yield</c> boundary. It exists only on
    /// the stack frame where it was created — which is exactly the intent: count things inside a
    /// method, read the histogram, return, and the storage is gone with the frame. There is
    /// nothing to dispose and nothing to return: the memory was never taken from an allocator.
    /// </para>
    /// <para>
    /// The backing spans are <em>borrowed</em>, never copied or kept: the constructor takes the
    /// two spans the caller stack-allocated (one for the distinct elements, one for their
    /// counts), and the bag only ever reads and writes them in place. Because of that, there is
    /// deliberately no factory or <c>Create</c> method: a factory would have to allocate its own
    /// storage, and a span into a factory's stack frame dies with the factory's return — the
    /// escape is a compile error, by design. The caller allocates, the caller owns the lifetime.
    /// Within one method, <see cref="Clear"/> resets the counters so the same stack memory can be
    /// reused for another counting round without touching the allocator.
    /// </para>
    /// <para>
    /// Capacity is fixed at construction (<c>min(values.Length, counts.Length)</c> distinct
    /// elements fit). Adding a <em>new</em> element when the bag is full returns
    /// <c>false</c> — a sizing condition, not an exception; adding an element that is already
    /// present only bumps its count and always succeeds. There is deliberately no
    /// <see cref="List{T}"/>-style growth: growth needs the heap, and this type's whole reason to
    /// exist is not needing it.
    /// </para>
    /// <para>
    /// Boundary: the element type is constrained to <c>unmanaged</c> — stack storage can not hold
    /// references, so reference-type and managed-struct elements are excluded by the compiler.
    /// Element comparison routes through <see cref="EqualityComparer{T}.Default"/> (the modern
    /// runtimes this type targets specialize it for <c>int</c> and enums without boxing).
    /// Bag semantics mirror <see cref="MultiList{T}"/> where the two meet: duplicates are
    /// counted, <see cref="CountOf"/> reports multiplicities, <see cref="Remove(T)"/> takes one
    /// copy away and returns the number remaining, and an element disappears once its last copy
    /// is removed (the packed entries shift over the hole).
    /// </para>
    /// <para>
    /// This type is single-threaded by construction (stack-only state).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// int FirstDuplicate(ReadOnlySpan&lt;int&gt; source)
    /// {
    ///     Span&lt;int&gt; values = stackalloc int[32];
    ///     Span&lt;int&gt; counts = stackalloc int[32];
    ///     var seen = new SpanBag&lt;int&gt;(values, counts);
    ///
    ///     foreach (var item in source)
    ///     {
    ///         if (seen.CountOf(item) &gt; 0)
    ///         {
    ///             return item;               // first value seen twice
    ///         }
    ///
    ///         if (!seen.Add(item))
    ///         {
    ///             throw new InvalidOperationException("Not enough stack capacity.");
    ///         }
    ///     }
    ///
    ///     return -1;
    /// }
    /// </code>
    /// </example>
    public ref struct SpanBag<T> where T : unmanaged
    {
        private readonly Span<T> _values;
        private readonly Span<int> _counts;
        private readonly int _capacity;
        private int _size;
        private int _totalCount;

        /// <summary>
        /// Initializes a <see cref="SpanBag{T}"/> over caller-provided storage. Typically the two
        /// spans are freshly <c>stackalloc</c>ed in the calling frame. The bag borrows them; it
        /// does not copy or own them.
        /// </summary>
        /// <param name="values">storage for up to <c>values.Length</c> distinct elements.</param>
        /// <param name="counts">storage for the counts parallel to <paramref name="values"/>.</param>
        /// <exception cref="ArgumentException">The two spans have different lengths.</exception>
        public SpanBag(Span<T> values, Span<int> counts)
        {
            if (values.Length != counts.Length)
            {
                throw new ArgumentException(
                    "The element and count storage must have the same length.", nameof(counts));
            }

            _values = values;
            _counts = counts;
            _capacity = values.Length;
            _size = 0;
            _totalCount = 0;
        }

        /// <summary>
        /// Gets the maximum number of distinct elements the bag can hold (the caller-provided
        /// storage length).
        /// </summary>
        public int Capacity => _capacity;

        /// <summary>
        /// Gets the number of distinct elements currently stored.
        /// </summary>
        public int DistinctCount => _size;

        /// <summary>
        /// Gets the total number of copies across all elements.
        /// </summary>
        public int TotalCount => _totalCount;

        /// <summary>
        /// Gets the number of additional <em>distinct</em> elements that still fit before
        /// <see cref="Add(T)"/> starts returning <c>false</c>.
        /// </summary>
        public int Remaining => _capacity - _size;

        /// <summary>
        /// Gets a value indicating whether the bag holds no copies at all.
        /// </summary>
        public bool IsEmpty => _totalCount == 0;

        /// <summary>
        /// Adds a single copy of the element. Adding an element that is already present always
        /// succeeds (its count is bumped in place); adding a <em>new</em> element when the bag is
        /// full returns <c>false</c> and changes nothing.
        /// </summary>
        /// <example>
        /// <code>
        /// if (!bag.Add(item)) { /* capacity exhausted for a new distinct element */ }
        /// </code>
        /// </example>
        public bool Add(T item)
        {
            for (var i = 0; i < _size; i++)
            {
                if (EqualityComparer<T>.Default.Equals(_values[i], item))
                {
                    _counts[i]++;
                    _totalCount++;
                    return true;
                }
            }

            if (_size == _capacity)
            {
                return false;
            }

            _values[_size] = item;
            _counts[_size] = 1;
            _size++;
            _totalCount++;
            return true;
        }

        /// <summary>
        /// Determines whether the bag contains at least one copy of the element.
        /// </summary>
        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        /// <summary>
        /// Gets the number of copies of the element (zero when absent).
        /// </summary>
        /// <example>
        /// <code>
        /// int copies = bag.CountOf(item);
        /// </code>
        /// </example>
        public int CountOf(T item)
        {
            var index = IndexOf(item);
            return index >= 0 ? _counts[index] : 0;
        }

        /// <summary>
        /// Removes a single copy of the element and returns the number of copies remaining
        /// afterwards. Returns zero when the element is absent (no copy was removed). The entry
        /// is dropped — and the packed entries shift over the hole — once the last copy goes.
        /// </summary>
        /// <example>
        /// <code>
        /// int remaining = bag.Remove(item); // removes a single copy
        /// </code>
        /// </example>
        public int Remove(T item)
        {
            var index = IndexOf(item);
            if (index < 0)
            {
                return 0;
            }

            var remaining = _counts[index] - 1;
            if (remaining == 0)
            {
                RemoveEntryAt(index);
            }
            else
            {
                _counts[index] = remaining;
            }

            _totalCount--;
            return remaining;
        }

        /// <summary>
        /// Removes every copy of the element. Returns <c>true</c> when at least one copy was
        /// removed.
        /// </summary>
        public bool RemoveAllCopies(T item)
        {
            var index = IndexOf(item);
            if (index < 0)
            {
                return false;
            }

            _totalCount -= _counts[index];
            RemoveEntryAt(index);
            return true;
        }

        /// <summary>
        /// Removes all elements and counts. The backing stack memory stays with the caller's
        /// frame, so the same bag instance can be reused for another counting round without
        /// touching the allocator.
        /// </summary>
        /// <example>
        /// <code>
        /// bag.Clear(); // reuse the same stackalloc storage for the next round
        /// </code>
        /// </example>
        public void Clear()
        {
            // Nothing needs wiping: the element storage is unmanaged (no references to release)
            // and the counts beyond _size are unreachable. Resetting the sizes is the whole reset.
            _size = 0;
            _totalCount = 0;
        }

        /// <summary>
        /// Enumerates every distinct element together with its copy count, in first-encounter
        /// (insertion) order — the histogram view of the bag. The enumerator is a
        /// <c>ref struct</c>, so the enumeration can not outlive the bag's frame either.
        /// </summary>
        /// <example>
        /// <code>
        /// foreach (var (item, count) in bag) { }
        /// </code>
        /// </example>
        public EntryEnumerator GetEnumerator()
        {
            return new EntryEnumerator(_values, _counts, _size);
        }

        /// <summary>
        /// Finds the entry index of the element, or <c>-1</c> when absent. A linear scan over the
        /// borrowed stack storage — the deliberate trade of this type for small, dense counting
        /// workloads.
        /// </summary>
        private int IndexOf(T item)
        {
            for (var i = 0; i < _size; i++)
            {
                if (EqualityComparer<T>.Default.Equals(_values[i], item))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Removes the entry at <paramref name="index"/> by shifting the tail over it, keeping
        /// the storage packed: no zero-count holes, first-encounter order of the remaining
        /// elements preserved.
        /// </summary>
        private void RemoveEntryAt(int index)
        {
            _size--;
            if (index < _size)
            {
                _values.Slice(index + 1, _size - index).CopyTo(_values.Slice(index));
                _counts.Slice(index + 1, _size - index).CopyTo(_counts.Slice(index));
            }
        }

        /// <summary>
        /// Pattern-based enumerator over the packed entries (one per distinct element). A
        /// <c>ref struct</c> like its owner, so it never escapes the frame; it carries no
        /// <c>Dispose</c> because there is nothing to release.
        /// </summary>
        public ref struct EntryEnumerator
        {
            private readonly Span<T> _values;
            private readonly Span<int> _counts;
            private readonly int _size;
            private int _index;

            public EntryEnumerator(Span<T> values, Span<int> counts, int size)
            {
                _values = values;
                _counts = counts;
                _size = size;
                _index = -1;
            }

            /// <summary>
            /// Gets the (element, count) entry at the current position. This is the property the
            /// pattern-based <c>foreach</c> reads.
            /// </summary>
            public (T Value, int Count) Current => (_values[_index], _counts[_index]);

            /// <summary>Gets the element at the current position.</summary>
            public T Value => _values[_index];

            /// <summary>Gets the copy count at the current position.</summary>
            public int Count => _counts[_index];

            /// <summary>
            /// Advances to the next entry. The enumeration is a live walk over the bag's storage.
            /// </summary>
            public bool MoveNext()
            {
                _index++;
                return _index < _size;
            }
        }
    }
}

#endif
