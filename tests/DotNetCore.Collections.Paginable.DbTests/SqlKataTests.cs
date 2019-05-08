using System.Data.SqlClient;
using SqlKata.Compilers;
using SqlKata.Execution;
using Xunit;

namespace DotNetCore.Collections.Paginable.DbTests
{
    public class UnitTest1
    {
        private readonly string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=D:\Development\Collections\tests\DotNetCore.Collections.Paginable.DbTests\DataSource\Samples.mdf;Integrated Security=True";

        [Fact]
        public void Test1()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                var compiler = new SqlServerCompiler();
                var db = new QueryFactory(connection, compiler);

                var page = db.Query("Int32Samples").GetPage<Int32Sample>(1, 9);
                Assert.Equal(24, page.TotalPageCount);
                Assert.Equal(210, page.TotalMemberCount);
                Assert.Equal(1, page.CurrentPageNumber);
                Assert.Equal(9, page.PageSize);
                Assert.Equal(9, page.CurrentPageSize);
                Assert.True(page.HasNext);
                Assert.False(page.HasPrevious);

                connection.Close();
            }
        }
    }

    public class Int32Sample
    {
        public int Id { get; set; }
    }
}
