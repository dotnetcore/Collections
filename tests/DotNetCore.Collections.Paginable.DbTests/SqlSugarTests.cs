using System;
using System.Collections.Generic;
using System.Text;
using DotNetCore.Collections.Paginable.DbTests.Models;
using Shouldly;
using SqlSugar;
using Xunit;

namespace DotNetCore.Collections.Paginable.DbTests
{
    public class SqlSugarTests
    {
        private readonly string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=D:\Development\Collections\tests\DotNetCore.Collections.Paginable.DbTests\DataSource\Samples.mdf;Integrated Security=True";

        private readonly SqlSugarClient _sqlSugar;

        public SqlSugarTests()
        {
            _sqlSugar = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = connectionString,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true
            });

            _sqlSugar.MappingTables.Add("Int32Sample", "Int32Samples");
        }

        [Fact]
        public void Test1()
        {
            var page = _sqlSugar.Queryable<Int32Sample>().GetPage(1, 9);
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
