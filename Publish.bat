@echo off
if not exist nuget_pub (
    md nuget_pub
)

for /R "nuget_pub" %%s in (*) do (
    del %%s
)

dotnet pack src/DotNetCore.Collections.Paginable -c Release -o ../../nuget_pub
dotnet pack src/DotNetCore.Collections.Paginable.Chloe -c Release -o ../../nuget_pub
dotnet pack src/DotNetCore.Collections.Paginable.DosOrm -c Release -o ../../nuget_pub
dotnet pack src/DotNetCore.Collections.Paginable.EntityFrameworkCore -c Release -o ../../nuget_pub
dotnet pack src/DotNetCore.Collections.Paginable.FreeSql -c Release -o ../../nuget_pub
dotnet pack src/DotNetCore.Collections.Paginable.NHibernate -c Release -o ../../nuget_pub
dotnet pack src/DotNetCore.Collections.Paginable.SqlKata -c Release -o ../../nuget_pub
dotnet pack src/DotNetCore.Collections.Paginable.SqlSugar -c Release -o ../../nuget_pub

for /R "nuget_pub" %%s in (*symbols.nupkg) do (
    del %%s
)

echo.
echo.

set /p key=input key:
set source=https://www.nuget.org/api/v2/package

for /R "nuget_pub" %%s in (*.nupkg) do ( 
    call nuget push %%s %key% -Source %source%	
	echo.
)

pause