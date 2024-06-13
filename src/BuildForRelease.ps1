param(
    [Parameter(HelpMessage = "If set, cleans up the previous build instead of performing a new one")]
    [switch]
    $clean = $false
)

<#
.DESCRIPTION
This script builds the solution for debugging (manual or automatic testing) or cleans up the previous build.
#>

# Set error action preference to stop on errors.
$ErrorActionPreference = "Stop"

# Import necessary functions from the Common module.
Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function AssertDotnet, GetArtefactsDir

function CopyContentForDemo($Destination) {
    $contentForDemoDir = Join-Path (Split-Path -Parent $PSScriptRoot) "content-for-demo"
    if (!(Test-Path $contentForDemoDir)) {
        throw "The directory with the content for demo does not exist: $contentForDemoDir"
    }

    Write-Host "Copying content for demo from $contentForDemoDir to: $Destination"
    Get-ChildItem -Path $contentForDemoDir | Copy-Item -Destination $Destination -Recurse -Container -Force
}

function Main {
    # Change to the script root directory.
    Set-Location $PSScriptRoot

    # Define the base build directory.
    $baseBuildDir = Join-Path $(GetArtefactsDir) "build" | Join-Path -ChildPath "Release"

    if ($clean) {
        # Clean up previous build if requested.
        Write-Host "Cleaning up previous build..."
        dotnet clean
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to clean the project."
        }

        Write-Host "Removing the build directory: $baseBuildDir"
        if (Test-Path $baseBuildDir) {
            Remove-Item -LiteralPath $baseBuildDir -Force -Recurse
        } else {
            Write-Host "Build directory does not exist: $baseBuildDir"
        }
    } else {
        # Ensure dotnet is installed.
        AssertDotnet

        # Build dotnet targets.
        $dotnetTargets = @(
            "AasxServerBlazor"
            "AasxServerAspNetCore"
        )

        foreach ($target in $dotnetTargets) {
            $buildDir = Join-Path $baseBuildDir $target

            Write-Host "Publishing $target to: $buildDir"
            dotnet publish -c Release -o $buildDir $target
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to publish the project: $target"
            }

            # Copy content for demo to the build directory.
            CopyContentForDemo -Destination $buildDir
        }
    }
}

# Store the current location, execute the main function, and return to the original location.
$previousLocation = Get-Location
try {
    Main
} finally {
    Set-Location $previousLocation
}
