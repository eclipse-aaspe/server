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
            throw "dotnet could not be found in the PATH. Look if you could find it, e.g., in " + `
               "$( Join-Path $env:LOCALAPPDATA "Microsoft\dotnet" ) and add it to PATH."
        }
    }
}

function GetArtefactsDir
{
    $repoRoot = Split-Path $PSScriptRoot -Parent
    $artefactsDir = Join-Path $repoRoot "artefacts"
    return $artefactsDir
}

Export-ModuleMember -Function `
    AssertDotnet, `
    GetArtefactsDir