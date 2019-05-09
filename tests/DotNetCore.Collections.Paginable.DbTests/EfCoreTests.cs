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
