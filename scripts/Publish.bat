@echo off
setlocal

rem Local fallback for publishing every DotNetCore.Collections package
rem (Multi + Paginable family) to nuget.org.
rem The primary publishing path is the GitHub Actions "Release" workflow, which
rem runs on tag pushes and reads the key from the NUGET_API_KEY repository secret.
rem
rem Usage (run from anywhere):
rem        scripts\Publish.bat
rem        set NUGET_API_KEY=<key> && scripts\Publish.bat      (non-interactive)
rem Requires: .NET SDK 8.0 or later on PATH.

pushd "%~dp0.." || goto :failed

if not exist nuget_pub (
    md nuget_pub
)

for /R "nuget_pub" %%s in (*) do (
    del "%%s"
)

if defined NUGET_API_KEY (
    set "key=%NUGET_API_KEY%"
) else (
    set /p key=input nuget.org api key:
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

echo.
echo.

set source=https://api.nuget.org/v3/index.json

for /R "nuget_pub" %%s in (*.nupkg) do (
    call dotnet nuget push "%%s" --api-key %key% --source %source% --skip-duplicate || goto :failed
    echo.
)

rem .snupkg symbol packages are published to the same endpoint (SourceLink enabled since 6.0).
for /R "nuget_pub" %%s in (*.snupkg) do (
    call dotnet nuget push "%%s" --api-key %key% --source %source% --skip-duplicate || goto :failed
    echo.
)

echo.
echo All packages published.
popd
endlocal
exit /b 0

:failed
echo.
echo Publish FAILED - see the errors above.
popd
endlocal
exit /b 1
