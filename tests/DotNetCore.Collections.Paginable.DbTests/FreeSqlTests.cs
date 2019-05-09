using DotNetCore.Collections.Paginable.DbTests.Models;
using FreeSql;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.DbTests
{
    public class FreeSqlTests
    {
        private readonly string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=D:\Development\Collections\tests\DotNetCore.Collections.Paginable.DbTests\DataSource\Samples.mdf;Integrated Security=True";

        private readonly IFreeSql _freeSql;

        public FreeSqlTests()
        {
            _freeSql = new FreeSql.FreeSqlBuilder()
                .UseConnectionString(DataType.SqlServer, connectionString)
                .UseAutoSyncStructure(false)
                .Build();

            _freeSql.CodeFirst.ConfigEntity<Int32Sample>(t => t.Name("Int32Samples"));
        }

        [Fact]
        public void Test1()
        {
            var page = _freeSql.Select<Int32Sample>().GetPage(1, 9);
            page.TotalPageCount.ShouldBe(24);
            page.TotalMemberCount.ShouldBe(210);
            page.CurrentPageNumber.ShouldBe(1);
            page.PageSize.ShouldBe(9);
            page.CurrentPageSize.ShouldBe(9);
            page.HasNext.ShouldBeTrue();
            page.HasPrevious.ShouldBeFalse();
        }
    }
}
