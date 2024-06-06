<#
.SYNOPSIS
This script checks the format of the code using dotnet-format.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    AssertDotnet, `
    AssertDotnetFormatVersion, `
    GetArtefactsDir

function Main {
    AssertDotnetFormatVersion

    $scriptDir = $PSScriptRoot
    Write-Host "Changing to script directory: $scriptDir"
    Set-Location $scriptDir

    Write-Host "Inspecting the code format with dotnet-format..."

    Write-Host "Restoring dotnet tools..."
    dotnet tool restore

    $artefactsDir = GetArtefactsDir
    Write-Host "Creating artefacts directory: $artefactsDir"
    New-Item -ItemType Directory -Force -Path $artefactsDir | Out-Null

    $reportPath = Join-Path $artefactsDir "dotnet-format-report.json"
    Write-Host "Report path: $reportPath"

    Write-Host "Running dotnet format..."
    $result = dotnet format --verify-no-changes --report $reportPath --exclude "**/DocTest*.cs" 2>&1
    Write-Host "dotnet format output:"
    Write-Host $result

    if (Test-Path $reportPath) {
        Write-Host "Report generated successfully."
        $formatReport = Get-Content $reportPath | ConvertFrom-Json
        if ($formatReport.Count -ge 1) {
            throw "There are $($formatReport.Count) dotnet-format issue(s). The report is stored in: $reportPath"
        } else {
            Write-Host "No formatting issues found."
        }
    } else {
        throw "The report file $reportPath was not generated."
    }
}

$previousLocation = Get-Location
try {
    Main
} finally {
    Set-Location $previousLocation
}
