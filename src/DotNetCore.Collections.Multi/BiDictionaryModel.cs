using System.Collections.Generic;

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Plain data-transfer model for <see cref="BiDictionary{TLeft,TRight}"/>: the bindings of a
    /// one-to-one map, as two parallel lists.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a <b>separate</b> model from <see cref="MultiDictionaryModel{TKey,TValue}"/>, and
    /// deliberately so. That model describes a multimap - one key to <em>many</em> values - and its
    /// inner <c>List&lt;TValue&gt;</c> would misrepresent a bijection, where every entry holds
    /// exactly one counterpart. Squeezing a one-to-one map into a one-to-many shape would invite a
    /// reader to expect multiplicity the type can not express, and would force
    /// <see cref="BiDictionary{TLeft,TRight}.FromModel"/> to add a "the inner list must have exactly
    /// one element" rule that has no counterpart in the multimap's own rebuild. Two parallel lists
    /// say what the type actually is: binding <c>i</c> joins <see cref="Lefts"/>[i] to
    /// <see cref="Rights"/>[i].
    /// </para>
    /// <para>
    /// Like <see cref="MultiDictionaryModel{TKey,TValue}"/> this is an ordinary mutable POCO -
    /// public settable properties, no attributes, no interface implementations, no base class - so
    /// nothing here forces a serializer on a consumer. Whether the model is written as JSON, as XML,
    /// as a database row or as anything else is the caller's choice, and this library takes a
    /// dependency on none of them.
    /// </para>
    /// <para>
    /// The model carries <b>data only</b>. The two comparers are configuration, not data - they are
    /// not serializable, and this model deliberately does not pretend otherwise - so both are
    /// supplied when the map is rebuilt, through <see cref="BiDictionary{TLeft,TRight}.FromModel"/>.
    /// </para>
    /// <para>
    /// Unlike <see cref="BiDictionary{TLeft,TRight}.ToDictionary"/>, a binding whose left value is
    /// <c>null</c> <b>is</b> carried by this model: the left side is a <c>List</c>, not a dictionary
    /// key, so the restriction that forces the omission there does not apply here.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var users = new BiDictionary&lt;int, string&gt;();
    /// users.Add(1, "alice");
    /// users.Add(2, "bob");
    ///
    /// BiDictionaryModel&lt;int, string&gt; model = users.ToSerializableModel();
    /// // model.Lefts  == [ 1, 2 ]
    /// // model.Rights == [ "alice", "bob" ]
    ///
    /// BiDictionary&lt;int, string&gt; restored = BiDictionary&lt;int, string&gt;.FromModel(model);
    /// </code>
    /// </example>
    public sealed class BiDictionaryModel<TLeft, TRight>
    {
        /// <summary>
        /// Gets or sets the left side of every binding. Parallel to <see cref="Rights"/>: entry
        /// <c>i</c> of this list is bound to <see cref="Rights"/>[i].
        /// </summary>
        public List<TLeft> Lefts { get; set; } = new List<TLeft>();

        /// <summary>
        /// Gets or sets the right side of every binding, in the same order as
        /// <see cref="Lefts"/>. Entry <c>i</c> is the counterpart of <see cref="Lefts"/>[i].
        /// </summary>
        public List<TRight> Rights { get; set; } = new List<TRight>();
    }
}
