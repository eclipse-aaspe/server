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

    $baseBuildDir = Join-Path $( GetArtefactsDir ) "build" `
        | Join-Path -ChildPath "Release"

    if ($clean)
    {
        Write-Host "Removing the build directory: $baseBuildDir"
        Remove-Item -LiteralPath $baseBuildDir -Force -Recurse
    }
    else
    {
        AssertDotnet

        $targets = $(
        "AasxBlazor"
        "AasxServerCore"
        <#
        TODO (mristin, 2020-09-01): AasxServerWindows does not compile due to
        an error related to missing dependencies.
        #>
        #"AasxServerWindows"
        )

        foreach ($target in $targets)
        {
            $buildDir = Join-Path $baseBuildDir $target
            Write-Host "Publishing $target to: $buildDir"
            dotnet.exe publish -c Release -o $buildDir $target
        }
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
