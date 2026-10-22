using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DotNetCore.Collections
{
    /// <summary>
    /// Rebuilds a <see cref="QueryModel"/> back into an executable delegate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rebuild targets <see cref="Enumerable"/> rather than re-implementing the operators: the
    /// model already carries one <see cref="LambdaExpression"/> per operator argument, so the
    /// semantics of the thirteen standard operators are obtained from the BCL instead of being
    /// copied. Only the three operators the engine owns (<c>DistinctBy</c>, <c>CountBy</c> and the
    /// selector form of <c>Min</c> / <c>Max</c>) go through
    /// <see cref="QueryOperators"/>, because the BCL shapes they would map to do not exist on every
    /// target - and a reference executor that behaves differently per TFM would make the translation
    /// untestable.
    /// </para>
    /// <para>
    /// This is the in-memory reference path. It exists so that the translation can be verified
    /// without an ORM; it is not the performance path, and it is not what an adapter should use.
    /// </para>
    /// </remarks>
    internal static class QueryExpressionComposer
    {
        private static readonly MethodInfo DistinctByOperator = FindOperator("DistinctBy");
        private static readonly MethodInfo CountByOperator = FindOperator("CountBy");
        private static readonly MethodInfo MinOperator = FindOperator("Min");
        private static readonly MethodInfo MaxOperator = FindOperator("Max");

        internal static Func<object> Compose(QueryModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var rowsType = typeof(IEnumerable<>).MakeGenericType(model.SourceType);
            Expression current = Expression.Constant(model.Rows, rowsType);

            foreach (var stage in model.Stages)
            {
                current = AppendStage(current, stage);
            }

            if (model.Terminal != null)
            {
                current = AppendTerminal(current, model.Terminal);
            }

            return Expression.Lambda<Func<object>>(Expression.Convert(current, typeof(object))).Compile();
        }

        private static Expression AppendStage(Expression source, QueryStage stage)
        {
            switch (stage.Kind)
            {
                case QueryStageKind.Filter:
                {
                    var filter = (FilterStage)stage;
                    return Expression.Call(
                        typeof(Enumerable),
                        "Where",
                        new[] { stage.InputType },
                        source,
                        DelegateOf(filter.Predicate));
                }

                case QueryStageKind.Project:
                {
                    var project = (ProjectStage)stage;
                    return Expression.Call(
                        typeof(Enumerable),
                        "Select",
                        new[] { stage.InputType, stage.OutputType },
                        source,
                        DelegateOf(project.Selector));
                }

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
                            typeof(Enumerable),
                            name,
                            new[] { stage.InputType, ordering.KeySelector.ReturnType },
                            current,
                            DelegateOf(ordering.KeySelector));
                    }

                    return current;
                }

                case QueryStageKind.Skip:
                    return Expression.Call(
                        typeof(Enumerable),
                        "Skip",
                        new[] { stage.InputType },
                        source,
                        Expression.Constant(((PagingStage)stage).Count));

                case QueryStageKind.Take:
                    return Expression.Call(
                        typeof(Enumerable),
                        "Take",
                        new[] { stage.InputType },
                        source,
                        Expression.Constant(((PagingStage)stage).Count));

                case QueryStageKind.DistinctBy:
                {
                    var distinct = (DistinctByStage)stage;
                    var method = DistinctByOperator.MakeGenericMethod(
                        stage.InputType,
                        distinct.KeySelector.ReturnType);

                    return Expression.Call(method, source, DelegateOf(distinct.KeySelector));
                }

                case QueryStageKind.CountBy:
                {
                    var count = (CountByStage)stage;
                    var method = CountByOperator.MakeGenericMethod(
                        stage.InputType,
                        count.KeySelector.ReturnType);

                    return Expression.Call(method, source, DelegateOf(count.KeySelector));
                }

                case QueryStageKind.GroupBy:
                {
                    var group = (GroupByStage)stage;
                    var keyType = group.KeySelector.ReturnType;

                    return group.ElementSelector == null
                        ? Expression.Call(
                            typeof(Enumerable),
                            "GroupBy",
                            new[] { stage.InputType, keyType },
                            source,
                            DelegateOf(group.KeySelector))
                        : Expression.Call(
                            typeof(Enumerable),
                            "GroupBy",
                            new[] { stage.InputType, keyType, group.ElementSelector.ReturnType },
                            source,
                            DelegateOf(group.KeySelector),
                            DelegateOf(group.ElementSelector));
                }

                case QueryStageKind.Join:
                {
                    var join = (JoinStage)stage;
                    var innerType = typeof(IEnumerable<>).MakeGenericType(join.InnerElementType);

                    return Expression.Call(
                        typeof(Enumerable),
                        "Join",
                        new[]
                        {
                            stage.InputType,
                            join.InnerElementType,
                            join.OuterKeySelector.ReturnType,
                            stage.OutputType,
                        },
                        source,
                        Expression.Constant(join.Inner, innerType),
                        DelegateOf(join.OuterKeySelector),
                        DelegateOf(join.InnerKeySelector),
                        DelegateOf(join.ResultSelector));
                }

                default:
                    throw new NotSupportedException(
                        "The stage '" + stage.OperatorName + "' cannot be replayed in memory.");
            }
        }

        private static Expression AppendTerminal(Expression source, QueryTerminal terminal)
        {
            switch (terminal.OperatorName)
            {
                case "Count":
                    return terminal.Selector == null
                        ? Expression.Call(typeof(Enumerable), "Count", new[] { terminal.ElementType }, source)
                        : Expression.Call(
                            typeof(Enumerable),
                            "Count",
                            new[] { terminal.ElementType },
                            source,
                            DelegateOf(terminal.Selector));

                case "LongCount":
                    return terminal.Selector == null
                        ? Expression.Call(typeof(Enumerable), "LongCount", new[] { terminal.ElementType }, source)
                        : Expression.Call(
                            typeof(Enumerable),
                            "LongCount",
                            new[] { terminal.ElementType },
                            source,
                            DelegateOf(terminal.Selector));

                case "Sum":
                    // The selector form of Sum carries its numeric type in the delegate, and every
                    // target has the same numeric overloads, so the BCL resolves it by argument type.
                    return terminal.Selector == null
                        ? Expression.Call(typeof(Enumerable), "Sum", null, source)
                        : Expression.Call(
                            typeof(Enumerable),
                            "Sum",
                            new[] { terminal.ElementType },
                            source,
                            DelegateOf(terminal.Selector));

                case "Min":
                    return terminal.Selector == null
                        ? Expression.Call(typeof(Enumerable), "Min", new[] { terminal.ElementType }, source)
                        : Expression.Call(
                            MinOperator.MakeGenericMethod(terminal.ElementType, terminal.Selector.ReturnType),
                            source,
                            DelegateOf(terminal.Selector));

                case "Max":
                    return terminal.Selector == null
                        ? Expression.Call(typeof(Enumerable), "Max", new[] { terminal.ElementType }, source)
                        : Expression.Call(
                            MaxOperator.MakeGenericMethod(terminal.ElementType, terminal.Selector.ReturnType),
                            source,
                            DelegateOf(terminal.Selector));

                default:
                    throw new NotSupportedException(
                        "The aggregate '" + terminal.OperatorName + "' cannot be replayed in memory.");
            }
        }

        private static ConstantExpression DelegateOf(LambdaExpression lambda) =>
            Expression.Constant(lambda.Compile(), lambda.Type);

        private static MethodInfo FindOperator(string name)
        {
            foreach (var method in typeof(QueryOperators).GetMethods(BindingFlags.NonPublic | BindingFlags.Static))
            {
                if (method.Name == name)
                {
                    return method;
                }
            }

            throw new InvalidOperationException("The query operator '" + name + "' is missing.");
        }
    }
}
