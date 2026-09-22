using System;
using System.Collections.Generic;

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Defines the contract of a <em>multiset</em> (bag): an unordered collection that allows
    /// duplicate elements and tracks the number of occurrences (copies) of each element.
    /// </summary>
    /// <typeparam name="T">The type of the elements. The type parameter is deliberately unconstrained so that multisets of nullable element types are supported.</typeparam>
    /// <remarks>
    /// <para>
    /// This is the read/write abstraction shared by the unordered and the ordered multiset
    /// implementations of this package (<see cref="MultiList{T}"/> and
    /// <see cref="OrderedMultiList{T}"/>). Code that only needs multiset semantics - and not the
    /// ordering, positional access or set algebra offered by the concrete types - can be written
    /// against this interface, which makes it possible to mock, replace or swap the implementation.
    /// </para>
    /// <para>
    /// <b>Copy semantics of <see cref="IReadOnlyCollection{T}.Count"/>.</b> This interface inherits
    /// <see cref="IReadOnlyCollection{T}"/>, whose <see cref="IReadOnlyCollection{T}.Count"/>
    /// property is the <em>number of copies</em> - not the number of distinct elements - and is
    /// therefore always equal to <see cref="TotalCount"/>. This is a deliberate, contract-level
    /// guarantee: every implementation of this interface must report the expanded (copy) count
    /// through the inherited member, so a multiset with three copies of one single element reports
    /// a count of three. Use <see cref="DistinctCount"/> when the number of distinct elements is
    /// what is wanted. (See also the "semantic differences" notes shipped with this package.)
    /// </para>
    /// <para>
    /// <b>Equality of elements.</b> Element equality is supplied by the implementation (typically an
    /// <see cref="IEqualityComparer{T}"/> passed at construction). This interface deliberately does
    /// not expose a comparer property, because the comparer type is not uniform across
    /// implementations: an unordered multiset is keyed by an <see cref="IEqualityComparer{T}"/>
    /// whereas an ordered multiset is ordered by an <see cref="IComparer{T}"/>.
    /// </para>
    /// <para>
    /// <b>Scope.</b> The member set is intentionally minimal: it contains only the semantics that
    /// every multiset implementation can honour. Set algebra (<c>UnionWith</c>,
    /// <c>IntersectionWith</c>, <c>ExceptWith</c>, <c>SymmetricExceptWith</c> and the
    /// <c>IsSubsetOf</c> family) and view/export helpers (<c>AsReadOnly</c>, <c>ToList</c>,
    /// <c>ToArray</c>, <c>Clone</c>) are <em>not</em> part of this contract, because they are not
    /// uniformly available across the multiset family (for example <see cref="PackedBag{T}"/> and
    /// the concurrent and immutable variants do not offer set algebra, and some of them are
    /// immutable and therefore return a new instance from what looks like a mutating call). Those
    /// members remain available on the concrete types.
    /// </para>
    /// <para>
    /// This interface does <em>not</em> extend <see cref="ICollection{T}"/>: the collection contract
    /// requires <c>bool Remove(T)</c>, whereas a multiset removes <em>one copy</em> and reports how
    /// many copies remain, which is <see cref="Remove(T)"/> below.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// void Dump(IMultiSet&lt;string&gt; bag)
    /// {
    ///     Console.WriteLine(bag.Count + " copies, " + bag.DistinctCount + " distinct");
    ///     foreach (var (item, count) in bag.EntrySet())
    ///     {
    ///         Console.WriteLine(item + " x " + count);
    ///     }
    /// }
    ///
    /// Dump(new MultiList&lt;string&gt;());
    /// Dump(new OrderedMultiList&lt;string&gt;());
    /// </code>
    /// </example>
    public interface IMultiSet<T> : IReadOnlyCollection<T>
    {
        /// <summary>
        /// Gets the total number of copies held by the multiset.
        /// </summary>
        /// <value>
        /// The sum of the copy counts of all distinct elements. This is the same value that the
        /// inherited <see cref="IReadOnlyCollection{T}.Count"/> reports.
        /// </value>
        int TotalCount { get; }

        /// <summary>
        /// Gets the number of distinct elements held by the multiset, regardless of how many copies
        /// of each element are present.
        /// </summary>
        /// <value>
        /// The number of distinct elements. An empty multiset reports zero; a multiset holding ten
        /// copies of a single element reports one.
        /// </value>
        int DistinctCount { get; }

        /// <summary>
        /// Adds a single copy of the element.
        /// </summary>
        /// <param name="item">The element to add. <c>null</c> is a valid element for reference types.</param>
        /// <remarks>
        /// Adding an element that is already present increases its copy count by one and leaves
        /// <see cref="DistinctCount"/> unchanged.
        /// </remarks>
        void Add(T item);

        /// <summary>
        /// Adds the specified number of copies of the element.
        /// </summary>
        /// <param name="item">The element to add. <c>null</c> is a valid element for reference types.</param>
        /// <param name="times">The number of copies to add. Must be positive.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="times"/> is zero or negative.</exception>
        void Add(T item, int times);

        /// <summary>
        /// Adds one copy of each element of the specified collection.
        /// </summary>
        /// <param name="items">The elements to add.</param>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is <c>null</c>.</exception>
        /// <remarks>
        /// Duplicates inside <paramref name="items"/> are honoured: each occurrence contributes one
        /// copy. The order in which the elements are supplied does not affect the resulting
        /// multiset.
        /// </remarks>
        void AddRange(IEnumerable<T> items);

        /// <summary>
        /// Determines whether the multiset contains at least one copy of the element.
        /// </summary>
        /// <param name="item">The element to locate.</param>
        /// <returns><c>true</c> when at least one copy is present; otherwise <c>false</c>.</returns>
        bool Contains(T item);

        /// <summary>
        /// Determines whether the multiset contains at least one copy of every element of the
        /// specified collection.
        /// </summary>
        /// <param name="items">The elements to locate.</param>
        /// <returns>
        /// <c>true</c> when every element occurs at least once; <c>true</c> for an empty sequence;
        /// otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is <c>null</c>.</exception>
        bool ContainsAll(IEnumerable<T> items);

        /// <summary>
        /// Returns the number of copies of the element currently held by the multiset.
        /// </summary>
        /// <param name="item">The element to count.</param>
        /// <returns>The copy count, or zero when the element is absent.</returns>
        int CountOf(T item);

        /// <summary>
        /// Removes a single copy of the element.
        /// </summary>
        /// <param name="item">The element to remove.</param>
        /// <returns>
        /// The number of copies remaining after the removal, or zero when the element was absent
        /// (in which case no copy was removed).
        /// </returns>
        int Remove(T item);

        /// <summary>
        /// Removes up to the specified number of copies of the element.
        /// </summary>
        /// <param name="item">The element to remove.</param>
        /// <param name="times">The maximum number of copies to remove. Must be positive.</param>
        /// <returns>
        /// The number of copies remaining after the removal, or zero when the element was absent.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="times"/> is zero or negative.</exception>
        /// <remarks>
        /// When fewer copies are present than requested, every remaining copy is removed. The element
        /// is dropped from the multiset once its copy count reaches zero.
        /// </remarks>
        int Remove(T item, int times);

        /// <summary>
        /// Removes every copy of the element.
        /// </summary>
        /// <param name="item">The element to remove.</param>
        /// <returns>
        /// <c>true</c> when at least one copy was removed; <c>false</c> when the element was absent.
        /// </returns>
        bool RemoveAllCopies(T item);

        /// <summary>
        /// Removes all elements and copies.
        /// </summary>
        void Clear();

        /// <summary>
        /// Enumerates each distinct element exactly once, in an unspecified order.
        /// </summary>
        /// <returns>A sequence containing each distinct element once.</returns>
        IEnumerable<T> DistinctItems();

        /// <summary>
        /// Enumerates each distinct element together with its copy count.
        /// </summary>
        /// <returns>A sequence of <c>(Item, Count)</c> entries, one per distinct element.</returns>
        /// <remarks>
        /// This is the projection that makes the copy semantics explicit: the sum of the
        /// <c>Count</c> components equals <see cref="TotalCount"/> and the number of entries equals
        /// <see cref="DistinctCount"/>. The enumeration order is unspecified.
        /// </remarks>
        IEnumerable<(T Item, int Count)> EntrySet();
    }
}
