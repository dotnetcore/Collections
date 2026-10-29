using System.Collections.Generic;
using DotNetCore.Collections.Multi;

namespace DotNetCore.Collections.Testing.PropertyTesting.Adapters
{
    /// <summary>
    /// Adapts <see cref="PersistentBag{T}"/> - a structurally shared immutable multiset - to the
    /// property suite.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <remarks>
    /// <para>
    /// Every mutating member returns the new instance rather than the one it was handed, which is the
    /// only difference from <see cref="MultiListBagAdapter{T}"/>; the property itself is unchanged.
    /// </para>
    /// <para>
    /// An immutable bag has no return slot for "how many copies remain", so the adapter reads that
    /// value back off the instance it received - the same way
    /// <see cref="PersistentMultiDictionaryAdapter{TKey,TValue}"/> derives "was anything removed"
    /// from reference identity. The value is still checked against the model, so a bag that removed
    /// the wrong number of copies fails here rather than passing silently.
    /// </para>
    /// </remarks>
    public sealed class PersistentBagAdapter<T> : IBagUnderTest<PersistentBag<T>, T>
    {
        /// <inheritdoc />
        public PersistentBag<T> Create(IEqualityComparer<T> comparer)
        {
            return new PersistentBag<T>(comparer);
        }

        /// <inheritdoc />
        public PersistentBag<T> Add(PersistentBag<T> bag, T item)
        {
            return bag.Add(item);
        }

        /// <inheritdoc />
        public PersistentBag<T> AddCopies(PersistentBag<T> bag, T item, int times)
        {
            return bag.Add(item, times);
        }

        /// <inheritdoc />
        public PersistentBag<T> AddRange(PersistentBag<T> bag, IEnumerable<T> items)
        {
            return bag.AddRange(items);
        }

        /// <inheritdoc />
        public PersistentBag<T> RemoveOne(PersistentBag<T> bag, T item, out int remainingCopies)
        {
            var next = bag.Remove(item);
            remainingCopies = next.CountOf(item);
            return next;
        }

        /// <inheritdoc />
        public PersistentBag<T> RemoveCopies(PersistentBag<T> bag, T item, int times, out int remainingCopies)
        {
            var next = bag.Remove(item, times);
            remainingCopies = next.CountOf(item);
            return next;
        }

        /// <inheritdoc />
        public PersistentBag<T> RemoveAllCopies(PersistentBag<T> bag, T item, out bool removedAnything)
        {
            var next = bag.RemoveAllCopies(item);
            removedAnything = !ReferenceEquals(next, bag);
            return next;
        }

        /// <inheritdoc />
        public PersistentBag<T> Clear(PersistentBag<T> bag)
        {
            return bag.Clear();
        }

        /// <inheritdoc />
        public int CountOf(PersistentBag<T> bag, T item)
        {
            return bag.CountOf(item);
        }

        /// <inheritdoc />
        public int TotalCount(PersistentBag<T> bag)
        {
            return bag.TotalCount;
        }

        /// <inheritdoc />
        public int DistinctCount(PersistentBag<T> bag)
        {
            return bag.DistinctCount;
        }

        /// <inheritdoc />
        public int ReportedCount(PersistentBag<T> bag)
        {
            return ((IReadOnlyCollection<T>)bag).Count;
        }

        /// <inheritdoc />
        public bool Contains(PersistentBag<T> bag, T item)
        {
            return bag.Contains(item);
        }

        /// <inheritdoc />
        public IEnumerable<T> DistinctItems(PersistentBag<T> bag)
        {
            return bag.DistinctItems();
        }
    }
}
