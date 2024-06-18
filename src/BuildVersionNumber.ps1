param(
    [Parameter(HelpMessage = "Suffix to be appended to the version (e.g., alpha, beta)", Mandatory = $false)]
    [string]
    $suffix = "alpha",

    [Parameter(HelpMessage = "Branch name to determine the build suffix (e.g., main, release, feature/*)", Mandatory = $false)]
    [string]
    $branch = "develop",

    [Parameter(HelpMessage = "The GitHub run number", Mandatory = $true)]
    [int]
    $githubRunNumber
)

# Set error action preference to stop on errors.
$ErrorActionPreference = "Stop"

function GetVersionCore {
    # Read the current version from the file.
    $versionCore = Get-Content (Join-Path $PSScriptRoot "current_version.cfg") | ForEach-Object { $_.Trim() }

    # Return the version core.
    return $versionCore
}

function GetBuildSuffix {
    param (
        [string] $branch
    )

    if ($branch -eq "main") {
        return "latest"
    }
    elseif ($branch -eq "release") {
        return "stable"
    }
    else {
        Write-Host "Not main or release branch. Assuming develop branch."
        return "develop"
    }
}

function GetVersion {
    # Get the version core from the file.
    $versionCore = GetVersionCore

    # Get the build suffix based on the branch.
    $buildSuffix = GetBuildSuffix -branch $branch

    # Use the GitHub run number as the build number.
    $buildNumber = $githubRunNumber

    $aasmodel = "aasV3"

    if ([string]::IsNullOrEmpty($suffix)) {
        $suffix = "alpha"
    }
    
    # Construct the semantic version.
    $semanticVersion = "$versionCore.$buildNumber-$aasmodel-$suffix-$buildSuffix"

    return $semanticVersion
}

# Generate the version and print it to output.
$version = GetVersion
Write-Output $version
