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

function CopyContentForDemo($Destination)
{
    $contentForDemoDir = Join-Path (Split-Path -Parent $PSScriptRoot) "content-for-demo"
    if (!(Test-Path $contentForDemoDir))
    {
       throw "The directory with the content for demo does not exist: $contentForDemoDir"
    }

    Write-Host "Copying content for demo from $contentForDemoDir to: $Destination"

    Get-ChildItem -Path $contentForDemoDir `
        | Copy-Item -Destination $Destination -Recurse -Container -Force
}

function Main
{
    Set-Location $PSScriptRoot

    $baseBuildDir = Join-Path $( GetArtefactsDir ) "build" `
        | Join-Path -ChildPath "Release"

    if ($clean)
    {
        Write-Host "dotnet clean'ing ..."
        dotnet clean
        if ($LASTEXITCODE -ne 0)
        {
            throw "Failed to dotnet clean."
        }

        Write-Host "Removing the build directory: $baseBuildDir"
        Remove-Item -LiteralPath $baseBuildDir -Force -Recurse
    }
    else
    {
        AssertDotnet

        ##
        # Build dotnet targets
        ##

        $dotnetTargets = $(
        "AasxServerBlazor"
        "AasxServerCore"
        )

        foreach ($target in $dotnetTargets)
        {
            $buildDir = Join-Path $baseBuildDir $target

            Write-Host "Publishing with dotnet $target to: $buildDir"

            dotnet publish -c Release -o $buildDir $target
            if ($LASTEXITCODE -ne 0)
            {
                throw "Failed to dotnet publish: $target"
            }

            CopyContentForDemo -Destination $buildDir
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
