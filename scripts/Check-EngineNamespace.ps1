<#
    F7-07 namespace governance guard -- the source-level half.

    WHY THIS EXISTS
    ---------------
    The engine's extension methods are declared in `DotNetCore.Collections` on purpose. If any
    of them were declared under `System.Linq` (or anywhere else rooted at `System.`), the engine
    would be injecting same-signature extensions into the BCL's own namespace: a consumer that
    writes

        using System.Linq;
        using DotNetCore.Collections;

    would then get CS0121 on every colliding call, and the ambiguity would be the engine's fault,
    not the consumer's. That is the risk R7-05 names, and this guard is one of the two halves of
    the mitigation.

    WHY IT IS A SOURCE SCAN AND NOT ONLY A REFLECTION TEST
    ------------------------------------------------------
    The reflection half lives in tests/DotNetCore.Collections.Tests/NamespaceGovernanceTests.cs
    and inspects the *built* assembly. It therefore cannot see a `namespace System.Linq` that the
    TFM it runs on compiles away, and it cannot see a namespace that is declared but not yet
    referenced. Scanning the sources sees every declaration, on every TFM.

    SCOPE
    -----
    src/DotNetCore.Collections only. `obj/` and `bin/` are skipped: the generated
    *AssemblyInfo.cs files are the SDK's output, not a governance surface.

    EXIT CODES
    ----------
    0  every declared namespace is DotNetCore.Collections or a descendant of it
    1  at least one declaration escapes
    2  the engine source directory is missing (the guard would otherwise pass vacuously)
#>

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$engineSource = Join-Path $repoRoot 'src/DotNetCore.Collections'
$allowedRoot = 'DotNetCore.Collections'

if (-not (Test-Path -LiteralPath $engineSource -PathType Container)) {
    Write-Host "guard: engine source directory not found: $engineSource"
    exit 2
}

$violations = New-Object 'System.Collections.Generic.List[string]'
$scanned = 0

foreach ($file in (Get-ChildItem -LiteralPath $engineSource -Recurse -Filter '*.cs' -File)) {
    if ($file.FullName -match '[\\/](obj|bin)[\\/]') {
        continue
    }

    $scanned++
    $relative = $file.FullName.Substring($repoRoot.Length).TrimStart('\', '/').Replace('\', '/')
    $lineNumber = 0

    foreach ($rawLine in [System.IO.File]::ReadAllLines($file.FullName)) {
        $lineNumber++
        $line = $rawLine.TrimStart()

        # Skip line comments (including XML doc comments) and block-comment continuations.
        if ($line.StartsWith('//') -or $line.StartsWith('*')) {
            continue
        }

        # Matches `namespace X.Y`, `namespace X.Y {` and `namespace X.Y;` at the start of a line.
        if ($line -notmatch '^namespace\s+([A-Za-z_][A-Za-z0-9_.]*)\s*(\{\s*)?$') {
            continue
        }

        $declared = $Matches[1]
        if ($declared -eq $allowedRoot -or $declared.StartsWith($allowedRoot + '.')) {
            continue
        }

        $violations.Add("  $relative`:$lineNumber`tnamespace $declared")
    }
}

if ($violations.Count -gt 0) {
    Write-Host "guard: $($violations.Count) namespace declaration(s) escape '$allowedRoot'."
    Write-Host 'Every declaration under src/DotNetCore.Collections must be rooted at the engine namespace'
    Write-Host 'so that the engine can never inject an extension method into System.Linq (CS0121, R7-05).'
    Write-Host ''
    foreach ($violation in $violations) {
        Write-Host $violation
    }

    exit 1
}

Write-Host "guard: $scanned file(s) scanned, all namespace declarations rooted at '$allowedRoot'."
exit 0
