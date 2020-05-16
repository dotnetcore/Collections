using System;
using System.Linq;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable.DbTests.Models;
using FreeSql;
using NHibernate.Criterion;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.DbTests {
    public class FreeSqlTests {
        internal static readonly string ConnectionString =
            @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=D:\Development\Collections\tests\DotNetCore.Collections.Paginable.DbTests\DataSource\Samples.mdf;Integrated Security=True";

        private readonly IFreeSql _freeSql;

        public FreeSqlTests() {
            _freeSql = new FreeSql.FreeSqlBuilder()
                      .UseConnectionString(DataType.SqlServer, ConnectionString)
                      .UseAutoSyncStructure(false)
                      .Build();

            _freeSql.CodeFirst.ConfigEntity<Int32Sample>(t => t.Name("Int32Samples"));
        }

        [Fact]
        public void GetPageTest() {
            var page = _freeSql.Select<Int32Sample>().GetPage(1, 9);
            page.TotalPageCount.ShouldBe(24);
            page.TotalMemberCount.ShouldBe(210);
            page.CurrentPageNumber.ShouldBe(1);
            page.PageSize.ShouldBe(9);
            page.CurrentPageSize.ShouldBe(9);
            page.HasNext.ShouldBeTrue();
            page.HasPrevious.ShouldBeFalse();

            page[0].Value.Id.ShouldBe(1);
            page[1].Value.Id.ShouldBe(2);
            page[2].Value.Id.ShouldBe(3);
            page[3].Value.Id.ShouldBe(4);
            page[4].Value.Id.ShouldBe(5);
            page[5].Value.Id.ShouldBe(6);
            page[6].Value.Id.ShouldBe(7);
            page[7].Value.Id.ShouldBe(8);
            page[8].Value.Id.ShouldBe(9);
        }

        [Fact]
        public async Task GetPageAsyncTest() {
            var page = await _freeSql.Select<Int32Sample>().GetPageAsync(1, 9);
            page.TotalPageCount.ShouldBe(24);
            page.TotalMemberCount.ShouldBe(210);
            page.CurrentPageNumber.ShouldBe(1);
            page.PageSize.ShouldBe(9);
            page.CurrentPageSize.ShouldBe(9);
            page.HasNext.ShouldBeTrue();
            page.HasPrevious.ShouldBeFalse();

            page[0].Value.Id.ShouldBe(1);
            page[1].Value.Id.ShouldBe(2);
            page[2].Value.Id.ShouldBe(3);
            page[3].Value.Id.ShouldBe(4);
            page[4].Value.Id.ShouldBe(5);
            page[5].Value.Id.ShouldBe(6);
            page[6].Value.Id.ShouldBe(7);
            page[7].Value.Id.ShouldBe(8);
            page[8].Value.Id.ShouldBe(9);
        }

        [Fact]
        public void ToPaginableTest() {
            var list = _freeSql.Select<Int32Sample>().ToPaginable<Int32Sample>(9);
            var page = list.GetPage(2);
            page.TotalPageCount.ShouldBe(24);
            page.TotalMemberCount.ShouldBe(210);
            page.CurrentPageNumber.ShouldBe(2);
            page.PageSize.ShouldBe(9);
            page.CurrentPageSize.ShouldBe(9);
            page.HasNext.ShouldBeTrue();
            page.HasPrevious.ShouldBeTrue();

            page[0].Value.Id.ShouldBe(10);
            page[1].Value.Id.ShouldBe(11);
            page[2].Value.Id.ShouldBe(12);
            page[3].Value.Id.ShouldBe(13);
            page[4].Value.Id.ShouldBe(14);
            page[5].Value.Id.ShouldBe(15);
            page[6].Value.Id.ShouldBe(16);
            page[7].Value.Id.ShouldBe(17);
            page[8].Value.Id.ShouldBe(18);
        }

        [Fact]
        public void GetPageWithDbContextTest() {
            using (var ctx = _freeSql.CreateDbContext()) {
                var int32Samples = ctx.Set<Int32Sample>();

                var page = int32Samples.GetPage(1, 9);
                page.TotalPageCount.ShouldBe(24);
                page.TotalMemberCount.ShouldBe(210);
                page.CurrentPageNumber.ShouldBe(1);
                page.PageSize.ShouldBe(9);
                page.CurrentPageSize.ShouldBe(9);
                page.HasNext.ShouldBeTrue();
                page.HasPrevious.ShouldBeFalse();

                page[0].Value.Id.ShouldBe(1);
                page[1].Value.Id.ShouldBe(2);
                page[2].Value.Id.ShouldBe(3);
                page[3].Value.Id.ShouldBe(4);
                page[4].Value.Id.ShouldBe(5);
                page[5].Value.Id.ShouldBe(6);
                page[6].Value.Id.ShouldBe(7);
                page[7].Value.Id.ShouldBe(8);
                page[8].Value.Id.ShouldBe(9);
            }
        }

        [Fact]
        public void GetPageWithDbContextTest2() {
            using (var ctx = new Int32FreeSqlDbContext()) {
                var page = ctx.Int32Samples.GetPage(1, 9);
                page.TotalPageCount.ShouldBe(24);
                page.TotalMemberCount.ShouldBe(210);
                page.CurrentPageNumber.ShouldBe(1);
                page.PageSize.ShouldBe(9);
                page.CurrentPageSize.ShouldBe(9);
                page.HasNext.ShouldBeTrue();
                page.HasPrevious.ShouldBeFalse();

                page[0].Value.Id.ShouldBe(1);
                page[1].Value.Id.ShouldBe(2);
                page[2].Value.Id.ShouldBe(3);
                page[3].Value.Id.ShouldBe(4);
                page[4].Value.Id.ShouldBe(5);
                page[5].Value.Id.ShouldBe(6);
                page[6].Value.Id.ShouldBe(7);
                page[7].Value.Id.ShouldBe(8);
                page[8].Value.Id.ShouldBe(9);
            }
        }

        [Fact]
        public void GetPageTopOneTest() {
            var page = _freeSql.Select<Int32Sample>().Where(x => x.Id < 2).GetPage(1, 9);
            page.TotalPageCount.ShouldBe(1);
            page.TotalMemberCount.ShouldBe(1);
            page.CurrentPageNumber.ShouldBe(1);
            page.PageSize.ShouldBe(9);
            page.CurrentPageSize.ShouldBe(1);
            page.HasNext.ShouldBeFalse();
            page.HasPrevious.ShouldBeFalse();

            page.ToOriginalItems().Count().ShouldBe(1);
        }

    }

    public class Int32FreeSqlDbContext : FreeSql.DbContext {
        public DbSet<Int32Sample> Int32Samples { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder builder) {
            var _freeSql = new FreeSql.FreeSqlBuilder()
                          .UseConnectionString(DataType.SqlServer, FreeSqlTests.ConnectionString)
                          .UseAutoSyncStructure(false)
                          .Build();

            _freeSql.CodeFirst.ConfigEntity<Int32Sample>(t => t.Name("Int32Samples"));

            builder.UseFreeSql(_freeSql);
        }
    }
}