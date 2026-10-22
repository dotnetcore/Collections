using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DotNetCore.Collections
{
    /// <summary>
    /// A queryable whose provider is the engine's own translator.
    /// </summary>
    /// <remarks>
    /// The root of a query holds the rows the reference executor replays over; a queryable produced
    /// by <see cref="CollectionQueryProvider.CreateQuery{TElement}(Expression)"/> holds only the
    /// expression and never needs them, because execution always starts at the root constant.
    /// </remarks>
    internal interface ICollectionQueryRoot
    {
        IEnumerable Rows { get; }
    }

    /// <summary>
    /// The <see cref="IQueryable{T}"/> the engine's query operators extend.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <remarks>
    /// Nothing here is public: a caller obtains one through
    /// <c>AsCollectionQueryable()</c> and then binds against <see cref="IQueryable{T}"/>, which is
    /// also what an ORM's adapter expects to receive.
    /// <para>
    /// <see cref="IOrderedQueryable{T}"/> is implemented even before any ordering stage: the BCL's
    /// <c>Queryable.OrderBy</c> and <c>Queryable.ThenBy</c> cast what the provider hands back to that
    /// interface, so a queryable that only implemented <see cref="IQueryable{T}"/> would throw on
    /// the first ordering call rather than on the first stage the translator rejects.
    /// </para>
    /// </remarks>
    internal sealed class CollectionQueryable<T> : IOrderedQueryable<T>, ICollectionQueryRoot
    {
        private readonly CollectionQueryProvider provider;
        private readonly Expression expression;
        private readonly IEnumerable<T>? rows;

        internal CollectionQueryable(IEnumerable<T> rows)
        {
            this.provider = new CollectionQueryProvider();
            this.expression = Expression.Constant(this);
            this.rows = rows;
        }

        internal CollectionQueryable(CollectionQueryProvider provider, Expression expression)
        {
            this.provider = provider;
            this.expression = expression;
        }

        IEnumerable ICollectionQueryRoot.Rows =>
            this.rows ?? throw new InvalidOperationException(
                "Only the root of a query carries rows; a chained queryable is resolved through its root.");

        public Type ElementType => typeof(T);

        public Expression Expression => this.expression;

        public IQueryProvider Provider => this.provider;

        public IEnumerator<T> GetEnumerator() =>
            ((IEnumerable<T>)QueryExecutor.Execute(this.expression)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }

    /// <summary>
    /// The provider that turns a query built on a <see cref="CollectionQueryable{T}"/> into a
    /// <see cref="QueryModel"/> and replays that model in memory.
    /// </summary>
    internal sealed class CollectionQueryProvider : IQueryProvider
    {
        public IQueryable CreateQuery(Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var elementType = QueryModelBuilder.ElementTypeOfSequence(expression.Type);
            var queryableType = typeof(CollectionQueryable<>).MakeGenericType(elementType);

            return (IQueryable)Activator.CreateInstance(
                queryableType,
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new object[] { this, expression },
                null)!;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return new CollectionQueryable<TElement>(this, expression);
        }

        public object Execute(Expression expression) => QueryExecutor.Execute(expression);

        public TResult Execute<TResult>(Expression expression) => (TResult)QueryExecutor.Execute(expression);
    }
}
