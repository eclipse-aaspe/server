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
.DESCRIPTION
This script checks that the help output from the program and the message
documented in the Readme coincide.
#>

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function AssertDotnet

function Main
{
    # Ensure dotnet is installed.
    AssertDotnet

    # Change to the script root directory.
    Set-Location $PSScriptRoot

    # Execute the program to check help output against README.
    & dotnet run --project Script.CheckHelpInReadme
}

# Store the current location, execute the main function, and return to the original location.
$previousLocation = Get-Location
try
{
    Main
}
finally
{
    Set-Location $previousLocation
}
