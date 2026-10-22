using System;
using System.Linq;
using DotNetCore.Collections;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.OrmQueryTests
{
    /// <summary>
    /// F7-13: the model replayed onto EF Core produces the same SQL as the hand-written query.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every case is written once, against <see cref="IQueryable{T}"/> only, and then run twice:
    /// once natively on the DbSet, which is the oracle, and once through the engine's face and back
    /// out of <see cref="QueryModel.AsQueryable"/>. Comparing the two renderings is what turns "the
    /// model keeps the query" from an assertion into evidence - a stage rebuilt with the wrong
    /// operator, the wrong type argument, a lost ordering key or a reordered pipeline changes the
    /// SQL.
    /// </para>
    /// <para>
    /// No database is contacted. EF Core produces SQL from its own compilation step, so
    /// <c>ToQueryString</c> works against a connection string that names a server that does not
    /// exist. That is deliberate: the suite has to run in a build gate that has no database.
    /// </para>
    /// </remarks>
    public class EntityFrameworkCoreSqlTests
    {
        private static SampleContext SqlContext() =>
            new(new DbContextOptionsBuilder<SampleContext>()
                .UseSqlServer(OrmFixture.UnreachableSqlServer)
                .Options);

        /// <summary>The oracle: the query as a hand-written EF Core query.</summary>
        private static string NativeSql<TResult>(Func<IQueryable<Sample>, IQueryable<TResult>> build)
        {
            using var context = SqlContext();
            return OrmFixture.Normalise(build(context.Samples).ToQueryString());
        }

        /// <summary>The subject: the same query, translated to a model and replayed.</summary>
        private static string ReplayedSql<TResult>(Func<IQueryable<Sample>, IQueryable<TResult>> build)
        {
            using var context = SqlContext();
            var model = build(OrmFixture.Rows.AsCollectionQueryable()).ToQueryModel();
            return OrmFixture.Normalise(model.AsQueryable(context.Samples).ToQueryString());
        }

        private static void ShouldTranslateToTheSameSql<TResult>(
            Func<IQueryable<Sample>, IQueryable<TResult>> build) =>
            ReplayedSql(build).ShouldBe(NativeSql(build));

        [Fact]
        public void Filter_and_projection_translate_to_the_same_sql()
        {
            ShouldTranslateToTheSameSql(
                source => source.Where(x => x.Amount > 2).Select(x => new { x.Id, x.Name }));
        }

        [Fact]
        public void Ordering_with_ThenBy_translates_to_the_same_sql()
        {
            ShouldTranslateToTheSameSql(
                source => source.OrderBy(x => x.Name).ThenByDescending(x => x.Amount));
        }

        [Fact]
        public void Paging_translates_to_the_same_sql()
        {
            ShouldTranslateToTheSameSql(
                source => source.OrderBy(x => x.Id).Skip(1).Take(2));
        }

        [Fact]
        public void Grouping_translates_to_the_same_sql()
        {
            ShouldTranslateToTheSameSql(
                source => source.GroupBy(x => x.Amount % 2)
                    .Select(group => new { Key = group.Key, Count = group.Count() }));
        }

        [Fact]
        public void A_full_pipeline_translates_to_the_same_sql()
        {
            ShouldTranslateToTheSameSql(OrmFixture.Pipeline);
        }

        [Fact]
        public void A_keyed_operator_is_refused_before_any_sql_is_produced()
        {
            var model = OrmFixture.Rows.AsCollectionQueryable()
                .Where(x => x.Amount > 1)
                .DistinctBy(x => x.Name)
                .ToQueryModel();

            using var context = SqlContext();

            Should.Throw<NotSupportedException>(() => model.AsQueryable(context.Samples))
                .Message.ShouldContain("DistinctBy");
        }

        [Fact]
        public void The_replayed_query_returns_the_same_rows_as_the_native_one()
        {
            // The InMemory provider executes but produces no SQL, which makes it the right oracle for
            // the part the SQL comparison cannot see: that the replayed tree still means the same
            // thing. It is a real EF Core provider, so the comparison is against EF's own execution.
            var options = new DbContextOptionsBuilder<SampleContext>()
                .UseInMemoryDatabase("f713-oracle-" + Guid.NewGuid().ToString("N"))
                .Options;

            using var context = new SampleContext(options);
            context.Samples.AddRange(OrmFixture.Rows);
            context.SaveChanges();

            var model = OrmFixture.Pipeline(OrmFixture.Rows.AsCollectionQueryable()).ToQueryModel();

            var replayed = ((IQueryable<int>)model.AsQueryable(context.Samples)).ToArray();
            var native = OrmFixture.Pipeline(context.Samples).ToArray();

            replayed.ShouldBe(native);
            replayed.ShouldBe(new[] { 3, 4 });
        }
    }
}
