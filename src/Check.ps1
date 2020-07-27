<#
.SYNOPSIS
This script runs all the pre-merge checks locally.
#>

$ErrorActionPreference = "Stop"

function Main
{
    Set-Location $PSScriptRoot
    .\CheckPushCommitMessages.ps1
}

$previousLocation = Get-Location; try { Main } finally { Set-Location $previousLocation }
