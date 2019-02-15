using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetCore.Collections.Paginable.ConsoleTests
{
    class Program
    {
        static void Main(string[] args)
        {
            List1Test();

            List2Test();

            List3Test();

            List4Test();

            Console.ReadKey();

        }

        private static void List1Test()
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

            var metadata01 = page01.GetMetadata();
            var metadata02 = page02.GetMetadata();

            Console.WriteLine(metadata01);
            Console.WriteLine(metadata02);
        }

        private static void List2Test()
        {
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

            var metadata11 = page11.GetMetadata();
            var metadata12 = page12.GetMetadata();

            Console.WriteLine(metadata11);
            Console.WriteLine(metadata12);
        }

        private static void List3Test()
        {
            var list1 = new List<int>();
            var paginableList1 = list1.ToPaginable(20);
            var page01 = paginableList1.GetPage(1);

            Console.WriteLine("===========e00==============");

            for (var i = 0; i < page01.CurrentPageSize; i++)
            {
                Console.WriteLine($"{page01[i].ItemNumber}:{page01[i].Value}");
            }

            var metadata01 = page01.GetMetadata();

            Console.WriteLine(metadata01);
        }

        private static void List4Test()
        {
            var list1 = new List<int>().AsQueryable();
            var paginableList1 = list1.ToPaginable(20);
            var page01 = paginableList1.GetPage(1);

            Console.WriteLine("===========q00==============");

            for (var i = 0; i < page01.CurrentPageSize; i++)
            {
                Console.WriteLine($"{page01[i].ItemNumber}:{page01[i].Value}");
            }

            var metadata01 = page01.GetMetadata();

            Console.WriteLine(metadata01);
        }
    }
}
