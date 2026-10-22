using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The queryable face of the engine: entering a query, reading its translation, and the keyed
    /// operators the BCL does not own.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These members extend <see cref="IQueryable{T}"/>, which is a BCL receiver - a deliberate
    /// exception to the rule that the engine only extends its own type family, and one the namespace
    /// governance test now records. It is safe because a queryable receiver is an identity
    /// conversion while every BCL candidate needs an interface conversion, so the engine's candidate
    /// is strictly better and CS0121 never arises. That argument is not theory: it is measured on
    /// net451 / net8.0 / net9.0 / net10.0, on the three names the BCL acquired later
    /// (<c>DistinctBy</c> in .NET 6, <c>Order</c> in .NET 7, <c>CountBy</c> in .NET 9).
    /// </para>
    /// <para>
    /// The operators are additive to the value-enumerable face, not a replacement: a caller that
    /// wants the zero-allocation path calls <c>ToValueEnumerable()</c>, and a caller that wants the
    /// query translated calls <c>AsCollectionQueryable()</c> (or already holds an ORM's queryable).
    /// </para>
    /// </remarks>
    public static partial class CollectionExtensions
    {
        private static readonly MethodInfo DistinctByQueryableOperator = QueryableOperator("DistinctBy");
        private static readonly MethodInfo OrderQueryableOperator = QueryableOperator("Order");
        private static readonly MethodInfo OrderDescendingQueryableOperator = QueryableOperator("OrderDescending");
        private static readonly MethodInfo CountByQueryableOperator = QueryableOperator("CountBy");

        /// <summary>Enters the query layer from any queryable.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The queryable to continue from.</param>
        /// <returns>A queryable whose provider is the engine's translator.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// An ORM queryable is accepted: the engine does not enumerate it at this point, it only
        /// records it as the row source so that a translation can be rendered or replayed.
        /// </remarks>
        public static IQueryable<T> AsCollectionQueryable<T>(this IQueryable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source as CollectionQueryable<T> ?? new CollectionQueryable<T>(source);
        }

        /// <summary>Enters the query layer from a plain sequence.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence to query.</param>
        /// <returns>A queryable whose provider is the engine's translator.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static IQueryable<T> AsCollectionQueryable<T>(this IEnumerable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new CollectionQueryable<T>(source);
        }

        /// <summary>Translates a query into the provider-agnostic model an adapter renders.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The query to translate.</param>
        /// <returns>The translated model.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">The query uses an operator outside the
        /// translatable subset of 7.4.0.</exception>
        public static QueryModel ToQueryModel<T>(this IQueryable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return QueryModelBuilder.Build(source.Expression);
        }

        /// <summary>Keeps the first element of each key, translating instead of scanning.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="source">The query to continue.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The queryable of the deduplicated elements.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// Only the comparerless form is translated: a key comparer has no representation in a query
        /// language, so it stays on the value-enumerable face.
        /// </remarks>
        public static IQueryable<T> DistinctBy<T, TKey>(
            this IQueryable<T> source, Expression<Func<T, TKey>> keySelector)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            return source.Provider.CreateQuery<T>(
                Expression.Call(
                    null,
                    DistinctByQueryableOperator.MakeGenericMethod(typeof(T), typeof(TKey)),
                    source.Expression,
                    Expression.Quote(keySelector)));
        }

        /// <summary>Orders the query ascending by its elements.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The query to continue.</param>
        /// <returns>The ordered queryable.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static IQueryable<T> Order<T>(this IQueryable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.Provider.CreateQuery<T>(
                Expression.Call(null, OrderQueryableOperator.MakeGenericMethod(typeof(T)), source.Expression));
        }

        /// <summary>Orders the query descending by its elements.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The query to continue.</param>
        /// <returns>The ordered queryable.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static IQueryable<T> OrderDescending<T>(this IQueryable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.Provider.CreateQuery<T>(
                Expression.Call(null, OrderDescendingQueryableOperator.MakeGenericMethod(typeof(T)), source.Expression));
        }

        /// <summary>Counts how often each key occurs, translating instead of scanning.</summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="source">The query to continue.</param>
        /// <param name="keySelector">The projection of an element to its key.</param>
        /// <returns>The queryable of keys with their counts.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or
        /// <paramref name="keySelector"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// Only the comparerless form is translated, for the same reason as <c>DistinctBy</c>.
        /// </remarks>
        public static IQueryable<KeyValuePair<TKey, int>> CountBy<T, TKey>(
            this IQueryable<T> source, Expression<Func<T, TKey>> keySelector)
            where TKey : notnull
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (keySelector == null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            return source.Provider.CreateQuery<KeyValuePair<TKey, int>>(
                Expression.Call(
                    null,
                    CountByQueryableOperator.MakeGenericMethod(typeof(T), typeof(TKey)),
                    source.Expression,
                    Expression.Quote(keySelector)));
        }

        private static MethodInfo QueryableOperator(string name)
        {
            foreach (var method in typeof(CollectionExtensions).GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name != name)
                {
                    continue;
                }

                var receiver = method.GetParameters()[0].ParameterType;
                if (receiver.IsGenericType && receiver.GetGenericTypeDefinition() == typeof(IQueryable<>))
                {
                    return method;
                }
            }

            throw new InvalidOperationException("The queryable operator '" + name + "' is missing.");
        }
    }
}
