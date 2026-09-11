using System.Collections.Generic;

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Plain data-transfer model for <see cref="MultiList{T}"/>: the distinct elements and how many
    /// copies of each the multiset holds, as two parallel lists.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type is what gives a multiset an <b>explicit</b> serialization entry point. It is an
    /// ordinary mutable POCO - public settable properties, no attributes, no interface
    /// implementations, no base class - so nothing here forces a serializer on a consumer. Whether
    /// the model is written as JSON (including <c>System.Text.Json</c>, in-box from .NET 6 and a
    /// separate package below it), as XML, as a database row or as anything else is the caller's
    /// choice, and this library takes a dependency on none of them.
    /// </para>
    /// <para>
    /// <see cref="MultiList{T}.ToSerializableModel"/> snapshots the multiset and
    /// <see cref="MultiList{T}.FromModel"/> rebuilds one. Unlike
    /// <see cref="MultiList{T}.ToDictionary()"/>, which has to throw when the multiset holds a
    /// <c>null</c> element (a <c>null</c> can not be a dictionary key), the model represents
    /// <c>null</c> like any other element: a <c>null</c> in <see cref="Items"/> paired with its
    /// count in <see cref="Counts"/>. That difference is the reason a dedicated model exists rather
    /// than <c>ToDictionary()</c> being reused.
    /// </para>
    /// <para>
    /// The model carries <b>data only</b>. A multiset's comparer is configuration, not data - it is
    /// not serializable, and this model deliberately does not pretend otherwise - so the comparer is
    /// supplied when the multiset is rebuilt, through <see cref="MultiList{T}.FromModel"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var bag = new MultiList&lt;string&gt; { "apple", "apple", "banana" };
    ///
    /// MultiListModel&lt;string&gt; model = bag.ToSerializableModel();
    /// // model.Items  == [ "apple", "banana" ]
    /// // model.Counts == [ 2, 1 ]
    ///
    /// MultiList&lt;string&gt; restored = MultiList&lt;string&gt;.FromModel(model);
    /// </code>
    /// </example>
    public sealed class MultiListModel<T>
    {
        /// <summary>
        /// Gets or sets the distinct elements. Parallel to <see cref="Counts"/>: entry <c>i</c> of
        /// this list holds <see cref="Counts"/>[i] copies. A <c>null</c> entry is a stored
        /// <c>null</c> element.
        /// </summary>
        public List<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// Gets or sets the number of copies of each entry of <see cref="Items"/>. Every count must
        /// be positive; a zero or negative count is a malformed model and
        /// <see cref="MultiList{T}.FromModel"/> rejects it.
        /// </summary>
        public List<int> Counts { get; set; } = new List<int>();
    }
}
