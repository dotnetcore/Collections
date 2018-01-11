using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections.Paginable;
using Microsoft.EntityFrameworkCore;

namespace Sample.EfCore
{
    class Program
    {
        static void Main(string[] args)
        {
            //注意，本示例仅用于演示 api 如何使用，实际上并不能运行
            //Note: Such example is only used to demonstrate how the apis can be used, actually it cannot work.

            //...

            using (var ctx = new DemoContext())
            {
                var a = ctx.DemoModels.GetPage(5, 20);
                var b = ctx.DemoModels.Where(x => x.IsValid).GetPage(2, 20);
                var c = ctx.DemoModels.Include(l => l.Items).GetPage(5, 20);
                var d = ctx.DemoModels.Include(l => l.Items).Where(x => x.IsValid).GetPage(2, 20);

                foreach (var itemOfA in a)
                {
                    var indexInDb = itemOfA.ItemNumber;
                    var indexInResult = itemOfA.Offset;
                    var actuallyValue = itemOfA.Value; // an instance of type DemoModel.
                }

                foreach (var itemOfC in c.ToOrigonItems()) // to get a list of DemoModel you have requested
                {
                    //now, itemOfC is an instance of type DemoModel.
                }
            }

            using (var ctx = new DemoContext())
            {
                var e = ctx.DemoModels.ToPaginable(20);

                var howManyItemsInDb = e.MemberCount;
                var howManyPagesYouCanGet = e.PageCount;
                var howManyItemsDoYouWantPerPage = e.PageSize;

                var f = e.GetPage(2); //get second page (which index is 1)

                var realPageNumberYouHaveRequested = f.CurrentPageNumber;
                var countOfPageYouWantToGet = f.PageSize;
                var realCountOfItemsYouHaveGot = f.CurrentPageSize; // also means total member count in such page

                var realCountOfItemsInDb = f.TotalMemberCount;
                var realCountOfPagesYouCanGet = f.TotalPageCount;

                var originResults = f.ToOrigonItems(); // Get all items you have requested.

                foreach (var originItem in originResults)
                {
                    //...
                }

                foreach (var itemInPage in f)
                {
                    var indexInDb = itemInPage.ItemNumber;
                    var indexInResult = itemInPage.Offset;
                    var actuallyValue = itemInPage.Value; // an instance of type DemoModel.
                }

            }


            using (var ctx = new DemoContext())
            {
                var g = ctx.DemoModels.ToPaginable(20, 1000);
                // it means that, there are 20 item in each page, and only query the first 1000 records
            }
        }

        public class DemoContext : DbContext
        {
            //...

            public virtual DbSet<DemoModel> DemoModels { get; set; }
            public virtual DbSet<DemoModelItem> DemoModelItems { get; set; }
        }

        public class DemoModel
        {
            public Guid Id { get; set; }
            public List<DemoModelItem> Items { get; set; }
            public bool IsValid { get; set; }
        }

        public class DemoModelItem
        {
            public Guid Id { get; set; }
            public Guid FatherId { get; set; }
            public string Name { get; set; }
        }
    }
}
