param(
    [Parameter(HelpMessage = "Suffix to be appended to the version (e.g., alpha, beta)", Mandatory = $false)]
    [string]
    $suffix = "alpha"
)

# Set error action preference to stop on errors.
$ErrorActionPreference = "Stop"

# Import necessary functions from Common.psm1 module.
Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function GetArtefactsDir

function GetNextBuildNumber {
    # Read the current build number from the file.
    $currentBuild = Get-Content (Join-Path $PSScriptRoot "current_build_number.cfg") | ForEach-Object { $_.Trim() }

    # Increment the build number and save it back to the file.
    $nextBuild = [int]$currentBuild + 1
    $nextBuild | Set-Content (Join-Path $PSScriptRoot "current_build_number.cfg")

    # Return the incremented build number.
    return $nextBuild
}

function GetVersionCore {
    # Read the current version from the file.
    $versionCore = Get-Content (Join-Path $PSScriptRoot "current_version.cfg") | ForEach-Object { $_.Trim() }

    # Return the version core.
    return $versionCore
}

function GetBuildSuffix {
    # Determine if the build is on the main or release branch.
    $branch = git branch --show-current

    if ($branch -eq "main") {
        return "latest"
    }
    elseif ($branch -eq "release") {
        return "stable"
    }
    else {
        throw "Unknown branch: $branch"
    }
}

function GetVersion {
    # Get the version core from the file.
    $versionCore = GetVersionCore

    # Get the build suffix based on the branch.
    $buildSuffix = GetBuildSuffix

    # Get the next build number.
    $buildNumber = GetNextBuildNumber

    # Construct the semantic version.
    $semanticVersion = "$versionCore+$buildNumber-$suffix-$buildSuffix"

    return $semanticVersion
}

function UpdateProjectVersions($version) {
    # Get all csproj files in the solution.
    $projectFiles = Get-ChildItem -Path "$(Get-ArtefactsDir)\..\" -Recurse -Filter *.csproj

    # Iterate through each project file and update the <Version> tag.
    foreach ($file in $projectFiles) {
        Write-Host "Updating version in $($file.FullName)"
        (Get-Content -Path $file.FullName) | ForEach-Object {
            $_ -replace '<Version>.*<\/Version>', "<Version>$version<\/Version>"
        } | Set-Content -Path $file.FullName
    }
}

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
    # Get the semantic version.
    $version = GetVersion

    # Validate the generated semantic version.
    if ($version -eq "") {
        throw "Failed to generate semantic version."
    }

    # Update project versions.
    UpdateProjectVersions $version

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
