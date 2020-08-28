param(
    [Parameter(HelpMessage = "If set, cleans up the previous build instead of performing a new one")]
    [switch]
    $clean = $false
)

<#
.DESCRIPTION
This script builds the solution for debugging (manual or automatic testing).
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    AssertDotnet, `
    GetArtefactsDir

function Main
{
    Set-Location $PSScriptRoot

    $buildDir = Join-Path $( GetArtefactsDir ) "build" `
        | Join-Path -ChildPath "Release" `
        | Join-Path -ChildPath "aasx-server"

    if ($clean)
    {
        Write-Host "Removing the build directory: $buildDir"
        Remove-Item -LiteralPath $buildDir -Force -Recurse
    }
    else
    {
        AssertDotnet
        Write-Host "Publishing the solution to: $buildDir"
        dotnet.exe publish -c Release -o $buildDir
    }
}

$previousLocation = Get-Location; try
{
    Main
}
finally
{
    Set-Location $previousLocation
}
