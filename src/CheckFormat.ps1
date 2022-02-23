<#
.SYNOPSIS
This script checks the format of the code.
#>

$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    AssertDotnet,  `
    AssertDotnetFormatVersion,  `
    GetArtefactsDir

function Main
{
    AssertDotnetFormatVersion

    Set-Location $PSScriptRoot
    Write-Host "Inspecting the code format with dotnet-format..."

    $artefactsDir = GetArtefactsDir
    New-Item -ItemType Directory -Force -Path $artefactsDir|Out-Null

    $reportPath = Join-Path $artefactsDir "dotnet-format-report.json"
    dotnet format --verify-no-changes --report $reportPath --exclude "**/DocTest*.cs"
    $formatReport = Get-Content $reportPath |ConvertFrom-Json
    if ($formatReport.Count -ge 1)
    {
        throw "There are $( $formatReport.Count ) dotnet-format issue(s). " +  `
             "The report is stored in: $reportPath"
    }
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
