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
