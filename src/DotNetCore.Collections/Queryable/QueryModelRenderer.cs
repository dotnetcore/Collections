using System;
using System.Linq;
using System.Linq.Expressions;

namespace DotNetCore.Collections
{
    /// <summary>
    /// Replays a <see cref="QueryModel"/> onto a query provider as a
    /// <see cref="IQueryable"/> expression tree.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the twin of <see cref="QueryExpressionComposer"/>: the same model, rebuilt against
    /// <see cref="Queryable"/> instead of <see cref="Enumerable"/>. Where the composer produces a
    /// delegate that runs in memory, this produces an expression tree that a provider can translate
    /// into its own query language, which is what an adapter integration (F7-13) needs.
    /// </para>
    /// <para>
    /// The two paths are intentionally separate rather than one path with a switch, because the two
    /// BCL entry points differ in a way that cannot be hidden: <see cref="Enumerable"/> takes a
    /// delegate, <see cref="Queryable"/> takes a quoted lambda. Sharing the assembly step would mean
    /// emitting a delegate for the provider path or a quoted lambda for the in-memory path, and both
    /// are wrong.
    /// </para>
    /// <para>
    /// Only the operators a provider can express are replayed. The three operators the engine owns -
    /// <c>DistinctBy</c>, <c>CountBy</c> and the <c>Join</c> whose inner sequence is already
    /// materialised - are refused with a <see cref="NotSupportedException"/> that names the operator,
    /// following the R7-03 ruling: no approximated translation is produced in place of a real one.
    /// </para>
    /// </remarks>
    internal static class QueryModelRenderer
    {
        internal static IQueryable Render(QueryModel model, IQueryable source)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (model.Terminal != null)
            {
                throw new NotSupportedException(
                    "The query ends in the aggregate '" + model.Terminal.OperatorName + "', which a "
                    + "provider applies through its own aggregate API (Queryable.Count, Sum, Min, or "
                    + "Max) rather than as part of a row query. Render the stages and apply the "
                    + "terminal through the provider so that the aggregate is translated instead of "
                    + "the whole result being pulled into memory.");
            }

            if (source.ElementType != model.SourceType)
            {
                throw new ArgumentException(
                    "The query starts from '" + model.SourceType.FullName + "' but the source it is "
                    + "being rendered onto has the element type '" + source.ElementType.FullName
                    + "'. A model is tied to the row set it was translated from.",
                    nameof(source));
            }

            var expression = source.Expression;

            foreach (var stage in model.Stages)
            {
                expression = AppendStage(stage, expression);
            }

            return source.Provider.CreateQuery(expression);
        }

        private static Expression AppendStage(QueryStage stage, Expression source)
        {
            switch (stage.Kind)
            {
                case QueryStageKind.Filter:
                    return Expression.Call(
                        typeof(Queryable),
                        "Where",
                        new[] { stage.InputType },
                        source,
                        Expression.Quote(((FilterStage)stage).Predicate));

                case QueryStageKind.Project:
                    return Expression.Call(
                        typeof(Queryable),
                        "Select",
                        new[] { stage.InputType, stage.OutputType },
                        source,
                        Expression.Quote(((ProjectStage)stage).Selector));

                case QueryStageKind.Order:
                {
                    var order = (OrderStage)stage;
                    var current = source;

                    for (var i = 0; i < order.Orderings.Count; i++)
                    {
                        var ordering = order.Orderings[i];
                        var name = i == 0
                            ? (ordering.Descending ? "OrderByDescending" : "OrderBy")
                            : (ordering.Descending ? "ThenByDescending" : "ThenBy");

                        current = Expression.Call(
                            typeof(Queryable),
                            name,
                            new[] { stage.InputType, ordering.KeySelector.ReturnType },
                            current,
                            Expression.Quote(ordering.KeySelector));
                    }

                    return current;
                }

                case QueryStageKind.Skip:
                case QueryStageKind.Take:
                    return Expression.Call(
                        typeof(Queryable),
                        stage.Kind == QueryStageKind.Skip ? "Skip" : "Take",
                        new[] { stage.InputType },
                        source,
                        Expression.Constant(((PagingStage)stage).Count));

                case QueryStageKind.GroupBy:
                {
                    var group = (GroupByStage)stage;

                    return group.ElementSelector == null
                        ? Expression.Call(
                            typeof(Queryable),
                            "GroupBy",
                            new[] { stage.InputType, group.KeySelector.ReturnType },
                            source,
                            Expression.Quote(group.KeySelector))
                        : Expression.Call(
                            typeof(Queryable),
                            "GroupBy",
                            new[] { stage.InputType, group.KeySelector.ReturnType, group.ElementSelector.ReturnType },
                            source,
                            Expression.Quote(group.KeySelector),
                            Expression.Quote(group.ElementSelector));
                }

                case QueryStageKind.Join:
                    throw new NotSupportedException(
                        "The join's inner sequence is resolved to a value while the query is "
                        + "translated, so the stage holds a materialised sequence rather than a row "
                        + "set a provider could translate. A provider-side join needs an inner query "
                        + "the adapter supplies; keep this join on the client side with AsEnumerable(), "
                        + "or express it against the inner row set directly.");

                case QueryStageKind.DistinctBy:
                case QueryStageKind.CountBy:
                    throw new NotSupportedException(
                        "The operator '" + stage.OperatorName + "' has no equivalent in a query "
                        + "language: it is a keyed operator the engine owns, so a provider cannot "
                        + "translate it. Call AsEnumerable() before it, or let the adapter apply it "
                        + "over the rows the provider returns.");

                default:
                    throw new NotSupportedException(
                        "The stage '" + stage.OperatorName + "' has no query-language form. The "
                        + "translatable subset of 7.4.0 covers Where, Select, OrderBy, ThenBy, Skip, "
                        + "Take and GroupBy.");
            }
        }
    }
}
