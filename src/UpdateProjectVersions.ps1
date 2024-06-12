param(
    [Parameter(HelpMessage = "Version to be updated in the project files", Mandatory = $true)]
    [string]
    $version
)

# Set error action preference to stop on errors.
$ErrorActionPreference = "Stop"

function UpdateProjectVersions {
    param (
        [string]$version
    )

    # Get all csproj files in the solution.
    $projectFiles = Get-ChildItem -Path "$PSScriptRoot\.." -Recurse -Filter *.csproj

    # Iterate through each project file and update the <Version> tag.
    foreach ($file in $projectFiles) {
        # Check if the file path contains "obsolete" (case-insensitive).
        if ($file.FullName -match 'obsolete') {
            Write-Host "Skipping obsolete file: $($file.FullName)"
            continue
        }

        Write-Host "Updating version in $($file.FullName)"
        (Get-Content -Path $file.FullName) | ForEach-Object {
            $_ -replace '<Version>.*<\/Version>', "<Version>$version</Version>"
        } | Set-Content -Path $file.FullName
    }
}

# Execute the function
UpdateProjectVersions -version $version
