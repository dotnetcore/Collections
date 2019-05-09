using System.Data.SqlClient;
using DotNetCore.Collections.Paginable.DbTests.Models;
using Shouldly;
using SqlKata.Compilers;
using SqlKata.Execution;
using Xunit;

namespace DotNetCore.Collections.Paginable.DbTests
{
    public class SqlKataTests
    {
        private readonly string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=D:\Development\Collections\tests\DotNetCore.Collections.Paginable.DbTests\DataSource\Samples.mdf;Integrated Security=True";

        [Fact]
        public void GetPageTest()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var compiler = new SqlServerCompiler();
                var db = new QueryFactory(connection, compiler);

                var page = db.Query("Int32Samples").GetPage<Int32Sample>(1, 9);
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

                connection.Close();
            }
        }

        [Fact]
        public void ToPaginableTest()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var compiler = new SqlServerCompiler();
                var db = new QueryFactory(connection, compiler);

                var list = db.Query("Int32Samples").ToPaginable<Int32Sample>(9);
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

                connection.Close();
            }
        }
    }


}
