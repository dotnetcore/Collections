# Collections

[![NuGet](https://img.shields.io/nuget/v/DotNetCore.Collections.Paginable.svg)](https://www.nuget.org/packages/DotNetCore.Collections.Paginable/)
[![Member project of .NET Core Community](https://img.shields.io/badge/member%20project%20of-NCC-9e20c9.svg)](https://github.com/dotnetcore)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://raw.githubusercontent.com/dotnetcore/CAP/master/LICENSE.txt)
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fdotnetcore%2FCollections.svg?type=shield)](https://app.fossa.io/projects/git%2Bgithub.com%2Fdotnetcore%2FCollections?ref=badge_shield)

Utilities and extensions for Collections.

# Install

for Paginable:

```
Install-Package DotNetCore.Collections.Paginable
```

# Usage

[Example for Paginable x EFCore](https://github.com/dotnetcore/Collections/blob/dev/sample/Sample.EfCore/Program.cs)

[Example for Paginable x EF](https://github.com/dotnetcore/Collections/blob/dev/sample/Sample.Ef/Program.cs)


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

[![Member project of .NET China Foundation](https://github.com/dotnetcore/Home/blob/master/icons/member-project-of-netchina2.png)](https://github.com/dotnetcore)


## License
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fdotnetcore%2FCollections.svg?type=large)](https://app.fossa.io/projects/git%2Bgithub.com%2Fdotnetcore%2FCollections?ref=badge_large)
