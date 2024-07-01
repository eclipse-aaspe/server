<#*******************************************************************************
* Copyright (c) {2024} Contributors to the Eclipse Foundation
*
* See the NOTICE file(s) distributed with this work for additional
* information regarding copyright ownership.
*
* This program and the accompanying materials are made available under the
* terms of the Apache License Version 2.0 which is available at
* https://www.apache.org/licenses/LICENSE-2.0
*
* SPDX-License-Identifier: Apache-2.0
*******************************************************************************#>

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
    LogAndExecute "$( Join-Path $PSScriptRoot "CheckFormat.ps1" )"
    LogAndExecute "$( Join-Path $PSScriptRoot "CheckHelpInReadme.ps1" )"
}

$previousLocation = Get-Location; try
{
    Main
}
finally
{
    Set-Location $previousLocation
}
