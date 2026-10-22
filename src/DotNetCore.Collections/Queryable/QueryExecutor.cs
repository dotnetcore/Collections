using System;
using System.Linq.Expressions;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The single entry point of the reference execution path: expression tree in, value out.
    /// </summary>
    /// <remarks>
    /// Translation and execution are deliberately separate. <see cref="QueryModelBuilder"/> is what
    /// decides whether a query belongs to the translatable subset, and it is the only place that
    /// fails; the composer then replays an already-valid model. A failure in the composer therefore
    /// means the subset accepted a shape the BCL cannot replay on this target, which is reported as
    /// a <see cref="NotSupportedException"/> carrying the model it happened on.
    /// </remarks>
    internal static class QueryExecutor
    {
        internal static object Execute(Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return Execute(QueryModelBuilder.Build(expression));
        }

        internal static object Execute(QueryModel model)
        {
            Func<object> query;

            try
            {
                query = QueryExpressionComposer.Compose(model);
            }
            catch (InvalidOperationException exception)
            {
                throw Unreplayable(model, exception);
            }
            catch (ArgumentException exception)
            {
                throw Unreplayable(model, exception);
            }

            return query();
        }

        private static NotSupportedException Unreplayable(QueryModel model, Exception inner) =>
            new NotSupportedException(
                "The query '" + model.Describe() + "' cannot be replayed: no operator in the "
                + "translatable subset of 7.4.0 matches its types. The translation itself succeeded, so "
                + "an adapter that renders this model is not affected.",
                inner);
    }
}
