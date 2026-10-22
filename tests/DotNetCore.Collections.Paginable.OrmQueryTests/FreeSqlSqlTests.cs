using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using DotNetCore.Collections;
using FreeSql;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.OrmQueryTests
{
    /// <summary>
    /// F7-13: FreeSql, driven through its own query API rather than through <c>IQueryable</c>.
    /// </summary>
    /// <remarks>
    /// Same shape as the SqlSugar run: FreeSql's receiver is <c>ISelect&lt;T&gt;</c>, so an adapter
    /// walks the model's stages and calls the native operator. Ordering is the one place where the
    /// replay needs dispatch rather than a direct call, because FreeSql's <c>OrderBy</c> is generic in
    /// the key type and a model carries the key selector as a <see cref="LambdaExpression"/>; a real
    /// adapter would close that generic with the key type the same way.
    /// </remarks>
    public class FreeSqlSqlTests
    {
        private static readonly MethodInfo OrderByMethod =
            Operator(nameof(ISelect<object>.OrderBy));

        private static readonly MethodInfo OrderByDescendingMethod =
            Operator(nameof(ISelect<object>.OrderByDescending));

        private static IFreeSql Db() => new FreeSqlBuilder()
            .UseConnectionString(DataType.SqlServer, OrmFixture.UnreachableSqlServer)
            .UseAutoSyncStructure(false)
            .Build();

        private static MethodInfo Operator(string name) =>
            typeof(ISelect<Sample>).GetMethods()
                .Single(method => method.Name == name && method.IsGenericMethod
                    && method.GetParameters().Length == 1);

        /// <summary>The whole of an adapter's job: replay the model's stages onto the native API.</summary>
        private static ISelect<Sample> Replay(ISelect<Sample> select, QueryModel model)
        {
            foreach (var stage in model.Stages)
            {
                switch (stage.Kind)
                {
                    case QueryStageKind.Filter:
                        select = select.Where((Expression<Func<Sample, bool>>)((FilterStage)stage).Predicate);
                        break;

                    case QueryStageKind.Order:
                    {
                        var order = (OrderStage)stage;
                        if (order.Orderings.Count != 1)
                        {
                            throw new NotSupportedException(
                                "This replay covers one ordering key; the secondary keys of a ThenBy "
                                + "have no separate FreeSql call.");
                        }

                        var ordering = order.Orderings[0];
                        var method = (ordering.Descending ? OrderByDescendingMethod : OrderByMethod)
                            .MakeGenericMethod(ordering.KeySelector.ReturnType);

                        select = (ISelect<Sample>)method.Invoke(select, new object[] { ordering.KeySelector });
                        break;
                    }

                    case QueryStageKind.Skip:
                        select = select.Skip(((PagingStage)stage).Count);
                        break;

                    case QueryStageKind.Take:
                        select = select.Take(((PagingStage)stage).Count);
                        break;

                    default:
                        throw new NotSupportedException(
                            "The stage '" + stage.OperatorName + "' has no FreeSql replay in this suite.");
                }
            }

            return select;
        }

        [Fact]
        public void Filter_ordering_and_paging_translate_to_the_same_sql()
        {
            using var db = Db();

            var native = db.Select<Sample>()
                .Where(x => x.Amount > 2)
                .OrderBy(x => x.Name)
                .Skip(1)
                .Take(2)
                .ToSql();

            var model = OrmFixture.Rows.AsCollectionQueryable()
                .Where(x => x.Amount > 2)
                .OrderBy(x => x.Name)
                .Skip(1)
                .Take(2)
                .ToQueryModel();

            var replayed = Replay(db.Select<Sample>(), model).ToSql();

            OrmFixture.Normalise(replayed).ShouldBe(OrmFixture.Normalise(native));
        }

        [Fact]
        public void A_descending_ordering_translates_to_the_same_sql()
        {
            using var db = Db();

            var native = db.Select<Sample>()
                .Where(x => x.Amount > 2)
                .OrderByDescending(x => x.Name)
                .ToSql();

            var model = OrmFixture.Rows.AsCollectionQueryable()
                .Where(x => x.Amount > 2)
                .OrderByDescending(x => x.Name)
                .ToQueryModel();

            var replayed = Replay(db.Select<Sample>(), model).ToSql();

            OrmFixture.Normalise(replayed).ShouldBe(OrmFixture.Normalise(native));
        }

        [Fact]
        public void A_keyed_operator_has_no_native_replay_and_is_refused_by_name()
        {
            using var db = Db();

            var model = OrmFixture.Rows.AsCollectionQueryable()
                .CountBy(x => x.Name)
                .ToQueryModel();

            Should.Throw<NotSupportedException>(() => Replay(db.Select<Sample>(), model))
                .Message.ShouldContain("CountBy");
        }
    }
}
