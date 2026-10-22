using System;
using System.Linq;
using System.Reflection;
using DotNetCore.Collections;
using DotNetCore.Collections.Paginable;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.OrmQueryTests
{
    /// <summary>
    /// F7-13: the seam every provider integration shares.
    /// </summary>
    /// <remarks>
    /// The nine provider integrations differ in the queryable they accept - a DbSet, an
    /// <c>ISugarQueryable</c>, an <c>ISelect</c>, an <c>IQueryOver</c>, a SqlKata <c>Query</c> - but
    /// they all end in <see cref="PaginableQueryable{T}"/>, whose only input is an
    /// <see cref="IQueryable{T}"/>. That is what a replayed model is, so the seam below is the whole
    /// of the integration for the providers that hand over an <see cref="IQueryable"/>; the others
    /// need the same replay this project's SqlSugar and FreeSql runs perform.
    /// </remarks>
    public class PaginableSeamTests
    {
        [Fact]
        public void A_replayed_query_pages_through_the_paginable_queryable_path()
        {
            var model = OrmFixture.Pipeline(OrmFixture.Rows.AsCollectionQueryable()).ToQueryModel();
            var replayed = (IQueryable<int>)model.AsQueryable(OrmFixture.Rows.AsQueryable());

            var paginable = replayed.ToPaginable(2);
            var page = paginable.GetPage(1);

            page.TotalMemberCount.ShouldBe(2);
            page.TotalPageCount.ShouldBe(1);
            page.CurrentPageSize.ShouldBe(2);
            page[0].Value.ShouldBe(3);
            page[1].Value.ShouldBe(4);
        }

        [Fact]
        public void The_paginable_queryable_path_takes_exactly_what_a_replayed_model_produces()
        {
            // The seam claim, pinned: every provider integration ends in a PaginableQueryable<T>, and
            // its only input is an IQueryable<T>. If that ever changed, an adapter would need a
            // provider-specific bridge and the model would stop being enough on its own.
            var constructors = typeof(PaginableQueryable<int>).GetConstructors();

            constructors.ShouldNotBeEmpty();
            foreach (var constructor in constructors)
            {
                var first = constructor.GetParameters()[0];

                first.ParameterType.ShouldBe(typeof(IQueryable<int>));
                first.ParameterType.IsAssignableFrom(typeof(IQueryable<int>)).ShouldBeTrue();
            }
        }
    }
}
