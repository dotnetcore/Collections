using System;
using System.Diagnostics;
using System.Linq;
using DotNetCore.Collections.Paginable;
using ABPaginableCollections = PaginableCollections;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var list = Enumerable.Range(0, 10000000);
            Console.WriteLine($"origin list length = {list.Count()}");

            #region DotNetCore.Collection.Paginable - Enumerable

            Console.WriteLine("DotNetCore.Collection.Paginable - Enumerable");

            Stopwatch sw = new Stopwatch();
            sw.Start();

            var paginableList = list.ToPaginable(50);
            Console.WriteLine($"page size = {paginableList.PageSize}");
            Console.WriteLine($"total pages = {paginableList.PageCount}");

            var page = paginableList.GetPage(15);
            Console.WriteLine($"from member line #{page.FromMemberNumber()}");
            Console.WriteLine($"  to member line #{page.ToMemberNumber()}");
            Console.WriteLine($"Has Previous Page? = {page.HasPrevious}");
            Console.WriteLine($"Has Next Page?     = {page.HasNext}");

            for (var i = 0; i < page.CurrentPageSize; i++)
            {
                Console.Write($"{page[i].ItemNumber}:{page[i].Value}   ");
                if (i % 10 == 9)
                {
                    Console.WriteLine();
                }
            }
            sw.Stop();
            Console.WriteLine($"Cost {sw.ElapsedMilliseconds} milliseconds");
            Console.WriteLine("=====================================");

            #endregion

            #region DotNetCore.Collection.Paginable - Queryable

            Console.WriteLine("DotNetCore.Collection.Paginable - Queryable");

            sw.Restart();

            var paginableQuery = list.AsQueryable().ToPaginable(50);

            Console.WriteLine($"page size = {paginableQuery.PageSize}");
            Console.WriteLine($"total pages = {paginableQuery.PageCount}");

            var page2 = paginableQuery.GetPage(15);
            Console.WriteLine($"from member line #{page2.FromMemberNumber()}");
            Console.WriteLine($"  to member line #{page2.ToMemberNumber()}");
            Console.WriteLine($"Has Previous Page? = {page2.HasPrevious}");
            Console.WriteLine($"Has Next Page?     = {page2.HasNext}");

            for (var i = 0; i < page2.CurrentPageSize; i++)
            {
                Console.Write($"{page2[i].ItemNumber}:{page2[i].Value}   ");
                if (i % 10 == 9)
                {
                    Console.WriteLine();
                }
            }

            sw.Stop();
            Console.WriteLine($"Cost {sw.ElapsedMilliseconds} milliseconds");
            Console.WriteLine("=====================================");

            #endregion

            #region DotNetCore.Collection.Paginable - Enumerable - GetPage

            Console.WriteLine("DotNetCore.Collection.Paginable - Enumerable - GetPage");

            sw.Restart();

            var page101 = list.GetPage(15, 50);

            Console.WriteLine($"Cost {sw.ElapsedMilliseconds} milliseconds for paging page");
            Console.WriteLine("=====================================");
            
            Console.WriteLine($"from member line #{page101.FromMemberNumber()}");
            Console.WriteLine($"  to member line #{page101.ToMemberNumber()}");
            Console.WriteLine($"Has Previous Page? = {page101.HasPrevious}");
            Console.WriteLine($"Has Next Page?     = {page101.HasNext}");
            
            for (var i = 0; i < page101.CurrentPageSize; i++)
            {
                Console.Write($"{page101[i].ItemNumber}:{page101[i].Value}   ");
                if (i % 10 == 9)
                {
                    Console.WriteLine();
                }
            }
            sw.Stop();
            Console.WriteLine($"Cost {sw.ElapsedMilliseconds} milliseconds");
            Console.WriteLine("=====================================");

            #endregion

            #region DotNetCore.Collection.Paginable - Queryable - GetPage

            Console.WriteLine("DotNetCore.Collection.Paginable - Queryable - GetPage");

            sw.Restart();

            var page102 = list.AsQueryable().GetPage(15, 50);

            Console.WriteLine($"from member line #{page102.FromMemberNumber()}");
            Console.WriteLine($"  to member line #{page102.ToMemberNumber()}");
            Console.WriteLine($"Has Previous Page? = {page102.HasPrevious}");
            Console.WriteLine($"Has Next Page?     = {page102.HasNext}");

            for (var i = 0; i < page102.CurrentPageSize; i++)
            {
                Console.Write($"{page102[i].ItemNumber}:{page102[i].Value}   ");
                if (i % 10 == 9)
                {
                    Console.WriteLine();
                }
            }
            sw.Stop();
            Console.WriteLine($"Cost {sw.ElapsedMilliseconds} milliseconds");
            Console.WriteLine("=====================================");

            #endregion

            #region PaginableCollection - Enumerable

            Console.WriteLine("PaginableCollection - Enumerable");

            sw.Restart();

            var paginable3 = ABPaginableCollections.EnumerableExtensions.ToPaginable(list, 15, 50);
            Console.WriteLine($"page size = {paginable3.ItemCountPerPage}");
            Console.WriteLine($"total pages = {paginable3.TotalPageCount}");

            Console.WriteLine($"from member line #{paginable3.FirstItemNumber}");
            Console.WriteLine($"  to member line #{paginable3.LastItemNumber}");
            Console.WriteLine($"Has Previous Page? = {paginable3.HasPreviousPage}");
            Console.WriteLine($"Has Next Page?     = {paginable3.HasNextPage}");

            for (var i = 0; i < paginable3.ItemCountPerPage; i++)
            {
                Console.Write($"{paginable3[i].ItemNumber}:{paginable3[i].Item}   ");
                if (i % 10 == 9)
                {
                    Console.WriteLine();
                }
            }

            sw.Stop();
            Console.WriteLine($"Cost {sw.ElapsedMilliseconds} milliseconds");
            Console.WriteLine("=====================================");

            #endregion

            #region PaginableCollection - Queryable

            Console.WriteLine("PaginableCollection - Queryable");

            sw.Restart();

            var queryable2 = list.AsQueryable();

            var paginable4 = ABPaginableCollections.PaginableExtensions.ToPaginable(queryable2, 15, 50);
            Console.WriteLine($"page size = {paginable4.ItemCountPerPage}");
            Console.WriteLine($"total pages = {paginable4.TotalPageCount}");

            Console.WriteLine($"from member line #{paginable4.FirstItemNumber}");
            Console.WriteLine($"  to member line #{paginable4.LastItemNumber}");
            Console.WriteLine($"Has Previous Page? = {paginable4.HasPreviousPage}");
            Console.WriteLine($"Has Next Page?     = {paginable4.HasNextPage}");

            for (var i = 0; i < paginable4.ItemCountPerPage; i++)
            {
                Console.Write($"{paginable4[i].ItemNumber}:{paginable4[i].Item}   ");
                if (i % 10 == 9)
                {
                    Console.WriteLine();
                }
            }

            sw.Stop();
            Console.WriteLine($"Cost {sw.ElapsedMilliseconds} milliseconds");
            Console.WriteLine("=====================================");

            #endregion

            Console.ReadKey();
        }
    }
}