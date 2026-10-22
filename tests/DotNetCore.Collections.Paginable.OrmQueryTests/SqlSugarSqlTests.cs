using System;
using System.Linq;
using System.Linq.Expressions;
using DotNetCore.Collections;
using Shouldly;
using SqlSugar;
using Xunit;

namespace DotNetCore.Collections.Paginable.OrmQueryTests
{
    /// <summary>
    /// F7-13: SqlSugar, driven through its own query API rather than through <c>IQueryable</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// SqlSugar's receiver is <c>ISugarQueryable&lt;T&gt;</c>, so the model cannot be handed to
    /// <see cref="QueryModel.AsQueryable"/> - that method needs an <see cref="IQueryable"/>. What the
    /// model does give an adapter is the stages, one lambda each, and the run below is the whole of
    /// what an adapter has to do with them: walk the stages in order and call the native operator.
    /// </para>
    /// <para>
    /// The subset replayed here is the one whose element type never changes (filter, ordering,
    /// paging). Projection and grouping would need a type-varying replay - the queryable's element
    /// type changes with each stage - which this suite deliberately leaves out; EF Core covers those
    /// two against a provider that does take <see cref="IQueryable"/>.
    /// </para>
    /// </remarks>
    public class SqlSugarSqlTests
    {
        private static SqlSugarClient Client() => new(new ConnectionConfig
        {
            ConnectionString = OrmFixture.UnreachableSqlServer,
            DbType = DbType.SqlServer,
            IsAutoCloseConnection = true,
        });

        /// <summary>The whole of an adapter's job: replay the model's stages onto the native API.</summary>
        private static ISugarQueryable<Sample> Replay(ISugarQueryable<Sample> query, QueryModel model)
        {
            foreach (var stage in model.Stages)
            {
                switch (stage.Kind)
                {
                    case QueryStageKind.Filter:
                        query = query.Where((Expression<Func<Sample, bool>>)((FilterStage)stage).Predicate);
                        break;

                    case QueryStageKind.Order:
                    {
                        var order = (OrderStage)stage;
                        if (order.Orderings.Count != 1)
                        {
                            throw new NotSupportedException(
                                "This replay covers one ordering key; the secondary keys of a ThenBy "
                                + "have no separate SqlSugar call.");
                        }

                        var key = OrmFixture.AsObjectKey(order.Orderings[0].KeySelector);
                        query = order.Orderings[0].Descending
                            ? query.OrderBy(key, OrderByType.Desc)
                            : query.OrderBy(key);
                        break;
                    }

                    case QueryStageKind.Skip:
                        query = query.Skip(((PagingStage)stage).Count);
                        break;

                    case QueryStageKind.Take:
                        query = query.Take(((PagingStage)stage).Count);
                        break;

                    default:
                        throw new NotSupportedException(
                            "The stage '" + stage.OperatorName + "' has no SqlSugar replay in this suite.");
                }
            }

            return query;
        }

        [Fact]
        public void Filter_ordering_and_paging_translate_to_the_same_sql()
        {
            var native = Client()
                .Queryable<Sample>()
                .Where(x => x.Amount > 2)
                .OrderBy(x => x.Name)
                .Skip(1)
                .Take(2)
                .ToSql().Key;

            var model = OrmFixture.Rows.AsCollectionQueryable()
                .Where(x => x.Amount > 2)
                .OrderBy(x => x.Name)
                .Skip(1)
                .Take(2)
                .ToQueryModel();

            var replayed = Replay(Client().Queryable<Sample>(), model).ToSql().Key;

            OrmFixture.Normalise(replayed).ShouldBe(OrmFixture.Normalise(native));
        }

        [Fact]
        public void A_descending_ordering_translates_to_the_same_sql()
        {
            var native = Client()
                .Queryable<Sample>()
                .Where(x => x.Amount > 2)
                .OrderBy(x => x.Name, OrderByType.Desc)
                .ToSql().Key;

            var model = OrmFixture.Rows.AsCollectionQueryable()
                .Where(x => x.Amount > 2)
                .OrderByDescending(x => x.Name)
                .ToQueryModel();

            var replayed = Replay(Client().Queryable<Sample>(), model).ToSql().Key;

            OrmFixture.Normalise(replayed).ShouldBe(OrmFixture.Normalise(native));
        }

        [Fact]
        public void A_keyed_operator_has_no_native_replay_and_is_refused_by_name()
        {
            var model = OrmFixture.Rows.AsCollectionQueryable()
                .DistinctBy(x => x.Name)
                .ToQueryModel();

            Should.Throw<NotSupportedException>(() => Replay(Client().Queryable<Sample>(), model))
                .Message.ShouldContain("DistinctBy");
        }
    }
}
