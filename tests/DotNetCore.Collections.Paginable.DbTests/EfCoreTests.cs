using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetCore.Collections.Paginable.DbTests.Models;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.DbTests
{
    public class EfCoreTests
    {
        internal static readonly string ConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=D:\Development\Collections\tests\DotNetCore.Collections.Paginable.DbTests\DataSource\Samples.mdf;Integrated Security=True";

        [Fact]
        public void Test1()
        {
            using (var db = new Int32DbContext())
            {
                var page = db.Int32Samples.Where(x => x.Id > 0).GetPage(1, 9);
                page.TotalPageCount.ShouldBe(24);
                page.TotalMemberCount.ShouldBe(210);
                page.CurrentPageNumber.ShouldBe(1);
                page.PageSize.ShouldBe(9);
                page.CurrentPageSize.ShouldBe(9);
                page.HasNext.ShouldBeTrue();
                page.HasPrevious.ShouldBeFalse();
            }
        }

        public class Int32DbContext : DbContext
        {
            public virtual DbSet<Int32Sample> Int32Samples { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseSqlServer(EfCoreTests.ConnectionString);
            }
        }
    }
}
