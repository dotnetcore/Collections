namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Describes what a <see cref="CollectionChangedEventArgs{TKey}"/> reports about a change to a
    /// collection: copies were added, copies were removed, or the whole collection was reset.
    /// </summary>
    /// <remarks>
    /// This is deliberately a three-value enumeration rather than a copy of the framework's
    /// <c>NotifyCollectionChangedAction</c>. The framework's action set is index-oriented
    /// (<c>Add</c> / <c>Remove</c> / <c>Move</c> / <c>Replace</c> / <c>Reset</c>), because it is built
    /// for ordered, position-addressed collections; a multiset has no positions, and a single call
    /// can change an element's copy count by more than one, which no index-shaped action can express.
    /// </remarks>
    public enum CollectionChangeType
    {
        /// <summary>
        /// Copies were added to the collection: a new element, or more copies of an element already
        /// present.
        /// </summary>
        Add,

        /// <summary>
        /// Copies were removed from the collection: an element's copy count fell (possibly to zero,
        /// which drops the element).
        /// </summary>
        Remove,

        /// <summary>
        /// The whole collection was cleared. No single element is named; the collection is empty
        /// afterwards.
        /// </summary>
        Reset,
    }
}
