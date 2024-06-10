param(
    [Parameter(HelpMessage = "Suffix to be appended to the version (e.g., alpha, beta)", Mandatory = $false)]
    [string]
    $suffix = "alpha"
)

# Set error action preference to stop on errors.
$ErrorActionPreference = "Stop"

function GetNextBuildNumber
{
    # Read the current build number from the file.
    $currentBuild = Get-Content (Join-Path $PSScriptRoot "current_build_number.cfg") | ForEach-Object { $_.Trim() }

    # Increment the build number and save it back to the file.
    $nextBuild = [int]$currentBuild + 1
    $nextBuild | Set-Content (Join-Path $PSScriptRoot "current_build_number.cfg")

    # Return the incremented build number.
    return $nextBuild
}

function GetVersionCore
{
    # Read the current version from the file.
    $versionCore = Get-Content (Join-Path $PSScriptRoot "current_version.cfg") | ForEach-Object { $_.Trim() }

    # Return the version core.
    return $versionCore
}

function GetBuildSuffix
{
    # Determine if the build is on the main or release branch.
    $branch = git branch --show-current 2> $null  # Suppress errors if not in a Git repository
    if (-not $?)
    {
        Write-Host "Not in a Git repository. Assuming develop branch."
        return "develop"
    }

    if ($branch -eq "main")
    {
        return "latest"
    }
    elseif ($branch -eq "release")
    {
        return "stable"
    }
    else
    {
        Write-Host "Not main or release branch. Assuming develop branch."
        return "develop"
    }
}

function GetVersion
{
    # Get the version core from the file.
    $versionCore = GetVersionCore

    # Get the build suffix based on the branch.
    $buildSuffix = GetBuildSuffix

    # Get the next build number.
    $buildNumber = GetNextBuildNumber

    $aasmodel = "aasV3"

    # Construct the semantic version.
    $semanticVersion = "$versionCore-$buildNumber-$aasmodel-$suffix-$buildSuffix"

    return $semanticVersion
}

# Generate the version and print it to output.
$version = GetVersion
Write-Output $version
