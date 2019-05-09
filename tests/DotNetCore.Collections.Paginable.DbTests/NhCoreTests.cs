using DotNetCore.Collections.Paginable.DbTests.Models;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Tool.hbm2ddl;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.DbTests
{
    public class NhCoreTests
    {
        internal static readonly string ConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=D:\Development\Collections\tests\DotNetCore.Collections.Paginable.DbTests\DataSource\Samples.mdf;Integrated Security=True";

        [Fact]
        public void GetPageTest()
        {
            using (var session = NhHelper.OpenSession())
            {
                var page = session.QueryOver<Int32Sample>().GetPage(1, 9);
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
        public void ToPaginableTest()
        {
            using (var session = NhHelper.OpenSession())
            {
                var list = session.QueryOver<Int32Sample>().ToPaginable<Int32Sample>(9);
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
    }

    public class Int32SampleNhMap : ClassMap<Int32Sample>
    {
        public Int32SampleNhMap()
        {
            base.Id(x => x.Id);
            base.Table("Int32Samples");
        }
    }

    public static class NhHelper
    {
        private static ISessionFactory _sessionFactory;

        private static ISessionFactory SessionFactory {
            get {
                if (_sessionFactory == null)
                    InitializeSessionFactory();
                return _sessionFactory;
            }
        }

        private static void InitializeSessionFactory()
        {
            _sessionFactory =
                Fluently.Configure()
                    .Database(MsSqlConfiguration.MsSql2012.ConnectionString(NhCoreTests.ConnectionString))
                    .Mappings(m => m.FluentMappings.Add<Int32SampleNhMap>())
                    .ExposeConfiguration(cfg => new SchemaExport(cfg).Create(true, false))
                    .BuildSessionFactory();
        }

        public static ISession OpenSession() => SessionFactory.OpenSession();
    }
}
