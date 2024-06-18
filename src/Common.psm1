<#
.SYNOPSIS
This module contains common functions for continuous integration.
#>

<#
.SYNOPSIS
Asserts that dotnet is on the path.
#>
function AssertDotnet
{
    if (!(Get-Command "dotnet" -ErrorAction SilentlyContinue))
    {
        if ($null -eq $env:LOCALAPPDATA)
        {
            throw "dotnet could not be found in the PATH."
        }
        else
        {
            throw "dotnet could not be found in the PATH. Look if you could find it, e.g., in " +  `
                "$( Join-Path $env:LOCALAPPDATA "Microsoft\dotnet" ) and add it to PATH."
        }
    }
}

function FindDotnetToolVersion($PackageID)
{
    AssertDotnet

    $version = ''

    $lines = (dotnet tool list)|Select-Object -Skip 2
    $lines += (dotnet tool list -g)|Select-Object -Skip 2
    ForEach ($line in $( $lines -split "`r`n" ))
    {
        $parts = $line -Split '\s+'
        if ($parts.Count -lt 3)
        {
            throw "Expected at least 3 columns in a line of `dotnet tool list`, got output: ${lines}"
        }

        $aPackageID = $parts[0]
        $aPackageVersion = $parts[1]

        if ($aPackageID -eq $PackageID)
        {
            $version = $aPackageVersion
            break
        }
    }

    return $version
}

<#
.SYNOPSIS
Check the version of the given dotnet tool.
 #>
function AssertDotnetToolVersion($PackageID, $ExpectedVersion)
{
    AssertDotnet

    $version = FindDotnetToolVersion -PackageID $PackageID
    if ($version -eq '')
    {
        throw "No $PackageID could be found. Have you installed it? " +  `
                "Check the list of the installed dotnet tools with: " +  `
                "`dotnet tool list` and `dotnet tool list -g`."
    }
    else
    {
        if ($version -ne $ExpectedVersion)
        {
            throw "Expected $PackageID version $ExpectedVersion, but got: $version;" +  `
                    "Check the list of the installed dotnet tools with: " +  `
                    "`dotnet tool list` and `dotnet tool list -g`."
        }
        # else: the version is correct.
    }
}

<#
.SYNOPSIS
Check the version of dotnet-format so that the code is always formatted in the same manner.
#>
function AssertDotnetFormatVersion
{
    AssertDotnetToolVersion -packageID "dotnet-format" -expectedVersion "4.0.130203"
}

function GetArtefactsDir
{
    $repoRoot = Split-Path $PSScriptRoot -Parent
    $artefactsDir = Join-Path $repoRoot "artefacts"
    return $artefactsDir
}

Export-ModuleMember -Function `
    AssertDotnet, `
     AssertDotnetFormatVersion, `
     GetArtefactsDir