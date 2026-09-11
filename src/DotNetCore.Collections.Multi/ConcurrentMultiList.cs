using System;
using System.Collections;
using System.Collections.Generic;

// CS8714: T is deliberately unconstrained because null elements are supported
// (tracked in a dedicated bucket by the wrapped MultiList<T>).
#pragma warning disable CS8714

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Represents a thread-safe multiset (bag): an unordered collection that allows duplicate
    /// elements and tracks the number of occurrences (copies) of each element.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every operation is serialized behind a single lock, so the type is linearizable and
    /// trivially safe, at the cost of no read parallelism. That is deliberate:
    /// <see cref="ConcurrentMultiDictionary{TKey,TValue}"/> shards because keys are independent,
    /// but a multiset has one global state that its operations compare against, so sharding a
    /// bag's operations would trade the simple whole-state semantics for little gain. For read-most
    /// workloads prefer <see cref="ImmutableMultiList{T}"/> with a
    /// <see cref="ImmutableMultiList{T}.Builder"/>.
    /// </para>
    /// <para>
    /// Enumeration is over a snapshot: the enumerator is immune to concurrent writes, at the cost
    /// of copying the bag when enumeration starts.
    /// </para>
    /// <para>
    /// Element and copy-count semantics are those of <see cref="MultiList{T}"/> and are delegated
    /// to it wholesale.
    /// </para>
    /// </remarks>
    public class ConcurrentMultiList<T> : IEnumerable<T>, IReadOnlyCollection<T>
    {
        private readonly MultiList<T> _inner;
        private readonly object _lock;

        /// <summary>
        /// Initializes an empty concurrent multiset.
        /// </summary>
        public ConcurrentMultiList() : this(0, null)
        {
        }

        /// <summary>
        /// Initializes an empty concurrent multiset with the specified element comparer.
        /// </summary>
        public ConcurrentMultiList(IEqualityComparer<T>? comparer) : this(0, comparer)
        {
        }

        /// <summary>
        /// Initializes an empty concurrent multiset with the specified initial capacity.
        /// </summary>
        public ConcurrentMultiList(int capacity) : this(capacity, null)
        {
        }

        /// <summary>
        /// Initializes an empty concurrent multiset with the specified initial capacity and
        /// element comparer.
        /// </summary>
        public ConcurrentMultiList(int capacity, IEqualityComparer<T>? comparer)
        {
            _inner = new MultiList<T>(capacity, comparer);
            _lock = new object();
        }

        /// <summary>
        /// Initializes the concurrent multiset with the copies of the specified collection.
        /// </summary>
        public ConcurrentMultiList(IEnumerable<T> collection) : this(collection, null)
        {
        }

        /// <summary>
        /// Initializes the concurrent multiset with the copies of the specified collection and
        /// element comparer.
        /// </summary>
        public ConcurrentMultiList(IEnumerable<T> collection, IEqualityComparer<T>? comparer)
        {
            _inner = new MultiList<T>(collection, comparer);
            _lock = new object();
        }

        /// <summary>
        /// Gets the comparer that determines element equality.
        /// </summary>
        public IEqualityComparer<T> Comparer => _inner.Comparer;

        /// <summary>
        /// Gets the total number of elements stored, duplicates included.
        /// </summary>
        public int TotalCount
        {
            get
            {
                lock (_lock)
                {
                    return _inner.TotalCount;
                }
            }
        }

        /// <summary>
        /// Gets the number of distinct elements stored (copies are counted once).
        /// </summary>
        public int DistinctCount
        {
            get
            {
                lock (_lock)
                {
                    return _inner.DistinctCount;
                }
            }
        }

        /// <summary>
        /// Gets the total number of elements stored. Alias of <see cref="TotalCount"/>, present
        /// for <see cref="IReadOnlyCollection{T}"/>.
        /// </summary>
        public int Count => TotalCount;

        /// <summary>
        /// Adds the given element once.
        /// </summary>
        public void Add(T item)
        {
            lock (_lock)
            {
                _inner.Add(item);
            }
        }

        /// <summary>
        /// Adds the given element the specified number of times. A non-positive
        /// <paramref name="times"/> throws <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        public void Add(T item, int times)
        {
            lock (_lock)
            {
                _inner.Add(item, times);
            }
        }

        /// <summary>
        /// Adds the copies of the specified collection.
        /// </summary>
        public void AddRange(IEnumerable<T> items)
        {
            lock (_lock)
            {
                _inner.AddRange(items);
            }
        }

        /// <summary>
        /// Removes every copy of the given element and returns whether anything was removed.
        /// </summary>
        public bool RemoveAllCopies(T item)
        {
            lock (_lock)
            {
                return _inner.RemoveAllCopies(item);
            }
        }

        /// <summary>
        /// Removes the specified number of copies of the given element and returns how many copies
        /// were actually removed.
        /// </summary>
        public int Remove(T item, int times)
        {
            lock (_lock)
            {
                return _inner.Remove(item, times);
            }
        }

        /// <summary>
        /// Removes all elements.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _inner.Clear();
            }
        }

        /// <summary>
        /// Determines whether the bag contains at least one copy of the given element.
        /// </summary>
        public bool Contains(T item)
        {
            lock (_lock)
            {
                return _inner.Contains(item);
            }
        }

        /// <summary>
        /// Determines whether the bag contains at least one copy of every element of the given
        /// collection.
        /// </summary>
        public bool ContainsAll(IEnumerable<T> items)
        {
            lock (_lock)
            {
                return _inner.ContainsAll(items);
            }
        }

        /// <summary>
        /// Gets the number of copies of the given element stored in the bag.
        /// </summary>
        public int CountOf(T item)
        {
            lock (_lock)
            {
                return _inner.CountOf(item);
            }
        }

        /// <summary>
        /// Copies the elements (duplicates included) of a snapshot into the given array.
        /// </summary>
        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_lock)
            {
                _inner.CopyTo(array, arrayIndex);
            }
        }

        /// <summary>
        /// Takes a snapshot of the bag as an independent <see cref="MultiList{T}"/>. The snapshot
        /// is immune to concurrent writes; enumerations of this bag are snapshots too.
        /// </summary>
        public MultiList<T> Snapshot()
        {
            lock (_lock)
            {
                return _inner.Clone();
            }
        }

        /// <summary>
        /// Enumerates a snapshot of the bag. The enumerator is immune to concurrent writes.
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return Snapshot().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            lock (_lock)
            {
                return _inner.ToString();
            }
        }
    }
}
