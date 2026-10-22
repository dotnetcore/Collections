using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using DotNetCore.Collections;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Tests
{
    /// <summary>
    /// F7-12: the queryable face and its translation layer.
    /// </summary>
    /// <remarks>
    /// The oracles are LINQ to Objects expressions over the same data, so a stage that mistranslates
    /// fails by result rather than by comparison against a hand-written expectation. What the
    /// translation is *for* - the model an adapter renders - is asserted structurally, because the
    /// model is the product and the in-memory replay is only its proof.
    /// </remarks>
    public class QueryableTranslationTests
    {
        private static readonly int[] Numbers = { 5, 3, 9, 3, 1, 7, 5 };

        private static IQueryable<int> Query() => Numbers.AsCollectionQueryable();

        [Fact]
        public void Translation_records_the_stages_in_application_order()
        {
            var model = Query().Where(x => x > 3).Select(x => x * 2).Take(2).ToQueryModel();

            model.Stages.Count.ShouldBe(3);
            model.Stages[0].Kind.ShouldBe(QueryStageKind.Filter);
            model.Stages[1].Kind.ShouldBe(QueryStageKind.Project);
            model.Stages[2].Kind.ShouldBe(QueryStageKind.Take);
            model.SourceType.ShouldBe(typeof(int));
            model.ResultType.ShouldBe(typeof(int));
            model.Terminal.ShouldBeNull();
            model.Describe().ShouldContain("Take(2)");
        }

        [Fact]
        public void Filter_project_and_paging_replay_against_linq_to_objects()
        {
            var expected = Numbers.Where(x => x > 3).Select(x => x * 2).Take(2).ToArray();

            var actual = Query().Where(x => x > 3).Select(x => x * 2).Take(2).ToArray();

            actual.ShouldBe(expected);
        }

        [Fact]
        public void Skip_and_take_replay_against_linq_to_objects()
        {
            var expected = Numbers.Skip(2).Take(3).ToArray();

            var actual = Query().Skip(2).Take(3).ToArray();

            actual.ShouldBe(expected);
        }

        [Fact]
        public void Paging_accepts_a_captured_count()
        {
            var skip = 1;
            var take = 4;

            Query().Skip(skip).Take(take).ToArray().ShouldBe(Numbers.Skip(skip).Take(take).ToArray());
        }

        [Fact]
        public void Indexed_filter_replays_against_linq_to_objects()
        {
            var expected = Numbers.Where((x, index) => index > 1).ToArray();

            Query().Where((x, index) => index > 1).ToArray().ShouldBe(expected);
        }

        [Fact]
        public void Indexed_projection_replays_against_linq_to_objects()
        {
            var expected = Numbers.Select((x, index) => x + index).ToArray();

            Query().Select((x, index) => x + index).ToArray().ShouldBe(expected);
        }

        [Fact]
        public void Order_replays_ascending_and_descending()
        {
            Query().Order().ToArray().ShouldBe(Numbers.OrderBy(x => x).ToArray());
            Query().OrderDescending().ToArray().ShouldBe(Numbers.OrderByDescending(x => x).ToArray());
        }

        [Fact]
        public void ThenBy_extends_one_ordering_stage_instead_of_starting_another()
        {
            var model = Query().OrderBy(x => x % 2).ThenBy(x => x).ToQueryModel();

            model.Stages.Count.ShouldBe(1);

            var order = model.Stages[0].ShouldBeOfType<OrderStage>();
            order.Orderings.Count.ShouldBe(2);
            order.Orderings[0].OperatorName.ShouldBe("OrderBy");
            order.Orderings[1].OperatorName.ShouldBe("ThenBy");

            Query().OrderBy(x => x % 2).ThenBy(x => x).ToArray()
                .ShouldBe(Numbers.OrderBy(x => x % 2).ThenBy(x => x).ToArray());
        }

        [Fact]
        public void OrderByDescending_then_thenbydescending_replays()
        {
            Query().OrderByDescending(x => x % 3).ThenByDescending(x => x).ToArray()
                .ShouldBe(Numbers.OrderByDescending(x => x % 3).ThenByDescending(x => x).ToArray());
        }

        [Fact]
        public void DistinctBy_replays_and_records_the_key_selector()
        {
            var model = Query().DistinctBy(x => x % 3).ToQueryModel();
            model.Stages.Single().ShouldBeOfType<DistinctByStage>();

            Query().DistinctBy(x => x % 3).ToArray()
                .ShouldBe(Numbers.DistinctBy(x => x % 3).ToArray());
        }

        [Fact]
        public void CountBy_replays_and_keeps_the_first_seen_key_order()
        {
            var expected = Numbers
                .GroupBy(x => x % 3)
                .Select(group => new KeyValuePair<int, int>(group.Key, group.Count()))
                .ToArray();

            var model = Query().CountBy(x => x % 3).ToQueryModel();
            model.ResultType.ShouldBe(typeof(KeyValuePair<int, int>));

            var actual = Query().CountBy(x => x % 3).ToArray();

            actual.ShouldBe(expected);
        }

        [Fact]
        public void GroupBy_by_key_replays()
        {
            var expected = Numbers.GroupBy(x => x % 2).Select(group => group.Key).ToArray();

            Query().GroupBy(x => x % 2).Select(group => group.Key).ToArray().ShouldBe(expected);
        }

        [Fact]
        public void GroupBy_with_an_element_selector_replays()
        {
            var expected = Numbers
                .GroupBy(x => x % 2, x => x * 10)
                .Select(group => group.Sum())
                .ToArray();

            Query().GroupBy(x => x % 2, x => x * 10).Select(group => group.Sum()).ToArray()
                .ShouldBe(expected);
        }

        [Fact]
        public void Join_with_a_closed_over_inner_sequence_replays()
        {
            var inner = new[] { 0, 1, 2 };

            var expected = Numbers
                .Join(inner, x => x % 3, y => y, (x, y) => x + y)
                .ToArray();

            var model = Query().Join(inner, x => x % 3, y => y, (x, y) => x + y).ToQueryModel();
            model.Stages.Single().ShouldBeOfType<JoinStage>();

            Query().Join(inner, x => x % 3, y => y, (x, y) => x + y).ToArray().ShouldBe(expected);
        }

        [Fact]
        public void Count_and_longcount_replay_with_and_without_a_predicate()
        {
            Query().Count().ShouldBe(Numbers.Count());
            Query().Count(x => x > 3).ShouldBe(Numbers.Count(x => x > 3));
            Query().LongCount().ShouldBe(Numbers.LongCount());
            Query().LongCount(x => x > 3).ShouldBe(Numbers.LongCount(x => x > 3));

            Query().Where(x => x > 3).Count().ShouldBe(Numbers.Count(x => x > 3));
        }

        [Fact]
        public void Aggregates_are_recorded_on_the_model()
        {
            // A terminal executes immediately, so the model of an aggregate query is read from the
            // expression the aggregate would have been built from.
            Expression<Func<int, int>> selector = x => x;
            var aggregate = Expression.Call(
                typeof(Queryable),
                "Sum",
                new[] { typeof(int) },
                Query().Where(x => x > 3).Expression,
                Expression.Quote(selector));

            var model = QueryModelBuilder.Build(aggregate);

            model.Terminal.ShouldNotBeNull();
            model.Terminal!.OperatorName.ShouldBe("Sum");
            model.Terminal.Selector.ShouldNotBeNull();
            model.Stages.Single().Kind.ShouldBe(QueryStageKind.Filter);
            model.Describe().ShouldContain("Sum(");
        }

        [Fact]
        public void Sum_replays_with_and_without_a_selector()
        {
            Query().Sum().ShouldBe(Numbers.Sum());
            Query().Sum(x => x * 2).ShouldBe(Numbers.Sum(x => x * 2));
        }

        [Fact]
        public void Min_and_max_replay_with_and_without_a_selector()
        {
            Query().Min().ShouldBe(Numbers.Min());
            Query().Max().ShouldBe(Numbers.Max());
            Query().Min(x => x % 4).ShouldBe(Numbers.Min(x => x % 4));
            Query().Max(x => x % 4).ShouldBe(Numbers.Max(x => x % 4));
        }

        [Fact]
        public void Min_and_max_with_a_non_numeric_selector_are_supported_on_every_target()
        {
            // The selector form of Min / Max is numeric-only before .NET 6; the engine answers that
            // with its own projection so that one model behaves the same on every target.
            Query().Min(x => "v" + x).ShouldBe(Numbers.Min(x => "v" + x));
            Query().Max(x => x.ToString()).ShouldBe(Numbers.Max(x => x.ToString()));
        }

        [Fact]
        public void An_operator_outside_the_subset_is_rejected_by_name()
        {
            var query = Query().SelectMany(x => new[] { x, x });

            var exception = Should.Throw<NotSupportedException>(() => query.ToArray());
            exception.Message.ShouldContain("SelectMany");
            exception.Message.ShouldContain("AsEnumerable");
        }

        [Fact]
        public void The_comparer_overload_of_groupby_is_rejected()
        {
            var query = Query().GroupBy(x => x % 2, EqualityComparer<int>.Default);

            Should.Throw<NotSupportedException>(() => query.ToArray()).Message.ShouldContain("comparer");
        }

        [Fact]
        public void A_groupby_result_selector_is_rejected()
        {
            var query = Query().GroupBy(x => x % 2, (key, group) => key + group.Count());

            Should.Throw<NotSupportedException>(() => query.ToArray()).Message.ShouldContain("result selector");
        }

        [Fact]
        public void The_same_name_operators_bind_to_the_engine_on_a_queryable_receiver()
        {
            IQueryable<int> query = Query();

            query.DistinctBy(x => x + 1).Expression.ShouldBeAssignableTo<MethodCallExpression>()
                .Method.DeclaringType.ShouldBe(typeof(CollectionExtensions));
            query.Order().Expression.ShouldBeAssignableTo<MethodCallExpression>()
                .Method.DeclaringType.ShouldBe(typeof(CollectionExtensions));
            query.CountBy(x => x + 1).Expression.ShouldBeAssignableTo<MethodCallExpression>()
                .Method.DeclaringType.ShouldBe(typeof(CollectionExtensions));
        }

        [Fact]
        public void The_same_name_operators_still_bind_to_the_bcl_on_a_sequence_receiver()
        {
            var sequence = Numbers.ToList();

            var result = sequence.DistinctBy(x => x + 1);

            result.ShouldBeAssignableTo<IEnumerable<int>>();
            (result is IQueryable<int>).ShouldBeFalse();
        }

        [Fact]
        public void A_foreign_queryable_can_be_translated_without_being_enumerated()
        {
            var model = Numbers.AsQueryable().Where(x => x > 3).ToQueryModel();

            model.Stages.Single().Kind.ShouldBe(QueryStageKind.Filter);
            model.SourceType.ShouldBe(typeof(int));
            model.Describe().ShouldContain("from Int32");
        }

        [Fact]
        public void A_null_source_or_selector_is_rejected()
        {
            IQueryable<int> query = null!;
            IQueryable<int> source = Query();

            Should.Throw<ArgumentNullException>(() => query.AsCollectionQueryable());
            Should.Throw<ArgumentNullException>(() => ((IEnumerable<int>)null!).AsCollectionQueryable());
            Should.Throw<ArgumentNullException>(() => source.DistinctBy<int, int>(null!));
            Should.Throw<ArgumentNullException>(() => source.CountBy<int, int>(null!));
            Should.Throw<ArgumentNullException>(() => ((IQueryable<int>)null!).ToQueryModel());
        }

        [Fact]
        public void Entering_the_query_layer_twice_is_idempotent()
        {
            var once = Query();
            var twice = once.AsCollectionQueryable();

            twice.ShouldBeSameAs(once);
        }
    }
}
