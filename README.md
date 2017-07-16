# Collections
Utilities and extensions for Collections.

# Usage

```
var list = Enumerable.Range(0, 10000);

var paginableList = list.ToPaginable(50);   //Get a collection of Page, each page has 50 PageMembers
var page = paginableList.GetPage(15);       //Get page 15th

for (var i = 0; i < page.CurrentPageSize; i++)
{
    Console.Write($"{page[i].ItemNumber}:{page[i].Value}   ");
    if (i % 10 == 9)
    {
        Console.WriteLine();
    }
}
```

or

```
var list = Enumerable.Range(0, 10000);

var page = list.GetPage(15, 50);

for (var i = 0; i < page.CurrentPageSize; i++)
{
    Console.Write($"{page[i].ItemNumber}:{page[i].Value}   ");
    if (i % 10 == 9)
    {
        Console.WriteLine();
    }
}
```