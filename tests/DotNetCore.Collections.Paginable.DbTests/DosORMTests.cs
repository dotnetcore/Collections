using System;
using System.Collections.Generic;
using System.Text;
using Dos.ORM;
using DotNetCore.Collections.Paginable.DbTests.Models;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.DbTests
{
    public class DosORMTests
    {
        private readonly string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=D:\Development\Collections\tests\DotNetCore.Collections.Paginable.DbTests\DataSource\Samples.mdf;Integrated Security=True";

        private readonly DbSession _dosOrmSession;

        public DosORMTests()
        {
            _dosOrmSession = new DbSession(DatabaseType.SqlServer, connectionString);
        }

        [Fact]
        public void GetPageTest()
        {
            var page = _dosOrmSession.From<Int32Sample4DosORM>().GetPage(1, 9);
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
        public void ToPaginableTest()
        {
            var list = _dosOrmSession.From<Int32Sample4DosORM>().ToPaginable(9);
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
    }


    public class Int32Sample4DosORM : Entity
    {
        public Int32Sample4DosORM() : base("Int32Samples") { }

        public virtual int Id { get; set; }

        public override Field[] GetPrimaryKeyFields() => new Field[] { new Field("Id"), };
    }
}
