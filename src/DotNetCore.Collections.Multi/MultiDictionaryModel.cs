using System.Collections.Generic;

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Plain data-transfer model for <see cref="MultiDictionary{TKey,TValue}"/>: the keys and, for
    /// each of them, its values, as two parallel lists.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type is what gives a multimap an <b>explicit</b> serialization entry point. It is an
    /// ordinary mutable POCO - public settable properties, no attributes, no interface
    /// implementations, no base class - so nothing here forces a serializer on a consumer. Whether
    /// the model is written as JSON (including <c>System.Text.Json</c>, in-box from .NET 6 and a
    /// separate package below it), as XML, as a database row or as anything else is the caller's
    /// choice, and this library takes a dependency on none of them.
    /// </para>
    /// <para>
    /// The shape is deliberately the same one
    /// <see cref="MultiDictionary{TKey,TValue}.ToDictionary()"/> produces - key to its values - but
    /// with two differences that make it a serialization model rather than a live view. First, the
    /// inner collections are independent copies, not views onto the map, so the snapshot does not
    /// change under a serializer's feet. Second, the model is a plain container with no dictionary
    /// behind it, so it imposes none of a dictionary's rules: a <c>null</c> value is an ordinary
    /// value anywhere in <see cref="Values"/>. A <c>null</c> <em>key</em> still can not be rebuilt,
    /// because the multimap itself rejects <c>null</c> keys - that is the target type's contract,
    /// not a limitation of the model.
    /// </para>
    /// <para>
    /// The model carries <b>data only</b>. A multimap's key comparer and its inner-collection
    /// strategy are configuration, not data - they are not serializable, and this model deliberately
    /// does not pretend otherwise - so both are supplied when the map is rebuilt, through
    /// <see cref="MultiDictionary{TKey,TValue}.FromModel"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var map = new MultiDictionary&lt;string, int&gt;();
    /// map.Add("orders", 1001);
    /// map.Add("orders", 1002);
    ///
    /// MultiDictionaryModel&lt;string, int&gt; model = map.ToSerializableModel();
    /// // model.Keys   == [ "orders" ]
    /// // model.Values == [ [ 1001, 1002 ] ]
    ///
    /// MultiDictionary&lt;string, int&gt; restored = MultiDictionary&lt;string, int&gt;.FromModel(model);
    /// </code>
    /// </example>
    public sealed class MultiDictionaryModel<TKey, TValue>
    {
        /// <summary>
        /// Gets or sets the keys. Parallel to <see cref="Values"/>: entry <c>i</c> of this list owns
        /// <see cref="Values"/>[i].
        /// </summary>
        public List<TKey> Keys { get; set; } = new List<TKey>();

        /// <summary>
        /// Gets or sets the values of each key, in the same order as <see cref="Keys"/>. Entry
        /// <c>i</c> holds the values stored under <see cref="Keys"/>[i]; it is never <c>null</c> for
        /// a model produced by <see cref="MultiDictionary{TKey,TValue}.ToSerializableModel"/>, and
        /// <see cref="MultiDictionary{TKey,TValue}.FromModel"/> rejects a <c>null</c> entry.
        /// </summary>
        public List<List<TValue>> Values { get; set; } = new List<List<TValue>>();
    }
}
