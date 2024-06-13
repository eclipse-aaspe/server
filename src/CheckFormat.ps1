<#
.SYNOPSIS
This script checks the format of the code using dotnet-format.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    AssertDotnet, `
     AssertDotnetFormatVersion, `
     GetArtefactsDir

function Main
{
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
    dotnet format --verify-no-changes --report $reportPath --exclude "**/DocTest*.cs"
    $formatReport = Get-Content $reportPath | ConvertFrom-Json
    if ($formatReport.Count -ge 1)
    {
        Write-Host "There are $( $formatReport.Count ) dotnet-format issue(s):"
        foreach ($issue in $formatReport)
        {
            Write-Host "  - $( $issue.FileName ): Line $( $issue.LineSpan.StartLinePosition.Line ), Column $( $issue.LineSpan.StartLinePosition.Character ): $( $issue.Failure.Message )"
        }
        throw "Formatting issues found. The report is stored in: $reportPath"
    }
    else
    {
        Write-Host "No formatting issues found."
    }
}

$previousLocation = Get-Location
try
{
    Main
}
finally
{
    Set-Location $previousLocation
}
