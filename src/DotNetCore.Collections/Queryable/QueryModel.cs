using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The kind of a <see cref="QueryStage"/>: which operator shape the stage came from.
    /// </summary>
    public enum QueryStageKind
    {
        /// <summary>A predicate stage, built by <c>Where</c>.</summary>
        Filter = 0,

        /// <summary>A projection stage, built by <c>Select</c>.</summary>
        Project = 1,

        /// <summary>An ordering stage, built by <c>OrderBy</c> / <c>ThenBy</c> / <c>Order</c>.</summary>
        Order = 2,

        /// <summary>A paging stage, built by <c>Skip</c>.</summary>
        Skip = 3,

        /// <summary>A paging stage, built by <c>Take</c>.</summary>
        Take = 4,

        /// <summary>A distinct-by-key stage, built by <c>DistinctBy</c>.</summary>
        DistinctBy = 5,

        /// <summary>A count-by-key stage, built by <c>CountBy</c>.</summary>
        CountBy = 6,

        /// <summary>A grouping stage, built by <c>GroupBy</c>.</summary>
        GroupBy = 7,

        /// <summary>A join stage, built by <c>Join</c>.</summary>
        Join = 8,
    }

    /// <summary>
    /// One key of an ordering stage: the key selector together with its direction.
    /// </summary>
    /// <remarks>
    /// A single <see cref="OrderStage"/> carries every key of one ordering segment, because
    /// <c>ThenBy</c> is not an operator of its own: it only exists to extend the ordering in front of
    /// it. Splitting the segment into stages would lose the fact that the keys are compared in
    /// sequence.
    /// </remarks>
    public sealed class QueryOrdering
    {
        internal QueryOrdering(string operatorName, LambdaExpression keySelector, bool descending)
        {
            this.OperatorName = operatorName;
            this.KeySelector = keySelector;
            this.Descending = descending;
        }

        /// <summary>Gets the name of the operator that introduced this key.</summary>
        /// <value>One of <c>OrderBy</c>, <c>ThenBy</c>, <c>Order</c> or <c>OrderDescending</c>.</value>
        public string OperatorName { get; }

        /// <summary>Gets the key selector of this ordering key.</summary>
        public LambdaExpression KeySelector { get; }

        /// <summary>Gets a value indicating whether this key is compared in descending order.</summary>
        public bool Descending { get; }
    }

    /// <summary>
    /// One step of a translated query. A query is an ordered list of stages followed by an optional
    /// terminal aggregate.
    /// </summary>
    /// <remarks>
    /// A stage is deliberately a plain description, not a value enumerator: the query layer cannot
    /// reuse the engine's value-type enumerators because an expression tree is built at run time and
    /// therefore has no compile-time generic expansion to specialise. The stage model is what an
    /// adapter renders (F7-13) and what the reference executor replays in memory.
    /// </remarks>
    public abstract class QueryStage
    {
        private protected QueryStage(QueryStageKind kind, string operatorName, Type inputType, Type outputType)
        {
            this.Kind = kind;
            this.OperatorName = operatorName;
            this.InputType = inputType;
            this.OutputType = outputType;
        }

        /// <summary>Gets the kind of this stage.</summary>
        public QueryStageKind Kind { get; }

        /// <summary>Gets the name of the operator this stage was translated from.</summary>
        public string OperatorName { get; }

        /// <summary>Gets the element type this stage consumes.</summary>
        public Type InputType { get; }

        /// <summary>Gets the element type this stage produces.</summary>
        public Type OutputType { get; }

        /// <summary>Renders this stage as a single line, for diagnostics and tests.</summary>
        /// <returns>A human-readable description of the stage.</returns>
        public abstract string Describe();
    }

    /// <summary>A predicate stage, translated from <c>Where</c>.</summary>
    public sealed class FilterStage : QueryStage
    {
        internal FilterStage(string operatorName, Type inputType, LambdaExpression predicate)
            : base(QueryStageKind.Filter, operatorName, inputType, inputType) => this.Predicate = predicate;

        /// <summary>Gets the predicate. Two parameters mean the indexed <c>Where</c> overload.</summary>
        public LambdaExpression Predicate { get; }

        /// <summary>Renders this stage as a single line.</summary>
        /// <returns>A human-readable description of the stage.</returns>
        public override string Describe() => "Filter(" + this.Predicate + ")";
    }

    /// <summary>A projection stage, translated from <c>Select</c>.</summary>
    public sealed class ProjectStage : QueryStage
    {
        internal ProjectStage(string operatorName, Type inputType, Type outputType, LambdaExpression selector)
            : base(QueryStageKind.Project, operatorName, inputType, outputType) => this.Selector = selector;

        /// <summary>Gets the projection. Two parameters mean the indexed <c>Select</c> overload.</summary>
        public LambdaExpression Selector { get; }

        /// <summary>Renders this stage as a single line.</summary>
        /// <returns>A human-readable description of the stage.</returns>
        public override string Describe() => "Project(" + this.Selector + ")";
    }

    /// <summary>An ordering stage, translated from <c>OrderBy</c> / <c>ThenBy</c> / <c>Order</c>.</summary>
    public sealed class OrderStage : QueryStage
    {
        internal OrderStage(string operatorName, Type elementType)
            : base(QueryStageKind.Order, operatorName, elementType, elementType) => this.Orderings = new List<QueryOrdering>();

        /// <summary>Gets the ordering keys, in comparison order.</summary>
        public IReadOnlyList<QueryOrdering> Orderings { get; }

        internal void Add(string operatorName, LambdaExpression keySelector, bool descending) =>
            ((List<QueryOrdering>)this.Orderings).Add(new QueryOrdering(operatorName, keySelector, descending));

        /// <summary>Renders this stage as a single line.</summary>
        /// <returns>A human-readable description of the stage.</returns>
        public override string Describe()
        {
            var text = new StringBuilder("Order(");
            for (var i = 0; i < this.Orderings.Count; i++)
            {
                if (i != 0)
                {
                    text.Append(", ");
                }

                text.Append(this.Orderings[i].KeySelector).Append(this.Orderings[i].Descending ? " desc" : " asc");
            }

            return text.Append(')').ToString();
        }
    }

    /// <summary>A paging stage, translated from <c>Skip</c> or <c>Take</c>.</summary>
    public sealed class PagingStage : QueryStage
    {
        internal PagingStage(QueryStageKind kind, string operatorName, Type elementType, int count)
            : base(kind, operatorName, elementType, elementType) => this.Count = count;

        /// <summary>Gets the number of elements to skip or take.</summary>
        public int Count { get; }

        /// <summary>Renders this stage as a single line.</summary>
        /// <returns>A human-readable description of the stage.</returns>
        public override string Describe() => this.OperatorName + "(" + this.Count + ")";
    }

    /// <summary>A distinct-by-key stage, translated from <c>DistinctBy</c>.</summary>
    public sealed class DistinctByStage : QueryStage
    {
        internal DistinctByStage(string operatorName, Type elementType, LambdaExpression keySelector)
            : base(QueryStageKind.DistinctBy, operatorName, elementType, elementType) => this.KeySelector = keySelector;

        /// <summary>Gets the key selector the elements are deduplicated by.</summary>
        public LambdaExpression KeySelector { get; }

        /// <summary>Renders this stage as a single line.</summary>
        /// <returns>A human-readable description of the stage.</returns>
        public override string Describe() => "DistinctBy(" + this.KeySelector + ")";
    }

    /// <summary>A count-by-key stage, translated from <c>CountBy</c>.</summary>
    public sealed class CountByStage : QueryStage
    {
        internal CountByStage(string operatorName, Type inputType, Type outputType, LambdaExpression keySelector)
            : base(QueryStageKind.CountBy, operatorName, inputType, outputType) => this.KeySelector = keySelector;

        /// <summary>Gets the key selector the elements are counted by.</summary>
        public LambdaExpression KeySelector { get; }

        /// <summary>Renders this stage as a single line.</summary>
        /// <returns>A human-readable description of the stage.</returns>
        public override string Describe() => "CountBy(" + this.KeySelector + ")";
    }

    /// <summary>A grouping stage, translated from <c>GroupBy</c>.</summary>
    public sealed class GroupByStage : QueryStage
    {
        internal GroupByStage(string operatorName, Type inputType, Type outputType, LambdaExpression keySelector, LambdaExpression? elementSelector)
            : base(QueryStageKind.GroupBy, operatorName, inputType, outputType)
        {
            this.KeySelector = keySelector;
            this.ElementSelector = elementSelector;
        }

        /// <summary>Gets the key selector of the grouping.</summary>
        public LambdaExpression KeySelector { get; }

        /// <summary>Gets the element selector of the grouping, or <see langword="null"/> when the
        /// elements are grouped unchanged.</summary>
        public LambdaExpression? ElementSelector { get; }

        /// <summary>Renders this stage as a single line.</summary>
        /// <returns>A human-readable description of the stage.</returns>
        public override string Describe() =>
            this.ElementSelector == null
                ? "GroupBy(" + this.KeySelector + ")"
                : "GroupBy(" + this.KeySelector + ", " + this.ElementSelector + ")";
    }

    /// <summary>A join stage, translated from <c>Join</c>.</summary>
    public sealed class JoinStage : QueryStage
    {
        internal JoinStage(
            string operatorName,
            Type outerType,
            Type innerElementType,
            Type outputType,
            IEnumerable inner,
            LambdaExpression outerKeySelector,
            LambdaExpression innerKeySelector,
            LambdaExpression resultSelector)
            : base(QueryStageKind.Join, operatorName, outerType, outputType)
        {
            this.InnerElementType = innerElementType;
            this.Inner = inner;
            this.OuterKeySelector = outerKeySelector;
            this.InnerKeySelector = innerKeySelector;
            this.ResultSelector = resultSelector;
        }

        /// <summary>Gets the element type of the inner sequence.</summary>
        public Type InnerElementType { get; }

        /// <summary>Gets the inner sequence of the join.</summary>
        /// <remarks>
        /// A join is only translatable while the inner sequence is a closed-over value. An inner
        /// sequence that is itself a query would make the stage a sub-query, which the 7.4.0 subset
        /// deliberately leaves out.
        /// </remarks>
        public IEnumerable Inner { get; }

        /// <summary>Gets the key selector applied to the outer element.</summary>
        public LambdaExpression OuterKeySelector { get; }

        /// <summary>Gets the key selector applied to the inner element.</summary>
        public LambdaExpression InnerKeySelector { get; }

        /// <summary>Gets the result selector of the join.</summary>
        public LambdaExpression ResultSelector { get; }

        /// <summary>Renders this stage as a single line.</summary>
        /// <returns>A human-readable description of the stage.</returns>
        public override string Describe() =>
            "Join(" + this.OuterKeySelector + " = " + this.InnerKeySelector + " -> " + this.ResultSelector + ")";
    }

    /// <summary>
    /// A terminal aggregate: the operator that reduces the pipeline to a single value.
    /// </summary>
    public sealed class QueryTerminal
    {
        internal QueryTerminal(string operatorName, Type elementType, Type resultType, LambdaExpression? selector)
        {
            this.OperatorName = operatorName;
            this.ElementType = elementType;
            this.ResultType = resultType;
            this.Selector = selector;
        }

        /// <summary>Gets the name of the aggregate operator.</summary>
        public string OperatorName { get; }

        /// <summary>Gets the element type the aggregate is applied to.</summary>
        public Type ElementType { get; }

        /// <summary>Gets the type of the aggregate result.</summary>
        public Type ResultType { get; }

        /// <summary>Gets the selector applied to each element first, or <see langword="null"/>.</summary>
        public LambdaExpression? Selector { get; }

        /// <summary>Renders this terminal as a single line.</summary>
        /// <returns>A human-readable description of the terminal.</returns>
        public string Describe() =>
            this.Selector == null ? this.OperatorName + "()" : this.OperatorName + "(" + this.Selector + ")";
    }

    /// <summary>
    /// The translated form of one query: the source it starts from, the ordered stages applied to
    /// it, and the optional terminal aggregate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A model only ever exists for a query that the translator accepted, so every model is
    /// translatable by construction: an operator outside the subset fails while the model is being
    /// built, with a <see cref="NotSupportedException"/> naming the operator. That is the R7-03
    /// ruling expressed in code - the engine does not fake a translation for what it cannot express.
    /// </para>
    /// <para>
    /// The model is what an adapter renders into its own query language (F7-13) and what the
    /// reference executor replays in memory, which is what makes the translation testable without an
    /// ORM in the reference set.
    /// </para>
    /// </remarks>
    public sealed class QueryModel
    {
        internal QueryModel(
            Type sourceType,
            Type resultType,
            System.Linq.IQueryable source,
            System.Collections.IEnumerable rows,
            IReadOnlyList<QueryStage> stages,
            QueryTerminal? terminal)
        {
            this.SourceType = sourceType;
            this.ResultType = resultType;
            this.Source = source;
            this.Rows = rows;
            this.Stages = stages;
            this.Terminal = terminal;
        }

        /// <summary>Gets the element type of the source sequence.</summary>
        public Type SourceType { get; }

        /// <summary>
        /// Gets the element type the stages produce: the result type when there is no terminal, and
        /// the element type the terminal consumes when there is one.
        /// </summary>
        public Type ResultType { get; }

        /// <summary>Gets the queryable the query starts from.</summary>
        public System.Linq.IQueryable Source { get; }

        /// <summary>Gets the rows the reference executor replays the stages over.</summary>
        internal System.Collections.IEnumerable Rows { get; }

        /// <summary>Gets the stages of the query, in application order.</summary>
        public IReadOnlyList<QueryStage> Stages { get; }

        /// <summary>Gets the terminal aggregate, or <see langword="null"/> when the query returns a
        /// sequence.</summary>
        public QueryTerminal? Terminal { get; }

        /// <summary>
        /// Replays this model onto a query provider, producing the expression tree the provider
        /// translates into its own query language.
        /// </summary>
        /// <param name="source">The provider's queryable, over the same element type as the query started from.</param>
        /// <returns>A queryable over the stages of this model.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="source"/> has a different element type than the query started from.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// The model ends in an aggregate, or contains a stage a query language cannot express.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This is how an adapter integration consumes the model: the model says what the query does
        /// in provider-neutral terms, and this method turns that back into the provider's own
        /// vocabulary without any part of the adapter having to understand an expression tree.
        /// </para>
        /// <para>
        /// Three things are deliberately not replayed, because a provider has no form for them and
        /// inventing one is exactly what the R7-03 ruling forbids:
        /// </para>
        /// <list type="bullet">
        /// <item><description>
        /// An aggregate terminal. <c>Count</c> / <c>LongCount</c> / <c>Sum</c> / <c>Min</c> /
        /// <c>Max</c> are applied through the provider's own aggregate API, so the adapter reads
        /// <see cref="Terminal"/> and calls its provider's counterpart rather than folding the
        /// aggregate into the row query.
        /// </description></item>
        /// <item><description>
        /// <c>DistinctBy</c> and <c>CountBy</c>. Both are keyed operators the engine owns; no query
        /// language has them, and the <c>GroupBy</c> rewrite that would imitate them is not the same
        /// query. Apply them over the rows the provider returns.
        /// </description></item>
        /// <item><description>
        /// A join whose inner sequence was closed over. The stage holds a value, not a row set, so
        /// there is nothing for the provider to join against; such a join stays on the client side.
        /// </description></item>
        /// </list>
        /// <para>
        /// Refusing is per stage, not per model: a model that mixes a translatable prefix with an
        /// untranslatable suffix still fails here rather than silently dropping the suffix, and the
        /// exception names the operator so the caller knows which call to move behind
        /// <c>AsEnumerable()</c>.
        /// </para>
        /// </remarks>
        public System.Linq.IQueryable AsQueryable(System.Linq.IQueryable source) =>
            QueryModelRenderer.Render(this, source);

        /// <summary>Renders the whole model as a single line, for diagnostics and tests.</summary>
        /// <returns>A human-readable description of the model.</returns>
        public string Describe()
        {
            var text = new StringBuilder();
            text.Append("from ").Append(ShortName(this.SourceType));

            foreach (var stage in this.Stages)
            {
                text.Append(" -> ").Append(stage.Describe());
            }

            if (this.Terminal != null)
            {
                text.Append(" -> ").Append(this.Terminal.Describe());
            }

            return text.ToString();
        }

        /// <summary>Renders the whole model as a single line.</summary>
        /// <returns>A human-readable description of the model.</returns>
        public override string ToString() => this.Describe();

        private static string ShortName(Type type)
        {
            var name = type.Name;
            var tick = name.IndexOf('`');
            if (tick < 0)
            {
                return name;
            }

            var text = new StringBuilder(name.Substring(0, tick));
            text.Append('<');

            var arguments = type.GetGenericArguments();
            for (var i = 0; i < arguments.Length; i++)
            {
                if (i != 0)
                {
                    text.Append(',');
                }

                text.Append(ShortName(arguments[i]));
            }

            return text.Append('>').ToString();
        }
    }
}
