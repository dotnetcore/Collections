using System;
using System.Collections.Generic;

namespace DotNetCore.Collections.Testing.PropertyTesting
{
    /// <summary>
    /// Adapts one bag implementation to the vocabulary the property suite speaks, so that a single
    /// suite can certify both mutable and persistent implementations.
    /// </summary>
    /// <typeparam name="TBag">The concrete bag type.</typeparam>
    /// <typeparam name="T">The element type.</typeparam>
    /// <remarks>
    /// <para>
    /// The indirection exists because the two families do not share a mutating signature:
    /// <c>MultiList&lt;T&gt;.Add</c> mutates and returns <c>void</c>, whereas a persistent bag
    /// returns a new instance. Every member here therefore takes the current instance and returns
    /// the instance to use afterwards. A mutable adapter ignores the input and returns the same
    /// object; a persistent adapter returns the new one. The property suite never has to know which
    /// it is talking to.
    /// </para>
    /// <para>
    /// Removal members expose the value the contract promises through <c>out</c> parameters
    /// (<c>IMultiSet&lt;T&gt;.Remove</c> returns the number of copies <em>remaining</em>), because
    /// that return value is itself part of the contract under test.
    /// </para>
    /// <para>
    /// <c>EntrySet()</c> is deliberately absent: it returns a tuple, and this kit also compiles at
    /// C# 7.3 for the <c>net462</c> test project, where named tuples would drag in an otherwise
    /// unnecessary dependency. <see cref="DistinctItems"/> plus <see cref="CountOf"/> is
    /// equivalent and dependency-free.
    /// </para>
    /// </remarks>
    public interface IBagUnderTest<TBag, T>
    {
        /// <summary>
        /// Creates an empty bag.
        /// </summary>
        /// <param name="comparer">The element comparer, or <c>null</c> for the default one.</param>
        /// <returns>An empty bag.</returns>
        TBag Create(IEqualityComparer<T> comparer);

        /// <summary>
        /// Adds one copy of the element.
        /// </summary>
        /// <param name="bag">The current instance.</param>
        /// <param name="item">The element to add.</param>
        /// <returns>The instance to use afterwards.</returns>
        TBag Add(TBag bag, T item);

        /// <summary>
        /// Adds the specified number of copies of the element.
        /// </summary>
        /// <param name="bag">The current instance.</param>
        /// <param name="item">The element to add.</param>
        /// <param name="times">The number of copies to add.</param>
        /// <returns>The instance to use afterwards.</returns>
        TBag AddCopies(TBag bag, T item, int times);

        /// <summary>
        /// Adds one copy of each element of the specified sequence.
        /// </summary>
        /// <param name="bag">The current instance.</param>
        /// <param name="items">The elements to add.</param>
        /// <returns>The instance to use afterwards.</returns>
        /// <remarks>
        /// An adapter whose implementation has no bulk member composes this from
        /// <see cref="Add"/>, so the suite can still express the operation. The number of distinct
        /// elements in the result must not depend on the order of <paramref name="items"/>, which is
        /// what <c>BagBulkOperationTests</c> asserts.
        /// </remarks>
        TBag AddRange(TBag bag, IEnumerable<T> items);

        /// <summary>
        /// Removes a single copy of the element.
        /// </summary>
        /// <param name="bag">The current instance.</param>
        /// <param name="item">The element to remove.</param>
        /// <param name="remainingCopies">The value the contract promises as the return value.</param>
        /// <returns>The instance to use afterwards.</returns>
        TBag RemoveOne(TBag bag, T item, out int remainingCopies);

        /// <summary>
        /// Removes up to the specified number of copies of the element.
        /// </summary>
        /// <param name="bag">The current instance.</param>
        /// <param name="item">The element to remove.</param>
        /// <param name="times">The maximum number of copies to remove.</param>
        /// <param name="remainingCopies">The value the contract promises as the return value.</param>
        /// <returns>The instance to use afterwards.</returns>
        TBag RemoveCopies(TBag bag, T item, int times, out int remainingCopies);

        /// <summary>
        /// Removes every copy of the element.
        /// </summary>
        /// <param name="bag">The current instance.</param>
        /// <param name="item">The element to remove.</param>
        /// <param name="removedAnything">The value the contract promises as the return value.</param>
        /// <returns>The instance to use afterwards.</returns>
        TBag RemoveAllCopies(TBag bag, T item, out bool removedAnything);

        /// <summary>
        /// Removes every element and copy.
        /// </summary>
        /// <param name="bag">The current instance.</param>
        /// <returns>The instance to use afterwards.</returns>
        TBag Clear(TBag bag);

        /// <summary>
        /// Returns the number of copies of the element.
        /// </summary>
        /// <param name="bag">The current instance.</param>
        /// <param name="item">The element to count.</param>
        /// <returns>The copy count, or zero when absent.</returns>
        int CountOf(TBag bag, T item);

        /// <summary>
        /// Returns the total number of copies, equivalent to <c>TotalCount</c>.
        /// </summary>
        /// <param name="bag">The current instance.</param>
        /// <returns>The number of copies.</returns>
        int TotalCount(TBag bag);

        /// <summary>
        /// Returns the number of distinct elements.
        /// </summary>
        /// <param name="bag">The current instance.</param>
        /// <returns>The number of distinct elements.</returns>
        int DistinctCount(TBag bag);

        /// <summary>
        /// Returns the value of the inherited <c>Count</c> property, which the multiset contract
        /// pins to the number of copies rather than the number of distinct elements.
        /// </summary>
        /// <param name="bag">The current instance.</param>
        /// <returns>The value the collection reports as its count.</returns>
        int ReportedCount(TBag bag);

        /// <summary>
        /// Determines whether at least one copy of the element is present.
        /// </summary>
        /// <param name="bag">The current instance.</param>
        /// <param name="item">The element to locate.</param>
        /// <returns><c>true</c> when present.</returns>
        bool Contains(TBag bag, T item);

        /// <summary>
        /// Enumerates each distinct element once.
        /// </summary>
        /// <param name="bag">The current instance.</param>
        /// <returns>The distinct elements.</returns>
        IEnumerable<T> DistinctItems(TBag bag);
    }
}
