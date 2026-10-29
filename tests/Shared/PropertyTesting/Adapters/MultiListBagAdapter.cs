using System.Collections.Generic;
using DotNetCore.Collections.Multi;

namespace DotNetCore.Collections.Testing.PropertyTesting.Adapters
{
    /// <summary>
    /// Adapts <see cref="MultiList{T}"/> - a mutable multiset - to the property suite.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <remarks>
    /// Every mutating member calls into the collection and returns the same instance, which is what
    /// the suite expects from a mutable implementation. The persistent adapter added in 8.1.0 will
    /// differ only in returning the new instance.
    /// </remarks>
    public sealed class MultiListBagAdapter<T> : IBagUnderTest<MultiList<T>, T>
    {
        /// <inheritdoc />
        public MultiList<T> Create(IEqualityComparer<T> comparer)
        {
            return new MultiList<T>(comparer);
        }

        /// <inheritdoc />
        public MultiList<T> Add(MultiList<T> bag, T item)
        {
            bag.Add(item);
            return bag;
        }

        /// <inheritdoc />
        public MultiList<T> AddCopies(MultiList<T> bag, T item, int times)
        {
            bag.Add(item, times);
            return bag;
        }

        /// <inheritdoc />
        public MultiList<T> RemoveOne(MultiList<T> bag, T item, out int remainingCopies)
        {
            remainingCopies = bag.Remove(item);
            return bag;
        }

        /// <inheritdoc />
        public MultiList<T> RemoveCopies(MultiList<T> bag, T item, int times, out int remainingCopies)
        {
            remainingCopies = bag.Remove(item, times);
            return bag;
        }

        /// <inheritdoc />
        public MultiList<T> RemoveAllCopies(MultiList<T> bag, T item, out bool removedAnything)
        {
            removedAnything = bag.RemoveAllCopies(item);
            return bag;
        }

        /// <inheritdoc />
        public MultiList<T> Clear(MultiList<T> bag)
        {
            bag.Clear();
            return bag;
        }

        /// <inheritdoc />
        public int CountOf(MultiList<T> bag, T item)
        {
            return bag.CountOf(item);
        }

        /// <inheritdoc />
        public int TotalCount(MultiList<T> bag)
        {
            return bag.TotalCount;
        }

        /// <inheritdoc />
        public int DistinctCount(MultiList<T> bag)
        {
            return bag.DistinctCount;
        }

        /// <inheritdoc />
        /// <remarks>
        /// <see cref="MultiList{T}"/> implements <c>Count</c> explicitly for both collection
        /// interfaces, so the value has to be read through one of them. The multiset contract pins
        /// it to <c>TotalCount</c>, which is exactly what this property asserts.
        /// </remarks>
        public int ReportedCount(MultiList<T> bag)
        {
            return ((IReadOnlyCollection<T>)bag).Count;
        }

        /// <inheritdoc />
        public bool Contains(MultiList<T> bag, T item)
        {
            return bag.Contains(item);
        }

        /// <inheritdoc />
        public IEnumerable<T> DistinctItems(MultiList<T> bag)
        {
            return bag.DistinctItems();
        }
    }
}
