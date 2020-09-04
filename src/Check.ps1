<#
.SYNOPSIS
This script runs all the pre-merge checks locally.
#>

$ErrorActionPreference = "Stop"

function LogAndExecute($Expression)
{
    Write-Host "---"
    Write-Host "Running: $Expression"
    Write-Host "---"

    Invoke-Expression $Expression
}

function Main
{
    LogAndExecute "$(Join-Path $PSScriptRoot "CheckFormat.ps1")"
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
