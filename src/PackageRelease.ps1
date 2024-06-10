param(
    [Parameter(HelpMessage = "Suffix to be appended to the version (e.g., alpha, beta)", Mandatory = $true)]
    [string]
    $version = "0.3.0"
)

# Set error action preference to stop on errors.
$ErrorActionPreference = "Stop"

# Import necessary functions from Common.psm1 module.
Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function GetArtefactsDir

function PackageRelease($outputDir, $version) {
    # Define the base directory where built release files are stored.
    $baseBuildDir = Join-Path $(GetArtefactsDir) "build" | Join-Path -ChildPath "Release"

    # List of targets to package.
    $targets = @(
        "AasxServerBlazor"
        "AasxServerAspNetCore"
    )

    # Create output directory if it does not exist.
    New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

    foreach ($target in $targets) {
        $buildDir = Join-Path $baseBuildDir $target

        # Check if build directory exists.
        if (!(Test-Path $buildDir)) {
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

function Main {
    # Validate the generated semantic version.
    if ($version -eq "") {
        throw "Failed to generate semantic version."
    }

    # Define the output directory for the packaged release.
    $outputDir = Join-Path $(GetArtefactsDir) "release" | Join-Path -ChildPath $version

    # Remove previous release directory if it exists.
    if (Test-Path $outputDir) {
        Write-Host ("Removing previous release so that the new release is packaged clean: $outputDir")
        Remove-Item -Recurse -Force $outputDir
    }

    # Package the release.
    PackageRelease -outputDir $outputDir -version $version
}

# Store the current location, execute the main function, and return to the original location.
$previousLocation = Get-Location
try {
    Main
}
finally {
    Set-Location $previousLocation
}
