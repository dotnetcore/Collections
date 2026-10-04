using System;

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Carries the details of a single change to a collection that raises
    /// <c>CollectionChanged</c>: what kind of change it was, which element or key it touched, how
    /// many copies (or values) it added or removed, and the resulting count.
    /// </summary>
    /// <typeparam name="TKey">
    /// The element type for <see cref="MultiList{T}"/> (a multiset is a map from elements to copy
    /// counts, so its elements play the key role here) or the key type for
    /// <see cref="MultiDictionary{TKey,TValue}"/>.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// The shape is intentionally the same for both families: a multiset change is "element
    /// <see cref="Key"/> gained or lost <see cref="Count"/> copies and now holds
    /// <see cref="NewCount"/>", and a multimap change is "key <see cref="Key"/> gained or lost
    /// <see cref="Count"/> values and now holds <see cref="NewCount"/>". Reporting the <em>count</em>
    /// rather than a position is the whole point: a multiset has no positions, and one call can move
    /// an element by more than one copy, which an index-shaped notification could not express.
    /// </para>
    /// <para>
    /// The multimap reports the affected <em>key</em> and how many of its values changed, not the
    /// individual values: a consumer that needs the new contents reads them back through
    /// <see cref="MultiDictionary{TKey,TValue}.this[TKey]"/>. That keeps the payload a fixed,
    /// allocation-light shape and keeps the notification a change <em>signal</em> rather than a
    /// second copy of the data.
    /// </para>
    /// <para>
    /// This type is immutable and therefore safe to share between threads once built.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var bag = new MultiList&lt;string&gt;();
    /// bag.CollectionChanged += (sender, e) =&gt;
    ///     Console.WriteLine($"{e.ChangeType} {e.Key} x{e.Count} -&gt; {e.NewCount}");
    ///
    /// bag.Add("apple", 3);   // Add apple x3 -> 3
    /// bag.Remove("apple");   // Remove apple x1 -> 2
    /// </code>
    /// </example>
    public sealed class CollectionChangedEventArgs<TKey> : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionChangedEventArgs{TKey}"/> class.
        /// </summary>
        /// <param name="changeType">the kind of change that occurred.</param>
        /// <param name="key">
        /// the element (for a multiset) or key (for a multimap) whose count changed; ignored when
        /// <paramref name="changeType"/> is <see cref="CollectionChangeType.Reset"/>.
        /// </param>
        /// <param name="count">
        /// the number of copies (for a multiset) or values (for a multimap) added or removed; always
        /// <c>0</c> for <see cref="CollectionChangeType.Reset"/>.
        /// </param>
        /// <param name="newCount">
        /// the element's copy count (for a multiset) or the key's value count (for a multimap) after
        /// the change; always <c>0</c> for <see cref="CollectionChangeType.Reset"/>.
        /// </param>
        public CollectionChangedEventArgs(CollectionChangeType changeType, TKey key, int count, int newCount)
        {
            ChangeType = changeType;
            Key = key;
            Count = count;
            NewCount = newCount;
        }

        /// <summary>
        /// Gets the kind of change that occurred.
        /// </summary>
        public CollectionChangeType ChangeType { get; }

        /// <summary>
        /// Gets the element (for a multiset) or key (for a multimap) whose count changed. For
        /// <see cref="CollectionChangeType.Reset"/> this is <c>default(TKey)</c> and carries no
        /// meaning, because the whole collection was cleared rather than a single entry changed.
        /// </summary>
        public TKey Key { get; }

        /// <summary>
        /// Gets the number of copies (for a multiset) or values (for a multimap) added or removed.
        /// Always <c>0</c> for <see cref="CollectionChangeType.Reset"/>.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Gets the element's copy count (for a multiset) or the key's value count (for a multimap)
        /// after the change. Zero means the entry is gone. Always <c>0</c> for
        /// <see cref="CollectionChangeType.Reset"/>.
        /// </summary>
        public int NewCount { get; }

        /// <summary>
        /// Returns a one-line summary of the change.
        /// </summary>
        /// <example>
        /// <code>
        /// e.ToString();   // "Add apple x3 -> 3"
        /// </code>
        /// </example>
        public override string ToString()
        {
            return ChangeType == CollectionChangeType.Reset
                ? "Reset"
                : string.Format("{0} {1} x{2} -> {3}", ChangeType, Key, Count, NewCount);
        }
    }
}
