using System;
using System.Linq;

namespace DotNetCore.Collections.Paginable.ConsoleTests
{
    class Program
    {
        static void Main(string[] args)
        {
            var list1 = Enumerable.Range(0, 100);
            var paginableList1 = list1.ToPaginable(20);
            var page01 = paginableList1.GetPage(1);
            var page02 = paginableList1.GetPage(2);

            Console.WriteLine("===========e01==============");

            for (var i = 0; i < page01.CurrentPageSize; i++)
            {
                Console.WriteLine($"{page01[i].ItemNumber}:{page01[i].Value}");
            }

            Console.WriteLine("===========e02==============");

            for (var i = 0; i < page02.CurrentPageSize; i++)
            {
                Console.WriteLine($"{page02[i].ItemNumber}:{page02[i].Value}");
            }


            var list2 = Enumerable.Range(0, 100).AsQueryable();
            var paginableList2 = list2.ToPaginable(20);
            var page11 = paginableList2.GetPage(1);
            var page12 = paginableList2.GetPage(2);


            Console.WriteLine("===========q01==============");

            for (var i = 0; i < page11.CurrentPageSize; i++)
            {
                Console.WriteLine($"{page11[i].ItemNumber}:{page11[i].Value}");
            }


            Console.WriteLine("===========q02==============");

            for (var i = 0; i < page12.CurrentPageSize; i++)
            {
                Console.WriteLine($"{page12[i].ItemNumber}:{page12[i].Value}");
            }


            Console.ReadKey();

        }
    }
}
