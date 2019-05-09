using Chloe.Entity;
using Chloe.Infrastructure;
using Chloe.SqlServer;
using DotNetCore.Collections.Paginable.DbTests.Models;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.DbTests
{
    public class ChloeTests
    {
        private readonly string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=D:\Development\Collections\tests\DotNetCore.Collections.Paginable.DbTests\DataSource\Samples.mdf;Integrated Security=True";

        public ChloeTests()
        {
            DbConfiguration.UseTypeBuilders(typeof(Int32SampleMap));
        }

        [Fact]
        public void Test1()
        {
            using (var db = new MsSqlContext(connectionString))
            {
                var page = db.Query<Int32Sample>().GetPage(1, 9);
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

    public class Int32SampleMap : EntityTypeBuilder<Int32Sample>
    {
        public Int32SampleMap()
        {
            this.MapTo("Int32Samples");
        }
    }
}