using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Tests
{
    /// <summary>
    /// F7-13: replaying a model onto a query provider.
    /// </summary>
    /// <remarks>
    /// The oracle is LINQ to Objects over the same data, obtained through a queryable whose provider
    /// is <see cref="EnumerableQuery"/>. That is a real provider, not a stub: it runs whatever
    /// expression tree it is handed, so a stage that is rebuilt with the wrong operator, the wrong
    /// type argument or in the wrong order fails by result. What the replay is *for* - an expression
    /// tree another provider can translate - is asserted structurally in the remaining cases,
    /// including the operators the replay must refuse rather than approximate.
    /// </remarks>
    public class QueryModelRenderingTests
    {
        private static readonly int[] Numbers = { 5, 3, 9, 3, 1, 7, 5 };
        private static readonly int[] Inner = { 1, 3, 5 };

        /// <summary>A query built on the engine's own face.</summary>
        private static IQueryable<int> Query() => Numbers.AsCollectionQueryable();

        /// <summary>A plain LINQ to Objects queryable, standing in for a provider's row set.</summary>
        private static IQueryable<int> Rows() => Numbers.AsQueryable();

        private static IQueryable<int> RenderedInts(QueryModel model) => (IQueryable<int>)model.AsQueryable(Rows());

        [Fact]
        public void Filter_project_and_paging_replay_onto_a_provider()
        {
            var expected = Numbers.AsQueryable().Where(x => x > 3).Select(x => x * 2).Skip(1).Take(2).ToArray();

            var model = Query().Where(x => x > 3).Select(x => x * 2).Skip(1).Take(2).ToQueryModel();

            RenderedInts(model).ToArray().ShouldBe(expected);
        }

        [Fact]
        public void Ordering_replays_and_ThenBy_stays_a_secondary_key()
        {
            var expected = Numbers.AsQueryable().OrderBy(x => x % 2).ThenByDescending(x => x).ToArray();

            var model = Query().OrderBy(x => x % 2).ThenByDescending(x => x).ToQueryModel();

            RenderedInts(model).ToArray().ShouldBe(expected);
        }

        [Fact]
        public void A_second_ordering_segment_replays_as_a_new_OrderBy()
        {
            var expected = Numbers.AsQueryable().OrderBy(x => x).OrderByDescending(x => x % 2).ToArray();

            var model = Query().OrderBy(x => x).OrderByDescending(x => x % 2).ToQueryModel();

            RenderedInts(model).ToArray().ShouldBe(expected);
        }

        [Fact]
        public void Grouping_replays_without_an_element_selector()
        {
            var model = Query().GroupBy(x => x % 2).ToQueryModel();
            var rendered = (IQueryable<IGrouping<int, int>>)model.AsQueryable(Rows());

            rendered.Select(group => group.Key).ToArray()
                .ShouldBe(Numbers.AsQueryable().GroupBy(x => x % 2).Select(group => group.Key).ToArray());
        }

        [Fact]
        public void Grouping_replays_with_an_element_selector()
        {
            var model = Query().GroupBy(x => x % 2, x => x * 10).ToQueryModel();
            var rendered = (IQueryable<IGrouping<int, int>>)model.AsQueryable(Rows());

            rendered.Select(group => group.Sum()).ToArray()
                .ShouldBe(
                    Numbers.AsQueryable()
                        .GroupBy(x => x % 2, x => x * 10)
                        .Select(group => group.Sum())
                        .ToArray());
        }

        [Fact]
        public void The_rendered_queryable_reports_the_element_type_the_model_produced()
        {
            var model = Query().Where(x => x > 3).Select(x => x.ToString()).ToQueryModel();

            model.ResultType.ShouldBe(typeof(string));

            var rendered = model.AsQueryable(Rows());

            rendered.ElementType.ShouldBe(typeof(string));
            rendered.ShouldBeAssignableTo<IQueryable<string>>();
        }

        [Fact]
        public void The_rendered_expression_names_the_operators_in_application_order()
        {
            var model = Query().Where(x => x > 3).OrderBy(x => x).Skip(1).Take(2).Select(x => x * 2).ToQueryModel();

            var names = new List<string>();
            var current = model.AsQueryable(Rows()).Expression;
            while (current is MethodCallExpression call)
            {
                names.Add(call.Method.Name);
                current = call.Arguments[0];
            }

            names.ShouldBe(new[] { "Select", "Take", "Skip", "OrderBy", "Where" });
        }

        [Fact]
        public void A_model_without_stages_replays_to_the_source_itself()
        {
            var model = Query().ToQueryModel();

            model.Stages.ShouldBeEmpty();
            RenderedInts(model).ToArray().ShouldBe(Numbers);
        }

        [Fact]
        public void An_aggregate_model_is_refused_by_name()
        {
            Expression<Func<int, int>> selector = x => x;
            var aggregate = Expression.Call(
                typeof(Queryable),
                "Sum",
                new[] { typeof(int) },
                Query().Where(x => x > 3).Expression,
                Expression.Quote(selector));

            var model = QueryModelBuilder.Build(aggregate);
            model.Terminal.ShouldNotBeNull();

            var exception = Should.Throw<NotSupportedException>(() => model.AsQueryable(Rows()));
            exception.Message.ShouldContain("Sum");
        }

        [Fact]
        public void A_join_over_a_closed_over_sequence_is_refused()
        {
            var inner = Inner;
            var model = Query().Join(inner, x => x % 3, y => y, (x, y) => x + y).ToQueryModel();
            model.Stages.Single().ShouldBeOfType<JoinStage>();

            var exception = Should.Throw<NotSupportedException>(() => model.AsQueryable(Rows()));
            exception.Message.ShouldContain("inner sequence");
        }

        [Fact]
        public void Keyed_operators_are_refused_by_name()
        {
            var distinct = Query().DistinctBy(x => x % 3).ToQueryModel();
            Should.Throw<NotSupportedException>(() => distinct.AsQueryable(Rows()))
                .Message.ShouldContain("DistinctBy");

            var counted = Query().CountBy(x => x % 3).ToQueryModel();
            Should.Throw<NotSupportedException>(() => counted.AsQueryable(Rows()))
                .Message.ShouldContain("CountBy");
        }

        [Fact]
        public void A_source_of_another_element_type_is_rejected()
        {
            var model = Query().Where(x => x > 3).ToQueryModel();
            var wrong = new[] { "a", "b" }.AsQueryable();

            Should.Throw<ArgumentException>(() => model.AsQueryable(wrong));
        }

        [Fact]
        public void A_null_source_is_rejected()
        {
            var model = Query().ToQueryModel();
            IQueryable<int> source = null!;

            Should.Throw<ArgumentNullException>(() => model.AsQueryable(source));
        }

        [Fact]
        public void A_refusal_names_the_operator_and_never_drops_the_stage()
        {
            // A translatable prefix followed by an untranslatable suffix must fail rather than render
            // the prefix and quietly pretend the suffix never existed.
            var model = Query().Where(x => x > 3).DistinctBy(x => x % 3).ToQueryModel();

            var exception = Should.Throw<NotSupportedException>(() => model.AsQueryable(Rows()));
            exception.Message.ShouldContain("DistinctBy");
        }
    }
}
