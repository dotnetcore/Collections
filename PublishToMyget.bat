@echo off
setlocal

rem Publishes every DotNetCore.Collections package (Multi + Paginable family) to the MyGet feed.
rem Usage: PublishToMyget.bat
rem        set MYGET_API_KEY=<key> && PublishToMyget.bat      (non-interactive / CI)
rem Requires: .NET SDK 8.0 or later on PATH.
rem Note: the MyGet v2 endpoint does not accept .snupkg, so only .nupkg is pushed here.

if not exist nuget_pub (
    md nuget_pub
)

for /R "nuget_pub" %%s in (*) do (
    del "%%s"
)

if defined MYGET_API_KEY (
    set "key=%MYGET_API_KEY%"
) else (
    set /p key=input myget api key:
)

if not defined key (
    echo ERROR: no api key provided.
    goto :failed
)

rem -m:1 is REQUIRED: DocumentationFile is a single file in the project directory, so
rem parallel per-TFM builds race on it and fail with CS0016 ("file is being used by another
rem process"). Serialising the inner builds keeps packing deterministic.
rem Long-term fix: move DocumentationFile under the per-TFM output directory.

rem ::Multi
dotnet pack src/DotNetCore.Collections.Multi -c Release -m:1 -o nuget_pub || goto :failed

rem ::Paginable
dotnet pack src/DotNetCore.Collections.Paginable -c Release -m:1 -o nuget_pub || goto :failed
dotnet pack src/DotNetCore.Collections.Paginable.Chloe -c Release -m:1 -o nuget_pub || goto :failed
dotnet pack src/DotNetCore.Collections.Paginable.DosOrm -c Release -m:1 -o nuget_pub || goto :failed
dotnet pack src/DotNetCore.Collections.Paginable.EntityFramework -c Release -m:1 -o nuget_pub || goto :failed
dotnet pack src/DotNetCore.Collections.Paginable.EntityFrameworkCore -c Release -m:1 -o nuget_pub || goto :failed
dotnet pack src/DotNetCore.Collections.Paginable.FreeSql -c Release -m:1 -o nuget_pub || goto :failed
dotnet pack src/DotNetCore.Collections.Paginable.FreeSql.DbContext -c Release -m:1 -o nuget_pub || goto :failed
dotnet pack src/DotNetCore.Collections.Paginable.NHibernate -c Release -m:1 -o nuget_pub || goto :failed
dotnet pack src/DotNetCore.Collections.Paginable.SqlKata -c Release -m:1 -o nuget_pub || goto :failed
dotnet pack src/DotNetCore.Collections.Paginable.SqlSugar -c Release -m:1 -o nuget_pub || goto :failed

rem MyGet v2 endpoint rejects .snupkg - drop them before pushing.
for /R "nuget_pub" %%s in (*.snupkg) do (
    del "%%s"
)

echo.
echo.

set source=https://www.myget.org/F/alexinea/api/v2/package

for /R "nuget_pub" %%s in (*.nupkg) do (
    call dotnet nuget push "%%s" --api-key %key% --source %source% --skip-duplicate || goto :failed
    echo.
)

echo.
echo All packages published.
endlocal
exit /b 0

:failed
echo.
echo Publish FAILED - see the errors above.
endlocal
exit /b 1
