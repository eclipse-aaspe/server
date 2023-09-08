param(
    [Parameter(HelpMessage = "Version to be packaged", Mandatory = $true)]
    [string]
    $version
)

<#
.SYNOPSIS
This script packages files to be released.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    GetArtefactsDir

function PackageRelease($outputDir)
{
    $baseBuildDir = Join-Path $( GetArtefactsDir ) "build" `
        | Join-Path -ChildPath "Release"

    $targets = $(
    "AasxServerBlazor"
    "AasxServerCore"
    )

    New-Item -ItemType Directory -Force -Path $outputDir|Out-Null

    foreach ($target in $targets)
    {
        $buildDir = Join-Path $baseBuildDir $target

        if (!(Test-Path $buildDir))
        {
            throw ("The build directory with the release does " +
                    "not exist: $buildDir; did you build the targets " +
                    "with BuildForRelease.ps1?")
        }

        $archPath = Join-Path $outputDir "$target.zip"

        Write-Host "Compressing to: $archPath"

        Compress-Archive `
            -Path $buildDir `
            -DestinationPath $archPath
    }

    # Do not copy the source code in the releases.
    # The source code will be distributed automatically through Github releases.

    Write-Host "Done packaging the release."
}

function Main
{
    if ($version -eq "")
    {
        throw "Unexpected empty version"
    }

    $versionRe = [Regex]::new(
            '^[0-9]{4}-(0[1-9]|10|11|12)-(0[1-9]|1[0-9]|2[0-9]|3[0-1])' +
            '(\.(alpha|beta))?$')

    $latestVersionRe = [Regex]::new('^LATEST(\.(alpha|beta))?$')

    if ((!$latestVersionRe.IsMatch($version)) -and
            (!$versionRe.IsMatch($version)))
    {
        throw ("Unexpected version; " +
                "expected either year-month-day (*e.g.*, 2019-10-23) " +
                "followed by an optional maturity tag " +
                "(*e.g.*, 2019-10-23.alpha) " +
                "or LATEST, but got: $version")
    }

    $outputDir = Join-Path $( GetArtefactsDir ) "release" `
        | Join-Path -ChildPath $version

    if (Test-Path $outputDir)
    {
        Write-Host ("Removing previous release so that " +
                "the new release is packaged clean: $outputDir")
        Remove-Item -Recurse -Force $outputDir
    }

    PackageRelease -outputDir $outputDir
}

$previousLocation = Get-Location; try
{
    Main
}
finally
{
    Set-Location $previousLocation
}
