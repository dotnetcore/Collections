using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace DotNetCore.Collections
{
    /// <summary>
    /// Turns a LINQ expression tree into a <see cref="QueryModel"/>, and is the single place where
    /// the translatable subset of 7.4.0 is decided.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Everything outside the subset fails here, while the model is being built, with a
    /// <see cref="NotSupportedException"/> that names the operator. Nothing is silently degraded to
    /// an in-memory scan and nothing is bent into a translation it does not have - that is the R7-03
    /// ruling (the engine commits to the translatable subset only) expressed as code.
    /// </para>
    /// <para>
    /// The transposable operators are the thirteen <see cref="Queryable"/> operators the 7.4.0 scope
    /// lists (<c>Where</c>, <c>Select</c>, <c>Take</c>, <c>Skip</c>, <c>GroupBy</c>, <c>Join</c>,
    /// <c>OrderBy</c>, <c>ThenBy</c> and the five aggregates) plus the three keyed operators the
    /// engine owns on the queryable face (<c>DistinctBy</c>, <c>Order</c> / <c>OrderDescending</c>,
    /// <c>CountBy</c>).
    /// </para>
    /// </remarks>
    internal static class QueryModelBuilder
    {
        private static readonly HashSet<string> TerminalNames = BuildTerminalNames();

        internal static QueryModel Build(Expression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            QueryTerminal? terminal = null;
            var pipeline = expression;

            var top = pipeline as MethodCallExpression;
            if (top != null && IsTerminal(top))
            {
                terminal = CreateTerminal(top);
                pipeline = top.Arguments[0];
            }

            var stages = new List<QueryStage>();
            var root = Parse(pipeline, stages);

            var resultType = stages.Count == 0 ? root.ElementType : stages[stages.Count - 1].OutputType;
            return new QueryModel(root.ElementType, resultType, root.Source, root.Rows, stages, terminal);
        }

        private static RootQuery Parse(Expression expression, List<QueryStage> stages)
        {
            var call = expression as MethodCallExpression;
            if (call != null && IsQueryOperator(call))
            {
                var root = Parse(call.Arguments[0], stages);
                AppendStage(call, stages);
                return root;
            }

            return ResolveRoot(expression);
        }

        private static RootQuery ResolveRoot(Expression expression)
        {
            var constant = expression as ConstantExpression;
            var queryable = constant == null ? null : constant.Value as IQueryable;

            if (queryable != null)
            {
                var owned = constant!.Value as ICollectionQueryRoot;
                var rows = owned != null ? owned.Rows : (IEnumerable)queryable;
                return new RootQuery(queryable, queryable.ElementType, rows);
            }

            throw new NotSupportedException(
                "The query does not start from a collection queryable. Build the query from "
                + "AsCollectionQueryable() so that the source is a known row set.");
        }

        private static void AppendStage(MethodCallExpression call, List<QueryStage> stages)
        {
            var name = call.Method.Name;
            var input = ElementTypeOfSequence(call.Arguments[0].Type);
            var output = call.Method.ReturnType.IsGenericType
                ? ElementTypeOfSequence(call.Method.ReturnType)
                : input;

            switch (name)
            {
                case "Where":
                    stages.Add(new FilterStage(name, input, LambdaOf(call.Arguments[1], name)));
                    return;

                case "Select":
                    stages.Add(new ProjectStage(name, input, output, LambdaOf(call.Arguments[1], name)));
                    return;

                case "OrderBy":
                case "OrderByDescending":
                case "Order":
                case "OrderDescending":
                {
                    var descending = name.EndsWith("Descending", StringComparison.Ordinal);
                    var key = name == "Order" || name == "OrderDescending"
                        ? Identity(input)
                        : LambdaOf(call.Arguments[1], name);

                    var stage = new OrderStage(name, input);
                    stage.Add(name, key, descending);
                    stages.Add(stage);
                    return;
                }

                case "ThenBy":
                case "ThenByDescending":
                {
                    var previous = stages.Count == 0 ? null : stages[stages.Count - 1] as OrderStage;
                    if (previous == null || previous.InputType != input)
                    {
                        throw new NotSupportedException(
                            "ThenBy must extend an ordering stage on the same element type. A ThenBy "
                            + "that starts a new ordering is not translatable; use OrderBy instead.");
                    }

                    previous.Add(name, LambdaOf(call.Arguments[1], name), name == "ThenByDescending");
                    return;
                }

                case "Skip":
                    stages.Add(new PagingStage(QueryStageKind.Skip, name, input, ConstantCount(call.Arguments[1], name)));
                    return;

                case "Take":
                    stages.Add(new PagingStage(QueryStageKind.Take, name, input, ConstantCount(call.Arguments[1], name)));
                    return;

                case "DistinctBy":
                    EnsureNoComparer(call, name, 2);
                    stages.Add(new DistinctByStage(name, input, LambdaOf(call.Arguments[1], name)));
                    return;

                case "CountBy":
                    EnsureNoComparer(call, name, 2);
                    stages.Add(new CountByStage(name, input, output, LambdaOf(call.Arguments[1], name)));
                    return;

                case "GroupBy":
                    stages.Add(CreateGroupBy(call, name, input, output));
                    return;

                case "Join":
                    stages.Add(CreateJoin(call, name, input, output));
                    return;

                default:
                    throw Untranslatable(name);
            }
        }

        private static GroupByStage CreateGroupBy(MethodCallExpression call, string name, Type input, Type output)
        {
            var keySelector = LambdaOf(call.Arguments[1], name);
            LambdaExpression? elementSelector = null;

            for (var i = 2; i < call.Arguments.Count; i++)
            {
                var argument = Unwrap(call.Arguments[i]);
                var lambda = argument as LambdaExpression;

                if (lambda == null)
                {
                    throw new NotSupportedException(
                        "The comparer overloads of GroupBy are outside the translatable subset of "
                        + "7.4.0; the key comparer cannot be expressed by a query language.");
                }

                if (lambda.Parameters.Count != 1 || elementSelector != null)
                {
                    throw new NotSupportedException(
                        "The GroupBy result selector (and the element-plus-result overload) is outside "
                        + "the translatable subset of 7.4.0. Project the groups after the grouping instead.");
                }

                elementSelector = lambda;
            }

            return new GroupByStage(name, input, output, keySelector, elementSelector);
        }

        private static JoinStage CreateJoin(MethodCallExpression call, string name, Type input, Type output)
        {
            var inner = Evaluate(call.Arguments[1]) as IEnumerable;

            if (inner == null)
            {
                throw new NotSupportedException(
                    "The inner sequence of a join must be a closed-over value. An inner sequence that "
                    + "is itself a query is a sub-query, which is outside the translatable subset of "
                    + "7.4.0; materialise it first.");
            }

            var typeArguments = call.Method.GetGenericArguments();

            return new JoinStage(
                name,
                input,
                typeArguments[1],
                output,
                inner,
                LambdaOf(call.Arguments[2], name),
                LambdaOf(call.Arguments[3], name),
                LambdaOf(call.Arguments[4], name));
        }

        private static QueryTerminal CreateTerminal(MethodCallExpression call)
        {
            var name = call.Method.Name;
            var element = ElementTypeOfSequence(call.Arguments[0].Type);
            LambdaExpression? selector = null;

            if (call.Arguments.Count > 1)
            {
                selector = LambdaOf(call.Arguments[1], name);
            }

            return new QueryTerminal(name, element, call.Method.ReturnType, selector);
        }

        private static bool IsQueryOperator(MethodCallExpression call)
        {
            var declaring = call.Method.DeclaringType;
            if (declaring == typeof(Queryable))
            {
                return true;
            }

            if (declaring != typeof(CollectionExtensions))
            {
                return false;
            }

            var receiver = call.Method.GetParameters()[0].ParameterType;
            return receiver.IsGenericType
                && receiver.GetGenericTypeDefinition() == typeof(IQueryable<>)
                && !TerminalNames.Contains(call.Method.Name);
        }

        private static bool IsTerminal(MethodCallExpression call) =>
            call.Method.DeclaringType == typeof(Queryable) && TerminalNames.Contains(call.Method.Name);

        private static void EnsureNoComparer(MethodCallExpression call, string name, int expectedArguments)
        {
            if (call.Arguments.Count > expectedArguments)
            {
                throw new NotSupportedException(
                    "The comparer overload of '" + name + "' is outside the translatable subset of "
                    + "7.4.0; a key comparer cannot be expressed by a query language.");
            }
        }

        private static int ConstantCount(Expression expression, string name)
        {
            var argument = Unwrap(expression);

            if (argument.Type.Name == "Range")
            {
                throw new NotSupportedException(
                    "'" + name + "' was called with a Range, which is outside the translatable subset "
                    + "of 7.4.0 (Range only exists from .NET 6 onwards). Use the int overload.");
            }

            var constant = argument as ConstantExpression;
            if (constant == null || !(constant.Value is int))
            {
                throw new NotSupportedException(
                    "'" + name + "' needs a constant count to be translatable; a computed count is "
                    + "evaluated at run time and cannot be pushed into a query.");
            }

            return (int)constant.Value!;
        }

        /// <summary>
        /// Resolves an argument that stands for a value rather than for a lambda.
        /// </summary>
        /// <remarks>
        /// A closed-over sequence reaches the tree as a field or property read on the closure, not as
        /// a constant, so it has to be evaluated before it can be described as a stage. Only the
        /// arguments that are values by definition go through here - the join's inner sequence - so
        /// no part of the predicate or selector bodies is ever evaluated early.
        /// </remarks>
        private static object? Evaluate(Expression expression)
        {
            var constant = expression as ConstantExpression;
            if (constant != null)
            {
                return constant.Value;
            }

            try
            {
                var body = Expression.Convert(Unwrap(expression), typeof(object));
                return Expression.Lambda<Func<object?>>(body).Compile()();
            }
            catch (InvalidOperationException exception)
            {
                throw new NotSupportedException(
                    "The value argument of the query could not be resolved before translation.",
                    exception);
            }
        }

        private static LambdaExpression LambdaOf(Expression expression, string name)
        {
            var lambda = Unwrap(expression) as LambdaExpression;
            if (lambda == null)
            {
                throw new NotSupportedException(
                    "The argument of '" + name + "' is not a lambda expression, so it is outside the "
                    + "translatable subset of 7.4.0.");
            }

            return lambda;
        }

        private static Expression Unwrap(Expression expression)
        {
            var current = expression;
            while (current.NodeType == ExpressionType.Quote)
            {
                current = ((UnaryExpression)current).Operand;
            }

            return current;
        }

        private static LambdaExpression Identity(Type elementType)
        {
            var parameter = Expression.Parameter(elementType, "element");
            return Expression.Lambda(parameter, parameter);
        }

        internal static Type ElementTypeOfSequence(Type type)
        {
            if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition();
                if (definition == typeof(IQueryable<>)
                    || definition == typeof(IEnumerable<>)
                    || definition == typeof(IOrderedQueryable<>)
                    || definition == typeof(IOrderedEnumerable<>))
                {
                    return type.GetGenericArguments()[0];
                }
            }

            foreach (var candidate in type.GetInterfaces())
            {
                if (candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return candidate.GetGenericArguments()[0];
                }
            }

            throw new NotSupportedException(
                "The type '" + type.Name + "' is not a sequence, so the query cannot be translated.");
        }

        private static NotSupportedException Untranslatable(string name) =>
            new NotSupportedException(
                "The operator '" + name + "' is outside the translatable subset of 7.4.0. Call "
                + "AsEnumerable() and use the value-enumerable operators for an in-memory query, or "
                + "rewrite the query with the operators the subset covers.");

        private static HashSet<string> BuildTerminalNames()
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            names.Add("Count");
            names.Add("LongCount");
            names.Add("Sum");
            names.Add("Min");
            names.Add("Max");
            return names;
        }

        private sealed class RootQuery
        {
            internal RootQuery(IQueryable source, Type elementType, IEnumerable rows)
            {
                this.Source = source;
                this.ElementType = elementType;
                this.Rows = rows;
            }

            internal IQueryable Source { get; }

            internal Type ElementType { get; }

            internal IEnumerable Rows { get; }
        }
    }
}
