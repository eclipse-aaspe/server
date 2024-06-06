param(
    [Parameter(HelpMessage = "Version to be packaged", Mandatory = $true)]
    [string]
    $version
)

<#
.SYNOPSIS
This script packages files to be released.
#>

# Set error action preference to stop on errors.
$ErrorActionPreference = "Stop"

# Import necessary functions from Common.psm1 module.
Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function GetArtefactsDir

function PackageRelease($outputDir)
{
    # Define the base directory where built release files are stored.
    $baseBuildDir = Join-Path $( GetArtefactsDir ) "build" | Join-Path -ChildPath "Release"

    # List of targets to package.
    $targets = @(
        "AasxServerBlazor"
        "AasxServerAspNetCore"
    )

    # Create output directory if it does not exist.
    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

    foreach ($target in $targets)
    {
        $buildDir = Join-Path $baseBuildDir $target

        # Check if build directory exists.
        if (!(Test-Path $buildDir))
        {
            throw ("The build directory with the release does not exist: $buildDir;" +
                    "did you build the targets with BuildForRelease.ps1?")
        }

        $archPath = Join-Path $outputDir "$target.zip"

        Write-Host "Compressing to: $archPath"

        # Compress build directory to create release archive.
        Compress-Archive -Path $buildDir -DestinationPath $archPath
    }

    # Do not copy the source code in the releases.
    # The source code will be distributed automatically through GitHub releases.

    Write-Host "Done packaging the release."
}

function Main
{
    # Validate version parameter.
    if ($version -eq "")
    {
        throw "Unexpected empty version"
    }

    # Regular expressions for version validation.
    $versionRe = [Regex]::new(
            '^[0-9]{4}-(0[1-9]|10|11|12)-(0[1-9]|1[0-9]|2[0-9]|3[0-1])' +
                    '(\.(alpha|beta))?$')
    $latestVersionRe = [Regex]::new('^LATEST(\.(alpha|beta))?$')

    # Validate the provided version.
    if ((!$latestVersionRe.IsMatch($version)) -and
            (!$versionRe.IsMatch($version)))
    {
        throw ("Unexpected version; expected either year-month-day (*e.g., 2019-10-23) " +
                "followed by an optional maturity tag (*e.g., 2019-10-23.alpha) " +
                "or LATEST, but got: $version")
    }

    # Define the output directory for the packaged release.
    $outputDir = Join-Path $( GetArtefactsDir ) "release" | Join-Path -ChildPath $version

    # Remove previous release directory if it exists.
    if (Test-Path $outputDir)
    {
        Write-Host ("Removing previous release so that the new release is packaged clean: $outputDir")
        Remove-Item -Recurse -Force $outputDir
    }

    # Package the release.
    PackageRelease -outputDir $outputDir
}

# Store the current location, execute the main function, and return to the original location.
$previousLocation = Get-Location
try
{
    Main
}
finally
{
    Set-Location $previousLocation
}
