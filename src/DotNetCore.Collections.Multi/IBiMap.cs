using System;
using System.Collections.Generic;

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Defines the contract of a strict one-to-one (<em>bijective</em>) map: every
    /// <typeparamref name="TLeft"/> is bound to exactly one <typeparamref name="TRight"/>, and no
    /// <typeparamref name="TRight"/> is shared by two <typeparamref name="TLeft"/> values.
    /// </summary>
    /// <typeparam name="TLeft">The type of the left (forward key) values. The type parameter is deliberately unconstrained; <c>null</c> is supported through a dedicated bucket.</typeparam>
    /// <typeparam name="TRight">The type of the right (forward value / reverse key) values. The type parameter is deliberately unconstrained; <c>null</c> is supported through a dedicated bucket.</typeparam>
    /// <remarks>
    /// <para>
    /// This is the abstraction of the bidirectional dictionary of this package
    /// (<see cref="BiDictionary{TLeft,TRight}"/>). Both directions are O(1) lookups, so code that
    /// needs to resolve <c>left &#8594; right</c> and <c>right &#8594; left</c> without a scan can be
    /// written against this interface, which makes it possible to mock, replace or swap the
    /// implementation.
    /// </para>
    /// <para>
    /// <b>The inherited dictionary view.</b> This interface extends
    /// <see cref="IReadOnlyDictionary{TKey,TValue}"/> instantiated as
    /// <c>IReadOnlyDictionary&lt;TLeft, TRight&gt;</c>, which supplies the whole forward read
    /// surface: the inherited <c>Count</c> (the number of bindings),
    /// <see cref="IReadOnlyDictionary{TKey,TValue}.Keys"/>, the indexer
    /// <c>this[left]</c> (throws <see cref="KeyNotFoundException"/> for an unbound left value),
    /// <see cref="IReadOnlyDictionary{TKey,TValue}.Values"/>,
    /// <see cref="IReadOnlyDictionary{TKey,TValue}.ContainsKey"/> and
    /// <see cref="IReadOnlyDictionary{TKey,TValue}.TryGetValue"/>. The reverse direction is what
    /// the members below add.
    /// </para>
    /// <para>
    /// <b>Conflict handling is strict.</b> <see cref="Add"/> throws when either side is already in
    /// use, and there is deliberately no silently-overwriting setter: overwriting a right value
    /// would silently unbind the left value it used to belong to, which is an entry the caller
    /// never mentioned. Breaking an existing binding is therefore always an explicit
    /// <see cref="Remove"/> or <see cref="RemoveRight"/> first. Callers that prefer to test rather
    /// than catch use <see cref="TryAdd"/>, which reports the same conditions without throwing.
    /// </para>
    /// <para>
    /// <b>Scope.</b> The member set is intentionally minimal: it contains only the semantics that
    /// the bijective map can honour. Comparer properties are excluded, because exposing the
    /// underlying comparers is an implementation detail rather than a contract, and the view/export
    /// helpers (<c>AsReverse</c>, <c>Clone</c>, <c>ToDictionary</c>) are excluded as view-creation
    /// and materialisation conveniences; those members remain available on
    /// <see cref="BiDictionary{TLeft,TRight}"/>. Note that the reverse direction does not need
    /// <c>AsReverse</c> to be usable here: <see cref="ContainsRight"/>, <see cref="TryGetLeft"/> and
    /// <see cref="GetLeft"/> cover it directly.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// void Resolve(IBiMap&lt;int, string&gt; users)
    /// {
    ///     string name = users[1];              // forward
    ///     int id = users.GetLeft("bob");       // reverse
    ///
    ///     users.TryAdd(3, "bob");              // false - "bob" is already bound
    ///     users.Remove(2);                     // frees "bob" explicitly
    ///     users.TryAdd(3, "bob");              // true
    /// }
    ///
    /// Resolve(new BiDictionary&lt;int, string&gt;());
    /// </code>
    /// </example>
    public interface IBiMap<TLeft, TRight> : IReadOnlyDictionary<TLeft, TRight>
    {
        /// <summary>
        /// Determines whether a binding exists for the right value, i.e. whether the right value is
        /// currently bound to any left value.
        /// </summary>
        /// <param name="right">The right value to locate.</param>
        /// <returns><c>true</c> when the right value is bound; otherwise <c>false</c>.</returns>
        bool ContainsRight(TRight right);

        /// <summary>
        /// Gets the left value bound to the right value.
        /// </summary>
        /// <param name="right">The right value to resolve.</param>
        /// <param name="left">When this method returns <c>true</c>, the left value bound to <paramref name="right"/>; otherwise the default value of <typeparamref name="TLeft"/>.</param>
        /// <returns><c>true</c> when a binding exists; otherwise <c>false</c>.</returns>
        bool TryGetLeft(TRight right, out TLeft left);

        /// <summary>
        /// Gets the left value bound to the right value.
        /// </summary>
        /// <param name="right">The right value to resolve.</param>
        /// <returns>The left value bound to <paramref name="right"/>.</returns>
        /// <exception cref="KeyNotFoundException"><paramref name="right"/> is not bound to any left value.</exception>
        TLeft GetLeft(TRight right);

        /// <summary>
        /// Adds a binding from the left value to the right value.
        /// </summary>
        /// <param name="left">The left value to bind.</param>
        /// <param name="right">The right value to bind.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="left"/> already has a binding, or <paramref name="right"/> is already
        /// bound to a different left value.
        /// </exception>
        /// <remarks>
        /// Both sides are checked, so neither index is left half-written when the call throws.
        /// </remarks>
        void Add(TLeft left, TRight right);

        /// <summary>
        /// Adds the binding when neither side is already in use.
        /// </summary>
        /// <param name="left">The left value to bind.</param>
        /// <param name="right">The right value to bind.</param>
        /// <returns>
        /// <c>true</c> when the binding was added; <c>false</c> when <paramref name="left"/> already
        /// has a binding or <paramref name="right"/> is already bound to a different left value (in
        /// which case nothing is changed and nothing is thrown).
        /// </returns>
        bool TryAdd(TLeft left, TRight right);

        /// <summary>
        /// Removes the binding of the left value, which also frees its right value for a new
        /// binding.
        /// </summary>
        /// <param name="left">The left value whose binding is removed.</param>
        /// <returns><c>true</c> when a binding was removed; otherwise <c>false</c>.</returns>
        bool Remove(TLeft left);

        /// <summary>
        /// Removes the binding of the right value, which also frees its left value for a new
        /// binding.
        /// </summary>
        /// <param name="right">The right value whose binding is removed.</param>
        /// <returns><c>true</c> when a binding was removed; otherwise <c>false</c>.</returns>
        bool RemoveRight(TRight right);

        /// <summary>
        /// Removes every binding.
        /// </summary>
        void Clear();
    }
}
