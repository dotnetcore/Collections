using System;
using System.Collections.Generic;

// CS8714: TKey is deliberately unconstrained, matching the multimap types these helpers build
// (a null key *value* is rejected at runtime by MultiDictionary.Add, but the type parameter must
// stay nullable-friendly, e.g. TKey = string?); the "notnull" key constraint of the annotated
// Dictionary<TKey, TValue> (net5.0+ reference assemblies) is a false positive here.
#pragma warning disable CS8714

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Extension entry points that build a multimap from an ordinary source (F6-45): group a
    /// sequence by a key, index it uniquely, or adapt an existing read-only dictionary.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The multimap types of this package are built by adding entries one at a time. These helpers
    /// are the "from a sequence" and "from a dictionary" entry points that were missing, so callers
    /// do not have to hand-roll the loop - and, for the unique case, the duplicate-key check.
    /// </para>
    /// <para>
    /// Every helper returns an <b>independent copy</b>, never a live view: the result is an ordinary
    /// <see cref="MultiDictionary{TKey,TValue}"/> or <see cref="BiDictionary{TLeft,TRight}"/> that
    /// later changes to the source do not affect, and that the caller may mutate freely. A zero-copy
    /// view is deliberately not offered - the multimap types are concrete classes rather than
    /// interface implementations, so a view could only be a thin read-only facade that would drop
    /// the per-key set algebra that is their point (the same reasoning that makes
    /// <c>TwoKeyDictionary.AsReverse</c> a copy rather than a view).
    /// </para>
    /// <para>
    /// All three helpers read the source once, in order, and preserve the source order of the values
    /// within each key.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// string[] words = { "ant", "ape", "bee" };
    ///
    /// MultiDictionary&lt;char, string&gt; byFirstLetter = words.IndexBy(w =&gt; w[0]);
    /// byFirstLetter['a'].Count;   // 2 - "ant" and "ape"
    ///
    /// BiDictionary&lt;string, string&gt; byWord = words.UniqueIndexBy(w =&gt; w);
    /// </code>
    /// </example>
    public static class MultiDictionaryExtensions
    {
        /// <summary>
        /// Groups the elements of a sequence into a <see cref="MultiDictionary{TKey,TValue}"/> keyed
        /// by <paramref name="keySelector"/>, keeping duplicate elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the source elements.</typeparam>
        /// <typeparam name="TKey">The type of the grouping key.</typeparam>
        /// <param name="source">The sequence to group.</param>
        /// <param name="keySelector">Selects the key of each element. Must not return <c>null</c>.</param>
        /// <returns>
        /// A new <see cref="MultiDictionary{TKey,TSource}"/> holding one entry per source element,
        /// with duplicate elements under the same key kept (list semantics, not set semantics).
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> is <c>null</c>, or
        /// <paramref name="keySelector"/> returns <c>null</c> for some element.
        /// </exception>
        /// <example>
        /// <code>
        /// var byCountry = users.IndexBy(u =&gt; u.Country);
        /// byCountry["UK"].Count;   // the number of UK users
        /// </code>
        /// </example>
        public static MultiDictionary<TKey, TSource> IndexBy<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector)
        {
            return IndexBy(source, keySelector, null);
        }

        /// <summary>
        /// Groups the elements of a sequence into a <see cref="MultiDictionary{TKey,TValue}"/> keyed
        /// by <paramref name="keySelector"/>, keeping duplicate elements and comparing keys with
        /// <paramref name="keyComparer"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source elements.</typeparam>
        /// <typeparam name="TKey">The type of the grouping key.</typeparam>
        /// <param name="source">The sequence to group.</param>
        /// <param name="keySelector">Selects the key of each element. Must not return <c>null</c>.</param>
        /// <param name="keyComparer">
        /// The comparer used for keys, or <c>null</c> to use
        /// <see cref="EqualityComparer{TKey}.Default"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="MultiDictionary{TKey,TSource}"/> holding one entry per source element,
        /// with duplicate elements under the same key kept (list semantics, not set semantics).
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> is <c>null</c>, or
        /// <paramref name="keySelector"/> returns <c>null</c> for some element.
        /// </exception>
        /// <example>
        /// <code>
        /// var byHeader = lines.IndexBy(
        ///     l =&gt; l.Substring(0, l.IndexOf(':')),
        ///     StringComparer.OrdinalIgnoreCase);
        /// </code>
        /// </example>
        public static MultiDictionary<TKey, TSource> IndexBy<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey>? keyComparer)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            var result = new MultiDictionary<TKey, TSource>(keyComparer);
            foreach (var item in source)
            {
                result.Add(keySelector(item), item);
            }

            return result;
        }

        /// <summary>
        /// Indexes the elements of a sequence by a key that must be unique, into a
        /// <see cref="BiDictionary{TLeft,TRight}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source elements.</typeparam>
        /// <typeparam name="TKey">The type of the index key.</typeparam>
        /// <param name="source">The sequence to index.</param>
        /// <param name="keySelector">Selects the key of each element. Must not return <c>null</c>.</param>
        /// <returns>
        /// A new <see cref="BiDictionary{TKey,TSource}"/> mapping each key to its element.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> is <c>null</c>, or
        /// <paramref name="keySelector"/> returns <c>null</c> for some element.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="keySelector"/> returns the same key for two elements. Because
        /// <see cref="BiDictionary{TLeft,TRight}"/> is strictly one-to-one, the exception is also
        /// raised when two elements are themselves equal - unlike a plain map index, which would
        /// accept a repeated value.
        /// </exception>
        /// <example>
        /// <code>
        /// var byName = users.UniqueIndexBy(u =&gt; u.Name);
        /// byName["alice"];               // the user named alice
        /// byName.AsReverse()[user];      // "alice" - the inverse direction
        /// </code>
        /// </example>
        public static BiDictionary<TKey, TSource> UniqueIndexBy<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector)
        {
            return UniqueIndexBy(source, keySelector, null);
        }

        /// <summary>
        /// Indexes the elements of a sequence by a key that must be unique, into a
        /// <see cref="BiDictionary{TLeft,TRight}"/>, comparing keys with
        /// <paramref name="keyComparer"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the source elements.</typeparam>
        /// <typeparam name="TKey">The type of the index key.</typeparam>
        /// <param name="source">The sequence to index.</param>
        /// <param name="keySelector">Selects the key of each element. Must not return <c>null</c>.</param>
        /// <param name="keyComparer">
        /// The comparer used for keys, or <c>null</c> to use
        /// <see cref="EqualityComparer{TKey}.Default"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="BiDictionary{TKey,TSource}"/> mapping each key to its element.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> or <paramref name="keySelector"/> is <c>null</c>, or
        /// <paramref name="keySelector"/> returns <c>null</c> for some element.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="keySelector"/> returns the same key for two elements. Because
        /// <see cref="BiDictionary{TLeft,TRight}"/> is strictly one-to-one, the exception is also
        /// raised when two elements are themselves equal - unlike a plain map index, which would
        /// accept a repeated value.
        /// </exception>
        /// <example>
        /// <code>
        /// var byCode = currencies.UniqueIndexBy(c =&gt; c.Code, StringComparer.OrdinalIgnoreCase);
        /// </code>
        /// </example>
        public static BiDictionary<TKey, TSource> UniqueIndexBy<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey>? keyComparer)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            var result = new BiDictionary<TKey, TSource>(keyComparer, null);
            foreach (var item in source)
            {
                var key = keySelector(item);
                if (result.ContainsKey(key))
                {
                    throw new ArgumentException(
                        "The source sequence is not uniquely indexed: the key '" + key +
                        "' is produced for more than one element.",
                        nameof(source));
                }

                result.Add(key, item);
            }

            return result;
        }

        /// <summary>
        /// Adapts an existing read-only dictionary into a <see cref="MultiDictionary{TKey,TValue}"/>
        /// whose every key owns exactly one value.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <typeparam name="TValue">The type of the values.</typeparam>
        /// <param name="map">The dictionary to adapt.</param>
        /// <returns>
        /// A new <see cref="MultiDictionary{TKey,TValue}"/> holding one entry per source pair. It is
        /// an independent <b>copy</b>, not a view: later changes to <paramref name="map"/> are not
        /// observed, and the result may be mutated freely.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
        /// <remarks>
        /// The key comparer is carried over when <paramref name="map"/> is a
        /// <see cref="Dictionary{TKey,TValue}"/> (whose <c>Comparer</c> is readable); for any other
        /// implementation <see cref="EqualityComparer{TKey}.Default"/> is used, because
        /// <see cref="IReadOnlyDictionary{TKey,TValue}"/> does not expose a comparer.
        /// </remarks>
        /// <example>
        /// <code>
        /// IReadOnlyDictionary&lt;string, int&gt; stock = LoadStock();
        ///
        /// MultiDictionary&lt;string, int&gt; map = stock.AsMultiDictionary();
        /// map.Add("widget", 7);    // adds a second value under an existing key
        /// stock["widget"];          // unchanged - map is a copy
        /// </code>
        /// </example>
        public static MultiDictionary<TKey, TValue> AsMultiDictionary<TKey, TValue>(
            this IReadOnlyDictionary<TKey, TValue> map)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            var comparer = map is Dictionary<TKey, TValue> dictionary ? dictionary.Comparer : null;
            var result = new MultiDictionary<TKey, TValue>(comparer);
            foreach (var pair in map)
            {
                result.Add(pair.Key, pair.Value);
            }

            return result;
        }
    }
}
