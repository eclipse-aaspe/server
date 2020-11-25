<#
.DESCRIPTION
This script checks that the help output from the program and the message
documented in the Readme coincide.
#>

Import-Module (Join-Path $PSScriptRoot Common.psm1) -Function `
    AssertDotnet


function Main
{
    AssertDotnet
    Set-Location $PSScriptRoot

    & dotnet run --project Script.CheckHelpInReadme
}

$previousLocation = Get-Location; try
{
    Main
}
finally
{
    Set-Location $previousLocation
}
